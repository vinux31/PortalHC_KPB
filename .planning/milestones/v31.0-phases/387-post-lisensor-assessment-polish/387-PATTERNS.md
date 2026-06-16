# Phase 387: Post-Lisensor Assessment Polish - Pattern Map

**Mapped:** 2026-06-15
**Files analyzed:** 5 source files / 7 in-place edits (PXF-06/08/09/10/11/12/13)
**Analogs found:** 7 / 7 (all analogs live in the same codebase — copy verbatim)

> **Phase shape:** bug-fix/polish. NO new files. Every REQ = in-place edit to an existing method/view, mirroring an existing analog. All analogs + current code re-verified against HEAD this session.
> **OUT of scope:** `Helpers/ExcelExportHelper.cs` (PXF-07/14 → Phase 386). Do NOT touch.
> **Dependency:** edits to `AssessmentAdminController.cs` (PXF-06/08/09/10) overlap Phase 386 writes — execute 387 AFTER 386.

## File Classification

| Modified File / Method | Role | Data Flow | Closest Analog | Match Quality |
|------------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` → `SubmitEssayScore` (~3525) | controller | request-response (guard) | same file `FinalizeEssayGrading` status-guard + `Models/AssessmentConstants.cs` | exact (same file pattern) |
| `Controllers/AssessmentAdminController.cs` → `FinalizeEssayGrading` cert (~3697) | controller | CRUD (write+retry) | `Services/GradingService.cs:287-318` retry-loop | exact (same logic, sibling) |
| `Controllers/AssessmentAdminController.cs` → BulkExport "Detail Jawaban" (~4828) | controller | transform / file-I/O (Excel) | same loop's MC branch (`:4840+`) + `sessionResp` data | role-match |
| `Controllers/AssessmentAdminController.cs` → `FinalizeEssayGrading` broadcast (~3753) | controller → SignalR | event-driven (pub-sub) | same file `:3204-3211` + `CMPController.cs:1786` `workerSubmitted` | exact (same group/pattern) |
| `Views/CMP/Results.cshtml:388` + `Views/CMP/ExamSummary.cshtml:59` | view (Razor) | request-response (render a11y) | `Views/CMP/StartExam.cshtml:125/134/148` `letters[oi]` | exact (same partial call) |
| `Controllers/CMPController.cs` → `SubmitExam` MC upsert (~1714) | controller | CRUD (write guard) | local guard `answers.ContainsKey(q.Id)` already at `:1703` | self-analog |
| `Hubs/AssessmentHub.cs` → `SaveTextAnswer` (~134) | SignalR hub | event-driven (write guard) | `SaveMultipleAnswer:205-215` timer guard | exact (sibling method) |

## Pattern Assignments

### PXF-06 — `SubmitEssayScore` guard pasca-finalize (controller, request-response) [MED]

**Target:** `Controllers/AssessmentAdminController.cs:3525` — insert guard BETWEEN step 3 (score-range, `:3540`) and step 4 (save, `:3542`).

**Current code (VERIFIED `:3525-3544`):** loads response → question → validates score range → saves `response.EssayScore` — NO session-status check.

**Analog — status enum (VERIFIED `Models/AssessmentConstants.cs:17`):**
```csharp
public const string Completed = "Completed";
public const string PendingGrading = "Menunggu Penilaian"; // string != const name
// NOTE: NO "Failed" exists. Terminal/finalized = Completed ONLY. Pass/fail = IsPassed bool.
```

**Analog — guard shape mirrors sibling `FinalizeEssayGrading` (`session.Status` vs `AssessmentConstants.AssessmentStatus.*` → `Json{success=false,message}`).**

**Copy-ready excerpt (insert before `:3542` save):**
```csharp
var session = await _context.AssessmentSessions.FindAsync(sessionId);
if (session == null)
    return Json(new { success = false, message = "Sesi tidak ditemukan" });
if (session.Status == AssessmentConstants.AssessmentStatus.Completed)
    return Json(new { success = false, message = "Sesi sudah final, skor essay tidak dapat diubah." });
```

**Guards (D-01a — highest risk):** block ONLY on `Completed`. NEVER include `PendingGrading` / `InProgress` (those are the legit grading window). Use the CONSTANT, not literal `"Completed"`. `AssessmentConstants` namespace `HcPortal.Models` already in use in this file. No recompute / no re-issue cert.

---

### PXF-08 — Cert nomor retry 3× + log + surface (controller, CRUD) [LOW]

**Target:** `Controllers/AssessmentAdminController.cs:3697-3711` inside `FinalizeEssayGrading` — replace single-attempt `try { } catch (DbUpdateException) { /* silent */ }`.

**Analog — TIRU VERBATIM `Services/GradingService.cs:287-318` (VERIFIED this session):**
```csharp
if (session.GenerateCertificate && isPassed)
{
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
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
            await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow))
                );
            certSaved = true;
        }
        catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
        {
            // Retry dengan sequence baru
        }
    }

    if (!certSaved)
    {
        _logger.LogError("Failed to generate certificate number for SessionId={SessionId} after {MaxAttempts} attempts due to repeated collisions.",
            session.Id, maxCertAttempts);
    }
}
```

**Surface to HC (D-03):** the method's final `Json` (`:3753-3759`) returns `nomorSertifikat = updatedSession?.NomorSertifikat`. When `isPassed && GenerateCertificate && string.IsNullOrEmpty(NomorSertifikat)` add a flag, e.g. `certError = "Nomor sertifikat gagal dibuat, coba lagi."`. Check JS caller in `Views/Admin/EssayGrading*.cshtml` for wiring (LOW — manual verify ok).

**Guards:** GradingService uses `session.Id`; in `FinalizeEssayGrading` the param is `sessionId` — match the local var actually used in the target block (session is loaded `:3568` so `session.Id == sessionId`). `_logger` + `CertNumberHelper` already in use in this controller — no new import.

---

### PXF-09 — Excel "Detail Jawaban" essay cell (controller, transform/file-I/O) [LOW]

**Target:** `Controllers/AssessmentAdminController.cs:4828-4838` — the `if (tipe == "Essay")` branch in the **"Detail Jawaban"** sheet loop (header `:4807`). Columns: 1=No, 2=Soal, 3=Tipe, 4=Jawaban Peserta, 5=Jawaban Benar, 6=Status.

**CONFIRMED DISTINCT SURFACE (D-06):** this is "Detail Jawaban" in `AssessmentAdminController.cs`, NOT "Detail Per Soal" (`ExcelExportHelper.cs:83` `AddDetailPerSoalSheet`, Phase 386). Different file + method. If you reach an `ExcelExportHelper` call, STOP.

**Current code (VERIFIED `:4828-4838`):** hard-codes cols 4/5/6 to grading placeholder / `"—"` / `"—"`.

**Analog — data already in scope (VERIFIED):** `sessionResp` (`:4805` = `allResponses.Where(r => r.AssessmentSessionId == session.Id).ToList()`). Each `PackageUserResponse` has `TextAnswer` + `EssayScore` (int?) — confirmed used at `:3492-3493`.

**Copy-ready excerpt (replace the three "—" assignments):**
```csharp
var essayResp = sessionResp.FirstOrDefault(r => r.PackageQuestionId == q.Id);
ws.Cell(currentRow, 4).Value = string.IsNullOrWhiteSpace(essayResp?.TextAnswer) ? "Tidak dijawab" : essayResp.TextAnswer;
ws.Cell(currentRow, 5).Value = "—"; // essay: no deterministic "jawaban benar"
ws.Cell(currentRow, 6).Value = essayResp?.EssayScore.HasValue == true
    ? $"Skor: {essayResp.EssayScore}/{q.ScoreValue}"
    : "Belum dinilai";
```

**Guards:** essay has no `PackageOptionId` → the `:4840` filter (`r.PackageOptionId.HasValue`) is irrelevant; use raw `sessionResp`. Wording = discretion; load-bearing = show TextAnswer (col 4) + EssayScore (col 6).

---

### PXF-10 — `FinalizeEssayGrading` broadcast monitor (controller → SignalR, event-driven) [LOW]

**Target:** `Controllers/AssessmentAdminController.cs:3753` — add broadcast after `NotifyIfGroupCompleted` (`:3751`), before final `return Json`.

**Analog — group + batchKey pattern (VERIFIED `:3204-3211`, same file):**
```csharp
var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("workerAnswerEdited", new { ... });
```
Group `monitor-{batchKey}` is the one joined in `AssessmentHub.cs:57`.

**Analog — closest event `workerSubmitted` (VERIFIED `CMPController.cs:1786`):** fired when a session completes/grades; monitor JS already handles it for the "Completed" transition.

**RESOLVED gotcha (Open Question 1 / A3):** `AssessmentSession.Schedule` is a **scalar `DateTime`** (`Models/AssessmentSession.cs:18` → `public DateTime Schedule { get; set; }`), NOT a navigation property. So `session.Schedule.Date` works with the `FindAsync`-loaded `session` (`:3568`) — no Include needed for Schedule/Title/Category (all scalar). ONLY `session.User?.FullName` is a navigation → either Include it, or omit/fallback to `"Unknown"`.

**Copy-ready excerpt (after `:3751`):**
```csharp
var fbatchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{fbatchKey}").SendAsync("workerSubmitted", new {
    sessionId,
    workerName = session.User?.FullName ?? "Unknown", // User is nav; null-safe via FindAsync
    score = finalPercentage,
    result = isPassed ? "Pass" : "Fail",
    status = AssessmentConstants.AssessmentStatus.Completed,
    nomorSertifikat = updatedSession?.NomorSertifikat
});
```

**Guards:** `_hubContext` field already exists (`:27`). Fire-and-forget — must not break the primary flow. If `workerName` matters for monitor display, reload session with `.Include(s => s.User)`; otherwise `"Unknown"` fallback is acceptable (LOW, ≈1 operator).

---

### PXF-11 — Aria huruf A/B/C/D opsi gambar (view/Razor, render a11y) [LOW]

**Targets (TWO surfaces):**
- `Views/CMP/Results.cshtml:356` (`foreach (var option in question.Options)`) + `:388` (`AriaContext = "opsi"`).
- `Views/CMP/ExamSummary.cshtml:57` (`foreach (var optImg in item.OptionImages)`) + `:59` (`AriaContext = "opsi"`).

Both loops are plain `foreach` with NO index var (VERIFIED).

**Analog — TIRU `Views/CMP/StartExam.cshtml:125/134/148` (VERIFIED):**
```cshtml
string[] letters = { "A", "B", "C", "D" };
...
@for (int oi = 0; oi < qOptions.Count; oi++)
{
    var opt = qOptions[oi];
    var letter = oi < letters.Length ? letters[oi] : (oi + 1).ToString();
    ...
    @await Html.PartialAsync("_QuestionImage", new { ..., AriaContext = "opsi " + letter })
}
```
Also used in `_PreviewQuestion.cshtml:67`.

**Fix shape (add index to each loop, then `AriaContext = "opsi " + letter`):**
- Convert `foreach` → indexed `for` (if collection is `List`/`IList`), OR
- `foreach (var (option, oi) in question.Options.Select((o, i) => (o, i)))` (works for any `IEnumerable`).

**Guards:** `_QuestionImage` reads `AriaContext` via reflection (safe if absent — but it's already sent here, so just change the string). Renders NOTHING when `ImagePath` is null → letter only appears on options that have an image. Confirm `question.Options` / `item.OptionImages` indexability before using `[oi]`; if not `IList`, use the `Select((o,i))` form. **D-09: PXF-11 REQUIRES Playwright** (a11y render needs runtime Razor — lesson Phase 354; grep+build insufficient). Verify `aria-label` contains the letter at runtime.

---

### PXF-12 — `SubmitExam` MC no null-overwrite (controller, CRUD write guard) [LOW]

**Target:** `Controllers/CMPController.cs:1712-1716` — the MC upsert `if (existingResponses.TryGetValue(...))` branch inside `SubmitExam`.

**Current code (VERIFIED `:1703-1726`):** `selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : null;` then unconditionally `existingResponse.PackageOptionId = selectedOptId;` — nulls a saved answer when the question is absent from the form.

**Self-analog:** the `answers.ContainsKey(q.Id)` check already exists at `:1703` — reuse it as the update guard.

**Copy-ready excerpt:**
```csharp
if (existingResponses.TryGetValue(q.Id, out var existingResponse))
{
    if (answers.ContainsKey(q.Id)) // guard: don't null-overwrite absent question
    {
        existingResponse.PackageOptionId = selectedOptId;
        existingResponse.SubmittedAt = DateTime.UtcNow;
    }
}
else if (selectedOptId.HasValue) { /* unchanged add */ }
```

**Guards:** fix ONLY the `MultipleChoice` branch — MA (`:1728`) skips this upsert; Essay is manual. No side-effect on MA/Essay. `answers = Dictionary<int,int>` (key=questionId). Unit-testable (D-09): absent MC must not nullify a saved answer.

---

### PXF-13 — `Hub.SaveTextAnswer` guard timer-expired (SignalR hub, event-driven write guard) [LOW]

**Target:** `Hubs/AssessmentHub.cs:134` — insert guard after the `if (session == null) return;` block (`:149`), before the truncate/upsert.

**Analog — TIRU VERBATIM sibling `SaveMultipleAnswer:205-215` (VERIFIED this session):**
```csharp
// Validasi timer belum expired (server-side check, memperhitungkan ExtraTimeMinutes)
if (session.StartedAt.HasValue && session.DurationMinutes > 0)
{
    var elapsed = (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds;
    var allowed = (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60;
    if (elapsed > allowed)
    {
        _logger.LogWarning("SaveTextAnswer: timer expired for session {SessionId}", sessionId);
        return;
    }
}
```
(Only change vs analog: log string `SaveMultipleAnswer` → `SaveTextAnswer`.)

**Guards:** `SaveTextAnswer` already loads the full `session` entity via `FirstOrDefaultAsync` (`:143`) → `StartedAt`/`DurationMinutes`/`ExtraTimeMinutes` available, same `AssessmentSession` entity. `_logger` already present (used `:147`). Do NOT refactor the literal `"InProgress"` at `:144` (out of scope — consistent with `SaveMultipleAnswer:198`).

## Shared Patterns

### Status enum constant (PXF-06, PXF-10)
**Source:** `Models/AssessmentConstants.cs:13-22` (`AssessmentConstants.AssessmentStatus.*`)
**Apply to:** any new status comparison or status payload.
```csharp
AssessmentConstants.AssessmentStatus.Completed     // "Completed" — the only terminal/finalized state
AssessmentConstants.AssessmentStatus.PendingGrading // "Menunggu Penilaian" — grading window (do NOT block)
// NO "Failed" const. Pass/fail = IsPassed bool inside Completed.
```
Use constants, never string literals. Already imported (`HcPortal.Models`) in both controllers.

### JSON failure response (PXF-06, PXF-08)
**Source:** `AssessmentAdminController.cs` `FinalizeEssayGrading` / `SubmitEssayScore` existing returns.
**Apply to:** all controller guard/error exits.
```csharp
return Json(new { success = false, message = "<pesan Bahasa Indonesia jelas>" });
```
Messages in Bahasa Indonesia, explicit to HC (never silent — that is the bug being fixed in PXF-06/08).

### SignalR monitor broadcast (PXF-10)
**Source:** `AssessmentAdminController.cs:3204-3211` + hub join `AssessmentHub.cs:57`.
**Apply to:** any monitor-group push.
```csharp
var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{batchKey}").SendAsync("<event>", new { ... });
```
`session.Schedule` is scalar `DateTime` (no Include); `session.User` is navigation (Include or null-safe). Event `workerSubmitted` for completion transitions.

### Server-authoritative write guard (PXF-12, PXF-13)
**Source:** `AssessmentHub.cs:205-215` (timer) + `CMPController.cs:1703` (ContainsKey).
**Apply to:** all participant write paths.
- Timer: `elapsed > (DurationMinutes + ExtraTimeMinutes) * 60` → `LogWarning` + `return`.
- Presence: only write keys present in the submitted form; never null-overwrite absent.

## No Analog Found

None. All 7 REQ have an exact or near-exact in-codebase analog (table above). Planner should NOT fall back to RESEARCH.md generic examples — copy the verbatim excerpts here.

## Metadata

**Analog search scope:** `Controllers/`, `Services/`, `Hubs/`, `Views/CMP/`, `Views/Shared/`, `Models/`
**Files read this session (verified vs HEAD):** `Models/AssessmentConstants.cs`, `Models/AssessmentSession.cs`, `Services/GradingService.cs:283-322`, `Hubs/AssessmentHub.cs:130-219`, `Controllers/AssessmentAdminController.cs:3195-3211/3520-3568/3740-3760/4800-4844`, `Controllers/CMPController.cs:1700-1729`, `Views/CMP/Results.cshtml:352-391`, `Views/CMP/ExamSummary.cshtml:50-64`, `Views/CMP/StartExam.cshtml:120-154`
**Open questions resolved:**
- OQ1/A3 — `Schedule` is scalar `DateTime` (no Include for PXF-10 batchKey). User.FullName = nav (null-safe/Include).
- OQ2/A5 — controller test infra still flagged for planner (only pure-helper `AssessmentScoreAggregatorTests` confirmed; D-09 allows manual+build for LOW).
**Pattern extraction date:** 2026-06-15
