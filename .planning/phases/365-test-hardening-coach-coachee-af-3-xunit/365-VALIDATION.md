---
phase: 365
slug: test-hardening-coach-coachee-af-3-xunit
status: draft
nyquist_compliant: false
wave_0_complete: false
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
| **Estimated runtime** | ~10–15 s (real-SQL fixture spin-up termasuk) |

**Catatan lingkungan:** `MarkMappingCompletedTests` pakai `ProtonCompletionFixture` (real SQL Server, `UseSqlServer` + `MigrateAsync`). Butuh SQL Server lokal (sama seperti `ProtonCompletionServiceTests`/`ProtonApproveRejectParityTests` yang sudah hijau). InMemory TIDAK dipakai (D-04 — hanya real SQL enforce filtered unique index).

---

## Sampling Rate

- **After every task commit:** Run quick run command (filter MarkMappingCompletedTests).
- **After every plan wave:** Run `dotnet test` (full suite — pastikan zero regresi).
- **Before sign-off:** Full suite hijau + `dotnet build HcPortal.csproj` 0 error (bukti zero behavior change pasca core-extraction).
- **Max feedback latency:** ~15 s.

---

## Per-Task Verification Map

| Scenario (D-06) | Behavior locked | Test Type | Automated Command | Assert inti | Status |
|-----------------|-----------------|-----------|-------------------|-------------|--------|
| #1 Happy graduate | Tahun-3 lulus → ok=true + full mutasi | integration (real-SQL) | quick run | IsCompleted=true, IsActive=false, CompletedAt/EndDate non-null, cascade IsActive=false+DeactivatedAt, cascadeCount==N | ⬜ pending |
| #2 Re-assignability | pasca-graduate insert mapping aktif baru coachee sama → sukses | integration (real-SQL) | quick run | INSERT tidak kena `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (filtered IsActive=1) | ⬜ pending |
| #3 Guard no-Tahun3 | tak ada assignment Tahun 3 → (false,err) | integration | quick run | ok=false + error token + mapping/assignment tak termutasi | ⬜ pending |
| #4 Guard Tahun3-incomplete | Tahun3 ada, progress belum semua Approved / tak ada ProtonFinalAssessment → (false,err) | integration | quick run | ok=false + error token + tak termutasi | ⬜ pending |
| #5 Mapping null | mappingId tak ada → not-found path | integration | quick run | ok=false (core) / NotFound (wrapper) per OQ-2 | ⬜ pending |
| #6 History intact | graduate tak hapus progress | integration | quick run | COUNT ProtonDeliverableProgresses tak berubah pra/pasca | ⬜ pending |
| Parity (zero behavior change) | core extraction tak ubah perilaku | regression | full suite + `dotnet build` | suite hijau + build 0 error | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/MarkMappingCompletedTests.cs` — file test baru (`IClassFixture<ProtonCompletionFixture>`).
- [ ] Helper seed Tahun-3-complete (assignment + semua progress Approved + `ProtonFinalAssessment` + `CoachCoacheeMapping`) — pola `SeedProgressChainAsync` (ProtonApproveRejectParityTests) di-extend ke Tahun 3.

*Framework xUnit + `ProtonCompletionFixture` sudah ada — tidak perlu install infra baru.*

---

## Manual-Only Verifications

| Behavior | Why Manual | Test Instructions |
|----------|------------|-------------------|
| (none) | Semua skenario AF-3 ter-cover xUnit real-SQL | — |

*Audit log (wrapper) sengaja TIDAK di-assert (D-09) — bukan manual, di luar scope lock.*

---

## Validation Sign-Off

- [ ] 6 skenario D-06 punya [Fact] otomatis (real-SQL fixture)
- [ ] Parity: full suite hijau + build 0 error (bukti zero behavior change)
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter (saat planning/eksekusi)

**Approval:** pending
