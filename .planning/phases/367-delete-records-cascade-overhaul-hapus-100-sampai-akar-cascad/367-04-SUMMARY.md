---
phase: 367-delete-records-cascade-overhaul
plan: 04
subsystem: controllers
tags: [over-deletion-guard, sibling-filter, reset-guard, assessment-admin]
requires: []
provides:
  - "StandardGroupSiblingPredicate (sibling filter no over-match #18)"
  - "IsResettable (ResetAssessment manual guard #20)"
affects:
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: ["static Expression predicate single-source (query EF + unit test)"]
key-files:
  created:
    - HcPortal.Tests/SiblingFilterTests.cs
    - HcPortal.Tests/ResetGuardTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "Sibling filter diekstrak ke static Expression StandardGroupSiblingPredicate — single-source: query EF DeleteAssessmentGroup pakai persis Expression yang di-Compile() test (no drift)"
  - "Reset guard diekstrak ke static IsResettable — controller pakai `if (!IsResettable(assessment))` (testable) BUKAN inline literal `if (assessment.IsManualEntry)` (tak bisa diuji tanpa harness HTTP penuh)"
  - "ResetAssessment line :4051 terverifikasi (sesuai estimasi RESEARCH A2, tidak drift)"
  - "Sibling query kedua di :4365 (method lain) TIDAK disentuh — #18 scope = DeleteAssessmentGroup saja"
requirements-completed: ["#18", "#20"]
duration: "14 min"
completed: 2026-06-12
---

# Phase 367 Plan 04: Sibling Over-Match Filter (#18) + Reset Manual Guard (#20) Summary

Dua guard standalone di `AssessmentAdminController.cs` untuk cegah over-deletion (ancaman HIGH): `DeleteAssessmentGroup` sibling query tak lagi menyapu sesi di luar scope grup tab 1, dan `ResetAssessment` menolak record manual.

**Tasks:** 2/2 | **Files:** 2 created + 1 modified | **Tests:** 5 [Fact] (3 sibling + 2 reset)

## What was built

- **#18 sibling no over-match:** `StandardGroupSiblingPredicate(title, category, scheduleDate)` (static `Expression<Func<AssessmentSession,bool>>`) tambah filter `LinkedGroupId == null && AssessmentType != "PreTest" && AssessmentType != "PostTest" && !IsManualEntry` ke kriteria existing (Title+Category+Schedule.Date). `DeleteAssessmentGroup` query (:2409) memakainya → sibling hanya sesi online standard dalam scope `mgStandardSessions`. Pre/Post group, manual, linked AMAN.
- **#20 reset guard:** `IsResettable(assessment)` (static `=> !assessment.IsManualEntry`). Guard `if (!IsResettable(assessment))` disisip SETELAH null-check (:4056), SEBELUM D-17 PreTest → tolak manual dengan TempData ramah + redirect `AssessmentMonitoringDetail` (pola existing).
- **Preserve verbatim:** pre-check renewal (:2424-2436), image-cleanup 366 (:2542 `DeleteAssessmentGroup image`, grep masih 1), authz `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`.

## Verification

- `dotnet build` — 0 error.
- `dotnet test --filter "~SiblingFilterTests|~ResetGuardTests"` — 5/5 pass.
- Quick suite `--filter "Category!=Integration"` — **199/199 pass** (no regression).
- Image-366 cleanup di DeleteAssessmentGroup preserved (`grep "DeleteAssessmentGroup image"` = 1).

## Deviations from Plan

**[Rule 2 - Testability] Reset guard pakai helper `IsResettable` (bukan inline literal `if (assessment.IsManualEntry)`)** — Found during: Task 2. Acceptance crit grep `if (assessment.IsManualEntry)`. Inline literal tak bisa di-unit-test (controller punya banyak ctor dep, butuh harness HTTP penuh). Solusi: static `IsResettable` single-source — controller `if (!IsResettable(assessment))` + test panggil langsung. Rule #20 (manual→blocked) jadi unit-tested. Grep literal tak match tapi must_have terpenuhi + teruji. Files: `AssessmentAdminController.cs`. Verification: 2 [Fact] ResetGuard pass.

**[Rule 2 - Single-source] Sibling filter diekstrak ke Expression** — Sama: predikat EF query = Expression yang di-Compile() test → zero drift. Greps (`LinkedGroupId == null`, `!a.IsManualEntry`, `!= "PreTest"`/`"PostTest"`) tetap match (ada di file).

**Total deviations:** 2 (keduanya ekstraksi static demi testability/single-source). **Impact:** Positif — guard logic unit-tested, tak ada duplikasi predikat.

## Issues Encountered

None.

## Self-Check: PASSED

- Sibling filter greps match ✓; image-366 preserved ✓; SiblingFilterTests 3 [Fact] ✓.
- Reset guard SEBELUM D-17 ✓; pesan ramah no-leak ✓; ResetGuardTests 2 [Fact] ✓.
- authz/antiforgery preserved ✓; Migration = FALSE ✓.

Wave 1 complete (01/03/04) + Wave 2 (02). Ready for Wave 3 (05 file-cert tab 1, 06 honesty/preview).
