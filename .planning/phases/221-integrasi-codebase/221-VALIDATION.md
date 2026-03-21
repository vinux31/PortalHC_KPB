---
phase: 221
slug: integrasi-codebase
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-21
---

# Phase 221 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing (ASP.NET MVC — no automated test suite) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser verification |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` — must compile without errors
- **After every plan wave:** Manual browser check on affected pages
- **Before `/gsd:verify-work`:** Full UAT across all affected controllers/views
- **Max feedback latency:** 15 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 221-01-01 | 01 | 1 | INT-01, INT-02 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 221-02-01 | 02 | 1 | INT-03, INT-04 | build + manual | `dotnet build` | ✅ | ⬜ pending |
| 221-03-01 | 03 | 1 | INT-05, INT-06 | build + manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — project uses manual browser testing for UI verification.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Dropdown Bagian menampilkan data dari DB | INT-01 | UI dropdown rendering | Buka halaman yang ada filter Bagian, verifikasi 4 Bagian tampil |
| Cascade Bagian→Unit berfungsi | INT-02 | JS interaction | Pilih Bagian, verifikasi Unit yang tampil sesuai hierarki |
| Validasi worker Section/Unit | INT-03 | Form submission | Coba create worker dengan Section invalid, harus ditolak |
| L4/L5 section locking | INT-04 | Role-based UI | Login sebagai L4/L5, verifikasi dropdown Bagian terkunci |
| DokumenKkj grouping dari DB | INT-05 | UI grouping | Buka DokumenKkj, verifikasi grouping berdasarkan OrganizationUnit |
| ProtonData Bagian/Unit dari DB | INT-06 | UI dropdown | Buka ProtonData, verifikasi dropdown Bagian/Unit dari DB |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
