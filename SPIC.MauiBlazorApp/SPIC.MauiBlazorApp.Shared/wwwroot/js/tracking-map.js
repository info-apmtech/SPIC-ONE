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
// Route playback: animated playhead with direction orientation & focused camera.
// Draws the covered path (amber trail), moves a directional navigation arrow
// facing the travel heading, and smoothly tracks the movement without jumping.
// ---------------------------------------------------------------------------

function calculateBearing(lat1, lng1, lat2, lng2) {
    var dLng = (lng2 - lng1) * Math.PI / 180;
    var rLat1 = lat1 * Math.PI / 180;
    var rLat2 = lat2 * Math.PI / 180;
    var y = Math.sin(dLng) * Math.cos(rLat2);
    var x = Math.cos(rLat1) * Math.sin(rLat2) - Math.sin(rLat1) * Math.cos(rLat2) * Math.cos(dLng);
    var brng = (Math.atan2(y, x) * 180 / Math.PI + 360) % 360;
    return brng;
}

function getRouteBearing(points, index, map) {
    var p1, p2;
    if (index > 0 && index < points.length) {
        p1 = points[index - 1];
        p2 = points[index];
    } else if (points.length > 1) {
        p1 = points[0];
        p2 = points[1];
    }
    if (!p1 || !p2) return map.lastBearing || 0;

    var lat1 = p1.latitude, lng1 = p1.longitude;
    var lat2 = p2.latitude, lng2 = p2.longitude;

    if (Math.abs(lat1 - lat2) < 0.00001 && Math.abs(lng1 - lng2) < 0.00001) {
        return map.lastBearing || 0;
    }

    var brng = calculateBearing(lat1, lng1, lat2, lng2);
    map.lastBearing = brng;
    return brng;
}

window.setPlaybackPosition = function (elementId, points, index, isPlaying, speed) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map || !points || points.length === 0) return;

    index = Math.max(0, Math.min(index, points.length - 1));
    var latLngs = points.slice(0, index + 1).map(function (p) { return [p.latitude, p.longitude]; });
    var current = latLngs[latLngs.length - 1];
    var currentTarget = L.latLng(current[0], current[1]);

    if (map.playbackTrail) {
        map.playbackTrail.setLatLngs(latLngs);
    } else {
        map.playbackTrail = L.polyline(latLngs, { color: '#f59e0b', weight: 5, opacity: 0.95 }).addTo(map);
    }

    var bearing = Math.round(getRouteBearing(points, index, map));

    if (map.playbackMarker) {
        var el = map.playbackMarker.getElement();
        var svg = el ? el.querySelector('svg') : null;
        if (svg) {
            svg.style.transform = 'rotate(' + bearing + 'deg)';
        }
    } else {
        var icon = L.divIcon({
            className: 'jt-playback-marker-wrapper',
            iconSize: [32, 32],
            iconAnchor: [16, 16],
            html: '<div class="jt-playback-disc">' +
                '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" style="transform: rotate(' + bearing + 'deg); transition: transform 0.25s ease-out; display: block;" xmlns="http://www.w3.org/2000/svg">' +
                '<path d="M12 2L4 21L12 17L20 21L12 2Z" fill="#ffffff" stroke="#c2410c" stroke-width="1.2" stroke-linejoin="round"/>' +
                '</svg></div>'
        });
        map.playbackMarker = L.marker(currentTarget, { icon: icon, zIndexOffset: 1000 }).addTo(map);
    }

    var playbackSpeed = speed && speed > 0 ? speed : 1;
    var tickMs = 400 / playbackSpeed;
    var durationSec = Math.max(0.08, (tickMs * 0.9) / 1000);

    if (map._markerAnimId) {
        cancelAnimationFrame(map._markerAnimId);
        map._markerAnimId = null;
    }

    if (isPlaying) {
        var prevPos = map.playbackMarker.getLatLng();
        var startLat = prevPos.lat;
        var startLng = prevPos.lng;
        var destLat = currentTarget.lat;
        var destLng = currentTarget.lng;

        var distLat = Math.abs(destLat - startLat);
        var distLng = Math.abs(destLng - startLng);

        if (distLat > 0.000001 || distLng > 0.000001) {
            var startTime = performance.now();
            function animateStep(now) {
                var elapsed = now - startTime;
                var t = Math.min(1, elapsed / tickMs);
                var curLat = startLat + (destLat - startLat) * t;
                var curLng = startLng + (destLng - startLng) * t;
                if (map.playbackMarker) {
                    map.playbackMarker.setLatLng([curLat, curLng]);
                }
                if (t < 1) {
                    map._markerAnimId = requestAnimationFrame(animateStep);
                } else {
                    map._markerAnimId = null;
                }
            }
            map._markerAnimId = requestAnimationFrame(animateStep);
        } else {
            map.playbackMarker.setLatLng(currentTarget);
        }

        var currentZoom = map.getZoom();
        var isZoomedOut = currentZoom < 16;
        var justStarted = !map._wasPlaying;

        if (justStarted || isZoomedOut) {
            map.setView(currentTarget, Math.max(currentZoom, 16), { animate: true });
        } else {
            map.panTo(currentTarget, { animate: true, duration: durationSec, easeLinearity: 0.25, noMoveStart: true });
        }
        map._wasPlaying = true;
    } else {
        map.playbackMarker.setLatLng(currentTarget);
        map.panTo(currentTarget, { animate: false });
        map._wasPlaying = false;
    }
};

// Called whenever playback is reset for a fresh/changed session (TrackPlayback.razor's own
// session-id guard) - drops the old amber trail/marker so the next setPlaybackPosition call starts
// clean instead of carrying over a previous session's playhead.
window.clearPlayback = function (elementId) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map) return;
    if (map._markerAnimId) {
        cancelAnimationFrame(map._markerAnimId);
        map._markerAnimId = null;
    }
    map._wasPlaying = false;
    if (map.playbackMarker) { map.playbackMarker.remove(); map.playbackMarker = null; }
    if (map.playbackTrail) { map.removeLayer(map.playbackTrail); map.playbackTrail = null; }
};

// Called when TrackPlayback.razor is torn down (DisposeAsync) - without this, a stale Leaflet
// instance + its markers/polyline would leak and a later re-visit would silently attach to a
// torn-down DOM element.
window.destroyTrackingRouteMap = function (elementId) {
    if (window.mapInstances && window.mapInstances[elementId]) {
        var map = window.mapInstances[elementId];
        if (map._markerAnimId) {
            cancelAnimationFrame(map._markerAnimId);
            map._markerAnimId = null;
        }
        map.remove();
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
