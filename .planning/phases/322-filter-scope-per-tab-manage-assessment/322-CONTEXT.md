# Phase 322: Filter Scope Per Tab — ManageAssessment - Context

**Gathered:** 2026-05-22
**Status:** Ready for planning
**Milestone:** v17.0 Assessment Admin Power Tools

<domain>
## Phase Boundary

Rollback Phase 311 Plan 02 shared filter shell di `/Admin/ManageAssessment`. Setiap tab pakai filter native domain-specific masing-masing.

1. **Shell view `ManageAssessment.cshtml`** — hapus `<form id="filter-form">` shared (baris 88-190), hapus cross-tab `htmx:afterSwap` invalidation listener (baris 365-398), hapus bagian script update `hx-get` endpoint saat `shown.bs.tab` (baris 338-358), tambah JS function `filterTrainingRows()` untuk sub-tab Riwayat Training.
2. **Partial `_AssessmentGroupsTab.cshtml`** — convert filter form GET submit → HTMX inline trigger (search + kategori + status, 3-field). Convert pagination `<a href>` → `<button>` dengan HTMX + `hx-include="#filterFormAssessment"` (bonus fix preserve filter state).
3. **Partial `_TrainingRecordsTab.cshtml`** — convert filter form GET submit → HTMX inline trigger (Bagian + Kategori Training + Unit + Status + Cari Nama/Nopeg, 5-field). Cascade Bagian → Unit (clear Unit value) preserved via `onchange` inline pre-HTMX.
4. **Partial `_HistoryTab.cshtml`** — tambah filter client-side Cari Nama/Nopeg di sub-tab Riwayat Training (parity sama Riwayat Assessment), tambah `id="trainingHistoryTable"` + `data-worker` attribute per `<tr>` Riwayat Training.
5. **Controller `AssessmentAdminController.ManageAssessment`** — hapus block `ViewBag.Categories = await _cache.GetOrCreateAsync(...)` (redundant pasca shell filter delete; partial action fetch sendiri dengan cache key sama).

**Bugs yang dieliminasi (dilaporkan user 2026-05-22):**
- **Bug 1 — Double filter** di Tab Assessment Groups (dev): shell filter + partial filter dua-duanya render → 2 baris filter di UI.
- **Bug 2 — Filter contamination cross-tab**: filter Tab 1 (Open/Upcoming lifecycle assessment) bocor ke Tab 2 (Sudah/Belum completion worker) saat tab switch → semantic mismatch → result kosong/salah.
- **Bug 3 (bonus, pre-existing)** — Pagination Tab 1 anchor link `<a href="?tab=assessment&page=N&search=…">` cuma include `search` param, miss `category` + `statusFilter` → filter state hilang saat klik page.

**Out of scope (defer / not relevant):**
- Phase 312 delete modal — preserved tidak disentuh.
- DB schema / EF migration — none.
- Controller signature breaking changes — backward compat URL bookmark dipertahankan.
- E2E Playwright test untuk filter cross-tab isolation — defer (manual UAT cukup).
- UI-SPEC Phase 311 update document untuk reflect per-tab pattern — defer ke docs phase.
- AddManualAssessment/EditManualAssessment / Buat Assessment / Monitoring / Audit Log button — tidak terkait filter, preserved.
- `filterAssessmentRows()` existing JS untuk sub-tab Riwayat Assessment — preserved tidak disentuh.

</domain>

<decisions>
## Implementation Decisions

### Filter Architecture Decisions
- **D-01:** **Per-tab native filter (Option A from brainstorm)** — bukan shared shell. Rationale: 5 dari 7 nama filter punya domain semantic beda per tab (Status Open/Upcoming vs Sudah/Belum vs Pass/Fail; Kategori Distinct-DB vs hardcoded-8-enum; Search broad vs narrow). Shared filter cause contamination bug.
- **D-02:** **Sub-tab History filter own (client-side)** — Riwayat Assessment + Riwayat Training tiap sub-tab punya filter input + JS row hide/show pattern (existing `filterAssessmentRows`). Tidak shared dengan parent tab. Tidak refetch server (data set lokal dari `assessmentHistory`/`trainingHistory` ViewBag).
- **D-03:** **Pagination bonus fix included** — convert ke HTMX + `hx-include="#filterFormAssessment"` sekalian solve pre-existing bug (filter state hilang saat klik page).

### PLAN Sub-Numbering Strategy
- **D-04:** Pecah 7 task spec jadi **3 PLAN file atomic per layer**:
  - `322-01-PLAN.md` **partial-views-filter** — Task 1 (Tab 1 filter HTMX) + Task 2 (Tab 1 pagination HTMX) + Task 3 (Tab 2 filter 5-field HTMX) + Task 4 (Tab 3 Riwayat Training filter + data-worker)
  - `322-02-PLAN.md` **shell-controller-cleanup** — Task 5 (Shell view cleanup + `filterTrainingRows()` JS) + Task 6 (Controller `ManageAssessment` action drop ViewBag.Categories)
  - `322-03-PLAN.md` **uat** — Task 7 (Manual UAT 11-step golden path + edge + `322-UAT.md` write + tag + handoff)
- **D-05:** **Sequential strict** — `01 → 02 → 03` wajib urut. Rationale: PLAN 02 shell `filterTrainingRows()` reference DOM ID yang ditambah di PLAN 01 Task 4 (`#trainingHistoryTable` + `.training-history-row`). PLAN 03 UAT verify hasil PLAN 01+02 combined.

### Branch Strategy
- **D-06:** **Reuse branch `feature/phase-321-edit-jawaban`** kalau Phase 321 belum di-merge. Kalau Phase 321 sudah merge ke main, bikin branch baru `feature/phase-322-filter-scope`. Rationale: scope kecil 1-2 jam, view-only changes, low risk. Merge ke main setelah PLAN 03 UAT pass + tag `v17.0-p322-complete`.

### Testing Strategy
- **D-07:** **Manual UAT only** — no Playwright automation. Rationale: view-level HTMX behavior verification (filter scope, no double, cross-tab isolation) butuh visual inspection. Playwright bisa, tapi cost > benefit untuk scope kecil. UAT 11-step checklist di PLAN 03 sufficient.
- **D-08:** **`dotnet build` per task** — Razor view compile verify per file edit. Build fail = revert + investigate (Razor compile error common saat hx-attribute syntax salah).
- **D-09:** **Browser DevTools Network tab** untuk verify HTMX request URL benar (ke `ManageAssessmentTab_*` partial endpoint, BUKAN shell `ManageAssessment`).

### Migration + Handoff
- **D-10:** **NO migration. NO DB impact.** View + controller-action body changes only. Promo Dev/Prod via IT team standard flow (commit hash + tag flag NO-MIGRATION).
- **D-11:** **Commit cadence 1-task-1-commit** (Phase 320/321 pattern) — 7 task = 7 commit + 1 UAT commit. Message format `feat(322-NN): ...` atau `refactor(322-NN): ...` atau `docs(322): ...`. Pre-commit `docs/DEV_WORKFLOW.md §5` checklist per commit.

### UX Copy (Bahasa Indonesia)
- **D-12:** **Filter copy preserved** — Tab 1 "Semua Kategori", "Aktif (Open/Upcoming)"; Tab 2 "Semua Bagian", "-- Pilih Kategori --", "Semua Unit", "Semua"; Tab 3 "Cari nama pekerja / NIP..." (existing) + "Cari nama pekerja / Nopeg..." (NEW Riwayat Training). Tidak ada copy baru.

### Carrying Forward (Prior Phase Patterns)
- **D-13:** **HTMX 2.0 vendored locally** (Phase 311 Plan 02 D-02) — `~/lib/htmx/htmx.min.js` preserved. Tidak tambah dependency.
- **D-14:** **Skeleton placeholder + retry handler + error template** (Phase 311 Plan 02 D-04, D-07; Phase 311 Plan 04 BUG-5A retry) — preserved seluruhnya, tidak disentuh.
- **D-15:** **Header buttons toggle script** di `shown.bs.tab` handler — preserved (bagian update `hx-get` endpoint dihapus, bagian header visibility dipertahankan).
- **D-16:** **`filterAssessmentRows()` existing JS** untuk sub-tab Riwayat Assessment — preserved tidak disentuh, dipakai existing.
- **D-17:** **Phase 312 delete modal** di partial bottom `_AssessmentGroupsTab.cshtml` — preserved tidak disentuh, defensive `htmx:afterSwap` check tetap valid.
- **D-18:** **Categories cache key + invalidation** di Add/Edit/DeleteCategory action — preserved (partial action `ManageAssessmentTab_Assessment` masih pakai key sama; shell action drop redundant block tidak affect invalidation logic).

### Risk Mitigation
- **D-19:** **Single revert commit rollback** — kalau bug post-deploy ditemukan, single `git revert` mengembalikan Phase 311 Plan 02 state (shared filter + cross-tab listener restored). Phase 322 changes contained di 5 file.
- **D-20:** **Phase 311 Plan 02 UI-SPEC compliance impact** — flagged as risk Medium (shared filter design rolled back). Re-evaluate UI-SPEC sebelum merge atau defer ke docs phase. PLAN 03 UAT include explicit check tidak ada regression.

</decisions>

<references>
## References

### Spec & Plan (superpowers format, source of truth)
- `docs/superpowers/specs/2026-05-22-filter-scope-per-tab-manage-assessment-design.md` — design spec 7 section
- `docs/superpowers/plans/2026-05-22-filter-scope-per-tab-manage-assessment.md` — implementation plan 7 task

### Affected Files (5 file)
- `Views/Admin/ManageAssessment.cshtml` (shell, 487 baris)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (partial Tab 1, 585 baris)
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (partial Tab 2, 337 baris)
- `Views/Admin/Shared/_HistoryTab.cshtml` (partial Tab 3, 148 baris)
- `Controllers/AssessmentAdminController.cs` (action `ManageAssessment` baris 62-105, action `ManageAssessmentTab_*` preserved)

### Phase 311 Plan 02 (origin Plan 322 rollback)
- Commit `0f3e4690 feat(311-02): refactor ManageAssessment.cshtml ke HTMX-driven shell view (UI-SPEC compliance)` — shell refactor add shared filter
- Commit `bbf88fa8 feat(311-04): scope cross-tab invalidation ke filter-form provenance + drop once (BUG-2A/2B)` — cross-tab listener fix (akan dihapus full di Phase 322)
- Commit `b5fb6354 feat(311-04): retry handler pakai htmx.ajax direct call (BUG-5A)` — retry handler (preserved)

### Reference Login (Dev Local UAT)
- `admin@pertamina.com` — memory `reference_dev_credentials.md`
- URL local: `http://localhost:5277/Admin/ManageAssessment`
- URL dev: `http://10.55.3.3/KPB-PortalHC/Admin/ManageAssessment`

</references>
