"""Shared pieces of the SPIC ONE UI sweep: a tiny Chrome DevTools Protocol client,
the in-page metrics script, flag computation and the results/report writers.

Used by sweep_device.py (Android WebView over adb) and sweep_web.py (headless
Chrome/Edge). Only the standard library plus `websocket-client` are needed.
"""
from __future__ import annotations

import json
import os
import re
import time
import urllib.request
from dataclasses import dataclass, field
from typing import Any, Callable

try:
    import websocket  # websocket-client
except ImportError as exc:  # pragma: no cover
    raise SystemExit(
        "The 'websocket-client' package is required: python -m pip install websocket-client"
    ) from exc

FLAGS = ("OVERFLOW", "REDIRECT", "BLANK", "NOTFOUND", "JSERR", "ERRUI", "NETERR")

# Routes that are expected to bounce the user somewhere else once logged in.
EXPECTED_REDIRECT_WHEN_AUTHED = {"/", "/login"}

# Runs in the page after navigation. Returns a JSON object with the per-route metrics.
METRICS_JS = r"""(() => {
  const iw = innerWidth, ih = innerHeight, de = document.documentElement;
  const scrollable = (el) => {
    // An element inside a container that scrolls or clips horizontally does not
    // widen the page, so it is not reported as an offender.
    let p = el.parentElement, hops = 0;
    while (p && p !== document.body && hops < 12) {
      const ox = getComputedStyle(p).overflowX;
      if (ox === 'auto' || ox === 'scroll' || ox === 'hidden' || ox === 'clip') return true;
      p = p.parentElement; hops++;
    }
    return false;
  };
  const off = [];
  const all = document.querySelectorAll('body *');
  const limit = Math.min(all.length, 6000);
  for (let i = 0; i < limit; i++) {
    const el = all[i];
    const r = el.getBoundingClientRect();
    if (r.width > 0 && r.right > iw + 2) {
      const cs = getComputedStyle(el);
      if (cs.position === 'fixed' || cs.display === 'none' || cs.visibility === 'hidden') continue;
      if (scrollable(el)) continue;
      let name = el.tagName.toLowerCase();
      if (el.id) name += '#' + el.id;
      if (el.className && typeof el.className === 'string') {
        const cls = el.className.trim().split(/\s+/).filter(Boolean).slice(0, 2);
        if (cls.length) name += '.' + cls.join('.');
      }
      off.push({ el: name, right: Math.round(r.right), w: Math.round(r.width) });
    }
  }
  off.sort((a, b) => b.right - a.right);
  const txt = (document.body.innerText || '').trim();
  const visible = (el) => !!el && getComputedStyle(el).display !== 'none' && getComputedStyle(el).visibility !== 'hidden';
  const errUi = document.getElementById('blazor-error-ui');
  const reconnect = document.getElementById('components-reconnect-modal')
    || document.querySelector('.components-reconnect-show, .components-reconnect-failed, .components-reconnect-rejected');
  const head = txt.slice(0, 600);
  return {
    path: location.pathname,
    title: document.title,
    iw, ih,
    scrollW: de.scrollWidth,
    scrollH: de.scrollHeight,
    overflowX: de.scrollWidth > iw + 1,
    offenders: off.slice(0, 5),
    offenderCount: off.length,
    textLen: txt.length,
    textHead: txt.slice(0, 120).replace(/\s+/g, ' '),
    notFound: /not found|does not exist|sorry, there's nothing/i.test(head),
    unauthorized: /unauthori[sz]ed|access denied|no permission|forbidden/i.test(head),
    errUi: visible(errUi),
    reconnect: visible(reconnect),
    hasShell: !!document.querySelector('.app-shell, .main-content, .login-card, .pp-page'),
    hasBlazor: typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function'
  };
})()"""

# Navigates inside the SPA when Blazor exposes navigateTo; the caller falls back
# to a hard navigation when the returned value is false.
NAVIGATE_JS = """(() => {
  if (typeof Blazor !== 'undefined' && typeof Blazor.navigateTo === 'function') {
    Blazor.navigateTo(%s, false);
    return true;
  }
  return false;
})()"""


def _truncate(text: Any, n: int = 240) -> str:
    text = str(text or "").replace("\n", " ").strip()
    return text if len(text) <= n else text[: n - 3] + "..."


@dataclass
class EventLog:
    """Collects the browser events that matter for a route."""

    js_errors: list[str] = field(default_factory=list)
    console_errors: list[str] = field(default_factory=list)
    network_failures: list[str] = field(default_factory=list)
    _requests: dict[str, str] = field(default_factory=dict)

    def clear(self) -> None:
        self.js_errors.clear()
        self.console_errors.clear()
        self.network_failures.clear()
        self._requests.clear()

    def handle(self, msg: dict) -> None:
        method = msg.get("method")
        params = msg.get("params", {})
        if method == "Runtime.exceptionThrown":
            d = params.get("exceptionDetails", {})
            desc = d.get("exception", {}).get("description") or d.get("text", "")
            self.js_errors.append(_truncate(desc))
        elif method == "Runtime.consoleAPICalled" and params.get("type") == "error":
            text = " ".join(
                str(a.get("value", a.get("description", ""))) for a in params.get("args", [])
            )
            self.console_errors.append(_truncate(text))
        elif method == "Page.javascriptDialogOpening":
            # Native dialogs are a UX defect on phones (and block automation): flag them.
            self.console_errors.append(_truncate(f"DIALOG {params.get('type','')}: {params.get('message','')}"))
        elif method == "Log.entryAdded":
            entry = params.get("entry", {})
            if entry.get("level") == "error":
                src = entry.get("source", "")
                text = entry.get("text", "")
                url = entry.get("url", "")
                if src == "network":
                    self.network_failures.append(_truncate(f"{text} {url}"))
                else:
                    self.console_errors.append(_truncate(f"[{src}] {text} {url}"))
        elif method == "Network.requestWillBeSent":
            self._requests[params.get("requestId", "")] = params.get("request", {}).get("url", "")
        elif method == "Network.responseReceived":
            resp = params.get("response", {})
            status = resp.get("status", 0)
            if status >= 400:
                self.network_failures.append(_truncate(f"HTTP {status} {resp.get('url', '')}"))
        elif method == "Network.loadingFailed":
            if params.get("canceled"):
                return
            url = self._requests.get(params.get("requestId", ""), "")
            self.network_failures.append(
                _truncate(f"{params.get('errorText', 'failed')} {params.get('type', '')} {url}")
            )

    def snapshot(self) -> dict:
        dedupe = lambda xs: list(dict.fromkeys(xs))  # noqa: E731
        return {
            "jsErrors": dedupe(self.js_errors)[:5],
            "consoleErrors": dedupe(self.console_errors)[:5],
            "networkFailures": dedupe(self.network_failures)[:8],
        }


class CDP:
    """Minimal synchronous CDP client over one websocket page target."""

    def __init__(self, ws_url: str, timeout: float = 25.0, on_event: Callable[[dict], None] | None = None):
        self.ws = websocket.create_connection(ws_url, timeout=timeout, suppress_origin=True)
        self.timeout = timeout
        self._id = 0
        self.on_event = on_event or (lambda m: None)

    def close(self) -> None:
        try:
            self.ws.close()
        except Exception:
            pass

    def _dispatch(self, msg: dict) -> None:
        if "method" in msg:
            # A blocking alert()/confirm() would freeze every later CDP call, so accept it
            # immediately (fire-and-forget; the reply is an unmatched id and is ignored).
            if msg["method"] == "Page.javascriptDialogOpening":
                self._id += 1
                try:
                    self.ws.send(json.dumps({"id": self._id, "method": "Page.handleJavaScriptDialog", "params": {"accept": True}}))
                except Exception:
                    pass
            try:
                self.on_event(msg)
            except Exception:
                pass

    def send(self, method: str, params: dict | None = None) -> dict:
        self._id += 1
        mid = self._id
        self.ws.send(json.dumps({"id": mid, "method": method, "params": params or {}}))
        deadline = time.time() + self.timeout
        while time.time() < deadline:
            try:
                raw = self.ws.recv()
            except websocket.WebSocketTimeoutException:
                continue
            msg = json.loads(raw)
            if msg.get("id") == mid:
                if "error" in msg:
                    raise RuntimeError(f"{method}: {msg['error']}")
                return msg.get("result", {})
            self._dispatch(msg)
        raise TimeoutError(f"CDP call {method} timed out after {self.timeout}s")

    def evaluate(self, expression: str, await_promise: bool = True) -> Any:
        res = self.send(
            "Runtime.evaluate",
            {"expression": expression, "returnByValue": True, "awaitPromise": await_promise},
        )
        result = res.get("result", {})
        if "exceptionDetails" in res:
            d = res["exceptionDetails"]
            raise RuntimeError(d.get("exception", {}).get("description") or d.get("text"))
        return result.get("value", result.get("description"))

    def drain(self, seconds: float) -> None:
        """Pump events for `seconds` so exceptions/console/network land in the log."""
        end = time.time() + seconds
        self.ws.settimeout(0.25)
        try:
            while time.time() < end:
                try:
                    raw = self.ws.recv()
                except websocket.WebSocketTimeoutException:
                    continue
                except Exception:
                    break
                self._dispatch(json.loads(raw))
        finally:
            self.ws.settimeout(self.timeout)

    def wait_for(self, expression: str, timeout: float, interval: float = 0.4) -> bool:
        """Polls a JS expression until it is truthy; returns False on timeout."""
        end = time.time() + timeout
        while time.time() < end:
            try:
                if self.evaluate(expression):
                    return True
            except Exception:
                pass
            self.drain(interval)
        return False


def list_targets(port: int, host: str = "127.0.0.1") -> list[dict]:
    with urllib.request.urlopen(f"http://{host}:{port}/json", timeout=5) as r:
        return json.load(r)


def pick_page_target(port: int, host: str = "127.0.0.1") -> dict:
    pages = [t for t in list_targets(port, host) if t.get("type") == "page"]
    if not pages:
        raise RuntimeError(f"no page target on DevTools port {port}")
    return pages[0]


def normalize_path(p: str) -> str:
    p = (p or "").split("?")[0].split("#")[0].strip()
    p = p.rstrip("/") or "/"
    return p.lower()


def compute_flags(route: str, metrics: dict, events: dict, authed: bool, blank_chars: int = 40) -> list[str]:
    flags: list[str] = []
    if metrics.get("overflowX"):
        flags.append("OVERFLOW")
    actual = normalize_path(metrics.get("path", ""))
    expected = normalize_path(route)
    if actual != expected and not (authed and route in EXPECTED_REDIRECT_WHEN_AUTHED):
        flags.append("REDIRECT")
    if metrics.get("textLen", 0) < blank_chars:
        flags.append("BLANK")
    if metrics.get("notFound"):
        flags.append("NOTFOUND")
    if events.get("jsErrors") or events.get("consoleErrors"):
        flags.append("JSERR")
    if metrics.get("errUi") or metrics.get("reconnect"):
        flags.append("ERRUI")
    if events.get("networkFailures"):
        flags.append("NETERR")
    return flags


def slug(route: str) -> str:
    s = re.sub(r"[^A-Za-z0-9]+", "_", route).strip("_")
    return s or "root"


def sweep_route(
    cdp: CDP,
    route: str,
    wait_s: float,
    events: EventLog,
    base_url: str | None = None,
    authed: bool = True,
    settle_timeout: float = 15.0,
) -> dict:
    """Navigates to one route, waits, evaluates metrics and returns the result row."""
    events.clear()
    used_spa = False
    try:
        used_spa = bool(cdp.evaluate(NAVIGATE_JS % json.dumps(route)))
    except Exception:
        used_spa = False
    if not used_spa:
        if not base_url:
            raise RuntimeError("Blazor.navigateTo unavailable and no base URL for a hard navigation")
        cdp.send("Page.navigate", {"url": base_url.rstrip("/") + route})
        cdp.wait_for("document.readyState === 'complete' && document.body && document.body.innerText.length > 0", settle_timeout)
    # Give the circuit time to render the page, then keep waiting while the body is still empty.
    cdp.drain(wait_s)
    deadline = time.time() + settle_timeout
    metrics: dict = {}
    while True:
        try:
            metrics = cdp.evaluate(METRICS_JS) or {}
        except Exception as exc:
            metrics = {"path": "?", "textLen": 0, "evalError": str(exc)}
        if metrics.get("textLen", 0) >= 40 or time.time() > deadline:
            break
        cdp.drain(0.75)
    ev = events.snapshot()
    row = {
        "route": route,
        "path": metrics.get("path"),
        "flags": compute_flags(route, metrics, ev, authed),
        "navigation": "spa" if used_spa else "hard",
        "metrics": metrics,
        "events": ev,
    }
    return row


# ----------------------------------------------------------------------------
# Output
# ----------------------------------------------------------------------------

def write_results(out_dir: str, meta: dict, rows: list[dict]) -> str:
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "results.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"meta": meta, "results": rows}, f, indent=1)
    return path


def _offenders_md(metrics: dict) -> str:
    offs = metrics.get("offenders") or []
    if not offs:
        return ""
    return "; ".join(f"`{o['el']}` right={o['right']}" for o in offs[:3])


def _errors_md(events: dict) -> str:
    parts = []
    for key in ("jsErrors", "consoleErrors", "networkFailures"):
        for e in (events.get(key) or [])[:2]:
            parts.append(_truncate(e, 110).replace("|", "\\|"))
    return "<br>".join(parts)


def write_report(out_dir: str, meta: dict, rows: list[dict], labels: list[str]) -> str:
    """Writes report.md. `labels` are the column labels (viewport sizes or 'device');
    each row has a 'label' key. One route per table row, one column per label."""
    os.makedirs(out_dir, exist_ok=True)
    by_route: dict[str, dict[str, dict]] = {}
    order: list[str] = []
    for r in rows:
        if r["route"] not in by_route:
            by_route[r["route"]] = {}
            order.append(r["route"])
        by_route[r["route"]][r.get("label", labels[0])] = r

    total_flags = sum(len(r["flags"]) for r in rows)
    flagged_routes = sum(1 for route in order if any(by_route[route][l]["flags"] for l in by_route[route]))
    counts = {f: sum(1 for r in rows if f in r["flags"]) for f in FLAGS}

    lines = [f"# UI sweep report: {meta.get('kind', 'sweep')}", ""]
    lines.append(f"- Generated: {meta.get('generated')}")
    for k in ("baseUrl", "package", "device", "browser", "authenticated", "waitSeconds", "commit"):
        if k in meta and meta[k] not in (None, ""):
            lines.append(f"- {k}: {meta[k]}")
    lines.append(f"- Routes: {len(order)}; checks: {len(rows)}; flagged routes: {flagged_routes}; total flags: {total_flags}")
    lines.append("- Flag counts: " + ", ".join(f"{f} {counts[f]}" for f in FLAGS if counts[f]) if total_flags else "- Flag counts: none")
    lines.append("")
    lines.append("Flags: OVERFLOW = scrollWidth > innerWidth; REDIRECT = landed on another path; BLANK = body text under 40 chars; "
                 "NOTFOUND = not-found text; JSERR = JS exception or console.error; ERRUI = Blazor error UI or reconnect modal shown; "
                 "NETERR = failed request or HTTP status 400+ (web only).")
    lines.append("")
    header = "| # | Route | " + " | ".join(labels) + " |"
    lines.append(header)
    lines.append("|---|---|" + "|".join("---" for _ in labels) + "|")
    for i, route in enumerate(order):
        cells = []
        for l in labels:
            r = by_route[route].get(l)
            if r is None:
                cells.append("n/a")
            elif r["flags"]:
                cells.append(" ".join(f"**{f}**" for f in r["flags"]))
            else:
                cells.append("ok")
        lines.append(f"| {i:03d} | `{route}` | " + " | ".join(cells) + " |")
    lines.append("")

    detail_rows = [r for r in rows if r["flags"]]
    if detail_rows:
        lines.append("## Details for flagged checks")
        lines.append("")
        lines.append("| Route | Viewport | Flags | Landed on | scrollW/innerW | Top offending elements | Errors | Screenshot |")
        lines.append("|---|---|---|---|---|---|---|---|")
        for r in detail_rows:
            m = r.get("metrics", {})
            shot = r.get("screenshot") or ""
            shot_md = f"[png]({shot.replace(os.sep, '/')})" if shot else ""
            lines.append(
                f"| `{r['route']}` | {r.get('label', '')} | {' '.join(r['flags'])} | `{m.get('path', '?')}` | "
                f"{m.get('scrollW', '?')}/{m.get('iw', '?')} | {_offenders_md(m)} | {_errors_md(r.get('events', {}))} | {shot_md} |"
            )
        lines.append("")

    lines.append("## All checks")
    lines.append("")
    lines.append("| Route | Viewport | Landed on | Text chars | scrollW/innerW | Title | Screenshot |")
    lines.append("|---|---|---|---|---|---|---|")
    for r in rows:
        m = r.get("metrics", {})
        shot = r.get("screenshot") or ""
        shot_md = f"[png]({shot.replace(os.sep, '/')})" if shot else ""
        title = _truncate(m.get("title", ""), 40).replace("|", "\\|")
        lines.append(
            f"| `{r['route']}` | {r.get('label', '')} | `{m.get('path', '?')}` | {m.get('textLen', '?')} | "
            f"{m.get('scrollW', '?')}/{m.get('iw', '?')} | {title} | {shot_md} |"
        )
    lines.append("")
    path = os.path.join(out_dir, "report.md")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return path


def print_row(index: int, row: dict) -> None:
    m = row.get("metrics", {})
    flags = " ".join(row["flags"]) or "ok"
    label = row.get("label", "")
    print(
        f"{index:03d} {label:>10s} {row['route']:38s} -> {str(m.get('path', '?')):30s} "
        f"sw={m.get('scrollW', '?')}/{m.get('iw', '?')} txt={m.get('textLen', '?'):>5} {flags}",
        flush=True,
    )


def git_commit_short(repo_root: str) -> str:
    try:
        import subprocess

        out = subprocess.run(
            ["git", "-C", repo_root, "rev-parse", "--short", "HEAD"],
            capture_output=True, text=True, timeout=5,
        )
        return out.stdout.strip()
    except Exception:
        return ""
