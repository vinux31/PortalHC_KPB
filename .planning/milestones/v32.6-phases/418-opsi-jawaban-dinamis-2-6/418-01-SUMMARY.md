---
phase: 418-opsi-jawaban-dinamis-2-6
plan: 01
subsystem: testing
tags: [xunit, validation, options, edit-shrink-guard, tdd, red]

# Dependency graph
requires:
  - phase: 386-pxf-02
    provides: "QuestionOptionValidator.ValidateQuestionOptions (min-2 + correct-must-have-text) — analog yang di-extend ke max-6"
provides:
  - "4 Fact xUnit OPT-03 max-6 (RED): MaxSix_Rejected + FiveOptions_Accepted + SixOptions_Accepted + SixOpt_CorrectWithoutText_Rejected di OptionValidationTests.cs"
  - "4 Fact xUnit D-418-02 edit-shrink guard pure-logic (RED): EditShrinkGuardLogicTests.cs memanggil OptionShrinkGuard.FindBlockedOptionIds"
  - "Helpers/OptionShrinkGuard.cs STUB (NotImplementedException) — kontrak signature terkunci untuk Plan 418-02 GREEN"
affects: [418-02-validator-guard-production, opsi-dinamis-render, edit-shrink-controller-wiring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "RED Wave 0 via stub helper (compile hijau + runtime NotImplementedException) — analog OptionValidationTests Phase 386"
    - "Pure-logic guard test (irisan set) tanpa DbContext — isolasi keputusan dari query EF"

key-files:
  created:
    - "HcPortal.Tests/EditShrinkGuardLogicTests.cs"
    - "Helpers/OptionShrinkGuard.cs (STUB)"
  modified:
    - "HcPortal.Tests/OptionValidationTests.cs (+4 Fact)"

key-decisions:
  - "Pakai STUB OptionShrinkGuard.cs (NotImplementedException) bukan symbol-missing — agar test project COMPILE + RED runtime (sesuai critical_constraints harness)"
  - "Kontrak signature locked: FindBlockedOptionIds(IEnumerable<int> removed, IEnumerable<int> answered) -> IReadOnlyList<int> = irisan distinct"
  - "Pesan validator di-assert sesuai UI-SPEC C5: 'Maksimal 6 opsi' + 'berisi teks' (correct-tanpa-teks existing)"

patterns-established:
  - "Wave 0 RED: tulis test+stub dulu, kunci kontrak, biarkan merah sampai wave produksi (GREEN)"

requirements-completed: []  # OPT-03 tested (RED) tapi BELUM complete — GREEN di 418-02. JANGAN mark-complete di plan ini.

# Metrics
duration: 6min
completed: 2026-06-24
---

# Phase 418 Plan 01: Test Wave-0 (RED) Validator Max-6 + Edit-Shrink Guard Logic Summary

**8 Fact xUnit RED yang mengunci kontrak Fase 418 sebelum produksi ada: 4 Fact OPT-03 max-6 di OptionValidationTests + 4 Fact pure-logic edit-shrink guard (irisan set) di EditShrinkGuardLogicTests, dengan stub OptionShrinkGuard.cs yang mengunci signature untuk Plan 02 GREEN.**

## Performance

- **Duration:** ~6 min
- **Started:** 2026-06-24T02:32:41Z
- **Completed:** 2026-06-24T02:39:39Z
- **Tasks:** 2/2
- **Files modified:** 3 (1 extend, 2 baru)

## Accomplishments

- **Task 1 (OPT-03 max-6):** 4 Fact baru di `OptionValidationTests.cs`. `MaxSix_Rejected` RED (validator belum punya cek `filled > 6`); `FiveOptions_Accepted`/`SixOptions_Accepted`/`SixOpt_CorrectWithoutText_Rejected` hijau (validator length-agnostik). 7 Fact lama (min-2 + correct-tanpa-teks + Essay bypass) tetap hijau (regresi).
- **Task 2 (D-418-02 edit-shrink guard):** 4 Fact pure-logic di `EditShrinkGuardLogicTests.cs` memanggil `OptionShrinkGuard.FindBlockedOptionIds(removed, answered)` — menguji irisan set (blocked / allowed / nothing-removed / intersection-only). Semua RED via stub `NotImplementedException`.
- **Kontrak terkunci:** `Helpers/OptionShrinkGuard.cs` stub dengan signature + XML-doc yang Plan 418-02 WAJIB implementasikan (body irisan distinct nyata) tanpa mengubah signature.

## Task Commits

Each task was committed atomically (TDD RED gate):

1. **Task 1: Extend OptionValidationTests — max-6 reject + 5/6-opsi accept + correct-tanpa-teks (array-6)** — `029aaa0d` (test)
2. **Task 2: New EditShrinkGuardLogicTests — pure intersection logic (D-418-02)** — `2431b02b` (test)

**Plan metadata:** _(this commit)_ (docs: complete plan)

_Note: TDD Wave 0 — kedua commit adalah `test(...)` RED gate. GREEN (`feat`) menyusul di Plan 418-02._

## Files Created/Modified

- `HcPortal.Tests/OptionValidationTests.cs` — +4 Fact OPT-03 (max-6 reject + 5/6-opsi accept + correct-tanpa-teks array-6). +62 baris.
- `HcPortal.Tests/EditShrinkGuardLogicTests.cs` — BARU, 4 Fact pure-logic D-418-02 (irisan `removedOptionIds ∩ answeredOptionIds`).
- `Helpers/OptionShrinkGuard.cs` — BARU (STUB). `FindBlockedOptionIds(...)` throw `NotImplementedException`; XML-doc mengunci kontrak untuk Plan 418-02.

## RED Evidence (MUST-fail verification)

**Task 1** (`dotnet test --filter "FullyQualifiedName~OptionValidation"`):
```
Failed HcPortal.Tests.OptionValidationTests.MaxSix_Rejected [8 ms]
  Assert.False() Failure — Expected: False, Actual: True   ← validator menerima 7 opsi (belum ada cek filled>6)
Failed!  - Failed: 1, Passed: 10, Skipped: 0, Total: 11
```
RED reason BENAR: `MaxSix_Rejected` gagal karena aturan max-6 belum ada di produksi. 10 lainnya hijau (termasuk 5/6-opsi + correct-tanpa-teks-6 + 7 Fact lama) — sesuai prediksi plan.

**Task 2** (`dotnet test --filter "FullyQualifiedName~EditShrinkGuard"`):
```
Failed ... Blocked_WhenRemovedOptionWasAnswered / Allowed_WhenNoRemovedOptionAnswered /
       Allowed_WhenNothingRemoved / Blocked_ListContainsOnlyIntersection
  System.NotImplementedException : OptionShrinkGuard.FindBlockedOptionIds belum diimplementasikan — diisi di Plan 418-02 (GREEN).
Failed!  - Failed: 4, Passed: 0, Skipped: 0, Total: 4
```
RED reason BENAR: project COMPILE hijau (stub ada), 4/4 throw `NotImplementedException` saat runtime = true RED.

**Full suite** (`dotnet test HcPortal.Tests`): **Total: 683, Passed: 678, Failed: 5, Skipped: 0** — 5 failure persis = 1 (MaxSix) + 4 (EditShrinkGuard), semua dari plan ini. Tidak ada regresi pada 678 test existing (baseline 416/417 utuh).

## TDD Gate Compliance

Plan `type: tdd`, Wave 0 (RED). Gate sequence untuk plan ini = **RED only** (commit `test(...)`):
- ✅ RED gate: 2 commit `test(418-01)` dengan bukti MUST-fail terdokumentasi di atas.
- GREEN gate (`feat`) + REFACTOR: **TIDAK** di plan ini — tanggung jawab Plan 418-02 yang mengisi body `OptionShrinkGuard.FindBlockedOptionIds` (+ `if (filled > 6)` di `QuestionOptionValidator`). Ini ekspektasi Wave 0 (lihat 418-VALIDATION.md), bukan gate-miss.

## Decisions Made

- **STUB vs symbol-missing (Task 2):** Plan menawarkan dua opsi RED. Dipilih **STUB** `Helpers/OptionShrinkGuard.cs` (`NotImplementedException`) — sesuai `critical_constraints`/`tdd_red_note` prompt (harness perlu test project COMPILE). Hasil: build hijau + 4 test throw runtime = true RED. Plan 418-02 mengganti body stub (file ini ada di `files_modified` 418-02).
- **Kontrak signature locked:** `public static IReadOnlyList<int> FindBlockedOptionIds(IEnumerable<int> removedOptionIds, IEnumerable<int> answeredOptionIds)` = irisan distinct. Empty = boleh hapus. Plan 418-02 WAJIB tidak mengubah signature ini (test sudah pin).

## Deviations from Plan

None - plan executed exactly as written. (Pilihan STUB Task 2 adalah opsi yang DISEDIAKAN plan + diarahkan prompt critical_constraints, bukan deviasi.)

## Known Stubs

| Stub | File | Line | Reason / Resolusi |
|------|------|------|-------------------|
| `OptionShrinkGuard.FindBlockedOptionIds` throw `NotImplementedException` | `Helpers/OptionShrinkGuard.cs` | ~39 | **Sengaja** — RED Wave 0. Body irisan distinct nyata diisi **Plan 418-02 (GREEN)**. Tidak ada caller produksi (controller wiring juga 418-02). Tidak menghalangi tujuan fase — ini gate RED yang dimaksud.

## Issues Encountered

None. Kedua task RED sesuai prediksi pada percobaan pertama.

## User Setup Required

None - no external service configuration required. migration=FALSE (tidak menyentuh Migrations/ atau Data/; verified via git diff — hanya 2 file test + 1 helper stub).

## Next Phase Readiness

- **Plan 418-02 (GREEN) siap dieksekusi.** Kontrak yang harus dipenuhi:
  1. `QuestionOptionValidator.ValidateQuestionOptions`: tambah `if (filled > 6) return (false, "Maksimal 6 opsi per soal.");` setelah cek `filled < 2` → `MaxSix_Rejected` hijau.
  2. `Helpers/OptionShrinkGuard.cs`: ganti body stub `FindBlockedOptionIds` dengan irisan distinct (`removedOptionIds.Intersect(answeredOptionIds).Distinct().ToList()` atau setara) → 4 `EditShrinkGuardLogicTests` hijau. Signature TIDAK boleh berubah.
  3. Wire `OptionShrinkGuard` di `EditQuestion` POST controller (pre-SaveChanges) + hapus guard H3 (`:7972`) per 418-RESEARCH §Edit-Shrink Guard.
- Tidak ada blocker. Suite akan 683/683 hijau setelah 418-02 GREEN.

## Self-Check: PASSED

- Files: `HcPortal.Tests/OptionValidationTests.cs`, `HcPortal.Tests/EditShrinkGuardLogicTests.cs`, `Helpers/OptionShrinkGuard.cs`, `.planning/phases/418-opsi-jawaban-dinamis-2-6/418-01-SUMMARY.md` — all FOUND.
- Commits: `029aaa0d`, `2431b02b` — both FOUND.
- migration guard: no `Migrations/` or `Data/` touched (migration=FALSE preserved).

---
*Phase: 418-opsi-jawaban-dinamis-2-6*
*Completed: 2026-06-24*
