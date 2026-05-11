# Phase 306: Score Editable per Question Type — Pattern Map

**Mapped:** 2026-04-28
**Files analyzed:** 6 change units across 2 source files
**Analogs found:** 6 / 6

---

## File Classification

| New/Modified Code Unit | Role | Data Flow | Closest Analog | Match Quality |
|------------------------|------|-----------|----------------|---------------|
| `ManagePackageQuestions.cshtml` line 42 — header total points | view / Razor inline expression | request-response (SSR) | Same file line 42 existing `@questions.Count` expression | exact |
| `ManagePackageQuestions.cshtml` lines 184-187 — scoreValue input + help text | view / form input | form POST | Same file lines 173-177 (`maxCharacters` numeric input + help text) | exact |
| `ManagePackageQuestions.cshtml` lines 237-253 — new `editScoreWarningModal` block | view / Bootstrap modal HTML | event-driven (JS trigger) | Same file lines 237-253 `editTypeWarningModal` | exact |
| `ManagePackageQuestions.cshtml` lines 289-310 — `applyQTypeSwitch` JS edits | view / vanilla JS handler | event-driven (dropdown change) | Same file lines 289-310 — function body being modified | exact |
| `ManagePackageQuestions.cshtml` IIFE — NEW JS submit handler + `populateEditForm` extension | view / vanilla JS handler | event-driven (form submit + AJAX response) | Same file lines 263-270 `confirmTypeChange` listener + lines 342-380 `populateEditForm` | exact |
| `AssessmentAdminController.cs` — `CreateQuestion` + `EditQuestion` POST actions | controller | form POST → DB → audit log | Same file lines 4680-4699 (validation block) + lines 326-328 / 1792-1800 / 2007-2018 (audit log patterns) | exact |

---

## Pattern Assignments

### 1. Header Total Points — `ManagePackageQuestions.cshtml` line 42

**Action:** MODIFY — extend Razor expression

**Analog (existing, line 42):**
```razor
<span class="fw-semibold">Daftar Soal (@questions.Count soal)</span>
```

**Target pattern (copy from analog, extend inline):**
```razor
<span class="fw-semibold">Daftar Soal (@questions.Count soal • Total @questions.Sum(q => q.ScoreValue) poin)</span>
```

**Notes:** `questions` variable already in scope (line 6: `var questions = ViewBag.Questions as List<PackageQuestion> ?? new List<PackageQuestion>()`). No ViewBag change needed. `•` is U+2022 bullet — copy literal character.

---

### 2. ScoreValue Input + Help Text — `ManagePackageQuestions.cshtml` lines 184-187

**Action:** MODIFY — remove `disabled`, add `step="1" required`, change help text to static

**Analog (same file, lines 173-177 — `maxCharacters` input pattern):**
```razor
<input type="number" name="maxCharacters" id="maxCharacters"
       class="form-control form-control-sm" value="2000"
       min="100" max="10000" style="max-width:120px" />
<div class="form-text">Default: 2000 karakter</div>
```

**Existing code to replace (lines 184-187):**
```razor
<input type="number" name="scoreValue" id="scoreValue"
       class="form-control form-control-sm" value="10"
       min="1" max="100" style="max-width:80px" disabled />
<div class="form-text" id="scoreHelp">MC/MA: nilai tetap 10. Essay: bisa diubah.</div>
```

**Target pattern:**
```razor
<input type="number" name="scoreValue" id="scoreValue"
       class="form-control form-control-sm" value="10"
       min="1" max="100" step="1" required style="max-width:80px" />
<div class="form-text" id="scoreHelp">Range 1–100</div>
```

**Notes:** `disabled` removed (D-03, D-04). `step="1" required` added (D-12). Help text static "Range 1–100" (CD-01 locked). `id="scoreHelp"` retained so JS can reference it (static text set in Razor, dynamic update via JS removed in item 4 below).

---

### 3. NEW Modal `editScoreWarningModal` — insert after line 253

**Action:** NEW block — replicate structure of `editTypeWarningModal`

**Analog (exact template, lines 237-253):**
```html
<!-- Edit Warning Modal -->
<div class="modal fade" id="editTypeWarningModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-warning">
                <h5 class="modal-title"><i class="bi bi-exclamation-triangle me-2"></i>Peringatan Ubah Tipe Soal</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                Mengubah tipe soal akan menghapus jawaban peserta yang ada untuk soal ini. Lanjutkan?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-warning btn-sm" id="confirmTypeChange">Ya, Lanjutkan</button>
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Batal</button>
            </div>
        </div>
    </div>
</div>
```

**Target pattern (new modal, copy structure above exactly — change IDs and copy only):**
```html
<!-- Score Change Warning Modal -->
<div class="modal fade" id="editScoreWarningModal" tabindex="-1"
     aria-labelledby="editScoreWarningModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-warning">
                <h5 class="modal-title" id="editScoreWarningModalLabel">
                    <i class="bi bi-exclamation-triangle me-2"></i>Peringatan Ubah Skor
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Tutup"></button>
            </div>
            <div class="modal-body" id="editScoreWarningModalBody">
                <!-- populated by JS: populateScoreWarningModal() -->
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-warning btn-sm" id="confirmScoreChange">Ya, Lanjutkan</button>
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Batal</button>
            </div>
        </div>
    </div>
</div>
```

**Notes:** Differences from analog: `id`, `aria-labelledby`, title text, `id` on title h5, `aria-label="Tutup"` on close button (a11y), body has dynamic `id` for JS injection. Button classes identical (`btn-warning btn-sm` + `btn-secondary btn-sm`) per CD-02 lock.

---

### 4. JS `applyQTypeSwitch` Edits — lines 289-310

**Action:** MODIFY — remove 3 lines inside the existing function

**Analog (existing function body, lines 289-310):**
```javascript
function applyQTypeSwitch(qtype) {
    var optionsSection = document.getElementById('optionsSection');
    var rubrikSection = document.getElementById('rubrikSection');
    var scoreInput = document.getElementById('scoreValue');
    var maLabel = document.getElementById('maLabel');

    optionsSection.style.display = (qtype === 'Essay') ? 'none' : '';
    rubrikSection.style.display = (qtype === 'Essay') ? '' : 'none';
    scoreInput.disabled = (qtype !== 'Essay');          // LINE 297 — REMOVE
    if (qtype !== 'Essay') scoreInput.value = 10;       // LINE 298 — REMOVE
    maLabel.style.display = (qtype === 'MultipleAnswer') ? '' : 'none';

    // Switch radio <-> checkbox for correct answer inputs
    document.querySelectorAll('.correct-input').forEach(function (inp) {
        inp.type = (qtype === 'MultipleAnswer') ? 'checkbox' : 'radio';
        if (qtype !== 'MultipleAnswer') inp.checked = false;
    });

    // Update score help text
    var scoreHelp = document.getElementById('scoreHelp');
    scoreHelp.textContent = (qtype === 'Essay') ? 'Essay: atur nilai sesuai bobot soal.' : 'MC/MA: nilai tetap 10.'; // LINES 308-309 — REMOVE
}
```

**Target pattern (3 lines removed, nothing added):**
```javascript
function applyQTypeSwitch(qtype) {
    var optionsSection = document.getElementById('optionsSection');
    var rubrikSection = document.getElementById('rubrikSection');
    var scoreInput = document.getElementById('scoreValue');
    var maLabel = document.getElementById('maLabel');

    optionsSection.style.display = (qtype === 'Essay') ? 'none' : '';
    rubrikSection.style.display = (qtype === 'Essay') ? '' : 'none';
    // scoreInput.disabled line removed (D-03, D-04)
    // scoreInput.value = 10 reset line removed (D-02)
    maLabel.style.display = (qtype === 'MultipleAnswer') ? '' : 'none';

    // Switch radio <-> checkbox for correct answer inputs
    document.querySelectorAll('.correct-input').forEach(function (inp) {
        inp.type = (qtype === 'MultipleAnswer') ? 'checkbox' : 'radio';
        if (qtype !== 'MultipleAnswer') inp.checked = false;
    });
    // scoreHelp.textContent dynamic update removed (D-05) — static "Range 1–100" from Razor
}
```

---

### 5. NEW JS Submit Handler + `populateEditForm` Extension — inside IIFE

**Action:** NEW JS logic inserted in IIFE after the existing `confirmTypeChange` listener (around line 270)

**Analog A — existing `confirmTypeChange` listener (lines 263-270, same IIFE):**
```javascript
document.addEventListener('DOMContentLoaded', function () {
    editTypeWarningModal = new bootstrap.Modal(document.getElementById('editTypeWarningModal'));

    document.getElementById('confirmTypeChange').addEventListener('click', function () {
        document.getElementById('QuestionType').value = pendingType;
        applyQTypeSwitch(pendingType);
        editTypeWarningModal.hide();
    });
});
```

**Analog B — `populateEditForm` function (lines 342-380, same IIFE):**
```javascript
function populateEditForm(data) {
    // ...
    document.getElementById('scoreValue').value = data.scoreValue || 10;
    // ... rest of form population
    document.getElementById('submitBtn').innerHTML = '<i class="bi bi-save me-1"></i>Simpan Perubahan';
    document.getElementById('cancelEditBtn').style.display = '';
    document.getElementById('questionFormCard').scrollIntoView({ behavior: 'smooth' });
}
```

**Target pattern — new submit handler (copy `confirmTypeChange` listener structure, adapt for submit flow):**
```javascript
// Inside DOMContentLoaded block, after existing editTypeWarningModal init:
var editScoreWarningModal = new bootstrap.Modal(document.getElementById('editScoreWarningModal'));
var scoreChangeBypassed = false;

document.getElementById('questionForm').addEventListener('submit', function (event) {
    if (scoreChangeBypassed) { scoreChangeBypassed = false; return; } // bypass after confirm

    var isEditMode = document.getElementById('editQuestionId').value !== '';
    if (!isEditMode) return; // Create mode: no modal needed

    var scoreInput = document.getElementById('scoreValue');
    var originalScore = parseInt(scoreInput.dataset.originalScore, 10);
    var newScore = parseInt(scoreInput.value, 10);
    var affectedN = parseInt(document.getElementById('questionForm').dataset.affectedSessions, 10) || 0;

    if (!isNaN(originalScore) && newScore !== originalScore && affectedN > 0) {
        event.preventDefault();
        populateScoreWarningModal(
            document.getElementById('questionForm').dataset.questionOrder,
            originalScore,
            newScore,
            affectedN
        );
        editScoreWarningModal.show();
    }
});

document.getElementById('confirmScoreChange').addEventListener('click', function () {
    editScoreWarningModal.hide();
    scoreChangeBypassed = true;
    document.getElementById('questionForm').submit();
});

function populateScoreWarningModal(order, oldScore, newScore, affectedN) {
    document.getElementById('editScoreWarningModalBody').innerHTML =
        'Skor soal #' + order + ' akan diubah dari <strong>' + oldScore + '</strong> menjadi <strong>' +
        newScore + '</strong>. <strong>' + affectedN + ' peserta</strong> sudah menjawab — ' +
        'persentase mereka akan dihitung ulang otomatis. Lanjutkan?';
}
```

**Target pattern — `populateEditForm` extension (add 2 lines after `data.scoreValue` assignment, line 352):**
```javascript
document.getElementById('scoreValue').value = data.scoreValue || 10;
// ADD THESE TWO LINES:
document.getElementById('scoreValue').dataset.originalScore = data.scoreValue || 10;
document.getElementById('questionForm').dataset.affectedSessions = data.affectedSessions || 0;
document.getElementById('questionForm').dataset.questionOrder = data.order || '';
```

**Notes:** `data.affectedSessions` requires the GET `EditQuestion` AJAX response (line 4770-4785) to include `affectedSessions` count from a new DB query `_context.PackageUserResponses.Where(r => r.PackageQuestionId == q.Id).Select(r => r.AssessmentSessionId).Distinct().Count()`. The JSON object at line 4770-4785 must be extended with this field.

---

### 6. Controller: `CreateQuestion` + `EditQuestion` POST Actions

**Sub-unit 6a: Remove force-override lines + add range validation**

**Action:** MODIFY `CreateQuestion` (lines 4681-4682) and `EditQuestion` (lines 4822-4823)

**Analog (existing validation block style, lines 4685-4699 same actions):**
```csharp
// Validate per type (D-07)
var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
if (questionType == "MultipleChoice" && correctCount != 1)
{
    TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
if (questionType == "Essay" && string.IsNullOrWhiteSpace(rubrik))
{
    TempData["Error"] = "Rubrik wajib diisi untuk soal Essay.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

**Existing code to remove (lines 4681-4682 in CreateQuestion, lines 4822-4823 in EditQuestion):**
```csharp
if (questionType != "Essay") scoreValue = 10;   // REMOVE — over-restrictive (D-14)
if (scoreValue <= 0) scoreValue = 10;            // REMOVE — silent coerce (D-14)
```

**Target pattern (replace removed lines with range check — copy TempData + redirect style from analog above):**
```csharp
// Range validation (D-12, D-13)
if (scoreValue < 1 || scoreValue > 100)
{
    TempData["Error"] = "Nilai soal harus antara 1 dan 100.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

---

**Sub-unit 6b: Audit log for EditQuestion score change**

**Action:** NEW audit log block in `EditQuestion` POST — insert after `SaveChangesAsync()` at line 4872

**Analog (existing audit log with defensive try/catch — lines 2005-2018 `DeleteAssessment`):**
```csharp
try
{
    var deleteUser = await _userManager.GetUserAsync(User);
    var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP)
        ? (deleteUser?.FullName ?? "Unknown")
        : $"{deleteUser.NIP} - {deleteUser.FullName}";
    await _auditLog.LogAsync(
        deleteUser?.Id ?? "",
        deleteActorName,
        "DeleteAssessment",
        $"Deleted assessment '{assessmentTitle}' [ID={id}]",
        id,
        "AssessmentSession");
}
catch (Exception auditEx)
{
    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id);
}
```

**Target pattern (EditQuestion-ScoreChange audit, insert after SaveChangesAsync at line 4872):**
```csharp
// Audit log: score change (D-10)
if (scoreValue != oldScore) // oldScore captured before q.ScoreValue = scoreValue assignment
{
    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(
            currentUser?.Id ?? "",
            actorName,
            "EditQuestion-ScoreChange",
            $"Question #{q.Id} (Order {q.Order}, Package #{packageId}) ScoreValue: {oldScore} → {scoreValue} ({affectedSessionsCount} sessions affected)",
            q.Id,
            "PackageQuestion");
    }
    catch (Exception auditEx)
    {
        _logger.LogWarning(auditEx, "Audit logging failed during EditQuestion-ScoreChange for Question {Id}", q.Id);
    }
}
```

**Notes:**
- `oldScore` must be captured BEFORE the mutation: `var oldScore = q.ScoreValue;` (insert at start of edit block, before line 4843).
- `affectedSessionsCount` from: `var affectedSessionsCount = await _context.PackageUserResponses.Where(r => r.PackageQuestionId == questionId).Select(r => r.AssessmentSessionId).Distinct().CountAsync();` — compute ONCE, reuse for both modal data injection (JSON GET) and audit log (POST).
- `_logger` already injected at controller scope (line 23 of AssessmentAdminController) — use directly, no `GetRequiredService` needed here.

---

**Sub-unit 6c: Audit log for CreateQuestion non-default score (CD-05 locked: implement)**

**Action:** NEW audit log block in `CreateQuestion` POST — insert after `SaveChangesAsync()` at line 4735

**Analog (simple audit log inline without defensive wrap — lines 326-328 `AddCategory`):**
```csharp
await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "AddCategory",
    $"Added assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
    category.Id, "AssessmentCategory");
```

**Target pattern (CreateQuestion non-default score audit — defensive wrap preferred for consistency):**
```csharp
// Audit log: non-default score creation (D-11, CD-05)
if (scoreValue != 10)
{
    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(
            currentUser?.Id ?? "",
            actorName,
            "CreateQuestion-CustomScore",
            $"CreateQuestion: Question added with custom ScoreValue={scoreValue} (default 10) for Package #{packageId}",
            newQ.Id,
            "PackageQuestion");
    }
    catch (Exception auditEx)
    {
        _logger.LogWarning(auditEx, "Audit logging failed during CreateQuestion-CustomScore for Package {PackageId}", packageId);
    }
}
```

---

**Sub-unit 6d: EditQuestion GET — extend JSON response with `affectedSessions`**

**Action:** MODIFY `EditQuestion` GET AJAX JSON response (lines 4768-4785)

**Analog (existing JSON return, lines 4768-4785):**
```csharp
if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
{
    return Json(new
    {
        id = q.Id,
        order = q.Order,
        questionText = q.QuestionText,
        questionType = q.QuestionType ?? "MultipleChoice",
        scoreValue = q.ScoreValue,
        elemenTeknis = q.ElemenTeknis,
        rubrik = q.Rubrik,
        maxCharacters = q.MaxCharacters,
        options = q.Options.OrderBy(o => o.Id).Select(o => new
        {
            optionText = o.OptionText,
            isCorrect = o.IsCorrect
        }).ToList()
    });
}
```

**Target pattern (add `affectedSessions` field — compute before Json return):**
```csharp
if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
{
    var affectedSessions = await _context.PackageUserResponses
        .Where(r => r.PackageQuestionId == q.Id)
        .Select(r => r.AssessmentSessionId)
        .Distinct()
        .CountAsync();

    return Json(new
    {
        id = q.Id,
        order = q.Order,
        questionText = q.QuestionText,
        questionType = q.QuestionType ?? "MultipleChoice",
        scoreValue = q.ScoreValue,
        affectedSessions = affectedSessions,   // NEW field (D-09)
        elemenTeknis = q.ElemenTeknis,
        rubrik = q.Rubrik,
        maxCharacters = q.MaxCharacters,
        options = q.Options.OrderBy(o => o.Id).Select(o => new
        {
            optionText = o.OptionText,
            isCorrect = o.IsCorrect
        }).ToList()
    });
}
```

---

## Shared Patterns

### Audit Log — actorName Construction
**Source:** `AssessmentAdminController.cs` lines 322-328 (AddCategory), lines 374-380 (EditCategory), lines 1291-1298 (CreateAssessment)
**Apply to:** Sub-units 6b, 6c above
```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "ActionName",
    $"...detail string...",
    targetId,
    "TargetType");
```

### Audit Log — Defensive Try/Catch Fallback
**Source:** `AssessmentAdminController.cs` lines 1331-1342 and lines 2015-2018
**Apply to:** Sub-units 6b, 6c — wrap ALL `_auditLog.LogAsync` calls in phase 306
```csharp
try
{
    // ... _auditLog.LogAsync call ...
}
catch (Exception auditEx)
{
    _logger.LogWarning(auditEx, "Audit logging failed during [ActionName]");
}
```
**Note:** `_logger` is injected at constructor scope (`ILogger<AssessmentAdminController>`, line 23). Use `_logger` directly — do NOT use `HttpContext.RequestServices.GetRequiredService<ILogger<...>>()` (that pattern only appears in EditAssessment line 1806 due to scoping context, not the standard for these smaller actions).

### Validation Flash Error — TempData + Redirect
**Source:** `AssessmentAdminController.cs` lines 4686-4699 (existing correctCount + rubrik checks inside CreateQuestion and EditQuestion)
**Apply to:** Sub-unit 6a range validation
```csharp
TempData["Error"] = "...Bahasa Indonesia message...";
return RedirectToAction("ManagePackageQuestions", new { packageId });
```

### Bootstrap Modal Initialization — DOMContentLoaded
**Source:** `ManagePackageQuestions.cshtml` lines 263-264
**Apply to:** Sub-unit 5 (new `editScoreWarningModal` initialization inside the same `DOMContentLoaded` block)
```javascript
document.addEventListener('DOMContentLoaded', function () {
    editTypeWarningModal = new bootstrap.Modal(document.getElementById('editTypeWarningModal'));
    // ADD: editScoreWarningModal = new bootstrap.Modal(document.getElementById('editScoreWarningModal'));
});
```

---

## No Analog Found

No files in this phase lack an analog. All 6 change units have exact matches in the existing codebase.

---

## Implementation Order (for planner reference)

Recommended sequencing based on dependencies:

1. **Controller GET** — sub-unit 6d: extend `EditQuestion` JSON with `affectedSessions` field (no dependencies)
2. **Controller POST `CreateQuestion`** — sub-unit 6a (range check) + 6c (audit log) (no view dependency)
3. **Controller POST `EditQuestion`** — sub-unit 6a (range check) + 6b (audit log) (needs `affectedSessions` from step 1 context)
4. **View form input** — sub-unit 2 (remove `disabled`, add `step required`) — safe, no JS dependency
5. **View header** — sub-unit 1 (total points inline expression) — isolated 1-line change
6. **View modal HTML** — sub-unit 3 (new `editScoreWarningModal` block)
7. **View JS `applyQTypeSwitch`** — sub-unit 4 (remove 3 lines)
8. **View JS submit handler + `populateEditForm`** — sub-unit 5 (depends on modal HTML from step 6 + `affectedSessions` in JSON from step 1)

---

## Metadata

**Analog search scope:** `Controllers/AssessmentAdminController.cs`, `Views/Admin/ManagePackageQuestions.cshtml`
**Files scanned:** 2 source files + 3 upstream spec files (CONTEXT, RESEARCH, UI-SPEC)
**Pattern extraction date:** 2026-04-28
