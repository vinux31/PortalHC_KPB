---
phase: 191
slug: wizard-ui
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-17
---

# Phase 191 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework for Razor views) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet run` + manual browser checklist at `/Admin/CreateAssessment` |
| **Estimated runtime** | ~10 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` — zero compile errors
- **After every plan wave:** Full manual browser test checklist
- **Before `/gsd:verify-work`:** All FORM-01 manual test cases pass
- **Max feedback latency:** 10 seconds (build check)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 191-01-01 | 01 | 1 | FORM-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 191-01-02 | 01 | 1 | FORM-01 | manual browser | n/a | ❌ W0 | ⬜ pending |
| 191-01-03 | 01 | 1 | FORM-01 | manual browser | n/a | ❌ W0 | ⬜ pending |
| 191-01-04 | 01 | 1 | FORM-01 | manual browser | n/a | ❌ W0 | ⬜ pending |
| 191-01-05 | 01 | 1 | FORM-01 | manual browser | n/a | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] No automated test framework needed — manual browser testing per project convention
- [ ] `dotnet build` must pass (compile check only)

*Existing infrastructure covers build verification. UI behavior requires manual browser testing per project testing approach.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 4-step wizard navigation with per-step validation | FORM-01 | Client-side JS + DOM interaction | Navigate wizard, verify Next blocked without required fields |
| Back preserves state (selections intact) | FORM-01 | Browser DOM state | Select category in Step 1, advance to Step 2, click Back, verify category still selected |
| Multi-user selection intact after nav | FORM-01 | Browser DOM state | Select users in Step 2, go to Step 3, Back to Step 2, verify users still checked |
| Konfirmasi summary matches entered data | FORM-01 | DOM rendering | Fill all steps, verify Step 4 summary shows correct category, user count, settings |
| Submit calls existing POST unchanged | FORM-01 | Server-side behavior | Submit wizard, verify form posts to same action with same field names |
| ValidUntil saves to DB when set | FORM-01 | DB persistence | Set ValidUntil date, submit, check DB record |
| Proton mode: Step 2 shows coachees not users | FORM-01 | Conditional AJAX + DOM | Select Proton category, verify Step 2 loads coachees via AJAX |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
