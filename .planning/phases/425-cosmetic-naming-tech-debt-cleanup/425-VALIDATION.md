---
phase: 425
slug: cosmetic-naming-tech-debt-cleanup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 425 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET) |
| **Config file** | none — existing test project |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~ExamTimeRulesTests|FullyQualifiedName~ManualEntryRules"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~baseline suite 748/0/2 |

---

## Sampling Rate

- **After every task commit:** Run quick run command
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** suite runtime

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| {N}-01-01 | 01 | 1 | CLN-XX | — | {expected behavior} | unit | `{command}` | ✅ / ❌ W0 | ⬜ pending |

*Planner fills this map. Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Parity test extends `ExamTimeRulesTests.cs` — CLN-04 (formula lama vs `AllowedExamSeconds` identik di 4 situs)
- [ ] Cross-validation test (NEW) — CLN-02 (mismatch → warning + tetap simpan; match → no warning)

*Planner finalizes Wave 0 stubs.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| {behavior} | CLN-XX | {reason} | {steps} |

*Planner fills — CLN-01/03/05 likely grep/static-verifiable (not manual).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency acceptable
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
