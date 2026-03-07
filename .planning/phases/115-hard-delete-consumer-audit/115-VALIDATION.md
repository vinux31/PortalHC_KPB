---
phase: 115
slug: hard-delete-consumer-audit
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-07
---

# Phase 115 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual browser verification of all 4 requirements
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 115-01-01 | 01 | 1 | DEL-01 | manual | Browser: verify delete button in view mode | N/A | pending |
| 115-01-02 | 01 | 1 | DEL-02 | manual | Browser: click Hapus, verify cascade counts in modal | N/A | pending |
| 115-01-03 | 01 | 1 | DEL-03 | manual | Browser: confirm delete, verify no FK error, table refreshes | N/A | pending |
| 115-01-04 | 01 | 1 | AUD-01 | manual | Browser: after delete, visit PlanIdp and CoachingProton | N/A | pending |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Delete button visible in view mode | DEL-01 | UI interaction | Navigate to ProtonData Silabus, select filters, verify Hapus button on each Kompetensi row |
| Confirmation modal shows cascade counts | DEL-02 | UI interaction | Click Hapus, verify modal lists SubKompetensi, Deliverable, Progress, Session counts |
| Full cascade delete succeeds | DEL-03 | DB state verification | Confirm delete in modal, verify table refreshes without errors |
| Consumer pages work after delete | AUD-01 | Cross-page verification | After deleting a Kompetensi, navigate to PlanIdp and CoachingProton pages, verify they load correctly |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
