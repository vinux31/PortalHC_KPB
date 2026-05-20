# Milestone v3.18 — Phase 2: Edit Jawaban Peserta Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bangun halaman admin/HC untuk edit jawaban MC+MA peserta Completed dengan recompute otomatis (Score/IsPassed/ElemenTeknis), cascade sertifikat & TrainingRecord, audit trail granular, dan SignalR broadcast.

**Architecture:** 3 layer baru: (1) Model + Migration `AssessmentEditLog`, (2) Service method `GradingService.RegradeAfterEditAsync` + refactor compute internal, (3) Controller `AssessmentAdminController.EditPesertaAnswers` (GET/POST) + dry-run `PreviewEditScore` + View `Views/AssessmentAdmin/EditPesertaAnswers.cshtml`. UI dropdown ⋮ ditambah ke `AssessmentMonitoringDetail.cshtml` per-user table. SignalR signal baru `workerAnswerEdited` ke group `monitor-{batchKey}`. Tab "Edit History" dipasang di modal Activity Log existing. Transaction scope membungkus edit+audit+regrade+cascade.

**Tech Stack:** .NET 8 + EF Core 8 (existing), SignalR (existing AssessmentHub), Bootstrap 5 dropdown + modal (existing), ClosedXML/SkiaSharp (NOT used Phase 2). No frontend framework — vanilla JS + jQuery existing pattern.

**Spec reference:** `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` Section 4 (commit c37e55ef).

**Project test infra:** Project TIDAK punya unit/integration test (per spec 5.10). Verification path = `dotnet build` + EF migration apply lokal + manual UAT via browser. Pre-commit checklist dari [`docs/DEV_WORKFLOW.md`](../../DEV_WORKFLOW.md) §5 wajib dijalankan per task. Migration WAJIB di-test apply+rollback lokal sebelum commit.

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `Models/AssessmentEditLog.cs` | Create | EF entity audit granular per question edit (schema spec 4.7) |
| `Data/ApplicationDbContext.cs` | Modify | `DbSet<AssessmentEditLog>` + fluent index `(AssessmentSessionId, EditedAt DESC)` |
| `Migrations/{ts}_AddAssessmentEditLogs.cs` | Create | EF migration up/down |
| `Helpers/AssessmentEditEligibility.cs` | Create | Static `bool IsEditable(AssessmentSession)` + DB check assignment exists |
| `Services/GradingService.cs` | Modify | Refactor: extract `ComputeScoreAndETInternalAsync(session, overrideAnswers?)`. Add `RegradeAfterEditAsync(session)` |
| `Controllers/AssessmentAdminController.cs` | Modify | Add `GET EditPesertaAnswers`, `POST SubmitEditAnswers`, `GET PreviewEditScore` |
| `Views/AssessmentAdmin/EditPesertaAnswers.cshtml` | Create | Edit form page (header, per-soal row, reason dropdown, flip-detection modal) |
| `wwwroot/js/edit-peserta-answers.js` | Create | Frontend: dirty state, reason validation, preview AJAX, flip modal |
| `Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml` | Modify | Per-user table: dropdown ⋮ + IsEditable gating |
| `Views/Shared/_ActivityLogModal.cshtml` (atau equivalent) | Modify | Tab baru "Edit History" |
| `wwwroot/js/assessment-monitoring-detail.js` (atau inline) | Modify | SignalR handler `workerAnswerEdited` |

---

## Task 1: Model `AssessmentEditLog`

**Files:**
- Create: `Models/AssessmentEditLog.cs`

- [ ] **Step 1: Buat entity class**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    /// <summary>
    /// Granular audit log per question edit oleh Admin/HC.
    /// Snapshot text disimpan supaya audit tetap readable kalau soal/option dihapus.
    /// </summary>
    public class AssessmentEditLog
    {
        [Key]
        public int Id { get; set; }

        public int AssessmentSessionId { get; set; }
        [ForeignKey("AssessmentSessionId")]
        public virtual AssessmentSession? AssessmentSession { get; set; }

        public int PackageQuestionId { get; set; }

        public string QuestionTextSnapshot { get; set; } = "";
        public string OldAnswerJson { get; set; } = "[]";         // List<int> PackageOption.Id (MA = multi)
        public string OldAnswerTextSnapshot { get; set; } = "";   // "A. On Job Training"
        public string NewAnswerJson { get; set; } = "[]";
        public string NewAnswerTextSnapshot { get; set; } = "";

        public int? OldScore { get; set; }
        public int? NewScore { get; set; }
        public bool? OldIsPassed { get; set; }
        public bool? NewIsPassed { get; set; }

        public string ActorUserId { get; set; } = "";
        public string ActorName { get; set; } = "";   // "NIP - FullName"
        public string ActorRole { get; set; } = "";   // "Admin" / "HC"

        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        public string ReasonCode { get; set; } = "";  // SoalSalah / KunciSalah / BugSistem / PermintaanPeserta / Lainnya
        public string? ReasonText { get; set; }       // free text, required kalau ReasonCode == "Lainnya"
    }
}
```

- [ ] **Step 2: Register DbSet + fluent config**

Edit `Data/ApplicationDbContext.cs`, tambah `DbSet`:

```csharp
public DbSet<AssessmentEditLog> AssessmentEditLogs { get; set; }
```

Di `OnModelCreating` (cari section assessment-related, dekat `PackageUserResponse` entity config sekitar line 470), tambah:

```csharp
builder.Entity<AssessmentEditLog>(entity =>
{
    entity.HasOne(e => e.AssessmentSession)
        .WithMany()
        .HasForeignKey(e => e.AssessmentSessionId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(e => new { e.AssessmentSessionId, e.EditedAt })
        .HasDatabaseName("IX_AssessmentEditLogs_SessionId_EditedAt")
        .IsDescending(false, true);
});
```

- [ ] **Step 3: Generate migration**

```bash
dotnet ef migrations add AddAssessmentEditLogs --context ApplicationDbContext
```

Expected: file baru `Migrations/{timestamp}_AddAssessmentEditLogs.cs` + `.Designer.cs` + update `ApplicationDbContextModelSnapshot.cs`.

- [ ] **Step 4: Test apply + rollback**

```bash
dotnet ef database update --context ApplicationDbContext
```
Expected: table `AssessmentEditLogs` exist di DB lokal (`HcPortal.db` SQLite atau `HcPortalDB_Dev` SQLEXPRESS).

Test rollback:
```bash
dotnet ef database update {PreviousMigrationName} --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext   # re-apply
```

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded, 0 error.

- [ ] **Step 6: Commit**

```bash
git add Models/AssessmentEditLog.cs Data/ApplicationDbContext.cs Migrations/
git commit -m "feat(v3.18-phase2): add AssessmentEditLog model + migration (audit granular)"
```

---

## Task 2: Helper Eligibility `IsEditable`

**Files:**
- Create: `Helpers/AssessmentEditEligibility.cs`

- [ ] **Step 1: Implementasi**

```csharp
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Cek apakah AssessmentSession boleh di-edit oleh Admin/HC (spec 4.2).
    /// </summary>
    public static class AssessmentEditEligibility
    {
        public static async Task<bool> IsEditableAsync(ApplicationDbContext db, AssessmentSession s)
        {
            if (s == null) return false;
            if (s.Status != "Completed") return false;
            if (s.IsManualEntry) return false;
            if (s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3") return false;

            bool hasAssignment = await db.UserPackageAssignments
                .AnyAsync(a => a.AssessmentSessionId == s.Id);
            return hasAssignment;
        }

        /// <summary>Sync version for view-side rendering (assume assignment already loaded/known).</summary>
        public static bool IsEditableShallow(AssessmentSession s)
        {
            if (s == null) return false;
            if (s.Status != "Completed") return false;
            if (s.IsManualEntry) return false;
            if (s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3") return false;
            return true;
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded. Kalau error "AssessmentSession does not contain TahunKe", grep field aktual:

Run: `Grep pattern="TahunKe" path=Models/AssessmentSession.cs`. Sesuaikan property name.

- [ ] **Step 3: Commit**

```bash
git add Helpers/AssessmentEditEligibility.cs
git commit -m "feat(v3.18-phase2): add AssessmentEditEligibility helper (IsEditable gating)"
```

---

## Task 3: Refactor GradingService — Extract `ComputeScoreAndETInternalAsync`

**Files:**
- Modify: `Services/GradingService.cs` (refactor compute logic line 51-174)

- [ ] **Step 1: Tambah private method baru**

Di `GradingService.cs`, tambah private method baru SETELAH `GradeAndCompleteAsync` (sekitar line 320):

```csharp
/// <summary>
/// Pure compute: hitung total/max score + IsPassed + ElemenTeknis breakdown TANPA side effect (tidak insert DB).
/// Dipakai oleh GradeAndCompleteAsync (initial grading) + RegradeAfterEditAsync (re-grade post-edit) + PreviewEditScore (dry-run).
/// </summary>
/// <param name="session">Session target.</param>
/// <param name="overrideAnswers">
/// Optional. Dict (PackageQuestionId → List of selected PackageOption.Id).
/// Null → baca semua dari PackageUserResponses normal.
/// Non-null → pakai override untuk question yang ada di dict, fallback DB untuk sisanya. Path PreviewEditScore.
/// </param>
private async Task<(int totalScore, int maxScore, bool isPassed, List<SessionElemenTeknisScore> etScores)>
    ComputeScoreAndETInternalAsync(AssessmentSession session, IDictionary<int, List<int>>? overrideAnswers = null)
{
    var packageAssignment = await _context.UserPackageAssignments
        .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);
    if (packageAssignment == null)
        return (0, 0, false, new List<SessionElemenTeknisScore>());

    var shuffledIds = packageAssignment.GetShuffledQuestionIds();
    var packageQuestions = await _context.PackageQuestions
        .Include(q => q.Options)
        .Where(q => shuffledIds.Contains(q.Id))
        .ToListAsync();
    var questionLookup = packageQuestions.ToDictionary(q => q.Id);

    var dbResponses = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == session.Id)
        .ToListAsync();

    // Merge: build effective answers per question
    HashSet<int> SelectedOptions(int qId)
    {
        if (overrideAnswers != null && overrideAnswers.TryGetValue(qId, out var ov))
            return ov.ToHashSet();
        return dbResponses
            .Where(r => r.PackageQuestionId == qId && r.PackageOptionId.HasValue)
            .Select(r => r.PackageOptionId!.Value)
            .ToHashSet();
    }

    int totalScore = 0, maxScore = 0;
    foreach (var qId in shuffledIds)
    {
        if (!questionLookup.TryGetValue(qId, out var q)) continue;
        maxScore += q.ScoreValue;

        switch (q.QuestionType ?? "MultipleChoice")
        {
            case "MultipleChoice":
                var mcSel = SelectedOptions(qId);
                if (mcSel.Count > 0)
                {
                    var optId = mcSel.First();
                    var opt = q.Options.FirstOrDefault(o => o.Id == optId);
                    if (opt?.IsCorrect == true) totalScore += q.ScoreValue;
                }
                break;
            case "MultipleAnswer":
                var maSel = SelectedOptions(qId);
                var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                if (maSel.SetEquals(maCorrect)) totalScore += q.ScoreValue;
                break;
            case "Essay":
                break;
        }
    }

    int pct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
    bool isPassed = pct >= session.PassPercentage;

    var etScores = new List<SessionElemenTeknisScore>();
    var etGroups = packageQuestions
        .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis);

    foreach (var etGroup in etGroups)
    {
        int etCorrect = 0;
        int etTotal = etGroup.Count();
        foreach (var q in etGroup)
        {
            switch (q.QuestionType ?? "MultipleChoice")
            {
                case "MultipleChoice":
                    var mcSel = SelectedOptions(q.Id);
                    if (mcSel.Count > 0)
                    {
                        var opt = q.Options.FirstOrDefault(o => o.Id == mcSel.First());
                        if (opt?.IsCorrect == true) etCorrect++;
                    }
                    break;
                case "MultipleAnswer":
                    var maSel = SelectedOptions(q.Id);
                    var maCorrect = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                    if (maSel.SetEquals(maCorrect)) etCorrect++;
                    break;
                case "Essay":
                    break;
            }
        }
        etScores.Add(new SessionElemenTeknisScore
        {
            AssessmentSessionId = session.Id,
            ElemenTeknis = etGroup.Key,
            CorrectCount = etCorrect,
            QuestionCount = etTotal
        });
    }

    return (totalScore, maxScore, isPassed, etScores);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 3: Smoke test no regression**

`dotnet run`, lakukan exam submit normal (worker complete MC assessment). Verify session masuk Completed dengan score + ET breakdown sama seperti sebelum refactor. Karena `GradeAndCompleteAsync` BELUM kita ubah, behavior pure compute sama.

Catatan: kita BIARKAN `GradeAndCompleteAsync` keep inline logic existing (TIDAK refactor untuk pakai `ComputeScoreAndETInternalAsync`) supaya regression risk = 0. `ComputeScoreAndETInternalAsync` cuma dipakai oleh method baru (`RegradeAfterEditAsync` + `PreviewEditScore`).

- [ ] **Step 4: Commit**

```bash
git add Services/GradingService.cs
git commit -m "feat(v3.18-phase2): add ComputeScoreAndETInternalAsync (pure compute, overrideAnswers? param)"
```

---

## Task 4: `GradingService.RegradeAfterEditAsync`

**Files:**
- Modify: `Services/GradingService.cs`

- [ ] **Step 1: Implementasi public method**

Tambah di akhir class (setelah `ComputeScoreAndETInternalAsync`):

```csharp
/// <summary>
/// Re-grade session yang sudah Completed setelah edit jawaban oleh Admin/HC.
/// DELETE existing SessionElemenTeknisScores → recompute → update session + cascade sertifikat/TR.
/// CALLER bertanggung jawab: open transaction sebelum invoke, commit setelahnya.
/// </summary>
/// <returns>(newScore, newIsPassed, oldScore, oldIsPassed)</returns>
public async Task<(int newScore, bool newIsPassed, int? oldScore, bool? oldIsPassed)>
    RegradeAfterEditAsync(AssessmentSession session)
{
    int? oldScore = session.Score;
    bool? oldIsPassed = session.IsPassed;

    // 1. DELETE existing ET scores
    await _context.SessionElemenTeknisScores
        .Where(et => et.AssessmentSessionId == session.Id)
        .ExecuteDeleteAsync();

    // 2. Recompute (overrideAnswers = null → baca DB which is already updated)
    var (totalScore, maxScore, isPassed, etScores) = await ComputeScoreAndETInternalAsync(session);
    int newPct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

    // 3. Insert new ET scores
    _context.SessionElemenTeknisScores.AddRange(etScores);

    // 4. Update session — status guard berbeda: WHERE Status == "Completed"
    var rowsAffected = await _context.AssessmentSessions
        .Where(s => s.Id == session.Id && s.Status == "Completed")
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.Score, newPct)
            .SetProperty(r => r.IsPassed, isPassed)
            .SetProperty(r => r.UpdatedAt, DateTime.UtcNow)
        );

    if (rowsAffected == 0)
    {
        _logger.LogWarning(
            "GradingService.RegradeAfterEditAsync: session {SessionId} bukan Completed (race).",
            session.Id);
        throw new InvalidOperationException("Session bukan dalam status Completed saat re-grade.");
    }

    await _context.SaveChangesAsync();

    // 5. Cascade sertifikat + TrainingRecord (only when flip)
    bool wasPassed = oldIsPassed ?? false;
    if (wasPassed && !isPassed)
    {
        // Pass → Fail
        await _context.AssessmentSessions
            .Where(s => s.Id == session.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.NomorSertifikat, (string?)null)
                .SetProperty(r => r.ValidUntil, (DateTime?)null));

        var judul = $"Assessment: {session.Title}";
        await _context.TrainingRecords
            .Where(t => t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule)
            .ExecuteUpdateAsync(t => t.SetProperty(r => r.Status, "Failed"));

        _logger.LogInformation(
            "RegradeAfterEditAsync: session {SessionId} flip Pass→Fail — cert dicabut, TR=Failed.",
            session.Id);
    }
    else if (!wasPassed && isPassed)
    {
        // Fail → Pass
        if (session.GenerateCertificate && session.AssessmentType != "PreTest")
        {
            // Generate NomorSertifikat (retry 3x, reuse pattern existing GradeAndCompleteAsync ~line 287)
            var certNow = DateTime.Now;
            int certYear = certNow.Year;
            int attempts = 0;
            const int maxAttempts = 3;
            bool saved = false;
            while (!saved && attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    var nextSeq = await HcPortal.Helpers.CertNumberHelper.GetNextSeqAsync(_context, certYear);
                    var nomor = HcPortal.Helpers.CertNumberHelper.Format(certYear, nextSeq);
                    var validUntil = certNow.AddYears(3);
                    var updated = await _context.AssessmentSessions
                        .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.NomorSertifikat, nomor)
                            .SetProperty(r => r.ValidUntil, validUntil));
                    if (updated > 0) saved = true;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Cert generate retry {Attempt} session {SessionId}", attempts, session.Id);
                }
            }

            // Upsert TrainingRecord
            var judul = $"Assessment: {session.Title}";
            var existingTr = await _context.TrainingRecords
                .FirstOrDefaultAsync(t => t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule);
            if (existingTr == null)
            {
                _context.TrainingRecords.Add(new TrainingRecord
                {
                    UserId = session.UserId,
                    Judul = judul,
                    Kategori = session.Category ?? "Assessment",
                    Tanggal = session.Schedule,
                    TanggalSelesai = DateTime.UtcNow,
                    Penyelenggara = "Internal",
                    Status = "Passed"
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                existingTr.Status = "Passed";
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation(
            "RegradeAfterEditAsync: session {SessionId} flip Fail→Pass — cert generated (if applicable), TR=Passed.",
            session.Id);
    }
    // Pass→Pass, Fail→Fail: no cascade

    return (newPct, isPassed, oldScore, oldIsPassed);
}
```

- [ ] **Step 2: Verify CertNumberHelper signature**

Cek `Helpers/CertNumberHelper.cs` exist + signature `GetNextSeqAsync` + `Format`:

Run: `Grep pattern="GetNextSeqAsync|class CertNumberHelper" path=Helpers/CertNumberHelper.cs`. Kalau berbeda signature, adjust call site Step 1.

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Services/GradingService.cs
git commit -m "feat(v3.18-phase2): add GradingService.RegradeAfterEditAsync (recompute + cascade cert/TR on flip)"
```

---

## Task 5: Controller GET `EditPesertaAnswers`

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs`

- [ ] **Step 1: Tambah view model**

Di `Models/ViewModels/` (atau location ViewModels existing — grep tahu lokasi), bikin `EditPesertaAnswersViewModel.cs`:

```csharp
using HcPortal.Models;

namespace HcPortal.Models.ViewModels
{
    public class EditPesertaAnswersViewModel
    {
        public AssessmentSession Session { get; set; } = null!;
        public string FullName { get; set; } = "";
        public string NIP { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
        public List<EditQuestionRow> Questions { get; set; } = new();
    }

    public class EditQuestionRow
    {
        public int PackageQuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = ""; // MultipleChoice / MultipleAnswer / Essay
        public List<EditOptionRow> Options { get; set; } = new();
        public List<int> SelectedOptionIds { get; set; } = new();
        public List<int> CorrectOptionIds { get; set; } = new();
        public bool IsCurrentCorrect { get; set; }
    }

    public class EditOptionRow
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}
```

- [ ] **Step 2: Tambah action GET**

Di `Controllers/AssessmentAdminController.cs`, di area route assessment admin (cari section sekitar reset/end-exam), tambah:

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> EditPesertaAnswers(int id)
{
    var session = await _context.AssessmentSessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => s.Id == id);
    if (session == null)
    {
        TempData["Error"] = "Sesi tidak ditemukan.";
        return RedirectToAction("ManageAssessment");
    }

    if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
    {
        TempData["Error"] = "Sesi tidak dapat di-edit (status tidak Completed, Manual Entry, atau Proton Tahun 3).";
        return RedirectToAction("AssessmentMonitoringDetail", new {
            title = session.Title, category = session.Category, scheduleDate = session.Schedule
        });
    }

    var assignment = await _context.UserPackageAssignments
        .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
    if (assignment == null) return NotFound();

    var shuffledIds = assignment.GetShuffledQuestionIds();
    var questions = await _context.PackageQuestions
        .Include(q => q.Options)
        .Where(q => shuffledIds.Contains(q.Id))
        .ToListAsync();
    var responses = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == id)
        .ToListAsync();

    var rows = new List<HcPortal.Models.ViewModels.EditQuestionRow>();
    foreach (var qId in shuffledIds)
    {
        var q = questions.FirstOrDefault(x => x.Id == qId);
        if (q == null) continue;
        var selectedIds = responses
            .Where(r => r.PackageQuestionId == qId && r.PackageOptionId.HasValue)
            .Select(r => r.PackageOptionId!.Value)
            .ToList();
        var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
        bool isCorrect = (q.QuestionType ?? "MultipleChoice") switch
        {
            "MultipleChoice" => selectedIds.Count == 1 && correctIds.Contains(selectedIds[0]),
            "MultipleAnswer" => selectedIds.ToHashSet().SetEquals(correctIds.ToHashSet()),
            _ => false
        };
        rows.Add(new HcPortal.Models.ViewModels.EditQuestionRow
        {
            PackageQuestionId = q.Id,
            QuestionText = q.QuestionText,
            QuestionType = q.QuestionType ?? "MultipleChoice",
            Options = q.Options.Select(o => new HcPortal.Models.ViewModels.EditOptionRow
            {
                Id = o.Id, OptionText = o.OptionText, IsCorrect = o.IsCorrect
            }).ToList(),
            SelectedOptionIds = selectedIds,
            CorrectOptionIds = correctIds,
            IsCurrentCorrect = isCorrect
        });
    }

    var vm = new HcPortal.Models.ViewModels.EditPesertaAnswersViewModel
    {
        Session = session,
        FullName = session.User?.FullName ?? "Unknown",
        NIP = session.User?.NIP ?? "—",
        UpdatedAt = session.UpdatedAt ?? session.CreatedAt,
        Questions = rows
    };
    return View(vm);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Models/ViewModels/EditPesertaAnswersViewModel.cs Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase2): add GET EditPesertaAnswers controller + view model"
```

---

## Task 6: View `EditPesertaAnswers.cshtml`

**Files:**
- Create: `Views/AssessmentAdmin/EditPesertaAnswers.cshtml`

- [ ] **Step 1: Buat view full**

```html
@model HcPortal.Models.ViewModels.EditPesertaAnswersViewModel
@{
    ViewData["Title"] = $"Edit Jawaban — {Model.FullName}";
}

<div class="container-fluid py-3">
  <a asp-action="AssessmentMonitoringDetail"
     asp-route-title="@Model.Session.Title"
     asp-route-category="@Model.Session.Category"
     asp-route-scheduleDate="@Model.Session.Schedule.ToString("yyyy-MM-dd")"
     class="btn btn-sm btn-outline-secondary">← Back to Monitoring</a>

  <h3 class="mt-3">Edit Jawaban — @Model.FullName (NIP @Model.NIP)</h3>

  <div class="card mb-3">
    <div class="card-body small">
      <strong>@Model.Session.Title</strong> · Kategori: @Model.Session.Category
      · Schedule: @Model.Session.Schedule.ToString("dd MMM yyyy HH:mm")<br/>
      Skor saat ini: <strong>@(Model.Session.Score?.ToString() ?? "—")%</strong>
      · Status: <strong>@(Model.Session.IsPassed == true ? "Pass" : Model.Session.IsPassed == false ? "Fail" : "—")</strong>
    </div>
  </div>

  <div class="alert alert-warning">
    <strong>⚠️ Catatan:</strong> Edit jawaban akan recompute skor + spider chart otomatis.
    Aksi ini di-log audit. Hasil tidak ditampilkan ke peserta.
  </div>

  <form id="editAnswersForm" method="post"
        asp-action="SubmitEditAnswers"
        data-preview-url="@Url.Action("PreviewEditScore", new { sessionId = Model.Session.Id })">
    @Html.AntiForgeryToken()
    <input type="hidden" name="SessionId" value="@Model.Session.Id" />
    <input type="hidden" name="UpdatedAt" value="@Model.UpdatedAt.ToString("O")" />

    @foreach (var (q, idx) in Model.Questions.Select((q, i) => (q, i + 1)))
    {
      <div class="card mb-3 question-card"
           data-question-id="@q.PackageQuestionId"
           data-question-type="@q.QuestionType">
        <div class="card-body">
          <div class="mb-2">
            <strong>Soal @idx</strong>
            <span class="badge bg-secondary">@(q.QuestionType == "MultipleChoice" ? "MC" : q.QuestionType == "MultipleAnswer" ? "MA" : "Essay")</span>
            @if (q.IsCurrentCorrect) { <span class="badge bg-success">✓ Benar</span> }
            else { <span class="badge bg-danger">✗ Salah</span> }
          </div>
          <div class="mb-2">@q.QuestionText</div>

          @if (q.QuestionType == "Essay")
          {
            <div class="text-muted fst-italic">
              Essay – manual grading via halaman Penilaian Essay
            </div>
          }
          else if (q.QuestionType == "MultipleChoice")
          {
            <div class="ms-2">
              @foreach (var opt in q.Options)
              {
                <div class="form-check">
                  <input class="form-check-input answer-input" type="radio"
                         name="Answers[@q.PackageQuestionId]"
                         value="@opt.Id"
                         id="opt-@q.PackageQuestionId-@opt.Id"
                         @(q.SelectedOptionIds.Contains(opt.Id) ? "checked" : "") />
                  <label class="form-check-label" for="opt-@q.PackageQuestionId-@opt.Id">
                    @opt.OptionText
                    @if (opt.IsCorrect) { <span class="badge bg-success ms-2">Kunci</span> }
                  </label>
                </div>
              }
            </div>
          }
          else // MultipleAnswer
          {
            <div class="ms-2">
              @foreach (var opt in q.Options)
              {
                <div class="form-check">
                  <input class="form-check-input answer-input" type="checkbox"
                         name="Answers[@q.PackageQuestionId]"
                         value="@opt.Id"
                         id="opt-@q.PackageQuestionId-@opt.Id"
                         @(q.SelectedOptionIds.Contains(opt.Id) ? "checked" : "") />
                  <label class="form-check-label" for="opt-@q.PackageQuestionId-@opt.Id">
                    @opt.OptionText
                    @if (opt.IsCorrect) { <span class="badge bg-success ms-2">Kunci</span> }
                  </label>
                </div>
              }
            </div>
          }

          @if (q.QuestionType != "Essay")
          {
            <div class="reason-block mt-2 d-none">
              <label class="form-label small">Alasan edit <span class="text-danger">*</span></label>
              <select class="form-select form-select-sm reason-code"
                      name="Reasons[@q.PackageQuestionId].Code">
                <option value="">— Pilih —</option>
                <option value="SoalSalah">Soal salah</option>
                <option value="KunciSalah">Kunci jawaban salah</option>
                <option value="BugSistem">Bug sistem</option>
                <option value="PermintaanPeserta">Permintaan peserta</option>
                <option value="Lainnya">Lainnya</option>
              </select>
              <textarea class="form-control form-control-sm mt-2 reason-text d-none" rows="2"
                        name="Reasons[@q.PackageQuestionId].Text"
                        placeholder="Detail alasan (wajib kalau pilih Lainnya)"></textarea>
            </div>
          }
        </div>
      </div>
    }

    <div class="d-flex gap-2 mt-3">
      <a asp-action="AssessmentMonitoringDetail"
         asp-route-title="@Model.Session.Title"
         asp-route-category="@Model.Session.Category"
         asp-route-scheduleDate="@Model.Session.Schedule.ToString("yyyy-MM-dd")"
         class="btn btn-outline-secondary">Cancel</a>
      <button type="submit" class="btn btn-primary" id="submitEditBtn">Save & Recompute</button>
    </div>
  </form>
</div>

<!-- Flip confirmation modal -->
<div class="modal fade" id="flipConfirmModal" tabindex="-1">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">⚠️ Perubahan Hasil</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body" id="flipModalBody"></div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="button" class="btn btn-primary" id="flipConfirmBtn">Lanjut</button>
      </div>
    </div>
  </div>
</div>

@section Scripts {
  <script src="~/js/edit-peserta-answers.js" asp-append-version="true"></script>
}
```

- [ ] **Step 2: Stub JS file**

Buat `wwwroot/js/edit-peserta-answers.js` dengan content placeholder (diisi Task 7):

```javascript
console.log("edit-peserta-answers.js loaded");
```

- [ ] **Step 3: Verify build + UAT initial render**

Run: `dotnet build && dotnet run`. Buka `http://localhost:5277/AssessmentAdmin/EditPesertaAnswers/{sessionId}` untuk session Completed valid. Verify page render tanpa error 500, semua soal MC/MA tampil dengan radio/checkbox terisi sesuai jawaban current, kunci ditandai.

- [ ] **Step 4: Commit**

```bash
git add Views/AssessmentAdmin/EditPesertaAnswers.cshtml wwwroot/js/edit-peserta-answers.js
git commit -m "feat(v3.18-phase2): add EditPesertaAnswers.cshtml + JS stub"
```

---

## Task 7: Frontend JS — Dirty State + Reason Validation + Flip Modal

**Files:**
- Modify: `wwwroot/js/edit-peserta-answers.js`

- [ ] **Step 1: Implementasi full JS**

Replace content:

```javascript
document.addEventListener("DOMContentLoaded", () => {
  const form = document.getElementById("editAnswersForm");
  if (!form) return;
  const previewUrl = form.dataset.previewUrl;
  const submitBtn = document.getElementById("submitEditBtn");
  const flipModal = new bootstrap.Modal(document.getElementById("flipConfirmModal"));
  const flipBody = document.getElementById("flipModalBody");
  const flipConfirmBtn = document.getElementById("flipConfirmBtn");

  // Snapshot initial answers per question
  const initialState = {};
  document.querySelectorAll(".question-card").forEach(card => {
    const qid = card.dataset.questionId;
    const type = card.dataset.questionType;
    if (type === "Essay") return;
    const checked = Array.from(card.querySelectorAll(".answer-input:checked"))
      .map(i => i.value).sort();
    initialState[qid] = { type, options: checked };
  });

  function getCurrentAnswers(card) {
    const qid = card.dataset.questionId;
    const type = card.dataset.questionType;
    if (type === "Essay") return null;
    const checked = Array.from(card.querySelectorAll(".answer-input:checked"))
      .map(i => i.value).sort();
    return { qid, type, options: checked };
  }

  function isDirty(card) {
    const cur = getCurrentAnswers(card);
    if (!cur) return false;
    const init = initialState[cur.qid];
    if (!init) return false;
    if (cur.options.length !== init.options.length) return true;
    return cur.options.some((v, i) => v !== init.options[i]);
  }

  // Show reason block + highlight when dirty
  function updateDirtyUI(card) {
    const reasonBlock = card.querySelector(".reason-block");
    if (!reasonBlock) return;
    if (isDirty(card)) {
      card.classList.add("border-warning");
      reasonBlock.classList.remove("d-none");
    } else {
      card.classList.remove("border-warning");
      reasonBlock.classList.add("d-none");
      // Reset reason fields
      const select = reasonBlock.querySelector(".reason-code");
      const textarea = reasonBlock.querySelector(".reason-text");
      if (select) select.value = "";
      if (textarea) { textarea.value = ""; textarea.classList.add("d-none"); }
    }
  }

  // Listen for answer changes
  document.querySelectorAll(".answer-input").forEach(input => {
    input.addEventListener("change", () => {
      const card = input.closest(".question-card");
      updateDirtyUI(card);
    });
  });

  // Reason code "Lainnya" toggles textarea required
  document.querySelectorAll(".reason-code").forEach(sel => {
    sel.addEventListener("change", () => {
      const block = sel.closest(".reason-block");
      const ta = block.querySelector(".reason-text");
      if (sel.value === "Lainnya") {
        ta.classList.remove("d-none");
      } else {
        ta.classList.add("d-none");
        ta.value = "";
      }
    });
  });

  function collectDiff() {
    const diff = [];
    document.querySelectorAll(".question-card").forEach(card => {
      if (!isDirty(card)) return;
      const qid = card.dataset.questionId;
      const cur = getCurrentAnswers(card);
      const reasonBlock = card.querySelector(".reason-block");
      const code = reasonBlock.querySelector(".reason-code").value;
      const text = reasonBlock.querySelector(".reason-text").value;
      diff.push({ questionId: qid, options: cur.options.map(Number), reasonCode: code, reasonText: text });
    });
    return diff;
  }

  function validateClient(diff) {
    if (diff.length === 0) {
      alert("Tidak ada perubahan untuk disimpan.");
      return false;
    }
    for (const d of diff) {
      if (!d.reasonCode) {
        alert(`Alasan wajib untuk soal yang diubah (Question ID ${d.questionId}).`);
        return false;
      }
      if (d.reasonCode === "Lainnya" && !d.reasonText.trim()) {
        alert(`Detail alasan wajib kalau pilih "Lainnya" (Question ID ${d.questionId}).`);
        return false;
      }
    }
    return true;
  }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const diff = collectDiff();
    if (!validateClient(diff)) return;

    submitBtn.disabled = true;
    submitBtn.textContent = "Memeriksa...";

    try {
      // Dry-run preview
      const fd = new FormData();
      diff.forEach((d, i) => {
        fd.append(`Drafts[${i}].QuestionId`, d.questionId);
        d.options.forEach(o => fd.append(`Drafts[${i}].Options`, o));
      });
      const resp = await fetch(previewUrl, { method: "POST", body: fd, headers: { "RequestVerificationToken": form.querySelector('input[name=__RequestVerificationToken]').value } });
      if (!resp.ok) throw new Error("Preview gagal");
      const preview = await resp.json();
      const oldPassed = preview.oldIsPassed;
      const newPassed = preview.newIsPassed;
      const flip = (oldPassed === true && newPassed === false) || (oldPassed === false && newPassed === true);

      if (flip) {
        let msg = `Edit ini akan ubah hasil: <strong>${oldPassed ? "Pass" : "Fail"} → ${newPassed ? "Pass" : "Fail"}</strong><br/><br/>`;
        if (oldPassed && !newPassed && preview.hasCert) {
          msg += `Sertifikat existing (No: ${preview.nomorSertifikat}) akan dicabut.`;
        } else if (!oldPassed && newPassed && preview.willGenerateCert) {
          msg += "Sertifikat baru akan diterbitkan otomatis.";
        }
        flipBody.innerHTML = msg;
        flipConfirmBtn.onclick = () => { flipModal.hide(); form.submit(); };
        flipModal.show();
        submitBtn.disabled = false;
        submitBtn.textContent = "Save & Recompute";
        return;
      }

      // No flip → submit langsung
      form.submit();
    } catch (err) {
      console.error(err);
      alert("Gagal memeriksa preview: " + err.message);
      submitBtn.disabled = false;
      submitBtn.textContent = "Save & Recompute";
    }
  });
});
```

- [ ] **Step 2: Verify UAT initial render + dirty toggle**

Run: `dotnet run`. Buka edit page. Ubah 1 jawaban MC → card highlight warning, reason dropdown muncul. Pilih "Lainnya" → textarea muncul. Switch back ke jawaban asli → highlight + reason hilang.

- [ ] **Step 3: Commit**

```bash
git add wwwroot/js/edit-peserta-answers.js
git commit -m "feat(v3.18-phase2): edit-peserta-answers.js (dirty state + reason + flip preview)"
```

---

## Task 8: Controller POST `SubmitEditAnswers` + Transaction

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs`

- [ ] **Step 1: Bikin DTO**

Di `Models/ViewModels/`, tambah `EditAnswersSubmission.cs`:

```csharp
namespace HcPortal.Models.ViewModels
{
    public class EditAnswersSubmission
    {
        public int SessionId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<int, List<int>> Answers { get; set; } = new();   // qId → optionIds
        public Dictionary<int, EditReason> Reasons { get; set; } = new();  // qId → reason
    }
    public class EditReason
    {
        public string Code { get; set; } = "";
        public string? Text { get; set; }
    }
}
```

- [ ] **Step 2: Tambah POST action**

Di `AssessmentAdminController.cs`:

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitEditAnswers(HcPortal.Models.ViewModels.EditAnswersSubmission form,
    [FromServices] GradingService gradingService,
    [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<HcPortal.Hubs.AssessmentHub> hubContext)
{
    var session = await _context.AssessmentSessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => s.Id == form.SessionId);
    if (session == null) { TempData["Error"] = "Sesi tidak ditemukan."; return RedirectToAction("ManageAssessment"); }

    var redirectBack = RedirectToAction("AssessmentMonitoringDetail", new {
        title = session.Title, category = session.Category, scheduleDate = session.Schedule
    });

    // Eligibility
    if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
    {
        TempData["Error"] = "Sesi tidak dapat di-edit.";
        return redirectBack;
    }

    // Concurrency check
    var currentUpdatedAt = session.UpdatedAt ?? session.CreatedAt;
    if (Math.Abs((currentUpdatedAt - form.UpdatedAt).TotalSeconds) > 1)
    {
        TempData["Error"] = "Sesi sudah diubah admin lain. Silakan refresh halaman.";
        return redirectBack;
    }

    // Validate reasons
    foreach (var (qid, _) in form.Answers)
    {
        if (!form.Reasons.TryGetValue(qid, out var reason) || string.IsNullOrWhiteSpace(reason.Code))
        {
            TempData["Error"] = $"Alasan wajib untuk soal {qid}.";
            return redirectBack;
        }
        if (!new[] { "SoalSalah", "KunciSalah", "BugSistem", "PermintaanPeserta", "Lainnya" }.Contains(reason.Code))
        {
            TempData["Error"] = "ReasonCode tidak valid.";
            return redirectBack;
        }
        if (reason.Code == "Lainnya" && string.IsNullOrWhiteSpace(reason.Text))
        {
            TempData["Error"] = "ReasonText wajib kalau ReasonCode == Lainnya.";
            return redirectBack;
        }
    }

    var actorId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "";
    var actor = await _context.Users.FirstOrDefaultAsync(u => u.Id == actorId);
    var actorRole = User.IsInRole("Admin") ? "Admin" : "HC";
    var actorName = $"{actor?.NIP} - {actor?.FullName}";

    // Load questions+options + existing responses
    var qIds = form.Answers.Keys.ToList();
    var questions = await _context.PackageQuestions
        .Include(q => q.Options)
        .Where(q => qIds.Contains(q.Id))
        .ToListAsync();
    var existingResponses = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == session.Id && qIds.Contains(r.PackageQuestionId))
        .ToListAsync();

    using var tx = await _context.Database.BeginTransactionAsync();
    try
    {
        int editCount = 0;
        foreach (var (qId, newOptionIds) in form.Answers)
        {
            var q = questions.FirstOrDefault(x => x.Id == qId);
            if (q == null) continue;
            if ((q.QuestionType ?? "MultipleChoice") == "Essay") continue;

            // Validate optionIds valid for this question
            var validOptionIds = q.Options.Select(o => o.Id).ToHashSet();
            var sanitizedNew = newOptionIds.Where(id => validOptionIds.Contains(id)).ToList();

            // Snapshot OLD
            var oldResponses = existingResponses
                .Where(r => r.PackageQuestionId == qId && r.PackageOptionId.HasValue).ToList();
            var oldOptionIds = oldResponses.Select(r => r.PackageOptionId!.Value).ToList();
            var oldTextSnapshot = string.Join(", ",
                q.Options.Where(o => oldOptionIds.Contains(o.Id)).Select(o => o.OptionText));

            // Apply edit: MA pattern = delete-all + insert-new
            _context.PackageUserResponses.RemoveRange(oldResponses);
            foreach (var newOid in sanitizedNew)
            {
                _context.PackageUserResponses.Add(new PackageUserResponse
                {
                    AssessmentSessionId = session.Id,
                    PackageQuestionId = qId,
                    PackageOptionId = newOid,
                    TextAnswer = null,
                    SubmittedAt = DateTime.UtcNow
                });
            }

            // Snapshot NEW
            var newTextSnapshot = string.Join(", ",
                q.Options.Where(o => sanitizedNew.Contains(o.Id)).Select(o => o.OptionText));

            // Insert AssessmentEditLog (NewScore/NewIsPassed diisi nanti)
            _context.AssessmentEditLogs.Add(new AssessmentEditLog
            {
                AssessmentSessionId = session.Id,
                PackageQuestionId = qId,
                QuestionTextSnapshot = q.QuestionText ?? "",
                OldAnswerJson = System.Text.Json.JsonSerializer.Serialize(oldOptionIds),
                OldAnswerTextSnapshot = oldTextSnapshot,
                NewAnswerJson = System.Text.Json.JsonSerializer.Serialize(sanitizedNew),
                NewAnswerTextSnapshot = newTextSnapshot,
                OldScore = session.Score,
                OldIsPassed = session.IsPassed,
                ActorUserId = actorId,
                ActorName = actorName,
                ActorRole = actorRole,
                ReasonCode = form.Reasons[qId].Code,
                ReasonText = form.Reasons[qId].Text,
                EditedAt = DateTime.UtcNow
            });
            editCount++;
        }
        await _context.SaveChangesAsync();

        // Recompute
        var (newScore, newIsPassed, oldScore, oldIsPassed) = await gradingService.RegradeAfterEditAsync(session);

        // Backfill NewScore/NewIsPassed di AssessmentEditLog entries baru (last N rows for this session)
        await _context.AssessmentEditLogs
            .Where(l => l.AssessmentSessionId == session.Id && l.NewScore == null && l.ActorUserId == actorId)
            .ExecuteUpdateAsync(l => l
                .SetProperty(x => x.NewScore, newScore)
                .SetProperty(x => x.NewIsPassed, newIsPassed));

        // AuditLog generic
        _context.AuditLogs.Add(new AuditLog
        {
            ActionType = "EditAssessmentAnswer",
            EntityType = "AssessmentSession",
            EntityId = session.Id.ToString(),
            UserId = actorId,
            Description = $"Edit {editCount} jawaban session #{session.Id} ({session.User?.FullName}), score {oldScore?.ToString() ?? "—"} → {newScore}",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await tx.CommitAsync();

        // Cache invalidate
        _cache.Remove($"exam-status-{session.Id}");

        // SignalR broadcast
        var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
        await hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("workerAnswerEdited", new
        {
            sessionId = session.Id,
            workerName = session.User?.FullName ?? "Unknown",
            oldScore, newScore, oldIsPassed, newIsPassed,
            actorName, actorRole
        });

        string flip = (oldIsPassed == true, newIsPassed) switch
        {
            (true, false) => "Pass→Fail",
            (false, true) => "Fail→Pass",
            (true, true) => "Pass→Pass",
            _ => "Fail→Fail"
        };
        TempData["Success"] = $"Edit {editCount} jawaban berhasil. Score: {oldScore?.ToString() ?? "—"} → {newScore}, {flip}";
        return redirectBack;
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        _logger.LogError(ex, "Edit jawaban gagal untuk session {SessionId}", session.Id);
        TempData["Error"] = "Gagal menyimpan edit. Tidak ada perubahan tersimpan.";
        return redirectBack;
    }
}
```

- [ ] **Step 2: Inject `IMemoryCache` + `ILogger` kalau belum**

Verify constructor `AssessmentAdminController` sudah inject `_cache` + `_logger`. Kalau belum, tambah parameter sesuai pattern existing.

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs Models/ViewModels/EditAnswersSubmission.cs
git commit -m "feat(v3.18-phase2): POST SubmitEditAnswers (tx + audit + regrade + SignalR)"
```

---

## Task 9: Controller `PreviewEditScore` (Dry-Run)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs`

- [ ] **Step 1: DTO + action**

Tambah DTO di `Models/ViewModels/EditAnswersSubmission.cs`:

```csharp
public class EditDraft
{
    public int QuestionId { get; set; }
    public List<int> Options { get; set; } = new();
}
public class EditDraftSubmission
{
    public List<EditDraft> Drafts { get; set; } = new();
}
```

Tambah action:

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PreviewEditScore(int sessionId,
    HcPortal.Models.ViewModels.EditDraftSubmission form,
    [FromServices] GradingService gradingService)
{
    var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    if (session == null) return NotFound();
    if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
        return Forbid();

    var overrideAnswers = form.Drafts.ToDictionary(d => d.QuestionId, d => d.Options);

    // Use reflection / internal call: ComputeScoreAndETInternalAsync is private — expose via wrapper
    var (newScore, newIsPassed) = await gradingService.PreviewScoreAsync(session, overrideAnswers);

    return Json(new
    {
        oldScore = session.Score,
        oldIsPassed = session.IsPassed,
        newScore, newIsPassed,
        hasCert = !string.IsNullOrEmpty(session.NomorSertifikat),
        nomorSertifikat = session.NomorSertifikat,
        willGenerateCert = session.GenerateCertificate && session.AssessmentType != "PreTest"
    });
}
```

- [ ] **Step 2: Tambah public wrapper di GradingService**

Karena `ComputeScoreAndETInternalAsync` private, expose via public wrapper:

```csharp
public async Task<(int newScore, bool newIsPassed)> PreviewScoreAsync(
    AssessmentSession session,
    IDictionary<int, List<int>> overrideAnswers)
{
    var (totalScore, maxScore, isPassed, _) = await ComputeScoreAndETInternalAsync(session, overrideAnswers);
    int pct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
    return (pct, isPassed);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Smoke test endpoint**

Run `dotnet run`. Buka edit page, ubah jawaban, klik Save. Tap network tab DevTools → verify request POST `/AssessmentAdmin/PreviewEditScore?sessionId=...` muncul, return JSON `{ oldScore, newScore, oldIsPassed, newIsPassed, ... }`.

- [ ] **Step 5: Commit**

```bash
git add Controllers/AssessmentAdminController.cs Services/GradingService.cs Models/ViewModels/EditAnswersSubmission.cs
git commit -m "feat(v3.18-phase2): POST PreviewEditScore endpoint (dry-run flip detection)"
```

---

## Task 10: View `AssessmentMonitoringDetail.cshtml` — Dropdown ⋮

**Files:**
- Modify: `Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml`

- [ ] **Step 1: Lokasi per-user table action column**

Run: `Grep pattern="View Results|Reset|Akhiri Ujian|Reshuffle" path=Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml`. Identifikasi block action column existing.

- [ ] **Step 2: Refactor action column ke hybrid layout**

Pertahankan inline: `[View Results]` + `[🕐 Activity Log]`. Pindahkan ke dropdown ⋮: `Reset`, `Akhiri Ujian`, `Reshuffle`. Tambah item baru `✏️ Edit Jawaban` (kondisional `IsEditableShallow`).

Pseudocode HTML untuk action column:

```html
@{ var canEdit = HcPortal.Helpers.AssessmentEditEligibility.IsEditableShallow(session); }

<div class="d-flex gap-1 align-items-center">
  <a asp-action="ViewResults" asp-route-id="@session.Id" class="btn btn-sm btn-outline-primary">View Results</a>
  <button class="btn btn-sm btn-outline-secondary" data-bs-toggle="modal" data-bs-target="#activityLogModal-@session.Id" title="Activity Log">🕐</button>

  <div class="dropdown">
    <button class="btn btn-sm btn-outline-secondary dropdown-toggle"
            data-bs-toggle="dropdown" aria-expanded="false"
            aria-label="Aksi lain untuk @session.User?.FullName">⋮</button>
    <ul class="dropdown-menu dropdown-menu-end">
      @if (canEdit)
      {
        <li><a class="dropdown-item" asp-action="EditPesertaAnswers" asp-route-id="@session.Id">✏️ Edit Jawaban</a></li>
      }
      @if (session.Status != "Cancelled")
      {
        <li><a class="dropdown-item js-reset-session" data-id="@session.Id" href="#">🔄 Reset</a></li>
      }
      @if (session.Status == "InProgress")
      {
        <li><a class="dropdown-item js-end-exam" data-id="@session.Id" href="#">❌ Akhiri Ujian</a></li>
      }
      @if (isPackageMode && (session.Status == "Not Started" || session.Status == "Abandoned"))
      {
        <li><a class="dropdown-item js-reshuffle" data-id="@session.Id" href="#">🔀 Reshuffle</a></li>
      }
    </ul>
  </div>
</div>
```

(Sesuaikan handler `js-reset-session` / `js-end-exam` / `js-reshuffle` ke action handler existing — biasanya pakai form POST atau JS confirm. Pertahankan behavior existing, cuma pindah tempat ke dropdown.)

- [ ] **Step 3: Verify build + UAT**

Run: `dotnet build && dotnet run`. Buka monitoring detail page. Per row:
- Verify `View Results` + `🕐` tetap inline
- Click `⋮` → menu muncul
- Verify item conditional: `Edit Jawaban` hanya untuk session Completed non-Manual non-Proton-Tahun3
- Verify `Reset/Akhiri Ujian/Reshuffle` muncul sesuai status existing

- [ ] **Step 4: Commit**

```bash
git add Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml
git commit -m "feat(v3.18-phase2): per-user table action dropdown (hybrid inline+menu)"
```

---

## Task 11: SignalR Frontend Handler `workerAnswerEdited`

**Files:**
- Modify: JS handler di `Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml` (inline `@section Scripts` atau separate JS file)

- [ ] **Step 1: Cari section SignalR existing**

Run: `Grep pattern="workerSubmitted|connection.on" path=Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml`. Lokasi inline SignalR handler.

- [ ] **Step 2: Tambah handler workerAnswerEdited**

Di scripts section, setelah handler `workerSubmitted`:

```javascript
connection.on("workerAnswerEdited", function (data) {
  // Update row score + result cell tanpa full reload
  const row = document.querySelector(`tr[data-session-id="${data.sessionId}"]`);
  if (row) {
    const scoreCell = row.querySelector(".session-score");
    const resultCell = row.querySelector(".session-result");
    if (scoreCell) scoreCell.textContent = `${data.newScore}%`;
    if (resultCell) {
      resultCell.textContent = data.newIsPassed ? "Pass" : "Fail";
      resultCell.className = "session-result " + (data.newIsPassed ? "text-success" : "text-danger");
    }
  }

  // Toast
  const flip = (data.oldIsPassed === true && data.newIsPassed === false) ? "Pass→Fail"
             : (data.oldIsPassed === false && data.newIsPassed === true) ? "Fail→Pass"
             : (data.newIsPassed ? "Pass→Pass" : "Fail→Fail");
  const msg = `${data.actorRole} ${data.actorName} edit jawaban ${data.workerName}: ${data.oldScore}→${data.newScore}, ${flip}`;
  showToast(msg, "info");
});

function showToast(msg, level) {
  // Reuse pattern existing — kalau belum ada, fallback alert
  if (typeof window.toastr !== "undefined") {
    window.toastr[level](msg);
  } else {
    console.log("[toast]", msg);
  }
}
```

(Sesuaikan selector `[data-session-id]`, `.session-score`, `.session-result` ke struktur tabel actual.)

- [ ] **Step 3: Verify UAT 2-tab live update**

Run: `dotnet run`. Buka monitoring detail di 2 tab browser sekaligus (sama session group). Di tab 1, lakukan edit jawaban → save. Di tab 2 → verify row score + result cell update otomatis tanpa reload, toast muncul.

- [ ] **Step 4: Commit**

```bash
git add Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml
git commit -m "feat(v3.18-phase2): SignalR workerAnswerEdited handler (row update + toast)"
```

---

## Task 12: Activity Log Modal — Tab "Edit History"

**Files:**
- Modify: `Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml` (atau partial `_ActivityLogModal.cshtml` — grep dulu)

- [ ] **Step 1: Lokasi modal Activity Log**

Run: `Grep pattern="activityLogModal|Activity Log" path=Views/AssessmentAdmin/`. Identifikasi modal existing.

- [ ] **Step 2: Refactor modal body jadi tabs**

Bungkus content existing dalam tab "Activity Timeline", tambah tab "Edit History":

```html
<div class="modal-body">
  <ul class="nav nav-tabs" role="tablist">
    <li class="nav-item">
      <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#tab-timeline-@session.Id">Activity Timeline</button>
    </li>
    <li class="nav-item">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tab-edit-history-@session.Id"
              data-load-url="@Url.Action("EditHistoryPartial", new { sessionId = session.Id })">Edit History</button>
    </li>
  </ul>
  <div class="tab-content pt-3">
    <div class="tab-pane fade show active" id="tab-timeline-@session.Id">
      @* existing timeline content *@
    </div>
    <div class="tab-pane fade" id="tab-edit-history-@session.Id">
      <div class="edit-history-placeholder text-muted small">Memuat...</div>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Tambah partial action `EditHistoryPartial`**

Di `AssessmentAdminController.cs`:

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> EditHistoryPartial(int sessionId)
{
    var logs = await _context.AssessmentEditLogs
        .Where(l => l.AssessmentSessionId == sessionId)
        .OrderByDescending(l => l.EditedAt)
        .ToListAsync();
    return PartialView("_EditHistoryPartial", logs);
}
```

- [ ] **Step 4: Buat partial view**

`Views/AssessmentAdmin/_EditHistoryPartial.cshtml`:

```html
@model List<HcPortal.Models.AssessmentEditLog>

@if (!Model.Any())
{
  <p class="text-muted">Belum ada edit untuk sesi ini.</p>
}
else
{
  <ul class="list-unstyled">
    @foreach (var log in Model)
    {
      <li class="border-bottom pb-2 mb-2">
        <div class="small text-muted">[@log.EditedAt.ToString("yyyy-MM-dd HH:mm")] Soal #@log.PackageQuestionId</div>
        <div class="fw-bold">@log.QuestionTextSnapshot</div>
        <div>
          <span class="text-muted">@(string.IsNullOrEmpty(log.OldAnswerTextSnapshot) ? "—" : log.OldAnswerTextSnapshot)</span>
          → <span class="text-success">@log.NewAnswerTextSnapshot</span>
        </div>
        <div class="small">oleh @log.ActorRole (@log.ActorName)</div>
        <div class="small text-muted">Alasan: @log.ReasonCode @(log.ReasonText != null ? $"— {log.ReasonText}" : "")</div>
      </li>
    }
  </ul>
}
```

- [ ] **Step 5: Lazy-load tab via JS**

Tambah inline JS di scripts section MonitoringDetail:

```javascript
document.querySelectorAll('[data-load-url]').forEach(btn => {
  btn.addEventListener('shown.bs.tab', async () => {
    const url = btn.dataset.loadUrl;
    if (!url || btn.dataset.loaded === "1") return;
    const target = document.querySelector(btn.dataset.bsTarget);
    const placeholder = target.querySelector('.edit-history-placeholder');
    try {
      const resp = await fetch(url);
      target.innerHTML = await resp.text();
      btn.dataset.loaded = "1";
    } catch (err) {
      if (placeholder) placeholder.textContent = "Gagal memuat edit history.";
    }
  });
});
```

- [ ] **Step 6: Verify build + UAT**

Run: `dotnet build && dotnet run`. Buka monitoring detail → klik 🕐 modal → verify 2 tab muncul. Click tab "Edit History" → load entries dari `AssessmentEditLog` (kalau ada). Lakukan 1 edit dulu → reopen modal → tab Edit History show entry baru.

- [ ] **Step 7: Commit**

```bash
git add Views/AssessmentAdmin/AssessmentMonitoringDetail.cshtml Views/AssessmentAdmin/_EditHistoryPartial.cshtml Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase2): Activity Log Edit History tab (lazy-load partial)"
```

---

## Task 13: Manual UAT Full Checklist + Final Commit

**Files:** None (UAT only)

- [ ] **Step 1: Jalankan UAT checklist Phase 2 (spec 5.10)**

Tickbox di branch lokal sebelum push:

- [ ] Edit 1 soal MC, no flip → verify score recompute, spider chart di `Views/CMP/Results.cshtml` peserta update, AssessmentEditLog entry muncul di tab Edit History
- [ ] Edit 1 soal MA → verify multi-option update, PackageUserResponses sesuai pilihan baru
- [ ] Edit menyebabkan Pass → Fail flip (dgn sertifikat existing) → verify modal konfirmasi muncul, NomorSertifikat null setelah save, TrainingRecord status="Failed"
- [ ] Edit menyebabkan Fail → Pass flip → verify NomorSertifikat generate, TrainingRecord status="Passed"
- [ ] 2 admin buka edit page session sama bersamaan, A submit dulu, B coba submit → verify B kena stale "Sesi sudah diubah admin lain."
- [ ] Edit dengan reason kosong → block client + server
- [ ] Edit ReasonCode = "Lainnya" tanpa ReasonText → block
- [ ] Akses Edit page untuk session Cancelled / IsManualEntry / Tahun 3 → block + error message
- [ ] Akses Edit page tanpa role Admin/HC (login Worker) → 403/redirect
- [ ] SignalR refresh: monitor di tab/browser lain auto-update row score cell + toast
- [ ] AuditLog generic entry tercatat (`/Admin/AuditLog` table search "EditAssessmentAnswer")

- [ ] **Step 2: Pre-commit checklist DEV_WORKFLOW §5**

```
- [ ] dotnet build pass (tanpa warning baru)
- [ ] dotnet run + manual verify di http://localhost:5277
- [ ] Golden path & edge case dicek manual
- [ ] DB lokal: migration AddAssessmentEditLogs apply + rollback OK
- [ ] (Optional) Playwright tests pass
- [ ] Migration file Migrations/{ts}_AddAssessmentEditLogs.cs di-commit
- [ ] Team IT di-notify (commit hash + flag MIGRATION ADA "AddAssessmentEditLogs")
```

- [ ] **Step 3: Tag milestone phase**

```bash
git tag -a v3.18-phase2-complete -m "Milestone v3.18 Phase 2: Edit Jawaban complete"
```

- [ ] **Step 4: Push + handoff IT**

Notify IT team channel dengan format:
```
Milestone v3.18 Phase 2 deploy-ready. Commit hash: {LAST_COMMIT}.
Ada migration baru: AddAssessmentEditLogs — perlu dotnet ef database update di DB Dev.
Verify post-deploy: /AssessmentAdmin/AssessmentMonitoringDetail → dropdown ⋮ → Edit Jawaban accessible utk Admin/HC.
```

```bash
git push origin main
git push origin v3.18-phase2-complete
```

---

## Spec Coverage Map

| Spec Section | Covered by Task |
|---|---|
| 4.1 Scope (MC+MA edit, Completed only) | Task 2 (IsEditable), Task 5 (GET), Task 8 (POST) |
| 4.2 Helper Eligibility | Task 2 |
| 4.3 UI Dropdown Aksi Hybrid | Task 10 |
| 4.4 Edit Jawaban Page (route + layout + reason dropdown) | Task 5, 6, 7 |
| 4.5 POST Submit Flow (16 step) | Task 8 |
| 4.6 Konfirmasi Modal Pass↔Fail Flip | Task 6 (modal markup), Task 7 (JS preview AJAX), Task 9 (PreviewEditScore) |
| 4.7 Tabel `AssessmentEditLog` + index | Task 1 |
| 4.8 `RegradeAfterEditAsync` + `ComputeScoreAndETInternalAsync(session, overrideAnswers?)` | Task 3, 4 |
| 4.9 Activity Log Tab "Edit History" | Task 12 |
| 4.10 Permission Admin/HC | Inherited dari `[Authorize(Roles = "Admin, HC")]` di semua action |
| 5.1 SignalR `workerAnswerEdited` payload | Task 8 (broadcast), Task 11 (frontend handler) |
| 5.2 Cache Invalidation | Task 8 |
| 5.3 Transaction Scope | Task 8 |
| 5.4 Anti-forgery | Task 8 (action attr), Task 6 (view) |
| 5.5 Concurrency `UpdatedAt` token | Task 6 (hidden field), Task 8 (check) |
| 5.6 Error Handling | Task 8 |
| 5.7 A11y + Mobile | Task 10 (dropdown ARIA), Task 6 (form labels) |
| 5.8 Logging | Task 4 (RegradeAfterEditAsync), Task 8 (controller) |
| 5.10 Manual UAT | Task 13 |
| 5.11 Migration `AddAssessmentEditLogs` | Task 1 |

Tidak ada gap. Phase 2 ready untuk execute.
