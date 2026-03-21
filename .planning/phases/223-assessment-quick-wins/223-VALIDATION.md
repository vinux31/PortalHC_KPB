---
phase: 223
slug: assessment-quick-wins
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 223 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | No automated test framework (manual browser + DB query) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Build green + manual browser verification
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 223-01-01 | 01 | 1 | AINT-01 | manual (DB query) | `dotnet build` | N/A | ⬜ pending |
| 223-01-02 | 01 | 1 | AINT-04 | manual (DB query) | `dotnet build` | N/A | ⬜ pending |
| 223-01-03 | 01 | 1 | CLEN-01 | manual (browser) | `dotnet build` | N/A | ⬜ pending |
| 223-01-04 | 01 | 1 | CLEN-05 | code review | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework installation needed — project uses manual browser testing and DB query verification pattern.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SessionElemenTeknisScore rows inserted after SubmitExam | AINT-01 | Requires full exam flow in browser | 1. Start exam session 2. Answer questions 3. Submit 4. Query `SELECT * FROM SessionElemenTeknisScores WHERE SessionId = @id` |
| UserResponse.SubmittedAt populated after SaveLegacyAnswer | AINT-04 | Requires legacy exam answer submission | 1. Start legacy exam 2. Answer question 3. Query `SELECT SubmittedAt FROM UserResponses WHERE ...` |
| "Wait Certificate" status removed from UI | CLEN-01 | Visual verification in browser | 1. Open Records views 2. Verify no "Wait Certificate" badge/option |
| AccessToken documentation comment present | CLEN-05 | Code review only | 1. Open Models/AssessmentSession.cs 2. Verify XML doc comment on AccessToken property |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
