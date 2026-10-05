window.getCurrentPosition = function () {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            resolve({ error: "Geolocation is not supported by this browser." });
            return;
        }
        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    error: null
                });
            },
            (err) => {
                resolve({ latitude: 0, longitude: 0, error: err.message });
            },
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
        );
    });
};

window.showRouteOnMap = function (elementId, originLat, originLng, destLat, destLng) {
    var map = window.mapInstances ? window.mapInstances[elementId] : null;
    if (!map) return { distanceKm: 0, durationMin: 0, error: "Map is not ready." };

    if (map.marker) {
        map.marker.remove();
        map.marker = null;
    }

    if (map.routeLayer) {
        map.removeLayer(map.routeLayer);
        map.routeLayer = null;
    }

    var originIcon = L.divIcon({
        className: 'cu-map-pin-icon cu-map-pin-origin',
        iconSize: [28, 28],
        iconAnchor: [14, 14],
        html: '<i class="bi bi-person-fill"></i>'
    });
    var destIcon = L.divIcon({
        className: 'cu-map-pin-icon cu-map-pin-dest',
        iconSize: [28, 28],
        iconAnchor: [14, 14],
        html: '<i class="bi bi-geo-alt-fill"></i>'
    });

    map.marker = L.layerGroup([
        L.marker([originLat, originLng], { icon: originIcon })
            .bindPopup('<b>Your Location</b>'),
        L.marker([destLat, destLng], { icon: destIcon })
            .bindPopup('<b>SPIC Office</b><br/>SPIC House, 88 Anna Salai<br/>Little Mount, Guindy, Chennai – 600032')
    ]).addTo(map);

    var url = 'https://router.project-osrm.org/route/v1/driving/' +
        originLng + ',' + originLat + ';' + destLng + ',' + destLat +
        '?overview=full&geometries=geojson';

    return fetch(url)
        .then(function (response) {
            if (!response.ok) throw new Error('Route service returned ' + response.status);
            return response.json();
        })
        .then(function (data) {
            if (!data.routes || data.routes.length === 0) throw new Error('No route found.');
            var route = data.routes[0];
            map.routeLayer = L.polyline(
                route.geometry.coordinates.map(function (p) { return [p[1], p[0]]; }),
                {
                    color: '#2D72D9',
                    weight: 4,
                    opacity: 0.8
                }).addTo(map);
            map.fitBounds([[originLat, originLng], [destLat, destLng]], { padding: [40, 40] });
            return {
                distanceKm: route.distance / 1000,
                durationMin: route.duration / 60,
                error: null
            };
        })
        .catch(function (err) {
            return {
                distanceKm: 0,
                durationMin: 0,
                error: err && err.message ? err.message : 'Unable to calculate road route right now.'
            };
        });
};

window.openPrintableHtml = function (htmlContent) {
    var w = window.open('', '_blank');
    if (w) {
        w.document.write(htmlContent);
        w.document.close();

        // Wait until all images in the new window are loaded before printing.
        // This handles cached images (already complete) and fresh loads.
        try {
            var doc = w.document;
            var imgs = doc.images;
            if (!imgs || imgs.length === 0) {
                w.focus();
                w.print();
                return;
            }

            var remaining = imgs.length;

            function done() {
                if (w && !w.closed) {
                    w.focus();
                    // small timeout to ensure layout has stabilised
                    setTimeout(function () { w.print(); }, 50);
                }
            }

            // Check for images that are already complete (cached)
            for (var i = 0; i < imgs.length; i++) {
                var img = imgs[i];
                if (img.complete) {
                    remaining--;
                    continue;
                }

                // Attach handlers for load and error — either should decrement the counter
                (function (image) {
                    var onLoadOrError = function () {
                        image.removeEventListener('load', onLoadOrError);
                        image.removeEventListener('error', onLoadOrError);
                        remaining--;
                        if (remaining <= 0) done();
                    };
                    image.addEventListener('load', onLoadOrError);
                    image.addEventListener('error', onLoadOrError);
                })(img);
            }

            if (remaining <= 0) {
                done();
                return;
            }

            // Safety fallback: if images don't finish loading within 5 seconds, print anyway
            setTimeout(function () {
                if (remaining > 0) done();
            }, 5000);
        }
        catch (e) {
            // If anything goes wrong, attempt to print to avoid blocking the user
            w.focus();
            w.print();
        }
    }
};