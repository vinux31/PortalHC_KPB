---
phase: 146
slug: excel-import-update
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-10
---

# Phase 146 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (no automated test suite in project) |
| **Config file** | none |
| **Quick run command** | N/A — manual verification |
| **Full suite command** | N/A — manual verification |
| **Estimated runtime** | ~5 minutes manual testing |

---

## Sampling Rate

- **After every task commit:** Manual browser verification
- **After every plan wave:** Full import flow test (both file and paste, both 6-col and 7-col)
- **Before `/gsd:verify-work`:** All 4 success criteria verified manually
- **Max feedback latency:** N/A — manual testing

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 146-01-01 | 01 | 1 | SUBTAG-01 | manual | Download template, open in Excel, verify column G header | N/A | ⬜ pending |
| 146-01-02 | 01 | 1 | SUBTAG-03 | manual | Import 7-col file, verify SubCompetency saved with Title Case | N/A | ⬜ pending |
| 146-01-03 | 01 | 1 | SUBTAG-03 | manual | Import 6-col file (old template), verify no errors, SubCompetency = NULL | N/A | ⬜ pending |
| 146-01-04 | 01 | 1 | SUBTAG-03 | manual | Import mixed casing ("instrumentasi", "INSTRUMENTASI"), verify normalized to same form | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No automated test framework in project — manual testing is the established pattern.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Template column G header | SUBTAG-01 | Excel file download verification | Download template, open in Excel, check column G = "Sub Kompetensi" |
| Import with Sub Kompetensi | SUBTAG-03 | End-to-end browser flow | Upload 7-col Excel, verify DB has normalized SubCompetency |
| Backward compatibility | SUBTAG-03 | End-to-end browser flow | Upload 6-col Excel, verify import succeeds, SubCompetency = NULL |
| Casing normalization | SUBTAG-03 | Data normalization verification | Import "instrumentasi" and "INSTRUMENTASI", verify both stored as "Instrumentasi" |
| Paste-text 7-col support | SUBTAG-03 | Browser paste flow | Paste 7-col tab-separated text, verify SubCompetency parsed |
| Cross-package warning | SUBTAG-03 | Multi-package flow | Import different SubCompetency sets across sibling packages, verify warning shown |

---

## Validation Sign-Off

- [ ] All tasks have manual verification instructions
- [ ] Sampling continuity: every task verified after commit
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency acceptable for manual testing
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
