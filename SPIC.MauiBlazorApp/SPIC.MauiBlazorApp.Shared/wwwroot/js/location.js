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