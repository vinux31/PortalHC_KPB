# Stack Research

**Domain:** Brownfield ASP.NET Core MVC — Wizard Form, DB-Driven Categories, Certificate Auto-Numbering, PDF Download (v7.5)
**Researched:** 2026-03-17
**Confidence:** HIGH (all findings verified against csproj and existing source files)

---

## What Is Already in the Project (Do NOT Add Again)

| Technology | Version in csproj | Relevant Role |
|------------|-------------------|---------------|
| ASP.NET Core MVC | net8.0 | Web framework, Razor views, routing |
| Entity Framework Core + SQL Server | 8.0.0 | ORM, migrations |
| QuestPDF | 2026.2.2 | PDF generation — Community license already set in `Program.cs:8`, used in `CDPController.cs:2227,2371` |
| ClosedXML | 0.105.0 | Excel import/export |
| Bootstrap 5 | CDN (all views) | UI components including tabs, modals, nav |
| jQuery | CDN (all views) | DOM manipulation, AJAX, form validation |

**No new NuGet packages are required for any feature in this milestone.**

---

## Recommended Stack — New Capabilities

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| ASP.NET Core MVC | net8.0 (existing) | Wizard form POST, Clone action, Certificate PDF action | All new features fit the existing MVC action pattern; no new controller type needed |
| EF Core SqlServer | 8.0.0 (existing) | `AssessmentCategory` table, `ValidUntil` + `NomorSertifikat` on `AssessmentSession`, MAX query for auto-numbering | Two migrations cover all DB changes |
| QuestPDF 2026.2.2 | existing | Certificate PDF download | Identical pattern to existing CDP PDF exports; inline `Document.Create` lambda is consistent with current codebase style |
| Bootstrap 5 tabs | existing (CDN) | Wizard step UI | Zero new dependency; tabs + jQuery step logic is sufficient for a 4-step linear form |
| jQuery | existing (CDN) | Step-to-step navigation, client-side field validation per step | Already used extensively in `CreateAssessment.cshtml`; no new library to fight against |

### Supporting Libraries

No new libraries. Each capability maps directly to what is installed:

| Capability | Handled By | Notes |
|------------|-----------|-------|
| Wizard step navigation | Bootstrap 5 `nav-tabs` + jQuery | Show/hide `tab-pane` divs; disable forward navigation until current step validates |
| Client-side step validation | jQuery + `$(form).validate()` unobtrusive | Already loaded; validate visible fields only before advancing |
| Category dropdown | EF Core query → `SelectList` in ViewBag | `AssessmentCategory` rows loaded in controller, passed as `SelectList` |
| Clone pre-fill | Controller reads existing `AssessmentSession`, maps to ViewModel | Pure MVC — no library |
| Auto-numbering | `MAX` LINQ query + C# string format | No external library; see pattern below |
| Certificate PDF | `QuestPDF.Fluent.Document.Create(...)` | Reuse exact same API already in `CDPController.cs` |
| PDF download response | `File(pdf.GeneratePdf(), "application/pdf", filename)` | Standard `FileContentResult` |

---

## Implementation Patterns

### Wizard Form — Bootstrap Tabs + jQuery Step Controller

The 4-step form (Category → Users → Settings → Confirm) uses Bootstrap 5 `nav-tabs` with `tab-pane` content divs. A single `<form>` wraps all steps and is submitted only on the final Confirm step.

```html
<ul class="nav nav-tabs" id="wizardTabs" role="tablist">
  <li class="nav-item">
    <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#step1" type="button">
      1. Kategori
    </button>
  </li>
  <!-- steps 2, 3, 4 start with "disabled" class -->
</ul>

<form asp-action="CreateAssessment" method="post">
  <div class="tab-content">
    <div class="tab-pane fade show active" id="step1"> <!-- category fields --> </div>
    <div class="tab-pane fade" id="step2"> <!-- user selection --> </div>
    <div class="tab-pane fade" id="step3"> <!-- settings --> </div>
    <div class="tab-pane fade" id="step4"> <!-- confirm + submit --> </div>
  </div>
  <button type="button" id="btnPrev" style="display:none">Sebelumnya</button>
  <button type="button" id="btnNext">Lanjut</button>
  <button type="submit" id="btnSubmit" style="display:none">Simpan Assessment</button>
</form>
```

jQuery logic: intercept `#btnNext` click → validate all `:visible` required inputs in the current `tab-pane` → if valid, activate next tab, update button visibility. Prevent direct tab clicking by removing `data-bs-toggle` from tab buttons and only toggling programmatically.

**Why not a JS wizard library (jQuery Steps, SmartWizard, etc.):**
The existing `CreateAssessment.cshtml` has 1023 lines of conditional jQuery logic. A wizard library imposes its own event lifecycle (beforeStep, afterStep) that would conflict with existing event handlers. Bootstrap tabs are already loaded and require zero additional bytes.

### DB-Driven Assessment Categories

New `AssessmentCategory` model:

```csharp
public class AssessmentCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
```

Add `DbSet<AssessmentCategory> AssessmentCategories` to `ApplicationDbContext`. Seed with the existing hardcoded strings currently listed in `AssessmentSession.Category` comments: "Assessment OJ", "IHT", "Licencor", "OTS", "Mandatory HSSE Training", "Assessment Proton".

`AssessmentSession.Category` stays as `string` (no FK). This preserves compatibility with existing sessions and avoids a JOIN on every session query.

### Auto-Numbering Pattern for NomorSertifikat

Format: `SERT/{YEAR}/{SEQUENCE:D3}` — example: `SERT/2026/001`.

```csharp
private string GenerateCertificateNumber(int year)
{
    var prefix = $"SERT/{year}/";
    var lastSeq = _context.AssessmentSessions
        .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.StartsWith(prefix))
        .Select(s => s.NomorSertifikat!)
        .AsEnumerable()
        .Select(n => int.TryParse(n.Split('/').LastOrDefault(), out var seq) ? seq : 0)
        .DefaultIfEmpty(0)
        .Max();
    return $"{prefix}{(lastSeq + 1):D3}";
}
```

Called when a session completes with `IsPassed == true && GenerateCertificate == true`. The result is stored in `AssessmentSession.NomorSertifikat` (new field — requires migration; `TrainingRecord` already has this field but `AssessmentSession` does not).

### QuestPDF Certificate PDF

Follow the inline pattern from `CDPController.cs:2227`. The action:

1. Load `AssessmentSession` + `ApplicationUser` by session ID
2. Guard: `IsPassed != true || !GenerateCertificate` → return 403/redirect
3. Build PDF inline:

```csharp
var pdf = QuestPDF.Fluent.Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(QuestPDF.Helpers.PageSizes.A4);  // Portrait for certificate
        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
        page.Content().Column(col =>
        {
            // Certificate body: title, name, assessment title, score, date, valid-until, cert number
        });
    });
});
return File(pdf.GeneratePdf(), "application/pdf",
    $"Sertifikat_{session.NomorSertifikat ?? session.Id.ToString()}.pdf");
```

No `IDocument` class file is needed. The inline lambda is consistent with existing project style and sufficient for a single certificate template.

---

## Migrations Required

| Migration Name | Changes |
|----------------|---------|
| `AddAssessmentCategory` | Create `AssessmentCategories` table |
| `AddAssessmentSessionCertFields` | Add `ValidUntil DateTime?` and `NomorSertifikat string?` to `AssessmentSessions` table |

Run: `dotnet ef migrations add <Name>` then `dotnet ef database update`

No migration is needed for `TrainingRecord` — `ValidUntil`, `CertificateType`, and `NomorSertifikat` already exist on that model.

---

## Alternatives Considered

| Feature | Recommended | Alternative | Why Not |
|---------|-------------|-------------|---------|
| Wizard UI | Bootstrap 5 tabs + jQuery | External JS wizard library (jQuery Steps, SmartWizard) | Conflicts with 1023-line existing jQuery in the form; zero benefit over tabs for 4 linear steps |
| Wizard UI | Bootstrap 5 tabs + jQuery | Vue/React component | Requires build toolchain not present in project; overkill for a form |
| Auto-numbering | MAX query + C# format | SQL Server SEQUENCE object | Format includes year prefix which IDENTITY/SEQUENCE can't encode; C# approach is readable and maintainable |
| Category storage | `AssessmentCategory` DB table | Enum in C# | Enum requires code change to add category; DB table is the stated requirement |
| PDF template | Inline `Document.Create` lambda | Separate `IDocument` class | Inline matches existing project style; a class adds indirection for a single-use template |

---

## What NOT to Add

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Any JS wizard library | Conflicts with existing jQuery; adds download weight; Bootstrap tabs already loaded | Bootstrap 5 `nav-tabs` + custom jQuery |
| Fluent Validation NuGet | Adds package for simple field validation already covered by DataAnnotations + jQuery unobtrusive | `[Required]`, `[Range]` attributes + existing client-side validation |
| QuestPDF upgrade | 2026.2.2 is current; upgrading mid-milestone risks breaking existing CDPController PDF output | Stay on 2026.2.2 |
| SignalR for wizard state | No real-time requirement; form submits normally on final step | Standard form POST |
| Separate PDF microservice | Over-engineering; QuestPDF runs in-process, already licensed | Inline QuestPDF in CMPController |

---

## Version Compatibility

| Package | Version | Compatible With | Notes |
|---------|---------|-----------------|-------|
| QuestPDF | 2026.2.2 | net8.0 | In project, Community license active, PDF generation verified in CDPController |
| EF Core SqlServer | 8.0.0 | net8.0 + SQL Server 2019+ | No upgrade needed; migrations add two small changes |
| Bootstrap 5 | 5.x CDN | jQuery 3.x | Tab component available; no Bootstrap JS conflict with jQuery |

---

## Sources

- `HcPortal.csproj` — confirmed installed packages and exact versions (HIGH confidence)
- `Program.cs:6-8` — confirmed `QuestPDF.Settings.License = LicenseType.Community` (HIGH confidence)
- `Controllers/CDPController.cs:2227,2371` — confirmed inline `Document.Create` pattern (HIGH confidence)
- `Models/AssessmentSession.cs` — confirmed absence of `ValidUntil` and `NomorSertifikat` fields (HIGH confidence)
- `Models/TrainingRecord.cs` — confirmed `ValidUntil`, `CertificateType`, `NomorSertifikat` already exist (HIGH confidence)
- `.planning/PROJECT.md` — confirmed milestone scope and target features (HIGH confidence)

---

*Stack research for: PortalHC KPB v7.5 — Assessment Form Revamp & Certificate Enhancement*
*Researched: 2026-03-17*
