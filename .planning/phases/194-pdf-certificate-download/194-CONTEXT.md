# Phase 194: PDF Certificate Download - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can download their assessment certificate as a server-rendered PDF file from the Certificate page. The PDF replicates the existing HTML certificate design using QuestPDF. Additionally, the HTML Certificate view is updated to include NomorSertifikat, NIP, ValidUntil, and the actual completion date (replacing DateTime.Now).

</domain>

<decisions>
## Implementation Decisions

### PDF Visual Fidelity
- Exact match of HTML certificate layout in QuestPDF A4 landscape
- Embed Google Fonts (Playfair Display + Lato) as TTF files, register with QuestPDF
- Include SVG triangle watermark at 5% opacity
- Exact circular score badge (blue circle, gold border, white score text) in bottom-right
- Identical hex colors: #1a4a8d (blue), #c49a00 (gold)
- Signature area: "Authorized Sig." in Playfair Display + signature line + "HC Manager" label
- Proficiency description text kept: "Demonstrating proficiency and understanding of the subject matter."

### Claude's Discretion (Visual)
- Border detail: whether to replicate exact double-border (outer blue solid + inner gold double) or simplify — pick what looks best in QuestPDF

### Certificate Content
- NIP displayed below recipient name
- NomorSertifikat displayed bottom-left near Date of Issue ("No. Sertifikat: XXX")
- ValidUntil displayed below Date of Issue ("Berlaku Hingga: dd MMMM yyyy") — omit line entirely if null
- NomorSertifikat line omitted if null (graceful degradation)
- Date of Issue uses actual assessment completion date, NOT DateTime.Now
- English language text (matching current HTML certificate)
- All content from HTML retained: HC PORTAL KPB header, "Certificate of Completion", "This verifies that", proficiency text, assessment title

### Claude's Discretion (Content)
- Which date field to use for completion date (CompletedAt, UpdatedAt, or best available on AssessmentSession)

### Download UX
- Green (#198754) "Download PDF" button with bi-download icon
- Placed next to existing Print button in the no-print toolbar (alongside Kembali and Print)
- Direct file download (Content-Disposition: attachment)
- Filename: Sertifikat_{NIP}_{Title}_{Year}.pdf
- Title sanitized: replace non-alphanumeric characters with underscore

### Auth & Edge Cases
- Same 4 auth guards as Certificate view: owner/Admin/HC role check, Completed status, GenerateCertificate=true, IsPassed=true
- Route: CMP/CertificatePdf/{id}
- Null NIP fallback: use user ID in filename
- No caching — generate fresh PDF on each request

### HTML Certificate View Updates
- Update HTML Certificate.cshtml to match PDF content
- Add NIP below recipient name
- Add NomorSertifikat near Date of Issue
- Add ValidUntil below Date of Issue (omit if null)
- Replace DateTime.Now with actual completion date
- Same positions as PDF for visual consistency

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Certificate system
- `Views/CMP/Certificate.cshtml` — Current HTML certificate layout (the visual reference for PDF replication)
- `Controllers/CMPController.cs` lines 2326-2364 — Certificate action with auth guards (copy these for CertificatePdf)
- `Models/AssessmentSession.cs` — NomorSertifikat, ValidUntil, IsPassed, GenerateCertificate, Score, Status fields

### QuestPDF reference implementation
- `Controllers/CDPController.cs` — Existing QuestPDF usage (Document.Create, A4 landscape, font sizing patterns)

### Requirements
- `.planning/REQUIREMENTS.md` — CERT-03 requirement for PDF certificate download

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- CDPController QuestPDF pattern: `Document.Create` with A4 landscape, margin, column layout, table cells — reuse as scaffold
- QuestPDF already in project dependencies (HcPortal.csproj)
- Certificate.cshtml: complete visual reference with CSS values for colors, fonts, spacing
- Bootstrap Icons CDN already linked for icon reference

### Established Patterns
- PDF generation: inline `Document.Create` lambda in controller action, return `File()` with content-type `application/pdf`
- Auth guard pattern: owner check + role check + status check + flag check (Certificate action)
- Date formatting: `CultureInfo.GetCultureInfo("id-ID")` for Indonesian date format

### Integration Points
- CMPController: new `CertificatePdf(int id)` action alongside existing `Certificate(int id)`
- Certificate.cshtml: add Download PDF button in `.no-print` div
- Font files: new TTF files need to be added to project (Playfair Display, Lato)

</code_context>

<specifics>
## Specific Ideas

- PDF should look like a printed version of the web certificate — user wants exact visual match
- Green download button differentiates from blue Print button
- Completion date instead of DateTime.Now ensures certificate consistency across downloads

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 194-pdf-certificate-download*
*Context gathered: 2026-03-17*
