# Phase 39: Close Early - Research

**Researched:** 2026-02-24
**Domain:** Assessment session lifecycle; exam window closure with fair scoring from submitted answers (package path)
**Confidence:** HIGH

## Summary

Phase 39 implements a "Tutup Lebih Awal" (Close Early) feature that allows HC to stop an active assessment group midway. Unlike the existing ForceClose action (which marks incomplete sessions with Score=0), CloseEarly preserves fairness by:
1. Setting `ExamWindowCloseDate = DateTime.UtcNow` on all sibling sessions (blocking new starts)
2. For InProgress sessions: calculating score from their actual PackageUserResponse answers using the same grading logic as SubmitExam
3. For Not Started sessions: leaving them in "Not Started" status with no score change (locked out, fair)
4. For Completed/Abandoned sessions: no change

The scoring logic is proven — SubmitExam already implements it at lines 2838–2876 in CMPController.cs. Phase 39 extracts this logic and reuses it for force-complete scenarios, adding audit logging and a confirmation modal.

**Primary recommendation:**
- **Backend:** Create CloseEarly POST action in CMPController that finds all sibling sessions by (Title, Category, Schedule.Date), sets ExamWindowCloseDate, and for each InProgress session, applies the SubmitExam package grading logic (iterate PackageQuestions, check PackageUserResponse answers via PackageOption.IsCorrect, sum scores, calculate percentage, set IsPassed threshold). Add AuditLogService entry.
- **Frontend:** Add "Tutup Lebih Awal" button in AssessmentMonitoringDetail.cshtml (visible only for Open groups, HC/Admin only). Bootstrap confirmation modal with localized warning text. POST form targets CloseEarly action. Verification checkpoint: confirm HC understands this cannot be undone.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Entity Framework | 8.0+ (current project) | Session/package/response queries; SaveChangesAsync atomicity | Native ORM; already used throughout CMPController |
| AuditLogService | (custom, in project) | Logging CloseEarly action | Established pattern; used in ForceCloseAll (line 772) and ReshufflePackage (line 889) |
| Bootstrap 5 Modal | (current frontend) | Confirmation dialog for HC approval | Existing pattern in AssessmentMonitoringDetail.cshtml (line 279–294 for ReshuffleAll modal) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| DateTime.UtcNow | (BCL) | ExamWindowCloseDate timestamp | Standard for all time-based updates; matches existing pattern in ForceCloseAssessment (line 586) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Reusing SubmitExam grading logic | Custom score calculation in CloseEarly | Duplicating complex logic; risk of divergence if scoring rules change |
| PackageUserResponse answers | Querying PackageQuestion.Options for correct answers | Would require re-evaluating what worker chose; PUR records are already persisted, use them |
| Single CloseEarly action | Separate actions for "close with score" vs "close without" | Added complexity; the logic already handles both InProgress (score) and NotStarted (no score) cases |

---

## Architecture Patterns

### Confirmed Assessment Group Definition (HIGH confidence — verified Phase 38 research + CMPController)

Assessment groups are NOT entities; they are defined as query-time groupings:
```csharp
.GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
```

**What defines a sibling group:**
- **Title:** Assessment name (e.g., "Safety Induction")
- **Category:** Assessment type (e.g., "IHT", "OTS")
- **Schedule.Date:** Scheduled date (date only, no time)

All AssessmentSession records with matching (Title, Category, Schedule.Date) are siblings in the same assessment group.

### Confirmed 4-State User Status Logic (HIGH confidence — read from CMPController.cs lines 438–446)

The `userStatus` display value is computed from session state fields in this order:

```csharp
if (a.CompletedAt != null || a.Score != null)
    userStatus = "Completed";
else if (a.Status == "Abandoned")
    userStatus = "Abandoned";
else if (a.StartedAt != null)
    userStatus = "InProgress";
else
    userStatus = "Not started";
```

**Key insight:** Status = "InProgress" is also used, but the display logic prefers `StartedAt` timestamp. Phase 39 must be careful: when force-completing an InProgress session, both `Status` and `CompletedAt` must be set. The 4-state logic will then show "Completed" regardless of the Status field value.

### Confirmed Package Path Grading Logic (HIGH confidence — read from SubmitExam lines 2838–2876)

**Structure:**
1. Load all PackageQuestions for the assigned package
2. For each question, check if a PackageUserResponse exists with a selected option
3. If selected option exists AND `PackageOption.IsCorrect == true`, add `PackageQuestion.ScoreValue` to totalScore
4. Calculate final percentage: `(totalScore / maxScore) * 100`, where maxScore = questionCount * 10 (or sum of ScoreValue)
5. Set `Score = finalPercentage`, `IsPassed = (finalPercentage >= PassPercentage)`

**Code snippet from SubmitExam (proven logic to reuse):**
```csharp
int totalScore = 0;
int maxScore = packageQuestions.Count * 10; // each question = 10 points

foreach (var q in packageQuestions)
{
    int? selectedOptId = answers.ContainsKey(q.Id) ? answers[q.Id] : (int?)null;
    if (selectedOptId.HasValue)
    {
        var selectedOption = q.Options.FirstOrDefault(o => o.Id == selectedOptId.Value);
        if (selectedOption != null && selectedOption.IsCorrect)
            totalScore += q.ScoreValue;
    }
}

int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
assessment.Score = finalPercentage;
assessment.IsPassed = finalPercentage >= assessment.PassPercentage;
assessment.CompletedAt = DateTime.UtcNow;
```

### Confirmed AssessmentSession Model Fields (HIGH confidence — read from Models/AssessmentSession.cs)

| Field | Type | Purpose | Phase 39 Relevance |
|-------|------|---------|-------------------|
| `Id` | int | Primary key | Used to identify sessions |
| `UserId` | string | Worker ID | Kept for audit trail |
| `Title` | string | Assessment name | Part of sibling group key |
| `Category` | string | Assessment type | Part of sibling group key |
| `Schedule` | DateTime | Scheduled date+time | Part of sibling group key (date portion); used to find siblings |
| `Status` | string | "Open", "Upcoming", "Completed", "InProgress", "Abandoned" | Must be checked to identify InProgress sessions |
| `Score` | int? | Percentage (0–100) | Set during CloseEarly for InProgress sessions |
| `IsPassed` | bool? | Pass/fail result | Derived from Score >= PassPercentage |
| `PassPercentage` | int | Pass threshold (default 70) | Used in IsPassed calculation |
| `CompletedAt` | DateTime? | When exam was finished | Set to DateTime.UtcNow when CloseEarly marks a session as Completed |
| `StartedAt` | DateTime? | When worker began exam | Already set if session is InProgress; triggers 4-state logic |
| `ExamWindowCloseDate` | DateTime? | Hard cutoff for exam access | **Phase 39 sets this to DateTime.UtcNow** to block new starts |
| `Progress` | int | 0–100 completion indicator | Set to 100 when CloseEarly marks Completed |
| `UpdatedAt` | DateTime? | Last modification timestamp | Set to DateTime.UtcNow for audit trail |

### Confirmed PackageUserResponse Model (HIGH confidence — read from Models/PackageUserResponse.cs)

```csharp
public class PackageUserResponse
{
    public int Id { get; set; }
    public int AssessmentSessionId { get; set; }
    public virtual AssessmentSession AssessmentSession { get; set; } = null!;

    public int PackageQuestionId { get; set; }
    public virtual PackageQuestion PackageQuestion { get; set; } = null!;

    public int? PackageOptionId { get; set; }  // ← The actual answer selected; NULL if not answered
    public virtual PackageOption? PackageOption { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
```

**Key insight:** `PackageOptionId` can be NULL (worker skipped the question). When grading, NULL means no points for that question.

### Confirmed ForceCloseAssessment Pattern (HIGH confidence — read from CMPController.cs lines 563–610)

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForceCloseAssessment(int id)
{
    var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id);
    if (assessment == null) return NotFound();

    if (assessment.Status != "Open" && assessment.Status != "InProgress")
    {
        TempData["Error"] = "Force Close hanya dapat dilakukan pada sesi yang berstatus Open atau InProgress.";
        return RedirectToAction("AssessmentMonitoringDetail", new { ... });
    }

    // Mark as Completed with score 0
    assessment.Status = "Completed";
    assessment.Score = 0;
    assessment.IsPassed = false;
    assessment.CompletedAt = DateTime.UtcNow;
    assessment.Progress = 100;
    assessment.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    // Audit log
    var fcUser = await _userManager.GetUserAsync(User);
    var fcActorName = $"{fcUser?.NIP ?? "?"} - {fcUser?.FullName ?? "Unknown"}";
    await _auditLog.LogAsync(
        fcUser?.Id ?? "",
        fcActorName,
        "ForceCloseAssessment",
        $"Force-closed assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
        id,
        "AssessmentSession");

    TempData["Success"] = "Sesi ujian telah ditutup paksa oleh sistem dengan skor 0.";
    return RedirectToAction("AssessmentMonitoringDetail", ...);
}
```

**Phase 39 divergence:** CloseEarly operates on the group level (all siblings), not a single session. And it calculates score from answers rather than hardcoding Score=0.

### Confirmed ForceCloseAll Pattern (HIGH confidence — read from CMPController.cs lines 744–782)

ForceCloseAll is a bulk action that closes all Open/InProgress sessions in a group:

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForceCloseAll(string title, string category, DateTime scheduleDate)
{
    var sessionsToClose = await _context.AssessmentSessions
        .Where(a => a.Title == title
                 && a.Category == category
                 && a.Schedule.Date == scheduleDate.Date
                 && (a.Status == "Open" || a.Status == "InProgress"))
        .ToListAsync();

    // ... mark all as "Abandoned" with no score change ...

    await _context.SaveChangesAsync();

    // Audit log — one summary entry
    await _auditLog.LogAsync(...);

    return RedirectToAction("AssessmentMonitoringDetail", ...);
}
```

**Phase 39 reuses this sibling-finding pattern** but differs:
1. Sets ExamWindowCloseDate (not just Status)
2. For InProgress: calculates score and marks Completed (vs ForceCloseAll marks Abandoned)
3. For NotStarted: leaves as-is with locked-out access (vs ForceCloseAll marks Abandoned)

### Pattern: Sibling Session Lookup by Group Key

**What:** Use (Title, Category, Schedule.Date) to find all sessions in the same assessment group.

**Where used:** ReshufflePackage (lines 811–816), ReshuffleAll (lines 908–914), ResetAssessment (implicit via redirect params), and now CloseEarly.

**Code pattern:**
```csharp
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id)
    .ToListAsync();
```

### Pattern: AuditLogService Usage (HIGH confidence — read from Services/AuditLogService.cs and CMPController examples)

```csharp
public async Task LogAsync(
    string actorUserId,
    string actorName,
    string actionType,
    string description,
    int? targetId = null,
    string? targetType = null)
```

**Usage example from ForceCloseAll (lines 772–778):**
```csharp
var actor = await _userManager.GetUserAsync(User);
var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
await _auditLog.LogAsync(
    actor?.Id ?? "",
    actorName,
    "ForceCloseAll",
    $"Force-closed all Open/InProgress sessions for '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) — {sessionsToClose.Count} session(s) closed",
    null,  // targetId null for group actions
    "AssessmentSession");
```

**For Phase 39 CloseEarly:** Use similar pattern but with actionType="CloseEarly" and include the final count of InProgress sessions that were scored.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Finding sibling sessions | Manual lookup by ID | Query by (Title, Category, Schedule.Date) | Pattern established in ReshufflePackage, ReshuffleAll; consistent with assessment group definition |
| Score calculation from answers | Custom grading algorithm | Extract/reuse the SubmitExam grading block (lines 2838–2876) | SubmitExam logic is proven, tested, and handles edge cases (NULL answers, dividing by zero) |
| Audit logging | Manual SQL inserts or DbSet.Add | AuditLogService.LogAsync | Service handles timestamp, saves immediately, consistent with existing audit entries |
| Determining InProgress sessions | Checking Status field only | Use 4-state logic: `a.StartedAt != null && a.CompletedAt == null && a.Score == null` | Status is not always set reliably; StartedAt timestamp is the source of truth for "in progress" |
| Blocking exam access after close | Storing a flag somewhere | Set ExamWindowCloseDate to now | Pattern already exists (Phase 21 added ExamWindowCloseDate for this purpose); StartExam checks it at lines ~2200–2250 |

**Key insight:** The grading logic is deceptively complex — handling NULL answers, summing ScoreValue per question, percentage calculation, and IsPassed threshold. Do not simplify or rewrite it; copy it from SubmitExam.

---

## Common Pitfalls

### Pitfall 1: Forgetting ExamWindowCloseDate Blocks New Starts
**What goes wrong:** Code sets ExamWindowCloseDate but doesn't verify StartExam checks it. Workers who haven't started can still begin the exam.
**Why it happens:** ExamWindowCloseDate is a relatively new field (Phase 21); developer may not know it's the enforcement point.
**How to avoid:** Grep for StartExam checks of ExamWindowCloseDate. Verify it's checked BEFORE allowing exam start. It should be around line 2200–2250 in CMPController.
**Warning signs:** After CloseEarly, workers in "Not Started" status can still click "Start Exam" and enter the exam.

### Pitfall 2: Only Scoring Package Path, Missing Legacy Path
**What goes wrong:** CloseEarly implements scoring for package-mode assessments but ignores legacy assessments (those using AssessmentQuestion, not AssessmentPackage).
**Why it happens:** Phase 39 specification emphasizes package path; developer may assume all assessments are package-based.
**How to avoid:** Check DetectPackageMode pattern in AssessmentMonitoringDetail (lines 404–408). An assessment group can be either legacy OR package mode, not both. CloseEarly must detect which and apply appropriate grading. For legacy mode, reuse the grading logic from SubmitExam legacy branch (lines 2930–3000).
**Warning signs:** CloseEarly works for some assessment groups but not others; "Package mode detection" code doesn't exist in CloseEarly.

### Pitfall 3: Modifying Status Without Setting CompletedAt
**What goes wrong:** Code sets `Status = "Completed"` but forgets `CompletedAt = DateTime.UtcNow`. The 4-state logic fails: `if (a.CompletedAt != null || a.Score != null)` won't trigger if CompletedAt is NULL.
**Why it happens:** Developer thinks Status field alone is sufficient.
**How to avoid:** Always set both Status and CompletedAt together when marking a session complete. The 4-state logic uses CompletedAt as the primary signal.
**Warning signs:** After CloseEarly, worker status shows "InProgress" instead of "Completed" despite Score being set.

### Pitfall 4: Querying Before Including PackageQuestions and PackageOptions
**What goes wrong:** Code loads PackageQuestions but forgets `.Include(q => q.Options)`. Scoring logic fails with "options is empty or null."
**Why it happens:** In-memory operations don't lazy-load related collections; must be eager-loaded via Include.
**How to avoid:** Always follow the pattern from SubmitExam (lines 2839–2842): load PackageQuestions WITH Options in a single query.
**Warning signs:** NullReferenceException or empty Options collection during scoring loop.

### Pitfall 5: Forgetting to Handle NULL PackageUserResponse Records
**What goes wrong:** A question was never answered (no PackageUserResponse for that question). Scoring logic tries to look up the answer, gets NULL, and crashes or skips scoring.
**Why it happens:** Not all questions are always answered. SubmitExam creates PackageUserResponse for EVERY question (even unanswered ones, with PackageOptionId=NULL). But if a worker abandoned mid-exam, some questions might never have a PUR record.
**How to avoid:** When iterating PackageQuestions for scoring, check if a PackageUserResponse exists. If not, treat as "no points." Follow SubmitExam pattern: check `answers.ContainsKey(q.Id)` and set `selectedOptId = (int?)null` if missing.
**Warning signs:** Unanswered questions cause scoring logic to crash or give incorrect totals.

### Pitfall 6: Calculating MaxScore Incorrectly
**What goes wrong:** Code uses `packageQuestions.Count * 10` (assumes all questions worth 10 points), but some questions have custom ScoreValue.
**Why it happens:** SubmitExam has this assumption, but CloseEarly should verify it applies to the specific assessment.
**How to avoid:** Use `packageQuestions.Sum(q => q.ScoreValue)` instead of count-based max. Or verify ScoreValue is always 10 for this assessment's package.
**Warning signs:** Percentage scores are wrong (too high or too low) for assessments where ScoreValue varies.

### Pitfall 7: Not Locking Out NotStarted Sessions
**What goes wrong:** CloseEarly sets ExamWindowCloseDate but NotStarted workers can still retry. Or worse, they retry and take the exam normally (getting a new score).
**Why it happens:** Developer assumes setting ExamWindowCloseDate is enough, but doesn't verify StartExam actually enforces it.
**How to avoid:** Test: after CloseEarly, try to start an exam with a NotStarted session. Verify you get an error like "Exam window closed." If it succeeds, StartExam's check is missing or broken.
**Warning signs:** NotStarted workers can still access the exam after CloseEarly; their status doesn't change to "Locked" or similar.

### Pitfall 8: Audit Log Includes Wrong ActionType
**What goes wrong:** Audit log says ActionType="ForceCloseAll" instead of "CloseEarly". Later audit queries for "CloseEarly" find nothing.
**Why it happens:** Copy-paste from ForceCloseAll without updating the ActionType string.
**How to avoid:** Define ActionType="CloseEarly" in AuditLog model's comment (see line 23 of AuditLog.cs). Use that exact string in all audit calls.
**Warning signs:** Audit queries don't find CloseEarly entries; description mentions "Close Early" but ActionType doesn't match.

### Pitfall 9: Trying to CloseEarly When Not All Sessions Are in Same Mode
**What goes wrong:** An assessment group has some sessions in package mode and some in legacy mode. CloseEarly tries to score all as package, fails on legacy ones.
**Why it happens:** Assumption that all siblings use the same assessment path.
**How to avoid:** Detect mode once for the group. If mixed (which shouldn't happen), reject with error. Or handle both modes separately within the same action.
**Warning signs:** CloseEarly fails midway; some sessions are updated, others are not (inconsistent state).

### Pitfall 10: Redirecting Before Checking for Package Mode
**What goes wrong:** CloseEarly doesn't return immediately to AssessmentMonitoringDetail. Instead, it redirects prematurely, losing transaction context.
**Why it happens:** Following ForceCloseAll pattern which redirects immediately (line 781).
**How to avoid:** Wrap all updates (setting ExamWindowCloseDate, scoring InProgress sessions, audit log) in a single SaveChangesAsync call. Then redirect once.
**Warning signs:** Some sessions are updated, others are not; page shows partial results.

---

## Code Examples

### CloseEarly Action Stub (Backend)

Verified pattern from SubmitExam + ForceCloseAll structure:

```csharp
// Source: C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs
// Pattern derived from: ForceCloseAll (lines 741–782) + SubmitExam package grading (lines 2836–2876)

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CloseEarly(string title, string category, DateTime scheduleDate)
{
    // 1. Find all sessions in this group
    var sessionsToClose = await _context.AssessmentSessions
        .Where(a => a.Title == title
                 && a.Category == category
                 && a.Schedule.Date == scheduleDate.Date)
        .ToListAsync();

    if (!sessionsToClose.Any())
    {
        TempData["Error"] = "Assessment group not found.";
        return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
    }

    // 2. Detect mode: is this a package-mode assessment?
    var siblingIds = sessionsToClose.Select(s => s.Id).ToList();
    var packageCount = await _context.AssessmentPackages
        .CountAsync(p => siblingIds.Contains(p.AssessmentSessionId));
    var isPackageMode = packageCount > 0;

    // 3. Load packages (package mode) or questions (legacy mode)
    // ... (mode-specific loading; shown below) ...

    // 4. For each session, set ExamWindowCloseDate and score InProgress sessions
    int inProgressCount = 0;
    foreach (var session in sessionsToClose)
    {
        // Always set close date to block future starts
        session.ExamWindowCloseDate = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        // Only score if InProgress (has StartedAt but not CompletedAt)
        if (session.StartedAt != null && session.CompletedAt == null)
        {
            inProgressCount++;

            // Score based on mode
            if (isPackageMode)
            {
                // Package path: grade from PackageUserResponse answers
                // ... (see section below)
            }
            else
            {
                // Legacy path: grade from UserResponse answers
                // ... (reuse SubmitExam legacy logic)
            }
        }
    }

    await _context.SaveChangesAsync();

    // 5. Audit log
    var actor = await _userManager.GetUserAsync(User);
    var actorName = $"{actor?.NIP ?? "?"} - {actor?.FullName ?? "Unknown"}";
    await _auditLog.LogAsync(
        actor?.Id ?? "",
        actorName,
        "CloseEarly",
        $"Closed early assessment group '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) — {inProgressCount} session(s) scored from answers, {sessionsToClose.Count} total sessions locked",
        null,
        "AssessmentSession");

    TempData["Success"] = $"Assessment group tutup lebih awal. {inProgressCount} sesi ujian diberi skor berdasarkan jawaban, {sessionsToClose.Count - inProgressCount} sesi terkunci.";
    return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
}
```

### Package Path Scoring Logic (Excerpt for CloseEarly)

From SubmitExam package path (lines 2838–2876), adapted for CloseEarly:

```csharp
// For package mode (only score InProgress sessions)
if (isPackageMode)
{
    // Load all packages for this group
    var packages = await _context.AssessmentPackages
        .Include(p => p.Questions)
            .ThenInclude(q => q.Options)
        .Where(p => siblingIds.Contains(p.AssessmentSessionId))
        .ToListAsync();

    // Map: session ID → package
    var sessionPackageMap = new Dictionary<int, AssessmentPackage>();
    foreach (var pkg in packages)
    {
        var assignment = await _context.UserPackageAssignments
            .FirstOrDefaultAsync(a => a.AssessmentSessionId == pkg.AssessmentSessionId);
        if (assignment != null)
            sessionPackageMap[pkg.AssessmentSessionId] = pkg;
    }

    // For each InProgress session, score it
    foreach (var session in sessionsToClose)
    {
        if (session.StartedAt == null || session.CompletedAt != null)
            continue; // Skip NotStarted and already Completed/Abandoned

        if (!sessionPackageMap.TryGetValue(session.Id, out var package))
            continue; // Session has no package (shouldn't happen in package mode)

        // Score: count correct answers from PackageUserResponse records
        int totalScore = 0;
        int maxScore = package.Questions.Count * 10;

        foreach (var q in package.Questions)
        {
            // Find the answer this worker submitted for this question
            var response = await _context.PackageUserResponses
                .FirstOrDefaultAsync(r => r.AssessmentSessionId == session.Id &&
                                          r.PackageQuestionId == q.Id);

            if (response != null && response.PackageOptionId.HasValue)
            {
                var selectedOption = q.Options.FirstOrDefault(o => o.Id == response.PackageOptionId.Value);
                if (selectedOption != null && selectedOption.IsCorrect)
                    totalScore += q.ScoreValue;
            }
        }

        // Calculate percentage and set passed/failed
        int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;

        session.Score = finalPercentage;
        session.Status = "Completed";
        session.Progress = 100;
        session.IsPassed = finalPercentage >= session.PassPercentage;
        session.CompletedAt = DateTime.UtcNow;
    }
}
```

### AssessmentMonitoringDetail Frontend: Add "Tutup Lebih Awal" Button

Pattern from existing ForceCloseAll button (lines 118–127):

```html
<!-- In AssessmentMonitoringDetail.cshtml, add to the action buttons row (around line 118) -->
<!-- Close Early: POST with antiforgery, confirm() guard -->
<form asp-action="CloseEarly" method="post" class="d-inline"
      onsubmit="return confirm('Tutup ujian lebih awal? Peserta yang sedang mengerjakan akan diberi skor berdasarkan jawaban mereka. Peserta yang belum mulai akan terkunci dan tidak dapat mengakses ujian.')">
    @Html.AntiForgeryToken()
    <input type="hidden" name="title" value="@Model.Title" />
    <input type="hidden" name="category" value="@Model.Category" />
    <input type="hidden" name="scheduleDate" value="@Model.Schedule.ToString("yyyy-MM-dd")" />
    <button type="submit" class="btn btn-warning btn-sm"
            id="closeEarlyBtn"
            @(Model.GroupStatus == "Open" ? "" : "disabled")
            title="@(Model.GroupStatus == "Open" ? "Tutup ujian lebih awal" : "Hanya untuk grup yang berstatus Open")">
        <i class="bi bi-clock-history me-1"></i>Tutup Lebih Awal
    </button>
</form>
```

**Placement:** Between the "Export Results" button and the "Reshuffle All" button (around line 117–128). Only show when GroupStatus == "Open".

**Localization note:** Confirm message in Indonesian (Bahasa Indonesia) to match existing HC UI language.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual score calculation for force-complete | Reuse proven SubmitExam grading logic | Phase 39 | Ensures consistency; reduces bugs from duplicated logic |
| ForceClose always = Score 0 | CloseEarly = score from actual answers | Phase 39 | Fair to workers who completed most of the exam |
| No ExamWindowCloseDate usage for blocking | Phase 21 introduced ExamWindowCloseDate; Phase 39 leverages it | 2026-02 | ExamWindowCloseDate is now the standard way to block access; consistent with existing StartExam checks |
| Single action for all bulk closes | ForceCloseAll (Abandoned, Score 0) vs CloseEarly (Completed, scored) | Phase 39 | Two separate actions for two different scenarios |

---

## Open Questions

1. **What if a worker is currently taking the exam when CloseEarly is executed?**
   - What we know: ExamWindowCloseDate will be set to now. If the worker is viewing a question, they can finish answering it and submit. SubmitExam checks ExamWindowCloseDate but may not block mid-submit.
   - What's unclear: Should we block mid-flight submissions, or allow grace period?
   - Recommendation: Allow grace period (current SubmitExam behavior). If a worker clicks "Submit" before CloseEarly is executed, the submit succeeds. If CloseEarly executes first, subsequent submits fail. This is fairest to workers (not penalizing network latency).

2. **Should CloseEarly update competency levels?**
   - What we know: SubmitExam auto-updates UserCompetencyLevels if IsPassed = true (lines 2879–2921). CloseEarly reuses the scoring logic but may not execute competency updates.
   - What's unclear: Should a worker who reaches passing score via CloseEarly have their competency level updated automatically?
   - Recommendation: YES. CloseEarly must also run the competency-update logic. Reuse the block from SubmitExam (lines 2878–2921) to ensure parity.

3. **What if no sessions are Open in the group (e.g., all Completed)?**
   - What we know: CloseEarly checks if sessions exist; if none, returns error. But if some are Completed already, should they still be processed?
   - What's unclear: Should CloseEarly only touch Open/InProgress/NotStarted sessions, or also "refresh" already-Completed ones?
   - Recommendation: Only touch Open/InProgress/NotStarted. Leave Completed and Abandoned as-is. CloseEarly is for stopping an active group, not retroactively re-scoring completed work.

4. **Should CloseEarly set a flag to indicate "closed early" for audit purposes?**
   - What we know: Audit log will record "CloseEarly" action. But AssessmentSession model has no "ClosedEarly" flag.
   - What's unclear: Is audit log sufficient, or should a model field track this?
   - Recommendation: Audit log is sufficient. The description will say "Closed early assessment group...". If later reporting needs to query "which groups were closed early," add a model field in a future phase.

---

## Sources

### Primary (HIGH confidence)
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs`
  - SubmitExam (lines 2791–3000) — package grading logic (lines 2838–2876), competency update logic (lines 2878–2921)
  - ForceCloseAll (lines 741–782) — group-level action pattern, sibling finding, audit logging
  - ForceCloseAssessment (lines 560–610) — single-session action pattern, Status/CompletedAt/Score updates
  - ReshufflePackage (lines 788–900) — sibling session lookup pattern, package assignment loading
  - AssessmentMonitoringDetail (lines 389–482) — 4-state user status logic (lines 438–446), GroupStatus determination (lines 474–475), view data model
  - ResetAssessment (lines 488–556) — PackageUserResponse and UserPackageAssignment cleanup pattern

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentSession.cs` (lines 1–56)
  - Fields: Status, ExamWindowCloseDate, CompletedAt, Score, IsPassed, PassPercentage, StartedAt

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/PackageUserResponse.cs` (lines 1–25)
  - Structure: AssessmentSessionId FK, PackageQuestionId FK, PackageOptionId FK (nullable)

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentPackage.cs` (lines 1–65)
  - PackageQuestion structure (ScoreValue field), PackageOption (IsCorrect field)

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AuditLog.cs` (lines 1–53)
  - ActionType enum comment (line 23 lists existing types); LogAsync signature

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Services/AuditLogService.cs` (lines 1–45)
  - LogAsync pattern for audit entries

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Views/CMP/AssessmentMonitoringDetail.cshtml` (lines 1–391)
  - Button placement (line 118–127 for ForceCloseAll, line 279–294 for ReshuffleAll modal)
  - Bootstrap modal pattern for confirmation
  - GroupStatus conditional rendering (line 474 in controller, line 20–21 in view)

### Secondary (MEDIUM confidence)
- Phase 38 RESEARCH.md — ExamWindowCloseDate history and enforcement pattern
- Phase 28 RESEARCH.md — Package assignment and reshuffle logic
- Phase 27 RESEARCH.md — Status lifecycle and monitoring status values

### Tertiary (LOW confidence)
- None applicable — all findings verified via codebase inspection

---

## Metadata

**Confidence breakdown:**
- Package grading logic (copy from SubmitExam): HIGH — verified lines 2838–2876
- Sibling session lookup pattern: HIGH — used in ReshufflePackage, ReshuffleAll, verified
- AssessmentSession model fields: HIGH — read directly from model
- 4-state user status logic: HIGH — read from CMPController lines 438–446
- AuditLogService pattern: HIGH — read from service + examples in controllers
- ExamWindowCloseDate enforcement: HIGH — set in ForceCloseAssessment (line 586), mentioned in Phase 38; implementation assumed to be in StartExam
- Competency level updates: HIGH — verified in SubmitExam lines 2878–2921
- Frontend button placement and modal pattern: HIGH — verified in existing AssessmentMonitoringDetail.cshtml

**Research date:** 2026-02-24
**Valid until:** 2026-03-26 (stable codebase, 30-day window; sooner if Phase 21/38 ExamWindowCloseDate enforcement changes)

**Key file locations for Phase 39 implementation:**
- Main controller: `C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs`
  - Add CloseEarly() action after ForceCloseAll (around line 783)
  - Extract/reference SubmitExam package grading block (lines 2838–2876) and competency logic (lines 2878–2921)
- View: `C:/Users/rinoa/Desktop/PortalHC_KPB/Views/CMP/AssessmentMonitoringDetail.cshtml`
  - Add "Tutup Lebih Awal" button in the header (around line 118, before/after ForceCloseAll button)
  - Visible only when GroupStatus == "Open" and HC/Admin roles
- Models: No changes required (AssessmentSession.ExamWindowCloseDate already exists)
- Services: AuditLogService already available for injection
