---
phase: 401-proton-unit-resolution-hardening
plan: 03
subsystem: api
tags: [proton, coaching, unit-resolution, multi-unit, validation, no-clobber, reactivation, ui-indicator]

requires:
  - phase: 401-01
    provides: "ValidateAssignmentUnitInUserUnits shared helper + RED scaffolds"
  - phase: 401-02
    provides: "GetEligibleCoachees gate + read-path (shares CoachMappingController, Wave 2 after)"
provides:
  - "Assign/Edit/Import reject AssignmentUnit ∉ coachee.UserUnits active (after org-tree, before mutation/tx)"
  - "CleanupCoachCoacheeMappingOrg preserves a valid non-primary AssignmentUnit (no-clobber)"
  - "Import-reactivate preserves existing AssignmentUnit + validates; CoachCoacheeMappingReactivate rejects released-unit reactivation"
  - "D-01 on-demand orphan-unit indicator (alert-warning) on /Admin/CoachCoacheeMapping"
affects: [404]

tech-stack:
  added: []
  patterns:
    - "Write-path mass-assignment guard: client AssignmentUnit must pass org-tree (GetSectionUnitsDictAsync) + junction (shared helper) before mutation/transaction"
    - "No-clobber preserve-gate: keep valid non-primary unit ∈ UserUnits instead of resetting to primary (clobber only last-resort)"
    - "On-demand orphan indicator: GET computes orphan count → ViewBag → Bootstrap 5 alert-warning (CleanupReport idiom)"

key-files:
  created: []
  modified:
    - Controllers/CoachMappingController.cs
    - Views/Admin/CoachCoacheeMapping.cshtml
    - HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs
    - HcPortal.Tests/CleanupNoClobberTests.cs
    - HcPortal.Tests/ReactivateUnitValidationTests.cs

key-decisions:
  - "Validation placed AFTER each existing org-tree check, BEFORE mutation/BeginTransactionAsync (Edit Pitfall 4 — don't break Phase 129 rebuild)"
  - "Assign batch: req.CoacheeIds share one AssignmentUnit; loop rejects whole batch if ANY coachee lacks the unit (per-coachee picker is Phase 402, out of scope)"
  - "Import per-row check sets result.Status='Error' (per-row granularity, CONTEXT Claude's Discretion)"
  - "Cleanup clobber path (m.AssignmentUnit=userUnit) kept as LAST-resort only when existing unit ∉ UserUnits; preserve-gate runs first"
  - "Import-reactivate clobber line (inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim()) REMOVED — AssignmentUnit preserved (D-04)"
  - "AF-4 ±5s correlation window (EF.Functions.DateDiffSecond) UNTOUCHED — count stays 2 (D-05); only unit-validation guard inserted in Reactivate"
  - "D-01 rendered as alert-warning (not badge) for parity with CleanupReport idiom; ViewBag carries primitive int count + coachee-id list (no anonymous-type serialization)"
  - "Task 4 tests assert the ValidateAssignmentUnitInUserUnits decision primitive at helper level — controller mutations need HTTP context; deep SQL-real single-active deferred to Phase 404"

patterns-established:
  - "Client-supplied unit that becomes persisted AssignmentUnit MUST pass org-tree + junction validation on every write-path (Invariant #4)"
  - "Auto-fix/cleanup loops must not clobber valid persisted state — preserve-gate before any reset (data-loss vector, spec §10)"

requirements-completed: [PSU-03, PSU-04, PSU-05, PSU-07]

duration: ~2 sessions (paused mid-execution at human-verify checkpoint, resumed next day)
completed: 2026-06-19
---

# Phase 401 Plan 03: Write-Path Hardening + D-01 Orphan Indicator Summary

## What was built

Closed the write-path and visibility side of Phase 401 in `CoachMappingController` (shared file with 401-02 → executed in Wave 2 after it). Five changes:

1. **∈UserUnits validation (PSU-03)** — `Assign` (batch loop), `Edit` (pre-transaction, Pitfall 4), and `Import` (per-row) now reject any `AssignmentUnit` not in the coachee's active `UserUnits`, layered AFTER the existing org-tree check and BEFORE any mutation/transaction. Uses the 401-01 shared helper `ValidateAssignmentUnitInUserUnits`.
2. **Cleanup no-clobber (PSU-04)** — `CleanupCoachCoacheeMappingOrg` preserves a valid non-primary `AssignmentUnit` (∈ UserUnits) instead of clobbering it to primary; the old clobber path remains only as a last-resort fix when the unit is genuinely orphaned.
3. **Import-reactivate no-clobber (PSU-04/07a)** — removed the `inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim()` clobber; the existing unit is preserved and validated (reject row if released).
4. **Reactivate guard (PSU-07b)** — `CoachCoacheeMappingReactivate` rejects reactivation when `AssignmentUnit ∉ coachee.UserUnits active`. The AF-4 ±5s correlation window (`EF.Functions.DateDiffSecond`) is untouched (count stays 2, D-05).
5. **D-01 on-demand indicator (PSU-05)** — the `CoachCoacheeMapping` GET computes active mappings whose `AssignmentUnit` is empty or orphaned, surfaces a primitive count via `ViewBag.OrphanUnitMappings`, and the view renders a Bootstrap 5 `alert-warning` (CleanupReport idiom) telling operators those coachees won't appear in PROTON surfaces.

## Verification

- **`dotnet build`** 0 errors.
- **Grep contract**: `ValidateAssignmentUnitInUserUnits` present in Assign/Edit/Import/Cleanup/Reactivate (7 occurrences incl. helper def); `EF.Functions.DateDiffSecond` count = 2 (AF-4 intact); 0 `inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim()`; `OrphanUnitMappings` in controller + view.
- **Tests** (`Category!=Integration`): **391 passed / 0 failed / 2 skipped** (baseline 388/0/4 → +3 passing; the 2 RED 401-03 scaffolds turned GREEN; remaining skips are Phase-404 deep SQL-real). Target classes: 11/11 passed, 0 skipped.
- **BLOCKING human-verify checkpoint — D-01 indicator render (Razor; Phase 354 lesson)**: SATISFIED via Claude-driven Playwright on a snapshot→seed→restore cycle (Seed Data Workflow), app @ `localhost:5270`, login `admin@pertamina.com`:
  - Baseline (DB orphan=0): D-01 alert **ABSENT** (0 `.alert-warning` orphan alerts).
  - After seeding 1 orphan (`UPDATE` active mapping Id=12 `AssignmentUnit='ZZ-ORPHAN-401-TEST'` ∉ coachee UserUnits): D-01 alert **PRESENT** with count `1 mapping aktif`, classes `alert alert-warning alert-dismissible fade show`, `bi-exclamation-triangle` icon, `btn-close data-bs-dismiss="alert"`, and the "tidak muncul di surface PROTON" message.
  - DB restored (mapping Id=12 unit reverted to 'Alkylation Unit (065)', orphan=0 verified); `SEED_JOURNAL` marked cleaned; `.bak` deleted. Screenshot captured (`401-03-d01-present.png`, gitignored).

## Deviations

- None from plan. Task 4 finalized at the helper/decision-primitive level (as the plan prescribed) because the controller actions require HTTP context; the deep SQL-real single-active assertions remain explicitly deferred to Phase 404 (each remaining Skip references 404).

## What this enables

- Phase 401 PROTON unit-resolution hardening is functionally complete across all 6 plans (read-path gate, cert-gate, CDP/Bypass filter-axes, write-path hardening, and the operator-facing orphan indicator). Phase 404 (QA-01 deep integration smoke) can build on the now-GREEN decision primitives.
