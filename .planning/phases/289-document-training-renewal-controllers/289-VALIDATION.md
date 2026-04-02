---
phase: 289
slug: document-training-renewal-controllers
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 289 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual URL verification |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build --no-restore` |
| **Full suite command** | `dotnet build --no-restore && echo "BUILD OK"` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --no-restore`
- **After every plan wave:** Run `dotnet build --no-restore`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 289-01-01 | 01 | 1 | DOC-01 | build | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 289-01-02 | 01 | 1 | DOC-02 | build | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 289-01-03 | 01 | 1 | TRN-01 | build | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 289-01-04 | 01 | 1 | TRN-02 | build | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 289-01-05 | 01 | 1 | RNW-01 | build | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 289-01-06 | 01 | 1 | RNW-02 | build | `dotnet build --no-restore` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| URL /Admin/KkjMatrix accessible | DOC-01 | Route requires running server | Navigate to URL, verify page loads |
| URL /Admin/CpdpFiles accessible | DOC-02 | Route requires running server | Navigate to URL, verify page loads |
| URL /Admin/AddTraining accessible | TRN-01 | Route requires running server | Navigate to URL, verify page loads |
| URL /Admin/ImportTraining accessible | TRN-02 | Route requires running server | Navigate to URL, verify page loads |
| URL /Admin/RenewalCertificate accessible | RNW-01 | Route requires running server | Navigate to URL, verify page loads |
| Authorization roles unchanged | RNW-02 | Requires authenticated session | Login as different roles, verify access |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
