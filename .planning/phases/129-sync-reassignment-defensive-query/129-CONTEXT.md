# Phase 129: Sync, Reassignment & Defensive Query - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

All secondary progress-creation paths respect unit scoping, and unit changes trigger automatic progress rebuild. Covers: SilabusSave auto-sync (PROG-02), edit-mapping reassignment handler (REASSIGN-01), and CoachingProton defensive query filter (QUERY-01).

</domain>

<decisions>
## Implementation Decisions

### SilabusSave auto-sync (PROG-02)
- Saat HC tambah deliverable baru di silabus, sync progress hanya ke assignments yang AssignmentUnit match dengan ProtonKompetensi.Unit deliverable baru
- Auto-sync logic ditambahkan di akhir SilabusSave method (setelah semua upsert selesai)
- Saat HC hapus deliverable dari silabus, cascade delete semua ProtonDeliverableProgress + sessions + history yang merujuk ke deliverable itu
- Reuse unit matching pattern dari Phase 128: exact string match + trim

### Reassignment handler (REASSIGN-01)
- Saat admin edit mapping dan ubah AssignmentUnit, langsung rebuild progress tanpa konfirmasi
- Rebuild = CleanupProgressForAssignment (hapus old) + AutoCreateProgressForAssignment (buat baru dengan unit baru)
- Feedback ke admin via TempData: "Unit berubah → X progress dihapus, Y progress baru dibuat untuk unit [Z]"
- Hanya trigger rebuild kalau AssignmentUnit berubah (bukan setiap field change)

### Defensive query filter (QUERY-01)
- Filter di query level (WHERE clause) — tambah .Where() di LINQ query, data salah tidak pernah sampai ke view
- Filter ditambahkan di SEMUA query path: CoachingProton, FilterCoachingProton, ExportCoachingProton, HistoriProton
- Orphan data (jika ada) di-silent filter out, tidak perlu log atau warning
- Filter: ProtonKompetensi.Unit == assignment's resolved unit

### Claude's Discretion
- Apakah rebuild juga terjadi saat hanya AssignmentSection berubah (tanpa unit change) — pilih pendekatan terbaik
- Exact implementation detail auto-sync di SilabusSave (detect deliverable baru vs existing)
- Ordering of cleanup vs create dalam reassignment flow

</decisions>

<specifics>
## Specific Ideas

- SilabusSave (ProtonDataController line ~194) saat ini tidak ada auto-sync — ini fitur baru
- CoachCoacheeMappingEdit (AdminController line ~3038) sudah set AssignmentUnit, perlu detect perubahan dan trigger rebuild
- AutoCreateProgressForAssignment sudah ada unit filter dari Phase 128 — reuse langsung
- CleanupProgressForAssignment sudah ada cascade delete dari Phase 128 — reuse langsung

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AutoCreateProgressForAssignment` (AdminController): Sudah ada unit filter + fallback logic dari Phase 128
- `CleanupProgressForAssignment` (AdminController): Cascade delete progress + sessions + history dari Phase 128
- `ProtonDataController.SilabusSave` (line ~194): Batch upsert silabus rows, tempat tambah auto-sync
- `CDPController.CoachingProton` (line ~1135): Main query, tempat tambah defensive filter
- `CDPController.FilterCoachingProton` (line ~263): AJAX filter endpoint
- `CDPController.ExportCoachingProton`: Excel export endpoint
- `CDPController.HistoriProton`: Per-coachee history

### Established Patterns
- Unit matching: exact string match + trim (Phase 128)
- Unit fallback: AssignmentUnit → User.Unit → skip (Phase 128)
- TempData flash messages untuk feedback ke admin
- Cascade delete via explicit query (not EF cascade)

### Integration Points
- `AdminController.CoachCoacheeMappingEdit`: Detect unit change → trigger rebuild
- `ProtonDataController.SilabusSave`: Detect new deliverables → sync to matching assignments
- `CDPController` query methods: Add unit filter WHERE clause

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 129-sync-reassignment-defensive-query*
*Context gathered: 2026-03-08*
