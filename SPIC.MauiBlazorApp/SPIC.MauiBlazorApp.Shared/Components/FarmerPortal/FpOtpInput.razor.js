// Collocated JS module for FpOtpInput.razor.
// attach(input, length): keeps the code field to ASCII digits BEFORE Blazor reads it (element
// listeners run before Blazor's document-level handler), turns a messy paste ("482 913", a whole
// SMS) into the code, and keeps the caret at the end so typing always fills the next box.
// Mirrors FpUi.NormaliseOtp (C#), which stays the source of truth.

function normalise(raw, length) {
    const text = String(raw || "");
    if (length === 6) {
        // Runs of exactly six digits (no look-behind: older iOS WebViews cannot parse it).
        const runs = (text.match(/[0-9]+/g) || []).filter(r => r.length === 6);
        if (runs.length === 1) return runs[0];
    }
    return text.replace(/[^0-9]/g, "").slice(0, length);
}

function caretToEnd(input) {
    try {
        const n = input.value.length;
        input.setSelectionRange(n, n);
    } catch (_) { /* some input types do not support selection */ }
}

export function attach(input, length) {
    if (!input || input.__fpOtp) return;
    input.__fpOtp = true;

    input.addEventListener("input", () => {
        const clean = normalise(input.value, length);
        if (clean !== input.value) input.value = clean;
    });

    input.addEventListener("paste", (e) => {
        const data = e.clipboardData && e.clipboardData.getData("text");
        if (data == null) return;
        e.preventDefault();
        input.value = normalise(data, length);
        caretToEnd(input);
        input.dispatchEvent(new Event("input", { bubbles: true }));
    });

    for (const name of ["focus", "click", "keyup"]) {
        input.addEventListener(name, () => caretToEnd(input));
    }
}
