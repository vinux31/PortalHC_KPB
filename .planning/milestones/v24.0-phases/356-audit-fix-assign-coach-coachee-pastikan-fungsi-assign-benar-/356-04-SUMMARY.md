---
phase: 356-audit-fix-assign-coach-coachee
plan: 04
subsystem: coaching-ui
tags: [coaching, ui-guard, vanilla-js, audit-fix]
requires: []
provides: [af2-unit-guard, graduated-badge-fix]
affects: [Views/Admin/CoachCoacheeMapping.cshtml]
tech-stack:
  added: []
  patterns: [client-guard, defense-in-depth]
key-files:
  created: []
  modified:
    - Views/Admin/CoachCoacheeMapping.cshtml
key-decisions:
  - "AF-2 UI guard (Opsi A): disable + text-muted cross-unit checkbox; backend tak diubah"
  - "Guard hanya toggle disabled + text-muted, JANGAN style.display (milik filterCoacheesBySection); no CSS baru"
  - "Backstop submitAssign menolak >1 unit (defense-in-depth vs DOM manipulation)"
  - "D-06: cek IsCompleted sebelum IsActive agar badge Graduated tampil meski IsActive=false (AF-3)"
requirements-completed: [AF-2, AF-3]
duration: ~12 min
completed: 2026-06-09
---

# Phase 356 Plan 04: AF-2 UI Guard + D-06 Graduated Badge Summary

Modal assign kini membatasi pemilihan coachee ke satu unit per batch: `updateAssignmentDefaults()` mengunci unit dari centang pertama, men-disable + men-`text-muted` checkbox coachee unit lain, menampilkan hint, dan reset saat centang dikosongkan. Backstop di `submitAssign()` menolak submit bila >1 unit. Region tombol per-coachee disusun ulang (cek `IsCompleted` sebelum `IsActive`) agar badge "Graduated" tetap tampil setelah AF-3 menyetel `IsActive=false`.

## Tasks
- **Task 1** (`79806d45`): AF-2 guard — hint markup + extend updateAssignmentDefaults (reset+disable) + submitAssign backstop. Tidak sentuh `style.display`; no CSS baru.
- **Task 2** (`9824a48e`): D-06 — reorder badge region (IsCompleted first).

## Verification
- `dotnet build HcPortal.csproj` → 0 error (Razor compile OK).
- grep: `coacheeUnitConstraintHint` (markup+ref), `classList.toggle('text-muted'`, `selectedUnits.size > 1` backstop; guard tidak menulis `style.display`; badge order `@if (coachee.IsCompleted)` (L310) sebelum `else if (coachee.IsActive)` (L314).

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None.

## Next Phase Readiness
Plans kode 01-04 selesai (semua build 0 error, full suite 135/135 hijau). Ready for 356-05 (gate + UAT human-verify) — butuh user jalankan app lokal + SEED_WORKFLOW track id=4.
