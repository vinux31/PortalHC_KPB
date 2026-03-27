---
phase: 266
slug: review-submit-hasil
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-27
---

# Phase 266 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (Node.js) — sudah digunakan di Phase 265 |
| **Config file** | none — script standalone |
| **Quick run command** | `node uat-266-test.js` |
| **Full suite command** | Manual browser verification + DB query |
| **Estimated runtime** | ~120 seconds |

---

## Sampling Rate

- **After every task commit:** Run `node uat-266-test.js`
- **After every plan wave:** Manual browser verification + DB query
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 266-01-01 | 01 | 1 | SUBMIT-01 | Browser + visual | Playwright screenshot ExamSummary | ❌ W0 | ⬜ pending |
| 266-01-01 | 01 | 1 | SUBMIT-02 | Browser + visual | Playwright check alert-warning | ❌ W0 | ⬜ pending |
| 266-01-01 | 01 | 1 | SUBMIT-03 | DB query | SQL cross-check skor | Manual | ⬜ pending |
| 266-01-01 | 01 | 1 | RESULT-01 | Browser + visual | Playwright check badge pass/fail | ❌ W0 | ⬜ pending |
| 266-01-01 | 01 | 1 | RESULT-02 | Browser + visual | Playwright check list-group | ❌ W0 | ⬜ pending |
| 266-01-01 | 01 | 1 | RESULT-03 | Browser + visual | Playwright check ET table | ❌ W0 | ⬜ pending |
| 266-01-02 | 01 | 1 | CERT-01 | Browser + file | Playwright download check | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `uat-266-test.js` — Playwright script baru untuk phase 266 (review, submit, results, certificate)
- [ ] SQL queries untuk verifikasi grading dan ET scores (embedded di script atau manual)

*Referensi: `uat-265-test.js` di root project*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Grading accuracy | SUBMIT-03 | Butuh SQL query cross-check vs UI | Query AssessmentSessions + UserResponses, bandingkan skor |
| PDF download content | CERT-01 | File PDF perlu visual inspection | Download PDF, buka, verify konten sertifikat |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
