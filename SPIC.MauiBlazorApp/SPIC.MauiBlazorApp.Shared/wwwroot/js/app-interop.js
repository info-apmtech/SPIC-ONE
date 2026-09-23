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
