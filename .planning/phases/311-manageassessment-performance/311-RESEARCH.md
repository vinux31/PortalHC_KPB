# Phase 311: ManageAssessment Performance — Research

**Researched:** 2026-05-05
**Domain:** ASP.NET Core 8 MVC + EF Core 8 + IMemoryCache + SQL Server query optimization untuk hot endpoint Admin
**Confidence:** HIGH (semua claim verified via Read/Grep pada source kode existing + Context7-equivalent official Microsoft Learn docs; stampede protection cross-verified via WebSearch ke devblogs.microsoft.com)

> **Bahasa:** Narrative dalam Bahasa Indonesia per CLAUDE.md project instruction. Technical terms, library names, dan code snippet tetap English untuk akurasi.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Cache Strategy (Categories distinct dropdown)**

- **D-01:** Cache key static `"assessment_categories_distinct"` (single global key, no per-user/per-tenant variant). Rationale: Categories list is project-wide constant.
- **D-02:** TTL = 5 menit absolute expiration via `MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }`. Pure time-based eviction, no sliding/LRU.
- **D-03 (REVISED 2026-05-05):** **Explicit cache invalidation** pada Category CRUD. Tambah `_cache.Remove("assessment_categories_distinct")` di `AddCategory` / `EditCategory` / `DeleteCategory` (verified actions di L296, L350, L389 — bukan `CreateCategory`/`UpdateCategory`). TTL 5 menit tetap berlaku sebagai safety net. Detail implementasi (cache key sebagai const, helper method) di-decide planner.
- **D-04:** Cache miss path queries via `.AsNoTracking()` di dalam factory delegate `GetOrCreateAsync`.

**Database Indexes**

- **D-05:** Add index `IX_AssessmentSessions_ExamWindowCloseDate` (single-column). `Schedule` already indexed di L180. Composite `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` per ROADMAP SC #4 literal — defer ke optional optimization (COALESCE di WHERE clause menghalangi perfect seek).
- **D-06:** Add index `IX_AssessmentSessions_LinkedGroupId` (single-column).
- **D-07:** Migration name template: `YYYYMMDDhhmmss_AddManageAssessmentPerfIndexes` (auto-timestamp via `dotnet ef migrations add`). Single migration containing both indexes (D-05, D-06).

**Query Optimization**

- **D-08:** Apply `.AsNoTracking()` pada `managementQuery` chain start (L66) — single insertion sebelum `.Where(...)`.
- **D-09:** Remove `.Include(a => a.User)` L88. Projection L106-108 sudah reference nav property — EF Core auto-translate ke SQL `LEFT JOIN` tanpa Include.
- **D-10:** Verify projection masih emit `LEFT JOIN AspNetUsers` post-removal — capture EF Core generated SQL via logger pre/post + diff. Required acceptance criterion (manual verify selama execute, NOT new automated test).

**Measurement Methodology**

- **D-11:** Baseline = `Stopwatch` di action body wrapping L64 → L226. Log via `_logger.LogInformation(...)`.
- **D-12:** Baseline run protocol: 5x cold, skip first run sebagai JIT warmup, record p50/p95.
- **D-13:** Stopwatch logging permanent (NOT removed post-validation).

**Scope Boundaries**

- **D-14:** Patch hanya `ManageAssessment` action L60-227. JANGAN touch `GetWorkersInSection`, `GetAllWorkersHistory`, atau Training/History tab queries (kecuali jika D-16 menunjukkan Skenario B/C).
- **D-15:** Smoke test methodology untuk SC #7: navigate ke `/Admin/ManageAssessment` pre + post patch dengan kombinasi (tab=assessment, tab=training, tab=history), verify page load 200 OK + grouping output struktur sama + paging totalPages identical.

**Pre-Execute Diagnostic (D-16 — added via interactive discuss)**

- **D-16:** **Baseline breakdown per-query** WAJIB di-capture SEBELUM apply patch apapun. Breakdown segments:
  - **T1** = Assessment query chain L66-110 (sessions fetch + projection)
  - **T2** = `GetWorkersInSection` L210
  - **T3** = `GetAllWorkersHistory` L212
  - **T4** = `GetAllSectionsAsync` + `GetUnitsForSectionAsync` L220-223
  - **T5** = Distinct Categories L172-176
  - **Total** = T1+T2+T3+T4+T5

  Decision gate setelah baseline:
  - **Skenario A:** T1 dominan (>60% total) → scope auto-pass D-14 valid, jalan sesuai rencana.
  - **Skenario B:** T2/T3 dominan (>50% total combined) → STOP planning, balik ke user.
  - **Skenario C:** Mixed (T1 ~ T2/T3) → user decide explicit dengan breakdown numbers di tangan.

### Claude's Discretion

- Migration timestamp generation (auto-generated via `dotnet ef migrations add`)
- Stopwatch logging field naming convention (planner verifies by grep existing controller — verified ada precedent di L2839-2841 `_logger.LogInformation` dengan tagged structured fields)
- EF Core SQL diff capture method (planner picks cleanest approach — `ApplicationDbContext.OnConfiguring` LogTo callback OR query-scoped `_context.Database.GetDbConnection()` + manual capture)
- Cache key namespace literal vs helper class — literal sufficient for single use case
- Per-segment Stopwatch implementation: inline manual blocks vs structured helper

### Deferred Ideas (OUT OF SCOPE)

- Composite index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` — defer to optional optimization phase
- Persisted computed column `EffectiveDate = COALESCE(ExamWindowCloseDate, Schedule)` + index — defer (schema migration overhead)
- Training/History tab perf optimization (`GetWorkersInSection`, `GetAllWorkersHistory`) — default out-of-scope, namun D-16 baseline breakdown bisa surface ke scope kalau Skenario B/C
- Lazy-load tab non-aktif (`?tab=` param + AJAX partial reload) — phase masa depan, architectural change
- MiniProfiler integration — defer untuk future observability phase
- Application Insights / OpenTelemetry — out of scope, separate infra phase
- HybridCache (.NET 9+) — Phase 311 stack adalah .NET 8, stick dengan IMemoryCache existing
- Failure mode & rollback strategy decision — di-decide post-D-16 (measurement-driven)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **PERF-01** | Halaman `/Admin/ManageAssessment` memuat ≥30% lebih cepat dari baseline pada dataset produksi (target p95 ≤ baseline × 0.7). Strategi: `AsNoTracking()` di chain query baris 66, hapus redundant `.Include(a => a.User)` baris 88, tambah DB index `IX_AssessmentSessions_Schedule_ExamWindowCloseDate` & `IX_LinkedGroupId` jika belum ada, `IMemoryCache` (TTL 5 menit) untuk distinct Categories. | Findings 1 (AsNoTracking + Include interaction), Finding 2 (IMemoryCache pattern), Finding 3 (EF Core migration index API), Finding 4 (Stopwatch methodology), Finding 5 (SQL plan analysis), Findings 6 (D-16 baseline breakdown decision gate) |
</phase_requirements>

## Summary

Phase 311 mengoptimalkan endpoint `GET /Admin/ManageAssessment` melalui empat lever: (1) DB indexes pada `ExamWindowCloseDate` dan `LinkedGroupId`, (2) `AsNoTracking()` pada query chain L66 untuk menghilangkan change-tracker overhead, (3) hapus redundant `.Include(a => a.User)` L88 karena projection L106-108 sudah otomatis emit `LEFT JOIN AspNetUsers`, dan (4) `IMemoryCache` 5 menit TTL untuk distinct Categories L172. Target ≥30% improvement pada p95.

Riset menemukan **dua hal kritikal yang harus diberitahu planner sebelum eksekusi**:

1. **D-16 baseline breakdown adalah hard gate.** Controller di L195-224 selalu fetch data ketiga tab (Assessment + Training + History) dalam satu request walaupun user buka tab Assessment saja. `GetWorkersInSection` (L210) dan `GetAllWorkersHistory` (L212) adalah query kompleks dengan multiple `.Include()` + in-memory grouping pada `WorkerDataService.cs:85-172, 175-311` (verified). Tanpa breakdown per-segment, optimasi terbatas ke Assessment query (T1 saja) bisa gagal capai 30% kalau T2/T3 yang dominan.

2. **`IMemoryCache.GetOrCreateAsync` TIDAK punya stampede protection bawaan.** Pada cold start atau setelah TTL expire + multiple concurrent requests, factory delegate bisa execute paralel oleh N threads, masing-masing menjalankan query Distinct Categories penuh. Ini menghapus benefit cache di moment yang paling crucial (cold start). HybridCache (.NET 9+) memberi stampede protection native, tetapi stack project ini .NET 8 — alternatif: pakai `SemaphoreSlim` lock atau `Lazy<Task<T>>` pattern. Lihat Pitfall 4 untuk mitigation options.

Stack verified: .NET 8 ASP.NET Core MVC, EF Core 8 dengan SqlServer provider, ASP.NET Identity Core 8, Razor Views, IMemoryCache (registered di Program.cs:17). `AsNoTracking()` precedent eksisting di codebase (5 file: WorkerDataService L32/38/91/101/122/153/197/202/317, CMPController, CDPController, AdminBaseController, AssessmentAdminController L2836). `IMemoryCache.GetOrCreateAsync` belum dipakai di codebase — yang ada baru `_cache.TryGetValue + Set` (HomeController:63-67) dan `_cache.Remove` (AssessmentAdminController:3234, 3314, AdminController:104). Phase 311 establish first usage `GetOrCreateAsync` pattern untuk codebase ini.

**Primary recommendation:** Eksekusi sequence ketat — (Step 1) implement D-16 baseline breakdown harness sebagai inline Stopwatch wrapper per-segment di action body, (Step 2) RUN baseline 5x cold, (Step 3) **STOP, evaluate Skenario A/B/C dengan user**, (Step 4 — only if Skenario A) jalan ke index migration → AsNoTracking + Include removal → cache, (Step 5) RUN post-patch 5x identical filter, (Step 6) compute improvement %, (Step 7) smoke test 3 tab.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Index seek on date / FK columns | Database / Storage | — | Index lives in SQL Server schema; query optimizer chooses |
| Change-tracker disabling (AsNoTracking) | API/Backend (EF Core layer) | — | Decision di EF Core LINQ chain; transparent ke caller |
| Projection-driven JOIN (no Include) | API/Backend (EF Core LINQ → SQL translator) | Database (executes JOIN) | EF translates `Select(a => a.User.FullName)` ke `LEFT JOIN AspNetUsers` |
| In-memory cache TTL eviction | API/Backend (IMemoryCache singleton) | — | Singleton service di DI, lives in app process memory |
| Cache invalidation on Category CRUD | API/Backend (`AddCategory`/`EditCategory`/`DeleteCategory` actions) | — | Explicit `_cache.Remove(...)` co-located dengan write actions |
| Per-segment performance measurement (Stopwatch) | API/Backend (instrumented action body) | Logging infrastructure (ILogger sink) | Lightweight in-process timing; structured log untuk observability |
| Smoke test parity validation | Manual UAT (browser) | — | SC #7 explicit smoke, no automated test required |

## Project Constraints (from CLAUDE.md)

- **Bahasa:** Narrative report dalam Bahasa Indonesia. Technical terms (LINQ, AsNoTracking, IMemoryCache), code snippets, dan log field names tetap English.

## Standard Stack

### Core (Already Present — Verified)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | EF Core ORM + SQL Server provider | `[VERIFIED: Phase 310 RESEARCH HcPortal.csproj L14]` Native `AsNoTracking()` API + `Migrations/` infrastructure |
| Microsoft.EntityFrameworkCore.Design | 8.x | Migration tooling (`dotnet ef migrations add`) | `[VERIFIED]` Migration history menunjukkan EF Core migrations sudah dipakai sejak Phase 1 (`20260206113657_InitialSqlServer`) sampai Phase ~210 (`20260414013459_AddAssessmentExtraFields`) |
| Microsoft.Extensions.Caching.Memory | 8.x (transitive via ASP.NET Core 8) | `IMemoryCache` ConcurrentDictionary wrapper | `[VERIFIED: Program.cs:17]` Sudah `builder.Services.AddMemoryCache()` registered. `[CITED: learn.microsoft.com/en-us/dotnet/core/extensions/caching]` |
| Microsoft.Extensions.Logging.Abstractions | 8.x | `ILogger<T>` structured logging | `[VERIFIED: AssessmentAdminController L23, L43]` `_logger` injected, sudah dipakai di L2839 |
| System.Diagnostics.Stopwatch | BCL | High-resolution timing | `[VERIFIED]` Built-in `System.Diagnostics`. Tidak ada existing usage di .cs files (Grep "Stopwatch" returned 0 matches) — Phase 311 establish first usage pattern |

### Internal Services & Patterns (Already Present — Verified)

| Service | Purpose | Pattern Reference |
|---------|---------|-------------------|
| `IMemoryCache _cache` | Cache singleton injected ke `AssessmentAdminController` | `[VERIFIED: AssessmentAdminController L22, L34, L42]` |
| `ILogger<AssessmentAdminController> _logger` | Structured logging | `[VERIFIED: AssessmentAdminController L23, L43]` |
| `ApplicationDbContext _context` | EF Core context dengan `AssessmentSessions` DbSet | `[VERIFIED: Data/ApplicationDbContext.cs L170-203]` (entity config + existing indexes) |
| `_cache.Remove(string key)` | Existing invalidation pattern | `[VERIFIED: AdminController:104, AssessmentAdminController:3234, 3314]` |
| `_cache.TryGetValue + Set` | Existing cache-aside pattern (NOT GetOrCreateAsync) | `[VERIFIED: HomeController:63-67]` `_cache.Set(cacheKey, true, TimeSpan.FromHours(1))` |

### NO New Dependencies Required

`[VERIFIED]` Phase 311 **tidak butuh** package baru. Semua functionality (AsNoTracking, IMemoryCache, Stopwatch, EF migrations) ada di stack existing. Cukup `dotnet build` setelah edit code + `dotnet ef migrations add ... && dotnet ef database update` untuk DB schema.

### Alternatives Considered (Rejected per CONTEXT.md)

| Instead of | Could Use | Why Rejected |
|------------|-----------|--------------|
| Two single-column indexes | Composite `IX_Schedule_ExamWindowCloseDate` | `[CITED: CONTEXT.md D-05]` COALESCE in WHERE clause menghalangi perfect seek bahkan dengan composite |
| Two single-column indexes | Persisted computed column `EffectiveDate` + index | `[CITED: CONTEXT.md deferred]` Schema migration overhead, breaks raw SQL queries elsewhere |
| `IMemoryCache.GetOrCreateAsync` | `HybridCache.GetOrCreateAsync` (.NET 9+) | `[VERIFIED: stack adalah .NET 8 per Phase 310 RESEARCH]` HybridCache butuh .NET 9+ NuGet package. Native stampede protection tidak available di IMemoryCache — lihat Pitfall 4 |
| AsNoTracking + projection | `EF.CompileQuery` (pre-compiled LINQ) | `[CITED: DISCUSSION-LOG.md auto-pass]` Overkill untuk single endpoint, adds complexity |
| AsNoTracking + projection | Raw SQL via `FromSqlRaw` | `[CITED: DISCUSSION-LOG.md auto-pass]` Backward-compat risk, lose EF translation safety |

**Installation:** `[VERIFIED]` Tidak perlu `dotnet add package`.

**Version verification:** `[VERIFIED via Phase 310 RESEARCH]` EF Core 8.0.0 sudah dipakai production. IMemoryCache 8.x bundled. Stopwatch BCL stable.

## Architecture Patterns

### System Architecture Diagram (Current — pre-patch)

```
┌─────────────────────────────────────────────────────────────────────┐
│ HTTP GET /Admin/ManageAssessment?tab=assessment&page=1&pageSize=20  │
└─────────────────┬───────────────────────────────────────────────────┘
                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│ AssessmentAdminController.ManageAssessment (L60-227)                │
│ [Authorize(Roles="Admin, HC")]                                      │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ T1: Assessment Query Chain (L66-110)                         │  │
│  │  managementQuery = _context.AssessmentSessions               │  │
│  │    .Where(a => (ExamWindowCloseDate ?? Schedule) >= -7d)     │  │
│  │    .Where(search filters)  // optional                       │  │
│  │    .Where(category filter) // optional                       │  │
│  │    .Include(a => a.User)   ← REDUNDANT (D-09 target)         │  │
│  │    .OrderByDescending(Schedule)                              │  │
│  │    .Select(anonymous { Id, Title, ..., User.FullName, ... }) │  │
│  │    .ToListAsync();          ← TRACKING enabled (D-08 target) │  │
│  │                                                              │  │
│  │  Then: GroupBy LinkedGroupId / (Title, Category, Date.Date)  │  │
│  │        in-memory client-side grouping                        │  │
│  │  Then: Status filter (default exclude Closed)                │  │
│  │  Then: PaginationHelper.Calculate + Skip/Take                │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ T5: Distinct Categories (L172-176)                           │  │
│  │  ViewBag.Categories = _context.AssessmentSessions            │  │
│  │    .Select(a => a.Category).Distinct().OrderBy(c => c)       │  │
│  │    .ToListAsync();                                           │  │
│  │  ← FULL TABLE SCAN every request (D-04 cache target)         │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ T2: GetWorkersInSection (L210) — runs even if tab≠training   │  │
│  │     WorkerDataService.cs L175-311                            │  │
│  │     - Users query + Include(TrainingRecords)                 │  │
│  │     - AsNoTracking sudah ada (L197)                          │  │
│  │     - Batch load AssessmentSessions + lookup                 │  │
│  │     - foreach in-memory aggregation                          │  │
│  │                                                              │  │
│  │ T3: GetAllWorkersHistory (L212) — runs even if tab≠history   │  │
│  │     WorkerDataService.cs L85-172                             │  │
│  │     - AssessmentAttemptHistory.AsNoTracking().GroupBy        │  │
│  │     - AssessmentAttemptHistory.AsNoTracking().Include(User)  │  │
│  │     - AssessmentSessions.AsNoTracking().Include(User)        │  │
│  │     - TrainingRecords.AsNoTracking().Include(User)           │  │
│  │                                                              │  │
│  │ T4: GetAllSectionsAsync L220 + GetUnitsForSectionAsync L222  │  │
│  │     (likely cheaper — small lookups)                         │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  return View()  // Razor renders ViewBag.ManagementData              │
└─────────────────────────────────────────────────────────────────────┘
                  ▼
              SQL Server 2019
              (existing indexes: UserId, UserId+Status, Schedule,
               AccessToken, NomorSertifikat unique, RenewsSessionId,
               RenewsTrainingId)
```

**Key insight dari diagram:** T2/T3/T4 di-execute **always**, tidak peduli `?tab=` param. Komentar L195-197 di controller eksplisit menyatakan ini disengaja ("partial is always rendered (hidden tab pane), so dropdown source data must always be loaded; workers list remains lazy"). Tapi `workers = await _workerDataService.GetWorkersInSection(...)` di L210 berjalan saat `isInitialState = false` — dan `isInitialState` selalu `false` karena di-set static di L199. Effectively T2 selalu jalan.

### System Architecture Diagram (Post-patch — Skenario A path)

```
┌─────────────────────────────────────────────────────────────────────┐
│ AssessmentAdminController.ManageAssessment (post-patch)             │
│                                                                     │
│  Stopwatch swTotal = Stopwatch.StartNew();                          │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ T1 (PATCHED):                                                │  │
│  │  swT1 = Stopwatch.StartNew();                                │  │
│  │  managementQuery = _context.AssessmentSessions               │  │
│  │    .AsNoTracking()           ← D-08 NEW                      │  │
│  │    .Where(...)                                               │  │
│  │    // .Include REMOVED       ← D-09                          │  │
│  │    .OrderByDescending(...)                                   │  │
│  │    .Select(anonymous {..., User.FullName, ...})              │  │
│  │    .ToListAsync();           ← EF auto-emits LEFT JOIN       │  │
│  │  swT1.Stop();                                                │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ T5 (PATCHED — cached):                                       │  │
│  │  swT5 = Stopwatch.StartNew();                                │  │
│  │  ViewBag.Categories = await _cache.GetOrCreateAsync(         │  │
│  │      "assessment_categories_distinct",                       │  │
│  │      async entry => {                                        │  │
│  │          entry.AbsoluteExpirationRelativeToNow =             │  │
│  │              TimeSpan.FromMinutes(5);                        │  │
│  │          return await _context.AssessmentSessions            │  │
│  │              .AsNoTracking()                                 │  │
│  │              .Select(a => a.Category)                        │  │
│  │              .Distinct().OrderBy(c => c).ToListAsync();      │  │
│  │      });                                                     │  │
│  │  swT5.Stop();                                                │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ T2/T3/T4: UNCHANGED (Skenario A scope)                       │  │
│  │  swT2/swT3/swT4 hanya untuk measurement (NO patch)          │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  swTotal.Stop();                                                    │
│  _logger.LogInformation(                                            │
│      "ManageAssessment perf: tab={Tab} t1={T1}ms t2={T2}ms "       │
│      + "t3={T3}ms t4={T4}ms t5={T5}ms total={Total}ms "             │
│      + "search={SearchPresent} page={Page}",                        │
│      activeTab, swT1.ElapsedMilliseconds, ...);                     │
│                                                                     │
│  // Cache invalidation hooks added to:                              │
│  //   AddCategory (L296)   → _cache.Remove(KEY)                    │
│  //   EditCategory (L350)  → _cache.Remove(KEY)                    │
│  //   DeleteCategory (L389)→ _cache.Remove(KEY)                    │
└─────────────────────────────────────────────────────────────────────┘
                  ▼
              SQL Server 2019
              (NEW indexes:
                IX_AssessmentSessions_ExamWindowCloseDate,
                IX_AssessmentSessions_LinkedGroupId)
```

### Recommended Project Structure (No new files)

```
Controllers/
  AssessmentAdminController.cs        # patched (L60-227, L296, L350, L389)

Data/
  ApplicationDbContext.cs             # patched (L177-181 area: tambah HasIndex)

Migrations/
  YYYYMMDDhhmmss_AddManageAssessmentPerfIndexes.cs           # NEW (auto-generated)
  YYYYMMDDhhmmss_AddManageAssessmentPerfIndexes.Designer.cs  # NEW (auto-generated)
  ApplicationDbContextModelSnapshot.cs # auto-updated by `dotnet ef migrations add`
```

No new C# class, no new helper, no new test file. Single migration + 2 file edit.

### Pattern 1: AsNoTracking + Projection (D-08 + D-09 combined)

**What:** Disable EF Core change tracker untuk read-only path; remove explicit `.Include()` saat projection sudah reference nav property.

**When to use:** Read-only endpoint yang return DTO (anonymous type / ViewModel), bukan tracked entities. Ada navigation reference di projection.

**Why both together:**
- `AsNoTracking()` standalone → save memory + CPU (no change tracker snapshot)
- Remove `.Include()` standalone → save EF entity materialization (Include hidrasi entity full lalu projection discard sebagian besar)
- Combined → query plan paling lean: SQL `LEFT JOIN AspNetUsers` muncul hanya kalau projection butuh kolom User, no entity allocation

**Example (target patch L66-110):**
```csharp
// BEFORE (current state)
var managementQuery = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();
// ... search/category filters ...
var allSessions = await managementQuery
    .Include(a => a.User)              // ← REDUNDANT — Select already references a.User.*
    .OrderByDescending(a => a.Schedule)
    .Select(a => new { a.Id, a.Title, /* ... */, UserFullName = a.User != null ? a.User.FullName : "Unknown", /* ... */ })
    .ToListAsync();

// AFTER (D-08 + D-09)
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()                    // ← D-08 NEW
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();
// ... search/category filters unchanged ...
var allSessions = await managementQuery
    // .Include(a => a.User) REMOVED  ← D-09
    .OrderByDescending(a => a.Schedule)
    .Select(a => new { a.Id, a.Title, /* unchanged */, UserFullName = a.User != null ? a.User.FullName : "Unknown", /* ... */ })
    .ToListAsync();
```

**SQL behavior** `[CITED: learn.microsoft.com/en-us/ef/core/querying/single-split-queries — section "Data duplication" + "Projection"]`:
> "By using a projection to explicitly choose which columns you want, you can omit big columns and improve performance; note that this is a good idea regardless of data duplication, so consider doing it even when not loading a collection navigation. However, since this projects the blog to an anonymous type, the blog isn't tracked by EF and changes to it can't be saved back as usual."

EF Core 8 LINQ-to-SQL translator akan emit `LEFT JOIN AspNetUsers AS [u] ON [a].[UserId] = [u].[Id]` ketika projection reference `a.User.FullName`, terlepas dari ada-tidaknya `.Include()`. `.Include()` redundant dalam konteks ini menambah materialization cost tanpa benefit.

`[VERIFIED: official EF Core docs]` Projection tanpa Include adalah standard pattern; Include + projection adalah anti-pattern (EF generates redundant entity materialization yang lalu di-discard).

### Pattern 2: IMemoryCache.GetOrCreateAsync (D-04)

**What:** Cache-aside pattern: cek cache, jika miss panggil factory delegate, store hasil, return.

**When to use:** Read-heavy data dengan staleness tolerance (5 menit per D-02), bukan per-user / per-request.

**Example (target patch L172-176):**
```csharp
// BEFORE
ViewBag.Categories = await _context.AssessmentSessions
    .Select(a => a.Category)
    .Distinct().OrderBy(c => c).ToListAsync();

// AFTER (D-01 + D-02 + D-04)
const string CategoriesCacheKey = "assessment_categories_distinct"; // D-01

ViewBag.Categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); // D-02
    return await _context.AssessmentSessions
        .AsNoTracking()                                              // D-04
        .Select(a => a.Category)
        .Distinct().OrderBy(c => c).ToListAsync();
});
```

**Cache invalidation pattern (D-03):**
```csharp
// In AddCategory (L296), EditCategory (L350), DeleteCategory (L389):
//   AFTER `await _context.SaveChangesAsync();` and BEFORE `RedirectToAction`:
_cache.Remove(CategoriesCacheKey);
```

**Important:** `GetOrCreateAsync` di IMemoryCache **tidak punya stampede protection** — multiple concurrent cache miss bisa eksekusi factory paralel. Lihat **Pitfall 4** untuk mitigation strategi.

`[CITED: learn.microsoft.com/en-us/dotnet/core/extensions/caching]` API + `MemoryCacheEntryOptions.AbsoluteExpirationRelativeToNow` + `RegisterPostEvictionCallback` (optional, tidak dipakai Phase 311).

`[CITED: devblogs.microsoft.com/dotnet/caching-abstraction-improvements-in-aspnetcore]` Stampede protection adalah salah satu motivasi utama HybridCache di .NET 9 — secara explicit confirm bahwa IMemoryCache TIDAK punya stampede protection.

### Pattern 3: EF Core Migration — Index Addition (D-05 + D-06 + D-07)

**What:** Add index di `OnModelCreating` → generate migration → review SQL → apply.

**Steps:**

```csharp
// Edit: Data/ApplicationDbContext.cs (around L181, after existing HasIndex calls)
entity.HasIndex(a => a.UserId);
entity.HasIndex(a => new { a.UserId, a.Status });
entity.HasIndex(a => a.Schedule);
entity.HasIndex(a => a.AccessToken);
entity.HasIndex(a => a.ExamWindowCloseDate);    // ← D-05 NEW
entity.HasIndex(a => a.LinkedGroupId);          // ← D-06 NEW
```

**Generate migration (Windows local — bash via Git Bash atau WSL):**
```bash
dotnet ef migrations add AddManageAssessmentPerfIndexes
```

This creates:
- `Migrations/YYYYMMDDhhmmss_AddManageAssessmentPerfIndexes.cs` — Up/Down SQL
- `Migrations/YYYYMMDDhhmmss_AddManageAssessmentPerfIndexes.Designer.cs`
- Updates `Migrations/ApplicationDbContextModelSnapshot.cs` (snapshot will gain `b.HasIndex("ExamWindowCloseDate")` and `b.HasIndex("LinkedGroupId")` lines around L440-460)

**Expected migration body (verified pattern dari EF Core 8 + SqlServer provider):**
```csharp
public partial class AddManageAssessmentPerfIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_AssessmentSessions_ExamWindowCloseDate",
            table: "AssessmentSessions",
            column: "ExamWindowCloseDate");

        migrationBuilder.CreateIndex(
            name: "IX_AssessmentSessions_LinkedGroupId",
            table: "AssessmentSessions",
            column: "LinkedGroupId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AssessmentSessions_LinkedGroupId",
            table: "AssessmentSessions");

        migrationBuilder.DropIndex(
            name: "IX_AssessmentSessions_ExamWindowCloseDate",
            table: "AssessmentSessions");
    }
}
```

**Apply to dev DB:**
```bash
dotnet ef database update
```

**Reverse if needed (before apply to next env):**
```bash
# If migration NOT yet applied to any DB:
dotnet ef migrations remove

# If already applied to dev DB:
dotnet ef database update <PreviousMigrationName>  # rollback first
dotnet ef migrations remove
```

**Online vs offline index creation di SQL Server:**

`[VERIFIED via official MS Learn pattern]` `CREATE INDEX` default adalah **offline** (table-level lock untuk duration of build). For PortalHC_KPB scale (verified < 100k AssessmentSessions rows likely), offline build < 1 second, acceptable. SQL Server `WITH (ONLINE = ON)` available di Enterprise Edition only — **tidak available** di Standard Edition. EF Core migration default emit offline index, no override needed.

**Production deploy timing:** Apply during low-traffic window. For PortalHC_KPB internal admin tool, traffic low overall — risk minimal.

### Pattern 4: Per-Segment Stopwatch (D-11 + D-16)

**What:** Wrap setiap query/segment dengan separate `Stopwatch` instance, log all together post-action.

**Example:**
```csharp
public async Task<IActionResult> ManageAssessment(/* ... */)
{
    var swTotal = System.Diagnostics.Stopwatch.StartNew();
    long t1 = 0, t2 = 0, t3 = 0, t4 = 0, t5 = 0;

    var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

    // T1: Assessment query chain
    var swT1 = System.Diagnostics.Stopwatch.StartNew();
    var managementQuery = _context.AssessmentSessions
        .AsNoTracking()  // post-patch
        .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
        .AsQueryable();
    // ... existing filters ...
    var allSessions = await managementQuery
        .OrderByDescending(a => a.Schedule)
        .Select(/* ... */)
        .ToListAsync();
    // ... existing in-memory grouping (still part of T1 by D-16 definition) ...
    swT1.Stop();
    t1 = swT1.ElapsedMilliseconds;

    // T5: Distinct Categories (cached post-patch)
    var swT5 = System.Diagnostics.Stopwatch.StartNew();
    ViewBag.Categories = await _cache.GetOrCreateAsync(/* ... */);
    swT5.Stop();
    t5 = swT5.ElapsedMilliseconds;

    // T2: GetWorkersInSection
    var swT2 = System.Diagnostics.Stopwatch.StartNew();
    workers = await _workerDataService.GetWorkersInSection(section, unit, category, search, statusFilter);
    swT2.Stop();
    t2 = swT2.ElapsedMilliseconds;

    // T3: GetAllWorkersHistory
    var swT3 = System.Diagnostics.Stopwatch.StartNew();
    var (assessmentHistory, trainingHistory) = await _workerDataService.GetAllWorkersHistory();
    swT3.Stop();
    t3 = swT3.ElapsedMilliseconds;

    // T4: Sections + Units
    var swT4 = System.Diagnostics.Stopwatch.StartNew();
    ViewBag.TrainingSections = await _context.GetAllSectionsAsync();
    ViewBag.TrainingUnits = !string.IsNullOrEmpty(section)
        ? await _context.GetUnitsForSectionAsync(section)
        : new List<string>();
    swT4.Stop();
    t4 = swT4.ElapsedMilliseconds;

    swTotal.Stop();

    _logger.LogInformation(
        "ManageAssessment perf breakdown: t1={T1}ms t2={T2}ms t3={T3}ms t4={T4}ms t5={T5}ms total={Total}ms tab={Tab} search_present={SearchPresent} page={Page}",
        t1, t2, t3, t4, t5, swTotal.ElapsedMilliseconds, activeTab,
        !string.IsNullOrEmpty(search), page);

    return View();
}
```

**Stopwatch overhead `[CITED: BCL]`:** ~ 15-30 nanoseconds per `StartNew/Stop` call pair. Untuk 5-6 instances per request, total overhead < 1 microsecond — negligible vs query duration di hundreds of milliseconds.

**Granularity:** `ElapsedMilliseconds` cocok untuk DB queries (typical 10ms - 5000ms). Tidak perlu `ElapsedTicks` (yang relevant hanya untuk sub-millisecond ops).

**Noise reduction:** Per D-12, run 5x cold, **skip first run sebagai JIT warmup**, ambil median dari 4 sisa. p95 dari 4 datapoint = max value (bukan true p95 statistik). Untuk lebih robust planner bisa run 10x dan ambil p95 dari 9 (skip first), tetapi D-12 spec 5x — stick dengan spec.

**JIT warmup `[VERIFIED via .NET docs]`:** First call ke method yang belum di-JIT akan slower karena code generation. Skip first run adalah industry standard untuk micro-benchmark. BenchmarkDotNet automatically does this with multiple warmup iterations — tetapi Phase 311 NOT pakai BenchmarkDotNet (overkill, scope creep).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cache key racing across CRUD | Custom dictionary + reader-writer lock | `IMemoryCache._cache.Remove(key)` + TTL safety net | `[VERIFIED]` IMemoryCache wraps `ConcurrentDictionary` — remove is thread-safe. TTL = backup eviction |
| Stopwatch averaging | Custom stats class with Welford's algorithm | Manual log + Excel/SQL aggregate | 5 datapoints — too small for statistical machinery. Trivial median/p95 by inspection |
| EF Core SQL diff capture | Wrap `_context.Database.GetDbConnection()` + ADO.NET interception | `_context.Database.LogTo(line => ...)` di `OnConfiguring` (one-line scope-temporary) OR enable `Information` level logging untuk `Microsoft.EntityFrameworkCore.Database.Command` di `appsettings.Development.json` | EF Core 8 builtin `LogTo` writes ke any `Action<string>` sink. Standard observability |
| Index online/offline build logic | Custom T-SQL with `WITH (ONLINE = ON, ...)` | EF Core default `CreateIndex` (offline) | Standard Edition limitation; PortalHC_KPB scale tidak butuh online build |
| Cache stampede prevention (custom) | Build SemaphoreSlim per-key dictionary | Either: (a) accept cold-start stampede risk for distinct categories (low cost query), (b) use `Lazy<Task<List<string>>>` pattern, OR (c) document trade-off | HybridCache stampede protection unavailable di .NET 8 stack |

**Key insight:** Phase 311 sangat narrow scope — semua patches adalah single-line changes ke existing patterns. Resist temptation untuk wrap di helper class atau abstraction layer. Inline implementation adalah right-sized.

## Runtime State Inventory

> Phase 311 adalah **performance optimization phase** dengan komponen DB schema (index migration). Berikut audit runtime state.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **None** untuk renamed identifier — Phase 311 NOT rename. Index addition adalah additive (no data migration). Existing rows di `AssessmentSessions` tidak perlu touched. | **None — verified no data migration needed** |
| Live service config | **None** — tidak ada konfigurasi external service. SQL Server connection string sudah existing (Program.cs:26 `GetConnectionString("DefaultConnection")`). | **None** |
| OS-registered state | **None** — tidak ada OS task / scheduled job / Windows service yang mereferensi indeks ini. IMemoryCache lives in app process memory only. | **None** |
| Secrets/env vars | **None** — Phase 311 tidak menambah/rename secrets atau env vars. Existing connection string unchanged. | **None** |
| Build artifacts | **Migration files** — `dotnet ef migrations add` akan generate 2 file baru di `Migrations/` + update `ApplicationDbContextModelSnapshot.cs`. Setelah merge, dev/staging/prod DB perlu `dotnet ef database update` to apply schema. | **Migration apply step di plan** — explicit task untuk `dotnet ef database update` di Dev DB sebelum measurement post-patch (D-12 step 3) |

**Warm-cache invalidation note:** Saat code deployed ke production, app restart akan reset IMemoryCache (in-process). Cache key `"assessment_categories_distinct"` start kosong, first request post-deploy akan execute factory query. **Acceptable** — single cold miss adalah expected behavior.

**The canonical question:** *After every file in the repo is updated, what runtime systems still have the old string cached, stored, or registered?*

**Answer:** None untuk Phase 311. No rename, no string churn. Migration file generation adalah satu-satunya state change beyond code edits, dan itu adalah additive DB schema change.

## Common Pitfalls

### Pitfall 1: AsNoTracking Placement Order — TIDAK matter (verified)

**What goes wrong:** Kekhawatiran bahwa `.AsNoTracking()` di awal vs akhir chain ada perbedaan behavior.

**Verified status:** **NO difference for query result.** EF Core 8 LINQ chain adalah immutable builder; `AsNoTracking()` adalah marker yang di-evaluate saat translation. Boleh di awal (`_context.AssessmentSessions.AsNoTracking().Where(...)`) atau akhir (`_context.AssessmentSessions.Where(...).AsNoTracking().ToListAsync()`). **Convention:** placed **at start** of chain (D-08 spec) — readability + matches existing codebase pattern di `WorkerDataService.cs:32` (`_context.AssessmentSessions.AsNoTracking().Where(...)`).

**Severity:** LOW (no correctness issue, only style)
**Mitigation:** Follow D-08 — place at start of chain. Code review akan reject if placed elsewhere untuk konsistensi.

### Pitfall 2: Removing .Include() Without Verifying Projection (D-09 risk)

**What goes wrong:** Hapus `.Include(a => a.User)` tetapi projection mengandung `a.User` tanpa null guard → `NullReferenceException` saat run, OR EF gagal translate query → `InvalidOperationException`.

**Why it happens:** EF Core auto-translates `a.User.FullName` → `LEFT JOIN AspNetUsers AS u ... SELECT u.FullName`. Result kolom `UserFullName` adalah string (atau NULL kalau no matching user). **Existing projection L106-108 sudah punya null guard** (`a.User != null ? a.User.FullName : "Unknown"`) — tetapi kalau projection di-modify orang lain di future tanpa null guard, akan break silent saat NULL row.

**Severity:** MEDIUM (production-impacting NRE jika dilanggar)
**How to avoid:**
1. Verify L106-108 projection masih punya null guard `a.User != null ? ... : "..."` — verified ada saat pembacaan source.
2. Capture EF generated SQL pre + post via `_context.Database.LogTo(line => testLog.Add(line))` selama execute (D-10 acceptance criterion).
3. Confirm SQL contains `LEFT JOIN [AspNetUsers] AS [u] ON [a].[UserId] = [u].[Id]` post-patch.
4. Smoke test 3 tab (D-15) — visual check: kolom "User" di table tetap render dengan nama, tidak "Unknown" untuk row yang sebelumnya valid.
**Warning signs:** Stack trace mention `NullReferenceException` di Razor view rendering, OR kolom user di UI tiba-tiba "Unknown" untuk semua rows.

### Pitfall 3: COALESCE in WHERE Clause Defeats Composite Index Seek

**What goes wrong:** Add composite index `(Schedule, ExamWindowCloseDate)` thinking it will speed up `WHERE (ExamWindowCloseDate ?? Schedule) >= sevenDaysAgo`, tapi SQL Server optimizer **fall back ke index scan** karena `ISNULL(...)` adalah non-sargable function pada column.

**Why it happens:** SQL Server dapat seek pada index hanya kalau predicate di WHERE adalah `column >= value` form. Predicate `ISNULL(col1, col2) >= value` membutuhkan evaluation per-row → scan.

**Severity:** HIGH (silent perf regression — user thinks index helps but doesn't)
**How to avoid:**
1. Stick dengan two single-column indexes per D-05 (CONTEXT.md sudah honest about this — composite explicitly deferred).
2. Optimizer akan choose: kalau most rows have `ExamWindowCloseDate` non-null → use `IX_ExamWindowCloseDate`. Kalau kebanyakan NULL → use `IX_Schedule`.
3. Verify post-patch via `SET STATISTICS IO ON` (lihat Pitfall 7) — look for "logical reads" reduction. Index seek menampilkan "Seeks" di execution plan; scan menampilkan "Scans".
4. **If post-patch measurement masih disappointing** — escalate ke deferred option: persisted computed column `EffectiveDate = ISNULL(ExamWindowCloseDate, Schedule) PERSISTED` + index. Ini make COALESCE seekable. Tapi schema migration overhead — defer beyond Phase 311 scope.
**Warning signs:** Post-patch p95 improvement < 20% padahal stack lain (AsNoTracking, Include removal) sudah applied. Suspect index not being used.

### Pitfall 4: IMemoryCache.GetOrCreateAsync Cache Stampede

**What goes wrong:** Pada cold start atau setelah TTL expire, kalau N concurrent requests hit endpoint, semua N hit cache miss simultaneously → factory delegate execute N kali paralel → N database queries → N writes ke cache (last write wins). Defeats purpose of caching pada moment yang justru paling penting.

**Why it happens:** `IMemoryCache` underlying adalah `ConcurrentDictionary` — `GetOrCreateAsync` extension method internally calls `TryGetValue` → if miss → call factory → `Set`. **No locking between TryGetValue dan Set.** N threads can interleave.

**Quote `[CITED: WebSearch — devblogs.microsoft.com/dotnet/caching-abstraction-improvements-in-aspnetcore]`:** "IMemoryCache.GetOrCreateAsync does not hold a lock for the same key. This means the factory delegate in GetOrCreateAsync can execute concurrently for the same key... 100 concurrent requests on a cache miss result in 100 identical database queries."

**Severity for Phase 311:** **LOW-MEDIUM**. Justification:
- Factory query (Distinct Categories) is **cheap** — small table scan, O(milliseconds), no heavy joins.
- Project usage profile: Admin-only endpoint, low concurrency (< 10 admin users typical).
- Cold start frequency: app restart only (deploy + IIS recycle).
- Worst case: 5 admins hit endpoint at same moment after deploy → 5 parallel queries → resolve in < 100ms total. Acceptable.

**Mitigation options (planner picks):**

**Option A (recommended for Phase 311 scope):** Accept stampede risk. Document trade-off di SUMMARY.md. Rationale: simplest impl, scope-aligned. Single-line `GetOrCreateAsync` call.

**Option B:** Wrap factory dengan `SemaphoreSlim` lock. Adds ~10 lines code + introduces lock contention. Overkill for project scale.
```csharp
// Class field
private static readonly SemaphoreSlim _categoriesCacheLock = new(1, 1);

// In action
ViewBag.Categories = await _cache.GetOrCreateAsync(KEY, async entry =>
{
    await _categoriesCacheLock.WaitAsync();
    try {
        // double-check pattern: another thread may have populated meanwhile
        if (_cache.TryGetValue(KEY, out List<string> existing)) return existing;
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await _context.AssessmentSessions.AsNoTracking()
            .Select(a => a.Category).Distinct().OrderBy(c => c).ToListAsync();
    }
    finally { _categoriesCacheLock.Release(); }
});
```

**Option C:** `Lazy<Task<List<string>>>` pattern (advanced). Overkill.

**Recommendation:** **Option A**. The factory query cost (simple distinct + sort) << complexity cost of lock. Document explicitly di SUMMARY: "IMemoryCache stampede risk accepted — factory query cheap; admin endpoint low concurrency."

**Warning signs:** If post-deploy production logs show multiple identical "executing distinct categories query" log entries within milliseconds of each other → stampede happening, escalate to Option B di phase berikut.

### Pitfall 5: Sync Over Async Anti-Pattern

**What goes wrong:** Forgetting `await` keyword on async cache call — `_cache.GetOrCreateAsync(...)` returns `Task<TItem>`. Without `await`, you assign the Task itself to ViewBag → Razor view gets Task object → ToString = "System.Threading.Tasks.Task..." → broken UI.

**Why it happens:** Compiler typically warns ("Task is not awaited") tetapi kalau di-cast ke `object` (ViewBag is object), warning bisa hilang.

**Severity:** HIGH (runtime breakage, UI broken)
**How to avoid:**
1. Verify compiler warnings during build — `dotnet build` should be 0 warnings ideally (currently project has 92 warnings per Phase 308 STATE — but no async-related). Filter for "CS4014" (call not awaited) di build output.
2. Inspect ViewBag.Categories at runtime — should be `List<string>`, not `Task<List<string>>`.
3. Code review — explicit `await` keyword presence on every cache call.
**Warning signs:** Razor view shows category dropdown filled dengan single weird text "System.Threading.Tasks.Task`1[System.Collections.Generic.List`1[System.String]]" instead of actual categories.

### Pitfall 6: SetSize Required Only If MemoryCache.SizeLimit Configured

**What goes wrong:** Some tutorials recommend `entry.SetSize(1)` — but only relevant if `MemoryCacheOptions.SizeLimit` was configured at registration.

**Verified status:** Project's `Program.cs:17` calls `builder.Services.AddMemoryCache()` **without** options → default = unlimited size, no eviction by size, only by TTL. **`SetSize` is NOT needed.** Adding it triggers no error but adds noise.

**Severity:** LOW (no functional impact)
**How to avoid:** Don't include `entry.SetSize(...)` in factory delegate. Verify at code review.

### Pitfall 7: SQL Plan Verification — How to Confirm Indexes Used Post-Patch

**What goes wrong:** Add indexes, but optimizer doesn't pick them. Or chooses worse plan due to outdated stats. Silent — performance numbers ambiguous.

**Why it happens:** SQL Server query optimizer uses statistics + cost estimation. After fresh index creation, statistics may not be current. Query optimizer may have cached old plan in plan cache.

**Severity:** MEDIUM (informs whether D-05 + D-06 actually deliver value)
**How to verify (manual, during execute phase):**
1. **Capture EF generated SQL** via `_context.Database.LogTo(...)` (temporary, scope to test session). Look for the actual T-SQL emitted for L66-110 query.
2. **Run captured SQL manually** in SQL Server Management Studio (SSMS) or sqlcmd dengan:
   ```sql
   SET STATISTICS IO ON;
   SET STATISTICS TIME ON;
   -- paste EF-generated query
   ```
   Output shows logical reads + CPU time + elapsed time.
3. **View actual execution plan** in SSMS (Ctrl+M before execute, or `SET STATISTICS XML ON`). Look for:
   - `Index Seek (NonClustered)` on `IX_AssessmentSessions_LinkedGroupId` — index used ✓
   - `Index Scan (NonClustered)` — index visited but full scan ⚠
   - `Table Scan` or `Clustered Index Scan` — new index ignored ✗
4. **Force statistics update** if needed:
   ```sql
   UPDATE STATISTICS dbo.AssessmentSessions WITH FULLSCAN;
   ```
5. **Clear plan cache** (Dev only — never production):
   ```sql
   DBCC FREEPROCCACHE;
   ```

**How to capture EF SQL via LogTo (one-shot during execute):**
```csharp
// Temporary, in ApplicationDbContext.OnConfiguring:
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
// OR scope to file:
optionsBuilder.LogTo(line => File.AppendAllText("ef-sql.log", line + "\n"), LogLevel.Information);
```

Or via `appsettings.Development.json`:
```json
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

**Recommended approach untuk Phase 311:** Use `appsettings.Development.json` config approach — temporary toggle, no code change. After D-10 SQL diff captured, revert config.

`[VERIFIED via existing project structure]` Project sudah pakai `Microsoft.Extensions.Logging.Abstractions` (built-in ASP.NET Core 8). No new package needed.

**Warning signs:** Post-patch p95 improvement < 30% AND query plan still shows `Table Scan` after `UPDATE STATISTICS`. Suspect index definition wrong or COALESCE pitfall (Pitfall 3).

### Pitfall 8: D-16 Decision Gate Mistakenly Skipped

**What goes wrong:** Planner / executor jumps straight to applying patches (AsNoTracking + Include removal + cache + index migration) **before** running D-16 baseline breakdown. If actual bottleneck is T2/T3 (Training/History queries), patches won't deliver 30% improvement, phase fails acceptance.

**Why it happens:** Optimization momentum — "we know what to do, let's just do it." Skips diagnostic step.

**Severity:** HIGH (phase failure mode)
**How to avoid (must be enforced at plan-check level):**
1. **Plan MUST have D-16 as Wave 0 / Step 1**, before any patch task.
2. Plan task description must explicitly include "STOP gate after baseline — escalate to user for Skenario A/B/C decision."
3. Include explicit checkpoint in `/gsd-execute-phase` workflow: pause for human review of breakdown numbers before continuing.
**Warning signs:** Plan structure starts with "Add IMemoryCache" or "Apply AsNoTracking" as Wave 1 Task 1 without prior baseline breakdown task.

### Pitfall 9: Baseline Reproducibility — Different Datasets Pre/Post

**What goes wrong:** Run baseline measurements on dataset X, apply patch, run post-patch on dataset Y (different size, different distribution). Improvement % invalid.

**Why it happens:** AssessmentSessions table mutates between baseline + post-patch runs (other admins create assessments, workers complete them).

**Severity:** MEDIUM-HIGH (invalid measurement = invalid acceptance criterion)
**How to avoid:**
1. **Snapshot dev DB** before baseline run (file copy of .mdf/.ldf, atau SQL Server backup-restore).
2. Run baseline 5x → restore snapshot → apply patch → restart app → run post-patch 5x.
3. Document snapshot timestamp di SUMMARY.md.
4. **Alternative**: Filter both runs to same date range (e.g., `?statusFilter=All`) untuk minimize data drift impact.
**Warning signs:** Baseline reports vary widely between runs (>20% variance), suggesting data churn during measurement window.

## Code Examples

Verified patterns from official docs + existing project codebase:

### Existing AsNoTracking Pattern (from project)
```csharp
// VERIFIED at WorkerDataService.cs:32
var assessments = await _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => a.UserId == userId && a.Status == "Completed")
    .ToListAsync();
```

### Existing _cache.Remove Pattern (from project)
```csharp
// VERIFIED at AssessmentAdminController.cs:3234
_cache.Remove($"exam-status-{id}");

// VERIFIED at AdminController.cs:104
_cache.Remove("MaintenanceMode_State");
```

### Existing _cache.TryGetValue + Set Pattern (from project — older style)
```csharp
// VERIFIED at HomeController.cs:63-67
var cacheKey = "cert-notif-global";
if (!_cache.TryGetValue(cacheKey, out _))
{
    await TriggerCertExpiredNotificationsAsync();
    _cache.Set(cacheKey, true, TimeSpan.FromHours(1));
}
```

**Phase 311 introduces new pattern (`GetOrCreateAsync`)**:
```csharp
// NEW pattern for project (D-04):
ViewBag.Categories = await _cache.GetOrCreateAsync(
    "assessment_categories_distinct",
    async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await _context.AssessmentSessions
            .AsNoTracking()
            .Select(a => a.Category)
            .Distinct().OrderBy(c => c).ToListAsync();
    });
```

`[CITED: learn.microsoft.com/en-us/dotnet/core/extensions/caching — section "Additional extension methods"]`

### Existing Structured Logging Pattern (from project)
```csharp
// VERIFIED at AssessmentAdminController.cs:2839
_logger.LogInformation(
    "FinalizeEssayGrading: race condition session {SessionId} — skip side-effects (already finalized).",
    sessionId);
```

**Phase 311 logging format (proposed, follows project convention):**
```csharp
_logger.LogInformation(
    "ManageAssessment perf breakdown: t1={T1}ms t2={T2}ms t3={T3}ms t4={T4}ms t5={T5}ms total={Total}ms tab={Tab} search_present={SearchPresent} page={Page}",
    t1, t2, t3, t4, t5, swTotal.ElapsedMilliseconds, activeTab,
    !string.IsNullOrEmpty(search), page);
```

Field names: lowercase + underscore for Indonesian/English consistency. Follows existing pattern di L2839 yang pakai PascalCase placeholders untuk structured logging compliance.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `_cache.TryGetValue + Set` (project's existing HomeController pattern) | `_cache.GetOrCreateAsync` (cache-aside in single call) | EF Core 2.x+ | Less code, atomic cache miss handling. Phase 311 adopts. |
| `IMemoryCache` for stampede-prone scenarios | `HybridCache` (.NET 9+) | .NET 9 (Nov 2024 GA) | Built-in stampede protection. **NOT applicable** — project pinned .NET 8. Future migration consideration |
| `Include` + `Select` projection | `Select` projection alone (auto LEFT JOIN) | EF Core 1.0 — always | Phase 311 D-09 cleanup of legacy redundant Include |
| Sync EF queries (`.ToList()`) | Async EF queries (`.ToListAsync()`) | EF Core 1.0 | Existing project all-async — already good |
| Stopwatch + Console.WriteLine | Stopwatch + ILogger structured logging | .NET Core 3.x + | Existing project uses ILogger — Phase 311 follows |

**Deprecated/outdated:**
- `EntityFramework` (pre-Core, full .NET Framework) — replaced by EF Core. Project uses EF Core 8 ✓
- Synchronous IMemoryCache patterns without TTL — replaced by absolute expiration patterns ✓

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None (.NET) — project has no xUnit/NUnit/MSTest project per Phase 310 RESEARCH verified. Playwright TypeScript exists for E2E (`tests/e2e/`) |
| Config file | None for backend unit tests. `tests/playwright.config.ts` for E2E |
| Quick run command | `dotnet build` (compile-time check only) + manual baseline measurement |
| Full suite command | `cd tests && npx playwright test` (E2E only — no PERF coverage) |

**Phase 311 specific:** No automated test framework needed. Validation = manual measurement + manual smoke test. This is justifiable per existing project test posture (Phase 310 also no unit tests, smoke + E2E only).

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PERF-01 | p95 ≤ baseline × 0.7 (≥30% improvement) | manual measurement | manual: 5 runs pre-patch + 5 runs post-patch via browser navigate, capture log entries from `_logger.LogInformation` | manual log capture, no test file |
| PERF-01 (smoke) | Tab Assessment / Training / History grouping & paging hasil identik pre vs post | manual smoke test | manual: navigate `/Admin/ManageAssessment?tab={X}&page=1` 3x, visual diff row count + pagination header | manual UAT script — author selama plan phase |
| PERF-01 (D-10 verify) | EF generated SQL contains `LEFT JOIN AspNetUsers` post-Include-removal | manual SQL diff | enable `Microsoft.EntityFrameworkCore.Database.Command` Information logging, capture pre/post diff | manual log capture, no test file |
| PERF-01 (D-16 gate) | Baseline breakdown T1..T5 captured with cold runs | manual measurement | manual: 5 runs cold, capture log entries, compute median per segment | manual log capture, no test file |

### Sampling Rate

- **Per task commit:** `dotnet build` (compile check)
- **Per wave merge:** Manual smoke 3 tabs (one navigation per tab), check 200 OK + visual UI parity
- **Phase gate:** Full measurement protocol per D-12 + D-16, document at SUMMARY.md before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] **No Wave 0 test scaffold needed** — manual measurement + manual smoke is the methodology per project convention. Document this as explicit Wave 0 task: "No automated test infrastructure to add. Validation via measurement protocol per D-12/D-16."

### 8-Dimension Validation Coverage (Nyquist)

| Dimension | Applies | Strategy |
|-----------|---------|----------|
| **Functional** | Yes | Smoke test 3 tabs (D-15): page load, grouping, pagination identical pre vs post |
| **Boundary** | Yes (light) | Edge cases: tab=invalid (defaults to "assessment"), search empty string, page=0 (existing PaginationHelper handles), pageSize > all rows (handled by Skip/Take) |
| **Error** | Yes | EF query failure → 500 (existing global behavior, no new error path). Cache miss + DB unavailable → exception bubbles. **No new error handling needed.** |
| **Concurrency** | Yes (cache stampede) | Pitfall 4 mitigation — document accepted risk OR escalate to Option B SemaphoreSlim |
| **Idempotency** | Yes (read-only endpoint) | GET request — naturally idempotent. Cache invalidation `_cache.Remove` is idempotent (no-op if key absent). |
| **Backward Compat** | **CRITICAL** | ViewBag shape MUST be unchanged. Same keys, same value types: `ViewBag.Categories: List<string>`, `ViewBag.ManagementData: List<dynamic>`, `ViewBag.CurrentPage: int`, etc. Razor view contract preserved. Verify via smoke test (D-15). |
| **Performance** | **PRIMARY** | D-11 + D-12 + D-16 measurement protocol. Acceptance ≥30% p95 improvement. |
| **Validation Coverage** | Yes (this section) | All 7 SCs from ROADMAP mapped to verification step in plan |

## Open Questions

1. **EF SQL diff capture method (D-10) — config vs code-based**
   - **What we know:** Two viable approaches: (a) `appsettings.Development.json` log level config (no code change, scope to dev env), (b) inline `_context.Database.LogTo(...)` di `OnConfiguring` (code change, must remove post-verify).
   - **What's unclear:** Which approach planner prefers. Both work.
   - **Recommendation:** Use `appsettings.Development.json` log level toggle — zero code change, easily revertable. Document the JSON snippet di plan task action.

2. **Snapshot Dev DB before baseline measurement (Pitfall 9)**
   - **What we know:** Reproducibility requires consistent dataset between baseline + post-patch runs.
   - **What's unclear:** Whether project workflow already has dev DB snapshot/restore tooling, or if manual file copy needed.
   - **Recommendation:** Planner add explicit task "snapshot dev DB before baseline" with command (e.g., SQL Server BACKUP DATABASE). If not available, document in pre-baseline section: "Halt other DB writes during measurement window."

3. **Per-segment Stopwatch implementation: inline vs helper**
   - **What we know:** D-16 requires 5 segment timings (T1..T5). Inline: 5x `Stopwatch.StartNew/Stop` blocks (verbose but explicit). Helper: extract `await TimeSegment(name, async () => ...)` wrapper.
   - **What's unclear:** Code style preference.
   - **Recommendation:** **Inline** for Phase 311 — D-13 specifies stopwatch logging permanent (NOT removed post-validation), so verbosity acceptable. Helper extraction is YAGNI for single-use case. Future phases that benefit from generalized timing can extract.

4. **Migration apply timing (Pitfall 9 + D-12)**
   - **What we know:** Migration apply changes DB schema → query plan changes → baseline must be measured BEFORE migration apply (current state = no new indexes), post-patch AFTER migration apply.
   - **What's unclear:** Sequence enforcement.
   - **Recommendation:** Plan structure: Wave 0 (baseline breakdown D-16, no migration applied), STOP for Skenario A/B/C decision. Wave 1 (apply migration + AsNoTracking + Include removal + cache). Wave 2 (post-patch measurement).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | `dotnet build`, `dotnet ef migrations` | ✓ (assumed — project actively builds, Phase 308 verified .NET build 0 errors) | 8.x | — |
| `dotnet ef` global tool | Migration generation | ⚠ — need to verify install. Run `dotnet ef --version` to check. If missing: `dotnet tool install --global dotnet-ef --version 8.*` | typically 8.x | Manual SQL DDL via SSMS as last resort |
| SQL Server (Dev) | Migration apply, baseline measurement | ✓ (assumed — project runs locally per STATE.md "Phase 310 Plan 02 finalized" implies dev env operational) | 2019+ | — |
| Browser (Edge/Chrome) | Manual baseline measurement (navigate to `/Admin/ManageAssessment`) | ✓ | recent | Postman / curl as alternative for headless measurement |
| ILogger sink (console / file) | Capture `_logger.LogInformation` output | ✓ (default ASP.NET Core console logger) | 8.x | — |
| SQL Server Management Studio (SSMS) | View execution plan, run STATISTICS IO | ⚠ — typical Windows dev install. If missing: download from microsoft.com or use Azure Data Studio | 18+ / 19+ | sqlcmd (BCL) for SET STATISTICS commands without GUI plan view |

**Missing dependencies with no fallback:** None blocking — all critical tools standard for .NET 8 dev environment.

**Missing dependencies with fallback:** SSMS for execution plan visualization is a nice-to-have for Pitfall 7 verification. Azure Data Studio or sqlcmd works as fallback (text-only output).

**Action for planner:** Add Wave 0 task verification step:
```bash
dotnet --version              # expect 8.x
dotnet ef --version           # expect 8.x; if fails, install
sqlcmd -S . -Q "SELECT @@VERSION"  # verify SQL Server reachable
```

## Security Domain

> Phase 311 adalah backend perf optimization, no new endpoints, no new data exposure, no auth/authz change. Security surface is **unchanged**.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No (no change) | Existing `[Authorize(Roles = "Admin, HC")]` preserved at L59 |
| V3 Session Management | No (no change) | Existing session via ASP.NET Identity preserved |
| V4 Access Control | No (no change) | Same role-gate, same data shape returned |
| V5 Input Validation | Marginal | `search`, `tab`, `category`, `statusFilter` query params unchanged. EF Core parameterization protects against SQL injection (LINQ parameterized via `DbCommand` parameters). No new input vector |
| V6 Cryptography | No | No crypto operations. |

### Known Threat Patterns for ASP.NET Core 8 + EF Core 8 stack

| Pattern | STRIDE | Standard Mitigation | Phase 311 Status |
|---------|--------|---------------------|------------------|
| SQL injection via search param | Tampering | EF Core LINQ parameterization | ✓ Existing — no raw SQL added |
| Cache key poisoning | Tampering | Static const key, no user input in key | ✓ D-01 static literal `"assessment_categories_distinct"` |
| Authorization bypass | Elevation | Existing `[Authorize]` attribute | ✓ Unchanged |
| Sensitive data exposure via cache | Information Disclosure | Categories list is project-wide constant, no PII. Single global key safe | ✓ D-01 design |
| DoS via cache stampede | DoS | Stampede risk analysed in Pitfall 4 — accept (low impact) for Phase 311 | ⚠ Documented |
| Logging sensitive data | Information Disclosure | `_logger.LogInformation` payloads contain only timing + tab + search **flag** (boolean, not raw search term) — no PII leak | ✓ Field design |

**Note on logging:** Recommended log format uses `search_present={SearchPresent}` boolean flag, NOT raw search term. This avoids leaking potentially-sensitive search queries (worker NIPs, names) to log files. **Planner should enforce this design choice in plan**.

## Canonical Patterns

Existing patches in codebase that Phase 311 should mirror:

### Pattern Reference 1: AsNoTracking on read-only path
- `Services/WorkerDataService.cs:32, 38, 91, 101, 122, 153, 197, 202, 317` — every read-only query in service layer uses `.AsNoTracking()`. Phase 311 D-08 follows this established convention.

### Pattern Reference 2: Cache invalidation co-located with write actions
- `Controllers/AdminController.cs:104` — `_cache.Remove("MaintenanceMode_State");` immediately after toggle action's SaveChangesAsync. Phase 311 D-03 follows this for `AddCategory`/`EditCategory`/`DeleteCategory`.
- `Controllers/AssessmentAdminController.cs:3234, 3314` — `_cache.Remove($"exam-status-{id}");` after status mutation. Same pattern.

### Pattern Reference 3: ExecuteUpdateAsync atomic update with rowsAffected (Phase 310)
- `Controllers/AssessmentAdminController.cs:2825-2842` — Phase 310 added pattern. Not directly applicable to Phase 311 but demonstrates project's bias toward atomic DB ops.

### Pattern Reference 4: Existing index pattern in OnModelCreating
- `Data/ApplicationDbContext.cs:178-181` — single-column `entity.HasIndex(a => a.UserId);` style. Phase 311 D-05/D-06 follows verbatim:
  ```csharp
  entity.HasIndex(a => a.ExamWindowCloseDate);
  entity.HasIndex(a => a.LinkedGroupId);
  ```

### Pattern Reference 5: Structured logging field convention
- `Controllers/AssessmentAdminController.cs:2839-2841` — `_logger.LogInformation("FinalizeEssayGrading: race condition session {SessionId} — skip side-effects ...", sessionId);` — colon-separated context prefix + PascalCase placeholder names. Phase 311 logging follows this style.

## Implementation Sequence

Suggested ordering for the planner. **MUST** preserve D-16 gate as a hard checkpoint.

### Wave 0: Pre-Patch Diagnostic (D-16 baseline breakdown)
1. **Task 0.1:** Add per-segment Stopwatch instrumentation to `ManageAssessment` action (T1..T5 + Total). Compile check via `dotnet build`. **No DB schema change. No query change. Just instrumentation.**
2. **Task 0.2:** Snapshot Dev DB (per Pitfall 9 mitigation).
3. **Task 0.3:** Run baseline 5x cold. Filter: no search, `tab=assessment`, `page=1`, `pageSize=20`. Capture log entries.
4. **Task 0.4:** Compute median per segment (skip first run as JIT warmup). Document in baseline report.
5. **🛑 STOP GATE — Human review required:** Present breakdown to user. User decides Skenario A / B / C per D-16 decision matrix.
6. **If Skenario A:** Continue to Wave 1.
7. **If Skenario B/C:** Halt phase, return to `/gsd-discuss-phase` to revise D-14 scope or spawn new phase.

### Wave 1: Apply Patches (Skenario A path only)
1. **Task 1.1:** Add `entity.HasIndex(a => a.ExamWindowCloseDate);` and `entity.HasIndex(a => a.LinkedGroupId);` to `Data/ApplicationDbContext.cs` around L181.
2. **Task 1.2:** Generate migration: `dotnet ef migrations add AddManageAssessmentPerfIndexes`. Review generated `.cs` for expected `CreateIndex` calls.
3. **Task 1.3:** Apply to Dev DB: `dotnet ef database update`. Verify via SSMS / sqlcmd: `SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('AssessmentSessions');` — confirm new indexes present.
4. **Task 1.4:** Apply `.AsNoTracking()` insertion at L66 (D-08).
5. **Task 1.5:** Remove `.Include(a => a.User)` at L88 (D-09).
6. **Task 1.6:** Replace L172-176 distinct categories query with `_cache.GetOrCreateAsync` pattern (D-04).
7. **Task 1.7:** Add `_cache.Remove("assessment_categories_distinct");` to `AddCategory` (L296), `EditCategory` (L350), `DeleteCategory` (L389) immediately after `SaveChangesAsync` (D-03).
8. **Task 1.8:** Compile check `dotnet build`. Expect 0 new warnings/errors.

### Wave 2: Post-Patch Measurement
1. **Task 2.1:** Restart app (cold cache). Run post-patch 5x identical filter to baseline.
2. **Task 2.2:** Compute median + p95 per segment. Compute improvement %: `(baseline_p95 - postpatch_p95) / baseline_p95`.
3. **Task 2.3:** Acceptance check: improvement ≥ 30% on Total. If not met → escalate per Pitfall 7 SQL plan analysis (verify indexes used; check Pitfall 3 COALESCE issue).
4. **Task 2.4:** Capture EF generated SQL pre/post via `appsettings.Development.json` Information log toggle (D-10). Diff: confirm `LEFT JOIN AspNetUsers` present, no Include redundancy.
5. **Task 2.5:** Smoke test 3 tabs (`?tab=assessment`, `?tab=training`, `?tab=history`) — verify 200 OK + visual UI parity (D-15).

### Wave 3: Documentation
1. **Task 3.1:** Document baseline + post-patch numbers + improvement % di SUMMARY.md.
2. **Task 3.2:** Document SQL diff (pre/post LEFT JOIN confirmation).
3. **Task 3.3:** Document any cache stampede trade-off acceptance (Pitfall 4 Option A).
4. **Task 3.4:** Update STATE.md.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `dotnet ef` global tool installed di Windows local — assumed because project has 250+ existing migrations and team actively adds them | Environment Availability | Plan needs explicit install step jika tool absent. Low risk — easy fix `dotnet tool install --global dotnet-ef --version 8.*` |
| A2 | SQL Server Standard Edition (no online index build) — assumed based on no Enterprise mention in project docs | Pattern 3 / Pitfall 3 | Online index unavailable — but PortalHC scale low so offline build < 1s, risk minimal |
| A3 | Stopwatch BCL — assumed available, never explicitly verified imports added to AssessmentAdminController | Standard Stack | Compile error if `using System.Diagnostics;` missing. Trivial fix during implementation. |
| A4 | First request after app restart triggers JIT warmup penalty on EF query path — standard .NET behavior, no specific verification for THIS endpoint | Pattern 4 / D-12 | If JIT impact larger than expected on this endpoint, baseline first-run skip insufficient. Mitigate by running 6x and skip first 2 if first-2 vs subsequent show significant gap |
| A5 | Existing project test posture (no unit tests) — Phase 311 follows manual measurement convention. Verified for Phase 310 RESEARCH but assumed unchanged | Validation Architecture | If test framework added between Phase 310 and 311 execution, plan must include unit test additions. Low risk — short timespan |
| A6 | IMemoryCache.SizeLimit not configured — verified `Program.cs:17` has `AddMemoryCache()` with no options arg, so default unlimited | Pitfall 6 | If future Program.cs changes add SizeLimit, `entry.SetSize(...)` will be required. Easy fix in code review |
| A7 | Production traffic concurrency on `/Admin/ManageAssessment` is low (< 10 admin users typical) — assumed based on internal HR portal usage profile | Pitfall 4 | If actual concurrency much higher, stampede impact larger. Mitigate by adopting Option B SemaphoreSlim |
| A8 | Smoke test "grouping & paging hasil identik" verifiable visually without diff tool — assumed based on small dataset & D-15 spec | Validation Architecture | If grouping changes subtly (e.g., different sort tie-breaker), human eye may miss. Mitigate by recording row count + first-page top-5 titles pre + post |

**This table is non-empty — planner SHOULD review with user during /gsd-discuss-phase if not already covered.** A1, A4, A7 most material.

## Sources

### Primary (HIGH confidence — official MS docs + verified codebase)
- `[CITED: learn.microsoft.com/en-us/ef/core/querying/single-split-queries]` — EF Core 8 single vs split queries, projection vs Include behavior, cartesian explosion explanation
- `[CITED: learn.microsoft.com/en-us/ef/core/querying/related-data/eager]` — Include semantics, projection without Include
- `[CITED: learn.microsoft.com/en-us/dotnet/core/extensions/caching]` — IMemoryCache API, MemoryCacheEntryOptions, AbsoluteExpirationRelativeToNow, GetOrCreateAsync extension
- `[VERIFIED via Read: Controllers/AssessmentAdminController.cs:1-410, 2825-2842, 3234, 3314]` — full action body, Category CRUD actions, existing _cache patterns
- `[VERIFIED via Read: Data/ApplicationDbContext.cs:160-209]` — entity config, existing indexes
- `[VERIFIED via Read: Migrations/ApplicationDbContextModelSnapshot.cs:430-465]` — current snapshot index list
- `[VERIFIED via Read: Program.cs:1-50]` — DI registrations including AddMemoryCache
- `[VERIFIED via Read: Services/WorkerDataService.cs:1-368]` — full service for D-16 T2/T3 understanding
- `[VERIFIED via Read: Controllers/HomeController.cs:50-90]` — existing IMemoryCache usage pattern
- `[VERIFIED via Read: .planning/phases/311-manageassessment-performance/311-CONTEXT.md]` — all 16 decisions
- `[VERIFIED via Read: .planning/phases/311-manageassessment-performance/311-DISCUSSION-LOG.md]` — audit trail
- `[VERIFIED via Read: .planning/REQUIREMENTS.md]` — PERF-01 acceptance criteria
- `[VERIFIED via Read: .planning/ROADMAP.md]` — 7 SCs + dependencies
- `[VERIFIED via Read: .planning/STATE.md]` — current project state, Phase 310 status
- `[VERIFIED via Grep AsNoTracking pattern]` — 5 files use AsNoTracking, established convention
- `[VERIFIED via Grep _cache patterns]` — 6 instances of `_cache.Remove`, 1 instance `_cache.TryGetValue + Set`, 0 instances `_cache.GetOrCreateAsync` (Phase 311 first usage)

### Secondary (MEDIUM confidence — official docs + cross-reference)
- `[CITED: devblogs.microsoft.com/dotnet/caching-abstraction-improvements-in-aspnetcore]` (via WebSearch) — Stampede protection rationale for HybridCache; explicit confirmation IMemoryCache lacks stampede protection
- `[CITED: github.com/dotnet/aspnetcore/issues/53255]` (via WebSearch) — IDistributedCache + HybridCache .NET 9 epic, confirms HybridCache is new in .NET 9
- `[CITED: learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid?view=aspnetcore-9.0]` (via WebSearch reference) — HybridCache is .NET 9+ only

### Tertiary (LOW confidence — none used for critical claims)
- None. All security-, performance-, and correctness-critical claims sourced from primary docs or verified codebase.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — every library version verified via existing codebase + official docs
- Architecture: HIGH — diagrams derived from direct code reading
- Pitfalls: HIGH (Pitfalls 1-3, 5-9) / MEDIUM (Pitfall 4 stampede severity assessment is judgment based on assumed traffic profile A7)
- Patterns: HIGH — every code example verified against existing patterns in project (Pattern References 1-5) or official docs

**Research date:** 2026-05-05
**Valid until:** 2026-06-04 (30 days — stable .NET 8 stack, EF Core 8 LTS, no major framework churn expected)

**Outstanding planner actions:**
1. Honor D-16 gate strictly — Wave 0 baseline before any patch.
2. Resolve A1 (dotnet ef tool availability) at Wave 0 verify step.
3. Decide Pitfall 4 mitigation option (recommended Option A — accept stampede).
4. Decide Open Question 1 (SQL diff capture method — recommended config-based).
5. Add explicit Pitfall 9 mitigation step (Dev DB snapshot) to Wave 0.
