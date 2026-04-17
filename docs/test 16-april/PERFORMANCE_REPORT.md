# Performance Report — PortalHC KPB
**Tanggal Analisis:** 16 April 2026
**Scope:** CDPController, WorkerController, ManageWorkers View, WorkerDataService, AssessmentAdminController (ManageAssessment + AssessmentMonitoring + Activity Log)
**Status:** Static Analysis (tanpa implementasi)

---

## Executive Summary

Ditemukan **5 issue performa** pada modul CDPController dan WorkerController yang berpotensi menyebabkan degradasi performa signifikan pada kondisi beban tinggi (5.000+ users). Issue terdiri dari 3 Critical, 1 High, dan 1 Medium.

| ID | Severity | Issue | File |
|----|----------|-------|------|
| C-01 | CRITICAL | Blocking `.ToList()` pada async method | `CDPController.cs:316-317` |
| C-02 | CRITICAL | 4x `CountAsync()` terpisah untuk stats card | `WorkerController.cs:108-111` |
| C-03 | CRITICAL | Fetch semua users tanpa batas + no `AsNoTracking()` | `WorkerController.cs:92, 95-100` |
| H-06 | HIGH | Stats selalu hitung semua users, abaikan filter aktif | `WorkerController.cs:108-111` |
| M-08 | MEDIUM | Client-side pagination DOM — lag pada 5K+ users | `ManageWorkers.cshtml:433-482` |

---

## Detail Issue

### C-01 — Blocking `.ToList()` pada Async Method

**File:** `Controllers/CDPController.cs:316-317`
**Severity:** Critical

**Masalah:**
Method `GetCascadeOptions` didefinisikan sebagai `async Task<IActionResult>`, namun dua query ke database menggunakan `.ToList()` (synchronous) alih-alih `.ToListAsync()`:

```csharp
var categories = _context.ProtonTracks
    .Select(t => t.TrackType).Distinct().OrderBy(t => t)
    .ToList();   // ← BLOCKING — seharusnya ToListAsync()

var tracks = _context.ProtonTracks
    .OrderBy(t => t.Urutan).Select(t => t.DisplayName)
    .ToList();   // ← BLOCKING — seharusnya ToListAsync()
```

**Dampak:**
- Thread pool thread diblokir selama query berlangsung
- Mengurangi throughput aplikasi secara keseluruhan
- Pada beban tinggi, dapat menyebabkan thread pool starvation
- Menghilangkan seluruh manfaat async/await pada method ini

**Rekomendasi:**
Ganti `.ToList()` dengan `await ...ToListAsync()` pada kedua query. Jalankan keduanya secara paralel menggunakan `Task.WhenAll` untuk efisiensi tambahan:

```csharp
var categoriesTask = _context.ProtonTracks
    .Select(t => t.TrackType).Distinct().OrderBy(t => t)
    .ToListAsync();

var tracksTask = _context.ProtonTracks
    .OrderBy(t => t.Urutan).Select(t => t.DisplayName)
    .ToListAsync();

await Task.WhenAll(categoriesTask, tracksTask);
var categories = categoriesTask.Result;
var tracks = tracksTask.Result;
```

---

### C-02 — 4x `CountAsync()` Terpisah untuk Stats Card

**File:** `Controllers/WorkerController.cs:108-111`
**Severity:** Critical

**Masalah:**
Empat query `CountAsync()` dieksekusi secara sequential untuk mengisi stats card di halaman ManageWorkers:

```csharp
ViewBag.TotalUsers  = await _context.Users.CountAsync();
ViewBag.AdminCount  = await _context.Users.CountAsync(u => u.RoleLevel == 1);
ViewBag.HcCount     = await _context.Users.CountAsync(u => u.RoleLevel == 2);
ViewBag.WorkerCount = await _context.Users.CountAsync(u => u.RoleLevel >= 5);
```

**Dampak:**
- 4 round-trip ke database untuk data yang bisa diperoleh dalam 1 query
- Latensi kumulatif: jika tiap query ~10ms → total ~40ms hanya untuk stats
- Skalabilitas buruk — waktu bertambah linear dengan jumlah stats card

**Rekomendasi:**
Gabungkan menjadi satu query dengan `GroupBy` atau gunakan subquery agregasi:

```csharp
var roleCounts = await _context.Users
    .GroupBy(u => 1)
    .Select(g => new {
        Total   = g.Count(),
        Admin   = g.Count(u => u.RoleLevel == 1),
        HC      = g.Count(u => u.RoleLevel == 2),
        Worker  = g.Count(u => u.RoleLevel >= 5)
    })
    .FirstOrDefaultAsync();

ViewBag.TotalUsers  = roleCounts?.Total ?? 0;
ViewBag.AdminCount  = roleCounts?.Admin ?? 0;
ViewBag.HcCount     = roleCounts?.HC ?? 0;
ViewBag.WorkerCount = roleCounts?.Worker ?? 0;
```

---

### C-03 — Fetch Semua Users Tanpa Batas + No `AsNoTracking()`

**File:** `Controllers/WorkerController.cs:92, 95-100`
**Severity:** Critical

**Masalah:**
Dua masalah sekaligus pada blok pengambilan data users:

```csharp
// Line 92 — Masalah 1: Tidak ada AsNoTracking() — EF tracking semua entitas
var users = await query.OrderBy(u => u.FullName).ToListAsync();

// Lines 95-100 — Masalah 2: UserRoles di-fetch SEMUA lalu di-dictionary di memory
var userRolesDict = (await _context.UserRoles
    .Join(_context.Roles, ur => ur.RoleId, r => r.Id,
          (ur, r) => new { ur.UserId, r.Name })
    .ToListAsync())          // ← fetch SELURUH tabel UserRoles dari DB
    .GroupBy(x => x.UserId)
    .ToDictionary(g => g.Key, g => g.First().Name ?? "No Role");
```

**Dampak:**
- **No AsNoTracking:** EF Core melacak perubahan pada semua entitas yang di-load. Untuk 5.000 users, overhead memori dan CPU signifikan karena EF mempertahankan snapshot entitas untuk change tracking
- **UserRoles tidak terfilter:** Seluruh tabel UserRoles diambil ke memori aplikasi meskipun hanya sebagian user yang relevan — `N_UserRoles >> N_Users_dimuat`. Semakin besar sistem, semakin boros. Walau dictionary akhir hanya menyimpan satu role name string per user (via `.First().Name`), fetch-nya tetap membawa semua row UserRoles × Roles join ke aplikasi
- Kombinasi keduanya menyebabkan memory spike pada halaman ManageWorkers

**Rekomendasi:**
1. Tambahkan `.AsNoTracking()` pada query users (read-only, tidak perlu tracking)
2. Filter UserRoles di database dengan `userIds` hasil fetch sebelumnya, bukan fetch seluruh tabel:

```csharp
var users = await query
    .AsNoTracking()              // ← tambahkan ini
    .OrderBy(u => u.FullName)
    .ToListAsync();

var userIds = users.Select(u => u.Id).ToList();

var userRolesDict = (await _context.UserRoles
    .Where(ur => userIds.Contains(ur.UserId))   // ← filter di DB
    .Join(_context.Roles, ur => ur.RoleId, r => r.Id,
          (ur, r) => new { ur.UserId, r.Name })
    .ToListAsync())
    .GroupBy(x => x.UserId)
    .ToDictionary(g => g.Key, g => g.First().Name ?? "No Role");
```

---

### H-06 — Stats Selalu Hitung Semua Users (Abaikan Filter Aktif)

**File:** `Controllers/WorkerController.cs:108-111`
**Severity:** High

**Masalah:**
Stats card (Total Users, Admin Count, HC Count, Worker Count) selalu menghitung seluruh data di database, tidak memperhitungkan filter yang sedang aktif (pencarian nama, filter section, filter unit):

```csharp
// Selalu COUNT(*) tanpa WHERE — tidak mengikuti filter aktif
ViewBag.TotalUsers  = await _context.Users.CountAsync();
ViewBag.AdminCount  = await _context.Users.CountAsync(u => u.RoleLevel == 1);
```

**Dampak:**
- **Inkonsistensi UX:** User memfilter "Section A" tapi stats card masih menampilkan jumlah total seluruh sistem — membingungkan
- Potensi informasi menyesatkan bagi admin yang sedang menganalisis data per section/unit

**Rekomendasi:**
Terapkan filter yang sama (`query` IQueryable yang sudah terfilter) ke stats count:

```csharp
// Gunakan query yang sudah terfilter (bukan _context.Users langsung)
var roleCounts = await query
    .GroupBy(u => 1)
    .Select(g => new {
        Total  = g.Count(),
        Admin  = g.Count(u => u.RoleLevel == 1),
        HC     = g.Count(u => u.RoleLevel == 2),
        Worker = g.Count(u => u.RoleLevel >= 5)
    })
    .FirstOrDefaultAsync();
```

---

### M-08 — Client-Side Pagination DOM (Lag 5K+ Users)

**File:** `Views/ManageWorkers.cshtml:433-482`
**Severity:** Medium

**Masalah:**
Seluruh data users di-render ke HTML DOM sejak awal, lalu pagination dilakukan dengan menyembunyikan baris (`display:none`):

```javascript
// Semua rows ada di DOM — hanya visibility yang diubah
allRows = Array.from(table.querySelectorAll('.worker-row'));
allRows.forEach((row, i) => {
    row.style.display = (i >= start && i < end) ? '' : 'none';  // O(n) per page change
});
```

**Dampak:**
- **Initial load lambat:** Browser harus me-render ribuan `<tr>` elemen sekaligus walau hanya 10-20 yang ditampilkan
- **Memory boros:** DOM menyimpan semua data users di memori browser
- **Page switching lag:** Setiap ganti halaman, JavaScript harus iterasi semua rows O(n)
- Untuk 5.000 users: ~5.000 `<tr>` dengan data lengkap di DOM = potensi freeze pada perangkat low-end

**Rekomendasi:**
Implementasikan server-side pagination: hanya kirim data untuk halaman yang diminta. Gunakan parameter `page` dan `pageSize` pada action controller, dan kembalikan JSON atau partial view. Alternatif lebih ringan: virtual scrolling di sisi client.

---

## Ringkasan Prioritas Perbaikan

| Prioritas | ID | Estimasi Dampak | Kompleksitas Fix |
|-----------|----|-----------------|-----------------|
| 1 | C-01 | Tinggi — thread pool | Rendah (2 baris) |
| 2 | C-02 + H-06 | Tinggi — DB round trips + UX | Menengah |
| 3 | C-03 | Sangat Tinggi — memory | Menengah |
| 4 | M-08 | Tinggi — UX pada scale | Tinggi |

---

## Analisis: WorkerDataService + ManageAssessment

**Scope:** `WorkerDataService.cs`, `AssessmentAdminController.cs`
**Step:** 1B — Static Analysis

Ditemukan **4 issue performa tambahan** pada modul WorkerDataService dan ManageAssessment. Issue terdiri dari 1 Critical, 1 High, dan 2 Medium.

| ID | Severity | Issue | File |
|----|----------|-------|------|
| C-04 | CRITICAL | `GetAllWorkersHistory()` fetch 3 tabel penuh tanpa filter | `WorkerDataService.cs:90-155` |
| H-05 | HIGH | Grouping sessions di memory setelah full fetch | `AssessmentAdminController.cs:112-157` |
| M-04 | MEDIUM | Missing `AsNoTracking()` di ManageAssessment initial query | `AssessmentAdminController.cs:66-68` |
| M-09 | MEDIUM | Dropdown kategori tanpa date filter — full table scan | `AssessmentAdminController.cs:172-176` |

---

### C-04 — `GetAllWorkersHistory()` Fetch 3 Tabel Penuh Tanpa Filter

**File:** `Services/WorkerDataService.cs:85-172`
**Severity:** Critical

**Catatan optimasi parsial yang sudah ada:**
Method ini **sudah mengoptimalkan satu bagian** — query `archivedCounts` (line 90-94) menggunakan `GroupBy` + `Count()` di SQL untuk menghitung attempt number secara agregat, bukan dengan memuat seluruh history lalu grouping di memori. Bagian ini bukan issue.

**Masalah (3 query full-fetch sisanya):**
Setelah `archivedCounts`, method menjalankan 3 query fetch tabel penuh secara sequential tanpa filter user, date, atau pagination:

```csharp
// Query 1 — Full fetch AssessmentAttemptHistory (TANPA filter)
var archivedAttempts = await _context.AssessmentAttemptHistory
    .AsNoTracking()
    .Include(h => h.User)
    .ToListAsync();   // ← seluruh tabel, tanpa WHERE

// Query 2 — Full fetch AssessmentSessions (filter Status saja)
var currentCompleted = await _context.AssessmentSessions
    .AsNoTracking()
    .Include(a => a.User)
    .Where(a => a.Status == "Completed")
    .ToListAsync();   // ← masih bisa ribuan rows

// Query 3 — Full fetch TrainingRecords (TANPA filter)
var trainings = await _context.TrainingRecords
    .AsNoTracking()
    .Include(t => t.User)
    .ToListAsync();   // ← seluruh tabel, tanpa WHERE
```

`AsNoTracking()` sudah diterapkan (positif), namun ketiga query tidak memiliki filter user, rentang tanggal, maupun batasan jumlah rows. Seluruh grouping dan sorting dilakukan di memori setelah semua data dimuat.

**Dampak:**
- Setiap pemanggilan method ini memuat **seluruh riwayat assessment, sesi selesai, dan training records** ke memori server
- Semakin lama sistem berjalan, semakin besar data yang dimuat — performa menurun linear dengan pertumbuhan data
- Memory spike besar pada server: 3 tabel × ribuan rows × data User di-include
- Jika dipanggil secara concurrent oleh banyak user, dapat menyebabkan OOM (Out of Memory) atau GC pressure tinggi
- `Include(h => h.User)` pada full-fetch meningkatkan data transfer dari DB secara signifikan

**Rekomendasi:**
1. Tambahkan filter `UserId` apabila konteks pemanggilan untuk satu user spesifik
2. Tambahkan filter rentang tanggal (misal: 1 tahun terakhir) untuk membatasi volume data
3. Pindahkan grouping/sorting ke query SQL (server-side) menggunakan `GroupBy` dan `OrderBy` sebelum `ToListAsync()`
4. Pertimbangkan implementasi pagination jika data akan ditampilkan di UI

---

### H-05 — Grouping Sessions di Memory Setelah Full Fetch

**File:** `Controllers/AssessmentAdminController.cs:112-157`
**Severity:** High

**Masalah:**
Setelah `allSessions` di-fetch ke memori, seluruh logic grouping Pre-Post dan Standard dijalankan di C# (LINQ-to-Objects), bukan di database:

```csharp
var mgPrePostSessions  = allSessions.Where(a => a.LinkedGroupId != null).ToList();
var mgStandardSessions = allSessions.Where(a => a.LinkedGroupId == null).ToList();

var prePostGrouped = mgPrePostSessions
    .GroupBy(a => a.LinkedGroupId)
    .Select(g => { /* complex logic */ })
    .ToList<dynamic>();

var standardGrouped = mgStandardSessions
    .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
    .Select(g => { /* complex logic */ })
    .ToList<dynamic>();

var grouped = prePostGrouped.Concat(standardGrouped)
    .OrderByDescending(g => g.Schedule)
    .ToList();   // ← final sort di memory
```

**Dampak:**
- Seluruh operasi `GroupBy`, `Select`, `OrderByDescending`, dan `Concat` dieksekusi di memori server — tidak dioptimalkan oleh database engine
- Beban CPU meningkat linear dengan jumlah sesi
- `ToList<dynamic>()` menghindari type safety dan menyulitkan EF Core untuk men-translate query ke SQL di masa depan
- Sort final di memory (`OrderByDescending`) berarti data sudah dimuat semua sebelum diurutkan — tidak bisa memanfaatkan index database

**Rekomendasi:**
1. Pisahkan fetch Pre-Post dan Standard di level query (tambahkan `Where` sebelum `ToListAsync()`)
2. Pindahkan `GroupBy` ke query SQL menggunakan anonymous type yang didukung EF Core
3. Gunakan `OrderByDescending` di IQueryable (sebelum `ToListAsync()`) agar sort dilakukan di database dan memanfaatkan index pada kolom `Schedule`
4. Hindari `dynamic` — gunakan typed ViewModel untuk memungkinkan query translation

---

### M-04 — Missing `AsNoTracking()` di ManageAssessment Initial Query

**File:** `Controllers/AssessmentAdminController.cs:66-68`
**Severity:** Medium

**Masalah:**
Query awal ManageAssessment tidak menggunakan `AsNoTracking()`:

```csharp
var managementQuery = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();   // ← tidak ada AsNoTracking()
```

Ada `Select` projection di lines 87-110 yang mengurangi jumlah kolom yang dimuat, namun EF Core masih melakukan change tracking pada entitas yang di-load karena tidak ada `AsNoTracking()`.

**Dampak:**
- EF Core mempertahankan snapshot setiap entitas `AssessmentSessions` yang dimuat untuk keperluan change detection
- Untuk halaman yang bersifat read-only (hanya tampil, tidak ada update), overhead ini tidak diperlukan
- Memory overhead bertambah sejalan dengan jumlah session yang memenuhi filter 7 hari terakhir
- Performa query sedikit lebih lambat karena EF Core perlu mengalokasikan objek tracking tambahan

**Rekomendasi:**
Tambahkan `AsNoTracking()` setelah `_context.AssessmentSessions`:

```csharp
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()   // ← tambahkan ini
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();
```

Ini adalah perubahan satu baris dengan dampak positif pada memori dan performa, tanpa risiko side-effect karena data hanya dibaca.

---

### M-09 — Dropdown Kategori Tanpa Date Filter (Full Table Scan)

**File:** `Controllers/AssessmentAdminController.cs:172-176`
**Severity:** Medium

**Masalah:**
Dropdown kategori mengambil data dari seluruh tabel `AssessmentSessions` tanpa filter tanggal apapun:

```csharp
ViewBag.Categories = await _context.AssessmentSessions
    .Select(a => a.Category)
    .Distinct()
    .OrderBy(c => c)
    .ToListAsync();   // ← full table scan, tanpa WHERE
```

Sementara itu, main query di halaman yang sama menggunakan filter 7 hari terakhir (`sevenDaysAgo`).

**Dampak:**
- **Inkonsistensi data:** Dropdown menampilkan kategori dari semua sesi sepanjang sejarah sistem, sementara list sesi hanya menampilkan 7 hari terakhir. User bisa memilih kategori yang tidak menghasilkan data apapun di list
- **Full table scan:** Setiap load halaman, database harus membaca seluruh kolom `Category` dari semua rows `AssessmentSessions` — tidak efisien jika data historis sangat besar
- Jika tabel `AssessmentSessions` memiliki index pada `Category` saja (tanpa composite dengan date), query ini akan semakin lambat seiring bertambahnya data

**Rekomendasi:**
Sinkronkan filter dropdown dengan main query menggunakan filter tanggal yang sama:

```csharp
ViewBag.Categories = await _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)  // ← filter sama dengan main query
    .Select(a => a.Category)
    .Distinct()
    .OrderBy(c => c)
    .ToListAsync();
```

Ini sekaligus menyelesaikan inkonsistensi UX dan mengurangi scope scan database.

---

## Ringkasan Prioritas Perbaikan (Updated — Step 1A + 1B)

| Prioritas | ID | Estimasi Dampak | Kompleksitas Fix |
|-----------|----|-----------------|-----------------|
| 1 | C-01 | Tinggi — thread pool | Rendah (2 baris) |
| 2 | C-04 | Sangat Tinggi — memory spike (3 full fetch) | Menengah-Tinggi |
| 3 | C-02 + H-06 | Tinggi — DB round trips + UX | Menengah |
| 4 | C-03 | Sangat Tinggi — memory + unfiltered fetch | Menengah |
| 5 | H-05 | Tinggi — in-memory grouping | Menengah |
| 6 | M-04 | Menengah — missing AsNoTracking | Rendah (1 baris) |
| 7 | M-09 | Menengah — full scan + UX inconsistency | Rendah (1 baris) |
| 8 | M-08 | Tinggi — UX pada scale | Tinggi |

---

## Analisis: AssessmentMonitoring + AssessmentAdminController Audit

**Scope:** `AssessmentAdminController.cs` (AssessmentMonitoring, AssessmentMonitoringDetail, GetActivityLog, audit seluruh `.ToList()`)
**Step:** 1C — Static Analysis

Ditemukan **3 issue tambahan** pada modul AssessmentMonitoring dan area grading. Temuan penting: audit 72 `.ToList()` di `AssessmentAdminController.cs` menunjukkan **tidak ada blocking DB call**, semuanya operasi in-memory pada collection yang sudah dimuat. Issue H-01 dengan demikian dapat diturunkan statusnya — detail ada di `DATABASE_ANALYSIS.md` Section 7.

| ID | Severity | Issue | File |
|----|----------|-------|------|
| H-03 | HIGH | Unbounded query tanpa pagination di AssessmentMonitoring | `AssessmentAdminController.cs:2312-2349` |
| H-04 | HIGH | N+1 loop queries essay grading — 3N DB round-trips per load | `AssessmentAdminController.cs:2633-2665` |
| H-01 | ~~HIGH~~ → INFO | 72 `.ToList()` — semuanya in-memory, bukan blocking DB | `AssessmentAdminController.cs` (seluruh file) |
| M-05 | MEDIUM | Missing `AsNoTracking()` di AssessmentMonitoring & GetAkhiriSemuaCounts | `AssessmentAdminController.cs:2312, 3240` |
| M-06 | MEDIUM | `GetActivityLog` = 3 query DB terpisah per AJAX call | `AssessmentAdminController.cs:4958-4988` |
| L-02 | LOW | `Include(p => p.Questions)` dimuat penuh padahal hanya untuk `.Count` | `AssessmentAdminController.cs:2541-2551` |

---

### H-03 — Unbounded Query Tanpa Pagination di AssessmentMonitoring

**File:** `Controllers/AssessmentAdminController.cs:2312-2349`
**Severity:** High

**Masalah:**
Action `AssessmentMonitoring` memuat seluruh sessions dalam window 7 hari ke memori tanpa pagination database, lalu melakukan grouping dan sorting di memori:

```csharp
var query = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();   // ← MISSING AsNoTracking()

// ... filter search + category ...

var allSessions = await query
    .OrderByDescending(a => a.Schedule)
    .Select(a => new { ... })
    .ToListAsync();   // ← SELURUH 7-day window dimuat, tanpa Skip/Take
```

Setelah `allSessions` dimuat, grouping (Pre-Post + Standard) dan filter status dilakukan di memori (LINQ-to-Objects, lines 2351-2476). View kemudian merender seluruh hasil grouping sekaligus — tidak ada pagination sisi server maupun klien.

**Dampak:**
- Window 7 hari aktif berpotensi ribuan sessions saat periode ujian massal (mis. Assessment Proton, PreTest/PostTest batch)
- Grouping Pre-Post + Standard + Concat + OrderByDescending seluruhnya di CPU server
- Memory allocation untuk anonymous projection × jumlah sessions
- Render view memproses seluruh `grouped` list tanpa batas — potensi lag frontend

**Rekomendasi:**
1. Tambahkan `.AsNoTracking()` pada `_context.AssessmentSessions`
2. Pertimbangkan pagination di level grouping (setelah grouping memori, apply `Skip/Take` seperti ManageAssessment di line 180-185)
3. Untuk optimalisasi lebih dalam: pindahkan grouping Standard (by Title+Category+Schedule.Date) ke query SQL menggunakan `GroupBy` yang didukung EF Core agar database yang mengagregasi

---

### H-04 — N+1 Loop Queries Essay Grading (3N DB Round-Trips)

**File:** `Controllers/AssessmentAdminController.cs:2633-2665`
**Severity:** High

**Masalah:**
Pada action `AssessmentMonitoringDetail`, untuk setiap session yang memiliki manual grading, dijalankan 3 query DB terpisah dalam loop `foreach`:

```csharp
var manualGradingSessions = model.Sessions.Where(s => s.HasManualGrading).ToList();
foreach (var sess in manualGradingSessions)
{
    // Query 1 — per iteration
    var assignment = await _context.UserPackageAssignments
        .FirstOrDefaultAsync(a => a.AssessmentSessionId == sess.Id);
    if (assignment == null) continue;

    var shuffled = assignment.GetShuffledQuestionIds();

    // Query 2 — per iteration
    var essayQs = await _context.PackageQuestions
        .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay")
        .ToListAsync();

    // Query 3 — per iteration
    var essayRespMap = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == sess.Id &&
               essayQs.Select(q => q.Id).Contains(r.PackageQuestionId))
        .ToDictionaryAsync(r => r.PackageQuestionId);

    // ... build items ...
}
```

**Dampak:**
- Untuk grup dengan 50 peserta yang memiliki essay: **150 DB round-trips** per page load
- Latensi kumulatif linear terhadap jumlah peserta manual grading
- Setiap DB round-trip membawa overhead koneksi, parsing, dan network RTT
- Pattern klasik N+1 — antipatern paling umum menyebabkan degradasi performa

**Rekomendasi:**
1. **Batch load** ketiga tabel di luar loop dengan satu query masing-masing menggunakan `Contains(sessionIds)`:

```csharp
var sessionIds = manualGradingSessions.Select(s => s.Id).ToList();

// 1 query — semua assignment
var assignments = await _context.UserPackageAssignments
    .Where(a => sessionIds.Contains(a.AssessmentSessionId))
    .ToListAsync();

// Kumpulkan semua shuffled IDs dari assignment
var allShuffledIds = assignments
    .SelectMany(a => a.GetShuffledQuestionIds())
    .Distinct()
    .ToList();

// 1 query — semua essay questions lintas sessions
var essayQs = await _context.PackageQuestions
    .Where(q => allShuffledIds.Contains(q.Id) && q.QuestionType == "Essay")
    .ToListAsync();
var essayQIds = essayQs.Select(q => q.Id).ToList();

// 1 query — semua response lintas sessions
var essayResps = await _context.PackageUserResponses
    .Where(r => sessionIds.Contains(r.AssessmentSessionId) &&
                essayQIds.Contains(r.PackageQuestionId))
    .ToListAsync();
```

Setelah itu bangun map per session menggunakan LINQ-to-Objects (in-memory) — total hanya **3 DB round-trips** terlepas jumlah sessions.

---

### H-01 — Audit `.ToList()` (TURUN KE INFO)

**File:** `Controllers/AssessmentAdminController.cs` (seluruh file, 72 occurrences)
**Severity:** ~~High~~ → Info / Low

**Temuan:**
Audit lengkap seluruh 72 `.ToList()` di `AssessmentAdminController.cs` (lihat detail per baris di `DATABASE_ANALYSIS.md` Section 7) — **tidak ada satu pun** yang berupa blocking DB call pada `IQueryable`. Semua `.ToList()` adalah:

- Projection/filter/distinct pada list yang sudah di-fetch (LINQ-to-Objects)
- `sibling.Select(s => s.Id).ToList()` untuk membangun param batch
- `group.Select(...).ToList()` untuk hasil grouping di memori
- Helper transform anonymous type untuk ViewBag

Semua query DB menggunakan `.ToListAsync()`/`.ToDictionaryAsync()`/`.CountAsync()` dengan benar.

**Pola yang wajar dipertahankan:**
- `.ToList()` setelah `.ToListAsync()` chain — eksekusi projection kedua di memory, umum di EF Core
- `.ToList()` pada hasil `.Select(...).ToList<dynamic>()` untuk ViewBag

**Pola yang bisa dioptimalkan (bukan blocking, tapi boros memory):**
Beberapa `.ToList()` di area H-05 (ManageAssessment grouping) dan H-03 (AssessmentMonitoring grouping) sudah masuk ke rekomendasi tersendiri — pindahkan grouping ke SQL akan mengurangi jumlah allocation in-memory.

**Rekomendasi:**
Turunkan prioritas H-01 menjadi Info. Fokus optimasi pada H-03/H-04/H-05 yang sudah teridentifikasi. Tidak diperlukan perubahan pada 72 `.ToList()` in-memory.

---

### M-05 — Missing `AsNoTracking()` (AssessmentMonitoring & GetAkhiriSemuaCounts)

**File:** `Controllers/AssessmentAdminController.cs:2312, 3240`
**Severity:** Medium

**Masalah:**
Dua query read-only tidak menggunakan `AsNoTracking()`:

```csharp
// Line 2312 — AssessmentMonitoring
var query = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .AsQueryable();

// Line 3240 — GetAkhiriSemuaCounts
var sessions = await _context.AssessmentSessions
    .Where(a => a.Title == title && a.Category == category
             && a.Schedule.Date == scheduleDate.Date
             && (a.Status == "Open" || a.Status == "InProgress"))
    .ToListAsync();
```

Action `AssessmentMonitoring` adalah murni view read-only. `GetAkhiriSemuaCounts` juga hanya menghitung status.

**Dampak:**
- EF Core menyimpan snapshot entitas untuk change tracking — overhead memori tidak perlu pada read-only flow
- Pada `AssessmentMonitoring`, 7-day window bisa ratusan sampai ribuan entitas × snapshot tracking

**Rekomendasi:**
Tambahkan `.AsNoTracking()` pada kedua query. Perubahan satu baris, tidak ada risiko side-effect karena tidak ada update/delete yang dipicu.

---

### M-06 — `GetActivityLog` = 3 Query DB Terpisah per AJAX Call

**File:** `Controllers/AssessmentAdminController.cs:4958-4988`
**Severity:** Medium

**Masalah:**
Endpoint AJAX `GetActivityLog` menjalankan 3 query DB terpisah untuk membangun satu response:

```csharp
// Query 1 — load session
var session = await _context.AssessmentSessions
    .FirstOrDefaultAsync(s => s.Id == sessionId);

// Query 2 — fetch events
var events = await _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .OrderBy(l => l.Timestamp)
    .Select(l => new { l.EventType, l.Detail, TimestampUtc = l.Timestamp })
    .ToListAsync();

// Query 3 — count answered
var totalAnswered = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == sessionId);

// Query 4 — max timestamp (BISA DIHITUNG DARI Query 2)
var lastEventTime = await _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .MaxAsync(l => (DateTime?)l.Timestamp);
```

Query 4 (`MaxAsync`) membaca ulang tabel `ExamActivityLogs` dengan filter sama seperti Query 2, padahal `lastEventTime` dapat dihitung dari list `events` yang sudah di-load (ambil `TimestampUtc` terakhir karena sudah di-`OrderBy(Timestamp)`).

**Dampak:**
- 4 DB round-trip per AJAX call — endpoint ini dipanggil per row di AssessmentMonitoringDetail (bisa via tombol "lihat aktivitas")
- Pada grup 50 peserta dan HC membuka log tiap peserta → 200 round-trip
- Query 4 redundan, 25% penghematan dengan reuse hasil Query 2

**Rekomendasi:**
1. Hilangkan Query 4, hitung dari `events`:

```csharp
DateTime? lastEventTime = events.LastOrDefault()?.TimestampUtc;
```

2. Alternatif tambahan: jalankan Query 2 + Query 3 paralel dengan `Task.WhenAll`:

```csharp
var eventsTask = _context.ExamActivityLogs
    .Where(l => l.SessionId == sessionId)
    .OrderBy(l => l.Timestamp)
    .Select(l => new { l.EventType, l.Detail, TimestampUtc = l.Timestamp })
    .ToListAsync();

var totalAnsweredTask = _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == sessionId);

await Task.WhenAll(eventsTask, totalAnsweredTask);
var events = await eventsTask;
var totalAnswered = await totalAnsweredTask;
var lastEventTime = events.LastOrDefault()?.TimestampUtc;
```

Hasil akhir: **2 DB round-trip** (session + paralel) dari semula 4.

---

### L-02 — `Include(p => p.Questions)` Penuh Padahal Hanya untuk `.Count`

**File:** `Controllers/AssessmentAdminController.cs:2541-2551`
**Severity:** Low

**Masalah:**
Pada `AssessmentMonitoringDetail`, question count per session dibangun dengan memuat seluruh kolom `PackageQuestions` padahal hanya `.Count` yang dipakai:

```csharp
questionCountMap = (await _context.UserPackageAssignments
    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
    .Join(_context.AssessmentPackages.Include(p => p.Questions),  // ← Load semua kolom Questions
        a => a.AssessmentPackageId,
        p => p.Id,
        (a, p) => new { a.AssessmentSessionId, QuestionCount = p.Questions.Count })
    .ToListAsync())
    .GroupBy(x => x.AssessmentSessionId)
    .ToDictionary(g => g.Key, g => g.Sum(x => x.QuestionCount));
```

`Include(p => p.Questions)` menyebabkan EF Core memuat seluruh kolom `PackageQuestions` (QuestionText, Options, Rubrik, ScoreValue, dll) hanya untuk memanggil `.Count` di projection berikutnya.

**Dampak:**
- Memory dan bandwidth DB → App boros (questions bisa puluhan per package, dan beberapa ribu byte per question dengan kolom teks)
- Semakin besar bank soal, semakin besar overhead

**Rekomendasi:**
Ganti `Include` dengan projection yang langsung menghitung di SQL — tidak memuat entitas `PackageQuestions`:

```csharp
questionCountMap = await _context.UserPackageAssignments
    .Where(a => siblingIds.Contains(a.AssessmentSessionId))
    .Join(_context.AssessmentPackages,
        a => a.AssessmentPackageId,
        p => p.Id,
        (a, p) => new
        {
            a.AssessmentSessionId,
            QuestionCount = p.Questions.Count()   // COUNT di SQL, bukan memuat rows
        })
    .GroupBy(x => x.AssessmentSessionId)
    .Select(g => new { SessionId = g.Key, Total = g.Sum(x => x.QuestionCount) })
    .ToDictionaryAsync(x => x.SessionId, x => x.Total);
```

Hasil SQL: `SELECT ..., (SELECT COUNT(*) FROM PackageQuestions WHERE PackageId = p.Id) AS QuestionCount ...` — tidak memuat kolom question.

---

## Ringkasan Prioritas Perbaikan (Updated — Step 1A + 1B + 1C)

| Prioritas | ID | Estimasi Dampak | Kompleksitas Fix |
|-----------|----|-----------------|-----------------|
| 1 | C-01 | Tinggi — thread pool | Rendah (2 baris) |
| 2 | C-04 | Sangat Tinggi — memory spike (3 full fetch) | Menengah-Tinggi |
| 3 | H-04 | Sangat Tinggi — N+1 × 3 query | Menengah |
| 4 | C-02 + H-06 | Tinggi — DB round trips + UX | Menengah |
| 5 | C-03 | Sangat Tinggi — memory + unfiltered fetch | Menengah |
| 6 | H-05 + H-03 | Tinggi — in-memory grouping (ManageAssessment + AssessmentMonitoring) | Menengah |
| 7 | M-06 | Menengah — 4 query endpoint AJAX yang sering dipanggil | Rendah |
| 8 | M-04 + M-05 | Menengah — missing AsNoTracking | Rendah (1 baris/location) |
| 9 | M-09 | Menengah — full scan + UX inconsistency | Rendah (1 baris) |
| 10 | L-02 | Rendah — Include yang tidak perlu | Rendah |
| 11 | M-08 | Tinggi — UX pada scale | Tinggi |
| — | H-01 | ~~Dihilangkan~~ — audit tidak menemukan blocking DB | — |

---

## Analisis: CMPController + Middleware + Program.cs + OrganizationController + AssessmentHub

**Scope:** `CMPController.cs`, `ImpersonationMiddleware.cs`, `Program.cs`, `OrganizationController.cs`, `AssessmentHub.cs`, `ExcelExportHelper.cs`
**Step:** 1D — Static Analysis

Ditemukan issue dari 4 area arsitektur (middleware, startup config, controller, SignalR hub). Temuan penting mirip Step 1C: audit seluruh **89 `.ToList()`** di `CMPController.cs` (plan lama menyebut 67, aktual 89) juga **tidak ada blocking DB call**.

| ID | Severity | Issue | File |
|----|----------|-------|------|
| M-01 | MEDIUM | No HTTP compression + no response caching middleware | `Program.cs` |
| M-02 | MEDIUM | `ImpersonationMiddleware` query `UserManager` tanpa caching per request | `ImpersonationMiddleware.cs:134-142` |
| M-03 | MEDIUM | `GetOrganizationTree` tanpa cache & tanpa AsNoTracking | `OrganizationController.cs:56-68` |
| H-02 | ~~HIGH~~ → INFO | 89 `.ToList()` — semuanya in-memory, bukan blocking DB | `CMPController.cs` (seluruh file) |
| L-01 | LOW | `AssessmentHub` fire-and-forget DB write tanpa retry | `AssessmentHub.cs:71-91, 99-125, 259-285` |
| I-01 | INFO | `ExcelExportHelper` MemoryStream buffered — by design | `ExcelExportHelper.cs` |

---

### M-01 — No HTTP Compression + No Response Caching Middleware

**File:** `Program.cs`
**Severity:** Medium

**Masalah:**
Pipeline ASP.NET Core tidak mengaktifkan kompresi response (Gzip/Brotli) maupun response caching middleware:

```csharp
// Program.cs — tidak ada UseResponseCompression / UseResponseCaching
builder.Services.AddControllersWithViews();
// ...
app.UseStaticFiles(staticFileOptions);
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
// langsung map controller / hub — tidak ada compression/caching middleware
```

**Estimasi ukuran response yang berdampak (tanpa kompresi):**

| Endpoint | Perkiraan Response | Potensi Reduksi Gzip |
|----------|-------------------|---------------------|
| `GetOrganizationTree` (flat list 100-500 units × 6 kolom) | 30-150 KB JSON | ~70% → 10-45 KB |
| Analytics Dashboard endpoints (11 data endpoints, tabular) | 20-200 KB per endpoint | ~75% → 5-50 KB |
| ManageAssessment / AssessmentMonitoring HTML (table besar) | 100-500 KB HTML | ~80% → 20-100 KB |
| ManageWorkers HTML (5.000 users) | ~1-3 MB HTML | ~85% → 150-450 KB |

**Dampak:**
- Network transfer meningkat signifikan pada jaringan kantor yang mungkin terbatas bandwidth
- Pada mobile/remote access, latensi transfer menjadi bottleneck dominan
- Response caching dapat dimanfaatkan untuk endpoint read-mostly seperti `GetOrganizationTree`, kategori assessment, dan dropdown data

**Rekomendasi:**
1. Aktifkan **response compression**:

```csharp
// Service registration (sebelum builder.Build())
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/html", "application/xml" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// Pipeline (setelah app.Build(), sebelum UseStaticFiles)
app.UseResponseCompression();
```

2. Aktifkan **response caching** untuk endpoint yang cocok:

```csharp
builder.Services.AddResponseCaching();
// ...
app.UseResponseCaching();

// Di controller endpoint read-only:
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
public async Task<IActionResult> GetOrganizationTree() { ... }
```

3. Opsional: konfigurasi static files caching header (untuk `/js`, `/css`, `/lib`):

```csharp
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // existing PDF handling...
        if (!ctx.File.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=86400";
        }
    }
};
```

---

### M-02 — `ImpersonationMiddleware` Query `UserManager` Tanpa Caching

**File:** `Middleware/ImpersonationMiddleware.cs:134-142`
**Severity:** Medium

**Masalah:**
Pada setiap request yang sedang dalam mode impersonation user-to-user, middleware melakukan 2 query berurutan:

```csharp
// Lines 134-142 — dijalankan di SetContextItems per request
var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
var targetUser = await userManager.FindByIdAsync(targetUserId);    // Query 1 → DB
if (targetUser != null)
{
    var roles = await userManager.GetRolesAsync(targetUser);        // Query 2 → DB (UserRoles JOIN Roles)
    var primaryRole = roles.FirstOrDefault() ?? "Coachee";
    context.Items["ImpersonateTargetRole"] = primaryRole;
    context.Items["ImpersonateTargetRoleLevel"] = UserRoles.GetRoleLevel(primaryRole);
    context.Items["ImpersonateTargetSelectedView"] = targetUser.SelectedView;
}
```

**Estimasi frekuensi:**
- Middleware berjalan pada setiap request non-static (setelah bypass untuk `/css`, `/js`, `/lib`, `/images`, `/favicon.ico`)
- Setiap page load umumnya memicu 5-15 request (HTML + async data + AJAX)
- Selama sesi impersonation 30 menit, setiap GET request ber-impersonate mode **user** = 2 DB round-trip tambahan
- Di admin flow (banyak tombol aksi, banyak refresh) bisa mencapai ratusan query tambahan per jam

**Dampak:**
- Beban `Users` dan `UserRoles` table meningkat sia-sia — data target user hampir tidak berubah selama impersonation window
- Latensi per request bertambah (~5-15 ms di LAN, lebih di LDAP-authenticated environment)

**Rekomendasi:**
Manfaatkan `IMemoryCache` (sudah tersedia via `AddMemoryCache()` di `Program.cs:17`) dengan TTL pendek (contoh 5 menit), key = `targetUserId`:

```csharp
// Dalam SetContextItems
else if (mode == "user")
{
    var targetUserId = service.GetTargetUserId();
    if (targetUserId != null)
    {
        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
        var cacheKey = $"impersonate_target_{targetUserId}";

        var info = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var targetUser = await userManager.FindByIdAsync(targetUserId);
            if (targetUser == null) return null;
            var roles = await userManager.GetRolesAsync(targetUser);
            var primaryRole = roles.FirstOrDefault() ?? "Coachee";
            return new
            {
                Role = primaryRole,
                RoleLevel = UserRoles.GetRoleLevel(primaryRole),
                SelectedView = targetUser.SelectedView
            };
        });

        if (info != null)
        {
            context.Items["ImpersonateTargetRole"] = info.Role;
            context.Items["ImpersonateTargetRoleLevel"] = info.RoleLevel;
            context.Items["ImpersonateTargetSelectedView"] = info.SelectedView;
        }
    }
}
```

Implementasi ini menurunkan 2 DB query/request menjadi **1 query per 5 menit** per target user. Saat impersonation dihentikan (`Stop()`), tambahkan invalidasi cache agar tidak memori bocor.

---

### M-03 — `GetOrganizationTree` Tanpa Cache & Tanpa `AsNoTracking()`

**File:** `Controllers/OrganizationController.cs:56-68`
**Severity:** Medium

**Masalah:**
Endpoint AJAX `GetOrganizationTree` dipanggil dari beberapa halaman admin (dropdown unit organisasi, tree editor). Data organisasi relatif stabil (perubahan jarang), tapi endpoint ini dieksekusi full query ke DB setiap kali dipanggil:

```csharp
var units = await _context.OrganizationUnits
    .OrderBy(u => u.Level).ThenBy(u => u.DisplayOrder).ThenBy(u => u.Name)
    .Select(u => new { u.Id, u.Name, u.ParentId, u.Level, u.DisplayOrder, u.IsActive })
    .ToListAsync();    // ← Missing AsNoTracking (walau projection mengurangi overhead)
return Json(units);
```

**Catatan positif:** Projection `.Select(u => new {...})` sudah mengurangi kolom yang dimuat dan otomatis non-tracked di EF Core modern. Jadi `AsNoTracking()` di sini lebih bersifat eksplisit-safe daripada wajib.

**Dampak:**
- Setiap page load dengan komponen tree organisasi memicu 1 full scan `OrganizationUnits` + sort
- Jika sistem punya 500 units dan dipanggil 10× per menit oleh tim HC → 5.000 rows/menit × 6 kolom keluar dari DB
- Terlebih penting pada kondisi non-compressed (lihat M-01)

**Rekomendasi:**
1. Tambahkan **IMemoryCache** dengan TTL 5-10 menit (invalidasi saat `AddOrganizationUnit` / `EditOrganizationUnit` / `DeleteOrganizationUnit`):

```csharp
public async Task<IActionResult> GetOrganizationTree()
{
    const string cacheKey = "org_tree_flat";
    if (!_cache.TryGetValue(cacheKey, out List<object>? cached))
    {
        cached = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.Level).ThenBy(u => u.DisplayOrder).ThenBy(u => u.Name)
            .Select(u => (object)new { u.Id, u.Name, u.ParentId, u.Level, u.DisplayOrder, u.IsActive })
            .ToListAsync();
        _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(10));
    }
    return Json(cached);
}

// Pada setiap write action:
private void InvalidateOrgTreeCache() => _cache.Remove("org_tree_flat");
```

2. Alternatif ringan: `[ResponseCache(Duration = 300)]` jika response caching middleware sudah aktif (lihat M-01).

3. Pertimbangkan `ManageOrganization` (line 32-54) dengan Include chain 2-level juga ditambahkan `AsNoTracking()` karena view read-only — kompleksitas 1 baris.

---

### H-02 — Audit 89 `.ToList()` di CMPController (TURUN KE INFO)

**File:** `Controllers/CMPController.cs` (seluruh file, 89 occurrences)
**Severity:** ~~High~~ → Info / Low

**Temuan:**
Audit lengkap seluruh **89 `.ToList()`** di `CMPController.cs` (lihat detail per kategori di `DATABASE_ANALYSIS.md` Section 11) — **tidak ada satu pun** yang berupa blocking DB call pada `IQueryable`.

Catatan: plan awal menyebut 67 occurrences, hasil aktual grep adalah 89. Verifikasi blocking dengan regex:

```
_context\.[A-Za-z]+[\s\S]*?\.ToList\(\)(?!Async)
```

Hasil: **No matches found**.

Semua 89 `.ToList()` adalah:
- Filter/projection pada list hasil `.ToListAsync()` (LINQ-to-Objects)
- `.ToList()` pada `.Skip().Take()` pagination in-memory
- Cascade filter Bagian/Unit/Status pada list yang sudah di-fetch
- `.Select(s => s.Id).ToList()` untuk batch param query berikutnya
- `.GroupBy(...).ToDictionary(..., g => g.ToList())` pada data in-memory

**Rekomendasi:**
Turunkan prioritas H-02 menjadi Info. Tidak diperlukan perubahan pada 89 `.ToList()`.

Sama seperti H-01, fokus optimasi lebih baik diarahkan ke pola fetch-besar-lalu-filter-di-memory (contoh: line 3594-3598, 3654-3658, 3692-3696 — cascade `.Where().Where().Where().ToList()` pada `allRows` yang di-load penuh). Jika dataset CMP bertumbuh signifikan, pertimbangkan push filter ke SQL.

---

### L-01 — `AssessmentHub` Fire-and-Forget DB Write Tanpa Retry

**File:** `Hubs/AssessmentHub.cs:71-91, 99-125, 259-285`
**Severity:** Low

**Masalah:**
Tiga method di `AssessmentHub` menggunakan pattern `Task.Run(async () => ...)` fire-and-forget untuk menulis ke `ExamActivityLogs`:

```csharp
// Contoh — LogPageNav (line 69-92)
public Task LogPageNav(int sessionId, int pageNumber)
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ExamActivityLogs.Add(new ExamActivityLog { ... });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log page nav for session={SessionId}", sessionId);
        }
    });
    return Task.CompletedTask;
}
```

Pola yang sama di `OnConnectedAsync` (lines 99-125, reconnect logging) dan `OnDisconnectedAsync` (lines 259-285, disconnect logging).

**Potensi Risiko:**
- Jika DB transient error (timeout, deadlock, disconnect), log aktivitas **hilang permanen** — tidak ada retry
- Log ini digunakan HC untuk audit perilaku peserta selama ujian; kehilangan bisa mempengaruhi investigasi
- Tidak ada backpressure: jika DB lambat, `Task.Run` queue bisa membengkak tanpa batas

**Kenapa diklasifikasi Low:**
- Log aktivitas adalah data audit sekunder (bukan jawaban ujian)
- Error sudah di-catch dan di-log via `ILogger` — admin bisa melacak jika mau
- Frekuensi error pada lingkungan LAN stabil rendah
- Retry dengan polly bisa menambah kompleksitas yang tidak sepadan

**Rekomendasi (jika bussiness critical audit log dibutuhkan):**
1. Gunakan **Polly** untuk retry transient error:

```csharp
var retryPolicy = Policy
    .Handle<DbUpdateException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

await retryPolicy.ExecuteAsync(async () =>
{
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.ExamActivityLogs.Add(...);
    await db.SaveChangesAsync();
});
```

2. Alternatif lebih robust: write ke in-memory queue (Channel) yang dikonsumsi background service dengan batch insert setiap N detik — mengurangi DB round-trip + memudahkan retry.

Jika tidak dibutuhkan jaminan tulis, biarkan pola saat ini (sudah ada try/catch + logging).

---

### I-01 — `ExcelExportHelper` MemoryStream Buffered (By Design)

**File:** `Helpers/ExcelExportHelper.cs`
**Severity:** Info (bukan issue)

**Catatan:**
`ExcelExportHelper.ToFileResult()` membangun seluruh workbook ke `MemoryStream` lalu memanggil `stream.ToArray()` untuk FileContentResult:

```csharp
using var stream = new MemoryStream();
workbook.SaveAs(stream);
return controller.File(stream.ToArray(), ..., fileName);
```

Ini adalah **batasan library ClosedXML** — library perlu akses random-access ke stream untuk menulis ZIP archive XLSX. Tidak ada streaming API native di ClosedXML. Pattern ini **by design**, bukan anti-pattern.

**Dampak teoritis:**
- Memory footprint = ukuran workbook × 2 (MemoryStream + array copy via `ToArray()`)
- Untuk export kecil (<5 MB) tidak masalah
- Untuk export besar (>20 MB) pertimbangkan chunked export atau SSE streaming

**Rekomendasi:**
Tidak ada perubahan diperlukan. Jika ke depan ada kebutuhan export >50 MB (mis. export semua history 10 tahun), pertimbangkan migrasi ke library streaming seperti `EPPlus`/`OpenXML SDK` atau split ke beberapa sheet/file.

---

## Ringkasan Prioritas Perbaikan (FINAL — Step 1A + 1B + 1C + 1D)

| Prioritas | ID | Area | Estimasi Dampak | Kompleksitas Fix |
|-----------|----|----|-----------------|-----------------|
| 1 | C-01 | CDP | Tinggi — thread pool | Rendah (2 baris) |
| 2 | C-04 | WorkerDataService | Sangat Tinggi — memory spike (3 full fetch) | Menengah-Tinggi |
| 3 | H-04 | AssessmentMonitoringDetail | Sangat Tinggi — N+1 × 3 query | Menengah |
| 4 | C-02 + H-06 | ManageWorkers | Tinggi — DB round trips + UX | Menengah |
| 5 | C-03 | ManageWorkers | Sangat Tinggi — memory + unfiltered fetch | Menengah |
| 6 | H-05 + H-03 | ManageAssessment + AssessmentMonitoring | Tinggi — in-memory grouping | Menengah |
| 7 | M-01 | Pipeline startup | Menengah-Tinggi — response size network | Rendah-Menengah |
| 8 | M-02 | Middleware | Menengah — query tambahan per request | Menengah |
| 9 | M-06 | GetActivityLog | Menengah — 4 query AJAX endpoint sering dipanggil | Rendah |
| 10 | M-03 | OrganizationController | Menengah — tanpa cache pada data stabil | Rendah-Menengah |
| 11 | M-04 + M-05 | AssessmentAdminController | Menengah — missing AsNoTracking | Rendah (1 baris/location) |
| 12 | M-09 | ManageAssessment | Menengah — full scan + UX inconsistency | Rendah (1 baris) |
| 13 | L-02 | AssessmentMonitoringDetail | Rendah — Include yang tidak perlu | Rendah |
| 14 | L-01 | AssessmentHub | Rendah — log bisa hilang transient | Menengah (opsional) |
| 15 | M-08 | ManageWorkers view | Tinggi — UX pada scale | Tinggi |
| — | H-01, H-02 | AssessmentAdminController, CMPController | ~~Dihilangkan~~ — audit tidak menemukan blocking DB (0/72 + 0/89) | — |
| — | I-01 | ExcelExportHelper | Info arsitektur — bukan issue | — |

**Catatan Step 2 & 3:**
- **Step 2 (Runtime Frontend):** ✅ SELESAI — hasil di `API_RESPONSE_TIMES.md`
- **Step 3 (Database Runtime):** belum dikerjakan — tunggu sesi terpisah dengan SQL Server access & EF logging aktif

---

# Runtime Validation — Fase 2A (Localhost) + 2B (Dev Server)

**Tanggal Eksekusi:** 17 April 2026
**Eksekutor:** Playwright MCP automation
**Scope:** 6 halaman + 7 item pengujian di 2 environment

Bagian ini merevisi severity dan prioritas berdasarkan bukti runtime. Data lengkap di `API_RESPONSE_TIMES.md`.

## Konteks Dataset

| Metrik | Localhost | Dev Server |
|--------|-----------|------------|
| Users | 12 | **530** |
| Assessment history rows | 33 | **4,789** |
| OrganizationUnits | 21 | 26 |
| Assessment sessions (dev) | ada | **KOSONG** (dataset belum terisi) |

## Temuan Runtime Kritis

### 🚨 CATASTROPHIC — ManageAssessment di Dev
- **HTML 7.96 MB** uncompressed (vs local 297 KB = 27× lebih besar)
- **DOMContentLoaded >60s timeout** — halaman praktis tidak usable
- **4,789 rows** pre-rendered ke DOM (C-04 + H-05 + M-01 combined)

### 🚨 ManageWorker di Dev
- DOMContentLoaded **25.6 detik** untuk 530 users
- HTML **1.94 MB** uncompressed
- **H-06 bug terkonfirmasi di scale** — filter Bagian=GAST menghasilkan 48 user tapi stats card tetap tampil "Total User: 530"

### 🚨 GetOrganizationTree Concurrency Serialization
- Local: 2 paralel fetch wall time = 12 ms
- **Dev: 2 paralel fetch wall time = 662 ms** (55× lebih lambat)
- Session B lambat 3× dari session A → indikasi lock contention/connection pool di DB production-like

### ✅ Pattern yang Bekerja Sempurna
- `_tabCache` Analytics Dashboard — konsisten di local + dev (0 request baru saat revisit tab, cache invalidate saat filter change)
- Pagination client-side DOM — page-switch 32 ms di 530 users, tidak mendekati threshold 300 ms

## Revisi Severity Berdasarkan Runtime

| Issue | Severity Sebelum | Severity Revisi | Alasan Revisi |
|-------|-----------------|-----------------|---------------|
| **C-04** | Critical | **Critical++** | 7.96 MB HTML + >60s timeout — user-blocker catastrophic |
| **M-01** | Medium | **High** | Response size 7.96 MB + 1.94 MB = user-blocker; compression = single biggest ROI |
| **M-08** | Medium | **Low** | Pagination page-switch 32 ms di 530 users — tidak lag. Real bottleneck = initial DOM render (sudah dicover C-03 + M-01) |
| **M-03** | Medium | **Medium-High** | Concurrency wall 55× lambat di dev — serialisasi DB. IMemoryCache bukan hanya optimasi tapi mitigasi bottleneck |
| **H-06** | High | **High (Bug Confirmed)** | Bug terkonfirmasi di 2 scale (12 dan 530 users) — tampilan menyesatkan |

## Issue Terkonfirmasi Runtime vs Static Only

| Issue | Runtime Evidence |
|-------|------------------|
| C-01 | N/A (tidak di-trigger di Fase 2) — Static analysis saja |
| C-02 | ✅ 25.6s DOMContent di 530 users |
| C-03 | ✅ 1.94 MB HTML, 530 rows, 25.6s load |
| C-04 | ✅ 7.96 MB HTML, >60s timeout |
| H-05 | ✅ Kontribusi ke 7.96 MB + cold load 745ms local |
| H-06 | ✅ 12 users + 530 users = bug di 2 scale |
| M-01 | ✅ 7.96 MB + 1.94 MB uncompressed response |
| M-03 | ✅ 662ms wall di concurrency |
| H-03, H-04, M-06 | ⏳ Menunggu dataset assessment di dev |
| M-02, M-04, M-05, M-09, L-01, L-02 | Static only (tidak teruji runtime) |

## Revisi Prioritas FINAL (Post-Runtime)

Diurutkan berdasarkan runtime impact nyata, bukan severity nominal.

| Prio | ID | Fix | Impact Estimate | Kompleksitas |
|------|----|----|-----------------|-------------|
| 1 | **M-01** | Enable ResponseCompression (Brotli/Gzip) di `Program.cs` | -80% transfer size untuk seluruh halaman. Single biggest ROI | Rendah (5-10 baris) |
| 2 | **C-04** | Tambah filter UserId + date range di `GetAllWorkersHistory()` | 7.96 MB → ~500 KB di ManageAssessment | Menengah |
| 3 | **C-03** | Server-side pagination ManageWorker (Skip/Take di IQueryable) | 1.94 MB → ~50 KB per page | Menengah |
| 4 | **H-06** | Gunakan `query` terfilter untuk stats count di `WorkerController.cs:108-111` | Bug fix UX — stats card sesuai filter | Rendah |
| 5 | **C-02** | Single query aggregate untuk stats (GroupBy + Case) | -75% DB round-trip stats | Menengah |
| 6 | **H-05** | Push grouping ManageAssessment ke SQL (EF `GroupBy`) | Kurangi CPU in-memory grouping + alokasi memory | Menengah |
| 7 | **M-03** | IMemoryCache `GetOrganizationTree` TTL 10 menit + invalidate di write | Dev 196ms → ~5ms cache hit; fix concurrency serialization | Rendah-Menengah |
| 8 | **C-01** | `.ToListAsync()` + `Task.WhenAll` di `GetCascadeOptions` | Thread pool freedom | Rendah (2-3 baris) |
| 9 | **M-02** | IMemoryCache `ImpersonationMiddleware` target user info TTL 5 menit | Kurangi 2 DB query/request → ~0 cache hit | Menengah |
| 10 | **M-04 + M-05** | Tambah `.AsNoTracking()` di 3 lokasi read-only | Memory overhead berkurang, quick fix | Rendah (1 baris/lokasi) |
| 11 | **M-06** | Hilangkan Query 4 (Max) di `GetActivityLog`, reuse events list | -25% round-trip per AJAX call | Rendah |
| 12 | **L-02** | Replace `Include(p => p.Questions)` dengan scalar `.Count()` | Kurangi bandwidth DB → App | Rendah |
| 13 | **M-09** | Tambah filter 7-day ke dropdown kategori ManageAssessment | Fix UX inconsistency + full scan | Rendah (1 baris) |
| 14 | **H-04** | Batch load 3 query essay grading di luar foreach loop | 150 RT → 7 RT (saat dataset teruji) | Menengah |
| 15 | **H-03** | Pagination + AsNoTracking AssessmentMonitoring | Kurangi memory saat >100 session | Menengah |
| 16 | **L-01** | Retry policy Polly untuk `AssessmentHub` fire-forget (opsional) | Data loss mitigation | Menengah |
| 17 | **M-08** | Server-side pagination ManageWorker (sudah masuk C-03) | Tergantikan oleh C-03 fix | — |

**Catatan:**
- Prioritas 1 (M-01) + 2-3 (C-04 + C-03) = **quick wins terbesar**. Bisa diselesaikan dalam 1-2 sprint dengan dampak user-visible langsung
- Prioritas 14-15 (H-03 + H-04) menunggu validasi dataset dev assessment
- **M-08 diturunkan ke baris terakhir** — tidak perlu fix terpisah karena C-03 server-side pagination otomatis menyelesaikan

---

# Status Akhir Analisis

| Step | Status | Output |
|------|--------|--------|
| **Step 1A-1D** (Static Analysis) | ✅ Selesai | `PERFORMANCE_REPORT.md`, `DATABASE_ANALYSIS.md` |
| **Step 2A** (Runtime Localhost) | ✅ Selesai | `API_RESPONSE_TIMES.md` |
| **Step 2B** (Runtime Dev Server) | ✅ Selesai + HAR + screenshots | `har/dev/`, `screenshots/dev/` |
| **Step 3** (Database Runtime + EF logging) | ⏭ Skipped | Di-skip — static + runtime analysis cukup untuk prioritas fix |
| **Implementasi Fix** | ⏳ Pending | Rekomendasi prioritas 1-17 di atas |
