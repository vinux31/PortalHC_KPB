# Filter Scope Per Tab — ManageAssessment (Phase 322)

**Date:** 2026-05-22
**Milestone:** v17.0 Assessment Admin Power Tools
**Phase:** 322
**Related phases:** 311 (Plan 02 HTMX shell refactor — partial rollback), 312 (Delete modal — preserved)
**Risk:** Low–Medium (UI-only, no DB schema, no controller signature breaking)

---

## 1. Goal & Scope

### Goal

Filter scoped per tab di `/Admin/ManageAssessment`. Hapus shared filter form shell yang ditambahkan Phase 311 Plan 02. Setiap tab punya filter native domain-specific masing-masing. Sub-tab History punya filter client-side per sub-tab.

Tujuan eliminasi 2 bug yang dilaporkan user (2026-05-22):
1. **Double filter** di Tab Assessment Groups (dev) — shell filter + partial filter dua-duanya render.
2. **Filter contamination** — filter Tab 1 (Open/Upcoming lifecycle assessment) bocor ke Tab 2 (Sudah/Belum completion worker) → semantic mismatch → result kosong/salah.

### Scope (in)

- `Views/Admin/ManageAssessment.cshtml` — hapus `<form id="filter-form">` shell + script update endpoint `hx-get` saat tab change + cross-tab invalidation listener.
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — pertahankan filter inline existing (3-field), convert form GET submit → HTMX trigger ke partial endpoint sendiri.
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — pertahankan filter inline existing (5-field), convert form GET submit → HTMX trigger ke partial endpoint sendiri.
- `Views/Admin/Shared/_HistoryTab.cshtml` — tambah filter client-side Cari Nama/Nopeg di sub-tab Riwayat Training (parity sama Riwayat Assessment) + tambah `data-worker` attribute di `<tr>` Riwayat Training.
- `Controllers/AssessmentAdminController.cs` — bersihkan shell action `ManageAssessment` dari `ViewBag.Categories` cache (redundant pasca shell filter hapus). Partial actions tidak berubah signature.

### Scope (out)

- Cross-tab invalidation logic (Phase 311 Plan 04 BUG-2A/2B) — dihapus karena tidak ada state shared.
- Skeleton placeholder, error template, retry handler, regenerate token handler — semua preserved.
- Lazy load HTMX per tab + `once` modifier — preserved.
- Tab persistence via URL `?tab=` param — preserved.
- Header action buttons (`Buat Assessment | Monitoring | Audit Log`) toggle saat Tab 1 active — preserved.
- Phase 312 delete modal — preserved.
- DB schema, EF migration, controller signature breaking — none.

---

## 2. Architecture

```
┌─ ManageAssessment.cshtml (shell) ───────────────────────────────────┐
│  Breadcrumb + Header + Action buttons (toggle visibility per tab)    │
│  <ul nav-tabs> Assessment Groups | Input Records | History           │
│  <tab-content>                                                       │
│    <pane-assessment>                                                 │
│      <div.htmx-tab-wrapper                                           │
│           hx-get=…ManageAssessmentTab_Assessment                     │
│           hx-trigger="load"|"shown.bs.tab from:#tab-assessment once" │
│           hx-target="this" hx-swap="innerHTML">                      │
│        [skeleton placeholder]                                        │
│    <pane-training>                                                   │
│      <div.htmx-tab-wrapper hx-get=…ManageAssessmentTab_Training>     │
│        [skeleton placeholder]                                        │
│    <pane-history>                                                    │
│      <div.htmx-tab-wrapper hx-get=…ManageAssessmentTab_History>      │
│        [skeleton placeholder]                                        │
└──────────────────────────────────────────────────────────────────────┘

Partial swap loads (each partial contains OWN filter form):
  _AssessmentGroupsTab.cshtml
    → form: search + kategori + status (3-field)
    → HTMX trigger inline: hx-get=…Tab_Assessment hx-target=closest .htmx-tab-wrapper
    → pagination links: HTMX same endpoint, preserve filter state via hx-include
    → delete modal Phase 312 preserved
  _TrainingRecordsTab.cshtml
    → form: bagian + kategori-training + unit + status + nama/nopeg (5-field)
    → HTMX trigger inline: hx-get=…Tab_Training hx-target=closest .htmx-tab-wrapper
    → cascade Bagian → Unit preserved
  _HistoryTab.cshtml
    → 2 sub-tabs Riwayat Assessment | Riwayat Training
    → client-side row filter (no fetch): filterAssessmentRows() existing
    → NEW: filterTrainingRows() + data-worker attribute on training rows
```

### Key changes vs Phase 311 Plan 02

| Aspect | Phase 311 Plan 02 (current) | Phase 322 (target) |
|--------|------------------------------|--------------------|
| Filter location | Shell `<form id="filter-form">` shared 3-field | Per-partial inline native (3/5/2-field per tab/sub-tab) |
| Filter trigger | Shell form HTMX, target active pane | Partial form HTMX, target closest wrapper |
| Tab switch impact | Filter values ikut ke tab baru | Filter state independent per partial DOM |
| Cross-tab invalidation | `htmx:afterSwap` provenance check reset skeleton other tabs | DELETED (tidak ada state shared) |
| `shown.bs.tab` script | Update `hx-get` endpoint + filter-tab hidden | Hanya toggle header buttons visibility |
| Categories cache | Shell action fetch + partial action fetch (double) | Partial action only (shell redundant block dihapus) |

---

## 3. File-Level Changes

### 3.1 `Views/Admin/ManageAssessment.cshtml`

**Delete:**
- Baris 88-190 — `<form id="filter-form">` entire block (search + category + status dropdowns + reset link).
- Baris 338-358 — bagian script `shown.bs.tab` yang update `hx-get` endpoint dan filter-tab hidden input value.
- Baris 365-398 — `htmx:afterSwap` cross-tab invalidation listener (Phase 311 Plan 04 BUG-2A/2B).

**Preserve:**
- Breadcrumb, header, action buttons (baris 19-60).
- Tab navigation `<ul class="nav nav-tabs">` (baris 63-82).
- `<div class="tab-content">` + 3 panes + `.htmx-tab-wrapper` skeleton (baris 192-307).
- `shown.bs.tab` handler bagian header button toggle (baris 329-337).
- HTMX script tag (baris 310).
- Error template `renderHtmxError()` + listener `htmx:responseError` + `htmx:sendError` (baris 400-425).
- Retry button handler `data-htmx-retry` (baris 426-445).
- Regenerate token handler (baris 447-471).
- `filterAssessmentRows()` function (baris 473-485) — dipakai sub-tab Riwayat Assessment.

**Add:**
- `filterTrainingRows()` function (mirror `filterAssessmentRows` pattern):
  ```js
  function filterTrainingRows() {
      var workerFilter = document.getElementById('trainingWorkerFilter')?.value.toLowerCase() || '';
      var rows = document.querySelectorAll('#trainingHistoryTable .training-history-row');
      rows.forEach(function(row) {
          var worker = row.getAttribute('data-worker') || '';
          row.style.display = (!workerFilter || worker.includes(workerFilter)) ? '' : 'none';
      });
  }
  ```

**ViewBag references yang hilang:** `searchTerm`, `assessmentCategories`, `selectedCategory`, `selectedStatus` di shell `@{ ... }` block — bisa dihapus juga karena tidak ada filter shell yang render.

### 3.2 `Views/Admin/Shared/_AssessmentGroupsTab.cshtml`

**Convert form GET submit → HTMX inline trigger:**

Baris 12-83 (search form + filter row) — ubah:
- Form wrapping dihapus (tidak perlu lagi). Field jadi standalone HTMX-triggered.
- Atau: pertahankan `<form>` untuk grouping `hx-include`, tapi `onsubmit="return false;"` dan trigger via HTMX dari input/select langsung.

Pattern target:
```cshtml
<form id="filterFormAssessment" onsubmit="return false;">
  <input type="hidden" name="tab" value="assessment" />
  <input type="hidden" name="page" value="1" />

  <div class="row mb-4">
    <div class="col-md-6">
      <div class="input-group">
        <span class="input-group-text bg-white border-end-0">
          <i class="bi bi-search text-muted"></i>
        </span>
        <input type="text" class="form-control border-start-0 ps-0" name="search"
               id="searchAssessment"
               placeholder="Cari berdasarkan judul, kategori, nama, atau NIP..."
               value="@searchTerm"
               hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
               hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
               hx-target="closest .htmx-tab-wrapper"
               hx-swap="innerHTML"
               hx-include="closest form" />
        <button class="btn btn-primary" type="button"
                hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
                hx-target="closest .htmx-tab-wrapper"
                hx-swap="innerHTML"
                hx-include="closest form">Cari</button>
      </div>
    </div>
  </div>

  <div class="row mb-3">
    <div class="col-md-3">
      <select id="categoryFilter" name="category" class="form-select form-select-sm"
              hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
              hx-trigger="change"
              hx-target="closest .htmx-tab-wrapper"
              hx-swap="innerHTML"
              hx-include="closest form">
        <option value="">Semua Kategori</option>
        ... (option list existing)
      </select>
    </div>
    <div class="col-md-3">
      <select id="statusFilter" name="statusFilter" ... > (same pattern)
    </div>
    @if (resetCondition) {
      <div class="col-md-2">
        <button type="button" class="btn btn-sm btn-outline-secondary"
                onclick="resetAssessmentFilter()">Reset Filter</button>
      </div>
    }
  </div>
</form>
```

**Delete:** Legacy `applyAssessmentFilters()` JS reference (`onchange="applyAssessmentFilters()"`) — ganti dengan HTMX `hx-trigger="change"`.

**Pagination (baris 294-341):** Convert anchor `<a href="?tab=…">` jadi HTMX:
```cshtml
<a class="page-link"
   hx-get="@Url.Action("ManageAssessmentTab_Assessment","AssessmentAdmin")"
   hx-vals='{"page":"@i"}'
   hx-include="#filterFormAssessment"
   hx-target="closest .htmx-tab-wrapper"
   hx-swap="innerHTML"
   role="button">@i</a>
```
Bonus fix: pagination existing bug — link cuma include `search`, tidak include `category`/`statusFilter`. Via `hx-include="#filterFormAssessment"` semua filter state ikut serta.

**Preserve:** Stats badge, table render, dropdown actions, Phase 312 delete modal.

### 3.3 `Views/Admin/Shared/_TrainingRecordsTab.cshtml`

**Convert form submit → HTMX inline trigger:**

Baris 30-127 — form `<form method="get" action="@Url.Action("ManageAssessment", "AssessmentAdmin")">`:
- Ubah jadi `<form id="filterFormTraining" onsubmit="return false;">` (tidak post ke shell endpoint).
- Per select hapus `onchange="this.form.submit()"`, ganti dengan HTMX attribute langsung:
  ```cshtml
  <select class="form-select form-select-sm" name="section" id="filterSection"
          hx-get="@Url.Action("ManageAssessmentTab_Training","AssessmentAdmin")"
          hx-trigger="change"
          hx-target="closest .htmx-tab-wrapper"
          hx-swap="innerHTML"
          hx-include="closest form"
          onchange="document.getElementById('filterUnit').value='';">
  ```
  Cascade Bagian → Unit (clear Unit value) preserved via `onchange` — execute SEBELUM HTMX request. HTMX `hx-trigger="change"` akan kirim request setelah inline `onchange` selesai.

- Tombol search "Cari" (baris 117): convert `type="submit"` → `type="button"` + HTMX attributes (same as Assessment).
- Tombol Reset Filter (baris 120-123) `<a href="…">`: convert ke `<button>` + HTMX reset (target endpoint tanpa filter params).

**Preserve:** Hidden input `tab=training`, label fields, table render, expanded row collapse logic.

### 3.4 `Views/Admin/Shared/_HistoryTab.cshtml`

**Add filter client-side untuk sub-tab Riwayat Training (parity sama Riwayat Assessment):**

Baris 109 area — sub-tab Training pane, sebelum table:
```cshtml
<!-- Filter -->
<div class="row g-2 mb-3 pt-3 px-0">
    <div class="col-md-4">
        <input type="text" class="form-control form-control-sm"
               placeholder="Cari nama pekerja / NIP..."
               id="trainingWorkerFilter" oninput="filterTrainingRows()">
    </div>
</div>
```

**Add `id` ke training table + `data-worker` attribute per row:**

Baris 121 table tag — tambah `id="trainingHistoryTable"`.
Baris 134 row tag — ubah:
```cshtml
<tr class="training-history-row"
    data-worker="@row.WorkerName.ToLower() @(row.WorkerNIP?.ToLower() ?? "")">
```

**Preserve:** Sub-tab nav, Riwayat Assessment filter existing (`assessmentWorkerFilter`, `assessmentTitleFilter`), badge counts, empty state.

### 3.5 `Controllers/AssessmentAdminController.cs`

**Shell action `ManageAssessment` (baris 62-105):**
- Delete block baris 88-97 — `ViewBag.Categories = await _cache.GetOrCreateAsync(...)` block (redundant pasca shell filter hapus; partial action `ManageAssessmentTab_Assessment` fetch sendiri di baris 227-236).
- Keep tab routing (baris 73-74) + `ViewBag.ActiveTab` (baris 74).
- Param signature: keep as-is (backward compat — bookmarked URL dengan filter query params tetap diterima, walaupun sekarang dipakai partial endpoint).
- Telemetry log (baris 100-102) — preserve.

**Partial actions (`ManageAssessmentTab_Assessment` baris 117-244, `_Training` baris 249-283, `_History` baris 288-309):**
- Tidak diubah signature.
- Tidak diubah logic.

---

## 4. Data Flow (state isolation)

| Tab | Filter state location | Persist saat switch tab? | Filter trigger |
|-----|----------------------|--------------------------|----------------|
| Assessment Groups | URL query (`?tab=assessment&search=&category=&statusFilter=`) + DOM `#filterFormAssessment` field values | ❌ tidak (filter Tab 1 tidak ikut ke Tab 2/3) | HTMX `input changed delay:500ms` + `change` + button click |
| Input Records | URL query (`?tab=training&search=&section=&unit=&category=&statusFilter=`) + DOM `#filterFormTraining` | ❌ tidak | HTMX `change` (instant) + submit button click |
| History (Riwayat Assessment) | Client-side `<input>` value + JS row hide/show | n/a (sub-tab pakai data set lokal) | JS `oninput`/`onchange` |
| History (Riwayat Training) | Client-side `<input>` value (NEW) + JS row hide/show (NEW) | n/a | JS `oninput` (NEW) |

**Tab switch behavior:** lazy load partial sekali (`hx-trigger="… once"`). Filter state setiap partial independent dari DOM mount sampai unmount. Saat user kembali ke tab sebelumnya, partial wrapper TIDAK re-fetch (`once` mencegah re-trigger) — DOM partial preserved with filter values dari last interaction.

---

## 5. Testing

### Manual (golden path)

1. Buka `/Admin/ManageAssessment` → Tab Assessment Groups load. **Exactly 1 filter row visible** (search + kategori + status). Tidak double.
2. Filter `kategori=OJT` → table refetch via HTMX, badge "N grup assessment" update, hanya OJT visible.
3. Klik page 2 di pagination → table refetch dengan kategori=OJT preserved (bonus fix Miss #2).
4. Switch ke Tab Input Records → 5-field filter visible (Bagian + Kategori Training + Unit + Status + Cari nama). Filter dari Tab 1 (kategori=OJT) **tidak muncul** di sini.
5. Filter `Bagian=Operation` → Unit dropdown populate cascade (filter Unit dikosongkan dulu via `onchange` inline).
6. Filter `Kategori Training=OJT` + `Status=Sudah` + search "Andi" → table refetch dengan kombinasi filter.
7. Switch ke Tab History → 2 sub-tab. Riwayat Assessment active by default. Filter row (Cari + Title dropdown) visible. Filter row Tab 1/2 tidak ikut.
8. Klik sub-tab Riwayat Training → filter Cari nama/Nopeg visible (NEW). Filter sub-tab Assessment tidak ikut.
9. Kembali ke Tab Assessment Groups → filter terakhir (kategori=OJT, page 2) masih di DOM, data sesuai.

### Edge cases

- Network slow / HTMX timeout → error template visible (preserved). Klik "Coba Lagi" → refetch dengan filter state preserved via `hx-include`.
- Reset filter di Tab 1 → semua field clear, table reload default.
- Reset filter di Tab 2 → semua 5 field clear, table reload default.
- Bookmarked URL `…?tab=training&section=Operation&unit=NHT&statusFilter=Belum` → shell action route to Tab 2, partial action receive params, render filter pre-populated, table filtered.
- Hard refresh `Ctrl+Shift+R` di tab manapun → load fresh, filter URL params re-applied via `hx-vals`/`hx-include` saat HTMX initial load.

### Auto (deferred)

E2E test `tests/e2e/manage-assessment-filter-scope.spec.ts` — verify cross-tab isolation. Defer ke Plan 04 (testing) atau skip kalau manual UAT sufficient.

---

## 6. Risk & Rollback

**Risk:** Low–Medium.

- **Low:** UI-only changes, no DB schema, no controller signature breaking changes, lazy-load HTMX flow preserved.
- **Medium:** Phase 311 Plan 02 design (HTMX shared filter) di-rollback sebagian. UI-SPEC compliance Phase 311 perlu re-evaluate — apakah shared filter masih bagian dari spec, atau spec diupdate untuk per-tab.
- **Low:** Phase 312 delete modal di partial bottom — tidak disentuh, defensive `htmx:afterSwap` check preserved.

**Rollback:** Single revert commit kalau bug discovered post-deploy. Phase 311 partial filter code tidak di-strip (cuma form action + HTMX trigger yang diubah), jadi reverting commit Phase 322 mengembalikan shell filter + cross-tab listener; partial filter inline tetap exist (jadi double filter kembali, tapi tidak crash).

**No migration. No DB impact. No new dependency.**

---

## 7. Out of scope (defer to future phase if needed)

- E2E Playwright test untuk filter cross-tab isolation — bisa di-defer.
- UI-SPEC update document Phase 311 untuk reflect per-tab filter pattern — defer ke docs phase.
- Pagination existing bug fix di Tab History (kalau ada) — tidak relevan, History client-side row filter, no pagination.
- AddManualAssessment/EditManualAssessment buttons logic — tidak terkait filter.
