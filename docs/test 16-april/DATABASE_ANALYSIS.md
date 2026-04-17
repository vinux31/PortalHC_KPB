# Database Query Analysis — PortalHC KPB
**Tanggal Analisis:** 16 April 2026
**Scope:** CDPController, WorkerController, WorkerDataService, AssessmentAdminController (ManageAssessment + AssessmentMonitoring + Activity Log)
**Status:** Static Analysis

---

## 1. Klasifikasi Query Pattern

### 1.1 Tabel — Blocking `.ToList()` vs `.ToListAsync()`

| File | Line | Query | Mode | Masalah |
|------|------|-------|------|---------|
| `CDPController.cs` | 316 | `ProtonTracks.Select(TrackType).Distinct().OrderBy().ToList()` | **BLOCKING** | Sync call dalam async method |
| `CDPController.cs` | 317 | `ProtonTracks.OrderBy(Urutan).Select(DisplayName).ToList()` | **BLOCKING** | Sync call dalam async method |
| `WorkerController.cs` | 92 | `query.OrderBy(FullName).ToListAsync()` | Async ✓ | Tidak ada `AsNoTracking()` |
| `WorkerController.cs` | 95-100 | `UserRoles.Join(Roles,...).ToListAsync()` | Async ✓ | Tidak ada filter WHERE ke user IDs |
| `WorkerController.cs` | 108 | `Users.CountAsync()` | Async ✓ | Query redundan (4x) |
| `WorkerController.cs` | 109 | `Users.CountAsync(RoleLevel==1)` | Async ✓ | Query redundan (4x) |
| `WorkerController.cs` | 110 | `Users.CountAsync(RoleLevel==2)` | Async ✓ | Query redundan (4x) |
| `WorkerController.cs` | 111 | `Users.CountAsync(RoleLevel>=5)` | Async ✓ | Query redundan (4x) |

---

## 2. Detail Query Pattern per Controller

### 2.1 CDPController — `GetCascadeOptions`

**Lokasi:** `CDPController.cs` ~line 310-320

**Pattern yang Bermasalah:**
```csharp
public async Task<IActionResult> GetCascadeOptions(string? section)
{
    // Query 1 — async (benar)
    var units = await _context.GetUnitsForSectionAsync(section);

    // Query 2 — BLOCKING (salah)
    var categories = _context.ProtonTracks
        .Select(t => t.TrackType)
        .Distinct()
        .OrderBy(t => t)
        .ToList();   // ← synchronous

    // Query 3 — BLOCKING (salah)
    var tracks = _context.ProtonTracks
        .OrderBy(t => t.Urutan)
        .Select(t => t.DisplayName)
        .ToList();   // ← synchronous

    return Json(new { units, categories, tracks });
}
```

**Tabel ProtonTracks diakses:** Ya (2x, sequential blocking)
**Tabel yang terlibat:** `ProtonTracks`
**Estimasi rows:** Bergantung data master CDP — biasanya kecil (<100 rows), tapi pattern tetap salah karena blokir thread

**Pattern Perbaikan:**
```
ProtonTracks → ToListAsync (async) + Task.WhenAll (paralel)
```

---

### 2.2 WorkerController — `Index` Action

**Lokasi:** `WorkerController.cs` lines 92, 95-100, 108-111

#### Query A: Users Fetch (C-03, line 92)

```csharp
// IQueryable dibangun dengan filter opsional
var query = _context.Users.AsQueryable();
if (!string.IsNullOrEmpty(searchString)) query = query.Where(...);
if (!string.IsNullOrEmpty(section))      query = query.Where(...);
if (!string.IsNullOrEmpty(unit))         query = query.Where(...);

// Execute — MISSING AsNoTracking (line 92)
var users = await query
    .OrderBy(u => u.FullName)
    .ToListAsync();   // fetch bisa ribuan rows, tanpa AsNoTracking
```

**Problem:** EF Core change tracking aktif untuk semua entitas yang dimuat. Tidak diperlukan karena data ini hanya untuk ditampilkan (read-only).

**Estimasi overhead tracking:** ~200-300 byte per entitas untuk snapshot → 5.000 users × 300 byte = ~1.5 MB overhead tracking saja

#### Query B: UserRoles Fetch (C-03, lines 95-100)

```csharp
var userRolesDict = (await _context.UserRoles
    .Join(_context.Roles,
        ur => ur.RoleId,
        r  => r.Id,
        (ur, r) => new { ur.UserId, r.Name })
    .ToListAsync())   // ← fetch SEMUA UserRoles, bukan hanya untuk user yang di-load
    .GroupBy(x => x.UserId)
    .ToDictionary(g => g.Key, g => g.First().Name ?? "No Role");   // single role name per user
```

**Problem:** Jika sistem punya 5.000 users tapi hanya 50 yang dimuat (karena filter), query ini tetap mengambil seluruh tabel UserRoles ke memori aplikasi. Meskipun dictionary akhir hanya menyimpan **satu role name string** per user (via `.First().Name`), fetch-nya tetap memuat seluruh baris `UserRoles × Roles` join ke memori aplikasi sebelum disaring.

**SQL yang dihasilkan (perkiraan):**
```sql
SELECT ur.UserId, r.Name
FROM UserRoles ur
INNER JOIN Roles r ON ur.RoleId = r.Id
-- TIDAK ADA WHERE clause
```

**SQL yang seharusnya:**
```sql
SELECT ur.UserId, r.Name
FROM UserRoles ur
INNER JOIN Roles r ON ur.RoleId = r.Id
WHERE ur.UserId IN (... list of loaded user IDs ...)
```

#### Query C: Stats Count (C-02 + H-06, lines 108-111)

```csharp
// 4 query sequential ke tabel Users (lines 108-111)
ViewBag.TotalUsers  = await _context.Users.CountAsync();
ViewBag.AdminCount  = await _context.Users.CountAsync(u => u.RoleLevel == 1);
ViewBag.HcCount     = await _context.Users.CountAsync(u => u.RoleLevel == 2);
ViewBag.WorkerCount = await _context.Users.CountAsync(u => u.RoleLevel >= 5);
```

**SQL yang dihasilkan (4 query terpisah):**
```sql
SELECT COUNT(*) FROM Users;
SELECT COUNT(*) FROM Users WHERE RoleLevel = 1;
SELECT COUNT(*) FROM Users WHERE RoleLevel = 2;
SELECT COUNT(*) FROM Users WHERE RoleLevel >= 5;
```

**SQL optimal (1 query):**
```sql
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN RoleLevel = 1 THEN 1 ELSE 0 END) AS Admin,
    SUM(CASE WHEN RoleLevel = 2 THEN 1 ELSE 0 END) AS HC,
    SUM(CASE WHEN RoleLevel >= 5 THEN 1 ELSE 0 END) AS Worker
FROM Users;
```

**Issue tambahan (H-06):** Query stats tidak menggunakan `query` (IQueryable terfilter), melainkan `_context.Users` langsung. Akibatnya stats selalu menghitung total sistem, bukan hasil filter yang sedang aktif.

---

## 3. Ringkasan DB Round Trips per Request

### Halaman ManageWorkers (kondisi saat ini)

| # | Query | Round Trip | Masalah |
|---|-------|-----------|---------|
| 1 | Users filter + fetch | 1 RT | No AsNoTracking |
| 2 | UserRoles JOIN Roles (semua) | 1 RT | No WHERE filter |
| 3 | COUNT(*) total | 1 RT | Redundan |
| 4 | COUNT(*) RoleLevel=1 | 1 RT | Redundan |
| 5 | COUNT(*) RoleLevel=2 | 1 RT | Redundan |
| 6 | COUNT(*) RoleLevel>=5 | 1 RT | Redundan |
| **Total** | | **6 RT** | |

### Kondisi Optimal

| # | Query | Round Trip |
|---|-------|-----------|
| 1 | Users filter + fetch (AsNoTracking) | 1 RT |
| 2 | UserRoles JOIN Roles (WHERE userId IN ...) | 1 RT |
| 3 | Aggregated stats (single query) | 1 RT |
| **Total** | | **3 RT** |

**Penghematan:** 50% pengurangan round trips ke database per page load.

---

## 4. Tabel Impact Matrix

| Issue | Tabel Terdampak | Estimasi Rows | Frekuensi Akses | Severity |
|-------|----------------|---------------|----------------|---------|
| C-01 | ProtonTracks | <100 | Setiap dropdown cascade | Critical (thread block) |
| C-02 | Users | N/A (COUNT) | Setiap load halaman | Critical (4x RT) |
| C-03a | Users | 1 - N_total | Setiap load halaman | Critical (no tracking opt) |
| C-03b | UserRoles + Roles | N_total_roles | Setiap load halaman | Critical (unfiltered fetch) |
| H-06 | Users | N/A (COUNT) | Setiap load halaman | High (UX inconsistency) |
| C-04a | AssessmentAttemptHistory + User | N_total (full) | Setiap load riwayat worker | Critical (no filter) |
| C-04b | AssessmentSessions + User | N_completed | Setiap load riwayat worker | Critical (status-only filter) |
| C-04c | TrainingRecords + User | N_total (full) | Setiap load riwayat worker | Critical (no filter) |
| H-05 | AssessmentSessions (in-memory) | N_filtered | Setiap load ManageAssessment | High (in-memory grouping) |
| M-04 | AssessmentSessions | N_7hari | Setiap load ManageAssessment | Medium (no AsNoTracking) |
| M-09 | AssessmentSessions | N_total (full) | Setiap load ManageAssessment | Medium (full scan dropdown) |

---

## 5. Detail Query Pattern — WorkerDataService + AssessmentAdminController

### 5.1 WorkerDataService — `GetAllWorkersHistory()`

**Lokasi:** `Services/WorkerDataService.cs` lines 85-172

Method ini dipanggil untuk menampilkan riwayat assessment seorang worker. Struktur method berisi **1 query aggregate (sudah optimal)** + **3 query full-fetch (issue)**:

#### Query 0 — archivedCounts (SUDAH OPTIMAL, line 90-94)

```csharp
var archivedCounts = await _context.AssessmentAttemptHistory
    .AsNoTracking()
    .GroupBy(h => new { h.UserId, h.Title })
    .Select(g => new { g.Key.UserId, g.Key.Title, Count = g.Count() })
    .ToListAsync();
```

**Bukan issue** — query ini menggunakan `GroupBy` + `Count()` yang ter-translate ke SQL aggregate, tidak memuat entitas. Berfungsi menyediakan attempt number per (UserId, Title) tanpa fetch penuh `AssessmentAttemptHistory`.

#### Query 1 — AssessmentAttemptHistory (Full Fetch, line 100-103)

```csharp
var archivedAttempts = await _context.AssessmentAttemptHistory
    .AsNoTracking()
    .Include(h => h.User)
    .ToListAsync();   // ← TIDAK ADA WHERE clause
```

**SQL yang dihasilkan (perkiraan):**
```sql
SELECT h.*, u.*
FROM AssessmentAttemptHistory h
INNER JOIN Users u ON h.UserId = u.Id
-- TIDAK ADA WHERE
```

**Masalah:** Seluruh tabel riwayat dimuat — semakin tua sistem, semakin besar data.

#### Query 2 — AssessmentSessions (Filter Status Saja, line 121-125)

```csharp
var currentCompleted = await _context.AssessmentSessions
    .AsNoTracking()
    .Include(a => a.User)
    .Where(a => a.Status == "Completed")
    .ToListAsync();
```

**SQL yang dihasilkan:**
```sql
SELECT a.*, u.*
FROM AssessmentSessions a
INNER JOIN Users u ON a.UserId = u.Id
WHERE a.Status = 'Completed'
-- Masih bisa ribuan rows jika banyak sesi selesai
```

**Masalah:** Filter hanya pada `Status`, tidak ada filter UserId atau rentang tanggal.

#### Query 3 — TrainingRecords (Full Fetch, line 152-155)

```csharp
var trainings = await _context.TrainingRecords
    .AsNoTracking()
    .Include(t => t.User)
    .ToListAsync();   // ← TIDAK ADA WHERE clause
```

**SQL yang dihasilkan:**
```sql
SELECT t.*, u.*
FROM TrainingRecords t
INNER JOIN Users u ON t.UserId = u.Id
-- TIDAK ADA WHERE
```

**Tabel yang terlibat:** `AssessmentAttemptHistory`, `AssessmentSessions`, `TrainingRecords`, `Users` (di-join 3x)
**Total DB round trips:** 3 RT sequential per request
**Estimasi data yang dimuat:** Bergantung volume historis — bisa puluhan ribu rows

---

### 5.2 AssessmentAdminController — `Index` Action

**Lokasi:** `Controllers/AssessmentAdminController.cs` ~line 66-176

#### Query A: Initial Query (M-04)

```csharp
// MISSING AsNoTracking
var managementQuery = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();
```

**SQL yang dihasilkan:**
```sql
SELECT * FROM AssessmentSessions
WHERE COALESCE(ExamWindowCloseDate, Schedule) >= @sevenDaysAgo
```

**Masalah:** EF Core tracking aktif — overhead memori untuk setiap entitas yang dimuat.

#### Query B: Dropdown Kategori (M-09)

```csharp
// Full scan tanpa filter tanggal
ViewBag.Categories = await _context.AssessmentSessions
    .Select(a => a.Category)
    .Distinct()
    .OrderBy(c => c)
    .ToListAsync();
```

**SQL yang dihasilkan:**
```sql
SELECT DISTINCT Category
FROM AssessmentSessions
ORDER BY Category
-- TIDAK ADA WHERE — full table scan
```

**SQL yang seharusnya:**
```sql
SELECT DISTINCT Category
FROM AssessmentSessions
WHERE COALESCE(ExamWindowCloseDate, Schedule) >= @sevenDaysAgo
ORDER BY Category
```

#### Query C: In-Memory Grouping (H-05)

Setelah `allSessions` dimuat dari database, grouping dilakukan seluruhnya di memori C#:

```
allSessions (from DB)
  ├── Filter: LinkedGroupId != null  → mgPrePostSessions
  │     └── GroupBy(LinkedGroupId)  → prePostGrouped
  └── Filter: LinkedGroupId == null → mgStandardSessions
        └── GroupBy(Title, Category, Schedule.Date) → standardGrouped

prePostGrouped + standardGrouped
  └── Concat → OrderByDescending(Schedule) → grouped (final, di memory)
```

**Masalah:** Semua operasi di atas adalah LINQ-to-Objects, bukan LINQ-to-SQL. Database tidak terlibat dalam grouping/sorting — seluruh beban jatuh ke CPU server.

---

## 6. Ringkasan DB Round Trips per Request (Updated)

### Halaman ManageAssessment (kondisi saat ini)

| # | Query | Round Trip | Masalah |
|---|-------|-----------|---------|
| 1 | AssessmentSessions (7 hari, no AsNoTracking) | 1 RT | No AsNoTracking |
| 2 | Dropdown categories (full scan) | 1 RT | No date filter |
| **Total** | | **2 RT** | Grouping di memory |

### GetAllWorkersHistory (kondisi saat ini)

| # | Query | Round Trip | Masalah |
|---|-------|-----------|---------|
| 1 | AssessmentAttemptHistory (full, + User) | 1 RT | No filter |
| 2 | AssessmentSessions Completed (+ User) | 1 RT | Status filter only |
| 3 | TrainingRecords (full, + User) | 1 RT | No filter |
| **Total** | | **3 RT** | Semua in-memory grouping |

### Kondisi Optimal — GetAllWorkersHistory

| # | Query | Round Trip |
|---|-------|-----------|
| 1 | AssessmentAttemptHistory (WHERE UserId = @id) | 1 RT |
| 2 | AssessmentSessions Completed (WHERE UserId = @id) | 1 RT |
| 3 | TrainingRecords (WHERE UserId = @id) | 1 RT |
| **Total** | | **3 RT** (same, but filtered) |

---

## 7. Audit `.ToList()` di AssessmentAdminController.cs (H-01)

### 7.1 Ringkasan

Total `.ToList()` ditemukan: **72 occurrences**
Klasifikasi: **0 blocking DB call**, **72 in-memory (LINQ-to-Objects)**

Semua query DB sudah menggunakan varian async (`.ToListAsync()`, `.ToDictionaryAsync()`, `.CountAsync()`, dll). Tidak ditemukan pola `_context.X....ToList()` sinkron pada `IQueryable`. Verifikasi dilakukan dengan regex multi-line:

```
_context\.[A-Za-z]+[\s\S]*?\.ToList\(\)(?!Async)
```

Hasil: **No matches found.**

### 7.2 Kategori Penggunaan `.ToList()` In-Memory

| Kategori | Jumlah | Contoh Line |
|----------|--------|-------------|
| A. Filter/projection pada list yang sudah di-fetch (LINQ-to-Objects) | ±40 | 112, 113, 166, 168, 2352, 2353, 2470, 2475 |
| B. `Select(s => s.Id).ToList()` untuk param batch `Contains()` | ±10 | 1472, 1601, 2065, 2159, 2510, 2621, 3273, 3622, 3881 |
| C. `Select(...).ToList<dynamic>()` untuk ViewBag | ±6 | 157, 185, 1466, 2428, 2459, 2464 |
| D. Grouping result materialization | ±8 | 161, 275, 2428, 2459, 2585, 3328 |
| E. Split/Parse text utility (Pasted excel, CSV) | ±4 | 4331, 4443 |
| F. Build list dari properti yang sudah loaded via Include | ±4 | 3725, 3767, 3791, 3810, 3811, 4264, 4649, 4784 |

### 7.3 Detail Per Baris (Sampel Kunci)

| Line | Pola | Klasifikasi | Keterangan |
|------|------|-------------|-----------|
| 112 | `allSessions.Where(a => a.LinkedGroupId != null).ToList()` | **In-memory (A)** | `allSessions` adalah hasil `.ToListAsync()` di line sebelumnya |
| 113 | `allSessions.Where(a => a.LinkedGroupId == null).ToList()` | **In-memory (A)** | Sama seperti 112 — hanya filter in-memory |
| 129 | `g.Select(a => a.Id).ToList()` | **In-memory (B)** | Dalam `GroupBy` projection — `g` adalah `IGrouping` in-memory |
| 151 | `g.Select(a => a.Id).ToList()` | **In-memory (B)** | Sama seperti 129 |
| 157 | `.ToList<dynamic>()` pada hasil `Select` | **In-memory (C)** | Materialization ViewBag, bukan DB |
| 161 | `prePostGrouped.Concat(standardGrouped).OrderByDescending(...).ToList()` | **In-memory (D)** | Merge hasil 2 grouping memori |
| 166, 168 | `grouped.Where(g => g.GroupStatus != "Closed").ToList()` | **In-memory (A)** | Filter status setelah grouping memori |
| 185 | `grouped.Skip(...).Take(...).ToList()` | **In-memory (A)** | Pagination in-memory ViewBag |
| 219 | `.Select(r => r.Title).Where(...).Distinct().OrderBy().ToList()` | **In-memory (A)** | Pada `assessmentHistory` yang sudah di-fetch |
| 275 | `allCats.GroupBy(...).Select(...).ToList()` | **In-memory (D)** | `allCats` hasil `.ToListAsync()` di line 271 |
| 642, 716 | `sourceSessions.Select(...).ToList()` | **In-memory (B)** | Sumber dari query sebelumnya |
| 991 | `UserIds.Except(userDictionary.Keys).ToList()` | **In-memory (A)** | Set operation in-memory |
| 1405, 1406 | `groupSessions.Where(a => a.AssessmentType == "PreTest").ToList()` | **In-memory (A)** | `groupSessions` sudah loaded |
| 1411, 1412 | `preSessions.Select(s => s.Id).ToList()` | **In-memory (B)** | ViewBag param |
| 1415 | `preSessions.Where(...).Select(...).Distinct().ToList()` | **In-memory (A)** | Build userIds untuk query berikutnya |
| 1429, 1430 | `preSessions.Select(s => s.Id).ToList()` | **In-memory (B)** | Batch param |
| 1453 | `siblings.Where(...).Select(...).Distinct().ToList()` | **In-memory (A)** | `siblings` hasil `.ToListAsync()` |
| 1466 | `.Select(...).ToList<dynamic>()` | **In-memory (C)** | ViewBag |
| 1472 | `siblings.Select(s => s.Id).ToList()` | **In-memory (B)** | Batch param |
| 1535, 1536 | `allGroupSessions.Where(...).ToList()` | **In-memory (A)** | Setelah `.ToListAsync()` |
| 1576, 1579 | `UserIds.Where(...).ToList()` | **In-memory (A)** | Parameter list |
| 1601 | `sessionsToRemove.Select(...).ToList()` | **In-memory (B)** | Batch param |
| 1835 | `NewUserIds.Where(...).Distinct().ToList()` | **In-memory (A)** | List parameter |
| 1844 | `filteredNewUserIds.Except(userDictionary.Keys).ToList()` | **In-memory (A)** | Set operation |
| 1870 | `filteredNewUserIds.Select(uid => new AssessmentSession {...}).ToList()` | **In-memory (A)** | Build entities untuk AddRange |
| 2065, 2159 | `siblings.Select(...).ToList()` / `groupSessions.Select(...).ToList()` | **In-memory (B)** | Batch param |
| 2352, 2353 | `allSessions.Where(...).ToList()` | **In-memory (A)** | Sudah dibahas di H-03 |
| 2360, 2361 | `g.Where(a => a.AssessmentType == "PreTest").ToList()` | **In-memory (A)** | Dalam grouping projection |
| 2428, 2459 | `.Select(g => new MonitoringGroupViewModel {...}).ToList()` | **In-memory (C/D)** | Grouping result |
| 2464, 2470, 2475 | `prePostGroups.Concat(...).OrderByDescending(...).ToList()` dll | **In-memory (A/D)** | Merge + filter status |
| 2510, 2521 | `sessions.Select(s => s.Id).ToList()` / pattern map | **In-memory (B)** | Batch param + helper |
| 2585 | `sessionViewModels.OrderBy(...).ThenBy(...).ToList()` | **In-memory (D)** | Sort final di memory |
| 2621 | `model.Sessions.Select(s => s.Id).ToList()` | **In-memory (B)** | Batch param |
| 2633 | `model.Sessions.Where(s => s.HasManualGrading).ToList()` | **In-memory (A)** | Part of H-04 loop |
| 2661 | `essayQs.Select((q, idx) => new EssayGradingItemViewModel {...}).ToList()` | **In-memory (D)** | Build per-session items |
| 3184, 3197 | `sessionsToEnd.Where(...).ToList()` | **In-memory (A)** | `sessionsToEnd` hasil `.ToListAsync()` |
| 3273, 3622 | `sessions.Select(s => s.Id).ToList()` | **In-memory (B)** | Batch param |
| 3328 | `.Select(...).OrderBy(...).ThenBy(...).ToList()` | **In-memory (D)** | ExportAssessmentResults materialization |
| 3725, 3767 | `packages[0].Questions.OrderBy(...).Select(...).ToList()` | **In-memory (F)** | Properti `.Questions` dari Include |
| 3737 | `packages.SelectMany(...).ToList()` | **In-memory (F)** | Cross-package flatten |
| 3744, 3791, 3810, 3811 | Distinct/ordering/filter question IDs | **In-memory (A/F)** | Pure in-memory algorithm |
| 3881 | `packages.Select(p => p.Id).ToList()` | **In-memory (B)** | Batch param |
| 3895 | `packages.SelectMany(...).Distinct().OrderBy(...).ToList()` | **In-memory (F)** | Build ET group list |
| 3980 | `.Select(...).ToList()` dalam projection | **In-memory (D)** | ViewBag |
| 4065 | `pkg.Questions.Select(q => q.Id).ToList()` | **In-memory (F)** | Dari Include |
| 4137 | `View(pkg.Questions.OrderBy(q => q.Order).ToList())` | **In-memory (F)** | Return ordered list ke view |
| 4264 | `q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList()` | **In-memory (F)** | Dari Include |
| 4331 | `pasteText.Split('\n').Select(...).Where(...).ToList()` | **In-memory (E)** | Parse text CSV |
| 4443 | `cor.Split(',', ...).Select(...).Where(...).Distinct().ToList()` | **In-memory (E)** | Parse comma-separated |
| 4495 | `.Select(...).ToList()` dalam projection ViewBag | **In-memory (C)** | ViewBag |
| 4581 | `allEtGroups.Except(pkgEtGroups).ToList()` | **In-memory (A)** | Set operation |
| 4649 | `pkg.Questions.OrderBy(q => q.Order).ToList()` | **In-memory (F)** | Dari Include |
| 4784 | `.Select(...).ToList()` ViewBag projection | **In-memory (C)** | ViewBag |
| 4981 | `events.Select(e => new {...}).ToList()` | **In-memory (D)** | Format WIB timestamp |

### 7.4 Kesimpulan Audit `.ToList()`

- **0 issue blocking** — tidak perlu perubahan ke `.ToListAsync()`
- Beberapa `.ToList()` termasuk dalam issue lain yang sudah di-scope (H-03, H-05): masalahnya bukan pada `.ToList()` per se, melainkan pada **fetch besar sebelum grouping/filter in-memory**. Fix di issue induknya akan otomatis mengurangi `.ToList()` in-memory
- H-01 direkomendasikan **diturunkan dari High ke Info** di laporan performa

---

## 8. Issue Tambahan — AssessmentMonitoring + AssessmentMonitoringDetail + GetActivityLog

### 8.1 AssessmentMonitoring — H-03 + M-05

**Lokasi:** `Controllers/AssessmentAdminController.cs:2300-2484`

#### Query A: Main session fetch (H-03, M-05)

```csharp
var query = _context.AssessmentSessions           // ← MISSING AsNoTracking()
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();

// + filter search + filter category

var allSessions = await query
    .OrderByDescending(a => a.Schedule)
    .Select(a => new { a.Id, a.Title, a.Category, a.Schedule, /* 13 kolom total */ })
    .ToListAsync();      // ← tanpa Skip/Take
```

**SQL yang dihasilkan:**
```sql
SELECT Id, Title, Category, Schedule, ExamWindowCloseDate, Status,
       IsTokenRequired, AccessToken, CreatedAt, AssessmentType,
       LinkedGroupId, DurationMinutes, CompletedAt, IsPassed, StartedAt, HasManualGrading
FROM AssessmentSessions
WHERE COALESCE(ExamWindowCloseDate, Schedule) >= @sevenDaysAgo
  [AND LOWER(Title) LIKE @search]
  [AND Category = @category]
ORDER BY Schedule DESC
-- TIDAK ADA OFFSET/FETCH — seluruh 7-day window dimuat
```

**Masalah:** No `AsNoTracking()` + no pagination. Grouping & sort final di memory.

#### Query B: GetAkhiriSemuaCounts (M-05)

**Lokasi:** `AssessmentAdminController.cs:3238-3251`

```csharp
var sessions = await _context.AssessmentSessions   // ← MISSING AsNoTracking()
    .Where(a => a.Title == title && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && (a.Status == "Open" || a.Status == "InProgress"))
    .ToListAsync();

int inProgressCount = sessions.Count(s => s.StartedAt != null && s.CompletedAt == null && s.Score == null);
int notStartedCount = sessions.Count(s => s.StartedAt == null);
```

**Masalah:** Untuk sekadar 2 count, seluruh entitas (semua kolom) dimuat ke memory dengan tracking aktif. Bisa diganti 2 `CountAsync()` + `AsNoTracking()`.

### 8.2 AssessmentMonitoringDetail — H-04 + L-02

**Lokasi:** `Controllers/AssessmentAdminController.cs:2489-2669`

#### Query A: QuestionCount via Include (L-02)

```csharp
questionCountMap = (await _context.UserPackageAssignments
    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
    .Join(_context.AssessmentPackages.Include(p => p.Questions),   // ← Include penuh
        a => a.AssessmentPackageId,
        p => p.Id,
        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
    .ToListAsync())
    .GroupBy(...)
    .ToDictionary(...);
```

**Masalah:** `Include(p => p.Questions)` memaksa EF memuat seluruh kolom `PackageQuestions`. Kolom teks panjang (`QuestionText`, `Rubrik`) ikut terbawa walau hanya `.Count` yang diakses.

**SQL yang dihasilkan (perkiraan):**
```sql
SELECT a.*, p.*, q.*        -- q = semua kolom PackageQuestions
FROM UserPackageAssignments a
INNER JOIN AssessmentPackages p ON a.AssessmentPackageId = p.Id
LEFT JOIN PackageQuestions q ON q.PackageId = p.Id
WHERE a.AssessmentSessionId IN (...)
```

**SQL yang seharusnya (tanpa Include, EF mengubah `.Count()` ke scalar subquery):**
```sql
SELECT a.AssessmentSessionId,
       (SELECT COUNT(*) FROM PackageQuestions q WHERE q.PackageId = p.Id) AS QuestionCount
FROM UserPackageAssignments a
INNER JOIN AssessmentPackages p ON a.AssessmentPackageId = p.Id
WHERE a.AssessmentSessionId IN (...)
```

#### Query B: Essay Grading Loop (H-04) — 3 Query per Session

```csharp
foreach (var sess in manualGradingSessions)
{
    // Q1: UserPackageAssignments
    var assignment = await _context.UserPackageAssignments
        .FirstOrDefaultAsync(a => a.AssessmentSessionId == sess.Id);

    // Q2: PackageQuestions (filter shuffled IDs + Essay)
    var essayQs = await _context.PackageQuestions
        .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay")
        .ToListAsync();

    // Q3: PackageUserResponses (per session + per question IDs)
    var essayRespMap = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == sess.Id && essayQs.Select(q => q.Id).Contains(r.PackageQuestionId))
        .ToDictionaryAsync(r => r.PackageQuestionId);
}
```

**DB Round-Trips:** `3 × N_manualGradingSessions`

Untuk grup Pre-Post dengan 50 peserta dan manual grading pada post-test: 50 × 3 = **150 round-trips** per load halaman detail.

**Rekomendasi (batch out of loop):**
```sql
-- Q1: semua assignments
SELECT * FROM UserPackageAssignments WHERE AssessmentSessionId IN (sessionIds);

-- Q2: semua essay questions
SELECT * FROM PackageQuestions
WHERE Id IN (unionOfShuffledIds) AND QuestionType = 'Essay';

-- Q3: semua responses
SELECT * FROM PackageUserResponses
WHERE AssessmentSessionId IN (sessionIds) AND PackageQuestionId IN (essayQIds);
```

Total: **3 round-trips** tetap (bukan 3N).

### 8.3 GetActivityLog — M-06

**Lokasi:** `Controllers/AssessmentAdminController.cs:4956-4988`

**Pattern saat ini (4 round-trips):**

```csharp
// Q1
var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);

// Q2 — sudah ORDER BY Timestamp
var events = await _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .OrderBy(l => l.Timestamp)
    .Select(l => new { l.EventType, l.Detail, TimestampUtc = l.Timestamp })
    .ToListAsync();

// Q3
var totalAnswered = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == sessionId);

// Q4 — REDUNDAN (bisa dihitung dari Q2)
var lastEventTime = await _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .MaxAsync(l => (DateTime?)l.Timestamp);
```

**Optimasi 1 (hilangkan Q4):**
```csharp
DateTime? lastEventTime = events.LastOrDefault()?.TimestampUtc;
```
→ 3 round-trips.

**Optimasi 2 (paralel Q2+Q3):**
```csharp
var eventsTask = /* Q2 */.ToListAsync();
var totalAnsweredTask = /* Q3 */.CountAsync();
await Task.WhenAll(eventsTask, totalAnsweredTask);
```
→ 2 round-trips total (session serial + 2 paralel dihitung sebagai 1 waktu tunggu).

---

## 9. Ringkasan DB Round Trips per Request (Updated — Step 1C)

### Halaman AssessmentMonitoring (kondisi saat ini)

| # | Query | Round Trip | Masalah |
|---|-------|-----------|---------|
| 1 | AssessmentSessions (7 hari, filter search/cat) | 1 RT | No AsNoTracking, no pagination |
| **Total** | | **1 RT** | Grouping + status filter + sort di memory |

### Halaman AssessmentMonitoringDetail (kondisi saat ini, 50 peserta manual grading)

| # | Query | Round Trip | Masalah |
|---|-------|-----------|---------|
| 1 | AssessmentSessions (+ User Include) | 1 RT | — |
| 2 | AssessmentPackages Count (siblingIds) | 1 RT | — |
| 3 | UserPackageAssignments Join Packages (Include Questions) | 1 RT | Include penuh (L-02) |
| 4 | PackageUserResponses essay pending (batch) | 1 RT | — |
| 5 | AssessmentSessions.Find (Proton T3) | 1 RT | Conditional |
| 6 | AssessmentSessions (sibling T3) | 1 RT | Conditional |
| 7-156 | Per-session: UserPackageAssignments + PackageQuestions + PackageUserResponses | **3 × 50 = 150 RT** | N+1 (H-04) |
| **Total** | | **~156 RT** | Didominasi N+1 essay loop |

### Kondisi Optimal — AssessmentMonitoringDetail

| # | Query | Round Trip |
|---|-------|-----------|
| 1 | AssessmentSessions (+ User) | 1 RT |
| 2 | Packages count | 1 RT |
| 3 | Question count via scalar subquery | 1 RT |
| 4 | Essay pending count | 1 RT |
| 5 | Essay batch: assignments + questions + responses | 3 RT |
| **Total** | | **7 RT** (dari 156) |

Penghematan: **~95% pengurangan DB round-trips**.

### Endpoint AJAX GetActivityLog (kondisi saat ini)

| # | Query | Round Trip | Masalah |
|---|-------|-----------|---------|
| 1 | AssessmentSessions Find | 1 RT | — |
| 2 | ExamActivityLogs OrderBy | 1 RT | — |
| 3 | PackageUserResponses Count | 1 RT | — |
| 4 | ExamActivityLogs Max | 1 RT | Redundan (M-06) |
| **Total** | | **4 RT** | |

### Kondisi Optimal — GetActivityLog

| # | Query | Round Trip |
|---|-------|-----------|
| 1 | AssessmentSessions Find | 1 RT |
| 2+3 | Events + Count (paralel Task.WhenAll) | 1 RT (wall time) |
| **Total** | | **2 RT** (wall time) |

---

## 10. Tabel Impact Matrix (Step 1C)

| Issue | Tabel Terdampak | Estimasi Rows | Frekuensi Akses | Severity |
|-------|----------------|---------------|----------------|---------|
| H-03 | AssessmentSessions | 0-1000 (7-day window) | Setiap load AssessmentMonitoring | High (unbounded fetch) |
| H-04 | UserPackageAssignments + PackageQuestions + PackageUserResponses | 3 query × N manual grading sessions | Setiap load MonitoringDetail dengan essay | High (N+1) |
| M-05 | AssessmentSessions | 0-1000 | 2 lokasi berbeda | Medium (no tracking opt) |
| M-06 | ExamActivityLogs + PackageUserResponses | Per sessionId | Setiap klik "Lihat Aktivitas" di MonitoringDetail | Medium (RT redundan) |
| L-02 | UserPackageAssignments + AssessmentPackages + PackageQuestions | Include penuh vs scalar | Setiap load MonitoringDetail | Low (bandwidth) |

---

## 11. Audit `.ToList()` di CMPController.cs (H-02)

### 11.1 Ringkasan

Total `.ToList()` ditemukan: **89 occurrences** (plan awal menyebut 67 — koreksi).
Klasifikasi: **0 blocking DB call**, **89 in-memory (LINQ-to-Objects)**.

Verifikasi regex multi-line `_context\.[A-Za-z]+[\s\S]*?\.ToList\(\)(?!Async)` → **No matches found**. Semua query DB sudah menggunakan varian async.

### 11.2 Kategori Penggunaan `.ToList()` In-Memory

| Kategori | Jumlah | Contoh Line |
|----------|--------|-------------|
| A. Filter/projection pada list yang sudah di-fetch | ±32 | 102, 253, 257, 283, 309, 599, 614, 665, 718 |
| B. `Select(s => s.Id).ToList()` untuk batch param query lanjutan | ±15 | 2567, 2690, 2796, 3148, 3292, 3293, 3360, 3491, 3492 |
| C. `ToDictionary(g => g.Key, g => g.ToList())` grouping result | ±3 | 119, 129, 1433 |
| D. Pagination `Skip().Take().ToList()` in-memory | ±5 | 3569, 3603, 3630, 3665 |
| E. Cascade filter Bagian/Unit/Status pada `allRows` loaded | ±18 | 3594-3598, 3613, 3651-3658, 3692-3696, 3728-3735 |
| F. Group+Sort materialization di memory | ±6 | 507, 510, 558, 562, 3236, 4234 |
| G. Options/Questions ordering dari Include chain | ±8 | 938, 941, 1489, 1497, 3188, 3423 |
| H. Per-session score computation (in-memory aggregation) | ±2 | 3334, 3569 |

### 11.3 Detail Per Baris (Sampel Kunci)

| Line | Pola | Klasifikasi | Keterangan |
|------|------|-------------|-----------|
| 102 | `.ToList()` setelah projection in-memory | **In-memory (A)** | Materialization hasil `Select` |
| 119, 129 | `.GroupBy(...).ToDictionary(..., g => g.ToList())` | **In-memory (C)** | Grouping hasil fetch sebelumnya |
| 253, 257, 283, 309 | `.Select(...).Where(...).ToList()` | **In-memory (A)** | Transform in-memory |
| 507, 510 | `sectionUnitsDict.GroupBy(...).ToDictionary(..., g => ... .ToList())` + master categories | **In-memory (F)** | Build dropdown data |
| 515 | `sectionUnitsDict.Keys.ToList()` | **In-memory (A)** | Keys extraction |
| 599, 614 | `unified.Where(r => r.RecordType == ...).ToList()` | **In-memory (A)** | Filter record type |
| 665, 718 | `.ToList()` setelah GroupBy/Sort on in-memory collection | **In-memory (F)** | Materialization |
| 938, 941 | `packages.SelectMany(p => p.Questions).ToList()` / `q.Options.Select(o => o.Id).ToList()` | **In-memory (G)** | Dari Include chain |
| 1014, 1175 | `.Select(...).ToList()` ViewBag building | **In-memory (A)** | ViewBag projection |
| 1192-1258 | Shuffle algorithm arrays (singlePackageIds, allQuestions, etGroups) | **In-memory (A/G)** | Algorithmic arrays |
| 1433 | `responses.GroupBy(...).ToDictionary(..., g => g.Select(r => ...).ToList())` | **In-memory (C)** | Response grouping |
| 1489, 1497 | `q.Options.OrderBy(o => o.Id).ToList()` | **In-memory (G)** | Options sort |
| 2143-2200 | shuffledQuestionIds filtering / viewmodel building | **In-memory (B)** | Algorithm + param |
| 2536, 2567 | `.ToList()` pada materialized result dan ID extraction | **In-memory (A/B)** | Analytics filter |
| 2556, 2564 | `prePostPostSessions.Where(p => bagianUserIds.Contains(p.UserId)).ToList()` | **In-memory (E)** | Cascade filter Bagian/Unit |
| 2684, 2788, 2793 | `postSessions.Where(p => userIds.Contains(p.UserId)).ToList()` | **In-memory (E)** | Same cascade pattern |
| 2690, 2796 | `postSessions.Select(p => p.LinkedSessionId!.Value).Distinct().ToList()` | **In-memory (B)** | Batch param |
| 2701, 2818, 2912 | `.Select(...).ToList()` trend/summary projection | **In-memory (A)** | Analytics result |
| 3148, 3292, 3293, 3360, 3491, 3492 | `sessions.Select(s => s.Id).ToList()` | **In-memory (B)** | Batch param untuk query ID |
| 3163, 3383 | `responses.GroupBy(r => r.PackageQuestionId).ToList()` | **In-memory (C)** | In-memory grouping |
| 3181-3188 | `question.Options?.ToList()` | **In-memory (G)** | Null-coalesce Options list |
| 3236 | `sessionScores.OrderByDescending(s => s.TotalScore).ToList()` | **In-memory (F)** | Ranking sort |
| 3320, 3334, 3370, 3392, 3423 | Response projection / correctSessionIds / options ordering | **In-memory (A/G)** | Analytics detail build |
| 3569, 3603, 3630, 3665 | `groups.Skip(...).Take(...).ToList()` dll | **In-memory (D)** | Pagination hasil fetch |
| 3594-3598, 3613 | `groups.Where(g => g.Kategori == category).ToList()` (cascade) | **In-memory (E)** | Cascade filter |
| 3651-3658 | `allRows.Where(r => r.Judul == judul).ToList()` lalu cascade Bagian/Unit/Status | **In-memory (E)** | Multi-step filter on allRows |
| 3692-3735 | Repeat cascade pattern (halaman ExportPreview) | **In-memory (E)** | Filter ulang |
| 3776, 3930, 3991 | `.Select(...).ToList()` untuk view rendering | **In-memory (A)** | ViewModel build |
| 4234 | `items.GroupBy(b => b.Kategori ?? "Lainnya").OrderBy(g => g.Key).ToList()` | **In-memory (F)** | Grouping + sort |

### 11.4 Kesimpulan Audit `.ToList()` — CMP

- **0 issue blocking** — tidak perlu perubahan ke `.ToListAsync()`
- Pola dominan: fetch-besar-lalu-cascade-filter-in-memory (kategori E). Terlihat jelas di `ManageCMP`, `ExportCMPPreview`, dll (line 3594-3735). Beban CPU server naik linear dengan ukuran `allRows` — bukan blocking, tapi boros alokasi
- Jika dataset CMP bertumbuh >5.000 rows, disarankan push cascade filter (kategori, subkategori, search, bagian, unit, status) ke level `IQueryable` sebelum `.ToListAsync()`. Tidak urgent
- H-02 direkomendasikan **diturunkan dari High ke Info**

---

## 12. Issue Infrastruktur — Program.cs, Middleware, Organization, Hub

### 12.1 Program.cs — M-01 (No Compression / Response Caching)

Tidak ada query DB di Program.cs; isu ini tentang **data transfer size** yang mempengaruhi wall-time performa end-to-end. DB round-trip tidak berubah.

**Pipeline saat ini (Program.cs:188-199):**
```
UseStaticFiles → UseRouting → UseSession → UseAuthentication → UseAuthorization
   → UseMiddleware<MaintenanceMode> → UseMiddleware<Impersonation> → MapController/MapHub
```

**Tidak ada:**
- `UseResponseCompression()`
- `UseResponseCaching()`
- Static file `Cache-Control` header untuk `/js`, `/css`, `/lib`

**Posisi penyisipan optimal:**
```
... UseStaticFiles → UseRouting → [UseResponseCompression] → UseSession
    → UseAuthentication → UseAuthorization → [UseResponseCaching]
    → UseMiddleware<MaintenanceMode> → UseMiddleware<Impersonation>
```

### 12.2 ImpersonationMiddleware — M-02 (Query per Request)

**Lokasi:** `Middleware/ImpersonationMiddleware.cs:134-142`

**Query saat ini (per request ter-impersonate mode="user"):**

| # | Query | Tabel | Estimasi Latency |
|---|-------|-------|-----------------|
| 1 | `UserManager.FindByIdAsync(targetUserId)` | `AspNetUsers` (PK lookup) | ~2-5 ms |
| 2 | `UserManager.GetRolesAsync(targetUser)` | `AspNetUserRoles JOIN AspNetRoles` (PK/FK) | ~3-8 ms |
| **Total** | | | **~5-13 ms** per request |

**SQL perkiraan:**
```sql
-- Query 1
SELECT * FROM AspNetUsers WHERE Id = @targetUserId;

-- Query 2
SELECT r.Name
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE ur.UserId = @targetUserId;
```

**Frekuensi:**
Selama window impersonation (30 menit default), setiap request non-static ber-authenticated memicu 2 query. Admin flow biasanya 5-15 request per page + polling/AJAX → **10-30 query/menit** selama aktif impersonating.

**Dengan IMemoryCache (TTL 5 menit):**
- 1 query → 1 per 5 menit per target user
- Reduksi: >95% DB round-trip untuk path impersonation mode="user"

### 12.3 OrganizationController.GetOrganizationTree — M-03

**Lokasi:** `Controllers/OrganizationController.cs:56-68`

**SQL saat ini:**
```sql
SELECT Id, Name, ParentId, [Level], DisplayOrder, IsActive
FROM OrganizationUnits
ORDER BY [Level], DisplayOrder, Name
-- Tanpa WHERE, tanpa AsNoTracking (walau projection anonymous otomatis non-tracked)
```

**Frekuensi Akses:**
- Dropdown unit organisasi di beberapa halaman admin
- Tree editor refresh pada ManageOrganization
- AJAX call saat user interaksi dengan unit picker

**Estimasi ukuran response (500 units × 6 kolom):**
- Uncompressed JSON: ~80-150 KB
- Gzipped (M-01): ~25-45 KB
- Dengan IMemoryCache 10 menit: 0 DB round-trip kecuali expired / invalidated

**Tabel yang terlibat:** `OrganizationUnits` (standalone, tidak ada join)

**Juga terdampak (read-only view):**
- `OrganizationController.ManageOrganization` (line 34-53) — Include chain 2-level tanpa `AsNoTracking()`. Projection via Include, EF tetap tracking walau data tidak diubah

### 12.4 AssessmentHub Fire-and-Forget — L-01

**Lokasi:** `Hubs/AssessmentHub.cs:71-91 (LogPageNav), 99-125 (OnConnected), 259-285 (OnDisconnected)`

**Operasi DB per panggilan:**
Setiap fire-and-forget menjalankan 1-2 query:
- `LogPageNav`: 1 INSERT ke `ExamActivityLogs`
- `OnConnectedAsync`: 1 SELECT `AssessmentSessions` + 1 INSERT `ExamActivityLogs` (jika sesi aktif)
- `OnDisconnectedAsync`: 1 SELECT `AssessmentSessions` + 1 INSERT `ExamActivityLogs` (jika sesi aktif)

**DB round-trip tidak bermasalah** — yang dicatat di sini adalah risiko **kehilangan data** saat DB transient error. Tidak mempengaruhi query load DB.

### 12.5 ExcelExportHelper — I-01

Bukan issue DB. Catatan arsitektur: ClosedXML memerlukan stream random-access → MemoryStream buffered mandatory. Tidak ada query DB langsung di helper ini (dipanggil setelah query di controller).

---

## 13. Ringkasan FINAL — DB Round-Trips per Halaman Kunci

### Target kondisi optimal setelah semua rekomendasi diterapkan:

| Halaman / Endpoint | RT saat ini | RT optimal | Penghematan |
|--------------------|-------------|-----------|-------------|
| ManageWorkers | 6 | 3 | 50% |
| GetCascadeOptions (CDP) | 2 (blocking) | 2 (async paralel) | Wall-time ~50% |
| GetAllWorkersHistory | 3 (full fetch) | 3 (filtered) | Data transfer >90% |
| ManageAssessment | 2 + in-memory group | 2 + SQL group | CPU in-memory ~70% |
| AssessmentMonitoring | 1 (unbounded) | 1 (+ pagination) | Memory 50-80% |
| AssessmentMonitoringDetail (50 essay) | 156 | 7 | 95% |
| GetActivityLog (AJAX) | 4 | 2 (wall-time) | 50% |
| GetOrganizationTree | 1 (per call) | ~0 (cached 10 min) | ~95% over 10 min window |
| ImpersonationMiddleware (user mode) | 2 per request | ~0 (cached 5 min) | >95% per window |

### Tabel Impact Matrix (Step 1D)

| Issue | Tabel Terdampak | Estimasi Rows | Frekuensi | Severity |
|-------|----------------|---------------|----------|---------|
| M-01 | — (response transfer) | Bervariasi | Semua request | Medium (network) |
| M-02 | AspNetUsers + AspNetUserRoles + AspNetRoles | 1 user × 1-5 roles | Per request impersonation-user | Medium |
| M-03 | OrganizationUnits | 100-500 units | Per AJAX panggilan tree | Medium |
| H-02 | CMPController dataset | 89 .ToList() in-memory | Seluruh analytics & view | ~~High~~ → Info |
| L-01 | ExamActivityLogs + AssessmentSessions | Per event | Per page nav / connect / disconnect | Low (data loss risk) |
| I-01 | — | — | Setiap export Excel | Info arsitektur |

---

# Step 3 — Database Runtime Analysis Preparation

**Tanggal:** 17 April 2026
**Status:** ⏭ SKIPPED — Di-skip per keputusan. Static + runtime browser analysis cukup untuk menentukan prioritas fix.
**Scope:** Bagian ini berisi SQL pattern yang diinfer secara static (tanpa eksekusi DB). Berguna sebagai referensi jika EF logging diaktifkan di masa depan.

## 14. Expected SQL Patterns per Issue (Static Inference)

Bagian ini mendokumentasikan **SQL yang akan di-generate EF Core** untuk setiap issue kritis berdasarkan pembacaan kode, TANPA menjalankan query. Berguna untuk:
- Dibandingkan dengan actual SQL saat EF logging aktif nanti
- Menjadi dasar prediksi DB cost
- Memudahkan DBA review tanpa perlu akses aplikasi

**Catatan dialek:** EF Core menghasilkan SQL dengan syntax spesifik per provider:
- **SQL Server** (dev): `[brackets]`, `TOP`, `OFFSET...FETCH NEXT`
- **SQLite** (localhost): tidak ada brackets, `LIMIT...OFFSET`
- Pola logika identik — yang berbeda hanya syntax detail

---

### 14.1 C-01 — `CDPController.GetCascadeOptions` (lines 313-318)

**Kode EF:**
```csharp
var units = string.IsNullOrEmpty(section)
    ? new List<string>()
    : await _context.GetUnitsForSectionAsync(section);      // async OK

var categories = _context.ProtonTracks                      // BLOCKING .ToList()
    .Select(t => t.TrackType).Distinct().OrderBy(t => t).ToList();

var tracks = _context.ProtonTracks                          // BLOCKING .ToList()
    .OrderBy(t => t.Urutan).Select(t => t.DisplayName).ToList();
```

**Expected SQL (SQL Server):**
```sql
-- Query 1: categories (BLOCKING sync execution)
SELECT DISTINCT [t].[TrackType]
FROM [ProtonTracks] AS [t]
ORDER BY [t].[TrackType];

-- Query 2: tracks (BLOCKING sync execution)
SELECT [t].[DisplayName]
FROM [ProtonTracks] AS [t]
ORDER BY [t].[Urutan];
```

**Karakteristik:**
- 2 query sequential, **synchronous** — thread pool thread ter-block selama execution
- Tabel `ProtonTracks` kecil (<100 rows) → DB cost rendah per query
- **Issue** bukan di DB cost, tapi di **thread pool utilization** pada beban concurrent request
- Tidak ada WHERE clause — full table scan kecil

**Alternatif optimal (non-blocking + parallel):**
```sql
-- Dua query bisa dijalankan paralel via Task.WhenAll
-- SQL tetap sama persis, hanya execution mode berubah ke async
```

**Expected DB time per query:** <5 ms di dataset kecil. Fix sama sekali tidak memengaruhi DB cost — hanya thread pool yang diperbaiki.

---

### 14.2 C-02 — `WorkerController.Index` Stats Count (lines 108-111)

**Kode EF:**
```csharp
ViewBag.TotalUsers  = await _context.Users.CountAsync();
ViewBag.AdminCount  = await _context.Users.CountAsync(u => u.RoleLevel == 1);
ViewBag.HcCount     = await _context.Users.CountAsync(u => u.RoleLevel == 2);
ViewBag.WorkerCount = await _context.Users.CountAsync(u => u.RoleLevel >= 5);
```

**Expected SQL saat ini (4 round-trip):**
```sql
SELECT COUNT(*) FROM [AspNetUsers];
SELECT COUNT(*) FROM [AspNetUsers] WHERE [RoleLevel] = 1;
SELECT COUNT(*) FROM [AspNetUsers] WHERE [RoleLevel] = 2;
SELECT COUNT(*) FROM [AspNetUsers] WHERE [RoleLevel] >= 5;
```

**Karakteristik:**
- 4 DB round-trip terpisah per page load
- Setiap query full table scan pada `AspNetUsers` (kecuali ada index di `RoleLevel`)
- Di 530 users: 4 × ~5-10 ms ≈ 20-40 ms total (LAN overhead dominan)

**Expected SQL optimal (1 round-trip):**
```sql
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN [RoleLevel] = 1 THEN 1 ELSE 0 END) AS AdminCount,
    SUM(CASE WHEN [RoleLevel] = 2 THEN 1 ELSE 0 END) AS HcCount,
    SUM(CASE WHEN [RoleLevel] >= 5 THEN 1 ELSE 0 END) AS WorkerCount
FROM [AspNetUsers];
```

**Benefit:** -75% round-trip. Execution time di DB level kurang lebih sama (single pass), tapi network latency × 3 dihilangkan.

---

### 14.3 C-03 — `WorkerController.Index` Users + UserRoles Fetch (lines 92, 95-100)

**Kode EF:**
```csharp
// Line 92 — Users fetch (filtered, no AsNoTracking)
var users = await query.OrderBy(u => u.FullName).ToListAsync();

// Lines 95-100 — UserRoles JOIN Roles TANPA filter userIds
var userRolesDict = (await _context.UserRoles
    .Join(_context.Roles,
        ur => ur.RoleId,
        r  => r.Id,
        (ur, r) => new { ur.UserId, r.Name })
    .ToListAsync())
    .GroupBy(x => x.UserId)
    .ToDictionary(g => g.Key, g => g.First().Name ?? "No Role");
```

**Expected SQL saat ini:**
```sql
-- Query A: Users (tracked, no AsNoTracking)
SELECT [u].[Id], [u].[FullName], [u].[Email], [u].[NIP], [u].[Section],
       [u].[Unit], [u].[RoleLevel], [u].[IsActive], /* ... semua kolom ... */
FROM [AspNetUsers] AS [u]
WHERE /* filter kondisional: searchString, section, unit, roleLevel, IsActive */
ORDER BY [u].[FullName];

-- Query B: UserRoles JOIN Roles — FETCH SELURUH TABEL
SELECT [ur].[UserId], [r].[Name]
FROM [AspNetUserRoles] AS [ur]
INNER JOIN [AspNetRoles] AS [r] ON [ur].[RoleId] = [r].[Id];
-- TIDAK ADA WHERE untuk userIds terfilter
```

**Karakteristik di 530 users:**
- Query A: returns 530 rows × ~15 kolom (termasuk snapshot tracking → 500+ KB memori aplikasi)
- Query B: returns **semua** rows UserRoles × Roles (mungkin 500-600 rows kalau 1 role/user)
- Memory transfer redundan: Query B seharusnya hanya perlu data untuk 530 userId yang di-load

**Expected SQL optimal:**
```sql
-- Query A (dengan AsNoTracking — no SQL change, only EF behavior)
SELECT [u].[Id], [u].[FullName], /* ... */
FROM [AspNetUsers] AS [u]
WHERE /* filter */
ORDER BY [u].[FullName];

-- Query B: filter di DB dengan IN (userIds)
SELECT [ur].[UserId], [r].[Name]
FROM [AspNetUserRoles] AS [ur]
INNER JOIN [AspNetRoles] AS [r] ON [ur].[RoleId] = [r].[Id]
WHERE [ur].[UserId] IN (@userId_0, @userId_1, ..., @userId_529);
```

**Benefit:** Data transfer Query B berkurang proporsional dengan rasio users_loaded/total_users_with_roles. Di localhost 12 users, impact kecil. Di dev 530 users, moderate (tergantung penggunaan role by user lain).

---

### 14.4 C-04 — `WorkerDataService.GetAllWorkersHistory` (lines 85-172)

**Kode EF (4 query total — 1 sudah optimal, 3 full fetch):**
```csharp
// Query 0 — SUDAH OPTIMAL (GroupBy aggregate, tidak memuat rows)
var archivedCounts = await _context.AssessmentAttemptHistory
    .AsNoTracking()
    .GroupBy(h => new { h.UserId, h.Title })
    .Select(g => new { g.Key.UserId, g.Key.Title, Count = g.Count() })
    .ToListAsync();

// Query 1 — FULL FETCH (issue)
var archivedAttempts = await _context.AssessmentAttemptHistory
    .AsNoTracking()
    .Include(h => h.User)
    .ToListAsync();

// Query 2 — Filter hanya Status (issue)
var currentCompleted = await _context.AssessmentSessions
    .AsNoTracking()
    .Include(a => a.User)
    .Where(a => a.Status == "Completed")
    .ToListAsync();

// Query 3 — FULL FETCH (issue)
var trainings = await _context.TrainingRecords
    .AsNoTracking()
    .Include(t => t.User)
    .ToListAsync();
```

**Expected SQL:**
```sql
-- Query 0 (optimal) — aggregate, tidak memuat rows
SELECT [h].[UserId], [h].[Title], COUNT(*) AS [Count]
FROM [AssessmentAttemptHistory] AS [h]
GROUP BY [h].[UserId], [h].[Title];

-- Query 1 (FULL FETCH, joined User) — issue
SELECT [h].[Id], [h].[UserId], [h].[Title], [h].[StartedAt], [h].[CompletedAt],
       [h].[Score], [h].[IsPassed], [h].[ArchivedAt], [h].[AttemptNumber],
       /* semua kolom AssessmentAttemptHistory */
       [u].[Id], [u].[FullName], [u].[NIP], [u].[Section], [u].[Unit],
       /* semua kolom User */
FROM [AssessmentAttemptHistory] AS [h]
LEFT JOIN [AspNetUsers] AS [u] ON [h].[UserId] = [u].[Id];
-- TIDAK ADA WHERE clause — fetch seluruh tabel history

-- Query 2 (filter Status saja, joined User) — issue
SELECT [a].[Id], [a].[UserId], [a].[Title], [a].[Category], [a].[Schedule],
       [a].[Status], [a].[Score], [a].[IsPassed], [a].[CompletedAt],
       /* semua kolom AssessmentSessions */
       [u].[Id], [u].[FullName], /* semua kolom User */
FROM [AssessmentSessions] AS [a]
LEFT JOIN [AspNetUsers] AS [u] ON [a].[UserId] = [u].[Id]
WHERE [a].[Status] = N'Completed';
-- Bisa ribuan rows jika banyak sesi selesai

-- Query 3 (FULL FETCH, joined User) — issue
SELECT [t].[Id], [t].[UserId], [t].[Judul], [t].[Penyelenggara],
       [t].[Tanggal], [t].[TanggalMulai], /* semua kolom TrainingRecords */
       [u].[Id], [u].[FullName], /* semua kolom User */
FROM [TrainingRecords] AS [t]
LEFT JOIN [AspNetUsers] AS [u] ON [t].[UserId] = [u].[Id];
-- TIDAK ADA WHERE clause
```

**Karakteristik di Dev (530 users):**
- Query 1: 4,789 archived rows × (kolom history + kolom user lengkap) → **~6-7 MB** data transfer
- Query 2: potensi ribuan rows Completed
- Query 3: seluruh training records × kolom user
- Ketiga query sequential, masing-masing returns cardinality full
- **Dampak runtime dikonfirmasi di Fase 2B**: total HTML 7.96 MB = akumulasi dari 3 fetch ini + grouping di-memory

**Expected SQL optimal (dengan filter UserId + date range):**
```sql
-- Jika dipanggil untuk single user (context spesifik)
SELECT /* projection explicit (bukan SELECT *) */
FROM [AssessmentAttemptHistory] AS [h]
LEFT JOIN [AspNetUsers] AS [u] ON [h].[UserId] = [u].[Id]
WHERE [h].[UserId] = @userId
  AND [h].[ArchivedAt] >= DATEADD(year, -1, GETUTCDATE());

-- Jika dipanggil untuk semua users tapi dengan time window
SELECT /* projection explicit */
FROM [AssessmentAttemptHistory] AS [h]
LEFT JOIN [AspNetUsers] AS [u] ON [h].[UserId] = [u].[Id]
WHERE [h].[ArchivedAt] >= DATEADD(year, -1, GETUTCDATE());
```

**Benefit:** Data transfer 7.96 MB → estimasi ~500 KB - 1 MB (tergantung window). Pattern untuk Query 2 dan Query 3 sama.

---

### 14.5 M-03 — `OrganizationController.GetOrganizationTree` (lines 56-68)

**Kode EF:**
```csharp
var units = await _context.OrganizationUnits
    .OrderBy(u => u.Level).ThenBy(u => u.DisplayOrder).ThenBy(u => u.Name)
    .Select(u => new { u.Id, u.Name, u.ParentId, u.Level, u.DisplayOrder, u.IsActive })
    .ToListAsync();
return Json(units);
```

**Expected SQL:**
```sql
SELECT [o].[Id], [o].[Name], [o].[ParentId], [o].[Level],
       [o].[DisplayOrder], [o].[IsActive]
FROM [OrganizationUnits] AS [o]
ORDER BY [o].[Level], [o].[DisplayOrder], [o].[Name];
```

**Karakteristik:**
- Full table scan (tidak ada WHERE)
- Projection sudah minimal (6 kolom) — tidak ada Include overhead
- EF Core modern otomatis tidak tracking anonymous projection → `AsNoTracking()` redundan di sini
- Di Dev 26 units: <10ms tanpa masalah
- **Concurrency issue** muncul di 2+ paralel call (dikonfirmasi Fase 2B: 662ms wall time)

**Penyebab concurrency serialisasi:**
- Bukan query plan (simple SELECT), melainkan:
  1. DbContext scope serialization (setiap request pakai context baru, tapi connection pool mungkin capped)
  2. SQL Server Express default max concurrent connection per database
  3. Parameter sniffing kalau ada filter (di sini tidak ada)

**Expected SQL dengan cache IMemoryCache:**
```sql
-- Hanya dieksekusi 1× per 10 menit (TTL)
-- Query text sama persis, tapi frekuensi execution dari 10x/menit → 0.1x/menit
```

**Benefit runtime:** 662ms wall → ~5ms cache hit. Mengurangi DB connection pool pressure.

---

### 14.6 M-06 — `GetActivityLog` (lines 4956-4988)

**Kode EF (4 query — Query 4 redundan):**
```csharp
// Query 1 — load session
var session = await _context.AssessmentSessions
    .FirstOrDefaultAsync(s => s.Id == sessionId);

// Query 2 — fetch events (sudah ORDER BY Timestamp)
var events = await _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .OrderBy(l => l.Timestamp)
    .Select(l => new { l.EventType, l.Detail, TimestampUtc = l.Timestamp })
    .ToListAsync();

// Query 3 — count answered
var totalAnswered = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == sessionId);

// Query 4 — max timestamp (REDUNDAN — bisa dihitung dari events)
var lastEventTime = await _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .MaxAsync(l => (DateTime?)l.Timestamp);
```

**Expected SQL saat ini (4 round-trip):**
```sql
-- Query 1
SELECT TOP(1) [a].[Id], [a].[UserId], [a].[Title], [a].[Status],
              [a].[StartedAt], [a].[CompletedAt], [a].[DurationMinutes],
              /* semua kolom AssessmentSessions */
FROM [AssessmentSessions] AS [a]
WHERE [a].[Id] = @sessionId;

-- Query 2
SELECT [l].[EventType], [l].[Detail], [l].[Timestamp]
FROM [ExamActivityLogs] AS [l]
WHERE [l].[SessionId] = @sessionId
ORDER BY [l].[Timestamp];

-- Query 3
SELECT COUNT(*)
FROM [PackageUserResponses] AS [r]
WHERE [r].[AssessmentSessionId] = @sessionId;

-- Query 4 (REDUNDAN dengan Query 2)
SELECT MAX([l].[Timestamp])
FROM [ExamActivityLogs] AS [l]
WHERE [l].[SessionId] = @sessionId;
```

**Karakteristik:**
- 4 DB round-trip per AJAX call
- Query 2 dan Query 4 hit tabel `ExamActivityLogs` dua kali dengan filter identik
- Jika tabel besar, scan ulang = waste I/O

**Expected SQL optimal (hilangkan Query 4, reuse events):**
```csharp
// Di kode: hitung dari list yang sudah di-load
DateTime? lastEventTime = events.LastOrDefault()?.TimestampUtc;
```

Setelah optimasi tingkat 2 (paralel Query 2 + Query 3 via Task.WhenAll):
```
Wall-time: max(Q1, Q2||Q3) = 2 round-trip wall-time (dari 4)
```

**Benefit:** -50% round-trip wall-time, -25% total DB work.

---

### 14.7 Ringkasan Pattern SQL per Issue

| Issue | Jumlah Query Saat Ini | Pola Masalah | Target Fix |
|-------|----------------------|--------------|-----------|
| C-01 | 2 sync blocking | Thread pool block, SQL kecil | Async + Task.WhenAll |
| C-02 | 4 round-trip | Redundant count scans | 1 aggregate query |
| C-03 | 2 queries | Query B unfiltered full tabel | Tambah `WHERE UserId IN (...)` |
| C-04 | 4 queries (1 optimal + 3 full fetch) | Query 1, 2, 3 tanpa date/user filter | Tambah filter UserId/date range |
| M-03 | 1 query per call | Full scan + no cache (concurrent hit) | IMemoryCache TTL 10 menit |
| M-06 | 4 round-trip | Query 4 redundant + serial | Reuse events + Task.WhenAll |

**Total DB round-trip yang bisa dihemat (kalau semua fix diterapkan):**
- C-02: 4 → 1 = **-3 RT**
- C-04: 4 → 4 (sama count, tapi data transfer -90%+)
- M-03: 10/menit → 0.1/menit = **-99% frekuensi**
- M-06: 4 → 2-3 wall-time = **-25-50% RT**

Catatan: Section 15 (index candidate) dan Section 16 (SQL templates) akan dilanjutkan di langkah berikutnya.

---

## 15. Index Candidate List (Untuk DBA Review)

Bagian ini mendokumentasikan **index yang sudah ada** vs **index candidate yang direkomendasikan** berdasarkan pembacaan kode EF Fluent API di `Data/ApplicationDbContext.cs` + pola query issue Step 1-2. Tidak ada CREATE INDEX dijalankan — ini hanya rekomendasi untuk DBA review.

### 13.1 Existing Indexes (Dari `ApplicationDbContext.OnModelCreating`)

| Tabel | Index Existing | Tipe | Line Ref |
|-------|----------------|------|----------|
| `AssessmentSessions` | `UserId` | Non-unique | 178 |
| `AssessmentSessions` | `(UserId, Status)` composite | Non-unique | 179 |
| `AssessmentSessions` | `Schedule` | Non-unique | 180 |
| `AssessmentSessions` | `AccessToken` | Non-unique | 181 |
| `AssessmentSessions` | `NomorSertifikat` filtered | Unique (where NOT NULL) | 199-202 |
| `AssessmentAttemptHistory` | `UserId` | Non-unique | 500 |
| `AssessmentAttemptHistory` | `(UserId, Title)` composite | Non-unique | 501 |
| `ExamActivityLogs` | `SessionId` | Non-unique | 551 |
| `ExamActivityLogs` | `Timestamp` | Non-unique | 552 |
| `OrganizationUnits` | `(ParentId, Name)` composite | Unique | 559 |
| `OrganizationUnits` | `(ParentId, DisplayOrder)` composite | Non-unique | 560 |
| `PackageQuestions` | `AssessmentPackageId` | Non-unique | 426 |
| `PackageOptions` | `PackageQuestionId` | Non-unique | 438 |
| `UserPackageAssignments` | `(AssessmentSessionId, UserId)` composite | Unique | 456-457 |
| `UserPackageAssignments` | `UserId` | Non-unique | 459 |
| `PackageUserResponses` | `(AssessmentSessionId, PackageQuestionId)` composite | Non-unique | 480 |
| `ProtonTracks` | `(TrackType, TahunKe)` composite | Unique | 304 |

**Tabel TANPA index yang dikonfigurasi explicit** (selain PK/FK default):
- `TrainingRecords` — tidak ada `HasIndex()`
- `AspNetUsers` (ApplicationUser) — tidak ada index custom di OnModelCreating

### 13.2 Index Candidate per Issue

#### 13.2.1 C-02 — Stats Count by RoleLevel

**Query pattern:**
```sql
SELECT COUNT(*) FROM [AspNetUsers] WHERE [RoleLevel] = 1;
-- Plus 3 COUNT lain dengan filter RoleLevel berbeda
```

**Existing:** Tidak ada index pada `RoleLevel`.

**Candidate:**
```sql
CREATE NONCLUSTERED INDEX IX_AspNetUsers_RoleLevel
  ON [AspNetUsers] ([RoleLevel]);
```

**Prioritas:** **Low-Medium**. Kalau fix C-02 (aggregate single query) sudah diterapkan, index ini jadi kurang kritis karena EF akan gunakan single table scan untuk aggregate. Tetap berguna jika ada query `RoleLevel` lain di sistem.

**Note:** Evaluasi dulu selectivity — kalau mayoritas user adalah RoleLevel 6 (Coachee), index jadi tidak efektif untuk filter `= 6`. Berguna terutama untuk RoleLevel langka (Admin=1, HC=2).

#### 13.2.2 C-03 — UserRoles Join dengan Filter UserId

**Query pattern (setelah fix ditambah WHERE UserId IN):**
```sql
SELECT [ur].[UserId], [r].[Name]
FROM [AspNetUserRoles] AS [ur]
INNER JOIN [AspNetRoles] AS [r] ON [ur].[RoleId] = [r].[Id]
WHERE [ur].[UserId] IN (...);
```

**Existing:** ASP.NET Identity default pada `AspNetUserRoles` = PK `(UserId, RoleId)`. Lookup by UserId sudah efisien.

**Candidate:** Tidak ada. PK sudah cover.

**Prioritas:** **N/A** (tidak perlu index baru).

#### 13.2.3 C-04 — AssessmentSessions Filter Status

**Query pattern:**
```sql
SELECT [a].*, [u].*
FROM [AssessmentSessions] AS [a]
LEFT JOIN [AspNetUsers] AS [u] ON [a].[UserId] = [u].[Id]
WHERE [a].[Status] = N'Completed';
```

**Existing:** `(UserId, Status)` composite ada, tapi tidak membantu query dengan filter `Status` saja (leading key harus `UserId`).

**Candidate:**
```sql
CREATE NONCLUSTERED INDEX IX_AssessmentSessions_Status_CompletedAt
  ON [AssessmentSessions] ([Status], [CompletedAt] DESC);
```

**Prioritas:** **Medium**. Jika fix C-04 menambahkan filter date range pada `ArchivedAt` atau `CompletedAt`, composite `(Status, CompletedAt)` akan sangat membantu query filter "Completed dalam 1 tahun terakhir".

**Alternatif:** Kalau aplikasi sering filter by `(Title, Status)`, pertimbangkan:
```sql
CREATE NONCLUSTERED INDEX IX_AssessmentSessions_Title_Status_Schedule
  ON [AssessmentSessions] ([Title], [Status], [Schedule]);
```

#### 13.2.4 C-04 — TrainingRecords (Tidak Ada Index)

**Query pattern:**
```sql
SELECT [t].*, [u].*
FROM [TrainingRecords] AS [t]
LEFT JOIN [AspNetUsers] AS [u] ON [t].[UserId] = [u].[Id];
-- Full table scan saat ini
```

**Existing:** Tidak ada HasIndex. Hanya FK UserId default.

**Candidate:**
```sql
CREATE NONCLUSTERED INDEX IX_TrainingRecords_UserId_TanggalMulai
  ON [TrainingRecords] ([UserId], [TanggalMulai] DESC)
  INCLUDE ([Judul], [Penyelenggara], [Tanggal]);
```

**Prioritas:** **High**. Kalau fix C-04 menambahkan filter `WHERE UserId = @id AND TanggalMulai >= @dateFrom`, index ini cover query. `INCLUDE` kolom non-key mengurangi bookmark lookup.

#### 13.2.5 H-03 — AssessmentMonitoring 7-day Window

**Query pattern:**
```sql
SELECT /* ... */ FROM [AssessmentSessions] AS [a]
WHERE COALESCE([a].[ExamWindowCloseDate], [a].[Schedule]) >= @sevenDaysAgo
ORDER BY [a].[Schedule] DESC;
```

**Existing:** `HasIndex(Schedule)` ada.

**Masalah:** Penggunaan `COALESCE` **mencegah EF memakai index** pada `Schedule`. Ini adalah **sargability issue**.

**Candidate (rekomendasi refactor query + index):**

Opsi A — Tambah computed column + index:
```sql
ALTER TABLE [AssessmentSessions]
  ADD [EffectiveWindowDate] AS (ISNULL([ExamWindowCloseDate], [Schedule])) PERSISTED;

CREATE NONCLUSTERED INDEX IX_AssessmentSessions_EffectiveWindowDate
  ON [AssessmentSessions] ([EffectiveWindowDate] DESC);
```

Opsi B — Refactor query EF Core menjadi 2 branch:
```csharp
.Where(a => (a.ExamWindowCloseDate == null && a.Schedule >= sevenDaysAgo)
         || (a.ExamWindowCloseDate != null && a.ExamWindowCloseDate >= sevenDaysAgo))
```

**Prioritas:** **Medium** (tergantung frekuensi hit AssessmentMonitoring + ukuran dataset).

#### 13.2.6 H-04 — Essay Grading PackageQuestions Filter

**Query pattern:**
```sql
SELECT /* ... */ FROM [PackageQuestions] AS [q]
WHERE [q].[Id] IN (...shuffledIds...) AND [q].[QuestionType] = N'Essay';
```

**Existing:** PK `Id` ada. Tidak ada index pada `QuestionType`.

**Candidate:**
```sql
CREATE NONCLUSTERED INDEX IX_PackageQuestions_QuestionType_Id
  ON [PackageQuestions] ([QuestionType], [Id])
  INCLUDE ([QuestionText], [Rubrik], [ScoreValue]);
```

**Prioritas:** **Low**. Dalam praktek, filter `Id IN (...)` sudah efisien via PK. `QuestionType = 'Essay'` adalah post-filter kecil. Index ini marginal benefit.

#### 13.2.7 M-03 — GetOrganizationTree Sort

**Query pattern:**
```sql
SELECT [Id], [Name], [ParentId], [Level], [DisplayOrder], [IsActive]
FROM [OrganizationUnits]
ORDER BY [Level], [DisplayOrder], [Name];
```

**Existing:** `(ParentId, DisplayOrder)` dan `(ParentId, Name)` — tidak match dengan ORDER BY.

**Candidate:**
```sql
CREATE NONCLUSTERED INDEX IX_OrganizationUnits_Level_DisplayOrder_Name
  ON [OrganizationUnits] ([Level], [DisplayOrder], [Name])
  INCLUDE ([ParentId], [IsActive]);
```

**Prioritas:** **Low**. Dataset 26-500 units kecil, sort in-memory cepat. Index benefit minor. Fix M-03 pakai IMemoryCache (lebih besar impact) membuat index ini kurang urgent.

#### 13.2.8 M-06 — GetActivityLog ORDER BY

**Query pattern:**
```sql
SELECT [EventType], [Detail], [Timestamp]
FROM [ExamActivityLogs]
WHERE [SessionId] = @sessionId
ORDER BY [Timestamp];
```

**Existing:** `SessionId` single + `Timestamp` single — tidak composite.

**Candidate:**
```sql
-- Drop existing dan buat composite
DROP INDEX IF EXISTS IX_ExamActivityLogs_SessionId ON [ExamActivityLogs];
CREATE NONCLUSTERED INDEX IX_ExamActivityLogs_SessionId_Timestamp
  ON [ExamActivityLogs] ([SessionId], [Timestamp])
  INCLUDE ([EventType], [Detail]);
```

**Prioritas:** **Medium**. Query ini dipanggil setiap klik "Lihat Aktivitas" di Monitoring. Composite + INCLUDE menghilangkan bookmark lookup, membuat read 100% dari index.

**Catatan:** Index `Timestamp` single yang existing (line 552) **mungkin sebaiknya di-drop** kalau tidak ada query lain yang filter by Timestamp saja — single column index yang tidak terpakai = overhead write tanpa benefit read.

### 13.3 Ringkasan Prioritas Index Candidate

| # | Tabel | Index Candidate | Prioritas | Supporting Issue |
|---|-------|-----------------|-----------|------------------|
| 1 | `TrainingRecords` | `(UserId, TanggalMulai DESC) INCLUDE (...)` | **High** | C-04 |
| 2 | `AssessmentSessions` | `(Status, CompletedAt DESC)` atau `(Title, Status, Schedule)` | Medium | C-04 |
| 3 | `ExamActivityLogs` | `(SessionId, Timestamp) INCLUDE (...)` + drop single `Timestamp` | Medium | M-06 |
| 4 | `AssessmentSessions` | Computed `EffectiveWindowDate` + index | Medium | H-03 |
| 5 | `AspNetUsers` | `RoleLevel` | Low-Medium | C-02 |
| 6 | `PackageQuestions` | `(QuestionType, Id) INCLUDE (...)` | Low | H-04 |
| 7 | `OrganizationUnits` | `(Level, DisplayOrder, Name) INCLUDE (...)` | Low | M-03 |

### 13.4 Index yang Mungkin Underused (Calon Drop)

Perlu validasi dengan `sys.dm_db_index_usage_stats` (template di Section 16) sebelum drop:

| Tabel | Index | Kenapa Curiga |
|-------|-------|---------------|
| `ExamActivityLogs` | `Timestamp` (single) | Kalah dengan composite `(SessionId, Timestamp)` |
| `AssessmentSessions` | `AccessToken` | Dipakai verifikasi token — **jangan drop** |
| `AssessmentSessions` | `Schedule` | Sering kalah dengan COALESCE — verifikasi penggunaan |

### 13.5 Catatan Penting untuk DBA

1. **Jangan langsung CREATE INDEX di produksi** — uji di dev dulu, ukur dampak fill factor, rebuild cost
2. **Monitor `sys.dm_db_index_usage_stats`** setelah deploy untuk validasi index benar-benar dipakai
3. **Consider INCLUDE** untuk kolom yang sering di-SELECT agar read 100% dari index tanpa bookmark lookup
4. **Maintenance**: tambahan 7 index = overhead write (INSERT/UPDATE) pada tabel terkait. Kalau tabel sering di-write (mis. `AssessmentSessions`), trade-off read vs write harus diukur
5. **Filtered index** untuk kolom nullable yang sebagian besar NULL bisa lebih efisien (lihat existing `IX_AssessmentSessions_NomorSertifikat_Unique` sebagai contoh)

Catatan: Section 16 (SQL templates) dan Section 17 (validation guide) akan dilanjutkan di langkah berikutnya.

---

## 16. Diagnostic SQL Templates — Ready to Run When Needed

Bagian ini berisi **5 SQL/script template siap-pakai** untuk validasi runtime DB di masa depan. **Template ini tidak dijalankan sekarang** — hanya didokumentasikan agar DBA atau engineer bisa copy-paste langsung saat butuh validasi.

**Konteks penggunaan:**
- T1-T3 untuk SQL Server dev (`10.55.3.3\SQLEXPRESS` → `HcPortalDB_Dev`) — butuh credential minimal `VIEW SERVER STATE` atau equivalent
- T4 untuk SQLite localhost (`HcPortal.db` file) — butuh SQLite CLI atau DB Browser
- T5 untuk parsing log EF Core setelah logging diaktifkan

**Syntax highlighting:** query SQL Server pakai dialek T-SQL. SQLite pakai dialek SQLite 3.

---

### T1 — Missing Index Detection (SQL Server)

**Tujuan:** Identifikasi index yang SQL Server sarankan untuk dibuat berdasarkan query plan aktual dari cache.

**Prasyarat:**
- Akses SSMS ke `10.55.3.3\SQLEXPRESS`
- Permission: minimal `VIEW SERVER STATE` dan `VIEW DATABASE STATE` di database `HcPortalDB_Dev`

**Query:**
```sql
USE HcPortalDB_Dev;

SELECT TOP 20
    CONVERT(decimal(10, 2),
        migs.user_seeks * migs.avg_total_user_cost * (migs.avg_user_impact * 0.01)
    ) AS improvement_score,
    migs.user_seeks,
    migs.user_scans,
    migs.avg_total_user_cost,
    migs.avg_user_impact,
    mid.statement AS table_name,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    'CREATE INDEX IX_Missing_' + REPLACE(REPLACE(REPLACE(mid.statement, '[HcPortalDB_Dev].', ''), '[dbo].', ''), '[', '') +
    '_' + REPLACE(REPLACE(ISNULL(mid.equality_columns, ''), '[', ''), '], ', '_') +
    ' ON ' + mid.statement +
    ' (' + ISNULL(mid.equality_columns, '')
         + CASE WHEN mid.equality_columns IS NOT NULL
                 AND mid.inequality_columns IS NOT NULL THEN ', ' ELSE '' END
         + ISNULL(mid.inequality_columns, '') + ')'
         + CASE WHEN mid.included_columns IS NOT NULL
                THEN ' INCLUDE (' + mid.included_columns + ')' ELSE '' END AS create_index_ddl
FROM sys.dm_db_missing_index_group_stats migs
INNER JOIN sys.dm_db_missing_index_groups mig
    ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid
    ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY improvement_score DESC;
```

**Cara baca hasil:**
- **improvement_score** — skor komposit (seeks × cost × impact). Makin tinggi makin direkomendasikan
- **user_seeks** — berapa kali query plan butuh index ini (seek efficient)
- **user_scans** — berapa kali query plan fallback ke scan (tidak efficient)
- **table_name** — `[HcPortalDB_Dev].[dbo].[NamaTabel]`
- **equality_columns** — kolom untuk filter `=` (lead key index)
- **inequality_columns** — kolom untuk filter `<`, `>`, `BETWEEN`
- **included_columns** — kolom yang di-SELECT (kandidat INCLUDE)
- **create_index_ddl** — template CREATE INDEX siap pakai

**Kapan dijalankan:**
- Setelah ada beban production-like (minimal 1-2 hari aktivitas normal)
- Sebelum + sesudah fix untuk membandingkan rekomendasi

**Batasan:**
- DMV cache clear saat SQL Server restart → skor reset
- Hanya merekam query yang sudah di-compile dan ter-cache
- Tidak aware dengan semantik bisnis — DBA tetap review sebelum CREATE INDEX

---

### T2 — Historical Query Performance (SQL Server)

**Tujuan:** Top slow queries + query dengan CPU/IO tertinggi. Validate query mana yang butuh optimasi.

**Prasyarat:**
- Akses SSMS ke `HcPortalDB_Dev`
- Permission `VIEW SERVER STATE`

**Query:**
```sql
USE HcPortalDB_Dev;

SELECT TOP 30
    qs.execution_count,
    qs.total_worker_time / qs.execution_count AS avg_cpu_us,
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_us,
    qs.total_logical_reads / qs.execution_count AS avg_reads,
    qs.total_physical_reads / qs.execution_count AS avg_physical_reads,
    qs.creation_time,
    qs.last_execution_time,
    SUBSTRING(qt.text,
        qs.statement_start_offset / 2 + 1,
        (CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
         END - qs.statement_start_offset) / 2 + 1
    ) AS query_text,
    qp.query_plan
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
WHERE qt.text NOT LIKE '%sys.dm_%'  -- exclude DMV meta-queries
  AND (
        qt.text LIKE '%AssessmentSessions%'
     OR qt.text LIKE '%AssessmentAttemptHistory%'
     OR qt.text LIKE '%TrainingRecords%'
     OR qt.text LIKE '%OrganizationUnits%'
     OR qt.text LIKE '%ExamActivityLogs%'
     OR qt.text LIKE '%PackageUserResponses%'
     OR qt.text LIKE '%AspNetUsers%'
     OR qt.text LIKE '%AspNetUserRoles%'
  )
ORDER BY avg_elapsed_us DESC;
```

**Cara baca hasil:**
- **execution_count** — berapa kali query ini dieksekusi sejak cache populated
- **avg_cpu_us** — rata-rata CPU time (microseconds)
- **avg_elapsed_us** — rata-rata wall-time (microseconds) — yang paling relevan untuk UX
- **avg_reads** — rata-rata logical reads (page dari buffer cache). >1000 = perlu index/query tuning
- **avg_physical_reads** — reads dari disk (buffer miss). Tinggi = data set besar atau buffer kecil
- **query_text** — SQL statement yang tersimpan
- **query_plan** — XML plan (klik di SSMS untuk visual graph)

**Kapan dijalankan:**
- Setelah kondisi production normal (minimum 1 jam aktivitas)
- Sebelum fix: catat baseline untuk before/after comparison

**Batasan:**
- Cache flush saat restart atau `DBCC FREEPROCCACHE`
- Parameter sniffing bisa membuat 1 query text punya multiple plan — lihat `dm_exec_query_stats` per `plan_handle`

---

### T3 — Index Usage Stats (SQL Server)

**Tujuan:** Identifikasi index yang **tidak pernah digunakan** (calon drop) atau **sangat jarang digunakan** (underused). Kurangi overhead write tanpa benefit read.

**Prasyarat:** Sama seperti T2.

**Query:**
```sql
USE HcPortalDB_Dev;

SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS schema_name,
    OBJECT_NAME(i.object_id) AS table_name,
    i.name AS index_name,
    i.type_desc,
    i.is_unique,
    ISNULL(ius.user_seeks, 0) AS user_seeks,
    ISNULL(ius.user_scans, 0) AS user_scans,
    ISNULL(ius.user_lookups, 0) AS user_lookups,
    ISNULL(ius.user_updates, 0) AS user_updates,
    ius.last_user_seek,
    ius.last_user_scan,
    ius.last_user_lookup,
    ius.last_user_update,
    CASE
        WHEN ius.user_seeks IS NULL
         AND ius.user_scans IS NULL
         AND ius.user_lookups IS NULL THEN 'NEVER_USED'
        WHEN (ISNULL(ius.user_seeks,0) + ISNULL(ius.user_scans,0) + ISNULL(ius.user_lookups,0)) < ISNULL(ius.user_updates,0) THEN 'UNDERUSED'
        ELSE 'ACTIVE'
    END AS usage_status
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius
    ON ius.object_id = i.object_id
   AND ius.index_id = i.index_id
   AND ius.database_id = DB_ID()
WHERE i.type_desc != 'HEAP'
  AND i.is_primary_key = 0  -- PK jangan di-drop
  AND OBJECT_NAME(i.object_id) NOT LIKE 'sys%'
  AND OBJECT_NAME(i.object_id) IN (
      'AssessmentSessions','AssessmentAttemptHistory','TrainingRecords',
      'OrganizationUnits','ExamActivityLogs','PackageUserResponses',
      'PackageQuestions','UserPackageAssignments','AspNetUsers','AspNetUserRoles'
  )
ORDER BY
    CASE
        WHEN ius.user_seeks IS NULL AND ius.user_scans IS NULL AND ius.user_lookups IS NULL THEN 1
        WHEN (ISNULL(ius.user_seeks,0) + ISNULL(ius.user_scans,0) + ISNULL(ius.user_lookups,0)) < ISNULL(ius.user_updates,0) THEN 2
        ELSE 3
    END,
    user_updates DESC;
```

**Cara baca hasil:**
- **user_seeks / user_scans / user_lookups** — cara query plan memakai index (makin tinggi = makin berguna)
- **user_updates** — berapa kali index harus di-update saat INSERT/UPDATE/DELETE (overhead write)
- **usage_status**:
  - `NEVER_USED` → **kandidat drop** (asalkan bukan index unique untuk constraint)
  - `UNDERUSED` → write cost > read benefit → review manual
  - `ACTIVE` → keep

**Kapan dijalankan:**
- Setelah minimal 1-2 minggu aktivitas normal (biar sample representatif)
- Sebelum decide drop: konfirmasi dengan pemilik fitur bahwa index tersebut memang tidak dipakai

**Batasan:**
- DMV reset saat restart SQL Server
- Index yang dipakai untuk constraint FK/Unique: **jangan drop** walaupun usage low

---

### T4 — SQLite EXPLAIN QUERY PLAN (Localhost)

**Tujuan:** Validasi query plan untuk pattern issue di dataset localhost (SQLite). Cek apakah index existing dipakai atau terjadi full table scan.

**Prasyarat:**
- File `HcPortal.db` di folder project (localhost)
- SQLite CLI (`sqlite3.exe`) atau DB Browser for SQLite
- App tidak sedang lock file (hentikan `dotnet run` sementara)

**Template queries untuk 6 issue:**

```sql
-- =======================================================
-- Setup: buka database
-- sqlite3 HcPortal.db
-- =======================================================

-- C-01 GetCascadeOptions: ProtonTracks DISTINCT + ORDER BY
EXPLAIN QUERY PLAN
SELECT DISTINCT TrackType FROM ProtonTracks ORDER BY TrackType;

EXPLAIN QUERY PLAN
SELECT DisplayName FROM ProtonTracks ORDER BY Urutan;

-- C-02 Stats Count by RoleLevel
EXPLAIN QUERY PLAN
SELECT COUNT(*) FROM AspNetUsers WHERE RoleLevel = 1;

-- C-03 UserRoles JOIN tanpa filter
EXPLAIN QUERY PLAN
SELECT ur.UserId, r.Name
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id;

-- C-04 AssessmentAttemptHistory full fetch
EXPLAIN QUERY PLAN
SELECT h.*, u.*
FROM AssessmentAttemptHistory h
INNER JOIN AspNetUsers u ON h.UserId = u.Id;

-- C-04 AssessmentSessions filter Status
EXPLAIN QUERY PLAN
SELECT a.*, u.*
FROM AssessmentSessions a
INNER JOIN AspNetUsers u ON a.UserId = u.Id
WHERE a.Status = 'Completed';

-- H-03 AssessmentMonitoring 7-day window COALESCE
EXPLAIN QUERY PLAN
SELECT * FROM AssessmentSessions
WHERE COALESCE(ExamWindowCloseDate, Schedule) >= datetime('now', '-7 days')
ORDER BY Schedule DESC;

-- M-03 GetOrganizationTree ORDER BY multi-column
EXPLAIN QUERY PLAN
SELECT Id, Name, ParentId, Level, DisplayOrder, IsActive
FROM OrganizationUnits
ORDER BY Level, DisplayOrder, Name;

-- M-06 GetActivityLog ORDER BY
EXPLAIN QUERY PLAN
SELECT EventType, Detail, Timestamp
FROM ExamActivityLogs
WHERE SessionId = 1
ORDER BY Timestamp;
```

**Cara baca hasil:**
- `SCAN TABLE x` → full table scan (SLOW untuk tabel besar)
- `SEARCH TABLE x USING INDEX idx_name` → index dipakai (FAST)
- `SEARCH TABLE x USING COVERING INDEX` → index cover semua kolom, tanpa bookmark lookup (OPTIMAL)
- `USE TEMP B-TREE FOR ORDER BY` → sort di memory karena tidak ada index cover ORDER BY (SLOW untuk N besar)

**Kapan dijalankan:**
- Quick sanity check sebelum implementasi fix di localhost
- Validate prediksi index candidate dari Section 15

**Batasan:**
- SQLite plan berbeda dengan SQL Server. Pattern umum sama (scan vs seek), tapi cost model beda
- Dataset localhost kecil → plan bisa optimizer pilih scan meskipun ada index (karena lebih cepat di tabel kecil)

---

### T5 — EF Core Log Parser (Bash / PowerShell)

**Tujuan:** Parse log EF Core yang ter-generate (setelah EF logging diaktifkan via env var — lihat Section 17) menjadi tabel timing yang bisa dianalisis.

**Prasyarat:**
- File log dari app run dengan `ASPNETCORE_Logging__LogLevel__Microsoft_EntityFrameworkCore_Database_Command=Information` aktif
- Bash (Git Bash / WSL) atau PowerShell

**Format log EF Core yang dihasilkan (contoh):**
```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (15ms) [Parameters=[...], CommandType='Text', CommandTimeout='30']
      SELECT [a].[Id], [a].[UserId], ...
      FROM [AssessmentSessions] AS [a]
      WHERE [a].[Id] = @__sessionId_0
```

**Template Bash (regex ekstrak elapsed time + SQL):**
```bash
#!/bin/bash
# Usage: bash parse-ef-log.sh app.log > timings.csv

LOG_FILE="${1:-app.log}"

echo "elapsed_ms,sql_snippet"

grep -B0 -A5 "Executed DbCommand" "$LOG_FILE" | \
  awk '
    /Executed DbCommand \(([0-9]+)ms\)/ {
      match($0, /\(([0-9]+)ms\)/, arr);
      elapsed = arr[1];
      getline; getline;   # skip parameters line
      getline query;
      gsub(/"/, "'"'"'", query);
      gsub(/^[[:space:]]+/, "", query);
      # truncate query ke 100 char agar readable
      if (length(query) > 100) query = substr(query, 1, 100) "...";
      print elapsed "," query;
    }
  '
```

**Template PowerShell (Windows native):**
```powershell
# Usage: .\parse-ef-log.ps1 -LogFile app.log | Export-Csv timings.csv -NoTypeInformation

param([string]$LogFile = "app.log")

$content = Get-Content $LogFile -Raw
$pattern = '(?ms)Executed DbCommand \((?<ms>\d+)ms\).*?CommandTimeout=''\d+''\]\s*(?<sql>.*?)(?=\r?\n\r?\n|\r?\ninfo:|\r?\nwarn:|$)'

[regex]::Matches($content, $pattern) | ForEach-Object {
    [PSCustomObject]@{
        elapsed_ms = [int]$_.Groups['ms'].Value
        sql_snippet = ($_.Groups['sql'].Value -replace '\s+', ' ').Trim().Substring(0, [Math]::Min(100, $_.Groups['sql'].Value.Length))
    }
} | Sort-Object elapsed_ms -Descending
```

**Output contoh (CSV):**
```csv
elapsed_ms,sql_snippet
1247,"SELECT [h].[Id], [h].[UserId]... FROM [AssessmentAttemptHistory] AS [h] LEFT JOIN..."
892,"SELECT [a].* FROM [AssessmentSessions] AS [a] WHERE [a].[Status] = N'Completed'"
45,"SELECT COUNT(*) FROM [AspNetUsers]"
```

**Cara baca hasil:**
- Sort by `elapsed_ms` descending — top queries paling lambat
- Correlate dengan `sql_snippet` untuk identify issue (C-04 full fetch, H-04 essay loop, dll)
- Count occurrences untuk detect N+1 pattern: `cut -d, -f2 timings.csv | sort | uniq -c | sort -rn`

**Kapan dijalankan:**
- Setelah trigger skenario test dengan EF logging aktif (via Playwright / manual browser)
- Sebelum & sesudah fix untuk compare timing

**Batasan:**
- Log verbose bisa besar (MB-GB untuk sesi panjang) — rotate atau batasi skenario
- Regex sederhana, kalau ada SQL multi-line kompleks perlu tuning
- Parameter values di-elide EF untuk keamanan — kadang perlu set `EnableSensitiveDataLogging` (hanya di dev!) untuk lihat parameter actual

---

## 16.6 Ringkasan Use Case Template

| Template | Kapan Dipakai | Output Utama |
|----------|---------------|--------------|
| T1 Missing Index | Validate Section 15.3 priority list | CREATE INDEX DDL rekomendasi |
| T2 Query Stats | Benchmark sebelum/sesudah fix | Top-N slow queries |
| T3 Index Usage | Cleanup unused index | Daftar index `NEVER_USED`/`UNDERUSED` |
| T4 SQLite EXPLAIN | Quick sanity check localhost | Plan per query issue |
| T5 EF Log Parser | Correlate wall-time dgn SQL | CSV elapsed_ms per query |

## 16.7 Kombinasi Template untuk Scenario Validasi

**Scenario A: Validasi C-04 fix (filter date range)**
1. T5 sebelum fix → baseline timing `GetAllWorkersHistory` 3 query
2. Implement fix
3. T5 sesudah fix → compare
4. T2 historical → pastikan tidak ada regression di query lain

**Scenario B: Validasi M-03 IMemoryCache**
1. T2 baseline → catat execution_count `GetOrganizationTree` per jam
2. Implement cache
3. T2 sesudah → execution_count turun ~99%
4. T3 → pastikan index `OrganizationUnits` tidak jadi `UNDERUSED` (masih dipakai saat cache miss)

**Scenario C: Index candidate deployment**
1. T1 → confirm missing index rekomendasi
2. T3 baseline index usage
3. CREATE INDEX (oleh DBA)
4. Monitor 1 minggu
5. T3 ulang → validate index baru `ACTIVE`
6. T2 → compare query timing before/after

Catatan: Section 17 (validation guide) akan dilanjutkan di langkah terakhir.

---

## 17. Future Validation Guide — How to Enable EF Logging Zero-File-Change

Panduan ini adalah **referensi siap pakai** untuk engineer/DBA yang ingin melakukan validasi runtime database di masa depan tanpa memodifikasi file project.

### 17.1 Enable EF Core Logging via Environment Variable

ASP.NET Core mendukung konfigurasi logging via environment variable dengan format:
```
ASPNETCORE_Logging__LogLevel__<category>=<level>
```

Environment variable **override** nilai di `appsettings.json` dan `appsettings.Development.json` **tanpa memodifikasi file**. Variable hanya valid selama shell session / proses app — restart shell = variable hilang.

**Category yang relevan untuk EF Core:**
- `Microsoft.EntityFrameworkCore.Database.Command` — log SQL yang di-generate + execution time
- `Microsoft.EntityFrameworkCore.Database.Connection` — log open/close connection
- `Microsoft.EntityFrameworkCore.Query` — log query translation (verbose)

**Level yang relevan:**
- `Warning` (default) — hanya error/warning
- `Information` — **recommended untuk diagnostic** (log semua SQL + elapsed time)
- `Debug` — sangat verbose (lifecycle events)

#### Windows (cmd.exe)

```cmd
REM Set env var untuk session ini
set ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information

REM Run app — log akan muncul di console
dotnet run

REM Clear env var setelah selesai
set ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=
```

#### Windows (PowerShell)

```powershell
# Set env var untuk session ini
$env:ASPNETCORE_Logging__LogLevel__Microsoft__EntityFrameworkCore__Database__Command = "Information"

# Run app
dotnet run

# Clear
Remove-Item Env:\ASPNETCORE_Logging__LogLevel__Microsoft__EntityFrameworkCore__Database__Command
```

**Catatan:** PowerShell mengharuskan underscore (`__`) bukan titik (`.`) karena dot adalah operator.

#### Linux / macOS (bash)

```bash
# Set + run in single command (env var hanya untuk command ini)
ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information \
  dotnet run

# Atau persist untuk session
export ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information
dotnet run
unset ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command
```

#### Redirect Output ke File

Default logging EF Core ter-print ke console (stdout). Untuk analisis:

**Windows cmd:**
```cmd
dotnet run > app.log 2>&1
```

**PowerShell:**
```powershell
dotnet run *> app.log
```

**Bash:**
```bash
dotnet run 2>&1 | tee app.log
```

File `app.log` kemudian bisa di-parse dengan Template T5 (Section 16).

### 17.2 Optional — Enable Sensitive Data Logging (Dev Only)

Secara default, EF Core **elides parameter values** di log untuk keamanan:
```
WHERE [a].[Id] = @__sessionId_0   (nilai @__sessionId_0 tidak ditampilkan)
```

Jika butuh melihat nilai parameter actual untuk debug (mis. parameter sniffing investigation), aktifkan `EnableSensitiveDataLogging` via env var:

```
ASPNETCORE_SensitiveDataLoggingEnabled=true
```

**⚠️ PERINGATAN:**
- **Hanya aktifkan di localhost/dev**. Log akan berisi data pribadi (nama, email, NIP, dll)
- **Jangan commit log ke git**. Tambah ke `.gitignore`
- **Disable segera** setelah investigasi selesai
- **JANGAN ENABLE DI PRODUCTION** — bisa jadi compliance issue (GDPR/PDP)

Di codebase saat ini (cek `Program.cs`), jika tidak ada `EnableSensitiveDataLogging()` call, env var ini tidak berefek. Alternatif: modifikasi `DbContextOptions` tapi itu perubahan project (tidak sesuai constraint zero-change). **Lewati step ini kecuali benar-benar perlu.**

### 17.3 Workflow Validasi End-to-End

Alur lengkap validasi issue performa dari setup → capture → analysis → cleanup.

#### Step 1 — Preparation

- [ ] Pastikan localhost/dev **idle** (tidak ada user lain yang aktif)
- [ ] Backup/catat env var existing (`set > env-backup.txt` di cmd)
- [ ] Pastikan disk space cukup untuk log (minimal 100 MB per sesi)
- [ ] Tentukan **scope skenario** — reuse dari `STEP2_DISCUSSION.md`

#### Step 2 — Enable Logging

Pilih salah satu dari 15.1 sesuai shell Anda. Rekomendasi:
```cmd
set ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information
dotnet run > app.log 2>&1
```

Tunggu sampai log muncul `Now listening on: http://localhost:5277`.

#### Step 3 — Trigger Skenario

Opsi A — **Playwright automation** (reuse Fase 2A):
- Kembali ke skenario `STEP2_DISCUSSION.md` Fase 2A Item 3/4/5/6
- Jalankan Playwright dengan baseline yang sama

Opsi B — **Manual browser**:
- Login, navigasi ke halaman target (misal `/Admin/ManageAssessment`)
- Trigger action yang diperlukan (click, filter, dll)

#### Step 4 — Stop App + Capture Log

- Tekan `Ctrl+C` di terminal `dotnet run`
- File `app.log` siap untuk analysis
- Hitung ukuran: `dir app.log` (Windows) atau `ls -lh app.log` (bash)

#### Step 5 — Parse Log

Gunakan **Template T5** (Section 16) untuk parse jadi CSV:
```bash
bash parse-ef-log.sh app.log > timings.csv
```

Atau PowerShell:
```powershell
.\parse-ef-log.ps1 -LogFile app.log | Export-Csv timings.csv -NoTypeInformation
```

#### Step 6 — Cross-Reference dengan Section 14

Bandingkan SQL hasil log dengan **Expected SQL** di Section 14.1-14.6:
- Apakah pola SQL sesuai prediksi?
- Apakah ada parameter sniffing? (query sama, plan berbeda)
- Apakah ada N+1 yang tidak terduga?

#### Step 7 — Cleanup

- [ ] Unset/clear env var logging (lihat 15.1 masing-masing shell)
- [ ] Delete atau archive `app.log` — **jangan commit ke git**
- [ ] Delete `timings.csv` jika tidak diperlukan
- [ ] Verify no env var residual: `echo %ASPNETCORE_Logging__LogLevel__...%` (harus empty)

### 17.4 Triggering Skenario — Reuse Fase 2A Playwright

Skenario yang sudah terbukti efektif di Fase 2A (lihat `STEP2_DISCUSSION.md`):

| Item | Halaman | Issue Target |
|------|---------|--------------|
| 1 | `/CMP/AnalyticsDashboard` | Analytics 11 endpoint |
| 3 | `/Admin/ManageAssessment` | C-04 + H-05 |
| 4 | `/Admin/ManageWorkers` | C-02 + C-03 |
| 5 | `/Admin/AssessmentMonitoring` | H-03 + M-04 |
| 6 | `/Admin/ManageOrganization` | M-03 |

**Tips:**
- Gunakan Playwright `browser_navigate` ke halaman target → log EF akan otomatis ter-capture oleh shell
- Hindari pengujian saat ada user lain aktif untuk mengurangi noise
- Hentikan app singkat saat ada task ringan (mis. health check background) untuk log bersih

### 17.5 Validation Checklist

**Sebelum mulai:**
- [ ] Dataset representatif (minimal populate sample data dev kalau localhost terlalu kecil)
- [ ] Skenario target jelas (mana issue yang divalidasi)
- [ ] Baseline metrics dari Step 2 sudah ada (API_RESPONSE_TIMES.md)
- [ ] Env var set, dotnet run jalan, log file siap capture

**Saat validasi:**
- [ ] Capture **sebelum fix** (baseline) — save sebagai `app-before.log`
- [ ] Implementasikan fix di branch terpisah
- [ ] Capture **sesudah fix** — save sebagai `app-after.log`
- [ ] Parse keduanya dengan T5, compare CSV hasilnya

**Setelah validasi:**
- [ ] Cleanup env var
- [ ] Archive log untuk audit trail (jangan di git repo)
- [ ] Update `PERFORMANCE_REPORT.md` dengan angka actual
- [ ] Rollback env var ke default kalau ada yang di-export ke shell persistent

### 17.6 Troubleshooting Common Issues

| Masalah | Penyebab | Solusi |
|---------|----------|--------|
| Log EF tidak muncul | Env var tidak ter-set sebelum `dotnet run` | Set env var **di shell yang sama** dengan dotnet run, sebelum command |
| Log terlalu verbose | Category `Microsoft` juga diaktifkan | Scope spesifik: hanya `Microsoft.EntityFrameworkCore.Database.Command` |
| Log tidak parseable dengan T5 regex | Format EF berubah di versi lib berbeda | Adjust regex di T5 sesuai sample output actual |
| Parameter `@__p_0` tidak menunjukkan nilai | Sensitive data logging default OFF | Lihat 15.2 (opsional, dev only) |
| Log file terlalu besar (>500 MB) | Skenario terlalu panjang + log verbose | Batasi skenario per-item, rotate log per sesi |
| Permission denied akses DMV SQL Server | Credential kurang `VIEW SERVER STATE` | Minta DBA grant `GRANT VIEW SERVER STATE TO [user]` |
| Query plan XML terlalu panjang di T2 | Large plan untuk query kompleks | Save plan ke file terpisah, buka di SSMS graphical plan viewer |
| `EXPLAIN QUERY PLAN` SQLite tidak jalan | Database file locked oleh `dotnet run` | Stop app dulu sebelum akses `HcPortal.db` |
| Env var tidak clear setelah session | Shell persistent (WSL, Windows Terminal tab) | Manual clear: `set VAR=` (cmd) atau `unset VAR` (bash) |
| Log output tidak ter-redirect ke file | Gagal capture stdout+stderr | Pakai `> file 2>&1` (cmd) atau `*> file` (PowerShell) atau `2>&1 | tee file` (bash) |

### 17.7 Reverse Rollback — Memastikan Zero Trace

Kalau ingin memastikan **tidak ada trace** dari aktivitas validasi:

```bash
# 1. Unset semua env var
unset ASPNETCORE_Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command
unset ASPNETCORE_SensitiveDataLoggingEnabled

# 2. Delete log files
rm app*.log
rm timings*.csv

# 3. Verify no git diff
git status
# Harus menunjukkan tidak ada modifikasi project code (hanya dokumentasi yang kita tambah)

# 4. Verify env clean
env | grep -i aspnet
# Harus kosong (Linux) atau tampil default saja (Windows)
```

### 17.8 Catatan Akhir

**Konstrain yang terpenuhi di seluruh Step 3:**
- ✅ **Zero project file modification** (code, Views, config, migrations)
- ✅ **Zero DB execution** (tidak ada query ke `HcPortal.db` atau SQL Server dev)
- ✅ **Zero deployment change** (env var temporary, bukan file)
- ✅ **Zero data generated / altered**
- ✅ **Fully reversible** — semua aktivitas bisa di-rollback tanpa jejak

**Hasil Step 3 (4 langkah):**
- Section 14 — Expected SQL patterns per 6 issue
- Section 15 — Index candidate list (7 rekomendasi + existing inventory)
- Section 16 — 5 diagnostic SQL templates siap pakai
- Section 17 — Panduan aktivasi EF logging + workflow validasi

**Status:** Step 3 selesai dalam bentuk dokumentasi zero-impact. Validasi actual di DB akan dilakukan saat Anda/DBA siap, dengan referensi ke section-section ini.
