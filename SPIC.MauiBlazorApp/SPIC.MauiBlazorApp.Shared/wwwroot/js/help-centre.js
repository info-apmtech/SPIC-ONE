// Help centre v2: flow connectors, scroll tracking, read tracking and the guided tour.
// Everything here is presentation only; content and visibility come from the Razor components.
window.helpCentre = (function () {
    const observers = {};

    // Calls back into .NET without ever throwing or leaving a rejected promise behind: the page
    // may already be gone (its reference disposed) when a scroll, timer, key or resize fires.
    function call(ref, method, ...args) {
        try {
            const p = ref && ref.invokeMethodAsync(method, ...args);
            if (p && typeof p.catch === 'function') p.catch(() => { });
        } catch (e) { }
    }

    function svgPath(svg, d, color, dashed, marker) {
        const p = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        p.setAttribute('d', d);
        p.setAttribute('fill', 'none');
        p.setAttribute('stroke', color);
        p.setAttribute('stroke-width', '2');
        if (dashed) p.setAttribute('stroke-dasharray', '6 5');
        p.setAttribute('marker-end', 'url(#' + marker + ')');
        p.classList.add('hc-l');
        svg.appendChild(p);
    }

    // A right-angled path from box P to box Q that never crosses a box in between. In the same
    // row it loops over the row (over = true) or under it; between rows it bends half way.
    function elbow(P, Q, over) {
        if (Math.abs(P.cy - Q.cy) < 8) {
            if (over) return `M${P.cx} ${P.t} V${Math.min(P.t, Q.t) - 14} H${Q.cx} V${Q.t - 4}`;
            return `M${P.cx} ${P.b} V${Math.max(P.b, Q.b) + 14} H${Q.cx} V${Q.b + 4}`;
        }
        if (Q.t >= P.b) return `M${P.cx} ${P.b} V${(P.b + Q.t) / 2} H${Q.cx} V${Q.t - 4}`;
        return `M${P.cx} ${P.t} V${(P.t + Q.b) / 2} H${Q.cx} V${Q.b + 4}`;
    }

    function draw(wrap) {
        const svg = wrap.querySelector('svg.hc-links');
        if (!svg) return;
        svg.querySelectorAll('path.hc-l').forEach(p => p.remove());
        if (wrap.offsetParent === null) return; // hidden (phone layout)
        const W = wrap.getBoundingClientRect();
        const box = id => {
            const el = wrap.querySelector('[data-stage="' + CSS.escape(id) + '"]');
            if (!el) return null;
            const r = el.getBoundingClientRect();
            return { l: r.left - W.left, r: r.right - W.left, t: r.top - W.top, b: r.bottom - W.top, cx: (r.left + r.right) / 2 - W.left, cy: (r.top + r.bottom) / 2 - W.top };
        };
        wrap.querySelectorAll('[data-stage]').forEach(el => {
            const A = box(el.dataset.stage);
            (el.dataset.next || '').split(',').filter(Boolean).forEach(n => {
                const B = box(n.trim()); if (!A || !B) return;
                if (B.l >= A.r) {
                    const mx = (A.r + B.l) / 2;
                    svgPath(svg, `M${A.r} ${A.cy} H${mx} V${B.cy} H${B.l - 4}`, '#94a3b8', false, 'hc-ah');
                } else {
                    svgPath(svg, `M${A.cx} ${A.b} V${B.t - 4}`, '#94a3b8', false, 'hc-ah');
                }
            });
            if (el.dataset.return) {
                const B = box(el.dataset.return); if (A && B) svgPath(svg, `M${A.cx} ${A.t} V${B.cy - 14} H${B.r + 4}`, '#d97706', true, 'hc-ahd');
            }
            // "When something goes wrong": into the exception box from the step it leaves, and out
            // of it to the step it sends you back to.
            if (el.dataset.from) {
                const F = box(el.dataset.from); if (A && F) svgPath(svg, elbow(F, A, true), '#dc2626', true, 'hc-ahr');
            }
            if (el.dataset.to) {
                const T = box(el.dataset.to); if (A && T) svgPath(svg, elbow(A, T, false), '#dc2626', true, 'hc-ahr');
            }
        });
    }

    function drawFlow(id) {
        const wrap = document.getElementById(id);
        if (!wrap) return;
        draw(wrap);
        if (!wrap._hcRo && window.ResizeObserver) {
            wrap._hcRo = new ResizeObserver(() => draw(wrap));
            wrap._hcRo.observe(wrap);
        }
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(() => draw(wrap));
    }

    function observeToc(key, ids, dotnetRef) {
        stop(key);
        const els = ids.map(i => document.getElementById(i)).filter(Boolean);
        const onScroll = () => {
            let current = els.length ? els[0].id : null;
            els.forEach(e => { if (e.getBoundingClientRect().top < 140) current = e.id; });
            if (current && current !== observers[key + ':cur']) {
                observers[key + ':cur'] = current;
                call(dotnetRef, 'OnTocChanged', current);
            }
        };
        const scroller = window;
        scroller.addEventListener('scroll', onScroll, { passive: true, capture: true });
        observers[key] = () => scroller.removeEventListener('scroll', onScroll, { capture: true });
        onScroll();
    }

    function observeRead(key, ids, dotnetRef, ms) {
        stop(key);
        if (!window.IntersectionObserver) return;
        const timers = {};
        const done = new Set();
        const io = new IntersectionObserver(entries => {
            entries.forEach(en => {
                const id = en.target.id;
                if (done.has(id)) return;
                clearTimeout(timers[id]);
                // Counts only while a good part of the section (or of the screen) shows, for ms in a row.
                const seen = en.intersectionRatio >= 0.35 || en.intersectionRect.height >= window.innerHeight * 0.5;
                if (en.isIntersecting && seen) {
                    timers[id] = setTimeout(() => { done.add(id); call(dotnetRef, 'OnSectionRead', id); }, ms);
                }
            });
        }, { threshold: 0.35 });
        ids.map(i => document.getElementById(i)).filter(Boolean).forEach(e => io.observe(e));
        observers[key] = () => { io.disconnect(); Object.values(timers).forEach(clearTimeout); };
    }

    function stop(key) {
        if (observers[key]) { try { observers[key](); } catch (e) { } delete observers[key]; }
    }

    // instant: a deep link jumps straight there; a menu tap scrolls smoothly.
    function scrollToId(id, instant) {
        const el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: instant || reducedMotion() ? 'auto' : 'smooth', block: 'start' });
    }

    function isNarrow() {
        const page = document.querySelector('.hc-page');
        const w = page ? page.clientWidth : window.innerWidth;
        return w <= 900;
    }

    function reducedMotion() {
        return !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
    }

    // ---------------- guided tour ----------------
    let tourKeys = null;
    let tourSeq = 0; // the latest step asked for; an older step still waiting for its element gives up

    function tourShow(selector) {
        const spot = document.getElementById('hc-tour-spot');
        const card = document.getElementById('hc-tour-card');
        let el = null;
        try { el = selector ? document.querySelector(selector) : null; } catch (e) { el = null; }
        const visible = el && el.getClientRects().length > 0 && getComputedStyle(el).visibility !== 'hidden';
        if (!visible) {
            if (spot) spot.style.opacity = '0';
            if (card) { card.style.left = ''; card.style.top = ''; card.classList.add('is-center'); card.focus(); }
            return { found: false };
        }
        // Scroll only when the element is not already fully on screen. (Scrolling on every call
        // also made phones loop: scrolling resizes the viewport, and a resize repositions.)
        let r = el.getBoundingClientRect();
        if (r.top < 0 || r.left < 0 || r.bottom > window.innerHeight || r.right > window.innerWidth) {
            el.scrollIntoView({ block: 'center', behavior: 'auto' });
            r = el.getBoundingClientRect();
        }
        const pad = 8;
        if (spot) Object.assign(spot.style, { left: (r.left - pad) + 'px', top: (r.top - pad) + 'px', width: (r.width + pad * 2) + 'px', height: (r.height + pad * 2) + 'px', opacity: '1' });
        if (card) {
            card.classList.remove('is-center');
            const cw = card.offsetWidth, ch = card.offsetHeight;
            let top = r.bottom + 18;
            let left = Math.min(Math.max(r.left, 12), window.innerWidth - cw - 12);
            if (top + ch > window.innerHeight - 12) top = r.top - ch - 18;
            if (top < 12) {
                // The target fills the screen (a long table): put the card in the bottom corner.
                top = window.innerHeight - ch - 16;
                left = window.innerWidth - cw - 16;
            }
            card.style.top = top + 'px';
            card.style.left = left + 'px';
            card.focus();
        }
        return { found: true };
    }

    // Waits (up to timeoutMs) for the target to appear — a tour may start right after navigating.
    function tourShowWhenReady(selector, timeoutMs) {
        const seq = ++tourSeq;
        return new Promise(resolve => {
            const started = Date.now();
            const attempt = () => {
                try {
                    // The tour moved to another step (or ended) while this one waited: do not
                    // move the highlight back to an old step.
                    if (seq !== tourSeq) { resolve({ found: false, stale: true }); return; }
                    let el = null;
                    try { el = selector ? document.querySelector(selector) : null; } catch (e) { el = null; }
                    if ((el && el.getClientRects().length > 0) || Date.now() - started >= timeoutMs) resolve(tourShow(selector));
                    else setTimeout(attempt, 150);
                } catch (e) { resolve({ found: false }); }
            };
            attempt();
        });
    }

    function tourHide() {
        tourSeq++; // anything still waiting for an element stops
        const spot = document.getElementById('hc-tour-spot');
        if (spot) spot.style.opacity = '0';
    }

    function tourBindKeys(dotnetRef) {
        tourUnbindKeys();
        tourKeys = e => {
            // Arrow keys inside the language list or a text box belong to that control.
            const tag = (e.target && e.target.tagName || '').toLowerCase();
            if (e.key !== 'Escape' && (tag === 'select' || tag === 'input' || tag === 'textarea')) return;
            if (e.key === 'Escape') call(dotnetRef, 'OnTourEnd');
            else if (e.key === 'ArrowRight') call(dotnetRef, 'OnTourNext');
            else if (e.key === 'ArrowLeft') call(dotnetRef, 'OnTourBack');
        };
        document.addEventListener('keydown', tourKeys);
        // Reposition once the size has settled, and only when the width or height really changed.
        let timer = 0, w = window.innerWidth, h = window.innerHeight;
        tourKeys._r = () => {
            clearTimeout(timer);
            timer = setTimeout(() => {
                if (window.innerWidth === w && Math.abs(window.innerHeight - h) < 80) return; // phone address bar
                w = window.innerWidth; h = window.innerHeight;
                call(dotnetRef, 'OnTourReposition');
            }, 200);
        };
        tourKeys._stop = () => clearTimeout(timer);
        window.addEventListener('resize', tourKeys._r);
    }

    function tourUnbindKeys() {
        if (!tourKeys) return;
        document.removeEventListener('keydown', tourKeys);
        if (tourKeys._r) window.removeEventListener('resize', tourKeys._r);
        if (tourKeys._stop) tourKeys._stop();
        tourKeys = null;
    }

    return {
        drawFlow, observeToc, observeRead, stop, scrollToId, isNarrow, reducedMotion,
        print: () => window.print(),
        tour: { show: tourShow, showWhenReady: tourShowWhenReady, hide: tourHide, bindKeys: tourBindKeys, unbindKeys: tourUnbindKeys }
    };
})();
