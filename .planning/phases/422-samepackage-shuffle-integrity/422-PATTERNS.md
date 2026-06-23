# Phase 422: SamePackage & Shuffle Integrity - Pattern Map

**Mapped:** 2026-06-23
**Files analyzed:** 18 (3 new helpers + 1 helper extend + 1 controller + 1 model/dbcontext + 1 migration + 2 views + 8 new test + 1 test extend)
**Analogs found:** 18 / 18 (semua punya analog in-repo — fase ini murni hardening, ZERO greenfield)

> **Catatan kunci untuk planner:** Hampir SELURUH logika sudah ada di codebase. Pekerjaan = konsolidasi duplikasi ke pure-helper (kill-drift) + wire ke jalur bolong. JANGAN bikin abstraksi/library baru (CONTEXT specifics + RESEARCH "Don't Hand-Roll"). Semua excerpt di bawah punya file:line presisi — copy idiom langsung, jangan re-invent.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/PackageSizeAnalysis.cs` (NEW) | utility (pure-rules) | transform | `Helpers/ShuffleToggleRules.cs` | exact (pure static, EF-free, kill-drift) |
| `Helpers/ShuffleToggleRules.cs` (EXTEND D-04) | utility (pure-rules) | transform | self (`ShouldShowSizeMismatchWarning:18-20`) | exact (tambah method ON-path sebelahnya) |
| `Helpers/SiblingSessionQuery.cs` (REUSE, SHFX-06) | utility (query predicate) | transform | self (`SiblingPrePostAwarePredicate:14-24`) | exact (no edit — reuse di call-site) |
| `Controllers/AssessmentAdminController.cs` — `SyncToLinkedPostIfSamePackageAsync` (NEW private helper, D-06) | service (controller-private, EF-aware) | CRUD (deep-clone) | sync-block `CreateQuestion:6773-6786` + `SyncPackagesToPost:5875-5933` | exact (ekstrak blok copy-paste 5×) |
| `Controllers/AssessmentAdminController.cs` — `IsSessionEditLocked` (NEW static, D-07) | utility (pure predicate) | transform | ViewBag `ManagePackages:5811` (`isPostSession && SamePackage`) | exact (angkat inline → method) |
| `Controllers/AssessmentAdminController.cs` — `ToggleSamePackage` (NEW endpoint, D-01) | controller (POST) | request-response | `UpdateShuffleSettings:5623-5679` | exact (PRG+guard+sibling+audit template) |
| `Controllers/AssessmentAdminController.cs` — 5 endpoint POST guard (D-07) | controller (POST guard) | request-response | guard `UpdateShuffleSettings:5643-5647` | exact (TempData reject + redirect) |
| `Controllers/AssessmentAdminController.cs` — `CreatePackage` MAX+1 (D-02) | controller (POST) | CRUD | self `CreatePackage:5969-5976` (ganti count-based) | exact (in-place swap) |
| `Controllers/AssessmentAdminController.cs` — 5× `.ThenBy(p => p.Id)` (D-02) | controller (query) | transform | `OrderBy(p => p.PackageNumber)` ×5 (`:5447/5527/5572/5764/5895`) | exact (append ThenBy) |
| `Controllers/AssessmentAdminController.cs` — sibling key type-aware (SHFX-06) | controller (query) | transform | `SiblingSessionQuery.SiblingPrePostAwarePredicate` vs key `:5630-5635/5704-5706/5814-5819` | exact (ganti predicate) |
| `Controllers/AssessmentAdminController.cs` — `newPost` inherit SamePackage (SHFX-04) | controller (entity init) | CRUD | `newPost:2024-2045` (tambah 1 baris) | exact (mekanis) |
| `Controllers/AssessmentAdminController.cs` — Import wire sync (SHFX-01) | controller (POST) | CRUD | terminal `ImportPackageQuestions:6472-6486` + sync-block `:6773-6786` | exact (sisip sebelum return) |
| `Data/ApplicationDbContext.cs` — composite unique index (D-02) | config (fluent EF) | n/a | `ApplicationDbContext:359-361` (UserUnits composite unique) | exact |
| `Migrations/<ts>_AddPackageNumberUniqueIndex.cs` (NEW, D-02) | migration | batch | `20260618045427_AddUserUnitsTable.cs:35-57` (Sql backfill + CreateIndex) | role-match (dedup-then-index) |
| `Views/Admin/ManagePackages.cshtml` (toggle card + lock + warnings + hapus dup) | component (view) | request-response | shuffle card `:83-132` + lock banner `:29-44` + confirmDeletePackage `:405-412` | exact |
| `Views/Admin/ManagePackageQuestions.cshtml` (friendly disable) | component (view) | request-response | lock banner idiom `ManagePackages:29-44` | role-match (0 lock awareness saat ini) |
| 8 new test files (lihat Pattern Assignments §Tests) | test | request-response / transform | `ShuffleLockGuardTests.cs` (integration) + `ShuffleToggleRulesTests.cs` (pure) + `SiblingPrePostFilterTests.cs` | exact |
| `HcPortal.Tests/ShuffleToggleRulesTests.cs` (EXTEND, D-04) | test | transform | self (`Warning_Predicate:37-46`) | exact |

---

## Shared Patterns

### Kill-drift pure-helper (D-04, D-05, D-07 predicate)
**Source:** `Helpers/ShuffleToggleRules.cs:8-21` + `Helpers/RetakeRules.cs:18-100`
**Apply to:** `PackageSizeAnalysis.Compute`, `ShuffleToggleRules.ShouldShowKMinTruncationWarning`, `IsSessionEditLocked`
Logika keputusan/komputasi hidup di SATU static class pure (EF-free), dipanggil dari GET (ViewBag) DAN POST (guard) supaya tak divergen. Caller suplai FAKTA; helper memutuskan.
```csharp
// Helpers/ShuffleToggleRules.cs:8-21 (template kill-drift — XML-doc "Dipakai DI DUA TEMPAT")
public static class ShuffleToggleRules
{
    public static bool IsShuffleLocked(bool anyStarted, bool anyAssignment)
        => anyStarted || anyAssignment;

    public static bool ShouldShowSizeMismatchWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
        => packagesWithQuestions >= 2 && !shuffleQuestions && hasMismatch;
}
```

### TempData PRG (Post-Redirect-Get) toast — guard + warning (D-01, D-07)
**Source:** `AssessmentAdminController.cs:5643-5647` (shuffle lock reject) + `:5722-5725` (retake warning ko-eksis Success)
**Apply to:** SEMUA mutasi/guard fase 422 (toggle reject, lock-edit reject, shuffle warning)
```csharp
// AssessmentAdminController.cs:5643-5647 — guard reject idiom (mirror untuk D-01 anyStarted + D-07 IsSessionEditLocked)
if (ShuffleToggleRules.IsShuffleLocked(anyStarted, anyAssignment)) {
    TempData["Error"] = "Pengaturan pengacakan tidak dapat diubah karena sudah ada peserta yang memulai ujian.";
    return RedirectToAction("ManagePackages", new { assessmentId });
}
```
View render alert: `ManagePackages.cshtml:26` (`alert-danger` untuk `TempData["Error"]`, `alert-dismissible fade show` + `btn-close`).

### Server-authoritative RBAC + antiforgery (semua endpoint POST baru)
**Source:** `AssessmentAdminController.cs:5620-5623` (UpdateShuffleSettings attribute stack)
**Apply to:** `ToggleSamePackage` baru WAJIB stack identik
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleSamePackage(int assessmentId, bool samePackage) { ... }
```

### Audit log try/catch warn-only
**Source:** `AssessmentAdminController.cs:5663-5675` (UpdateShuffleSettings) + `:6050-6063` (DeletePackage)
**Apply to:** `ToggleSamePackage` (audit toggle ON/OFF). Pola: resolve `hcUser` → `actorNameStr = NIP - FullName` → `_auditLog.LogAsync(...)` dibungkus `try/catch (Exception ex) { _logger.LogWarning(...) }`.

### Migration Sql data-fix + CreateIndex (D-02, HIGHEST RISK)
**Source:** `Migrations/20260618045427_AddUserUnitsTable.cs:48-57` (Sql backfill, "Literal statik, no user input") + `:35-46` (CreateIndex)
**Apply to:** dedup ROW_NUMBER renumber SEBELUM `CreateIndex(unique:true)`. Lihat Pattern Assignment migration.

---

## Pattern Assignments

### `Helpers/PackageSizeAnalysis.cs` (NEW — utility, transform) — D-05

**Analog:** `Helpers/ShuffleToggleRules.cs` (struktur) + duplikasi yang DIHAPUS: `ManagePackages.cshtml:72-78` (view) ↔ `AssessmentAdminController.cs:5847-5852` (controller).

**Duplikasi sumber yang dikonsolidasi** (drift — kill keduanya, ganti satu helper):
```csharp
// AssessmentAdminController.cs:5845-5852 (controller copy)
var pkgWithQuestions = packages.Where(p => p.Questions.Any()).ToList();
int packagesWithQuestions = pkgWithQuestions.Count;
bool hasMismatch = false;
if (packagesWithQuestions > 0) {
    int refCount = pkgWithQuestions[0].Questions.Count;
    hasMismatch = pkgWithQuestions.Any(p => p.Questions.Count != refCount);
}
```
```razor
@* ManagePackages.cshtml:72-78 (view copy — re-derive, DRIFT) *@
bool hasMismatch = false; int? referenceCount = null;
if (packages.Any(p => p.Questions.Any())) {
    referenceCount = packages.Where(p => p.Questions.Any()).Select(p => p.Questions.Count).First();
    hasMismatch = packages.Where(p => p.Questions.Any()).Any(p => p.Questions.Count != referenceCount);
}
```
**New helper** (record-struct result, EF-free; suntik ke ViewBag, hapus kedua copy di atas):
```csharp
public static class PackageSizeAnalysis {
    public readonly record struct Result(int PackagesWithQuestions, int? ReferenceCount, bool HasMismatch);
    public static Result Compute(IEnumerable<AssessmentPackage> packages) {
        var withQ = packages.Where(p => p.Questions != null && p.Questions.Any()).ToList();
        if (withQ.Count == 0) return new Result(0, null, false);
        int refCount = withQ[0].Questions.Count;
        return new Result(withQ.Count, refCount, withQ.Any(p => p.Questions.Count != refCount));
    }
}
```
⚠️ Setelah ekstrak: controller `:5853-5856` set `ViewBag.PackagesWithQuestions/HasSizeMismatch` dari `Result`; view render ViewBag, JANGAN re-derive.

---

### `Helpers/ShuffleToggleRules.cs` (EXTEND — utility, transform) — D-04

**Analog:** self, `ShouldShowSizeMismatchWarning:18-20` (OFF-path). Tambah method ON-path sejajar (komplement `!shuffleQuestions` → `shuffleQuestions`).
```csharp
// EXISTING :18-20 (OFF-path warning — JANGAN diubah)
public static bool ShouldShowSizeMismatchWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
    => packagesWithQuestions >= 2 && !shuffleQuestions && hasMismatch;

// NEW D-04 (ON-path: "soal dipangkas ke K=min") — mirror, hanya beda !shuffleQuestions → shuffleQuestions
public static bool ShouldShowKMinTruncationWarning(int packagesWithQuestions, bool shuffleQuestions, bool hasMismatch)
    => packagesWithQuestions >= 2 && shuffleQuestions && hasMismatch;
```
K=min adalah fakta dari `ShuffleEngine.cs:117` (`int K = packages.Min(p => p.Questions.Count)`) — helper hanya MEMBACA mismatch, TIDAK menduplikasi algoritma min.

---

### `Helpers/SiblingSessionQuery.cs` (REUSE — utility, transform) — SHFX-06

**Analog:** self `SiblingPrePostAwarePredicate:14-24` (kanonik, dipakai IDENTIK di StartExam/Reshuffle). TANPA edit helper — ganti call-site type-agnostic di controller.
```csharp
// Helpers/SiblingSessionQuery.cs:14-24 (predicate kanonik — reuse apa adanya)
public static Expression<Func<AssessmentSession, bool>> SiblingPrePostAwarePredicate(
    string title, string category, DateTime scheduleDate, string? assessmentType)
{
    bool isPrePost = assessmentType == "PreTest" || assessmentType == "PostTest";
    return s => s.Title == title && s.Category == category && s.Schedule.Date == scheduleDate.Date
                && ( isPrePost ? s.AssessmentType == assessmentType
                               : (s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest") );
}
```
**Call-site type-AGNOSTIC yang DIGANTI** (3 tempat — sekarang Pre+Post tercampur = over-lock SHUF-ISS-01):
```csharp
// AssessmentAdminController.cs:5630-5635 (UpdateShuffleSettings) — komentar :5629 SALAH ("key identik StartExam")
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title && s.Category == assessment.Category && s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id).ToListAsync();
// Idem :5704-5706 (UpdateRetakeSettings) + :5814-5819 (GET ManagePackages shuffle state)
```
⚠️ **OPEN Q1 / Pitfall 4 (RESEARCH A3):** type-aware HANYA untuk **lock-detection** (`anyStarted`/`anyAssignment`). Propagation write shuffle Pre↔Post (`:5649-5659`, "Propagate ke SEMUA sibling grup") SENGAJA cross-type — JANGAN ubah scope propagation. Klarifikasi di plan; default = ubah seminimal (lock-detection saja) untuk backward-compat WAJIB.

---

### `AssessmentAdminController.cs` — `SyncToLinkedPostIfSamePackageAsync` (NEW private, D-06) — SHFX-01

**Analog:** sync-block `CreateQuestion:6773-6786` (template paling lengkap) yang di-copy-paste di 5 call-site → ekstrak SATU helper; wrap `SyncPackagesToPost:5875-5933` (sudah benar, JANGAN ubah).

**Blok copy-paste yang DIKONSOLIDASI** (template `:6773-6786`):
```csharp
// AssessmentAdminController.cs:6773-6786 (CreateQuestion sync — pola di-ekstrak)
var parentPkgCQ = await _context.AssessmentPackages.FindAsync(packageId);
if (parentPkgCQ != null) {
    var parentSession = await _context.AssessmentSessions.FindAsync(parentPkgCQ.AssessmentSessionId);
    if (parentSession?.AssessmentType == "PreTest" && parentSession.LinkedSessionId.HasValue) {
        var linkedPost = await _context.AssessmentSessions.FindAsync(parentSession.LinkedSessionId.Value);
        if (linkedPost != null && linkedPost.SamePackage)
            await SyncPackagesToPost(parentSession.Id, linkedPost.Id);
    }
}
```
**New helper** (terima PRE session id; cek Pre→linkedPost.SamePackage→sync):
```csharp
private async Task SyncToLinkedPostIfSamePackageAsync(int preSessionId) {
    var pre = await _context.AssessmentSessions.FindAsync(preSessionId);
    if (pre?.AssessmentType == "PreTest" && pre.LinkedSessionId.HasValue) {
        var post = await _context.AssessmentSessions.FindAsync(pre.LinkedSessionId.Value);
        if (post != null && post.SamePackage)
            await SyncPackagesToPost(pre.Id, post.Id);  // :5875-5933 deep-clone, sudah benar
    }
}
```
**6 jalur wire** (5 existing ganti blok inline → panggil helper; 1 BARU = Import):
- `CopyPackagesFromPre:5948` (sudah panggil `SyncPackagesToPost` langsung — boleh tetap atau seragamkan)
- `CreatePackage:5983-5991` → ganti blok inline → `await SyncToLinkedPostIfSamePackageAsync(assessment.Id);`
- `DeletePackage:6067-6076` → idem
- `CreateQuestion:6773-6786` → idem
- `EditQuestion:~7036-7047` → idem
- `DeleteQuestion:~7129-7140` → idem
- **`ImportPackageQuestions` (SHFX-01, BOCOR):** sisip SEBELUM `return RedirectToAction` (terminal `:6472-6486`):
```csharp
// AssessmentAdminController.cs:6472-6486 (terminal Import — TIDAK ada sync saat ini = SHUF-ISS-03 HIGH)
if (newQuestions.Count > 0) {
    _context.PackageQuestions.AddRange(newQuestions);
    using var importTx = await _context.Database.BeginTransactionAsync();
    try { await _context.SaveChangesAsync(); await importTx.CommitAsync(); }
    catch { await importTx.RollbackAsync(); throw; }
}
// >>> SISIP DI SINI (success-path): resolve pkg→AssessmentSessionId, lalu:
//     await SyncToLinkedPostIfSamePackageAsync(<preSessionId dari packageId>);
```
⚠️ Import beroperasi pada paket Pre (param `packageId` → `pkg.AssessmentSessionId` = Pre session id). Pitfall 3: guard `IsSessionEditLocked(packageSession)` di AWAL (tolak Import LANGSUNG ke Post terkunci), sync di AKHIR untuk Pre.

---

### `AssessmentAdminController.cs` — `IsSessionEditLocked` (NEW static, D-07) + guard 5 endpoint — SHFX-03

**Analog:** inline ViewBag `ManagePackages:5811` (`ViewBag.IsSamePackageLocked = isPostSession && assessment.SamePackage`) → angkat jadi static method; guard idiom `:5643-5647`.
```csharp
// Source inline :5811 → method
private static bool IsSessionEditLocked(AssessmentSession s)
    => s.AssessmentType == "PostTest" && s.SamePackage;
```
**Guard di AWAL 5 endpoint POST** (CreatePackage:5958, DeletePackage:5999, CreateQuestion:6625, EditQuestion:6842, DeleteQuestion:7084):
```csharp
// resolve session (CreatePackage = assessmentId; Delete*/Create*/Edit* = packageId → pkg.AssessmentSessionId)
var session = await _context.AssessmentSessions.FindAsync(assessmentId /* atau resolved */);
if (session != null && IsSessionEditLocked(session)) {
    TempData["Error"] = "Paket Post-Test ini terkunci (paket-sama). Edit soal harus dilakukan di sesi Pre-Test.";
    return RedirectToAction("ManagePackages", new { assessmentId = session.Id });
}
```
Tetap sembunyikan tombol di view (`ManagePackages:312/379` — `ViewBag.IsSamePackageLocked != true` sudah ada) untuk UX; guard server = tolak-keras (defense-in-depth, SHUF-ISS-02 root = view-only).

---

### `AssessmentAdminController.cs` — `ToggleSamePackage` (NEW endpoint, D-01) — SHFX-02

**Analog:** `UpdateShuffleSettings:5619-5679` (template PRG + guard + sibling + audit lengkap). Guard anyStarted idiom `:5638-5641`.
```csharp
// Template guard anyStarted (mirror untuk grup Pre+Post pasangan)
bool anyStarted = await _context.AssessmentSessions
    .AnyAsync(s => siblingSessionIds.Contains(s.Id) && s.StartedAt != null);
// D-01: tolak toggle bila anyStarted (StartedAt set / InProgress / Completed); belum-mulai = boleh
```
Struktur endpoint (lihat RESEARCH Code Example D-04 `:407-438` lengkap):
1. `FindAsync(assessmentId)` → NotFound bila null; validasi `AssessmentType=="PostTest" && LinkedSessionId.HasValue` (else TempData Error).
2. GUARD: `anyStarted` di grup → TempData Error + redirect (no write).
3. Set `post.SamePackage = samePackage` + `post.UpdatedAt = DateTime.UtcNow` + SaveChanges.
4. ON → `await SyncToLinkedPostIfSamePackageAsync(post.LinkedSessionId.Value)` + TempData Success (sync+lock). **SaveChanges SamePackage SEBELUM panggil helper** (urutan penting — helper baca `post.SamePackage==true`).
5. OFF → **KEEP paket clone** (JANGAN sync/delete — Pitfall 5) + TempData Success ("kunci dilepas; paket dipertahankan").
6. Audit try/catch warn-only (`:5663-5675`).

⚠️ **OPEN Q2 (RESEARCH):** `SyncPackagesToPost:5878-5889` hapus pkg+Q+Options Post TAPI tidak eksplisit hapus `UserPackageAssignment` → cek dangling assignment saat re-sync (guard D-01 no-started mengurangi risiko; test edge belum-mulai-tapi-ada-assignment).

---

### `AssessmentAdminController.cs` — `CreatePackage` MAX+1 + 5× ThenBy(Id) (D-02) — SHFX-05

**Analog:** self `CreatePackage:5969-5976` (count-based, DIGANTI) + 5 site `OrderBy(p => p.PackageNumber)`.
```csharp
// REPLACE :5969-5976 (count-based — bentrok setelah delete)
var maxNumber = await _context.AssessmentPackages
    .Where(p => p.AssessmentSessionId == assessmentId)
    .Select(p => (int?)p.PackageNumber).MaxAsync();   // null bila belum ada
var pkg = new AssessmentPackage {
    AssessmentSessionId = assessmentId,
    PackageName = packageName.Trim(),
    PackageNumber = (maxNumber ?? 0) + 1
};
```
**5× `.ThenBy(p => p.Id)`** (append ke setiap `OrderBy(p => p.PackageNumber)` — deterministik lintas reshuffle):
- `:5447`, `:5527`, `:5572`, `:5764` (GET ManagePackages), `:5895` (SyncPackagesToPost). Semua → `.OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)`.

---

### `AssessmentAdminController.cs` — `newPost` inherit SamePackage (SHFX-04)

**Analog:** `newPost:2024-2045` (entity init tambah-peserta D-31). `repPost = postGroup.First()` (`:1999`). Tambah 1 baris (mekanis):
```csharp
// newPost initializer :2024-2045 → tambah baris (analog BannerColor = repPost.BannerColor :2044)
SamePackage = repPost.SamePackage,
```

---

### `Data/ApplicationDbContext.cs` — composite unique index (D-02)

**Analog:** `ApplicationDbContext.cs:358-361` (UserUnits composite unique) + `:232-235` (filtered unique idiom, JANGAN dipakai — kolom non-nullable).
```csharp
// ApplicationDbContext.cs:359-361 (idiom composite unique — copy untuk AssessmentPackage)
entity.HasIndex(uu => new { uu.UserId, uu.Unit })
      .IsUnique()
      .HasDatabaseName("IX_UserUnits_UserId_Unit_Unique");
```
**Apply** (tambah blok `modelBuilder.Entity<AssessmentPackage>`):
```csharp
entity.HasIndex(p => new { p.AssessmentSessionId, p.PackageNumber })
      .IsUnique()
      .HasDatabaseName("IX_AssessmentPackages_SessionId_PackageNumber_Unique");
```
⚠️ Pitfall 2: `PackageNumber` = `int` NON-nullable (`AssessmentPackage.cs:19`) → **PLAIN unique, NO `.HasFilter`**. JANGAN tiru `:232-235`/`:353-356` (yang pakai filter untuk kolom nullable/IsPrimary).

---

### `Migrations/<ts>_AddPackageNumberUniqueIndex.cs` (NEW, D-02) — HIGHEST RISK

**Analog:** `Migrations/20260618045427_AddUserUnitsTable.cs:48-57` (Sql data-fix + `:35-46` CreateIndex). Filtered-index alt: `20260611001939_AddPendingProtonBypassActiveUniqueIndex.cs` (TIDAK dipakai — index plain). Generate via `dotnet ef migrations add AddPackageNumberUniqueIndex` lalu EDIT Up() sisip dedup SEBELUM CreateIndex.
```csharp
// Up() — dedup ROW_NUMBER SEBELUM CreateIndex (else CREATE fails on existing dup). Literal statik, no user input.
migrationBuilder.Sql(@"
    WITH Numbered AS (
        SELECT Id, ROW_NUMBER() OVER (PARTITION BY AssessmentSessionId ORDER BY PackageNumber, Id) AS rn
        FROM AssessmentPackages )
    UPDATE p SET p.PackageNumber = n.rn
    FROM AssessmentPackages p INNER JOIN Numbered n ON p.Id = n.Id;");
migrationBuilder.CreateIndex(
    name: "IX_AssessmentPackages_SessionId_PackageNumber_Unique",
    table: "AssessmentPackages",
    columns: new[] { "AssessmentSessionId", "PackageNumber" },
    unique: true);
// Down(): DropIndex saja (renumber tak di-revert — data-fix permanen aman).
```
**migration=TRUE** → notify IT (commit hash + flag). Verifikasi lokal `sqlcmd -C -I`: `SELECT AssessmentSessionId, PackageNumber, COUNT(*) ... GROUP BY ... HAVING COUNT(*) > 1` = 0 baris sebelum & sesudah.

---

### `Views/Admin/ManagePackages.cshtml` (component) — D-01/D-03/D-04/D-05

**Analog:** shuffle card `:83-132` (struktur card + form-switch + warning + AntiForgeryToken), lock banner `:29-44`, confirmDeletePackage `:405-412`.

**Toggle SamePackage card** (mirror shuffle card `:104-129` — form POST `ToggleSamePackage`, form-switch, `@Html.AntiForgeryToken()`, disable saat anyStarted):
```razor
@* mirror :104-128 — form-switch + disabled saat lock + Simpan button *@
<form method="post" asp-action="ToggleSamePackage" asp-controller="AssessmentAdmin">
    @Html.AntiForgeryToken()
    <input type="hidden" name="assessmentId" value="@ViewBag.AssessmentId" />
    <div class="form-check form-switch mb-2">
        <input class="form-check-input" type="checkbox" name="samePackage" value="true" @(...checked...) @(...disabled...) />
    </div>
    ...
</form>
```
**Hapus duplikasi mismatch** `:72-78` → render `ViewBag` dari `PackageSizeAnalysis` (D-05). **Warning ON-path** (D-04) tambah alert mirror `shuffleSizeWarning:124-127` dengan teks "soal dipangkas ke K=min". **Warning SamePackage+Acak ON** (D-03) alert non-blocking mirror `:117-123` (idiom `alert-warning d-flex bi-question-circle`).
**Confirm-before JS** (D-01 toggle ON timpa paket): mirror `confirmDeletePackage:405-412` (single-quote string, no double-quote di attribute) — Pitfall 6.

---

### `Views/Admin/ManagePackageQuestions.cshtml` (component) — friendly disable D-07

**Analog:** lock banner `ManagePackages.cshtml:29-44` (`alert-info` + `bi-lock-fill` + link "Buka Pre-Test"). View ini saat ini 0 lock awareness. Tambah banner + disable tombol Create/Edit/Delete soal saat `ViewBag.IsSamePackageLocked == true` (server SUDAH hard-reject via D-07 — ini cuma UX layer).

---

### Tests (test, request-response/transform)

| New/Extend File | Type | Analog | Pattern |
|-----------------|------|--------|---------|
| `PackageSizeAnalysisTests.cs` (NEW) | pure-unit | `ShuffleToggleRulesTests.cs:37-46` (`[Theory][InlineData]`) | no DB, no fixture; paritas hasMismatch/refCount/withQ |
| `ShuffleToggleRulesTests.cs` (EXTEND) | pure-unit | self `Warning_Predicate:37-46` | tambah `[Theory]` ON-path `ShouldShowKMinTruncationWarning` |
| `SessionEditLockTests.cs` (NEW) | unit + integration | `ShuffleToggleRulesTests` (unit) + `ShuffleLockGuardTests` (endpoint guard) | `IsSessionEditLocked` truth-table + 5 endpoint no-write saat locked |
| `SamePackageSyncTests.cs` (NEW) | integration | `ShuffleLockGuardTests.cs:22-30` (IClassFixture<ProtonCompletionFixture> + `[Trait("Category","Integration")]`) | Import ke Pre+SamePackage → Post ter-sync; no-op bila bukan PreTest/!SamePackage |
| `SamePackageToggleGuardTests.cs` (NEW) | integration | `ShuffleLockGuardTests.cs` (seed sibling + anyStarted) | toggle ON sync / OFF keep clone; guard anyStarted reject |
| `SamePackageInheritTests.cs` (NEW) | integration | `ShuffleLockGuardTests.cs` (fixture) | newPost inherit `SamePackage = repPost.SamePackage` |
| `PackageNumberUniqueTests.cs` (NEW) | integration | `ShuffleLockGuardTests.cs:150-165` (seed AssessmentPackage) | MAX+1 setelah delete tengah tak bentrok; index reject dup → DbUpdateException |
| `PackageNumberMigrationTests.cs` (NEW) | integration | `AddUserUnitsTable` Sql + `ShuffleLockGuardTests` fixture | seed dup → run renumber SQL → 0 dup, gap-free per session |
| `SiblingTypeAwareLockTests.cs` (NEW) atau extend `SiblingPrePostFilterTests.cs` | unit/integration | `SiblingPrePostFilterTests.cs:23-45` (`Pred(type).Compile()` in-memory) | Pre mulai → Post TIDAK terkunci (type-aware lock-detection) |

**Fixture pattern kanonik** (`ShuffleLockGuardTests.cs:22-30`):
```csharp
[Trait("Category", "Integration")]   // → skip via dotnet test --filter "Category!=Integration"
public class XxxTests : IClassFixture<ProtonCompletionFixture> {
    private readonly ProtonCompletionFixture _fixture;
    public XxxTests(ProtonCompletionFixture fixture) { _fixture = fixture; }
    // await using var ctx = new ApplicationDbContext(_fixture.Options);
    // marker = "TAG-" + Guid.NewGuid().ToString("N").Substring(0,8) untuk isolasi seed
}
```
**Pure-test pattern kanonik** (`ShuffleToggleRulesTests.cs:11-46`): no `using HcPortal.Data`, no fixture, `[Theory][InlineData]` truth-table.

---

## No Analog Found

(none) — Semua 18 file/perubahan punya analog langsung in-repo. Fase 422 adalah hardening murni: tidak ada role/data-flow baru yang belum pernah dikerjakan codebase.

---

## Metadata

**Analog search scope:** `Controllers/AssessmentAdminController.cs`, `Helpers/` (ShuffleToggleRules, SiblingSessionQuery, RetakeRules, ShuffleEngine), `Data/ApplicationDbContext.cs`, `Migrations/` (AddUserUnitsTable, AddPendingProtonBypassActiveUniqueIndex), `Models/AssessmentPackage.cs`, `Views/Admin/ManagePackages.cshtml`, `HcPortal.Tests/` (ShuffleLockGuardTests, ShuffleToggleRulesTests, SiblingPrePostFilterTests, ParticipantRemoveGuardTests).
**Files scanned:** 14 source/test/view + 2 upstream (CONTEXT/RESEARCH) + CLAUDE.md.
**Pattern extraction date:** 2026-06-23
**Branch:** ITHandoff (app port lokal 5270; sqlcmd -C -I; migration=TRUE → notify IT).
