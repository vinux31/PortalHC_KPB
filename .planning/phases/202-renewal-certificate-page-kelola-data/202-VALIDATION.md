---
phase: 202
slug: renewal-certificate-page-kelola-data
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-19
---

# Phase 202 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (project tidak punya automated test suite) |
| **Config file** | none |
| **Quick run command** | `dotnet run`, navigate to `/Admin/RenewalCertificate` |
| **Full suite command** | Manual flow testing per requirement |
| **Estimated runtime** | ~120 seconds (manual) |

---

## Sampling Rate

- **After every task commit:** Run app, verify changed page visually
- **After every plan wave:** Full manual flow testing all requirements
- **Before `/gsd:verify-work`:** All 5 RNPAGE requirements verified manually
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 202-01-01 | 01 | 1 | RNPAGE-01 | manual | Navigate `/Admin/RenewalCertificate`, verify only Expired+AkanExpired shown | N/A | ⬜ pending |
| 202-01-02 | 01 | 1 | RNPAGE-02 | manual | Select filters → table updates via AJAX | N/A | ⬜ pending |
| 202-01-03 | 01 | 1 | RNPAGE-03 | manual | Click Renew → CreateAssessment pre-filled | N/A | ⬜ pending |
| 202-02-01 | 02 | 1 | RNPAGE-04 | manual | Checkbox bulk select, verify kategori constraint | N/A | ⬜ pending |
| 202-02-02 | 02 | 1 | RNPAGE-05 | manual | Navigate Kelola Data, verify card + badge | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements. No automated test framework to install.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Daftar hanya Expired+AkanExpired belum di-renew | RNPAGE-01 | UI rendering, data filtering | Buka `/Admin/RenewalCertificate`, verify no Aktif/Permanent rows |
| Filter AJAX update | RNPAGE-02 | Browser interaction | Select Bagian → table refreshes without reload |
| Single Renew → CreateAssessment | RNPAGE-03 | Cross-page redirect + pre-fill | Click Renew, verify Title/Kategori/peserta pre-filled |
| Bulk select kategori constraint | RNPAGE-04 | JS checkbox interaction | Check 1 row, verify different-category checkboxes disabled |
| Card badge count | RNPAGE-05 | UI rendering | Navigate Kelola Data, verify card visible with count |

---

## Validation Sign-Off

- [ ] All tasks have manual verify instructions
- [ ] Sampling continuity: verified after each wave
- [ ] No automated test framework needed (manual project)
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
