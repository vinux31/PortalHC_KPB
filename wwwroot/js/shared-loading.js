/**
 * Shared form submit loading state.
 * Disables the submit button and shows a spinner.
 *
 * Usage:
 *   initFormLoading('myFormId', 'Menyimpan...');
 *   — or —
 *   initFormLoading('myFormId');  // defaults to "Memproses..."
 */
function initFormLoading(formId, loadingText) {
    var form = document.getElementById(formId);
    if (!form) return;
    loadingText = loadingText || 'Memproses...';
    form.addEventListener('submit', function () {
        var btn = form.querySelector('button[type="submit"]');
        if (btn && !btn.disabled) {
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>' + loadingText;
        }
    });
}
