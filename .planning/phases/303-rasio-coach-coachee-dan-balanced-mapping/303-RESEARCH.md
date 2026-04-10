# Phase 303: Rasio Coach-Coachee dan Balanced Mapping - Research

**Researched:** 2026-04-10
**Domain:** ASP.NET Core MVC — halaman analitik Coach Workload, threshold config, saran reassign, Chart.js bar chart
**Confidence:** HIGH (semua temuan dari codebase langsung)

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

- **D-01:** Halaman baru terpisah dari CoachCoacheeMapping — bukan embedded
- **D-02:** Nama menu: "Coach Workload"
- **D-03:** Penempatan menu: sebelum "Deliverable Progress Override" di sidebar CMP
- **D-04:** Bar chart horizontal — setiap coach = 1 bar, warna hijau/kuning/merah berdasarkan threshold
- **D-05:** Summary cards: Total Coach Aktif, Total Coachee Aktif, Rata-rata Rasio, Coach Overloaded
- **D-06:** Tabel detail per coach: Nama Coach, Section, Jumlah Coachee, Status
- **D-07:** Filter by section (dropdown)
- **D-08:** Export Excel — download data rasio ke Excel
- **D-09:** Beban = coachee aktif (IsActive=true DAN IsCompleted=false)
- **D-10:** Coachee graduated (IsCompleted=true) tidak dihitung dalam beban
- **D-11:** Saran Penyeimbangan — section terpisah, tombol Approve/Skip per saran
- **D-12:** Saran reassign prioritaskan coach tujuan di section yang sama dengan coachee
- **D-13:** Saran butuh approval Admin — tidak otomatis dijalankan
- **D-14:** Saat assign coachee baru, suggest coach beban terendah di section yang sama
- **D-15:** Threshold configurable via tombol + modal popup, tersimpan di database
- **D-16:** Threshold tersimpan di database (bukan hardcoded)
- **D-17:** Admin — full access (lihat, set threshold, approve saran)
- **D-18:** HC — read-only (lihat data rasio dan saran, tidak bisa approve/ubah threshold)
- **D-19:** Tidak perlu notifikasi otomatis

### Claude's Discretion

- Default value threshold awal
- Algoritma prioritas saran reassign
- Exact chart library/implementasi (Chart.js atau native)
- Styling dan spacing detail
- Empty state saat tidak ada data

### Deferred Ideas (OUT OF SCOPE)

Tidak ada — diskusi tetap dalam scope phase

</user_constraints>

---

## Summary

Phase 303 menambahkan halaman analitik "Coach Workload" yang menampilkan distribusi beban coach, summary cards, bar chart horizontal berwarna threshold, tabel detail, saran reassign, dan auto-suggest saat assign baru. Fitur ini dibangun di atas sistem `CoachCoacheeMapping` yang sudah ada dan mengikuti semua pola yang sudah mapan di proyek ini.

Halaman baru akan menggunakan controller `CoachMappingController` yang sudah ada (tambah action baru) karena domain-nya sama persis — coach-coachee mapping. Tidak perlu controller baru. Satu entitas baru diperlukan: `CoachWorkloadThreshold` untuk menyimpan konfigurasi threshold di database sesuai D-15/D-16.

Auto-suggest (D-14) diintegrasikan ke action `CoachCoacheeMappingAssign` yang sudah ada — menambahkan logika untuk menyertakan data beban coach saat merespons modal assign.

**Primary recommendation:** Tambah 2 action di `CoachMappingController` (`CoachWorkload` GET + `ExportCoachWorkload` GET), 2 action POST (`SetWorkloadThreshold`, `ApproveReassignSuggestion`), 1 entitas baru `CoachWorkloadThreshold`, dan modifikasi minimal di `CoachCoacheeMappingAssign` untuk auto-suggest.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | sudah ada di project | Excel export | Sudah digunakan di ExcelExportHelper [VERIFIED: codebase] |
| Chart.js | sudah di-load via CDN di project | Bar chart horizontal | Sudah digunakan di halaman AnalyticsDashboard [ASSUMED — perlu verifikasi di layout] |
| Bootstrap 5.3 | sudah ada | UI cards, modal, tabel | Layout standar project [VERIFIED: CONVENTIONS.md] |
| Bootstrap Icons | sudah ada | Icon set | Standar project [VERIFIED: CONVENTIONS.md] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| jQuery | sudah ada | AJAX call untuk approve/skip saran | Sudah ada di semua views [VERIFIED: CONVENTIONS.md] |
| EF Core LINQ | .NET 8 | Query CoachCoacheeMappings dengan filter | Pattern standar project [VERIFIED: CONVENTIONS.md] |

**Installation:** Tidak ada library baru — semua sudah tersedia di project.

---

## Architecture Patterns

### Recommended File Additions

```
Controllers/
└── CoachMappingController.cs         # MODIFY — tambah 4 action baru

Models/
└── CoachWorkloadThreshold.cs         # NEW — entity threshold config

Data/
└── ApplicationDbContext.cs           # MODIFY — tambah DbSet<CoachWorkloadThreshold>

Migrations/
└── {timestamp}_AddCoachWorkloadThreshold.cs  # NEW via dotnet ef

Views/Admin/
└── CoachWorkload.cshtml              # NEW — halaman utama

Views/CMP/
└── Index.cshtml                      # MODIFY — tambah menu card "Coach Workload"
```

### Pattern 1: Query Beban Coach (D-09, D-10)

**What:** Hitung beban aktif per coach — hanya mapping dengan `IsActive=true` DAN `IsCompleted=false`
**When to use:** Di action `CoachWorkload` GET dan saat generate saran reassign

```csharp
// Source: logika dari CoachMappingController.cs existing query pattern [VERIFIED: codebase]
var activeMappings = await _context.CoachCoacheeMappings
    .Where(m => m.IsActive && !m.IsCompleted)
    .ToListAsync();

var workloadByCoach = activeMappings
    .GroupBy(m => m.CoachId)
    .Select(g => new {
        CoachId = g.Key,
        ActiveCoacheeCount = g.Count()
    })
    .ToDictionary(x => x.CoachId, x => x.ActiveCoacheeCount);
```

### Pattern 2: Entitas Threshold Config

**What:** Tabel tunggal dengan 1 baris untuk menyimpan konfigurasi threshold
**When to use:** Saat Admin set threshold via modal (D-15, D-16)

```csharp
// Source: pola dari Models/CoachCoacheeMapping.cs [VERIFIED: codebase]
public class CoachWorkloadThreshold
{
    public int Id { get; set; }
    /// <summary>Batas maksimal coachee per coach. Default 5.</summary>
    public int MaxCoacheesPerCoach { get; set; } = 5;
    /// <summary>Batas warning (kuning). Default 4.</summary>
    public int WarningThreshold { get; set; } = 4;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedById { get; set; } = "";
}
```

**Default value (Claude's Discretion):** `MaxCoacheesPerCoach = 5`, `WarningThreshold = 4`. Reasoning: rasio 1:5 adalah batas lazim untuk coaching program korporat. Warning di 4 memberi sinyal awal sebelum limit.

### Pattern 3: Saran Reassign — Algoritma

**What:** Generate saran pindah coachee dari coach overloaded ke coach dengan beban rendah
**Algorithm (Claude's Discretion):**

1. Identifikasi coach overloaded: `ActiveCoacheeCount > MaxCoacheesPerCoach`
2. Untuk tiap coachee di coach overloaded, cari coach target kandidat:
   - Prioritas 1: Coach di section yang sama dengan coachee (D-12)
   - Prioritas 2: Coach dengan beban terendah
   - Prioritas 3: Jika beban coachee Tahun 1 lebih tinggi, prioritaskan coachee tersebut untuk dipindah lebih dahulu (Tahun 1 = ProtonTrack baru, lebih butuh redistribusi)
3. Satu saran per coachee (tidak multiple alternative)
4. Skip coach target yang sudah di threshold atau melebihi

```csharp
// Pseudocode algoritma saran
var suggestions = new List<ReassignSuggestion>();
foreach (var overloadedCoach in overloadedCoaches)
{
    var coachees = activeMappings.Where(m => m.CoachId == overloadedCoach.CoachId).ToList();
    foreach (var mapping in coachees)
    {
        var coacheeSection = userDict[mapping.CoacheeId]?.Section ?? "";
        var targetCoach = underloadedCoaches
            .Where(c => userDict[c.CoachId]?.Section == coacheeSection)
            .OrderBy(c => c.ActiveCoacheeCount)
            .FirstOrDefault()
            ?? underloadedCoaches.OrderBy(c => c.ActiveCoacheeCount).FirstOrDefault();

        if (targetCoach != null)
            suggestions.Add(new ReassignSuggestion { ... });
    }
}
```

### Pattern 4: Approve Reassign — POST Action

**What:** Admin approve satu saran reassign — update mapping CoachId
**When to use:** Tombol Approve di section saran (D-11, D-13)

```csharp
// Source: pola dari existing POST actions di CoachMappingController [VERIFIED: codebase]
[HttpPost]
[Authorize(Roles = "Admin")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveReassignSuggestion(int mappingId, string newCoachId)
{
    var mapping = await _context.CoachCoacheeMappings.FindAsync(mappingId);
    if (mapping == null) return NotFound();
    mapping.CoachId = newCoachId;
    await _context.SaveChangesAsync();
    await _auditLog.LogAsync(...);
    return Json(new { success = true });
}
```

### Pattern 5: Auto-Suggest di Assign Modal (D-14)

**What:** Saat assign coachee baru, kirimkan data beban coach ke frontend agar suggest coach terendah
**Integration point:** Action `CoachCoacheeMappingAssign` existing — tambah `ViewBag.CoachWorkloads`

```csharp
// Di CoachCoacheeMapping GET action — tambahkan workload data
var workloads = activeMappings
    .GroupBy(m => m.CoachId)
    .ToDictionary(g => g.Key, g => g.Count());
ViewBag.CoachWorkloads = workloads; // { coachId: count }
```

Di view assign modal (JavaScript):
```javascript
// Highlight coach dengan beban terendah saat section dipilih
// coachWorkloads = JSON dari ViewBag
const sorted = eligibleCoachesInSection.sort((a,b) =>
    (coachWorkloads[a.id] || 0) - (coachWorkloads[b.id] || 0));
// Tambah badge "(Beban: N)" di samping nama coach di dropdown
```

### Pattern 6: Role-based Access (D-17, D-18)

```csharp
// Admin: full access
[Authorize(Roles = "Admin")]
public async Task<IActionResult> SetWorkloadThreshold(...) { }

[Authorize(Roles = "Admin")]
public async Task<IActionResult> ApproveReassignSuggestion(...) { }

// Admin + HC: read-only view
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> CoachWorkload(...) { }
```

Di view: gunakan `@if (User.IsInRole("Admin"))` untuk show/hide tombol threshold dan approve.

### Anti-Patterns to Avoid

- **Jangan hardcode threshold:** Threshold harus dari database, bukan konstanta di C# (D-16)
- **Jangan hitung beban dari semua mapping:** Filter `IsActive && !IsCompleted` — mapping deactivated/completed tidak dihitung (D-09, D-10)
- **Jangan auto-execute reassign:** Saran harus melalui approval Admin (D-13)
- **Jangan buat controller baru:** Domain ini milik `CoachMappingController` — tambah action di sana, views tetap di `Views/Admin/`

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export | Custom StreamWriter | `ExcelExportHelper.ToFileResult()` + ClosedXML | Sudah ada helper, format konsisten [VERIFIED: codebase] |
| Pagination | Custom skip/take | `PaginationHelper.Calculate()` | Sudah ada helper [VERIFIED: codebase] |
| Section dropdown | Query manual | `_context.GetSectionUnitsDictAsync()` | Sudah ada helper di DbContext [VERIFIED: codebase] |
| Bar chart | SVG manual | Chart.js (sudah ada di project) | Responsif, interaktif, sudah digunakan [ASSUMED] |
| Anti-forgery | Manual token | `[ValidateAntiForgeryToken]` attribute | Standar project [VERIFIED: CONVENTIONS.md] |

---

## Common Pitfalls

### Pitfall 1: Lupa Filter IsCompleted
**What goes wrong:** Beban coach termasuk coachee yang sudah graduated
**Why it happens:** Query hanya filter `IsActive=true` tanpa `IsCompleted=false`
**How to avoid:** Selalu query `Where(m => m.IsActive && !m.IsCompleted)` untuk hitung beban
**Warning signs:** Summary card "Coach Overloaded" bernilai lebih tinggi dari ekspektasi

### Pitfall 2: Race Condition Saran Reassign
**What goes wrong:** Dua admin approve dua saran berbeda untuk coachee yang sama
**Why it happens:** Tidak ada lock saat approve
**How to avoid:** Sebelum apply reassign, re-check bahwa mapping masih aktif dan coachId masih sama; return error jika sudah berubah

### Pitfall 3: Threshold Null di Database
**What goes wrong:** Tidak ada row di tabel `CoachWorkloadThresholds` saat pertama deploy
**Why it happens:** Migration membuat tabel kosong
**How to avoid:** Di controller, jika `threshold == null`, gunakan default value (5) dan tampilkan UI untuk set threshold. Atau seed 1 row default di migration.

### Pitfall 4: CoachWorkload View Harus di Views/Admin/ Bukan Views/CMP/
**What goes wrong:** View tidak ditemukan karena salah folder
**Why it happens:** `CoachMappingController` override `View()` untuk resolve ke `~/Views/Admin/`
**How to avoid:** Simpan `CoachWorkload.cshtml` di `Views/Admin/`, bukan `Views/CMP/` meskipun diakses dari CMP menu

### Pitfall 5: Menu CMP Tidak Mengarah ke /Admin/CoachWorkload
**What goes wrong:** Link menu mengarah ke `CMP/CoachWorkload` yang tidak ada
**Why it happens:** Menu di `Views/CMP/Index.cshtml` menggunakan `Url.Action`
**How to avoid:** `@Url.Action("CoachWorkload", "CoachMapping")` — controller "CoachMapping" dengan route `/Admin/CoachWorkload`

---

## Code Examples

### Query Data untuk Halaman CoachWorkload

```csharp
// Source: pola dari CoachMappingController.CoachCoacheeMapping action [VERIFIED: codebase]
public async Task<IActionResult> CoachWorkload(string? section)
{
    // Load threshold (atau default jika belum ada)
    var threshold = await _context.CoachWorkloadThresholds.FirstOrDefaultAsync()
        ?? new CoachWorkloadThreshold { MaxCoacheesPerCoach = 5, WarningThreshold = 4 };

    // Load all users (untuk lookup nama/section)
    var allUsers = await _context.Users
        .Select(u => new { u.Id, u.FullName, u.Section, u.IsActive })
        .ToListAsync();
    var userDict = allUsers.ToDictionary(u => u.Id);

    // Hitung beban aktif per coach (D-09, D-10)
    var activeMappings = await _context.CoachCoacheeMappings
        .Where(m => m.IsActive && !m.IsCompleted)
        .ToListAsync();

    // Load coach role users
    var coachUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);
    var activeCoaches = coachUsers.Where(u => u.IsActive).ToList();

    // Build workload rows
    var workloadByCoach = activeMappings
        .GroupBy(m => m.CoachId)
        .ToDictionary(g => g.Key, g => g.Count());

    var rows = activeCoaches.Select(c => new {
        CoachId = c.Id,
        CoachName = c.FullName,
        CoachSection = c.Section ?? "",
        CoacheeCount = workloadByCoach.GetValueOrDefault(c.Id, 0),
        Status = workloadByCoach.GetValueOrDefault(c.Id, 0) >= threshold.MaxCoacheesPerCoach ? "Overloaded"
               : workloadByCoach.GetValueOrDefault(c.Id, 0) >= threshold.WarningThreshold ? "Warning"
               : "OK"
    }).ToList();

    // Filter by section
    if (!string.IsNullOrEmpty(section))
        rows = rows.Where(r => r.CoachSection == section).ToList();

    // Summary cards
    ViewBag.TotalActiveCoaches = rows.Count;
    ViewBag.TotalActiveCoachees = activeMappings.Count;
    ViewBag.AvgRatio = rows.Count > 0
        ? Math.Round((double)activeMappings.Count / rows.Count, 1) : 0;
    ViewBag.OverloadedCount = rows.Count(r => r.Status == "Overloaded");
    ViewBag.Threshold = threshold;
    ViewBag.WorkloadRows = rows.OrderByDescending(r => r.CoacheeCount).ToList();
    ViewBag.Sections = (await _context.GetSectionUnitsDictAsync()).Keys.ToList();
    ViewBag.SectionFilter = section;

    // Saran reassign — generate di sini atau via separate method
    ViewBag.ReassignSuggestions = GenerateReassignSuggestions(rows, activeMappings, userDict, threshold);

    return View();
}
```

### ExcelExportHelper Usage Pattern

```csharp
// Source: ExcelExportHelper.cs [VERIFIED: codebase]
public IActionResult ExportCoachWorkload()
{
    // ... query data sama seperti CoachWorkload action ...
    using var workbook = new XLWorkbook();
    var ws = ExcelExportHelper.CreateSheet(workbook, "Coach Workload",
        new[] { "Nama Coach", "Section", "Jumlah Coachee", "Status" });
    int row = 2;
    foreach (var r in rows)
    {
        ws.Cell(row, 1).Value = r.CoachName;
        ws.Cell(row, 2).Value = r.CoachSection;
        ws.Cell(row, 3).Value = r.CoacheeCount;
        ws.Cell(row, 4).Value = r.Status;
        row++;
    }
    return ExcelExportHelper.ToFileResult(workbook, "coach_workload.xlsx", this);
}
```

### CMP Index.cshtml — Tambah Menu Card (D-03)

```html
<!-- Source: Views/CMP/Index.cshtml existing pattern [VERIFIED: codebase] -->
<!-- Tempatkan SEBELUM card "Deliverable Progress Override" -->
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-12 col-md-6 col-lg-4">
    <div class="card border-0 shadow-sm h-100">
        <div class="card-body">
            <div class="d-flex align-items-center mb-3">
                <div class="icon-box bg-danger bg-opacity-10 text-danger rounded-3 p-3 me-3">
                    <i class="bi bi-people fs-3"></i>
                </div>
                <div>
                    <h5 class="mb-0">Coach Workload</h5>
                    <small class="text-muted">Rasio & Distribusi</small>
                </div>
            </div>
            <p class="text-muted mb-3">Pantau beban coach, rasio coachee, dan saran penyeimbangan assignment</p>
            <a href="@Url.Action("CoachWorkload", "CoachMapping")" class="btn btn-danger w-100">
                <i class="bi bi-arrow-right-circle me-2"></i>Lihat Workload
            </a>
        </div>
    </div>
</div>
}
```

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| — | Phase 303 adalah fitur baru, tidak ada predecessor | Greenfield dalam domain yang ada |

**Catatan penting:** Menu "Deliverable Progress Override" yang disebut di D-03 ternyata BELUM ADA di `Views/CMP/Index.cshtml` saat ini (hanya ada 3 menu: Manajemen Sertifikasi, Dasbor Analitik, Budget Training). [VERIFIED: codebase scan]. Artinya "sebelum Deliverable Progress Override" secara praktis berarti **ditambahkan sebagai card baru** di section HC/Admin di CMP/Index — posisi paling kiri di row baru atau sesuai diskusi.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Chart.js sudah di-load di project via layout/CDN | Standard Stack | Perlu tambah CDN di view CoachWorkload.cshtml — low risk, 1 baris |
| A2 | "Deliverable Progress Override" yang disebut D-03 mungkin belum ada atau ada di halaman lain | Code Examples (CMP Index) | Menu mungkin perlu ditempatkan di posisi berbeda |
| A3 | Default threshold 5 coachee max, warning 4 | Architecture Patterns | Bisa diubah Admin kapan saja via UI — zero risk setelah deploy |

---

## Open Questions

1. **"Deliverable Progress Override" tidak ditemukan di CMP/Index.cshtml**
   - What we know: Menu tersebut tidak ada di file yang diverifikasi
   - What's unclear: Apakah ada di halaman lain, atau belum diimplementasi, atau namanya berbeda
   - Recommendation: Planner tambahkan menu Coach Workload sebagai card terakhir di section HC/Admin di CMP/Index — atau tanya user saat phase execute

2. **Chart.js availability**
   - What we know: AnalyticsDashboard pasti menggunakan chart — perlu verifikasi library mana
   - What's unclear: Apakah Chart.js di-load di shared layout atau per-page
   - Recommendation: Lihat `Views/CMP/AnalyticsDashboard.cshtml` saat implementasi; jika belum ada, tambahkan CDN di CoachWorkload.cshtml

---

## Environment Availability

Step 2.6: SKIPPED — phase ini adalah pure code/config changes tanpa external dependencies baru. Semua tools (dotnet ef, ClosedXML, Bootstrap) sudah ada.

---

## Validation Architecture

Tidak ada automated test framework yang aktif untuk controller actions di proyek ini (Playwright E2E di `/tests/` — tidak ada unit test). Validasi dilakukan via manual browser testing sesuai pola project.

**Per task verification:**
- Build: `dotnet build` harus 0 error
- Migration: `dotnet ef database update` berhasil
- Manual: Akses `/Admin/CoachWorkload` — halaman render tanpa exception

**Phase gate:** Manual smoke test semua fitur sebelum commit final.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | ya | `[Authorize(Roles = "Admin")]` untuk approve/threshold, `[Authorize(Roles = "Admin, HC")]` untuk view |
| V5 Input Validation | ya | Validasi `mappingId` dan `newCoachId` di ApproveReassignSuggestion sebelum update DB |
| V2 Authentication | inherited | AdminBaseController sudah handle |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR — approve reassign mapping milik orang lain | Tampering | Verify mapping exists + IsActive sebelum update |
| Mass threshold manipulation | Tampering | `[Authorize(Roles = "Admin")]` on SetWorkloadThreshold |
| XSS di nama coach di chart | Tampering | Razor encoding otomatis; JSON serialization untuk chart data |

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CoachMappingController.cs` — query patterns, Excel export, section filter, role checks
- `Models/CoachCoacheeMapping.cs` — field definitions (IsActive, IsCompleted, CoachId, CoacheeId)
- `Data/ApplicationDbContext.cs` — DbSets, GetSectionUnitsDictAsync helper
- `Helpers/ExcelExportHelper.cs` — CreateSheet + ToFileResult pattern
- `.planning/codebase/CONVENTIONS.md` — naming, DI, route, auth patterns
- `.planning/codebase/STRUCTURE.md` — where to add new code
- `Views/CMP/Index.cshtml` — existing CMP menu structure

### Secondary (MEDIUM confidence)
- `303-CONTEXT.md` — user decisions yang menjadi dasar scope

### Tertiary (LOW confidence)
- Chart.js availability di project [ASSUMED — tidak diverifikasi di layout file]

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library dari codebase langsung
- Architecture: HIGH — mengikuti pola yang sudah terbukti di CoachMappingController
- Pitfalls: HIGH — ditemukan dari analisis kode aktual
- Saran reassign algorithm: MEDIUM — Claude's Discretion, logika dibuat masuk akal

**Research date:** 2026-04-10
**Valid until:** 2026-05-10 (stable codebase)
