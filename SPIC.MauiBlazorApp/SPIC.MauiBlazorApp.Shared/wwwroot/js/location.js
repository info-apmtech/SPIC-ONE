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