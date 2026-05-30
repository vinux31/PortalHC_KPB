# Phase 337: CMP/Records Full Overhaul (Approach C) — Context

**Gathered:** 2026-05-30
**Status:** Ready for research/planning
**Milestone:** v20.0
**Source:** Memory `project_cmp_records_audit_2026_05_27.md` (Approach C locked 2026-05-30 via /gsd-discuss-phase 337)
**Phase type:** Code overhaul HIGH severity + ARCH
**Effort:** L (~1 minggu+, 3 wave internal)

---

## Goal

Full overhaul `/CMP/Records` page (My Records tab + Team View tab) — fix 15 bug filter+data integrity + 7 UX race/state + 5 quality a11y+ViewModel + 3 arch SQL push-down+pagination. Eliminate 5 CRITICAL filter silent-fail browser-verified + race condition + dead code bloat + N+1 query risk.

26 REQ deliver: CMP-01..26.

---

## Decisions Locked (this discuss session)

| # | Decision | Pilihan | Rationale |
|---|----------|---------|-----------|
| D-01 | **Plan split** | **3 plan per wave** | Plan 01 filter+data (CMP-01..11), Plan 02 UX+quality (CMP-12..23), Plan 03 arch SQL+pagination (CMP-24..26). Reduce blast radius, atomic commits, parallel-friendly antar wave. |
| D-02 | **Pagination Team View** | **Numeric pager + 20/page** (50/100 option) | Reuse `PaginationHelper.Calculate` pattern existing (AssessmentAdminController.cs:62 ManageAssessment + L4508 export query). Server-side. Konsisten dgn enterprise pattern Portal HC. |
| D-03 | **A11y scope** | **Basic + ARIA tab** | Keyboard nav row (replace inline onclick) + ARIA roles tab nav (role=tab + aria-controls + aria-selected) — REQ CMP-19/20. Skip full WCAG AA audit (out of scope, ~2-3 hari extra). |
| D-04 | **Rollout strategy** | **Direct replace, no feature flag** | Refactor langsung, lokal verify + Playwright UAT sebelum push. Konsisten dgn v19.0 pattern (cascade hardening single batch, IT promo akhir). |

---

## Carried from Memory (audit 27 May 2026)

### 15 Bug Filter + Data Integrity → 11 REQ (Wave 1)

5 CRITICAL filter silent-fail (browser-verified `admin@pertamina.com/123456`):
- **B-01 → CMP-01**: Status `"Sudah"/"Belum"` diabaikan saat Category kosong (`WorkerDataService.cs:302` guard `if (!IsNullOrEmpty(statusFilter) && !IsNullOrEmpty(category))`). Live test: 12 worker tetap muncul termasuk Rino 10 Asm + 5 Tng.
- **B-02 → CMP-02**: Sub Category dropdown 100% no-op. UI kirim `subCategory` ke `/CMP/RecordsTeamPartial` tapi `CMPController.cs:740-758` terima param + TIDAK forward ke `GetWorkersInSection`. Live fetch `?subCategory=DOESNOTEXIST` → 12 row.
- **B-03 → CMP-03**: Category filter TIDAK narrow worker list, cuma ubah `CompletionPercentage` internal (`WorkerDataService.cs:283-291`). Kolom Assessment/Training count tetap total semua kategori. Misleading.
- **B-04 → CMP-04**: Category match pakai `.Contains` substring (`WorkerDataService.cs:287`). "OJT" match "POJT". Harus equality.
- **B-05 → CMP-05**: Export endpoints abaikan `subCategory` (UI kirim, server drop). `ExportRecordsTeamTraining` filter worker by category tapi tidak filter trainings by category column → export tetap include training di luar kategori filter.

HIGH data integrity (static analysis):
- **B-06 → CMP-06**: `GetUnifiedRecords:51` `Status = a.IsPassed == true ? "Passed" : "Failed"`. Session Completed + `IsPassed == null` → "Failed" palsu.
- **B-07 → CMP-07**: Training `SertifikatUrl` ada di model tapi TIDAK dirender di kolom Sertifikat My Records (`Records.cshtml:184-199`). Hanya assessment cert link.
- **B-08 → CMP-08**: Training `Status="Permanent"` (no ValidUntil) tampil "—" tanpa indikasi permanen.
- **B-09 → CMP-09**: `data-title="@item.Title.ToLower()"` Razor HtmlEncode → "&" jadi `&amp;` → search "&" gagal match.
- **B-10 → CMP-10**: Badge `My Records (@totalCount)` static, tidak update saat search/year filter. Tidak ada counter per-filter.
- **B-11 → CMP-11**: `AssessmentAttemptHistory` null `Title` pakai `?? ""` di tuple key (`WorkerDataService.cs:129`) → semua null-title collide → AttemptNumber salah.

### 7 UX Race + State → 7 REQ (Wave 2)

- **U-01 → CMP-12**: Race AJAX `filterTeamTable()` debounce 300ms tanpa AbortController. Late response timpa fresh.
- **U-02 → CMP-13**: Stale export URL: `updateExportLinks()` dipanggil DI AKHIR `doFetch`. Klik Export sebelum fetch selesai → param lama.
- **U-03 → CMP-14**: Sub Category dropdown enabled tapi kosong untuk OJT (tidak ada children di AssessmentCategories). Browser-verified.
- **U-04 → CMP-15**: Date filter exclude worker dgn 0 record (`WorkerDataService.cs:244`). Counter turun tanpa hint UX.
- **U-05 → CMP-16**: My Records vs Team View asymmetric: My Records tidak punya Category/Status/Date, Team View tidak punya year quick filter.
- **U-06 → CMP-17**: Filter state tidak persist antar switch tab.
- **U-07 → CMP-18**: `_RecordsTeamBody.cshtml:47-56` build `data-categories`, `data-subcategories`, `data-completed-*` per row tapi dead code (filter sekarang server-side). Bloat payload.

### 5 Quality A11y + VM → 5 REQ (Wave 2 lanjutan)

- **C-01 → CMP-19**: Inline `onclick="window.location.href='...'"` (`Records.cshtml:158`). Row tidak keyboard-navigable.
- **C-02 → CMP-20**: Tab nav `<a>` tanpa `role="tab"` + `aria-controls` + `aria-selected`.
- **C-03 → CMP-21**: View duplicate `UserManager.GetUserAsync` + `GetRolesAsync` (`Records.cshtml:14-17`, `RecordsTeam.cshtml:7-10`). NRE risk + duplicate. Pakai ViewModel.
- **C-04 → CMP-22**: `roleLevel <= 4` di view (`:42, :214`) duplicate controller (`:487`).
- **C-05 → CMP-23**: `Records.cshtml:65` `Model.Select(r => r.Date.Year).Distinct()` re-evaluated tiap render.

### 3 Arch → 3 REQ (Wave 3)

- **A-01 → CMP-24**: Export endpoints panggil `GetAllWorkersHistory()` load ALL workers + assessments + trainings ke memory, filter via HashSet. Tidak scalable untuk org > ratusan. SQL push-down via `IQueryable` composition.
- **A-02 → CMP-25**: Date filter in-memory `.Where` (`WorkerDataService.cs:229-241`), bukan SQL.
- **A-03 → CMP-26**: Tidak ada pagination di Team View → tambah `PaginationHelper.Calculate` pattern + numeric pager UI 20/50/100.

---

## Files to Modify (verified scout)

| File | LoC current | Touch area |
|------|-------------|------------|
| `Controllers/CMPController.cs` | 4703 | Records action L479, RecordsTeamPartial L740, ExportRecordsTeamAssessment L637, ExportRecordsTeamTraining L690. ~5% of file touched, surgical edits. |
| `Services/WorkerDataService.cs` | 368 | GetUnifiedRecords:28, GetWorkersInSection:175, GetAllWorkersHistory:85, filter logic L283-302. Substantial refactor. |
| `Views/CMP/Records.cshtml` | 346 | Tab nav L14-17 ARIA, kolom Sertifikat L184-199, inline onclick L158, year list L65, badge counter |
| `Views/CMP/RecordsTeam.cshtml` | 330 | Filter UI, AJAX debounce + AbortController, export URL sync, pagination UI baru |
| `Views/CMP/_RecordsTeamBody.cshtml` | 78 | Dead data-* purge L47-56, row keyboard nav |

**Total scope file:** 5825 LoC. **Estimated delta:** ~600-900 LoC change (filter refactor + arch + a11y).

### New files (likely)

- `Models/ViewModels/CMPRecordsViewModel.cs` (new) — replace inline `UserManager.GetUserAsync` di view, encapsulate (user, role, records, totalCount, filter state)
- `Services/WorkerDataServiceExtensions.cs` (optional) — extension methods untuk IQueryable composition kalau SQL push-down jadi multi-method

### Existing pattern reuse

- **PaginationHelper.Calculate**: `AssessmentAdminController.cs:62` + `L4508` (ManageAssessment + export). 1-liner: `var paging = PaginationHelper.Calculate(query.Count, page, pageSize); ViewBag.PageSize = pageSize;`
- **HTMX inline filter pattern**: Phase 322 (filter-scope-per-tab-manage-assessment) — already shipped + verified, optional reuse kalau filter UI mau di-modernize bersamaan
- **AbortController AJAX pattern**: cek `wwwroot/js/*.js` existing kalau ada pattern, atau implement baru
- **ViewModel + role-scope service**: cek `Models/ViewModels/*` existing untuk inspiration (e.g., Assessment*ViewModel)

---

## Scope IN

### Wave 1 (Plan 01) — Filter + Data Integrity
- 11 REQ CMP-01..11
- Fix 5 CRITICAL filter silent-fail (browser-verifiable via Playwright UAT)
- Fix 6 data integrity (Status derivation + SertifikatUrl render + Permanent indicator + HtmlEncode search + per-filter counter + AttemptNumber correct)
- Touch: `CMPController.cs` Records+RecordsTeamPartial+Export, `WorkerDataService.cs`, `Records.cshtml`, `RecordsTeam.cshtml`

### Wave 2 (Plan 02) — UX + Quality
- 7 UX REQ CMP-12..18 (race AJAX + export sync + state persist + dead code purge)
- 5 Quality REQ CMP-19..23 (keyboard nav + ARIA tab + ViewModel + roleLevel dedup + memoize year)
- Touch: `Records.cshtml`, `RecordsTeam.cshtml`, `_RecordsTeamBody.cshtml`, new `CMPRecordsViewModel.cs`, JS AbortController

### Wave 3 (Plan 03) — Arch
- 3 REQ CMP-24..26 (SQL push-down `GetAllWorkersHistory` + date filter SQL + pagination Team View)
- Touch: `WorkerDataService.cs` substantial refactor, `CMPController.cs` RecordsTeamPartial pagination wire-up, `RecordsTeam.cshtml` pager UI

## Scope OUT (defer v21.0+)

- ❌ Full WCAG AA audit (D-03 confirmed Basic + ARIA tab only)
- ❌ Feature flag rollout (D-04 confirmed direct replace)
- ❌ My Records FULL parity dengan Team View (U-05 partial — only year quick filter parity)
- ❌ Mobile-first redesign Records page (kalau muncul saat exec, defer)
- ❌ Records page TypeScript migration (server-side Razor stays)
- ❌ Filter preset / saved view per user (kalau muncul saat exec, defer)
- ❌ Export beyond Excel (PDF export Team View) — Gap #6 sudah Phase 338 W4 BulkExportPdf
- ❌ Realtime updates SignalR (Phase 999.1 backlog)

---

## Dependencies

| From | To | Hand-off |
|------|-----|---------|
| Phase 336 SHIPPED (verified) → Phase 337 | No direct dependency, 337 independent track (zero file overlap dengan 336 doc-only) |
| Phase 337 → Phase 338 | No direct dependency, 338 Cilacap UX touches berbeda file (`AssessmentAdminController` vs 337 `CMPController`). Optional handoff: ViewModel pattern reuse kalau 338 butuh. |
| Phase 337 Wave 1 → Wave 2 | Sequential within phase. Wave 2 build on Wave 1 filter+data correctness baseline. |
| Phase 337 Wave 2 → Wave 3 | Sequential. Wave 3 arch refactor butuh stable view + ViewModel from Wave 2. |

**No cross-phase blockers.** Phase 337 can start immediately after Phase 336 SHIPPED.

---

## Open Questions (resolve during exec)

1. **OQ-337-1**: ViewModel naming — `CMPRecordsViewModel` (My Records) vs `RecordsTeamViewModel` (Team View) — 2 VM separate atau 1 VM polymorphic? Resolve di Plan 02 Wave 2 task design.
2. **OQ-337-2**: SQL push-down strategy `GetAllWorkersHistory` — pure `IQueryable` composition vs EF Core projection (Select DTO) vs raw SQL? Resolve di Plan 03 Wave 3 research.
3. **OQ-337-3**: Pagination page size persist per user (cookie/session) atau session-scoped (form state)? Resolve di Plan 03 Wave 3 UX detail.
4. **OQ-337-4**: My Records tab year quick filter — dropdown vs button group "2024 | 2025 | 2026 | All"? Resolve di Plan 02 Wave 2.

---

## Pre-condition Drift Verify

Memory file:line citations dari audit 2026-05-27. Drift sejak audit: **1 commit `c7adcb73` (Phase 327-05) — DateOnly sweep `var today` CMP+Home**. Minor, file:line akurat ±5 baris. Plan execution wajib verify line exact saat plan generation.

---

## Cross-link

- **Memory source:** `project_cmp_records_audit_2026_05_27.md` (Approach C locked 2026-05-30, file:line verified ±5 drift)
- **REQ:** `.planning/REQUIREMENTS.md` CMP-01..26 (26 REQ Category 1)
- **ROADMAP entry:** Phase 337 cmp-records-full-overhaul-filter-data-arch-a11y (Plans TBD)
- **Pagination pattern reuse:** `Controllers/AssessmentAdminController.cs:62, :4508` + `Services/PaginationHelper.cs`
- **Phase 322 shipped** (filter-scope-per-tab-manage-assessment) — HTMX inline filter pattern reuse kalau relevant

---

## Threat Model Pointer (for planner)

5 STRIDE category kemungkinan:
- **Tampering**: filter regression breaks existing functionality (mitigate: Playwright UAT pre-shipping, Phase 322+325 regression smoke)
- **Information Disclosure**: pagination/filter expose data cross-section (mitigate: role-scope check `roleLevel <= 4` honor, ViewModel sanitize)
- **DoS**: SQL push-down regression generates expensive query (mitigate: SQL profiling pre-ship, EXPLAIN PLAN sanity check)
- **Repudiation**: ViewModel refactor lose audit trail (mitigate: existing AuditLog flow untouched, ViewModel only data layer)
- **Elevation of Privilege**: tab role check regression bypass (mitigate: integration test `tests/e2e/cmp-records.spec.ts` baru)

Detail STRIDE generate saat plan (planner spawn).

---

## Next Step

**`/gsd-plan-phase 337`** — generate 3 plan (Plan 01 Wave 1 filter+data + Plan 02 Wave 2 UX+quality + Plan 03 Wave 3 arch) via gsd-planner agent. Plan-checker validate. Iterate sampai PASS.

Plan 01 estimasi: ~4-5 hari (11 REQ, paling complex karena filter logic refactor + Playwright UAT 5 CRITICAL).
Plan 02 estimasi: ~2-3 hari (12 REQ, UX + quality, ViewModel refactor)
Plan 03 estimasi: ~2-3 hari (3 REQ, arch SQL push-down + pagination)

Total: ~8-11 hari (range ~1.5-2 minggu). Sedikit lebih panjang dari estimate awal 1 minggu+ — sesuaikan expectations user.

---

*Created: 2026-05-30 via `/gsd-discuss-phase 337` — 4 gray area decided via AskUserQuestion (all recommended picks: D-01 3-plan split + D-02 Numeric pager 20/page + D-03 Basic+ARIA + D-04 Direct replace). OQ-337-1..4 captured for plan/exec resolution.*
