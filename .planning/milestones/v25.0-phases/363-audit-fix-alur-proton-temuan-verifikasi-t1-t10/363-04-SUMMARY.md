---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
plan: 04
subsystem: grading
tags: [grading, sertifikat, validuntil, regrade]
requires: []
provides:
  - "RegradeAfterEditAsync Fail→Pass set NomorSertifikat saja — ValidUntil ikut setup sesi HC"
affects: [363-07]
tech-stack:
  added: []
  patterns: []
key-files:
  created: []
  modified:
    - Services/GradingService.cs
key-decisions:
  - "Surgical deletion 2 baris per D-10 LOCKED; edge Pass→Fail→Pass ValidUntil null = accepted, cek UAT Plan 07"
requirements-completed: [T6]
duration: 4 min
completed: 2026-06-11
---

# Phase 363 Plan 04: Drop ValidUntil Hardcode di Regrade (T6) Summary

Jalur regrade Fail→Pass (`RegradeAfterEditAsync:516-521`) tidak lagi hardcode `ValidUntil = today + 3 tahun` — kini set `NomorSertifikat` saja, paritas penuh dengan jalur pass normal `GradeAndCompleteAsync:286`; dua sertifikat dari sesi sama tidak bisa lagi punya masa berlaku beda tergantung jalur penerbitan.

- Duration: 4 min | Tasks: 1/1 | Files: 1

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `bb2df303` | fix(363-04): drop hardcoded ValidUntil +3yr from regrade Fail->Pass |

## What Was Built

- Hapus `var validUntil = DateOnly.FromDateTime(certNow).AddYears(3);` + `.SetProperty(r => r.ValidUntil, validUntil)`.
- Komentar penanda `// T6/D-10: ValidUntil mengikuti setup sesi HC (paritas GradeAndCompleteAsync)`.
- TIDAK disentuh: revoke Pass→Fail (:482-483 — `ValidUntil (DateOnly?)null` tetap), hook Phase 360 (`RemoveExamOriginAsync`/`RevertPendingToMenungguAsync`/`EnsureAsync`/`MarkPendingReadyIfAnyAsync`), retry-loop cert-number.

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- Grep AC 5/5: `AddYears(3)` hilang, `SetProperty ValidUntil, validUntil` hilang, `NomorSertifikat, nomor` tetap (:520), revoke null tetap (:483), komentar T6/D-10 ada (:516).
- `dotnet build` 0 error; `dotnet test --filter "Category!=Integration"` → 180/180 PASS.
- UAT regrade live → Plan 07 (termasuk edge double-flip).

## Self-Check: PASSED

## Next

Ready for 363-05 (T3 HIGH loophole reaktivasi mapping skip year-gate).
