# Phase 391: Penambahan Peserta Fleksibel saat Ujian Berjalan - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 2 (1 MODIFY + 1 CREATE)
**Analogs found:** 2 / 2 (100% — kedua file punya analog in-repo yang sudah dibaca langsung)

> ⚠️ **D-01 supersede ROADMAP/REQUIREMENTS.** CONTEXT.md D-01 mengganti kalimat usang "peserta baru mewarisi status induk". Sesi baru = `Open`/`Upcoming` **diturunkan dari jadwal** (DeriveReadyStatus), **BUKAN** `savedAssessment.Status`. Planner WAJIB map "mewarisi status induk" → "siap-mulai (Open/Upcoming)".

---

## File Classification

| New/Modified File | Op | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|----|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` | MODIFY | controller | request-response (form POST → EF update + insert) | (self: `CMPController.StartExam` untuk pola window/status; blok sibling-update existing dalam file yang sama) | exact (in-file pattern reuse) |
| `HcPortal.Tests/FlexibleParticipantAddTests.cs` | CREATE | test (integration real-SQL) | event-driven verify / data-level replication | `HcPortal.Tests/PostLisensorPolishTests.cs` | exact (same role + same data flow) |

Helper baru di dalam controller: `private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)` — analog logika: `CMPController.StartExam` L915/L953.

---

## Pattern Assignments

### `Controllers/AssessmentAdminController.cs` (controller, request-response) — 5 titik bedah

Surface tunggal: `EditAssessment` POST (~L1790-2229). Empat keputusan terkunci (D-01..D-04) + 1 helper baru. Semua pola yang dibutuhkan **sudah ada di repo** — phase ini = wire-ulang + filter, bukan bangun baru.

---

#### Titik A — Helper baru `DeriveReadyStatus` (D-01)

**Analog (source of truth):** `Controllers/CMPController.cs` StartExam — pola WIB.

Auto Upcoming→Open saat jadwal tiba (L914-924):
```csharp
// Auto-transition: Upcoming → Open when scheduled date+time has arrived in WIB (persisted to DB)
if (assessment.Status == "Upcoming" && assessment.Schedule <= DateTime.UtcNow.AddHours(7))
{
    if (!_impersonationService.IsImpersonating())
    {
        assessment.Status = "Open";
        assessment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
```

Window tutup (L952-957):
```csharp
// Enforce exam window close date (LIFE-02 / DATA-03)
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
{
    TempData["Error"] = "Ujian sudah ditutup. Waktu ujian telah berakhir.";
    return RedirectToAction("Assessment");
}
```

**Konstanta status** (jangan hardcode) — `Models/AssessmentConstants.cs` L13-22:
```csharp
public static class AssessmentStatus
{
    public const string Open = "Open";
    public const string Upcoming = "Upcoming";
    public const string Completed = "Completed";
    public const string InProgress = "InProgress";
    // ... PendingGrading, Cancelled, Abandoned
}
```

**Pattern to copy** (helper baru, mirror StartExam L915 — WIB = `DateTime.UtcNow.AddHours(7)`, JANGAN `DateTime.Now`/UTC polos):
```csharp
private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)
{
    var nowWib = DateTime.UtcNow.AddHours(7);
    // Jadwal sudah tiba → Open; belum → Upcoming (mirror StartExam L915).
    if (schedule <= nowWib)
        return AssessmentConstants.AssessmentStatus.Open;
    return AssessmentConstants.AssessmentStatus.Upcoming;
}
```
> ⚠️ Catatan konsistensi: cabang Pre-Post hardcode `Status = "Upcoming"` (L1940/L1961). Helper baru memakai konstanta `AssessmentConstants.AssessmentStatus.*` (anti-pattern hardcode). Ikuti konstanta untuk kode baru.

---

#### Titik B — BULK ASSIGN, ganti `Status` inherit (D-01) @ L2155-2174

**Current code** (`AssessmentAdminController.cs` L2155-2174):
```csharp
var newSessions = filteredNewUserIds.Select(uid => new AssessmentSession
{
    Title = savedAssessment.Title,
    Category = savedAssessment.Category,
    Schedule = savedAssessment.Schedule,
    DurationMinutes = savedAssessment.DurationMinutes,
    Status = savedAssessment.Status,            // ◄── L2161 TARGET D-01
    BannerColor = savedAssessment.BannerColor,
    IsTokenRequired = savedAssessment.IsTokenRequired,
    AccessToken = savedAssessment.AccessToken,
    PassPercentage = savedAssessment.PassPercentage,
    AllowAnswerReview = savedAssessment.AllowAnswerReview,
    ShuffleQuestions = savedAssessment.ShuffleQuestions,
    ShuffleOptions = savedAssessment.ShuffleOptions,
    GenerateCertificate = savedAssessment.GenerateCertificate,
    ExamWindowCloseDate = savedAssessment.ExamWindowCloseDate,
    Progress = 0,
    UserId = uid,
    CreatedBy = editUser?.Id
}).ToList();
```

**Change:** `Status = savedAssessment.Status` → `Status = DeriveReadyStatus(savedAssessment.Schedule, savedAssessment.ExamWindowCloseDate)`. Semua field lain TETAP.

**Idempotency context (sudah ada, jangan ubah)** — filter duplikat L2125-2137:
```csharp
var existingSiblingUserIds = await _context.AssessmentSessions
    .Where(a => a.Title == savedAssessment.Title
             && a.Category == savedAssessment.Category
             && a.Schedule.Date == savedAssessment.Schedule.Date)
    .Select(a => a.UserId).Distinct().ToListAsync();
var filteredNewUserIds = NewUserIds
    .Where(uid => !existingSiblingUserIds.Contains(uid)).Distinct().ToList();
```
> BULK ASSIGN sudah idempotent — PART tidak perlu menangani duplikat.

---

#### Titik C — Guard `Completed` berbasis window (D-02) @ L1992

**Current code** (`AssessmentAdminController.cs` L1991-1996):
```csharp
// Prevent editing completed assessments (optional - you can remove this if needed)
if (assessment.Status == "Completed")
{
    TempData["Error"] = "Cannot edit completed assessments.";
    return RedirectToAction("ManageAssessment");
}
```

**Pattern to copy** (mirror window-check StartExam L953; cek window grup, bukan status 1 sesi representatif):
```csharp
bool hasAddition = NewUserIds != null && NewUserIds.Count > 0;
// Guard Completed: blokir HANYA untuk EDIT murni (tanpa penambahan).
if (assessment.Status == AssessmentConstants.AssessmentStatus.Completed && !hasAddition)
{
    TempData["Error"] = "Cannot edit completed assessments.";
    return RedirectToAction("ManageAssessment");
}
// Penambahan → izinkan selama window terbuka (lihat Open Question A1 fallback null).
```
> JANGAN hapus guard total — D-02 = jangan salah-blokir penambahan, BUKAN buang guard EDIT murni. Untuk fallback `ExamWindowCloseDate == null` lihat Open Questions di RESEARCH.md (rekomendasi: null = boleh-tambah, sejajar StartExam yang hanya cek window bila `.HasValue`).

---

#### Titik D — Skip sesi berjalan di edit-loop (D-03) @ L2056-2075

**Fetch siblings (group-key, sudah ada)** L2050-2054:
```csharp
var siblings = await _context.AssessmentSessions
    .Where(a => a.Title == origTitle
             && a.Category == origCategory
             && a.Schedule.Date == origScheduleDate)
    .ToListAsync();
```

**Current loop overwrite SEMUA sibling** L2057-2075:
```csharp
var now = DateTime.UtcNow;
foreach (var sibling in siblings)
{
    sibling.Title = model.Title;
    sibling.Category = model.Category;
    sibling.Schedule = model.Schedule;
    sibling.DurationMinutes = model.DurationMinutes;
    sibling.Status = model.Status;
    sibling.BannerColor = model.BannerColor;
    // ... IsTokenRequired, AccessToken, PassPercentage, AllowAnswerReview,
    //     ShuffleQuestions, ShuffleOptions, GenerateCertificate, ExamWindowCloseDate
    sibling.UpdatedAt = now;
}
```

**Pattern to copy** (tambah guard di awal body — default aman = skip total, lindungi timer & integritas):
```csharp
foreach (var sibling in siblings)
{
    // D-03: sesi sedang berjalan → jangan sentuh field volatil apa pun.
    if (sibling.StartedAt != null && sibling.CompletedAt == null) continue;

    sibling.Title = model.Title;
    // ... (sisa field tak berubah)
    sibling.UpdatedAt = now;
}
```
> Predikat `StartedAt != null && CompletedAt == null` = definisi "berjalan", konsisten dengan `hasInProgress` query L2078-2081. Reuse predikat yang sama.

---

#### Titik E — Notice informatif (D-04) @ L2077-2085

**Current code** (`AssessmentAdminController.cs` L2077-2085):
```csharp
var hasInProgress = await _context.AssessmentSessions
    .AnyAsync(s => s.Title == origTitle && s.Category == origCategory
        && s.Schedule.Date == origScheduleDate
        && s.StartedAt != null && s.CompletedAt == null);
if (hasInProgress)
{
    TempData["Warning"] = "Perhatian: Ada peserta yang sedang mengerjakan ujian. Perubahan Title/Category/Schedule tidak akan berlaku untuk sesi yang sedang berjalan.";
}
```

**Pattern to copy** (ganti `Warning` → `Info` + wording menenangkan — `_Layout.cshtml` L210-219 SUDAH render `Info` sebagai alert biru; NOL perubahan view):
```csharp
if (hasInProgress)
{
    TempData["Info"] = "Ada peserta yang sedang mengerjakan ujian. " +
        "Peserta baru tetap dapat ditambahkan dan langsung mulai selama waktu ujian masih terbuka. " +
        "Sesi peserta yang sedang berjalan tidak terpengaruh perubahan.";
}
```
> JANGAN pakai `TempData["Success"]` untuk notice — Success di-render DUA KALI di ManageAssessment (layout L220 + page block). `Info` render SEKALI (hanya layout L210-219), warna netral.

---

#### Titik F — Pre-Post branch (D-05 konsistensi) @ L1806-1988

**Already correct** (jangan over-engineer):
- Sesi baru di-set `Status = "Upcoming"` (L1940 `newPre` + L1961 `newPost`) — sudah sesuai D-01.
- `return RedirectToAction("ManageAssessment")` di L1987 — SEBELUM guard `Completed` L1992, jadi D-02 sudah aman di sini.

**Yang perlu dipertimbangkan (low-risk, RESEARCH Pitfall 6 / A2):** shared-field loop Pre-Post (L1832-1844) & per-phase loop (L1847-1866) menimpa SEMUA sesi grup. Terapkan filter sesi-berjalan (D-03) bila relevan untuk konsistensi:
```csharp
foreach (var s in allGroupSessions)
{
    // (terapkan guard sesi-berjalan bila ada Pre/Post InProgress — umumnya belum mulai, low-risk)
    s.Title = model.Title;
    s.Category = model.Category;
    // ...
}
```
> JANGAN duplikasi guard `Completed`/window ke Pre-Post — ia sudah `return` sebelum guard.

---

### `HcPortal.Tests/FlexibleParticipantAddTests.cs` (test, integration real-SQL)

**Analog:** `HcPortal.Tests/PostLisensorPolishTests.cs` — copy fixture + replication-at-data-level pattern.

**Imports + fixture pattern** (L13-58):
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class FlexibleParticipantAddFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public FlexibleParticipantAddFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();   // disposable DB; HcPortalDB_Dev TIDAK tersentuh
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException(
                $"Phase 391 FlexibleParticipantAdd setup failed during MigrateAsync of {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug fix. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}
```

**Test class + trait pattern** (L60-66):
```csharp
[Trait("Category", "Integration")]
public class FlexibleParticipantAddTests : IClassFixture<FlexibleParticipantAddFixture>
{
    private readonly FlexibleParticipantAddFixture _fixture;
    public FlexibleParticipantAddTests(FlexibleParticipantAddFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
```

**Seed helper pattern** (L68-94) — adaptasi untuk kebutuhan 391 (sesi sibling per status):
```csharp
private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
{
    var u = new ApplicationUser { UserName = "part-" + Guid.NewGuid().ToString("N")[..8], Email = "part@test.local", FullName = "Flex Test" };
    ctx.Users.Add(u);
    await ctx.SaveChangesAsync();
    return u.Id;
}

private static async Task<int> SeedSiblingSessionAsync(ApplicationDbContext ctx, string userId, string title, string category, DateTime schedule, string status, DateTime? startedAt = null, DateTime? completedAt = null)
{
    var session = new AssessmentSession
    {
        UserId = userId, Title = title, Category = category, Status = status, AccessToken = "",
        Schedule = schedule, DurationMinutes = 60, PassPercentage = 70,
        StartedAt = startedAt, CompletedAt = completedAt, Progress = 0
    };
    ctx.AssessmentSessions.Add(session);
    await ctx.SaveChangesAsync();
    return session.Id;
}
```

**Replication-at-data-level pattern** (PostLisensorPolish L116-167) — replikasi keputusan controller PERSIS di test (helper berat di-instantiate; pola project = replikasi byte-identik, BUKAN WebApplicationFactory). Helper test harus mirror `DeriveReadyStatus` + filter sesi-berjalan + cek window:
```csharp
// Mirror DeriveReadyStatus controller byte-identik
private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate)
{
    var nowWib = DateTime.UtcNow.AddHours(7);
    return schedule <= nowWib ? S.Open : S.Upcoming;
}
```

**Fact + assertion pattern** (L169-229) — 4 fact (a/b/c/d) per D-06:
```csharp
[Fact]
public async Task AddParticipant_WithInProgressSibling_CreatesNewSession()  // (a)
{
    await using var ctx = NewCtx();
    // seed grup: 1 sesi InProgress (StartedAt set) + tambah peserta baru
    // Assert: sesi baru tercipta (sibling count bertambah)
}

[Fact]
public async Task AddParticipant_NewSession_HasReadyStatus_NotInProgress()  // (b)
{
    // Assert: Status sesi baru ∈ {Open, Upcoming}, BUKAN InProgress
    // schedule masa lalu → Open; schedule masa depan → Upcoming
}

[Fact]
public async Task AddParticipant_InProgressSibling_StatusScheduleDurationUnchanged()  // (c)
{
    // seed sesi InProgress dgn Status/Schedule/Duration awal → tambah peserta + edit field bersama
    // Assert: sesi InProgress UNCHANGED (filter D-03 skip)
}

[Fact]
public async Task AddParticipant_SomeCompleted_NotBlocked_WhileWindowOpen()  // (d)
{
    // seed grup dgn sebagian sesi Completed + window terbuka → penambahan harus lolos
    // Assert: tidak terblokir, sesi baru tercipta
}
```
> Setiap fact pakai `await using var ctx = NewCtx();` untuk seed + `await using var verify = NewCtx();` terpisah untuk assert (re-query dari DB, bukan in-memory tracking). Pola verify-separate-context di PostLisensorPolish L183-186/L225-228.

---

## Shared Patterns

### WIB time comparison (single-source)
**Source:** `Controllers/CMPController.cs` StartExam L915 & L953
**Apply to:** Helper `DeriveReadyStatus`, guard window D-02, test helper
```csharp
var nowWib = DateTime.UtcNow.AddHours(7);   // Schedule/ExamWindowCloseDate = WIB-local naive
// JANGAN DateTime.Now (TZ mesin) — JANGAN DateTime.UtcNow polos (regresi d844c552)
```

### AssessmentStatus constants (anti-hardcode)
**Source:** `Models/AssessmentConstants.cs` L13-22
**Apply to:** Semua set/compare status di kode baru (helper, guard, test)
```csharp
AssessmentConstants.AssessmentStatus.Open       // "Open"
AssessmentConstants.AssessmentStatus.Upcoming   // "Upcoming"
AssessmentConstants.AssessmentStatus.Completed  // "Completed"
AssessmentConstants.AssessmentStatus.InProgress // "InProgress"
// Test pakai alias: using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;
```

### Sibling group-key query
**Source:** `AssessmentAdminController.cs` L2050-2054 (fetch), L2078-2081 (hasInProgress), L2125-2131 (bulk-assign filter)
**Apply to:** Filter sesi-berjalan (D-03), seed grup di test
```csharp
.Where(a => a.Title == origTitle
         && a.Category == origCategory
         && a.Schedule.Date == origScheduleDate)
// "Satu assessment" = sibling = Title + Category + Schedule.Date. JANGAN buat definisi grup baru.
```

### "Sesi berjalan" predicate
**Source:** `AssessmentAdminController.cs` L2081, `CMPController.cs` (StartedAt/CompletedAt semantics)
**Apply to:** Filter D-03, fact (c)
```csharp
sibling.StartedAt != null && sibling.CompletedAt == null   // = sedang ujian (lindungi timer)
```

### Flash message via TempData → _Layout render
**Source:** `Views/Shared/_Layout.cshtml` L190-228
**Apply to:** Notice D-04
```
TempData["Info"]    → alert biru   (L210-219) — render SEKALI ✅ pakai ini untuk D-04
TempData["Warning"] → alert kuning "Warning:" (L190-199) — kesan-error ❌ JANGAN
TempData["Success"] → alert hijau  — DOUBLE render di ManageAssessment ❌ JANGAN untuk notice
TempData["Error"]   → alert merah  (L200-209)
```

---

## No Analog Found

Tidak ada. Kedua file punya analog langsung di repo:
- Controller pattern = reuse in-file (StartExam window/status, sibling-update loop existing).
- Test = `PostLisensorPolishTests.cs` (Phase 387, pola identik).

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| — | — | — | Semua tercakup |

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdminController, CMPController), `Models/AssessmentConstants.cs`, `HcPortal.Tests/`, `Views/Shared/_Layout.cshtml`
**Files scanned (read langsung):** 5 — AssessmentAdminController.cs (L1825-1988, L1980-2089, L2110-2229), CMPController.cs (L905-1010), AssessmentConstants.cs (full), PostLisensorPolishTests.cs (L1-229), _Layout.cshtml (L188-229)
**Line anchors re-verified:** ya — nomor baris cocok dengan CONTEXT/RESEARCH (guard L1992, edit-loop L2057-2075, warning L2082, Status inherit L2161, BULK ASSIGN L2114-2226, Pre-Post `Upcoming` L1940/L1961, StartExam window L915/L953)
**Pattern extraction date:** 2026-06-17
