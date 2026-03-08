# Phase 127: Audit & Fix CoachingProton Progress Table Data Source and Assignment Scoping - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit and fix the entire ProtonDeliverableProgress data flow: add auto-creation mechanism, link progress to assignment, cleanup orphan data, validate deliverable data against silabus, and rewrite Dashboard + CoachingProton page scoping to be assignment-based. This phase absorbs Phase 126 (scope progress to assignment context).

</domain>

<decisions>
## Implementation Decisions

### Mekanisme pembuatan progress (auto-create)
- Auto-create ProtonDeliverableProgress records saat admin assign ProtonTrackAssignment ke coachee
- Trigger: saat SaveChanges di assign modal (AdminController.CoachCoacheeMappingAssign / CoachCoacheeMappingEdit)
- Auto-create progress records untuk semua deliverable di track yang di-assign, status "Pending"
- Auto-sync: saat silabus diupdate (ProtonDataController.SaveSilabus), cek semua coachee dengan active assignment ke track itu, buat progress baru untuk deliverable yang belum ada. Tidak hapus progress existing.

### Progress ↔ Assignment linkage
- Tambah kolom `ProtonTrackAssignmentId` (FK) ke ProtonDeliverableProgress
- Migration strategy: Delete ALL existing ProtonDeliverableProgress records + recreate via auto-create based on active assignments. Data existing adalah testing/manual insert, tidak ada evidence sungguhan.
- Juga delete all CoachingSession yang punya ProtonDeliverableProgressId (orphan cleanup)
- Juga delete all DeliverableStatusHistory terkait progress yang dihapus
- Unique constraint diubah dari `(CoacheeId, ProtonDeliverableId)` ke `(ProtonTrackAssignmentId, ProtonDeliverableId)`

### Cleanup saat reassignment
- Saat coachee di-reassign ke track baru: hapus semua ProtonDeliverableProgress milik assignment lama + auto-create baru untuk track baru
- Hapus juga CoachingSession dan DeliverableStatusHistory terkait progress yang dihapus
- Tetap hapus semua bahkan kalau ada progress yang sudah Approved — reassign = fresh start
- ProtonFinalAssessment TETAP dipertahankan (FK ke assignment bukan ke progress, data historis kompetensi berharga)
- ProtonTrackAssignment lama tetap di DB (IsActive=false) — tidak dihapus

### Validasi deliverable vs silabus
- Query di CoachingProton page double-check bahwa deliverable's track cocok dengan assignment's track (belt and suspenders)
- Saat admin hapus deliverable dari silabus: cascade delete progress records yang merujuk ke deliverable itu
- Filter dropdown existing di CoachingProton sudah cukup, tidak perlu filter tambahan

### Dashboard scoping (absorb Phase 126)
- BuildProtonProgressSubModelAsync diubah ke assignment-based scoping
- Hanya tampilkan coachee yang punya active ProtonTrackAssignment
- Coach: lihat coachee dari CoachCoacheeMapping aktif yang punya assignment
- SectionHead/SrSpv: lihat semua coachee dengan assignment di section mereka
- HC/Admin: lihat semua coachee dengan assignment aktif
- Stats/chart dihitung dari progress yang terkait assignment aktif

### Claude's Discretion
- ProtonNotification cleanup saat reassign — biarkan atau hapus (notifikasi hanya informational, no FK to progress)
- Exact implementation of auto-sync trigger in SaveSilabus
- Migration ordering dan safety checks

</decisions>

<specifics>
## Specific Ideas

- Data ProtonDeliverableProgress saat ini di-insert manual (SQL) — tidak ada kode di aplikasi yang create records ini
- Baru unit RFCC NHT yang punya silabus di ProtonData, tapi progress records mungkin merujuk ke deliverable dari unit/bagian lain
- Satu coachee hanya bisa punya 1 active ProtonTrackAssignment pada satu waktu (existing behavior)
- Phase 126 digabung ke phase ini karena scope overlap

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController.CoachCoacheeMappingAssign` (line ~2960): Tempat assign track + create mapping. Tambah auto-create progress di sini.
- `AdminController.CoachCoacheeMappingEdit` (line ~3002): Edit mapping. Tambah reassign cleanup + auto-create di sini.
- `ProtonDataController.SaveSilabus` (line ~200): Save silabus. Tambah auto-sync progress di sini.
- Existing cascade delete logic di `ProtonDataController.DeleteKompetensi` (line ~860) — pattern untuk cleanup terkait.

### Established Patterns
- No FK constraint pattern: CoachingSession.ProtonDeliverableProgressId, ProtonNotification.CoacheeId — all string IDs tanpa FK
- ProtonTrackAssignment deactivation pattern: set IsActive=false, buat baru (not delete+recreate)
- Cascade delete via explicit query (not EF cascade) — matches ProtonDataController pattern

### Integration Points
- `CDPController.BuildProtonProgressSubModelAsync` (~line 336): Dashboard stats/chart — rewrite scoping
- `CDPController.CoachingProton` (~line 1130): Main progress table — add assignment-based query filter
- `CDPController.FilterCoachingProton` (~line 263): AJAX filter endpoint — same scoping fix
- `CDPController.Deliverable` — detail view
- `CDPController.SubmitCoachingSession` — submit evidence
- `CDPController.ApproveDeliverable / RejectDeliverable` — approval flow
- `CDPController.ExportCoachingProton` — Excel export
- `CDPController.HistoriProton` — per-coachee history
- `ProtonDataController.Override` — admin override
- `AdminController.GetEligibleCoachees` — exam eligibility check

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 127-audit-fix-coachingproton-progress-table-data-source-and-assignment-scoping*
*Context gathered: 2026-03-08*
