# Feature Research

**Domain:** ASP.NET MVC HR Portal — Assessment Form UX & Certificate Enhancement (v7.5)
**Researched:** 2026-03-17
**Confidence:** HIGH — existing codebase fully inspected (models, controller, views)

---

## Feature Landscape

### Table Stakes (Users Expect These)

Features that HC admins and workers assume will work correctly. Missing or broken = system feels unreliable.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Wizard step indicator (breadcrumb/progress bar) | Multi-step forms without progress indication feel broken; users do not know how far along they are | LOW | Pure HTML/CSS + JS show/hide — no server round-trips between steps; all steps in single `<form>` |
| Step validation before advancing to next step | Clicking "Next" on incomplete step should show inline errors, not silently skip | MEDIUM | Client-side JS validation per-step + server-side on final POST; prevents partial or wrong data |
| Category dropdown populated from DB | HC admins must add categories without a developer deployment; hardcoded list in view is a maintenance liability | MEDIUM | New `AssessmentCategories` table seeded with existing 6 values; CMPController passes list to view via ViewBag |
| Clone / duplicate existing assessment | Recreating a recurring assessment (e.g., annual HSSE Training) manually every time wastes HC time | MEDIUM | GET `CloneAssessment(id)` reads source AssessmentSession; returns pre-filled CreateAssessment view; new row only on POST |
| ValidUntil on AssessmentSession | Passed online assessments issue certificates — those certificates need expiry dates like TrainingRecord already has | LOW | New nullable `DateTime?` column on AssessmentSession via EF migration; displayed on certificate view and PDF |
| Auto-generated NomorSertifikat on AssessmentSession | Certificate numbers must be unique and traceable; the existing manual-entry path for TrainingRecord is not appropriate for auto-issued online certs | MEDIUM | Generate on `IsPassed = true AND GenerateCertificate = true AND NomorSertifikat IS NULL`; store on session row |
| PDF certificate download | Print-to-PDF via browser is unreliable (margins, fonts, color vary per browser/OS); HC needs a deterministic distributable file | HIGH | QuestPDF library; new `DownloadCertificate(id)` action returning `FileContentResult`; layout mirrors existing Certificate.cshtml |

### Differentiators (Competitive Advantage)

Features that make this portal noticeably better than a plain CRUD form.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Wizard Confirm/Review step | Final step shows all selections read-only before submit — eliminates batch-creation errors for 10+ user assessments | LOW | Summary panel populated from hidden inputs or JS in-memory state; no extra DB calls |
| Category-driven default autofill | Selecting "Mandatory HSSE Training" auto-populates pass threshold, cert flag, banner color — reduces setup errors | MEDIUM | Defaults stored per-category row in DB; JS fetches on category select (or inline JSON in page); no model change needed |
| Clone from Assessment list (one-click) | "Duplicate" button on existing assessment index row → pre-fills wizard at Step 1; HC only changes title and date | LOW | Clone button in Assessment table row; GET action redirects to CreateAssessment with query params or TempData |
| Certificate expiry warning for online assessments | Extend the existing IsExpiringSoon warning (already on TrainingRecord/UnifiedTrainingRecord) to cover online assessment certs | LOW | Add ValidUntil to AssessmentSession; UnifiedTrainingRecord unified helper already merges both source types |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Server round-trip save between wizard steps | "What if the browser closes mid-form?" | Creates partial/orphan AssessmentSession rows needing cleanup; overkill for a 4-step form that completes in <2 minutes | Single POST on final step; use JS hidden inputs to carry earlier step values; optionally localStorage for draft |
| PDF generation via headless Chrome / Puppeteer | Reuses existing HTML cert template exactly | Heavy dependency; server RAM usage; Windows service startup latency; complex deployment | QuestPDF is .NET-native, deterministic, fast; minor effort to replicate layout in C# fluent API |
| AssessmentCategory hierarchy (subcategories) | "OJT has subtypes (OJT-1, OJT-2)" | Cascading dropdowns; migration complexity; existing AssessmentCompetencyMap uses flat category string | Flat list with optional `DisplayOrder`; add `ParentId` only if subcategory requirement is confirmed |
| Editable certificate template via UI | "Change logo or signatory without deploying" | Template builder is a product in itself; far out of scope for this milestone | Static QuestPDF layout; configurable text fields (signatory name, org name) via AppSettings or a small config table |
| Wizard state persisted in DB between browser sessions | Resume half-filled assessment creation later | Stale draft rows, orphan cleanup, extra table; not justified for a 2-minute form | Not needed; use `localStorage` if a "save draft" requirement is ever raised |

---

## Feature Dependencies

```
[DB-driven AssessmentCategories table]
    └──enables──> [Category dropdown in Wizard Step 1]
    └──enables──> [Category-driven default autofill via JS]
    └──enables──> [Admin CRUD for categories]

[Wizard Step UI (client-side restructure)]
    └──requires──> [DB-driven Categories] for Step 1 dropdown
    └──enhances──> [Clone Assessment] — Clone sets initial wizard state

[ValidUntil on AssessmentSession] (new DB column, EF migration)
    └──enables──> [Certificate expiry display on Records (IsExpiringSoon)]
    └──enables──> [ValidUntil printed on PDF certificate]

[NomorSertifikat on AssessmentSession] (new DB column, auto-generated)
    └──requires──> [IsPassed=true AND GenerateCertificate=true gate] (already exists)
    └──displayed on──> [QuestPDF certificate download]

[QuestPDF Certificate Download]
    └──requires──> [NomorSertifikat on AssessmentSession]
    └──requires──> [ValidUntil on AssessmentSession]
    └──references──> [Existing Certificate.cshtml] (layout only — no code reuse)
```

### Dependency Notes

- **DB-driven Categories requires migration first:** Seed with the exact 6 existing hardcoded strings to avoid breaking Category filters on existing AssessmentSession rows.
- **AssessmentSession.Category stays as `string` (no FK):** Consistent with the existing `AssessmentCompetencyMap.AssessmentCategory` string pattern; avoids cascade-delete risk on historical data.
- **Wizard requires no new DB columns:** It is purely a UX restructure of the existing `CreateAssessment` form. The POST action signature and `AssessmentSession` model binding are unchanged.
- **Clone creates no draft row:** GET `CloneAssessment(id)` returns a pre-populated view. A new row is only committed on the user's final POST submit.
- **QuestPDF must be phased after NomorSertifikat and ValidUntil:** PDF layout references both fields. If PDF ships without them, the certificate is incomplete.

---

## MVP Definition

This is a brownfield milestone — all features in scope are confirmed by the product owner. "MVP" here means phase ordering for safe incremental delivery.

### Phase 1 — Foundation (DB + Wizard)
- [ ] `AssessmentCategories` table with EF migration and seed data — unblocks all other features
- [ ] Admin CRUD for categories (new action in AdminController or CMPController) — HC self-service
- [ ] Wizard step UI on `CreateAssessment.cshtml` (JS step visibility, progress indicator, confirm/summary step)

### Phase 2 — Data Enhancements
- [ ] `ValidUntil` nullable column on `AssessmentSession` (EF migration)
- [ ] `NomorSertifikat` nullable string column on `AssessmentSession` (EF migration) + auto-generation on pass
- [ ] Clone assessment action (`GET CloneAssessment(id)` → pre-filled wizard)

### Phase 3 — PDF Certificate
- [ ] QuestPDF NuGet package integration
- [ ] `DownloadCertificate(id)` action returning `FileContentResult` as `application/pdf`
- [ ] "Download PDF" button on existing `Certificate.cshtml` view

### Add After Validation
- [ ] Category-driven default autofill (JS on category select)
- [ ] Expiry warning on Records for online assessment ValidUntil (extend existing IsExpiringSoon display)

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Wizard step UI | HIGH | MEDIUM | P1 |
| DB-driven categories | HIGH | MEDIUM | P1 |
| Admin category CRUD | MEDIUM | LOW | P1 |
| NomorSertifikat auto-generate | HIGH | LOW | P1 |
| ValidUntil on AssessmentSession | HIGH | LOW | P1 |
| Clone assessment | MEDIUM | LOW | P1 |
| QuestPDF certificate download | HIGH | HIGH | P1 |
| Category-driven defaults autofill | MEDIUM | MEDIUM | P2 |
| Expiry warning on Records (online certs) | LOW | LOW | P2 |

---

## Implementation Notes Per Feature

### Wizard Form
- Existing `CreateAssessment.cshtml` is a single long form. Restructure into 4 visually distinct `<div>` sections with JS `show/hide`. No changes to the POST action.
- Steps: (1) Category + Title, (2) Select Users (existing multi-user logic unchanged), (3) Settings (schedule, duration, token, pass%, cert toggle, ValidUntil), (4) Confirm/summary.
- Hidden `<input>` elements carry values from earlier steps into the final POST to avoid losing data on navigation.
- Bootstrap 5 `nav-pills` or a custom stepper `<ul>` for the progress indicator.
- Step-level JS validation: required fields highlighted before `next()` is allowed; server-side ModelState still validates on final submit as fallback.

### DB-driven Categories
- New model: `AssessmentCategory { int Id, string Name, string? DisplayColor, int? DefaultPassPercentage, bool DefaultGenerateCertificate, int DisplayOrder, bool IsActive }`.
- Seed exactly: `OJT`, `IHT`, `Training Licencor`, `OTS`, `Mandatory HSSE Training`, `Proton` — these match existing `AssessmentSession.Category` string values in production data.
- `AssessmentSession.Category` remains a plain `string` column (no FK). This avoids cascade-delete risk and matches the existing `AssessmentCompetencyMap.AssessmentCategory` string pattern.
- CMPController `CreateAssessment` (GET) passes `List<AssessmentCategory>` via `ViewBag.Categories`.

### Clone Assessment
- `GET /CMP/CloneAssessment/{id}` — load source `AssessmentSession`, map to a new unpersisted ViewModel, return `CreateAssessment` view pre-populated.
- Do NOT copy: `UserId`, `Questions`, `Responses`, `Score`, `IsPassed`, `CompletedAt`, `StartedAt`, `ElapsedSeconds`, `NomorSertifikat`, `CreatedAt`.
- Copy: `Title` (prefix with "Salinan - "), `Category`, `DurationMinutes`, `PassPercentage`, `AllowAnswerReview`, `GenerateCertificate`, `BannerColor`, `IsTokenRequired`, `ProtonTrackId`, `TahunKe`.
- Add `ViewBag.ClonedFrom = originalTitle` so the Confirm step can display "Disalin dari: [original title]".

### ValidUntil on AssessmentSession
- Single nullable `DateTime?` column. EF migration required.
- Displayed on Certificate view (HTML) and included in QuestPDF output.
- HC sets this during assessment creation in Wizard Step 3 — optional field; leave blank for non-expiring certs.
- The UnifiedTrainingRecord helper in CMPController (around line 1066) already handles `ValidUntil` from TrainingRecord; extend to read from `AssessmentSession.ValidUntil` for online cert rows.

### NomorSertifikat Auto-Generation
- Generate when: `IsPassed = true AND GenerateCertificate = true AND NomorSertifikat IS NULL`.
- Trigger point: inside the existing exam-finish logic (wherever `IsPassed` is set — locate the `FinishExam`/`SubmitAnswers` action flow).
- Format: `CERT-{YEAR}-{SEQ:D4}` (e.g., `CERT-2026-0042`). Sequential counter per year via `MAX` query on existing AssessmentSession NomorSertifikat values with same year prefix. Simple; no separate sequence table needed.
- New nullable `string? NomorSertifikat` column on `AssessmentSession`; EF migration required.
- If `GenerateCertificate = false` or assessment fails, NomorSertifikat stays null — no number is issued.

### QuestPDF Certificate Download
- NuGet: `QuestPDF` (MIT/Community license — free for organizations under $1M revenue; appropriate for internal corporate portal).
- New action: `GET /CMP/DownloadCertificate/{id}` → access-checks same as existing `Certificate(id)` (must be owner or Admin/HC); returns `File(bytes, "application/pdf", "Sertifikat-{title}.pdf")`.
- Create a `CertificateDocument` class implementing `IDocument`; layout in C# fluent API, not Razor.
- Layout matches `Certificate.cshtml`: A4 landscape, employee name prominent, training title, completion date, `NomorSertifikat`, `ValidUntil` if not null, Pertamina logo from `wwwroot/images/`.
- Embed fonts (e.g., DejaVu Sans or Liberation Sans from NuGet `QuestPDF` built-ins) to avoid server font dependency.
- Add "Download PDF" button next to existing "Print" button on `Certificate.cshtml`.

---

## Existing System Integration

| Existing Component | How New Features Use It |
|-------------------|------------------------|
| `AssessmentSession` model | Extended with `ValidUntil` and `NomorSertifikat` columns; no other model changes |
| `CreateAssessment.cshtml` (worktree version) | Restructured into wizard steps; POST action unchanged |
| `Certificate.cshtml` | Gets a new "Download PDF" button; layout reference for QuestPDF |
| `CMPController` exam-finish flow | Extended to auto-generate NomorSertifikat on pass |
| `UnifiedTrainingRecord` helper (CMPController ~line 1031) | Extended to propagate `ValidUntil` from `AssessmentSession` rows |
| `AssessmentCompetencyMap.AssessmentCategory` (string) | Confirms flat string-per-session category pattern is safe; no FK needed |
| Admin CRUD pattern (AdminController) | Copy ManageWorkers CRUD pattern for new ManageAssessmentCategories actions |

---

## Sources

- `Models/AssessmentSession.cs` — inspected; fields confirmed; `Category` is plain string; no `ValidUntil` or `NomorSertifikat` yet
- `Models/TrainingRecord.cs` — inspected; `ValidUntil`, `NomorSertifikat`, `IsExpiringSoon`, `DaysUntilExpiry` all present
- `Views/CMP/CreateAssessment.cshtml` (worktree) — inspected; categories hardcoded as `List<SelectListItem>` lines 6-14; single-page form confirmed
- `Views/CMP/Certificate.cshtml` — inspected; HTML-only, print-focused, A4 landscape layout
- `Controllers/CMPController.cs` — inspected; Certificate action (line ~2327), unified helper (line ~1031), exam completion flow (line ~1412+)
- `Data/ApplicationDbContext.cs` — `AssessmentCompetencyMap.AssessmentCategory` index confirms flat string category pattern

---
*Feature research for: PortalHC KPB — v7.5 Assessment Form Revamp & Certificate Enhancement*
*Researched: 2026-03-17*
