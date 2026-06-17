# Phase 394: Page + Setup Room + authoring soal - Research

**Researched:** 2026-06-17
**Domain:** ASP.NET Core 8 MVC + Razor + Bootstrap 5 wizard UI (brownfield reuse of CreateAssessment + ManagePackageQuestions); controller→InjectAssessmentService seam
**Confidence:** HIGH (all claims verified against live code in this working tree)

<user_constraints>
## User Constraints (from 394-CONTEXT.md)

### Locked Decisions
- **D-01:** Page = wizard multi-langkah mirror `CreateAssessment` — `nav-pills` (`#wizardStepNav`) + `step-panel` + **satu `<form>`** membungkus semua langkah; reuse pola JS `showStep()`/pill-state. Bukan single-page-scroll, bukan accordion.
- **D-02:** Urutan **6 langkah**: 1. Setup Room → 2. Pilih Pekerja → 3. Authoring Soal → 4. Sertifikat → 5. [Jawaban per-pekerja] → 6. Konfirmasi. Pekerja **sebelum** Soal. **Langkah 5 (Jawaban) = disisipkan Phase 395** — di 394 cukup placeholder/kerangka + navigasi.
- **D-03 (Setup):** Field mirror `CreateAssessment`: judul, kategori, tipe (`Standard`/`PreTest`/`PostTest`), jadwal/`CompletedAt` backdate ≤ hari ini, durasi, `PassPercentage`, `AllowAnswerReview` (default `true`), tombol "Cek" judul reuse `GET /Admin/CheckTitleAvailability`.
- **D-04 (Authoring):** **Inline authoring SAJA** — tidak ada opsi pilih/clone paket existing. Field identik `ManagePackages`/`ManagePackageQuestions`: tipe MC/MA/Essay + opsi + `IsCorrect` + `ScoreValue` + `ElemenTeknis` + `Rubrik`. Mekanisme embedding = researcher/planner discretion; **wajib nol-duplikasi semantik** (field & validasi soal identik). Paket draft dibentuk di flow (state form), tak commit DB sampai final.
- **D-05 (Worker picker):** **Reuse picker `CreateAssessment` step-2 apa-adanya** (filter Section + search "Cari nama atau email…" + Pilih/Batalkan Semua + checkbox `name="UserIds"` + panel "Peserta Terpilih"). NIP tak dikenal mustahil by-construction.
- **D-06 (Sertifikat):** UX toggle = **radio 3-mode** (Auto / Manual / Tanpa) + field kondisional. Auto → preview format `KPB/xxx/{ROMAN}/{year}` + `ValidUntil` + checkbox Permanent. Manual → input `NomorSertifikat` (wajib unik) + `ValidUntil` + Permanent. Tanpa → tak ada field. `ValidUntil` `null` = permanent.
- **D-07 (Persistence):** Semua data 394 **ditahan di state form/session** sampai commit final — **tak ada write DB / draft DB di 394**. Patuh 0-migration. End-state = wizard langkah 1-4 fungsional + kerangka langkah 5 & 6. **Commit inject aktual** (panggil `InjectAssessmentService`) terjadi **setelah 395**. Tidak ada fitur "simpan draft & lanjut nanti".

### Claude's Discretion (teknis)
- **Mekanisme reuse authoring soal:** extract shared partial vs replikasi markup+JS inline vs embed. Pilihan = planner; **wajib field/validasi identik** ManagePackages.
- **Bentuk holding form-state:** hidden JSON field vs view-model bound vs JS in-memory model serialize-on-submit. Discretion.
- **Kontrak controller→`InjectAssessmentService`:** ikut signature/DTO dari Phase 393.
- Penamaan/ikon tiap langkah, styling kartu, copy notice, debounce tombol Cek-judul, wiring DI controller.
- Penanganan langkah 5 placeholder (disabled pill vs panel kosong).

### Deferred Ideas (OUT OF SCOPE)
- **Clone/pilih paket soal existing** sebagai basis authoring — ditolak 394 (inline-only).
- **Draft tersimpan DB** (tutup & lanjut nanti) — ditolak.
- **Import Excel** = Phase 396. **Jawaban per-pekerja + auto-generate** = Phase 395 (langkah 5). **Link Pre/Post ke room existing** = Phase 397.
- **Single-page-scroll / accordion** layout — ditolak.
- **Toggle cert dropdown / switch-2-tingkat** — ditolak; radio 3-mode.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INJ-03 | Admin & HC buka `/Admin/InjectAssessment` dari Kelola Data (Section C), RBAC `Admin,HC` | RBAC pattern = `[Authorize(Roles = "Admin, HC")]` per-action (verified `AssessmentAdminController.cs:650,845,859`); Section C card pattern (verified `Index.cshtml:180-194`) |
| INJ-04 | Setup room mirror CreateAssessment (judul/kategori/tipe/backdate/durasi/PassPercentage/AllowAnswerReview) + Cek judul | Field markup verified `CreateAssessment.cshtml:118-268, 375-419, 507-514, 610-616`; `CheckTitleAvailability` verified `:842-855` |
| INJ-05 | Authoring MC/MA/Essay + opsi + IsCorrect + ScoreValue + ElemenTeknis + Rubrik | Authoring form + JS verified `ManagePackageQuestions.cshtml:116-260, 329-614`; backend `CreateQuestion` verified `:6500-6512`. **CRITICAL: server-POST-per-question model conflicts with D-07 — see Pitfall 1** |
| INJ-06 | Worker picker (NIP wajib ada) | Picker markup + JS verified `CreateAssessment.cshtml:271-349, 1439-1498`; ViewBag feed verified `:653-662` |
| INJ-07 | Toggle sertifikat 3-mode (auto/manual/tanpa) | `InjectCertMode` enum exists (`Models/InjectAssessmentDtos.cs:4`); cert card style verified `CreateAssessment.cshtml:556-602`; `CertNumberHelper.Build` for preview |
</phase_requirements>

## Summary

Phase 394 builds a 6-step wizard page that mirrors `CreateAssessment.cshtml` and reuses the authoring components of `ManagePackageQuestions.cshtml`, all driving toward an `InjectRequest` DTO that the **already-built** `InjectAssessmentService` (Phase 393) will consume. The single most important finding: **the existing authoring backend (`CreateQuestion`) is a server-POST-per-question that requires a real persisted `packageId` and reloads the page after each save** — this is architecturally incompatible with D-07's "hold all data in form state, zero DB write until final commit." Therefore the authoring step CANNOT call `CreateQuestion`; it must capture questions into client-side state (the wizard's single `<form>`) and only materialize them inside `InjectAssessmentService.InjectBatchAsync` at commit time (which happens in Phase 395, not 394).

The good news: the commit contract is already fully defined. `InjectRequest` (verified `Models/InjectAssessmentDtos.cs`) carries `Title/Category/AssessmentType/CompletedAt/StartedAt/Schedule/DurationMinutes/PassPercentage/AllowAnswerReview/CertMode/Questions[]/Workers[]`. Phase 394 captures **everything except `Workers[].Answers`** (filled by Phase 395). The service is registered in DI (`Program.cs:57`) and `CheckTitleAvailability`, the worker picker ViewBag feed, RBAC, and the Section-C card pattern are all verified and ready to mirror verbatim.

The app uses `AddControllersWithViews()` WITHOUT `AddRazorRuntimeCompilation` → views are embedded at build time. Runtime wizard/picker/toggle behavior MUST be verified with Playwright (lesson Phase 354/392) — and the app MUST be built+run from this main working tree, not the ITHandoff sibling worktree (lesson 392).

**Primary recommendation:** Mirror `CreateAssessment.cshtml`'s wizard scaffold (nav-pills + step-panel + single `<form>` + `goToStep`/`updatePills`/`validateStep`/`populateSummary` JS, extended from 4→6 steps). Reuse the worker picker (D-05) and `CheckTitleAvailability` verbatim. For authoring (D-04), **extract a shared partial of the question-form markup** but rewire its JS to append questions to client-side state and serialize them into hidden form fields on submit — do NOT reuse the `CreateQuestion` server POST. Build a `ViewModels/InjectAssessmentViewModel.cs` whose POST shape maps cleanly to `InjectRequest`. Leave step 5 (Jawaban) as a navigable placeholder panel and gate the final `btn-success` commit on step-5 completion so Phase 395 slots in without refactor.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| RBAC gate (Admin,HC) | API/Backend (Controller `[Authorize]`) | Browser (card visibility) | Server-authoritative authz; UI gating is cosmetic only (Pitfall 6) |
| Section-C card render | Frontend Server (Razor) | — | `Index.cshtml` server-rendered, role-gated `@if (User.IsInRole...)` |
| Wizard step nav + per-step validation | Browser (vanilla JS) | — | Client UX; server re-validates at commit (Phase 395) |
| Setup room fields | Frontend Server (Razor form) + Browser (date max=today) | API (model bind) | Inputs bind to InjectAssessmentViewModel on POST |
| Title duplicate check ("Cek") | API/Backend (`CheckTitleAvailability` JSON) | Browser (fetch + render) | Read-only DB query server-side; client renders result |
| Worker picker (search/filter/select) | Browser (client-side filter) | Frontend Server (ViewBag.Users feed) | All users loaded server-side; filter/select is pure client JS |
| Authoring question capture | Browser (in-memory JS state → hidden fields) | — | **NOT server POST** — held in form state per D-07 |
| Certificate mode + preview | Browser (radio toggle + format preview) | — | Format preview is display-only; real cert allocation = service (393) |
| Inject commit (build sessions/grade/cert) | API/Backend (`InjectAssessmentService`) + Database | — | Phase 395 wires the actual call; 394 only prepares the request |

## Standard Stack

### Core (verified — already in repo, do NOT add new packages)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controllers + Razor views | `[VERIFIED: HcPortal.csproj:4 TargetFramework=net8.0]`; established app pattern |
| EF Core (SqlServer) | 8.0.0 | DbContext (used only at commit, Phase 395) | `[VERIFIED: HcPortal.csproj:18]` |
| Bootstrap 5 | vendored | card, nav-pills, form-check, form-switch, input-group, badge, alert, modal | `[VERIFIED: 394-UI-SPEC.md + wwwroot/lib/bootstrap]`; FIXED stack, no new UI lib |
| Bootstrap Icons | 1.10.0 (CDN) | `bi bi-*` icons | `[VERIFIED: 394-UI-SPEC.md]` |
| Playwright | (tests/) | Runtime verification of wizard/picker/toggle | `[VERIFIED: tests/playwright.config.ts exists]`; mandatory per Phase 354 lesson |
| xUnit | net8.0 | ViewModel→InjectRequest mapping unit tests | `[VERIFIED: HcPortal.Tests/ exists, 492 tests]` |

### Supporting (verified — reused, no new code)
| Asset | Location | Purpose | When to Use |
|-------|----------|---------|-------------|
| `InjectAssessmentService` | `Services/InjectAssessmentService.cs` | Commit orchestrator (Phase 393, BUILT) | DI-inject into new controller; call at commit (Phase 395) |
| `InjectRequest` + DTOs | `Models/InjectAssessmentDtos.cs` | Commit contract | ViewModel POST maps to this |
| `CheckTitleAvailability` | `AssessmentAdminController.cs:846` | Title dup check JSON | "Cek Judul" button reuses verbatim |
| `FindTitleDuplicatesAsync` / `NormalizeTitleForDup` | `AdminBaseController.cs:271-293` | static helpers | already wired inside CheckTitleAvailability |
| `CertNumberHelper.Build(seq, date)` | `Helpers/CertNumberHelper.cs` | `KPB/{seq:D3}/{ROMAN}/{year}` | Auto-mode preview format string (client can hardcode pattern preview) |
| Worker picker markup+JS | `CreateAssessment.cshtml:271-349, 1439-1498` | search/filter/select | Reuse verbatim (D-05) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Client-state authoring (hidden fields) | Reuse `CreateQuestion` server POST | REJECTED — `CreateQuestion` needs real packageId + page reload → violates D-07 (no DB write) + breaks single-form wizard |
| Extract shared partial for question form | Replicate markup inline | Partial = less drift but the JS must be rewired regardless; partial recommended for the *markup*, custom JS for state capture (see Pattern 3) |
| `InjectAssessmentViewModel` bound POST | Single hidden JSON blob | Bound model = ASP.NET validation + cleaner mapping; JSON blob simpler for nested questions. Hybrid recommended (see Pattern 4) |

**Installation:** None. Zero new packages. `InjectAssessmentService` already registered: `[VERIFIED: Program.cs:57 builder.Services.AddScoped<HcPortal.Services.InjectAssessmentService>()]`.

## Architecture Patterns

### System Architecture Diagram

```
[Admin/HC browser]
       │  GET /Admin/InjectAssessment  ([Authorize(Roles="Admin, HC")])
       ▼
┌─────────────────────────────────────────────────────────────┐
│ InjectAssessmentController.InjectAssessment() [GET]           │
│  - populate ViewBag.Users / ViewBag.Sections / ViewBag.Categories │
│    (SAME feed as CreateAssessment GET :653-668)               │
│  - return View(new InjectAssessmentViewModel())               │
└───────────────────────────┬───────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ InjectAssessment.cshtml  — ONE <form>, 6 step-panels          │
│  Step1 Setup ─► Step2 Pekerja ─► Step3 Soal ─► Step4 Cert     │
│        ─► Step5 [Jawaban placeholder] ─► Step6 Konfirmasi      │
│                                                                │
│  vanilla JS WizardController: goToStep/updatePills/            │
│  validateStep/populateSummary  (ported from CreateAssessment) │
│                                                                │
│  ── "Cek Judul" ──► fetch GET /Admin/CheckTitleAvailability ──►│ (read-only, JSON)
│  ── authoring ──► append question to JS state[] ──► hidden fields│ (NO server POST)
│  ── worker picker ──► checkbox name="UserIds" (client filter)  │
└───────────────────────────┬───────────────────────────────────┘
                            │  POST (final commit) — WIRED IN PHASE 395, not 394
                            ▼
┌─────────────────────────────────────────────────────────────┐
│ InjectAssessmentController.InjectAssessment(VM) [POST]        │
│  - map InjectAssessmentViewModel ─► InjectRequest             │
│    (Workers[].Answers EMPTY until Phase 395)                  │
│  - call _injectService.InjectBatchAsync(req, actorId, name)   │
└───────────────────────────┬───────────────────────────────────┘
                            ▼
        InjectAssessmentService (Phase 393, BUILT)
        ─► GradingService ─► CertNumberHelper ─► DB (atomic tx)
                            ▼
        Sessions render at /CMP/Records ("Assessment Online")
        + /CMP/Results (per-soal) — NO branch on AssessmentType (verified)
```

> Phase 394 deliverable stops at the GET page + the 6-step form that captures everything for `InjectRequest` EXCEPT `Workers[].Answers`. The POST handler may exist as a stub/scaffold in 394, but the actual `InjectBatchAsync` call is wired in Phase 395 (D-07: "commit inject aktual terjadi setelah 395").

### Recommended File Structure (all NEW unless noted)
```
Controllers/
└── InjectAssessmentController.cs   # NEW — [Authorize(Roles="Admin, HC")], [Route("Admin/[action]")]
Views/Admin/
├── InjectAssessment.cshtml         # NEW — 6-step wizard
└── _InjectQuestionForm.cshtml      # NEW (optional) — shared authoring form partial (markup only)
ViewModels/
└── InjectAssessmentViewModel.cs    # NEW — POST shape → maps to InjectRequest
Views/Admin/Index.cshtml            # EDIT — add Section-C card (one col-md-4 block)
```

> Mirror the AssessmentAdminController View-resolution trick if controller name ≠ "Admin": `AssessmentAdminController.cs:58-60` overrides `View()` to resolve `~/Views/Admin/{action}.cshtml`. The new controller's views live in `Views/Admin/`, so either name the controller's view folder accordingly or add the same `protected new ViewResult View()` override. `[VERIFIED: AssessmentAdminController.cs:58-60]`

### Pattern 1: Wizard scaffold (mirror, extend 4→6 steps)
**What:** `nav nav-pills nav-fill gap-2 #wizardStepNav` with N `<li><button id="pill-n">`, N `.step-panel` divs (`d-none` toggle), one `<form>`, and a `WizardController` IIFE.
**When:** The whole page (D-01).
**Key JS functions to port** (from `CreateAssessment.cshtml:877-1548`, verified):
- `goToStep(n)` — hide all `.step-panel`, show `#step-n`, `updatePills()`, scroll top; call `populateSummary()` when reaching confirm step.
- `updatePills()` — loop pills: active=`bg-primary text-white active` + `bi-circle-fill`; completed (`visited && i<current`)=`bg-success text-white` + `bi-check-circle-fill`; visited=`text-primary border border-primary`; pending=`text-muted border` disabled.
- `validateStep(n)` — per-step client validation, add `is-invalid`, show step-error alert; return bool.
- `populateSummary()` — fill confirm-step summary `<span>`s.
- Event wiring: `btnNext{n}` → `if (validateStep(n)) goToStep(n+1)`; `btnPrev{n}` → `goToStep(n-1)`; `.edit-from-confirm` (sets `returnToConfirm=true`, jumps to step); `.btnBackToConfirm`; pill clicks (only if `visitedSteps.has(n)`).
**Adaptation for 6 steps:** change loop bounds `for (var i=1; i<=6; i++)`, add `btnNext4/btnNext5`, `btnPrev5/btnPrev6`, and the step-5 placeholder advances without validation (or validates "soal+pekerja done" — see Pattern 5).

### Pattern 2: Worker picker (reuse verbatim — D-05)
**What:** filter bar (`#sectionFilter`, `#userSearchInput`, `#selectAllBtn`, `#deselectAllBtn`, `#selectedCountBadge`) + `#userCheckboxContainer` (checkbox `name="UserIds"`, `max-height:320px`) + "Peserta Terpilih" panel (`role="status" aria-live="polite"`).
**Controller feed (mirror `CreateAssessment` GET `:653-662`):**
```csharp
// Source: AssessmentAdminController.cs:653-662 [VERIFIED]
var users = await _context.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.FullName)
    .Select(u => new { u.Id, FullName = u.FullName ?? "", Email = u.Email ?? "", Section = u.Section ?? "" })
    .ToListAsync();
ViewBag.Users = users;
ViewBag.SelectedUserIds = new List<string>();
ViewBag.Sections = await _context.GetAllSectionsAsync();
```
**JS to port:** `applyFilters()`, select/deselect-all, `updateSelectedCount()`, `renderSelectedParticipants()` (`CreateAssessment.cshtml:1439-1498, 1558-1653`). **Strip the Proton branch** (`applyProtonMode`, `#protonUserCheckboxContainer`) — inject has no Proton mode.
**Note:** the picker markup contains a Proton-eligible sub-section (`#protonEligibleSection`) and Proton track field — OMIT these for inject (not in scope).

### Pattern 3: Authoring soal (extract shared partial, REWIRE the JS — D-04, Discretion)
**What:** The question form markup (`ManagePackageQuestions.cshtml:116-260`) — `QuestionType` select, `questionText` textarea, options A-D (`input-group` + radio/checkbox `correctA..D` + `optionA..D` text), `rubrik` (Essay), `scoreValue` (1-100), `elemenTeknis`.
**CRITICAL — do NOT reuse the backend:** `CreateQuestion` (`:6500`) is `[HttpPost]`, takes a real `packageId`, persists immediately, and `RedirectToAction("ManagePackageQuestions")` (full reload). This is incompatible with D-07 and the single-form wizard.
**Recommended mechanism:**
1. Extract the form *markup* into `_InjectQuestionForm.cshtml` (or replicate inline) — keep field names/validation IDENTICAL for nol-duplikasi semantik.
2. Rewire JS: on "Tambah Soal", read the form fields into a JS object `{questionText, questionType, scoreValue, elemenTeknis, rubrik, options:[{text,isCorrect}], tempId}`, push to an in-memory `questions[]` array, render into the left "Daftar Soal" list, and clear the form.
3. On wizard submit, serialize `questions[]` into hidden form fields (or a single hidden JSON input) so they bind to `InjectAssessmentViewModel.Questions`.
4. Mirror the type-toggle JS (`applyQTypeSwitch`, `ManagePackageQuestions.cshtml:402-418`): Essay → hide options, show rubrik; MA → checkbox correct-inputs + "≥2 benar" notice; MC → radio.
**Validation to mirror (from `CreateQuestion:6539-6558`):** scoreValue 1-100; MC exactly 1 correct; MA ≥2 correct; Essay rubrik required. Replicate client-side AND re-validate at service level (Phase 393 `PreflightValidateAsync` already enforces MC=1/MA≥1/Essay-rules — note service uses MA≥1 for *answers* but authoring should keep MA≥2 *correct options* per existing UI rule).
**OMIT for 394:** image upload (`questionImage`, `optionAImage`...) — inject DTO (`InjectQuestionSpec`/`InjectOptionSpec`) has NO image field (`[VERIFIED: Models/InjectAssessmentDtos.cs:7-25]`). Authoring images are out of scope; drop the entire img-drop blocks and FileReader JS.

### Pattern 4: ViewModel → InjectRequest mapping
**What:** `InjectAssessmentViewModel` holds POST inputs; controller maps to `InjectRequest` (the service contract).
**Field map (verified against `Models/InjectAssessmentDtos.cs`):**
```
VM.Title          → InjectRequest.Title
VM.Category       → InjectRequest.Category
VM.AssessmentType → InjectRequest.AssessmentType  // "Standard"/"PreTest"/"PostTest" (NOT "Manual")
VM.CompletedAt    → InjectRequest.CompletedAt      // backdate ≤ today (D-06)
VM.DurationMinutes→ InjectRequest.DurationMinutes
VM.PassPercentage → InjectRequest.PassPercentage
VM.AllowAnswerReview → InjectRequest.AllowAnswerReview (default true)
VM.CertMode (None/Auto/Manual radio) → InjectRequest.CertMode (InjectCertMode enum)
VM.Questions[]    → InjectRequest.Questions (InjectQuestionSpec: QuestionText/QuestionType/ScoreValue/Order/ElemenTeknis/Rubrik/TempId/Options[])
VM.UserIds[] + per-worker cert fields → InjectRequest.Workers (InjectWorkerSpec: Nip/ManualCertNumber/CertValidUntil + Answers EMPTY in 394)
```
> **NIP vs UserId note:** `InjectWorkerSpec.Nip` keys on `ApplicationUser.NIP` (service resolves NIP→user via `_context.Users.Where(u => u.NIP...)`, `InjectAssessmentService.cs:348-350`). The worker picker checkbox value is `user.Id` (`name="UserIds"`). The 394 controller must translate selected `UserIds` → their `NIP` when building `InjectWorkerSpec`, OR the planner may add a parallel ViewModel field. **Flag for planner.** `[VERIFIED: picker uses user.Id (CreateAssessment.cshtml:319); service keys on NIP (InjectAssessmentService.cs:348)]`

### Pattern 5: Step-5 placeholder seam (D-02, D-07 — design for Phase 395)
**What:** Step 5 (Jawaban) is a navigable panel with an `alert-info` notice, no inputs yet.
**Recommendation:** Render the pill + panel for step 5 exactly like the others, but the panel body = the placeholder notice (`alert-info` + `bi-hourglass-split`, copy in UI-SPEC line 123). `validateStep(5)` returns true (or checks soal+pekerja done). Gate the final commit button (step 6) on step-5 completion so when Phase 395 fills step 5, no scaffold refactor is needed. Keep `btn-success` "Inject Assessment" present in step 6 but its POST→service call is wired in 395.

### Anti-Patterns to Avoid
- **Reusing `CreateQuestion`/`DeleteQuestion`/`EditQuestion` server POSTs for authoring** — they persist to DB immediately + reload page → violates D-07 + breaks single-form wizard.
- **Carrying image-upload markup into the inject question form** — inject DTO has no image fields; dead complexity.
- **Branching CMP/Results or GetUnifiedRecords on `AssessmentType=="Manual"`** — there is NO such branch and inject sets `AssessmentType` to Standard/PreTest/PostTest specifically so it renders like online. Do not add one. `[VERIFIED: no "Manual" branch in CMPController.cs; GetUnifiedRecords filters only UserId+Status, WorkerDataService.cs:28-47]`
- **Gating RBAC only in the view** (`@if User.IsInRole`) — must ALSO put `[Authorize(Roles="Admin, HC")]` on every controller action (server-authoritative). The card `@if` is cosmetic.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Title duplicate check | Custom query | `GET /Admin/CheckTitleAvailability` + its client fetch JS | Already returns `{exists, groupCount, matches[]}`; client render code exists `CreateAssessment.cshtml:1250-1283` `[VERIFIED]` |
| Cert number format preview | Custom string concat scattered | `CertNumberHelper.Build` pattern (display only as `KPB/xxx/{ROMAN}/{year}`) | Single source of cert format; preview is cosmetic, real allocation in service |
| Worker list + search/filter | New endpoint + JS | Reuse `ViewBag.Users` feed + picker JS (D-05) | Verbatim reuse, zero new code |
| Score/pass/cert computation | Any client/controller math | `InjectAssessmentService` → `GradingService` (Phase 393) | Nol-duplikasi; byte-identik online. 394 only captures input |
| Wizard step state machine | New framework | Port `WizardController` IIFE from CreateAssessment | Proven pattern, 4→6 step extension is mechanical |

**Key insight:** The entire "compute" half of inject is DONE (Phase 393). Phase 394 is pure input-capture UI. Every number (score, pass, cert) is computed by the reused engine at commit — the UI must never calculate them.

## Runtime State Inventory

> Phase 394 is a NEW page (greenfield UI on existing schema). No rename/refactor/migration. Verified categories:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — 394 writes NOTHING to DB (D-07; commit deferred to 395) | None |
| Live service config | None — no external service touched | None |
| OS-registered state | None | None |
| Secrets/env vars | None new. (Existing: `Authentication__UseActiveDirectory=false` for local Playwright — unchanged) | None |
| Build artifacts | App embeds views at build (no `AddRazorRuntimeCompilation`) → after editing `.cshtml`, MUST `dotnet build` + run from MAIN tree to see changes (Pitfall 4) | Rebuild before Playwright |

## Common Pitfalls

### Pitfall 1: Authoring backend cannot be reused as-is (server-POST conflicts with D-07)
**What goes wrong:** A plan assumes "reuse ManagePackageQuestions authoring" means calling `CreateQuestion`. That action requires a persisted `packageId`, writes to DB immediately, and `RedirectToAction` (full page reload) — breaking both the single-form wizard (D-01) and the no-DB-write rule (D-07).
**Why it happens:** "Reuse authoring" is ambiguous; the existing authoring IS server-round-trip per question.
**How to avoid:** Reuse the *markup + field semantics + type-toggle JS*, but capture questions into client-side state → hidden form fields → mapped to `InjectRequest.Questions` only at commit (Pattern 3). `[VERIFIED: CreateQuestion is [HttpPost] with RedirectToAction, AssessmentAdminController.cs:6500,6519]`
**Warning signs:** Plan task says "POST to CreateQuestion" or "create draft package in step 3."

### Pitfall 2: UserId vs NIP mismatch at the picker→service boundary
**What goes wrong:** Picker checkboxes carry `user.Id`; `InjectWorkerSpec` keys on `Nip`. Building workers from `UserIds` directly fails NIP-resolution in the service preflight.
**Why it happens:** Two different identifiers for the same user.
**How to avoid:** In the 394 controller, translate selected `UserIds` → each user's `NIP` when constructing `InjectWorkerSpec` (or add a hidden NIP-per-user map). Flag for planner. `[VERIFIED: name="UserIds" value="@user.Id" CreateAssessment.cshtml:319; service .Where(u => u.NIP...) InjectAssessmentService.cs:348]`
**Warning signs:** Preflight returns "NIP {id} tidak ditemukan" for valid users.

### Pitfall 3: Razor + JS runtime behavior not verified (grep+build insufficient)
**What goes wrong:** Wizard pill state, step nav, MC/MA/Essay field toggle, cert-radio conditional fields look correct in source but fail at runtime.
**Why it happens:** Dynamic Razor + vanilla JS; static review misses runtime wiring bugs.
**How to avoid:** Playwright e2e (login fixture, AD-off, `--workers=1`) asserting: RBAC redirect, 6-pill nav, Cek-judul, picker search/select, type-toggle, cert-radio toggle, step-5 placeholder. Lesson Phase 354/387/392. `[CITED: STATE.md decisions 387/392; UI-SPEC.md:30]`
**Warning signs:** "build passes so it works" reasoning.

### Pitfall 4: Stale binary / wrong working tree (lesson Phase 392)
**What goes wrong:** App runs from the ITHandoff sibling worktree (`PortalHC_KPB-ITHandoff`) or a stale binary; edited `.cshtml` changes don't appear because views are embedded at build (no runtime compilation). Playwright then "verifies" the old markup.
**Why it happens:** `AddControllersWithViews()` without `AddRazorRuntimeCompilation`.
**How to avoid:** `dotnet build HcPortal.csproj` (0 error) in the MAIN tree → run `HcPortal.exe`/`dotnet run` from MAIN tree on :5277 AD-off, THEN run Playwright. `[VERIFIED: STATE.md:41,119; UI-SPEC.md:30]`
**Warning signs:** New page returns 404, or new markup absent at runtime.

### Pitfall 5: Forgetting `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` on the commit POST
**What goes wrong:** CSRF-vulnerable POST, or POST rejected.
**Why it happens:** Wizard has one `<form>`; easy to omit the token or the attribute.
**How to avoid:** Mirror `CreateAssessment.cshtml:103` (`@Html.AntiForgeryToken()` inside form) + POST action `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]` (mirror `:858-860`). `CheckTitleAvailability` is `[HttpGet]` read-only → no token (mirror `:843-846`). `[VERIFIED]`

## Code Examples

### RBAC + route on the new controller (mirror verbatim)
```csharp
// Source: AssessmentAdminController.cs:19-20, 649-651, 858-860 [VERIFIED]
[Route("Admin/[action]")]
public class InjectAssessmentController : AdminBaseController   // or Controller; AdminBaseController gives NormalizeTitleForDup etc.
{
    [HttpGet]
    [Authorize(Roles = "Admin, HC")]            // note the SPACE after comma — match existing
    public async Task<IActionResult> InjectAssessment() { /* populate ViewBag.Users/Sections/Categories; return View(vm) */ }

    [HttpPost]
    [Authorize(Roles = "Admin, HC")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InjectAssessment(InjectAssessmentViewModel vm) { /* map→InjectRequest; call in PHASE 395 */ }
}
```

### Section-C card (add to Index.cshtml after line 263)
```html
<!-- Source: Index.cshtml:180-194 pattern [VERIFIED] -->
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
                <small class="text-muted">Catat hasil assessment manual (luring/kertas) seakan online — muncul di riwayat, rincian jawaban, sertifikat opsional.</small>
            </div>
        </div>
    </a>
</div>
}
```

### "Cek Judul" client fetch (reuse verbatim)
```javascript
// Source: CreateAssessment.cshtml:1250-1283 [VERIFIED]
// button: <button id="btnCheckTitle" data-check-url="@Url.Action("CheckTitleAvailability","AssessmentAdmin")">
// fetch(url + '?title=' + encodeURIComponent(title), {headers:{'X-Requested-With':'XMLHttpRequest'}})
//   → JSON {exists, groupCount, matches:[{category, tanggal, peserta}]}
//   → render alert-success "Aman" | alert-warning list of matches
```
> The endpoint lives on `AssessmentAdminController`; the new page can point `data-check-url` at `@Url.Action("CheckTitleAvailability","AssessmentAdmin")` — no need to duplicate the endpoint.

### Type-toggle JS (mirror for authoring)
```javascript
// Source: ManagePackageQuestions.cshtml:402-418 [VERIFIED]
function applyQTypeSwitch(qtype) {
    optionsSection.style.display = (qtype === 'Essay') ? 'none' : '';
    rubrikSection.style.display  = (qtype === 'Essay') ? '' : 'none';
    maLabel.style.display        = (qtype === 'MultipleAnswer') ? '' : 'none';
    document.querySelectorAll('.correct-input').forEach(function (inp) {
        inp.type = (qtype === 'MultipleAnswer') ? 'checkbox' : 'radio';
        if (qtype !== 'MultipleAnswer') inp.checked = false;
    });
}
```

## State of the Art

| Old Approach | Current Approach | When | Impact |
|--------------|------------------|------|--------|
| Authoring = separate page per packageId (`ManagePackageQuestions`) | Inline client-state authoring in wizard (this phase) | v32.2/394 | Must rewire, not reuse, the authoring POST |
| BulkBackfill (`TrainingAdminController:836`) for manual entry | InjectAssessment wizard + service | v32.2 | BulkBackfill retired in Phase 396 (not 394) |
| Cert via `DateTime.Now` (online) | Cert via backdate `CompletedAt` (inject, D-12) | Phase 393 | Already handled in service; 394 preview shows `{year}` = ujian year |

**Deprecated/outdated:** Nothing in 394 scope is deprecated. The BulkBackfill tool still exists (`Index.cshtml:290-303`, Admin-only) and is retired later (Phase 396) — do not touch it in 394.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Section-C card icon `bi bi-clipboard-plus` (UI-SPEC suggests this or `bi-pencil-square`) | Code Examples | Cosmetic only — planner/UI-checker picks final icon |
| A2 | New controller named `InjectAssessmentController` with action `InjectAssessment` → route `/Admin/InjectAssessment` via `[Route("Admin/[action]")]` | File Structure | If named differently, route differs; CONTEXT mandates `/Admin/InjectAssessment` so this is low-risk |
| A3 | Authoring should keep MA = ≥2 correct OPTIONS (matching existing `CreateQuestion:6552` UI rule), even though service preflight validates MA answers ≥1 | Pattern 3 | If authoring uses ≥1, drift from established authoring UX; verify with planner. The service rule (≥1 *selected answer*) is a different axis from authoring rule (≥2 *correct options*) |

**Note:** A1-A3 are all low-risk cosmetic/naming/validation-parity items, not architectural unknowns. No compliance/security/retention assumptions.

## Open Questions

1. **UserId→NIP translation ownership**
   - What we know: picker emits `UserIds` (user.Id); service needs `Nip`.
   - What's unclear: whether controller resolves NIP at commit, or VM carries NIP map.
   - Recommendation: controller resolves `UserIds → NIP` at POST when building `InjectWorkerSpec` (simplest, single query). Planner decides.

2. **AssessmentType field for PreTest/PostTest in 394**
   - What we know: `InjectRequest.AssessmentType` accepts "Standard"/"PreTest"/"PostTest"; CreateAssessment uses a `Standard`/`PrePostTest` toggle that splits into two sessions.
   - What's unclear: D-03 lists tipe `Standard`/`PreTest`/`PostTest` as a simple 3-value select. Pre/Post LINKING to a room is Phase 397.
   - Recommendation: In 394, render tipe as a plain 3-option `form-select` (Standard/PreTest/PostTest) feeding `AssessmentType` directly. Do NOT port the CreateAssessment Pre-Post dual-schedule machinery (that's a different feature; linking = 397).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/run | ✓ | 8.0.418 | — |
| SQL Server (SQLEXPRESS) | runtime page render (user/category feeds) + Playwright | ✓ (per prior phases) | — | — |
| Node + Playwright | runtime UI verification | ✓ | `tests/playwright.config.ts` present | — |
| Bootstrap 5 + Icons | UI | ✓ vendored/CDN | 1.10.0 icons | — |
| `InjectAssessmentService` | controller DI | ✓ | registered Program.cs:57 | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None.

## Validation Architecture

> nyquist_validation enabled (config.json workflow.nyquist_validation = true).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (.NET 8, `HcPortal.Tests`) for ViewModel mapping; Playwright (`tests/`) for runtime UI |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/playwright.config.ts` |
| Quick run command | `dotnet test --filter Category!=Integration` |
| Full suite command | `dotnet test` + `cd tests && npx playwright test e2e/inject-assessment-394.spec.ts --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INJ-03 | RBAC: Admin/HC reach page; other role → 403/AccessDenied; Section-C card visible to Admin/HC | e2e (Playwright) | `npx playwright test e2e/inject-assessment-394.spec.ts -g "RBAC"` | ❌ Wave 0 |
| INJ-03 | GET `/Admin/InjectAssessment` returns 200 + wizard renders (6 pills) | e2e | `... -g "wizard nav"` | ❌ Wave 0 |
| INJ-04 | Cek Judul fetch renders available/duplicate; backdate input max=today rejects future | e2e | `... -g "cek judul"`, `... -g "backdate"` | ❌ Wave 0 |
| INJ-05 | Type toggle MC/MA/Essay shows correct fields; "Tambah Soal" appends to list; ≥1 soal required | e2e | `... -g "authoring"` | ❌ Wave 0 |
| INJ-05/06/07 | ViewModel → InjectRequest mapping (Questions/Workers/CertMode correct shape) | unit (xUnit) | `dotnet test --filter "FullyQualifiedName~InjectViewModelMap"` | ❌ Wave 0 |
| INJ-06 | Worker picker search/filter/select; ≥1 required to advance; selected count updates | e2e | `... -g "picker"` | ❌ Wave 0 |
| INJ-07 | Cert radio Auto→preview+ValidUntil+permanent; Manual→NomorSertifikat; Tanpa→hide | e2e | `... -g "cert radio"` | ❌ Wave 0 |
| D-07 | Step-5 placeholder navigable; no DB write (no AssessmentSession row created by browsing 394) | e2e + manual | `... -g "step5 placeholder"` + DB count check | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build HcPortal.csproj` (0 error) + `dotnet test --filter Category!=Integration` (no regression).
- **Per wave merge:** full `dotnet test` + targeted Playwright spec (`--workers=1`, AD-off, MAIN tree).
- **Phase gate:** Playwright spec green + full xUnit suite green + `dotnet ef migrations add _verify` → 0 model diff (confirm 0-migration) then discard, before `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `tests/e2e/inject-assessment-394.spec.ts` — covers INJ-03/04/05/06/07 + step-5 placeholder (login fixture pattern from `assessment-title-flexible.spec.ts`, AD-off, `--workers=1`).
- [ ] `HcPortal.Tests/InjectViewModelMapTests.cs` — unit: `InjectAssessmentViewModel` → `InjectRequest` mapping (Questions[], Workers[], CertMode, AssessmentType, backdate). Pure mapping, no DB → fast, `Category!=Integration`.
- [ ] (If controller resolves UserId→NIP) unit assertion that selected UserIds map to correct NIPs.

*Framework already installed (xUnit + Playwright present) — no install needed.*

## Security Domain

> security_enforcement enabled (absent in config = enabled).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | New controller follows existing `[Authorize]`-per-action pattern; no new trust boundary |
| V2 Authentication | no (reuses app auth) | Existing Identity / AD; page behind `[Authorize]` |
| V3 Session Management | no | No new session state; D-07 = client form state only (non-sensitive) |
| V4 Access Control | **yes** | `[Authorize(Roles="Admin, HC")]` on EVERY action (GET + POST + any AJAX). Card `@if` is cosmetic, not the gate. No IDOR: worker picker only lists existing `AspNetUsers`; no arbitrary-id acceptance beyond existing users (service preflight rejects unknown NIP). |
| V5 Input Validation | **yes** | Client validation (scoreValue 1-100, MC=1/MA≥2 correct, Essay rubrik, backdate≤today) PLUS server re-validation. Backdate guard server-side at commit (service `PreflightValidateAsync` already enforces ≤today, `InjectAssessmentService.cs:354-361`). Never trust client. |
| V6 Cryptography | no | No crypto in 394 (cert number = sequence helper, not secret) |
| V13 API/Web Service | yes (CSRF) | POST commit + any state-changing AJAX → `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()`. `CheckTitleAvailability` GET read-only → no token. |

### Known Threat Patterns for ASP.NET MVC + Razor wizard
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Non-Admin/HC reaches inject page via direct URL | Elevation of Privilege | `[Authorize(Roles="Admin, HC")]` server-side on all actions (not just card visibility) |
| CSRF on commit POST | Tampering | `[ValidateAntiForgeryToken]` + antiforgery token in the single `<form>` (mirror CreateAssessment:103) |
| Future-dated backdate / absurd year injected by tampering client | Tampering | Server preflight `CompletedAt.Date > today || Year < 2000` reject (already in service `:356`); client `max=today` is UX only |
| Out-of-range scoreValue / invalid option / unknown NIP via crafted POST | Tampering | Server preflight reject-all (service D-03, `PreflightValidateAsync`); client validation is convenience |
| Duplicate manual cert number | Tampering / data integrity | Service preflight + UNIQUE index (D-09, already enforced) |
| XSS via question text / title rendered back | Tampering (Injection) | Razor auto-encodes by default; do NOT use `@Html.Raw` on user input. Summary/confirm uses `textContent` (JS) not `innerHTML` for user data (mirror `populateSummary` which uses `.textContent`, CreateAssessment:1096) |

> **Threat-model inputs for planner's `<threat_model>`:** new endpoints = `GET /Admin/InjectAssessment`, `POST /Admin/InjectAssessment` (commit, wired 395), reuse of `GET /Admin/CheckTitleAvailability`. All require Admin/HC. No new IDOR surface (picker = existing users only). Primary controls: server-side RBAC on every action + antiforgery on POST + Razor auto-encoding (no `@Html.Raw` on user strings).

## Sources

### Primary (HIGH confidence — verified in this working tree)
- `Services/InjectAssessmentService.cs` (full read) — commit contract, preflight, NIP keying, cert/backdate handling.
- `Models/InjectAssessmentDtos.cs` (full read) — `InjectRequest`/`InjectQuestionSpec`/`InjectOptionSpec`/`InjectWorkerSpec`/`InjectCertMode`/`InjectResult`.
- `Views/Admin/CreateAssessment.cshtml` (lines 1-1653) — wizard scaffold, worker picker, cert card, all wizard JS.
- `Views/Admin/ManagePackageQuestions.cshtml` (full read) — authoring form markup + type-toggle JS + CreateQuestion form action.
- `Controllers/AssessmentAdminController.cs:1-60, 645-870, 6474-6558` — RBAC attrs, View-resolution override, GET feed, CheckTitleAvailability, CreateQuestion signature.
- `Controllers/AdminBaseController.cs:270-294` — `NormalizeTitleForDup` + `FindTitleDuplicatesAsync`.
- `Views/Admin/Index.cshtml:174-303` — Section-C card pattern + RBAC `@if`.
- `Services/WorkerDataService.cs:28-47` — GetUnifiedRecords (no IsManualEntry filter, labels "Assessment Online").
- `Program.cs:51-57` — DI registration (InjectAssessmentService scoped).
- `HcPortal.csproj:4,14-27` — net8.0, EF Core 8.0.0, ClosedXML 0.105.0, QuestPDF 2026.2.2.
- `.planning/config.json` — workflow flags (nyquist_validation, ui_phase true).
- `tests/e2e/assessment-title-flexible.spec.ts` — Playwright login/AD-off pattern.
- `.planning/phases/393-backend-core-inject/393-VALIDATION.md` — test infra (xUnit + real-SQL integration).
- Grep: no `AssessmentType == "Manual"` branch in CMPController.cs (verified negative claim).

### Secondary (MEDIUM)
- `.planning/STATE.md` decisions 387/392 — Razor+JS Playwright mandate, stale-binary/wrong-worktree lesson.

### Tertiary (LOW)
- None — all claims verified against live code.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages/services verified in csproj + Program.cs + live files.
- Architecture (wizard/picker/cert reuse): HIGH — read the actual source line-by-line.
- Authoring reuse pitfall: HIGH — confirmed CreateQuestion is server-POST+redirect, incompatible with D-07.
- Commit contract: HIGH — read full InjectRequest DTO + service body.
- Pitfalls: HIGH — UserId/NIP mismatch and stale-binary both verified against code + STATE history.

**Research date:** 2026-06-17
**Valid until:** ~30 days (stable brownfield; service contract locked by Phase 393)
