---
phase: 421-retake-lifecycle-hardening
plan: 03
subsystem: assessment-retake
tags: [retake, participant-remove, cooldown-window, certificate-revoke, hardening, uat]
requires: [AssessmentAdminController, RetakeRules, RetakeCountingRules]
provides: [participant-remove-history-guard, cooldown-window-warning, maxattempts-confirm-modal, cert-revoke-confirm]
affects: [Controllers/AssessmentAdminController.cs, Helpers/RetakeRules.cs, Views/Admin/EditAssessment.cshtml, Views/Admin/ManagePackages.cshtml, Views/Admin/AssessmentMonitoringDetail.cshtml]
tech-stack:
  added: []
  patterns: [server-round-trip-soft-confirm, cascade-delete-cleanup, pure-rules-kill-drift, plus-7h-WIB-window-convention]
key-files:
  created:
    - HcPortal.Tests/ParticipantRemoveGuardTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
    - Helpers/RetakeRules.cs
    - Views/Admin/EditAssessment.cshtml
    - Views/Admin/ManagePackages.cshtml
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - HcPortal.Tests/RetakeRulesTests.cs
    - HcPortal.Tests/RetakeSettingsEndpointTests.cs
key-decisions:
  - "RTH-04 soft-confirm via server round-trip: history-cancel simpan keep-list TempData + warning -> redirect EditAssessment -> tombol 'Tetap Hapus' replay UserIds + confirmRemoveWithHistory=true (server-authoritative)."
  - "Cleanup arsip andalkan cascade DB (RemoveRange AttemptHistory tracked + SaveChanges) - 0 manual RemoveRange archive; test assert 0 orphan."
  - "D-02 predikat di RetakeRules.CooldownMayExceedWindow (pure, +7h WIB satu tempat) - controller+test panggil sama (kill-drift)."
  - "D-07 used-count no-op di POST (angka dari GET MaxInGroupAsync 421-02); modal pra-simpan client-side non-blocking."
  - "D-04 conditional confirm cabut-cert view-only (VM expose IsPassed/NomorSertifikat); pencabutan tetap server-side D-03 via service."
  - "UAT live nangkap 3 bug render (D-04 quote-break, D-07 bootstrap-timing, RTH-04 TempData JsonElement) yang build/grep/integration-test TAK nangkap."
requirements-completed: [RTH-02, RTH-04, RTH-05]
duration: 1 sesi
completed: 2026-06-23
---

# Phase 421 Plan 03: Participant-Remove Guard + Cooldown Warning + Cert-Revoke Confirm Summary

Tutup RTH-04 (guard hapus peserta ber-riwayat + cascade cleanup), RTH-05 (modal pra-simpan MaxAttempts non-blocking), porsi RTH-01/D-02 (warning cooldown>window), porsi RTH-02/D-04 (konfirmasi cabut-cert). Server-authoritative; UAT live nangkap+fix 3 bug render.

**Durasi:** 1 sesi · **Task:** 4 (3 kode + 1 UAT) · **File:** 8 (1 baru test + 7 modifikasi).

## Yang dibangun

- **T1 (RTH-04/D-06):** `EditAssessment` POST +param `confirmRemoveWithHistory`; guard deteksi Abandoned/StartedAt/AttemptHistory; history-cancel → keep-list TempData + warning → redirect → tombol "Tetap Hapus" replay UserIds+flag (server round-trip Pola B). Cleanup arsip via cascade DB existing. **ParticipantRemoveGuardTests 5/5** (Test 4: 0-orphan real SQL).
- **T2 (D-02 + D-07):** `RetakeRules.CooldownMayExceedWindow` pure (+7h WIB satu tempat) + warning non-blocking `UpdateRetakeSettings`; D-07 used-count no-op berdokumentasi (angka dari GET 421-02); modal pra-simpan ManagePackages non-blocking. **RetakeRules 32 unit** (incl boundary +7h kill-drift) + **RetakeSettingsEndpoint +2** (non-blocking save + parity).
- **T3 (RTH-02/D-04):** conditional confirm cabut-cert di `AssessmentMonitoringDetail` (view-only; VM expose IsPassed/NomorSertifikat); pencabutan tetap server-side (D-03 via service).
- **T4 (UAT live @5270):** drive Playwright 4 skenario + seed minimal + snapshot→restore (Seed Workflow). **Nangkap 3 bug render** (lihat di bawah), fix + re-verify, semua PASS.

## Verifikasi

- **xUnit:** ParticipantRemoveGuard 5/5 + RetakeRules 32 + RetakeSettingsEndpoint (+2) + RetakeCountingRules 5 + RetakeService 10 — **full suite 649/0/2** (no regresi).
- **Build:** 0-err.
- **UAT live @5270 (4/4 PASS pasca-fix):**
  - RTH-04: warning "1 peserta memiliki riwayat ujian..." + cancel → "Tetap Hapus" → delete (grp104 sisa hanya Taufik, Admin sessions=0) + **orphan archive=0** (cascade).
  - RTH-05/D-07: modal "...tidak bisa menambah percobaan baru. Lanjutkan menyimpan?" → "Ya, Lanjutkan" → Success (max tersimpan 1 meski used=2, **non-blocking**).
  - RTH-01/D-02: cooldown 120h > sisa window → Warning "...bisa melewati batas tutup ujian...tetap bisa disimpan" + Success (non-blocking); cooldown 24h → no warning.
  - RTH-02/D-04: sesi LULUS+cert → confirm "...SUDAH LULUS...MENCABUT sertifikat (nomor dihapus)...Lanjutkan?".
- **DB:** snapshot→seed→RESTORE WITH REPLACE; baseline verified (seed 0 leftover); SEED_JOURNAL cleaned.

## Deviations from Plan

**[UAT findings — 3 bug render fixed live]** Task 4 UAT nangkap 3 bug yang build/grep/integration-test TAK nangkap (semua di view baru, rendering-class):
1. **TEMUAN-1 (D-04):** `onsubmit="return confirm(@Html.Raw(Json.Serialize(resetConfirm)))"` — `Json.Serialize` keluarkan string double-quote yang tabrak atribut `onsubmit="..."` (juga double-quote) → atribut tertutup dini → `onsubmit` rusak `return confirm(`. **Fix:** `onsubmit="return confirm('@resetConfirm')"` (single-quote JS string, idiom existing form reset). Akar: idiom Json.Serialize di RESEARCH salah untuk atribut double-quote.
2. **TEMUAN-2 (D-07):** script modal inline jalan saat parse SEBELUM `bootstrap.bundle` (akhir layout) load → `bootstrap is not defined` (ReferenceError) → IIFE throw → interceptor submit tak attach → modal tak muncul. **Fix:** bungkus `DOMContentLoaded` + guard `typeof bootstrap` (lesson Phase 390.1).
3. **TEMUAN-3 (RTH-04):** TempData provider = **session** → nilai dideserialisasi `JsonElement` bukan `string` → `TempData["PendingKeepUserIds"] is string` false → tombol "Tetap Hapus" tak render. **Fix:** `TempData.Peek(...)?.ToString()` (JsonElement-safe).

Semua 3 fix di-commit `8800d0e2` + re-verify live PASS. **Total deviations:** 3 (UAT render-fix, bukan scope). **Impact:** positif — UAT membuktikan nilainya (3 bug produksi tertangkap sebelum ship).

## Self-Check: PASSED

- key-files ada di disk + ter-commit (T1 `e05b192a`, T2 `97b8e47b`, T3 `6043739b`, UAT-fix `8800d0e2`).
- Acceptance criteria 3 task + 4 skenario UAT semua PASS.
- Full suite 649/0/2 hijau; build 0-err; DB restored pristine.

## Issues Encountered

3 bug render ditemukan UAT (TEMUAN-1/2/3) — semua fixed + re-verified live. Pelajaran: view baru dengan inline JS/confirm/TempData WAJIB UAT browser — build/grep/integration-test tak menjangkau quote-escaping, script-timing, dan TempData-serialization render-class bugs.

## Next

Plan 03 COMPLETE. Phase 421 (RTH-01..05) semua tertutup. Sisa: gerbang fase (secure/validate) + finalize milestone v32.7 phase 421.
