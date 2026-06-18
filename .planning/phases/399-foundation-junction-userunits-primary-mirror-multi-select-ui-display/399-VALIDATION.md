---
phase: 399
slug: foundation-junction-userunits-primary-mirror-multi-select-ui-display
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 399 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Derived from 399-RESEARCH.md "## Validation Architecture". Planner fills the Per-Task Verification Map.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8 / EF Core 8) — see existing test project |
| **Config file** | TBD by planner (existing `*.Tests` csproj) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~UserUnit"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~TBD seconds (suite ~351+ tests historically) |

---

## Sampling Rate

- **After every task commit:** Run quick command (`dotnet test --filter ~UserUnit`)
- **After every plan wave:** Run full suite (`dotnet test`)
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error + `dotnet ef database update` applied locally
- **Max feedback latency:** TBD seconds

---

## Per-Task Verification Map

> Planner MUST fill one row per task. Note Nyquist caveat: EF-InMemory does NOT enforce
> the filtered-unique `(UserId) WHERE IsPrimary=1` index — full SQL-real invariant tests
> are deferred to Phase 404 (QA). Phase 399 still requires automated coverage for
> write-through mirror logic, primary-recompute, audit set-diff, import parse, and MU-07 guard.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 399-01-01 | 01 | 0 | MU-05 | — | Migration applies + backfill 1 primary-row/pekerja (Unit non-null) | manual+sql | `dotnet ef database update` + DB check | ❌ W0 | ⬜ pending |
| 399-xx-xx | TBD | TBD | MU-01..07 | TBD | TBD | unit/integration | TBD | TBD | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Identified in RESEARCH.md "## Validation Architecture" (planner to finalize file names):

- [ ] Test write-through `SyncUserUnitsAsync` — set persisted, exactly 1 IsPrimary, mirror `ApplicationUser.Unit` = primary (MU-02)
- [ ] Test primary-recompute — remove primary → promote next; clear all → `Unit=null` + 0 IsPrimary rows (MU-02)
- [ ] Test audit set-diff — units added/removed/primary-changed produces correct change log entries (MU-02)
- [ ] Test junction-write validation — `Unit ∈ units-of-Bagian` rejects out-of-Bagian unit (MU-05)
- [ ] Test Excel import parse — pipe split `UnitA|UnitB`, first=primary, dedup, per-unit Bagian validation, backward-compat 1-unit (MU-04)
- [ ] Test MU-07 guard — remove unit referenced by active CoachCoacheeMapping → confirm→auto-deactivate path; referenced by active ProtonTrackAssignment → HARD-BLOCK (MU-07)
- [ ] Integration: backfill idempotency (run twice → no duplicate primary rows)
- [ ] (Deferred to Phase 404) SQL-real filtered-unique index enforcement — NOT in Phase 399 (EF-InMemory limitation)

*Existing xUnit infrastructure covers framework; new test files added per above.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration + backfill on real SQLEXPRESS | MU-05 | Schema/DB state not unit-testable | `dotnet ef database update` localhost; verify each existing user w/ Unit has 1 primary UserUnits row, Unit-null users have 0 rows |
| Multi-select checkbox-list + primary radio renders + round-trips | MU-01/MU-03 | Razor dynamic — grep+build insufficient (project Lesson Phase 354) | `dotnet run` localhost:5277 + Playwright: assign 2 units, save, reopen → both checked, primary marked |
| Display all units (primary marked) across 7 surfaces incl _PSign | MU-03 | Visual render incl cert/print | Browser check Profile/WorkerDetail/Settings/ManageWorkers/Excel/Home/_PSign |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency acceptable
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
