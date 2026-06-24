# Phase 417: Section Pagination - Pattern Map

**Mapped:** 2026-06-23
**Files analyzed:** 6 (2 new, 3 modified, 1 verify-only) + 2 new test files
**Analogs found:** 6 / 6 (semua punya analog langsung di repo — fase generalisasi sempit, bukan greenfield)

Konteks: Brownfield .NET 8 MVC (Razor) + Bootstrap 5. Phase 417 men-generalisasi pagination flat
yang SUDAH ADA di `Views/CMP/StartExam.cshtml` menjadi section-aware. Hampir semua prasyarat
(data Section, urutan section-aware, admin toggle/quick-button, toast, flush guard, mobile override,
reset-to-null) SUDAH ADA dari Phase 415/416. Setiap analog di bawah dibaca langsung dari kode live.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| **NEW** `Helpers/SectionPaginator.cs` | utility (pure fn) | transform | `Helpers/ShuffleEngine.cs` (Phase 416 pure section-aware engine) + `Helpers/PaginationHelper.cs` (page-math) | exact (pure, NON-RNG, section-aware, sama tim/fase) |
| **MODIFY** `Models/PackageExamViewModel.cs` (`ExamQuestionItem` ~L25) | model (viewmodel) | transform | field-addition existing di `ExamQuestionItem` (RND-01 `ImagePath`/`ImageAlt` L42-44) | exact (tambah field ke kelas yang sama) |
| **MODIFY** `Controllers/CMPController.cs` `StartExam` (~L1208 loop, ~L1266 ViewBag, ~L1329 mobile UA) | controller | request-response | dirinya sendiri — bagian loop build `examQuestions` + ViewBag block + mobile UA block | exact (edit in-place jalur yang sama) |
| **MODIFY** `Views/CMP/StartExam.cshtml` (pagination loop L85-214, JS L443-479 + L802 + L991-1187 + L1228-1243, CSS L1536) | component (Razor view) | request-response | dirinya sendiri — flat pagination + JS pageQuestionIds/updatePanel/changePage/showResumeFailureToast | exact (generalisasi in-place) |
| **VERIFY-ONLY** Admin quick-button `SetAllSectionsNewPage` + toggle | controller + view | CRUD | `AssessmentAdminController.cs:6428` + `ManagePackageQuestions.cshtml:84-93` | **SUDAH SELESAI Phase 415 — tidak dibangun ulang, hanya verifikasi** |
| **NEW** `HcPortal.Tests/SectionPaginatorTests.cs` | test (xUnit) | transform | `HcPortal.Tests/SectionScopedShuffleTests.cs` (pure, in-memory `PkgSec(...)` fixture, golden-baseline) | exact (pola fixture pure unit identik) |
| **NEW** `tests/e2e/section-pagination.spec.ts` | test (Playwright e2e) | request-response | `tests/e2e/scoped-shuffle.spec.ts` (mode:'serial', DB backup/restore, wizard + SQL seed Section) | exact (sama domain Section, sama wiring StartExam) |

---

## Pattern Assignments

### `Helpers/SectionPaginator.cs` (NEW — utility pure fn, transform)

**Analog:** `Helpers/ShuffleEngine.cs` (struktur kelas static pure + doc-comment + invariant backward-compat) ditambah `Helpers/PaginationHelper.cs` (clamp/page-math murni).

Kenapa analog ini: ShuffleEngine adalah ekstraksi fungsi-murni Phase 416 yang DERIVATIF dari kebutuhan
identik — section-aware, NON-RNG, unit-testable tanpa DB, dengan invariant "all-null = byte-identik
baseline". RESEARCH Open Question #1 merekomendasikan ekstrak `ComputeSectionPages` ke `Helpers/`
sejalan pola ini (blast-radius kecil, Wave-0 xUnit murni).

**Class structure + doc-comment pattern** (`ShuffleEngine.cs:1-39`):
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 4XX — Pure ... (no EF, no DB, fully synchronous). Single source of truth ...
    /// BACKWARD-COMPAT (invariant): bila SEMUA SectionId=null → ... output BYTE-IDENTIK baseline ...
    /// Pure by design (only System/Linq/HcPortal.Models) → unit-testable without a database.
    /// </summary>
    public static class ShuffleEngine
    {
        public static List<int> BuildQuestionAssignment(List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
        { ... }
    }
}
```

**Signature target (Phase 417)** — fungsi murni yang MENGISI field PageNumber/IsSectionStart/IsSectionContinuation
pada `IList<ExamQuestionItem>` yang SUDAH urut section-aware (dari `GetShuffledQuestionIds`):
```csharp
// Algoritma §7.2 / RESEARCH Pattern 1 — deterministik, NON-RNG.
public static void ComputePages(IList<ExamQuestionItem> ordered, int perPage)
{
    int page = 0, countOnPage = 0;
    int? prevSection = -1;            // sentinel ≠ section nyata / null
    bool firstQuestion = true;
    foreach (var q in ordered)
    {
        bool sectionChanged = !Equals(q.SectionNumber, prevSection);
        bool needNewPageForSection = sectionChanged && q.SectionStartNewPage && !firstQuestion;
        bool pageFull = countOnPage >= perPage;
        if (needNewPageForSection || pageFull) { page++; countOnPage = 0; }
        q.PageNumber = page;
        q.IsSectionStart = sectionChanged;
        q.IsSectionContinuation = !sectionChanged && countOnPage == 0;
        countOnPage++;
        prevSection = q.SectionNumber;
        firstQuestion = false;
    }
}
```

**Clamp helper pattern** (`PaginationHelper.cs:7-13`) — untuk resume clamp (boleh inline di controller atau
ekstra method di SectionPaginator):
```csharp
var currentPage = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));  // pola clamp existing
```

**Backward-compat invariant (WAJIB, sama disiplin SHF-04):** bila semua `SectionNumber == null`,
`sectionChanged` hanya true di soal pertama (sentinel), `needNewPageForSection` selalu false
(`!firstQuestion` guard) → page hanya naik karena `pageFull` → IDENTIK `index / perPage` lama.

---

### `Models/PackageExamViewModel.cs` — `ExamQuestionItem` (MODIFY — model, transform)

**Analog:** dirinya sendiri. `ExamQuestionItem` (L25-45) sudah pernah ditambahi field per-fase dengan
komentar fase (RND-01 `ImagePath`/`ImageAlt` L42-44; `QuestionType` L32-37; `MaxCharacters` L39-40).
Ikuti pola yang sama — append field baru + komentar fase + XML-doc.

**Existing field-addition pattern** (`PackageExamViewModel.cs:42-44`):
```csharp
// RND-01: gambar soal (StartExam). Diisi controller dari PackageQuestion.ImagePath/ImageAlt.
public string? ImagePath { get; set; }
public string? ImageAlt { get; set; }
```

**Target fields (Phase 417)** — tambahkan ke `ExamQuestionItem` (RESEARCH Code Examples):
```csharp
// Phase 417 PAG-01/02: section-aware pagination metadata (computed at render, NOT persisted — D-11)
public int? SectionNumber { get; set; }          // null = grup "Lainnya"
public string? SectionName { get; set; }          // nama Section (D-417-01, name-only)
public bool SectionStartNewPage { get; set; }     // dari AssessmentPackageSection.StartNewPage
public int PageNumber { get; set; }               // 0-based, hasil ComputePages
public bool IsSectionStart { get; set; }          // header polos (Section change)
public bool IsSectionContinuation { get; set; }   // header "(lanjutan)" (auto-split, same section)
```

Sumber data Section (model, sudah ada): `AssessmentPackageSection` (`Models/AssessmentPackage.cs:34-57`)
— `SectionNumber` (L44), `Name` (L47, nullable), `StartNewPage` (L50, default false), `ShuffleEnabled` (L53);
`PackageQuestion.SectionId` (L99) + nav `Section` (L101).

---

### `Controllers/CMPController.cs` `StartExam` (MODIFY — controller, request-response)

**Analog:** dirinya sendiri — tiga titik edit in-place di jalur GET StartExam package-path.

**(a) Isi field Section saat build `examQuestions`** — loop di `CMPController.cs:1210-1235`. `q.Section`
SUDAH ter-`Include` (`:1053-1056`, wiring Phase 416). Tambahkan ke object initializer:
```csharp
// Existing initializer (CMPController.cs:1224-1234) — tambah 3 field section di bawah ImageAlt:
examQuestions.Add(new ExamQuestionItem
{
    QuestionId = q.Id,
    QuestionText = q.QuestionText,
    DisplayNumber = displayNum++,
    Options = opts,
    QuestionType = q.QuestionType ?? "MultipleChoice",
    MaxCharacters = q.MaxCharacters > 0 ? q.MaxCharacters : 2000,
    ImagePath = q.ImagePath,
    ImageAlt = q.ImageAlt,
    // Phase 417: section metadata (q.Section sudah ter-Include di :1053)
    SectionNumber = q.Section?.SectionNumber,
    SectionName = q.Section?.Name,
    SectionStartNewPage = q.Section?.StartNewPage ?? false,  // "Lainnya"/null → tak paksa page-break (§15.A)
});
```

**(b) Mobile UA detection (`questionsPerPage`) + panggil ComputePages.** ⚠️ PINDAHKAN blok mobile UA
(`CMPController.cs:1329-1334`) ke SEBELUM `ComputePages` karena `perPage` dibutuhkan saat compute (RESEARCH
Assumption A2 — konfirmasi tak ada side-effect lain). Existing block:
```csharp
// Mobile page size (D-15): 5 questions per page on mobile devices  (CMPController.cs:1329-1334)
var userAgent = Request.Headers["User-Agent"].ToString();
if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
{
    ViewBag.QuestionsPerPage = 5;
}
// → setelah examQuestions terbangun + perPage resolved:
//   SectionPaginator.ComputePages(examQuestions, perPage);
```

**(c) Clamp RESUME_PAGE + ViewBag** — generalisasi `CMPController.cs:1266` (existing
`ViewBag.LastActivePage = assessment.LastActivePage ?? 0`). RESEARCH Pattern 3:
```csharp
int maxPage = examQuestions.Count > 0 ? examQuestions.Max(q => q.PageNumber) : 0;
int resumePage = assessment.LastActivePage ?? 0;
if (resumePage < 0 || resumePage > maxPage) resumePage = 0;   // fallback aman page 0 (D-417-05)
ViewBag.LastActivePage = resumePage;
```

**(d) (opsional) `ViewBag.SectionConfig`** — pola ViewBag existing block (`:1265-1269`); hanya bila view
butuh metadata Section di luar per-soal (RESEARCH Assumption A1: field per-soal biasanya cukup).

**Tidak disentuh:** `.Include(q => q.Section)` (`:1053`), guard struktur Section (`:1071-1127`),
`UpdateSessionProgress` set `LastActivePage` (`CMPController.cs:482`), reset-to-null (`:1195`).

---

### `Views/CMP/StartExam.cshtml` (MODIFY — component/Razor, request-response)

**Analog:** dirinya sendiri — flat pagination + JS map + navigator + toast. Generalisasi in-place,
JAGA backward-compat (`hasSections` branch).

**(1) totalPages dari computed page (Pitfall 2)** — ganti `Ceiling` (`StartExam.cshtml:7`):
```razor
@* existing :7 → ganti dengan max PageNumber *@
int totalPages = Model.Questions.Any() ? Model.Questions.Max(q => q.PageNumber) + 1 : 1;
bool hasSections = Model.Questions.Any(q => q.SectionNumber != null);
```

**(2) Pagination loop — grouping by PageNumber (bukan Skip/Take naif)** — generalisasi `StartExam.cshtml:85-214`.
Existing flat loop:
```razor
@for (int page = 0; page < totalPages; page++)               @* :85 *@
{
    var pageQuestions = Model.Questions
        .Skip(page * questionsPerPage).Take(questionsPerPage).ToList();   @* :87-90 — GANTI *@
    <div class="exam-page" id="page_@(page)" style="display: @(page == 0 ? "block" : "none")">
        @foreach (var q in pageQuestions) { ... kartu soal qcard_@q.QuestionId ... }   @* :95-188 TAK BERUBAH *@
        <!-- Page navigation Previous/Next/Submit -->                                  @* :190-212 TAK BERUBAH *@
    </div>
}
```
Target: `Model.Questions.GroupBy(q => q.PageNumber).OrderBy(g => g.Key)` (RESEARCH Pattern 2). Kartu soal
(`:98-187`) dan page-nav (`:190-212`) DIPERTAHANKAN VERBATIM — hanya wadah loop + sisipan header berubah.

**(3) Header Section (D-417-01/02, IC-1/IC-2)** — sisipkan SEBELUM kartu soal saat `IsSectionStart`/`IsSectionContinuation`,
HANYA bila `hasSections` (backward-compat Pitfall 3). Style per UI-SPEC (text-only, no colored band):
```razor
@if (hasSections && (q.IsSectionStart || q.IsSectionContinuation))
{
    <div class="text-primary fw-semibold border-bottom pb-1 mb-2">
        @q.SectionName
        @if (q.IsSectionContinuation) { <span class="text-muted small fw-normal">(lanjutan)</span> }
    </div>
}
```

**(4) JS page-maps — SATU sumber dari q.PageNumber (Pitfall 1)** — generalisasi `StartExam.cshtml:443` +
`:465-479`. Existing (HARUS diganti dari `index/perPage` ke grouping `PageNumber`):
```javascript
const TOTAL_PAGES = @totalPages;                                              // :443 — pakai totalPages baru
// pageQuestionIds (:465-470) — GANTI Skip/Take → GroupBy(q.PageNumber)
const pageQuestionIds = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(
    Model.Questions.GroupBy(q => q.PageNumber).OrderBy(g => g.Key)
        .Select(g => g.Select(q => q.QuestionId).ToList()).ToList()));
// allQuestionsData (:473-479) — GANTI pageNumber = index/perPage → q.PageNumber + tambah sectionName
const allQuestionsData = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(
    Model.Questions.Select(q => new {
        questionId = q.QuestionId, pageNumber = q.PageNumber,
        displayNumber = q.DisplayNumber, sectionName = q.SectionName }).ToList()));
// (opsional, RESEARCH OQ#2) pageSectionMap[page] = sectionName — pre-build dari GroupBy(PageNumber)
```

**(5) Navigator per-Section grouping (D-417-03, IC-4)** — generalisasi `updatePanel()` (`StartExam.cshtml:1096-1136`).
Existing flat-render badge dari `allQuestionsData.forEach`. Tambah deteksi pergantian `sectionName` →
sisip baris label full-width SEBELUM grup. CSS grid 7-kolom (`:1536-1542`): label pakai `grid-column: 1/-1`
(JANGAN konsumsi sel badge — Anti-pattern Pattern 4). Mirror ke `#drawerNumbers` (`:1118-1135`). XSS-safe:
pakai `.textContent`/`.innerText` (pola existing `:1106` `btn.innerText`), JANGAN `innerHTML`.
Backward-compat: bila no-section → render flat seperti sekarang (tanpa label).

**(6) Resume → RESUME_PAGE + toast (D-417-05/06, IC-6, Pitfall 5)** — perbaiki handler resume
`StartExam.cshtml:1233-1239` yang HARDCODE `currentPage = 0`:
```javascript
document.getElementById('resumeConfirmBtn').addEventListener('click', function() {  // :1233
    resumeModal.hide();
    currentPage = 0;                              // :1235 — GANTI ke RESUME_PAGE (server sudah clamp)
    document.getElementById('page_' + currentPage).style.display = 'block';
    updatePanel(); window.scrollTo(0, 0);
});
```
Target: `changePage(RESUME_PAGE, true)` (atau `currentPage = RESUME_PAGE`), lalu toast bila `RESUME_PAGE > 0`.

**Toast — REUSE `showResumeFailureToast` (D-417-06, IC-6)** — analog `StartExam.cshtml:802-822`. Existing
mekanisme (`#resumeToastContainer`, `position-fixed bottom-0 end-0 p-3`, `z-index:1100`, `bootstrap.Toast`,
autohide 6000ms). Untuk D-417-06 INFORMATIONAL: ganti kelas `text-bg-warning` → `text-bg-info`/`text-bg-primary`.
`X` = displayNumber soal pertama di halaman tujuan via helper existing `getDisplayNumForQuestion(qId)` (`:579`)
+ `pageQuestionIds[RESUME_PAGE][0]`:
```javascript
if (IS_RESUME && RESUME_PAGE > 0) {
    var firstQid = pageQuestionIds[RESUME_PAGE] && pageQuestionIds[RESUME_PAGE][0];
    var num = getDisplayNumForQuestion(firstQid);
    showResumeInfoToast('Lanjut dari soal no. ' + num + '.');  // clone pola :802, warna info
}
```

**(7) Indikator halaman ber-Section (D-417-04, IC-5)** — update saat `performPageSwitch` (`:1026-1050`);
derive section aktif dari `pageSectionMap[currentPage]` (atau `allQuestionsData` filter). Format
`"{NamaSection} — Halaman {n}/{total}"`; no-section → `"Halaman {n}/{total}"`.

**JANGAN regresi (IC-8):** guard `hasPendingSaves()` di `changePage` (`:986-1024`) flush autosave
antar-halaman — PERTAHANKAN saat ganti halaman lintas-section. `updateMobileNavButtons()` (`:1138-1153`)
pakai `TOTAL_PAGES` baru (Pitfall 2).

---

### Admin quick-button "Semua Section mulai halaman baru" (VERIFY-ONLY — SUDAH ADA Phase 415)

**JANGAN BANGUN ULANG.** RESEARCH Don't Hand-Roll mengkonfirmasi keduanya sudah shipped Phase 415:

**Controller** (`AssessmentAdminController.cs:6428-6452`) — `SetAllSectionsNewPage(int packageId)`,
`[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`, set semua `StartNewPage=true`,
audit log `LogSectionAuditAsync`, `SyncToPostIfSamePackageAsync`.

**View** (`ManagePackageQuestions.cshtml:84-93`) — form POST + antiforgery + tombol
`btn-outline-secondary btn-sm` + `<i class="bi bi-file-earmark-break me-1">` + label "Semua Section mulai halaman baru".

Aksi 417: VERIFIKASI fungsional (set semua section → render StartExam page-break per section). Bila wording
butuh selaras UI-SPEC IC-9 (rekomendasi `btn-outline-primary`), itu cosmetic discretion — bukan greenfield.

---

### `HcPortal.Tests/SectionPaginatorTests.cs` (NEW — test xUnit, transform)

**Analog:** `HcPortal.Tests/SectionScopedShuffleTests.cs` — pola fixture pure, in-memory, no-DB, golden-baseline.

**Header/doc pattern** (`SectionScopedShuffleTests.cs:10-23`): kelas pure, no `[Trait("Category","Integration")]`,
build package in-memory, golden-order = kontrak backward-compat.

**In-memory fixture builder `PkgSec(...)`** (`SectionScopedShuffleTests.cs:34-66`) — REUSABLE/clone:
```csharp
private static AssessmentPackage PkgSec(int packageNumber,
    (int id, int order, string? et, int? sectionNumber)[] qs, ...)
{
    var p = new AssessmentPackage { PackageNumber = packageNumber, Id = packageNumber };
    foreach (var (id, order, et, sectionNumber) in qs)
    {
        var q = new PackageQuestion { Id = id, Order = order, ElemenTeknis = et, SectionId = sectionNumber, ... };
        if (sectionNumber != null)
            q.Section = new AssessmentPackageSection { Id = sectionNumber.Value, SectionNumber = sectionNumber.Value, ShuffleEnabled = ... };
        p.Questions.Add(q);
    }
    return p;
}
```
Untuk 417 butuh varian yang juga set `StartNewPage` per section, dan helper bangun `List<ExamQuestionItem>`
urut (atau panggil `ComputePages` langsung atas list ExamQuestionItem in-memory).

**Golden-baseline pattern** (`SectionScopedShuffleTests.cs:84-90`) — captured baseline literal + komentar
"Jangan ubah nilai ini — kalau test merah, perbaiki ENGINE". Pakai untuk no-Section golden-baseline (PAG-01).

**Test method pattern** (`SectionScopedShuffleTests.cs:104-110`):
```csharp
[Fact]
public void AllNullSection_ProducesIdenticalOrderToLegacyBaseline()
{
    var actual = ShuffleEngine.BuildQuestionAssignment(AllNullFixture(), shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
    Assert.Equal(GoldenOrderBaseline, actual);
}
```

**Cakupan target (RESEARCH Test Map)** — nama method mengikuti yang dicantumkan researcher:
`PageNumber_FlowsTenPerPage`, `NoSection_IdenticalToFlatBaseline` (golden), `StartNewPage_BreaksBeforeSection`,
`LongSection_AutoSplitsTenPerPage` (+ `IsSectionContinuation`), `LainnyaGroup_NoForcedBreak`,
`Resume_ClampsToValidRange`, `Resume_OutOfRange_FallsBackToZero`, `MobileFivePerPage_SectionAware` (perPage=5).

---

### `tests/e2e/section-pagination.spec.ts` (NEW — test Playwright e2e, request-response)

**Analog:** `tests/e2e/scoped-shuffle.spec.ts` — sama domain Section, sama jalur StartExam, sama seed strategy.

**Header/SEED_WORKFLOW pattern** (`scoped-shuffle.spec.ts:1-37`): doc skenario, `mode:'serial'`,
DB backup beforeAll / restore afterAll (CLAUDE.md SEED_WORKFLOW temporary+local-only), app @localhost:5277,
`--workers=1`, auth `admin@pertamina.com`, peserta `rino.prasetyo@pertamina.com`.

**Imports + helpers** (`scoped-shuffle.spec.ts:32-86`):
```typescript
import { test, expect, type Page, type Browser } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';
test.describe.configure({ mode: 'serial' });
// execSql/queryStr via db.queryScalar/db.queryString (localhost-guard); seed Section via SQL UPDATE
// pada record wizard; today()/tomorrow() helper jadwal (startable hari ini, EWCD besok)
```
Helper tersedia: `tests/helpers/{dbSnapshot.ts, auth.ts}`, `tests/e2e/helpers/examTypes.ts`.
Pola seed Section: buat assessment via wizard → `UPDATE AssessmentPackageSections SET StartNewPage=...`
+ `UPDATE PackageQuestions SET SectionId=...` pada record baru (dilindungi snapshot/restore).

**Cakupan target (RESEARCH Wave-0 gaps):** header on section change, "(lanjutan)" auto-split,
StartNewPage page-break, navigator grouping, resume landing page + toast "Lanjut dari soal no. X",
no-Section flat smoke (backward-compat), mobile 5/halaman. Verifikasi resume migrasi-laten
(sesi InProgress lahir pra-417, LastActivePage clamp aman).

---

## Shared Patterns

### Backward-compat invariant (no-Section = byte-identik baseline) — CROSS-CUTTING
**Source:** `ShuffleEngine.cs:28-35` (invariant SHF-04) + RESEARCH Pitfall 3.
**Apply to:** `SectionPaginator.ComputePages` (algoritma), `StartExam.cshtml` (branch `hasSections`),
`SectionPaginatorTests` (golden-baseline test).
Bila semua `SectionNumber==null`: tanpa header, navigator flat, indikator tanpa label, page-map identik
`index/perPage`. WAJIB golden-test. Disiplin sama persis dengan ShuffleEngine all-null.

### Single source of truth Razor↔JS (anti-drift) — CROSS-CUTTING
**Source:** RESEARCH Pitfall 1 + arsitektur (controller-compute > view-compute).
**Apply to:** controller compute `PageNumber` sekali; `StartExam.cshtml` `pageQuestionIds`/`allQuestionsData`/
`pageSectionMap` SEMUA dibangun dari `q.PageNumber` yang sama (`GroupBy(q.PageNumber)`), bukan `index/perPage`.

### XSS output-encoding nama Section — SHARED (V14)
**Source:** RESEARCH Security Domain + pola existing `StartExam.cshtml:1106` (`btn.innerText`).
**Apply to:** header Section (Razor `@q.SectionName` auto-encode); navigator/indikator JS pakai
`.textContent`/`.innerText`, JANGAN `innerHTML`. Nama Section HC-controlled.

### Toast mechanism reuse — SHARED
**Source:** `StartExam.cshtml:802-822` `showResumeFailureToast`.
**Apply to:** toast resume D-417-06 — clone mekanisme (`#resumeToastContainer` + `bootstrap.Toast`,
autohide 6000ms), ganti warna ke informational. JANGAN bikin container/mekanisme baru.

### Autosave flush guard (IC-8, no-regress) — SHARED
**Source:** `StartExam.cshtml:986-1024` (`hasPendingSaves()` di `changePage`).
**Apply to:** semua perpindahan halaman (termasuk lintas-section). Pertahankan guard menunggu
`pendingSaves`/`inFlightSaves`/`essayInFlight` sebelum switch.

### SEED_WORKFLOW e2e (CLAUDE.md) — SHARED (test)
**Source:** `scoped-shuffle.spec.ts:24` + `tests/helpers/dbSnapshot.ts`.
**Apply to:** `section-pagination.spec.ts` — BACKUP beforeAll / RESTORE afterAll, temporary+local-only,
catat `docs/SEED_JOURNAL.md`, --workers=1.

---

## No Analog Found

(none — semua file punya analog langsung di repo)

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| — | — | — | Fase generalisasi sempit; setiap surface mereuse pola existing Phase 415/416 atau dirinya sendiri. |

---

## Metadata

**Analog search scope:** `Helpers/`, `Models/`, `Controllers/CMPController.cs` (StartExam region),
`Controllers/AssessmentAdminController.cs`, `Views/CMP/StartExam.cshtml`, `Views/Admin/ManagePackageQuestions.cshtml`,
`HcPortal.Tests/`, `tests/e2e/` + `tests/helpers/`.
**Files scanned (read):** ShuffleEngine.cs, PaginationHelper.cs, PackageExamViewModel.cs, AssessmentPackage.cs,
CMPController.cs (L1040-1337), StartExam.cshtml (L1-100, 100-229, 440-599, 800-839, 986-1196, 1195-1254, 1528-1572),
SectionScopedShuffleTests.cs (L1-144), scoped-shuffle.spec.ts (L1-90), AssessmentAdminController.cs (L6428-6452),
ManagePackageQuestions.cshtml (L84-93).
**Pattern extraction date:** 2026-06-23
**Pure-helper extraction recommended** (RESEARCH OQ#1): `Helpers/SectionPaginator.cs` — sejalan ShuffleEngine,
memudahkan xUnit murni Wave-0, blast-radius kecil.
