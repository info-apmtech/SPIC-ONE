// JMDO tracking route map: draws a session's recorded path (start pin -> polyline -> current/last
// position pin) on a Leaflet map. Used by MarkAttendance.razor's "Tracking Active"/"Tracking
// Stopped" view. Reuses the same mapInstances registry app-interop.js's initLeafletMap already
// keeps, so this map is just another entry in it, told apart only by its element id.

// Rejects the (0,0)-ish "no GPS fix yet" sentinel - defense in depth against a map ever being
// anchored/marked at Null Island (Gulf of Guinea), which is exactly what produced the Live
// Tracking "Africa + India" bug: a caller passing a zero-default while a real fix was still
// loading planted a Start marker there, which then poisoned every later fitBounds() call since it
// never gets fixed up once the map is marked initialized. Both call sites (TrackPlayback's history
// and Live Tracking tabs) are protected the same way here, with no change needed at either's own
// call site when they do have a real coordinate (the overwhelmingly common case).
function isPlausibleLatLng(lat, lng) {
    return typeof lat === 'number' && typeof lng === 'number' &&
        lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180 &&
        (Math.abs(lat) > 0.01 || Math.abs(lng) > 0.01);
}

window.initTrackingRouteMap = function (elementId, startLat, startLng) {
    if (!window.mapInstances) window.mapInstances = {};
    var hasValidStart = isPlausibleLatLng(startLat, startLng);

    if (!window.mapInstances[elementId]) {
        var map = hasValidStart
            ? L.map(elementId).setView([startLat, startLng], 15)
            : L.map(elementId).setView([20, 0], 2); // no real fix yet - start zoomed out, not pinned anywhere specific
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(map);
        window.mapInstances[elementId] = map;
    }

    var map = window.mapInstances[elementId];
    if (!hasValidStart) return; // nothing plausible to place a Start marker at yet

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
//
// fitBounds (optional, defaults true): Playback History's existing call site never passes a 3rd
// arg, so its camera-framing behaviour is byte-for-byte unchanged. TrackPlayback's Live Tracking
// tab passes false on every poll tick after the first, to follow the current-position marker
// (panTo) without a jump-cut camera reframe every ~15s.
window.updateTrackingRouteMap = function (elementId, input, fitBounds) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map || !input || input.length === 0) return;

    var segments = [];
    if (Array.isArray(input[0])) {
        // Multi-segment array: [[p1, p2, ...], [p3, p4, ...]]
        segments = input;
    } else {
        // Flat array of points
        segments = [input];
    }

    // Convert to Leaflet latLngs structure: array of [lat, lng] arrays
    var multiLatLngs = [];
    var allBounds = [];

    for (var s = 0; s < segments.length; s++) {
        var seg = segments[s];
        var segLatLngs = [];
        for (var i = 0; i < seg.length; i++) {
            var p = seg[i];
            if (isPlausibleLatLng(p.latitude, p.longitude)) {
                var ll = [p.latitude, p.longitude];
                segLatLngs.push(ll);
                allBounds.push(ll);
            }
        }
        if (segLatLngs.length > 0) {
            multiLatLngs.push(segLatLngs);
        }
    }

    if (multiLatLngs.length === 0) return;

    // Leaflet polyline: when given an array of LatLng arrays, it renders a multi-polyline
    // where each segment is visually disconnected - no false straight line connects across gaps!
    if (map.trackRouteLine) {
        map.trackRouteLine.setLatLngs(multiLatLngs);
    } else {
        map.trackRouteLine = L.polyline(multiLatLngs, { color: '#2D72D9', weight: 4, opacity: 0.85 }).addTo(map);
    }

    // Start marker at the first point of the first valid segment
    var startLL = multiLatLngs[0][0];
    var startIcon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-start',
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        html: '<i class="bi bi-flag-fill"></i>'
    });
    if (map.trackStartMarker) {
        map.trackStartMarker.setLatLng(startLL);
    } else {
        map.trackStartMarker = L.marker(startLL, { icon: startIcon })
            .bindPopup('<b>Start</b>')
            .addTo(map);
    }

    // Current/End marker at the last point of the last valid segment
    var lastSeg = multiLatLngs[multiLatLngs.length - 1];
    var currentLL = lastSeg[lastSeg.length - 1];
    var currentIcon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-current',
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        html: '<i class="bi bi-person-fill"></i>'
    });
    if (map.trackCurrentMarker) {
        map.trackCurrentMarker.setLatLng(currentLL);
    } else {
        map.trackCurrentMarker = L.marker(currentLL, { icon: currentIcon })
            .bindPopup('<b>Current location</b>')
            .addTo(map);
    }

    if (fitBounds === false) {
        map.panTo(currentLL, { animate: true });
    } else {
        map.fitBounds(allBounds, { padding: [36, 36], maxZoom: 16 });
    }
};

// ---------------------------------------------------------------------------
// Live Tracking current-location-only view: ONE marker, no polyline, no start/end markers, no
// fitBounds over historical points. Deliberately a separate function pair from
// initTrackingRouteMap/updateTrackingRouteMap above (which stay completely unchanged for Playback
// History) rather than a mode flag on the same functions, so there is no risk of the two modes'
// behaviour bleeding into each other. Shares the same mapInstances registry, told apart only by
// element id (TrackPlayback.razor uses "ma-live-tracking-map" for this, "ma-tracking-map" for
// Playback History's route view).
// ---------------------------------------------------------------------------

window.initLiveLocationMap = function (elementId, lat, lng) {
    if (!window.mapInstances) window.mapInstances = {};
    if (!isPlausibleLatLng(lat, lng)) return; // nothing real to show yet - caller (RenderLiveMapAsync) already guards this, but stay defensive

    if (!window.mapInstances[elementId]) {
        var map = L.map(elementId).setView([lat, lng], 16);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(map);
        // Only a user-initiated drag/zoom (never our own programmatic setView/panTo calls, which
        // don't fire 'dragstart' and only fire 'zoomstart' if we ourselves changed zoom - we never
        // do after this initial setView except via the explicit Recenter button, which resets this
        // flag itself) marks the map as "being explored", so updateLiveLocationMarker's auto-pan
        // backs off and lets the user look around without the camera fighting them.
        map.on('dragstart zoomstart', function () { map._userInteracted = true; });
        window.mapInstances[elementId] = map;
    }

    var map = window.mapInstances[elementId];
    var icon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-current',
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        html: '<i class="bi bi-person-fill"></i>'
    });

    if (map.liveLocationMarker) {
        map.liveLocationMarker.setLatLng([lat, lng]);
    } else {
        map.liveLocationMarker = L.marker([lat, lng], { icon: icon })
            .bindPopup('<b>Current location</b>')
            .addTo(map);
    }

    map.setView([lat, lng], 16);
};

// Moves the existing marker only - never reinitializes the map, never calls fitBounds, never
// touches zoom. Recenters the camera (panTo, not setView, so zoom is left alone) UNLESS the user
// has manually panned/zoomed since the map was created or last explicitly recentered.
window.updateLiveLocationMarker = function (elementId, lat, lng) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map || !isPlausibleLatLng(lat, lng)) return;

    var icon = L.divIcon({
        className: 'jt-map-pin-icon jt-map-pin-current',
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        html: '<i class="bi bi-person-fill"></i>'
    });

    if (map.liveLocationMarker) {
        map.liveLocationMarker.setLatLng([lat, lng]);
    } else {
        map.liveLocationMarker = L.marker([lat, lng], { icon: icon })
            .bindPopup('<b>Current location</b>')
            .addTo(map);
    }

    if (!map._userInteracted) {
        map.panTo([lat, lng], { animate: true });
    }
};

// Explicit "Recenter" control - re-centers on the given location regardless of prior manual
// interaction, then clears the flag so auto-follow resumes until the user pans/zooms again.
window.recenterLiveLocationMap = function (elementId, lat, lng) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map || !isPlausibleLatLng(lat, lng)) return;
    map._userInteracted = false;
    map.setView([lat, lng], 16, { animate: true });
};

window.destroyLiveLocationMap = function (elementId) {
    if (window.mapInstances && window.mapInstances[elementId]) {
        window.mapInstances[elementId].remove();
        delete window.mapInstances[elementId];
    }
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

function getRouteBearing(points, index, map, isGap) {
    if (isGap && index < points.length - 1) {
        var p1 = points[index];
        var p2 = points[index + 1];
        if (p1 && p2 && isPlausibleLatLng(p1.latitude, p1.longitude) && isPlausibleLatLng(p2.latitude, p2.longitude)) {
            var brng = calculateBearing(p1.latitude, p1.longitude, p2.latitude, p2.longitude);
            map.lastBearing = brng;
            return brng;
        }
    }
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
    var currentPt = points[index];
    if (!isPlausibleLatLng(currentPt.latitude, currentPt.longitude)) return;

    var currentTarget = L.latLng(currentPt.latitude, currentPt.longitude);

    // Build trail up to index, preserving segmentation so the trail does not draw lines across gaps
    var trailSegments = [];
    var currentSeg = [];
    for (var i = 0; i <= index; i++) {
        var pt = points[i];
        if (!isPlausibleLatLng(pt.latitude, pt.longitude)) continue;
        if (i > 0 && pt.segmentIndex !== undefined && points[i - 1].segmentIndex !== undefined && pt.segmentIndex !== points[i - 1].segmentIndex) {
            if (currentSeg.length > 0) {
                trailSegments.push(currentSeg);
                currentSeg = [];
            }
        }
        currentSeg.push([pt.latitude, pt.longitude]);
    }
    if (currentSeg.length > 0) {
        trailSegments.push(currentSeg);
    }

    if (map.playbackTrail) {
        map.playbackTrail.setLatLngs(trailSegments);
    } else {
        map.playbackTrail = L.polyline(trailSegments, { color: '#f59e0b', weight: 5, opacity: 0.95 }).addTo(map);
    }

    var isGap = false;
    if (index > 0 && points[index].segmentIndex !== undefined && points[index - 1].segmentIndex !== undefined) {
        isGap = points[index].segmentIndex !== points[index - 1].segmentIndex;
    }

    var bearing = Math.round(getRouteBearing(points, index, map, isGap));

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

    var prevPos = map.playbackMarker.getLatLng();
    var startLat = prevPos.lat;
    var startLng = prevPos.lng;
    var destLat = currentTarget.lat;
    var destLng = currentTarget.lng;

    var distLat = Math.abs(destLat - startLat);
    var distLng = Math.abs(destLng - startLng);
    var isLargeJump = distLat > 0.005 || distLng > 0.005; // ~500m

    if (isPlaying && !isGap && !isLargeJump) {
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
        // Discontinuity, manual scrub, or large jump: snap immediately without smooth interpolation across the gap
        map.playbackMarker.setLatLng(currentTarget);
        map.panTo(currentTarget, { animate: false });
        map._wasPlaying = isPlaying;
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
