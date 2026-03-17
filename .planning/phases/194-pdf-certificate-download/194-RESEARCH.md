# Phase 194: PDF Certificate Download - Research

**Researched:** 2026-03-17
**Domain:** QuestPDF server-side PDF generation in ASP.NET Core MVC
**Confidence:** HIGH

## Summary

Phase 194 adds a server-rendered PDF download for the HC Portal assessment certificate. QuestPDF 2026.2.2 is already installed and in active use in CDPController, so there is no new dependency to add. The implementation pattern — `Document.Create` inline lambda → `GeneratePdf(MemoryStream)` → `File(..., "application/pdf", filename)` — is established and proven in the project.

The work splits into two deliverables: (1) a new `CertificatePdf` controller action in CMPController that mirrors the existing `Certificate` action's four auth guards and generates the PDF, and (2) an update to `Certificate.cshtml` that adds a Download PDF button and brings the HTML view in sync with new data fields (NIP, NomorSertifikat, ValidUntil, actual completion date).

The primary technical challenge is font embedding. QuestPDF requires TTF files to be registered via `FontManager.RegisterFont()`; Google Fonts CDN (used in the HTML view) is not available at PDF render time. Playfair Display and Lato TTF files must be downloaded and added to the project as embedded resources or wwwroot files. The existing CDPController PDF actions use only default system fonts, so font registration is new ground for this codebase but is well-documented in QuestPDF.

**Primary recommendation:** Follow the CDPController `Document.Create` → `GeneratePdf` → `File()` scaffold exactly. Register Playfair Display and Lato from wwwroot/fonts TTF bytes before document creation. Replicate the Certificate action auth guards verbatim.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**PDF Visual Fidelity**
- Exact match of HTML certificate layout in QuestPDF A4 landscape
- Embed Google Fonts (Playfair Display + Lato) as TTF files, register with QuestPDF
- Include SVG triangle watermark at 5% opacity
- Exact circular score badge (blue circle, gold border, white score text) in bottom-right
- Identical hex colors: #1a4a8d (blue), #c49a00 (gold)
- Signature area: "Authorized Sig." in Playfair Display + signature line + "HC Manager" label
- Proficiency description text kept: "Demonstrating proficiency and understanding of the subject matter."

**Certificate Content**
- NIP displayed below recipient name
- NomorSertifikat displayed bottom-left near Date of Issue ("No. Sertifikat: XXX")
- ValidUntil displayed below Date of Issue ("Berlaku Hingga: dd MMMM yyyy") — omit line entirely if null
- NomorSertifikat line omitted if null (graceful degradation)
- Date of Issue uses actual assessment completion date, NOT DateTime.Now
- English language text (matching current HTML certificate)
- All content from HTML retained: HC PORTAL KPB header, "Certificate of Completion", "This verifies that", proficiency text, assessment title

**Download UX**
- Green (#198754) "Download PDF" button with bi-download icon
- Placed next to existing Print button in the no-print toolbar (alongside Kembali and Print)
- Direct file download (Content-Disposition: attachment)
- Filename: Sertifikat_{NIP}_{Title}_{Year}.pdf
- Title sanitized: replace non-alphanumeric characters with underscore

**Auth & Edge Cases**
- Same 4 auth guards as Certificate view: owner/Admin/HC role check, Completed status, GenerateCertificate=true, IsPassed=true
- Route: CMP/CertificatePdf/{id}
- Null NIP fallback: use user ID in filename
- No caching — generate fresh PDF on each request

**HTML Certificate View Updates**
- Update HTML Certificate.cshtml to match PDF content
- Add NIP below recipient name
- Add NomorSertifikat near Date of Issue
- Add ValidUntil below Date of Issue (omit if null)
- Replace DateTime.Now with actual completion date
- Same positions as PDF for visual consistency

### Claude's Discretion

- Border detail: whether to replicate exact double-border (outer blue solid + inner gold double) or simplify — pick what looks best in QuestPDF
- Which date field to use for completion date (CompletedAt, UpdatedAt, or best available on AssessmentSession)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CERT-03 | User dapat download sertifikat sebagai file PDF (server-side via QuestPDF) | QuestPDF 2026.2.2 already in project; CDPController pattern is the scaffold; font embedding via TTF files is the key technical step |
</phase_requirements>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| QuestPDF | 2026.2.2 | Server-side PDF generation | Already in HcPortal.csproj; actively used in CDPController |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Globalization.CultureInfo | BCL | Indonesian date formatting | Already used in Certificate.cshtml and CDPController |
| System.Text.RegularExpressions | BCL | Filename sanitisation | Replace non-alphanumeric chars with underscore |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| QuestPDF | Playwright headless HTML-to-PDF | Playwright requires a browser process and extra infra; QuestPDF is already installed |
| QuestPDF | iTextSharp / PdfSharp | Different API; QuestPDF is already the project standard |

**No new packages to install.** QuestPDF 2026.2.2 is already referenced.

---

## Architecture Patterns

### Recommended Project Structure

```
wwwroot/
└── fonts/
    ├── PlayfairDisplay-Regular.ttf
    ├── PlayfairDisplay-Bold.ttf
    ├── PlayfairDisplay-Italic.ttf
    └── Lato-Regular.ttf
    └── Lato-Bold.ttf
    └── Lato-Light.ttf

Controllers/
└── CMPController.cs  (new CertificatePdf action — ~80 lines)

Views/CMP/
└── Certificate.cshtml  (updated: NIP, NomorSertifikat, ValidUntil, CompletedAt)
```

### Pattern 1: QuestPDF Inline Document (established in CDPController)

**What:** `Document.Create` lambda inline inside the action, `GeneratePdf` to `MemoryStream`, returned as `File()`.

**When to use:** Every PDF action in this codebase.

**Example (from CDPController lines 2227-2283):**
```csharp
// Source: Controllers/CDPController.cs lines 2227-2283
var pdf = QuestPDF.Fluent.Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
        page.Margin(20);
        page.Content().Column(col =>
        {
            col.Item().Text("...").FontSize(14).Bold();
        });
    });
});

var pdfStream = new MemoryStream();
pdf.GeneratePdf(pdfStream);
return File(pdfStream.ToArray(), "application/pdf", "filename.pdf");
```

### Pattern 2: QuestPDF Font Registration

**What:** Load TTF bytes from wwwroot at action time, register with `QuestPDF.Infrastructure.FontManager.RegisterFont()`, then reference font family by name in `.FontFamily("Playfair Display")`.

**When to use:** Any QuestPDF document needing non-system fonts (new in this phase).

**Example:**
```csharp
// Load and register once before Document.Create
var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
var playfairPath = Path.Combine(env.WebRootPath, "fonts", "PlayfairDisplay-Regular.ttf");
QuestPDF.Infrastructure.FontManager.RegisterFont(File.OpenRead(playfairPath));
// repeat for each weight/style variant

// Usage inside page:
col.Item().Text("Certificate of Completion")
    .FontFamily("Playfair Display").FontSize(48).Bold().FontColor("#1a4a8d");
```

### Pattern 3: Auth Guard Replication

**What:** Copy the four-guard sequence from `Certificate(int id)` verbatim into `CertificatePdf(int id)`.

**Sequence:**
1. `FirstOrDefaultAsync` by id → 404 if null
2. `GetUserAsync` → `Challenge()` if null
3. `GetRolesAsync` → owner or Admin/HC → `Forbid()` if not authorized
4. `Status != "Completed"` → redirect to Assessment list
5. `!GenerateCertificate` → 404
6. `IsPassed != true` → redirect to Results

### Pattern 4: Filename Sanitisation

```csharp
// Replace non-alphanumeric characters (except underscore) with underscore
var safeTitle = Regex.Replace(assessment.Title, @"[^a-zA-Z0-9]", "_");
var nip = assessment.User?.NIP ?? user.Id;
var year = (assessment.CompletedAt ?? DateTime.UtcNow).Year;
var filename = $"Sertifikat_{nip}_{safeTitle}_{year}.pdf";
```

### Pattern 5: Completion Date Selection

`AssessmentSession` has `CompletedAt` (DateTime?) and `UpdatedAt` (DateTime?). `CompletedAt` is the most semantically correct field — it records when the exam was completed. Use `CompletedAt ?? UpdatedAt ?? CreatedAt` as fallback chain. Format with `CultureInfo.GetCultureInfo("id-ID")` for Indonesian month names, consistent with existing Certificate.cshtml.

### Pattern 6: QuestPDF Visual Layout for Certificate

QuestPDF renders in a box model. Certificate visual areas map as:
- **Outer border:** `page.Background()` + absolute layer or `Border()` on root container
- **Watermark:** QuestPDF does not have native opacity for SVG overlay; best approach is to render a PNG watermark image with low opacity baked in, OR skip SVG watermark and use a simple styled shape
- **Score badge:** Absolutely-positioned circle is not directly supported in QuestPDF flow model — use `page.Foreground()` layer with a fixed-size container aligned bottom-right
- **Footer split (date-left, signature-right):** `Row` with two `RelativeColumn` items

### Anti-Patterns to Avoid

- **Registering fonts inside `Document.Create`:** Font registration must happen before `Document.Create` is called.
- **Calling `FontManager.RegisterFont` on every request with the same font:** QuestPDF caches internally, but registering duplicate fonts is safe — avoid only if performance profiling shows it as a bottleneck (it won't for this use case).
- **Using `DateTime.Now` for Date of Issue:** Locked decision is to use `CompletedAt` (actual completion date). `DateTime.Now` breaks consistency across re-downloads.
- **Streaming the PDF directly without materialising to array:** `pdf.GeneratePdf(pdfStream)` writes to the stream; call `pdfStream.ToArray()` before returning — the existing CDPController pattern already does this correctly.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PDF generation | Custom HTML-to-image pipeline | QuestPDF `Document.Create` | Already in project; handles pagination, font embedding, PDF spec compliance |
| Font management | Bundling fonts as base64 strings | `FontManager.RegisterFont(stream)` | QuestPDF's official font API; handles subsetting automatically |
| Content-Disposition header | Manual header manipulation | `File(bytes, "application/pdf", filename)` | ASP.NET Core `File()` sets Content-Disposition: attachment automatically |

---

## Common Pitfalls

### Pitfall 1: SVG Watermark — QuestPDF Cannot Render Arbitrary SVG

**What goes wrong:** QuestPDF does not have a native SVG renderer. Passing the triangle SVG from Certificate.cshtml directly will fail.

**Why it happens:** QuestPDF's image API accepts raster formats (PNG, JPEG) or its own vector primitives, not arbitrary SVG markup.

**How to avoid:** Two options —
1. Pre-render the triangle SVG to a PNG with a transparent background at 5% opacity and embed as a PNG byte array at compile time (simplest).
2. Use QuestPDF's `Canvas` API (Skia-based) to draw the triangle programmatically: three `DrawLine` calls or a `DrawPath`.

**Warning signs:** `NotSupportedException` or missing image at runtime when attempting to pass SVG bytes to `.Image()`.

### Pitfall 2: Font Family Name Must Match Registered Name Exactly

**What goes wrong:** `.FontFamily("Playfair Display")` fails silently (falls back to default font) if the registered TTF declares a different internal family name.

**Why it happens:** QuestPDF uses the font's internal metadata name, not the filename.

**How to avoid:** After registering, verify the family name by checking the TTF's `nameTable` — or just use the exact Google Fonts family name, which matches the CSS family name used in Certificate.cshtml.

### Pitfall 3: `IWebHostEnvironment` Not Available via Constructor Injection in CMPController

**What goes wrong:** CMPController may not already have `IWebHostEnvironment` injected if existing actions don't need it.

**Why it happens:** It must be added to the constructor parameters if not present.

**How to avoid:** Check CMPController constructor. If `IWebHostEnvironment` is missing, add `IWebHostEnvironment env` parameter and store as `_env`. Alternatively, resolve from `HttpContext.RequestServices` inline (avoids constructor change).

### Pitfall 4: QuestPDF Community License Mode

**What goes wrong:** QuestPDF prints a license warning to console in non-production builds unless settings configured.

**Why it happens:** QuestPDF 2024+ requires explicit community license acknowledgement.

**How to avoid:** The project likely already calls `QuestPDF.Settings.License = LicenseType.Community;` somewhere (CDPController usage works, so it must be configured). Verify in `Program.cs`. If not found, add it there.

---

## Code Examples

### Complete CertificatePdf Action Scaffold

```csharp
// Source: Derived from CDPController pattern + Certificate auth guard pattern
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
    bool isAuthorized = assessment.UserId == user.Id ||
                        userRoles.Contains("Admin") ||
                        userRoles.Contains("HC");
    if (!isAuthorized) return Forbid();

    if (assessment.Status != "Completed")
    {
        TempData["Error"] = "Assessment not completed yet.";
        return RedirectToAction("Assessment");
    }

    if (!assessment.GenerateCertificate) return NotFound();

    if (assessment.IsPassed != true)
    {
        TempData["Error"] = "Certificate is only available for passed assessments.";
        return RedirectToAction("Results", new { id });
    }

    // Register fonts
    var fontsPath = Path.Combine(_env.WebRootPath, "fonts");
    QuestPDF.Infrastructure.FontManager.RegisterFont(
        System.IO.File.OpenRead(Path.Combine(fontsPath, "PlayfairDisplay-Regular.ttf")));
    // ... register other weights

    var completedAt = assessment.CompletedAt ?? assessment.UpdatedAt ?? assessment.CreatedAt;
    var dateStr = completedAt.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"));

    var pdf = QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
            page.Margin(15); // approx 10mm matching HTML border inset
            page.Content().Column(col =>
            {
                // ... certificate layout
            });
        });
    });

    var pdfStream = new MemoryStream();
    pdf.GeneratePdf(pdfStream);

    var nip = assessment.User?.NIP ?? user.Id;
    var safeTitle = Regex.Replace(assessment.Title, @"[^a-zA-Z0-9]", "_");
    var year = completedAt.Year;
    var filename = $"Sertifikat_{nip}_{safeTitle}_{year}.pdf";

    return File(pdfStream.ToArray(), "application/pdf", filename);
}
```

### Download PDF Button for Certificate.cshtml

```html
<!-- Add after existing Print button in .no-print div -->
<a href="@Url.Action("CertificatePdf", "CMP", new { id = Model.Id })"
   class="btn-print"
   style="background: #198754; margin-left: 10px;"
   download>
    <i class="bi bi-download"></i> Download PDF
</a>
```

### HTML Certificate.cshtml Updates (date + new fields)

```razor
@* Replace DateTime.Now *@
@{
    var completedAt = Model.CompletedAt ?? Model.UpdatedAt ?? Model.CreatedAt;
    var date = completedAt.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"));
}

@* Below recipient name — add NIP *@
@if (!string.IsNullOrEmpty(Model.User?.NIP))
{
    <div style="font-size: 18px; color: #555;">NIP: @Model.User.NIP</div>
}

@* In footer date-section — add NomorSertifikat and ValidUntil *@
@if (!string.IsNullOrEmpty(Model.NomorSertifikat))
{
    <div style="font-size: 14px; color: #666;">No. Sertifikat: @Model.NomorSertifikat</div>
}
@if (Model.ValidUntil.HasValue)
{
    <div style="font-size: 14px; color: #666;">
        Berlaku Hingga: @Model.ValidUntil.Value.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"))
    </div>
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| DateTime.Now for Date of Issue | Use CompletedAt (actual completion date) | Phase 194 | Consistent certificates across re-downloads |
| No PDF download | Server-rendered QuestPDF download | Phase 194 | Users get a portable, printable certificate file |

---

## Open Questions

1. **Triangle watermark rendering in QuestPDF**
   - What we know: QuestPDF cannot render raw SVG. Options are pre-rendered PNG or Skia Canvas.
   - What's unclear: Whether the project already has any PNG assets, or whether Skia Canvas API is clean for this shape.
   - Recommendation: Implement the triangle as a Skia Canvas draw call (three path points) inside `page.Background()` at 5% opacity — this avoids adding a binary asset to the repo.

2. **`IWebHostEnvironment` in CMPController constructor**
   - What we know: CDPController injects `IWebHostEnvironment` to read logo images (seen in line 2539 reference).
   - What's unclear: Whether CMPController already has it.
   - Recommendation: Check constructor before planning. If absent, add as constructor parameter.

3. **QuestPDF Community License configuration location**
   - What we know: CDPController QuestPDF usage works, so license must be set somewhere.
   - Recommendation: Search `Program.cs` for `LicenseType.Community`. If not found, add `QuestPDF.Settings.License = LicenseType.Community;` as the first line in `Program.cs`.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project pattern) |
| Config file | none |
| Quick run command | n/a — UI phase |
| Full suite command | n/a — UI phase |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CERT-03 | Clicking "Download PDF" returns a valid PDF file | manual browser | n/a | n/a |
| CERT-03 | PDF filename matches Sertifikat_{NIP}_{Title}_{Year}.pdf | manual browser | n/a | n/a |
| CERT-03 | Non-owner cannot download certificate via direct URL | manual browser | n/a | n/a |
| CERT-03 | Non-passed assessment returns error/redirect | manual browser | n/a | n/a |

### Sampling Rate
- **Per task:** Build passes (`dotnet build`) — no runtime errors
- **Phase gate:** Manual browser verification before `/gsd:verify-work`

### Wave 0 Gaps
None — no automated test infrastructure required for UI phase.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` lines 2227-2283 — QuestPDF Document.Create pattern in use
- `Controllers/CMPController.cs` lines 2326-2364 — Certificate auth guard pattern to replicate
- `Views/CMP/Certificate.cshtml` — Visual reference; all CSS values, layout structure
- `Models/AssessmentSession.cs` — CompletedAt, NomorSertifikat, ValidUntil, IsPassed, GenerateCertificate, Score fields
- `HcPortal.csproj` line 23 — QuestPDF 2026.2.2 confirmed installed

### Secondary (MEDIUM confidence)
- QuestPDF font registration: `FontManager.RegisterFont(Stream)` is documented API; usage consistent with known QuestPDF patterns

### Tertiary (LOW confidence)
- Triangle watermark via Skia Canvas — approach is plausible based on QuestPDF Skia dependency, but not verified against current QuestPDF 2026.x docs

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — QuestPDF version confirmed in csproj; no new dependencies
- Architecture: HIGH — both scaffold patterns (CDPController PDF, Certificate auth guard) exist in source
- Font embedding: MEDIUM — QuestPDF FontManager API is standard but not yet used in this project; registration flow well-known
- SVG watermark: LOW — QuestPDF SVG support not confirmed; Skia Canvas approach is plausible workaround

**Research date:** 2026-03-17
**Valid until:** 2026-04-17 (QuestPDF is stable; 30-day window)
