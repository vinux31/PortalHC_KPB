---
phase: 420-form-create-edit-persistensi-field-ux-pre-post
plan: 02
subsystem: api
tags: [aspnet-mvc, access-control, guard, redirect, assessment, prepost, completed-lock, manual-entry, routing]

# Dependency graph
requires:
  - phase: 420-01-persistensi-field
    provides: "Edit std loop hardening (ValidUntil + retake) yang tak berbenturan dgn guard yang diangkat sebelum cabang Pre-Post"
  - phase: 405-attempt-retake-backend
    provides: "RetakeServiceFixture (real-SQL @SQLEXPRESS, MigrateAsync full chain) — template integration test"
provides:
  - "FORM-05: guard Status==Completed group-aware (AnyAsync LinkedGroupId) diangkat ke ATAS cabang Pre-Post di POST EditAssessment — sesi/grup Completed ditolak edit (redirect ManageAssessment + TempData Error, metadata tak dimutasi)"
  - "FORM-06: GET EditAssessment redirect IsManualEntry -> EditManualAssessment (TrainingAdmin) — routing-correctness"
  - "EditGuardRedirect420Tests.cs — 5 test real-SQL (action-invoke + replika-body) lock+redirect+negatif"
affects: [420-03-ux-prepost, 422-samepackage-shuffle, 423-certificate-issuance, 424-grading-gating]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Guard server-authoritative DIANGKAT sebelum cabang yang melakukan mutasi+return (anti guard-tak-terjangkau)"
    - "Group-aware completed lock via AnyAsync(LinkedGroupId==g && Status==Completed) — blokir seluruh grup bila SATU sesi Completed"
    - "Redirect routing manual-entry di GET (mirror filter IsManualEntry analog controller lain)"
    - "Test guard/redirect = action-invoke (return-early → RedirectToActionResult aktual) + replika-body (jalur lanjut, hindari render View null!-deps)"

key-files:
  created:
    - "HcPortal.Tests/EditGuardRedirect420Tests.cs"
  modified:
    - "Controllers/AssessmentAdminController.cs"

key-decisions:
  - "FORM-05 group-aware PENUH (Open Q#1): blokir POST Edit bila ADA SATU sesi grup Pre-Post (LinkedGroupId sama) Completed; standard (LinkedGroupId null) cek sesi sendiri (identik guard lama)"
  - "Guard lama single-mode (:2014-2019) DIBIARKAN (defense-in-depth jalur standard) — identik dgn guard baru untuk standard non-grup, harmless, default-safe per plan"
  - "TIDAK reuse AssessmentEditEligibility.IsEditableAsync (semantik TERBALIK: true HANYA bila Completed) — guard ditulis sendiri"
  - "Test campuran: action-invoke (Test 1/2/4 return-early) + replika-body guard (Test 3/5 jalur lanjut hindari deps null!/View render)"

patterns-established:
  - "Pattern: guard akses-kontrol diangkat ke atas SEMUA cabang mutasi (cabang Pre-Post return mendahului guard lama → bug E-04 ditutup dgn pengangkatan)"
  - "Pattern: group-aware lock = AnyAsync atas LinkedGroupId, bukan cek anchor tunggal"

requirements-completed: [FORM-05, FORM-06]

# Metrics
duration: 4min
completed: 2026-06-22
---

# Phase 420 Plan 02: Form Create/Edit — Guard Lock Completed + Redirect Manual Summary

**Tutup dua celah server-authoritative di form Edit assessment: angkat guard `Status==Completed` group-aware ke ATAS cabang Pre-Post (POST Edit) sehingga sesi/grup Completed ditolak edit (FORM-05/E-04), dan redirect GET Edit sesi entry-manual ke EditManualAssessment (FORM-06/E-08) — tanpa mengubah perilaku sesi standard yang sah.**

## Performance

- **Duration:** 4 min
- **Started:** 2026-06-22T13:56:13Z
- **Completed:** 2026-06-22T14:00Z
- **Tasks:** 2
- **Files modified:** 2 (1 created, 1 modified)

## Accomplishments
- **FORM-05 (E-04):** Guard `Status=="Completed"` group-aware DIANGKAT ke atas cabang Pre-Post di POST `EditAssessment` (setelah null-check, sebelum `if (AssessmentType=="PreTest"||"PostTest")`). Bug asal: cabang Pre-Post `return RedirectToAction("ManageAssessment")` mendahului guard lama single-mode → guard tak pernah tercapai untuk Pre-Post (metadata pasca-Completed bisa dimutasi). Group-aware: bila SATU sesi dalam grup (LinkedGroupId sama) Completed → seluruh grup terkunci dari Edit. Standard (LinkedGroupId null) → cek sesi sendiri (identik guard lama). Menolak edit → redirect ManageAssessment + TempData Error, metadata TIDAK dimutasi.
- **FORM-06 (E-08):** GET `EditAssessment` tambah `if (assessment.IsManualEntry) RedirectToAction("EditManualAssessment","TrainingAdmin", new { id })` tepat setelah null-check (sebelum query berat / `bool isPrePost`). Sesi entry-manual diarahkan ke form edit manual yang benar, bukan form online (mirror filter IsManualEntry analog TrainingAdminController EditManualAssessment GET).
- **Test:** `EditGuardRedirect420Tests.cs` (5 Fact real-SQL `IClassFixture<RetakeServiceFixture>`) — group-aware lock (Pre-Post sibling Completed), standard lock (regresi), negatif all-Open (backward-compat), redirect manual, negatif online. **5/5 GREEN @SQLEXPRESS.**
- **Backward-compat & keamanan terjaga:** `[Authorize(Roles="Admin, HC")]` (GET+POST) + `[ValidateAntiForgeryToken]` (POST) DIPERTAHANKAN; sesi standard non-Completed non-manual tetap dapat di-Edit (non-integration suite 448/0/2 — identik baseline 420-01, no regresi).

## Task Commits

Each task was committed atomically:

1. **Task 0: Wave-0 test stubs FORM-05 (lock group-aware) + FORM-06 (redirect manual)** - `b09c8301` (test)
2. **Task 1: FORM-05 angkat guard Completed group-aware + FORM-06 redirect manual GET** - `e043983c` (feat)

**Plan metadata:** (final docs commit below)

## Files Created/Modified
- `HcPortal.Tests/EditGuardRedirect420Tests.cs` (created) - 5 test real-SQL (RetakeServiceFixture, MigrateAsync full chain @SQLEXPRESS). Test 1 group-aware lock (Pre-Post, Post Completed → POST Edit Pre Open ditolak + Title tak berubah), Test 2 standard lock (Completed tunggal ditolak + non-mutasi), Test 3 negatif all-Open (replika-body guard `AnyAsync(...Completed)==false`), Test 4 redirect manual (`RedirectToActionResult` EditManualAssessment/TrainingAdmin/route id), Test 5 negatif online (replika-body `IsManualEntry==false`). Action-invoke + null!-substitute deps (guard/redirect tak deref deps lain); MakeController pola AssessmentWindowRemovalTests.
- `Controllers/AssessmentAdminController.cs` (modified) - GET EditAssessment (:1705) +redirect IsManualEntry; POST EditAssessment (:1838-1850) +guard group-aware `isCompleted` (`AnyAsync` bila LinkedGroupId, else `Status=="Completed"`) → redirect ManageAssessment+TempData Error, ditempatkan SEBELUM cabang Pre-Post (:1854). Guard lama (:2014-2019) dibiarkan.

## Decisions Made
- **FORM-05 group-aware PENUH (Open Q#1 final):** memblokir POST Edit bila `AnyAsync(a => a.LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed")` true — blokir SELURUH metadata grup bila ADA satu sesi Completed. Standard (LinkedGroupId null) jatuh ke `isCompleted = assessment.Status == "Completed"` (identik guard lama). Penempatan SEBELUM cabang Pre-Post (`:1838`) menutup bug E-04 (cabang Pre-Post `return :2010` mendahului guard lama `:2014`). Threat T-420-lock dimitigasi server-side.
- **Guard lama single-mode (`:2014-2019`) DIBIARKAN (default-safe per plan):** untuk standard non-grup, guard baru (LinkedGroupId null → `isCompleted = Status=="Completed"`) IDENTIK dengan guard lama → keduanya cover jalur standard, tidak duplikat-berbahaya. Plan memberi opsi hapus guard lama HANYA bila guard baru pasti meng-cover standard; dipilih default ANGKAT guard baru + BIARKAN guard lama (defense-in-depth, aman).
- **TIDAK reuse `AssessmentEditEligibility.IsEditableAsync`:** semantik TERBALIK (`true` HANYA bila Completed — untuk edit jawaban peserta sesi selesai). FORM-05 mau MEMBLOKIR Completed → guard ditulis sendiri.
- **Test campuran action-invoke + replika-body:** Test 1/2/4 = action-invoke (guard/redirect return lebih awal → assert `RedirectToActionResult` aktual + non-mutasi). Test 3/5 (jalur "lanjut") = replika-body predikat guard (`AnyAsync(...Completed)==false` / `IsManualEntry==false`) agar tidak menjalankan action penuh yang menyentuh deps `null!` / render `View()` (custom override butuh ActionDescriptor) — membuktikan guard TIDAK memblokir sesi yang sah.

## Deviations from Plan

None - plan executed exactly as written.

(Catatan non-deviasi: nomor baris di plan/RESEARCH bergeser ~+6..+18 dari nilai literal karena tambahan 420-01 — struktur identik dan terverifikasi: GET null-check + `bool isPrePost`; POST null-check + cabang Pre-Post + guard lama. Penyisipan dilakukan pada anchor struktural, bukan nomor baris absolut.)

## Issues Encountered
None - kedua task berjalan sesuai plan. Build hijau tiap task (0 error, 25 warning pre-existing out-of-scope); EditGuardRedirect420 5/5 GREEN @SQLEXPRESS; non-integration suite 448/0/2 (identik baseline 420-01, no regresi).

## Backward-Compat Verification (mode Standard)
- POST Edit: guard baru HANYA menambah blok `isCompleted` di atas cabang Pre-Post; tidak mengubah/menghapus logika existing. Standard non-Completed → `isCompleted=false` → lanjut normal. Test 3 (replika) membuktikan grup all-Open tak terblokir.
- GET Edit: redirect HANYA untuk `IsManualEntry==true`; sesi online (IsManualEntry=false) tidak terdampak (Test 5). Tidak mengubah query/ViewBag/View existing.
- `[Authorize(Roles="Admin, HC")]` (GET :1690 / POST :1820) + `[ValidateAntiForgeryToken]` (POST :1821) DIPERTAHANKAN (tidak disentuh).

## Known Stubs
None - perubahan murni guard/redirect logic; tidak ada placeholder/empty-value/komponen tanpa data-source.

## User Setup Required
None - no external service configuration required. migration=FALSE (tidak ada perubahan schema).

## Next Phase Readiness
- **Plan 420-03** (UX Pre-Post FORM-07..11 + rename `AssessmentTypeInput`→`CreationMode` FORM-10; e2e FORM-01 lifecycle penuh) siap — guard/redirect 420-02 terpisah dari redesign view Create.
- **Secure-phase formal (`gsd-secure-phase 420`):** threat T-420-lock (Tampering metadata Completed) + T-420-manual + T-420-csrf + T-420-authz sudah dimitigasi server-side; gerbang secure terpisah memverifikasi.
- migration=FALSE → notify IT saat bundle deploy dengan flag migration=FALSE untuk plan ini.

## Self-Check: PASSED

- Files: FOUND HcPortal.Tests/EditGuardRedirect420Tests.cs, Controllers/AssessmentAdminController.cs, 420-02-SUMMARY.md
- Commits: FOUND b09c8301 (test), e043983c (feat)
- Build: 0 error; EditGuardRedirect420 5/5 GREEN @SQLEXPRESS; non-integration suite 448/0/2 (no regresi baseline 420-01)
- Grep gates: guard `LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed"` (:1845) SEBELUM cabang Pre-Post (:1854); `RedirectToAction("EditManualAssessment", "TrainingAdmin"` (:1706) di GET Edit; `[Authorize(Admin,HC)]`+`[ValidateAntiForgeryToken]` utuh

---
*Phase: 420-form-create-edit-persistensi-field-ux-pre-post*
*Completed: 2026-06-22*
