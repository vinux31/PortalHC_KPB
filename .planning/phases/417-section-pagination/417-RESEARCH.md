# Phase 417: Section Pagination - Research

**Researched:** 2026-06-23
**Domain:** Brownfield .NET 8 MVC (Razor) + Bootstrap 5 — generalisasi pagination ujian flat menjadi section-aware
**Confidence:** HIGH (semua temuan di-grounding ke kode live yang dibaca langsung)

## Summary

Phase 417 men-generalisasi pagination ujian yang SUDAH ADA di `Views/CMP/StartExam.cshtml` (flat 10-soal/halaman desktop, 5 mobile) menjadi **section-aware**: header Section saat berganti Section, page-break sebelum Section ber-`StartNewPage=true`, auto-pecah per-10 untuk Section panjang, navigator dikelompokkan per-Section, indikator halaman ber-nama-Section, dan resume yang menghitung-ulang halaman dari config. Migration=FALSE — kolom `StartNewPage` per-Section sudah ada dari Phase 415, `LastActivePage int?` tidak berubah, page-number TIDAK disimpan per-soal (dihitung saat render, D-11).

Temuan paling berdampak ke perencanaan: **(1)** urutan soal section-aware (Section 1→2→…→Lainnya) SUDAH dibangun oleh `ShuffleEngine.BuildQuestionAssignment` (Phase 416) dan tersedia di `assignment.GetShuffledQuestionIds()`; controller tinggal MENGHITUNG `PageNumber` per-soal di atas urutan itu. **(2)** Admin quick-button "Semua Section mulai halaman baru" (IC-9 / §15.D) **SUDAH SELESAI di Phase 415** — action `SetAllSectionsNewPage` + tombol di `ManagePackageQuestions.cshtml` sudah ada; 417 hanya perlu MEMVERIFIKASI, bukan membangun. **(3)** Gap data tunggal: `ExamQuestionItem` belum membawa info Section (`SectionId`/`SectionNumber`/`SectionName`/`StartNewPage`) — ini harus ditambahkan agar view bisa menyisipkan header + grouping.

**Primary recommendation:** Hitung `PageNumber` per-soal **di controller** (single source of truth, deterministik, mudah di-unit-test sebagai fungsi murni), kirim ke view via field baru di `ExamQuestionItem` (+ `ViewBag.SectionConfig` untuk metadata header). View me-render `exam-page` div per nomor halaman terhitung (bukan `Skip/Take` naif) dan JS `pageQuestionIds`/`allQuestionsData` dibangun dari `PageNumber` yang sama — satu sumber kebenaran lintas Razor+JS. Backward-compat: bila semua `SectionId=null`, algoritma menghasilkan page-map identik dengan `index / questionsPerPage` lama → render byte-identik.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-417-01:** Header Section = **NAMA Section saja** (tanpa nomor) di atas grup soal Section. Muncul saat berganti Section (boleh muncul di tengah halaman pada mode default, §7.1).
- **D-417-02:** Saat Section panjang **auto-pecah** ke halaman lanjutan (>10 soal), header Section **DIULANG** dengan tanda **"(lanjutan)"** di halaman sambungan.
- **D-417-03:** Grid nomor soal **DIKELOMPOKKAN per-Section** dengan **label Section** di atas tiap grup (bukan flat 1..N). Assessment **tanpa Section** (semua `SectionId=null`) → tetap flat 1..N (backward-compat).
- **D-417-04:** Navigasi halaman menampilkan **Section aktif + halaman**, mis. `"<Nama Section> — Halaman 2/5"`. Pakai **nama Section saja** (selaras D-417-01). Tanpa Section → `"Halaman 2/5"` saja.
- **D-417-05:** Saat config Section diubah HC pasca-lock → nomor halaman **dihitung ulang dari config** (§15.A, §7.2). Identitas soal stabil by question id; `LastActivePage` null/di luar rentang → **fallback aman ke halaman 0**.
- **D-417-06:** Saat peserta **resume** & diarahkan ke halaman terhitung > 0 → tampilkan **toast informatif** `"Lanjut dari soal no. X"` (X = nomor soal pertama di halaman tujuan). Reuse pola `showResumeFailureToast`.

### Claude's Discretion
- Bentuk persis perhitungan `PageNumber` per-soal: controller (`ViewBag.SectionConfig`/precomputed page-map) vs view — planner putuskan, asal section-aware & deterministik.
- Wording & penempatan tombol cepat "Semua section mulai halaman baru" di UI Kelola Section. **(CATATAN: sudah ada dari Phase 415 — lihat Don't Hand-Roll.)**
- Mobile **5 soal/halaman** mengikuti aturan Section yang sama (`ViewBag.QuestionsPerPage` sudah ada).
- Styling visual header Section, label grup navigator, dan toast.

### Deferred Ideas (OUT OF SCOPE)
- Header dengan jumlah soal / progress per-Section ("3/8 soal Section ini") — ditolak di 417 (header = nama saja, D-417-01).
- PAG-04 (label/header Section di export Excel/PDF) = **Fase 419** (bukan scope render ujian 417).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **PAG-01** | Default tampilan ujian = 10 soal/halaman mengalir, dengan header Section saat berganti Section. | Pattern 1 (compute PageNumber controller) + Pattern 2 (render exam-page per computed page + header on section change). Existing flat pagination di `StartExam.cshtml:85-214` di-generalisasi. |
| **PAG-02** | Section ber-"Mulai Halaman Baru" dimulai di halaman baru; Section panjang otomatis terpecah per 10 soal. | Algoritma §7.2 (increment page bila `StartNewPage=true` ATAU halaman penuh). `AssessmentPackageSection.StartNewPage` (Phase 415) sudah tersedia, di-`Include` via `q.Section` di `CMPController:1053`. |
| **PAG-03** | Resume ujian (`LastActivePage`) tetap mengarah ke halaman yang benar saat pagination Section aktif. | Pattern 3 (resume mapping). `LastActivePage int?` global (`AssessmentSession:59`), recompute page-map saat render, clamp/fallback page 0, toast D-417-06. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Hitung `PageNumber` per-soal (section-aware) | API/Backend (`CMPController.StartExam`) | — | Server-authoritative, deterministik, mudah di-unit-test sbg fungsi murni; menghindari drift Razor↔JS. Urutan soal otoritatif sudah di server (`ShuffledQuestionIds`). |
| Metadata Section (nama, StartNewPage) ke view | API/Backend (`ViewBag.SectionConfig` + field di `ExamQuestionItem`) | — | View butuh nama Section + flag page-break untuk header & grouping; data ada di `q.Section` yang sudah ter-`Include`. |
| Render header Section + `(lanjutan)` + page-break div | Frontend Server (Razor `StartExam.cshtml`) | — | Penyisipan markup di titik batas Section saat membangun `exam-page` div. |
| Navigator per-Section + label grup (`#panelNumbers`/`#drawerNumbers`) | Browser/Client (JS `updatePanel()`) | Frontend Server (data via `allQuestionsData`) | Panel dibangun client-side dari `allQuestionsData`; perlu field `sectionName` per-soal. |
| Indikator halaman ber-nama-Section | Browser/Client (JS `changePage()`/`performPageSwitch()`) | — | Berubah saat ganti halaman; derive dari `currentPage` → section mapping (data dari `pageSectionMap`). |
| Resume → landing page terhitung + toast | API/Backend (recompute) + Browser/Client (toast) | — | `RESUME_PAGE` dari server (`ViewBag.LastActivePage`), clamp di JS, toast informatif via `showResumeFailureToast` pattern. |
| Admin quick-button "Semua Section mulai halaman baru" | API/Backend (`SetAllSectionsNewPage`) + Frontend Server (view) | — | **SUDAH SELESAI Phase 415** — verifikasi only. |
| Autosave flush antar-halaman (cross-section) | Browser/Client (`changePage`→`hasPendingSaves`) | — | Sudah ada guard; 417 tak boleh regresi (IC-8). |

## Standard Stack

### Core (semua sudah vendored — TIDAK ada paket baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET MVC (Razor) | net8.0 | Render server `StartExam.cshtml` | [VERIFIED: HcPortal.csproj `<TargetFramework>net8.0`] — stack proyek |
| Bootstrap 5 | vendored di `wwwroot/lib/bootstrap/` | `badge`, `card`, `list-group`, `toast`, `offcanvas`, `btn` | [VERIFIED: 417-UI-SPEC.md + StartExam.cshtml memakai kelas Bootstrap 5] |
| Bootstrap Icons | vendored | `bi bi-*` (mis. `bi-file-earmark-break` quick-button) | [VERIFIED: dipakai di StartExam.cshtml + ManagePackageQuestions.cshtml] |
| SignalR | `wwwroot/lib/signalr/` | Autosave essay/MA antar-halaman (`assessment-hub.js`) | [VERIFIED: assessment-hub.js dibaca] |
| EF Core | net8.0 | `.Include(q => q.Section)` load di StartExam | [VERIFIED: CMPController.cs:1050-1059] |
| xUnit | (HcPortal.Tests) | Unit test fungsi murni page-computation | [VERIFIED: HcPortal.Tests/ — 665/665 pass per 416 SUMMARY] |
| Playwright | `tests/e2e/` (chromium) | e2e render/resume real-browser | [VERIFIED: tests/playwright.config.ts dibaca] |

**Installation:** Tidak ada. Semua dependency sudah ada di repo. **migration=FALSE.**

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Compute PageNumber di controller | Compute di view (Razor loop inline) | View-compute menggandakan logika di Razor + JS (`pageQuestionIds` & `allQuestionsData`) → rawan drift. Controller-compute = satu sumber kebenaran, mudah di-unit-test. **Pilih controller.** |
| Field `PageNumber` di `ExamQuestionItem` | `ViewBag` Dictionary page-map terpisah | ViewBag dictionary perlu lookup `qId→page` di banyak titik; field di item lebih ergonomis untuk loop view + serialisasi JSON. Boleh kombinasi (field utama + `ViewBag.SectionConfig` untuk nama Section). |

## Architecture Patterns

### System Architecture Diagram (data flow)

```
                         [Worker buka /CMP/StartExam/{id}]
                                      │
                                      ▼
   ┌──────────────────────────── CMPController.StartExam (GET) ────────────────────────────┐
   │  1. Load packages .Include(q.Options).Include(q.Section)        (CMPController:1050)   │
   │  2. assignment.GetShuffledQuestionIds()  ← urutan Section 1→2→…→Lainnya (Phase 416)    │
   │  3. Loop shuffledIds → build examQuestions (DisplayNumber GLOBAL 1..N)  (:1208-1235)   │
   │  4. ★NEW★ ComputeSectionPages(orderedQuestions, sectionLookup, questionsPerPage)       │
   │        → set q.PageNumber + q.SectionId/SectionNumber/SectionName/IsSectionStart       │
   │  5. ViewBag.LastActivePage = clamp(assessment.LastActivePage, 0..maxPage)  (:1266)     │
   │  6. ViewBag.SectionConfig = [{number, name, startNewPage}]  (★NEW★)                    │
   │  7. ViewBag.QuestionsPerPage = 5 bila mobile UA  (:1329-1334)                          │
   └────────────────────────────────────────┬──────────────────────────────────────────────┘
                                             ▼
   ┌──────────────────────────── StartExam.cshtml (Razor render) ─────────────────────────┐
   │  • Group questions by q.PageNumber (BUKAN Skip/Take naif)                             │
   │  • Per page div: bila soal pertama page = awal Section → <header NamaSection>         │
   │                  bila page = lanjutan Section auto-split → <header NamaSection(lanjutan)>│
   │  • Emit JS: pageQuestionIds (page→ids) + allQuestionsData (qId,pageNumber,sectionName) │
   │             + pageSectionMap (page→sectionName) — SEMUA dari q.PageNumber yg sama      │
   └────────────────────────────────────────┬──────────────────────────────────────────────┘
                                             ▼
   ┌──────────────────────────── Browser JS (StartExam.cshtml inline) ────────────────────┐
   │  • updatePanel(): render badge per-Section + label grup (D-417-03)                    │
   │  • changePage()/performPageSwitch(): update indikator "NamaSection — Halaman n/total" │
   │  • Resume: currentPage = RESUME_PAGE (sudah di-clamp server) → toast "Lanjut dari      │
   │    soal no. X" bila > 0 (reuse showResumeFailureToast)                                 │
   │  • Guard hasPendingSaves() di changePage TETAP (autosave flush, IC-8)                  │
   └───────────────────────────────────────────────────────────────────────────────────────┘
```

### Recommended approach — minimal blast-radius
```
Models/PackageExamViewModel.cs   → tambah field Section + PageNumber di ExamQuestionItem
Helpers/ (opsional)              → ekstrak ComputeSectionPages sbg static pure fn (testable)
Controllers/CMPController.cs     → panggil ComputeSectionPages di StartExam + ViewBag.SectionConfig + clamp RESUME_PAGE
Views/CMP/StartExam.cshtml       → render grouping by PageNumber + header + navigator + indikator + toast
(Admin surface)                  → TIDAK disentuh (quick-button sudah ada Phase 415)
```

### Pattern 1: Compute `PageNumber` per-soal sebagai fungsi murni (di controller)
**What:** Iterasi soal terurut (sudah Section 1→2→…→Lainnya dari `GetShuffledQuestionIds`), naikkan counter halaman bila (a) soal mulai Section baru yang `StartNewPage=true`, ATAU (b) halaman sekarang sudah berisi `questionsPerPage` soal.
**When to use:** Sekali di `StartExam`, setelah `examQuestions` dibangun (`CMPController.cs:~1235`).
**Example (pseudo, fungsi murni — ideal untuk Helpers + xUnit):**
```csharp
// Source: derived dari spec §7.2 + struktur ExamQuestionItem (PackageExamViewModel.cs:25)
// orderedQuestions: List<ExamQuestionItem> SUDAH urut Section (dari GetShuffledQuestionIds)
// Mengisi q.PageNumber, q.IsSectionStart, q.IsSectionContinuation. NON-RNG, deterministik.
static void ComputeSectionPages(IList<ExamQuestionItem> ordered, int perPage)
{
    int page = 0;
    int countOnPage = 0;
    int? prevSection = -1;             // sentinel ≠ any real section / null
    bool firstQuestion = true;
    foreach (var q in ordered)
    {
        bool sectionChanged = !Equals(q.SectionNumber, prevSection);
        bool needNewPageForSection = sectionChanged && q.SectionStartNewPage && !firstQuestion;
        bool pageFull = countOnPage >= perPage;
        if (needNewPageForSection || pageFull)
        {
            page++;
            countOnPage = 0;
        }
        q.PageNumber = page;
        q.IsSectionStart = sectionChanged;                 // header full pada soal ini
        q.IsSectionContinuation = !sectionChanged && countOnPage == 0; // auto-split → header "(lanjutan)"
        countOnPage++;
        prevSection = q.SectionNumber;
        firstQuestion = false;
    }
}
```
**Backward-compat invariant:** bila SEMUA `SectionNumber == null`, `sectionChanged` hanya true di soal pertama (`prevSection` sentinel) → `needNewPageForSection` selalu false (firstQuestion guard), page hanya naik karena `pageFull` → identik dengan `index / perPage` lama. `IsSectionStart` true hanya di soal #1 (no header karena no-Section branch di view).

### Pattern 2: View me-render `exam-page` div per `PageNumber` terhitung (bukan Skip/Take)
**What:** Ganti `Model.Questions.Skip(page*perPage).Take(perPage)` (`StartExam.cshtml:87-90`) dengan grouping `Model.Questions.GroupBy(q => q.PageNumber)` (atau `Where(q => q.PageNumber == page)`). `totalPages = Questions.Max(q => q.PageNumber) + 1`.
**When to use:** Loop `@for (int page = 0; page < totalPages; page++)` di `StartExam.cshtml:85`.
**Example (Razor sketch):**
```razor
@* Source: generalisasi StartExam.cshtml:85-214 *@
@{
    var pages = Model.Questions.GroupBy(q => q.PageNumber).OrderBy(g => g.Key).ToList();
    int totalPages = pages.Count;   // ganti Ceiling(TotalQuestions/perPage)
    bool hasSections = Model.Questions.Any(q => q.SectionNumber != null);
}
@foreach (var pg in pages)
{
    var pageQuestions = pg.ToList();
    <div class="exam-page" id="page_@pg.Key" style="display:@(pg.Key==0?"block":"none")">
        @foreach (var q in pageQuestions)
        {
            @* Header Section: hanya bila assessment ber-section (D-417-03 backward-compat) *@
            @if (hasSections && (q.IsSectionStart || q.IsSectionContinuation))
            {
                <div class="text-primary fw-semibold border-bottom pb-1 mb-2">
                    @q.SectionName
                    @if (q.IsSectionContinuation)
                    { <span class="text-muted small fw-normal">(lanjutan)</span> }
                </div>
            }
            @* ...kartu soal EXISTING tak berubah (qcard_@q.QuestionId)... *@
        }
        @* ...page navigation EXISTING (Previous/Next/Submit)... *@
    </div>
}
```

### Pattern 3: Resume mapping (PAG-03 / §15.A / D-417-05/06)
**What:** `LastActivePage` adalah page-index GLOBAL (`int?`). Saat render, page-map dihitung ulang dari config (Pattern 1). Clamp `RESUME_PAGE` ke `[0, maxPage]`; null/out-of-range → 0. Identitas soal stabil by question id (page bergeser, isi soal tetap). Toast `"Lanjut dari soal no. X"` saat resume page > 0 (X = `DisplayNumber` soal pertama di halaman itu).
**Where:** Clamp di controller (`ViewBag.LastActivePage`, `CMPController.cs:1266`) — server-authoritative. Toast di JS (reuse `showResumeFailureToast` di `StartExam.cshtml:802`, ubah ke `text-bg-info`/`text-bg-primary` untuk nada informasi).
**Example (controller clamp + JS toast trigger):**
```csharp
// Source: CMPController.cs:1266 (existing) — tambah clamp
int maxPage = examQuestions.Count > 0 ? examQuestions.Max(q => q.PageNumber) : 0;
int resumePage = assessment.LastActivePage ?? 0;
if (resumePage < 0 || resumePage > maxPage) resumePage = 0;   // fallback aman page 0 (D-417-05)
ViewBag.LastActivePage = resumePage;
```
```javascript
// Source: pola StartExam.cshtml:725-727 (resume failure) — versi informatif
// firstQuestionNumberOnPage[page] dari pageQuestionIds → displayNumber soal pertama
if (IS_RESUME && RESUME_PAGE > 0) {
    var firstQid = pageQuestionIds[RESUME_PAGE] && pageQuestionIds[RESUME_PAGE][0];
    var num = getDisplayNumForQuestion(firstQid);  // existing helper :579
    showResumeFailureToast('Lanjut dari soal no. ' + num + '.');  // reuse mekanisme; warna info
}
```
> Catatan: existing resume hanya men-set `currentPage = 0` di tombol "Lanjutkan" (`StartExam.cshtml:1235`). Untuk PAG-03 perlu set `currentPage = RESUME_PAGE` (atau panggil `changePage(RESUME_PAGE, true)`) saat tombol resume di-klik, BUKAN hardcode 0. Ini perubahan kecil tapi load-bearing.

### Pattern 4: Navigator per-Section grouping (D-417-03 / IC-4)
**What:** `updatePanel()` (`StartExam.cshtml:1096`) saat ini me-render flat badge dari `allQuestionsData`. Generalisasi: kelompokkan badge per `sectionName`, sisipkan baris label full-width sebelum tiap grup. Backward-compat: bila no-Section, render flat (tidak ada label).
**How:** Tambah `sectionName`/`sectionNumber` ke objek `allQuestionsData` (di serialisasi Razor `StartExam.cshtml:473-479`). Di `updatePanel()`, deteksi pergantian `sectionName` → append elemen label (full-width, `grid-column: 1/-1` di grid 7-kolom `#panelNumbers`). Mirror ke `#drawerNumbers` (mobile).
**Anti-pattern:** JANGAN menaruh label di dalam sel grid badge — akan merusak grid 7-kolom (`StartExam.cshtml:1536-1542`). Gunakan `grid-column: 1 / -1` untuk baris label.

### Anti-Patterns to Avoid
- **Skip/Take naif setelah pagination section-aware:** `Skip(page*perPage)` mengabaikan page-break `StartNewPage` → halaman salah. Gunakan grouping by `PageNumber`.
- **Page-number disimpan per-soal (kolom DB):** ditolak D-11 (acak/reshuffle merusaknya). Selalu hitung saat render.
- **Hitung page di JS sendiri terpisah dari Razor:** menggandakan algoritma → drift `pageQuestionIds` vs render. Satu sumber: `q.PageNumber` dari controller.
- **Header Section sebagai band warna full-width:** ditolak UI-SPEC (rusak balance 60/30/10). Gunakan `text-primary fw-semibold` + opsional `border-bottom`.
- **Reset `currentPage=0` saat resume:** bug PAG-03 — harus ke `RESUME_PAGE` terhitung.
- **Membangun ulang admin quick-button:** sudah ada Phase 415 (lihat Don't Hand-Roll).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Admin tombol "Semua Section mulai halaman baru" | Action + view baru | `SetAllSectionsNewPage` (`AssessmentAdminController.cs:6432`) + tombol di `ManagePackageQuestions.cshtml:86-90` (`bi bi-file-earmark-break`) | **SUDAH SELESAI Phase 415 (SEC-02).** Termasuk audit log + `SyncToPostIfSamePackageAsync`. 417 hanya VERIFIKASI. |
| Per-section `StartNewPage` toggle (CRUD) | Form toggle baru | Kolom `AssessmentPackageSection.StartNewPage` + UI toggle di `ManagePackageQuestions.cshtml:227` (Create) + edit (`AssessmentAdminController.cs:6367`) | Sudah ada Phase 415. Data tersedia untuk render. |
| Urutan soal section-aware (Section 1→2→…→Lainnya) | Sortir ulang di controller/view | `ShuffleEngine.BuildQuestionAssignment` → `assignment.GetShuffledQuestionIds()` | Sudah section-aware Phase 416; grup "Lainnya" selalu terakhir (D-15). |
| Toast resume | Mekanisme toast baru | `showResumeFailureToast()` (`StartExam.cshtml:802`) + `#resumeToastContainer` + `bootstrap.Toast` | D-417-06 eksplisit reuse. Cukup ganti warna ke `text-bg-info`/`text-bg-primary`. |
| Autosave flush antar-halaman | Flush logic baru | `hasPendingSaves()` guard di `changePage()` (`StartExam.cshtml:986-1024`) | Sudah menunggu `pendingSaves`/`inFlightSaves`/`essayInFlight`. IC-8: jangan regresi. |
| Mobile 5/halaman | Branch mobile terpisah | `ViewBag.QuestionsPerPage = 5` (`CMPController.cs:1333`) → `questionsPerPage` di view | Sudah ada; algoritma Pattern 1 pakai `perPage` param → mobile otomatis ikut aturan Section. |
| Load `q.Section` di StartExam | `.Include` tambahan | Sudah ada `.ThenInclude(q => q.Section)` (`CMPController.cs:1053-1056`) | Wiring Phase 416 Plan 02. Section tersedia di loop `examQuestions`. |
| Reset `LastActivePage` saat reshuffle/restore | Logic reset baru | `CMPController.cs:1195` + `AssessmentAdminController.cs:4947` (`SetProperty(LastActivePage, null)`) | Sudah ada; null → fallback page 0 saat render (D-417-05). |

**Key insight:** Phase 417 adalah **generalisasi sempit** dari pagination yang sudah matang, BUKAN fitur greenfield. Sebagian besar prasyarat (data Section, urutan section-aware, admin toggle+quick-button, toast pattern, flush guard, mobile override, reset-to-null) SUDAH ADA dari Phase 415/416. Pekerjaan inti: **(1)** tambah field Section+PageNumber di `ExamQuestionItem`, **(2)** fungsi `ComputeSectionPages`, **(3)** render grouping+header+navigator+indikator di view, **(4)** clamp resume + set `currentPage=RESUME_PAGE` + toast.

## Runtime State Inventory

> Rename/refactor/migration phase? **TIDAK** — Phase 417 adalah perubahan render/compute murni, migration=FALSE.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — page-number TIDAK disimpan (D-11); `LastActivePage int?` (page-index global) sudah ada, tak berubah skema. | None — verified `AssessmentSession.cs:59` + REQUIREMENTS migration=FALSE. |
| Live service config | None — tak ada n8n/external service tersentuh. | None. |
| OS-registered state | None. | None. |
| Secrets/env vars | None. | None. |
| Build artifacts | None — tak ada paket baru (Bootstrap/SignalR/xUnit/Playwright sudah vendored). | None. |

**Catatan migrasi-data laten:** Sesi InProgress yang lahir SEBELUM 417 menyimpan `LastActivePage` sebagai page-index flat. Karena identitas soal stabil by question id dan page dihitung-ulang dari config saat render (D-417-05), nilai lama tetap valid sebagai page-index global; bila di luar rentang baru → clamp ke 0 (aman). Tidak perlu migrasi data. **Verifikasi ini di e2e resume.**

## Common Pitfalls

### Pitfall 1: Drift `pageQuestionIds`/`allQuestionsData` vs render server
**What goes wrong:** JS `pageQuestionIds` (`StartExam.cshtml:465-470`) & `allQuestionsData` (`:473-479`) saat ini menghitung `pageNumber = index / questionsPerPage` SENDIRI di Razor. Bila render server pakai `PageNumber` section-aware tapi JS map masih `index/perPage`, navigator melompat ke halaman salah.
**Why it happens:** Dua sumber kebenaran terpisah untuk nomor halaman.
**How to avoid:** Serialisasi `pageQuestionIds`/`allQuestionsData` HARUS dibangun dari `q.PageNumber` yang sama (mis. `Model.Questions.GroupBy(q => q.PageNumber)`), bukan `index/perPage`. Satu compute (Pattern 1) → konsisten di Razor + JS.
**Warning signs:** Klik badge navigator ke soal section ber-`StartNewPage` mendarat di halaman salah; `scrollPanelToCurrentPage()` salah.

### Pitfall 2: `totalPages` masih dari `Ceiling(TotalQuestions/perPage)`
**What goes wrong:** `TOTAL_PAGES`/`totalPages` (`StartExam.cshtml:7, 443`) dihitung `Ceiling(N/perPage)`. Dengan page-break `StartNewPage` jumlah halaman BISA LEBIH BANYAK dari `Ceiling` (page tidak penuh karena page-break). Next/Submit button & `changePage` bound salah → soal terakhir tak terlihat / submit muncul prematur.
**How to avoid:** `totalPages = Questions.Max(q => q.PageNumber) + 1`. Pakai nilai ini di Razor (`@for`), `TOTAL_PAGES` const JS (`:443`), dan `updateMobileNavButtons()` (`:1146`).
**Warning signs:** Tombol "Review and Submit" muncul sebelum halaman terakhir; halaman terakhir kosong.

### Pitfall 3: Backward-compat no-Section TIDAK byte-identik
**What goes wrong:** Header Section / label navigator muncul di assessment tanpa Section, atau urutan/halaman bergeser dari perilaku lama. Melanggar invariant D-416-04 (carried) & PAG-01 backward-compat.
**Why it happens:** Lupa branch `hasSections` (semua `SectionId==null`).
**How to avoid:** Branch `bool hasSections = Model.Questions.Any(q => q.SectionNumber != null)` di view. Bila false: no header, navigator flat (perilaku `updatePanel()` lama), indikator tanpa label Section. Pattern 1 sudah menjamin page-map identik saat all-null (lihat invariant). **Wajib golden-test:** render no-Section = identik baseline.
**Warning signs:** ~600 soal legacy + assessment live menampilkan header/label asing.

### Pitfall 4: Header `(lanjutan)` salah deteksi vs Section baru
**What goes wrong:** Auto-split (Section >10 soal pecah ke halaman lanjutan) menampilkan header polos (bukan "(lanjutan)"), atau sebaliknya Section baru di awal halaman menampilkan "(lanjutan)".
**Why it happens:** `IsSectionStart` (Section berubah) vs `IsSectionContinuation` (Section SAMA, tapi soal pertama di halaman baru karena penuh) tidak dibedakan.
**How to avoid:** Bedakan eksplisit (Pattern 1): `IsSectionStart = sectionChanged`; `IsSectionContinuation = !sectionChanged && countOnPage == 0`. Header polos saat `IsSectionStart`, header "(lanjutan)" saat `IsSectionContinuation`.
**Warning signs:** Section 11+ soal: halaman ke-2 tampil nama polos seolah Section baru.

### Pitfall 5: Resume reset ke page 0 (bukan RESUME_PAGE)
**What goes wrong:** Tombol "Lanjutkan" resume (`StartExam.cshtml:1233-1239`) hardcode `currentPage = 0`. PAG-03 minta mendarat di halaman terhitung. Worker resume selalu kembali ke awal.
**How to avoid:** Set `currentPage = RESUME_PAGE` (server sudah clamp) atau panggil `changePage(RESUME_PAGE, true)` di handler tombol resume. Tampilkan toast D-417-06 setelahnya.
**Warning signs:** e2e resume mendarat di soal 1 padahal `LastActivePage > 0`.

### Pitfall 6: Lupa render `q.Section` ter-load di non-StartExam path
**What goes wrong:** Hanya StartExam yang load `q.Section`. Bila ada path lain yang me-render exam (preview impersonate), Section null → grup "Lainnya".
**How to avoid:** Konfirmasi StartExam adalah satu-satunya entry render ujian (verified: legacy path dihapus Phase 227, `CMPController.cs:1320-1325`). `.ThenInclude(q => q.Section)` sudah ada di `:1053`. Tidak ada aksi tambahan, tapi catat di plan.

## Code Examples

### Tambah field Section + PageNumber ke ExamQuestionItem
```csharp
// Source: Models/PackageExamViewModel.cs:25 (ExamQuestionItem) — tambah field
public class ExamQuestionItem
{
    // ...existing: QuestionId, QuestionText, DisplayNumber, Options, QuestionType, MaxCharacters, ImagePath, ImageAlt...

    // Phase 417 PAG-01/02: section-aware pagination metadata (computed at render, NOT persisted — D-11)
    public int? SectionNumber { get; set; }          // null = grup "Lainnya"
    public string? SectionName { get; set; }          // nama Section (D-417-01, name-only)
    public bool SectionStartNewPage { get; set; }     // dari AssessmentPackageSection.StartNewPage
    public int PageNumber { get; set; }               // 0-based, hasil ComputeSectionPages
    public bool IsSectionStart { get; set; }          // header polos (Section change)
    public bool IsSectionContinuation { get; set; }   // header "(lanjutan)" (auto-split, same section)
}
```

### Isi field Section saat build examQuestions (controller)
```csharp
// Source: CMPController.cs:1224-1234 (loop build examQuestions) — tambah assignment field
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
// setelah loop: ComputeSectionPages(examQuestions, questionsPerPage);  // questionsPerPage dari ViewBag (mobile 5)
```
> ⚠️ `questionsPerPage` (mobile override 5) ditentukan di `CMPController.cs:1329-1334` SETELAH vm dibangun. Untuk `ComputeSectionPages` butuh nilai itu lebih awal — **pindahkan deteksi mobile UA ke sebelum compute**, atau hitung page-map di view (tapi view-compute melanggar single-source). Rekomendasi: pindahkan blok mobile UA detection ke atas (sebelum `ComputeSectionPages`).

### ViewBag.SectionConfig (metadata untuk header/indikator)
```csharp
// Source: pola ViewBag di CMPController.cs:1265-1269 — tambah SectionConfig
ViewBag.SectionConfig = packages.First().Questions   // atau dari section lookup distinct
    .Where(q => q.Section != null)
    .Select(q => q.Section!)
    .DistinctBy(s => s.SectionNumber)
    .OrderBy(s => s.SectionNumber)
    .Select(s => new { number = s.SectionNumber, name = s.Name, startNewPage = s.StartNewPage })
    .ToList();
// Catatan: header & indikator JS bisa cukup pakai field di ExamQuestionItem (SectionName/PageNumber).
// SectionConfig opsional bila view butuh metadata di luar per-soal.
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Flat pagination `Skip/Take` per-10 | Section-aware page-map (`PageNumber` per-soal) | Phase 417 (ini) | Render grouping by computed page; backward-compat dijaga via branch `hasSections`. |
| Urutan soal 1 kolam global | Section-aware (Section 1→2→…→Lainnya) | Phase 416 (selesai) | `GetShuffledQuestionIds()` sudah urut Section; 417 tinggal paginate. |
| Tanpa Section data model | `AssessmentPackageSection` + `PackageQuestion.SectionId` + `StartNewPage` | Phase 415 (selesai) | Data + admin toggle + quick-button sudah ada. |

**Deprecated/outdated:**
- Legacy non-package exam path: dihapus Phase 227 (`CMPController.cs:1320-1325`) — StartExam satu-satunya render ujian. Tidak perlu mendukung path lama.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Header Section di-render dari `q.SectionName` (field di `ExamQuestionItem`) cukup; tidak butuh `ViewBag.SectionConfig` terpisah untuk header per-soal. | Code Examples / Pattern 2 | Rendah — bila view butuh metadata Section di luar per-soal (mis. daftar Section untuk indikator), tambah `ViewBag.SectionConfig` (sudah disediakan opsional). Tidak mengubah arsitektur. |
| A2 | Memindahkan deteksi mobile UA (`ViewBag.QuestionsPerPage=5`) ke SEBELUM `ComputeSectionPages` aman (tidak ada dependency lain pada urutan blok itu). | Code Examples (catatan ⚠️) | Sedang — jika compute di controller. Mitigasi: verifikasi tidak ada side-effect lain; alternatif hitung page-map di view (tapi menggandakan logika). Konfirmasi saat implement. |
| A3 | Tidak ada path render ujian selain `StartExam` yang perlu page-map (preview impersonate memakai jalur StartExam yang sama). | Pitfall 6 | Rendah — verified legacy path dihapus Phase 227; impersonate masuk lewat StartExam (read-only, `CMPController.cs:1159-1161`). |

**Catatan:** Sebagian besar klaim di-VERIFIED via pembacaan kode langsung. Item di atas adalah keputusan implementasi yang planner/executor konfirmasi saat eksekusi, bukan fakta teknis yang belum terverifikasi.

## Open Questions

1. **Lokasi `ComputeSectionPages` — Helpers static class vs inline di controller?**
   - What we know: Fungsi murni (NON-RNG, deterministik). Pattern 416 (`ShuffleEngine`) menunjukkan tim memang mengekstrak fungsi murni ke `Helpers/` agar mudah di-unit-test (`SectionScopedShuffleTests.cs`).
   - What's unclear: Apakah cukup private method di `CMPController` atau ekstrak ke `Helpers/SectionPaginator.cs`.
   - Recommendation: **Ekstrak ke `Helpers/`** (mis. `SectionPaginator.ComputePages(...)`) — sejalan saran spec §13 ("lapisan abstraksi urutan-soal") + memudahkan xUnit murni (Wave 0). Blast-radius kecil.

2. **Apakah indikator halaman butuh `pageSectionMap` (page→sectionName) eksplisit di JS?**
   - What we know: Indikator "NamaSection — Halaman n/total" berubah saat `changePage`. Section aktif = section soal pertama halaman itu.
   - What's unclear: Derive dari `allQuestionsData` (filter pageNumber, ambil sectionName pertama) cukup, atau perlu map `page→sectionName` pre-built.
   - Recommendation: Pre-build `pageSectionMap[page] = sectionName` di Razor (dari `Questions.GroupBy(PageNumber)`) — O(1) lookup di `changePage`, hindari scan tiap navigasi. Sumber sama (`q.PageNumber`).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8.0) | `dotnet build`/`run` (CLAUDE.md verify lokal) | ✓ (proyek aktif) | net8.0 | — |
| SQL Server lokal (HcPortalDB_Dev) | DB check + e2e seed (CLAUDE.md, port 5277) | ✓ (proyek aktif) | — | — |
| Node + Playwright (chromium) | e2e render/resume | ✓ | `tests/` (chromium project) | `cd tests; npx playwright install chromium` bila browser absen |
| Bootstrap 5 / SignalR / Bootstrap Icons | Render view | ✓ vendored | `wwwroot/lib/` | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Playwright chromium binary (install command tersedia bila absen).

## Validation Architecture

> nyquist_validation = true (config.json). Section disertakan.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (HcPortal.Tests) + Playwright (tests/e2e, chromium) |
| Config file | `tests/playwright.config.ts` (baseURL http://localhost:5277, fullyParallel:false, --workers=1) |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~SectionPaginat"` |
| Full suite command | `dotnet test HcPortal.Tests` (baseline 665/665 per 416 SUMMARY) + `cd tests && npx playwright test section-pagination.spec.ts` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PAG-01 | 10/halaman mengalir + header saat ganti Section | unit (pure ComputePages) | `dotnet test --filter "Name~PageNumber_FlowsTenPerPage"` | ❌ Wave 0 |
| PAG-01 | No-Section byte-identik baseline flat | unit (golden) | `dotnet test --filter "Name~NoSection_IdenticalToFlatBaseline"` | ❌ Wave 0 |
| PAG-01 | Header Section render saat ganti Section | e2e | `npx playwright test section-pagination.spec.ts -g "header"` | ❌ Wave 0 |
| PAG-02 | StartNewPage → page-break sebelum Section | unit | `dotnet test --filter "Name~StartNewPage_BreaksBeforeSection"` | ❌ Wave 0 |
| PAG-02 | Section >10 soal auto-split + "(lanjutan)" | unit + e2e | `dotnet test --filter "Name~LongSection_AutoSplitsTenPerPage"` / `... -g "lanjutan"` | ❌ Wave 0 |
| PAG-02 | "Lainnya" tak paksa page-break (kecuali penuh) | unit | `dotnet test --filter "Name~LainnyaGroup_NoForcedBreak"` | ❌ Wave 0 |
| PAG-03 | Resume → landing page terhitung (page > 0) | unit (clamp) + e2e | `dotnet test --filter "Name~Resume_ClampsToValidRange"` / `... -g "resume"` | ❌ Wave 0 |
| PAG-03 | LastActivePage out-of-range → fallback page 0 | unit | `dotnet test --filter "Name~Resume_OutOfRange_FallsBackToZero"` | ❌ Wave 0 |
| PAG-03 | Toast "Lanjut dari soal no. X" saat resume > 0 | e2e | `npx playwright test section-pagination.spec.ts -g "toast"` | ❌ Wave 0 |
| (cross) | Navigator per-Section grouping render | e2e | `... -g "navigator"` | ❌ Wave 0 |
| (cross) | Mobile 5/halaman ikut aturan Section | unit (perPage=5) | `dotnet test --filter "Name~MobileFivePerPage_SectionAware"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~SectionPaginat"` (< 30s, fungsi murni)
- **Per wave merge:** `dotnet test HcPortal.Tests` (full xUnit, jaga 665+ baseline hijau)
- **Phase gate:** Full xUnit hijau + `npx playwright test section-pagination.spec.ts` hijau sebelum `/gsd-verify-work` (real-browser UAT WAJIB untuk Razor/JS — lesson 354).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/SectionPaginatorTests.cs` — unit murni untuk `ComputePages` (PAG-01/02/03 logika halaman; pola fixture mirip `SectionScopedShuffleTests.cs` — in-memory, no-DB). Mencakup: flow 10/page, StartNewPage break, auto-split + continuation flag, Lainnya no-force-break, no-Section golden-baseline, mobile perPage=5, resume clamp.
- [ ] `tests/e2e/section-pagination.spec.ts` — e2e render+resume real-browser (pola `scoped-shuffle.spec.ts`: `mode:'serial'`, DB backup/restore beforeAll/afterAll, `createAssessmentViaWizard` + seed Section via SQL UPDATE, login coachee → StartExam). Mencakup: header on section change, "(lanjutan)" auto-split, StartNewPage page-break, navigator grouping, resume landing page + toast, no-Section flat smoke (backward-compat).
- [ ] (opsional) Tidak perlu conftest/fixture baru — `SectionScopedShuffleTests.cs` `PkgSec(...)` fixture pattern reusable; e2e helper `examTypes.ts` sudah ada.
- [ ] Framework install: tidak perlu (xUnit + Playwright sudah terpasang).

## Security Domain

> security_enforcement tidak di-set false di config → enabled. Phase 417 = render/compute UI; permukaan keamanan minimal (tak ada endpoint mutasi baru, tak ubah grading/skor/auth).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ada auth baru; StartExam authz (owner/Admin/HC) existing (`CMPController.cs:920`). |
| V3 Session Management | no | `LastActivePage` page-index, bukan auth session; existing. |
| V4 Access Control | no | Tak ada endpoint mutasi baru di 417 (quick-button sudah ada+ber-`[Authorize(Roles="Admin,HC")]` + `[ValidateAntiForgeryToken]` di Phase 415). |
| V5 Input Validation | yes (ringan) | Server-authoritative page compute (tak percaya input client page). `currentPage` dari client di `UpdateSessionProgress` di-clamp/abaikan bila invalid (existing autosave; resume re-clamp di server). |
| V6 Cryptography | no | Tidak ada. |
| V14 Output Encoding (XSS) | yes | Nama Section di-render. Razor auto-encode `@q.SectionName`. JS navigator: gunakan `textContent`/`innerText` (bukan `innerHTML`) untuk label Section (pola existing `StartExam.cshtml:1106` `btn.innerText`). |

### Known Threat Patterns for .NET MVC/Razor + JS

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via nama Section (HC-controlled) di header/navigator | Tampering/Info | Razor `@` auto-encode; JS `textContent`/`innerText`, JANGAN `innerHTML` (pola existing). Konsisten UI-SPEC (resume toast XSS-safe). |
| Client memalsukan `currentPage`/`LastActivePage` untuk lompat soal | Tampering | Page compute server-authoritative; resume clamp di server (`ViewBag.LastActivePage`). Page-index bukan kontrol akses — semua soal sudah terkirim ke client by design (existing). Risiko rendah. |
| Section config diubah pasca-lock → page shift | Tampering (HC, by-design) | Recompute dari config + identitas soal stabil by id + fallback page 0 (D-417-05). Bukan ancaman eksternal; aturan bisnis. |

**Catatan:** Tidak ada perubahan pada grading, skor, sertifikat, atau RBAC. Permukaan keamanan baru = output-encoding nama Section (XSS) — tertutup oleh Razor auto-encode + JS `textContent` pattern yang sudah dipakai. Threat model tetap dalam batas existing exam-render.

## Sources

### Primary (HIGH confidence — kode live dibaca langsung)
- `Views/CMP/StartExam.cshtml` (1642 baris penuh) — pagination flat, pageQuestionIds/allQuestionsData, changePage/performPageSwitch, updatePanel, showResumeFailureToast, resume init, mobile nav, CSS grid navigator.
- `Controllers/CMPController.cs:910-1337` — StartExam: load `.Include(q.Section)`, BuildQuestionAssignment, loop examQuestions, ViewBag.LastActivePage/IsResume/SavedAnswers, mobile UA QuestionsPerPage. `:439-482` UpdateSessionProgress. `:1195` reset null.
- `Models/PackageExamViewModel.cs` — ExamQuestionItem (gap: tak ada field Section).
- `Models/AssessmentPackage.cs` — AssessmentPackageSection (SectionNumber, Name, StartNewPage, ShuffleEnabled); PackageQuestion.SectionId.
- `Models/AssessmentSession.cs:59` — LastActivePage int?.
- `Helpers/ShuffleEngine.cs` — BuildQuestionAssignment section-aware (urutan Section 1→2→…→Lainnya).
- `Controllers/AssessmentAdminController.cs:6428-6452` — SetAllSectionsNewPage (quick-button SUDAH ADA). `:4947` reset null. `:6296-6451` Section CRUD.
- `Views/Admin/ManagePackageQuestions.cshtml:86-90,116-156,227` — quick-button + StartNewPage toggle UI (SUDAH ADA Phase 415).
- `wwwroot/js/assessment-hub.js` — SignalR connection wrapper (flush guard ada di StartExam.cshtml).
- `HcPortal.Tests/SectionScopedShuffleTests.cs` — pola fixture pure unit test (PkgSec).
- `tests/e2e/scoped-shuffle.spec.ts` + `tests/playwright.config.ts` — pola e2e section.

### Secondary (MEDIUM confidence — spec/dokumen proyek)
- `docs/superpowers/specs/2026-06-22-...-design.md` §7.1/§7.2 (pagination), §15.A (resume/Lainnya), §15.D (admin Kelola Section), §15.E (mobile/autosave/navigator), D-10/D-11/D-15.
- `.planning/phases/417-section-pagination/417-CONTEXT.md` (D-417-01..06), `417-UI-SPEC.md` (IC-1..IC-9).
- `.planning/phases/416-scoped-shuffle-acak-per-section/416-01-SUMMARY.md` (urutan section-aware, wiring `.Include(q.Section)`).
- `.planning/REQUIREMENTS.md` (PAG-01/02/03 mapping).

## Project Constraints (from CLAUDE.md)

- **Respon Bahasa Indonesia** (dokumen + UI copy).
- **Verifikasi lokal WAJIB sebelum commit:** `dotnet build` + `dotnet run` (cek http://localhost:5277) + cek DB lokal + Playwright bila ada. Real-browser UAT untuk Razor/JS/SignalR (lesson 354).
- **Jangan edit kode/DB langsung di Dev/Prod.** Promosi ke Dev = tanggung jawab IT (notify dengan commit hash + flag migration). **migration=FALSE** untuk 417.
- **Jangan push tanpa verifikasi lokal.**
- **Seed Data Workflow:** untuk e2e/test yang butuh seed — klasifikasi (temporary+local-only), snapshot DB lokal (sqlcmd BACKUP), catat `docs/SEED_JOURNAL.md`, RESTORE setelah test. Pola `scoped-shuffle.spec.ts` (backup/restore beforeAll/afterAll) sudah mematuhi ini.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency dibaca langsung dari repo (net8.0, Bootstrap 5, SignalR, xUnit, Playwright vendored). Tak ada paket baru.
- Architecture/Patterns: HIGH — page-compute algoritma di-derive dari spec §7.2 + struktur kode existing (StartExam.cshtml + ShuffleEngine urutan section-aware). Backward-compat invariant terverifikasi by construction.
- Pitfalls: HIGH — semua pitfall di-grounding ke baris kode spesifik (pageQuestionIds compute, totalPages Ceiling, resume currentPage=0 hardcode).
- Don't Hand-Roll: HIGH — quick-button + toggle + toast + flush guard + urutan + reset-null SEMUA dikonfirmasi sudah ada via grep/read.

**Research date:** 2026-06-23
**Valid until:** 2026-07-23 (stable brownfield; valid selama Phase 415/416 tidak di-revert)

---
*Phase: 417-section-pagination*
*Researched: 2026-06-23*
