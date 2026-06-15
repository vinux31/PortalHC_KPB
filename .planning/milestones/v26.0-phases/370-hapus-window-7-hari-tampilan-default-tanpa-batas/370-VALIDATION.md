---
phase: 370
slug: hapus-window-7-hari-tampilan-default-tanpa-batas
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-11
audited: 2026-06-12
auditor: gsd-nyquist-auditor
---

# Phase 370 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + EFCore.InMemory 8.0.0 (dotnet 8.0) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~ManageAssessment"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | quick ~10s · full ~45s |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + `dotnet test --filter "FullyQualifiedName~ManageAssessment"`
- **After every plan wave:** Run `dotnet test` (full — HARUS 226 hijau setelah hapus 3 [Fact])
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 370-01-T1 | 01 | 1 | URG-02 | — | window dihapus tanpa melonggarkan authorize/filter status | grep-guard + build | `grep -rn "ApplySevenDayWindow\|sevenDaysAgo" Controllers/ Views/ wwwroot/ tests/ HcPortal.Tests/` → zero hit; `dotnet build` 0 error | ✅ (grep, no file) | ✅ green |
| 370-01-T1 | 01 | 1 | URG-02 | — | suite tidak break setelah hapus 3 [Fact] (229→226→227) | full suite | `dotnet test` → Failed: 0, Total: 227 | ✅ | ✅ green |
| 370-01-SC1 | 01 | post | URG-02 (SC1) | — | default view (no search) tampilkan sesi >7 hari (window removal regression guard) | integration (xUnit + EFCore.InMemory) | `dotnet test --filter "FullyQualifiedName~AssessmentWindowRemovalTests"` → Failed: 0, Passed: 1 | ✅ `HcPortal.Tests/AssessmentWindowRemovalTests.cs` | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

> **Audit 2026-06-12 (gsd-nyquist-auditor):** SC1 di-promote dari manual-only (D-04) → automated. Integration test pertama yang meng-instantiate `AssessmentAdminController` (null-deps pattern: hanya `_context` InMemory + real `MemoryCache` + real `AuditLogService` + `NullLogger`; 9 deps lain `null!`). 3 iter debug: (1) `_logger` deref → NullLogger; (2) EF InMemory inner-join nav `a.User` drop row tanpa FK → seed `ApplicationUser`; (3) anon-type internal lintas-assembly → reflection `GetProperty("Title")`. Self-verified `dotnet test` Failed:0 Passed:1 + full suite 227.

---

## Wave 0 Requirements

- [x] DELETE `HcPortal.Tests/AssessmentSearchWindowTests.cs` (D-02 — menghapus 3 [Fact]; suite 229→226). DONE (SUMMARY: `Test-Path`=False).

*Tidak ada framework install — xUnit sudah ada. (Post-audit 2026-06-12: 1 test baru `AssessmentWindowRemovalTests.cs` ditambah → 227; D-04 di-override untuk SC1 saja, lihat Per-Task Map.)*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ~~Default view tampilkan sesi >7 hari~~ → **PROMOTED to automated 2026-06-12** | URG-02 (SC1) | ✅ Kini ter-cover `AssessmentWindowRemovalTests.cs` (integration). UAT live 5/5 tetap sebagai bukti end-to-end. | (automated — lihat Per-Task Map row 370-01-SC1) |
| Search tetap menjangkau semua sesi (no regresi 260611-m9r) | URG-02 (SC2) | Runtime EF `.Contains()` di partial-view path; jalur search tak diubah fase ini (mitigate-existing T-370-03). Integration test bisa ditambah serupa SC1 bila perlu, tapi UAT 5/5 sudah cover. | Search judul sesi lama → tetap muncul (default natural, window sudah tak ada) |
| Pagination Tab Assessment jalan dengan dataset membesar | URG-02 (SC4) | Logika paging murni ter-cover `PaginationHelper.Calculate` unit test (`ManageAssessmentMedFixTests.cs`); rendering page-nav = UI, UAT-only. | Default view → navigasi page 2+ → row berlanjut konsisten |

*SC1 di-automate (regression guard window-removal). SC2/SC4 tetap manual-only by design (UI/EF-query runtime) — sudah UAT 5/5 + paging unit-covered. Tidak ada MISSING automated gap tersisa.*

---

## Validation Audit 2026-06-12

| Metric | Count |
|--------|-------|
| Gaps found | 1 (SC1 window-removal manual-only) |
| Resolved | 1 (automated via `AssessmentWindowRemovalTests.cs`) |
| Escalated | 0 |

Full suite post-audit: `dotnet test` → **Failed: 0, Total: 227** (226 + 1 baru). Self-verified.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (test file delete DONE; +1 test added)
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-12
