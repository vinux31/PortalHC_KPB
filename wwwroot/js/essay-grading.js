// Phase 384 UIG-02/03 — Handler AJAX page penilaian essay per-worker (/Admin/EssayGrading).
// Extract dari handler inline AssessmentMonitoringDetail.cshtml (Phase 298-05/310) DENGAN
// perubahan D-09: finalize sukses → update IN-PLACE ke state "Selesai" (TANPA location.reload,
// URL tetap /EssayGrading). D-10 read-only: input/tombol disabled via Razor (server-render).
// URL WAJIB via appUrl() (sub-path /KPB-PortalHC). Antiforgery via #antiforgeryForm token.

document.addEventListener("DOMContentLoaded", () => {
    // Guard: hanya jalan di page essay grading (cegah zombie handler bila script ke-load di surface lain).
    if (!document.querySelector('.essay-grading-card')) return;

    // ---- Alert dismissible di top .container-fluid + auto-dismiss (clone Phase 310) ----
    function showAlert(type, icon, message) {
        var html = '<div class="alert alert-' + type + ' alert-dismissible fade show mb-3" role="alert">'
            + '<i class="bi ' + icon + ' me-2"></i>' + message
            + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>'
            + '</div>';
        var container = document.querySelector('.container-fluid');
        if (!container) return;
        container.insertAdjacentHTML('afterbegin', html);
        var dismissMs = type === 'danger' ? 7000 : 5000;
        setTimeout(function () {
            var alertEl = container.querySelector('.alert.alert-' + type);
            if (alertEl) {
                var closeBtn = alertEl.querySelector('.btn-close');
                if (closeBtn) closeBtn.click();
            }
        }, dismissMs);
    }

    // ---- Simpan Skor per soal Essay (AJAX, REUSE apa adanya) ----
    document.querySelectorAll('.btn-save-essay-score').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            const sessionId = this.dataset.sessionId;
            const questionId = this.dataset.questionId;
            const card = this.closest('.essay-grading-card');
            const scoreInput = card.querySelector('.essay-score-input');
            const score = parseInt(scoreInput.value);
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            if (isNaN(score)) {
                alert('Masukkan nilai skor yang valid.');
                return;
            }

            this.disabled = true;
            try {
                const res = await fetch(appUrl('/Admin/SubmitEssayScore'), {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: 'sessionId=' + sessionId + '&questionId=' + questionId + '&score=' + score
                        + '&__RequestVerificationToken=' + encodeURIComponent(token)
                });
                const data = await res.json();
                if (data.success) {
                    const badge = document.getElementById('badge_' + sessionId + '_' + questionId);
                    if (badge) {
                        badge.className = 'badge bg-success essay-status-badge';
                        badge.textContent = 'Sudah Dinilai';
                    }
                    if (data.allGraded) {
                        const finalizeSection = document.getElementById('finalizeSection_' + sessionId);
                        if (finalizeSection) finalizeSection.style.display = 'block';
                    }
                } else {
                    alert(data.message);
                }
            } catch (err) {
                alert('Gagal menyimpan skor. Silakan coba lagi.');
            } finally {
                this.disabled = false;
            }
        });
    });

    // ---- Selesaikan Penilaian Essay (D-09: in-place "Selesai", BUKAN reload) ----
    document.querySelectorAll('.btn-finalize-grading').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            if (!confirm('Setelah diselesaikan, status peserta akan diperbarui dan sertifikat akan digenerate. Lanjutkan?')) return;
            const sessionId = this.dataset.sessionId;
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            this.disabled = true;
            try {
                const res = await fetch(appUrl('/Admin/FinalizeEssayGrading'), {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: 'sessionId=' + sessionId + '&__RequestVerificationToken=' + encodeURIComponent(token)
                });
                const data = await res.json();
                if (data.success && data.alreadyFinalized) {
                    // Phase 310 D-03 — friendly no-op, render alert-info, JANGAN reload (state sudah final)
                    var msg = data.message;
                    if (data.nomorSertifikat) {
                        msg += ' Nomor sertifikat: ' + data.nomorSertifikat + '.';
                    }
                    showAlert('info', 'bi-info-circle-fill', '<strong>Info:</strong> ' + msg);
                    finalizeInPlace();
                } else if (data.success) {
                    // D-09 — finalize sukses pertama: update IN-PLACE ke "Selesai", TANPA location.reload / redirect.
                    showAlert('success', 'bi-check-circle-fill', '<strong>Berhasil:</strong> Penilaian telah diselesaikan.');
                    finalizeInPlace();
                } else {
                    // Phase 310 D-04 — error spesifik per status, inline alert
                    showAlert('danger', 'bi-x-circle-fill', '<strong>Error:</strong> ' + data.message);
                    this.disabled = false;
                }
            } catch (err) {
                alert('Gagal menyelesaikan penilaian. Silakan coba lagi.');
                this.disabled = false;
            }
        });
    });

    // D-09: ubah page ke state read-only "Selesai" in-place (input + tombol disabled).
    function finalizeInPlace() {
        document.querySelectorAll('.essay-score-input').forEach(function (i) { i.disabled = true; });
        document.querySelectorAll('.btn-save-essay-score').forEach(function (b) { b.disabled = true; });
        document.querySelectorAll('.btn-finalize-grading').forEach(function (b) { b.disabled = true; });
    }

    // ---- Init Bootstrap tooltip untuk wrapper read-only (D-10) ----
    if (window.bootstrap && bootstrap.Tooltip) {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            new bootstrap.Tooltip(el);
        });
    }
});
