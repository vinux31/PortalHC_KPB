---
phase: 418-opsi-jawaban-dinamis-2-6
plan: 02
subsystem: api
tags: [aspnet-mvc, model-binding, options, edit-shrink-guard, fk-restrict, authoring, controller]

# Dependency graph
requires:
  - phase: 418-01
    provides: "OptionShrinkGuard.cs STUB (signature terkunci) + 5 RED test (OptionValidation MaxSix + 4 EditShrinkGuard)"
  - phase: 386-pxf-02
    provides: "QuestionOptionValidator.ValidateQuestionOptions (min-2 + correct-must-have-text) — di-extend max-6"
  - phase: 415
    provides: "data model PackageOption dinamis (tanpa field huruf) + import A–F; soal 5–6 opsi yang kini editable"
provides:
  - "CreateQuestion/EditQuestion POST kontrak baru: List<OptionInput> options + int? correctIndex (≤6)"
  - "ResolveCorrectness helper — MC single-select via correctIndex (flag #1 keystone); MA per-checkbox IsCorrect"
  - "OptionShrinkGuard.FindBlockedOptionIds body nyata (irisan distinct) + wiring di EditQuestion pre-SaveChanges (D-418-02, tutup 999.14)"
  - "QuestionOptionValidator max-6 (OPT-03)"
  - "Guard H3 (tolak edit >4 opsi) DIHAPUS — soal 5–6 opsi editable (OPT-01)"
  - "Loop upsert opsi dinamis A–F preserve Id + gambar"
affects: [418-03-view-js-markup, 418-04-e2e-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "List<OptionInput> indexed model binding (options[i].Prop) menggantikan param diskret A–D"
    - "Server-authoritative correctness resolution: MC=correctIndex (single name grup), MA=per-checkbox IsCorrect"
    - "Edit-shrink guard: query existence PackageUserResponse SEBELUM SaveChanges → TempData full-page (bukan FK-Restrict 500)"
    - "Kill-drift: aturan 'opsi dihapus' (i>=keep ATAU Text kosong; Essay=all) IDENTIK di guard + loop upsert"

key-files:
  created:
    - "Models/OptionInput.cs"
  modified:
    - "Helpers/OptionShrinkGuard.cs (stub → body nyata)"
    - "Helpers/QuestionOptionValidator.cs (+max-6)"
    - "Controllers/AssessmentAdminController.cs (CreateQuestion/EditQuestion refactor + ResolveCorrectness + guard edit-shrink + drop H3 + loop A–F + relokasi atribut)"
    - "HcPortal.Tests/SectionCrudTests.cs (2 call site CreateQuestion → kontrak baru)"
    - "HcPortal.Tests/SectionFixRegressionTests.cs (3 call site EditQuestion → kontrak baru; 2 test H3-rejection obsolete di-rewrite jadi NEW behavior)"

key-decisions:
  - "Flag #1 KEYSTONE: MC pakai satu name grup 'correctIndex' (radio native single-select) → controller map index→IsCorrect; MA pakai options[i].IsCorrect checkbox. ResolveCorrectness meng-OVERRIDE IsCorrect untuk MC dari binding."
  - "id baris dinamis tetap option_{letter} (A–F) bukan option_{index} — backward-compat e2e + populateEditForm (flag #2; diserahkan ke Plan 03 markup)."
  - "Pesan edit-shrink sertakan huruf opsi terblok (UI-SPEC C5) — murah, dihitung dari posisi di existingForGuard."
  - "Aturan removed-detection di guard DAN loop upsert dibuat identik (kill-drift) per plan-check #4."

patterns-established:
  - "OptionInput whitelist eksplisit (NO Id) — mitigasi mass-assignment T-418-06"
  - "Image-validate loop dinamis: new[]{questionImage}.Concat(options.Select(o=>o.Image)) — cakup E/F (T-418-04)"

requirements-completed: [OPT-01, OPT-03]

# Metrics
duration: 15min
completed: 2026-06-24
---

# Phase 418 Plan 02: HTTP Contract Refactor ke Opsi Dinamis 2–6 (GREEN) Summary

**Refactor CreateQuestion/EditQuestion POST dari 16+ param diskret A–D menjadi binding `List<OptionInput>` (≤6) + `correctIndex` MC single-select; hapus guard H3 sehingga soal 5–6 opsi import editable; pasang guard edit-shrink (D-418-02) query-existence pre-SaveChanges yang menutup hazard 999.14 (FK-Restrict 500); tambah validator max-6 — 683/683 xUnit GREEN.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-06-24T02:44:52Z
- **Completed:** 2026-06-24T02:59:45Z
- **Tasks:** 3/3
- **Files modified:** 6 (1 baru, 5 modified)

## Accomplishments

- **Task 1 (helper GREEN):** `Models/OptionInput.cs` baru (whitelist Text/IsCorrect/Image/ImageAlt/RemoveImage, NO Id — T-418-06); `OptionShrinkGuard.FindBlockedOptionIds` body nyata (irisan distinct, signature terkunci 418-01); `QuestionOptionValidator` +`filled > 6` → "Maksimal 6 opsi per soal." (OPT-03). 15 test filter GREEN.
- **Task 2 (CreateQuestion):** signature ganti ke `List<OptionInput> options` + `int? correctIndex`; `ResolveCorrectness` (flag #1 keystone — MC index→IsCorrect, MA per-checkbox); image-validate loop dinamis incl E/F; correctCount + validator dari `options.Select(...)`; persist skip baris kosong + `Take(6)`.
- **Task 3 (EditQuestion):** signature sama; **guard H3 DIHAPUS** (soal 5–6 opsi editable — OPT-01); **guard edit-shrink** terpasang pre-SaveChanges (removedOptionIds → `FindBlockedOptionIds` → TempData full-page + redirect, tutup 999.14); loop upsert dinamis A–F preserve Id+gambar dengan aturan removed IDENTIK guard (kill-drift); GET JSON shape utuh.
- **Full xUnit suite 683/683 GREEN** (5 RED→GREEN; 0 regresi).

## Task Commits

Each task was committed atomically:

1. **Task 1: OptionInput model + OptionShrinkGuard + validator max-6 (GREEN)** — `76f0f5ca` (feat)
2. **Task 2: CreateQuestion list-binding opsi dinamis + correctIndex MC** — `b2d1c35b` (feat)
3. **Task 3: EditQuestion list-binding + edit-shrink guard + drop H3 + loop A–F** — `7187a9f7` (feat)

**Plan metadata:** _(commit final docs)_

## Files Created/Modified

- `Models/OptionInput.cs` — BARU. Binding model opsi dinamis, properti di-whitelist eksplisit (no Id, mitigasi mass-assignment T-418-06).
- `Helpers/OptionShrinkGuard.cs` — stub `NotImplementedException` → body `removedOptionIds.Intersect(answeredOptionIds).Distinct().ToList()`.
- `Helpers/QuestionOptionValidator.cs` — tambah `if (filled > 6) return (false, "Maksimal 6 opsi per soal.");` setelah cek `filled < 2`.
- `Controllers/AssessmentAdminController.cs` — CreateQuestion + EditQuestion POST di-refactor; `ResolveCorrectness` helper baru; guard edit-shrink; guard H3 dihapus; loop upsert A–F; relokasi `TruncateAlt` (fix atribut, lihat Deviasi).
- `HcPortal.Tests/SectionCrudTests.cs` — 2 call site CreateQuestion ke kontrak baru.
- `HcPortal.Tests/SectionFixRegressionTests.cs` — 3 call site EditQuestion ke kontrak baru; 2 test H3-rejection obsolete (`H3_EditQuestionWithMoreThan4Options_Rejected_OptionsUnchanged`, `H3_EditQuestionConvertToEssay_MoreThan4Options_Rejected_OptionsIntact`) di-rewrite jadi `Edit6Options_NoResponses_Succeeds_OptionsUpdated` + `EditConvertToEssay_6Options_NoResponses_Succeeds_OptionsRemoved`.

## KONTRAK MARKUP UNTUK PLAN 03 (WAJIB)

Plan 03 (view/JS `ManagePackageQuestions.cshtml` + `_InjectQuestionForm`) HARUS memakai konvensi field exact ini agar model binding server cocok:

| Field | name (binding) | id (display/e2e) | Catatan |
|-------|----------------|------------------|---------|
| Teks opsi | `options[i].Text` | `option_{letter}` (A–F) | i = index 0-based; letter dihitung dari posisi |
| Gambar opsi (authoring) | `options[i].Image` (file) | `opt{letter}Img*` | inject TANPA gambar (scope 394) |
| Alt gambar opsi | `options[i].ImageAlt` | — | |
| Hapus gambar opsi (edit) | `options[i].RemoveImage` (value=true) | `opt{letter}ImgClear` | checkbox/hidden |
| **Benar — MultipleChoice (radio)** | **`correctIndex`** value=`@i` | `correct_{letter}` | **satu name grup** → native single-select; controller map index→IsCorrect |
| **Benar — MultipleAnswer (checkbox)** | **`options[i].IsCorrect`** value=true | `correct_{letter}` | per-baris; controller pakai apa adanya |

Aturan server (LOCK):
- **MC:** controller MENGABAIKAN `options[i].IsCorrect` dari binding; kebenaran HANYA dari `correctIndex` (radio). Markup MC HARUS pakai `name="correctIndex"` value=index, BUKAN `options[i].IsCorrect`.
- **MA:** controller pakai `options[i].IsCorrect` (checkbox per-baris); `correctIndex` diabaikan.
- **Posisi i dihapus** (EditQuestion) bila `i >= keep` ATAU `options[i].Text` kosong (keep = jumlah baris terisi, cap 6). Markup yang re-letter HARUS mempertahankan urutan baris stabil index-aligned dengan opsi existing (OrderBy Id) agar guard edit-shrink + upsert akurat.
- **Baris kosong diabaikan** server-side (skip `IsNullOrWhiteSpace(Text)`), max 6.
- GET JSON `options[]` (OrderBy Id) TIDAK berubah — `populateEditForm` enumerasi `data.options.length` (flag #2).

## Decisions Made

- **Flag #1 (keystone):** Dipilih satu `name="correctIndex"` untuk radio MC (rekomendasi RESEARCH A2) di-resolusi server via `ResolveCorrectness` — overwrite `IsCorrect` MC dari index, abaikan binding per-baris. MA tetap `options[i].IsCorrect`. Dikunci sebagai kontrak Plan 03 (tabel di atas).
- **Pesan edit-shrink** sertakan huruf opsi terblok (mis. `Opsi "E" sudah dijawab...`) — UI-SPEC C5; dihitung dari posisi di `existingForGuard` (OrderBy Id), murah.
- **2 test H3 obsolete di-rewrite, bukan dihapus** — perilaku lama (tolak >4 opsi) kontradiktif dengan tujuan fase (OPT-01). Test baru menegaskan perilaku BARU (6-opsi editable; konversi-Essay sukses bila tak ada response). Proteksi setara lama (edit-shrink) sudah punya `EditShrinkGuardLogicTests` sendiri.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Restore [HttpPost]/[Authorize]/[ValidateAntiForgeryToken] pada CreateQuestion**
- **Found during:** Task 2 (CreateQuestion refactor)
- **Issue:** Atribut `[HttpPost]`, `[Authorize(Roles="Admin, HC")]`, `[ValidateAntiForgeryToken]` (baris 7688-7690) nyangkut pada method `private static TruncateAlt` (disisipkan di antara atribut dan `CreateQuestion`), sehingga `CreateQuestion` POST tidak punya proteksi CSRF maupun authz (T-418-03 + T-418-08 terbuka). Bug pre-existing tapi DIPERSYARATKAN must_honor #9 (PRESERVE authz/antiforgery) — refactor signature akan mengabadikan lubang ini bila diabaikan.
- **Fix:** Relokasi `TruncateAlt` ke ATAS atribut (dengan komentarnya), sehingga `[HttpPost]/[Authorize]/[ValidateAntiForgeryToken]` kini benar-benar mendekorasi `CreateQuestion`. EditQuestion sudah benar (atributnya tepat di atas signature) — tidak disentuh.
- **Files modified:** Controllers/AssessmentAdminController.cs
- **Verification:** Build 0-err; atribut kini secara sintaks tepat di atas `public async Task<IActionResult> CreateQuestion(...)`.
- **Committed in:** `b2d1c35b` (Task 2)

**2. [Rule 3 - Blocking] Update 5 test call site + rewrite 2 test H3 obsolete**
- **Found during:** Task 3 (full xUnit suite gagal compile: CS1501 26/32-arg)
- **Issue:** `SectionCrudTests` (2×) + `SectionFixRegressionTests` (3×) memanggil CreateQuestion/EditQuestion dengan signature lama → test project tak compile. Selain itu, 2 test di SectionFixRegressionTests menegaskan perilaku guard-H3 lama (tolak >4 opsi) yang DIHAPUS fase ini — assertion kontradiktif tujuan.
- **Fix:** Semua call site ke kontrak baru (`List<OptionInput>` + `correctIndex`); 2 test H3-rejection di-rewrite jadi test perilaku BARU (6-opsi editable + konversi-Essay sukses tanpa response).
- **Files modified:** HcPortal.Tests/SectionCrudTests.cs, HcPortal.Tests/SectionFixRegressionTests.cs
- **Verification:** Full xUnit 683/683 GREEN.
- **Committed in:** `7187a9f7` (Task 3)

---

**Total deviations:** 2 auto-fixed (1 missing critical/security, 1 blocking)
**Impact on plan:** Deviasi #1 menutup lubang CSRF/authz yang akan diabadikan refactor — esensial keamanan (T-418-03/08). Deviasi #2 wajib agar suite compile + test mencerminkan perilaku fase. Tidak ada scope creep.

## Issues Encountered

- Penemuan atribut `[ValidateAntiForgeryToken]` salah-tempel pada `TruncateAlt` (bukan `CreateQuestion`) — bug pre-existing yang ditangani sebagai Deviasi Rule 2 (lihat di atas).

## TDD Gate Compliance

Plan ini = GREEN gate untuk RED yang ditanam Plan 418-01. Gate sequence terpenuhi:
- RED (`test(418-01)`): `029aaa0d` + `2431b02b` (Plan 01).
- GREEN (`feat(418-02)`): `76f0f5ca` (helper) + `b2d1c35b` + `7187a9f7`. 5 RED → GREEN; suite 683/683.
- REFACTOR: tidak perlu (kode bersih per task).

## User Setup Required

None - no external service configuration required. migration=FALSE (verified: tidak ada perubahan `Migrations/` atau `Data/`; grading tetap PackageOption.Id-keyed, tak disentuh).

## Known Stubs

None. Stub `OptionShrinkGuard.FindBlockedOptionIds` dari 418-01 kini berisi implementasi nyata (irisan distinct). Tidak ada placeholder/hardcoded-empty yang menghalangi tujuan fase.

## Threat Flags

None — semua surface (CSRF, authz, IDOR Section, image upload E/F, mass-assignment, FK-Restrict) sudah tercakup di `<threat_model>` plan (T-418-03..09) dan dimitigasi. Deviasi #1 justru MENUTUP T-418-03/08 yang sebelumnya terbuka karena bug atribut.

## Next Phase Readiness

- **Plan 418-03 (view/JS markup) siap.** Kontrak field exact + aturan radio/checkbox + id `option_{letter}` + aturan removed terdokumentasi di "KONTRAK MARKUP UNTUK PLAN 03" di atas. Server sudah server-authoritative — markup tinggal mengirim field sesuai tabel.
- **Plan 418-04 (e2e/UAT) siap** — perlu app live @5277 + Playwright real-browser (lesson 354) untuk verifikasi single-select MC lintas 6 baris, edit-shrink alert (real-SQL seed response), dan render A–F.
- Tidak ada blocker. migration=FALSE. NOT pushed (deploy bundle v32.6).

## Self-Check: PASSED

- Files: `Models/OptionInput.cs`, `Helpers/OptionShrinkGuard.cs`, `Helpers/QuestionOptionValidator.cs`, `Controllers/AssessmentAdminController.cs`, `.planning/phases/418-opsi-jawaban-dinamis-2-6/418-02-SUMMARY.md` — all FOUND.
- Commits: `76f0f5ca`, `b2d1c35b`, `7187a9f7` — all FOUND.
- migration guard: no `Migrations/` or `Data/` touched (migration=FALSE preserved).
- Suite: 683/683 xUnit GREEN; build 0 errors.

---
*Phase: 418-opsi-jawaban-dinamis-2-6*
*Completed: 2026-06-24*
