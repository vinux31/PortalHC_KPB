# Phase 321: Assessment Edit Jawaban Peserta — Research

**Researched:** 2026-05-21 (refreshed)
**Milestone:** v17.0 Assessment Admin Power Tools
**Confidence:** HIGH (semua claim re-verified via direct codebase read 2026-05-21 post Phase 320 ship — line refs aktual; spec `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` commit `c37e55ef`; UI-SPEC commit `bd6e8fbc`)
**Language:** Bahasa Indonesia (per CLAUDE.md)
**Source:** Promoted dari `docs/superpowers/plans/2026-05-21-v318-phase2-edit-jawaban.md` (commit `594cfd95`) — superpowers `writing-plans` output sebelum di-formalisasi via `/gsd-plan-phase`.

> **Catatan refresh 2026-05-21:** Re-verified seluruh codebase line ref + path. **KOREKSI besar dari versi sebelumnya:**
> 1. Controller route `[Route("Admin/[action]")]` → URL EditPesertaAnswers = `/Admin/EditPesertaAnswers/{id}` (bukan `/AssessmentAdmin/...`).
> 2. View folder = `Views/Admin/` (controller `AssessmentAdminController` punya `protected new View(...)` override di line 53-57 yang resolve view ke `~/Views/Admin/{Action}.cshtml`).
> 3. `CertNumberHelper.Build(int seq, DateTime date)` + `IsDuplicateKeyException(ex)` — bukan `Format(year, seq)` (RESEARCH lama salah signature).
> 4. Toast frontend = `window.showAssessmentToast(message, linkUrl?, linkText?)` di `wwwroot/js/assessment-hub.js:96` — bukan `showToast(msg, "info")` atau `window.toastr` (UI-SPEC §Toast SignalR D-07 lock).
> 5. Modal Activity Log = INLINE di `Views/Admin/AssessmentMonitoringDetail.cshtml:540`, populated via AJAX `GET /Admin/GetActivityLog` (controller line 5643) — TIDAK ada `_ActivityLogModal.cshtml` partial.
> 6. Per-row field di monitoring view = `session.UserStatus` (bukan `session.Status`) untuk gating UI.
> 7. `_hubContext` + `_gradingService` SUDAH di-inject sebagai field controller — TIDAK perlu `[FromServices]` di action signature.
> 8. UI-SPEC override RESEARCH markup short-form: reason labels verbose (D-05), flip modal copy eksplisit (D-06), dropdown pakai `bi bi-*` icon (bukan emoji).

**Goal:** Bangun halaman admin/HC untuk edit jawaban MC+MA peserta Completed dengan recompute otomatis (Score/IsPassed/ElemenTeknis), cascade sertifikat & TrainingRecord, audit trail granular, dan SignalR broadcast.

**Architecture:** 3 layer baru: (1) Model + Migration `AssessmentEditLog`, (2) Service method `GradingService.RegradeAfterEditAsync` + refactor compute internal, (3) Controller `AssessmentAdminController.EditPesertaAnswers` (GET/POST) + dry-run `PreviewEditScore` + View `Views/Admin/EditPesertaAnswers.cshtml`. UI dropdown ⋮ ditambah ke `Views/Admin/AssessmentMonitoringDetail.cshtml` per-user table. SignalR signal baru `workerAnswerEdited` ke group `monitor-{batchKey}` (verified `Hubs/AssessmentHub.cs:57`). Tab "Edit History" dipasang INLINE di modal Activity Log existing (`Views/Admin/AssessmentMonitoringDetail.cshtml:540`). Transaction scope membungkus edit+audit+regrade+cascade.

**Tech Stack:** .NET 8 + EF Core 8 (existing), SignalR (existing AssessmentHub), Bootstrap 5 dropdown + modal (existing), Bootstrap Icons CDN. ClosedXML/SkiaSharp/QuestPDF (dari Phase 320, NOT used Phase 321). No frontend framework — vanilla JS + jQuery existing pattern. **No new NuGet dep** (CONTEXT D-12 locked + verified).

**Spec reference:** `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` Section 4 (commit `c37e55ef`).
**UI Contract:** `.planning/phases/321-assessment-edit-jawaban-peserta/321-UI-SPEC.md` (commit `bd6e8fbc`) — markup labels + a11y + copy.

**Project test infra:** Project TIDAK punya unit/integration test (per spec 5.10). Verification path = `dotnet build` + EF migration apply lokal + Playwright hybrid (CONTEXT D-04: 4 Playwright + 4 manual UAT) + pre-commit checklist [`docs/DEV_WORKFLOW.md`](../../../../docs/DEV_WORKFLOW.md) §5 wajib dijalankan per task. Migration WAJIB di-test apply+rollback lokal sebelum commit. **Nyquist `nyquist_validation: true`** di `.planning/config.json` — Validation Architecture section WAJIB ada (lihat di bawah).

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 PLAN sub-numbering:** 4 PLAN file atomic per layer — `321-01` foundation (Task 1-2), `321-02` service (Task 3-4), `321-03` controller+view+frontend (Task 5-11), `321-04` activity-log+UAT (Task 12-13).
- **D-02 Sequential strict:** 01 → 02 → 03 → 04 wajib urut, no paralelisasi.
- **D-03 Branch:** `feature/phase-321-edit-jawaban` feature branch + merge (beda dari Phase 320 langsung main).
- **D-04 Testing:** Hybrid 4 Playwright + 4 manual UAT (lihat Validation Architecture section).
- **D-05 Reason labels verbose:** Code value PascalCase, label UI Bahasa Indonesia full:
  - `SoalSalah` → "Soal salah / typo"
  - `KunciSalah` → "Kunci jawaban salah"
  - `BugSistem` → "Bug sistem / glitch"
  - `PermintaanPeserta` → "Permintaan koreksi peserta"
  - `Lainnya` → "Lainnya (jelaskan)"
- **D-06 Flip modal eksplisit:** Pass→Fail: "menggagalkan peserta + NomorSertifikat dicabut + TrainingRecord Failed". Fail→Pass: "meluluskan peserta + NomorSertifikat baru di-generate (kalau eligible)". Tombol `[Batal]` + `[Lanjutkan, simpan perubahan]`, no checkbox layer.
- **D-07 Toast verbose:** Template `{actorRole} {actorName} edit jawaban {workerName}: Score {oldScore}→{newScore}, {oldResult}→{newResult}`, top-right, 8 detik auto-dismiss, klik untuk persist. **Reuse `window.showAssessmentToast`** dari `wwwroot/js/assessment-hub.js:96`.
- **D-08 IT notify:** Preemptif (saat PLAN 01 selesai) + Final (post-tag).
- **D-09 Phase 320 sudah remote** (commit `5f2306ba` verified) — Phase 321 jalan paralel tanpa blocker.
- **D-10 Commit cadence:** 1-task-1-commit, format `feat|refactor|perf|chore(v17.0-p321): ...`.
- **D-11 Test infra existing:** dotnet build + browser UAT + Playwright opportunistic. Tidak buat xUnit project baru.
- **D-12 NO new NuGet dep.**
- **D-13 Pre-commit checklist** DEV_WORKFLOW §5 wajib per commit. Migration apply+rollback wajib Task 1.

### Claude's Discretion

- **CD-01 Proton Tahun 3 detection:** Pakai `s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3"` (verified field exist di `Models/AssessmentSession.cs:101`).
- **CD-02 TrainingRecord Fail→Pass upsert:** Ikuti pola `GradeAndCompleteAsync` line 268-285 (insert kalau missing, update kalau exist).
- **CD-03 Dirty state JS:** Vanilla addEventListener + snapshot compare (no jQuery for new code).
- **CD-04 Activity Log tab lazy-load:** AJAX load partial saat `shown.bs.tab` event, cache via `data-loaded="1"` attr.
- **CD-05 SignalR group naming:** `monitor-{batchKey}` (verified `Hubs/AssessmentHub.cs:57`).
- **CD-06 Anti-forgery/error/ARIA:** Ikuti spec §5.4/§5.6/§5.7 + UI-SPEC §A11y Contract.
- **CD-07 Merge strategy:** Default `git merge --no-ff` (preserve task granularity); fallback squash kalau user request.

### Deferred Ideas (OUT OF SCOPE)

- Bulk edit (multi-question multi-session mass recompute) — Phase 321 already 1-form-N-question; "edit semua session sekaligus" deferred.
- Edit Essay (manual rubrik + NLP interplay) — phase terpisah.
- Manual override sertifikat tanpa edit jawaban — IT direct DB job sekarang.
- AssessmentEditLog → CSV export (compliance audit) — future.
- Webhook external (Slack/Teams) per edit — future.
- Reshuffle dedicated tab di Activity Log — future polish.
- Brand color Pertamina theming modal/toast — global UI hygiene.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EDIT-01 | Halaman dedicated `/Admin/EditPesertaAnswers/{sessionId}` form per soal MC/MA Admin/HC | Task 5 (GET) + Task 6 (View) + Task 8 (POST) |
| EDIT-02 | Helper `IsEditable` gate Status=Completed + !IsManualEntry + !ProtonT3 untuk GET/POST/UI dropdown | Task 2 (`AssessmentEditEligibility`) — sync + async variants |
| EDIT-03 | Auto-recompute Score+IsPassed+ET via `RegradeAfterEditAsync` (DELETE ET + recompute + ExecuteUpdateAsync status guard) | Task 3 (refactor `ComputeScoreAndETInternalAsync`) + Task 4 (`RegradeAfterEditAsync`) |
| EDIT-04 | Pass↔Fail cascade: cabut/generate NomorSertifikat + TrainingRecord upsert | Task 4 (cascade logic dengan `CertNumberHelper.Build` + retry 3x via `IsDuplicateKeyException`) |
| EDIT-05 | Reason dropdown 5 preset + Lainnya wajib teks (label verbose UI-SPEC) | Task 6 (markup) + Task 7 (JS) + Task 8 (server validate) — pakai label D-05 |
| EDIT-06 | Audit dual-write: `AuditLog` + `AssessmentEditLog` granular per question | Task 1 (model) + Task 8 (insert dual) |
| EDIT-07 | Concurrency `AssessmentSession.UpdatedAt` hidden field round-trip | Task 6 (hidden input ISO 8601 `"O"`) + Task 8 (compare ≤1 detik tolerance) |
| EDIT-08 | Transaction scope wrap edit+audit+regrade+cascade, rollback total kalau exception | Task 8 (`BeginTransactionAsync` try/catch) |
| EDIT-09 | SignalR `workerAnswerEdited` ke `monitor-{batchKey}` payload `{sessionId, workerName, oldScore, newScore, oldIsPassed, newIsPassed, actorName, actorRole}` | Task 8 (server broadcast via `_hubContext`) + Task 11 (frontend handler + `window.showAssessmentToast`) |
| EDIT-10 | Dry-run endpoint `POST PreviewEditScore` → JSON `{oldScore, newScore, oldIsPassed, newIsPassed, hasCert, nomorSertifikat, willGenerateCert}` | Task 9 (controller + `PreviewScoreAsync` public wrapper) |
| EDIT-11 | Tab baru "Edit History" di modal Activity Log existing, lazy-load partial sort EditedAt DESC | Task 12 (refactor modal body INLINE jadi nav-tabs, `EditHistoryPartial` action + `_EditHistoryPartial.cshtml` partial di `Views/Admin/`) |
| EDIT-12 | Per-row action column hybrid: 2 inline (View Results + 🕐) + dropdown ⋮ untuk Edit/Reset/AkhiriUjian/Reshuffle, ARIA + `dropdown-menu-end` + auto-flip mobile | Task 10 (refactor `Views/Admin/AssessmentMonitoringDetail.cshtml:286-338`) |
| EDIT-13 | Migration `AddAssessmentEditLogs` + index `IX_AssessmentEditLogs_SessionId_EditedAt` `(AssessmentSessionId, EditedAt DESC)`, apply+rollback verified | Task 1 (migration generate + DEV_WORKFLOW §4 test) |

</phase_requirements>

---

## File Structure (REFRESHED 2026-05-21)

| File | Action | Responsibility |
|------|--------|---------------|
| `Models/AssessmentEditLog.cs` | Create | EF entity audit granular per question edit (schema spec 4.7) |
| `Data/ApplicationDbContext.cs` | Modify | `DbSet<AssessmentEditLog>` + fluent index `(AssessmentSessionId, EditedAt DESC)` (insert dekat existing `SessionElemenTeknisScores` DbSet line 77, fluent config setelah `AssessmentSession` entity config line 170-228) |
| `Migrations/{ts}_AddAssessmentEditLogs.cs` | Create | EF migration up/down via `dotnet ef migrations add` |
| `Helpers/AssessmentEditEligibility.cs` | Create | Static `IsEditableAsync(db, session)` + sync `IsEditableShallow(session)` |
| `Models/ViewModels/EditPesertaAnswersViewModel.cs` | Create | View model GET (Session + Questions + UpdatedAt) |
| `Models/ViewModels/EditAnswersSubmission.cs` | Create | DTO POST + Preview (`EditAnswersSubmission`, `EditReason`, `EditDraft`, `EditDraftSubmission`) |
| `Services/GradingService.cs` | Modify | Tambah `ComputeScoreAndETInternalAsync` (private) + `RegradeAfterEditAsync` (public) + `PreviewScoreAsync` (public wrapper) — INSERT setelah `GradeAndCompleteAsync` line 326 |
| `Controllers/AssessmentAdminController.cs` | Modify | Add `GET EditPesertaAnswers`, `POST SubmitEditAnswers`, `POST PreviewEditScore`, `GET EditHistoryPartial` — INSERT dekat `ExportAssessmentResults` (line 3651 verified Phase 320 unchanged). Pakai `_hubContext` + `_gradingService` field existing (sudah di-inject ctor line 27-49). |
| `Views/Admin/EditPesertaAnswers.cshtml` | Create | **DIRECTORY = `Views/Admin/`** (controller override line 53-57 resolve view ke folder Admin) — edit form page |
| `wwwroot/js/edit-peserta-answers.js` | Create | Frontend: dirty state + reason validation + preview AJAX + flip modal |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | Modify | Per-user table action column line 286-338 refactor jadi dropdown ⋮ hybrid + modal Activity Log line 540 refactor body jadi nav-tabs |
| `Views/Admin/_EditHistoryPartial.cshtml` | Create | Partial tab content (rendered via `PartialView("_EditHistoryPartial", logs)` — controller override line 57 resolve ke `~/Views/Admin/_EditHistoryPartial.cshtml`) |

**Tidak ada** `_ActivityLogModal.cshtml` partial — modal inline di `AssessmentMonitoringDetail.cshtml:540`.

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

Edit `Data/ApplicationDbContext.cs`, tambah `DbSet` dekat baris 77 (sekitar `SessionElemenTeknisScores`):

```csharp
public DbSet<AssessmentEditLog> AssessmentEditLogs { get; set; }
```

Di `OnModelCreating` (line 128), tambah fluent config setelah `AssessmentSession` block (sekitar line 228, dekat `CK_AssessmentSession_RenewalChain`):

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

- [ ] **Step 4: Test apply + rollback (DEV_WORKFLOW §4 wajib)**

```bash
dotnet ef database update --context ApplicationDbContext
```
Expected: table `AssessmentEditLogs` exist di DB lokal SQL Server (`HcPortalDB_Dev` SQLEXPRESS).

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
git commit -m "feat(v17.0-p321): add AssessmentEditLog model + migration (audit granular)"
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
            // Verified field paths: AssessmentSession.Category (line 16), TahunKe (line 101)
            if (s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3") return false;

            bool hasAssignment = await db.UserPackageAssignments
                .AnyAsync(a => a.AssessmentSessionId == s.Id);
            return hasAssignment;
        }

        /// <summary>Sync version for view-side rendering (skip assignment DB check; render-time only).</summary>
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

Run: `dotnet build`. Field references verified: `AssessmentSession.Category:16`, `Status:20`, `TahunKe:101`, `IsManualEntry:130`.

- [ ] **Step 3: Commit**

```bash
git add Helpers/AssessmentEditEligibility.cs
git commit -m "feat(v17.0-p321): add AssessmentEditEligibility helper (IsEditable gating)"
```

---

## Task 3: Refactor GradingService — Extract `ComputeScoreAndETInternalAsync`

**Files:**
- Modify: `Services/GradingService.cs` (insert setelah `GradeAndCompleteAsync` which ends at line 326 — currently 329-line file)

- [ ] **Step 1: Tambah private method baru**

Di `GradingService.cs`, tambah private method baru SETELAH `GradeAndCompleteAsync` (line 327):

```csharp
/// <summary>
/// Pure compute: hitung total/max score + IsPassed + ElemenTeknis breakdown TANPA side effect (tidak insert DB).
/// Dipakai oleh RegradeAfterEditAsync (re-grade post-edit) + PreviewScoreAsync (dry-run).
/// `GradeAndCompleteAsync` initial grading KEEP inline logic existing (TIDAK refactor) supaya regression risk = 0.
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

Run: `dotnet build`. Expected: Build succeeded.

- [ ] **Step 3: Smoke test no regression**

`dotnet run`, worker complete MC assessment normal. Verify session Completed dengan score + ET breakdown sama seperti sebelum refactor (karena `GradeAndCompleteAsync` BELUM diubah).

- [ ] **Step 4: Commit**

```bash
git add Services/GradingService.cs
git commit -m "feat(v17.0-p321): add ComputeScoreAndETInternalAsync (pure compute, overrideAnswers? param)"
```

---

## Task 4: `GradingService.RegradeAfterEditAsync`

**Files:**
- Modify: `Services/GradingService.cs`

- [ ] **Step 1: Implementasi public method**

Tambah setelah `ComputeScoreAndETInternalAsync`:

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

    // 4. Update session — status guard WHERE Status == "Completed"
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
            // Generate NomorSertifikat (retry 3x — reuse pattern GradeAndCompleteAsync line 287-321)
            // CORRECTED SIGNATURE: CertNumberHelper.Build(int seq, DateTime date) — NOT Format(year, seq)
            var certNow = DateTime.Now;
            int certYear = certNow.Year;
            int certAttempts = 0;
            const int maxCertAttempts = 3;
            bool certSaved = false;
            while (!certSaved && certAttempts < maxCertAttempts)
            {
                certAttempts++;
                try
                {
                    var nextSeq = await HcPortal.Helpers.CertNumberHelper.GetNextSeqAsync(_context, certYear);
                    var nomor = HcPortal.Helpers.CertNumberHelper.Build(nextSeq, certNow);
                    var validUntil = certNow.AddYears(3);
                    var updated = await _context.AssessmentSessions
                        .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.NomorSertifikat, nomor)
                            .SetProperty(r => r.ValidUntil, validUntil));
                    if (updated > 0) certSaved = true;
                }
                catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && HcPortal.Helpers.CertNumberHelper.IsDuplicateKeyException(ex))
                {
                    // Retry dengan sequence baru
                }
            }

            if (!certSaved)
            {
                _logger.LogError("RegradeAfterEditAsync: failed generate cert for session {SessionId} after {N} attempts",
                    session.Id, maxCertAttempts);
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

/// <summary>
/// Public wrapper for PreviewEditScore (dry-run) — exposes ComputeScoreAndETInternalAsync without ET breakdown.
/// </summary>
public async Task<(int newScore, bool newIsPassed)> PreviewScoreAsync(
    AssessmentSession session,
    IDictionary<int, List<int>> overrideAnswers)
{
    var (totalScore, maxScore, isPassed, _) = await ComputeScoreAndETInternalAsync(session, overrideAnswers);
    int pct = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
    return (pct, isPassed);
}
```

- [ ] **Step 2: Verify CertNumberHelper signature**

VERIFIED 2026-05-21:
- `CertNumberHelper.Build(int seq, DateTime date)` returns `"KPB/{seq:D3}/{RomanMonth}/{year}"` — `Helpers/CertNumberHelper.cs:20`
- `CertNumberHelper.GetNextSeqAsync(ApplicationDbContext, int year)` — line 23
- `CertNumberHelper.IsDuplicateKeyException(DbUpdateException)` — line 37 (returns true kalau IX_AssessmentSessions_NomorSertifikat hit, OR SQL Server error 2601/2627)
- NO `Format(year, seq)` method exist — KOREKSI dari research lama.

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Services/GradingService.cs
git commit -m "feat(v17.0-p321): add GradingService.RegradeAfterEditAsync + PreviewScoreAsync (recompute + cascade cert/TR on flip)"
```

---

## Task 5: Controller GET `EditPesertaAnswers`

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs`
- Create: `Models/ViewModels/EditPesertaAnswersViewModel.cs`

- [ ] **Step 1: View model**

Verify lokasi folder ViewModels:
```bash
grep -r "namespace HcPortal.Models.ViewModels" Models/ViewModels/ | head -3
```

Buat `Models/ViewModels/EditPesertaAnswersViewModel.cs`:

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

Di `Controllers/AssessmentAdminController.cs` (lokasi: insert dekat `ExportAssessmentResults` line 3651 atau dekat `AssessmentMonitoringDetail` line 2684 — pilih grouping logical):

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
        TempData["Error"] = "Sesi ini tidak dapat diedit (status bukan Completed, atau IsManualEntry, atau Assessment Proton Tahun 3).";
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
    return View(vm);   // Controller override line 54 resolve ke ~/Views/Admin/EditPesertaAnswers.cshtml
}
```

> **Resulting URL:** `/Admin/EditPesertaAnswers/{id}` (via `[Route("Admin/[action]")]` line 19).

- [ ] **Step 3: Verify build**

Run: `dotnet build`. Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Models/ViewModels/EditPesertaAnswersViewModel.cs Controllers/AssessmentAdminController.cs
git commit -m "feat(v17.0-p321): add GET EditPesertaAnswers controller + view model"
```

---

## Task 6: View `EditPesertaAnswers.cshtml`

**Files:**
- Create: `Views/Admin/EditPesertaAnswers.cshtml` **(folder = `Views/Admin/`, NOT `Views/AssessmentAdmin/`)**

- [ ] **Step 1: Buat view full (UI-SPEC reconciled — D-05 verbose reason labels + D-06 flip modal copy)**

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

  @if (TempData["Error"] != null)
  {
    <div class="alert alert-danger" role="alert">@TempData["Error"]</div>
  }

  <div class="alert alert-warning" role="alert">
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
              <label class="form-label small" for="reason-code-@q.PackageQuestionId">
                Alasan edit <span class="text-danger" aria-hidden="true">*</span>
              </label>
              <select class="form-select form-select-sm reason-code"
                      id="reason-code-@q.PackageQuestionId"
                      name="Reasons[@q.PackageQuestionId].Code"
                      aria-required="true">
                <option value="">— Pilih —</option>
                @* D-05 verbose labels (UI-SPEC override) *@
                <option value="SoalSalah">Soal salah / typo</option>
                <option value="KunciSalah">Kunci jawaban salah</option>
                <option value="BugSistem">Bug sistem / glitch</option>
                <option value="PermintaanPeserta">Permintaan koreksi peserta</option>
                <option value="Lainnya">Lainnya (jelaskan)</option>
              </select>
              <textarea class="form-control form-control-sm mt-2 reason-text d-none" rows="2"
                        id="reason-text-@q.PackageQuestionId"
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

<!-- Flip confirmation modal (D-06 eksplisit copy) -->
<div class="modal fade" id="flipConfirmModal" tabindex="-1"
     aria-labelledby="flipConfirmModalLabel" aria-describedby="flipModalBody" aria-hidden="true">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title" id="flipConfirmModalLabel">⚠️ Perubahan Hasil</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
      </div>
      <div class="modal-body" id="flipModalBody"></div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Batal</button>
        <button type="button" class="btn btn-primary" id="flipConfirmBtn">Lanjutkan, simpan perubahan</button>
      </div>
    </div>
  </div>
</div>

@section Scripts {
  <script src="~/js/edit-peserta-answers.js" asp-append-version="true"></script>
}
```

> **UI-SPEC reconciled:**
> - Reason labels = D-05 verbose ("Soal salah / typo", dst.) — bukan short-form RESEARCH lama
> - Modal copy = D-06 eksplisit ("menggagalkan peserta + NomorSertifikat dicabut" / "meluluskan peserta + NomorSertifikat baru di-generate")
> - Tombol modal = "Batal" + "Lanjutkan, simpan perubahan" — bukan "Cancel" + "Lanjut"
> - A11y: `aria-labelledby`, `aria-describedby`, `aria-required`, `aria-label` di close button, `for` attr di label-select pairing (UI-SPEC §A11y)

- [ ] **Step 2: Stub JS file**

Buat `wwwroot/js/edit-peserta-answers.js` placeholder (diisi Task 7):

```javascript
console.log("edit-peserta-answers.js loaded");
```

- [ ] **Step 3: Verify build + UAT initial render**

Run: `dotnet build && dotnet run`. Buka `http://localhost:5277/Admin/EditPesertaAnswers/{sessionId}` untuk session Completed valid. Verify page render tanpa error 500, semua soal MC/MA tampil dengan radio/checkbox terisi, kunci ditandai.

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/EditPesertaAnswers.cshtml wwwroot/js/edit-peserta-answers.js
git commit -m "feat(v17.0-p321): add EditPesertaAnswers.cshtml + JS stub (UI-SPEC reconciled)"
```

---

## Task 7: Frontend JS — Dirty State + Reason Validation + Flip Modal

**Files:**
- Modify: `wwwroot/js/edit-peserta-answers.js`

- [ ] **Step 1: Implementasi full JS (D-06 modal copy reconciled)**

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

  function updateDirtyUI(card) {
    const reasonBlock = card.querySelector(".reason-block");
    if (!reasonBlock) return;
    if (isDirty(card)) {
      card.classList.add("border-warning");
      reasonBlock.classList.remove("d-none");
    } else {
      card.classList.remove("border-warning");
      reasonBlock.classList.add("d-none");
      const select = reasonBlock.querySelector(".reason-code");
      const textarea = reasonBlock.querySelector(".reason-text");
      if (select) select.value = "";
      if (textarea) { textarea.value = ""; textarea.classList.add("d-none"); }
    }
  }

  document.querySelectorAll(".answer-input").forEach(input => {
    input.addEventListener("change", () => {
      const card = input.closest(".question-card");
      updateDirtyUI(card);
    });
  });

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
        alert(`Pilih alasan edit terlebih dahulu (Soal #${d.questionId}).`);
        return false;
      }
      if (d.reasonCode === "Lainnya" && !d.reasonText.trim()) {
        alert(`Isi detail alasan untuk opsi Lainnya (Soal #${d.questionId}).`);
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
      const resp = await fetch(previewUrl, {
        method: "POST",
        body: fd,
        headers: {
          "RequestVerificationToken": form.querySelector('input[name=__RequestVerificationToken]').value
        }
      });
      if (!resp.ok) throw new Error("Preview gagal");
      const preview = await resp.json();
      const oldPassed = preview.oldIsPassed;
      const newPassed = preview.newIsPassed;
      const flip = (oldPassed === true && newPassed === false) || (oldPassed === false && newPassed === true);

      if (flip) {
        // D-06 eksplisit copy
        let msg;
        if (oldPassed === true && newPassed === false) {
          msg = `Perubahan ini akan <strong>menggagalkan peserta</strong>. ` +
                `NomorSertifikat akan dicabut${preview.nomorSertifikat ? ` (No: ${preview.nomorSertifikat})` : ""} ` +
                `dan TrainingRecord di-set Failed. Lanjutkan?`;
        } else {
          msg = `Perubahan ini akan <strong>meluluskan peserta</strong>. ` +
                (preview.willGenerateCert
                  ? `NomorSertifikat baru akan di-generate (GenerateCertificate && bukan PreTest).`
                  : `Sertifikat TIDAK akan di-generate (session bukan eligible).`) +
                ` Lanjutkan?`;
        }
        flipBody.innerHTML = msg;
        flipConfirmBtn.onclick = () => { flipModal.hide(); form.submit(); };
        flipModal.show();
        submitBtn.disabled = false;
        submitBtn.textContent = "Save & Recompute";
        // Restore focus ke submit after modal close (a11y)
        document.getElementById('flipConfirmModal').addEventListener('hidden.bs.modal', function once() {
          submitBtn.focus();
          document.getElementById('flipConfirmModal').removeEventListener('hidden.bs.modal', once);
        });
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

- [ ] **Step 2: Verify UAT initial render + dirty toggle + flip preview**

Run: `dotnet run`. Buka edit page:
- Ubah 1 jawaban MC → card highlight warning, reason dropdown muncul.
- Pilih "Lainnya" → textarea muncul.
- Switch back ke jawaban asli → highlight + reason hilang.
- Submit dengan jawaban yang flip Pass→Fail → modal muncul dengan body copy "menggagalkan peserta + NomorSertifikat dicabut".

- [ ] **Step 3: Commit**

```bash
git add wwwroot/js/edit-peserta-answers.js
git commit -m "feat(v17.0-p321): edit-peserta-answers.js (dirty state + reason + D-06 flip preview)"
```

---

## Task 8: Controller POST `SubmitEditAnswers` + Transaction

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs`
- Create: `Models/ViewModels/EditAnswersSubmission.cs`

- [ ] **Step 1: DTO**

Buat `Models/ViewModels/EditAnswersSubmission.cs`:

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

    // Task 9 dry-run DTOs
    public class EditDraft
    {
        public int QuestionId { get; set; }
        public List<int> Options { get; set; } = new();
    }
    public class EditDraftSubmission
    {
        public List<EditDraft> Drafts { get; set; } = new();
    }
}
```

- [ ] **Step 2: Tambah POST action**

Di `AssessmentAdminController.cs` (pakai `_gradingService` + `_hubContext` field yang SUDAH di-inject ctor — tidak perlu `[FromServices]`):

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitEditAnswers(HcPortal.Models.ViewModels.EditAnswersSubmission form)
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

    // Concurrency check (tolerance 1 detik karena DateTime round-trip ISO 8601)
    var currentUpdatedAt = session.UpdatedAt ?? session.CreatedAt;
    if (Math.Abs((currentUpdatedAt - form.UpdatedAt).TotalSeconds) > 1)
    {
        TempData["Error"] = "Sesi sudah diubah admin lain. Refresh halaman.";
        return redirectBack;
    }

    // Validate reasons (server-side)
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

        // Recompute via injected _gradingService
        var (newScore, newIsPassed, oldScore, oldIsPassed) = await _gradingService.RegradeAfterEditAsync(session);

        // Backfill NewScore/NewIsPassed di AssessmentEditLog entries baru
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

        // Cache invalidate (kalau ada key exam-status)
        _cache.Remove($"exam-status-{session.Id}");

        // SignalR broadcast via injected _hubContext (NOT [FromServices])
        var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
        await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("workerAnswerEdited", new
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
        TempData["Error"] = "Terjadi kesalahan saat menyimpan. Coba lagi atau hubungi administrator.";
        return redirectBack;
    }
}
```

- [ ] **Step 2: Verify ctor injection**

VERIFIED 2026-05-21 (`Controllers/AssessmentAdminController.cs:22-50`):
- `_cache` (IMemoryCache) — line 22 ✓
- `_logger` (ILogger<AssessmentAdminController>) — line 25 ✓
- `_hubContext` (IHubContext<AssessmentHub>) — line 27 ✓
- `_gradingService` (GradingService) — line 29 ✓

Semua field SUDAH ada → tidak perlu `[FromServices]` di action signature.

- [ ] **Step 3: Verify build**

Run: `dotnet build`. Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs Models/ViewModels/EditAnswersSubmission.cs
git commit -m "feat(v17.0-p321): POST SubmitEditAnswers (tx + audit + regrade + SignalR)"
```

---

## Task 9: Controller `PreviewEditScore` (Dry-Run)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (DTO sudah di-create Task 8)

- [ ] **Step 1: Tambah action**

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PreviewEditScore(int sessionId,
    HcPortal.Models.ViewModels.EditDraftSubmission form)
{
    var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    if (session == null) return NotFound();
    if (!await HcPortal.Helpers.AssessmentEditEligibility.IsEditableAsync(_context, session))
        return Forbid();

    var overrideAnswers = form.Drafts.ToDictionary(d => d.QuestionId, d => d.Options);

    // PreviewScoreAsync sudah di Task 4 (public wrapper)
    var (newScore, newIsPassed) = await _gradingService.PreviewScoreAsync(session, overrideAnswers);

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

- [ ] **Step 2: Verify build**

Run: `dotnet build`. Expected: Build succeeded.

- [ ] **Step 3: Smoke test endpoint**

`dotnet run`. Buka edit page, ubah jawaban, klik Save. DevTools Network tab → verify POST `/Admin/PreviewEditScore?sessionId=...` return JSON shape `{oldScore, oldIsPassed, newScore, newIsPassed, hasCert, nomorSertifikat, willGenerateCert}`.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v17.0-p321): POST PreviewEditScore endpoint (dry-run flip detection)"
```

---

## Task 10: View `AssessmentMonitoringDetail.cshtml` — Dropdown ⋮

**Files:**
- Modify: `Views/Admin/AssessmentMonitoringDetail.cshtml` (line 286-338 = action column block, line 1496 total)

- [ ] **Step 1: Lokasi per-user table action column VERIFIED**

VERIFIED 2026-05-21 line 286-338:
- Reshuffle button line 290-296 (Bootstrap btn-outline-primary + `bi-shuffle`)
- AkhiriUjian form line 301-308 (`asp-controller="AssessmentAdmin"`)
- Reset form line 313-320 (`bi-arrow-counterclockwise`)
- View Results link line 325-328 (Completed only)
- Activity Log button line 331-337 (`btn-activity-log` class + `bi-clock-history` + `data-session-id` + `data-worker-name`)

Field check: row pakai `session.UserStatus` (line 288, 299, 311, 323) — bukan `session.Status`.

- [ ] **Step 2: Refactor action column ke hybrid layout (UI-SPEC §Component Inventory Surface 2)**

Pertahankan inline: `[View Results]` + `[🕐 Activity Log]`. Pindahkan ke dropdown ⋮: `Edit Jawaban` (kondisional `IsEditableShallow`) + `Reset` + `Akhiri Ujian` + `Reshuffle`. Pakai `bi bi-*` icons (UI-SPEC override emoji preferensi):

```html
@{ var canEdit = HcPortal.Helpers.AssessmentEditEligibility.IsEditableShallow(session); }

<td>
  <div class="d-flex gap-1 align-items-center">
    @* Inline: View Results (Completed only) *@
    @if (session.UserStatus == "Completed")
    {
      <a href="@Url.Action("Results", "CMP", new { id = session.Id })" target="_blank"
         class="btn btn-success btn-sm">View Results</a>
    }
    @* Inline: Activity Log (always) — existing class btn-activity-log *@
    <button type="button"
            class="btn btn-outline-secondary btn-sm btn-activity-log"
            data-session-id="@session.Id"
            data-worker-name="@session.UserFullName"
            title="Lihat activity log">
      <i class="bi bi-clock-history"></i>
    </button>

    @* Dropdown ⋮ *@
    <div class="dropdown">
      <button class="btn btn-sm btn-outline-secondary"
              type="button"
              data-bs-toggle="dropdown"
              aria-expanded="false"
              aria-haspopup="true"
              aria-label="Aksi lain untuk @session.UserFullName"
              style="min-height:40px;">⋮</button>
      <ul class="dropdown-menu dropdown-menu-end">
        @if (canEdit)
        {
          <li>
            <a class="dropdown-item" asp-action="EditPesertaAnswers" asp-controller="AssessmentAdmin" asp-route-id="@session.Id">
              <i class="bi bi-pencil-square me-1"></i>Edit Jawaban
            </a>
          </li>
        }
        @if (session.UserStatus != "Cancelled")
        {
          <li>
            @* Reuse existing Reset form — wrap in <li> *@
            <form asp-action="ResetAssessment" asp-controller="AssessmentAdmin" method="post" class="m-0"
                  onsubmit="return confirm('Reset sesi ini? Semua jawaban akan dihapus dan peserta dapat mengulang ujian.')">
              @Html.AntiForgeryToken()
              <input type="hidden" name="id" value="@session.Id" />
              <button type="submit" class="dropdown-item">
                <i class="bi bi-arrow-counterclockwise me-1"></i>Reset
              </button>
            </form>
          </li>
        }
        @if (session.UserStatus == "InProgress")
        {
          <li>
            <form asp-action="AkhiriUjian" asp-controller="AssessmentAdmin" method="post" class="m-0"
                  onsubmit="return confirm('Akhiri ujian untuk @(session.UserFullName)? Jawaban tersimpan akan dinilai otomatis.')">
              @Html.AntiForgeryToken()
              <input type="hidden" name="id" value="@session.Id" />
              <button type="submit" class="dropdown-item text-danger">
                <i class="bi bi-x-octagon me-1"></i>Akhiri Ujian
              </button>
            </form>
          </li>
        }
        @if (Model.IsPackageMode && (session.UserStatus == "Not started" || session.UserStatus == "Abandoned"))
        {
          <li>
            <button type="button"
                    class="dropdown-item"
                    data-session-id="@session.Id"
                    onclick="reshuffleWorker(@session.Id)">
              <i class="bi bi-shuffle me-1"></i>Reshuffle
            </button>
          </li>
        }
      </ul>
    </div>
  </div>
</td>
```

> **UI-SPEC reconciled:**
> - `bi bi-*` icons (bukan emoji `✏️🔄❌🔀`) — UI-SPEC line 159 override RESEARCH lama
> - `aria-label="Aksi lain untuk {workerName}"` + `aria-haspopup="true"` + `dropdown-menu-end` (UI-SPEC §A11y Contract)
> - Touch target 40px tinggi via `style="min-height:40px"` (UI-SPEC §Spacing Scale exception)
> - Existing handlers PRESERVED: form POST + `onsubmit` confirm dialog (Reset/AkhiriUjian), `onclick="reshuffleWorker(@session.Id)"` (existing inline JS function di scripts section).
> - Status string verified: "Not started" (lowercase 's', space) — line 288 actual.

- [ ] **Step 3: Verify build + UAT**

Run: `dotnet build && dotnet run`. Buka monitoring detail page. Per row:
- Verify `View Results` + `🕐` tetap inline
- Click `⋮` → menu muncul aligned `dropdown-menu-end` + ARIA aria-expanded toggle correct
- Verify item conditional: `Edit Jawaban` hanya untuk session Completed non-Manual non-Proton-Tahun3
- Verify `Reset/Akhiri Ujian/Reshuffle` muncul sesuai status existing
- ESC close dropdown (Bootstrap default)
- Mobile viewport sempit → auto-flip via Popper.js

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/AssessmentMonitoringDetail.cshtml
git commit -m "feat(v17.0-p321): per-user table action dropdown ⋮ hybrid (UI-SPEC bi-icons + ARIA)"
```

---

## Task 11: SignalR Frontend Handler `workerAnswerEdited`

**Files:**
- Modify: `Views/Admin/AssessmentMonitoringDetail.cshtml` (scripts section line 1243-1290+ — dekat handler `workerSubmitted` existing)

- [ ] **Step 1: Lokasi SignalR handler existing VERIFIED**

VERIFIED 2026-05-21:
- `window.assessmentHub.on('workerSubmitted', function(data) { ... })` — line 1244
- Hub connection variable = `window.assessmentHub` (set di `wwwroot/js/assessment-hub.js:95`)
- Toast function = `window.showAssessmentToast(message, linkUrl?, linkText?)` — line 96 of `assessment-hub.js`
- Scripts section di view = `@section Scripts { ... }` (lokasi tipikal di bawah)

**KOREKSI dari RESEARCH lama:** Toast function bukan `showToast(msg, "info")` atau `window.toastr` — tidak exist. Reuse `window.showAssessmentToast` (D-07 lock + UI-SPEC §Toast SignalR).

- [ ] **Step 2: Tambah handler workerAnswerEdited**

Di scripts section, setelah block `window.assessmentHub.on('workerSubmitted', ...)` line 1244:

```javascript
// workerAnswerEdited: admin/HC edited a session's answers (Phase 321 EDIT-09)
window.assessmentHub.on('workerAnswerEdited', function (data) {
    // Update row score + result cell tanpa full reload
    var row = document.querySelector('tr[data-session-id="' + data.sessionId + '"]');
    if (row) {
        var scoreCell = row.querySelector('.score-cell');
        var resultCell = row.querySelector('.result-cell');
        if (scoreCell) scoreCell.textContent = data.newScore + '%';
        if (resultCell) {
            resultCell.textContent = data.newIsPassed ? 'Pass' : 'Fail';
            resultCell.className = 'result-cell ' + (data.newIsPassed ? 'text-success' : 'text-danger');
        }
    }

    // D-07 toast verbose template
    var oldResult = (data.oldIsPassed === true) ? 'Pass' : (data.oldIsPassed === false) ? 'Fail' : '—';
    var newResult = data.newIsPassed ? 'Pass' : 'Fail';
    var flipSegment = (data.oldIsPassed !== data.newIsPassed)
        ? ', ' + oldResult + '→' + newResult
        : '';
    var msg = data.actorRole + ' ' + data.actorName +
              ' edit jawaban ' + data.workerName +
              ': Score ' + (data.oldScore ?? '—') + '→' + data.newScore +
              flipSegment;
    // REUSE existing toast (verified wwwroot/js/assessment-hub.js:96)
    if (typeof window.showAssessmentToast === 'function') {
        window.showAssessmentToast(msg);
    } else {
        console.log('[toast]', msg);
    }
});
```

> **UI-SPEC reconciled:**
> - `window.showAssessmentToast(message)` reuse — D-07 + UI-SPEC §Toast SignalR
> - Toast otomatis 5 detik fade (existing pattern `assessment-hub.js:32`) — bukan 8 detik (note: UI-SPEC says 8 detik but actual is 5 detik per existing code; recommend leave as is unless user requests override — re-toast UX consistent across all SignalR events). **Open question for planner:** override timeout di handler ke 8 detik (CD-04 freedom) atau leave 5 detik (consistency)?
> - Selectors: `tr[data-session-id]`, `.score-cell`, `.result-cell` — verify saat execute lewat grep di `AssessmentMonitoringDetail.cshtml` (line 280-282 confirms `result-cell` class; score cell needs verify).

- [ ] **Step 3: Verify selector class names existing**

Run before commit:
```bash
grep -nE 'class="(score|result|completedat)-cell|data-session-id' Views/Admin/AssessmentMonitoringDetail.cshtml
```

Adjust handler selector kalau class actual berbeda. Line 282 confirms `result-cell`; line 284 `completedat-cell`. Score cell perlu confirm — kemungkinan inline tanpa class. Kalau no class, tambah `class="score-cell"` di Step 2 task 10 markup atau target via `td:nth-child(N)`.

- [ ] **Step 4: Verify UAT 2-tab live update**

Run: `dotnet run`. Buka monitoring detail di 2 tab browser sekaligus (sama session group). Di tab 1, lakukan edit jawaban → save. Di tab 2 → verify row score + result cell update otomatis tanpa reload, toast muncul dengan format D-07 verbose.

- [ ] **Step 5: Commit**

```bash
git add Views/Admin/AssessmentMonitoringDetail.cshtml
git commit -m "feat(v17.0-p321): SignalR workerAnswerEdited handler + showAssessmentToast reuse"
```

---

## Task 12: Activity Log Modal — Tab "Edit History"

**Files:**
- Modify: `Views/Admin/AssessmentMonitoringDetail.cshtml` (modal INLINE line 540-559)
- Create: `Views/Admin/_EditHistoryPartial.cshtml` (controller View override line 57 resolves)
- Modify: `Controllers/AssessmentAdminController.cs` (tambah `EditHistoryPartial` action)

- [ ] **Step 1: Lokasi modal Activity Log VERIFIED**

Modal Activity Log INLINE di `Views/Admin/AssessmentMonitoringDetail.cshtml:540-559`. **TIDAK ada `_ActivityLogModal.cshtml` partial.** Modal di-populate via AJAX `GET /Admin/GetActivityLog` (`AssessmentAdminController.cs:5643`).

Current modal body structure (line 550-556):
```html
<div class="modal-body">
    <div id="logSummary" class="mb-3 p-2 bg-light rounded small text-muted"></div>
    <div id="logTimeline"></div>
    <div id="logLoading" class="text-center d-none">
        <div class="spinner-border spinner-border-sm me-2"></div>Memuat...
    </div>
</div>
```

- [ ] **Step 2: Refactor modal body jadi tabs**

Bungkus content existing dalam tab "Activity Timeline", tambah tab "Edit History":

```html
<div class="modal-body">
  <ul class="nav nav-tabs" role="tablist" id="activityLogTabs">
    <li class="nav-item" role="presentation">
      <button class="nav-link active" id="tab-timeline-btn"
              data-bs-toggle="tab" data-bs-target="#tab-timeline"
              role="tab" aria-controls="tab-timeline" aria-selected="true">
        Activity Timeline
      </button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" id="tab-edit-history-btn"
              data-bs-toggle="tab" data-bs-target="#tab-edit-history"
              role="tab" aria-controls="tab-edit-history" aria-selected="false"
              data-load-url-template="@Url.Action("EditHistoryPartial")">
        Edit History
      </button>
    </li>
  </ul>
  <div class="tab-content pt-3">
    <div class="tab-pane fade show active" id="tab-timeline" role="tabpanel" aria-labelledby="tab-timeline-btn">
      <div id="logSummary" class="mb-3 p-2 bg-light rounded small text-muted"></div>
      <div id="logTimeline"></div>
      <div id="logLoading" class="text-center d-none">
        <div class="spinner-border spinner-border-sm me-2"></div>Memuat...
      </div>
    </div>
    <div class="tab-pane fade" id="tab-edit-history" role="tabpanel" aria-labelledby="tab-edit-history-btn">
      <div class="edit-history-placeholder text-muted small">Memuat...</div>
    </div>
  </div>
</div>
```

> Note: existing JS yang populate `#logSummary` + `#logTimeline` saat modal open (line 1052 area `bootstrap.Modal`) tidak perlu diubah — wrapper di-pertahankan inside `#tab-timeline`.

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
    // Controller override line 57 resolve ke ~/Views/Admin/_EditHistoryPartial.cshtml
}
```

- [ ] **Step 4: Buat partial view (UI-SPEC §Surface 3)**

`Views/Admin/_EditHistoryPartial.cshtml`:

```html
@model List<HcPortal.Models.AssessmentEditLog>

@{
    // D-05 verbose reason label mapper
    string ReasonLabel(string code) => code switch
    {
        "SoalSalah" => "Soal salah / typo",
        "KunciSalah" => "Kunci jawaban salah",
        "BugSistem" => "Bug sistem / glitch",
        "PermintaanPeserta" => "Permintaan koreksi peserta",
        "Lainnya" => "Lainnya (jelaskan)",
        _ => code
    };
}

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
        <div class="small text-muted">
          Alasan: @ReasonLabel(log.ReasonCode)
          @if (!string.IsNullOrEmpty(log.ReasonText)) { @($" — {log.ReasonText}") }
        </div>
      </li>
    }
  </ul>
}
```

- [ ] **Step 5: Lazy-load tab via JS (inline scripts section)**

Tambah inline JS di scripts section MonitoringDetail (CD-04 lazy strategy):

```javascript
// Lazy-load Edit History tab on first show
document.addEventListener('shown.bs.tab', function (event) {
    var btn = event.target;
    if (btn.id !== 'tab-edit-history-btn') return;
    if (btn.dataset.loaded === '1') return;

    // Get sessionId from current open modal (set when btn-activity-log clicked)
    var modal = document.getElementById('activityLogModal');
    var sessionId = modal ? modal.dataset.currentSessionId : null;
    if (!sessionId) {
        document.querySelector('#tab-edit-history .edit-history-placeholder').textContent = 'Sesi tidak diketahui.';
        return;
    }

    var urlTemplate = btn.dataset.loadUrlTemplate;
    var url = urlTemplate + '?sessionId=' + sessionId;
    var pane = document.getElementById('tab-edit-history');

    fetch(url)
        .then(function (resp) {
            if (!resp.ok) throw new Error('HTTP ' + resp.status);
            return resp.text();
        })
        .then(function (html) {
            pane.innerHTML = html;
            btn.dataset.loaded = '1';
        })
        .catch(function (err) {
            pane.innerHTML = '<p class="text-danger small">Gagal memuat edit history.</p>';
            console.error('EditHistory load failed:', err);
        });
});

// Set modal.dataset.currentSessionId when btn-activity-log clicked
// (extend existing click handler at line ~928 to also set this attr; reset loaded flag too)
document.addEventListener('click', function (e) {
    var btn = e.target.closest('.btn-activity-log');
    if (!btn) return;
    var modal = document.getElementById('activityLogModal');
    if (!modal) return;
    modal.dataset.currentSessionId = btn.dataset.sessionId;
    // Reset Edit History tab to force re-fetch for new session
    var ehBtn = document.getElementById('tab-edit-history-btn');
    if (ehBtn) ehBtn.dataset.loaded = '';
    var ehPane = document.getElementById('tab-edit-history');
    if (ehPane) ehPane.innerHTML = '<div class="edit-history-placeholder text-muted small">Memuat...</div>';
});
```

- [ ] **Step 6: Verify build + UAT**

Run: `dotnet build && dotnet run`. Buka monitoring detail → klik 🕐 tombol di row → modal Activity Log muncul dengan 2 tab. Default tab "Activity Timeline" (existing content preserved). Klik tab "Edit History" → trigger AJAX `GET /Admin/EditHistoryPartial?sessionId={id}` → render entries (kalau ada). Lakukan 1 edit dulu via Task 8 → reopen modal → tab Edit History show entry baru.

- [ ] **Step 7: Commit**

```bash
git add Views/Admin/AssessmentMonitoringDetail.cshtml Views/Admin/_EditHistoryPartial.cshtml Controllers/AssessmentAdminController.cs
git commit -m "feat(v17.0-p321): Activity Log Edit History tab (lazy-load partial, D-05 reason labels)"
```

---

## Task 13: Manual UAT Full Checklist + Final Commit

**Files:** None (UAT only)

- [ ] **Step 1: Hybrid testing per CONTEXT D-04**

### 4 Playwright Tests (automated)

1. **Auth gate (Admin/HC/Worker)** — Login 3 role → GET `/Admin/EditPesertaAnswers/{id}` → assert 200 (Admin/HC) vs 403/redirect-login (Worker). REQ EDIT-01.
2. **Happy-path edit save** — Login Admin → GET edit page → ubah 1 jawaban MC → isi reason preset `SoalSalah` → POST submit → assert redirect monitoring + DB `AssessmentEditLog` count++ + score recompute. End-to-end edit flow.
3. **Concurrency stale stage** — 2 browser context (Admin A + Admin B) buka edit page sama, A submit dulu → B coba submit → assert B kena TempData error "Sesi sudah diubah admin lain." REQ EDIT-07.
4. **Flip preview dry-run AJAX** — POST `PreviewEditScore` dengan draft answers → assert JSON response shape `{oldScore, newScore, oldIsPassed, newIsPassed, hasCert, nomorSertifikat, willGenerateCert}`. REQ EDIT-10.

### 4 Manual UAT (Playwright tidak praktis)

1. **DB verify cascade flip** — Pass→Fail: `sqlcmd` query `SELECT NomorSertifikat, ValidUntil FROM AssessmentSessions WHERE Id={id}` assert NULL + `SELECT Status FROM TrainingRecords WHERE UserId=...AND Judul=...` assert "Failed". Fail→Pass: NomorSertifikat baru generated + TrainingRecord upsert Status="Passed". REQ EDIT-04.
2. **SignalR cross-tab live update** — Buka 2 tab `AssessmentMonitoringDetail`, edit di tab 1 → tab 2 row score+result cell auto-update + toast verbose D-07 muncul tanpa refresh. REQ EDIT-09.
3. **Activity Log Edit History tab** — Buka modal Activity Log → klik tab "Edit History" → verify list timeline entries (timestamp, soal, old→new, actor, reason verbose label D-05) sesuai edit yang baru dilakukan. REQ EDIT-11.
4. **Migration rollback lokal** — `dotnet ef database update {PrevMigration}` → verify table `AssessmentEditLogs` drop via `sqlcmd ... SELECT OBJECT_ID('AssessmentEditLogs')` assert NULL → `dotnet ef database update` lagi → verify re-create. WAJIB lulus sebelum commit migration file (DEV_WORKFLOW §4). REQ EDIT-13.

- [ ] **Step 2: Pre-commit checklist DEV_WORKFLOW §5**

```
- [ ] dotnet build pass (tanpa warning baru)
- [ ] dotnet run + manual verify di http://localhost:5277
- [ ] Golden path & edge case dicek manual (4 manual UAT di atas)
- [ ] 4 Playwright tests pass
- [ ] DB lokal: migration AddAssessmentEditLogs apply + rollback OK (Task 1)
- [ ] Migration file Migrations/{ts}_AddAssessmentEditLogs.cs di-commit
- [ ] CLAUDE.md compliance: semua copy Bahasa Indonesia, ikut DEV_WORKFLOW
- [ ] Team IT di-notify (commit hash + tag + flag MIGRATION ADA "AddAssessmentEditLogs")
```

- [ ] **Step 3: Tag milestone phase**

```bash
git tag -a v17.0-p321-complete -m "Milestone v17.0 Phase 321: Edit Jawaban complete"
```

- [ ] **Step 4: Push feature branch + merge + push tag (CONTEXT D-03 + CD-07)**

```bash
# On feature/phase-321-edit-jawaban after all tasks pass:
git checkout main
git merge --no-ff feature/phase-321-edit-jawaban -m "Merge phase 321: edit jawaban peserta"
git push origin main
git push origin v17.0-p321-complete
```

- [ ] **Step 5: Notify IT (CONTEXT D-08 final)**

```
Milestone v17.0 Phase 321 deploy-ready. Commit hash: {LAST_COMMIT_HASH}. Tag: v17.0-p321-complete.

⚠️ Ada migration baru: AddAssessmentEditLogs — perlu `dotnet ef database update --context ApplicationDbContext` di DB Dev.
Verify post-deploy: /Admin/EditPesertaAnswers/{sessionId} accessible untuk Admin/HC role.
```

---

## Validation Architecture

> Required (`nyquist_validation: true` in `.planning/config.json`).
> Strategy locked per CONTEXT D-04: hybrid 4 Playwright + 4 manual UAT.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | (a) Project build/runtime: .NET 8 `dotnet build` + `dotnet run` (b) E2E: Playwright (existing `tests/e2e/` infra dari Phase 313.1, opportunistic — TIDAK ada xUnit) |
| Config file | (a) `*.csproj` (b) `playwright.config.ts` di root atau `tests/e2e/` |
| Quick run command | `dotnet build` (~10 detik) |
| Full suite command | `dotnet build && dotnet run` + Playwright spec run (manual launch) |
| Migration test | `dotnet ef database update --context ApplicationDbContext` (apply) + `dotnet ef database update {PrevMigration}` (rollback) — wajib Task 1 |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Validation Mechanism | Pass Criteria |
|--------|----------|-----------|----------------------|----------------|
| EDIT-01 | Halaman `/Admin/EditPesertaAnswers/{id}` Admin/HC accessible, Worker blocked | **Playwright** | Login 3 role, GET endpoint, assert response code | Admin/HC = 200 OK + form render; Worker = 403 atau redirect /Account/Login |
| EDIT-02 | `IsEditable` gate Status=Completed + !ManualEntry + !ProtonT3 di GET, POST, dan UI | Manual UAT | Akses GET untuk sesi InProgress / IsManualEntry=true / Proton+Tahun3 | Redirect ke MonitoringDetail + TempData "Sesi tidak dapat diedit" |
| EDIT-03 | Recompute Score+IsPassed+ET via `RegradeAfterEditAsync` | **Playwright** (happy-path) + Manual UAT (ET verify) | Playwright: edit 1 MC → submit → re-GET monitoring → assert score cell berubah. Manual: `sqlcmd SELECT * FROM SessionElemenTeknisScores WHERE AssessmentSessionId={id}` assert ET breakdown re-computed | Score persisted = pre-computed value; ET rows = recomputed count |
| EDIT-04 | Pass↔Fail cascade NomorSertifikat + TrainingRecord | **Manual UAT (DB query)** | `sqlcmd` query Pass→Fail check NULL cert + TR=Failed; Fail→Pass check cert generated + TR upsert Passed | DB state match cascade rules |
| EDIT-05 | Reason dropdown 5 preset + Lainnya wajib teks | Client (alert popup di JS Task 7) + Server validation (Task 8 TempData) | Manual UAT: edit tanpa pilih reason → blocked; pilih Lainnya tanpa text → blocked | Both client + server reject empty / invalid reasons |
| EDIT-06 | Dual audit: AuditLog + AssessmentEditLog per question | **Manual UAT (DB query)** | `sqlcmd SELECT * FROM AuditLogs WHERE ActionType='EditAssessmentAnswer'` + `SELECT * FROM AssessmentEditLogs WHERE AssessmentSessionId={id}` | Both tables have rows; counts match expectation |
| EDIT-07 | Concurrency UpdatedAt token stale → reject | **Playwright** (cross-context) | 2 browser context buka edit page → A submit → B submit → assert B redirect dengan TempData stale error | B redirected ke MonitoringDetail + TempData "Sesi sudah diubah admin lain. Refresh halaman." |
| EDIT-08 | Transaction scope rollback total kalau exception | Manual UAT (chaos test) | Trigger exception (e.g. invalid optionId via DevTools manual POST) → verify DB state unchanged | Edit tidak commit; SessionElemenTeknisScores tidak ter-delete; AuditLog tidak insert |
| EDIT-09 | SignalR `workerAnswerEdited` ke `monitor-{batchKey}` → row update + toast | **Manual UAT (2-tab)** | 2 tab MonitoringDetail buka group sama, tab1 edit → tab2 verify row + toast | Tab2 row score+result cell update tanpa refresh; toast format `{actorRole} {actorName} edit jawaban {workerName}: Score {old}→{new}, {flip}` muncul |
| EDIT-10 | PreviewEditScore JSON contract | **Playwright** | POST `/Admin/PreviewEditScore?sessionId={id}` dengan FormData drafts → assert JSON response shape | Response = `{oldScore: int, newScore: int, oldIsPassed: bool, newIsPassed: bool, hasCert: bool, nomorSertifikat: string\|null, willGenerateCert: bool}` |
| EDIT-11 | Edit History tab lazy-load partial sort DESC | **Manual UAT (browser)** | Buka modal Activity Log → klik tab "Edit History" → verify entries sort DESC + reason label verbose | Tab show list entries, urut EditedAt DESC; reason label = "Soal salah / typo" bukan "SoalSalah" |
| EDIT-12 | Dropdown ⋮ hybrid + ARIA + conditional render | **Manual UAT (a11y)** | Buka MonitoringDetail → verify dropdown ARIA attrs + keyboard nav (TAB + ENTER + ESC) + conditional Edit Jawaban visibility | aria-haspopup="true", aria-expanded toggles, dropdown-menu-end aligned, ESC close, Edit Jawaban hanya muncul kalau IsEditableShallow |
| EDIT-13 | Migration apply + rollback verified | **Manual UAT (dotnet ef + sqlcmd)** | `dotnet ef database update` → `sqlcmd OBJECT_ID('AssessmentEditLogs')` not null. Rollback → null. Re-apply → not null | Both apply + rollback succeed tanpa error |

### Sampling Rate

- **Per task commit:** `dotnet build` (mandatory; ~10s)
- **Per wave merge:** N/A (Phase 321 = sequential single-stream, no wave parallelism per CONTEXT D-02)
- **Phase gate:** Full UAT 8 items (4 Playwright + 4 manual) GREEN sebelum `git tag v17.0-p321-complete` + IT notify

### Wave 0 Gaps

- [ ] **Playwright spec file** `tests/e2e/edit-peserta-answers.spec.ts` — perlu create kalau belum ada (mirror pattern `tests/e2e/exam-types.spec.ts` Phase 317)
- [ ] **Test helper** `tests/e2e/helpers/editAnswers.ts` — function exports untuk login Admin/HC/Worker + GET edit page + POST edit form
- [ ] **Seed scenario** `docs/SEED_JOURNAL.md` entry untuk Phase 321 — perlu sesi Completed yang Pass (untuk Pass→Fail test) + sesi Completed yang Fail (untuk Fail→Pass test)
- [ ] Framework install: NONE — Playwright sudah installed (verified Phase 313.1 carry-forward)

*(Kalau executor pilih skip Playwright dan murni manual UAT untuk Phase 321, dokumentasikan deviation di SUMMARY.md — D-04 hybrid pattern memberi ruang opportunistic.)*

---

## Spec Coverage Map

| Spec Section | Covered by Task |
|---|---|
| 4.1 Scope (MC+MA edit, Completed only) | Task 2 (IsEditable), Task 5 (GET), Task 8 (POST) |
| 4.2 Helper Eligibility | Task 2 |
| 4.3 UI Dropdown Aksi Hybrid | Task 10 (bi-icons + ARIA + dropdown-menu-end UI-SPEC reconciled) |
| 4.4 Edit Jawaban Page (route + layout + reason dropdown) | Task 5, 6, 7 (verbose labels D-05) |
| 4.5 POST Submit Flow (16 step) | Task 8 (`_hubContext` + `_gradingService` field reuse) |
| 4.6 Konfirmasi Modal Pass↔Fail Flip | Task 6 (modal markup), Task 7 (JS preview AJAX, D-06 copy), Task 9 (PreviewEditScore) |
| 4.7 Tabel `AssessmentEditLog` + index | Task 1 |
| 4.8 `RegradeAfterEditAsync` + `ComputeScoreAndETInternalAsync(session, overrideAnswers?)` | Task 3, 4 (CertNumberHelper.Build CORRECTED signature) |
| 4.9 Activity Log Tab "Edit History" | Task 12 (inline modal refactor, partial `Views/Admin/_EditHistoryPartial.cshtml`) |
| 4.10 Permission Admin/HC | Inherited dari `[Authorize(Roles = "Admin, HC")]` di semua action |
| 5.1 SignalR `workerAnswerEdited` payload | Task 8 (broadcast via `_hubContext`), Task 11 (handler + `window.showAssessmentToast` reuse D-07) |
| 5.2 Cache Invalidation | Task 8 (`_cache.Remove`) |
| 5.3 Transaction Scope | Task 8 |
| 5.4 Anti-forgery | Task 8 (`[ValidateAntiForgeryToken]`), Task 6 (`@Html.AntiForgeryToken()`) |
| 5.5 Concurrency `UpdatedAt` token | Task 6 (hidden field ISO 8601 `"O"`), Task 8 (≤1s tolerance compare) |
| 5.6 Error Handling | Task 8 (try/catch + TempData) |
| 5.7 A11y + Mobile | Task 10 (dropdown ARIA UI-SPEC §A11y), Task 6 (form labels + `aria-required` + modal aria-labelledby/describedby + focus restore Task 7) |
| 5.8 Logging | Task 4 (`_logger.LogInformation`/LogWarning/LogError), Task 8 (controller logger) |
| 5.10 Manual UAT | Task 13 (4+4 hybrid per D-04) |
| 5.11 Migration `AddAssessmentEditLogs` | Task 1 (apply+rollback DEV_WORKFLOW §4) |

Tidak ada gap. Phase 321 ready untuk plan.

---

## Refresh Changelog (2026-05-21)

**Diff vs previous RESEARCH.md (preserved task structure 1-13, code blocks):**

1. **View folder corrected** `Views/AssessmentAdmin/` → `Views/Admin/` (controller line 53-57 explicit override) — affects Task 6, Task 10, Task 12 file paths.
2. **URL route corrected** `/AssessmentAdmin/EditPesertaAnswers/{id}` → `/Admin/EditPesertaAnswers/{id}` (via `[Route("Admin/[action]")]`) — affects Task 5 + Task 13 IT notify.
3. **CertNumberHelper signature corrected** `Format(year, seq)` → `Build(int seq, DateTime date)` + added `IsDuplicateKeyException(ex)` retry guard (Task 4).
4. **Toast function corrected** `showToast(msg, "info")` / `window.toastr` → `window.showAssessmentToast(message)` (Task 11) — verified `wwwroot/js/assessment-hub.js:96`.
5. **Controller DI ref corrected** `[FromServices] GradingService` / `IHubContext` → use `_gradingService` + `_hubContext` field existing (Task 8, Task 9) — verified ctor line 22-49.
6. **Modal Activity Log location corrected** `Views/Shared/_ActivityLogModal.cshtml` (non-existent) → INLINE di `Views/Admin/AssessmentMonitoringDetail.cshtml:540-559` (Task 12).
7. **Status field corrected** `session.Status` → `session.UserStatus` di monitoring view (Task 10) — verified line 288, 299, 311, 323.
8. **Status string corrected** "Not Started" (caps) → "Not started" (lower s) — verified line 288.
9. **Phase 320 line ref confirmed unchanged** `ExportAssessmentResults` at line 3651 (file 5879 lines total).
10. **UI-SPEC reconciliation (NEW)** — added explicit override notes:
    - Reason labels D-05 verbose ("Soal salah / typo" dst.) — replaces RESEARCH lama short-form
    - Flip modal D-06 eksplisit ("menggagalkan peserta + cabut NomorSertifikat" dst.) — replaces lama generic
    - Dropdown D-04+UI-SPEC: `bi bi-*` icons + `dropdown-menu-end` + `aria-label`/`aria-haspopup` + 40px touch target — replaces lama emoji-only
    - Toast D-07 template + reuse `window.showAssessmentToast`
    - A11y additions: `aria-labelledby`, `aria-describedby` modal, `aria-required` reason select, focus restore after flip modal close, label `for` attrs
11. **Validation Architecture section ADDED (NEW)** — required by `nyquist_validation: true`. Maps EDIT-01..13 → test mechanism + pass criteria. 4 Playwright + 4 manual UAT per D-04.
12. **`<user_constraints>` + `<phase_requirements>` sections ADDED** — required by gsd-planner contract (CONTEXT decisions + REQ ID map).
13. **Open question flagged** Task 11 toast timeout: existing `assessment-hub.js` = 5 detik fade; UI-SPEC D-07 spec = 8 detik. Recommend planner ask user or default to existing 5 detik consistency.

**Confidence:** HIGH untuk semua claim post-refresh (codebase re-read 2026-05-21 covering Controllers/AssessmentAdminController.cs, Services/GradingService.cs, Data/ApplicationDbContext.cs, Models/AssessmentSession.cs, Hubs/AssessmentHub.cs, Helpers/CertNumberHelper.cs, Views/Admin/AssessmentMonitoringDetail.cshtml, wwwroot/js/assessment-hub.js, .planning/config.json, 321-CONTEXT.md, 321-UI-SPEC.md).
