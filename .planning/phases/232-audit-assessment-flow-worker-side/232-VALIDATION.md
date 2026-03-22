---
phase: 232
slug: audit-assessment-flow-worker-side
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 232 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test suite) |
| **Config file** | None |
| **Quick run command** | Manual: buka halaman di browser, ikuti flow |
| **Full suite command** | Manual: all flows end-to-end |
| **Estimated runtime** | ~15 minutes per full manual pass |

---

## Sampling Rate

- **After every task commit:** Manual spot-check affected flow
- **After every plan wave:** Full manual walkthrough of all affected flows
- **Before `/gsd:verify-work`:** Full suite must pass manual verification
- **Max feedback latency:** N/A (manual testing)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 232-01-01 | 01 | 1 | AFLW-01 | manual | — | ❌ manual | ⬜ pending |
| 232-01-02 | 01 | 1 | AFLW-02 | manual | — | ❌ manual | ⬜ pending |
| 232-01-03 | 01 | 1 | AFLW-02 | manual | — | ❌ manual | ⬜ pending |
| 232-02-01 | 02 | 1 | AFLW-03 | manual | — | ❌ manual | ⬜ pending |
| 232-02-02 | 02 | 1 | AFLW-04 | manual | — | ❌ manual | ⬜ pending |
| 232-02-03 | 02 | 1 | AFLW-05 | manual | — | ❌ manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No automated test framework — project uses manual browser testing pattern.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Assessment list filter & display | AFLW-01 | UI rendering + data filtering | Login as worker, verify assessment list shows correct items per status |
| Token entry + exam start | AFLW-02 | User interaction flow | Enter valid/invalid token, verify UX responses |
| Timer accuracy + auto-save | AFLW-02 | Real-time behavior | Start exam, verify timer counts down, click answers verify auto-save |
| Submit + scoring chain | AFLW-03 | End-to-end data flow | Complete exam, verify score, IsPassed, NomorSertifikat |
| Session resume | AFLW-04 | State restoration | Close browser mid-exam, re-enter token, verify state restored |
| Results page + review | AFLW-05 | Visual rendering | View results, verify answer review display |
| SignalR worker notifications | AFLW-02 | Real-time cross-session | HC resets session while worker on exam page, verify modal + redirect |

---

## Validation Sign-Off

- [ ] All tasks have manual verification steps documented
- [ ] Sampling continuity: manual check after each task
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency: manual (acceptable for audit phase)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
