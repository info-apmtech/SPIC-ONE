"""Derives the SPIC ONE route list from the Razor source (`@page "..."` directives).

    python tools/ui-sweep/routes.py                       # one route per line
    python tools/ui-sweep/routes.py --json                # JSON array
    python tools/ui-sweep/routes.py --include-params tools/ui-sweep/sample-ids.json
    python tools/ui-sweep/routes.py --filter "^/(Login|Privacy)" --with-files

Parameterised routes (`/ApprovalDetail/{Id:int}`) are excluded unless --include-params
points at a JSON file of sample values, keyed by parameter name (case-insensitive) or by
the full route template, e.g. {"Id": 1, "dealerId": 42, "/MOApproval/{Id:int}": 7}.
Optional parameters (`{x:int?}`) with no sample produce the route without the segment.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
DEFAULT_PAGES_DIR = os.path.join(REPO_ROOT, "SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages")

PAGE_RE = re.compile(r'^\s*@page\s+"([^"]+)"', re.MULTILINE)
PARAM_RE = re.compile(r"\{(\*?)([A-Za-z_][A-Za-z0-9_]*)(?::([A-Za-z]+))?(\?)?\}")


def scan_pages(pages_dir: str) -> list[tuple[str, str]]:
    """Returns (route_template, relative_file) for every @page directive, in file order."""
    found: list[tuple[str, str]] = []
    if not os.path.isdir(pages_dir):
        raise SystemExit(f"pages directory not found: {pages_dir}")
    for root, _dirs, files in os.walk(pages_dir):
        for name in sorted(files, key=str.lower):
            if not name.lower().endswith(".razor"):
                continue
            path = os.path.join(root, name)
            try:
                with open(path, encoding="utf-8-sig") as f:
                    text = f.read()
            except UnicodeDecodeError:
                with open(path, encoding="latin-1") as f:
                    text = f.read()
            rel = os.path.relpath(path, pages_dir).replace(os.sep, "/")
            for m in PAGE_RE.finditer(text):
                found.append((m.group(1).strip(), rel))
    return found


def substitute_params(template: str, samples: dict) -> str | None:
    """Fills a route template from the samples dict; None when a required value is missing."""
    lower_samples = {str(k).lower(): v for k, v in samples.items()}
    if template.lower() in lower_samples:
        return str(lower_samples[template.lower()])

    def repl(m: re.Match) -> str:
        name, optional = m.group(2), bool(m.group(4))
        value = lower_samples.get(name.lower())
        if value is None:
            if optional:
                return ""
            raise KeyError(name)
        return str(value)

    try:
        out = PARAM_RE.sub(repl, template)
    except KeyError:
        return None
    out = re.sub(r"/{2,}", "/", out).rstrip("/") or "/"
    return out


def derive_routes(
    pages_dir: str = DEFAULT_PAGES_DIR,
    include_params: str | None = None,
    route_filter: str | None = None,
    with_files: bool = False,
):
    samples: dict = {}
    if include_params:
        with open(include_params, encoding="utf-8") as f:
            samples = json.load(f)
    entries = []
    seen: set[str] = set()
    skipped: list[str] = []
    for template, rel in scan_pages(pages_dir):
        if "{" in template:
            if not include_params:
                skipped.append(template)
                continue
            route = substitute_params(template, samples)
            if route is None:
                skipped.append(template)
                continue
        else:
            route = template
        if not route.startswith("/"):
            route = "/" + route
        if route.lower() in seen:
            continue
        seen.add(route.lower())
        entries.append({"route": route, "template": template, "file": rel})

    entries.sort(key=lambda e: (e["route"] != "/", e["route"].lower()))
    if route_filter:
        rx = re.compile(route_filter, re.IGNORECASE)
        entries = [e for e in entries if rx.search(e["route"])]
    return (entries if with_files else [e["route"] for e in entries]), skipped


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--pages-dir", default=DEFAULT_PAGES_DIR, help="folder scanned recursively for *.razor")
    ap.add_argument("--include-params", metavar="JSON", help="sample values for parameterised routes")
    ap.add_argument("--filter", metavar="REGEX", help="keep only routes matching this regex (case-insensitive)")
    ap.add_argument("--json", action="store_true", help="print a JSON array instead of one route per line")
    ap.add_argument("--with-files", action="store_true", help="include the template and source file per route")
    ap.add_argument("--out", metavar="FILE", help="write the list to a file instead of stdout")
    args = ap.parse_args(argv)

    routes, skipped = derive_routes(args.pages_dir, args.include_params, args.filter, args.with_files)
    if args.json or args.with_files:
        text = json.dumps(routes, indent=1)
    else:
        text = "\n".join(routes) + "\n"
    if args.out:
        with open(args.out, "w", encoding="utf-8") as f:
            f.write(text)
    else:
        sys.stdout.write(text if text.endswith("\n") else text + "\n")
    print(f"# {len(routes)} routes from {args.pages_dir}", file=sys.stderr)
    if skipped:
        print(f"# skipped {len(skipped)} parameterised route(s) (use --include-params): {', '.join(skipped)}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
