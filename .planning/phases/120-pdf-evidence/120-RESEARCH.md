# Phase 120: PDF Evidence - Research

**Researched:** 2026-03-08
**Domain:** QuestPDF document generation in ASP.NET Core
**Confidence:** HIGH

## Summary

Phase 120 adds a "Download PDF" button to the Deliverable detail page's Evidence Coach card. The PDF is generated on-demand using QuestPDF (already installed v2026.2.2) with an existing pattern in `ExportCoachingProtonPdf` (CDPController line ~2184). The new action loads the latest CoachingSession for a given progressId, formats it as a portrait A4 form with label-value pairs, and renders the Coach's P-Sign badge using QuestPDF layout primitives (recreating the `_PSign.cshtml` visual).

This is a straightforward phase: one new controller action, one button added to the view, and QuestPDF layout code. All data models and dependencies already exist.

**Primary recommendation:** Follow the existing `ExportCoachingProtonPdf` pattern exactly -- same Document.Create/MemoryStream/File() flow, but portrait A4 with form-style layout instead of landscape table.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Portrait A4, form-style (single page, fields stacked vertically)
- Header: Pertamina logo centered at top + "EVIDENCE COACHING REPORT" title
- Fields as label-value pairs: Nama Coachee, Track, Kompetensi, Sub Kompetensi, Deliverable, Tanggal Coaching, Catatan Coach, Kesimpulan, Result
- P-Sign badge: bottom-left corner, Coach's P-Sign only (1 signer)
- On-demand generation, no storage, always latest DB data
- New action in CDPController alongside ExportCoachingProtonPdf
- Anyone who can view the Deliverable detail page can download the PDF
- Button inside Evidence Coach card (Card 3), btn-outline-secondary, label "PDF Evidence Report"
- Filename: Evidence_{CoacheeName}_{Deliverable}_{Date}.pdf
- Footer: "Generated: {date time} -- Page 1 of 1 -- PortalHC KPB"
- Pertamina blue accent (header bar, field labels)
- Empty fields show "-" dash placeholder
- P-Sign missing Position/Unit: hide missing rows

### Claude's Discretion
- Exact QuestPDF layout code and spacing
- Blue accent shade and exact font sizes
- P-Sign rendering in QuestPDF (recreate from _PSign.cshtml)
- Error response if no coaching session exists

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PDF-01 | Auto-generate PDF form after coach submits evidence | Existing QuestPDF pattern in ExportCoachingProtonPdf; on-demand generation via new DownloadEvidencePdf action |
| PDF-02 | PDF contains: Coachee info, Track, Kompetensi, SubKompetensi, Deliverable, Tanggal, Catatan Coach, Kesimpulan, Result | All fields available from ProtonDeliverableProgress (with includes) + CoachingSession model |
| PDF-03 | PDF has P-Sign Coach at bottom-left | PSignViewModel has LogoUrl/Position/Unit/FullName; logo at wwwroot/images/psign-pertamina.png; recreate _PSign.cshtml layout in QuestPDF |
| PDF-04 | Download from Deliverable detail page via "Download PDF" button | Button goes in Card 3 (Evidence Coach) in Deliverable.cshtml, after coaching session data |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| QuestPDF | v2026.2.2 | PDF generation | Already installed, used by ExportCoachingProtonPdf |

### Supporting
No new dependencies needed. Everything is already in the project.

## Architecture Patterns

### Existing Pattern: ExportCoachingProtonPdf (CDPController line ~2184)
```csharp
// Pattern: Document.Create → MemoryStream → File()
var pdf = QuestPDF.Fluent.Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(QuestPDF.Helpers.PageSizes.A4); // .Landscape() for existing, Portrait for new
        page.Margin(20);
        page.Content().Column(col => { /* layout */ });
    });
});
var pdfStream = new MemoryStream();
pdf.GeneratePdf(pdfStream);
return File(pdfStream.ToArray(), "application/pdf", "filename.pdf");
```

### New Action Structure: DownloadEvidencePdf
```
Route: CDP/DownloadEvidencePdf?progressId={id}
1. Load user, check access (same as Deliverable action)
2. Load ProtonDeliverableProgress with includes (Deliverable → SubKompetensi → Kompetensi → Track)
3. Load latest CoachingSession for this progressId
4. Load Coach user (from CoachingSession.CoachId) for P-Sign data
5. Build QuestPDF document (portrait A4, form layout)
6. Return File()
```

### Data Loading (all fields available)
```
From ProtonDeliverableProgress + includes:
- Coachee name: _context.Users.Find(progress.CoacheeId).FullName
- Track: progress.ProtonDeliverable.ProtonSubKompetensi.ProtonKompetensi.ProtonTrack.TrackType + TahunKe
- Kompetensi: ...ProtonKompetensi.NamaKompetensi
- SubKompetensi: ...ProtonSubKompetensi.NamaSubKompetensi
- Deliverable: ...ProtonDeliverable.NamaDeliverable

From CoachingSession (latest for this progressId):
- Tanggal: session.Date
- CatatanCoach: session.CatatanCoach
- Kesimpulan: session.Kesimpulan
- Result: session.Result

From ApplicationUser (coach):
- Position, Unit, FullName for P-Sign
```

### P-Sign in QuestPDF
Recreate the `_PSign.cshtml` layout using QuestPDF primitives:
```
- Container with border (1.5pt grey)
- Centered: Logo image (load from wwwroot/images/psign-pertamina.png as byte[])
- Centered: Position text (if not empty)
- Centered: Unit text (if not empty)
- Centered: FullName (bold)
- Width ~240px (~6cm)
```

Logo loading: `System.IO.File.ReadAllBytes(Path.Combine(env.WebRootPath, "images", "psign-pertamina.png"))`

### View Change (Deliverable.cshtml)
Add download button after the coaching session foreach loop (line ~338), before card closing div:
```html
@if (coachingSessions65.Any())
{
    <a asp-controller="CDP" asp-action="DownloadEvidencePdf"
       asp-route-progressId="@Model.Progress.Id"
       class="btn btn-outline-secondary mt-2">
        <i class="bi bi-file-pdf me-1"></i>PDF Evidence Report
    </a>
}
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PDF generation | HTML-to-PDF, custom streams | QuestPDF fluent API | Already in project, well-tested |
| Image embedding | Base64 in HTML | QuestPDF .Image(bytes) | Native support, no encoding overhead |

## Common Pitfalls

### Pitfall 1: Forgetting to load logo as bytes
**What goes wrong:** QuestPDF needs byte[] for images, not file paths
**How to avoid:** Load `File.ReadAllBytes()` once before document creation, inject via `IWebHostEnvironment`

### Pitfall 2: Access control mismatch
**What goes wrong:** New action has different access rules than Deliverable page
**How to avoid:** Copy access control logic from Deliverable action (lines 756-790) exactly

### Pitfall 3: No coaching session exists
**What goes wrong:** Button visible but no data to generate PDF
**How to avoid:** Button is conditional on `coachingSessions65.Any()` so this should not happen. Add server-side guard returning NotFound() as safety net.

### Pitfall 4: Filename special characters
**What goes wrong:** Coachee name or deliverable with special chars breaks filename
**How to avoid:** Replace spaces with underscores, strip other special chars

## Code Examples

### QuestPDF Form-Style Label-Value Row
```csharp
// Reusable helper for label-value pairs
void AddField(ColumnDescriptor col, string label, string value, string accentColor)
{
    col.Item().Row(row =>
    {
        row.RelativeItem(1).Background(accentColor).Padding(6)
            .Text(label).FontSize(9).Bold().FontColor("#FFFFFF");
        row.RelativeItem(2).BorderBottom(1).BorderColor("#dee2e6").Padding(6)
            .Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(10);
    });
}
```

### QuestPDF Image Embedding
```csharp
var logoBytes = System.IO.File.ReadAllBytes(
    Path.Combine(_env.WebRootPath, "images", "psign-pertamina.png"));
// In document:
col.Item().Image(logoBytes).FitWidth();
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PDF-01 | PDF generated on download click | manual-only | Navigate to deliverable with evidence, click PDF button | N/A |
| PDF-02 | PDF contains all required fields | manual-only | Open downloaded PDF, verify all fields present | N/A |
| PDF-03 | P-Sign badge in PDF | manual-only | Check bottom-left of PDF for P-Sign | N/A |
| PDF-04 | Download button in Card 3 | manual-only | View deliverable detail page with evidence | N/A |

### Sampling Rate
- Per task: manual browser verification
- Phase gate: download PDF and visually inspect all fields + P-Sign

### Wave 0 Gaps
None -- no automated test infrastructure in this project; all verification is manual browser testing.

## Sources

### Primary (HIGH confidence)
- Existing codebase: CDPController.cs ExportCoachingProtonPdf (line 2184-2241) -- verified QuestPDF pattern
- Existing codebase: Models/PSignViewModel.cs -- P-Sign data structure
- Existing codebase: Views/Shared/_PSign.cshtml -- P-Sign visual reference
- Existing codebase: Models/CoachingSession.cs -- all required fields confirmed
- Existing codebase: Models/ProtonModels.cs -- ProtonDeliverableProgress with full hierarchy
- Existing codebase: Views/CDP/Deliverable.cshtml -- Card 3 structure confirmed (line 279-341)
- Existing codebase: wwwroot/images/psign-pertamina.png -- logo file confirmed

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - QuestPDF already installed and used in same controller
- Architecture: HIGH - direct clone of existing pattern with layout changes
- Pitfalls: HIGH - straightforward implementation, risks well-understood

**Research date:** 2026-03-08
**Valid until:** 2026-04-08 (stable, no external dependencies)
