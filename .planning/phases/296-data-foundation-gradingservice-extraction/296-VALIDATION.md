---
phase: 296
slug: data-foundation-gradingservice-extraction
status: validated
nyquist_compliant: partial
wave_0_complete: true
created: 2026-04-06
validated: 2026-04-06
---

# Phase 296 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build (no test project — single HcPortal.csproj) |
| **Config file** | none — no test project in repo |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd-verify-work`:** Build must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 296-01-01 | 01 | 1 | FOUND-01, FOUND-02 | — | N/A | build | `dotnet build` | ✅ | ✅ green |
| 296-01-02 | 01 | 1 | FOUND-05 | T-296-01 | Nullable columns, no breaking change | build | `dotnet build` | ✅ | ✅ green |
| 296-02-01 | 02 | 1 | FOUND-03, FOUND-04 | T-296-02, T-296-04 | Race condition guard, duplicate guard | build | `dotnet build` | ✅ | ✅ green |
| 296-02-02 | 02 | 2 | FOUND-06, FOUND-07, FOUND-08, FOUND-09 | — | N/A | build | `dotnet build` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| GradingService scoring logic calculates correct percentage | FOUND-03 | No test project — requires runtime with test data | 1. Create assessment session 2. Submit answers via CMP 3. Verify score matches expected percentage |
| Race condition guard prevents double-grading | FOUND-04 | Requires concurrent request simulation | 1. Submit exam from two tabs simultaneously 2. Verify only one grading executes (second returns race condition skip) |
| Migration applies without data loss on existing rows | FOUND-05 | Requires database with existing data | 1. Run `dotnet ef database update` 2. Verify existing rows unaffected 3. New columns are nullable/default |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter — partial (3 manual-only)

**Approval:** validated 2026-04-06

---

## Validation Audit 2026-04-06

| Metric | Count |
|--------|-------|
| Requirements audited | 9 |
| Covered (build) | 6 |
| Manual-only | 3 |
| Gaps resolved | 0 |
| Escalated | 0 |
