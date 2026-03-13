# Phase 162: Simplifikasi Action Close + Auto-Grade - Context

**Gathered:** 2026-03-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Replace 3 inconsistent close actions (ForceClose=score 0, ForceCloseAll=Abandoned, CloseEarly=partial grade) with 2 consistent actions that always auto-grade saved answers. Matching industry standard behavior (Exam.net, Canvas).

Old actions removed: ForceCloseAssessment, ForceCloseAll, CloseEarly
New actions: "Akhiri Ujian" (individual), "Akhiri Semua Ujian" (bulk)

</domain>

<decisions>
## Implementation Decisions

### Grading Behavior
- **Akhiri Ujian** (individual): Selalu auto-grade jawaban tersimpan — hitung skor real, sama seperti worker submit sendiri via SubmitExam
- **Akhiri Semua Ujian** (bulk): Auto-grade semua InProgress workers dari jawaban tersimpan
- TrainingRecord otomatis dibuat + notify if group completed — sama persis seperti SubmitExam flow
- Worker InProgress yang belum jawab satu soal pun: skor 0, status Completed (konsisten, tidak ada special case)
- Tidak ada opsi HC untuk pilih "grade vs skor 0" — selalu auto-grade

### Status Cancelled untuk Not-started
- Status baru: "Cancelled" — untuk worker yang belum mulai saat HC klik Akhiri Semua Ujian
- Cancelled = final, tidak bisa di-Reset. HC harus buat assessment baru jika mau kasih kesempatan ulang
- Badge abu-abu "Dibatalkan" di monitoring detail
- Data lama Abandoned TIDAK dimigrasi — Abandoned tetap valid untuk worker yang memilih keluar sendiri (AbandonExam)
- Cancelled masuk hitungan Total di summary card, tapi tidak masuk Completed/InProgress (kolom terpisah "Dibatalkan")
- Cancelled masuk Excel export dengan keterangan "Dibatalkan", kolom skor kosong

### Tombol Visibility
- "Akhiri Ujian" (individual): Hanya muncul untuk worker InProgress — tidak muncul untuk Open/Not-started
- "Akhiri Semua Ujian" (bulk): Muncul di level grup, affects semua Open + InProgress

### UI Tombol & Konfirmasi
- Label individual: "Akhiri Ujian"
- Label bulk: "Akhiri Semua Ujian"
- Warna: btn-danger (merah) — tetap destructive action
- Modal konfirmasi Akhiri Semua: tampilkan ringkasan dampak + jumlah. Contoh: "Akhiri semua ujian? 3 peserta InProgress akan dinilai dari jawaban tersimpan. 2 peserta belum mulai akan dibatalkan. Tindakan ini tidak dapat dibatalkan."
- Modal konfirmasi Akhiri Ujian (individual): pesan singkat dengan nama worker

### Worker Notification on Close
- Worker melihat modal: "Ujian Anda telah diakhiri oleh penyelenggara."
- Modal muncul 5 detik + tombol "Lihat Hasil" untuk redirect lebih cepat
- Redirect ke halaman Results (worker lihat skor di sana, bukan di modal)
- Skor TIDAK ditampilkan di modal — cukup notifikasi saja
- Detection tetap via polling CheckExamStatus (10 detik) — upgrade ke SignalR di Phase 164

### Claude's Discretion
- Exact grading logic reuse (extract from SubmitExam or call shared method)
- CheckExamStatus response format adjustment for new statuses
- Modal styling and animation details
- Audit log format for new actions

</decisions>

<specifics>
## Specific Ideas

- Behavior harus konsisten dengan Exam.net: force-end selalu grade jawaban tersimpan, tidak ada data yang hilang
- Konfirmasi modal untuk Akhiri Semua harus informatif — HC perlu tahu dampak sebelum confirm (berapa InProgress, berapa Not-started)
- Worker experience saat di-close harus sama seperti saat expired exam (5 detik modal + redirect)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SubmitExam` (CMPController ~line 1540): Full grading logic untuk package + legacy path, TrainingRecord creation, group completion notification — bisa di-extract ke shared method
- `CheckExamStatus` (CMPController): Polling endpoint yang sudah jalan — perlu update untuk detect Completed (dari auto-grade) dan Cancelled
- Expired exam modal di StartExam.cshtml: 5-detik countdown + redirect pattern — bisa reuse untuk close notification

### Established Patterns
- ForceCloseAssessment (AdminController ~line 2233): POST with antiforgery, ownership check, audit log — pattern untuk new actions
- CloseEarly (AdminController ~line 2635): Bulk operation pattern with group query by (title, category, scheduleDate)
- Summary card calculation di AssessmentMonitoring (AdminController ~line 1678): TotalCount, CompletedCount, PassedCount — perlu tambah CancelledCount

### Integration Points
- AssessmentMonitoringDetail.cshtml: 3 form buttons (ForceClose, ForceCloseAll, CloseEarly) → replace with 2 buttons
- JavaScript dynamic row update (~line 662): Renders ForceClose button for InProgress — perlu update
- ExportAssessmentResults: Perlu handle Cancelled status di Excel output
- AuditLog: New action types untuk Akhiri Ujian dan Akhiri Semua Ujian

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 162-simplifikasi-action-close-auto-grade*
*Context gathered: 2026-03-13*
