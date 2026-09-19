// MAUI host only. Loaded after app-interop.js, it replaces the browser-only helpers
// with calls into DeviceInterop.cs so downloads, external links and geolocation use
// the native APIs on Android, iOS, Mac and Windows. The shared pages keep calling the
// same function names and never learn they are inside a WebView.
(function () {
    var asm = 'SPIC.MauiBlazorApp';

    function invoke(method) {
        var args = Array.prototype.slice.call(arguments, 1);
        return DotNet.invokeMethodAsync.apply(DotNet, [asm, method].concat(args));
    }

    var extByType = {
        'application/pdf': '.pdf',
        'image/png': '.png',
        'image/jpeg': '.jpg',
        'image/gif': '.gif',
        'image/webp': '.webp',
        'text/html': '.html',
        'text/csv': '.csv',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': '.xlsx',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document': '.docx'
    };

    function nameFor(contentType) {
        var ext = extByType[(contentType || '').split(';')[0].trim()] || '';
        return 'document_' + new Date().toISOString().replace(/[:.]/g, '-') + ext;
    }

    // Save to the app cache and open with the system viewer (or the share sheet).
    window.downloadFileFromBytes = function (fileName, contentType, base64Data) {
        return invoke('SaveAndOpenFile', fileName || nameFor(contentType), contentType || 'application/octet-stream', base64Data);
    };

    window.openPdfInNewWindow = function (base64Data) {
        return invoke('SaveAndOpenFile', nameFor('application/pdf'), 'application/pdf', base64Data);
    };

    window.openFileInNewWindow = function (base64Data, contentType) {
        var type = contentType || 'application/octet-stream';
        return invoke('SaveAndOpenFile', nameFor(type), type, base64Data);
    };

    // Print preview: hand the HTML to the system browser, which has its own print/share.
    window.openPrintableHtml = function (htmlContent) {
        var base64 = btoa(unescape(encodeURIComponent(htmlContent || '')));
        return invoke('SaveAndOpenFile', nameFor('text/html'), 'text/html', base64);
    };

    // window.open: WebViews have no popup window, so route everything to the OS.
    // tel:, sms:, wa.me and http(s) all end up in the right app.
    window.open = function (url) {
        if (url) { invoke('OpenExternal', String(url)); }
        return null;
    };

    // navigator.geolocation needs a WebView permission bridge per platform;
    // the native API asks for the permission itself.
    window.getCurrentPosition = function () {
        return invoke('GetCurrentPosition');
    };
})();
