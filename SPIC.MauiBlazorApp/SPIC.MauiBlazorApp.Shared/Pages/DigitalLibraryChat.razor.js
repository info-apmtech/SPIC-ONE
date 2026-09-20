// Collocated JS module for DigitalLibraryChat.razor: Web Speech API dictation for the
// composer's Voice button. Loaded with import("./_content/SPIC.MauiBlazorApp.Shared/Pages/DigitalLibraryChat.razor.js")
// so nothing browser-only leaks into the C# side (the same file is used by the MAUI
// BlazorWebView, where the API simply reports itself as unsupported).
//
// Callbacks on the .NET component:
//   OnSpeech(text, isFinal)  interim text while the user speaks, then the final transcript
//   OnSpeechError(error)     "not-allowed", "no-speech", "audio-capture", ...
//   OnSpeechEnd()            recognition stopped (silence, stop() or an error)

let recognition = null;
let dotNet = null;

function ctor() {
    if (typeof window === 'undefined') return null;
    return window.webkitSpeechRecognition || window.SpeechRecognition || null;
}

export function isSupported() {
    return !!ctor();
}

// Starts listening. Returns false when the browser has no Web Speech API, so the caller
// can raise the "not available on this device" toast instead of leaving a dead button.
export function start(ref) {
    const Recognition = ctor();
    if (!Recognition) return false;

    stop();
    dotNet = ref;

    try {
        recognition = new Recognition();
        recognition.lang = 'en-IN';
        recognition.interimResults = true;
        recognition.continuous = false;
        recognition.maxAlternatives = 1;

        recognition.onresult = (e) => {
            let interim = '';
            let final = '';
            for (let i = e.resultIndex; i < e.results.length; i++) {
                const result = e.results[i];
                const text = (result[0] && result[0].transcript) || '';
                if (result.isFinal) final += text; else interim += text;
            }
            const text = (final || interim).trim();
            if (dotNet) dotNet.invokeMethodAsync('OnSpeech', text, final.length > 0);
        };

        recognition.onerror = (e) => {
            const code = (e && e.error) ? String(e.error) : 'error';
            if (dotNet) dotNet.invokeMethodAsync('OnSpeechError', code);
        };

        recognition.onend = () => {
            if (dotNet) dotNet.invokeMethodAsync('OnSpeechEnd');
        };

        recognition.start();
        return true;
    } catch (_) {
        recognition = null;
        return false;
    }
}

// Stops listening; the final transcript still arrives through onresult/onend.
export function stop() {
    try {
        if (recognition) recognition.stop();
    } catch (_) { }
}

// Tears everything down when the page is disposed (no callbacks into a dead component).
export function dispose() {
    try {
        if (recognition) {
            recognition.onresult = null;
            recognition.onerror = null;
            recognition.onend = null;
            recognition.abort();
        }
    } catch (_) { }
    recognition = null;
    dotNet = null;
}
