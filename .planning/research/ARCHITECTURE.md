# Architecture Research

**Domain:** Brownfield ASP.NET MVC — Assessment form revamp & certificate enhancement (v7.5)
**Researched:** 2026-03-17
**Confidence:** HIGH (all findings from direct codebase inspection)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Browser (Razor + JS)                        │
│  CreateAssessment wizard (4-step single-page)                    │
│  Certificate.cshtml (HTML) + new PDF download button             │
└─────────────────────┬───────────────────────────────────────────┘
                      │ POST (same shape as today)
┌─────────────────────▼───────────────────────────────────────────┐
│                     AdminController.cs                           │
│  CreateAssessment GET  (modify: DB categories, clone pre-fill)   │
│  CreateAssessment POST (modify: ValidUntil, NomorSertifikat)     │
│  CloneAssessment GET   (new: read session → redirect with QS)    │
├─────────────────────────────────────────────────────────────────┤
│                     CMPController.cs                             │
│  Certificate GET   → HTML view (existing, unchanged)             │
│  CertificatePdf GET (new: QuestPDF binary stream)                │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                   ApplicationDbContext                           │
│  AssessmentSessions  (add: ValidUntil, NomorSertifikat)         │
│  AssessmentCategories (new table with seed data)                 │
│  TrainingRecords  (NomorSertifikat, ValidUntil already exist)    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Component Responsibilities

| Component | Responsibility | Status |
|-----------|----------------|--------|
| `AdminController.CreateAssessment GET` | Load users, sections, ProtonTracks, categories for form | Modify: swap hardcoded categories → DB query; read clone QS params |
| `AdminController.CreateAssessment POST` | Validate, create N sessions for N users, audit | Modify: add ValidUntil mapping, auto-number NomorSertifikat |
| `AdminController.CloneAssessment GET` | Read existing session → redirect to CreateAssessment with pre-fill query string | New action |
| `CMPController.Certificate GET` | Serve HTML certificate view | Unchanged |
| `CMPController.CertificatePdf GET` | Generate + stream QuestPDF binary (A4 landscape) | New action |
| `AssessmentCategory` model | Name, DefaultPassPercentage, DisplayOrder, IsActive | New model + migration |
| `AssessmentSession` model | Add `ValidUntil DateTime?` and `NomorSertifikat string?` | Modify model + migration |
| `CreateAssessment.cshtml` | 4-step wizard UI; categories from ViewBag | Major view rewrite |
| `Certificate.cshtml` | Add "Download PDF" link pointing to CertificatePdf | Minor modify |

---

## Recommended Project Structure

No new folders needed. All changes are in-place within existing structure:

```
Controllers/
  AdminController.cs         # modify CreateAssessment GET/POST; add CloneAssessment
  CMPController.cs           # add CertificatePdf action

Models/
  AssessmentSession.cs       # add ValidUntil, NomorSertifikat
  AssessmentCategory.cs      # new model

Data/
  ApplicationDbContext.cs    # add DbSet<AssessmentCategory>

Views/
  Admin/
    CreateAssessment.cshtml  # wizard rewrite
  CMP/
    Certificate.cshtml       # add PDF download button link

Migrations/
  [timestamp]_v75_AssessmentFormRevamp.cs  # new migration
```

---

## Architectural Patterns

### Pattern 1: Wizard as Single-Page Hide/Show (NOT Multi-Action)

**What:** All wizard steps live in one `CreateAssessment.cshtml` Razor view. Step navigation is pure JavaScript `display:none / display:block`. The final "Submit" POSTs to the existing `AdminController.CreateAssessment POST` with the same parameter shape (`AssessmentSession model, List<string> UserIds`).

**When to use:** The existing POST action is ~310 lines with heavy validation logic. Splitting into separate controller actions would require extracting that logic into a shared method — significant refactor risk in brownfield. Single-page wizard keeps the server contract intact.

**Trade-offs:**
- Pro: zero changes to POST action signature or validation flow
- Pro: back/forward navigation stays client-side — no round-trips, no TempData, no session state
- Con: all form fields must be present in DOM on submit; use `type="hidden"` inputs for fields on non-visible steps
- Con: JS step validation must mirror server-side rules client-side (duplicate logic, but unavoidable)

**Step layout:**
- Step 1: Category + Judul
- Step 2: Pilih Users (existing user search + section filter UI)
- Step 3: Settings (schedule, duration, passPercentage, ValidUntil, flags)
- Step 4: Confirm + Submit (summary of all steps, then POST)

### Pattern 2: DB-Driven Categories via ViewBag (Consistent with Existing Pattern)

**What:** New `AssessmentCategory` table replaces the hardcoded `SelectListItem` list in `CreateAssessment.cshtml`. `CreateAssessment GET` queries `_context.AssessmentCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder)` and passes via `ViewBag.Categories`.

**When to use:** Existing pattern already uses `ViewBag.ProtonTracks` and `ViewBag.Sections` for dynamic data. This follows it exactly.

**Trade-offs:**
- Pro: no new API endpoint; ViewBag pattern is established in this codebase
- Pro: `DefaultPassPercentage` per category moves from JS hardcode to a DB column, rendered as `data-pass-percentage` on each `<option>` element
- Con: adding a new category requires a DB insert (no admin UI in scope for v7.5 — acceptable)

**Seed data note:** Migration must seed exactly the six existing category string values (`OJT`, `IHT`, `Training Licencor`, `OTS`, `Mandatory HSSE Training`, `Assessment Proton`) to preserve `AdminController` branching logic that is keyed on those exact strings (especially `"Assessment Proton"`).

### Pattern 3: CloneAssessment as GET Redirect with Query String

**What:** `GET /Admin/CloneAssessment?id=123` reads an existing `AssessmentSession`, then redirects to `GET /Admin/CreateAssessment?category=OJT&title=...&duration=60&...`. The `CreateAssessment GET` checks `Request.Query` for pre-fill params and overrides the new default model before returning the view.

**When to use:** Avoids storing partial state in TempData or Session. The form is immediately editable — HC can modify any field before submitting.

**Trade-offs:**
- Pro: no new view, no new POST action; all wizard pre-fill handled client-side after page load
- Pro: URL query string is transparent — HC can bookmark or share pre-fill URLs
- Con: questions/answers are NOT cloned (they belong to the exam package, not the session — correct behavior)
- Con: URL length ceiling (not an issue at this payload size)

**Redirect target:** `RedirectToAction("CreateAssessment", new { category=..., title=..., duration=..., passPercentage=..., generateCertificate=..., protonTrackId=... })`

### Pattern 4: NomorSertifikat Auto-Generation Inside POST Loop

**What:** When `model.GenerateCertificate == true`, generate `NomorSertifikat` for each new `AssessmentSession` inside the existing POST `foreach (userId in UserIds)` loop. Format: `CERT/{YYYY}/{N:D4}` where N is a count of existing certificate sessions in the same year + offset per batch iteration.

**When to use:** Simple, no external sequence table. Safe at Pertamina's scale (well under 1000 certificate-bearing assessments per year).

**Trade-offs:**
- Pro: self-contained; no extra DB round-trip per certificate (count once before the loop, increment in-memory)
- Con: not gap-free if sessions are later deleted — acceptable for an internal portal
- Con: only generates when `GenerateCertificate == true`; sessions without certificates get `null` NomorSertifikat

**Implementation:**
```csharp
int existingCertCount = 0;
if (model.GenerateCertificate)
{
    var year = DateTime.UtcNow.Year;
    existingCertCount = await _context.AssessmentSessions
        .CountAsync(s => s.GenerateCertificate && s.CreatedAt.Year == year);
}

int certIndex = 0;
foreach (var userId in UserIds)
{
    var session = new AssessmentSession { /* ... existing mapping ... */ };
    if (model.GenerateCertificate)
    {
        session.NomorSertifikat = $"CERT/{DateTime.UtcNow.Year}/{existingCertCount + certIndex + 1:D4}";
        certIndex++;
    }
    session.ValidUntil = model.ValidUntil;
    _context.AssessmentSessions.Add(session);
}
```

### Pattern 5: QuestPDF Certificate Download (New Action, Reference CDPController)

**What:** Add `GET /CMP/CertificatePdf/{id}` to `CMPController`. Same authorization guard as `Certificate(id)` (owner or Admin/HC role, IsPassed == true, GenerateCertificate == true). Builds document content with QuestPDF's fluent API and returns `File(pdfBytes, "application/pdf", $"Certificate-{id}.pdf")`.

**Reference implementation:** `CDPController.ExportProgressPdf` (lines 2189–2539). Copy the `QuestPDF.Fluent.Document.Create(container => { page.Size(A4.Landscape)... })` structure verbatim, substituting certificate content.

**Trade-offs:**
- Pro: QuestPDF NuGet is already installed — no new dependencies
- Pro: returns a clean binary file download (no browser print dialog required)
- Con: QuestPDF cannot use web fonts (Playfair Display, Lato from Google Fonts); use system fonts or `NotoSerif` / `Arial` fallback
- Con: SVG watermark and Bootstrap icons from `Certificate.cshtml` must be rebuilt as QuestPDF layout primitives or omitted in the PDF version

**Authorization pattern:**
```csharp
[HttpGet]
public async Task<IActionResult> CertificatePdf(int id)
{
    var assessment = await _context.AssessmentSessions
        .Include(a => a.User)
        .FirstOrDefaultAsync(a => a.Id == id);
    if (assessment == null) return NotFound();

    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var userRoles = await _userManager.GetRolesAsync(user);
    bool isAuthorized = assessment.UserId == user.Id
        || userRoles.Contains("Admin") || userRoles.Contains("HC");
    if (!isAuthorized) return Forbid();

    if (assessment.Status != "Completed" || assessment.IsPassed != true
        || !assessment.GenerateCertificate) return NotFound();

    var pdfBytes = /* QuestPDF Document.Create(...).GeneratePdf() */;
    return File(pdfBytes, "application/pdf", $"Certificate-{id}.pdf");
}
```

---

## Data Flow

### CreateAssessment Wizard Submit

```
[Step 4: Confirm] → user clicks Submit
    ↓
POST /Admin/CreateAssessment
    (AssessmentSession model + List<string> UserIds — same signature as today)
    ↓
AdminController.CreateAssessment POST (existing ~310-line action)
    → existing validation (unchanged)
    → count existing certs for year (if GenerateCertificate)
    → foreach userId:
        - create AssessmentSession
        - set ValidUntil = model.ValidUntil
        - set NomorSertifikat = auto-generated (if GenerateCertificate)
    → existing audit + notification
    ↓
TempData["CreatedAssessment"] → RedirectToAction("CreateAssessment")
```

### Category Load Flow (Modified GET)

```
GET /Admin/CreateAssessment
    ↓ (was: hardcoded SelectListItem list in .cshtml)
    ↓ (now: _context.AssessmentCategories query in controller)
    → ViewBag.Categories = List<AssessmentCategory>
      (each carries DefaultPassPercentage for JS use)
    → Check Request.Query for clone pre-fill params
      (category, title, duration, passPercentage, etc.)
    → Build initial model (override defaults with clone params if present)
    ↓
CreateAssessment.cshtml
    → renders <select> from ViewBag.Categories
    → each <option data-pass-percentage="...">
    → JS builds categoryDefaults map from data attributes at runtime
```

### Certificate PDF Flow (New)

```
User on Certificate.cshtml → clicks "Download PDF" link
    ↓
GET /CMP/CertificatePdf/{id}
    ↓
CMPController.CertificatePdf
    → load AssessmentSession + User (same query as Certificate action)
    → same auth guard (owner or Admin/HC)
    → same guards (Completed, IsPassed, GenerateCertificate)
    → QuestPDF Document.Create (A4 Landscape)
       • header: "HC PORTAL KPB"
       • recipient name
       • course title
       • completion date
       • score badge (if Score != null)
       • NomorSertifikat (if set)
    → return File(pdfBytes, "application/pdf", "Certificate-{id}.pdf")
```

---

## Integration Points: New vs. Modified

| Component | New / Modified | What Changes |
|-----------|---------------|--------------|
| `AssessmentSession` model | Modified | Add `ValidUntil DateTime?`, `NomorSertifikat string?` |
| `AssessmentCategory` model | New | `Name`, `DefaultPassPercentage`, `DisplayOrder`, `IsActive` |
| `ApplicationDbContext` | Modified | Add `DbSet<AssessmentCategory>` |
| EF Migration | New | Adds two columns to AssessmentSessions; creates AssessmentCategories table with seed data |
| `AdminController.CreateAssessment GET` | Modified | Query DB for categories; read clone query-string params |
| `AdminController.CreateAssessment POST` | Modified | Map `ValidUntil`; auto-generate `NomorSertifikat` |
| `AdminController.CloneAssessment GET` | New | Read session → build redirect with pre-fill query string |
| `CreateAssessment.cshtml` | Major rewrite | 4-step wizard UI; categories from `ViewBag.Categories` |
| `CMPController.CertificatePdf GET` | New | QuestPDF binary stream (A4 landscape) |
| `Certificate.cshtml` | Minor modify | Add "Download PDF" `<a>` link to `CertificatePdf` |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Wizard steps → POST | HTML form fields (hidden for inactive steps) | All step data must be in DOM at submit |
| CloneAssessment → CreateAssessment | HTTP redirect with query string | GET reads `Request.Query` for pre-fill; no TempData |
| CertificatePdf → QuestPDF | In-process method call | No service abstraction needed; matches CDPController pattern |
| AssessmentCategory → CreateAssessment form | ViewBag | Consistent with ProtonTracks and Sections pattern |
| NomorSertifikat generation | Inline in POST loop | Count before loop; increment in-memory |

---

## Build Order (Dependency-Safe)

**Phase 1 — Data Layer (foundation for all other phases)**
- Add `AssessmentCategory` model
- Add `ValidUntil` + `NomorSertifikat` to `AssessmentSession`
- Add `DbSet<AssessmentCategory>` to `ApplicationDbContext`
- Create and apply migration with seed data for six categories

Every subsequent phase depends on the migration being applied before it can be tested end-to-end.

**Phase 2 — DB Categories in Form (smallest isolated change)**
- Modify `CreateAssessment GET` to query `AssessmentCategories` and pass via `ViewBag.Categories`
- Update `CreateAssessment.cshtml` to render the `<select>` from `ViewBag.Categories` (keep single-page form, no wizard yet)
- Update JS `categoryDefaults` to read `data-pass-percentage` from `<option>` elements instead of hardcoded map
- Remove the hardcoded `SelectListItem` list from the view

Verifiable without wizard: submit the existing single-page form with a DB-sourced category.

**Phase 3 — Wizard UI**
- Rewrite `CreateAssessment.cshtml` into 4-step wizard (step navigation via JS)
- Wire step validation in JS (mirrors existing server-side rules)
- Add `ValidUntil` datepicker to Step 3 (Settings)

Phase 3 does not change the POST action. The view changes are safe to build after Phase 2 confirms category loading works.

**Phase 4 — ValidUntil + NomorSertifikat on Session**
- Modify `CreateAssessment POST` to map `ValidUntil` from model
- Add NomorSertifikat auto-generation inside POST loop

Depends on Phase 1 (columns exist) and Phase 3 (wizard has the `ValidUntil` field in Step 3).

**Phase 5 — Clone Feature**
- Add `CloneAssessment GET` action to `AdminController`
- Modify `CreateAssessment GET` to read pre-fill query string params
- Add "Clone" button to assessment list or detail view

Depends on Phase 3 (wizard must be stable for pre-fill to land in correct step).

**Phase 6 — PDF Certificate Download**
- Add `CertificatePdf GET` to `CMPController`
- Add "Download PDF" link to `Certificate.cshtml`

Fully independent of Phases 3–5. Only dependency is Phase 1 (NomorSertifikat column must exist to display it on PDF). Can be built in parallel with Phases 3–5 if desired.

---

## Anti-Patterns

### Anti-Pattern 1: Multi-Action Wizard with Server Round-Trips

**What people do:** Each wizard step is a separate controller action with its own POST, storing partial state in TempData or Session.

**Why it's wrong:** The existing `CreateAssessment POST` has ~310 lines of validation, notification, and audit logic. Splitting it requires extracting that into a shared private method — a significant brownfield refactor risk, with no functional benefit at this scale.

**Do this instead:** Single-page wizard with JS step navigation. One POST to the existing action. Zero risk to the existing submit path.

### Anti-Pattern 2: Storing NomorSertifikat on TrainingRecord for Online Assessments

**What people do:** At session completion, mirror `NomorSertifikat` from `AssessmentSession` into a new or existing `TrainingRecord` row.

**Why it's wrong:** `TrainingRecord.NomorSertifikat` is already the field for manually-imported offline records (used in CMPController import/export flow). Dual-sourcing the same-named field across two tables creates confusion about which is authoritative.

**Do this instead:** Store `NomorSertifikat` on `AssessmentSession` only. `Certificate.cshtml` and `CertificatePdf` already read from `AssessmentSession` directly. No `TrainingRecord` sync needed.

### Anti-Pattern 3: Renaming "Assessment Proton" in Category Seed Data

**What people do:** Treat DB migration as a chance to clean up category names (e.g., rename to "Proton Assessment").

**Why it's wrong:** `AdminController.CreateAssessment POST` has Proton-specific branching logic keyed on the exact string `"Assessment Proton"` — TahunKe detection, ProtonTrackId validation, duration=0 sentinel for Tahun 3 interview sessions. If the value changes, that branching silently breaks.

**Do this instead:** Seed exactly the six existing string values as the canonical `Name` values. If a different display label is needed, add a separate `DisplayName` column and render that in the `<option>` text.

### Anti-Pattern 4: Generating NomorSertifikat for Non-Certificate Sessions

**What people do:** Always auto-assign a certificate number when creating any session.

**Why it's wrong:** Most sessions have `GenerateCertificate = false`. Assigning numbers to non-certificate sessions pollutes the sequence and creates gaps where numbers were "used" on sessions that never produce a certificate.

**Do this instead:** Only generate when `model.GenerateCertificate == true`. Leave `NomorSertifikat` as `null` for all other sessions.

---

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Current (~200 workers) | All in-process; count-based NomorSertifikat is safe |
| NomorSertifikat uniqueness | At current scale, count before loop is safe. If concurrent POSTs become an issue, use `SELECT MAX(NomorSertifikat)` with a DB-level unique index |
| PDF generation | QuestPDF is synchronous and CPU-bound. Inline is fine at current scale. If PDF becomes slow, move to background job |

---

## Sources

- Direct inspection: `Controllers/AdminController.cs` — CreateAssessment GET (L759), POST (L795-1104)
- Direct inspection: `Controllers/CMPController.cs` — Certificate (L2327), QuestPDF usage (L8-9)
- Direct inspection: `Controllers/CDPController.cs` — ExportProgressPdf QuestPDF pattern (L2189-2539)
- Direct inspection: `Models/AssessmentSession.cs` — full model (no ValidUntil/NomorSertifikat present)
- Direct inspection: `Models/TrainingRecord.cs` — NomorSertifikat + ValidUntil already exist here
- Direct inspection: `Views/Admin/CreateAssessment.cshtml` — hardcoded categories at lines 7-15, JS categoryDefaults at lines 537-551
- Direct inspection: `Views/CMP/Certificate.cshtml` — HTML layout reference for PDF rebuild
- Direct inspection: `Data/ApplicationDbContext.cs` — DbSet inventory, no AssessmentCategory present

---

*Architecture research for: PortalHC KPB v7.5 — Assessment Form Revamp & Certificate Enhancement*
*Researched: 2026-03-17*
