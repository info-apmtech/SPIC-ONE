// Shared JS interop used by both hosts (MAUI index.html and Web App.razor).
// Keep this file the single copy: the two host pages used to carry identical
// inline versions of everything below.

// ---------------------------------------------------------------------------
// Leaflet map helpers (Logistics, LogisticsMaster, Register, SubDealerRegistration)
// ---------------------------------------------------------------------------
var mapInstances = {};
var dotNetRefs = {};

window.initLeafletMap = (elementId, dotNetRef) => {
    dotNetRefs[elementId] = dotNetRef;

    if (!mapInstances[elementId]) {
        // Default view: centre of India.
        mapInstances[elementId] = L.map(elementId).setView([20.5937, 78.9629], 5);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(mapInstances[elementId]);

        mapInstances[elementId].on('click', function (e) {
            var lat = e.latlng.lat;
            var lng = e.latlng.lng;

            if (mapInstances[elementId].marker) {
                mapInstances[elementId].marker.setLatLng([lat, lng]);
            } else {
                mapInstances[elementId].marker = L.marker([lat, lng]).addTo(mapInstances[elementId]);
            }

            if (dotNetRefs[elementId]) {
                dotNetRefs[elementId].invokeMethodAsync('UpdateLocationFromMap', lat, lng);
            }
        });
    }
};

window.updateLeafletMap = (elementId, lat, lng) => {
    if (mapInstances[elementId]) {
        var map = mapInstances[elementId];
        map.setView([lat, lng], 15);

        if (map.marker) {
            map.marker.setLatLng([lat, lng]);
        } else {
            map.marker = L.marker([lat, lng]).addTo(map);
        }
    }
};

// ---------------------------------------------------------------------------
// File download / open helpers
// ---------------------------------------------------------------------------
window.downloadFileFromBytes = (fileName, contentType, base64Data) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${contentType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

function base64ToBlob(base64Data, contentType) {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    return new Blob([new Uint8Array(byteNumbers)], { type: contentType || 'application/octet-stream' });
}

window.openPdfInNewWindow = (base64Data) => {
    const url = URL.createObjectURL(base64ToBlob(base64Data, 'application/pdf'));
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 30000);
};

// Same as openPdfInNewWindow but for any content type (used to view an ID proof
// document, PDF or image, without needing a token-in-URL for the new tab).
window.openFileInNewWindow = (base64Data, contentType) => {
    const url = URL.createObjectURL(base64ToBlob(base64Data, contentType));
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 30000);
};

// ---------------------------------------------------------------------------
// Layout helpers (moved out of MainLayout.razor / NavMenu.razor: Blazor never
// executes <script> elements that are part of a component's markup)
// ---------------------------------------------------------------------------
window.spic = window.spic || {};

// Move the user profile menu into document.body on small viewports so it is
// independent from the offcanvas/sidebar stacking context.
window.spic.reparentUserMenu = function (menuId, open) {
    try {
        var menu = document.getElementById(menuId);
        if (!menu) return;

        if (open && window.innerWidth <= 768) {
            if (!menu.__originalParent) {
                menu.__originalParent = menu.parentNode;
                menu.__originalNext = menu.nextSibling;
            }
            document.body.appendChild(menu);
            menu.style.position = 'fixed';
            menu.style.top = '62px';
            menu.style.right = '8px';
            menu.style.left = 'auto';
            menu.style.bottom = 'auto';
            menu.style.width = 'min(340px, calc(100vw - 16px))';
            menu.style.maxHeight = 'calc(100vh - 72px)';
            menu.style.overflowY = 'auto';
        } else {
            if (menu.__originalParent) {
                if (menu.__originalNext) menu.__originalParent.insertBefore(menu, menu.__originalNext);
                else menu.__originalParent.appendChild(menu);
            }
            ['position', 'top', 'right', 'left', 'bottom', 'width', 'maxHeight', 'overflowY']
                .forEach(function (p) { menu.style[p] = ''; });
        }
    } catch (e) {
        // swallow errors
    }
};

window.navSearch = {
    scrollToMatch: function () {
        var match = document.querySelector('.search-match');
        if (match) match.scrollIntoView({ behavior: 'smooth', block: 'center' });
    },
    clickMatch: function () {
        var match = document.querySelector('.search-match');
        if (match) match.click();
    }
};

// Blazor does not reset scroll position on navigation, so a new page would open at the
// previous page's scroll offset. Which element actually scrolls depends on the screen size:
//   desktop/tablet - .main-content (MainLayout) is the scroll container;
//   phone          - the page itself scrolls, and because Login.css (loaded globally) gives
//                    html AND body overflow-y:auto with height:100%, it is <body> that scrolls,
//                    which window.scrollTo() does not reach.
// So every candidate is reset.
window.scrollPageToTop = function () {
    var targets = [
        document.querySelector('.main-content'),
        document.querySelector('.content-wrap'),
        document.querySelector('.app-shell'),
        document.body,
        document.documentElement,
        document.scrollingElement
    ];
    targets.forEach(function (el) {
        if (el && el.scrollTop !== 0) el.scrollTop = 0;
    });
    window.scrollTo(0, 0);
};

// Share a title / text / url through the platform share sheet. Returns
// "shared" | "cancelled" | "copied" (clipboard fallback) | "none". The MAUI host
// (wwwroot/maui-interop.js) replaces this with the native share sheet.
window.spic.share = async function (title, text, url) {
    var data = {};
    if (title) data.title = title;
    if (text) data.text = text;
    if (url) data.url = url;
    if (navigator.share && (!navigator.canShare || navigator.canShare(data))) {
        try { await navigator.share(data); return "shared"; }
        catch (e) { if (e && e.name === "AbortError") return "cancelled"; }
    }
    var fallback = [text, url].filter(Boolean).join("\n");
    try {
        if (navigator.clipboard && fallback) { await navigator.clipboard.writeText(fallback); return "copied"; }
    } catch (e) { /* no clipboard permission */ }
    return "none";
};

// ---------------------------------------------------------------------------
// Error telemetry (docs/metrics-telemetry-plan.md row 8). window errors and unhandled
// promise rejections are queued (max 20) until ClientTelemetry calls attach(dotnetRef),
// then sent to ReportJsError. Blazor's own reconnect chatter and empty "Script error."
// messages are ignored. Nothing here may throw.
// ---------------------------------------------------------------------------
window.spic.telemetry = (function () {
    var ref = null;
    var queue = [];

    function isNoise(msg) {
        if (!msg) return true;
        var m = String(msg);
        if (m === 'Script error.' || m === 'Script error') return true;
        return /websocket|connection (was )?(closed|disconnected|lost)|disconnected|reconnect|circuit/i.test(m);
    }

    function send(item) {
        try {
            ref.invokeMethodAsync('ReportJsError', item[0], item[1], item[2], item[3])
                .catch(function () { });
        } catch (e) { /* circuit gone */ }
    }

    function report(msg, src, stack, url) {
        try {
            if (isNoise(msg)) return;
            var item = [String(msg).slice(0, 2000), src ? String(src).slice(0, 300) : null,
                stack ? String(stack).slice(0, 8000) : null, url || location.href];
            if (ref) send(item);
            else if (queue.length < 20) queue.push(item);
        } catch (e) { /* never throw from the error hook */ }
    }

    window.addEventListener('error', function (e) {
        try {
            if (!e || (!e.message && !e.error)) return;   // resource load errors carry neither
            var src = e.filename ? e.filename + ':' + (e.lineno || 0) + ':' + (e.colno || 0) : null;
            report(e.message || (e.error && e.error.message), src, e.error && e.error.stack, location.href);
        } catch (x) { }
    });

    window.addEventListener('unhandledrejection', function (e) {
        try {
            var r = e ? e.reason : null;
            var msg = r && r.message ? r.message : (typeof r === 'string' ? r : (r ? String(r) : ''));
            report(msg, 'unhandledrejection', r && r.stack, location.href);
        } catch (x) { }
    });

    return {
        attach: function (dotnetRef) {
            ref = dotnetRef;
            var pending = queue;
            queue = [];
            pending.forEach(send);
        },
        report: report
    };
})();

// ---------------------------------------------------------------------------
// Viewport watcher: tells a component whether the viewport is phone width
// (<= 767.98px, the shell's phone breakpoint) now and whenever that changes,
// so pages that swap phone/desktop markup never get stuck in the wrong mode.
// ---------------------------------------------------------------------------
window.spicViewport = (function () {
    var watchers = {};
    var nextId = 1;
    var query = '(max-width: 767.98px)';

    return {
        watchPhone: function (dotNetRef, method) {
            var mq = window.matchMedia(query);
            var id = nextId++;
            var last = mq.matches;
            var frame = 0;

            // Only report real changes; the resize listener is a backup for environments
            // where the MediaQueryList change event is missed (e.g. DevTools device toggle).
            var check = function () {
                frame = 0;
                var now = mq.matches;
                if (now === last) return;
                last = now;
                dotNetRef.invokeMethodAsync(method, now).catch(function (err) {
                    console.warn('[spicViewport] ' + method + ' failed', err);
                });
            };
            var onResize = function () {
                if (!frame) frame = requestAnimationFrame(check);
            };

            if (mq.addEventListener) mq.addEventListener('change', check);
            else mq.addListener(check);
            window.addEventListener('resize', onResize);

            watchers[id] = { mq: mq, check: check, onResize: onResize };
            return { id: id, isPhone: last };
        },

        unwatch: function (id) {
            var w = watchers[id];
            if (!w) return;
            if (w.mq.removeEventListener) w.mq.removeEventListener('change', w.check);
            else w.mq.removeListener(w.check);
            window.removeEventListener('resize', w.onResize);
            delete watchers[id];
        }
    };
})();
