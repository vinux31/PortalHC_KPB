---
phase: 365
slug: test-hardening-coach-coachee-af-3-xunit
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-12
---

# Phase 365 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Phase test-only (migration=false). Gate = `dotnet test` hijau + zero behavior change (build hijau).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests, net8.0) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (existing) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter FullyQualifiedName~MarkMappingCompletedTests` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~1 s (filter) / ~39 s (full suite, real-SQL fixture termasuk) |

**Catatan lingkungan:** `MarkMappingCompletedTests` pakai `ProtonCompletionFixture` (real SQL Server, `UseSqlServer` + `MigrateAsync`). Butuh SQL Server lokal `localhost\SQLEXPRESS` (sama seperti `ProtonCompletionServiceTests`/`ProtonApproveRejectParityTests`). InMemory TIDAK dipakai (D-04 — hanya real SQL enforce filtered unique index).

---

## Sampling Rate

- **After every task commit:** quick run (filter MarkMappingCompletedTests).
- **After every plan wave:** `dotnet test` full suite (pastikan zero regresi).
- **Before sign-off:** Full suite hijau + `dotnet build HcPortal.csproj` 0 error (bukti zero behavior change pasca core-extraction).
- **Max feedback latency:** ~15 s (filter), ~40 s (full).

---

## Per-Task Verification Map

| Scenario (D-06) | [Fact] | Behavior locked | Automated Command | Status |
|-----------------|--------|-----------------|-------------------|--------|
| #1 Happy graduate | `MarkMappingCompleted_Happy_FullEndState` | IsCompleted=true, IsActive=false, CompletedAt/EndDate, cascade IsActive=false+DeactivatedAt, cascadeCount==N, progress count utuh | filter | ✅ green |
| #2 Re-assignability | `MarkMappingCompleted_ReassignableAfterGraduate` | index bergigi pra-graduate (Throws DbUpdateException) + bebas pasca (insert sukses) — bukti D-03 | filter | ✅ green |
| #3 Guard no-Tahun3 | `MarkMappingCompleted_Guard_NoTahun3` | ok=false + token "Tahun 3" + tak termutasi | filter | ✅ green |
| #4a Guard Tahun3 progress-not-approved | `MarkMappingCompleted_Guard_Tahun3_ProgressNotApproved` | ok=false + token "belum lulus" + tak termutasi | filter | ✅ green |
| #4b Guard Tahun3 no-FinalAssessment | `MarkMappingCompleted_Guard_Tahun3_NoFinalAssessment` | ok=false + token "belum lulus" + tak termutasi | filter | ✅ green |
| #5 Mapping null | `MarkMappingCompleted_MappingNotFound` | ok=false + token "tidak ditemukan" + cascadeCount=0 | filter | ✅ green |
| #6 History intact | `MarkMappingCompleted_ProgressHistoryIntact` | COUNT progress tak berubah pra/pasca | filter | ✅ green |
| Parity (zero behavior change) | (full suite) | core extraction tak ubah perilaku | `dotnet test` + `dotnet build` | ✅ green (229→229→236) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/MarkMappingCompletedTests.cs` — file test baru (`IClassFixture<ProtonCompletionFixture>`).
- [x] Helper seed Tahun-3-complete (`SeedGraduateReadyAsync` + `SeedTrackChainAsync`) — pola `SeedProgressChainAsync` di-extend ke Tahun 3 + ProtonFinalAssessment + CoachCoacheeMapping.

*Framework xUnit + `ProtonCompletionFixture` sudah ada — tidak perlu install infra baru.*

---

## Manual-Only Verifications

| Behavior | Why Manual | Test Instructions |
|----------|------------|-------------------|
| (none) | Semua skenario AF-3 ter-cover xUnit real-SQL | — |

*Audit log (wrapper) sengaja TIDAK di-assert (D-09) — bukan manual, di luar scope lock.*

---

## Validation Audit 2026-06-12

| Metric | Count |
|--------|-------|
| Requirements/scenarios | 7 (6 D-06, #4 split jadi 2) + parity |
| COVERED | 8/8 (7 [Fact] hijau + full-suite parity) |
| PARTIAL | 0 |
| MISSING | 0 |

State A audit (VALIDATION.md sudah ada dari planning). Cross-ref [Fact] name → scenario: semua ada + hijau. Tidak ada gap → auditor TIDAK di-spawn. Bukti: filter 7/7 (0 failed, 980 ms) + full suite 236/236 (0 failed, 39 s).

---

## Validation Sign-Off

- [x] 6 skenario D-06 (→ 7 [Fact]) punya verifikasi otomatis (real-SQL fixture)
- [x] Parity: full suite hijau (236/236) + build 0 error (bukti zero behavior change)
- [x] No watch-mode flags
- [x] Feedback latency < 40s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-12
