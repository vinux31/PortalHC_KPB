# Phase 118: P-Sign Infrastructure - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Build a reusable P-Sign (digital initial/paraf) component that renders any user's identity badge with Logo Pertamina, Position, Unit, and Full Name. Deliverable: Razor Partial View + ViewModel + CSS. No PDF generation (that's Phase 120).

</domain>

<decisions>
## Implementation Decisions

### Visual layout
- Vertical layout: Logo on top, then Position, then Unit, then Full Name
- Border box with rounded corners (digital "stamp" appearance)
- Width ~180px (medium size)
- Color theme: neutral/grey — grey border, black text
- Nama bold, Role+Unit regular weight
- No NIP displayed
- No "Ditandatangani secara digital" disclaimer text
- No date/timestamp in the badge itself

### Data source
- Position from ApplicationUser.Position (not role system display name)
- Unit from ApplicationUser.Unit
- FullName from ApplicationUser.FullName
- Scope: generic component for ALL users/roles, not Coach-only
- Fallback: hide rows with null/empty values (Position null = row hidden, Unit null = row hidden). P-Sign still renders with available data.

### Render mode
- Razor Partial View only: `Views/Shared/_PSign.cshtml`
- ViewModel: `PSignViewModel` with LogoUrl, Position, Unit, FullName
- Phase 120 will handle HTML-to-PDF conversion — P-Sign renders as part of the HTML page
- No separate image generation endpoint needed

### CSS
- Inline styles within `<style>` tag inside `_PSign.cshtml` — self-contained partial
- No external CSS file

### Logo asset
- File: `wwwroot/images/psign-pertamina.png` (rename from "P-Sign Pertamina.png" — remove spaces)
- Logo height ~40px inside the badge

### Settings preview
- Show P-Sign preview on Account/Settings page
- Position: Claude's discretion (natural placement within existing Settings layout)

### Claude's Discretion
- Exact spacing, padding, font sizes within the badge
- Settings page preview placement
- ViewModel location (Models/ or ViewModels/ — follow existing convention)

</decisions>

<specifics>
## Specific Ideas

- User already copied logo to `wwwroot/images/P-Sign Pertamina.png` — needs rename to `psign-pertamina.png`
- Pattern reference: Certificate.cshtml already has a signature section with logo (similar structure)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ApplicationUser` model: Already has `Position`, `Unit`, `FullName`, `NIP` fields — no schema changes needed
- `Views/CMP/Certificate.cshtml`: Has `.logo` and `.signature-section` CSS patterns for reference

### Established Patterns
- Partial views in `Views/Shared/` for cross-controller components (`_Layout.cshtml`, `_ValidationScriptsPartial.cshtml`)
- ViewModels as simple classes in `Models/` directory (e.g., `DashboardViewModel`)
- Inline `<style>` blocks used in several views (Certificate.cshtml, Progress.cshtml)

### Integration Points
- `Views/Account/Settings.cshtml`: Add P-Sign preview section
- `Views/Shared/_PSign.cshtml`: New partial view (consumed by Phase 120 PDF and Phase 119 Deliverable page)
- Any controller can pass `PSignViewModel` to views that `@Html.PartialAsync("_PSign", model)`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 118-p-sign-infrastructure*
*Context gathered: 2026-03-07*
