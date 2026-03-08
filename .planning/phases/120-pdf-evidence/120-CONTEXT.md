# Phase 120: PDF Evidence - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Coach can download a professional PDF evidence form after submitting coaching evidence. PDF is generated on-demand using QuestPDF, contains coaching session data for a single deliverable, and includes the Coach's P-Sign badge. No PDF storage — generated fresh each download.

</domain>

<decisions>
## Implementation Decisions

### PDF layout & design
- Portrait A4, form-style (single page, fields stacked vertically)
- Header: Pertamina logo centered at top + "EVIDENCE COACHING REPORT" title
- Fields arranged as label-value pairs, stacked vertically
- Field order: Nama Coachee, Track, Kompetensi, Sub Kompetensi, Deliverable, Tanggal Coaching, Catatan Coach (multi-line), Kesimpulan, Result
- P-Sign badge: bottom-left corner, Coach's P-Sign only (1 signer)
- Field labels in Indonesian (matching app UI)

### Generation trigger & storage
- On-demand only — PDF generated fresh each time user clicks download button
- No file storage on disk, no caching
- Always reflects latest DB data (no snapshot)
- New action in CDPController (alongside existing ExportCoachingProtonPdf)
- Anyone who can view the Deliverable detail page can download the PDF

### Download button placement
- Inside Evidence Coach card (Card 3 on Deliverable detail page)
- Below the coaching session data
- Only visible when evidence/coaching session exists (card is conditional)
- Button style: btn-outline-secondary
- Button label: "PDF Evidence Report"

### PDF filename & branding
- Filename: Evidence_{CoacheeName}_{Deliverable}_{Date}.pdf (spaces replaced with underscores)
- Footer: "Generated: {date time} — Page 1 of 1 — PortalHC KPB"
- Color theme: Pertamina blue accent (header bar, field labels)
- Standard A4 margins (~2cm), print-friendly (blue accents light enough for B&W)

### Error handling
- P-Sign with missing Position/Unit: hide missing rows (same as _PSign.cshtml behavior)
- Empty coaching fields (Catatan, Kesimpulan): show "-" dash placeholder
- PDF only shows latest coaching session (not all sessions)

### Claude's Discretion
- Exact QuestPDF layout code and spacing
- Blue accent shade and exact font sizes
- P-Sign rendering in QuestPDF (recreate visual from _PSign.cshtml using QuestPDF API)
- Error response if no coaching session exists (should not happen since button is conditional)

</decisions>

<specifics>
## Specific Ideas

- Title is "EVIDENCE COACHING REPORT" (English) but field labels are Indonesian — matches mixed-language convention in the app
- Existing QuestPDF pattern in CDPController (ExportCoachingProtonPdf, line ~2184) serves as implementation reference
- P-Sign partial (_PSign.cshtml) defines the visual — recreate same layout using QuestPDF components (logo, position, unit, name)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `QuestPDF` package already installed (v2026.2.2) — no new dependency
- `PSignViewModel` at `Models/PSignViewModel.cs` — has LogoUrl, Position, Unit, FullName
- `_PSign.cshtml` at `Views/Shared/` — reference for P-Sign visual layout
- `ExportCoachingProtonPdf` in CDPController (line ~2184) — QuestPDF pattern reference

### Established Patterns
- QuestPDF Document.Create → container.Page → page.Content().Column() pattern
- MemoryStream → File() return for PDF downloads
- CoachingSession loaded via _context.CoachingSessions with ProtonDeliverableProgressId

### Integration Points
- `Views/CDP/Deliverable.cshtml` — add Download PDF button inside Evidence Coach card (Card 3)
- `Controllers/CDPController.cs` — new DownloadEvidencePdf action
- Route: `CDP/DownloadEvidencePdf?progressId={id}`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 120-pdf-evidence*
*Context gathered: 2026-03-08*
