---
phase: 252
slug: xss-escape-ajax-approval-badge
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-24
---

# Phase 252 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | UAT manual (browser) — tidak ada test runner otomatis untuk Razor views |
| **Config file** | none |
| **Quick run command** | Code review — grep untuk interpolasi tanpa `escHtml()` |
| **Full suite command** | Manual browser test |
| **Estimated runtime** | ~60 seconds (manual) |

---

## Sampling Rate

- **After every task commit:** Code review — cari `data.approverName` dan `data.reviewerName` dalam template literal tanpa `escHtml()`
- **After every plan wave:** Manual UAT — jalankan approval dengan nama berisi `<b>test</b>` dan verifikasi tooltip menampilkan teks literal
- **Before `/gsd:verify-work`:** Full UAT must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 252-01-01 | 01 | 1 | SEC-02 | code review | `grep -n "escHtml" Views/CDP/CoachingProton.cshtml` | ✅ | ⬜ pending |
| 252-01-02 | 01 | 1 | SEC-02 | code review | `grep -c "escHtml(data\." Views/CDP/CoachingProton.cshtml` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — hanya modifikasi JavaScript dalam file Razor yang sudah ada.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Karakter HTML di approverName muncul sebagai teks literal di tooltip | SEC-02 | Razor view — no automated test runner | 1. Approve coaching dengan user yang namanya berisi `<b>test</b>` 2. Lihat tooltip badge — harus tampil `<b>test</b>` sebagai teks, bukan bold |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
