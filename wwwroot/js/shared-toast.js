/**
 * Shared toast notification helper.
 *
 * Usage: showToast('Berhasil disimpan!', 'success');
 */
function showToast(message, type) {
    var icon = type === 'success' ? 'check-circle' : 'exclamation-triangle';
    var toast = document.createElement('div');
    toast.className = 'alert alert-' + type + ' position-fixed top-0 end-0 m-3 shadow';
    toast.style.zIndex = '9999';
    toast.style.minWidth = '280px';
    toast.innerHTML = '<i class="bi bi-' + icon + ' me-2"></i>' + message;
    document.body.appendChild(toast);
    setTimeout(function () { toast.remove(); }, 3500);
}
