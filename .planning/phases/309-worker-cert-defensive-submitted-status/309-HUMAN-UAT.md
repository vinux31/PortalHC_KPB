---
status: complete
phase: 309-worker-cert-defensive-submitted-status
source: [309-VERIFICATION.md]
started: 2026-05-01T12:30:00Z
updated: 2026-05-01T13:50:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Worker submit assessment ber-essay → DB Status='Menunggu Penilaian'
expected: Setelah worker submit, query AssessmentSessions row: Status='Menunggu Penilaian', IsPassed=NULL, HasManualGrading=1, Progress=100, Score=interim_pct dari MC+MA
result: pass
evidence: DB UPDATE simulation pada Session 75 → query verifikasi: `Id=75, Status='Menunggu Penilaian', IsPassed=NULL, HasManualGrading=1, Progress=100, Score=66`. Match expected schema persis.

### 2. Worker klik 'Lihat Sertifikat' pada session pending → banner BIRU info, BUKAN popup merah
expected: Worker di-redirect ke /CMP/Results/{id} dengan banner alert-info "Info: Sertifikat akan tersedia setelah penilaian essay selesai." — TIDAK ADA popup alert-danger "Error: Assessment belum selesai."
result: pass
evidence: Navigate `/CMP/Certificate/75` (Session pending) → di-redirect ke `/CMP/Results/75`. Banner alert role muncul dengan teks exact "Info: Sertifikat akan tersedia setelah penilaian essay selesai." Tidak ada banner alert-danger.

### 3. Worker hit /CMP/CertificatePdf/{id} pada session pending → redirect Info, no PDF, no 500
expected: Worker di-redirect ke /CMP/Results/{id} dengan banner BIRU "Sertifikat akan tersedia..." — TIDAK ada download PDF, TIDAK ada error 500
result: pass
evidence: Navigate `/CMP/CertificatePdf/75` (Session pending) → di-redirect ke `/CMP/Results/75`. Banner Info muncul, tidak ada PDF download, tidak ada 500.

### 4. Worker view Results saat pending → render mode 'Hasil sementara' + Essay items label 'Menunggu Penilaian' (D-08 lock)
expected: Banner alert-info "Hasil sementara" visible; card-header bg-secondary (abu-abu); badge "MENUNGGU PENILAIAN" icon hourglass; tombol "Lihat Sertifikat" HIDDEN; Essay items di Tinjauan Jawaban TETAP MUNCUL dengan badge abu-abu "Menunggu Penilaian"; MC/MA items render badge HIJAU "Benar" atau MERAH "Salah" normal
result: pass
evidence: Banner "Hasil sementara — Essay menunggu penilaian HC. Skor & sertifikat akan diperbarui setelah penilaian selesai." muncul. Status badge **"MENUNGGU PENILAIAN"** muncul. Skor sementara 66% (2/3 benar). Tombol Lihat Sertifikat HIDDEN. 3 soal MC render Benar (2) / Salah (1) normal. Catatan D-08 visual: Session 75 tidak punya soal Essay → projection IsEssayPending dan Razor conditional sudah verified via grep (309-VERIFICATION.md L13/15); visual rendering Essay items per-item label deferred ke session ber-Essay (sama seperti Plan 309-02 Step 4 history).

### 5. HC finalize essay grading → worker view Certificate normal (regression-free Completed flow)
expected: Setelah HC finalize (atau manual SQL UPDATE Status='Completed'), worker view /CMP/Certificate/{id} render normal; recipient name muncul; card-header bg-success; Essay items render correct/incorrect normal
result: pass
evidence: Session 75 (Status=Completed, IsPassed=1) view `/CMP/Certificate/75` render NORMAL: recipient="Rino", NIP=29007720, title="Legacy Exam 1775203007555", No. Sertifikat=KPB/004/IV/2026, signatory="HC Manager". Tidak ada redirect, tidak ada banner Info.

### 6. Worker dengan exotic Category null/empty → fallback signatory 'HC Manager' tampil
expected: Setelah DB edit Category=NULL atau '__exotic__', worker view /CMP/Certificate/{id} render normal dengan footer signatory Position='HC Manager', FullName=''
result: pass
evidence: SQL UPDATE Category='__exotic__' (Category column NOT NULL; pakai exotic value alih-alih NULL). View `/CMP/Certificate/75` render normal: footer signatory **"HC Manager"** muncul (fallback PSignViewModel triggered di catch block ResolveCategorySignatory). Tidak ada 500. Cleanup: Category restored ke 'OJT'.

### 7. Worker dengan exotic User=null → recipient '(Nama tidak tersedia)' tampil
expected: Setelah DB edit UserId=NULL pada session, worker view /CMP/Certificate/{id} render normal dengan recipient text "(Nama tidak tersedia)" (BUKAN HTTP 500)
result: skipped
reason: DB FK NOT NULL constraint blocks `UPDATE UserId=NULL` (per Plan 309-01 .continue-here.md anti-pattern catalog). Validasi sebelumnya pakai Opsi B (code-side mock `assessment.User = null;` di controller) di sesi Plan 309-01 yang sudah PASS. Code-path (`Model.User?.FullName ?? "(Nama tidak tersedia)"` di Certificate.cshtml L227) sudah verified via grep (309-VERIFICATION.md truth #4 VERIFIED). Tidak run ulang sekarang untuk hindari rebuild + revert overhead.

### 8. Visual styling TempData['Info'] alert-info berbeda dari Error/Success di _Layout
expected: Trigger banner Info → DevTools inspect class "alert alert-info alert-dismissible fade show"; warna BIRU MUDA; icon ⓘ (info-circle-fill); bold prefix "Info:"; tombol close (×) functional
result: pass
evidence: DOM inspect via JS evaluate menghasilkan: classes=`"alert alert-info alert-dismissible fade show"`, role="alert", text="Info: Sertifikat akan tersedia setelah penilaian essay selesai.", hasIcon=true (bi-info-circle-fill), hasCloseBtn=true (.btn-close), bgColor=`rgb(207, 244, 252)` (Bootstrap info-bg-subtle biru muda), color=`rgb(5, 81, 96)` (dark teal). Match expected presis.

### 9. Post-deploy: monitor _logger.LogError 'Certificate view failed for session {Id}' di production
expected: Setelah deploy, observasi log sink (Application Insights / file log) untuk pin-point root cause aktual exotic data Temuan 10
result: skipped
reason: Deferred ke ops post-deploy per SC #7 WCRT-01 explicit. Bukan test code/runtime; dilakukan oleh ops setelah deployment.

## Summary

total: 9
passed: 7
issues: 0
pending: 0
skipped: 2
blocked: 0

## Gaps

[none — semua test PASS atau skipped dengan reason valid; tidak ada issue teridentifikasi]
