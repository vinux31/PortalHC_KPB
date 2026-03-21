---
phase: 212
slug: tipe-filter-renewal-flow-addtraining-renewal
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-21
---

# Phase 212 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT (ASP.NET MVC — no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser verification |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Build + manual browser verification
- **Before `/gsd:verify-work`:** Full UAT pass
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 212-01-01 | 01 | 1 | ENH-01 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 212-01-02 | 01 | 1 | ENH-02 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 212-01-03 | 01 | 1 | ENH-03 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 212-01-04 | 01 | 1 | ENH-04, FIX-04 | build + manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Tipe filter dropdown filters table correctly | ENH-01 | Browser UI interaction | Select "Assessment" → only assessment rows visible; select "Training" → only training rows; "Semua" → all |
| Renewal popup modal appears for all types | ENH-02 | Browser UI interaction | Click Renew on any row → modal with 2 options appears |
| Bulk renew blocks mixed types | ENH-03 | Browser UI interaction | Select mix of Assessment+Training → click bulk renew → modal shows error, button disabled |
| AddTraining renewal mode prefills correctly | ENH-04, FIX-04 | Browser UI interaction | Open AddTraining via renewal link → Title, Category, Peserta prefilled, banner visible, FK saved on submit |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
