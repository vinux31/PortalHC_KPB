---
phase: 396
slug: import-excel-retire-bulkbackfill
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 396 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail test/sampling diturunkan planner dari `396-RESEARCH.md` §"Validation Architecture".

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (unit/integration .NET) + Playwright (e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `tests/e2e/` (Playwright) |
| **Quick run command** | `dotnet test --filter Category!=Integration` |
| **Full suite command** | `dotnet test` + `npx playwright test tests/e2e/inject-*.spec.ts --workers=1` |
| **Estimated runtime** | ~30-60s fast suite; e2e tambahan |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + `dotnet test --filter Category!=Integration`
- **After every plan wave:** full `dotnet test` + Playwright e2e relevan
- **Before `/gsd-verify-work`:** full suite hijau + `dotnet run` (localhost:5277) UAT
- **Max feedback latency:** ~60s (fast suite)

---

## Per-Task Verification Map

*(Diisi/diperhalus oleh planner dari RESEARCH §Validation Architecture. Peta awal INJ-10/INJ-11.)*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 396-01-01 | 01 | 0 | INJ-10 | — | parser map huruf→opsi by urutan authored (A=Options[0]); round-trip gen→parse identik | unit | `dotnet test --filter FullyQualifiedName~InjectExcel` | ❌ W0 | ⬜ pending |
| 396-01-02 | 01 | 0 | INJ-10 | T-396-01 | sel kosong = OMIT spec → grade 0 (bukan kirim MC kosong = reject-all) | unit | `dotnet test --filter FullyQualifiedName~InjectExcel` | ❌ W0 | ⬜ pending |
| 396-01-03 | 01 | 0 | INJ-10 | T-396-02 | NIP di luar picker ditolak; huruf opsi invalid ditolak; essay>ScoreValue ditolak; ≥1 error → rollback total (daftar lengkap) | unit | `dotnet test --filter FullyQualifiedName~InjectExcel` | ❌ W0 | ⬜ pending |
| 396-02-xx | 02 | 1 | INJ-10 | — | Excel path emit InjectWorkerSpec identik form → preview==commit (Aggregator) | integration | `dotnet test --filter Category=Integration&FullyQualifiedName~InjectExcel` | ❌ W0 | ⬜ pending |
| 396-03-xx | 03 | 2 | INJ-11 | T-396-03 | route BulkBackfill 404; 2 entry-point UI hilang; ManualDuplicatePredicate TETAP; DuplicateGuardTests tetap compile/hijau | unit+grep | `dotnet build` + `dotnet test --filter FullyQualifiedName~DuplicateGuard` | ✅ | ⬜ pending |
| 396-03-xx | 03 | 2 | INJ-10,INJ-11 | — | e2e: download template→isi→upload→preview→commit→Records/Results; baris invalid→error list→rollback | e2e | `npx playwright test tests/e2e/inject-excel-396.spec.ts --workers=1` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/InjectExcelHelperTests.cs` — unit parser/generator (map huruf↔opsi, blank=skip, round-trip, validasi-reject) untuk INJ-10
- [ ] `HcPortal.Tests/InjectExcelImportTests.cs` (Category=Integration) — Excel→InjectWorkerSpec identik form, preview==commit, atomic rollback
- [ ] `tests/e2e/inject-excel-396.spec.ts` — Playwright lifecycle download→fill→upload→preview→commit + invalid→rollback + BulkBackfill 404/cards-gone
- [ ] Reuse fixtures existing `InjectAssessmentServiceTests`/`InjectPreviewEqualsCommitTests` (real-SQL)

*Infra xUnit + Playwright sudah ada (393/395); 396 nambah file test baru di atas.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Hasil import muncul "Assessment Online" di /CMP/Records + rincian per-soal /CMP/Results + sertifikat | INJ-10 | UAT "seakan online" lintas-surface (sebagian di-cover Phase 398) | `dotnet run` → InjectAssessment → upload Excel → commit → buka /CMP/Records + /CMP/Results worker |

*Sisanya ber-otomasi (unit/integration/e2e).*

---

## Validation Sign-Off

- [ ] Semua task punya `<automated>` verify atau dependency Wave 0
- [ ] Sampling continuity: tak ada 3 task beruntun tanpa automated verify
- [ ] Wave 0 cover semua referensi MISSING
- [ ] No watch-mode flags (Playwright `--workers=1`)
- [ ] Feedback latency < 60s (fast suite)
- [ ] `nyquist_compliant: true` di-set setelah Wave 0 lengkap

**Approval:** pending
