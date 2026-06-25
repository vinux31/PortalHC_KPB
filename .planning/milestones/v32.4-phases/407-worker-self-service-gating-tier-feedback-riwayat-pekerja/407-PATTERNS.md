# Phase 407: Worker Self-Service + Gating Tier Feedback + Riwayat Pekerja - Pattern Map

**Mapped:** 2026-06-22
**Files analyzed:** 8 (3 new, 5 modified)
**Analogs found:** 8 / 8 (all exact or strong role-match — fully brownfield reuse phase)

> Catatan: Fase 407 adalah **pure wiring** di atas mesin retake 405 + pola UI 406. Hampir SEMUA pola sudah ada di repo dan terbukti. Tabel di bawah memetakan setiap file baru/dimodifikasi ke analog terdekat dengan excerpt siap-salin (file:line dari RESEARCH §Sources sudah diverifikasi langsung terhadap kode).

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/CMPController.cs` — action `RetakeExam` (NEW POST) | controller | request-response (PRG mutation) | `Controllers/AssessmentAdminController.cs:4244-4327` `ResetAssessment` | exact |
| `Controllers/CMPController.cs` — `Results` action extend (build VM flags + riwayat) | controller | CRUD read-path | `Controllers/AssessmentAdminController.cs:3485-3524` `RiwayatPercobaan` + `Services/RetakeService.cs:232-248` `CanRetakeAsync` | exact |
| `Controllers/CMPController.cs` — DI inject `RetakeService` | controller (constructor) | dependency-injection | `Controllers/AssessmentAdminController.cs` `_retakeService` field/ctor | exact |
| `Helpers/RetakeRules.cs` — `ResolveReviewMode` + `RetakeReviewMode` enum (NEW pure method) | utility (pure helper) | transform (deterministic) | `Helpers/RetakeRules.cs:29-52` `CanRetake` | exact (same file/idiom) |
| `Models/AssessmentResultsViewModel.cs` — add retake/tier fields | model (VM) | data-carrier | self (extend existing VM `:3-22`) | exact |
| `Models/AllWorkersHistoryRow.cs` — add `IsCurrentAttempt` | model (DTO) | data-carrier | self (extend `:7-40`) + `RiwayatAttemptViewModel.IsCurrent :26-27` | exact |
| `Views/CMP/Results.cshtml` — retake block + modal + 3-state tier + riwayat card | view (Razor) | request-response (render) | self review block `:316-418` + `Views/Admin/_RiwayatPercobaan.cshtml` + `Views/CMP/StartExam.cshtml:455-490` (countdown) | exact (multi-mirror) |
| `Views/CMP/Records.cshtml` — per-row riwayat trigger (OPTIONAL) | view (Razor) | request-response (render) | self Aksi cell `:260-283` "Lihat Hasil" | exact |
| `Views/CMP/_RiwayatPekerja.cshtml` (NEW partial, gated worker variant) | view (partial) | request-response (render) | `Views/Admin/_RiwayatPercobaan.cshtml` (full file) | role-match (gated variant) |
| `HcPortal.Tests/RetakeRulesTests.cs` — extend `ResolveReviewMode` tests | test (unit) | transform-assert | self (`:14-60` Can() helper + Fact idiom) | exact |
| `HcPortal.Tests/` — NEW `RetakeExam` endpoint test | test (integration/controller) | request-response-assert | `HcPortal.Tests/RetakeServiceTests.cs:1-60` (fixture + NoOpHubContext + NullLogger) | role-match |

---

## Pattern Assignments

### `Controllers/CMPController.cs` — `RetakeExam` action (controller, request-response/PRG)

**Analog:** `Controllers/AssessmentAdminController.cs:4244-4327` (`ResetAssessment`) — mirror baris-per-baris. Beda: actor=worker, guard=ownership+`CanRetakeAsync` (bukan IsResettable/Pre-Post HC), redirect=`StartExam`.

**Auth + antiforgery + ownership pattern** (mirror `ResetAssessment` attrs `:4245-4248` + `Results` ownership `CMPController.cs:2193-2196` + `StartExam` effective-user `:909-912`):
```csharp
[HttpPost]
[ValidateAntiForgeryToken]          // class-level [Authorize] sudah di CMPController :25
public async Task<IActionResult> RetakeExam(int id)
{
    var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id);
    if (assessment == null) return NotFound();

    var (user, _) = await GetCurrentUserRoleLevelAsync();   // effective user (impersonation-aware) — idiom :909
    if (user == null) return Challenge();
    if (assessment.UserId != user.Id) return Forbid();      // RTK-09 ownership (worker self-service only — IDOR guard)
```

**Server-authoritative re-check + service delegation** (mirror `ResetAssessment :4300-4315` ExecuteAsync + `CanRetakeAsync` from `RetakeService.cs:232`):
```csharp
    if (!await _retakeService.CanRetakeAsync(id))           // D-01 — countdown JS bukan gate; server otoritatif
    {
        TempData["Error"] = "Ujian ulang tidak bisa dijalankan saat ini. Coba muat ulang halaman atau hubungi HC.";
        return RedirectToAction("Results", new { id });
    }

    var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}"; // mirror :4298
    var rs = await _retakeService.ExecuteAsync(id, user.Id, actorName, "RetakeAssessment", "worker_retake");
    if (!rs.Success) { TempData["Error"] = rs.Error ?? "Gagal."; return RedirectToAction("Results", new { id }); }
```

**TempData token clear + redirect** (verbatim mirror `ResetAssessment :4317-4326`, must-fix #1 — `StartExam` pakai `TempData.Peek` non-consume at `CMPController.cs:944`):
```csharp
    TempData.Remove($"TokenVerified_{id}");                 // must-fix #1 — re-arm token; mirror :4319
    return RedirectToAction("StartExam", new { id });       // spec re-entry target (HC redirect ke Monitoring; worker ke StartExam)
}
```

**`ExecuteAsync`/`CanRetakeAsync` signatures (DO NOT duplicate logic — call only)** [Source `Services/RetakeService.cs:69, 232`]:
```csharp
Task<RetakeResult> ExecuteAsync(int sessionId, string actorUserId, string actorName, string actionType, string reason);
Task<bool>         CanRetakeAsync(int sessionId);   // wraps RetakeRules.CanRetake + DB-aware counting
// public readonly record struct RetakeResult(bool Success, string? Error);  (:17)
```

---

### `Controllers/CMPController.cs` — DI inject `RetakeService` (controller, dependency-injection)

**Analog:** `AssessmentAdminController` already injects `RetakeService` (field `_retakeService` + ctor). `CMPController` currently does NOT [VERIFIED: RESEARCH A4]. `RetakeService` is already `AddScoped` in `Program.cs:63` — no DI registration change needed.

**Current `CMPController` ctor** (`CMPController.cs:43-73`) — add `RetakeService retakeService` parameter + field `private readonly RetakeService _retakeService;` + assignment `_retakeService = retakeService;`. Mirror the existing field/ctor/assignment triple already used for `_gradingService` (`:40, :56, :71`) and `_impersonationService` (`:41, :57, :72`).

---

### `Controllers/CMPController.cs` — `Results` action extend (controller, CRUD read-path)

**Analog (VM flags):** `Services/RetakeService.cs:237-242` counting formula (mirror verbatim for `CurrentAttempt`).
**Analog (riwayat load):** `AssessmentAdminController.cs:3493-3522` (`RiwayatPercobaan` data-load) — reuse penuh.

**Existing VM build site** (`CMPController.cs:2347-2366`) — append the new fields after `IsPendingGrading`. The existing `AllowAnswerReview` branch at `:2243` builds `QuestionReviews`; KEEP it (tier feedback reuses `QuestionReviews` for `ShowWrongFlagsOnly`).

**Build retake/tier flags** (mirror counting `RetakeService.cs:237-242`; tier uses `assessment.IsPassed` bool? NOT `viewModel.IsPassed` bool — Pitfall 5):
```csharp
int eraRetakeArchives = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title && h.Category == assessment.Category
             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
    .CountAsync();
int currentAttempt = eraRetakeArchives + 1;
bool canRetake = await _retakeService.CanRetakeAsync(id);
bool attemptsRemaining = currentAttempt < assessment.MaxAttempts;
var reviewMode = RetakeRules.ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining);
DateTime? cooldownUntil = (assessment.AllowRetake && assessment.RetakeCooldownHours > 0 && assessment.CompletedAt.HasValue)
    ? assessment.CompletedAt.Value.AddHours(assessment.RetakeCooldownHours) : (DateTime?)null;
bool isCapReached = assessment.IsPassed == false && assessment.AllowRetake && currentAttempt >= assessment.MaxAttempts;
// → set viewModel.RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached
```
> `AssessmentSession` field types confirmed: `AllowAnswerReview` bool `:33`, `AllowRetake` bool `:46`, `MaxAttempts` int `:50`, `RetakeCooldownHours` int `:54`, `IsPassed` bool? `:56`.

**Riwayat load** (verbatim mirror `AssessmentAdminController.cs:3493-3522` — reuse `RetakeArchiveBuilder.Build(0,...)` sentinel + `RiwayatUnifier.Build`):
```csharp
var histories = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title && h.Category == assessment.Category)
    .OrderByDescending(h => h.AttemptNumber).ToListAsync();
var histIds = histories.Select(h => h.Id).ToList();
var archiveRows = await _context.AssessmentAttemptResponseArchives
    .Where(a => histIds.Contains(a.AttemptHistoryId)).ToListAsync();
var currentRows = new List<AssessmentAttemptResponseArchive>();
if (assessment.Status == "Completed") {
    var assign = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
    var qids = assign?.GetShuffledQuestionIds() ?? new List<int>();
    if (qids.Count > 0) {
        var qs = await _context.PackageQuestions.Include(q => q.Options).Where(q => qids.Contains(q.Id)).ToListAsync();
        var resp = await _context.PackageUserResponses.Where(r => r.AssessmentSessionId == id).ToListAsync();
        if (qs.Count > 0) currentRows = RetakeArchiveBuilder.Build(0, qs, resp);
    }
}
viewModel.RiwayatAttempts = RiwayatUnifier.Build(assessment, histories, archiveRows, currentRows);
```

**Authz already present** (`Results :2192-2196`) — owner||L<=3||L4-section. Ownership for retake is re-asserted in `RetakeExam` (write path). No change to read authz.

---

### `Helpers/RetakeRules.cs` — `ResolveReviewMode` + `RetakeReviewMode` enum (utility, transform)

**Analog:** `Helpers/RetakeRules.cs:29-52` (`CanRetake`) — same file, same static-pure idiom (caller supplies facts; deterministic; unit-testable in all branches). Add adjacent to `CanRetake`.

**Pure method pattern to mirror** (`CanRetake :29-52` — early-return fail-fast chain):
```csharp
public static bool CanRetake(bool allowRetake, string? assessmentType, bool isManualEntry, string status,
    bool? isPassed, int attemptsUsed, int maxAttempts, int retakeCooldownHours, DateTime? completedAt, DateTime nowUtc)
{
    if (!allowRetake) return false;
    if (assessmentType == "PreTest") return false;
    // ... fail-fast chain ...
}
```

**New helper to ADD** (RESEARCH §Architecture Pattern 3 — truth table D-03; Pitfall 5 pending→ShowFullReview):
```csharp
public enum RetakeReviewMode { ShowFullReview, ShowWrongFlagsOnly, ShowScoreOnly }

public static RetakeReviewMode ResolveReviewMode(bool allowAnswerReview, bool? isPassed, bool attemptsRemaining)
{
    if (!allowAnswerReview) return RetakeReviewMode.ShowScoreOnly;
    if (isPassed == false && attemptsRemaining) return RetakeReviewMode.ShowWrongFlagsOnly;
    return RetakeReviewMode.ShowFullReview;   // passed | exhausted | pending(null) — Pitfall 5
}
```

---

### `Models/AssessmentResultsViewModel.cs` (model, data-carrier)

**Analog:** self — extend existing VM (`:3-22`). Keep `AllowAnswerReview :12` (feeds tier; CONTEXT says do not delete).

**Existing fields to keep** (`:9-21`): `Score`, `PassPercentage`, `IsPassed` (bool — keep for cert/banner), `AllowAnswerReview`, `CompletedAt`, `QuestionReviews`, `IsPendingGrading`.

**Add** (mirror plain-property idiom of existing VM; `RetakeMode` uses enum from `RetakeRules`):
```csharp
public HcPortal.Helpers.RetakeReviewMode RetakeMode { get; set; } = HcPortal.Helpers.RetakeReviewMode.ShowFullReview;
public bool CanRetake { get; set; }
public int CurrentAttempt { get; set; }
public int MaxAttempts { get; set; }
public DateTime? CooldownUntilUtc { get; set; }
public bool IsCapReached { get; set; }
public List<RiwayatAttemptViewModel>? RiwayatAttempts { get; set; }
```

---

### `Models/AllWorkersHistoryRow.cs` (model, data-carrier)

**Analog:** self (`:7-40`) + `RiwayatAttemptViewModel.IsCurrent` (`:26-27`).

**Add** (mirror existing nullable-prop comment style `:35-39`; mirror `IsCurrent` semantics — true only for current Completed row):
```csharp
// RTK-12 (Phase 407): tandai baris percobaan aktif saat ini untuk badge "Percobaan saat ini".
// Mirror RiwayatAttemptViewModel.IsCurrent — set true hanya untuk current Completed branch.
public bool IsCurrentAttempt { get; set; }
```

---

### `Views/CMP/Results.cshtml` (view, request-response/render)

#### (a) Retake control block — mirror existing button idiom + countdown from `StartExam.cshtml`

**Existing action area** (`Results.cshtml:420-431`) — place retake control inside/adjacent to `<div class="d-flex gap-2 mb-4">`. Existing primary CTA idiom to mirror (`:424`):
```cshtml
<a asp-action="Certificate" asp-route-id="@Model.AssessmentId" class="btn btn-primary" target="_blank">
    <i class="bi bi-award me-1"></i>Lihat Sertifikat
</a>
```

**Prescriptive markup** — 407-UI-SPEC §Component 1 (`btnRetake` disabled+`data-cooldown-until` during cooldown; modal-trigger when eligible; `alert-warning` lock when `IsCapReached`). Tier/eligibility values come from VM; view does NOT compute.

**Countdown JS** — mirror exam-timer idiom `StartExam.cshtml:455-490`:
```javascript
function updateTimer() {
    var wallElapsed = Math.floor((Date.now() - timerStartWallClock) / 1000);
    var remaining = Math.max(0, timerStartRemaining - wallElapsed);
    // ... format HH:MM:SS, write el.innerText ...
    if (remaining <= 0) { clearInterval(timerInterval); /* ... */ }
}
var timerInterval = setInterval(updateTimer, 1000);
updateTimer();
```
Adapt: read `data-cooldown-until` (ISO-8601 `"o"`), compute `new Date(attr) - Date.now()`, tick `#retakeCountdown`, on `<=0` `clearInterval` + auto-enable/relabel button to "Ujian Ulang" wired to modal.

#### (b) Confirmation modal (D-02) — mirror standard Bootstrap modal idiom + `_ImageLightboxModal` placement

Place near existing `@await Html.PartialAsync("_ImageLightboxModal")` (`Results.cshtml:434`). The confirm form is the antiforgery POST to `RetakeExam`. Markup verbatim in 407-UI-SPEC §Component 2 — key: `<form method="post" asp-action="RetakeExam" asp-controller="CMP" asp-route-id="@Model.AssessmentId">` + `@Html.AntiForgeryToken()` (RTK-09) + `btn-close aria-label="Tutup"`.

#### (c) Tier-gated feedback — expand boolean `:316/:413` into 3-state switch

**Existing boolean branch** (`Results.cshtml:316`):
```cshtml
@if (Model.AllowAnswerReview && Model.QuestionReviews != null) { /* full review :318-411 */ }
else if (!Model.AllowAnswerReview) { /* :413-418 score-only notice */ }
```
Becomes `@switch (Model.RetakeMode)`:
- `ShowFullReview` → existing markup `:318-411` VERBATIM (do NOT delete).
- `ShowScoreOnly` → existing notice `:415-417` VERBATIM.
- `ShowWrongFlagsOnly` → NEW branch (407-UI-SPEC §Component 3): same `QuestionReviews` loop, suppress all answer-key signals.

**LEAK SITES to suppress in `ShowWrongFlagsOnly`** (security-critical — these lines MUST NOT render):
```cshtml
@* :366 *@  itemClass = "list-group-item-success";
@* :367 *@  icon = "bi-check-circle-fill text-success";
@* :386-389 *@  @if (option.IsCorrect) { <small class="ms-auto text-muted">(Jawaban Benar)</small> }
@* :403 *@  <small class="text-muted d-block mt-2">@question.CorrectAnswer</small>   // essay rubric/key
```
In `ShowWrongFlagsOnly` render ONLY: question text + worker's selected option text + `(Jawaban Anda)` (`:382-385`) + per-soal ✓/✗ verdict badge (`:339-350`). For essay: only `@question.UserAnswer` (`:402`), NEVER `@question.CorrectAnswer` (`:403`).

#### (d) Worker riwayat card — render `_RiwayatPekerja` partial (see below)

Card shell from 407-UI-SPEC §Component 4: `<div class="card shadow-sm mb-4">` header "Riwayat Percobaan Saya" (`bi-clock-history`) → `@await Html.PartialAsync("_RiwayatPekerja", Model.RiwayatAttempts)` or inline accordion. Empty-state copy in UI-SPEC.

---

### `Views/CMP/_RiwayatPekerja.cshtml` (NEW partial, gated worker variant)

**Analog:** `Views/Admin/_RiwayatPercobaan.cshtml` (full file) — copy structure, apply 3 worker deltas.

**Mirror verbatim** (HC partial `:15-103`): accordion-per-attempt, `accordion-button` aria, tri-state status cell (`:75-91`), empty-states (`:11, :63`), `@model List<HcPortal.Models.RiwayatAttemptViewModel>`, all user content via Razor `@` (auto-encode — XSS-safe; HC partial already verdict-only, no answer-key — RESEARCH Pitfall 1).

**Worker deltas (3 changes)** [RESEARCH Pitfall 1 + UI-SPEC §Component 4]:
1. **Badge "Gagal" → "Tidak Lulus"** — HC partial `:33` `<span class="badge text-bg-danger ms-2">Gagal</span>` → worker `Tidak Lulus` (consistent Results `:82` status badge / Records `:222`). Keep "Lulus" (`:29`) + "Menunggu Penilaian" (`:37`).
2. **Column header "Jawaban Peserta" → "Jawaban Saya"** — HC `:54` `<th>Jawaban Peserta</th>` → worker `<th>Jawaban Saya</th>`.
3. **Gating `ShowScoreOnly`** — when worker's `RetakeMode == ShowScoreOnly` (AllowAnswerReview==false), render NOTHING per-soal (HC partial always shows verdict-only; worker must respect score-only). Pass gating flag via ViewBag or a wrapper VM. Archived rows are structurally verdict-only already (`AssessmentAttemptResponseArchive` stores only `AnswerText`+`IsCorrect`+`AwardedScore` `:33-39` — no option-list/key) so no per-row key-suppression needed.

**Status cell tri-state to mirror verbatim** (HC `:76-90`):
```cshtml
@if (row.IsCorrect == true) { <i class="bi bi-check-circle-fill text-success" title="Benar"></i><span class="visually-hidden">Benar</span> }
else if (row.IsCorrect == false) { <i class="bi bi-x-circle-fill text-danger" title="Salah"></i><span class="visually-hidden">Salah</span> }
else { <span class="text-muted" title="Menunggu penilaian">—</span><span class="visually-hidden">Menunggu</span> }
```

**Current-attempt badge** (HC `:40-43` already has it — keep): `<span class="badge bg-info ms-2">Percobaan saat ini</span>` driven by `attempt.IsCurrent`.

---

### `Views/CMP/Records.cshtml` (view, render — OPTIONAL trigger)

**Analog:** self Aksi cell `:260-283`. Existing "Lihat Hasil" (`:262`) already routes to Results where riwayat lives — sufficient by default (D-04 discretion).

**Existing "Lihat Hasil" idiom** (`:262-264`):
```cshtml
<a href="@resultsUrl" class="btn btn-sm btn-outline-primary">
    <i class="bi bi-bar-chart-line me-1"></i>Lihat Hasil
</a>
```
If a per-row "Riwayat Percobaan" trigger is added (planner's choice), mirror this `btn btn-sm btn-outline-secondary` idiom with `bi bi-clock-history` + `title`/`aria-label`, placed in the same Aksi cell.

---

### `HcPortal.Tests/RetakeRulesTests.cs` — extend `ResolveReviewMode` (test, unit)

**Analog:** self (`:14-60`) — `RetakeRulesTests` class, `Can(...)` default-eligible helper idiom + `[Fact]` per branch. No DB, no fixture (mirror `ShuffleToggleRulesTests`).

**Idiom to mirror** (`:37-60`):
```csharp
[Fact] public void Eligible_WhenAllConditionsMet() => Assert.True(Can());
[Fact] public void Blocked_WhenAllowRetakeOff() => Assert.False(Can(allowRetake: false));
```

**Add** — 5 branches of `ResolveReviewMode` truth table (RESEARCH §Pattern 3 + Pitfall 5 pending null):
```csharp
[Fact] public void Tier_ScoreOnly_WhenReviewDisabled()
    => Assert.Equal(RetakeReviewMode.ShowScoreOnly, RetakeRules.ResolveReviewMode(false, false, true));
[Fact] public void Tier_WrongFlagsOnly_WhenFailedWithAttemptsLeft()
    => Assert.Equal(RetakeReviewMode.ShowWrongFlagsOnly, RetakeRules.ResolveReviewMode(true, false, true));
[Fact] public void Tier_FullReview_WhenFailedExhausted()
    => Assert.Equal(RetakeReviewMode.ShowFullReview, RetakeRules.ResolveReviewMode(true, false, false));
[Fact] public void Tier_FullReview_WhenPassed()
    => Assert.Equal(RetakeReviewMode.ShowFullReview, RetakeRules.ResolveReviewMode(true, true, true));
[Fact] public void Tier_FullReview_WhenPendingNull()    // Pitfall 5 — pending not retake-eligible
    => Assert.Equal(RetakeReviewMode.ShowFullReview, RetakeRules.ResolveReviewMode(true, null, true));
```

---

### `HcPortal.Tests/` — NEW `RetakeExam` endpoint test (test, integration/controller)

**Analog:** `HcPortal.Tests/RetakeServiceTests.cs:1-60` — disposable real-SQL fixture (`RetakeServiceFixture` `MigrateAsync` full chain @localhost\SQLEXPRESS, `[Trait("Category","Integration")]`, drop-on-dispose), `NoOpHubContext` hand-stub (no Moq), `NullLogger`.

**Hub/logger stub idiom to reuse** (RESEARCH §Wave 0 — `RetakeServiceTests.cs:15-16`): `NoOpHubContext` (SendAsync no-op) + `NullLogger`, real `AuditLogService` over test DbContext.

**Cases (RTK-09)**: non-owner → `Forbid`; not-eligible → redirect Results + TempData["Error"]; success → token cleared (`TempData[TokenVerified_{id}]` removed) + redirect `StartExam`. Mirror fixture setup `:34-60` for any integration variant; or controller unit with mocked `RetakeService` + faked `GetCurrentUserRoleLevelAsync` user.

---

## Shared Patterns

### Server-authoritative eligibility (V1/V4 ASVS)
**Source:** `Services/RetakeService.cs:232-248` (`CanRetakeAsync`) wrapping `Helpers/RetakeRules.cs:29-52` (`CanRetake`).
**Apply to:** `RetakeExam` (re-check before mutate) + `Results` (build `CanRetake`/`IsCapReached`). NEVER duplicate cooldown/cap logic in the controller — call `CanRetakeAsync`.
```csharp
if (!await _retakeService.CanRetakeAsync(id)) { /* reject — countdown JS is UX only, not a gate */ }
```

### Antiforgery on state-changing POST (V13/CSRF)
**Source:** `AssessmentAdminController.cs:4245-4247` (`[HttpPost][Authorize][ValidateAntiForgeryToken]`) + `UpdateRetakeSettings :5613-5615`.
**Apply to:** `RetakeExam` action attrs + the modal form (`@Html.AntiForgeryToken()` per 407-UI-SPEC §Component 2).

### Ownership / effective-user guard (V4 — IDOR prevention)
**Source:** `CMPController.cs:909-912` (`StartExam` effective-user owner-check) + `Results :2193-2196`.
**Apply to:** `RetakeExam` — `var (user, _) = await GetCurrentUserRoleLevelAsync(); if (assessment.UserId != user.Id) return Forbid();`

### Actor-name formatting for audit
**Source:** `AssessmentAdminController.cs:4298` (and `UpdateRetakeSettings :5651`).
**Apply to:** `RetakeExam` ExecuteAsync call.
```csharp
var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
```

### TempData token clear (must-fix #1)
**Source:** `AssessmentAdminController.cs:4317-4319` (`TempData.Remove($"TokenVerified_{id}")`) — service is HTTP-agnostic (`RetakeService.cs:38-39` defers this to caller); `StartExam` reads via `TempData.Peek` (`CMPController.cs:944`, non-consume).
**Apply to:** `RetakeExam` — call AFTER `ExecuteAsync` success, BEFORE redirect to `StartExam`.

### Riwayat data-load + pure unifier reuse
**Source:** `AssessmentAdminController.cs:3493-3522` (`RiwayatPercobaan`) → `RetakeArchiveBuilder.Build(0,...)` sentinel + `RiwayatUnifier.Build` (`Helpers/RiwayatUnifier.cs:21-69`, EF-free pure — gating added at view/VM layer, not in unifier).
**Apply to:** `Results` action (riwayat load) + `_RiwayatPekerja` partial render.

### Per-soal verdict ✓/✗ (kill-drift)
**Source:** `AssessmentScoreAggregator.IsQuestionCorrect` (used at `CMPController.cs:2263` for current) + `AssessmentAttemptResponseArchive.IsCorrect` (frozen verdict for archived, `:35-36`). Tri-state: true/false/null.
**Apply to:** tier feedback verdict badges + riwayat status cells. Do NOT recompute correctness in the view.

### XSS — Razor `@` auto-encode, never `Html.Raw` for user content (V5)
**Source:** HC partial `_RiwayatPercobaan.cshtml` (all `@row.QuestionText`/`@row.AnswerText` auto-encoded) + Results review block.
**Apply to:** all new feedback/riwayat markup. The only `Html.Raw` in scope (`Records.cshtml:204`) is app-controlled chart JSON — do NOT extend to free-text.

---

## No Analog Found

None. Every file in scope maps to an existing analog (this is a brownfield reuse phase — RESEARCH "Key insight: hampir SEMUA logika 407 sudah ada"). The only genuinely new artifacts (`ResolveReviewMode`, `_RiwayatPekerja.cshtml`, `RetakeExam`) are each direct mirrors of an existing sibling (`CanRetake`, `_RiwayatPercobaan.cshtml`, `ResetAssessment` respectively).

---

## Metadata

**Analog search scope:** `Controllers/`, `Helpers/`, `Models/`, `Services/`, `Views/CMP/`, `Views/Admin/`, `HcPortal.Tests/`
**Files scanned (read in full or targeted ranges):** 15 — `AssessmentAdminController.cs` (ResetAssessment, RiwayatPercobaan, UpdateRetakeSettings), `CMPController.cs` (ctor, Results, StartExam), `RetakeRules.cs`, `RetakeService.cs`, `RiwayatUnifier.cs`, `RiwayatAttemptViewModel.cs`, `AssessmentResultsViewModel.cs`, `AllWorkersHistoryRow.cs`, `AssessmentAttemptResponseArchive.cs`, `AssessmentSession.cs`, `Results.cshtml`, `Records.cshtml`, `StartExam.cshtml`, `_RiwayatPercobaan.cshtml`, `RetakeRulesTests.cs`, `RetakeServiceTests.cs`
**Line anchors:** verified directly against repo (RESEARCH §Sources cross-checked; minor drift — e.g. leak-site option highlight at `:366/:367/:386-389`, essay key at `:403`, action area `:420-431` — corrected above where they differed from research estimates).
**Pattern extraction date:** 2026-06-22
