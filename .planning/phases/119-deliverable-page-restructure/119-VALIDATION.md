---
phase: 119
slug: deliverable-page-restructure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-08
---

# Phase 119 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC Razor views) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual UAT in browser |
| **Estimated runtime** | ~10 seconds (build), manual UAT ~5 min |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual browser check across roles (Coachee, Coach, SrSpv, SH, HC)
- **Before `/gsd:verify-work`:** Full UAT with all status states (Pending, Submitted, Approved, Rejected)
- **Max feedback latency:** 10 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 119-01-01 | 01 | 1 | PAGE-01 | manual | `dotnet build` | N/A | ⬜ pending |
| 119-01-02 | 01 | 1 | PAGE-02 | manual | `dotnet build` | N/A | ⬜ pending |
| 119-01-03 | 01 | 1 | PAGE-03 | manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This is a view-only change — `dotnet build` confirms compilation.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 4 distinct card sections visible | PAGE-01 | Razor view layout — visual check | Navigate to Deliverable detail, verify 4 cards render |
| Riwayat Status timeline from history | PAGE-02 | Visual timeline rendering | Check chronological entries from DeliverableStatusHistory |
| Evidence Coach shows coaching data + download | PAGE-03 | File download + visual | Verify Catatan Coach, Kesimpulan, Result fields and download button |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
