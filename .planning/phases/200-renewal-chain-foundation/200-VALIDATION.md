---
phase: 200
slug: renewal-chain-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-19
---

# Phase 200 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | No automated test framework — manual + dotnet build |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser verification |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + `dotnet ef migrations list`
- **Before `/gsd:verify-work`:** Full suite must be green + DB schema verified + IsRenewed logic verified via CDP Certification Management page
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 200-01-01 | 01 | 1 | RENEW-01 | smoke | `dotnet build && dotnet ef database update` | ✅ existing | ⬜ pending |
| 200-01-02 | 01 | 1 | RENEW-01 | smoke | `dotnet build` | ✅ existing | ⬜ pending |
| 200-02-01 | 02 | 2 | RENEW-02 | smoke | `dotnet build` | ✅ existing | ⬜ pending |
| 200-02-02 | 02 | 2 | RENEW-02 | manual | Browser: CDP Certification Management page | ❌ manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test files needed — project uses manual verification pattern with `dotnet build` as compilation gate.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| IsRenewed=true hanya jika ada renewal lulus | RENEW-02 | No test framework; requires DB data + browser | 1. Seed sertifikat dengan renewal lulus → verify IsRenewed=true di CDP page 2. Seed sertifikat dengan renewal gagal saja → verify IsRenewed=false |
| Sertifikat tanpa renewal tetap IsRenewed=false | RENEW-02 | Negative case requires data setup | 1. Sertifikat tanpa renewal session/record → verify IsRenewed=false |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
