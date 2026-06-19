---
phase: 402
slug: coaching-cross-unit-mapping
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-19
---

# Phase 402 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8.0.418) + Playwright (UI) |
| **Config file** | existing test project (see 402-RESEARCH.md §test strategy) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~CoachMapping|FullyQualifiedName~CDP"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~60–120 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick filtered test command
- **After every plan wave:** Run full suite
- **Before `/gsd-verify-work`:** Full suite must be green + `dotnet build` 0 error
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD (planner fills) | — | — | CXU-01..05 | — | — | unit/playwright | — | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Detailed map filled by gsd-planner + finalized by /gsd-validate-phase (Nyquist auditor).*

---

## Wave 0 Requirements

- [ ] Controller unit tests for CoachCoacheeMappingAssign cross-Bagian reject (CXU-02) + per-coachee unit ∈ UserUnits (CXU-03)
- [ ] Test eligible-coachee set-aware filter (CXU-01)
- [ ] Test CDP union self-scope multi-unit coach (CXU-05)
- [ ] Playwright: assign cross-unit within 1 Bagian (coachee unit-X + unit-Y → 1 coach in one batch)

*Detail derived from 402-RESEARCH.md "## Validation Architecture".*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cross-unit assign round-trip + coach multi-unit view | all CXU | Live browser UAT per CLAUDE.md Develop Workflow | `dotnet run` on localhost:5270 (branch ITHandoff) + DB local check |

*SQL-real multi-unit integration tests = Phase 404 (out of scope here).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
