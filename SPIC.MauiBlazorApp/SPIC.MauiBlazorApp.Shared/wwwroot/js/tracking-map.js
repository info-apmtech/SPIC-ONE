// JMDO tracking route map: draws a session's recorded path (start pin -> polyline -> current/last
// position pin) on a Leaflet map. Used by MarkAttendance.razor's "Tracking Active"/"Tracking
// Stopped" view. Reuses the same mapInstances registry app-interop.js's initLeafletMap already
// keeps, so this map is just another entry in it, told apart only by its element id.

window.initTrackingRouteMap = function (elementId, startLat, startLng) {
    if (!window.mapInstances) window.mapInstances = {};

    if (!window.mapInstances[elementId]) {
        var map = L.map(elementId).setView([startLat, startLng], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(map);
        window.mapInstances[elementId] = map;
    }

    var map = window.mapInstances[elementId];

    var startIcon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-start',
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        html: '<i class="bi bi-flag-fill"></i>'
    });

    if (map.trackStartMarker) {
        map.trackStartMarker.setLatLng([startLat, startLng]);
    } else {
        map.trackStartMarker = L.marker([startLat, startLng], { icon: startIcon })
            .bindPopup('<b>Start</b>')
            .addTo(map);
    }

    map.setView([startLat, startLng], 15);
};

// points: array of { latitude, longitude }, oldest first. Redraws the full route each call - point
// counts here (at most a few thousand per session, see JMDOTrackingController.GetSessionPoints) are
// small enough that Leaflet re-rendering the whole polyline is cheaper than diffing it.
window.updateTrackingRouteMap = function (elementId, points) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map || !points || points.length === 0) return;

    var latLngs = points.map(function (p) { return [p.latitude, p.longitude]; });

    if (map.trackRouteLine) {
        map.trackRouteLine.setLatLngs(latLngs);
    } else {
        map.trackRouteLine = L.polyline(latLngs, { color: '#2D72D9', weight: 4, opacity: 0.85 }).addTo(map);
    }

    var current = latLngs[latLngs.length - 1];
    var currentIcon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-current',
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        html: '<i class="bi bi-person-fill"></i>'
    });

    if (map.trackCurrentMarker) {
        map.trackCurrentMarker.setLatLng(current);
    } else {
        map.trackCurrentMarker = L.marker(current, { icon: currentIcon })
            .bindPopup('<b>Current location</b>')
            .addTo(map);
    }

    var bounds = latLngs.slice();
    if (map.trackStartMarker) bounds.push(map.trackStartMarker.getLatLng());
    map.fitBounds(bounds, { padding: [36, 36], maxZoom: 16 });
};

// ---------------------------------------------------------------------------
// Route playback: a thin renderer only - all timing/play-pause/speed state lives in C#
// (MarkAttendance.razor's playback timer), this just draws how far the "playhead" has travelled
// (an amber trail from the start up to the given index) and moves a marker to match, same division
// of responsibility as the live elapsed-timer badge already uses elsewhere on this page.
// ---------------------------------------------------------------------------

window.setPlaybackPosition = function (elementId, points, index) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map || !points || points.length === 0) return;

    index = Math.max(0, Math.min(index, points.length - 1));
    var latLngs = points.slice(0, index + 1).map(function (p) { return [p.latitude, p.longitude]; });
    var current = latLngs[latLngs.length - 1];

    if (map.playbackTrail) {
        map.playbackTrail.setLatLngs(latLngs);
    } else {
        map.playbackTrail = L.polyline(latLngs, { color: '#f59e0b', weight: 5, opacity: 0.95 }).addTo(map);
    }

    var icon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-playback',
        iconSize: [26, 26],
        iconAnchor: [13, 13],
        html: '<i class="bi bi-geo-fill"></i>'
    });

    if (map.playbackMarker) {
        map.playbackMarker.setLatLng(current);
    } else {
        map.playbackMarker = L.marker(current, { icon: icon, zIndexOffset: 1000 }).addTo(map);
    }

    map.panTo(current, { animate: true, duration: 0.3 });
};

// Called whenever playback is reset for a fresh/changed session (MarkAttendance.razor's own
// session-id guard) - drops the old amber trail/marker so the next setPlaybackPosition call starts
// clean instead of carrying over a previous session's playhead.
window.clearPlayback = function (elementId) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map) return;
    if (map.playbackMarker) { map.playbackMarker.remove(); map.playbackMarker = null; }
    if (map.playbackTrail) { map.removeLayer(map.playbackTrail); map.playbackTrail = null; }
};

// Called when the view navigates away from the tracking map (Blazor reuses the component instance
// across /MarkAttendance?view=... changes - see MarkAttendance.razor's own OnParametersSetAsync
// comment - so without this, a stale Leaflet instance + its markers/polyline would leak and a later
// re-visit would silently attach to a torn-down DOM element).
window.destroyTrackingRouteMap = function (elementId) {
    if (window.mapInstances && window.mapInstances[elementId]) {
        window.mapInstances[elementId].remove();
        delete window.mapInstances[elementId];
    }
};

// ---------------------------------------------------------------------------
// Single-location map modal / viewer (used by MarkAttendance.razor "view on map")
// ---------------------------------------------------------------------------
window.initSingleLocationMap = function (elementId, lat, lng, popupText) {
    if (!window.mapInstances) window.mapInstances = {};
    if (window.mapInstances[elementId]) {
        try { window.mapInstances[elementId].remove(); } catch (e) { }
        delete window.mapInstances[elementId];
    }

    var map = L.map(elementId).setView([lat, lng], 16);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);

    var markerIcon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-current',
        iconSize: [32, 32],
        iconAnchor: [16, 16],
        html: '<i class="bi bi-geo-alt-fill text-danger" style="font-size: 1.5rem; display: block; filter: drop-shadow(0 2px 4px rgba(0,0,0,0.3));"></i>'
    });

    var marker = L.marker([lat, lng], { icon: markerIcon }).addTo(map);
    if (popupText) {
        marker.bindPopup('<b>' + popupText + '</b>').openPopup();
    }
    window.mapInstances[elementId] = map;

    setTimeout(function () {
        map.invalidateSize();
    }, 250);
};

window.destroySingleLocationMap = function (elementId) {
    if (window.mapInstances && window.mapInstances[elementId]) {
        try { window.mapInstances[elementId].remove(); } catch (e) { }
        delete window.mapInstances[elementId];
    }
};
