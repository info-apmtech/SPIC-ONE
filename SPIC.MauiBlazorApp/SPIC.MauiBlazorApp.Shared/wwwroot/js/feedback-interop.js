// Interop for Components/Feedback (BottomSheet, ActionBar, OfflineBanner).
// Loaded once by each host page (Web App.razor and MAUI index.html):
//   <script src="_content/SPIC.MauiBlazorApp.Shared/js/feedback-interop.js"></script>
// Everything lives under window.spicFeedback. Idempotent: loading twice is harmless.
(function () {
    'use strict';

    if (window.spicFeedback && window.spicFeedback.version) return;

    var api = { version: '1.0.0' };

    // ---------------------------------------------------------------------
    // Connectivity: navigator.onLine + online/offline events -> ConnectivityState
    // ---------------------------------------------------------------------
    var connRef = null;
    var connBound = false;

    function notifyConnectivity() {
        if (!connRef) return;
        try {
            connRef.invokeMethodAsync('OnConnectivityChanged', navigator.onLine !== false);
        } catch (e) { /* circuit gone */ }
    }

    api.connectivity = {
        // Returns the current state and starts forwarding changes to .NET.
        subscribe: function (dotNetRef) {
            connRef = dotNetRef;
            if (!connBound) {
                window.addEventListener('online', notifyConnectivity);
                window.addEventListener('offline', notifyConnectivity);
                connBound = true;
            }
            return navigator.onLine !== false;
        },
        unsubscribe: function () {
            connRef = null;
            if (connBound) {
                window.removeEventListener('online', notifyConnectivity);
                window.removeEventListener('offline', notifyConnectivity);
                connBound = false;
            }
        },
        isOnline: function () { return navigator.onLine !== false; }
    };

    // ---------------------------------------------------------------------
    // Body scroll lock (ref-counted so nested sheets/dialogs behave)
    // ---------------------------------------------------------------------
    var lockCount = 0;
    var savedOverflow = { html: '', body: '' };

    function lockScroll() {
        if (lockCount++ === 0) {
            savedOverflow.html = document.documentElement.style.overflow;
            savedOverflow.body = document.body.style.overflow;
            document.documentElement.style.overflow = 'hidden';
            document.body.style.overflow = 'hidden';
        }
    }

    function unlockScroll() {
        if (lockCount === 0) return;
        if (--lockCount === 0) {
            document.documentElement.style.overflow = savedOverflow.html;
            document.body.style.overflow = savedOverflow.body;
        }
    }

    api.scrollLock = { lock: lockScroll, unlock: unlockScroll };

    // ---------------------------------------------------------------------
    // Bottom sheet / drawer
    //   - pushes a history entry on open so browser/hardware back closes it
    //   - Escape closes the top-most sheet
    //   - drag handle: swipe down to dismiss (phone)
    //   - focus management + scroll lock
    // ---------------------------------------------------------------------
    var sheets = {};      // id -> entry
    var stack = [];       // open ids, oldest first
    var sheetGlobalsBound = false;

    function currentHistoryId() {
        var s = window.history.state;
        return s && typeof s === 'object' && s.spicSheet ? s.spicSheet : null;
    }

    function requestClose(entry, reason) {
        if (!entry || entry.closing) return;
        entry.closing = true;
        if (reason === 'history') entry.closedByHistory = true;
        try {
            entry.ref.invokeMethodAsync('RequestClose', reason);
        } catch (e) {
            entry.closing = false;
        }
    }

    function onPopState() {
        if (stack.length === 0) return;
        var currentId = currentHistoryId();
        var keep = currentId ? stack.indexOf(currentId) : -1;
        // Every sheet opened after the entry we landed on has been popped: close it.
        for (var i = stack.length - 1; i > keep; i--) {
            requestClose(sheets[stack[i]], 'history');
        }
    }

    function onKeyDown(e) {
        if (e.key !== 'Escape' || stack.length === 0) return;
        if (e.defaultPrevented) return;
        var top = sheets[stack[stack.length - 1]];
        if (top) {
            e.preventDefault();
            requestClose(top, 'escape');
        }
    }

    function bindSheetGlobals() {
        if (sheetGlobalsBound) return;
        window.addEventListener('popstate', onPopState);
        document.addEventListener('keydown', onKeyDown);
        sheetGlobalsBound = true;
    }

    function attachDrag(entry) {
        var panel = entry.panel;
        var handle = panel.querySelector('.fb-sheet__handle-wrap');
        if (!handle || !window.PointerEvent) return;

        var startY = 0, lastY = 0, startT = 0, dragging = false;

        function onDown(e) {
            if (e.button !== undefined && e.button !== 0) return;
            dragging = true;
            startY = lastY = e.clientY;
            startT = Date.now();
            panel.style.transition = 'none';
            try { handle.setPointerCapture(e.pointerId); } catch (err) { }
        }
        function onMove(e) {
            if (!dragging) return;
            lastY = e.clientY;
            var dy = Math.max(0, lastY - startY);
            panel.style.transform = 'translateY(' + dy + 'px)';
        }
        function onUp() {
            if (!dragging) return;
            dragging = false;
            var dy = Math.max(0, lastY - startY);
            var dt = Math.max(1, Date.now() - startT);
            var velocity = dy / dt; // px per ms
            panel.style.transition = '';
            if (dy > 96 || (dy > 24 && velocity > 0.6)) {
                // Leave the panel where the finger left it; the closing animation takes over.
                requestClose(entry, 'swipe');
            } else {
                panel.style.transform = '';
            }
        }

        handle.addEventListener('pointerdown', onDown);
        handle.addEventListener('pointermove', onMove);
        handle.addEventListener('pointerup', onUp);
        handle.addEventListener('pointercancel', onUp);

        entry.detachDrag = function () {
            handle.removeEventListener('pointerdown', onDown);
            handle.removeEventListener('pointermove', onMove);
            handle.removeEventListener('pointerup', onUp);
            handle.removeEventListener('pointercancel', onUp);
        };
    }

    api.sheet = {
        // Called by BottomSheet after it renders in the open state.
        open: function (id, panel, dotNetRef) {
            if (sheets[id]) return;
            bindSheetGlobals();

            var entry = {
                id: id,
                panel: panel,
                ref: dotNetRef,
                closing: false,
                closedByHistory: false,
                previousFocus: document.activeElement,
                detachDrag: null
            };
            sheets[id] = entry;
            stack.push(id);

            lockScroll();

            try {
                window.history.pushState({ spicSheet: id }, '', window.location.href);
            } catch (e) { /* history unavailable (rare) */ }

            attachDrag(entry);

            if (panel && typeof panel.focus === 'function') {
                window.requestAnimationFrame(function () {
                    try { panel.focus({ preventScroll: true }); } catch (e) { }
                });
            }
        },

        // Called by BottomSheet after it renders in the closed state (or on dispose).
        close: function (id) {
            var entry = sheets[id];
            if (!entry) return;

            delete sheets[id];
            var idx = stack.indexOf(id);
            if (idx >= 0) stack.splice(idx, 1);

            if (entry.detachDrag) entry.detachDrag();
            unlockScroll();

            // Programmatic close (button, backdrop, Escape, swipe): drop the history entry
            // we pushed, but only if it is still the current one. A history-driven close
            // already popped it.
            if (!entry.closedByHistory && currentHistoryId() === id) {
                try { window.history.back(); } catch (e) { }
            }

            var prev = entry.previousFocus;
            if (prev && typeof prev.focus === 'function' && document.contains(prev)) {
                try { prev.focus({ preventScroll: true }); } catch (e) { }
            }
        },

        openCount: function () { return stack.length; }
    };

    // ---------------------------------------------------------------------
    // Action bar: keep the spacer exactly as tall as the fixed bar and publish
    // --fb-actionbar-height so ToastHost can sit above it on phones.
    // ---------------------------------------------------------------------
    var bars = new Map();

    function syncBar(bar, spacer) {
        var fixed = window.getComputedStyle(bar).position === 'fixed';
        if (fixed) {
            var h = bar.offsetHeight;
            spacer.style.height = h + 'px';
            document.documentElement.style.setProperty('--fb-actionbar-height', h + 'px');
        } else {
            spacer.style.height = '';
            document.documentElement.style.removeProperty('--fb-actionbar-height');
        }
    }

    api.actionBar = {
        observe: function (bar, spacer) {
            if (!bar || !spacer || bars.has(bar)) return;
            var handler = function () { syncBar(bar, spacer); };
            var ro = null;
            if (window.ResizeObserver) {
                ro = new ResizeObserver(handler);
                ro.observe(bar);
            }
            window.addEventListener('resize', handler);
            bars.set(bar, { ro: ro, handler: handler });
            handler();
        },
        unobserve: function (bar) {
            var rec = bars.get(bar);
            if (!rec) return;
            if (rec.ro) rec.ro.disconnect();
            window.removeEventListener('resize', rec.handler);
            bars.delete(bar);
            if (bars.size === 0) {
                document.documentElement.style.removeProperty('--fb-actionbar-height');
            }
        }
    };

    // ---------------------------------------------------------------------
    // Small helpers
    // ---------------------------------------------------------------------
    api.focus = function (el) {
        if (el && typeof el.focus === 'function') {
            try { el.focus({ preventScroll: true }); } catch (e) { }
        }
    };

    window.spicFeedback = api;
})();
