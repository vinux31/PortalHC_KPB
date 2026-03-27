---
phase: 265
slug: worker-exam-flow
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-27
---

# Phase 265 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT (browser-based) |
| **Config file** | N/A — UAT scenarios in PLAN.md |
| **Quick run command** | Browser test di server dev (http://10.55.3.3/KPB-PortalHC/) |
| **Full suite command** | All UAT scenarios per worker |
| **Estimated runtime** | ~30 minutes (3 workers × ~10 min each) |

---

## Sampling Rate

- **After every task commit:** Visual check di browser
- **After every plan wave:** Run all scenarios for that wave's worker
- **Before `/gsd:verify-work`:** All 8 EXAM requirements must PASS
- **Max feedback latency:** Immediate (manual observation)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 265-01-01 | 01 | 1 | EXAM-01 | manual | Browser: /CMP/Assessment | N/A | ⬜ pending |
| 265-01-02 | 01 | 1 | EXAM-02 | manual | Browser: token modal | N/A | ⬜ pending |
| 265-01-03 | 01 | 1 | EXAM-03 | manual | Browser: /CMP/StartExam/{id} | N/A | ⬜ pending |
| 265-01-04 | 01 | 1 | EXAM-04 | manual | Browser: observe timer | N/A | ⬜ pending |
| 265-01-05 | 01 | 1 | EXAM-05 | manual | Browser + DB query | N/A | ⬜ pending |
| 265-01-06 | 01 | 1 | EXAM-06 | manual | Browser: next/prev/jump | N/A | ⬜ pending |
| 265-01-07 | 01 | 1 | EXAM-07 | manual | Browser: observe badges | N/A | ⬜ pending |
| 265-02-01 | 02 | 1 | EXAM-08 | manual | Browser + DB query | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This is a manual UAT phase — no automated test setup needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Assessment list display | EXAM-01 | UI visual check | Login as worker, navigate to /CMP/Assessment, verify cards |
| Token verification | EXAM-02 | Interactive modal | Enter 6-digit token, verify redirect |
| Exam start & questions | EXAM-03 | UI rendering | Verify questions display with radio buttons |
| Timer accuracy | EXAM-04 | Real-time observation | Watch timer countdown, compare with wall clock |
| Auto-save | EXAM-05 | Browser + DB | Select answer, verify DB has record |
| Page navigation | EXAM-06 | Interactive UI | Click next/prev/jump, verify page switches |
| Network indicators | EXAM-07 | Visual check | Observe #hubStatusBadge and #networkStatusBadge |
| Abandon exam | EXAM-08 | Interactive + DB | Click abandon, verify status in DB, verify re-entry blocked |

---

## Validation Sign-Off

- [ ] All tasks have manual verification steps
- [ ] Sampling continuity: every scenario has visual + DB verification
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < immediate
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
