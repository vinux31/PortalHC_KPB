---
phase: 149
slug: homepage-view-controller-redesign
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-10
---

# Phase 149 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UI testing + grep verification |
| **Config file** | None — structural refactoring, not feature addition |
| **Quick run command** | `grep -rn "IdpTotalCount\|RecentActivities\|UpcomingDeadlines" Views/Home/Index.cshtml Controllers/HomeController.cs Models/DashboardHomeViewModel.cs` (should return 0) |
| **Full suite command** | Run dev server, navigate to Home/Index, verify: hero displays, Quick Access cards render, no console errors |
| **Estimated runtime** | ~5 seconds (grep), ~30 seconds (visual) |

---

## Sampling Rate

- **After every task commit:** Run quick grep command
- **After every plan wave:** Run full suite (visual inspection)
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 149-01-01 | 01 | 1 | HOME-01 | grep | `grep -c "glass-card\|IdpTotalCount\|PendingAssessmentCount" Views/Home/Index.cshtml` (expect 0) | ✅ | ⬜ pending |
| 149-01-02 | 01 | 1 | HOME-02 | grep | `grep -c "RecentActivities\|timeline-item" Views/Home/Index.cshtml` (expect 0) | ✅ | ⬜ pending |
| 149-01-03 | 01 | 1 | HOME-03 | grep | `grep -c "UpcomingDeadlines\|deadline-card" Views/Home/Index.cshtml` (expect 0) | ✅ | ⬜ pending |
| 149-01-04 | 01 | 1 | HOME-04 | grep | `grep -c "GetRecentActivities\|GetUpcomingDeadlines\|GetMandatoryTrainingStatus" Controllers/HomeController.cs` (expect 0) | ✅ | ⬜ pending |
| 149-01-05 | 01 | 1 | HERO-01 | grep+visual | `grep -c "hero-section::before\|hero-section::after" wwwroot/css/home.css` (expect 0) | ✅ | ⬜ pending |
| 149-01-06 | 01 | 1 | HERO-02 | visual | Load Home/Index, verify hero shows greeting, name, position, unit, date | N/A | ⬜ pending |
| 149-01-07 | 01 | 1 | QUICK-01 | grep+visual | Cards use `card border-0 shadow-sm` classes, no gradient backgrounds | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements. No new test framework needed — this phase is pure refactoring/cleanup verified by grep and visual inspection.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Hero displays greeting, name, position, unit, date | HERO-02 | Visual UI layout check | Load Home/Index, verify hero shows "Selamat [time], [Name]" + position + unit + date |
| Quick Access cards match CMP/CDP styling | QUICK-01 | Visual consistency check | Compare Quick Access cards with CMP/CDP Index cards for consistent Bootstrap styling |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
