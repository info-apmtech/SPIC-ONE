"""Compares two sweep runs (before/after) and lists regressions.

    python tools/ui-sweep/compare.py out/web-before out/web-after
    python tools/ui-sweep/compare.py out/web-before out/web-after --out out/compare --diff-label 1920x1080 --pixel-threshold 0.5

A regression is a flag present on a (route, viewport) check in the AFTER run that was not
present in the BEFORE run. Fixed flags, checks that disappeared and new checks are listed too.

Pixel diff: for every check whose viewport label contains --diff-label (default "1920", the
desktop web view that existing users see) the two screenshots are compared with Pillow when
it is installed; the share of pixels that differ by more than --tolerance per channel is
reported and a diff image is written to --out. Without Pillow the pixel diff is skipped and
the report says so. Exit codes: 0 no regressions, 1 regressions (or pixel diffs above the
threshold), 2 bad input.
"""
from __future__ import annotations

import argparse
import json
import os
import sys

try:
    from PIL import Image, ImageChops  # type: ignore

    HAVE_PIL = True
except Exception:  # pragma: no cover
    HAVE_PIL = False


def load_run(path: str) -> tuple[dict, dict[tuple[str, str], dict], str]:
    """Returns (meta, {(route, label): row}, run_dir)."""
    run_dir = path
    if os.path.isdir(path):
        path = os.path.join(path, "results.json")
    else:
        run_dir = os.path.dirname(path)
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    rows = data.get("results", data if isinstance(data, list) else [])
    index = {}
    for r in rows:
        label = r.get("label") or (f"{r['width']}x{r['height']}" if "width" in r else "device")
        index[(r["route"], label)] = r
    return data.get("meta", {}), index, run_dir


def pixel_diff(a_path: str, b_path: str, out_path: str | None, tolerance: int) -> dict:
    a = Image.open(a_path).convert("RGB")
    b = Image.open(b_path).convert("RGB")
    result = {"sizeA": a.size, "sizeB": b.size}
    if a.size != b.size:
        w, h = min(a.width, b.width), min(a.height, b.height)
        a, b = a.crop((0, 0, w, h)), b.crop((0, 0, w, h))
        result["note"] = "sizes differ; compared the common top-left area"
    diff = ImageChops.difference(a, b)
    # A pixel counts as changed when any channel moves by more than the tolerance.
    mask = diff.convert("L").point(lambda v: 255 if v > tolerance else 0)
    changed = mask.histogram()[255]
    total = a.width * a.height
    result["changedPct"] = round(100.0 * changed / total, 3) if total else 0.0
    bbox = mask.getbbox()
    result["bbox"] = bbox
    if out_path and changed:
        # Diff image: faded 'after' with the changed pixels highlighted in red.
        faded = Image.blend(b, Image.new("RGB", b.size, (255, 255, 255)), 0.6)
        red = Image.new("RGB", b.size, (220, 0, 0))
        faded.paste(red, mask=mask)
        faded.save(out_path)
        result["image"] = out_path
    return result


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("before", help="folder (or results.json) of the baseline run")
    ap.add_argument("after", help="folder (or results.json) of the new run")
    ap.add_argument("--out", help="folder for compare.md and diff images (default: <after>/compare)")
    ap.add_argument("--diff-label", default="1920", help="only pixel-diff viewports whose label contains this (default 1920); 'all' for every viewport; 'none' to skip")
    ap.add_argument("--pixel-threshold", type=float, default=0.5, help="percent of changed pixels that counts as a regression")
    ap.add_argument("--tolerance", type=int, default=24, help="per-channel difference (0-255) ignored as noise")
    args = ap.parse_args(argv)

    try:
        meta_a, a, dir_a = load_run(args.before)
        meta_b, b, dir_b = load_run(args.after)
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2
    out_dir = os.path.abspath(args.out or os.path.join(dir_b, "compare"))
    os.makedirs(out_dir, exist_ok=True)

    regressions, fixed, unchanged_flagged, new_checks, gone_checks = [], [], [], [], []
    for key, rb in sorted(b.items()):
        ra = a.get(key)
        if ra is None:
            new_checks.append((key, rb))
            continue
        fa, fb = set(ra.get("flags", [])), set(rb.get("flags", []))
        if fb - fa:
            regressions.append((key, sorted(fb - fa), rb))
        if fa - fb:
            fixed.append((key, sorted(fa - fb)))
        if fa & fb:
            unchanged_flagged.append((key, sorted(fa & fb)))
    for key in sorted(a.keys() - b.keys()):
        gone_checks.append(key)

    pixel_rows = []
    pixel_regressions = 0
    pixel_note = ""
    if args.diff_label.lower() == "none":
        pixel_note = "Pixel diff skipped (--diff-label none)."
    elif not HAVE_PIL:
        pixel_note = "Pixel diff skipped: Pillow is not installed (python -m pip install Pillow)."
    else:
        for key in sorted(a.keys() & b.keys()):
            route, label = key
            if args.diff_label.lower() != "all" and args.diff_label not in label:
                continue
            sa, sb = a[key].get("screenshot"), b[key].get("screenshot")
            if not sa or not sb:
                continue
            pa, pb = os.path.join(dir_a, sa), os.path.join(dir_b, sb)
            if not (os.path.isfile(pa) and os.path.isfile(pb)):
                pixel_rows.append((key, {"error": "screenshot file missing"}))
                continue
            name = f"diff_{label}_{os.path.basename(sb)}"
            try:
                res = pixel_diff(pa, pb, os.path.join(out_dir, name), args.tolerance)
            except Exception as exc:
                res = {"error": str(exc)}
            if res.get("changedPct", 0) > args.pixel_threshold:
                pixel_regressions += 1
                res["regression"] = True
            pixel_rows.append((key, res))
        which = "every viewport" if args.diff_label.lower() == "all" else f"viewports whose label contains '{args.diff_label}'"
        pixel_note = f"Pixel diff over {len(pixel_rows)} screenshot pair(s) for {which}; threshold {args.pixel_threshold}% changed pixels, tolerance {args.tolerance}/255."

    lines = ["# Sweep comparison", ""]
    lines.append(f"- Before: `{args.before}` ({meta_a.get('kind', '?')}, {meta_a.get('generated', '?')}, commit {meta_a.get('commit', '?')})")
    lines.append(f"- After: `{args.after}` ({meta_b.get('kind', '?')}, {meta_b.get('generated', '?')}, commit {meta_b.get('commit', '?')})")
    lines.append(f"- Checks compared: {len(a.keys() & b.keys())}; regressions: {len(regressions)}; fixed: {len(fixed)}; still flagged: {len(unchanged_flagged)}; "
                 f"new checks: {len(new_checks)}; missing checks: {len(gone_checks)}; pixel regressions: {pixel_regressions}")
    lines.append("")
    lines.append("## Regressions (new flags in AFTER)")
    lines.append("")
    if regressions:
        lines.append("| Route | Viewport | New flags | Landed on | scrollW/innerW | Offenders / errors |")
        lines.append("|---|---|---|---|---|---|")
        for (route, label), flags, rb in regressions:
            m = rb.get("metrics", {})
            ev = rb.get("events", {})
            det = "; ".join(f"`{o['el']}`" for o in (m.get("offenders") or [])[:3])
            errs = "; ".join((ev.get("jsErrors") or [])[:1] + (ev.get("consoleErrors") or [])[:1] + (ev.get("networkFailures") or [])[:1])
            det = (det + ("<br>" if det and errs else "") + errs).replace("|", "\\|")
            lines.append(f"| `{route}` | {label} | {' '.join(flags)} | `{m.get('path', '?')}` | {m.get('scrollW', '?')}/{m.get('iw', '?')} | {det} |")
    else:
        lines.append("None.")
    lines.append("")
    lines.append("## Fixed (flags gone in AFTER)")
    lines.append("")
    lines.extend([f"- `{r}` {l}: {' '.join(f)}" for (r, l), f in fixed] or ["None."])
    lines.append("")
    lines.append("## Still flagged in both runs")
    lines.append("")
    lines.extend([f"- `{r}` {l}: {' '.join(f)}" for (r, l), f in unchanged_flagged] or ["None."])
    lines.append("")
    if new_checks or gone_checks:
        lines.append("## Coverage changes")
        lines.append("")
        lines.extend([f"- new: `{r}` {l} ({' '.join(rb.get('flags', [])) or 'ok'})" for (r, l), rb in new_checks])
        lines.extend([f"- missing in AFTER: `{r}` {l}" for r, l in gone_checks])
        lines.append("")
    lines.append("## Pixel diff")
    lines.append("")
    lines.append(pixel_note)
    lines.append("")
    if pixel_rows:
        lines.append("| Route | Viewport | Changed % | Sizes | Note | Diff image |")
        lines.append("|---|---|---|---|---|---|")
        for (route, label), res in pixel_rows:
            img = res.get("image")
            img_md = f"[png]({os.path.relpath(img, out_dir).replace(os.sep, '/')})" if img else ""
            pct = res.get("changedPct", "")
            pct_md = f"**{pct}**" if res.get("regression") else str(pct)
            sizes = f"{res.get('sizeA', '')} / {res.get('sizeB', '')}" if "sizeA" in res else ""
            lines.append(f"| `{route}` | {label} | {pct_md} | {sizes} | {res.get('note', res.get('error', ''))} | {img_md} |")
        lines.append("")

    report = os.path.join(out_dir, "compare.md")
    with open(report, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print("\n".join(lines[:4]))
    for (route, label), flags, _ in regressions:
        print(f"REGRESSION {route} {label}: {' '.join(flags)}")
    for (route, label), res in pixel_rows:
        if res.get("regression"):
            print(f"PIXELDIFF  {route} {label}: {res['changedPct']}% changed")
    print(f"Report: {report}")
    return 1 if (regressions or pixel_regressions) else 0


if __name__ == "__main__":
    raise SystemExit(main())
