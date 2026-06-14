---
phase: 373-shuffle-engine-read-logic-reshuffle
verified: 2026-06-13T10:45:00Z
status: human_needed
score: 5/5
overrides_applied: 0
human_verification:
  - test: "Buka exam aktif (ShuffleQuestions=ON) di localhost:5277 — peserta mengerjakan soal"
    expected: "Soal tampil teracak; urutan berbeda antar peserta; tidak ada error render"
    why_human: "Razor runtime + UI exam — tidak bisa diverifikasi via grep atau dotnet test"
  - test: "Buka exam aktif (ShuffleQuestions=OFF, 1 paket) di localhost:5277 — beberapa peserta berbeda"
    expected: "Semua peserta melihat soal dalam urutan identik (q.Order)"
    why_human: "Memerlukan multi-session browser live untuk konfirmasi urutan identik"
  - test: "HC login → reshuffle satu peserta Not-started dari assessment ShuffleOptions=ON → cek DB kolom ShuffledOptionIdsPerQuestion"
    expected: "Nilai bukan '{}' — berisi JSON dict pasangan questionId:optionIds"
    why_human: "Memerlukan HC role + klik UI + inspeksi DB; full UAT = Phase 375 scope"
---

# Phase 373: Shuffle Engine (read logic + reshuffle) — Verification Report

**Phase Goal:** CMPController.StartExam gerbang flag saat bangun UserPackageAssignment + ekstrak core pure (testable tanpa DB): Acak Soal ON=existing (1 paket acak / ≥2 sampling K); OFF+1 paket=urut q.Order; OFF+≥2 paket=round-robin index-session-stabil 1 paket/worker + guard paket kosong; Acak Pilihan independen (ON dict / OFF "{}"); resume stale-count guard deterministik; cleanup komentar stale CMPController.cs:1054; ReshufflePackage/ReshuffleAll hormati KEDUA flag (fix bug existing opsi hard-code "{}").
**Verified:** 2026-06-13T10:45:00Z
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

---

## Goal Achievement

### Observable Truths (ROADMAP Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Acak Soal ON tak berubah (1 paket urutan acak; ≥2 paket sampling K + acak) | VERIFIED | `ShuffleEngine.BuildCrossPackageAssignment` memindahkan VERBATIM canonical ON-path dari CMPController (per-ET `basePerET`). `ShuffleEngineTests`: `On_SinglePackage_SeedStable_ReturnsAllShuffled` + `On_MultiPackage_SeedStable_SamplesKMin` hijau. CMPController.cs:987 delegasi ke `ShuffleEngine.BuildQuestionAssignment(..., assessment.ShuffleQuestions, ...)`. |
| 2 | Acak Soal OFF + 1 paket → semua peserta soal & urutan identik (q.Order) | VERIFIED | `ShuffleEngine.BuildQuestionAssignment` cabang OFF+1 paket: `q1.OrderBy(x => x.Order).Select(x => x.Id)`, tidak ada rng. Test `Off_SinglePackage_ReturnsQuestionsInOrder` membuktikan urutan deterministik. |
| 3 | Acak Soal OFF + ≥2 paket → tiap worker 1 paket utuh deterministik (index-session-stabil), seimbang, tahan resume/reshuffle; paket kosong di-skip | VERIFIED | Cabang OFF+≥2: filter `packagesWithQuestions` SEBELUM modulo → `chosen = packagesWithQuestions[workerIndex % count]`. Tests: `Off_MultiPackage_WorkerIndexMapsToPackage` [Theory 4 cases], `Off_MultiPackage_EmptyPackageExcludedBeforeModulo`, `Off_AllPackagesEmpty_ReturnsEmpty_NoDivideByZero`, `Off_MultiPackage_AppendNewWorker_DoesNotShiftExisting`, `Determinism_CalledTwice_SameInput_SameOutput`. Worker-index dari `siblingSessionIds.OrderBy(x => x).IndexOf(id)` di CMPController:959 + AssessmentAdminController:5113,5167. OQ#1 parity: kedua endpoint reshuffle dan StartExam sama-sama key pada `Title+Category+Schedule.Date`. |
| 4 | Acak Pilihan ON/OFF independen dari Acak Soal; OFF → view urutan DB | VERIFIED | `BuildOptionShuffle(questions, shuffleOptions, rng)`: OFF → empty dict → serialize "{}"; ON → dict per question. Tests: `Options_On_BuildsNonEmptyDict`, `Options_Off_ReturnsEmptyDict`, `Independence_QuestionsOff_OptionsOn`. CMPController:993 gerbang `assessment.ShuffleOptions`. Comment :1060 menjelaskan fallback DB-order. |
| 5 | Reshuffle hormati flag (incl. opsi diacak saat ShuffleOptions ON — bug lama fixed) | VERIFIED | `ShuffledOptionIdsPerQuestion = "{}"` hard-code: 0 match di AssessmentAdminController. Sekarang `System.Text.Json.JsonSerializer.Serialize(optDict)` di :5126 dan :5227. ReshufflePackage(:5115) + ReshuffleAll(:5213) keduanya memanggil `ShuffleEngine.BuildQuestionAssignment` + `ShuffleEngine.BuildOptionShuffle`. Test `ShuffleReshuffleTests`: `OptionShuffle_On_DoesNotSerializeToEmptyObject` + `OptionShuffle_Off_SerializesToEmptyObject`. |

**Score: 5/5 truths verified**

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/ShuffleEngine.cs` | Pure static core: BuildQuestionAssignment + BuildOptionShuffle + Shuffle\<T\> (no EF/DB/async) | VERIFIED | Ada, 226 baris. `public static class ShuffleEngine`. 0 match `EntityFrameworkCore\|ApplicationDbContext\|async`. `basePerET` 3 match (canonical per-ET Phase 2). 4 member: `Shuffle<T>` (public), `BuildQuestionAssignment` (public), `BuildOptionShuffle` (public), `BuildCrossPackageAssignment` (private). |
| `HcPortal.Tests/ShuffleEngineTests.cs` | 14 pure unit tests (SHUF-04/05/06/07/08), no DB/fixture | VERIFIED | Ada, 193 baris. `public class ShuffleEngineTests`. 0 match `Trait("Category","Integration")\|IClassFixture\|ApplicationDbContext`. `new Random(42)` 2 match (seed-stable). 14 test methods menutup semua mode. Hijau 14/14. |
| `Controllers/CMPController.cs` | StartExam delegasi ke ShuffleEngine, gerbang flag, worker-index stabil, komentar diperbaiki, lokal dup dihapus | VERIFIED | `ShuffleEngine.BuildQuestionAssignment` = 1 match. `ShuffleEngine.BuildOptionShuffle` = 1 match. `assessment.ShuffleQuestions/ShuffleOptions` = 1 match masing-masing. `OrderBy(x => x)` = 1 match di region sibling. `option shuffle removed` = 0 match (SHUF-15). `private static List<int> BuildCrossPackageAssignment` = 0 match. `private static void Shuffle<T>` = 0 match. `SavedQuestionCount = shuffledIds.Count` dan `catch (DbUpdateException)` keduanya dipertahankan. |
| `Controllers/AssessmentAdminController.cs` | ReshufflePackage + ReshuffleAll delegasi ke ShuffleEngine, "{}" bug fixed, divergent dup dihapus, auth/guards preserved | VERIFIED | `ShuffleEngine.BuildQuestionAssignment` = 2 match. `ShuffleEngine.BuildOptionShuffle` = 2 match. `ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize` = 2 match. `ShuffledOptionIdsPerQuestion = "{}"` = 0 match. `private static List<int> BuildCrossPackageAssignment` = 0 match. `private static void Shuffle<T>` = 0 match. `[Authorize(Roles="Admin, HC")]` ada di kedua endpoint (:5063, :5151). `[ValidateAntiForgeryToken]` ada di kedua (:5064, :5152). `userStatus != "Not started"` = 2 match (:5083, :5200). |
| `HcPortal.Tests/ShuffleReshuffleTests.cs` | 2 pure [Fact] regresi SHUF-09 | VERIFIED | Ada, 44 baris. `Assert.NotEqual("{}"` dan `Assert.Equal("{}"` = 1 match masing-masing. Hijau 2/2. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `CMPController.StartExam` build branch | `ShuffleEngine.BuildQuestionAssignment / BuildOptionShuffle` | Delegasi gerbang `assessment.ShuffleQuestions` + `assessment.ShuffleOptions`, `workerIndex` dari `sortedSiblingIds.IndexOf(id)` | WIRED | CMPController:986-994 — call sites terverifikasi via grep dan pembacaan kode langsung |
| `CMPController.StartExam` sibling lookup | Stable worker index | `siblingSessionIds.OrderBy(x => x).ToList()` → `.IndexOf(id)` | WIRED | CMPController:959-960 — Pitfall 2 fix terkonfirmasi |
| `AssessmentAdminController.ReshufflePackage` | `ShuffleEngine.BuildQuestionAssignment / BuildOptionShuffle` | Delegasi gerbang `assessment.ShuffleQuestions/ShuffleOptions`, `workerIndex` dari `sortedSiblingIds.IndexOf(sessionId)` | WIRED | AssessmentAdminController:5113-5117 |
| `AssessmentAdminController.ReshuffleAll` | `ShuffleEngine.BuildQuestionAssignment / BuildOptionShuffle` | Sort sekali sebelum loop; per-session `workerIndex = sortedSiblingIds.IndexOf(session.Id)`, gerbang `session.ShuffleQuestions/ShuffleOptions` | WIRED | AssessmentAdminController:5167, 5212-5215 |
| `reshuffle ShuffledOptionIdsPerQuestion` | Real option-shuffle dict (bukan empty hard-coded) | `System.Text.Json.JsonSerializer.Serialize(optDict)` | WIRED | AssessmentAdminController:5126, 5227 — bug lama `= "{}"` telah hilang |
| `ShuffleEngine.BuildCrossPackageAssignment` | `Helpers/ShuffleEngine.cs` canonical (per-ET basePerET) | BUKAN salinan divergent AssessmentAdminController (per-package baseCount) | WIRED | `basePerET` = 3 match di ShuffleEngine.cs; 0 match `baseCount` untuk versi per-package lama |
| `HcPortal.Tests/ShuffleEngineTests.cs` | `ShuffleEngine` static methods | Panggilan langsung, in-memory POCO fixtures | WIRED | `ShuffleEngine.(BuildQuestionAssignment\|BuildOptionShuffle)` dipanggil di semua test methods |

---

### Data-Flow Trace (Level 4)

Hanya relevan untuk komponen yang merender data dinamis. ShuffleEngine adalah pure helper (tidak render). CMPController.StartExam adalah controller yang membangun ViewModel dari `shuffledIds` dan `optionShuffleDict`:

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `CMPController.StartExam` UserPackageAssignment | `shuffledIds` | `ShuffleEngine.BuildQuestionAssignment(packages, ...)` dimana `packages` = EF query nyata dengan `.Include(p => p.Questions).ThenInclude(q => q.Options)` | Ya — paket diambil dari DB, diproses oleh engine pure | FLOWING |
| `CMPController.StartExam` option shuffle | `optionShuffleDict` | `ShuffleEngine.BuildOptionShuffle(assignedQuestions, assessment.ShuffleOptions, rng)` dimana `assignedQuestions` subset dari packages DB | Ya — ShuffleOptions OFF serialize `"{}"` sebagai fallback order-DB yang benar; ON hasilkan dict nyata | FLOWING |
| `AssessmentAdminController.ReshufflePackage` | `shuffledIds`, `optDict` | `ShuffleEngine.BuildQuestionAssignment/BuildOptionShuffle(packages, assessment.ShuffleQuestions/ShuffleOptions, ...)` dimana packages dari EF query nyata | Ya — tidak ada return statis; hasil di-serialize ke DB via `UserPackageAssignment` | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| `ShuffleEngine` pure — 0 EF/DB imports | `rg "EntityFrameworkCore\|ApplicationDbContext\|async" Helpers/ShuffleEngine.cs` | 0 match | PASS |
| Canonical ON-path (basePerET) ada di core | `rg "basePerET" Helpers/ShuffleEngine.cs` | 3 match | PASS |
| Bug `"{}"` hilang dari reshuffle | `rg 'ShuffledOptionIdsPerQuestion = "\{\}"' Controllers/AssessmentAdminController.cs` | 0 match | PASS |
| Komentar stale SHUF-15 dihapus | `rg "option shuffle removed" Controllers/CMPController.cs` | 0 match | PASS |
| Local dup BuildCrossPackageAssignment dihapus dari kedua controller | `rg "private static List<int> BuildCrossPackageAssignment" Controllers/` | 0 match | PASS |
| Local Shuffle\<T\> dihapus dari kedua controller | `rg "private static void Shuffle<T>" Controllers/` | 0 match | PASS |
| 23 shuffle tests hijau | `dotnet test --filter "FullyQualifiedName~Shuffle"` | Passed! 23/23 (0 failures) | PASS |
| Full suite 329 tests hijau | `dotnet test` | Passed! 329/329 (0 failures, 1m25s) | PASS |
| Build sukses | `dotnet build` | Build succeeded, 0 errors | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|------------|-----------|--------|---------|
| SHUF-04 | 373-01, 373-02 | Acak Soal ON = perilaku existing (1 paket: acak urutan; ≥2 paket: sampling K lintas paket + ET-balanced + acak) | SATISFIED | `BuildCrossPackageAssignment` (canonical per-ET) dipindah verbatim ke ShuffleEngine; tests `On_SinglePackage_SeedStable_ReturnsAllShuffled` + `On_MultiPackage_SeedStable_SamplesKMin` hijau; StartExam delegasi via `assessment.ShuffleQuestions` |
| SHUF-05 | 373-01, 373-02 | Acak Soal OFF + 1 paket = semua soal, urutan q.Order (semua peserta identik) | SATISFIED | Cabang `packages.Count == 1` + `shuffleQuestions == false` → `q.Order` sort, no rng; test `Off_SinglePackage_ReturnsQuestionsInOrder` |
| SHUF-06 | 373-01, 373-02 | Acak Soal OFF + ≥2 paket = distribusi 1 paket utuh/worker, round-robin index-session-stabil, urutan q.Order, guard paket kosong | SATISFIED | Filter-then-modulo + `workerIndex % packagesWithQuestions.Count`; 5 tests OFF-multi; worker-index stabil di CMPController + kedua reshuffle endpoints |
| SHUF-07 | 373-01, 373-02 | Acak Pilihan independen dari Acak Soal — ON bangun optionShuffleDict, OFF simpan "{}" | SATISFIED | `BuildOptionShuffle(questions, shuffleOptions, rng)` — 2 paths; `Independence_QuestionsOff_OptionsOn` test; CMPController:993 gerbang `assessment.ShuffleOptions` |
| SHUF-08 | 373-01, 373-02 | Resume stale-count guard deterministik untuk mode OFF | SATISFIED | OFF path sepenuhnya deterministik (tidak pakai rng); `Determinism_CalledTwice_SameInput_SameOutput` test; `SavedQuestionCount = shuffledIds.Count` + stale-count guard di CMPController:1033 dipertahankan verbatim |
| SHUF-09 | 373-03 | ReshufflePackage/ReshuffleAll hormati ShuffleQuestions DAN ShuffleOptions (fix bug existing hard-code opsi "{}") | SATISFIED | Hard-code `= "{}"` = 0 match; `JsonSerializer.Serialize(optDict)` = 2 match; kedua endpoint delegasi `BuildQuestionAssignment` + `BuildOptionShuffle`; `ShuffleReshuffleTests` 2/2 hijau |
| SHUF-15 | 373-02 | Cleanup komentar stale CMPController.cs:1054 ("option shuffle removed") | SATISFIED | `rg "option shuffle removed" Controllers/CMPController.cs` → 0 match; komentar baru di :1059-1060 menjelaskan mekanisme ShuffleOptions dengan benar |

**Semua 7 requirement tercakup. Tidak ada orphaned requirements.**

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| Tidak ada | — | — | — |

Tidak ada TODO/FIXME/placeholder, stub return, atau empty-implementation ditemukan di file yang dimodifikasi Phase 373.

---

### Human Verification Required

#### 1. Exam ON — Urutan Soal Teracak di Browser

**Test:** `dotnet run` di localhost:5277. Login sebagai peserta, buka assessment dengan ShuffleQuestions=ON. Lihat halaman ujian.
**Expected:** Soal tampil dalam urutan acak yang konsisten; tidak ada error rendering; urutan berbeda dari urutan q.Order default.
**Why human:** Razor runtime + UI exam — tidak bisa diverifikasi dengan grep atau unit test.

#### 2. Exam OFF + 1 Paket — Urutan Identik Semua Peserta

**Test:** `dotnet run` di localhost:5277. Buka assessment ShuffleQuestions=OFF dengan 1 paket dari dua akun berbeda.
**Expected:** Kedua peserta melihat soal dalam urutan yang persis sama (q.Order), bukan random.
**Why human:** Memerlukan multi-session browser live untuk konfirmasi urutan identik.

#### 3. Reshuffle ShuffleOptions=ON → DB Dict Non-Kosong

**Test:** HC login → buka ManagePackages → reshuffle peserta Not-started dari assessment ShuffleOptions=ON → cek kolom `ShuffledOptionIdsPerQuestion` di DB.
**Expected:** Nilai berisi JSON dict seperti `{"1":[10,12,11],"2":[20,21]}` — bukan `"{}"`.
**Why human:** Memerlukan HC role + klik UI + inspeksi DB; full Playwright UAT = Phase 375 scope.

---

### Gaps Summary

Tidak ada gaps. Semua 5 roadmap success criteria terverifikasi secara programatik. 7/7 requirements SHUF-04/05/06/07/08/09/15 satisfied. 329/329 tests hijau termasuk 23 Shuffle-specific tests (14 pure engine + 2 reshuffle regresi + 7 Phase 372 real-SQL). Build 0 error.

Status `human_needed` mencerminkan bahwa verifikasi browser/UI live (ujian tampil, opsi teracak di DB) merupakan UAT yang wajar sebelum push — bukan indikasi code gap. Behavior visual dan efek toggle live = Phase 375 full UAT scope sesuai 373-VALIDATION.md.

---

_Verified: 2026-06-13T10:45:00Z_
_Verifier: Claude (gsd-verifier)_
