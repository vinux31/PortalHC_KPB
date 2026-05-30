---
phase: 337-cmp-records-full-overhaul-filter-data-arch-a11y
plan: 03
subsystem: cmp-records
tags: [arch, sql-pushdown, iqueryable, pagination, ef-core, performance]

requires:
  - phase: 337-01
    provides: Wave 1 filter correctness + data integrity baseline
  - phase: 337-02
    provides: Wave 2 UX/Quality + ViewModel + sessionStorage pattern
provides:
  - 3 REQ arch refactor (CMP-24/25/26): SQL push-down + pagination
  - GetAllWorkersHistory IQueryable composition + 5 optional filter params (workerIds/from/to/category/subCategory)
  - GetWorkersInSection date filter SQL Where clause (COALESCE-based)
  - Team View numeric pager 20/50/100 + sessionStorage state persist + page reset on filter
  - PHASE 337 COMPLETE 26/26 REQ

affects: [v20.0 milestone close (bundle 14 commit ke origin/main)]

tech-stack:
  added: []
  patterns:
    - "IQueryable composition + .Select projection ke DTO before .ToListAsync materialization"
    - "COALESCE date coalesce in SQL via `(field1 ?? field2)` EF Core 8 translation"
    - "X-Pagination response header (JSON CurrentPage/TotalPages/TotalCount/PageSize) untuk AJAX pager"
    - "passedAssessmentLookup dual mode: sessionsByUser (bila date filter) vs fresh GroupBy count (else)"

key-files:
  created: []
  modified:
    - Services/WorkerDataService.cs
    - Services/IWorkerDataService.cs
    - Controllers/CMPController.cs
    - Views/CMP/RecordsTeam.cshtml

key-decisions:
  - "OQ-337-2 RESOLVED: IQueryable composition + Select projection (NOT raw SQL) — EF Core 8 SQL Server smart compile, portable test"
  - "OQ-337-3 RESOLVED: sessionStorage (NOT cookie) — konsisten dengan Plan 02, per-tab scope"
  - "Backward compat overload pattern: GetAllWorkersHistory() tanpa argumen = identik behavior (5 optional params default null)"
  - "AttemptNumber title-null fallback ke 1 preserved (Plan 01 CMP-11)"
  - "AttemptNumber lookup scoped ke workerIds bila filter aktif (reduce intermediate dictionary size)"
  - "passedAssessmentLookup conditional: derive dari sessionsByUser bila date filter (avoid extra query), else fresh GroupBy count"
  - "Pagination page reset to 1 on filter change (UX guard, hindari stuck di page kosong)"
  - "Pager hidden bila TotalPages <= 1 (12 worker dengan pageSize 20 → no pager visible)"

patterns-established:
  - "EF Core composed IQueryable Where + Select projection = single SELECT JOIN (verified via Database.Command log)"
  - "X-Pagination response header JSON sebagai out-of-band metadata untuk partial view AJAX"

requirements-completed: [CMP-24, CMP-25, CMP-26]

duration: ~25min
completed: 2026-05-30
---

# Phase 337-03: CMP Records Wave 3 Arch SQL Push-Down + Pagination Summary

**3 REQ arch (CMP-24/25/26) implemented + EF Core SQL log-verified push-down + auto-Playwright UAT 3/3 PASS + Wave 1+2 regression PASS.**

**🎉 PHASE 337 COMPLETE: 26/26 REQ delivered (Wave 1 11 + Wave 2 12 + Wave 3 3).**

## Performance

- **Duration:** ~25 min (3 task code + 1 checkpoint UAT)
- **Completed:** 2026-05-30
- **Tasks:** 4 (T1-T3 code, T4 UAT)
- **Files modified:** 4

## Accomplishments

- **SQL push-down GetAllWorkersHistory** (CMP-24) — IQueryable composition + Select projection ke `AllWorkersHistoryRow` DTO; 5 optional filter params (workerIds/from/to/category/subCategory); AttemptNumber lookup scoped
- **Date filter SQL Where** (CMP-25) — `(TanggalMulai ?? Tanggal) >= dateFrom` translate ke `COALESCE([t].[TanggalMulai], [t].[Tanggal]) >= @p` di SQL; assessment session juga COALESCE Schedule
- **Pagination Team View** (CMP-26 + D-02) — numeric pager 20/50/100 page size + sliding window 7 buttons + sessionStorage persist + filter reset page=1
- **Export endpoints push-down** — Export Training/Assessment forward workerIds+from+to+category+subCategory ke GetAllWorkersHistory (no in-memory .Where chain)
- **Backward compat** — Old GetAllWorkersHistory() call still works (5 optional defaults null)

## Task Commits

1. **T1+T2-03: Service SQL push-down (GetAllWorkersHistory + GetWorkersInSection)** — `6cf0efc6` (feat)
2. **T2-03: Controller Export + Pagination signature** — `f082ec51` (feat)
3. **T3-03: RecordsTeam.cshtml Pagination UI + state + page reset** — `e6c8c470` (feat)

## Files Modified

- `Services/WorkerDataService.cs` — GetAllWorkersHistory 5 optional params + IQueryable composition + Select projection; GetWorkersInSection HAPUS .Include(TrainingRecords) + separate query dengan SQL date Where + sessionsByUser/trainingsByUser dictionary lookup
- `Services/IWorkerDataService.cs` — GetAllWorkersHistory signature parity (5 optional params)
- `Controllers/CMPController.cs` — RecordsTeamPartial page/pageSize param + PaginationHelper.Calculate + X-Pagination header; Export Assessment/Training pass filter params ke GetAllWorkersHistory
- `Views/CMP/RecordsTeam.cshtml` — Card-footer pagination UI (page size selector + pager nav) + JS pagination state + renderPagination sliding window + restorePaginationState + filter reset page

## UAT Verification (Auto-Playwright + EF Log)

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| CMP-24 | ✅ PASS | EF log: `WHERE [t].[UserId] IN (@__userIds_0_split)` + Select projection field-by-field di SQL — no full entity materialization. Filter regression: Mandatory HSSE → 2 worker (Rino+Iwan) ✓ |
| CMP-25 | ✅ PASS | EF log: `WHERE [t].[UserId] IN (...) AND COALESCE([t].[TanggalMulai], [t].[Tanggal]) >= @__dateFrom_Value_1 AND COALESCE([t].[TanggalMulai], [t].[Tanggal]) <= @__dateTo_Value_2` (DateTime2 parameterized). Assessment session similar dengan COALESCE Schedule. Date 2026-04 → 5 worker narrow ✓ |
| CMP-26 | ✅ PASS | Pagination UI render "Page 1 dari 1 (12 workers)" + pageSize selector 20/50/100; pageSize=50 toggle → sessionStorage `{page:1, pageSize:50}`; filter reset → page=1 preserved pageSize=50 (12 worker baseline restored) |

**Wave 1+2 Regression Smoke:**
- Category=Mandatory HSSE Training → 2 worker (Rino+Iwan) ✓ identik baseline Plan 01+02
- Date filter narrow + CMP-15 hint visible ✓ Plan 02 baseline
- sessionStorage cmp-records-team-filter + cmp-records-team-pagination both persist ✓
- ARIA tab + role=link row + ViewModel refactor ✓ (no error rendering)

## SQL Push-Down Verification (EF Core Log)

Server-side `Microsoft.EntityFrameworkCore.Database.Command` log capture saat filter Date 2026-04:
```sql
SELECT [t].[Id], [t].[Judul], [t].[Kategori], ...
FROM [TrainingRecords] AS [t]
WHERE [t].[UserId] IN (@__userIds_0_split)
  AND COALESCE([t].[TanggalMulai], [t].[Tanggal]) >= @__dateFrom_Value_1
  AND COALESCE([t].[TanggalMulai], [t].[Tanggal]) <= @__dateTo_Value_2
```

✓ Single SELECT per category (NO N+1)
✓ Parameterized (no SQL injection)
✓ COALESCE-based date coalesce (EF Core 8 idiomatic)
✓ Executed di ~12ms per query

## Threats

| Threat ID | Status |
|-----------|--------|
| T-337-03-01 pageSize injection DoS | mitigated (whitelist 20/50/100 di controller, invalid → 20) |
| T-337-03-02 TotalCount cross-section leakage | mitigated (workerList sudah scoped via L4 lock di GetWorkersInSection) |
| T-337-03-03 SQL cartesian product regression | mitigated (EF log verify single SELECT per query) |
| T-337-03-04 Pagination audit | accept (read-only UI state) |
| T-337-03-05 page param bypass L4 scope | mitigated (pagination apply SETELAH GetWorkersInSection L4 lock) |
| T-337-03-06 X-Pagination header leak proxy | accept (TotalCount non-sensitive) |

## Seed Workflow

- No temporary seed required (arch refactor, no DB mutation)
- DB state: baseline 12 workers untouched

## Lessons & Surprises

- `(h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt) ?? DateTime.MinValue` build ERROR CS0019 — `ArchivedAt` is `DateTime` non-nullable, jadi `??` extra invalid. Fix: hapus extra coalesce (chain already returns DateTime non-null karena ArchivedAt).
- EF Core 8 translate `(t.TanggalMulai ?? t.Tanggal) >= @p` ke `COALESCE([t].[TanggalMulai], [t].[Tanggal]) >= @p` — clean idiomatic SQL.
- `restoreFilterState()` plain set value TIDAK fire change handler (carry-over Plan 02 lesson) — solved via dispatchEvent.
- Pager hidden bila TotalPages <= 1 — `renderPagination` early return. 12 worker dengan pageSize 20 → no pager visible (expected behavior, info text tetap render "Page 1 dari 1").

## Phase 337 Overall

**26/26 REQ delivered, 12 commits total lokal:**
- Wave 1 (Plan 01): 4 commits (T1+T2+T3 code + SUMMARY)
- Wave 2 (Plan 02): 5 commits (T1+T2+T3+T4 code + SUMMARY)
- Wave 3 (Plan 03): 4 commits (T1+T2 service + T2 controller + T3 view + SUMMARY pending)

**Cumulative commits lokal:** 7c65c658, fe3fbe43, 4de88754, 1ee5d8b1, 4ea33336, 53f3b8be, 82dcf22a, 2d6add37, 9e01394d, 6cf0efc6, f082ec51, e6c8c470, + this SUMMARY.

**Bundle status:** NOT pushed ke origin/main (defer v20.0 milestone close, follow Phase 325-335 pattern).

## Next

- v20.0 milestone close: bundle ~13 commit Phase 337 ke origin/main (with IT_NOTIFY.md)
- Phase 338 Cilacap UX + Restore (per ROADMAP v20.0)
