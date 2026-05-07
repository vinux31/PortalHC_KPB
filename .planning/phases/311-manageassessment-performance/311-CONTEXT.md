# Phase 311: ManageAssessment Performance - Context

**Gathered:** 2026-05-05 (initial), revised 2026-05-07 (post-baseline brainstorm)
**Status:** Ready for planning (replan required — direction reframed)
**Mode:** interactive discuss — 4 gray areas selected, all answered

---

## ⚠ Direction reframed (2026-05-07)

Original Phase 311 direction (backend query optimization: AsNoTracking + 2 indexes + Categories cache, target ≥30% backend p95 reduction) **superseded by brainstorm session 2026-05-07** based on baseline measurement evidence.

**Reframe rationale:**
- Per-segment Stopwatch baseline (commit `a4ce556e`) menunjukkan backend cepat: TTFB 281ms di server `10.55.3.3`, total 11-27ms warm di Dev DB. Backend bukan bottleneck.
- User confirmation 3 datapoint network: wifi lain instan, hotspot HP <10s, wifi kantor >1 menit. Server, code, DB sama → bottleneck di network path proxy Pertamina.
- Chrome DevTools wifi kantor: TTFB 281ms (cepat), content download 1.6 menit (40 byte/sec throughput) untuk 3.4 KB document — proxy throttle/inspection.
- Strategi yang bekerja: minimize HTML payload yang lewat proxy via lazy-load per tab.

**Approved direction:** HTMX lazy-load architecture (Opsi 2 dari 6 yang dievaluasi). See `311-DESIGN.md` (approved 2026-05-07).

**Old decisions D-01..D-16 (initial 2026-05-05):** mostly SUPERSEDED. Preserved di git history (commits `fd2bcf15`, `44b11e66`). Stopwatch instrumentation D-11/D-13 (commit `a4ce556e`) **preserved** — dipindah ke per-action.

---

<domain>
## Phase Boundary

Refactor `Controllers/AssessmentAdminController.cs` action `ManageAssessment` (L60-262) menjadi shell + 3 partial endpoints, plus refactor 3 Razor partials (`Views/Admin/Shared/_AssessmentGroupsTab.cshtml`, `_TrainingRecordsTab.cshtml`, `_HistoryTab.cshtml`) ke pattern HTMX lazy-load. Vendor HTMX 2.0.x latest stable di `wwwroot/lib/htmx/`. Plus opportunistic backend wins (AsNoTracking + 2 indexes + Categories cache) sebagai Plan 03.

**Acceptance criteria:**
- Initial response document: <14 KB (TCP first roundtrip)
- End-to-end load wifi kantor: ≤40 detik (dari baseline ~1.4 menit) — ≥50% reduction
- Tab switching latency post-initial: ≤2 detik di wifi kantor
- Smoke test parity: visual struktur identik per tab (kolom, row count, ordering)
- No regression backend: TTFB ≤500ms
- Backward compat: filter (search/category/status), pagination behavior preserved
- ViewBag contract preserved untuk partial endpoints

**In-scope:**
- Refactor `ManageAssessment` action → shell-only (no data fetch except cached Categories untuk dropdown)
- Create 3 partial actions: `ManageAssessmentTab_Assessment`, `_Training`, `_History` → return `PartialView`
- Refactor `Views/Admin/ManageAssessment.cshtml` → HTMX attributes + skeleton placeholders
- Vendor HTMX 2.0.x ke `wwwroot/lib/htmx/`
- Filter form HTMX trigger setup (debounce 500ms search, instant dropdowns)
- AJAX pagination via HTMX
- Error handling template (alert + retry)
- Plan 03 opportunistic backend (AsNoTracking + IX_LinkedGroupId + IX_ExamWindowCloseDate + IMemoryCache Categories TTL 5min)
- Update REQUIREMENTS.md §PERF-01 dengan strategy + criteria baru
- Stopwatch instrumentation moved to per-action context (preserve `a4ce556e` pattern)
- Smoke test parity manual UAT per tab

**Out-of-scope (boundary anchors):**
- Network/proxy investigation (escalate paralel ke IT, bukan code domain)
- Service worker / advanced frontend caching
- WebSocket / real-time updates
- SSR streaming / SPA migration
- Composite indexes / persisted computed columns
- Unrelated refactor di controller atau view

</domain>

<decisions>
## Implementation Decisions (revised 2026-05-07)

### Trigger Semantics

- **D-01:** Inactive tab (Training/History) trigger via Bootstrap 5 native event integrasi: `hx-trigger='shown.bs.tab from:closest button.nav-link once'`. Listens to Bootstrap event setelah tab visually aktif (animation done, DOM ready) — respect Bootstrap tab lifecycle. Active tab pakai `hx-trigger="load"` (auto-fire saat shell render).

- **D-02:** HTMX 2.0.x latest stable. Vendor lokal di `wwwroot/lib/htmx/htmx.min.js` (~14 KB minified). Bukan CDN — proxy Pertamina mungkin block/lambat external CDN. Drop IE11 support acceptable karena admin user pakai Chrome/Edge modern.

### Filter Form Behavior

- **D-03:** Live filter dengan debounce. Search box: `hx-trigger='input changed delay:500ms'` (debounce 500ms). Dropdown category & status: `hx-trigger='change'` (instant fire). HTMX `hx-target` = pane tab aktif, `hx-include="#filter-form"` (semua filter values dikirim). Filter trigger affects only active tab fetch (lihat D-05 cache invalidation).

- **D-04:** Pagination via AJAX HTMX. Pagination links di partial render dengan `hx-get` atribut → swap pane tab aktif dengan page baru. No full page reload. Konsisten dengan pattern HTMX di tab lain.

### Tab Cache Invalidation Strategy

- **D-05:** Filter change → invalidate semua tab. Implementation: filter form change trigger fire pada tab aktif (re-fetch dengan filter baru), DAN trigger JS untuk reset `data-loaded="true"` flag pada tab non-aktif (atau strip cached HTML). Saat user pindah ke tab non-aktif setelah filter, lazy-fetch ulang dengan filter baru. Pro: data konsisten, no stale state. Cost: 1 round trip extra saat user pindah tab post-filter (acceptable karena partial size kecil 30-50 KB).

- **D-06:** Partial endpoint HTTP response: `Cache-Control: no-store`. Implementasi: tambah `[ResponseCache(NoStore = true)]` attribute pada 3 partial actions, ATAU set di middleware. Browser tidak cache → tidak ada stale data dari browser cache. HTMX manage tab cache di JS-side, tidak butuh HTTP cache.

### Plan Scope: Old Backend Patches Fate

- **D-07:** Plan 03 opportunistic same-phase. Keep AsNoTracking + IX_LinkedGroupId + IX_ExamWindowCloseDate + Categories MemoryCache (TTL 5min) sebagai Plan 03 di Phase 311 yang dijalankan PARALEL/SETELAH Plan 02 HTMX. Justifikasi: low-cost (~50 baris kode + 1 migration), small wins backend (10-20% per partial action), resilience untuk scaling future. Tidak block Plan 02. PERF-01 traceable.

- **D-08:** Update `REQUIREMENTS.md` §PERF-01 sebagai task awal di Plan 02. Edit:
  - Acceptance: ganti "p95 ≥30% reduction backend" → "≤40 detik di wifi kantor (≥50% reduction dari baseline 1.4 min), <14 KB initial doc, ≤2s tab switch"
  - Strategy: ganti "AsNoTracking + indexes + IMemoryCache" → "HTMX lazy load per tab + 3 partial endpoints + Plan 03 opportunistic backend (AsNoTracking + 2 indexes + Categories cache)"
  - Source: reference `311-DESIGN.md` (approved 2026-05-07)

### Preserved from Prior Phase 311 Work

- **D-09 (preserved from old D-11/D-13):** Stopwatch instrumentation per-action. Pattern dari commit `a4ce556e` (sekarang single-action 5 Stopwatch) di-pindah ke per-action context: 1 Stopwatch per partial action (`ManageAssessmentTab_Assessment` measure T1, `_Training` measure T2+T4, `_History` measure T3). Logger format diadaptasi: `"ManageAssessment perf [tab={Tab}]: elapsed={Ms}ms search_present={SearchPresent} page={Page}"`. Permanent (NOT removed post-validation).

- **D-10 (preserved project principle):** Backward compat. ViewBag/contract harus preserved untuk partial endpoints — Razor partials existing (`_AssessmentGroupsTab.cshtml`, `_TrainingRecordsTab.cshtml`, `_HistoryTab.cshtml`) di-render via partial action endpoint dengan ViewBag shape sama persis dengan rendering inline lama. Smoke test parity manual UAT verifikasi.

### Claude's Discretion (planner pilih)

- **HTMX swap mode**: `hx-swap="innerHTML"` (default, tukar konten dalam div) atau `outerHTML`. Default: `innerHTML`.
- **Skeleton style**: Bootstrap 5 `.placeholder-glow` + `.placeholder` classes (sudah ada di project). Atau custom CSS.
- **Error template**: Simple `<div class="alert alert-danger">...<button onclick="htmx.trigger(...)">Coba lagi</button></div>`. Style by Claude.
- **Partial action method naming**: `ManageAssessmentTab_Assessment`, `_Training`, `_History` (PascalCase, underscore separator) sesuai konvensi controller existing.
- **Migration timestamp**: auto-generated by `dotnet ef migrations add AddManageAssessmentPerfIndexes` (Plan 03).
- **Active tab `hx-trigger`**: `hx-trigger="load"` (immediate after HTMX init) atau `hx-trigger="load delay:50ms"` (after first paint). Default: `load`.
- **Race condition handling**: HTMX `hx-sync="this:replace"` untuk auto-cancel previous in-flight saat user cepat klik antar tab.

### Folded Todos

None — `realtime-assessment.md` (matched score 0.6) tidak relevan ke perf optimization (different concern: real-time monitoring assessment rooms).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents (researcher, planner, executor) MUST read these before acting.**

### Project Roots & Conventions

- `.planning/REQUIREMENTS.md` §PERF-01 — Acceptance criteria authoritative source (akan di-update di Plan 02 task awal per D-08)
- `.planning/ROADMAP.md` §"Phase 311: ManageAssessment Performance" — 7 SC + Risk/Effort tags (mostly superseded; planner reconcile)
- `.planning/PROJECT.md` — Project principles (backward compatibility, response language Bahasa Indonesia)

### Phase 311 Internal Refs

- `.planning/phases/311-manageassessment-performance/311-DESIGN.md` — **APPROVED design doc 2026-05-07** (source of truth direction)
- `.planning/phases/311-manageassessment-performance/311-RESEARCH.md` — research lama; sebagian still relevan untuk Plan 03 (Pitfall 6/7/9 backend)
- `.planning/phases/311-manageassessment-performance/311-PATTERNS.md` — pattern map lama (mostly superseded; planner re-derive)
- `.planning/phases/311-manageassessment-performance/311-DISCUSSION-LOG.md` — full Q&A audit trail discuss session

### Source Files (Modify Targets)

- `Controllers/AssessmentAdminController.cs:60-262` — `ManageAssessment` action refactor target (split jadi shell + 3 partial actions)
- `Views/Admin/ManageAssessment.cshtml` (176 lines) — shell view refactor (HTMX attrs + skeleton)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (333 lines) — di-render via partial action `ManageAssessmentTab_Assessment`
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (337 lines) — di-render via partial action `ManageAssessmentTab_Training`
- `Views/Admin/Shared/_HistoryTab.cshtml` (148 lines) — di-render via partial action `ManageAssessmentTab_History`
- `wwwroot/lib/` — vendor target untuk HTMX 2.0.x

### Plan 03 Backend Targets

- `Data/ApplicationDbContext.cs:170-200` — AssessmentSessions entity config (existing indexes pattern)
- `Migrations/ApplicationDbContextModelSnapshot.cs:440-460` — current snapshot (Plan 03 add IX_LinkedGroupId + IX_ExamWindowCloseDate)
- `Program.cs:17` — `AddMemoryCache()` already registered (no add)

### External Specs / Best Practices (research-cited 2026-05-07)

- HTMX docs — `https://htmx.org/docs/` (trigger syntax, swap modes, hx-include semantics)
- HTMX lazy load pattern — `https://htmx.org/examples/lazy-load/`
- Cloudflare TCP 14 KB rule — `https://www.cloudflare.com/learning/performance/speed-up-a-website/`
- Mike's Razor Pages + Bootstrap lazy load — `https://www.mikesdotnetting.com/article/350/razor-pages-and-bootstrap-lazy-loading-tabs`
- MDN deferred vs lazy — `https://developer.mozilla.org/en-US/docs/Web/Performance/Guides/Lazy_loading`

### EF Core / Bootstrap Patterns (existing project conventions)

- Phase 296+ refactors — `.AsNoTracking()` precedent on read-only projection paths
- `Data/ApplicationDbContext.cs:178-181` — single-column `HasIndex` convention (analog untuk D-07 Plan 03 indexes)
- Bootstrap 5 placeholder classes — already loaded di project (`bootstrap-icons.css` confirmed di Network tab screenshot 2026-05-07)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`IMemoryCache _cache`** sudah injected ke `AssessmentAdminController` constructor (L22, L34). Untuk Plan 03 Categories cache.
- **`PaginationHelper.Calculate`** L191 — existing pagination utility. Preserve as-is, partial actions tetap pakai.
- **`_logger`** instance available — used untuk Stopwatch logging (D-09).
- **Bootstrap 5 `.placeholder-glow` + `.placeholder` classes** — sudah aktif di project. Skeleton bisa langsung pakai (Claude's Discretion).
- **jQuery available** — bisa dipakai untuk JS handler kalau perlu, tapi HTMX handle most cases.

### Established Patterns

- **EF Core projection without Include** (anti-Include-redundancy pattern): codebase sudah pakai pola ini di Phase 296+ refactors. Plan 03 D-09 mengikuti precedent.
- **Single-column `HasIndex` convention** di `ApplicationDbContext.cs:178-200` — analog untuk Plan 03 indexes.
- **PartialView return** pattern: existing controllers banyak return `PartialView("~/Views/Admin/Shared/_...cshtml", model)` — analog untuk 3 partial actions baru.
- **`[Authorize(Roles = "Admin, HC")]`** consistent di `AssessmentAdminController` — apply ke 3 partial actions baru juga.
- **PathBase `/KPB-PortalHC`** — semua URL relatif harus respect prefix ini (verified `appsettings.json:9` + `Program.cs:186` `app.UsePathBase`).

### Integration Points

- **No external API dependencies** — pure DB + memory cache + Razor partials.
- **Bootstrap 5 nav-tabs structure** existing di `Views/Admin/ManageAssessment.cshtml:57-91` — preserve struktur button[data-bs-toggle="tab"] + tab-pane#pane-{tab}, modify isi tab-pane jadi HTMX-driven.
- **Filter form** existing di view — wrap dalam `<form id="filter-form">` (atau pakai existing form id), tambah `hx-include="#filter-form"` di tab pane HTMX attrs.
- **No SignalR / real-time push** — read-only request-response pattern.

### Constraints / Gotchas

- **PathBase `/KPB-PortalHC`** — HTMX `hx-get` URL harus include path base. Pakai Razor helper `@Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin")` untuk auto-resolve.
- **Bootstrap event timing** — `shown.bs.tab` fires AFTER tab transition complete. HTMX listener via `from:closest button.nav-link` perlu careful: button ada di nav, tab pane di sibling div. Verify selector during planning.
- **HTMX 2.0 breaking changes** — beberapa atribut renamed dari 1.x. Planner reference 2.0 docs khusus.
- **Backward compat** — controller signature `ManageAssessment(string? search, int page = 1, ...)` WAJIB unchanged untuk URL bookmarks tetap valid. Internal hanya jadi shell, parameter tetap accepted (forwarded ke partial endpoints via `hx-include`).

</code_context>

<specifics>
## Specific Ideas

- **HTMX integration ASP.NET Core MVC** — pakai `Url.Action()` Razor helper untuk URL resolution (auto-respect PathBase). Jangan hardcode URL.
- **Vendor HTMX**: download `htmx.min.js` v2.0.x dari https://unpkg.com/htmx.org@2.0/dist/htmx.min.js, save ke `wwwroot/lib/htmx/htmx.min.js`. Verify SHA hash kalau ada di docs official.
- **Skeleton placeholder height**: match approximate height tabel actual (3-5 row × ~60px) supaya layout shift minimal saat content swap.
- **Stopwatch logging**: tetap `_logger.LogInformation(...)` dengan structured fields. Per partial action 1 Stopwatch, log saat return.
- **Filter form id**: kalau view existing belum punya `<form id="filter-form">`, tambah saat refactor shell view.

</specifics>

<deferred>
## Deferred Ideas

- **Composite index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate`** — defer ke phase masa depan kalau Plan 03 single-column indexes belum cukup. Schema migration overhead untuk persisted computed column tidak justified saat ini.
- **Persisted computed column `EffectiveDate = COALESCE(ExamWindowCloseDate, Schedule)` + index** — defer (schema migration overhead, breaks backward compat dengan raw SQL queries elsewhere).
- **Frontend service worker / advanced caching** — overkill untuk masalah saat ini.
- **WebSocket / real-time updates** untuk ManageAssessment — different concern, tidak related ke proxy lag.
- **Lazy-load pattern reuse di halaman lain** (ManageWorkers, Coach Workload, etc.) — defer; setelah Phase 311 prove pattern bekerja, evaluate replicate ke halaman lain di phase masa depan.
- **MiniProfiler / Application Insights / OpenTelemetry** — out of scope, separate observability phase.
- **Move filter logic ke client-side** (filter without round trip) — defer; client-side filtering butuh prefetch all data which contradicts payload-reduction goal.

### Reviewed Todos (not folded)

- `realtime-assessment.md` (2026-03-09, score 0.6) — concept of real-time assessment monitoring (live status push?). Different concern dari lazy-load architecture. Tidak relevan untuk Phase 311. Stays di todos backlog untuk evaluation di milestone berikutnya.

</deferred>

---

*Phase: 311-manageassessment-performance*
*Context revised: 2026-05-07 (interactive discuss-phase --revise)*
*Approved direction: HTMX lazy load (Opsi 2 dari 6, brainstorm 2026-05-07)*
*Decisions: 8 new (D-01..D-08) + 2 preserved (D-09 Stopwatch, D-10 backward compat) + 6 Claude's Discretion items*
*Old D-01..D-16 (initial 2026-05-05): superseded; preserved di git history.*
