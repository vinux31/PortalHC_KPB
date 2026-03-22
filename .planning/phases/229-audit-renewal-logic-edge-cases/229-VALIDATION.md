---
phase: 229
slug: audit-renewal-logic-edge-cases
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-22
---

# Phase 229 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification + grep-based code audit |
| **Config file** | none — no automated test framework in project |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + manual verification
- **Before `/gsd:verify-work`:** Full build must succeed + manual UAT
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 229-01-01 | 01 | 1 | LDAT-01 | code audit + grep | `grep -n "RenewsTrainingId\|RenewsSessionId" Controllers/AdminController.cs` | ✅ | ⬜ pending |
| 229-01-02 | 01 | 1 | LDAT-02 | code audit + grep | `grep -rn "RenewalCount\|BuildRenewalRowsAsync" Controllers/` | ✅ | ⬜ pending |
| 229-01-03 | 01 | 1 | LDAT-03 | code audit | `grep -n "DeriveCertificateStatus" Models/CertificationManagementViewModel.cs` | ✅ | ⬜ pending |
| 229-01-04 | 01 | 1 | LDAT-04 | code audit + grep | `grep -n "GroupBy\|GroupKey\|Base64" Controllers/AdminController.cs` | ✅ | ⬜ pending |
| 229-01-05 | 01 | 1 | LDAT-05 | code audit + grep | `grep -n "MapKategori" Controllers/AdminController.cs Controllers/CDPController.cs` | ✅ | ⬜ pending |
| 229-02-01 | 02 | 2 | EDGE-01 | manual | Browser test: bulk renew mixed types | N/A | ⬜ pending |
| 229-02-02 | 02 | 2 | EDGE-02 | code audit + grep | `grep -n "IsRenewed\|already.*renew" Controllers/AdminController.cs` | ✅ | ⬜ pending |
| 229-02-03 | 02 | 2 | EDGE-03 | manual | Browser test: empty renewal list | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework setup needed — this is a code audit phase verified via grep + manual browser testing.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| FK chain set correctly on renew | LDAT-01 | Requires creating renewal via UI | Create renewal for each of 4 FK combinations, verify DB records |
| Badge count sync | LDAT-02 | Requires visual verification | Check Admin/Index badge matches renewal list count |
| Bulk mixed-type rejection | EDGE-01 | Requires UI interaction | Select mixed Assessment+Training certs, attempt bulk renew, verify error |
| Double renewal prevention | EDGE-02 | Requires UI interaction | Renew a cert, attempt to renew same cert again, verify blocked |
| Empty state display | EDGE-03 | Requires visual verification | Ensure all certs are active/renewed, check empty state message |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
