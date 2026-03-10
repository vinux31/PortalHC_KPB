---
phase: 147
slug: scoring-results-ui
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-10
---

# Phase 147 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | No automated test framework in project |
| **Config file** | none |
| **Quick run command** | N/A — manual browser testing |
| **Full suite command** | N/A — manual browser testing |
| **Estimated runtime** | ~2 minutes per manual check |

---

## Sampling Rate

- **After every task commit:** Manual browser check of Results page
- **After every plan wave:** Full manual verification of all requirements
- **Before `/gsd:verify-work`:** All manual checks must pass
- **Max feedback latency:** N/A (manual)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 147-01-01 | 01 | 1 | ANAL-01 | manual | Browser: submit exam, check sub-competency scores | N/A | ⬜ pending |
| 147-01-02 | 01 | 1 | ANAL-02 | manual | Browser: verify radar chart renders with correct axes | N/A | ⬜ pending |
| 147-01-03 | 01 | 1 | ANAL-03 | manual | Browser: verify summary table columns and values | N/A | ⬜ pending |
| 147-01-04 | 01 | 1 | ANAL-04 | manual | Browser: view legacy assessment result, verify section hidden | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — project uses manual browser testing (established pattern).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| GroupBy scoring produces correct percentages | ANAL-01 | No test framework; UI-dependent | Submit exam with tagged questions, verify per-sub-competency scores on Results page |
| Radar chart renders with correct axes | ANAL-02 | Visual rendering verification | Check Chart.js radar chart appears with one axis per sub-competency, 0-100% scale |
| Summary table shows correct data | ANAL-03 | UI data display verification | Verify table columns: Sub Kompetensi, Benar, Total, Persentase with correct values |
| Section hidden for legacy assessments | ANAL-04 | Requires legacy data scenario | View results for assessment with no SubCompetency data, verify no analysis section |
| Radar hidden for <3 sub-competencies | ANAL-04 | Edge case UI behavior | View results with 1-2 sub-competencies, verify table only (no radar) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency acceptable
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
