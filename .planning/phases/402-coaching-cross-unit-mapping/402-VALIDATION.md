---
phase: 402
slug: coaching-cross-unit-mapping
status: passed
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-19
finalized: 2026-06-21
---

# Phase 402 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> **Disposition (2026-06-21): Option 2 — fix gaps now.** CXU-01 (PARTIAL) + CXU-05
> (MISSING-automated, false-confidence) were closed by extracting pure static seams from
> production controllers and pointing the tests at them. No behavior change.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8.0.418) + Playwright (UI) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (EF InMemory; SQL-real Facts `[Skip]` → Phase 404) |
| **Quick run command** | `dotnet test --no-build --filter "FullyQualifiedName~CrossUnitAssignTests\|FullyQualifiedName~CdpCoachUnionScopeTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | filtered ~1s · full ~105s |

---

## Sampling Rate

- **After every task commit:** Run quick filtered test command
- **After every plan wave:** Run full suite
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

| Requirement | Plan | Secure Behavior | Test Type | Automated Test (production-calling) | Status |
|-------------|------|-----------------|-----------|-------------------------------------|--------|
| **CXU-01** eligible set-aware = whole Bagian (NOT unit-scoped) | 402-01/02 | no unit leak/loss; inactive + already-mapped excluded | unit | `CrossUnitAssignTests.Eligible_set_aware_is_unit_agnostic_and_excludes_inactive_and_already_mapped` → calls **`CoachMappingController.FilterEligibleCoachees`** | ✅ green |
| **CXU-02** cross-Bagian reject (coachee.Section == coach.Section) | 402-01/02 | block cross-Bagian assign | unit | `CrossUnitAssignTests.CoacheeSectionMatchesCoach_*` (3) → calls **`CoachMappingController.CoacheeSectionMatchesCoach`** | ✅ green |
| **CXU-03** per-coachee unit ∈ coachee.UserUnits (batch reject) | 402-01/02 | reject unit not owned by coachee | unit | `CrossUnitAssignTests.PerCoachee_unit_batch_rejects_*` → calls **`CoachMappingController.ValidateAssignmentUnitInUserUnits`** | ✅ green |
| **CXU-04** relaxed single-unit lock (1 batch, multi-unit) | 402-04 | client lock removed; Bagian-level backstop | e2e + UAT | Playwright cross-unit assign + live browser UAT (`7f5b6a17`) | ✅ green |
| **CXU-05** coach union self-scope + foreign-unit coercion | 402-03 | foreign unit → null = union (no cross-coach leak) | unit | `CdpCoachUnionScopeTests.Coerce_*` (6) + union/narrow (3) → calls **`CDPController.CoerceCoachUnitScope`** | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Seams extracted (2026-06-21, Option 2):**
- `CDPController.CoerceCoachUnitScope(IEnumerable<string> coachActiveUnits, string? requestedUnit)` — pure coercion (foreign/blank → null, owned → unchanged). Both call sites refactored to it (`FilterCoachingProton` + `ExportDashboardProgress`), removing a duplicated block.
- `CoachMappingController.FilterEligibleCoachees(IEnumerable<ApplicationUser>, IEnumerable<string>)` — pure eligible-set filter (active && not-already-mapped, **no unit scoping**). `CoachCoacheeMapping` builder refactored to it.

Both seams are pure (no DB) — DB queries + authz remain in the controllers (unchanged), so the new security surface is two pure functions.

---

## Wave 0 Requirements

- [x] Controller unit tests: CoachCoacheeMappingAssign cross-Bagian reject (CXU-02) + per-coachee unit ∈ UserUnits (CXU-03)
- [x] Eligible-coachee set-aware filter (CXU-01) — production seam `FilterEligibleCoachees`
- [x] CDP union self-scope + foreign-unit coercion (CXU-05) — production seam `CoerceCoachUnitScope`
- [x] Playwright + live UAT: assign cross-unit within 1 Bagian (coachee unit-X + unit-Y → 1 coach, one batch)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cross-unit assign round-trip + coach multi-unit dashboard view | CXU-04 (+ all CXU end-to-end) | Live browser UAT per CLAUDE.md Develop Workflow | `dotnet run` @ localhost:5270 (branch ITHandoff) + DB local check — DONE (`7f5b6a17`) |

*SQL-real multi-unit integration (single-active filtered-unique index, AssignmentUnit ∈ UserUnits at DB layer) = **Phase 404 QA-03/QA-04** — InMemory cannot enforce filtered-unique indexes. Stub: `CrossUnitAssignTests.SingleActive_invariant_is_sql_real_phase404` `[Skip]`.*

---

## Validation Audit 2026-06-21

| Metric | Count |
|--------|-------|
| Gaps found | 2 (CXU-01 PARTIAL, CXU-05 MISSING-automated) |
| Resolved (now production-calling) | 2 |
| Escalated to manual-only | 0 |
| Deferred to Phase 404 (SQL-real) | 1 (single-active invariant) |

**Test counts:** filtered 15 passed / 1 skipped · full suite **547 passed / 0 failed / 6 skipped** (was 540 → +7 cases, no regression). Build 0 error / 28 warn (baseline).

**⚠️ Re-trigger required (impl changed):** Option 2 extracted two seams from production controllers (`CDPController`, `CoachMappingController`). Per milestone policy, re-run **`/gsd-code-review 402`** and **`/gsd-secure-phase 402`** to re-cover the changed surface (specifically: code-review IN-06 false-confidence is now resolved; secure T-402-08/09 coercion path is now a named pure seam). The seams are pure + behavior-identical, so re-coverage is expected to be a fast confirm.

---

## Validation Sign-Off

- [x] All requirements have `<automated>` verify or live-UAT (CXU-04) coverage
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** passed (Option 2 — gaps fixed via production seams; re-trigger review+secure flagged)
