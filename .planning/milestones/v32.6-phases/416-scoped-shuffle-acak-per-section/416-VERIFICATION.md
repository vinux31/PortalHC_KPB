---
phase: 416-scoped-shuffle-acak-per-section
verified: 2026-06-23T12:08:02Z
status: human_needed
score: 7/7 must-haves verified
overrides_applied: 0
re_verification:
  previous_status: none
  previous_score: n/a
human_verification:
  - test: "ET-coverage warning (D-416-03) render sisi-positif — alert kuning 'Elemen Teknis' muncul saat cakupan ET tipis"
    expected: "Alert non-blocking tampil di panel Section ManagePackageQuestions saat distinct ET Section melebihi kuota soal yang dipresentasikan"
    why_human: "Predikat saat ini (DistinctEt > K, K = COUNT soal Section single-package) tak terjangkau data nyata (DEF-416-01) → alert TIDAK PERNAH render. Bukan bug fungsional/keamanan (warning = sinyal non-blocking, INTI load-bearing 'Section sempit tetap boleh' sudah terbukti S3+S3b). Keputusan desain: terima sebagai dead-nicety (defer ke 419/backlog) ATAU re-spec predikat (distinct ET lintas paket-saudara vs K-min) sekarang. Butuh keputusan owner — bukan auto-fixable."
  - test: "UAT live browser @5277 — isolasi section + precedence D-14 + backward-compat (langkah 1-7 plan 03 Task 2)"
    expected: "Soal teracak DALAM Section (Pompa blok lalu Valve blok, tak lompat); induk OFF → semua terurut; 1 Section Acak=OFF → Section itu terurut; all-null = perilaku lama; DB ShuffledQuestionIds = blok-per-section"
    why_human: "Checkpoint:human-verify (gate blocking) di Plan 03 Task 2 di-AUTO-SATISFY oleh autopilot §5 (Playwright green sebagai bukti runtime), bukan oleh mata-manusia. Razor/JS render WAJIB dikonfirmasi manusia sebelum ship (lesson 354). e2e 5/5 hijau menutup risiko teknis; konfirmasi visual akhir tetap rekomendasi pra-ship."
---

# Phase 416: Scoped Shuffle (Acak per-Section) Verification Report

**Phase Goal:** Pengacakan soal & pilihan terjadi hanya di dalam lingkup Section (soal tak melompat antar-Section), dengan assessment tanpa Section berperilaku persis seperti sekarang.
**Verified:** 2026-06-23T12:08:02Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #   | Truth | Status | Evidence |
| --- | ----- | ------ | -------- |
| 1 | Assessment tanpa Section (semua SectionId=null) = urutan IDENTIK baseline pra-416 (golden-order, SHF-04) | ✓ VERIFIED | `AllNullSection_ProducesIdenticalOrderToLegacyBaseline` PASS (baseline {12,21}, seed 42); engine konstruksi 1-key→1-call (ShuffleEngine.cs:81-90); partisi/sort NON-RNG → tak geser stream rng; e2e S2 backward-compat hijau |
| 2 | Soal hanya diacak DI DALAM Section-nya (tak melompat antar-Section, SHF-01) | ✓ VERIFIED | `ScopedShuffle_NoCrossSectionLeak` + `Reshuffle_SectionIsolation` PASS; partisi BEFORE sampling (ShuffleEngine.cs:74-90, SlicePackagesBySection:129-147); e2e S1 runtime: DB `ShuffledQuestionIds` == DOM, blok kontigu, no interleave, Sec1 mendahului Sec2 |
| 3 | Urutan global = blok per-Section urut SectionNumber, "Lainnya" selalu terakhir (D-15) | ✓ VERIFIED | `SectionOrder_LainnyaAlwaysLast` PASS; `OrderBy(k => k==LainnyaKey).ThenBy(k => k)` (ShuffleEngine.cs:77-78) via SectionStructureComparer; e2e S1 assert "Lainnya" terakhir |
| 4 | Induk ShuffleQuestions OFF → semua Section terurut (toggle per-Section diabaikan, D-14); ON → tiap Section ikut ShuffleEnabled-nya | ✓ VERIFIED | `Precedence_ParentOff_AllOrdered` + `Precedence_ParentOn_PerSectionToggle` PASS; `sectionShuffle = shuffleQuestions && ResolveSectionShuffle(...)` (ShuffleEngine.cs:87, :156-164) |
| 5 | >1 paket: tiap Section diisi gabungan Section padanan lintas-paket, di-sampling dalam batas Section, cakupan ET per-Section (SHF-03) | ✓ VERIFIED | `MultiPackage_EtCoveragePerSection` + `EtSpanningSections_CoveredIndependently` PASS; BuildCrossPackageAssignment (ET-aware Phase 1/2/3) dipanggil per-slice section (ShuffleEngine.cs:103, :219-360) |
| 6 | workerIndex + seed sama → urutan sama (determinisme, SHF-03/04) | ✓ VERIFIED | `Determinism_WorkerIndexStable` PASS; workerIndex dari sibling-set terurut identik StartExam/Reshuffle (cross-call parity) |
| 7 | Option-shuffle di-gate per-Section ShuffleEnabled (D-416-01); 3+ call-site runtime aktif (StartExam/EagerAssign/Reshuffle*) drift-free | ✓ VERIFIED | `OptionShuffle_GatedPerSection` PASS; BuildSectionAwareOptionShuffle (ShuffleEngine.cs:193-206); 4 lokasi load `.ThenInclude(q => q.Section)` + 5 `BuildSectionAwareOptionShuffle` call (CMP:1142, AAC:2559/6051/6167) seragam |

**Score:** 7/7 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Helpers/ShuffleEngine.cs` | Section-partition di BuildQuestionAssignment + BuildSectionQuestionAssignment + gate option per-Section | ✓ VERIFIED | Substantive (142 LOC delta). `BuildSectionQuestionAssignment` (:100), `SlicePackagesBySection` (:129), `ResolveSectionShuffle` (:156), `BuildSectionAwareOptionShuffle` (:193). Signature publik `BuildQuestionAssignment(...)` DIPERTAHANKAN. `BuildCrossPackageAssignment` canonical tak ditulis ulang. Wired ke 4 call-site. |
| `HcPortal.Tests/SectionScopedShuffleTests.cs` | Suite unit baru: golden-order, isolasi, precedence, pooling, determinisme, ET | ✓ VERIFIED | 345 LOC, 10 metode `[Fact]` substantif (asersi penuh, bukan stub). 10/10 PASS (di-run independen). Fixture `PkgSec(...)` + `AllNullFixture()` + `GoldenOrderBaseline {12,21}`. |
| `Controllers/CMPController.cs` | StartExam load Section + BuildSectionAwareOptionShuffle | ✓ VERIFIED | `.ThenInclude(q => q.Section)` ×2 (:1054 assignment block, :1090 re-guard 415); BuildSectionAwareOptionShuffle (:1142) menerima assignedQuestions ter-filter. workerIndex/re-guard 415 tak berubah. |
| `Controllers/AssessmentAdminController.cs` | ReshufflePackage + ReshuffleAll + CreateEagerAssignmentsAsync load Section + gate option + ViewBag.SectionEtWarnings | ✓ VERIFIED | `.ThenInclude(q => q.Section)` di EagerAssign (:2550), ReshufflePackage (:6029), ReshuffleAll (:6111); 3 `BuildSectionAwareOptionShuffle` (:2559/:6051/:6167); ViewBag.SectionEtWarnings + record SectionEtWarning (:7673-7686). |
| `Views/Admin/ManagePackageQuestions.cshtml` | Render alert ET-coverage per-Section (non-blocking, BI) | ⚠️ HOLLOW (render path dead) | Render block ada (:182-200, alert-warning, teks "Elemen Teknis", non-blocking — no disabled/gate). Substantive code, WIRED ke ViewBag. NAMUN data source (predikat `DistinctEt > K` AAC:7680) tak pernah true untuk single-package data → alert tak pernah render (DEF-416-01). Lihat Data-Flow Trace. |
| `tests/e2e/scoped-shuffle.spec.ts` | Playwright e2e: isolasi section + backward-compat + ET-warning + DB lifecycle | ✓ VERIFIED | 538 LOC, 5 `test(` (S1/S2/S3/S3b/S4). db.backup/restore, mode:'serial', createAssessmentViaWizard. Orchestrator: 5/5 green @5277 (×2 deterministik). DB restored bersih (0 SCOPED416). |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | --- | --- | ------ | ------- |
| ShuffleEngine.BuildQuestionAssignment | BuildSectionQuestionAssignment | partisi by KeyOf(q.Section?.SectionNumber) → loop concat | ✓ WIRED | ShuffleEngine.cs:74-90 partisi + :88 callee |
| ShuffleEngine partisi | SectionStructureComparer.KeyOf/LainnyaKey | null-safe key grup Lainnya | ✓ WIRED | :75, :77, :135, :158 |
| CMPController.StartExam load block | ShuffleEngine.BuildQuestionAssignment (partisi pakai q.Section) | `.ThenInclude(q => q.Section)` | ✓ WIRED | CMP:1054 → engine partisi benar (bukan jatuh "Lainnya" palsu) |
| AAC ManagePackageQuestions GET | ManagePackageQuestions.cshtml | ViewBag.SectionEtWarnings | ⚠️ PARTIAL | Set (AAC:7673) + dirender (view:184), tapi list SELALU kosong (predikat unreachable, DEF-416-01) → alert tak render |
| CreateEagerAssignmentsAsync (AddParticipantsLive) | engine section-aware (SAMA dgn StartExam) | `.ThenInclude(q => q.Section)` + BuildSectionAwareOptionShuffle | ✓ WIRED | AAC:2550/:2559 — drift-free (Pitfall 5 ditutup) |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| ManagePackageQuestions.cshtml (alert ET) | `ViewBag.SectionEtWarnings` | AAC:7673 `.Where(w => w.DistinctEt > w.K)` di mana K=COUNT(soal Section), DistinctEt=distinct ET soal Section SAMA | ✗ NO — list selalu kosong | ⚠️ STATIC/HOLLOW — tiap soal = 1 string ET → DistinctEt ≤ K SELALU → predikat `> K` tak pernah true → alert dead-code (DEF-416-01) |
| StartExam render | `ShuffledQuestionIds` (UPA) → DOM qcard | engine BuildQuestionAssignment (real partition) | ✓ YES — e2e S1: DB == DOM, blok-per-section | ✓ FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Golden-order backward-compat (SHF-04) | `dotnet test --filter SectionScopedShuffle` | AllNullSection...LegacyBaseline PASS | ✓ PASS |
| Section isolation (SHF-01) | dotnet test (no-build) | ScopedShuffle_NoCrossSectionLeak + LainnyaAlwaysLast PASS | ✓ PASS |
| Precedence + option gate (SHF-02) | dotnet test (no-build) | Precedence_ParentOff_AllOrdered + OptionShuffle_GatedPerSection PASS | ✓ PASS |
| Engine + legacy suite (regresi) | `dotnet test --filter "ShuffleEngine\|SectionScopedShuffle" --no-build` | **26/26 PASS** (16 legacy Phase 373 + 10 baru) | ✓ PASS |
| Full suite (orchestrator) | `dotnet test HcPortal.Tests` | 665/665 PASS (claimed by orchestrator) | ✓ PASS (carry) |
| e2e runtime (orchestrator) | `npx playwright test scoped-shuffle.spec.ts --workers=1` | 5/5 PASS @5277 (claimed) | ✓ PASS (carry) |

_Catatan: `dotnet build`/`dotnet test` full-rebuild gagal HANYA karena app dev jalan @5277 mengunci `HcPortal.exe` (MSB3027 file-lock, env artifact — bukan compile error). Verifikasi via `--no-build` terhadap assembly pra-build: kompile sukses, 26/26 hijau._

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| SHF-01 | 416-01/02/03 | Acak hanya dalam Section; tanpa Section = perilaku lama | ✓ SATISFIED | Truth 1/2/3; engine partition + e2e S1/S2; REQUIREMENTS.md:23,85 mapped Phase 416 |
| SHF-02 | 416-01/02/03 | Toggle acak per-Section (induk/anak) | ✓ SATISFIED | Truth 4/7; ResolveSectionShuffle (D-14) + BuildSectionAwareOptionShuffle (D-416-01); REQUIREMENTS.md:24,86 |
| SHF-03 | 416-01/02/03 | >1 paket pooling per-Section + cakupan ET per-Section | ✓ SATISFIED (warning render = caveat) | Truth 5/6; ET-aware per-slice + e2e. NB: jaminan cakupan ET = ENGINE (terbukti unit); peringatan-UI D-416-03 render dead (DEF-416-01, non-blocking, lihat human_verification). REQUIREMENTS.md:25,87 |
| SHF-04 | 416-01/02/03 | Reshuffle (per-paket & semua peserta) hormati batas Section | ✓ SATISFIED | Truth 1/2/7; golden-order + 3 reshuffle call-site uniform + Reshuffle_SectionIsolation; REQUIREMENTS.md:26,88 |

**Orphaned requirements:** NONE. REQUIREMENTS.md maps exactly SHF-01..04 to Phase 416 (line 100: "416 = 4 (SHF-01..04)"); all 4 declared in PLAN frontmatter and verified.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| Controllers/AssessmentAdminController.cs | 7680 | Predikat `DistinctEt > w.K` unreachable (filter selalu kosong → ViewBag list empty) | ⚠️ Warning | Alert ET-coverage tak pernah render (DEF-416-01). NON-BLOCKING by design; INTI D-416-03 (Section sempit tetap boleh) terbukti S3/S3b. Cosmetic dead-nicety, bukan kerusakan/keamanan. |
| Helpers/ShuffleEngine.cs | 102, 109, 118 | `return new List<int>()` guard | ℹ️ Info | Defensif by design (slice kosong → skip, D-416-03 best-effort) — BUKAN stub; jalur normal mengisi list. |

### Human Verification Required

#### 1. ET-coverage warning render (sisi-positif) — DEF-416-01

**Test:** Buat Section dengan cakupan ET melebihi kuota soal → harapkan alert kuning "Elemen Teknis" tampil di panel Section.
**Expected:** Alert non-blocking muncul sebagai sinyal ke HC.
**Why human:** Predikat saat ini (`DistinctEt > K`, K = COUNT soal Section single-package) **tak terjangkau data nyata** — tiap soal menyimpan 1 string ET → DistinctEt ≤ K SELALU → alert TIDAK PERNAH render. Ini **bukan bug fungsional/keamanan**: warning = sinyal NON-BLOCKING, dan INTI load-bearing D-416-03 ("Section sempit tetap boleh mulai/kelola") sudah terbukti runtime (e2e S3 + kontrol-negatif S3b). Memperbaiki butuh **keputusan desain** (re-spec predikat: distinct ET lintas paket-saudara vs K-min yang dipresentasikan) — bukan auto-fixable.
**Opsi keputusan:**
- (a) **Terima sebagai dead-nicety** → defer ke Phase 419 (Export+Test/UAT) atau backlog. Tujuan fase (isolasi section runtime) sudah tercapai tanpa warning ini.
- (b) **Re-spec predikat sekarang** → angkat sebagai gap kecil ke `/gsd-plan-phase 416 --gaps`.

#### 2. UAT live browser @5277 (checkpoint Plan 03 Task 2)

**Test:** Jalankan app @5277, lakukan langkah 1-7 di 416-03-PLAN Task 2 (isolasi section visual, precedence D-14 induk OFF / Section-OFF, backward-compat all-null, ET non-blocking, cek `UserPackageAssignment.ShuffledQuestionIds` = blok-per-section).
**Expected:** Soal teracak DALAM Section (Pompa blok lalu Valve, tak lompat); induk OFF → semua terurut; 1 Section Acak=OFF → terurut; all-null = perilaku lama; tak ada error render.
**Why human:** Plan 03 Task 2 adalah `checkpoint:human-verify` (gate blocking) yang **di-AUTO-SATISFY oleh autopilot §5** (Playwright real-chromium green sebagai bukti runtime), BUKAN oleh mata-manusia. Razor/JS/SignalR render WAJIB dikonfirmasi manusia pra-ship (lesson 354 — runtime-smoke tak selalu nangkap). e2e 5/5 hijau menutup risiko teknis; konfirmasi visual akhir tetap rekomendasi sebelum ship ke Dev.

### Gaps Summary

**TIDAK ADA gap yang memblokir tujuan fase.** Tujuan 416 — "pengacakan soal & pilihan terjadi hanya di dalam lingkup Section (soal tak melompat antar-Section), dengan assessment tanpa Section berperilaku persis seperti sekarang" — **TERCAPAI penuh dan terbukti** di tiga tingkat: (1) engine murni (26/26 unit incl golden-order byte-identik {12,21}), (2) wiring 4 call-site seragam (StartExam/EagerAssign/ReshufflePackage/ReshuffleAll semua load q.Section + BuildSectionAwareOptionShuffle, drift-free), (3) runtime real-browser (e2e S1: DB ShuffledQuestionIds == DOM, blok-per-section, no interleave; S2 backward-compat). migration=FALSE dikonfirmasi (0 perubahan Migrations/Data/Models — reuse kolom 415).

**Satu caveat non-blocking (DEF-416-01):** peringatan cakupan ET (D-416-03) memakai predikat `DistinctEt > K` yang tak terjangkau data single-package → alert sisi-positif tak pernah render (dead-nicety). Ini NON-BLOCKING by design; sub-requirement INTI SHF-03 (jaminan cakupan ET) dipenuhi oleh ENGINE (terbukti unit `MultiPackage_EtCoveragePerSection`/`EtSpanningSections_CoveredIndependently`), bukan oleh warning. INTI load-bearing D-416-03 (non-blocking) terbukti runtime (S3) + tanpa false-positive (S3b). Diangkat sebagai **human_verification item** untuk keputusan owner (terima dead-nicety vs re-spec), BUKAN gap yang memaksa revision-loop.

Status **human_needed** (bukan gaps_found) karena: semua 7 truth VERIFIED, semua REQ SATISFIED, tak ada blocker — namun ada 2 item yang butuh keputusan/konfirmasi manusia (DEF-416-01 disposition + UAT visual checkpoint yang di-auto-satisfy autopilot).

---

_Verified: 2026-06-23T12:08:02Z_
_Verifier: Claude (gsd-verifier)_
