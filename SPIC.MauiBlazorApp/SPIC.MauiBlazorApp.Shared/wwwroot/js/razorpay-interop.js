// Razorpay Standard Checkout interop for the SDWA Guest House online payment step.
// Loaded alongside https://checkout.razorpay.com/v1/checkout.js (see App.razor / index.html) -
// that script must come from Razorpay's own CDN, never vendored, so it always reflects
// Razorpay's current supported payment methods and security patches.
window.spicRazorpay = {
    open: function (options, dotNetRef) {
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
    }
};
