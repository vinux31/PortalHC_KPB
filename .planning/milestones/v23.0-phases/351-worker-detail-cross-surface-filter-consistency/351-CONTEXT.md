# Phase 351: Worker Detail + Cross-Surface Filter Consistency - Context

**Gathered:** 2026-06-05
**Status:** Ready for planning
**Mode:** `--auto` (Claude selected recommended defaults from audit-spec fix directions; no interactive Q&A)

<domain>
## Phase Boundary

Tutup 4 gap konsistensi/feedback search-filter di surface **Worker Detail** (`RecordsWorkerDetail.cshtml`)
dan **My Records** (`Records.cshtml`), backed by `GetUnifiedRecords`:
- **SF-03 (MED):** Worker Detail 0-match feedback — counter "Menampilkan X dari Y" + pesan "Tidak ada hasil untuk filter ini." (`aria-live`).
- **SF-04 (MED):** Filter Kategori Worker Detail mencocokkan kategori **record aktual** (bukan exact-equals ke master + opsi mati).
- **SF-05 (LOW):** Paritas alat filter My Records ↔ Worker Detail.
- **SF-07 (LOW):** Back-nav Worker Detail → Team View preserve param penuh (`subCategory`/`dateFrom`/`dateTo`/`searchScope`).

**Di luar boundary:** Team View server-side search/export (= Phase 350, SHIPPED). **No migration.**
Sequential setelah Phase 350 (✅ done) — namun overlap `WorkerDataService.cs` MINIM: SF-04 fix di
controller+view (pakai `unifiedRecords` yang sudah di-return), bukan di `GetUnifiedRecords` itu sendiri.
</domain>

<decisions>
## Implementation Decisions

### SF-03 — Worker Detail 0-match feedback + counter
- **D-01:** Mirror pola My Records yang SUDAH ADA (`Records.cshtml:337-379`) ke `RecordsWorkerDetail.cshtml` `filterTable()` (`:336-358`):
  - Track `visibleCount` di loop `forEach` (saat ini hanya toggle `display`).
  - Inject baris empty-state `id="workerDetailEmptyState"` (`<td colspan="7" ...>Tidak ada hasil untuk filter ini.</td>`) saat `visibleCount === 0 && totalRows > 0`; sembunyikan saat ada hasil. (Pola identik `myRecordsEmptyState` `:366-379`.)
  - Tambah counter visible "Menampilkan X dari Y" (elemen baru di atas/di header tabel) + `aria-live="polite"` untuk screen reader.
  - Reuse `wwwroot/css/records.css` (Phase 347) — tidak ada style baru ad-hoc.

### SF-04 — Filter Kategori match record aktual
- **D-02:** Bangun opsi dropdown Kategori Worker Detail dari **nilai Kategori yang benar-benar ada di `unifiedRecords`** (distinct, non-empty), BUKAN dari master `AssessmentCategories`.
  - Saat ini opsi = `ViewBag.MasterCategoriesJson` (controller `CMPController.cs:577-579` = `allCats.Select(c => c.Name)`) → opsi mati (master tanpa record) + record legacy/free-text tak ter-filter.
  - Fix: ganti sumber opsi jadi `unifiedRecords.Where(r => !string.IsNullOrEmpty(r.Kategori)).Select(r => r.Kategori).Distinct().OrderBy(...)` (di controller ATAU view — Claude's discretion mekanisme).
  - **Pertahankan** compare exact-equals `category === categoryFilter` (`RecordsWorkerDetail.cshtml:352`) — kini aman karena opsi & data-category berasal dari sumber sama (record aktual). `data-category` row (`:216`) tidak berubah.
  - **SubKategori cascade tetap master-based** (`subCategoryMap` dari `ViewBag.SubCategoryMapJson`, Training-only) — di luar scope SF-04 inti; risiko sama dicatat sebagai residual minor (lihat Deferred).

### SF-05 — Paritas My Records ↔ Worker Detail
- **D-03:** Naikkan set filter My Records (`Records.cshtml`) agar sebanding Worker Detail. Saat ini My Records = Search + Tahun (quick-btn) saja; Worker Detail = Search + Kategori + SubKategori + Tahun + Tipe.
  - Tambah **Tipe** (Assessment/Training) filter ke My Records (rows sudah punya `data-type` `Records.cshtml:343`).
  - Tambah **Kategori** filter ke My Records (opsi = distinct-actual, konsisten D-02). Tambah `data-category` ke baris My Records (saat ini hanya data-title/year/type).
  - Target paritas = `{Search, Kategori, Tipe, Tahun}` di kedua surface. **SubKategori TIDAK ditambah** ke My Records (cascade Training lebih dalam; bukan kehilangan data — Deferred minor).
  - Pertahankan UX year quick-button group My Records (`:62-77`).

### SF-07 — Back-nav preserve param penuh
- **D-04:** **sessionStorage-primary.** `cmp-records-team-filter` (RecordsTeam.cshtml `saveFilterState`) SUDAH persist seluruh 9 filter termasuk `subCategory`/`dateFrom`/`dateTo`/`searchScope`; `restoreFilterState` (`:305-327`) restore saat Team View load.
  - **Deliverable:** kembali dari Worker Detail → Team View memulihkan SELURUH filter (4 yang hilang di query-string ikut pulih) via sessionStorage.
  - **Planner WAJIB VERIFIKASI:** `restoreFilterState` menang atas query-string parsial DAN memicu re-fetch (`filterTeamTable()`/`doFetch`) sehingga 4 param yang dipulihkan benar-benar diterapkan ke tabel. Tambah guard bila restore ter-skip saat query-string ada.
  - **Fallback (HANYA bila verifikasi gagal):** extend back-nav query-string round-trip — `CMPController.RecordsWorkerDetail(:538)` signature +4 param + `FilterState` anon (`:588-595`) +4 field + 2 asp-route block (breadcrumb `:27-32` + button `:43-47`) +4 route + inbound link RecordsTeam→WorkerDetail +4. Lebih berat (sentuh RecordsTeam yang baru di-touch Phase 350) → hindari kecuali sessionStorage terbukti tak cukup.

### Claude's Discretion
- Mekanisme tepat sumber opsi Kategori actual (controller ViewBag vs view-side LINQ).
- Markup/penempatan counter "Menampilkan X dari Y" + apakah ekstrak helper opsi-Kategori dipakai bersama 2 view.
- Apakah SubKategori parity diangkat (default: tidak, Deferred).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit & requirements
- `docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md` — **SF-03/04/05/07** (§1.3 Worker Detail map, §2 findings table + suggested fix, §3 by-design). file:line per finding.
- `.planning/REQUIREMENTS.md` — SF-03/SF-04/SF-05/SF-07 (definisi + Out-of-Scope by-design).
- `.planning/ROADMAP.md` §"Phase 351" (baris 883-895) — Success Criteria 1-5 + UI hint.

### Prior-art patterns (WAJIB di-reuse)
- `Views/CMP/Records.cshtml:337-379` — pola My Records `visibleCount` + counter badge + inject `myRecordsEmptyState` = **template persis SF-03** untuk Worker Detail.
- `.planning/phases/350-team-view-search-scope-export-parity/350-CONTEXT.md` — REC-06 D-07 invariant (Team View aggregation; Phase 351 tidak menyentuh), records.css reuse.
- v22.0 **MAP-07/08** — pola 0-match feedback + counter (referensi gaya).
- Phase 347 — `wwwroot/css/records.css` (shared styles 3 view CMP/Records).

### Source files (titik sentuh)
- `Views/CMP/RecordsWorkerDetail.cshtml` — filter bar `:128-181`, Kategori opsi `:137-149`, `filterTable()` `:336-358`, `clearFilters()` `:380-388`, back-nav `:27-32`+`:40-50`, rows `data-*` `:213-218`, server empty-state `:200-208`, FilterState `:12`.
- `Views/CMP/Records.cshtml` — filter bar `:54-93`, `filterTable()` `:336-402` (counter+empty pattern), rows `data-*`.
- `Controllers/CMPController.cs` — `RecordsWorkerDetail` action `:538-598` (MasterCategoriesJson `:577-579`, FilterState anon `:588-595`, authz `:543-556`), `Records`/My Records action (build My Records VM).
- `Views/CMP/RecordsTeam.cshtml` — `saveFilterState`/`restoreFilterState` `:300-327` (sessionStorage all-9), `filterTeamTable`/`doFetch` (re-fetch trigger).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **My Records filterTable pattern** (`Records.cshtml:337-379`): `visibleCount` + counter element updates + empty-state row inject/hide — copy near-verbatim into Worker Detail (SF-03), rename id `myRecordsEmptyState`→`workerDetailEmptyState`.
- **`wwwroot/css/records.css`**: shared across Records/RecordsWorkerDetail/RecordsTeam — no new CSS file.
- **`unifiedRecords`** already returned to both Worker Detail & My Records by `GetUnifiedRecords` — has `.Kategori` per record → build actual-distinct Kategori options without service change.
- **subCategoryMap cascade** (Worker Detail `:361-375`) — keep as is.

### Established Patterns
- Client-side filter = toggle `row.style.display` + `data-*` attrs (`.toLowerCase()` both sides). Worker Detail rows already carry `data-category`/`data-subcategory`/`data-type`; My Records rows carry `data-type` (must ADD `data-category` for SF-05).
- sessionStorage filter persistence (`cmp-records-team-filter` Team, `cmp-records-my-filter` My Records) restored on load.

### Integration Points
- SF-03: `RecordsWorkerDetail.cshtml` `filterTable()` + new counter element (view-only).
- SF-04: Kategori options source — `CMPController.RecordsWorkerDetail` ViewBag (`:577-579`) or view-side LINQ over `unifiedRecords`.
- SF-05: `Records.cshtml` filter bar + `filterTable()` + rows (`data-category`); My Records controller action (Kategori distinct + ensure data available).
- SF-07: `RecordsTeam.cshtml` restoreFilterState precedence + re-fetch (verify); fallback = `CMPController.RecordsWorkerDetail` signature + FilterState + back-nav routes.

### Constraints
- **No migration.** **No `GetUnifiedRecords` change** for SF-04 (use returned records).
- Preserve authz (`RecordsWorkerDetail` roleLevel guard + L4 section-lock `:543-556`) verbatim.
- Don't regress Phase 350 Team View (D-07) — Phase 351 is Worker Detail + My Records client-side + 1 controller ViewBag.

</code_context>

<specifics>
## Specific Ideas

- SF-03 pesan persis: **"Tidak ada hasil untuk filter ini."** + counter **"Menampilkan X dari Y"** (`aria-live="polite"`) — selaras gaya My Records & v22 MAP-07/08.
- SF-04 repro: Worker Detail dengan record berkategori free-text/legacy yang tak ada di master → kategori itu tak muncul/tak bisa difilter; + opsi master tanpa record = mati. Actual-distinct menutup keduanya.
- SF-07: sessionStorage sudah simpan semua; isu nyata = pastikan restore menang + memicu fetch. Bukan menambah data, tapi memulihkan konteks filter penuh saat "Back to Team View".
</specifics>

<deferred>
## Deferred Ideas

- **SubKategori actual-match** (Worker Detail + My Records) — risiko dead-option sama spt Kategori tapi Training-only cascade master-driven; di luar SF-04 inti. Catat sebagai residual minor untuk milestone lanjutan bila muncul keluhan.
- **SF-07 full query-string round-trip** (controller signature + FilterState + inbound RecordsTeam link +4 param) — FALLBACK only, dipakai jika verifikasi sessionStorage gagal. Default: tidak diimplementasikan.
- Tidak ada scope-creep baru — semua dalam SF-03/04/05/07.

</deferred>

---

*Phase: 351-worker-detail-cross-surface-filter-consistency*
*Context gathered: 2026-06-05 (--auto)*
