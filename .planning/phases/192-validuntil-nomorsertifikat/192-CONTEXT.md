# Phase 192: ValidUntil & NomorSertifikat - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can set a certificate expiry date when creating an assessment, and the system generates a unique certificate number automatically for each session when the assessment is created. ValidUntil property and wizard binding already exist from Phase 191 — this phase adds NomorSertifikat generation and ValidUntil propagation to per-session records.

</domain>

<decisions>
## Implementation Decisions

### Certificate Number Format
- Format: `KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}` — e.g., `KPB/042/III/2026`
- Sequence: 3 digits, zero-padded (001–999)
- Sequence resets per year (starts at 001 every January)
- Roman month based on assessment creation date (bulan saat Admin buat assessment)
- All sessions in same batch get same month (bulan pembuatan)

### Generation Timing
- NomorSertifikat generated at assessment creation time (saat Admin klik "Buat Assessment")
- Every AssessmentSession gets a number immediately — regardless of whether user passes exam later
- Concurrency handling: retry with next sequence number on UNIQUE constraint violation (max 3 retries)

### ValidUntil Mapping
- All sessions in a batch get the same ValidUntil date from the wizard
- ValidUntil is optional — null means no expiry
- ValidUntil property already exists on AssessmentSession (Phase 191)
- POST action already has ModelState.Remove("ValidUntil") (Phase 191)

### Edge Cases & Visibility
- Existing sessions (pre-Phase 192) keep NomorSertifikat = null — no backfill
- NomorSertifikat stored in DB only — no UI display in this phase (Records display deferred to later phase)
- New column: `NomorSertifikat` (string, nullable, UNIQUE constraint) on AssessmentSessions table

### Claude's Discretion
- Exact retry loop implementation for concurrency handling
- Roman numeral conversion helper method placement
- Whether to use a helper method or inline for sequence number generation
- Migration naming convention

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Assessment creation logic (primary modification target)
- `Controllers/AdminController.cs` — CreateAssessment POST action (lines ~947–1200): batch session creation loop where NomorSertifikat and ValidUntil must be assigned
- `Models/AssessmentSession.cs` — Entity model; ValidUntil already present (line 65), NomorSertifikat property needs to be added

### Phase 191 artifacts (just completed — wizard + ValidUntil)
- `.planning/phases/191-wizard-ui/191-RESEARCH.md` — Documents POST action structure, ModelState patterns, existing session creation loop

### Database context
- `Data/ApplicationDbContext.cs` — EF Core context; needs UNIQUE constraint configuration for NomorSertifikat

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ModelState.Remove("ValidUntil")` already in POST action (Phase 191)
- Session creation loop in AdminController POST: iterates `UserIds`, creates `AssessmentSession` per user — this is where NomorSertifikat assignment goes

### Established Patterns
- `ModelState.Remove()` for optional fields: ExamWindowCloseDate, ValidUntil — same pattern for NomorSertifikat (server-generated, not from form)
- EF Core migrations: standard `dotnet ef migrations add` workflow
- UNIQUE constraints: via `HasIndex().IsUnique()` in ApplicationDbContext

### Integration Points
- POST action session creation loop: each `new AssessmentSession { ... }` needs `NomorSertifikat = generatedNumber` and `ValidUntil = model.ValidUntil`
- Sequence number query: `SELECT MAX(seq) FROM AssessmentSessions WHERE year = @year` or similar to determine next number

</code_context>

<specifics>
## Specific Ideas

- Format mengikuti gaya surat resmi Indonesia: KPB/{SEQ}/{ROMAN-MONTH}/{YEAR}
- Angka Romawi untuk bulan: I, II, III, IV, V, VI, VII, VIII, IX, X, XI, XII
- Nomor sertifikat dibuat saat assessment dibuat, bukan saat user lulus ujian

</specifics>

<deferred>
## Deferred Ideas

- Tampilkan NomorSertifikat di halaman CMP Records — phase selanjutnya
- Backfill nomor sertifikat untuk session lama — tidak dilakukan
- NomorSertifikat di PDF sertifikat — Phase 194

</deferred>

---

*Phase: 192-validuntil-nomorsertifikat*
*Context gathered: 2026-03-17*
