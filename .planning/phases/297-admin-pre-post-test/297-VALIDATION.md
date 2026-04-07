---
phase: 297
slug: admin-pre-post-test
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-07
---

# Phase 297 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | ASP.NET Core build + manual verification |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 297-01-01 | 01 | 1 | PPT-01 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-01-02 | 01 | 1 | PPT-02 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-01-03 | 01 | 1 | PPT-03 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-02-01 | 02 | 1 | PPT-04 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-02-02 | 02 | 1 | PPT-05 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-03-01 | 03 | 2 | PPT-06, PPT-07 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-04-01 | 04 | 2 | PPT-08, PPT-09 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 297-05-01 | 05 | 3 | PPT-10 | — | Cascade reset no orphan | manual | N/A | — | ⬜ pending |
| 297-06-01 | 06 | 3 | PPT-11 | — | Delete no orphan | manual | N/A | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pre-Post cascade reset | PPT-10 | Requires DB state with linked sessions | Create Pre-Post assessment, complete Pre, reset Pre, verify Post also reset |
| Pre-Post group delete | PPT-11 | Requires DB state with linked sessions | Create Pre-Post assessment, delete group, verify both sessions deleted |
| Certificate from Post only | PPT-08, PPT-09 | Requires grading pipeline execution | Complete Pre-Post, verify certificate references Post score only |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
