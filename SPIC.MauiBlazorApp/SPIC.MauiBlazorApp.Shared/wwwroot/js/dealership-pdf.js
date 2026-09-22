// Dealership application form: renders each attached PDF inline with pdf.js and
// triggers the browser print dialog. Moved out of DEALERSHIPPDF.razor because
// Blazor does not execute <script> elements that are part of component markup.
//
// pdf.js (1.4 MB with its worker) is loaded on demand from the local copy under
// lib/pdfjs so it costs nothing on every other page.
(function () {
    var base = '_content/SPIC.MauiBlazorApp.Shared/lib/pdfjs/';
    var loading = null;

    function ensurePdfJs() {
        if (typeof pdfjsLib !== 'undefined') return Promise.resolve();
        if (loading) return loading;
        loading = new Promise(function (resolve, reject) {
            var s = document.createElement('script');
            s.src = base + 'pdf.min.js';
            s.onload = function () {
                pdfjsLib.GlobalWorkerOptions.workerSrc = base + 'pdf.worker.min.js';
                resolve();
            };
            s.onerror = function () { reject(new Error('pdf.js failed to load')); };
            document.head.appendChild(s);
        });
        return loading;
    }

    async function renderPdfWithRetry(target, url, maxRetries) {
        for (let attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                // Fetch the PDF ourselves: avoids pdf.js internal HTTP error throws.
                const response = await fetch(url, { credentials: 'include' });
                if (!response.ok) throw new Error('HTTP ' + response.status);
                const contentType = response.headers.get('content-type') || '';
                if (!contentType.includes('pdf') && !contentType.includes('octet-stream')) {
                    throw new Error('Response is not a PDF (content-type: ' + contentType + ')');
                }
                const buffer = await response.arrayBuffer();
                const pdf = await pdfjsLib.getDocument({ data: buffer }).promise;

                target.innerHTML = '';
                for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
                    const page = await pdf.getPage(pageNum);
                    const viewport = page.getViewport({ scale: 1.5 });
                    const canvas = document.createElement('canvas');
                    canvas.height = viewport.height;
                    canvas.width = viewport.width;
                    target.appendChild(canvas);
                    await page.render({ canvasContext: canvas.getContext('2d'), viewport: viewport }).promise;
                }
                target.classList.add('rendered');
                return;
            } catch (err) {
                console.error('Attempt ' + attempt + '/' + maxRetries + ' failed for PDF:', url, err);
                if (attempt < maxRetries) {
                    await new Promise(r => setTimeout(r, attempt * 1000));
                } else {
                    const container = target.closest('.document-view-container');
                    if (container) container.style.display = 'none';
                }
            }
        }
    }

    window.renderAllPDFs = async function () {
        try { await ensurePdfJs(); } catch (e) { console.error(e); return; }
        const targets = document.querySelectorAll('.pdf-render-target:not(.rendered)');
        for (let i = 0; i < targets.length; i++) {
            const url = targets[i].getAttribute('data-pdf-url');
            if (url) await renderPdfWithRetry(targets[i], url, 3);
        }
    };

    window.triggerPrint = function () {
        try {
            if (document.activeElement && typeof document.activeElement.blur === 'function') {
                document.activeElement.blur();
            }
            window.scrollTo(0, 0);
            setTimeout(function () { window.print(); }, 700);
        } catch (e) { /* ignore */ }
    };
})();
