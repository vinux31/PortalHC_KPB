---
phase: 282
slug: maintenance-mode
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-01
---

# Phase 282 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC — no automated test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + manual browser check
- **Before `/gsd:verify-work`:** Full build must succeed + manual UAT
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 282-01-01 | 01 | 1 | MAINT-01 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 282-01-02 | 01 | 1 | MAINT-02 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 282-01-03 | 01 | 1 | MAINT-03 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 282-01-04 | 01 | 1 | MAINT-04 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 282-01-05 | 01 | 1 | MAINT-05 | build + manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework installation needed — project uses manual browser UAT.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Admin toggle maintenance on/off | MAINT-01 | UI interaction | Login as Admin, go to /Admin/Maintenance, toggle on, verify status changes |
| Non-admin redirect to maintenance page | MAINT-02 | Browser redirect | Login as User, access any page, verify redirected to maintenance page |
| Admin+HC bypass | MAINT-03 | Role-based access | Login as HC, access pages while maintenance active, verify access works |
| Custom message + estimated time shown | MAINT-04 | Visual check | Check maintenance page shows admin's custom message and estimated time |
| Active session redirect | MAINT-05 | Session behavior | Stay logged in as User, admin activates maintenance, User refreshes, verify redirect |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
