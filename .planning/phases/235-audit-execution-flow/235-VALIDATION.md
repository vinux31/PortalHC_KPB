---
phase: 235
slug: audit-execution-flow
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 235 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual server-side verification (ASP.NET Core — no unit test framework in project) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 235-01-01 | 01 | 1 | EXEC-01 | grep | `grep -n "RecordStatusHistory" Controllers/CDPController.cs` | ✅ | ⬜ pending |
| 235-01-02 | 01 | 1 | EXEC-01 | grep | `grep -n "EvidencePath" Controllers/CDPController.cs` | ✅ | ⬜ pending |
| 235-02-01 | 02 | 1 | EXEC-02 | grep | `grep -n "TransitionStatus" Controllers/CDPController.cs` | ❌ W0 | ⬜ pending |
| 235-03-01 | 03 | 1 | EXEC-03 | grep | `grep -n "StatusHistory.*Pending" Controllers/AdminController.cs` | ❌ W0 | ⬜ pending |
| 235-04-01 | 04 | 2 | EXEC-04 | grep | `grep -n "CreateHCNotificationAsync\|NotifyAsync" Controllers/CDPController.cs` | ✅ | ⬜ pending |
| 235-05-01 | 05 | 2 | EXEC-05 | grep | `grep -n "IsInRole.*Admin" Views/CDP/PlanIdp.cshtml` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers all phase requirements — this is an audit/fix phase, verification via grep and build.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Concurrent approve race condition | EXEC-02 | Requires two simultaneous HTTP requests | Open two browser tabs, click approve at same time |
| Evidence resubmit preserves old files | EXEC-01 | File system interaction | Submit evidence, get rejected, resubmit — verify old file still exists |
| Notification received by correct user | EXEC-04 | Requires checking notification bell UI | Submit evidence as coachee, check coach notification bell |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
