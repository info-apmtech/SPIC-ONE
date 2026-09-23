// Collocated JS module for SamplePayment.razor: draws the UPI QR code into a <canvas> and
// hands back a PNG for "Download QR Code".
//
// The generator itself is the vendored MIT build at js/qrcode.min.js, loaded on demand from
// here (a classic script that exposes window.qrcode) so neither host's index.html / App.razor
// has to change and the MAUI BlazorWebView keeps working offline.
//
// Exports:
//   draw(canvas, text, size)  -> true when the QR was rendered
//   toPngBase64(canvas)       -> base64 PNG (no data: prefix) for FileDownloadService

const SCRIPT_URL = './_content/SPIC.MauiBlazorApp.Shared/js/qrcode.min.js';

let loading = null;

function ensureLibrary() {
    if (typeof window === 'undefined') return Promise.resolve(null);
    if (window.qrcode) return Promise.resolve(window.qrcode);

    loading = loading || new Promise((resolve) => {
        const script = document.createElement('script');
        script.src = SCRIPT_URL;
        script.async = true;
        script.onload = () => resolve(window.qrcode || null);
        script.onerror = () => { loading = null; resolve(null); };
        document.head.appendChild(script);
    });

    return loading;
}

// Type 0 = smallest version that fits the payload; 'M' recovery survives a printed page.
export async function draw(canvas, text, size) {
    if (!canvas || !text) return false;

    const qrcode = await ensureLibrary();
    if (!qrcode) return false;

    let qr;
    try {
        qr = qrcode(0, 'M');
        qr.addData(text);
        qr.make();
    } catch (e) {
        return false;
    }

    const modules = qr.getModuleCount();
    const quiet = 4;                                   // quiet zone required by the spec
    const target = size || 220;
    const cell = Math.max(1, Math.floor(target / (modules + quiet * 2)));
    const pixels = cell * (modules + quiet * 2);

    const ratio = window.devicePixelRatio || 1;
    canvas.width = pixels * ratio;
    canvas.height = pixels * ratio;
    canvas.style.width = pixels + 'px';
    canvas.style.height = pixels + 'px';

    const ctx = canvas.getContext('2d');
    if (!ctx) return false;

    ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, pixels, pixels);
    ctx.fillStyle = '#000000';

    for (let row = 0; row < modules; row++) {
        for (let col = 0; col < modules; col++) {
            if (qr.isDark(row, col)) {
                ctx.fillRect((col + quiet) * cell, (row + quiet) * cell, cell, cell);
            }
        }
    }

    return true;
}

// Base64 only: the C# side turns it into bytes and lets the host save or open the file
// (an <a download> is ignored by mobile WebViews).
export function toPngBase64(canvas) {
    if (!canvas) return null;
    try {
        const url = canvas.toDataURL('image/png');
        const comma = url.indexOf(',');
        return comma < 0 ? null : url.substring(comma + 1);
    } catch (e) {
        return null;
    }
}
