---
phase: 267
slug: resilience-edge-cases
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-28
---

# Phase 267 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (Node.js) — same as Phase 265-266 |
| **Config file** | `package.json` (scripts) |
| **Quick run command** | `node uat-267-test.js` |
| **Full suite command** | `node uat-267-test.js` |
| **Estimated runtime** | ~180 seconds (includes 1-2 min timer wait) |

---

## Sampling Rate

- **After every task commit:** Run `node uat-267-test.js`
- **After every plan wave:** Run full suite
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 180 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 267-01-01 | 01 | 1 | EDGE-01 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |
| 267-01-02 | 01 | 1 | EDGE-02 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |
| 267-01-03 | 01 | 1 | EDGE-03 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |
| 267-01-04 | 01 | 1 | EDGE-04 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |
| 267-01-05 | 01 | 1 | EDGE-05 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |
| 267-01-06 | 01 | 1 | EDGE-06 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |
| 267-01-07 | 01 | 1 | EDGE-07 | e2e | `node uat-267-test.js` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `uat-267-test.js` — Playwright test script for all EDGE scenarios
- [ ] Assessment baru di server dev untuk Regan (durasi panjang) dan Arsyad (durasi 1-2 menit)

*Existing Playwright infrastructure from Phase 265 reusable.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Timer habis natural | EDGE-07 | Requires real 1-2 min wait | Buat assessment 1-2 menit, mulai, tunggu habis |
| Visual offline badge | EDGE-01 | Visual appearance check | Verify badge shows during network block |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 180s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
