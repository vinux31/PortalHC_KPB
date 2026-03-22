---
phase: 228
slug: best-practices-research
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-22
---

# Phase 228 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual verification (research/documentation phase — no code tests) |
| **Config file** | none |
| **Quick run command** | `ls docs/research-*.html` |
| **Full suite command** | `ls docs/research-*.html | wc -l` (expect 5) |
| **Estimated runtime** | ~1 second |

---

## Sampling Rate

- **After every task commit:** Verify HTML file exists and is valid
- **After every plan wave:** Check all expected HTML files present
- **Before `/gsd:verify-work`:** All 5 HTML documents present with required sections
- **Max feedback latency:** 1 second

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 228-01-01 | 01 | 1 | RSCH-01 | file check | `test -f docs/research-renewal-best-practices.html` | ❌ W0 | ⬜ pending |
| 228-02-01 | 02 | 1 | RSCH-02 | file check | `test -f docs/research-assessment-best-practices.html` | ❌ W0 | ⬜ pending |
| 228-03-01 | 03 | 1 | RSCH-03 | file check | `test -f docs/research-monitoring-best-practices.html` | ❌ W0 | ⬜ pending |
| 228-04-01 | 04 | 1 | RSCH-04 | file check | `test -f docs/research-comparison-summary.html` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements — no test framework needed for documentation phase.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Renewal comparison covers 3+ platforms | RSCH-01 | Content quality check | Read HTML, verify Coursera, LinkedIn Learning, and 1+ HR portal compared |
| Assessment comparison covers 3+ platforms | RSCH-02 | Content quality check | Read HTML, verify Moodle, Google Forms Quiz, Examly compared |
| Monitoring UX patterns documented | RSCH-03 | Content quality check | Read HTML, verify concrete UX pattern descriptions present |
| Recommendations mapped to phases 229-232 | RSCH-04 | Content quality check | Read HTML, verify each recommendation tagged with target phase |
| 3-tier ranking applied | RSCH-04 | Content quality check | Read HTML, verify Must-fix / Should-improve / Nice-to-have tiers |

---

## Validation Sign-Off

- [x] All tasks have file existence verification
- [x] Sampling continuity: file checks after each commit
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 1s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
