function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function escapeHtml(value) {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function getPageStyles() {
    const parts = [];

    document.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
        if (link.href) {
            parts.push(`<link rel="stylesheet" href="${link.href}">`);
        }
    });

    document.querySelectorAll("style").forEach(style => {
        parts.push(`<style>${style.textContent || ""}</style>`);
    });

    return parts.join("\n");
}

async function waitForStylesheets(doc) {
    const links = Array.from(doc.querySelectorAll('link[rel="stylesheet"]'));

    await Promise.all(
        links.map(link => {
            if (link.sheet) return Promise.resolve();

            return new Promise(resolve => {
                let completed = false;

                const done = () => {
                    if (completed) return;
                    completed = true;
                    resolve();
                };

                link.addEventListener("load", done, { once: true });
                link.addEventListener("error", done, { once: true });
                setTimeout(done, 2500);
            });
        })
    );
}

async function waitForImages(doc) {
    const images = Array.from(doc.images || []);

    await Promise.all(
        images.map(img => {
            if (img.complete) return Promise.resolve();

            return new Promise(resolve => {
                let completed = false;

                const done = () => {
                    if (completed) return;
                    completed = true;
                    resolve();
                };

                img.addEventListener("load", done, { once: true });
                img.addEventListener("error", done, { once: true });
                setTimeout(done, 2500);
            });
        })
    );
}

async function waitForFonts(doc) {
    try {
        if (doc.fonts && doc.fonts.ready) {
            await Promise.race([
                doc.fonts.ready,
                wait(2500)
            ]);
        }
    } catch {
        // Printing must continue even when font readiness fails.
    }
}

async function waitForLayout(win) {
    await new Promise(resolve => {
        win.requestAnimationFrame(() => {
            win.requestAnimationFrame(resolve);
        });
    });
}

/*
 * Print the complete Logistics PDF while preserving the same screen design.
 *
 * Key point:
 * We clone the full .page-wrapper rather than reconstructing/re-styling
 * the document. Only scrolling/viewport restrictions are removed for print.
 */
export async function printAllPages(elementId, title) {
    const sourceDocument = document.getElementById(elementId);

    if (!sourceDocument) {
        console.error(`Print source '${elementId}' was not found.`);
        window.print();
        return;
    }

    // Clone the full PDF viewer so its real screen CSS hierarchy is preserved.
    const sourceRoot =
        sourceDocument.closest(".page-wrapper") ||
        sourceDocument;

    const clone = sourceRoot.cloneNode(true);

    // Remove screen controls from the print copy only.
    clone
        .querySelectorAll(
            ".screen-only, .pdf-back-row, .pdf-print-btn, button"
        )
        .forEach(element => element.remove());

    const iframe = document.createElement("iframe");
    iframe.setAttribute("aria-hidden", "true");
    iframe.setAttribute("tabindex", "-1");

    /*
     * Keep a desktop viewport so @media screen (max-width: 850px)
     * does NOT switch the print copy into mobile/single-column mode.
     */
    iframe.style.position = "fixed";
    iframe.style.left = "-20000px";
    iframe.style.top = "0";
    iframe.style.width = "1200px";
    iframe.style.height = "1000px";
    iframe.style.border = "0";
    iframe.style.background = "#ffffff";
    iframe.style.pointerEvents = "none";

    document.body.appendChild(iframe);

    const printWindow = iframe.contentWindow;
    const printDocument =
        iframe.contentDocument ||
        printWindow?.document;

    if (!printWindow || !printDocument) {
        iframe.remove();
        window.print();
        return;
    }

    const styles = getPageStyles();
    const safeTitle =
        escapeHtml(title || document.title || "Logistics PDF");

    const safeBaseUrl =
        escapeHtml(document.baseURI);

    printDocument.open();

    printDocument.write(`<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport"
          content="width=device-width, initial-scale=1" />
    <base href="${safeBaseUrl}" />

    <title>${safeTitle}</title>

    ${styles}

    <style>
        /*
         * FINAL PRINT OVERRIDES
         *
         * Do not redesign the LogisticsPdf.
         * Preserve screen widths, spacing, fonts, grids and logos.
         * Only remove scroll/viewport restrictions that block pagination.
         */

        @page {
            size: A4 portrait;
            margin: 0;
        }

        html,
        body {
            width: 100% !important;

            height: auto !important;
            min-height: 0 !important;
            max-height: none !important;

            margin: 0 !important;
            padding: 0 !important;

            overflow: visible !important;

            background: #ffffff !important;
        }

        body {
            display: block !important;
            position: static !important;
        }

        /*
         * Screen viewer currently uses:
         * position: fixed;
         * height: 100vh;
         * overflow-y: auto;
         *
         * Only these viewer restrictions are removed for printing.
         */
        .page-wrapper {
            position: static !important;
            inset: auto !important;

            width: 100% !important;

            height: auto !important;
            min-height: 0 !important;
            max-height: none !important;

            margin: 0 !important;
            padding: 0 !important;

            display: block !important;

            overflow: visible !important;

            background: #ffffff !important;
        }

        /*
         * IMPORTANT:
         * Put the PDF document back to the SAME A4 width used on screen.
         * This overrides older @media print rules that changed it to 100%.
         */
        .view-document {
            position: relative !important;
            display: block !important;

            width: 210mm !important;
            max-width: 850px !important;

            height: auto !important;
            min-height: 297mm !important;
            max-height: none !important;

            margin: 0 auto !important;

            overflow: visible !important;

            background: #ffffff !important;

            border: 0 !important;
            box-shadow: none !important;
        }

        /*
         * Preserve the exact current screen layout.
         * No print-specific font/grid/header resizing here.
         */
        .view-body,
        .view-section,
        .table-wrapper {
            height: auto !important;
            max-height: none !important;
            overflow: visible !important;
        }

        /*
         * Allow long sections/tables to continue onto the next A4 page,
         * while keeping small cards/rows together where possible.
         */
        .view-section {
            break-inside: auto !important;
            page-break-inside: auto !important;
        }

        .view-section-title {
            break-after: avoid-page !important;
            page-break-after: avoid !important;
        }

        .sub-card,
        .reservation-card,
        .remarks-box {
            break-inside: avoid-page !important;
            page-break-inside: avoid !important;
        }

        .view-table {
            width: 100% !important;

            break-inside: auto !important;
            page-break-inside: auto !important;
        }

        .view-table thead {
            display: table-header-group !important;
        }

        .view-table tfoot {
            display: table-footer-group !important;
        }

        .view-table tr {
            break-inside: avoid-page !important;
            page-break-inside: avoid !important;
        }

        /*
         * Never include screen actions.
         */
        .screen-only,
        .pdf-back-row,
        .pdf-print-btn {
            display: none !important;
        }

        /*
         * Keep SPIC / GreenStar / yellow section colours in Save as PDF.
         */
        .view-header,
        .view-section-title,
        .view-section-icon,
        .view-table th,
        .status-pill,
        .approval-pill,
        .company-pill {
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
    </style>
</head>

<body>
    ${clone.outerHTML}
</body>
</html>`);

    printDocument.close();

    await waitForStylesheets(printDocument);
    await waitForImages(printDocument);
    await waitForFonts(printDocument);
    await waitForLayout(printWindow);

    // Let Chromium finish pagination.
    await wait(200);

    let removed = false;

    const cleanup = () => {
        if (removed) return;
        removed = true;

        try {
            iframe.remove();
        } catch {
        }
    };

    try {
        printWindow.onafterprint = cleanup;

        printWindow.focus();
        printWindow.print();

        // Fallback cleanup for WebViews/browsers that do not fire afterprint.
        setTimeout(cleanup, 60000);
    } catch (error) {
        cleanup();

        console.error(
            "Logistics PDF print failed.",
            error
        );

        window.print();
    }
}