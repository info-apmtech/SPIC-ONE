// Print the payment proof without leaving the page: the image is placed in a hidden
// same-origin iframe (srcdoc) that prints itself once the picture has loaded. A PDF opens in
// a new tab, where the browser's own viewer prints it.
export function printProof(url, isPdf) {
    try {
        if (isPdf) {
            window.open(url, "_blank", "noopener");
            return true;
        }

        const frame = document.createElement("iframe");
        frame.setAttribute("aria-hidden", "true");
        frame.style.position = "fixed";
        frame.style.width = "0";
        frame.style.height = "0";
        frame.style.border = "0";
        frame.style.right = "0";
        frame.style.bottom = "0";

        const safe = String(url).replace(/"/g, "&quot;");
        frame.srcdoc =
            "<!doctype html><html><head><title>Payment proof</title>" +
            "<style>@page{margin:12mm}html,body{margin:0}img{max-width:100%;max-height:100vh;display:block;margin:0 auto}</style>" +
            "</head><body><img src=\"" + safe + "\" alt=\"Payment proof\"></body></html>";

        frame.onload = () => {
            const doc = frame.contentDocument;
            const img = doc && doc.querySelector("img");
            const go = () => {
                try {
                    frame.contentWindow.focus();
                    frame.contentWindow.print();
                } finally {
                    setTimeout(() => frame.remove(), 60000);
                }
            };
            if (!img || img.complete) go();
            else {
                img.onload = go;
                img.onerror = () => frame.remove();
            }
        };

        document.body.appendChild(frame);
        return typeof window.print === "function";
    } catch (e) {
        return false;
    }
}
