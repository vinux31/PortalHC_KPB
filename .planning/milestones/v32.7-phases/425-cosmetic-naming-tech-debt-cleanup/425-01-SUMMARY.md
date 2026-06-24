---
phase: 425-cosmetic-naming-tech-debt-cleanup
plan: 01
subsystem: database
tags: [xml-doc, data-annotations, razor, assessment, documentation, tech-debt]

# Dependency graph
requires:
  - phase: 424-grading-dedup-flow-gating
    provides: Gating Pre→Post + grading consistency (konteks model AssessmentSession final pra-cleanup)
provides:
  - "AssessmentPhase ditandai RESERVED via XML-doc (kolom tetap, no migration) — dead-field FLOW-06"
  - "Komentar Status diselaraskan ke 7 nilai kanonik AssessmentConstants.AssessmentStatus"
  - "[Display(Name=\"Berlaku Sampai\")] sebagai satu sumber label ValidUntil"
  - "XML-doc LinkedSessionId + RenewsSessionId + RenewsTrainingId dikoreksi: app-level null-clear (bukan DB ON DELETE SET NULL)"
  - "Komentar sentinel AssessmentPackageId (paket seed, bukan paket aktual)"
  - "Label ValidUntil konsisten 'Berlaku Sampai' di CreateAssessment + EditAssessment + AddManualAssessment"
affects: [425-02, 425-03, 425-04, milestone-audit-v32.7]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "RESERVED XML-doc marker untuk dead-field (alternatif drop kolom — nol-risiko skema)"
    - "[Display] sebagai satu sumber label (single source of truth) untuk field model lintas view"
    - "XML-doc FK menyebut perilaku app-level eksplisit + lokasi kode (bukan klaim DB cascade keliru)"

key-files:
  created: []
  modified:
    - Models/AssessmentSession.cs
    - Models/UserPackageAssignment.cs
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/EditAssessment.cshtml

key-decisions:
  - "AssessmentPhase di-RESERVED via XML-doc, BUKAN drop (D-01): hindari migration destruktif di fase cleanup; migration=FALSE"
  - "Koreksi PA-04 diperluas ke RenewsSessionId/RenewsTrainingId (Rule 1): klaim 'ON DELETE SET NULL' identik keliru — FK sebenarnya DeleteBehavior.NoAction + null-clear app-level"
  - "AddManualAssessment.cshtml tidak diubah: label sudah konsisten 'Berlaku Sampai' (sesuai catatan plan)"

patterns-established:
  - "Pattern 1: RESERVED XML-doc untuk dead-field — pertahankan kolom nullable, tandai jangan baca/tulis"
  - "Pattern 2: [Display] di POCO sebagai satu sumber label; cshtml selaras via asp-for / teks label disamakan"
  - "Pattern 3: XML-doc FK harus menyebut perilaku riil (app-level vs DB cascade) + lokasi kode bukti"

requirements-completed: [CLN-01, CLN-03]

# Metrics
duration: 13min
completed: 2026-06-24
---

# Phase 425 Plan 01: Cosmetic / Naming / Tech-Debt Cleanup (label & dokumentasi) Summary

**Penyelarasan label & dokumentasi kosmetik murni (XML-doc/komentar/[Display]/label cshtml) tanpa ubah perilaku, binding, atau skema — AssessmentPhase RESERVED, Status 7-nilai, ValidUntil [Display] satu-sumber, koreksi 3 XML-doc FK app-level, sentinel AssessmentPackageId, dan label "Berlaku Sampai" konsisten di 3 view.**

## Performance

- **Duration:** 13 min
- **Started:** 2026-06-24T09:12:23Z
- **Completed:** 2026-06-24T09:25:30Z
- **Tasks:** 3
- **Files modified:** 4 (dari 5 di files_modified; AddManualAssessment.cshtml sudah konsisten → tak diubah)

## Accomplishments
- **CLN-03 (FLOW-06):** Kolom dead-field `AssessmentPhase` ditandai RESERVED via XML-doc (0 referensi app terkonfirmasi grep Controllers/Services/Views) — kolom tetap di skema, ZERO migration baru.
- **CLN-01 (FLOW-05):** Komentar `Status` usang 3-nilai diganti ke 7 nilai kanonik (`AssessmentConstants.AssessmentStatus`).
- **CLN-01 (FLD-5.2-06):** `[Display(Name="Berlaku Sampai")]` ditambahkan ke `ValidUntil` sebagai satu sumber label; label "Tanggal Expired Sertifikat" di CreateAssessment + EditAssessment diselaraskan ke "Berlaku Sampai".
- **CLN-01 (PA-04):** XML-doc `LinkedSessionId` dikoreksi — klaim "ON DELETE SET NULL" keliru → app-level null-clear (`RecordCascadeDeleteService.cs:235-237`). Deviasi Rule 1: koreksi identik diterapkan ke `RenewsSessionId`/`RenewsTrainingId` (FK = `DeleteBehavior.NoAction`).
- **CLN-01 (PA-05):** Komentar sentinel `AssessmentPackageId` — menjelaskan paket PERTAMA (seed), bukan paket aktual (`CMPController.cs:1087-1093`); soal aktual dari `ShuffledQuestionIds`.

## Task Commits

Setiap task di-commit atomik:

1. **Task 1: RESERVED XML-doc AssessmentPhase + komentar Status 7-nilai** - `87069c6e` (docs)
2. **Task 2: [Display] ValidUntil + koreksi XML-doc LinkedSessionId (+RenewsSessionId/RenewsTrainingId via Rule 1)** - `94694719` (docs)
3. **Task 3: Sentinel AssessmentPackageId + label ValidUntil selaras 'Berlaku Sampai' di 2 view** - `f5f6274a` (docs)

**Plan metadata:** (commit final docs — SUMMARY + STATE + ROADMAP)

## Files Created/Modified
- `Models/AssessmentSession.cs` - RESERVED XML-doc AssessmentPhase; komentar Status 7-nilai; [Display] ValidUntil; koreksi XML-doc LinkedSessionId + RenewsSessionId + RenewsTrainingId (app-level null-clear)
- `Models/UserPackageAssignment.cs` - Komentar sentinel AssessmentPackageId (paket seed, bukan aktual)
- `Views/Admin/CreateAssessment.cshtml` - Label ValidUntil "Tanggal Expired Sertifikat" → "Berlaku Sampai"
- `Views/Admin/EditAssessment.cshtml` - Label ValidUntil + komentar HTML "Tanggal Expired Sertifikat" → "Berlaku Sampai"

## Decisions Made
- **RESERVED bukan DROP (D-01):** AssessmentPhase dipertahankan di skema (nullable, aman) untuk hindari migration destruktif di fase cleanup. migration=FALSE.
- **AddManualAssessment.cshtml tak disentuh untuk label:** sudah konsisten "Berlaku Sampai" (sesuai catatan plan). File ini akan disentuh Plan 425-03 untuk warning display (scope berbeda).
- **Verifikasi referensi dokumentasi terhadap kode riil:** Sebelum menulis XML-doc/komentar, klaim diverifikasi terhadap sumber: `AssessmentPhase` 0-ref (grep), `RecordCascadeDeleteService.cs:235-237` (Delta #8), `ApplicationDbContext.cs` (NoAction), `CMPController.cs:1087-1093` (sentinel).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Dokumentasi Menyesatkan] Koreksi XML-doc RenewsSessionId/RenewsTrainingId**
- **Found during:** Task 2 ([Display] ValidUntil + koreksi XML-doc LinkedSessionId)
- **Issue:** Plan hanya menargetkan `LinkedSessionId` untuk koreksi klaim keliru "ON DELETE SET NULL". Saat verifikasi, ditemukan `RenewsSessionId` (baris 132/135) dan `RenewsTrainingId` (baris 139/142) mengandung klaim XML-doc identik & sama-sama keliru — `ApplicationDbContext.cs:181/186/246/251` jelas memakai `DeleteBehavior.NoAction` dengan komentar eksplisit "NoAction: SQL Server blocks ON DELETE SET NULL on self/cross FKs... Null-clearing on source delete is handled at application level". Selain itu acceptance criteria Task 2 mensyaratkan `grep "ON DELETE SET NULL"` tidak meninggalkan klaim positif.
- **Fix:** Kedua XML-doc dikoreksi ke perilaku riil (DeleteBehavior.NoAction + null-clear app-level), menyebut lokasi konfigurasi `ApplicationDbContext.cs:243-246` dan `:248-251`. Klaim "ON DELETE SET NULL" hanya tersisa dalam konteks negasi ("BUKAN ON DELETE SET NULL").
- **Files modified:** Models/AssessmentSession.cs
- **Verification:** `grep "^\s*/// ON DELETE SET NULL"` (klaim positif) == 0; build 0 error; suite 748/0/2.
- **Committed in:** `94694719` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 dokumentasi menyesatkan / Rule 1)
**Impact on plan:** Koreksi selaras tujuan PA-04 (dokumentasi akurat cegah salah-interpretasi) dan memenuhi acceptance criteria grep. Perubahan komentar murni — nol perilaku/skema. No scope creep (3 XML-doc FK semuanya satu kelas defect identik).

## Issues Encountered
None - semua task berjalan sesuai rencana; satu deviasi dokumentasi ditangani via Rule 1.

## Known Stubs
None - plan ini pure label/komentar/XML-doc; tidak ada data source baru, placeholder, atau TODO yang ditinggalkan.

## User Setup Required
None - tidak ada konfigurasi external service. migration=FALSE (tidak ada migration untuk dijalankan IT).

## Next Phase Readiness
- **Verifikasi:** build 0 error (24 warning baseline pre-existing, tak bertambah); full suite **748/0/2** (baseline identik, 0 regresi); diff hanya menyentuh 4 file model/view; tidak ada file Migrations/ baru.
- **Untuk Plan 425-03:** `AddManualAssessment.cshtml` belum disentuh oleh plan ini — siap untuk warning display 425-03.
- **Untuk milestone audit v32.7:** REQUIREMENTS.md:67 (CLN-03 "drop ATAU RESERVED") + ROADMAP.md (migration "KEMUNGKINAN TRUE") perlu rekonsiliasi ke keputusan final RESERVED + migration=FALSE (action item planner per RESEARCH §rekonsiliasi).
- **UAT manual (opsional):** @ http://localhost:5270 (ITHandoff) buka form Create/Edit/AddManual → label expiry tampil "Berlaku Sampai" konsisten.

## Self-Check: PASSED

**Files verified (4/4 exist):**
- FOUND: Models/AssessmentSession.cs
- FOUND: Models/UserPackageAssignment.cs
- FOUND: Views/Admin/CreateAssessment.cshtml
- FOUND: Views/Admin/EditAssessment.cshtml

**Commits verified (3/3 exist):**
- FOUND: 87069c6e (Task 1)
- FOUND: 94694719 (Task 2)
- FOUND: f5f6274a (Task 3)

---
*Phase: 425-cosmetic-naming-tech-debt-cleanup*
*Completed: 2026-06-24*
