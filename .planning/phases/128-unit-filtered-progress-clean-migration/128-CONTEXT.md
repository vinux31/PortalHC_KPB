# Phase 128: Unit-Filtered Progress & Clean Migration - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Progress data contains only deliverables matching the coachee's assignment unit. AutoCreateProgressForAssignment filters by AssignmentUnit + migration wipes and recreates all progress data with correct unit filter applied.

Requirements: PROG-01, MIG-01, MIG-02

</domain>

<decisions>
## Implementation Decisions

### Unit fallback logic
- AssignmentUnit null → fallback ke ApplicationUser.Unit milik coachee
- Kalau dua-duanya null/kosong → skip, jangan buat progress. Tampilkan warning ke admin: "Progress tidak dibuat untuk [nama coachee] karena unit belum diset"
- Kalau resolved unit tidak match ProtonKompetensi.Unit manapun di track → skip + warning: "Tidak ada deliverable untuk unit [X] di track [Y]"
- Resolusi unit dilakukan di dalam method AutoCreateProgressForAssignment (bukan di caller)

### Migration safety
- Tidak perlu backup table — data existing adalah testing/manual insert (keputusan Phase 127)
- Langsung wipe: DELETE semua ProtonDeliverableProgress, CoachingSessions (yang punya ProgressId), DeliverableStatusHistory
- Recreate dari active ProtonTrackAssignment dengan unit filter
- Migration baru dibuat setelah migration existing (20260306101100_MoveSectionHeadToLevel4) — urutan timestamp normal
- Migration log summary ke console output (PRINT/RAISERROR): "Deleted X progress, Y sessions, Z histories. Created N new progress rows for M assignments."

### Unit matching
- Exact string match + trim whitespace — konsisten dengan pattern existing di ProtonDataController
- Tidak case-insensitive (data konsisten dari SaveSilabus)

### Logging & audit trail
- Runtime auto-create TIDAK perlu log ke AuditLog — assign/edit mapping sudah di-log, auto-create adalah side effect
- Migration summary log ke console saja (sudah di atas)

### Claude's Discretion
- Recreate approach: SQL langsung di migration atau custom migration code via DbContext — pilih yang terbaik
- Migration safety checks dan error handling
- Exact implementation detail dari warning message di UI

</decisions>

<specifics>
## Specific Ideas

- AutoCreateProgressForAssignment (AdminController line 5231) saat ini query deliverable by protonTrackId saja — tambah filter `.Where(d => d.ProtonSubKompetensi.ProtonKompetensi.Unit == resolvedUnit)`
- Warning ditampilkan di TempData setelah assign/edit mapping (existing pattern untuk flash messages)
- Satu coachee bisa punya 1 active ProtonTrackAssignment pada satu waktu

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AutoCreateProgressForAssignment` (AdminController:5231): Method existing, perlu tambah unit filter + fallback logic
- `CleanupProgressForAssignment` (AdminController:5250): Cascade delete pattern sudah ada — reuse untuk migration logic
- `CoachCoacheeMapping.AssignmentUnit` (nullable string): Field sudah ada dari Phase 123

### Established Patterns
- Unit matching di ProtonDataController selalu exact string match (`k.Unit == unit`)
- Flash messages via TempData["Success"] / TempData["Error"] di AdminController
- Migration pakai raw SQL untuk data operations (pattern dari Phase 127)
- AuditLog pattern: `_auditLog.LogAsync(actor.Id, actor.FullName, action, details)`

### Integration Points
- `AdminController.CoachCoacheeMappingAssign` (line ~2989): Panggil AutoCreateProgress setelah assign
- `AdminController.CoachCoacheeMappingEdit` (line ~3060): Panggil AutoCreateProgress setelah edit
- `ApplicationUser.Unit`: Source untuk fallback unit (perlu query user data)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 128-unit-filtered-progress-clean-migration*
*Context gathered: 2026-03-08*
