---
phase: 415-section-foundation-import-excel-diperluas
plan: 02
subsystem: ui
tags: [aspnet-mvc, razor, bootstrap5, section, crud, antiforgery, xss-safe, sql-server, xunit]

# Dependency graph
requires:
  - phase: 415-01-section-foundation-data-model
    provides: "Entity AssessmentPackageSection + PackageQuestion.SectionId int? nullable FK + DbSet + FK Question→Section SetNull + unique index (AssessmentPackageId, SectionNumber); migration AddAssessmentPackageSection applied lokal"
provides:
  - "4 endpoint Section CRUD: CreateSection / EditSection / DeleteSection / SetAllSectionsNewPage (RBAC Admin,HC + antiforgery + warn-only audit + PRG/TempData)"
  - "Parameter int? sectionId pada CreateQuestion + EditQuestion (assign soal→Section, null=Lainnya, IDOR-guard)"
  - "ViewBag.Sections di ManagePackageQuestions GET (ordered by SectionNumber)"
  - "Panel inline Kelola Section (CRUD table + form + 2 toggle StartNewPage/ShuffleEnabled + tombol Semua Section mulai halaman baru)"
  - "Dropdown Section di form buat/edit soal + daftar soal dikelompokkan per-Section dengan header (grup Lainnya terakhir)"
affects: [415-03-import-excel-diperluas, 416-scoped-shuffle, 417-section-pagination, 418-opsi-dinamis, 419-export-polish-uat]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Section CRUD endpoint = copy verbatim UpdateShuffleSettings idiom (RBAC + antiforgery + warn-only audit helper + PRG/TempData)"
    - "AnyAsync dup pre-check sebelum unique-index violation (Phase 404 lesson) untuk CreateSection/EditSection"
    - "IDOR guard: section.AssessmentPackageId == packageId sebelum Edit/Delete/assign (T-415-08)"
    - "Razor templated delegate Func<T, HelperResult> @<text>...</text> untuk render baris soal yang dipakai ulang di loop grouping per-Section (zero partial file, zero Html.Raw)"

key-files:
  created:
    - ".planning/phases/415-section-foundation-import-excel-diperluas/415-02-SUMMARY.md"
  modified:
    - "Controllers/AssessmentAdminController.cs"
    - "Views/Admin/ManagePackageQuestions.cshtml"
    - "HcPortal.Tests/SectionCrudTests.cs"

key-decisions:
  - "Edit Section reuse form yang sama (JS set form.action='/Admin/EditSection' + hidden sectionId) — tidak ada form EditSection statis terpisah, mirror pola loadEditForm/resetForm soal existing"
  - "Section group header + dropdown option pakai em-dash literal — di-encode Razor (—→&#x2014;), Nama Section auto-encode (Pompa & Kompresor → Pompa &amp; Kompresor), zero @Html.Raw (XSS-safe T-415-07)"
  - "DeleteSection TANPA manual loop SectionId=null — andalkan FK SetNull (Plan 01), verified runtime + test"

patterns-established:
  - "Section panel inline (full-width card di atas row g-4) — D-415-01, HC lihat Section + soal sekaligus"
  - "JS minim (D-415-02): No.Section angka untuk urut (bukan drag-drop), assign via dropdown form soal, prefill Section panel via data-attribute (XSS-safe .value bukan innerHTML)"

requirements-completed: [SEC-01, SEC-02, SEC-03, SEC-05]

# Metrics
duration: 16min
completed: 2026-06-22
---

# Phase 415 Plan 02: Section Surface Admin (CRUD + Panel Inline + Dropdown + Grouped List) Summary

**Surface admin Section penuh di ManagePackageQuestions: 4 endpoint CRUD (RBAC+antiforgery+audit+PRG, dup-precheck + IDOR-guard) + panel inline Kelola Section dengan 2 toggle per-Section + dropdown assign soal→Section + daftar soal dikelompokkan per-Section (grup "Lainnya" terakhir), XSS-safe via Razor auto-encode (zero @Html.Raw), runtime-verified @5277 HTTP 200 + 9/9 SectionCrud test hijau; migration=FALSE.**

## Performance

- **Duration:** 16 min
- **Started:** 2026-06-22T12:52:42Z
- **Completed:** 2026-06-22T13:08:48Z
- **Tasks:** 3
- **Files modified:** 3 (0 created kode, 1 SUMMARY)

## Accomplishments
- **4 endpoint Section CRUD** di `AssessmentAdminController` — `CreateSection`/`EditSection`/`DeleteSection`/`SetAllSectionsNewPage`, semua `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]` + warn-only audit (helper `LogSectionAuditAsync`) + PRG/TempData (copy verbatim idiom `UpdateShuffleSettings`); `CreateSection`/`EditSection` pre-check duplikat `(packageId, sectionNumber)` via `AnyAsync` (Phase 404 lesson); `EditSection`/`DeleteSection` IDOR-guard (`section.AssessmentPackageId == packageId`); `DeleteSection` andalkan FK SetNull (no manual loop)
- **`int? sectionId` di `CreateQuestion` + `EditQuestion`** (optional param, default null) + IDOR-guard section milik paket + `EditQuestion` GET JSON expose `sectionId` untuk prefill dropdown saat edit
- **Panel inline "Kelola Section"** (full-width `card shadow-sm` di atas `row g-4`, D-415-01) — tabel CRUD (No.Section/Nama/badge toggle/aksi icon-only) + form buat/edit + 2 `form-check form-switch` (Mulai Halaman Baru, Acak) + tombol cepat "Semua Section mulai halaman baru" + delete-confirm reassurance
- **Dropdown Section** di form soal (`— Tanpa Section (Lainnya) —` default) + **daftar soal dikelompokkan per-Section** (header neutral `bg-light fw-semibold` + grup "Lainnya (tanpa Section)" terakhir) via Razor templated delegate
- **Runtime-verified @5277** (lesson 354): page HTTP 200, panel render, CreateSection→render→DeleteSection live PRG, DB persist toggle (`Pompa & Kompresor`, StartNewPage=1, ShuffleEnabled=1), XSS-encode (`Pompa &amp; Kompresor`), test data di-cleanup
- **9/9 SectionCrud** (4 data-layer Wave-1 + 5 controller-driven baru) hijau + fast suite **412/412** unregressed

## Task Commits

Each task was committed atomically:

1. **Task 1: Section CRUD endpoints + sectionId + GET sections** - `0b294e89` (feat)
2. **Task 2: Inline Section panel + dropdown + grouped question list** - `7c814110` (feat)
3. **Task 3: Controller-driven Section CRUD integration tests** - `9fec1dc7` (test)

**Plan metadata:** _(this commit)_ `docs(415-02)`

_migration=FALSE — Plan 02 tidak menyentuh skema (no Migrations/ atau Data/ diff)._

### Endpoint signatures (untuk Plan 03 import yang auto-create Section)

```csharp
// Semua [HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken], redirect ManagePackageQuestions
public async Task<IActionResult> CreateSection(int packageId, int sectionNumber, string? name, bool startNewPage, bool shuffleEnabled)
public async Task<IActionResult> EditSection(int sectionId, int packageId, int sectionNumber, string? name, bool startNewPage, bool shuffleEnabled)
public async Task<IActionResult> DeleteSection(int sectionId, int packageId)         // FK SetNull → soal jadi Lainnya
public async Task<IActionResult> SetAllSectionsNewPage(int packageId)                // bulk StartNewPage=true
// + private helper LogSectionAuditAsync(string actionType, string description, int entityId)
// CreateQuestion/EditQuestion: tambahan trailing param `int? sectionId = null` (null = Lainnya, IDOR-guard)
```

## Files Created/Modified
- `Controllers/AssessmentAdminController.cs` — region "Section Management per Package (Phase 415)": 4 endpoint + `LogSectionAuditAsync` helper; `int? sectionId` di CreateQuestion/EditQuestion + IDOR-guard + assign `SectionId`; `EditQuestion` GET JSON `sectionId`; `ManagePackageQuestions` GET load + `ViewBag.Sections`
- `Views/Admin/ManagePackageQuestions.cshtml` — panel inline Kelola Section + dropdown Section di form soal + daftar soal grouped per-Section (Razor templated delegate `questionRow`) + JS `loadSectionEdit`/`resetSectionForm` + prefill/reset dropdown di `populateEditForm`/`resetForm`
- `HcPortal.Tests/SectionCrudTests.cs` — +5 controller-driven test (StubUserManager + StubWebHostEnvironment + NullTempDataProvider + NoopHubContext harness) drive endpoint ASLI vs SectionFixture SQLEXPRESS

## Decisions Made
- **Edit Section reuse form yang sama** (JS `form.action='/Admin/EditSection'` + hidden `sectionId` + prefill via `data-*` attribute) — tidak ada form `EditSection` statis terpisah; mirror pola `loadEditForm`/`resetForm` soal existing. Konsekuensi: grep `asp-action="EditSection"` tidak ada di markup statis (string `EditSection` ada di JS) — by design.
- **XSS-safe via Razor `@`** — Nama Section + label group header + dropdown option semua auto-encode (runtime confirm `Pompa & Kompresor`→`Pompa &amp; Kompresor`, em-dash `—`→`&#x2014;`); zero `@Html.Raw` di markup baru (T-415-07).
- **DeleteSection TANPA manual loop** `SectionId=null` — andalkan FK SetNull (Plan 01); runtime + test membuktikan soal survive dgn `SectionId=null`.
- **`sectionId` optional param (default null)** di CreateQuestion/EditQuestion — kompatibel-mundur dgn form/import yang belum kirim sectionId; tidak refactor optionA..D ke array (itu Phase 418).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Test harness butuh StubWebHostEnvironment untuk drive CreateQuestion**
- **Found during:** Task 3 (test `CreateQuestion_AssignsSection_OrLeavesUngrouped`)
- **Issue:** `CreateQuestion` mengevaluasi `_env.WebRootPath` untuk argumen `FileUploadHelper.SaveFileAsync` SEBELUM cek file-null di dalam helper → `_env: null!` di harness melempar `NullReferenceException` di `CreateQuestion.cs:7392`. Bukan bug produk (jalur runtime `_env` selalu non-null via DI); murni keterbatasan harness unit.
- **Fix:** Tambah `StubWebHostEnvironment` (WebRootPath = `Path.GetTempPath()`) di konstruksi controller test. `SaveFileAsync` tetap return null saat `questionImage==null` → temp path TIDAK pernah ditulis.
- **Files modified:** HcPortal.Tests/SectionCrudTests.cs
- **Verification:** SectionCrud 9/9 Passed (write-path SQLEXPRESS nyata, no-skip).
- **Committed in:** `9fec1dc7` (Task 3 commit)

**2. [Rule 3 - Blocking] NullTempDataProvider perlu didefinisikan lokal**
- **Found during:** Task 3 (compile error CS0246)
- **Issue:** `NullTempDataProvider` adalah private nested class per-file (tidak di-share antar test file), jadi tidak tersedia di `SectionCrudTests`.
- **Fix:** Definisikan `NullTempDataProvider : ITempDataProvider` nested di `SectionCrudTests` (mirror `FlexibleParticipantAddLiveTests`).
- **Files modified:** HcPortal.Tests/SectionCrudTests.cs
- **Verification:** Test project build 0 error.
- **Committed in:** `9fec1dc7` (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (kedua test-infra Rule 3 blocking; kode produk tak terdampak)
**Impact on plan:** Kedua deviasi murni test-harness (controller produksi tidak diubah; `_env`/TempData selalu disuplai DI/middleware di runtime). Tidak ada scope creep.

## Issues Encountered
- Awal call `CreateQuestion` salah hitung jumlah argumen positional (28 vs 26) — diperbaiki dengan menghapus 2 `null` ekstra (signature: 15 core + 10 param gambar + `sectionId`). Resolved sebelum commit.
- Razor view compile RUNTIME (csproj tanpa `RazorCompileOnBuild`) → `dotnet build` 0-error TIDAK menjamin view valid; wajib boot @5277 + render-probe (lesson 354). Templated delegate `@<text>...</text>` (`questionRow`) terbukti compile + render benar saat runtime.

## Known Stubs
None — panel Section, dropdown, dan grouped-list semua di-wire ke data nyata (`ViewBag.Sections` + `Questions.SectionId`). Helper text toggle "Berlaku penuh di Fase 416/417" adalah label informatif (kolom StartNewPage/ShuffleEnabled DISIMPAN sekarang; konsumsi shuffle/pagination memang dijadwalkan 416/417 per UI-SPEC) — bukan stub data.

## User Setup Required
None - no external service configuration required.

## TDD Gate Compliance
Task 3 (`tdd="true"`) = integration test controller-driven atas endpoint yang dibuat Task 1 (`feat` `0b294e89`) — endpoint produksi MENDAHULUI test (`feat` sebelum `test`, gate ordering terpenuhi). RED murni-baru tidak applicable karena endpoint sudah ada saat test ditulis; test berfungsi sebagai gate kunci yang mengunci kontrak CRUD (persist toggle, dup-reject, FK SetNull, assign-section). Commit `test(...)` `9fec1dc7` ada.

## Next Phase Readiness
- **Surface Section siap dikonsumsi Plan 03 (import Excel diperluas).** Endpoint CRUD + signature ter-dokumentasi di atas; import auto-create Section dapat memanggil pola yang sama (`AssessmentPackageSection` insert + `PackageQuestion.SectionId` assign).
- **migration=FALSE** Plan 02 — notify IT tetap hanya Plan 01 (`AddAssessmentPackageSection`, hash `2391257c`, migration=TRUE).
- **Catatan untuk Plan 03/418:** dropdown form soal pakai `name="sectionId"` (binds null saat empty); CreateQuestion/EditQuestion sudah terima `sectionId` (optional, kompatibel-mundur). Authoring form opsi dinamis A–F tetap Phase 418 (TIDAK disentuh di sini).
- **Lesson re-confirmed (354):** Razor runtime compile — view-render probe @5277 WAJIB untuk perubahan Razor/JS; `dotnet build` 0-error tidak cukup.

## Self-Check: PASSED
- Files: 4/4 found (SUMMARY + AssessmentAdminController.cs + ManagePackageQuestions.cshtml + SectionCrudTests.cs)
- Commits: 3/3 found (0b294e89, 7c814110, 9fec1dc7)

---
*Phase: 415-section-foundation-import-excel-diperluas*
*Completed: 2026-06-22*
