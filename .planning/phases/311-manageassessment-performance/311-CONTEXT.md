# Phase 311: ManageAssessment Performance - Context

**Gathered:** 2026-05-05
**Status:** Ready for planning
**Mode:** auto (autonomous decision pass — system auto mode active, no interactive Q&A)

<domain>
## Phase Boundary

Optimize backend query performance untuk endpoint `GET /Admin/ManageAssessment` (action di `Controllers/AssessmentAdminController.cs:60-227`) sehingga response time p95 ≤ baseline × 0.7 (≥30% improvement) pada dataset produksi.

**In-scope:**
- Assessment tab query chain L66-110 (sessions fetch + projection)
- Distinct Categories query L172 (dropdown source)
- DB indexes pada `AssessmentSessions.ExamWindowCloseDate` dan `LinkedGroupId` jika belum ada
- Baseline + post-patch measurement methodology
- Smoke test parity: grouping & paging hasil identik dengan pre-patch

**Out-of-scope (boundary anchors):**
- Training tab data fetch L206-224 (`GetWorkersInSection`, `GetAllWorkersHistory`) — bisa lambat, tapi ROADMAP SC #7 hanya require smoke test parity, bukan optimization
- History tab queries — same as above
- Pagination algorithm changes (existing `PaginationHelper.Calculate` preserved)
- Search/filter algorithm changes (only AsNoTracking + index, not query rewrites)
- View rendering performance (Razor view L226 not touched)
- Frontend caching / SignalR push refresh — separate phase scope
- New capabilities (e.g., infinite scroll, server-side rendering changes) — belong in other phases

</domain>

<decisions>
## Implementation Decisions

### Cache Strategy (Categories distinct dropdown)

- **D-01:** Cache key static `"assessment_categories_distinct"` (single global key, no per-user/per-tenant variant). Rationale: Categories list is project-wide constant, identical untuk semua admin/HC users; per-user differentiation tidak applicable.

- **D-02:** TTL = 5 menit absolute expiration via `MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }`. Pure time-based eviction, no sliding/LRU. Rationale: ROADMAP SC #5 explicit, Categories rarely change (CRUD operations not in this controller's hot path), 5-min staleness fully acceptable user-side.

- **D-03:** No explicit cache invalidation pada Category CRUD operations. Strategy = TTL drift only. Rationale: simpler implementation; if admin add/edit/delete category, dropdown shows new value within ≤5 min. Trade-off accepted (consistent with caching philosophy: prefer simpler invalidation when staleness window is acceptable). Future enhancement noted in deferred ideas.

- **D-04:** Cache miss path queries via `.AsNoTracking()` (read-only fetch into `IMemoryCache`):
  ```csharp
  ViewBag.Categories = await _cache.GetOrCreateAsync("assessment_categories_distinct", async entry => {
      entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
      return await _context.AssessmentSessions
          .AsNoTracking()
          .Select(a => a.Category)
          .Distinct()
          .OrderBy(c => c)
          .ToListAsync();
  });
  ```

### Database Indexes

- **D-05:** Add index `IX_AssessmentSessions_ExamWindowCloseDate` (single-column) — `Schedule` already indexed (`Data/ApplicationDbContext.cs:180` `entity.HasIndex(a => a.Schedule)`). Composite `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` per ROADMAP SC #4 *literal* dapat di-add SEBAGAI tambahan, namun query L67 `(a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo` mengandung COALESCE yang menghalangi perfect index seek pada composite — optimizer akan tetap melakukan scan bahkan dengan composite. Rationale: rely on query optimizer untuk choose best plan dari dua single-column indexes. **Defer composite ke optional optimization** kalau measurement post-patch menunjukkan masih ada bottleneck.

- **D-06:** Add index `IX_AssessmentSessions_LinkedGroupId` (single-column) — currently belum ada (verified via grep `Data/ApplicationDbContext.cs`). Used by `GroupBy(a => a.LinkedGroupId)` di L117. Rationale: ROADMAP SC #4 explicit + `LinkedGroupId` is FK-like nullable column heavily used in pre-post grouping path.

- **D-07:** Migration name: `20260506000000_AddManageAssessmentPerfIndexes` (exact timestamp resolved at execution time). Single migration containing both indexes (D-05, D-06). Rationale: atomic deploy, easy rollback. Generated via `dotnet ef migrations add AddManageAssessmentPerfIndexes`.

### Query Optimization

- **D-08:** Apply `.AsNoTracking()` pada `managementQuery` chain start (L66) — single insertion sebelum `.Where(...)`. Rationale: full read-only path (controller hanya project ke anonymous type at L91-110, no entity tracking needed). EF Core change tracking disabled = ~10-20% memory + CPU saving on result hydration.

- **D-09:** Remove `.Include(a => a.User)` L88. Projection L106-108 (`a.User != null ? a.User.FullName : "Unknown"` etc.) sudah reference nav property — EF Core auto-translate ke SQL `LEFT JOIN` tanpa Include. Rationale: explicit Include dengan projection adalah anti-pattern (EF generates redundant entity materialization yang lalu di-discard). Verified via existing pattern di Phase 296+ refactors.

- **D-10:** Verify projection masih emit `LEFT JOIN AspNetUsers` post-removal — capture EF Core generated SQL via logger pre/post + diff. Required acceptance criterion (manual verify selama execute, NOT new automated test).

### Measurement Methodology

- **D-11:** Baseline = `Stopwatch` di action body (`ManageAssessment`), wrap dari L64 (start) sampai `return View()` L226 (stop). Log via `_logger.LogInformation("ManageAssessment perf: tab={Tab} elapsed={Ms}ms", tab, sw.ElapsedMilliseconds)`. Rationale: lightweight (microsecond overhead), captures full request scope (DB + grouping + pagination + ViewBag), no external infra dependency, production-deployable.

- **D-12:** Baseline run protocol:
  1. Run pre-patch 5x dengan filter cold (no search), `tab=assessment`, page=1, pageSize=20
  2. Record p50/p95 dari log entries (skip first run as JIT warmup)
  3. Apply patch (D-04..D-09)
  4. Run post-patch 5x identical filter
  5. Compute improvement: `(baseline_p95 - postpatch_p95) / baseline_p95 ≥ 0.30`
  6. Document di SUMMARY.md hasil pre/post numbers + improvement %

- **D-13:** Stopwatch logging permanent (NOT removed post-validation) — biaya negligible, value untuk ongoing perf monitoring di production.

### Scope Boundaries

- **D-14:** Patch hanya `ManageAssessment` action L60-227. JANGAN touch `GetWorkersInSection` (Services/WorkerDataService.cs), `GetAllWorkersHistory`, atau Training/History tab queries. Rationale: ROADMAP SC #7 require smoke test parity untuk Training/History tabs (grouping & paging identical), tidak require optimization.

- **D-15:** Smoke test methodology untuk SC #7: navigate ke `/Admin/ManageAssessment` pre + post patch dengan kombinasi (tab=assessment, tab=training, tab=history), verify:
  - Page load tidak error (200 OK)
  - Grouping output struktur sama (count rows, pagination headers)
  - Paging totalPages identical untuk same dataset

### Claude's Discretion

- **Migration timestamp generation:** Auto-generated via `dotnet ef migrations add` — no override.
- **Stopwatch logging format:** structured logging fields (tab, elapsed_ms, search_term_present, page) — exact field names di-finalize saat planning.
- **EF Core SQL diff capture method:** preferred via `_context.Database.SetCommandTimeout` + `LogTo` callback OR `EFCoreLogger` middleware — planner decides cleanest approach.
- **Cache key namespace:** `"assessment_categories_distinct"` literal (no shared cache helper extraction) — sufficient untuk single-use case Phase 311.

### Folded Todos

None — `realtime-assessment.md` todo (created 2026-03-09) is unrelated to perf optimization.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Roots & Conventions

- `.planning/REQUIREMENTS.md` §PERF-01 — Acceptance criteria authoritative source (≥30% p95 improvement, strategi explicit)
- `.planning/ROADMAP.md` §"Phase 311: ManageAssessment Performance" — 7 SC + Risk/Effort tags
- `.planning/PROJECT.md` — Project principles (e.g., backward compatibility, observability convention)

### Source Files (Modify Targets)

- `Controllers/AssessmentAdminController.cs:60-227` — `ManageAssessment` action target
- `Data/ApplicationDbContext.cs:170-200` — `AssessmentSessions` entity config (existing indexes)
- `Program.cs:17` — `builder.Services.AddMemoryCache()` (already registered, no add)

### Migration Reference

- `Migrations/ApplicationDbContextModelSnapshot.cs:440-460` — current `AssessmentSessions` index snapshot
- Existing single-column index pattern di `Data/ApplicationDbContext.cs:178-181` — analog untuk new indexes (D-05, D-06)

### EF Core Pattern References

- Phase 296 SUMMARY (`.planning/phases/296-data-foundation-grading-service/296-SUMMARY.md` if exists) — `.AsNoTracking()` precedent on read-only projection paths
- `docs/test 16-april/DATABASE_ANALYSIS.md:1417` — existing analysis confirming `HasIndex(Schedule)` already exists (informs D-05 decision)

### Logging Convention

- Existing `_logger.LogInformation` calls in same controller untuk consistency reference (struktur logging field names)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`IMemoryCache _cache`** sudah injected ke `AssessmentAdminController` constructor (L22, L34). Tidak perlu DI registration — langsung pakai `_cache.GetOrCreateAsync(...)`.
- **`PaginationHelper.Calculate`** L180 — existing pagination utility. Preserve as-is, no changes needed.
- **`_logger`** instance available via constructor injection — used untuk Stopwatch logging (D-11).

### Established Patterns

- **EF Core projection without Include** (anti-Include-redundancy pattern): codebase sudah pakai pola ini di Phase 296+ refactors. New work D-09 mengikuti precedent.
- **Single-column HasIndex** convention di `ApplicationDbContext.cs:178-200` — analog untuk D-05, D-06 additions.
- **Migration naming convention** `YYYYMMDDHHmmss_Description.cs` — auto-generated by EF tools, no manual override.
- **MemoryCache TTL pattern** — first usage di codebase untuk this controller; no existing pattern, set new precedent dengan `AbsoluteExpirationRelativeToNow`.

### Integration Points

- **No external API dependencies** — pure DB + memory cache.
- **ViewBag contract** — Razor view `Views/Admin/ManageAssessment.cshtml` (TBD verify exists) consume `ViewBag.Categories`, `ViewBag.ManagementData`, etc. Patch must preserve ViewBag shape exactly.
- **No SignalR / real-time push** — read-only request-response pattern.

### Constraints / Gotchas

- **COALESCE in WHERE clause** L67 `(a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo` translates ke SQL `ISNULL(ExamWindowCloseDate, Schedule)` — this prevents pure index seek even with composite index. Optimizer mostly falls back ke scan + filter. D-05 honest about this limitation.
- **`Distinct().OrderBy()` di Categories query** L172-176 — without index pada Category column, this is a sort-distinct table scan. Cache hit path eliminates this (only run cold). NOT in scope to add `IX_AssessmentSessions_Category`.
- **Backward compat** — controller signature dan ViewBag shape WAJIB unchanged. Only internal implementation changes.

</code_context>

<specifics>
## Specific Ideas

- **Logging field names** preferred to match existing structured logging in codebase. Planner to verify by grep `_logger.LogInformation` di same controller for naming pattern.
- **Migration ordering** — sebelum execute, verify no pending migration di `Migrations/` directory yang belum di-apply ke Dev DB (gosulu `dotnet ef migrations list`). New migration appended at HEAD, no edit existing.
- **Reproducibility of measurement** — baseline + postpatch runs harus dengan dataset yang sama (no data mutation between runs). If running on local Dev DB, snapshot DB state before run.

</specifics>

<deferred>
## Deferred Ideas

- **Composite index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate`** — defer to optional optimization phase if D-05's two single-column indexes don't yield sufficient improvement. Add via separate migration.
- **Persisted computed column `EffectiveDate = COALESCE(ExamWindowCloseDate, Schedule)` + index** — alternative to fix COALESCE seek issue. Defer (schema migration overhead, breaks backward compat with raw SQL queries elsewhere).
- **Categories cache invalidation on Category CRUD** — TTL drift acceptable for now. If user reports stale dropdowns become annoying, add explicit cache key bump via `_cache.Remove("assessment_categories_distinct")` di `CreateCategory/UpdateCategory/DeleteCategory` actions. Track as future improvement.
- **Training/History tab perf optimization** — `GetWorkersInSection` (L210) dan `GetAllWorkersHistory` (L212) potentially slow on large worker datasets. Out of scope Phase 311. Could be Phase 315 or backlog item.
- **MiniProfiler integration** — comprehensive request profiling tool. Defer untuk future observability phase.
- **Application Insights / OpenTelemetry** — production-grade APM. Out of scope, separate infra phase.

### Reviewed Todos (not folded)

- `realtime-assessment.md` (2026-03-09) — concept of real-time assessment monitoring. Unrelated to perf optimization. Stays in todos backlog.

</deferred>

---

*Phase: 311-manageassessment-performance*
*Context gathered: 2026-05-05 (autonomous pass — auto mode active)*
*Auto-mode log: All 15 decisions selected via codebase analysis + ROADMAP literal interpretation. No interactive Q&A.*
