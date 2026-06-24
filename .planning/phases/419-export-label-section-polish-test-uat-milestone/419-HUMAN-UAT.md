---
status: partial
phase: 419-export-label-section-polish-test-uat-milestone
source: [419-VERIFICATION.md]
started: 2026-06-24T17:10:00Z
updated: 2026-06-24T17:10:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. 4 e2e Playwright real-browser UAT @5277 (cross-milestone × Section)
expected: D-04.1 lifecycle (render A–F + header Section + pagination + resume + export label live), D-04.2 Inject×Section (skor/cert benar), D-04.3 LinkPrePost×Section koherensi (link inject-Pre→room Post sukses, online untouched), D-04.4 Add/Remove×Section + pagination (eager-assign per-section konsisten) — keempat PASS runtime (un-fixme + `npx playwright test --workers=1`).
result: [pending]
note: Spec masih `test.fixme` draft; D-04.2/3/4 baru code-analyzed. Full exam-taking lifecycle sudah ter-UAT di 415–418; hanya interaksi gabungan Section yang belum di-run end-to-end live. Lesson 354 (Razor/JS/SignalR WAJIB real-browser).

### 2. PDF export per-peserta (BulkExportPdf) berisi heading "Section {n}: {Nama}" + huruf A–F
expected: PDF berisi 3 heading Section urut [Section 1, Section 2, Lainnya] + huruf opsi A–F sesuai opsi dinamis.
result: [largely-satisfied this session — pending optional manual UI download double-check]
note: LIVE UAT @5277 sesi ini SUDAH membuktikan: BulkExportPdf HTTP 200 (zip per-peserta PDF), `extract_text` menemukan "Section 1: Proses Alkilasi" + "Section 2: Sistem Pendingin" + "Lainnya" + "Detail Jawaban per Soal". (Verifier mendapat 204 karena DB sudah di-RESTORE pristine saat ia fetch — bukan cacat kode.) Hanya double-check download via klik UI yang tersisa.

### 3. Audit milestone v32.6 formal (20/20 REQ + koherensi lintas-milestone)
expected: PASSED 20/20 REQ ter-cover + interaksi lintas-milestone (Inject v32.2 / LinkPrePost 397 / Add-Remove v32.5) koheren, siap ship.
result: [pending]
note: Concern tingkat-MILESTONE — dilakukan via `/gsd-audit-milestone v32.6` setelah phase complete, bukan di verifikasi phase ini. Bahan 20/20 + analisis koherensi by-design sudah disiapkan di 419-AUDIT-READINESS.md.

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
