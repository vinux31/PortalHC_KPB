---
phase: 165
slug: worker-to-hc-progress-push-polling-removal
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-13
---

# Phase 165 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser-based verification + `dotnet build` compile check |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual: two browser tabs (HC monitoring + worker exam), trigger each event |
| **Estimated runtime** | ~5 seconds (build), ~5 minutes (manual flow) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual two-browser test of each push scenario
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 165-01-01 | 01 | 1 | MONITOR-01 | manual | `dotnet build` (compile) | N/A | ⬜ pending |
| 165-01-02 | 01 | 1 | MONITOR-02, MONITOR-03 | manual | `dotnet build` (compile) | N/A | ⬜ pending |
| 165-02-01 | 02 | 2 | MONITOR-01 | manual | Two-browser progress push test | N/A | ⬜ pending |
| 165-02-02 | 02 | 2 | MONITOR-02, MONITOR-03 | manual | Two-browser start/submit test | N/A | ⬜ pending |
| 165-03-01 | 03 | 3 | CLEAN-01 | code inspection | `grep -n "setInterval" Views/CMP/StartExam.cshtml Views/Admin/AssessmentMonitoringDetail.cshtml` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No automated test framework needed — multi-user SignalR push events require live browser sessions.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Worker answers → HC row updates within 1-2s | MONITOR-01 | Requires two live SignalR sessions | Open HC monitoring + worker exam tabs, answer question, observe row flash + progress update |
| Worker submits → HC shows "Selesai" + score | MONITOR-02 | Requires two live SignalR sessions | Worker submits exam, observe HC row status change + score + toast |
| Worker starts → HC shows "Dalam Pengerjaan" | MONITOR-03 | Requires two live SignalR sessions | Worker opens exam, observe HC row status change + toast |
| No polling setInterval calls remain | CLEAN-01 | Code inspection sufficient | Grep for setInterval in both views, confirm only timer/auto-save remain |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
