// Collocated JS module for MoreSheet.razor (served at
// _content/SPIC.MauiBlazorApp.Shared/Components/Shell/MoreSheet.razor.js).
//
// Responsibilities:
//   * push ONE history entry (same URL) when the sheet opens so the browser /
//     Android hardware back button closes the sheet instead of leaving the page;
//   * lock body scroll while the sheet is open;
//   * move keyboard focus into the dialog.

let dotNetRef = null;
let pushed = false;
let listening = false;

function onPopState() {
    if (!pushed) return;
    pushed = false;
    stopListening();
    unlockBody();
    if (dotNetRef) {
        try { dotNetRef.invokeMethodAsync('CloseFromHistory'); } catch (_) { /* circuit gone */ }
    }
}

function startListening() {
    if (listening) return;
    window.addEventListener('popstate', onPopState);
    listening = true;
}

function stopListening() {
    if (!listening) return;
    window.removeEventListener('popstate', onPopState);
    listening = false;
}

function lockBody() { try { document.body.classList.add('shell-sheet-open'); } catch (_) { } }
function unlockBody() { try { document.body.classList.remove('shell-sheet-open'); } catch (_) { } }

/** Returns true when a history entry was pushed. */
export function open(ref, sheetElement) {
    dotNetRef = ref;
    lockBody();
    try {
        if (sheetElement && typeof sheetElement.focus === 'function') {
            sheetElement.focus({ preventScroll: true });
        }
    } catch (_) { }

    try {
        if (!pushed) {
            history.pushState({ spicSheet: true }, '');
            pushed = true;
        }
        startListening();
        return pushed;
    } catch (_) {
        pushed = false;
        return false;
    }
}

/** popHistory: true when the sheet was closed by the user (not by Back) and the pushed entry must be consumed. */
export function close(popHistory) {
    stopListening();
    unlockBody();
    if (popHistory && pushed) {
        pushed = false;
        try { history.back(); } catch (_) { }
    } else {
        pushed = false;
    }
}

export function dispose() {
    stopListening();
    unlockBody();
    pushed = false;
    dotNetRef = null;
}
