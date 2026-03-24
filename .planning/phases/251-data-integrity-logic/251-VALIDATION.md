---
phase: 251
slug: data-integrity-logic
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-24
---

# Phase 251 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing + dotnet build |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + browser smoke test |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + browser smoke test
- **Before `/gsd:verify-work`:** All 6 success criteria verified manually
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 251-01-01 | 01 | 1 | DATA-01 | build + manual | `dotnet build` | N/A | ⬜ pending |
| 251-01-02 | 01 | 1 | DATA-02 | build + migration | `dotnet ef migrations add` | N/A | ⬜ pending |
| 251-01-03 | 01 | 1 | DATA-03 | build + manual | `dotnet build` | N/A | ⬜ pending |
| 251-01-04 | 01 | 1 | DATA-04 | build + manual | `dotnet build` | N/A | ⬜ pending |
| 251-01-05 | 01 | 1 | DATA-05 | build + manual | `dotnet build` | N/A | ⬜ pending |
| 251-01-06 | 01 | 1 | DATA-06 | build + manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Project uses manual testing — no test framework to install.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Status sertifikat konsisten UTC vs local | DATA-01 | Perlu verifikasi visual badge status | Buka CertificationManagement, cek badge Akan Expired/Expired |
| OrganizationUnit nama sama beda parent | DATA-02 | Perlu CRUD di browser | Kelola Data > buat 2 unit nama sama di parent berbeda |
| Bulk renewal tanpa ValidUntil ditolak | DATA-03 | Perlu submit form di browser | Bulk renew tanpa isi ValidUntil, cek error message |
| Edit assessment past date | DATA-04 | Perlu submit form di browser | Edit assessment jadwal kemarin, submit, verify sukses |
| Log warning RenewalFkMap | DATA-05 | Perlu cek console log | Inject malformed JSON, cek log output |
| _lastScopeLabel dihapus | DATA-06 | Code review cukup | Verify field dihapus, return value digunakan |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
