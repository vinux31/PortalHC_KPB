# Phase 349: ManageAssessment + Monitoring LOW Polish - Research

**Researched:** 2026-06-05
**Domain:** ASP.NET Core 8 MVC Razor view polish (i18n / a11y / empty-state / display-nits / code-hygiene) — NO logic-bearing change
**Confidence:** HIGH (semua target file di-Read langsung pasca-348; line number ter-verifikasi pada working tree commit `d998b256`)

> Semua klaim di dokumen ini ditandai provenance: `[VERIFIED: …]` (dikonfirmasi via Read/Grep di sesi ini), `[CITED: …]` (dari dokumen spec/context project), `[ASSUMED]` (pengetahuan training, belum diverifikasi). Lihat **Assumptions Log** di bawah.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (carry-forward dari Phase 348 — tidak re-discuss)
- **D-A:** No migration, no schema change. `[CITED: 349-CONTEXT.md:18]`
- **D-B:** Sequential strict — Phase 349 SETELAH 348 selesai (348 COMPLETE 2026-06-05). Basis kode = pasca-348. Jangan paralel. `[CITED: 349-CONTEXT.md:19]`
- **D-C:** Pakai konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` (= `"Menunggu Penilaian"`) + label "Menunggu Penilaian". BUKAN literal string. Berlaku MAP-18, MAP-20. `[CITED: 349-CONTEXT.md:20]`
- **D-D:** M4 (Tab3 History PendingGrading) sudah dicakup REC-07/346 — MAP-20 = badge tambahan opsional, jangan duplikat logic. `[CITED: 349-CONTEXT.md:21]`

### Gray-area resolved (user pilih SEMUA rekomendasi 2026-06-05)
- **D-01 (MAP-06):** Tombol clear filter Tab1 → relabel **"Reset Semua Filter"** yang hapus SEMUA (search + kategori + status) sekaligus. BUKAN clear-search-only. `[CITED: 349-CONTEXT.md:24]`
- **D-02 (MAP-05/07/08):** Empty-state/feedback **full**:
  - MAP-05: Tab1 empty-state filter-aware — kategori/status aktif + search kosong → "Tidak ada assessment untuk filter ini" + Reset (bukan "Buat assessment pertama").
  - MAP-07: Tab3 client-filter 0-match → inject baris "Tidak ada hasil untuk filter ini." + `aria-live="polite"` (assessment & training).
  - MAP-08: Tab3 tambah baris **"Menampilkan X dari Y"** (visible-row count ikut filter aktif). `[CITED: 349-CONTEXT.md:25-28]`
- **D-03 (MAP-10/12):** Monitoring Detail summary cards **lengkap**:
  - MAP-10: tambah kartu **"Abandoned"** supaya Total = jumlah semua kartu; sync JS `updateSummaryFromDOM`.
  - MAP-12: tombol **"Akhiri Semua Ujian" kondisional** — render hanya bila `@Model.InProgressCount > 0 || Model.GroupStatus == "Open"`; modal wording predikat identik aksi cancel. `[CITED: 349-CONTEXT.md:29-31]`
- **D-04 (MAP-17):** Monitoring list grup Pre-Post token-required → tambah **"Regenerate Token"** (target `LinkedGroupId`, koord MAM-01 Phase 348). BUKAN minimal "View Detail" saja. `[CITED: 349-CONTEXT.md:32]`

### Claude's Discretion (mekanis / spec-decided — planner ikuti spec §Phase 349)
MAP-01 (i18n Detail), MAP-02 (label "NIP"), MAP-03 (chevron+aria), MAP-04 (drop ARIA nested-interactive), MAP-09 (skeleton match), MAP-11 (InProgressCount + drop dead var), MAP-13 (TotalCount exclude Cancelled), MAP-14 (buang "real-time"), MAP-15 (Status dropdown jangan misrepresent), MAP-16 (buang kategori dobel), MAP-18 (tri-state IsPassed==null), MAP-19 (selalu render CompletionDisplayText), MAP-20 (badge History pending — konstanta D-C), MAP-21 (paging.Take ViewBag), MAP-22 (drop param mati), MAP-23 (extend search ke Category). `[CITED: 349-CONTEXT.md:34-50]`

### Deferred Ideas (OUT OF SCOPE)
- Extend search Monitoring list ke Nama/NIP per-user — OUT OF SCOPE (list aggregate, hanya Category ditambah, MAP-23). `[CITED: 349-CONTEXT.md:100]`
- Resource-file i18n (proper localization framework) — out of scope; codebase pakai inline Indonesia. `[CITED: 349-CONTEXT.md:101]`
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description (ringkas) | Research Support (lokasi pasca-348 + temuan kunci) |
|----|-----------------------|-----------------------------------------------------|
| MAP-01 | i18n Monitoring Detail EN→ID | `AssessmentMonitoringDetail.cshtml` — header tabel L214-220, kartu L150/156/162/168, "Back to Monitoring" L72, "Per-User Status" L183, "Export Results" L192, "No sessions found" L227. Murni teks Razor. |
| MAP-02 | Standar label identitas "NIP" | `_HistoryTab.cshtml` — Assessment SUDAH "NIP" (header L62, placeholder L33). **Tinggal:** Training header L178 ("Nopeg"→"NIP") + Training placeholder L159 ("Nopeg"→"NIP"). |
| MAP-03 | Chevron + aria-label toggle collapse | Tab1 "N peserta" `_AssessmentGroupsTab.cshtml:232-237` (button, NO aria-label, NO chevron). Tab2 expand `_TrainingRecordsTab.cshtml:249-254` (ADA aria-expanded + title, chevron `bi-chevron-down` STATIS — perlu rotate via CSS `[aria-expanded="true"]`). |
| MAP-04 | Drop ARIA nested-interactive Tab3 | `_HistoryTab.cshtml:78-81` — `<tr role="link" tabindex="0">` + `<a>` "Lihat" L112 = nested-interactive. Drop `role`/`tabindex`/`aria-label` dari `<tr>` (+ hapus JS click/keydown L132-147 yang navigasi row). Simpan tombol "Lihat". |
| MAP-05 | Tab1 empty-state filter-aware | `_AssessmentGroupsTab.cshtml:133-156` — empty-state hanya bedakan `searchTerm` vs tidak. Tambah cabang: kategori/status aktif + search kosong → pesan + Reset. ViewBag tersedia: `selectedAssessmentCategory`, `selectedAssessmentStatus`, `searchTerm`. |
| MAP-06 | Relabel "Reset Semua Filter" (D-01) | `_AssessmentGroupsTab.cshtml:143-146` (link "Hapus Pencarian" di empty-state) + tombol reset existing L100-108 sudah hapus SEMUA. D-01 = jadikan satu pesan jujur. |
| MAP-07 | Tab3 client-filter 0-match message | `ManageAssessment.cshtml` — `filterAssessmentRows()` L316-327 + `filterTrainingRows()` L330-337. Inject baris "Tidak ada hasil…" + `aria-live="polite"` saat semua row `display:none`. |
| MAP-08 | Tab3 "Menampilkan X dari Y" | `_HistoryTab.cshtml:13,20` (badge count statis `.Count`) + filter JS di `ManageAssessment.cshtml`. Hitung visible-row pasca-filter. |
| MAP-09 | Skeleton loader match kolom asli | `ManageAssessment.cshtml` — Tab2 skeleton L149 (4 filter; ASLI 5: section/category/unit/status/search) + L156-165 (5 kolom; ASLI 7). History skeleton L186-198 (2 filter OK; 4 kolom — ASLI Assessment 8, Training 5). |
| MAP-10 | Detail card "Abandoned" (D-03) | `AssessmentMonitoringDetail.cshtml:146-177` (5 card; NO Abandoned, NO MenungguPenilaian). `updateSummaryFromDOM` L1281-1300 (else→notStarted). **ViewModel TANPA `AbandonedCount`** — perlu tambah field + controller assign (L3320-3336). |
| MAP-11 | InProgressCount + drop dead var | `AssessmentMonitoringDetail.cshtml:161` (inline LINQ → `@Model.InProgressCount`). Dead var `completedPct` L33-35 + `passRatePct` L37-39 → DROP (D pilih minimal-risk). |
| MAP-12 | "Akhiri Semua Ujian" kondisional (D-03) | `AssessmentMonitoringDetail.cshtml:196-199` (selalu render). Gate `@if (Model.InProgressCount > 0 || Model.GroupStatus == "Open")`. Modal wording L541-542 (JS populate via `GetAkhiriSemuaCounts`). |
| MAP-13 | Monitor list TotalCount exclude Cancelled | `AssessmentAdminController.cs` — Pre-Post `TotalCount` L2811 + standard `TotalCount` L2875 = `g.Count()`. Ubah ke exclude Cancelled. **CATATAN:** standard `CancelledCount` TIDAK di-set (L2868-2882 missing) — parity perlu tambah. |
| MAP-14 | Buang "real-time" subtitle | `AssessmentMonitoring.cshtml:27` — `<p class="text-muted">…real-time…</p>`. Murni teks. |
| MAP-15 | Status dropdown jangan misrepresent | `AssessmentAdminController.cs:2898-2911` — search non-empty + status kosong → no filter (Closed ikut) tapi `ViewBag.SelectedStatus` kosong → view default "active". Set `status="All"` saat search broaden scope. View L12/79-120. |
| MAP-16 | Buang kategori dobel | `AssessmentMonitoring.cshtml:256-259` (muted subtitle `@group.Category`) vs L271 (badge `@group.Category`). Hapus subtitle dup. |
| MAP-17 | Pre-Post Regenerate Token (D-04) | `AssessmentMonitoring.cshtml:297-327` — `<td>` Aksi KOSONG untuk `IsPrePostGroup` (dropdown hanya `!IsPrePostGroup`). Tambah dropdown View Detail + Regenerate Token. Handler `RegenerateToken(int id)` L2616 SUDAH LinkedGroupId-aware (MAM-01). JS `.btn-regenerate-token` L413-436 reuse. |
| MAP-18 | Tab2 tri-state IsPassed==null | `_TrainingRecordsTab.cshtml:266` — `a.IsPassed == true ? "Lulus" : "Tidak Lulus"` (null→"Tidak Lulus" SALAH) + Status `a.IsPassed == false ? "Failed" : ""` (null→""). Tambah cabang null→"Menunggu Penilaian" (konstanta D-C). |
| MAP-19 | Tab2 selalu render CompletionDisplayText | `_TrainingRecordsTab.cshtml:239-246` — `worker.TotalTrainings == 0 → "Belum ada"`. D pilih: selalu `CompletionDisplayText`. |
| MAP-20 | Tab3 History badge pending (koord D-D) | `_HistoryTab.cshtml:102-106` — **SUDAH DONE** Phase 346 REC-07 (badge `@AssessmentConstants.AssessmentStatus.PendingGrading`). MAP-20 = verifikasi/no-op (jangan duplikat). |
| MAP-21 | Drop magic-number 20 → paging.Take | `_AssessmentGroupsTab.cshtml:16` (`pageSize` hidden), L180 (`rowNum`), L366 (text). Controller `ManageAssessmentTab_Assessment` L210-218 set CurrentPage/TotalPages/TotalCount tapi **TIDAK** `ViewBag.PageSize`. Expose `paging.Take`. (Tab2 SUDAH expose `ViewBag.PageSize` L268/279 — referensi pola.) |
| MAP-22 | Drop param mati | `ManageAssessmentTab_History` L301-302: `page`/`pageSize`/`statusFilter` TAK DIPAKAI (hanya `page` di-log L319). `ManageAssessmentTab_Training` L245: `page`/`pageSize` SEKARANG DIPAKAI (MAM-07) → **bukan dead lagi**. Re-evaluasi: drop History 3 param + cleanup wiring `ManageAssessment.cshtml:17,19`. |
| MAP-23 | Extend search Monitoring ke Category | `AssessmentAdminController.cs:2739-2743` — search hanya `Title.Contains`. Tambah `|| a.Category.ToLower().Contains(lower)`. Update placeholder `AssessmentMonitoring.cshtml:75`. |
</phase_requirements>

---

## Summary

Phase 349 adalah **murni polish view** pada 7 file yang SEMUANYA dimodifikasi di Phase 348. Tidak ada migration, schema, atau perubahan behavior. 23 REQ (MAP-01..23) menutup 29 LOW dari audit 2026-06-04. Mayoritas adalah edit teks Razor (i18n), atribut HTML (a11y), markup kondisional (empty-state), dan pembersihan kode (magic-number, dead var, dead param). `[VERIFIED: semua 7 file di-Read 2026-06-05]`

**Drift line number signifikan:** spec menulis line pre-348; sebagian SUDAH bergeser ATAU SUDAH dikerjakan Phase 348. Tiga temuan penting yang mengubah scope:
1. **MAP-13 reshuffleWorker** (disebut di spec MAM-13) **SUDAH DONE** di Phase 348 — `function reshuffleWorker(btn)` ada di `AssessmentMonitoringDetail.cshtml:739`. (Catatan: MAP-13 di Phase 349 = TotalCount exclude Cancelled, item berbeda, BELUM done.)
2. **MAP-20** (Tab3 History badge "Menunggu Penilaian") **SUDAH DONE** di `_HistoryTab.cshtml:102-106` (Phase 346 REC-07). MAP-20 = verifikasi/no-op.
3. **MAP-22** sebagian obsolet: `ManageAssessmentTab_Training` `page`/`pageSize` SEKARANG dipakai (Phase 348 MAM-07). Hanya `ManageAssessmentTab_History` yang masih punya 3 param mati.

**Primary recommendation:** Kelompokkan per-file/per-grup (i18n, a11y, empty-state, display-nits, code-hygiene). Sebagian besar = view-only (visual verify via Playwright). Hanya **MAP-10** (tambah `AbandonedCount` field ViewModel + controller), **MAP-13** (TotalCount exclude Cancelled), **MAP-15** (Status ViewBag), **MAP-21** (paging.Take ViewBag), **MAP-22** (drop param), **MAP-23** (search Category) yang menyentuh controller/ViewModel (logic-bearing-ringan, testable). Verifikasi `dotnet build HcPortal.csproj -c Debug` tiap commit (CLAUDE.md mandat). `[VERIFIED]`

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| i18n teks UI (MAP-01/02/14) | Razor View | — | Teks hardcoded di .cshtml; codebase pakai inline Indonesia (bukan resource file) `[CITED: 349-CONTEXT.md:81]` |
| a11y atribut + chevron CSS (MAP-03/04) | Razor View | Browser (CSS rotate) | aria-label/role/tabindex + `[aria-expanded]` CSS = client-side affordance |
| Empty-state/feedback markup (MAP-05/06/07/08) | Razor View | Browser (client-filter JS) | Tab1/Tab2 server-render empty-state; Tab3 client-filter JS inject 0-match row |
| Skeleton loader (MAP-09) | Razor View | — | Placeholder markup statis di host page |
| Summary card "Abandoned" (MAP-10) | API/Backend (ViewModel+controller) | Razor View + Browser (JS sync) | Butuh field `AbandonedCount` baru + JS `updateSummaryFromDOM` count Abandoned |
| Display binding/dead-var (MAP-11/16/18/19) | Razor View | — | Ganti binding source / hapus markup dobel; tanpa query change |
| Conditional render button (MAP-12/17) | Razor View | — | `@if` gate berdasarkan field ViewModel existing (InProgressCount/GroupStatus/IsTokenRequired) |
| Aggregate count correctness (MAP-13/15) | API/Backend (controller EF query) | Razor View | `g.Count(...)` exclude Cancelled + `ViewBag.SelectedStatus` accuracy |
| Pagination metadata (MAP-21) | API/Backend (controller ViewBag) | Razor View | Expose `paging.Take` via ViewBag, view konsumsi |
| Dead param cleanup (MAP-22) | API/Backend (controller signature) | Razor View (wiring) | Drop param mati action + cleanup URL wiring host page |
| Search scope (MAP-23) | API/Backend (controller EF query) | Razor View (placeholder) | Tambah `Category.Contains` ke search predicate |

> **Catatan tier:** 13 dari 23 MAP = View-only. 6 menyentuh controller/ViewModel (MAP-10/13/15/21/22/23). MAP-20 = no-op (sudah done). Tidak ada yang menyentuh Database/Storage tier (no migration). `[VERIFIED]`

## Standard Stack

Phase ini **TIDAK menambah dependency**. Semua MAP pakai stack existing. `[VERIFIED: HcPortal.csproj]`

### Core (existing — tidak install baru)
| Komponen | Versi | Purpose | Catatan |
|----------|-------|---------|---------|
| ASP.NET Core MVC | net8.0 | Razor view rendering, controller actions | `[VERIFIED: HcPortal.csproj target net8.0]` |
| HTMX | 2.0.x (vendored `~/lib/htmx/htmx.min.js`) | Tab partial swap + filter/pagination re-fetch | `[VERIFIED: ManageAssessment.cshtml:206]` |
| Bootstrap | 5.x (collapse, dropdown, modal, badge, placeholder-glow) | UI komponen + skeleton loader | `[VERIFIED: markup _AssessmentGroupsTab/ManageAssessment]` |
| Bootstrap Icons | (bi-*) | chevron (`bi-chevron-down/up`), ikon aksi | `[VERIFIED: markup semua view]` |
| jQuery + $.ajax | (existing) | reshuffle/regenerate AJAX di Detail | `[VERIFIED: AssessmentMonitoringDetail.cshtml:746]` |
| SignalR (`window.assessmentHub`) | (existing) | live push Monitoring Detail (TIDAK di Monitoring list — basis MAP-14) | `[VERIFIED: AssessmentMonitoringDetail.cshtml:1302+]` |
| `IOrgLabelService OrgLabels` | (existing, @inject) | label organisasi dinamis (Bagian/Unit/Sub-unit) | `[VERIFIED: _ViewImports.cshtml:6]` — TIDAK relevan MAP (label org bukan target) |
| `AssessmentConstants` | (existing) | konstanta status (PendingGrading, Cancelled, Completed) | `[VERIFIED: Models/AssessmentConstants.cs:13-21]` |

**Version verification:** Tidak ada package baru → tidak ada `npm view`/`dotnet add`. Build verify saja: `dotnet build HcPortal.csproj -c Debug`. `[VERIFIED: CLAUDE.md develop workflow]`

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Inline Indonesia (MAP-01/02/14) | Resource-file (.resx) localization | OUT OF SCOPE per D — codebase konvensi inline; .resx = over-engineering untuk 1 milestone `[CITED: 349-CONTEXT.md:101]` |
| Drop dead var (MAP-11) | Surface `completedPct`/`passRatePct` jadi progress bar | D pilih DROP (minimal-risk, hindari display baru) `[CITED: 349-CONTEXT.md:40]` |
| Drop param mati (MAP-22) | Dokumentasi intent Phase 322 | D pilih DROP `[CITED: 349-CONTEXT.md:49]` |

**Installation:** Tidak ada. `[VERIFIED]`

## Architecture Patterns

### System Architecture Diagram (data flow — fitur tersentuh)

```
                    ┌─────────────────────────────────────────────┐
   Browser          │  ManageAssessment.cshtml (HTMX host 3-tab)  │
   (admin/HC)  ───► │  - Tab1 Assessment  Tab2 Training  Tab3 Hist │
                    └───────────────┬─────────────────────────────┘
                                    │ hx-get (partial swap)
                    ┌───────────────▼─────────────────────────────┐
                    │  AssessmentAdminController                    │
                    │   ManageAssessmentTab_Assessment (L106)       │──► EF query AssessmentSessions
                    │   ManageAssessmentTab_Training   (L245)       │──► GetWorkersInSection + paginate
                    │   ManageAssessmentTab_History    (L301)       │──► GetAllWorkersHistory
                    └───────┬───────────┬───────────┬───────────────┘
                            │           │           │ PartialView
              ┌─────────────▼┐  ┌───────▼──────┐  ┌─▼──────────────┐
              │_AssessmentGr.│  │_TrainingReco.│  │_HistoryTab     │
              │ MAP-03/05/06 │  │ MAP-03/18/19 │  │ MAP-02/04/08/20│
              │ /21          │  │              │  │ (client-filter)│
              └──────────────┘  └──────────────┘  └────────────────┘
                                                    │ filter JS (MAP-07)
                                                    ▼ inject 0-match row

   ── Monitoring (separate page, full reload — NO HTMX) ──
                    ┌─────────────────────────────────────────────┐
   Browser    ───► │  AssessmentMonitoring (list)  GET            │
                    │   controller L2724  → search/status/category │ MAP-13/15/16/17/23
                    │   AssessmentMonitoring.cshtml  MAP-14        │
                    └───────────────┬─────────────────────────────┘
                                    │ <a href> detailUrl
                    ┌───────────────▼─────────────────────────────┐
                    │  AssessmentMonitoringDetail  GET (L3227)     │ MAP-01/10/11/12
                    │   ◄── SignalR push (workerSubmitted/Started) │
                    │   updateSummaryFromDOM JS (MAP-10 sync)      │
                    └─────────────────────────────────────────────┘
```
`[VERIFIED: trace dari Read semua file]`

### Recommended Project Structure (file tersentuh — TIDAK ada file baru)
```
Controllers/
└── AssessmentAdminController.cs   # MAP-10/13/15/21/22/23 (controller-side)
Models/
└── AssessmentMonitoringViewModel.cs  # MAP-10 (tambah AbandonedCount field)
Views/Admin/
├── ManageAssessment.cshtml        # MAP-07/09/22 (host: client-filter JS + skeleton + wiring)
├── AssessmentMonitoring.cshtml    # MAP-14/15/16/17/23
├── AssessmentMonitoringDetail.cshtml  # MAP-01/10/11/12
└── Shared/
    ├── _AssessmentGroupsTab.cshtml    # MAP-03/05/06/21
    ├── _TrainingRecordsTab.cshtml     # MAP-03/18/19
    └── _HistoryTab.cshtml             # MAP-02/04/08/20(no-op)
```
`[VERIFIED]`

### Pattern 1: i18n inline Razor (MAP-01/02/14)
**What:** Ganti string literal Inggris di view jadi Indonesia langsung di markup.
**When to use:** Semua teks chrome (header tabel, label kartu, tombol, subtitle).
**Example (analog Phase 347 POL-01 + existing Monitoring list yang SUDAH Indonesia):**
```cshtml
@* AssessmentMonitoring.cshtml SUDAH pakai Indonesia: "Peserta/Selesai/Lulus/Aksi" — parity untuk Detail *@
<th>Nama</th>        @* dari "Name" *@
<th>Progres</th>     @* dari "Progress" *@
<th>Status</th>      @* tetap *@
<th>Nilai</th>       @* dari "Score" *@
<th>Hasil</th>       @* dari "Result" *@
<th>Selesai Pada</th>@* dari "Completed At" *@
<th>Aksi</th>        @* dari "Actions" *@
```
`[VERIFIED: AssessmentMonitoring.cshtml:197-201 sudah Indonesia; AssessmentMonitoringDetail.cshtml:214-220 masih Inggris]`

### Pattern 2: Chevron rotate via CSS `[aria-expanded]` (MAP-03)
**What:** Toggle button collapse pakai `aria-expanded` (Bootstrap auto-set) + CSS rotate chevron.
**When to use:** Tab1 "N peserta" + Tab2 expand-records.
**Example (analog Monitoring list `.ppt-expand-btn` yang SUDAH punya chevron toggle via JS):**
```cshtml
@* Pola CSS-only (lebih simpel dari JS Monitoring list L401-411): *@
<button class="btn btn-sm btn-link toggle-chevron" type="button"
        data-bs-toggle="collapse" data-bs-target="#@collapseId"
        aria-expanded="false" aria-controls="@collapseId"
        aria-label="Tampilkan/sembunyikan @userCount peserta">
    <i class="bi bi-people me-1"></i>@userCount peserta
    <i class="bi bi-chevron-down chevron-icon ms-1"></i>
</button>
<style>
  .toggle-chevron[aria-expanded="true"] .chevron-icon { transform: rotate(180deg); }
  .chevron-icon { transition: transform 0.2s; }
</style>
```
`[VERIFIED: _AssessmentGroupsTab.cshtml:232-237 belum punya aria-label/chevron; AssessmentMonitoring.cshtml:401-411 contoh chevron toggle via JS]`
**Catatan:** Tab2 (`_TrainingRecordsTab.cshtml:249-254`) sudah punya `aria-expanded` + chevron `bi-chevron-down` STATIS — tinggal tambah CSS rotate + aria-label deskriptif.

### Pattern 3: Drop ARIA nested-interactive (MAP-04)
**What:** Hilangkan `<tr role="link" tabindex="0">` (yang membungkus `<a>` interaktif) — pilih SATU affordance.
**When to use:** Tab3 Riwayat Assessment drill-down.
**Example:**
```cshtml
@* SEBELUM (nested-interactive — <a> di dalam <tr role=link>): *@
<tr class="assessment-history-row @(hasDrillDown ? "cil03-row-link" : "")"
    @(hasDrillDown ? Html.Raw("data-href=... tabindex=0 role=link aria-label=...") : ...)>
    ... <a href="/CMP/Results/...">Lihat</a> ...

@* SESUDAH (drop role/tabindex/aria-label dari <tr>; simpan tombol "Lihat"): *@
<tr class="assessment-history-row">
    ... <a href="/CMP/Results/...">Lihat</a> ...
```
**PITFALL:** Hapus JUGA JS click/keydown handler `_HistoryTab.cshtml:132-147` (row navigation) + CSS `tr.cil03-row-link` L127-131 yang jadi dead. Kalau tidak, row tetap clickable tanpa role (UX inkonsisten). `[VERIFIED: _HistoryTab.cshtml:78-81 + 132-147]`

### Pattern 4: Client-filter 0-match injection (MAP-07/08)
**What:** Saat semua row `display:none` pasca-filter JS, inject baris pesan + hitung visible count.
**When to use:** Tab3 `filterAssessmentRows()` + `filterTrainingRows()`.
**Example:**
```javascript
function filterAssessmentRows() {
    // ... existing filter loop ...
    var visible = 0;
    rows.forEach(function(row){ if (row.style.display !== 'none') visible++; });
    // MAP-08: update "Menampilkan X dari Y"
    var counter = document.getElementById('assessmentVisibleCount');
    if (counter) counter.textContent = 'Menampilkan ' + visible + ' dari ' + rows.length;
    // MAP-07: 0-match row (aria-live polite)
    var noResult = document.getElementById('assessmentNoResult');
    if (noResult) noResult.style.display = (visible === 0) ? '' : 'none';
}
```
**PITFALL:** Pesan 0-match + counter HARUS ada di markup `_HistoryTab.cshtml` (di-render server-side), JS hanya toggle display. `aria-live="polite"` di container 0-match agar screen-reader umumkan. `[VERIFIED: ManageAssessment.cshtml:316-337]`

### Pattern 5: Conditional button render (MAP-12/17)
**What:** Gate tombol dengan `@if` berdasarkan field ViewModel.
**Example MAP-12:**
```cshtml
@if (Model.InProgressCount > 0 || Model.GroupStatus == "Open")
{
    <button type="button" class="btn btn-danger btn-sm" id="akhiriSemuaBtn" ...>
        <i class="bi bi-x-circle me-1"></i>Akhiri Semua Ujian
    </button>
}
```
**Example MAP-17 (Pre-Post di Monitoring list — reuse dropdown `!IsPrePostGroup`):**
```cshtml
@* AssessmentMonitoring.cshtml:298 — extend ke IsPrePostGroup juga: *@
<div class="dropdown">
    <button class="btn btn-sm btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown">Aksi</button>
    <ul class="dropdown-menu dropdown-menu-end">
        <li><a class="dropdown-item" href="@detailUrl"><i class="bi bi-eye me-1"></i>View Detail</a></li>
        @if (group.IsTokenRequired)
        {
            <li><hr class="dropdown-divider"></li>
            <li><button type="button" class="dropdown-item text-warning btn-regenerate-token"
                        data-id="@group.RepresentativeId">  @* RepresentativeId = PreTest rep, punya LinkedGroupId → MAM-01 regen all siblings *@
                <i class="bi bi-arrow-repeat me-1"></i>Regenerate Token</button></li>
        }
    </ul>
</div>
```
`[VERIFIED: AssessmentMonitoring.cshtml:297-327 Aksi <td> kosong untuk Pre-Post; RegenerateToken L2616 LinkedGroupId-aware]`

### Pattern 6: Aggregate count exclude (MAP-13) — konstanta
**What:** Ubah `g.Count()` jadi `g.Count(a => a.Status != Cancelled)` agar progress bar bisa 100%.
**Example:**
```csharp
// AssessmentAdminController.cs:2875 (standard group) + L2811 (Pre-Post):
TotalCount = g.Count(a => a.Status != AssessmentConstants.AssessmentStatus.Cancelled),
// + parity: tambah CancelledCount ke standardGroups (saat ini MISSING L2868-2882):
CancelledCount = g.Count(a => a.Status == AssessmentConstants.AssessmentStatus.Cancelled),
```
**PITFALL:** Pre-Post `TotalCount` L2811 = `postSubs.Count > 0 ? postSubs.Count : preSubs.Count` (bukan `g.Count()`) — exclude Cancelled harus apply ke `postSubs`/`preSubs` count, BUKAN `g`. Verifikasi konsistensi `progressPct` view L230-232 (`CompletedCount/TotalCount`). `[VERIFIED: controller L2811/2875]`

### Anti-Patterns to Avoid
- **Literal "Menunggu Penilaian":** MAP-18/20 WAJIB konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` (D-C). `[CITED: 349-CONTEXT.md:20]`
- **Literal "Cancelled" di MAP-13:** tersedia `AssessmentConstants.AssessmentStatus.Cancelled` — pakai konstanta (meski controller existing kadang literal, polish = konsisten). `[VERIFIED: AssessmentConstants.cs:20]`
- **Ubah signature `GetWorkersInSection`/`GetAllWorkersHistory`:** out-of-scope (no logic change). MAP-22 hanya drop param di ACTION, bukan service.
- **Tambah display/feature baru:** MAP-11 drop dead var (BUKAN surface jadi progress bar) — D minimal-risk.
- **Nested-interactive baru:** MAP-04 menghapus, jangan ciptakan ulang di MAP-03 (button collapse OK, `<tr>` interaktif TIDAK).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Pagination metadata (MAP-21) | Hitung skip/take manual di view | `PaginationHelper.Calculate` → `paging.Take` via ViewBag | Sudah ada, ter-test (98/98 Phase 348); Tab2 sudah pakai pola ini `[VERIFIED: Helpers/PaginationHelper.cs + controller L274]` |
| Status derivation (MAP-10 sync) | Re-derive status di JS dari raw field | Baca `.status-cell` textContent (pola existing `updateSummaryFromDOM`) | JS sudah baca DOM label; cukup tambah cabang Abandoned `[VERIFIED: AssessmentMonitoringDetail.cshtml:1281-1300]` |
| Chevron toggle (MAP-03) | JS event listener show/hide | CSS `[aria-expanded="true"]` rotate | Bootstrap auto-set `aria-expanded`; CSS-only lebih simpel + bebas-bug dari JS Monitoring-list pattern `[VERIFIED]` |
| Token regen Pre-Post (MAP-17) | Endpoint baru untuk Pre-Post | `RegenerateToken(int id)` existing (LinkedGroupId-aware Phase 348) | Kirim RepresentativeId, endpoint cari siblings via LinkedGroupId `[VERIFIED: controller L2635-2643]` |
| Konstanta status | String literal | `AssessmentConstants.AssessmentStatus.*` | Single source of truth (D-C) `[VERIFIED]` |

**Key insight:** Phase ini = ZERO komponen baru. Semua MAP reuse pola/helper/konstanta yang SUDAH ada di codebase (sebagian baru ditambah Phase 348). Risiko utama = salah baca line number drift, BUKAN kompleksitas teknis. `[VERIFIED]`

## Runtime State Inventory

> Phase ini BUKAN rename/refactor/migration — pure view polish. Inventory tetap diisi eksplisit untuk kejelasan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **None** — tidak ada perubahan data tersimpan (no migration, no schema, no seed). Verified: D-A locked + tidak ada `RemoveRange`/`SaveChanges` baru di MAP. | none |
| Live service config | **None** — tidak menyentuh n8n/Datadog/external service. View+controller only. | none |
| OS-registered state | **None** — tidak ada task/service/scheduler. | none |
| Secrets/env vars | **None** — tidak ada key/env baru. (Dev run tetap butuh `ASPNETCORE_ENVIRONMENT=Development`, existing.) | none |
| Build artifacts | **None new** — `dotnet build HcPortal.csproj` re-compile view+controller; tidak ada egg-info/binary stale. MAP-10 tambah field ViewModel → recompile otomatis. | `dotnet build` per commit |

**Catatan:** Tidak ada runtime state baru. Yang berubah hanya output render (HTML/teks) + 1 field ViewModel in-memory (`AbandonedCount`). `[VERIFIED]`

## Common Pitfalls

### Pitfall 1: Line number drift dari spec (pre-348 → pasca-348)
**What goes wrong:** Spec MAP menulis `file:line` PRE-348. Banyak sudah bergeser ATAU sudah dikerjakan.
**Why it happens:** Phase 348 modif 6 view + controller (pagination footer, helper, badge binding, HX-Trigger, @inject).
**How to avoid:** Tabel `phase_requirements` di atas berisi line PASCA-348 (Read 2026-06-05). Re-Grep sebelum edit bila ragu. JANGAN percaya line spec mentah.
**Warning signs:** Edit target tidak match konteks; MAP-13/20 "sudah ada". `[VERIFIED]`

### Pitfall 2: MAP-13 vs MAM-13 — item BERBEDA dengan label mirip
**What goes wrong:** Spec MAM-13 (Phase 348) = reshuffleWorker selector (SUDAH DONE). MAP-13 (Phase 349) = Monitor list TotalCount exclude Cancelled (BELUM). Mudah tertukar.
**How to avoid:** MAP-13 = `AssessmentAdminController.cs:2811/2875` (TotalCount). reshuffleWorker = `AssessmentMonitoringDetail.cshtml:739` (jangan sentuh, sudah `function reshuffleWorker(btn)`).
**Warning signs:** Mau edit reshuffleWorker di Phase 349 → STOP, itu MAM-13/348. `[VERIFIED: L739 sudah pakai btn param]`

### Pitfall 3: MAP-20 sudah dikerjakan Phase 346 (REC-07)
**What goes wrong:** Tambah badge "Menunggu Penilaian" lagi → duplikat.
**How to avoid:** `_HistoryTab.cshtml:102-106` SUDAH render badge pending (`@AssessmentConstants.AssessmentStatus.PendingGrading`). MAP-20 = verifikasi/no-op (D-D). Tugas planner: konfirmasi tidak ada double-badge.
**Warning signs:** Cabang `else` di L102-106 sudah ada amber badge. `[VERIFIED]`

### Pitfall 4: MAP-10 — Total ≠ sum kartu meski tambah Abandoned
**What goes wrong:** D-03 minta Total = jumlah semua kartu. UserStatus punya 6 nilai (`DeriveUserStatus` L2677-2690): Menunggu Penilaian / Completed / Dibatalkan / Abandoned / InProgress / Not started. Kartu existing = 5 (Total, Completed, InProgress, NotStarted, Cancelled). Tambah Abandoned = 6 elemen tapi **"Menunggu Penilaian" masih tak punya kartu** → Total tetap ≠ sum bila ada essay pending.
**Why it happens:** MAM-04 (Phase 348) memisahkan PendingGrading dari Completed → muncul bucket baru tak terhitung.
**How to avoid:** Planner putuskan: (a) tambah JUGA kartu "Menunggu Penilaian" agar Total = Completed+InProgress+NotStarted+Cancelled+Abandoned+MenungguPenilaian, ATAU (b) fold MenungguPenilaian ke Completed di hitungan card-total (tapi MAM-04 sengaja exclude). **Rekomendasi:** (a) — tambah 2 field (`AbandonedCount` + reuse `MenungguPenilaianCount` existing) + 2 kartu, plus JS `updateSummaryFromDOM` count keduanya. `MenungguPenilaianCount` SUDAH ada di ViewModel (L22) tapi TIDAK di-assign di Detail action (L3320-3336 hanya Pre/standard list yang assign). **CRITICAL untuk planner.**
**Warning signs:** Total=10, kartu jumlah=8 (2 essay pending hilang). `[VERIFIED: DeriveUserStatus L2677 + Detail model L3320-3336 + ViewModel L22]`

### Pitfall 5: MAP-10 JS `updateSummaryFromDOM` bucket-all-else-to-notStarted
**What goes wrong:** JS L1292 `else notStarted++` — Abandoned + Menunggu Penilaian masuk notStarted, jadi card live-update salah saat SignalR push.
**How to avoid:** Tambah cabang `else if (text === 'Abandoned') abandoned++;` (+ Menunggu Penilaian bila ditambah card) + update `count-abandoned` element. Sinkron dengan field controller.
**Warning signs:** SignalR `workerStarted`/`workerSubmitted` memanggil `updateSummaryFromDOM` L1331/1381 → card notStarted naik salah. `[VERIFIED: AssessmentMonitoringDetail.cshtml:1281-1300]`

### Pitfall 6: MAP-09 skeleton column-count mismatch jadi layout shift
**What goes wrong:** Skeleton Tab2 (4 filter / 5 kolom) vs konten asli (5 filter / 7 kolom) → flash layout shift saat swap.
**How to avoid:** Hitung kolom REAL: Tab2 `_TrainingRecordsTab.cshtml` thead L206-214 = 7 kolom (No/Nama/Nopeg/Jabatan/Unit/Status Training/Aksi); filter L42-163 = 5 (section/category/unit/status/search). History Assessment thead L60-70 = 8 kolom; Training thead L176-182 = 5 kolom.
**Warning signs:** Skeleton lebih pendek/sempit dari tabel asli. `[VERIFIED: counting thead actual]`

### Pitfall 7: MAP-15 Status dropdown — koord dengan CIL-02 search-broaden logic
**What goes wrong:** Controller L2898-2902: search non-empty + status kosong → tampilkan SEMUA (termasuk Closed), set `status="active"` HANYA bila BOTH kosong. Saat search-only, `status` tetap null → `ViewBag.SelectedStatus=null` → view `selStatus = ?? "active"` → dropdown SALAH tampil "Open+Upcoming" padahal Closed muncul.
**How to avoid:** Di cabang search-non-empty + status kosong, set `status = "All"` (broaden scope jujur). Cek tidak break CIL-02 (Phase 338 search "Cilacap" include Closed).
**Warning signs:** Search judul Closed → muncul di hasil tapi dropdown bilang "Open+Upcoming". `[VERIFIED: controller L2898-2911 + view L12]`

### Pitfall 8: MAP-22 — Training param TIDAK lagi dead (Phase 348 MAM-07)
**What goes wrong:** Spec MAP-22 bilang drop `pageSize` Training. Tapi Phase 348 MAM-07 SEKARANG pakai `page`/`pageSize` di Training (L273-279). Drop = break pagination.
**How to avoid:** Drop HANYA `ManageAssessmentTab_History` param mati (`page`/`pageSize`/`statusFilter` L301-302 — tak dipakai, hanya `page` di-log). Training: biarkan `page`/`pageSize` (fungsional). Cleanup wiring `ManageAssessment.cshtml:17,19` (`urlTraining`/`urlHistory` pakai `page`) sesuaikan.
**Warning signs:** Hapus pageSize Training → pagination Tab2 rusak. `[VERIFIED: controller L245-279 vs L301-319]`

### Pitfall 9: HTMX re-swap kehilangan event listener (MAP-03/07 JS)
**What goes wrong:** Tab partial swap via HTMX → JS yang attach di partial (`<script>` di `_HistoryTab.cshtml`) re-execute, tapi handler yang attach di `ManageAssessment.cshtml` `@section Scripts` (host, TIDAK swap) tetap. MAP-07 filter JS ada di HOST (`filterAssessmentRows` L316) — aman. MAP-03 chevron CSS-only — aman. MAP-04 row JS ada di PARTIAL (`_HistoryTab.cshtml:132-147`) — bila di-drop, pastikan tak ada referensi sisa.
**How to avoid:** CSS-only untuk MAP-03 (bebas re-bind). MAP-07/08 fungsi di host page (sudah). Verifikasi pasca-swap manual.
**Warning signs:** Filter berhenti jalan setelah ganti tab. `[VERIFIED: ManageAssessment.cshtml:316-337 host vs _HistoryTab.cshtml:132-147 partial]`

## Code Examples

### MAP-21: expose paging.Take via ViewBag (controller)
```csharp
// AssessmentAdminController.cs ManageAssessmentTab_Assessment — SETELAH L215 tambah:
var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);
ViewBag.ManagementData = grouped.Skip(paging.Skip).Take(paging.Take).ToList();
ViewBag.CurrentPage = paging.CurrentPage;
ViewBag.TotalPages = paging.TotalPages;
ViewBag.TotalCount = paging.TotalCount;
ViewBag.PageSize = paging.Take;   // ← MAP-21: drop magic-number 20 di view
```
```cshtml
@* _AssessmentGroupsTab.cshtml header: tambah baca PageSize *@
var pageSize = (int)(ViewBag.PageSize ?? 20);
@* L180: var rowNum = (currentPage - 1) * pageSize + 1; *@
@* L366: Menampilkan @((currentPage - 1) * pageSize + 1) - @(Math.Min(currentPage * pageSize, totalCount)) *@
@* L16: <input type="hidden" name="pageSize" value="@pageSize" /> *@
```
**Source:** Pola identik sudah di `_TrainingRecordsTab.cshtml:16,391` + controller L268/279. `[VERIFIED]`

### MAP-18: tri-state IsPassed di Tab2 manual-assessment (konstanta D-C)
```cshtml
@* _TrainingRecordsTab.cshtml:264-266 assessmentRows projection — ubah Detail + Status: *@
.Select(a => new {
    Type = "Assessment", Date = a.CompletedAt ?? a.Schedule, Title = a.Title,
    Detail = a.Score.HasValue
        ? $"{a.Score} - {(a.IsPassed == true ? "Lulus" : a.IsPassed == false ? "Tidak Lulus" : AssessmentConstants.AssessmentStatus.PendingGrading)}"
        : "—",
    Status = a.IsPassed == true ? "Passed"
           : a.IsPassed == false ? "Failed"
           : AssessmentConstants.AssessmentStatus.PendingGrading,  // ← null = pending
    ValidUntil = a.ValidUntil, Id = a.Id, Score = a.Score, IsPassed = a.IsPassed
});
```
**Catatan:** statusClass switch L322-329 perlu cabang baru untuk "Menunggu Penilaian" → `bg-warning text-dark`. `[VERIFIED: _TrainingRecordsTab.cshtml:264-266,322-329]`

### MAP-23: extend search ke Category (controller)
```csharp
// AssessmentAdminController.cs:2739-2743 (AssessmentMonitoring):
if (!string.IsNullOrEmpty(search))
{
    var lower = search.ToLower();
    query = query.Where(a => a.Title.ToLower().Contains(lower)
                          || a.Category.ToLower().Contains(lower));  // ← MAP-23
}
```
```cshtml
@* AssessmentMonitoring.cshtml:75 placeholder update: *@
placeholder="Cari nama atau kategori assessment..."
```
**Catatan:** Nama/NIP TIDAK (list aggregate, tak render per-user — OUT OF SCOPE). `[VERIFIED: controller L2739-2743 + view L75 + CONTEXT:100]`

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Status badge bind `group.Status` (rep) | `group.GroupStatus` (derived) | Phase 348 MAM-10 | `_AssessmentGroupsTab.cshtml:196-220` sudah pakai GroupStatus — MAP tak perlu sentuh badge |
| Hardcode category dropdown Monitoring | data-driven `ViewBag.MonitoringCategories` | Phase 348 MAM-11 | MAP-16/23 koord dengan dropdown data-driven existing |
| reshuffleWorker via querySelector ambigu | `reshuffleWorker(this)` param | Phase 348 MAM-13 | MAP-13 (TotalCount) BUKAN ini — jangan tertukar |
| Tab3 History pending = blank | badge "Menunggu Penilaian" | Phase 346 REC-07 | MAP-20 = no-op (sudah done) |
| Delete full-page POST | `hx-post` re-swap | Phase 348 MAM-08 | MAP tak sentuh delete |

**Deprecated/outdated (jangan ikuti spec mentah):**
- Spec MAP-13 ref `L2819-2822` / MAM-13 ref `L739-743` — line drift, lihat tabel pasca-348.
- Spec MAP-20 "rows muncul dari REC-07/346" — sudah ter-render + ter-badge.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Bootstrap auto-set `aria-expanded` saat collapse toggle (basis MAP-03 CSS-only) | Pattern 2 | LOW — bila tidak, fallback JS toggle ala Monitoring-list L401-411. `[ASSUMED dari Bootstrap 5 behavior; existing `.ppt-expand-btn` chevron pakai JS, BUKAN CSS — perlu konfirmasi aria-expanded ter-set]` |
| A2 | "Menunggu Penilaian" perlu kartu sendiri agar Total=sum (MAP-10) | Pitfall 4 | MED — bila user OK Total≠sum saat essay-pending, cukup Abandoned saja. Planner/discuss konfirmasi. `[ASSUMED interpretasi D-03 "Total = jumlah semua kartu"]` |
| A3 | `dotnet test HcPortal.Tests` framework = xUnit, runnable lokal | Validation Architecture | LOW — terverifikasi ada 12 test file + ManageAssessmentMedFixTests Phase 348. `[ASSUMED runner env tersedia; build/test belum dijalankan di sesi research]` |

**Catatan:** 3 assumption, semua LOW-MED. A2 paling penting — planner/discuss-phase HARUS konfirmasi interpretasi "Total = jumlah semua kartu" (apakah termasuk kartu Menunggu Penilaian).

## Open Questions (RESOLVED)

1. **MAP-10 — kartu "Menunggu Penilaian" ikut ditambah atau hanya Abandoned?**
   - **RESOLVED (CONTEXT.md D-03 + UI-SPEC §Summary Card Layout):** Tambah 2 kartu — **Abandoned + Menunggu Penilaian** (set lengkap 7 kartu). Invariant Total Ditugaskan = Selesai + Sedang Mengerjakan + Belum Mulai + Dibatalkan + Abandoned + Menunggu Penilaian. `AbandonedCount` = field ViewModel baru; `MenungguPenilaianCount` sudah ada di ViewModel (L22) tapi belum di-assign di Detail action → tambah assign. JS `updateSummaryFromDOM` tambah cabang Abandoned + PendingGrading. Implementasi di Plan 349-04 Task 2.
   - What we know: D-03 = tambah Abandoned + Total=sum. `DeriveUserStatus` menghasilkan 6 status; essay-pending = bucket ke-6 tak terhitung.
   - What's unclear (sebelum resolve): apakah essay-pending session ada di grup yang dimonitor (Total=sum hanya pecah bila ada essay pending).
   - Recommendation (diadopsi): planner tambah `AbandonedCount` field + assign `MenungguPenilaianCount` di Detail action (saat ini tak di-assign L3320-3336), render 2 kartu (Abandoned + Menunggu Penilaian), JS count keduanya.

2. **MAP-22 — cleanup wiring `urlTraining`/`urlHistory` (ManageAssessment.cshtml:17,19) seberapa jauh?**
   - **RESOLVED (UI-SPEC §Display Binding MAP-22 + Plan 349-05 Task 1):** Drop param mati di SIGNATURE `ManageAssessmentTab_History` (`page`/`pageSize`/`statusFilter`) + cleanup wiring `urlHistory`. `urlTraining` BIARKAN `page`/`pageSize` (fungsional — Phase 348 MAM-07). Tidak break HTMX (extra URL param di-ignore action tanpa param).
   - What we know: `urlTraining` kirim `page`, `urlHistory` kirim `page`. History action drop `page` → URL param jadi no-op.
   - What's unclear (sebelum resolve): apakah hapus `&page=` dari `urlHistory` string interpolation atau biarkan (harmless extra param).
   - Recommendation (diadopsi): drop param mati di SIGNATURE action (History), cleanup `urlHistory` wiring; `urlTraining` biarkan `page` (fungsional).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build/run | ✓ (asumsi — env existing v22.0) | net8.0 | — |
| SQL Server (HcPortal_Dev / SQLEXPRESS) | UAT lokal (Detail render, search) | ✓ (Dev creds di MEMORY) | — | — |
| Playwright MCP | visual UAT (mayoritas MAP = view) | ✓ (dipakai Phase 348 UAT) | — | manual browser |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None signifikan.
**Catatan:** Phase = view-polish; dependency = stack existing. App run WAJIB `ASPNETCORE_ENVIRONMENT=Development` + `http://localhost:5277` (CLAUDE.md + Phase 348 specifics). `[VERIFIED: CLAUDE.md + 348-PATTERNS]`

## Validation Architecture

> `workflow.nyquist_validation: true` di config.json → section WAJIB. `[VERIFIED: .planning/config.json:15]`

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (asumsi standar; 12 test file existing) |
| Quick run command | `dotnet test HcPortal.Tests --filter FullyQualifiedName~ManageAssessment` |
| Full suite command | `dotnet test HcPortal.Tests` |

`[VERIFIED: ls HcPortal.Tests/*.cs = 12 file termasuk ManageAssessmentMedFixTests.cs Phase 348]`

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MAP-13 | TotalCount exclude Cancelled (progress bisa 100%) | unit (logic-bearing) | extract helper `CountExcludingCancelled` ATAU integration test controller | ❌ Wave 0 (`ManageAssessmentLowPolishTests.cs`) |
| MAP-21 | paging.Take exposed (rowNum/text konsisten) | unit | `dotnet test --filter ~Pagination` (PaginationHelper SUDAH ter-test 98/98) | ✅ (ManageAssessmentMedFixTests) — paging.Take sudah ter-cover |
| MAP-15 | Status="All" saat search broaden | unit (logic) | test cabang `status` controller (sulit unit murni; lebih cocok Playwright) | ❌ Wave 0 atau Playwright |
| MAP-22 | Drop param mati History (signature compile) | smoke (build) | `dotnet build HcPortal.csproj -c Debug` | ✅ build gate |
| MAP-23 | Search match Category | unit (logic) | mirip `WorkerDataServiceSearchTests.cs` pola (search predicate) | ❌ Wave 0 (opsional) |
| MAP-01/02/03/04/05/06/07/08/09/10/11/12/14/16/17/18/19/20 | i18n/a11y/empty-state/display/conditional | visual (Playwright/manual) | Playwright MCP UAT | manual-only (justified: render-correctness, bukan logic) |

**Justifikasi manual-only:** 17 dari 23 MAP = perubahan teks/atribut/markup/CSS yang TIDAK ter-otomasi unit-test bermakna (mengikuti pola Phase 348: "display/HTMX/SignalR → grep acceptance + Playwright UAT"). Logic-bearing testable = MAP-13/15/21/23 (+ MAP-22 via build). `[CITED: ManageAssessmentMedFixTests.cs:7 — pola Phase 348]`

### Sampling Rate
- **Per task commit:** `dotnet build HcPortal.csproj -c Debug` (WAJIB — CLAUDE.md) + `dotnet test HcPortal.Tests --filter ~ManageAssessment` bila task logic-bearing.
- **Per wave merge:** `dotnet test HcPortal.Tests` (full suite, ≥98 hijau dari Phase 348).
- **Phase gate:** Full suite green + Playwright UAT 5 SC + browser-verify MAP-10 card-sum + MAP-13 progress-100% sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ManageAssessmentLowPolishTests.cs` — covers MAP-13 (TotalCount exclude Cancelled), MAP-23 (search Category predicate), opsional MAP-15 (status broaden). Bila MAP-13 di-extract jadi helper static → testable seperti `DeriveUserStatus`.
- [ ] Tidak perlu conftest/fixture baru — pola test Phase 348 cukup (static helper + PaginationHelper).
- [ ] Framework install: TIDAK perlu (xUnit existing).

*(Mayoritas MAP = visual; testable subset kecil. Disarankan ≥1 test file baru untuk MAP-13/23 agar verifier punya automated gate untuk item logic-bearing.)*

## Security Domain

> `security_enforcement` absent di config → treat as enabled. Phase = view-polish, surface keamanan minimal. `[VERIFIED: config.json tidak punya security_enforcement key]`

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak ubah auth; action existing `[Authorize(Roles="Admin, HC")]` |
| V3 Session Management | no | Tidak sentuh session |
| V4 Access Control | no | MAP-17 reuse `RegenerateToken` existing (sudah `[ValidateAntiForgeryToken]` + role-guarded) |
| V5 Input Validation | minimal | MAP-23 search → EF parameterized `.Contains` (no raw SQL); MAP teks tanpa user-input baru |
| V6 Cryptography | no | Tidak ada crypto (token regen pakai `GenerateSecureToken` existing, tak diubah) |

### Known Threat Patterns for ASP.NET Core MVC Razor
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via Razor output | Tampering/Info | Razor auto-HTML-encode `@var` (default). MAP teks statis + `@Model` field → aman. **Watch:** `Html.Raw` (MAP-04 drop existing Html.Raw di `_HistoryTab.cshtml:81` — mengurangi surface, bagus). |
| CSRF token regen (MAP-17) | Spoofing | `RegenerateToken` SUDAH `[ValidateAntiForgeryToken]` (L2616) + JS kirim `RequestVerificationToken` header (L420). Reuse, tak ubah. |
| Search injection (MAP-23) | Tampering | EF Core `.Where(a => a.Category.ToLower().Contains(lower))` = parameterized, no injection. `[VERIFIED]` |

**Catatan:** Tidak ada surface keamanan BARU. MAP-04 justru MENGURANGI (drop `Html.Raw` + nested-interactive). MAP-17 reuse endpoint guarded existing. `[VERIFIED]`

## Sources

### Primary (HIGH confidence)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (684 baris, Read penuh) — MAP-03/05/06/21 targets
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (466 baris, Read penuh) — MAP-03/18/19 targets
- `Views/Admin/Shared/_HistoryTab.cshtml` (203 baris, Read penuh) — MAP-02/04/08/20 targets
- `Views/Admin/ManageAssessment.cshtml` (340 baris, Read penuh) — MAP-07/09/22 host
- `Views/Admin/AssessmentMonitoring.cshtml` (439 baris, Read penuh) — MAP-14/15/16/17/23
- `Views/Admin/AssessmentMonitoringDetail.cshtml` (Read L1-620 + L739-783 + L1030-1430) — MAP-01/10/11/12
- `Controllers/AssessmentAdminController.cs` (Read L106-225, L245-344, L2616-2690, L2724-2923, L3270-3358) — controller-side MAP
- `Models/AssessmentMonitoringViewModel.cs` (Read penuh) — MAP-10 field gap
- `Models/AssessmentConstants.cs` (Read L1-30) — konstanta D-C
- `HcPortal.Tests/ManageAssessmentMedFixTests.cs` (Read L1-70) — pola test Phase 348
- `git log` working tree commit `d998b256` — confirm Phase 348 COMPLETE (`ada31549`)

### Secondary (project docs — HIGH untuk decisions)
- `.planning/phases/349-manageassessment-monitoring-low-polish/349-CONTEXT.md` — D-A..D-D + D-01..04 + Discretion
- `.planning/phases/348-manageassessment-monitoring-med-fix/348-CONTEXT.md` + `348-PATTERNS.md` — carry-forward + analog
- `docs/superpowers/specs/2026-06-04-manageassessment-monitoring-audit-design.md` §Phase 349 — tabel MAP-01..23 (line PRE-348, di-drift-koreksi di research ini)
- `.planning/REQUIREMENTS.md:88-110` — MAP-01..23 entries
- `.planning/STATE.md` + `.planning/config.json` — milestone v22.0, nyquist enabled
- `CLAUDE.md` — develop workflow (build lokal sebelum commit, no edit Dev/Prod, respond Bahasa Indonesia)

### Tertiary (LOW confidence — flagged)
- A1 (Bootstrap aria-expanded auto-set untuk CSS chevron) — `[ASSUMED]`, existing pattern pakai JS toggle; konfirmasi saat plan/execute.

## Project Constraints (from CLAUDE.md)
- **Respond Bahasa Indonesia** — semua komunikasi + komentar kode prosa Indonesia (kode/identifier English). `[CITED: CLAUDE.md]`
- **Verifikasi lokal sebelum commit:** `dotnet build` + `dotnet run` (`http://localhost:5277`) + cek DB lokal + Playwright bila ada. `[CITED: CLAUDE.md Develop Workflow #3]`
- **Jangan edit kode/DB di server Dev/Prod.** Promosi = tanggung jawab Team IT. `[CITED: CLAUDE.md]`
- **Jangan push tanpa verifikasi lokal.** Notifikasi IT dengan commit hash + flag migration (Phase 349 = NO migration). `[CITED: CLAUDE.md]`
- **Seed data:** Phase 349 tidak butuh seed (view polish). Bila UAT butuh data → klasifikasi `temporary+local-only`, snapshot DB, journal, restore. `[CITED: CLAUDE.md Seed Workflow]`

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero dependency baru, semua di-verify dari file existing.
- Architecture/targets: HIGH — 7 file di-Read langsung pasca-348, line ter-verifikasi commit `d998b256`.
- Pitfalls: HIGH — drift (MAP-13/20/22) dan MAP-10 sum-gap di-temukan via Read aktual, bukan asumsi.
- MAP-10 interpretasi (Total=sum): MED — A2 perlu konfirmasi user/discuss.

**Research date:** 2026-06-05
**Valid until:** 2026-06-12 (7 hari — codebase aktif; Phase 348 baru selesai, file bisa berubah bila ada hotfix). Re-Grep line bila >7 hari.
