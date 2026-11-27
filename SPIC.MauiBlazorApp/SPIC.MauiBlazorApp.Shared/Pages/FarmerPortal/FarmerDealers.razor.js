// Collocated JS module for FarmerDealers.razor (farmer portal Task 2): the dealer map.
// Uses the global Leaflet (L) both hosts already load (as ContactUs / Logistics do) with the same
// OpenStreetMap tiles. Pins are divIcons (no marker image paths), popups are built with DOM nodes and
// textContent only (firm names are staff-typed text), and a tapped pin tells .NET which card to show.

let current = null; // { el, map, layer }

function pinIcon(kind) {
    const html = kind === "me"
        ? '<span class="fpd-pin__me"></span>'
        : '<span class="fpd-pin__drop"><i class="bi bi-shop"></i></span>';
    return window.L.divIcon({
        className: "fpd-pin fpd-pin--" + kind,
        html,
        iconSize: kind === "me" ? [22, 22] : [34, 42],
        iconAnchor: kind === "me" ? [11, 11] : [17, 40],
        popupAnchor: [0, -36]
    });
}

function popup(d) {
    const box = document.createElement("div");
    box.className = "fpd-pop";
    const name = document.createElement("strong");
    name.textContent = d.name;
    box.appendChild(name);
    if (d.place) {
        const place = document.createElement("span");
        place.textContent = d.place + (d.distance ? " · " + d.distance : "");
        box.appendChild(place);
    }
    if (d.directions && /^https?:\/\//i.test(d.directions)) {
        const a = document.createElement("a");
        a.href = d.directions;
        a.target = "_blank";
        a.rel = "noopener noreferrer";
        a.textContent = "Directions";
        box.appendChild(a);
    }
    return box;
}

/** Draws (or redraws) the map in el. Returns false when Leaflet is not available. */
export function show(el, me, dealers, dotnet) {
    const L = window.L;
    if (!el || !L) return false;

    if (current && current.el !== el) {
        try { current.map.remove(); } catch (_) { /* element already gone */ }
        current = null;
    }
    if (!current) {
        const map = L.map(el, { zoomControl: true, attributionControl: true, scrollWheelZoom: false });
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 18,
            attribution: "&copy; OpenStreetMap"
        }).addTo(map);
        current = { el, map, layer: L.layerGroup().addTo(map) };
    }

    const { map, layer } = current;
    layer.clearLayers();
    const points = [];

    (dealers || []).forEach(d => {
        const marker = L.marker([d.lat, d.lng], { icon: pinIcon("dealer"), title: d.name, keyboard: true });
        marker.bindPopup(popup(d), { closeButton: true, autoPanPadding: [24, 24] });
        marker.on("click", () => {
            if (dotnet) dotnet.invokeMethodAsync("PinSelected", d.id).catch(() => { });
        });
        marker.addTo(layer);
        points.push([d.lat, d.lng]);
    });

    if (me && typeof me.lat === "number" && typeof me.lng === "number") {
        L.marker([me.lat, me.lng], { icon: pinIcon("me"), title: "You", interactive: false, zIndexOffset: 1000 }).addTo(layer);
        points.push([me.lat, me.lng]);
    }

    // The container may have just become visible: let Leaflet measure it first.
    map.invalidateSize();
    if (points.length > 1) {
        map.fitBounds(points, { padding: [36, 36], maxZoom: 14 });
    } else if (points.length === 1) {
        map.setView(points[0], 13);
    } else {
        map.setView([10.8, 78.7], 7); // Tamil Nadu: nothing to show yet
    }
    setTimeout(() => { try { map.invalidateSize(); } catch (_) { } }, 150);
    return true;
}

export function destroy(el) {
    if (current && (!el || current.el === el)) {
        try { current.map.remove(); } catch (_) { }
        current = null;
    }
}
