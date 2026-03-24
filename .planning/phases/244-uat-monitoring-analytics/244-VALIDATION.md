---
phase: 244
slug: uat-monitoring-analytics
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-24
---

# Phase 244 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser UAT (no automated test framework) |
| **Config file** | none — UAT phase |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must succeed
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 244-01-01 | 01 | 1 | MON-01 | manual | Browser: dual-browser SignalR test | N/A | ⬜ pending |
| 244-02-01 | 02 | 1 | MON-02 | manual | Browser: token mgmt linear flow | N/A | ⬜ pending |
| 244-03-01 | 03 | 2 | MON-03 | manual | Browser: export + open Excel | N/A | ⬜ pending |
| 244-04-01 | 04 | 2 | MON-04 | manual | Browser: analytics cascading filter | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — this is a manual UAT phase.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SignalR real-time update | MON-01 | Requires dual browser + live WebSocket | HC opens MonitoringDetail, worker takes exam in separate browser, verify live stat card updates |
| Token management flow | MON-02 | Sequential UI actions with state verification | Copy token → regenerate → verify old invalid → force close → reset → re-exam |
| Excel export | MON-03 | File download + content verification | Export results → open file → verify structure and data |
| Analytics cascading filter | MON-04 | Interactive filter UI + chart rendering | Test all filter combinations, verify chart data updates |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: UAT phase — all manual
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
