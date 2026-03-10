---
phase: 148
slug: css-audit-cleanup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-10
---

# Phase 148 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Visual smoke test + grep verification |
| **Config file** | None — CSS changes don't require unit tests |
| **Quick run command** | `grep -cE "glass-card\|backdrop-filter: blur\|filter: blur\|\.timeline\|\.deadline-card\|data-aos" wwwroot/css/home.css Views/Home/Index.cshtml` |
| **Full suite command** | Run local dev server, verify CMP/CDP/Admin pages display correctly, Homepage loads without errors |
| **Estimated runtime** | ~5 seconds (grep), ~60 seconds (visual) |

---

## Sampling Rate

- **After every task commit:** Run quick grep command
- **After every plan wave:** Run full suite command
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 148-01-01 | 01 | 1 | CSS-01 | grep | `grep -cE "glass-card\|backdrop-filter: blur\|filter: blur" wwwroot/css/home.css` returns 0 | ✅ | ⬜ pending |
| 148-01-02 | 01 | 1 | CSS-02 | grep | `grep -cE "\.timeline\|\.deadline-card" wwwroot/css/home.css` returns 0 | ✅ | ⬜ pending |
| 148-01-03 | 01 | 1 | CSS-03 | grep | `grep -c "data-aos" Views/Home/Index.cshtml` returns 0 | ✅ | ⬜ pending |
| 148-01-04 | 01 | 1 | CSS-01,02,03 | visual | Dev server — CMP/CDP/Admin pages unaffected | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.* No test framework setup needed — grep verification and visual inspection suffice.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| CMP/CDP/Admin pages visually unaffected | CSS-01, CSS-02 | CSS visual regression needs human eye | Load each page, verify styling intact |
| Homepage loads without console errors | CSS-03 | Browser console check | Open DevTools, verify no JS errors from missing AOS attrs |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
