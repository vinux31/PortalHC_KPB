---
phase: 396
slug: import-excel-retire-bulkbackfill
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-18
updated: 2026-06-18
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
| **Estimated runtime** | ~5s fast suite (389 tests) ; e2e ~20s/skenario |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + `dotnet test --filter Category!=Integration`
- **After every plan wave:** full `dotnet test` + Playwright e2e relevan
- **Before `/gsd-verify-work`:** full suite hijau + `dotnet run` (localhost:5277) UAT
- **Max feedback latency:** ~5s (fast suite, 389 tests)

---

## Per-Task Verification Map

State A audit 2026-06-18 — semua referensi Wave-0 kini ADA dan HIJAU (dijalankan sesi eksekusi 396).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 396-01-01 | 01 | 1 | INJ-10 | — | parser map huruf→opsi by urutan authored (A=Options[0]); round-trip gen→parse identik (ONE comparator D-04) | unit | `dotnet test --filter FullyQualifiedName~InjectExcelHelper` | ✅ `InjectExcelHelperTests.cs` | ✅ green (8/8) |
| 396-01-02 | 01 | 1 | INJ-10 | T-396-03 | sel kosong = OMIT spec → grade 0 (bukan kirim MC kosong = reject-all) D-06 | unit | `dotnet test --filter FullyQualifiedName~InjectExcelHelper` | ✅ `InjectExcelHelperTests.cs` | ✅ green (8/8) |
| 396-01-03 | 01 | 1 | INJ-10 | T-396-01,02 | NIP luar picker ditolak; huruf opsi invalid ditolak; essay>ScoreValue ditolak; ≥1 error → rollback total (daftar lengkap) D-09 | unit | `dotnet test --filter FullyQualifiedName~InjectExcelHelper` | ✅ `InjectExcelHelperTests.cs` | ✅ green (8/8) |
| 396-02-01 | 02 | 2 | INJ-10 | — | helper impl GenerateTemplate+ParseMatrix → Wave-0 suite RED→GREEN | unit | `dotnet test --filter FullyQualifiedName~InjectExcelHelper` | ✅ `InjectExcelHelperTests.cs` | ✅ green (8/8) |
| 396-02-02 | 02 | 2 | INJ-10 | T-396-04 | EssayTextRequired scoped form-only (D-05); Excel essay skor-tanpa-teks tidak ditolak | unit/integration | `dotnet test --filter FullyQualifiedName~InjectExcelImport` | ✅ `InjectExcelImportTests.cs` | ✅ green (3/3) |
| 396-03-01 | 03 | 3 | INJ-10 | T-396-07 | Excel path emit spec identik form → preview==commit (Aggregator), no new grading branch | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~InjectExcelImport"` | ✅ `InjectExcelImportTests.cs` | ✅ green (3/3) |
| 396-03-02 | 03 | 3 | INJ-10 | T-396-01 | baris invalid (huruf/skor/NIP) → rollback atomik, 0 sesi tertulis | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~InjectExcelImport"` | ✅ `InjectExcelImportTests.cs` | ✅ green (3/3) |
| 396-04-01 | 04 | 4 | INJ-10 | T-396-09,10 | e2e: toggle + download template → upload → preview → commit → Records/Results; cache-gate anti silent-grade-0; XSS .textContent | e2e | `npx playwright test tests/e2e/inject-excel-396.spec.ts --workers=1` | ✅ `inject-excel-396.spec.ts` | ✅ green (6 skenario) |
| 396-05-01 | 05 | 5 | INJ-11 | T-396-11 | ManualDuplicatePredicate/CleanupAttemptHistory/ClosedXML TETAP; DuplicateGuardTests compile+hijau; build 0-err | unit | `dotnet build` + `dotnet test --filter FullyQualifiedName~DuplicateGuard` | ✅ `DuplicateGuardTests.cs` | ✅ green (9/9) |
| 396-05-02 | 05 | 5 | INJ-11 | T-396-03,12 | route BulkBackfill 404 (GET+POST); 2 entry-point UI + orphan divider hilang | e2e | `npx playwright test tests/e2e/inject-excel-396.spec.ts -g "BulkBackfill retired" --workers=1` | ✅ `inject-excel-396.spec.ts` (Scenario 6) | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/InjectExcelHelperTests.cs` — unit parser/generator (map huruf↔opsi, blank=skip, round-trip, validasi-reject) untuk INJ-10 — **8/8 green**
- [x] `HcPortal.Tests/InjectExcelImportTests.cs` (Category=Integration) — Excel→spec identik form, preview==commit, atomic rollback, essay text-optional — **3/3 green**
- [x] `tests/e2e/inject-excel-396.spec.ts` — Playwright lifecycle download→fill→upload→preview→commit + invalid→rollback + blank-warn + BulkBackfill 404/cards-gone — **6 skenario green**
- [x] Reuse fixtures existing (real-SQL) — `DuplicateGuardTests` 9/9 hijau (INJ-11 keeps), full fast suite 389/389

*Infra xUnit + Playwright sudah ada (393/395); 396 nambah 3 file test baru di atas.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Status |
|----------|-------------|------------|--------|
| Hasil import muncul "Assessment Online" di /CMP/Records + rincian per-soal /CMP/Results + sertifikat dapat diunduh | INJ-10 | UAT "seakan online" lintas-surface (formal di Phase 398) | ✅ VERIFIED 2026-06-18 — browser UAT (Playwright MCP @localhost:5277): commit Excel → sesi #173 Score=100/Completed + cert KPB/005/VI/2026 + /CMP/Results/173 per-soal "Tinjauan Jawaban" + /CMP/Certificate/173 unduh. DB di-restore. |

*Sisanya ber-otomasi (unit/integration/e2e). Manual-only ini SUDAH diverifikasi sesi UAT; re-konfirmasi lintas-surface penuh di Phase 398.*

---

## Validation Sign-Off

- [x] Semua task punya `<automated>` verify atau dependency Wave 0
- [x] Sampling continuity: tak ada 3 task beruntun tanpa automated verify
- [x] Wave 0 cover semua referensi MISSING (semua ✅ green)
- [x] No watch-mode flags (Playwright `--workers=1`)
- [x] Feedback latency < 60s (fast suite ~5s)
- [x] `nyquist_compliant: true` di-set setelah Wave 0 lengkap

**Approval:** verified 2026-06-18

---

## Validation Audit 2026-06-18

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 (semua referensi sudah COVERED+green pasca-eksekusi) |
| Escalated | 0 |

State A audit: VALIDATION.md draft pra-eksekusi (semua ⬜ pending) di-rekonsiliasi terhadap test ter-implementasi. Semua 10 baris peta → ✅ green; 1 manual-only sudah diverifikasi via browser UAT. Tak ada gap MISSING/PARTIAL → auditor tidak di-spawn (Step 3 short-circuit). Baseline: build 0-err, fast suite 389/389, InjectExcelHelper 8/8, InjectExcelImport(Integration) 3/3, DuplicateGuard 9/9, Playwright inject-excel-396 6 skenario. **NYQUIST-COMPLIANT.**
