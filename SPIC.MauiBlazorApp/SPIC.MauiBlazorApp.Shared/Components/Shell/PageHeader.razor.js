// Collocated JS module for PageHeader.razor.
// goBack(): returns true and navigates back when the tab has history to go back to,
// false otherwise so the caller can fall back to LoginState.LandingPage.
export function goBack() {
    try {
        if (window.history && window.history.length > 1) {
            window.history.back();
            return true;
        }
    } catch (_) { }
    return false;
}
