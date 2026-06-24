---
phase: 416-scoped-shuffle-acak-per-section
plan: 01
subsystem: assessment
tags: [shuffle, section, scoped-shuffle, pure-engine, refactor, elemen-teknis, backward-compat, xunit]

# Dependency graph
requires:
  - phase: 415-section-foundation-import-excel-diperluas
    provides: "AssessmentPackageSection.ShuffleEnabled (kolom per-section) + PackageQuestion.SectionId (FK nullable) + SectionStructureComparer (LainnyaKey null-safe)"
  - phase: 373 (v27.0 shuffle toggle)
    provides: "Helpers/ShuffleEngine.cs (BuildCrossPackageAssignment ET-aware canonical + BuildQuestionAssignment + BuildOptionShuffle + Shuffle<T>); ShuffleEngineTests.cs golden-order contract"
provides:
  - "ShuffleEngine.BuildQuestionAssignment kini SECTION-AWARE: partisi soal lintas-paket by SectionNumber → distribusi per-Section → concat urut, grup 'Lainnya' selalu terakhir (D-15)"
  - "Sub-fungsi BuildSectionQuestionAssignment (private) — algoritma distribusi existing per-slice section (ON canonical / OFF urut Order)"
  - "BuildSectionAwareOptionShuffle (public) — gate opsi per-Section (induk ∧ ShuffleEnabled, D-416-01); soal Section-OFF fallback DB-order"
  - "Helper privat SlicePackagesBySection (shallow, no deep clone EF) + ResolveSectionShuffle (precedence D-14)"
  - "Golden-order invariant: all-null = byte-identik baseline pra-416 (kontrak backward-compat SHF-04)"
affects: [417-section-pagination, 419-export-test-uat-milestone, "Plan 02 (wiring StartExam/Reshuffle*/EagerAssign ke jalur section-aware + load q.Section)"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Section-partition wrapper: entry signature dipertahankan, partisi+concat di DALAM (blast-radius minimal untuk 3 call-site)"
    - "Golden-order by construction: 1 grup tunggal → sub-fungsi dipanggil SEKALI = output identik baseline; partisi/sort NON-RNG (no stream drift)"
    - "Per-section gate via SectionStructureComparer.KeyOf (null-safe LainnyaKey); reuse Phase 415 comparer, no Dictionary<int?,...>"

key-files:
  created:
    - "HcPortal.Tests/SectionScopedShuffleTests.cs (suite unit baru, 10 metode, pure no-DB)"
  modified:
    - "Helpers/ShuffleEngine.cs (section-partition refactor + 4 helper baru)"

key-decisions:
  - "Pertahankan signature publik BuildQuestionAssignment (Open Q #1) — partisi di dalam; 3 call-site Plan 02 nyaris tak berubah"
  - "Partisi via q.Section?.SectionNumber (navigasi); call-site produksi Plan 02 WAJIB load q.Section. Fallback aman: Section null → grup Lainnya (perilaku global lama)"
  - "ResolveSectionShuffle default true bila Section tak ter-load → gate akhir = induk (degradasi anggun, tak crash)"
  - "Guard slice kosong → skip section (return empty), JANGAN throw (D-416-03 best-effort, Open Q #2)"
  - "GoldenOrderBaseline {12, 21} di-capture dari engine Phase 373 (seed 42) sebagai konstanta beku, BUKAN akses BuildCrossPackageAssignment private"

patterns-established:
  - "Pattern: Section-scoped randomization — partition BEFORE sampling (cegah leak antar-section, Pitfall 1); concat urut SectionNumber, Lainnya terakhir"
  - "Pattern: Backward-compat golden-order regression test — baseline beku list literal + Assert.Equal pada jalur all-null"

requirements-completed: [SHF-01, SHF-02, SHF-03, SHF-04]

# Metrics
duration: 15min
completed: 2026-06-23
---

# Phase 416 Plan 01: Scoped Shuffle (Acak per-Section) Engine Summary

**ShuffleEngine.BuildQuestionAssignment kini mempartisi soal lintas-paket per-Section (kunci komposit (SectionNumber, ET)) lalu menjalankan distribusi ET-aware existing per-slice + concat urut dengan grup "Lainnya" terakhir — assessment tanpa Section tetap byte-identik baseline pra-416 (golden-order).**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-06-23T10:27:27Z
- **Completed:** 2026-06-23T10:45:00Z (approx)
- **Tasks:** 2 (TDD pair RED → GREEN)
- **Files modified:** 1 created + 1 modified

## Accomplishments
- `Helpers/ShuffleEngine.cs` di-refactor jadi **section-aware** TANPA mengubah signature publik `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` — 3 call-site (StartExam / Reshuffle* / EagerAssign) tak terdampak; wiring di Plan 02.
- Sub-fungsi `BuildSectionQuestionAssignment` membungkus algoritma distribusi lama per-slice Section (ON → `BuildCrossPackageAssignment` VERBATIM; OFF → urut `Question.Order` / round-robin ≥2 paket). Algoritma inti canonical TIDAK ditulis ulang.
- `BuildSectionAwareOptionShuffle` meng-gate acak opsi per-Section (induk ∧ `ShuffleEnabled`, D-416-01); soal di Section OFF tak masuk dict → caller fallback DB-order. Grup "Lainnya" ikut induk (D-15).
- Precedence induk/anak (D-14) via `ResolveSectionShuffle`; grup "Lainnya" (SectionId=null) selalu di urutan TERAKHIR (D-15) via `SectionStructureComparer.KeyOf` (null-safe).
- **Golden-order backward-compat (SHF-04) terbukti:** all-null → 1 grup tunggal "Lainnya" → sub-fungsi dipanggil SEKALI = output byte-identik baseline beku `{12, 21}` (seed 42). Partisi/sort NON-RNG → tak menggeser stream `rng` (Pitfall 2 dihindari).
- Suite test baru `SectionScopedShuffleTests.cs` (10 metode) membuktikan SHF-01/02/03/04 di tingkat engine murni; suite lama `ShuffleEngineTests.cs` (Phase 373, 16 metode golden-order) TETAP HIJAU + tak disentuh.

## Task Commits

Each task committed atomically (TDD RED → GREEN):

1. **Task 1: Wave-0 scaffold SectionScopedShuffleTests.cs (RED + golden-order baseline)** - `9a135d89` (test)
2. **Task 2: Refactor ShuffleEngine section-partition + BuildSectionQuestionAssignment + gate option per-Section (GREEN)** - `0094996a` (feat)

_TDD pair: test commit (RED) → feat commit (GREEN). REFACTOR gate tidak diperlukan (kode sudah bersih saat GREEN)._

## Files Created/Modified
- `HcPortal.Tests/SectionScopedShuffleTests.cs` (CREATED) — 10 metode unit pure: `AllNullSection_ProducesIdenticalOrderToLegacyBaseline`, `ScopedShuffle_NoCrossSectionLeak`, `SectionOrder_LainnyaAlwaysLast`, `Precedence_ParentOff_AllOrdered`, `Precedence_ParentOn_PerSectionToggle`, `OptionShuffle_GatedPerSection`, `MultiPackage_EtCoveragePerSection`, `EtSpanningSections_CoveredIndependently`, `Determinism_WorkerIndexStable`, `Reshuffle_SectionIsolation`. Fixture `PkgSec(...)` ber-section + `AllNullFixture()` + `GoldenOrderBaseline {12, 21}`.
- `Helpers/ShuffleEngine.cs` (MODIFIED) — `BuildQuestionAssignment` jadi partition+concat wrapper; tambah `BuildSectionQuestionAssignment` (private), `SlicePackagesBySection` (private), `ResolveSectionShuffle` (private), `BuildSectionAwareOptionShuffle` (public). XML-doc header di-update sebut scoping per-Section + invariant golden-order. `BuildCrossPackageAssignment` (canonical) tak diubah.

## Decisions Made
- **Pertahankan signature publik** (Open Q #1, planner discretion): partisi diinjeksi di DALAM entry-point → 3 call-site Plan 02 nyaris tak berubah (hanya pastikan `q.Section` ter-load). Alternatif (entry baru) ditolak — risiko drift workerIndex + blast-radius lebih besar.
- **Partisi via `q.Section?.SectionNumber`** (navigasi). Plan 02 WAJIB memuat `q.Section` (atau menyediakan map). Fallback aman: bila `q.Section` null, soal jatuh ke grup "Lainnya" (perilaku global lama, tak crash) — didokumentasikan di XML-doc + SUMMARY untuk Plan 02.
- **`GoldenOrderBaseline {12, 21}`** di-capture dari engine Phase 373 (seed 42) via test temporer (dihapus sebelum commit), BUKAN dengan mengakses `BuildCrossPackageAssignment` private. Konstanta beku = kontrak SHF-04.
- **Guard defensif slice kosong → skip (no throw)** (Open Q #2, D-416-03 best-effort) — sama pola `K==0` existing.

## Deviations from Plan

None - plan executed exactly as written.

Catatan TDD-ordering (bukan deviasi): Task 1 (test scaffold) sengaja merujuk `BuildSectionAwareOptionShuffle` yang baru ditambahkan Task 2, sehingga build test project belum hijau saat commit Task 1 (state RED murni — satu compile-error `CS0117` sebagai driver). Ini sesuai semantik TDD `tdd="true"` di plan (Task 1 `<action>`: "gunakan API gate yang ditambah Task 2"). Task 2 (GREEN) menutup gerbang: build hijau + 26/26 engine test + 665/665 full suite. Tidak ada perubahan scope.

## Issues Encountered
- **Baseline awal salah-tebak:** konstanta `GoldenOrderBaseline` mula-mula di-tulis `{11, 20}` (tebakan). Di-capture nilai SEBENARNYA `{12, 21}` dari engine pra-refactor lewat test temporer ber-`ITestOutputHelper` (verbosity detailed), lalu konstanta diperbaiki + file temporer dihapus. Terverifikasi: `AllNullSection_ProducesIdenticalOrderToLegacyBaseline` GREEN.

## Verification
- `dotnet build HcPortal.Tests` → 0 error (3 warning pre-existing, bukan dari plan ini).
- `dotnet test --filter "FullyQualifiedName~ShuffleEngine|FullyQualifiedName~SectionScopedShuffle"` → **26/26 PASS** (16 legacy golden-order + 10 baru).
- Full suite `dotnet test HcPortal.Tests` → **665/665 PASS, 0 fail** (no regresi).
- `git diff --stat HcPortal.Tests/ShuffleEngineTests.cs` → kosong (test lama tak diubah).
- migration=FALSE dipertahankan: `git status Migrations/ Data/ Models/` → 0 perubahan (reuse `AssessmentPackageSection.ShuffleEnabled` dari 415).

## TDD Gate Compliance
- RED gate: `test(416-01)` `9a135d89` ✓
- GREEN gate: `feat(416-01)` `0094996a` ✓ (setelah RED)
- REFACTOR gate: tidak diperlukan (kode bersih saat GREEN).

## Next Phase Readiness
- **Engine section-aware siap untuk Plan 02 (wiring).** Plan 02 WAJIB:
  1. Pastikan `q.Section` ter-`Include` (`.ThenInclude(q => q.Section)`) di **3 call-site** load: `CMPController.StartExam` (~L1050), `AssessmentAdminController.CreateEagerAssignmentsAsync` (~L2546), `ReshufflePackage`/`ReshuffleAll` (~L6022/L6102) — kalau tidak, partisi senyap jatuh ke "Lainnya" (Pitfall 3).
  2. Ganti `BuildOptionShuffle(...)` → `BuildSectionAwareOptionShuffle(assignedQuestions, assessment.ShuffleOptions, rng)` di ketiga call-site (gate opsi per-Section, D-416-01).
  3. ET-coverage warning (D-416-03) di `ManagePackageQuestions` GET + view (non-blocking) — bila dijadwalkan di Plan 02/03.
- Tidak ada blocker. migration=FALSE → notify IT saat handoff (commit hash + flag).
- Phase 417 (pagination) dapat ekstrak abstraksi urutan-soal bila perlu; 416 sengaja TIDAK over-abstract.

## Self-Check: PASSED
- Files: HcPortal.Tests/SectionScopedShuffleTests.cs ✓, Helpers/ShuffleEngine.cs ✓, 416-01-SUMMARY.md ✓
- Commits: 9a135d89 (test/RED) ✓, 0094996a (feat/GREEN) ✓

---
*Phase: 416-scoped-shuffle-acak-per-section*
*Completed: 2026-06-23*
