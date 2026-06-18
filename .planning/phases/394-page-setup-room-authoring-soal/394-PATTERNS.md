# Phase 394: Page + Setup Room + authoring soal - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 4 (3 NEW + 1 MODIFIED)
**Analogs found:** 4 / 4 (all exact or strong role+flow matches; verified line-by-line against live code in main tree)

> Brownfield reuse phase. Every analog below was opened and verified against the live working tree (not just RESEARCH.md citations). Line numbers confirmed accurate as of HEAD on `main`.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/InjectAssessmentController.cs` (NEW) | controller (MVC) | request-response (GET page + read-only feeds; POST commit STUB only in 394) | `Controllers/AssessmentAdminController.cs` (CreateAssessment GET/POST + CheckTitleAvailability) | exact (same role, same flow, same RBAC, same View-folder trick) |
| `Views/Admin/InjectAssessment.cshtml` (NEW) | view (Razor wizard) | request-response + client-state capture | `Views/Admin/CreateAssessment.cshtml` (wizard scaffold + picker + cert card + confirm) | exact (mirror) — authoring sub-region from `ManagePackageQuestions.cshtml` |
| `ViewModels/InjectAssessmentViewModel.cs` (NEW) | view-model (DTO) | transform (form POST → `InjectRequest`) | `Models/InjectAssessmentDtos.cs` (`InjectRequest`/`InjectQuestionSpec`/`InjectWorkerSpec`) | exact (target contract already defined by Phase 393) |
| `Views/Admin/Index.cshtml` (MODIFIED) | view (dashboard card) | request-response (server-rendered, role-gated) | `Views/Admin/Index.cshtml` Section-C existing cards (self-analog) | exact (copy a sibling `col-md-4` card block) |

> Optional 5th file at planner discretion: `Views/Admin/_InjectQuestionForm.cshtml` (NEW partial — extract authoring *markup* from `ManagePackageQuestions.cshtml:116-260`). Classification: partial-view, transform. Analog = `ManagePackageQuestions.cshtml`. See Pattern Assignment #2.

---

## Pattern Assignments

### `Controllers/InjectAssessmentController.cs` (controller, request-response)

**Analog:** `Controllers/AssessmentAdminController.cs`

**Class header + route + View-folder override** (analog `AssessmentAdminController.cs:19-20, 58-62` — VERIFIED):
- Decorate class `[Route("Admin/[action]")]` so action `InjectAssessment` → URL `/Admin/InjectAssessment` (matches CONTEXT mandate).
- The controller name (`InjectAssessment`) ≠ view folder (`Admin`). The analog solves this with a `View()` override resolving `~/Views/Admin/{action}.cshtml`. **Copy this override verbatim** (lines 58-62) OR put the new view in `Views/InjectAssessment/`. Recommend the override for consistency with the established convention:
```csharp
// Source: AssessmentAdminController.cs:58-62 [VERIFIED]
protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
```

**RBAC pattern** (analog `:649-651, 858-860` — VERIFIED — note the SPACE after the comma, match exactly):
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> InjectAssessment() { /* ViewBag feeds + return View(vm) */ }

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> InjectAssessment(InjectAssessmentViewModel vm) { /* map→InjectRequest; service call WIRED IN PHASE 395 */ }
```

**GET feed pattern** (analog `:653-668` — VERIFIED — copy verbatim, drop Proton/renewal branches):
```csharp
// Source: AssessmentAdminController.cs:653-668 [VERIFIED]
var users = await _context.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.FullName)
    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
    .ToListAsync();
ViewBag.Users = users;
ViewBag.SelectedUserIds = new List<string>();
ViewBag.Sections = await _context.GetAllSectionsAsync();
ViewBag.Categories = await _context.AssessmentCategories
    .Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
```
> OMIT from the analog feed: `ViewBag.ProtonTracks`, `ViewBag.ParentCategories`, and the entire `renewSessionId`/`renewTrainingId` renewal-mode block (`:692-836`). Inject has no Proton mode and no renewal mode.

**Title check — REUSE the existing endpoint, do NOT duplicate** (analog `:842-855` — VERIFIED):
```csharp
// Source: AssessmentAdminController.cs:842-855 [VERIFIED] — lives on AssessmentAdminController
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> CheckTitleAvailability(string title)
{
    var matches = await FindTitleDuplicatesAsync(_context, title);
    return Json(new { exists = matches.Count > 0, groupCount = matches.Count,
        matches = matches.Select(m => new { category = m.Category, tanggal = m.Tanggal, peserta = m.Peserta }) });
}
```
> **REUSE-vs-rewire decision:** REUSE verbatim. The new view's "Cek Judul" button points `data-check-url` at `@Url.Action("CheckTitleAvailability","AssessmentAdmin")`. Do NOT re-declare this endpoint on `InjectAssessmentController`. `FindTitleDuplicatesAsync`/`NormalizeTitleForDup` are `static` on `AdminBaseController` (`:271-293`, VERIFIED) — inheriting `AdminBaseController` gives access if you ever need it server-side.

**Base class:** Inherit `AdminBaseController` (analog does — `AssessmentAdminController : AdminBaseController`, `:20`) to get `_context`, `_userManager`, audit log, and the title-dup static helpers. Constructor must also DI-inject `InjectAssessmentService` (registered `Program.cs:57`, VERIFIED) for the Phase-395 commit call.

**ViewModel → InjectRequest mapping** (target `Models/InjectAssessmentDtos.cs:14-62` — VERIFIED full read):
```
vm.Title          → InjectRequest.Title
vm.Category       → InjectRequest.Category
vm.AssessmentType → InjectRequest.AssessmentType   // "Standard"/"PreTest"/"PostTest" — NEVER "Manual" (DTO comment :50)
vm.CompletedAt    → InjectRequest.CompletedAt        // backdate ≤ today (D-06); StartedAt/Schedule null→fallback CompletedAt
vm.DurationMinutes→ InjectRequest.DurationMinutes
vm.PassPercentage → InjectRequest.PassPercentage
vm.AllowAnswerReview → InjectRequest.AllowAnswerReview (default true)
vm.CertMode       → InjectRequest.CertMode (InjectCertMode enum: None=0/Auto=1/Manual=2, :4)
vm.Questions[]    → InjectRequest.Questions (InjectQuestionSpec: QuestionText/QuestionType/ScoreValue/Order/ElemenTeknis/Rubrik/TempId + Options[{OptionText,IsCorrect,TempId}])
vm.UserIds[]      → InjectRequest.Workers (InjectWorkerSpec: Nip/ManualCertNumber/CertValidUntil; Answers EMPTY in 394 — filled Phase 395)
```
> **CRITICAL boundary — UserId→NIP translation (Pitfall 2):** picker checkbox value = `user.Id` (`CreateAssessment.cshtml:319`, VERIFIED), but `InjectWorkerSpec.Nip` keys on `ApplicationUser.NIP` (`InjectAssessmentService.cs:347-350` builds `usersByNip` via `.Where(u => u.NIP != null && nips.Contains(u.NIP))`, VERIFIED). The controller MUST resolve selected `UserIds` → their `NIP` when building `InjectWorkerSpec` (single `_context.Users` query). If skipped, service preflight returns "NIP tidak ditemukan" for valid users.

> **Scope note:** In 394 the POST handler is a STUB/scaffold only. The actual `_injectService.InjectBatchAsync(req, ...)` call is wired in Phase 395 after answers exist (D-07). 394 may map the VM and return the view with a "lengkapi jawaban dulu" notice, or simply scaffold the signature.

---

### `Views/Admin/InjectAssessment.cshtml` (view, request-response + client-state)

**Analog:** `Views/Admin/CreateAssessment.cshtml` (wizard scaffold, picker, cert card, confirm, all JS) + `Views/Admin/ManagePackageQuestions.cshtml` (authoring sub-region)

#### Sub-pattern A — Wizard scaffold (mirror, extend 4→6 steps)

**nav-pills markup** (analog `CreateAssessment.cshtml:76-99` — VERIFIED):
```html
<nav class="mb-4">
  <ul class="nav nav-pills nav-fill gap-2" id="wizardStepNav">
    <li class="nav-item"><button type="button" class="nav-link bg-primary text-white active" id="pill-1"><i class="bi bi-circle-fill me-1"></i>1. Setup Room</button></li>
    <li class="nav-item"><button type="button" class="nav-link text-muted border" id="pill-2" disabled><i class="bi bi-circle me-1"></i>2. Pilih Pekerja</button></li>
    <!-- ... pills 3-6 ... -->
  </ul>
</nav>
<form asp-action="InjectAssessment" asp-controller="InjectAssessment" method="post" id="injectAssessmentForm" novalidate>
  @Html.AntiForgeryToken()   <!-- analog :102-103 — antiforgery inside the single form -->
```
> 6 pills (D-02): 1.Setup Room · 2.Pilih Pekerja · 3.Authoring Soal · 4.Sertifikat · 5.Jawaban · 6.Konfirmasi. Pills 2-6 start `disabled` until visited (analog convention `:84-95`). One `<form>` wraps all 6 `.step-panel` divs.

**Wizard JS — port `WizardController` IIFE** (analog `CreateAssessment.cshtml:877-937+` — VERIFIED):
```javascript
// Source: CreateAssessment.cshtml:877-937 [VERIFIED]
(function WizardController() {
    var currentStep = 1; var visitedSteps = new Set([1]); var returnToConfirm = false;
    function goToStep(n) { /* hide all .step-panel, show #step-n, updatePills(), if (n===CONFIRM) populateSummary(); scrollTo top */ }
    function updatePills() {
        for (var i = 1; i <= 6; i++) {   // CHANGED 4→6
            // active: bg-primary text-white active + bi-circle-fill
            // completed (visited && i<current): bg-success text-white + bi-check-circle-fill
            // visited: text-primary border border-primary
            // pending: text-muted border + disabled
        }
    }
    function validateStep(n) { /* per-step is-invalid + step-error alert; return bool */ }
    function populateSummary() { /* fill confirm spans via .textContent (XSS-safe, analog :1096) */ }
})();
```
**REUSE-vs-rewire decision:** REUSE structure verbatim; REWIRE bounds `for(i=1;i<=6;i++)`, add `btnNext4/btnNext5`, `btnPrev5/btnPrev6`, and `goToStep` triggers `populateSummary()` at step 6 (not 4). Step-5 `validateStep` returns true (placeholder) or checks "soal+pekerja done" (Pattern 5 below).

#### Sub-pattern B — Worker picker (REUSE verbatim — D-05)

**Markup** (analog `CreateAssessment.cshtml:271-349` — VERIFIED):
```html
<!-- Source: CreateAssessment.cshtml:277-337 [VERIFIED] -->
<select id="sectionFilter" class="form-select form-select-sm" style="max-width:220px;">...</select>
<input type="text" id="userSearchInput" placeholder="Cari nama atau email..." />
<button id="selectAllBtn">Pilih Semua</button> <button id="deselectAllBtn">Batalkan Semua</button>
<span class="badge bg-primary" id="selectedCountBadge">0 terpilih</span>
<div id="userCheckboxContainer" class="border rounded p-3" style="max-height:320px;overflow-y:auto;">
  @foreach (var user in ViewBag.Users) {
    <div class="form-check user-check-item mb-1" data-name="@user.FullName" data-email="@user.Email" data-section="@user.Section">
      <input class="form-check-input user-checkbox" type="checkbox" name="UserIds" value="@user.Id" id="user_@user.Id" />
      ...
    </div>
  }
</div>
<div id="selected-participants-panel" role="status" aria-live="polite">...</div>
```
**REUSE-vs-rewire decision:** REUSE markup + JS (`applyFilters`/select-all/deselect/`updateSelectedCount`/`renderSelectedParticipants`, analog `:1439-1498`) verbatim. **STRIP the Proton branch** (analog `:339-346` `#protonEligibleSection`/`#protonUserCheckboxContainer` + `applyProtonMode` JS) — inject has no Proton mode. The checkbox `value="@user.Id"` is the source of the UserId→NIP translation flagged above.

#### Sub-pattern C — Certificate radio (NEW behavior, mirror the card *styling* — D-06)

**Card styling analog** (`CreateAssessment.cshtml:556-602` — VERIFIED): `<div class="card mb-4"><div class="card-header bg-light"><h6><i class="bi bi-award me-2"></i>Sertifikat</h6></div>...`. The analog uses a `form-switch` GenerateCertificate toggle + a `ValidUntil` date input `min=today`. **REWIRE:** replace the switch with **3 `form-check` radios** (Auto/Manual/Tanpa per D-06) and JS that toggles conditional blocks: Auto→format preview `<code>KPB/xxx/{ROMAN}/{year}</code>` + `ValidUntil` + permanent checkbox; Manual→`NomorSertifikat` text + `ValidUntil` + permanent; Tanpa→hide all. Note the analog's `ValidUntil` uses `min=today` (forward-dated cert) — for inject the cert *year* follows backdate `CompletedAt` (D-12, handled by service), so the preview shows `{year}` only; `ValidUntil` itself stays a normal date.

#### Sub-pattern D — Confirmation + alerts (mirror)

- Confirm summary cards + `edit-from-confirm` buttons + final `btn-success btn-lg w-100` commit button: analog `CreateAssessment.cshtml:638-752`. `populateSummary()` uses `.textContent` (NOT innerHTML) for user data — keep this (XSS mitigation, analog `:1096`).
- TempData / ModelState alert summary: analog `:23-57` (`alert alert-dismissible fade show` + `bi` icon; invalid summary `alert-warning` + `<ul>`).

#### Sub-pattern E — Step-5 placeholder seam (design for Phase 395)

Render pill-5 + panel-5 like the others, but panel body = `alert-info` + `bi-hourglass-split` notice (copy in UI-SPEC line 123). `validateStep(5)` returns true. Gate the step-6 `btn-success` enablement on step-5 completion so Phase 395 slots in its answer inputs without refactoring the scaffold.

---

### Authoring soal sub-region / `Views/Admin/_InjectQuestionForm.cshtml` (partial OR inline — planner discretion)

**Analog:** `Views/Admin/ManagePackageQuestions.cshtml`

**Markup to mirror** (analog `:116-260` — VERIFIED — keep field NAMES/IDs identical for nol-duplikasi semantik):
```html
<!-- Source: ManagePackageQuestions.cshtml:127-244 [VERIFIED] -->
<select id="QuestionType" name="questionType">   <!-- MultipleChoice / MultipleAnswer / Essay -->
<textarea id="questionText" name="questionText" required>
<div id="optionsSection">
  <div id="maLabel" class="alert alert-info py-1 px-2 small" style="display:none">Centang semua opsi yang benar (minimal 2).</div>
  <!-- A-D rows: input-group-sm + .correct-input radio/checkbox name="correctA".."correctD" + input name="optionA".."optionD" -->
</div>
<div id="rubrikSection" style="display:none"><textarea name="rubrik" id="rubrik">...</textarea></div>
<input type="number" name="scoreValue" id="scoreValue" value="10" min="1" max="100" required>
<input type="text" name="elemenTeknis" id="elemenTeknis">
<button type="submit" id="submitBtn"><i class="bi bi-plus-circle me-1"></i>Tambah Soal</button>
```

**Type-toggle JS to mirror** (analog `:402-418` — VERIFIED):
```javascript
// Source: ManagePackageQuestions.cshtml:402-418 [VERIFIED]
function applyQTypeSwitch(qtype) {
    document.getElementById('optionsSection').style.display = (qtype === 'Essay') ? 'none' : '';
    document.getElementById('rubrikSection').style.display  = (qtype === 'Essay') ? '' : 'none';
    document.getElementById('maLabel').style.display        = (qtype === 'MultipleAnswer') ? '' : 'none';
    document.querySelectorAll('.correct-input').forEach(function (inp) {
        inp.type = (qtype === 'MultipleAnswer') ? 'checkbox' : 'radio';
        if (qtype !== 'MultipleAnswer') inp.checked = false;
    });
}
```

**Validation parity to mirror** (analog `CreateQuestion:6539-6561` — VERIFIED): `scoreValue` 1-100; MC exactly 1 correct (`correctCount != 1`); MA ≥2 correct (`correctCount < 2`); Essay rubrik required. Replicate client-side; service re-validates at commit (Phase 393 `PreflightValidateAsync`).

> ### ⚠ CRITICAL REUSE-vs-REWIRE decision (Pitfall 1 — the single most important finding)
> The authoring **backend** `CreateQuestion` (analog `AssessmentAdminController.cs:6493-6561`, VERIFIED) is `[HttpPost]`, requires a **persisted `packageId`** (`<input type="hidden" name="packageId">`, view `:124`), writes to DB immediately, and `RedirectToAction("ManagePackageQuestions")` (full page reload). The view's authoring `<form>` posts to `asp-action="CreateQuestion"` (`:122`, VERIFIED).
>
> **This is architecturally incompatible with D-07 (no DB write) and D-01 (single-form wizard).** Therefore:
> - **REUSE:** the question-form *markup* (field names/IDs/validation) + the `applyQTypeSwitch` type-toggle JS.
> - **REWIRE (do NOT reuse):** the submit path. On "Tambah Soal", read the form fields into a JS object `{questionText, questionType, scoreValue, elemenTeknis, rubrik, options:[{text,isCorrect}], tempId}`, push to an in-memory `questions[]` array, render into the "Daftar Soal" list, clear the form. On wizard submit, serialize `questions[]` into hidden form fields (or one hidden JSON input) bound to `InjectAssessmentViewModel.Questions`. **Never POST to `CreateQuestion`.**
> - **OMIT entirely:** image-upload blocks (`questionImage`/`optionAImage`... + FileReader JS, analog `:144-169, 190-210`) — `InjectQuestionSpec`/`InjectOptionSpec` have NO image field (`InjectAssessmentDtos.cs:6-25`, VERIFIED). Also drop the `maxCharacters` Essay field unless mapped (DTO has no such field). Also OMIT the edit-mode / `editQuestionId` / type-change-warning-modal machinery unless the planner wants in-flow edit.

---

### `ViewModels/InjectAssessmentViewModel.cs` (view-model, transform)

**Analog (target contract):** `Models/InjectAssessmentDtos.cs` (`InjectRequest` + nested specs, VERIFIED full read)

The VM's POST shape should map 1:1 to `InjectRequest` (field map under the controller assignment above). Recommended shape: bound scalar properties for Setup (Title/Category/AssessmentType/CompletedAt/DurationMinutes/PassPercentage/AllowAnswerReview), `CertMode` + cert fields, `List<string> UserIds`, and `List<InjectQuestionVM> Questions` (or a single hidden JSON string deserialized in the controller). The nested question/option shape must carry: `QuestionText, QuestionType, ScoreValue, Order, ElemenTeknis, Rubrik, TempId, Options[{OptionText, IsCorrect, TempId}]` to match `InjectQuestionSpec`/`InjectOptionSpec`. `Workers[].Answers` is left EMPTY in 394.

**No image fields, no `maxCharacters`, no Proton fields** — keep the VM lean to the DTO surface.

---

### `Views/Admin/Index.cshtml` (MODIFIED — add Section-C card)

**Analog (self):** existing Section-C cards `Index.cshtml:179-264` (VERIFIED)

Insert ONE new `col-md-4` block inside the Section-C `<div class="row g-3 mb-2">` (which spans `:179-264`; add the new card just before the closing `</div>` at `:264`, after the "Certificate Renewal" card). Copy the sibling card pattern verbatim:
```html
<!-- Source pattern: Index.cshtml:180-194 [VERIFIED] -->
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("InjectAssessment", "InjectAssessment")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-clipboard-plus fs-5 text-primary"></i>
                    <span class="fw-bold">Inject Assessment Manual</span>
                </div>
                <small class="text-muted">Catat hasil assessment manual (ujian luring/kertas) seakan online — muncul di riwayat, rincian jawaban, dan sertifikat opsional.</small>
            </div>
        </div>
    </a>
</div>
}
```
> Copy text from UI-SPEC line 104. Icon `bi bi-clipboard-plus` (or `bi-pencil-square`) `text-primary fs-5`. **Do NOT touch** the Admin-only "Bulk Import Nilai (Excel)" card in Section D (`:290-303`) — it is retired later (Phase 396), not 394.

---

## Shared Patterns

### Authentication / RBAC (server-authoritative)
**Source:** `AssessmentAdminController.cs:649-651, 858-860` (`[Authorize(Roles = "Admin, HC")]` per-action) + `Index.cshtml:180` (`@if (User.IsInRole("Admin") || User.IsInRole("HC"))`)
**Apply to:** EVERY action on `InjectAssessmentController` (GET + POST). The card `@if` in Index.cshtml is cosmetic only — the controller attribute is the real gate. Note the literal `"Admin, HC"` (space after comma) to match the established convention.

### CSRF (anti-forgery)
**Source:** `CreateAssessment.cshtml:103` (`@Html.AntiForgeryToken()` inside the single `<form>`) + `AssessmentAdminController.cs:860` (`[ValidateAntiForgeryToken]` on POST)
**Apply to:** the wizard `<form>` (token) + the POST commit action (attribute). `CheckTitleAvailability` is `[HttpGet]` read-only → NO token (analog `:843-846`).

### XSS / output encoding
**Source:** `CreateAssessment.cshtml:1096` (`populateSummary` uses `.textContent`, not `innerHTML`, for user data)
**Apply to:** all confirm-step summary rendering and any client-side rendering of question text / title / option text. Razor auto-encodes by default — do NOT `@Html.Raw` user input.

### Title-duplicate check (reuse, don't rebuild)
**Source:** `AssessmentAdminController.cs:842-855` (endpoint) + `AdminBaseController.cs:271-293` (`NormalizeTitleForDup`/`FindTitleDuplicatesAsync` static helpers) + `CreateAssessment.cshtml:1250-1283` (client fetch render)
**Apply to:** the "Cek Judul" button — point `data-check-url` at the existing `AssessmentAdmin` endpoint; reuse the client render JS verbatim.

### Worker feed + picker
**Source:** `AssessmentAdminController.cs:653-668` (ViewBag feed) + `CreateAssessment.cshtml:271-349, 1439-1498` (markup + JS)
**Apply to:** step-2. Reuse verbatim minus Proton/renewal branches.

### Commit orchestration (Phase 393, already built)
**Source:** `Services/InjectAssessmentService.cs` (registered `Program.cs:57`) consuming `Models/InjectAssessmentDtos.cs:InjectRequest`
**Apply to:** controller DI + the POST commit (wired Phase 395, not 394). The UI must NEVER compute score/pass/cert — the service does it for byte-identical online parity.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none) | — | — | All four files have strong existing analogs. The only NET-NEW behaviors are (a) the 3-mode cert radio toggle JS — but its card *styling* mirrors `CreateAssessment.cshtml:556-602` — and (b) the client-state authoring capture JS, which has NO direct analog because the existing authoring is server-POST-per-question (this is the deliberate REWIRE, not a missing analog). Use RESEARCH.md Pattern 3/4 for the rewired JS shape. |

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdminController, AdminBaseController), `Views/Admin/` (CreateAssessment, ManagePackageQuestions, Index), `ViewModels/` + `Models/` (InjectAssessmentDtos), `Services/InjectAssessmentService.cs`, `Program.cs`.
**Files scanned (read + verified):** 8 (AssessmentAdminController.cs §1-65/643-872/6474-6563, AdminBaseController.cs §268-296, CreateAssessment.cshtml §76-99/271-349/556-602/877-937, ManagePackageQuestions.cshtml §1-60/116-260/390-418, Index.cshtml §174-303, InjectAssessmentDtos.cs full, InjectAssessmentService.cs §347-350, Program.cs §50-61).
**Verification:** All RESEARCH.md line citations re-checked against live code on `main`. All accurate (the Section-C card "add after 263" = end of the Section-C `row g-3` div, confirmed; `CreateQuestion` true line `6500` of signature with `[HttpPost]` decorator at `6493`, confirmed).
**Pattern extraction date:** 2026-06-17
