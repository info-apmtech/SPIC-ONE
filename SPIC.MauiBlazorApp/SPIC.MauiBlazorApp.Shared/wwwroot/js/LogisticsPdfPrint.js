/*
 * LogisticsPdf.razor.js
 *
 * Place this file in the SAME folder as LogisticsPdf.razor.
 *
 * No App.razor change.
 * No <script src="js/logisticsPdf.js">.
 * No host-wwwroot logisticsPdf.js file.
 */

const PDFJS_VERSION = "3.11.174";

const PDFJS_LIB =
    "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/" +
    PDFJS_VERSION +
    "/pdf.min.js";

const PDFJS_WORKER =
    "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/" +
    PDFJS_VERSION +
    "/pdf.worker.min.js";

let pdfJsPromise = null;

function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function loadScript(url, timeoutMs = 8000) {
    return new Promise((resolve, reject) => {
        if (window.pdfjsLib) {
            resolve();
            return;
        }

        const existing =
            Array.from(document.scripts || [])
                .find(script =>
                    (script.src || "").includes(url)
                );

        if (existing) {
            const started = Date.now();

            const timer =
                setInterval(() => {
                    if (window.pdfjsLib) {
                        clearInterval(timer);
                        resolve();
                        return;
                    }

                    if (
                        Date.now() - started >=
                        timeoutMs
                    ) {
                        clearInterval(timer);

                        reject(
                            new Error(
                                "PDF.js loading timed out."
                            )
                        );
                    }
                }, 50);

            return;
        }

        const script =
            document.createElement("script");

        script.src = url;
        script.async = true;
        script.crossOrigin = "anonymous";

        let completed = false;

        const finish = error => {
            if (completed) return;
            completed = true;
            clearTimeout(timer);

            if (error) {
                reject(error);
            } else {
                resolve();
            }
        };

        const timer =
            setTimeout(
                () =>
                    finish(
                        new Error(
                            "PDF.js loading timed out."
                        )
                    ),
                timeoutMs
            );

        script.onload =
            () => finish();

        script.onerror =
            () =>
                finish(
                    new Error(
                        "Unable to load PDF.js."
                    )
                );

        document.head.appendChild(script);
    });
}

async function ensurePdfJs() {
    if (window.pdfjsLib) {
        try {
            window.pdfjsLib
                .GlobalWorkerOptions
                .workerSrc =
                PDFJS_WORKER;
        } catch {
        }

        return window.pdfjsLib;
    }

    if (!pdfJsPromise) {
        pdfJsPromise =
            (async () => {
                await loadScript(
                    PDFJS_LIB
                );

                if (!window.pdfjsLib) {
                    throw new Error(
                        "PDF.js loaded but pdfjsLib is unavailable."
                    );
                }

                window.pdfjsLib
                    .GlobalWorkerOptions
                    .workerSrc =
                    PDFJS_WORKER;

                return window.pdfjsLib;
            })();
    }

    try {
        return await pdfJsPromise;
    } catch (error) {
        pdfJsPromise = null;
        throw error;
    }
}

function showMessage(
    target,
    title,
    detail,
    isError
) {
    target.innerHTML = "";

    const box =
        document.createElement("div");

    box.style.padding = "20px";
    box.style.textAlign = "center";
    box.style.fontSize = "11px";
    box.style.lineHeight = "1.5";
    box.style.borderRadius = "6px";

    if (isError) {
        box.style.color = "#991b1b";
        box.style.border =
            "1px solid #fecaca";
        box.style.background =
            "#fff7f7";
    } else {
        box.style.color = "#6b7280";
    }

    const heading =
        document.createElement("div");

    heading.style.fontWeight = "700";
    heading.textContent = title;

    box.appendChild(heading);

    if (detail) {
        const small =
            document.createElement("div");

        small.style.marginTop = "4px";
        small.style.fontSize = "10px";
        small.textContent = detail;

        box.appendChild(small);
    }

    target.appendChild(box);
}

async function fetchPdfBytes(url) {
    const response =
        await fetch(
            url,
            {
                method: "GET",
                credentials: "include",
                cache: "no-store"
            }
        );

    if (!response.ok) {
        throw new Error(
            `HTTP ${response.status}`
        );
    }

    const contentType =
        (
            response.headers.get(
                "content-type"
            ) || ""
        ).toLowerCase();

    if (
        contentType &&
        !contentType.includes("pdf") &&
        !contentType.includes(
            "octet-stream"
        )
    ) {
        throw new Error(
            "Response is not a PDF (" +
            contentType +
            ")."
        );
    }

    const bytes =
        await response.arrayBuffer();

    if (
        !bytes ||
        bytes.byteLength === 0
    ) {
        throw new Error(
            "PDF file is empty."
        );
    }

    return bytes;
}

async function renderOnePdf(
    target,
    pdfjsLib
) {
    if (
        target.dataset.rendered ===
        "true"
    ) {
        return;
    }

    if (
        target.dataset.rendering ===
        "true"
    ) {
        for (
            let i = 0;
            i < 150;
            i++
        ) {
            if (
                target.dataset
                    .rendering !==
                "true"
            ) {
                return;
            }

            await wait(100);
        }

        return;
    }

    const url =
        target.getAttribute(
            "data-pdf-url"
        );

    if (!url) {
        target.dataset.rendered =
            "error";

        showMessage(
            target,
            "Unable to preview this PDF.",
            "Document URL is empty.",
            true
        );

        return;
    }

    target.dataset.rendering =
        "true";

    showMessage(
        target,
        "Loading document pages...",
        "",
        false
    );

    let lastError = null;

    for (
        let attempt = 1;
        attempt <= 3;
        attempt++
    ) {
        try {
            const bytes =
                await fetchPdfBytes(url);

            const loadingTask =
                pdfjsLib.getDocument({
                    data: bytes
                });

            const pdf =
                await loadingTask.promise;

            target.innerHTML = "";

            for (
                let pageNumber = 1;
                pageNumber <= pdf.numPages;
                pageNumber++
            ) {
                const page =
                    await pdf.getPage(
                        pageNumber
                    );

                const viewport =
                    page.getViewport({
                        scale: 1.5
                    });

                const canvas =
                    document.createElement(
                        "canvas"
                    );

                const context =
                    canvas.getContext(
                        "2d",
                        { alpha: false }
                    );

                if (!context) {
                    throw new Error(
                        "Canvas rendering is unavailable."
                    );
                }

                canvas.width =
                    Math.ceil(
                        viewport.width
                    );

                canvas.height =
                    Math.ceil(
                        viewport.height
                    );

                await page.render({
                    canvasContext: context,
                    viewport,
                    background: "#ffffff"
                }).promise;

                /*
                 * Same visual behaviour as DealerPDF:
                 * every saved PDF page appears directly
                 * in the Attached Documents section.
                 *
                 * Convert to img so Print cloning keeps
                 * the rendered PDF page.
                 */
                const image =
                    document.createElement(
                        "img"
                    );

                image.src =
                    canvas.toDataURL(
                        "image/jpeg",
                        0.95
                    );

                image.alt =
                    (
                        target.dataset
                            .documentTitle ||
                        "Attached document"
                    ) +
                    " - Page " +
                    pageNumber;

                image.className =
                    "logistics-saved-pdf-page";

                image.style.display =
                    "block";

                image.style.width =
                    "100%";

                image.style.maxWidth =
                    "100%";

                image.style.height =
                    "auto";

                image.style.maxHeight =
                    "none";

                image.style.objectFit =
                    "contain";

                image.style.margin =
                    "0 auto 12px auto";

                image.style.background =
                    "#ffffff";

                image.style.border =
                    "1px solid #e5e7eb";

                target.appendChild(
                    image
                );

                canvas.width = 1;
                canvas.height = 1;
            }

            target.dataset.rendered =
                "true";

            target.classList.add(
                "rendered"
            );

            delete target.dataset
                .rendering;

            return;
        } catch (error) {
            lastError = error;

            console.error(
                `Saved Logistics PDF render attempt ${attempt}/3 failed.`,
                error
            );

            if (attempt < 3) {
                await wait(
                    attempt * 700
                );
            }
        }
    }

    delete target.dataset.rendering;

    target.dataset.rendered =
        "error";

    showMessage(
        target,
        "Unable to preview this PDF.",
        lastError?.message ||
        String(lastError || ""),
        true
    );
}

export async function renderSavedDocuments(
    selector =
        ".logistics-saved-pdf-target"
) {
    const targets =
        Array.from(
            document.querySelectorAll(
                selector
            )
        );

    if (targets.length === 0) {
        return;
    }

    let pdfjsLib;

    try {
        pdfjsLib =
            await ensurePdfJs();
    } catch (error) {
        console.error(
            "Unable to initialize PDF.js.",
            error
        );

        for (
            const target of targets
        ) {
            showMessage(
                target,
                "Unable to preview this PDF.",
                error?.message ||
                String(error),
                true
            );
        }

        return;
    }

    for (
        const target of targets
    ) {
        await renderOnePdf(
            target,
            pdfjsLib
        );
    }
}

function setImportant(
    element,
    property,
    value
) {
    element.style.setProperty(
        property,
        value,
        "important"
    );
}

function rememberStyle(element) {
    return element.getAttribute(
        "style"
    );
}

function restoreStyle(
    element,
    value
) {
    if (value === null) {
        element.removeAttribute(
            "style"
        );
    } else {
        element.setAttribute(
            "style",
            value
        );
    }
}

function prepareClone(clone) {
    clone
        .querySelectorAll(
            ".screen-only, " +
            ".pdf-back-row, " +
            ".pdf-print-btn, " +
            "button"
        )
        .forEach(
            element =>
                element.remove()
        );

    setImportant(
        clone,
        "position",
        "static"
    );

    setImportant(
        clone,
        "display",
        "block"
    );

    setImportant(
        clone,
        "width",
        "210mm"
    );

    setImportant(
        clone,
        "max-width",
        "210mm"
    );

    setImportant(
        clone,
        "height",
        "auto"
    );

    setImportant(
        clone,
        "min-height",
        "297mm"
    );

    setImportant(
        clone,
        "max-height",
        "none"
    );

    setImportant(
        clone,
        "margin",
        "0 auto"
    );

    setImportant(
        clone,
        "overflow",
        "visible"
    );

    setImportant(
        clone,
        "box-shadow",
        "none"
    );

    setImportant(
        clone,
        "background",
        "#ffffff"
    );

    clone
        .querySelectorAll(
            ".view-body, " +
            ".view-section, " +
            ".table-wrapper, " +
            ".logistics-attached-documents, " +
            ".logistics-document-preview-card, " +
            ".logistics-saved-pdf-target"
        )
        .forEach(element => {
            setImportant(
                element,
                "height",
                "auto"
            );

            setImportant(
                element,
                "min-height",
                "0"
            );

            setImportant(
                element,
                "max-height",
                "none"
            );

            setImportant(
                element,
                "overflow",
                "visible"
            );
        });

    const attached =
        clone.querySelector(
            ".logistics-attached-documents"
        );

    if (attached) {
        setImportant(
            attached,
            "break-before",
            "page"
        );

        setImportant(
            attached,
            "page-break-before",
            "always"
        );
    }

    clone
        .querySelectorAll(
            ".logistics-saved-pdf-page"
        )
        .forEach(
            (image, index) => {
                setImportant(
                    image,
                    "display",
                    "block"
                );

                setImportant(
                    image,
                    "width",
                    "100%"
                );

                setImportant(
                    image,
                    "max-width",
                    "100%"
                );

                setImportant(
                    image,
                    "height",
                    "auto"
                );

                setImportant(
                    image,
                    "max-height",
                    "270mm"
                );

                setImportant(
                    image,
                    "object-fit",
                    "contain"
                );

                setImportant(
                    image,
                    "break-inside",
                    "avoid-page"
                );

                setImportant(
                    image,
                    "page-break-inside",
                    "avoid"
                );

                if (index > 0) {
                    setImportant(
                        image,
                        "break-before",
                        "page"
                    );

                    setImportant(
                        image,
                        "page-break-before",
                        "always"
                    );
                }
            }
        );
}

async function waitForImages(root) {
    const images =
        Array.from(
            root.querySelectorAll(
                "img"
            )
        );

    await Promise.all(
        images.map(image => {
            if (image.complete) {
                return Promise.resolve();
            }

            return new Promise(
                resolve => {
                    let completed =
                        false;

                    const done = () => {
                        if (completed) {
                            return;
                        }

                        completed = true;
                        resolve();
                    };

                    image.addEventListener(
                        "load",
                        done,
                        { once: true }
                    );

                    image.addEventListener(
                        "error",
                        done,
                        { once: true }
                    );

                    setTimeout(
                        done,
                        3000
                    );
                }
            );
        })
    );
}

export async function printAllPages(
    elementId,
    title
) {
    /*
     * Render saved PDF pages first when possible.
     * A timeout prevents a bad attachment from
     * blocking the browser print dialog forever.
     */
    try {
        await Promise.race([
            renderSavedDocuments(
                ".logistics-saved-pdf-target"
            ),
            wait(12000)
        ]);
    } catch (error) {
        console.warn(
            "Attachment rendering did not finish before print.",
            error
        );
    }

    const source =
        document.getElementById(
            elementId
        );

    if (!source) {
        throw new Error(
            `Print source '${elementId}' was not found.`
        );
    }

    document
        .getElementById(
            "logistics-print-root"
        )
        ?.remove();

    document
        .getElementById(
            "logistics-print-page-style"
        )
        ?.remove();

    const clone =
        source.cloneNode(true);

    prepareClone(clone);

    const printRoot =
        document.createElement(
            "div"
        );

    printRoot.id =
        "logistics-print-root";

    printRoot.style.display =
        "block";

    printRoot.style.position =
        "static";

    printRoot.style.width =
        "100%";

    printRoot.style.height =
        "auto";

    printRoot.style.overflow =
        "visible";

    printRoot.style.background =
        "#ffffff";

    printRoot.appendChild(
        clone
    );

    const pageStyle =
        document.createElement(
            "style"
        );

    pageStyle.id =
        "logistics-print-page-style";

    pageStyle.textContent =
        "@page { size: A4 portrait; margin: 0; }";

    document.head.appendChild(
        pageStyle
    );

    const bodyChildren =
        Array.from(
            document.body.children
        );

    const savedChildren =
        bodyChildren.map(
            element => ({
                element,
                style:
                    rememberStyle(
                        element
                    )
            })
        );

    const htmlStyle =
        rememberStyle(
            document.documentElement
        );

    const bodyStyle =
        rememberStyle(
            document.body
        );

    /*
     * Temporarily hide the live Blazor app.
     * Only the cloned Logistics PDF remains.
     * Therefore no MainLayout/top bar/search/sidebar
     * is included in Save as PDF.
     */
    for (
        const item of savedChildren
    ) {
        item.element.style.display =
            "none";
    }

    document.body.appendChild(
        printRoot
    );

    setImportant(
        document.documentElement,
        "height",
        "auto"
    );

    setImportant(
        document.documentElement,
        "overflow",
        "visible"
    );

    setImportant(
        document.body,
        "height",
        "auto"
    );

    setImportant(
        document.body,
        "min-height",
        "0"
    );

    setImportant(
        document.body,
        "max-height",
        "none"
    );

    setImportant(
        document.body,
        "margin",
        "0"
    );

    setImportant(
        document.body,
        "padding",
        "0"
    );

    setImportant(
        document.body,
        "overflow",
        "visible"
    );

    setImportant(
        document.body,
        "background",
        "#ffffff"
    );

    await waitForImages(
        printRoot
    );

    await wait(100);

    let restored = false;

    const restore = () => {
        if (restored) {
            return;
        }

        restored = true;

        try {
            printRoot.remove();
        } catch {
        }

        try {
            pageStyle.remove();
        } catch {
        }

        restoreStyle(
            document.documentElement,
            htmlStyle
        );

        restoreStyle(
            document.body,
            bodyStyle
        );

        for (
            const item of savedChildren
        ) {
            restoreStyle(
                item.element,
                item.style
            );
        }
    };

    window.addEventListener(
        "afterprint",
        restore,
        { once: true }
    );

    try {
        if (
            document.activeElement &&
            typeof document.activeElement
                .blur === "function"
        ) {
            document.activeElement
                .blur();
        }

        window.scrollTo(
            0,
            0
        );

        window.print();

        setTimeout(
            restore,
            60000
        );
    } catch (error) {
        restore();
        throw error;
    }
}