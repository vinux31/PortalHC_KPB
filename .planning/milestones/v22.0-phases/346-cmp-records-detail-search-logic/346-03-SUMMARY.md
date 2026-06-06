---
phase: 346-cmp-records-detail-search-logic
plan: 03
subsystem: CMP authorization (Results/Certificate/CertificatePdf)
tags: [security, authz, access-control, refactor]
requires: []
provides: ["IsResultsAuthorized static helper", "section-scoped authz on 3 actions"]
affects: ["Controllers/CMPController.cs"]
tech-stack:
  added: []
  patterns: ["pure-static testable authz helper (mirror Phase 345 ComputeHistoryStats)"]
key-files:
  created: []
  modified: ["Controllers/CMPController.cs"]
key-decisions:
  - "roleLevel 'is >= 1 and <= 3' (NOT '<= 3') so roleLevel 0 (no-role/error) is rejected — T-346-05"
  - "Legacy userRoles.Contains(Admin/HC) removed — single-source via helper (D-09)"
  - ".Include(a=>a.User) verify-only — already present in all 3 actions, not duplicated"
requirements-completed: [REC-04]
duration: 9 min
completed: 2026-06-04
---

# Phase 346 Plan 03: REC-04 Authz — IsResultsAuthorized Summary

Melonggarkan otorisasi 3 action `CMPController.cs` (`Results`, `Certificate`, `CertificatePdf`) dari `owner || Admin || HC` menjadi `owner ∥ roleLevel 1-3 ∥ (roleLevel==4 && Section non-null && ownerSection==userSection)` via static helper `IsResultsAuthorized` (D-09 single-source, testable). SECURITY-SENSITIVE — melonggarkan akses hasil assessment tim (D-01 privasi).

**Tasks:** 2 | **Files:** 1 | **Commits:** 2 (`2fe9d028` helper, `989f3576` wiring)

## What was built

- **Task 1** (`2fe9d028`): `public static bool IsResultsAuthorized(string? ownerUserId, string currentUserId, int roleLevel, string? currentUserSection, string? ownerSection)` ditambah dekat `GetCurrentUserRoleLevelAsync`. Logic: owner→true; `roleLevel is >= 1 and <= 3`→true; `roleLevel==4 && !IsNullOrEmpty(currentUserSection) && ownerSection==currentUserSection`→true; else false.
- **Task 2** (`989f3576`): ketiga authz block diganti `var (user, roleLevel) = await GetCurrentUserRoleLevelAsync(); if (user==null) return Challenge(); bool isAuthorized = IsResultsAuthorized(assessment.UserId, user.Id, roleLevel, user.Section, assessment.User?.Section); if (!isAuthorized) return Forbid();`. Cek `userRoles.Contains(Admin/HC)` lama + `var userRoles` dihapus.

## Threat Model — all mitigated

| Threat | Mitigation (verified) |
|--------|----------------------|
| T-346-01 L4 null-Section info disclosure | `!string.IsNullOrEmpty(currentUserSection)` guard (grep ✓; unit 346-06) |
| T-346-02 L4 cross-section | `ownerSection==currentUserSection` required (unit 346-06) |
| T-346-03 L5/L6 EoP | only owner-check passes L5/L6 (unit 346-06) |
| T-346-04 IDOR | owner=`assessment.UserId` server-side, 3× helper call (grep ✓) |
| T-346-05 roleLevel-0 regression | `is >= 1 and <= 3` rejects 0 (grep ✓) |
| T-346-06 null-user bypass | `return Challenge()` preserved in all 3 (grep ✓) |

## Verification

- `dotnet build` → Build succeeded, 0 Error, 21 Warnings (unchanged — no new unused-var from removed userRoles).
- grep: helper present + null-guard + `is >= 1 and <= 3` ✓ · helper call exactly 3× ✓ · region L1815-2184 has 0 `userRoles.Contains(Admin/HC)` ✓ · `.Include(a=>a.User)` count still 5 (not duplicated) ✓ · no leftover `userRoles` in the 3 actions ✓.

## Deviations from Plan

None — plan executed exactly as written (helper signature, logic, and wiring verbatim; `roleLevel is >= 1 and <= 3` per plan's koreksi).

## Self-Check: PASSED

- file modified exists ✓ · 2 commits (`git log --grep="346-03"`) ✓ · all acceptance_criteria re-run PASS ✓ · build green ✓ · all 6 threats mapped to grep/test ✓.

## Notes

- **Plan numbering typo:** 346-03 plan text refers to "Plan 05" for the authz test matrix + UAT — that is incorrect. The 8-case xUnit matrix + Playwright authz UAT are in **346-06** (346-05 = REC-07/08/09 logic). Flagged for 346-06 executor/verifier.
- **AUTHZ-01 side-fix delivered:** L3/L4 supervisors can now open `Certificate` + `Results` → Worker Detail buttons from 346-02 (Lihat Hasil + Sertifikat) are now functional for atasan. Joint UAT in 346-06.
- Ready for 346-04 (Wave 2, same file CMPController.cs different region L652/704/753 — serial after this).
