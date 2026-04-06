---
phase: 296
slug: data-foundation-gradingservice-extraction
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-06
---

# Phase 296 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet test (xUnit/MSTest — existing project test setup) |
| **Config file** | none — use existing project test configuration |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 296-01-01 | 01 | 1 | FOUND-01, FOUND-02 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 296-01-02 | 01 | 1 | FOUND-05 | — | Nullable columns, no breaking change | build | `dotnet build` | ✅ | ⬜ pending |
| 296-02-01 | 02 | 1 | FOUND-03, FOUND-04 | — | Race condition guard, status guard | build | `dotnet build` | ✅ | ⬜ pending |
| 296-02-02 | 02 | 2 | FOUND-06, FOUND-07, FOUND-08, FOUND-09 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| GradingService correctly replaces inline grading | FOUND-03, FOUND-04 | Requires running app with test data | 1. Create assessment session 2. Submit exam via CMP 3. Verify score calculated correctly |
| Migration applies without data loss | FOUND-05 | Requires database with existing data | 1. Run `dotnet ef database update` 2. Verify existing rows unaffected 3. New columns are nullable/default |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
