# Phase 322: Filter Scope Per Tab — Research

**Date:** 2026-05-22
**Source:** brainstorming session 2026-05-22 (user reports double filter + cross-tab contamination)

## Problem Investigation

### Symptom Reported by User

**Dev (`http://10.55.3.3/KPB-PortalHC/Admin/ManageAssessment`):**
- Tab Assessment Groups menampilkan **3 row filter** stacked:
  - Row 1: Search input col-md-4 + Cari button + Semua Kategori col-md-3 + Aktif (Open/Upcoming) col-md-3 (= shell filter)
  - Row 2: Search input col-md-6 + Cari button (= partial Tab 1 search form)
  - Row 3: Semua Kategori col-md-3 + Aktif (Open/Upcoming) col-md-3 (= partial Tab 1 filter row)
- Tab Input Records menampilkan **2 row filter**: shell filter (row 1) + partial 5-field filter (row 2) Bagian + Kategori Training + Unit + Status + Cari Nama/Nopeg.
- Filter shell tetap visible di semua tab (Tab 1, 2, 3) — values terbawa saat pindah tab.

**Local (`http://localhost:5277/Admin/ManageAssessment`):**
- Tab Assessment Groups menampilkan 1 row filter (shell shape: col-md-4 + col-md-3 + col-md-3). Partial filter terlihat tidak render (perlu verifikasi cache/refresh).
- Tab Input Records — user report 5-field granular filter tidak muncul (kemungkinan cache browser / HTMX swap incomplete saat screenshot).

### Root Cause Investigation

**Trace commit history `Views/Admin/ManageAssessment.cshtml`:**
```
70dd99d5 fix(312): WR-02 use @Url.Action via data-attributes (path-prefix safe)
97ca421d feat(312-02): conditional delete render + 2-step impact preview modal
b5fb6354 feat(311-04): retry handler pakai htmx.ajax direct call (BUG-5A)
bbf88fa8 feat(311-04): scope cross-tab invalidation ke filter-form provenance + drop once (BUG-2A/2B)
0f3e4690 feat(311-02): refactor ManageAssessment.cshtml ke HTMX-driven shell view (UI-SPEC compliance)
```

Phase 311 Plan 02 (commit `0f3e4690`) add shared filter form di shell view (baris 88-190 `<form id="filter-form">`). Phase 311 NOT cleanup filter di partial views (`_AssessmentGroupsTab.cshtml` baris 12-83, `_TrainingRecordsTab.cshtml` baris 28-130). Result: both render → **double**.

**Per-tab partial filter inline code masih ada:**
- `_AssessmentGroupsTab.cshtml:12-83` — `<form method="get" asp-action="ManageAssessment" asp-controller="AssessmentAdmin">` search form + `<select onchange="applyAssessmentFilters()">` kategori + status row.
- `_TrainingRecordsTab.cshtml:28-130` — `<form method="get" action="@Url.Action("ManageAssessment", "AssessmentAdmin")">` dengan 5-field (Bagian/Kategori Training/Unit/Status/Cari Nama).
- `_HistoryTab.cshtml:39-55` — sub-tab Riwayat Assessment filter client-side existing (`assessmentWorkerFilter` + `assessmentTitleFilter`). Sub-tab Riwayat Training **belum ada filter**.

### Domain Semantic Analysis (Why Per-Tab Filter is Correct)

| Filter | Tab 1 (Assessment Groups) | Tab 2 (Input Records) | Tab 3 History (Riwayat Assessment) | Tab 3 History (Riwayat Training) |
|--------|---------------------------|----------------------|-----------------------------------|----------------------------------|
| **Data subjek** | AssessmentSessions grouped | Workers + manual records | AllWorkersHistoryRow (assessment) | AllWorkersHistoryRow (training) |
| **Search scope** | judul + kategori + nama + NIP | nama + nopeg | nama + NIP (client-side) | (NEW) nama + Nopeg client-side |
| **Filter "Kategori"** | Distinct DB (dinamis) | Hardcoded 8 enum (OJT/IHT/.../ISS/OSS) | Title dropdown (bukan kategori) | — |
| **Filter "Status"** | Open/Upcoming/Closed/Aktif (lifecycle assessment) | Sudah/Belum (completion worker) | Pass/Fail (kolom data row) | — |
| **Bagian + Unit** | tidak relevan | ✅ relevan + cascade | tidak relevan | tidak relevan |

**5 dari 7 filter punya domain semantic beda per tab.** Contoh konkret bug:
- Filter `Status=Closed` di Tab 1 (lifecycle assessment) → switch Tab 2 → `statusFilter=Closed` ikut terkirim → controller Tab 2 expects `Sudah`/`Belum` → result empty/salah.
- Filter `Kategori=Proton` di Tab 1 (Distinct DB) → switch Tab 2 → Tab 2 enum hardcoded 8 mungkin tidak ada "Proton" exact match → empty.

## Task Breakdown (7 task)

### Task 1: Convert `_AssessmentGroupsTab.cshtml` filter form to HTMX inline
**File:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:12-83`
**Type:** UI refactor (Razor view)
**Effort:** 15 menit
**Output:** Filter form id=`filterFormAssessment` dengan input/select pakai `hx-get` direct ke `ManageAssessmentTab_Assessment` endpoint, `hx-target="closest .htmx-tab-wrapper"`, `hx-include="closest form"`. Reset button convert ke HTMX trigger.

### Task 2: Convert `_AssessmentGroupsTab.cshtml` pagination to HTMX
**File:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:294-341`
**Type:** UI refactor + bonus bug fix
**Effort:** 10 menit
**Output:** Pagination `<a href="?tab=…">` → `<button>` dengan `hx-get` + `hx-vals='{"page":"N"}'` + `hx-include="#filterFormAssessment"`. Bonus: preserve filter state (kategori + statusFilter) saat pagination (pre-existing bug fix).

### Task 3: Convert `_TrainingRecordsTab.cshtml` filter form to HTMX inline
**File:** `Views/Admin/Shared/_TrainingRecordsTab.cshtml:28-130`
**Type:** UI refactor (Razor view)
**Effort:** 20 menit
**Output:** Form id=`filterFormTraining` dengan 5 field HTMX-enabled (Bagian/Kategori Training/Unit/Status/Cari Nama). Cascade Bagian → Unit preserved via `onchange="document.getElementById('filterUnit').value='';"` inline (execute SEBELUM HTMX `change` trigger). Reset button HTMX trigger.

### Task 4: Add Riwayat Training filter + data-worker di `_HistoryTab.cshtml`
**File:** `Views/Admin/Shared/_HistoryTab.cshtml:108-146`
**Type:** UI feature add (client-side filter parity)
**Effort:** 10 menit
**Output:** Sub-tab Riwayat Training tambah `<input id="trainingWorkerFilter" oninput="filterTrainingRows()">`. Table `id="trainingHistoryTable"`, row `class="training-history-row" data-worker="@row.WorkerName.ToLower() @(row.WorkerNIP?.ToLower() ?? "")"`. Filter JS function `filterTrainingRows()` ditambah di shell view (Task 5).

### Task 5: Cleanup `ManageAssessment.cshtml` shell view
**File:** `Views/Admin/ManageAssessment.cshtml`
**Type:** UI refactor (Razor view + script cleanup + JS function add)
**Effort:** 25 menit
**Output:**
- Delete shared filter form (baris 88-190 `<form id="filter-form">`)
- Delete cross-tab `htmx:afterSwap` invalidation listener (baris 365-398)
- Delete bagian script update `hx-get` endpoint saat `shown.bs.tab` (baris 338-358) — preserve bagian header buttons toggle visibility
- Add `filterTrainingRows()` JS function (mirror `filterAssessmentRows` pattern, target `#trainingHistoryTable .training-history-row`)
- Cleanup unused ViewBag references di top `@{ ... }` block (`searchTerm`, `assessmentCategories`, `selectedCategory`, `selectedStatus`)

### Task 6: Cleanup `AssessmentAdminController.cs` shell action
**File:** `Controllers/AssessmentAdminController.cs:62-105` (method `ManageAssessment`)
**Type:** Controller refactor (drop redundant DB query)
**Effort:** 5 menit
**Output:** Delete block `ViewBag.Categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, ...)` (baris 88-97). Action signature preserved (backward compat). Telemetry log preserved. ViewBag.SearchTerm/SelectedCategory/SelectedStatus/SelectedSection/SelectedUnit/CurrentPage/PageSize tetap (preserve API contract — partial action read via param, tapi shell ViewBag tidak harm).

### Task 7: Manual UAT — golden path + edge cases
**File:** `.planning/phases/322-filter-scope-per-tab-manage-assessment/322-UAT.md` (create)
**Type:** Verification
**Effort:** 30 menit
**Output:** 11-step UAT checklist (3 verify no double + 8 verify cross-tab isolation + edge). Login `admin@pertamina.com` lokal `http://localhost:5277/Admin/ManageAssessment`. DevTools Network tab untuk verify HTMX endpoint. Result `PASS`/`FAIL` per step. `322-UAT.md` write + commit.

## Sequencing & Dependencies

```
Task 1 (Tab 1 filter HTMX)
   ↓
Task 2 (Tab 1 pagination HTMX, depends Task 1 #filterFormAssessment)
   ↓
Task 3 (Tab 2 filter HTMX, independent)
   ↓
Task 4 (Tab 3 sub-tab Training filter + data-worker, independent)
   ↓
Task 5 (Shell cleanup + filterTrainingRows JS, depends Task 4 DOM IDs)
   ↓
Task 6 (Controller cleanup, independent after Task 5)
   ↓
Task 7 (UAT verify Task 1-6 combined)
```

Sequential strict — race tidak ada (file-disjoint kecuali Task 5 cross-reference Task 4 DOM IDs).

## Risk Profile

- **Low:** UI-only, no DB schema, no controller signature breaking.
- **Medium:** Phase 311 Plan 02 UI-SPEC compliance design rolled back partial. Re-evaluate Phase 311 UI-SPEC sebelum merge atau defer.
- **Low:** Phase 312 delete modal preserved (defensive `htmx:afterSwap` check tetap valid setelah cross-tab listener dihapus).
- **Low:** HTMX 2.0 vendored locally — no new dependency.

## Verification Strategy

1. **`dotnet build`** per task — Razor compile error common saat hx-attribute typo.
2. **`dotnet watch run` + browser** — manual visual verify per task golden path step.
3. **DevTools Network tab** — verify HTMX request URL ke `ManageAssessmentTab_*` (BUKAN shell `ManageAssessment` = page reload).
4. **DevTools Console** — pastikan tidak ada JS error.
5. **DevTools DOM inspect** — verify `data-worker` attribute exists, `id` attribute benar, filter form id benar.

## Requirements (TBD — Internal Bug Fix)

Phase 322 = internal bug fix, tidak ada REQ baru di REQUIREMENTS.md. Reference issue:
- User report 2026-05-22 (chat session) — double filter di dev + cross-tab contamination + Tab 2 filter granular tidak muncul (cache).
- Pre-existing bug pagination filter state hilang — fix bareng.
