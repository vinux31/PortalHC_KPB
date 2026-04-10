---
phase: 303
slug: rasio-coach-coachee-dan-balanced-mapping
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-10
---

# Phase 303 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET Core MVC — no automated test framework configured) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd-verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 303-01-01 | 01 | 1 | — | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework installation needed — project uses manual browser verification with `dotnet build` as compilation gate.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Bar chart renders with correct colors | D-04 | Visual rendering — no headless browser configured | Navigate to Coach Workload page, verify bars show green/yellow/red |
| Summary cards show correct counts | D-05 | UI verification | Compare card values with database counts |
| Saran reassign generates correctly | D-11 | Algorithm output | Create imbalanced coach loads, verify suggestions appear |
| Role-based access (Admin vs HC) | D-17/D-18 | Auth flow | Login as HC, verify read-only; login as Admin, verify full access |
| Excel export contains correct data | D-08 | File download | Click Export, open Excel, verify data matches displayed table |
| Threshold modal saves to DB | D-15/D-16 | E2E flow | Set threshold, reload page, verify persisted |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
