---
phase: 370
slug: hapus-window-7-hari-tampilan-default-tanpa-batas
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-11
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
| 370-01-?? | 01 | 1 | URG-02 | — | window dihapus tanpa melonggarkan authorize/filter status | grep-guard + build | `grep -rn "ApplySevenDayWindow\|sevenDaysAgo" Controllers/ Views/ wwwroot/ tests/ HcPortal.Tests/` → zero hit; `dotnet build` 0 error | ✅ (grep, no file) | ⬜ pending |
| 370-01-?? | 01 | 1 | URG-02 | — | suite tidak break setelah hapus 3 [Fact] (229→226) | full suite | `dotnet test` → Failed: 0, Total: 226 | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] DELETE `HcPortal.Tests/AssessmentSearchWindowTests.cs` (D-02 — menghapus 3 [Fact], bukan menambah; suite 229→226)

*Tidak ada framework install — xUnit sudah ada. Tidak ada test baru (D-04: grep-guard + UAT menggantikan).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Default view tampilkan sesi >7 hari (window hilang) | URG-02 (SC1) | Perilaku runtime controller+DbContext — D-04 locked: NO new unit/integration test | `Authentication__UseActiveDirectory=false dotnet run` → login admin @5277 → `/Admin/ManageAssessment` Tab Assessment default → sesi lama (mis. Post Test OJT >7 hari, 55 sesi legacy tersedia) TAMPIL; cek juga `/Admin/AssessmentMonitoring` |
| Search tetap menjangkau semua sesi (no regresi 260611-m9r) | URG-02 (SC2) | idem | Search judul sesi lama → tetap muncul (sekarang default natural, window sudah tak ada) |
| Pagination Tab Assessment jalan dengan dataset membesar | URG-02 (SC4) | idem | Default view → navigasi page 2+ → row berlanjut konsisten |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
