---
phase: 227
slug: major-refactors
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 227 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual SQL verification + dotnet build |
| **Config file** | none — no automated test framework in project |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet ef migrations list` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet ef migrations list`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 227-01-01 | 01 | 1 | CLEN-04 | manual SQL | `SELECT COUNT(*) FROM AssessmentSessions WHERE NomorSertifikat IS NOT NULL AND IsPassed != 1` | N/A | ⬜ pending |
| 227-01-02 | 01 | 1 | CLEN-03 | migration | `dotnet ef migrations list` | N/A | ⬜ pending |
| 227-02-01 | 02 | 2 | CLEN-02 | manual SQL | `SELECT COUNT(*) FROM AssessmentQuestions` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework setup needed — verification is via SQL queries and build checks.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Legacy sessions migrated to package format | CLEN-02 | Data migration requires SQL verification | Run pre/post migration row counts |
| NomorSertifikat only on passed sessions | CLEN-04 | Business logic check | Query sessions where IsPassed=false AND NomorSertifikat IS NOT NULL — should be 0 |
| Orphan tables removed | CLEN-03 | Schema change | Verify tables not in `dotnet ef dbcontext info` output |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
