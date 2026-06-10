# Proton Completion Logic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bikin kelulusan Proton konsisten — exam Tahun 1/2 yang lulus ikut menerbitkan penanda `ProtonFinalAssessment` (dulu cuma interview Tahun 3), dengan gate berurutan (deliverable 100% → final assessment) yang dipaksa di server + gate antar-tahun.

**Architecture:** Satu helper service `ProtonCompletionService` jadi sumber tunggal pembuatan/penghapusan penanda (`Origin` = Exam/Interview/Bypass), dipanggil dari `GradingService` (exam), `AssessmentAdminController.SubmitInterviewResults` (interview), dan nanti Bypass. Gate eligibility (deliverable 100% + Tahun N-1 lulus) divalidasi server-side di `CreateAssessment` POST. Logic murni (cross-year) diekstrak ke helper static biar bisa di-unit-test tanpa DbContext (pola `CoacheeEligibilityCalculator`).

**Tech Stack:** ASP.NET Core MVC, EF Core (SQL Server), xUnit (`HcPortal.Tests`). Lokal: `Authentication__UseActiveDirectory=false dotnet run` → `http://localhost:5277`. DB lokal `HcPortalDB_Dev` (SQLEXPRESS).

**Spec sumber:** `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md`

**Catatan migration:** Task 1 = 1 schema migration (`Origin`). Notify IT flag migration (DEV_WORKFLOW). Snapshot DB lokal sebelum apply (SEED_WORKFLOW).

---

## File Structure

| File | Tanggung jawab | Aksi |
|------|----------------|------|
| `Models/ProtonModels.cs` | Tambah `Origin` di `ProtonFinalAssessment` | Modify |
| `Migrations/<ts>_AddOriginToProtonFinalAssessment.cs` | Schema migration kolom `Origin` + set existing="Interview" | Create (via dotnet ef) |
| `Helpers/ProtonYearGate.cs` | Logic murni cross-year (Tahun N butuh N-1 lulus) — unit-testable | Create |
| `Services/ProtonCompletionService.cs` | Ensure/Remove penanda + cek tahun lulus, sumber tunggal | Create |
| `Program.cs` | Register `ProtonCompletionService` di DI | Modify |
| `Services/GradingService.cs` | Panggil helper saat exam Proton lulus / re-grade flip | Modify |
| `Controllers/AssessmentAdminController.cs` | Refactor SubmitInterviewResults ke helper + gate server-side di CreateAssessment POST | Modify |
| `Controllers/CoachMappingController.cs` | Cross-year gate di GetEligibleCoachees + netralin shortcut Tahun 3 + graduation gate | Modify |
| `Controllers/CDPController.cs` + view/viewmodel | Hapus tampilan `CompetencyLevelGranted` + grafik tren | Modify |
| `Controllers/AdminController.cs` (atau MaintenanceController) | Endpoint backfill 1x idempotent | Modify/Create |
| `HcPortal.Tests/*` | Test per task | Create |

---

## Task 1: Tambah `Origin` ke ProtonFinalAssessment + migration

**Files:**
- Modify: `Models/ProtonModels.cs:207-226` (class `ProtonFinalAssessment`)
- Create: migration via `dotnet ef`

- [ ] **Step 1: Tambah properti `Origin`**

Di `Models/ProtonModels.cs`, dalam class `ProtonFinalAssessment`, setelah `public string Status { get; set; } = "Completed";`:

```csharp
    /// <summary>Asal penanda: "Exam" (exam Tahun 1/2), "Interview" (Tahun 3), "Bypass" (admin override). Nullable utk baris lama (di-set "Interview" oleh migration). Dipakai re-grade Pass→Fail agar hanya hapus penanda Origin=="Exam".</summary>
    [MaxLength(20)]
    public string? Origin { get; set; }
```

- [ ] **Step 2: Buat migration**

Run:
```bash
dotnet ef migrations add AddOriginToProtonFinalAssessment
```
Expected: file migration baru di `Migrations/`.

- [ ] **Step 3: Set baris lama → "Interview" di migration Up()**

Di file migration baru, setelah `AddColumn<string>(... "Origin" ...)`, tambah di akhir `Up()`:

```csharp
    migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin = 'Interview' WHERE Origin IS NULL;");
```
(Semua penanda lama = interview Tahun 3, satu-satunya jalur pembuatan sebelumnya.)

- [ ] **Step 4: Snapshot DB lokal + apply**

Run (catat di `docs/SEED_JOURNAL.md` dulu — snapshot):
```bash
dotnet ef database update
dotnet build
```
Expected: build 0 error, migration applied.

- [ ] **Step 5: Commit**

```bash
git add Models/ProtonModels.cs Migrations/
git commit -m "feat(proton): add Origin marker to ProtonFinalAssessment"
```

---

## Task 2: Helper murni `ProtonYearGate` (cross-year) + test

**Files:**
- Create: `Helpers/ProtonYearGate.cs`
- Test: `HcPortal.Tests/ProtonYearGateTests.cs`

- [ ] **Step 1: Tulis test gagal**

`HcPortal.Tests/ProtonYearGateTests.cs`:

```csharp
using HcPortal.Helpers;
using Xunit;

public class ProtonYearGateTests
{
    [Theory]
    [InlineData("Tahun 1", new string[] { }, true)]              // entry, no prereq
    [InlineData("Tahun 2", new[] { "Tahun 1" }, true)]           // N-1 lulus
    [InlineData("Tahun 2", new string[] { }, false)]             // N-1 belum
    [InlineData("Tahun 3", new[] { "Tahun 1", "Tahun 2" }, true)]
    [InlineData("Tahun 3", new[] { "Tahun 1" }, false)]          // N-1 (Tahun 2) belum
    public void IsYearUnlocked_enforces_previous_year(string target, string[] passedYears, bool expected)
    {
        Assert.Equal(expected, ProtonYearGate.IsYearUnlocked(target, passedYears));
    }
}
```

- [ ] **Step 2: Run test → FAIL**

Run: `dotnet test --filter ProtonYearGateTests`
Expected: FAIL (ProtonYearGate tidak ada).

- [ ] **Step 3: Implementasi helper**

`Helpers/ProtonYearGate.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Gate antar-tahun (A-5.1): Tahun N hanya boleh kalau Tahun N-1 (TrackType sama) sudah lulus.
    /// Tahun 1 = entry (tanpa prasyarat). Logic murni — tanpa DbContext, unit-testable.
    /// "passedYears" = daftar TahunKe ("Tahun 1"/"Tahun 2"/"Tahun 3") yang sudah punya penanda lulus utk worker+TrackType tsb.
    /// </summary>
    public static class ProtonYearGate
    {
        private static int YearNum(string tahunKe) =>
            tahunKe switch { "Tahun 1" => 1, "Tahun 2" => 2, "Tahun 3" => 3, _ => 0 };

        public static bool IsYearUnlocked(string targetTahunKe, IEnumerable<string> passedYears)
        {
            int target = YearNum(targetTahunKe);
            if (target <= 1) return true; // Tahun 1 atau tidak dikenal-aman = entry
            string prev = $"Tahun {target - 1}";
            return passedYears.Contains(prev);
        }
    }
}
```

- [ ] **Step 4: Run test → PASS**

Run: `dotnet test --filter ProtonYearGateTests`
Expected: PASS (5 kasus).

- [ ] **Step 5: Commit**

```bash
git add Helpers/ProtonYearGate.cs HcPortal.Tests/ProtonYearGateTests.cs
git commit -m "feat(proton): add ProtonYearGate cross-year unlock helper + tests"
```

---

## Task 3: `ProtonCompletionService` (sumber tunggal penanda) + register DI

**Files:**
- Create: `Services/ProtonCompletionService.cs`
- Modify: `Program.cs` (registrasi service — cari blok `builder.Services.AddScoped<...>` lain, tambah sebaris)
- Test: `HcPortal.Tests/ProtonCompletionServiceTests.cs` (integration real-SQL, pola TEST-05)

- [ ] **Step 1: Implementasi service**

`Services/ProtonCompletionService.cs`:

```csharp
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    /// <summary>
    /// Sumber TUNGGAL pembuatan/penghapusan penanda kelulusan Proton (ProtonFinalAssessment).
    /// Dipanggil dari GradingService (exam), SubmitInterviewResults (interview), Bypass (Diskusi B).
    /// Idempotent: hormati 1-penanda-per-assignment. CompetencyLevelGranted dormant=0 (A-3).
    /// </summary>
    public class ProtonCompletionService
    {
        private readonly ApplicationDbContext _context;
        public ProtonCompletionService(ApplicationDbContext context) => _context = context;

        /// <summary>Terbitkan penanda utk assignment aktif (coachee+track) bila belum ada. Return true bila baru dibuat.</summary>
        public async Task<bool> EnsureAsync(string coacheeId, int protonTrackId, string createdById, string origin, string? notes)
        {
            var assignment = await _context.ProtonTrackAssignments
                .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == protonTrackId && a.IsActive);
            if (assignment == null) return false;

            var exists = await _context.ProtonFinalAssessments
                .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
            if (exists) return false;

            _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
            {
                CoacheeId = coacheeId,
                CreatedById = createdById,
                ProtonTrackAssignmentId = assignment.Id,
                Status = "Completed",
                CompetencyLevelGranted = 0, // dormant (A-3)
                Origin = origin,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Hapus penanda HANYA bila Origin=="Exam" (A-M9) — dipakai re-grade Pass→Fail. Bypass/Interview kebal.</summary>
        public async Task RemoveExamOriginAsync(string coacheeId, int protonTrackId)
        {
            var assignment = await _context.ProtonTrackAssignments
                .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == protonTrackId && a.IsActive);
            if (assignment == null) return;

            await _context.ProtonFinalAssessments
                .Where(fa => fa.ProtonTrackAssignmentId == assignment.Id && fa.Origin == "Exam")
                .ExecuteDeleteAsync();
        }

        /// <summary>Daftar TahunKe yang sudah lulus utk worker pada TrackType tertentu (utk gate cross-year).</summary>
        public async Task<List<string>> GetPassedYearsAsync(string coacheeId, string trackType)
        {
            return await _context.ProtonFinalAssessments
                .Where(fa => fa.CoacheeId == coacheeId)
                .Join(_context.ProtonTrackAssignments, fa => fa.ProtonTrackAssignmentId, a => a.Id, (fa, a) => a)
                .Join(_context.ProtonTracks, a => a.ProtonTrackId, t => t.Id, (a, t) => t)
                .Where(t => t.TrackType == trackType)
                .Select(t => t.TahunKe)
                .Distinct()
                .ToListAsync();
        }
    }
}
```

- [ ] **Step 2: Register di DI**

Di `Program.cs`, dekat registrasi service lain (cari `AddScoped<GradingService>` atau sejenis), tambah:

```csharp
builder.Services.AddScoped<ProtonCompletionService>();
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: 0 error.

- [ ] **Step 4: Test integration (real-SQL, pola TEST-05) — Ensure idempotent + RemoveExamOrigin selektif**

`HcPortal.Tests/ProtonCompletionServiceTests.cs` — ikuti pola disposable real-SQL fixture yang ada (lihat test integration TEST-05 existing untuk setup `ApplicationDbContext`). Test minimal:
- `EnsureAsync` bikin penanda pertama (return true), call kedua return false (idempotent).
- `RemoveExamOriginAsync` hapus penanda Origin="Exam", TIDAK hapus Origin="Bypass".
- `GetPassedYearsAsync` kembaliin TahunKe yang ada penanda utk TrackType cocok.

```csharp
// Sketsa assert inti (sesuaikan fixture real-SQL existing):
// var svc = new ProtonCompletionService(ctx);
// Assert.True(await svc.EnsureAsync(coacheeId, trackId, "hc1", "Exam", null));
// Assert.False(await svc.EnsureAsync(coacheeId, trackId, "hc1", "Exam", null));
// // seed penanda Origin="Bypass" di track lain → RemoveExamOriginAsync tak menghapusnya
// await svc.RemoveExamOriginAsync(coacheeId, trackId);
// Assert.Empty(ctx.ProtonFinalAssessments.Where(f => f.Origin == "Exam"));
// Assert.NotEmpty(ctx.ProtonFinalAssessments.Where(f => f.Origin == "Bypass"));
```

Run: `dotnet test --filter ProtonCompletionServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Services/ProtonCompletionService.cs Program.cs HcPortal.Tests/ProtonCompletionServiceTests.cs
git commit -m "feat(proton): add ProtonCompletionService as single penanda source + tests"
```

---

## Task 4: Wire helper ke GradingService (exam lulus + re-grade flip)

**Files:**
- Modify: `Services/GradingService.cs` (inject service; hook di `GradeAndCompleteAsync` ~L262-299 dan `RegradeAfterEditAsync` L458/471)

- [ ] **Step 1: Inject `ProtonCompletionService`**

Di constructor `GradingService`, tambah parameter `ProtonCompletionService protonCompletion` dan simpan ke field `_protonCompletion` (ikuti pola field DI yang ada, mis. `_workerDataService`).

- [ ] **Step 2: Hook exam lulus (non-essay completion)**

Di `GradeAndCompleteAsync`, setelah blok generate sertifikat (setelah L294, sebelum `await _workerDataService.NotifyIfGroupCompleted(session);` L297):

```csharp
            // A-4: Proton exam lulus → terbitkan penanda (Origin=Exam). Guard A-M11.
            if (isPassed && session.Category == "Assessment Proton" && session.ProtonTrackId.HasValue)
            {
                await _protonCompletion.EnsureAsync(
                    session.UserId, session.ProtonTrackId.Value, session.CreatedBy ?? "",
                    "Exam", $"Exam {session.TahunKe} lulus (skor {finalPercentage}%).");
            }
```
> Catatan: path Essay (`PendingGrading`, IsPassed=null) TIDAK terbit di sini — penanda terbit nanti saat HC selesai nilai essay (lihat Step 4).

- [ ] **Step 3: Hook re-grade flip (RegradeAfterEditAsync)**

Di `RegradeAfterEditAsync`:
- Di cabang `wasPassed && !isPassed` (L458, Pass→Fail, setelah cabut sertifikat), tambah:

```csharp
                if (session.Category == "Assessment Proton" && session.ProtonTrackId.HasValue)
                    await _protonCompletion.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);
```

- Di cabang `!wasPassed && isPassed` (L471, Fail→Pass, setelah generate sertifikat), tambah:

```csharp
                if (session.Category == "Assessment Proton" && session.ProtonTrackId.HasValue)
                    await _protonCompletion.EnsureAsync(
                        session.UserId, session.ProtonTrackId.Value, session.CreatedBy ?? "",
                        "Exam", $"Exam {session.TahunKe} lulus via re-grade.");
```

- [ ] **Step 4: Hook path Essay grading (Proton exam ber-essay)**

Cari endpoint penyelesaian grading essay (grep `HasManualGrading` / `SubmitEssay` / `GradeEssay` di `AssessmentAdminController.cs`). Di titik session jadi `Completed` + `IsPassed` ter-set: tambah pemanggilan `EnsureAsync` yang sama (Origin="Exam") bila `session.Category=="Assessment Proton" && IsPassed==true`. Kalau Proton tidak pernah pakai Essay (konfirmasi: Proton = Standard-only), tandai step ini N/A + catat di commit.

- [ ] **Step 5: Build + smoke test**

Run: `dotnet build` → 0 error.
Manual/integration: simulasikan grade Proton Tahun 1 lulus → penanda terbit. (Tambah integration test bila fixture memungkinkan; minimal verifikasi via Task 11 UAT.)

- [ ] **Step 6: Commit**

```bash
git add Services/GradingService.cs
git commit -m "feat(proton): emit/remove penanda on exam pass and re-grade flips"
```

---

## Task 5: Refactor SubmitInterviewResults ke helper (Origin=Interview)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs:3737-3766` (inline create) → panggil `ProtonCompletionService`

- [ ] **Step 1: Inject service ke controller**

Tambah `ProtonCompletionService` ke constructor `AssessmentAdminController` (field `_protonCompletion`).

- [ ] **Step 2: Ganti blok inline (L3740-3766) dengan helper**

Ganti blok `if (isPassed && session.ProtonTrackId.HasValue) { ... new ProtonFinalAssessment ... }` menjadi:

```csharp
            if (isPassed && session.ProtonTrackId.HasValue)
            {
                await _protonCompletion.EnsureAsync(
                    session.UserId, session.ProtonTrackId.Value, actorForFix?.Id ?? "",
                    "Interview", $"Interview {session.TahunKe} lulus. Assessor: {dto.Judges}");
            }
```
> Hapus `await _context.SaveChangesAsync();` duplikat bila helper sudah save — tapi pertahankan SaveChanges utk perubahan session (InterviewResultsJson dll) di L3768. Pastikan urutan: simpan session dulu (L3768) lalu `EnsureAsync` (yang punya SaveChanges sendiri), atau sebaliknya — yang penting dua-duanya tersimpan.

- [ ] **Step 3: Build**

Run: `dotnet build` → 0 error.

- [ ] **Step 4: Verifikasi perilaku tak berubah**

Interview Tahun 3 lulus → penanda terbit `Origin="Interview"` (sama seperti dulu, kini lewat helper). Cek via UAT Task 11.

- [ ] **Step 5: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "refactor(proton): route interview penanda through ProtonCompletionService"
```

---

## Task 6: Gate eligibility server-side di POST CreateAssessment (A-M2 + A-5.1)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs:842+` (POST `CreateAssessment`, setelah validasi `UserIds` ~L893, sebelum bikin session)

- [ ] **Step 1: Tulis validasi server-side utk kategori Proton**

Setelah blok validasi UserIds (sekitar L905, sebelum eksekusi pembuatan session), tambah:

```csharp
            // A-M2: gate Proton WAJIB server-side (jangan percaya checkbox JS).
            if (model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue && UserIds != null)
            {
                var track = await _context.ProtonTracks.FindAsync(model.ProtonTrackId.Value);
                var trackDeliverableIds = await _context.ProtonDeliverableList
                    .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == model.ProtonTrackId.Value)
                    .Select(d => d.Id).ToListAsync();

                foreach (var uid in UserIds.Distinct())
                {
                    // (a) deliverable 100% per-unit (reuse CoacheeEligibilityCalculator)
                    if (trackDeliverableIds.Any())
                    {
                        var unit = await _context.CoachCoacheeMappings
                            .Where(m => m.CoacheeId == uid && m.IsActive).Select(m => m.AssignmentUnit).FirstOrDefaultAsync()
                            ?? await _context.Users.Where(u => u.Id == uid).Select(u => u.Unit).FirstOrDefaultAsync();
                        var expected = await _context.ProtonDeliverableList.CountAsync(d =>
                            d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == model.ProtonTrackId.Value
                            && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == (unit ?? "").Trim());
                        var statuses = await _context.ProtonDeliverableProgresses
                            .Where(p => p.CoacheeId == uid && trackDeliverableIds.Contains(p.ProtonDeliverableId))
                            .Select(p => p.Status).ToListAsync();
                        if (!HcPortal.Helpers.CoacheeEligibilityCalculator.IsEligiblePerUnit(statuses, expected))
                        {
                            ModelState.AddModelError("UserIds", $"Worker {uid} belum 100% deliverable — tidak boleh diikutkan.");
                        }
                    }
                    // (b) gate antar-tahun: Tahun N-1 lulus (kecuali renewal — RenewsSessionId punya prasyarat sendiri)
                    if (track != null && !model.RenewsSessionId.HasValue)
                    {
                        var passed = await _protonCompletion.GetPassedYearsAsync(uid, track.TrackType);
                        if (!HcPortal.Helpers.ProtonYearGate.IsYearUnlocked(track.TahunKe, passed))
                            ModelState.AddModelError("UserIds", $"Worker {uid}: {track.TahunKe} terkunci — {track.TahunKe} sebelumnya belum lulus.");
                    }
                }
                if (!ModelState.IsValid)
                {
                    // re-populate ViewBag (ikuti pola error-return existing L1167-1182) lalu:
                    return View("CreateAssessment", model);
                }
            }
```
> Exempt bypass (A-M4): assignment hasil bypass akan punya penanda Tahun N-1 (CL-B) → lolos; CL-C exempt karena bypass jalur sendiri (Diskusi B), bukan lewat CreateAssessment.

- [ ] **Step 2: Build**

Run: `dotnet build` → 0 error.

- [ ] **Step 3: Test — request manual worker belum 100% ditolak**

Integration/UAT: POST CreateAssessment Proton dgn UserId yang deliverable belum 100% → ModelState error, session TIDAK dibuat. (Verifikasi Task 11.)

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(proton): enforce eligibility + cross-year gate server-side on CreateAssessment"
```

---

## Task 7: GetEligibleCoachees — cross-year gate + netralin shortcut Tahun 3 (A-5.2/A-M7)

**Files:**
- Modify: `Controllers/CoachMappingController.cs:1342-1425` (GetEligibleCoachees)

- [ ] **Step 1: Netralin shortcut "Tahun 3 no deliverable → all eligible"**

Blok L1363-1372 (`if (!trackDeliverableIds.Any()) { return semua assigned }`) ubah jadi murni data-driven: kalau track BENAR tak punya deliverable, tetap fallback all-eligible (transisi sampai silabus Tahun 3 diisi) TAPI hapus asumsi khusus "Tahun 3". Biarkan logic apa adanya (sudah data-driven berdasar `trackDeliverableIds.Any()`), cukup update komentar L1363 dari "Tahun 3 (interview) tracks have no deliverables" → "Track tanpa deliverable di silabus → semua assigned eligible (transisi sampai silabus diisi)".

- [ ] **Step 2: Tambah cross-year gate ke hasil eligible**

Sebelum `return Json(users);` (L1424), filter `eligibleCoacheeIds` dengan gate antar-tahun. Resolve `track` (TrackType+TahunKe) dari `protonTrackId`, lalu buang coachee yang Tahun N-1 belum lulus:

```csharp
            var gateTrack = await _context.ProtonTracks.FindAsync(protonTrackId);
            if (gateTrack != null)
            {
                var gated = new List<string>();
                foreach (var cid in eligibleCoacheeIds)
                {
                    var passed = await _protonCompletion.GetPassedYearsAsync(cid, gateTrack.TrackType);
                    if (HcPortal.Helpers.ProtonYearGate.IsYearUnlocked(gateTrack.TahunKe, passed))
                        gated.Add(cid);
                }
                eligibleCoacheeIds = gated;
            }
```
> Inject `ProtonCompletionService` ke `CoachMappingController` (field `_protonCompletion`).

- [ ] **Step 3: Build + test**

Run: `dotnet build` → 0 error. Unit test `ProtonYearGate` (Task 2) sudah mencakup logic; integration eligible diverifikasi Task 11.

- [ ] **Step 4: Commit**

```bash
git add Controllers/CoachMappingController.cs
git commit -m "feat(proton): apply cross-year gate to eligible list + make Tahun 3 data-driven"
```

---

## Task 8: Graduation gate ringan (A-M8)

**Files:**
- Modify: `Controllers/CoachMappingController.cs:1107-1164` (action Mark graduated, set `IsCompleted`)

- [ ] **Step 1: Tambah cek Tahun 3 lulus sebelum set IsCompleted**

Sebelum `mapping.IsCompleted = true;` (L1138), tambah:

```csharp
            // A-M8: hanya boleh graduated kalau Tahun 3 sudah lulus (penanda Tahun 3 ada).
            var t3Track = await _context.ProtonTracks
                .FirstOrDefaultAsync(t => t.TrackType == /* TrackType worker */ trackTypeOfCoachee && t.TahunKe == "Tahun 3");
            var passedYears = await _protonCompletion.GetPassedYearsAsync(mapping.CoacheeId, trackTypeOfCoachee);
            if (!passedYears.Contains("Tahun 3"))
            {
                TempData["Error"] = "Belum bisa graduated — Tahun 3 belum lulus.";
                return RedirectToAction(/* balik ke halaman asal sesuai action existing */);
            }
```
> Resolve `trackTypeOfCoachee` dari assignment aktif/terakhir coachee (`ProtonTrackAssignments` → `ProtonTrack.TrackType`). Sesuaikan nama variabel + redirect dengan action existing.

- [ ] **Step 2: Build**

Run: `dotnet build` → 0 error.

- [ ] **Step 3: Verifikasi**

Coba mark graduated worker yang Tahun 3 belum lulus → ditolak. Yang sudah → sukses. (UAT Task 11.)

- [ ] **Step 4: Commit**

```bash
git add Controllers/CoachMappingController.cs
git commit -m "feat(proton): gate Mark-graduated behind Tahun 3 pass"
```

---

## Task 9: Matikan tampilan CompetencyLevelGranted (A-3/A-M12)

**Files:**
- Modify: `Controllers/CDPController.cs:376, 542, 590-592, 3517` (set/baca level + grafik tren) + ViewModel + view terkait

- [ ] **Step 1: Hapus pemakaian level di controller**

- `CDPController.cs:376` & `:542`: hapus assignment `CompetencyLevelGranted = finalAssessment?.CompetencyLevelGranted` (atau set null/0 konsisten — karena dormant).
- `CDPController.cs:590-592`: hapus blok grafik tren yang `Average(fa => fa.CompetencyLevelGranted)` + `trendLabels/trendValues` terkait.
- `CDPController.cs:3517`: hapus `CompetencyLevel = hasAssessment ? fa!.CompetencyLevelGranted : null` (atau set null).

- [ ] **Step 2: Bersihkan ViewModel + View**

Grep `CompetencyLevel` di `Models/**ViewModel*.cs` + `Views/CDP/*.cshtml` → hapus properti + binding tampilan level + elemen grafik tren (chart). Pastikan tidak ada binding nyangkut.

Run: `dotnet build` → 0 error (kalau ada referensi tersisa, build gagal → bersihkan).

- [ ] **Step 3: Verifikasi halaman CDP/HistoriProton render tanpa level + tanpa grafik tren**

UAT Task 11.

- [ ] **Step 4: Commit**

```bash
git add Controllers/CDPController.cs Models/ Views/CDP/
git commit -m "refactor(proton): remove dormant CompetencyLevel display and trend chart"
```

---

## Task 10: Backfill penanda data lama (A-M3/A-M10)

**Files:**
- Modify/Create: endpoint admin-only idempotent (mis. di `Controllers/AdminController.cs`) `POST /Admin/BackfillProtonPenanda`

- [ ] **Step 1: Implementasi endpoint backfill**

```csharp
[HttpPost]
[Authorize(Roles = "Admin")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> BackfillProtonPenanda()
{
    var sessions = await _context.AssessmentSessions
        .Where(s => s.Category == "Assessment Proton" && s.IsPassed == true && s.ProtonTrackId != null
                    && (s.TahunKe == "Tahun 1" || s.TahunKe == "Tahun 2"))
        .Select(s => new { s.UserId, s.ProtonTrackId, s.TahunKe, s.CompletedAt, s.CreatedBy })
        .ToListAsync();

    int created = 0;
    foreach (var s in sessions)
    {
        // A-M10: cari assignment match (coachee, track) — bisa inactive; EnsureAsync pakai aktif,
        // jadi utk backfill resolve assignment paling sesuai era exam secara eksplisit:
        var assignment = await _context.ProtonTrackAssignments
            .Where(a => a.CoacheeId == s.UserId && a.ProtonTrackId == s.ProtonTrackId)
            .OrderByDescending(a => a.AssignedAt <= (s.CompletedAt ?? DateTime.MaxValue))
            .ThenByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();
        if (assignment == null) continue;

        var exists = await _context.ProtonFinalAssessments.AnyAsync(f => f.ProtonTrackAssignmentId == assignment.Id);
        if (exists) continue;

        // (opsional) verifikasi deliverable 100% saat backfill — sesuai A-1; bila ingin ketat, cek di sini.
        _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
        {
            CoacheeId = s.UserId, CreatedById = s.CreatedBy ?? "", ProtonTrackAssignmentId = assignment.Id,
            Status = "Completed", CompetencyLevelGranted = 0, Origin = "Exam",
            Notes = $"Backfill: exam {s.TahunKe} lulus.", CreatedAt = DateTime.UtcNow, CompletedAt = s.CompletedAt
        });
        created++;
    }
    await _context.SaveChangesAsync();
    TempData["Success"] = $"Backfill selesai — {created} penanda dibuat.";
    return RedirectToAction("Index");
}
```

- [ ] **Step 2: Build**

Run: `dotnet build` → 0 error.

- [ ] **Step 3: Snapshot DB lalu jalankan sekali di lokal**

Catat di `docs/SEED_JOURNAL.md`, snapshot DB, panggil endpoint (admin login), verifikasi jumlah penanda + dashboard worker lama jadi "Lulus".

- [ ] **Step 4: Commit**

```bash
git add Controllers/AdminController.cs
git commit -m "feat(proton): one-off backfill endpoint for historical Tahun 1/2 passes"
```

---

## Task 11: UAT end-to-end (Playwright/manual) + verifikasi

**Files:** (no source — verifikasi)

- [ ] **Step 1: Jalankan app**

Run: `Authentication__UseActiveDirectory=false dotnet run` → `http://localhost:5277` (admin login, pwd 123456 — lihat reference_dev_credentials).

- [ ] **Step 2: Skenario verifikasi**

- [ ] Worker Tahun 1 deliverable belum 100% → TIDAK muncul di eligible list (CreateAssessment Proton). POST manual paksa → ditolak (A-M2).
- [ ] Worker Tahun 1 deliverable 100% → muncul eligible → buat exam → worker lulus → **dashboard "Lulus Tahun 1"** (penanda Exam terbit). 
- [ ] Edit jawaban worker jadi gagal (re-grade Pass→Fail) → penanda Exam hilang, status turun. Penanda Bypass/Interview (kalau ada) tetap.
- [ ] Gate antar-tahun: worker Tahun 1 belum lulus → Tahun 2 tidak eligible.
- [ ] Interview Tahun 3 lulus → penanda Origin="Interview" (perilaku lama tetap).
- [ ] Mark graduated worker Tahun 3 belum lulus → ditolak; sudah lulus → sukses.
- [ ] CDP/HistoriProton render tanpa kolom level + tanpa grafik tren, tanpa error.
- [ ] Backfill: worker lama yang lulus exam Tahun 1/2 → jadi "Lulus".

- [ ] **Step 3: Restore DB seed (SEED_WORKFLOW)** bila pakai seed temporary; tandai journal `cleaned`.

- [ ] **Step 4: Commit catatan UAT** (bila ada artefak test e2e).

```bash
git add tests/ docs/
git commit -m "test(proton): e2e UAT for completion logic"
```

---

## Self-Review (penulis plan)

- **Spec coverage:** A-1 (gate, Task 6/7) · A-2 (tak ubah approval — N/A kode) · A-3 (Task 9) · A-4 (Task 3/4/5) · A-5.1 (Task 6/7) · A-5.2 (Task 7) · A-M1 (Task 4) · A-M2 (Task 6) · A-M3/M10 (Task 10) · A-M8 (Task 8) · A-M9 (Task 1/3/4) · A-M11 (Task 4 guard) · A-M12 (Task 9) · A-M13 (Task 6 renewal-exempt). ✔ semua tertutup.
- **Placeholder scan:** kode konkret di tiap step; Task 4 Step 4 (essay path) + Task 8 var resolve = perlu konfirmasi runtime saat eksekusi (ditandai eksplisit, bukan placeholder diam).
- **Type consistency:** `EnsureAsync(coacheeId, protonTrackId, createdById, origin, notes)`, `RemoveExamOriginAsync(coacheeId, protonTrackId)`, `GetPassedYearsAsync(coacheeId, trackType)`, `ProtonYearGate.IsYearUnlocked(target, passedYears)`, `CoacheeEligibilityCalculator.IsEligiblePerUnit(statuses, expected)` — konsisten dipakai Task 3/4/6/7/8.

## Out of scope
- Fitur Bypass Tahun (Diskusi B) — resume setelah plan ini selesai + verified.
- Drop kolom `CompetencyLevelGranted` (dibiarkan dormant).
