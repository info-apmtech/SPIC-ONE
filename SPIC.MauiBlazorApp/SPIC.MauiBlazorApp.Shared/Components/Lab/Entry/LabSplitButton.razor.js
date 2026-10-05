// LabSplitButton menu placement. The menu is fixed to the viewport so a scrolling table
// (overflow-x: auto clips absolutely positioned children) or a sheet cannot cut it off:
// right-aligned under the button, flipped above it when there is no room below, clamped
// to the viewport. Scrolling or resizing closes the menu (through its backdrop).
let cleanup = null;

export function place(anchor, menu, backdrop) {
    if (cleanup) cleanup();
    if (!anchor || !menu) return;

    const r = anchor.getBoundingClientRect();
    const mw = menu.offsetWidth;
    const mh = menu.offsetHeight;
    const vw = document.documentElement.clientWidth;
    const vh = window.innerHeight;

    const left = Math.max(8, Math.min(r.right - mw, vw - mw - 8));
    let top = r.bottom + 4;
    if (top + mh > vh - 8 && r.top - mh - 4 >= 8) top = r.top - mh - 4;

    // A transformed ancestor (the BottomSheet panel keeps its slide-in transform) becomes the
    // containing block of fixed elements, so viewport coordinates are shifted by its box.
    const box = containingBlock(menu);
    const dx = box ? box.getBoundingClientRect().left : 0;
    const dy = box ? box.getBoundingClientRect().top : 0;

    menu.style.position = "fixed";
    menu.style.left = `${left - dx}px`;
    menu.style.top = `${Math.max(8, top) - dy}px`;
    menu.style.right = "auto";
    menu.style.bottom = "auto";

    const close = (e) => {
        if (e && e.type === "scroll" && menu.contains(e.target)) return;
        if (cleanup) cleanup();
        if (backdrop && backdrop.isConnected) backdrop.click();
    };
    window.addEventListener("scroll", close, true);
    window.addEventListener("resize", close);
    cleanup = () => {
        window.removeEventListener("scroll", close, true);
        window.removeEventListener("resize", close);
        cleanup = null;
    };
}

function containingBlock(el) {
    for (let p = el.parentElement; p && p !== document.body; p = p.parentElement) {
        const s = getComputedStyle(p);
        if (s.transform !== "none" || s.perspective !== "none" || s.filter !== "none" ||
            /transform|perspective|filter/.test(s.willChange) || /paint|layout|strict|content/.test(s.contain)) {
            return p;
        }
    }
    return null;
}

export function release() {
    if (cleanup) cleanup();
}
