---
phase: 262
slug: fix-hardcoded-urls-in-views-for-sub-path-deployment-compatibility
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-27
---

# Phase 262 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET Core MVC — no JS test framework) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual spot check
- **Before `/gsd:verify-work`:** Full build must pass
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 262-01-01 | 01 | 1 | D-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 262-01-02 | 01 | 1 | D-04 | build | `dotnet build` | ✅ | ⬜ pending |
| 262-02-01 | 02 | 1 | D-03, D-05 | build+grep | `dotnet build && grep -r "fetch('/" Views/` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — verification is build success + grep for remaining hardcoded URLs.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| All fetch/ajax calls work under /KPB-PortalHC/ | D-05 | Requires running server with PathBase | Deploy to dev server, test NotificationBell, CoachCoacheeMapping, ProtonData pages |
| Static assets (images) load correctly | D-03 | Visual verification | Check /images/psign-pertamina.png loads on Certificate and ManageCategories pages |
| Tag helper URLs resolve correctly | D-02 | Requires server with PathBase | Click navigation links, verify URLs include /KPB-PortalHC/ prefix |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
