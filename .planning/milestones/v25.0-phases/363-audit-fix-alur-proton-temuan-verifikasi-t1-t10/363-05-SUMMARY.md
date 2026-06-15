---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
plan: 05
subsystem: proton-gate
tags: [coach-mapping, year-gate, reactivation, bypass, logging]
requires:
  - "363-03: ProtonCompletionService 4-arg ctor (NewSvc test helper)"
provides:
  - "Gate reaktivasi cross-year: assignment Tahun N inactive non-bypass tidak bisa direaktivasi tanpa Tahun N-1 lulus (T3)"
  - "reactExempt: inactive Origin=Bypass tetap exempt (stempel permanen)"
  - "T9 log-warn Urutan non-kontigu di 2 call-site"
  - "T10 documented by-design (D-13)"
affects: [363-07]
tech-stack:
  added: []
  patterns: ["predikat-replikasi gate reaktivasi"]
key-files:
  created: []
  modified:
    - Controllers/CoachMappingController.cs
    - Controllers/AssessmentAdminController.cs
    - HcPortal.Tests/ProtonYearGateIntegrationTests.cs
key-decisions:
  - "Semua perubahan gate di blok PRE-tx — block return Json(success=false) tanpa rollback (RESEARCH A5)"
  - "Komentar 360 W-07/I-08 'JANGAN ubah cabang 1' direkonsiliasi (Open Q1): 360 melarang ubah EXEMPT bypass; 363 menambah gate di jalur reaktivasi"
requirements-completed: [T3, T9, T10]
duration: 14 min
completed: 2026-06-11
---

# Phase 363 Plan 05: Gate Reaktivasi Cross-Year (T3) + T9/T10 Summary

Loophole T3 HIGH ditutup: cabang-1 `CoachCoacheeMappingAssign` kini skip HANYA assignment aktif (filter `IsActive`), sehingga kandidat reaktivasi inactive non-bypass turun ke `IsPrevYearPassedAsync` dan kena hard-block D-05; inactive `Origin="Bypass"` tetap exempt via `reactExempt` (stempel permanen 360 D-04).

- Duration: 14 min | Tasks: 3/3 | Files: 4

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 (+T9 CoachMapping) | `f271ae11` | fix(363-05): gate reaktivasi cross-year di CoachCoacheeMappingAssign |
| 2+3a | `32f67656` | docs(363-05): T9 log-warn CreateAssessment + T10 by-design comment |
| 3b | `a79b2a73` | test(363-05): reactivation gate predicate facts |

## What Was Built

- **T3**: `hasForRequestedTrack` → `activeForRequestedTrack` (+`a.IsActive`); urutan exempt: active-skip → active-bypass exempt → `reactExempt` (inactive bypass) → gate penanda. Blok reaktivasi `:597-606` dan pesan hard-block tidak diubah.
- **T9**: log-warn di `CoachMappingController` (prevTrack null saat Urutan>1) + `AssessmentAdminController` CreateAssessment (prevTahunKe null saat protonUrutan>1) — log-only, tanpa throw/block; Urutan=1 normal tanpa warning.
- **T10**: komentar by-design D-13 di blok enforce-100% `BackfillProtonPenanda` + note RESOLVED di 363-FINDINGS.md. Nol perubahan logic.
- **Tests**: `ReactivationBlockedAsync` (replika byte-identik gate) + 3 fact — blocked/bypass-exempt/active-skip. Suite YearGate 7/7.

## Deviations from Plan

None - plan executed exactly as written. (Catatan komit: T9 CoachMapping ikut commit Task 1 karena satu file.)

## Verification

- Grep AC: `activeForRequestedTrack` ada, `hasForRequestedTrack` hilang, `reactExempt` ada, "JANGAN ubah cabang 1" hilang, `IsPrevYearPassedAsync` tetap, "Urutan tidak kontigu" 2 file, "T10/D-13" ada.
- `dotnet test --filter ProtonYearGateIntegration` → 7/7 PASS.
- UAT reaktivasi live → Plan 07.

## Self-Check: PASSED

## Next

Ready for 363-06 (2 polish CDPController: T5 "Belum Mulai" + T8 evidence-history).
