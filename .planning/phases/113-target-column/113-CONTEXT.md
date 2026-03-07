# Phase 113: Target Column - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Add a Target text column to the silabus table in ProtonData/Index. Users can see Target in view mode and edit it in edit mode. Saved via existing SilabusSave endpoint. No other pages affected.

</domain>

<decisions>
## Implementation Decisions

### Data model placement
- Target column belongs to `ProtonSubKompetensi` entity (1 Target per SubKompetensi)
- Type: `string`, nvarchar(500)
- Migration sets default value `"-"` for all existing rows

### View mode display
- Target column positioned after SubKompetensi, before Deliverable
- Uses rowspan merged cell matching SubKompetensi's rowspan (since Target is per-SubKompetensi)

### Edit mode display
- Target field appears on every flat row (same as current edit layout — per Deliverable row)
- All rows in the same SubKompetensi share the same Target value
- On save, Target value taken from first row of each SubKompetensi group

### Validation
- Target is required — cannot be empty or whitespace
- Max length: 500 characters
- Existing data pre-filled with "-" via migration so save is not blocked
- Validation enforced on SilabusSave (server-side)

### Claude's Discretion
- Client-side validation UX (inline error vs alert)
- Exact input styling in edit mode

</decisions>

<specifics>
## Specific Ideas

No specific requirements — standard column addition following existing silabus patterns.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SilabusRowDto` (ProtonDataController.cs:11): Add Target property here
- `ProtonSubKompetensi` (ProtonModels.cs:42): Add Target property to this model
- `SilabusSave` action (ProtonDataController.cs:138): Update to persist Target field

### Established Patterns
- Edit mode uses flat rows with `data-field` attributes on inputs
- View mode uses rowspan for Kompetensi and SubKompetensi grouping
- `silabusRows` JS array holds all flat row data client-side

### Integration Points
- `Views/ProtonData/Index.cshtml`: Both view table (~line 323) and edit table (~line 410) need Target column
- `ProtonDataController.cs`: SilabusSave action maps DTO to entities
- Migration needed for `ProtonSubKompetensi.Target` column with default "-"

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 113-target-column*
*Context gathered: 2026-03-07*
