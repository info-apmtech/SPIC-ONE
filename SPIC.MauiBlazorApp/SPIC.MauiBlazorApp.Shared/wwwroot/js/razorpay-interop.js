// Razorpay Standard Checkout interop for the SDWA Guest House online payment step.
// Razorpay Checkout is loaded dynamically on demand from Razorpay's own CDN (never vendored),
// ensuring it only loads when a payment is actually initiated and does not run on app startup.
window.spicRazorpay = {
    open: function (options, dotNetRef) {
        function launch() {
            try {
                if (typeof Razorpay === "undefined") {
                    dotNetRef.invokeMethodAsync("OnRazorpayFailed", "The payment gateway script did not load. Check your connection and try again.");
                    return;
                }

                var rzp = new Razorpay({
                    key: options.key,
                    amount: options.amount,
                    currency: options.currency,
                    name: options.name,
                    description: options.description,
                    order_id: options.orderId,
                    prefill: options.prefill,
                    theme: options.theme,
                    handler: function (response) {
                        dotNetRef.invokeMethodAsync(
                            "OnRazorpaySuccess",
                            response.razorpay_order_id,
                            response.razorpay_payment_id,
                            response.razorpay_signature);
                    },
                    modal: {
                        ondismiss: function () {
                            dotNetRef.invokeMethodAsync("OnRazorpayDismiss");
                        }
                    }
                });

                rzp.on("payment.failed", function (response) {
                    var reason = response && response.error ? response.error.description : null;
                    dotNetRef.invokeMethodAsync("OnRazorpayFailed", reason || "Payment failed.");
                });

                rzp.open();
            } catch (err) {
                dotNetRef.invokeMethodAsync("OnRazorpayFailed", err && err.message ? err.message : "Payment initialization failed.");
            }
        }

        if (typeof Razorpay === "undefined") {
            var existingScript = document.querySelector('script[src*="checkout.razorpay.com"]');
            if (existingScript) {
                existingScript.addEventListener("load", launch);
                existingScript.addEventListener("error", function () {
                    dotNetRef.invokeMethodAsync("OnRazorpayFailed", "Failed to load payment gateway.");
                });
            } else {
                var script = document.createElement("script");
                script.src = "https://checkout.razorpay.com/v1/checkout.js";
                script.async = true;
                script.onload = launch;
                script.onerror = function () {
                    dotNetRef.invokeMethodAsync("OnRazorpayFailed", "The payment gateway script did not load. Check your connection and try again.");
                };
                document.head.appendChild(script);
            }
        } else {
            launch();
        }
    }
};

