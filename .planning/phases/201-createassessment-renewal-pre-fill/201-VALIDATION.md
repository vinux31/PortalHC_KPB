---
phase: 201
slug: createassessment-renewal-pre-fill
status: draft
nyquist_compliant: false
wave_0_complete: true
created: 2026-03-19
---

# Phase 201 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (project convention) |
| **Config file** | N/A |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual browser flow: GET pre-fill → edit → POST → DB check |
| **Estimated runtime** | ~60 seconds (manual) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (compilation check)
- **After every plan wave:** Manual browser verification of affected flows
- **Before `/gsd:verify-work`:** Full manual flow must pass
- **Max feedback latency:** 10 seconds (build), 60 seconds (manual)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 201-01-01 | 01 | 1 | RENEW-03 | manual | `dotnet build` + browser GET `/Admin/CreateAssessment?renewSessionId={id}` | N/A | ⬜ pending |
| 201-01-02 | 01 | 1 | RENEW-03 | manual | `dotnet build` + browser GET `/Admin/CreateAssessment?renewTrainingId={id}` | N/A | ⬜ pending |
| 201-01-03 | 01 | 1 | RENEW-03 | manual | `dotnet build` + browser POST → check DB RenewsSessionId | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework install needed — project uses manual browser testing convention.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pre-fill Title, Category, peserta from renewSessionId | RENEW-03 | UI form pre-fill requires browser | GET `/Admin/CreateAssessment?renewSessionId={valid_id}`, verify fields populated |
| Pre-fill Title only from renewTrainingId | RENEW-03 | UI form pre-fill requires browser | GET `/Admin/CreateAssessment?renewTrainingId={valid_id}`, verify Title only |
| GenerateCertificate auto-checked | RENEW-03 | Checkbox state requires browser | Open renewal mode, verify checkbox checked |
| ValidUntil required in renewal mode | RENEW-03 | Form validation requires browser | Submit without ValidUntil, expect validation error |
| POST saves RenewsSessionId/RenewsTrainingId | RENEW-03 | DB check after form submit | Create assessment via renewal, check DB record |
| Invalid query param redirect | RENEW-03 | Browser redirect behavior | GET with invalid ID, verify redirect + warning |
| Batalkan Renewal button | RENEW-03 | JavaScript interaction | Click cancel button, verify param removal |

---

## Validation Sign-Off

- [x] All tasks have manual verify instructions
- [x] Sampling continuity: build check after every commit
- [x] Wave 0 covers all MISSING references (none needed)
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
