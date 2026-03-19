---
phase: 203
slug: certificate-history-modal
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-19
---

# Phase 203 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (project pattern) |
| **Config file** | none — no automated test suite |
| **Quick run command** | `dotnet run` + browser |
| **Full suite command** | `dotnet build` (compile check only) |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` to verify compilation
- **After every plan wave:** Manual browser verification of affected pages
- **Before `/gsd:verify-work`:** Full manual UAT per requirements
- **Max feedback latency:** 15 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 203-01-01 | 01 | 1 | HIST-01 | compile | `dotnet build` | ✅ | ⬜ pending |
| 203-01-02 | 01 | 1 | HIST-01 | compile | `dotnet build` | ✅ | ⬜ pending |
| 203-01-03 | 01 | 1 | HIST-01 | compile | `dotnet build` | ✅ | ⬜ pending |
| 203-02-01 | 02 | 1 | HIST-02 | manual | Browser: Renewal page history icon | ❌ W0 | ⬜ pending |
| 203-02-02 | 02 | 1 | HIST-03 | manual | Browser: CDP CertMgmt name click | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers all phase requirements (ASP.NET Core MVC + Bootstrap already in project)
- `WorkerId` field addition to `SertifikatRow` is a prerequisite task, not a framework gap

*No new framework installation needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Modal displays grouped certificates by renewal chain, newest first | HIST-01 | UI visual verification, no automated test suite | Open Renewal page → click history icon → verify grouping order |
| Renewal mode shows Renew button only on expired/akan expired not-yet-renewed | HIST-02 | Requires seeded data with mixed statuses | Click history on worker with expired cert → verify Renew button presence |
| CDP CertificationManagement name click opens readonly modal (no Renew button) | HIST-03 | Cross-page navigation + role context | Login as L4+ → CDP CertMgmt → click worker name → verify no action buttons |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
