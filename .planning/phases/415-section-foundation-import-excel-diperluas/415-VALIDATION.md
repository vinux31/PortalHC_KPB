---
phase: 415
slug: section-foundation-import-excel-diperluas
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-22
validated: 2026-06-23
---

# Phase 415 — Validation Strategy

> Per-phase validation contract. Finalisasi `/gsd-validate-phase 415` (2026-06-23): REQ baseline + perilaku-fix
> code-review + gap re-check + 1 bug laten (null-key) semuanya terkunci automated test. 2 item LOW = manual-only.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (+ EF Core InMemory 8.0.0; real-SQL fixture `localhost\SQLEXPRESS`/`HcPortalDB_Dev` untuk path ExecuteUpdate) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Section"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **Result (2026-06-23)** | Section filter **38/38**, full suite **651/651** (0 fail/skip) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + quick filter run.
- **After every plan wave:** full suite hijau (baseline TIDAK regresi — invariant kompatibel-mundur).
- **Before `/gsd-verify-work`:** full suite green + `dotnet run` @5277 boot 200 + (UI) Playwright UAT.
- **Max feedback latency:** ~90 detik (quick), ~3 menit (full).

---

## Per-Task Verification Map (REQ baseline)

| Requirement | Test file | Status |
|-------------|-----------|--------|
| SEC-01/02/03 (Section entity + CRUD + assign) | `SectionCrudTests.cs` (9) | COVERED |
| SEC-04 (per-Section count guard import + StartExam D-13) | `SectionImportTests.cs` + `SectionMismatchGuardTests.cs` | COVERED |
| SEC-06 (SyncPackagesToPost clone+remap) | `SectionSyncPrePostTests.cs` (2) | COVERED |
| IMP-01/02/03 (dual-format import, A–F, fingerprint, count) | `SectionImportTests.cs` (7) | COVERED |

---

## Fix + Re-check Regression Coverage (`SectionFixRegressionTests.cs`, 18 test)

Mengunci perilaku 12 fix code-review + 3 gap re-check + 1 bug laten yang ditemukan validate-phase. Tiap test LULUS pada kode pasca-fix dan akan GAGAL pada kode pra-fix.

| Behavior | Test | Status |
|----------|------|--------|
| H1 Pre/Post-aware sibling (tak salah-blok se-tanggal) | `H1_PrePostSameDate_DifferentSection_ImportNotBlocked` + control 2-PreTest | COVERED |
| H2 huruf benar → opsi non-kosong (MC E-blank, MA A,E) | `H2_CorrectLetterPointsToEmptyOption_*` + MA + control | COVERED |
| H3 tolak edit soal >4 opsi (preserve data) | `H3_EditQuestionWithMoreThan4Options_Rejected_OptionsUnchanged` + control 4-opt | COVERED |
| H4 SamePackage sync Section CRUD + **skip-if-Post-taken** | `H4_CreateSectionOnPre_SyncsPostStructure` + `H4_SectionEdit_PostAlreadyTaken_SkipsSyncNoThrow` | COVERED |
| M1 count pasca-dedup | `M1_DedupAwareCount_*` | COVERED |
| M2 deteksi format by nama header | `M2_LegacyHeadersWithStrayColumn10_ParsedAsLegacy_NotShifted` + control | COVERED |
| M3 baseline additive (existing+incoming) | `M3_AdditiveBaseline_TargetPreexisting_ImportRejected` + control | COVERED |
| L3 banding SETIAP sibling (Section-aware + legacy-murni) | `L3_ComparesEverySibling_*` + `L3_LegacyAllNull_WithSiblings_ComparesEvery_NoNullKeyThrow` | COVERED |
| **NULL-KEY** legacy/Lainnya + sibling tak 500 (escalation) | `NullSafe_LegacyImportWithMatchingSibling_Succeeds_NoThrow` + `NullSafe_MixedSectionAndLainnya_ComparesWithoutThrow` | COVERED |

---

## Manual-Only (tidak praktis di unit test)

| Item | Alasan | Verifikasi |
|------|--------|-----------|
| L2 race CreateSection/EditSection (catch 2601/2627) | Butuh dua submit konkuren nyata (TOCTOU); unit test single-thread tak repro race | Code review + log; real-SQL manual bila perlu |
| L6 format label Section konsisten antar elemen UI | Tampilan Razor | Playwright UAT (Phase 419) |

---

## Validation Audit 2026-06-23

| Metric | Count |
|--------|-------|
| Gaps found (fix/re-check behaviors un-tested) | 8 |
| Resolved (automated tests added) | 8 |
| Escalated → impl bug found | 1 (null-key ArgumentNullException) |
| Escalation fixed + locked | 1 (sentinel `SectionStructureComparer.KeyOf`) |
| Manual-only | 2 (L2, L6) |
| New test methods | 18 (`SectionFixRegressionTests.cs`) |

**Escalation (FIXED):** `Dictionary<int?,int>` menolak key null → import legacy/Lainnya dengan paket saudara melempar `ArgumentNullException` (500) SEBELUM perbandingan — **langgar keystone backward-compat**. Bug laten sejak 415-03 (committed), tak ada test lama yang exercise legacy-with-sibling. Fix: `SectionStructureComparer` pakai sentinel `LainnyaKey` + `KeyOf`; call-site import (`AssessmentAdminController.cs`) + guard StartExam (`CMPController.cs`) group via `KeyOf`. CS8714 warning hilang. Tertutup oleh 3 test null-safe.

---

## Backward-Compat Invariant (KEYSTONE)

Assessment/paket **tanpa Section** (`SectionId` semua null) HARUS menghasilkan urutan soal + grading + import yang **identik** dengan baseline pra-415. Gerbang regresi utama milestone — tes lama tetap hijau tanpa modifikasi (full suite 651/651, 0 regresi). Bug null-key di atas adalah pelanggaran invariant ini yang ditemukan + ditutup oleh validate-phase.

---

## Sign-Off

- [x] Semua REQ (SEC-01..06, IMP-01..03) automated COVERED.
- [x] Perilaku 12 fix code-review + 3 gap re-check terkunci regression test.
- [x] 1 bug laten (null-key, keystone) ditemukan + di-fix + di-lock.
- [x] Full suite 651/651, 0 regresi. Build 0 error.
- [ ] 2 item LOW manual-only (L2 race, L6 label UI) — verifikasi non-unit.
- [ ] **Belum di-commit** — impl fix + test file di-commit bersama (lihat `415-REVIEW-FIX.md`).
