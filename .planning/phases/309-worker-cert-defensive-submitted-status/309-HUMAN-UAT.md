---
status: partial
phase: 309-worker-cert-defensive-submitted-status
source: [309-VERIFICATION.md]
started: 2026-05-01T12:30:00Z
updated: 2026-05-01T12:30:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Worker submit assessment ber-essay → DB Status='Menunggu Penilaian'
expected: Setelah worker submit, query AssessmentSessions row: Status='Menunggu Penilaian', IsPassed=NULL, HasManualGrading=1, Progress=100, Score=interim_pct dari MC+MA
result: [pending]
why_human: Butuh DB query SQL Server untuk verify state setelah worker action; tidak bisa di-script tanpa seed helper E2E (skipped Wave 0 per VALIDATION.md)

### 2. Worker klik 'Lihat Sertifikat' pada session pending → banner BIRU info, BUKAN popup merah
expected: Worker di-redirect ke /CMP/Results/{id} dengan banner alert-info "Info: Sertifikat akan tersedia setelah penilaian essay selesai." — TIDAK ADA popup alert-danger "Error: Assessment belum selesai."
result: [pending]
why_human: Visual UX verification (warna banner BIRU vs MERAH) + flow integration (klik tombol → redirect → render banner) tidak deterministic via grep

### 3. Worker hit /CMP/CertificatePdf/{id} pada session pending → redirect Info, no PDF, no 500
expected: Worker di-redirect ke /CMP/Results/{id} dengan banner BIRU "Sertifikat akan tersedia..." — TIDAK ada download PDF, TIDAK ada error 500
result: [pending]
why_human: HTTP behavior end-to-end (redirect status 302 + TempData pickup di next request) butuh real HTTP roundtrip

### 4. Worker view Results saat pending → render mode 'Hasil sementara' + Essay items label 'Menunggu Penilaian' (D-08 lock)
expected: Banner alert-info "Hasil sementara" visible; card-header bg-secondary (abu-abu); badge "MENUNGGU PENILAIAN" icon hourglass; tombol "Lihat Sertifikat" HIDDEN; Essay items di Tinjauan Jawaban TETAP MUNCUL dengan badge abu-abu "Menunggu Penilaian"; MC/MA items render badge HIJAU "Benar" atau MERAH "Salah" normal
result: [pending]
why_human: Visual rendering verification (tri-state colors, hidden buttons, per-item badge) butuh visual UAT — flag projection sudah verified via grep tapi rendered output tidak

### 5. HC finalize essay grading → worker view Certificate normal (regression-free Completed flow)
expected: Setelah HC finalize (atau manual SQL UPDATE Status='Completed'), worker view /CMP/Certificate/{id} render normal; recipient name muncul; card-header bg-success; Essay items render correct/incorrect normal
result: [pending]
why_human: Workflow integration test (HC action → DB state change → worker re-view) butuh multi-user manual flow

### 6. Worker dengan exotic Category null/empty → fallback signatory 'HC Manager' tampil
expected: Setelah DB edit Category=NULL atau '__exotic__', worker view /CMP/Certificate/{id} render normal dengan footer signatory Position='HC Manager', FullName=''
result: [pending]
why_human: Butuh DB edit + restore (data integrity audit), tidak boleh persistent state

### 7. Worker dengan exotic User=null → recipient '(Nama tidak tersedia)' tampil
expected: Setelah DB edit UserId=NULL pada session, worker view /CMP/Certificate/{id} render normal dengan recipient text "(Nama tidak tersedia)" (BUKAN HTTP 500)
result: [pending]
why_human: Defensive scenario via DB edit + restore atau code-side mock; null-safe accessor sudah verified via grep tapi rendered fallback string tidak

### 8. Visual styling TempData['Info'] alert-info berbeda dari Error/Success di _Layout
expected: Trigger banner Info → DevTools inspect class "alert alert-info alert-dismissible fade show"; warna BIRU MUDA; icon ⓘ (info-circle-fill); bold prefix "Info:"; tombol close (×) functional
result: [pending]
why_human: Visual / a11y verification (color contrast, icon rendering, dismiss interaction)

### 9. Post-deploy: monitor _logger.LogError 'Certificate view failed for session {Id}' di production
expected: Setelah deploy, observasi log sink (Application Insights / file log) untuk pin-point root cause aktual exotic data Temuan 10
result: [pending]
why_human: Production observability monitoring, bukan test code; SC #7 WCRT-01 explicit deferred ke ops

## Summary

total: 9
passed: 0
issues: 0
pending: 9
skipped: 0
blocked: 0

## Gaps
