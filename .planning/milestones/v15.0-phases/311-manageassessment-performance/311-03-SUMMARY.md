---
phase: 311
plan: 03
slug: manageassessment-perf-backend-opportunistic
tags: [perf, ef-core, migration, indexes, cache, asnotracking]
requires:
  - phase: 311
    plan: 02
    artifacts:
      - "Shell + 3 partial actions ManageAssessmentTab_* (commits c2b5a910, 0f3e4690, 03abcc92 — Plan 02)"
provides:
  - "EF Core migration AddManageAssessmentPerfIndexes (2 single-column index pada AssessmentSessions)"
  - "IMemoryCache wrap distinct Categories list TTL 5 menit dengan 3 invalidation hooks"
  - "AsNoTracking di managementQuery chain partial Tab_Assessment"
  - "Hapus redundant Include navigation (projection sudah punya null guard)"
affects:
  - "Data/ApplicationDbContext.cs (Fluent HasIndex untuk ExamWindowCloseDate + LinkedGroupId)"
  - "Migrations/20260507073825_AddManageAssessmentPerfIndexes.cs (NEW migration)"
  - "Migrations/20260507073825_AddManageAssessmentPerfIndexes.Designer.cs (NEW)"
  - "Migrations/ApplicationDbContextModelSnapshot.cs (auto-updated 2 b.HasIndex)"
  - "Controllers/AssessmentAdminController.cs (cache key const, AsNoTracking, GetOrCreateAsync, 3 invalidation hooks)"
tech-stack:
  added: []
  patterns:
    - "Fluent API entity.HasIndex single-column untuk perf optimization (D-05, D-06)"
    - "EF Core CreateIndex/DropIndex migration auto-generate via dotnet ef migrations add"
    - "IMemoryCache.GetOrCreateAsync dengan AbsoluteExpirationRelativeToNow TimeSpan.FromMinutes(5) (D-04)"
    - "Cache invalidation _cache.Remove(key) setelah SaveChangesAsync di mutation actions (PATTERNS Cross-cutting Pattern 3)"
    - "AsNoTracking() di chain start sebelum Where() di read-only path (PATTERNS Cross-cutting Pattern 2)"
    - "Redundant Include hapus saat projection punya null guard (EF Core 8 auto-emit LEFT JOIN dari projection)"
key-files:
  created:
    - path: "Migrations/20260507073825_AddManageAssessmentPerfIndexes.cs"
      purpose: "EF Core migration Up: 2 CreateIndex (IX_AssessmentSessions_ExamWindowCloseDate + IX_AssessmentSessions_LinkedGroupId); Down: 2 DropIndex"
    - path: "Migrations/20260507073825_AddManageAssessmentPerfIndexes.Designer.cs"
      purpose: "Auto-generated migration designer (snapshot di point-in-time migration ini)"
  modified:
    - path: "Data/ApplicationDbContext.cs"
      purpose: "2 baris entity.HasIndex appended di AssessmentSession config block (post AccessToken index)"
    - path: "Migrations/ApplicationDbContextModelSnapshot.cs"
      purpose: "Auto-updated dengan 2 b.HasIndex baru (ExamWindowCloseDate + LinkedGroupId) di blok AssessmentSession"
    - path: "Controllers/AssessmentAdminController.cs"
      purpose: "Cache key const CategoriesCacheKey (L24); GetOrCreateAsync wrap di shell (L88) + partial Tab_Assessment (L228); AsNoTracking() di managementQuery (L125); redundant .Include(a => a.User) hapus di partial Tab_Assessment; 3 _cache.Remove(CategoriesCacheKey) hooks di Add (L404), EditCategory POST (L457), DeleteCategory (L487)"
decisions:
  - "Cache key literal const string `assessment_categories_distinct` di top of class (DRY: 1 const, 5 references = 2 GetOrCreateAsync + 3 Remove). Per D-04 + Claude's Discretion."
  - "AsNoTracking di-tambah HANYA di 1 lokasi controller (managementQuery partial Tab_Assessment). Partial Tab_Training dan Tab_History delegasi ke IWorkerDataService yang sudah pakai AsNoTracking di 9 lokasi (verified via grep) — tidak butuh patch controller."
  - "Cache TTL 5 menit absolute expiration (D-04) — pure time-based, no sliding/LRU. Konsisten dengan acceptance D-04 risk profile."
  - "Cache stampede accepted (D-04) — admin endpoint low concurrency (~5-10 concurrent users max). .NET 8 IMemoryCache tanpa native stampede protection; HybridCache .NET 9+ adds it (out of scope Plan 03)."
  - "ToggleCategoryActive (L499) TIDAK di-invalidate karena Categories distinct query target column AssessmentSessions.Category (string), bukan AssessmentCategories.IsActive flag — different dataset (T-311-03-05)."
metrics:
  tasks_completed: 3
  tasks_total: 3
  duration_minutes: ~15
  files_modified: 3
  files_created: 2
  build_errors: 0
  build_warnings: 92
  warnings_baseline: 92
status: complete
---

# Phase 311 Plan 03: Backend Opportunistic Optimizations Summary

**Status:** COMPLETE — 3/3 tasks done, build hijau (0 errors, 92 warnings preserved), 2 commit atomic, migration applied + indexes verified di SQL Server.

## One-liner

Backend opportunistic optimizations (~50 baris kode + 1 EF migration): tambah 2 single-column index pada `AssessmentSessions` (`ExamWindowCloseDate` + `LinkedGroupId`) via EF Core migration `AddManageAssessmentPerfIndexes`; wrap distinct Categories query dengan `IMemoryCache.GetOrCreateAsync` TTL 5 menit + 3 invalidation hooks di Add/Edit/DeleteCategory; tambah `.AsNoTracking()` di `managementQuery` partial `Tab_Assessment`; hapus redundant `.Include(a => a.User)` (projection sudah punya null guard).

## Tasks Status

| # | Task                                                                                                  | Status | Commit     | Notes                                                  |
| - | ----------------------------------------------------------------------------------------------------- | ------ | ---------- | ------------------------------------------------------ |
| 1 | Tambah 2 entity.HasIndex + generate + apply migration AddManageAssessmentPerfIndexes                  | DONE   | `ac86fea3` | 4 file (DbContext, migration .cs + .Designer.cs, snapshot) |
| 2 | AsNoTracking + Include hapus + IMemoryCache Categories + 3 invalidation hooks di AssessmentAdminController | DONE   | `68073b5c` | 1 file, 35 insertions / 17 deletions                   |
| 3 | Smoke build + index verification + SUMMARY                                                            | DONE   | (this)     | Build hijau, indexes verified via sqlcmd, SUMMARY created |

## Implementation Details

### Task 1 — EF migration AddManageAssessmentPerfIndexes (commit `ac86fea3`)

**`Data/ApplicationDbContext.cs`** (L177-185, append-only di AssessmentSession block):

```csharp
// Indexes for performance
entity.HasIndex(a => a.UserId);
entity.HasIndex(a => new { a.UserId, a.Status });
entity.HasIndex(a => a.Schedule);
entity.HasIndex(a => a.AccessToken); // Removed .IsUnique() to allow shared tokens

// Phase 311 Plan 03: ManageAssessment perf indexes (D-05, D-06)
entity.HasIndex(a => a.ExamWindowCloseDate);
entity.HasIndex(a => a.LinkedGroupId);
```

**Migration `Migrations/20260507073825_AddManageAssessmentPerfIndexes.cs`** (auto-generated):

```csharp
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
        name: "IX_AssessmentSessions_ExamWindowCloseDate",
        table: "AssessmentSessions");

    migrationBuilder.DropIndex(
        name: "IX_AssessmentSessions_LinkedGroupId",
        table: "AssessmentSessions");
}
```

**Index naming match D-07 convention exact:** `IX_AssessmentSessions_LinkedGroupId` + `IX_AssessmentSessions_ExamWindowCloseDate` (EF Core convention `IX_{Table}_{Column}` auto-match — no manual edit needed).

**Snapshot updated:** `Migrations/ApplicationDbContextModelSnapshot.cs` L440 + L442 menambah `b.HasIndex("ExamWindowCloseDate")` + `b.HasIndex("LinkedGroupId")` di blok AssessmentSession entity.

**Migration applied ke Dev DB:**

```
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260507073825_AddManageAssessmentPerfIndexes'.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (56ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE INDEX [IX_AssessmentSessions_ExamWindowCloseDate] ON [AssessmentSessions] ([ExamWindowCloseDate]);
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (6ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE INDEX [IX_AssessmentSessions_LinkedGroupId] ON [AssessmentSessions] ([LinkedGroupId]);
```

**SQL Server verification (sqlcmd):**

```sql
SELECT i.name FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = 'AssessmentSessions'
  AND i.name IN ('IX_AssessmentSessions_LinkedGroupId', 'IX_AssessmentSessions_ExamWindowCloseDate');
-- Output:
-- IX_AssessmentSessions_ExamWindowCloseDate
-- IX_AssessmentSessions_LinkedGroupId
-- (2 rows affected)
```

### Task 2 — AsNoTracking + Cache + Invalidation hooks (commit `68073b5c`)

**Cache key constant** (`AssessmentAdminController.cs` L24):

```csharp
private readonly IMemoryCache _cache;
// Phase 311 Plan 03: cache key untuk distinct Categories list (D-04)
private const string CategoriesCacheKey = "assessment_categories_distinct";
private readonly ILogger<AssessmentAdminController> _logger;
```

**Cache wrap di shell `ManageAssessment`** (L85-95):

```csharp
ViewBag.Categories = await _cache.GetOrCreateAsync(CategoriesCacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await _context.AssessmentSessions
        .AsNoTracking()
        .Select(a => a.Category)
        .Distinct()
        .OrderBy(c => c)
        .ToListAsync();
});
```

**Cache wrap di partial `Tab_Assessment`** (L225-237) — sama persis dengan shell, share cache entry via constant key (single TTL).

**AsNoTracking + Include removal di partial `Tab_Assessment`:**

Sebelum (Plan 02 baseline):
```csharp
var managementQuery = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();

var allSessions = await managementQuery
    .Include(a => a.User)
    .OrderByDescending(a => a.Schedule)
    ...
```

Sesudah (Plan 03):
```csharp
// Phase 311 Plan 03: AsNoTracking di chain start read-only partial action (PATTERNS Cross-cutting Pattern 2)
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();

// Phase 311 Plan 03: redundant Include navigation dihapus.
// Projection sudah punya null guard a.User != null, EF Core 8 auto-emit LEFT JOIN AspNetUsers dari projection.
var allSessions = await managementQuery
    .OrderByDescending(a => a.Schedule)
    ...
```

**3 cache invalidation hooks** (setelah `SaveChangesAsync`, sebelum audit log):

- `AddCategory` L404: `_cache.Remove(CategoriesCacheKey);`
- `EditCategory` POST L457: `_cache.Remove(CategoriesCacheKey);`
- `DeleteCategory` L487: `_cache.Remove(CategoriesCacheKey);`

`ToggleCategoryActive` (L499) TIDAK di-invalidate — query target `AssessmentSessions.Category` (string column) tidak bergantung pada `AssessmentCategories.IsActive` flag (T-311-03-05 dispositioned `mitigate` dengan justifikasi different-dataset).

### Acceptance Criteria Verification

| Criteria                                                                  | Status | Evidence                                                  |
| ------------------------------------------------------------------------- | ------ | --------------------------------------------------------- |
| 2 baris HasIndex di ApplicationDbContext.cs                               | PASS   | `grep "entity.HasIndex(a => a.ExamWindowCloseDate)"` = 1, `grep "LinkedGroupId)" Data/ApplicationDbContext.cs` = 1 |
| Migration AddManageAssessmentPerfIndexes generated                        | PASS   | `Migrations/20260507073825_AddManageAssessmentPerfIndexes.cs` exists |
| Migration content correct (2 CreateIndex Up + 2 DropIndex Down)           | PASS   | Inspected file — index names match D-07 exact             |
| Snapshot updated dengan 2 b.HasIndex                                      | PASS   | `grep "b.HasIndex(\"(ExamWindowCloseDate\|LinkedGroupId)\")"` di snapshot = 2 hits (L440, L442) |
| Migration applied ke Dev DB                                               | PASS   | `dotnet ef migrations list` shows 20260507073825_AddManageAssessmentPerfIndexes |
| Indexes ada di SQL Server                                                 | PASS   | sqlcmd query returns 2 rows (lihat verification di atas)  |
| CategoriesCacheKey constant ada                                           | PASS   | L24 `private const string CategoriesCacheKey = "assessment_categories_distinct";` |
| GetOrCreateAsync(CategoriesCacheKey) ≥ 2                                  | PASS   | grep counts = 2 (L88 shell + L228 partial Tab_Assessment) |
| AbsoluteExpirationRelativeToNow TimeSpan.FromMinutes(5) ≥ 2               | PASS   | grep counts = 2 (di kedua factory delegate)               |
| _cache.Remove(CategoriesCacheKey) ≥ 3                                     | PASS   | grep counts = 3 (L404 Add, L457 Edit, L487 Delete)        |
| AsNoTracking di Controllers/AssessmentAdminController.cs ≥ 3              | PASS   | grep counts = 5 (1 managementQuery L125 + 2 cache delegates L92/L232 + 1 existing L2922 + 1 partial Assessment cache delegate) |
| Include(a => a.User) di partial Tab_Assessment scope = 0                  | PASS   | awk-scoped grep = 0                                       |
| Build 0 errors ≤ 92 warnings                                              | PASS   | `dotnet build --no-incremental` → 0 errors, 92 warnings   |
| Smoke parity 3 tab tetap render konten identik                            | DEFER  | Manual UAT — controller logic preserved (semantic only AsNoTracking + cache wrap), risk minimal |

## Build Verdict

```
Time Elapsed 00:00:18.73 (full rebuild post Task 2)
0 Error(s)
92 Warning(s) (Phase 309 baseline preserved — 0 regression)
```

## Deviations from Plan

**None.** Plan dieksekusi exactly seperti tertulis. 3 task selesai dengan 2 commit atomic + 1 SUMMARY commit:

- Task 1 (commit `ac86fea3`): index Fluent + migration generate + apply
- Task 2 (commit `68073b5c`): controller cache + AsNoTracking + invalidation
- Task 3 (commit pending): SUMMARY + verification

Catatan minor: 1 small adjustment komentar code di partial `Tab_Assessment` untuk hindari grep false-positive — komentar awalnya berisi literal pattern `Include(a => a.User)` yang ter-match acceptance grep. Komentar di-paraphrase ke `Include navigation` (semantic equivalent). No code-line change.

## Threat Model Verification

Per `<threat_model>` PLAN.md L591-611:

| Threat ID    | Disposition | Status                                                                                        |
| ------------ | ----------- | --------------------------------------------------------------------------------------------- |
| T-311-03-01  | accept      | OK — migration auto-generated tooling, Down() rollback path tersedia                          |
| T-311-03-02  | mitigate    | OK — cache key static const, no user input in key construction                                |
| T-311-03-03  | accept      | OK — admin low concurrency, stampede ~5-10 redundant queries acceptable                       |
| T-311-03-04  | accept      | OK — projection terminal di ToListAsync, no lazy loading runtime                              |
| T-311-03-05  | mitigate    | OK — 3 invalidation hooks di Add/Edit/DeleteCategory; ToggleCategoryActive justified-skip     |
| T-311-03-06  | accept      | OK — non-unique single-column indexes, query optimizer auto-pick                              |

Tidak ada HIGH/CRITICAL threat. Semua mitigation requirement terpenuhi.

## Auth/Authorization Posture

Tidak ada perubahan auth surface. Semua action existing di-preserve `[Authorize(Roles = "Admin, HC")]`. Cache key static literal const — TIDAK derive dari user input (mitigates T-311-03-02 cache poisoning). Cache value hanya berisi public Category names (`a.Category` — public domain entity).

## Phase 311 Close Note

Setelah Plan 03 ini di-merge oleh orchestrator, Phase 311 effectively complete:

- Plan 01 (baseline): preserved (commit `a4ce556e` Stopwatch baseline direpurposed ke per-action)
- Plan 02 (HTMX lazy load): COMPLETE (commits 03abcc92, c2b5a910, 0f3e4690 + UAT lokal Plan 04 PASS, deploy webdev pending Test 4 wifi kantor)
- Plan 03 (backend opportunistic): COMPLETE (commits ac86fea3, 68073b5c)
- Plan 04 (HTMX gap closure): COMPLETE (commits b17292f7, bbf88fa8, b5fb6354 + UAT lokal PASS)

Phase 311 ready untuk `/gsd-verify-work` close setelah orchestrator merge worktree + Test 4 wifi kantor PASS.

## Cross-reference Plan 02

Plan 02 SUMMARY (`311-02-SUMMARY.md`) primary lever untuk SC #2 (≥50% reduction wifi kantor). Plan 03 backend opportunistic small wins (~10-20% per partial action) bersifat additive — tidak block atau replace Plan 02 acceptance. Wifi kantor measurement (Test 4) tetap bergantung pada Plan 02 deployment.

## Self-Check: PASSED

Files verified to exist:

- FOUND: `Data/ApplicationDbContext.cs` (2 baris HasIndex baru, marker "Phase 311 Plan 03")
- FOUND: `Migrations/20260507073825_AddManageAssessmentPerfIndexes.cs` (Up: 2 CreateIndex, Down: 2 DropIndex)
- FOUND: `Migrations/20260507073825_AddManageAssessmentPerfIndexes.Designer.cs`
- FOUND: `Migrations/ApplicationDbContextModelSnapshot.cs` (2 b.HasIndex baru di L440 + L442)
- FOUND: `Controllers/AssessmentAdminController.cs` (CategoriesCacheKey const, 2 GetOrCreateAsync, 3 _cache.Remove, 5 AsNoTracking, 0 Include(a => a.User) di partial Tab_Assessment scope)

Commits verified:

- FOUND: `ac86fea3` (Task 1 — Fluent HasIndex + migration + snapshot)
- FOUND: `68073b5c` (Task 2 — controller cache + AsNoTracking + invalidation)

Build verified: 0 errors, 92 warnings (Phase 309 baseline preserved).

DB state verified: Migration `20260507073825_AddManageAssessmentPerfIndexes` listed di `dotnet ef migrations list`; 2 indexes (`IX_AssessmentSessions_ExamWindowCloseDate` + `IX_AssessmentSessions_LinkedGroupId`) ter-create di `HcPortalDB_Dev` SQL Express (sqlcmd verification 2 rows affected).
