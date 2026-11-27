// Collocated module for FarmerAssistant.razor (Ask SPIC AI): keeps the newest message in view above the
// sticky composer. The desktop shell scrolls .main-content, the phone the window; scrollIntoView handles both.

export function scrollToEnd(el, smooth) {
    if (!el || !el.scrollIntoView) return;
    const reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    el.scrollIntoView({ block: 'end', behavior: smooth && !reduce ? 'smooth' : 'auto' });
}
