# Phase 115: Hard Delete + Consumer Audit - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can permanently remove incorrectly entered Kompetensi master data with full cascade delete. All silabus consumers (PlanIdp, CoachingProton, Status tab) must remain intact after deletion. Delete is always allowed — no blocking, full cascade including progress and coaching sessions.

</domain>

<decisions>
## Implementation Decisions

### Delete button placement
- View mode only — Delete button appears alongside existing Nonaktifkan/Aktifkan button
- Style: btn-outline-danger with bi-trash icon + "Hapus" text
- Appears on ALL Kompetensi rows (both active and inactive)
- Nonaktifkan/Aktifkan button remains — both buttons coexist

### Confirmation dialog
- AJAX pre-check before opening modal: click Delete -> fetch counts + cascade info -> show modal
- Dialog shows ALL cascade counts in one list: SubKompetensi, Deliverable, ProtonDeliverableProgress, CoachingSession — no special coloring, semua counts ditampilkan sama rata
- No blocking — delete selalu diizinkan, tidak peduli ada progress atau coaching session
- Full cascade delete: Kompetensi -> SubKompetensi -> Deliverable -> ProtonDeliverableProgress -> CoachingSession (dan data terkait)

### Consumer audit
- Code-level review during research phase — trace semua code paths yang mereferensi ProtonKompetensi/SubKompetensi/Deliverable
- Consumers: PlanIdp, CoachingProton, Status tab (Phase 114)
- Consumer pages harus gracefully handle data yang sudah tidak ada (hide orphan references)

### Post-delete behavior
- Setelah delete sukses: panggil ulang loadSilabusData() untuk refresh tabel Silabus
- Tidak perlu toast/notifikasi sukses — tabel ter-refresh sudah cukup sebagai feedback
- Status tab update saat user klik tab (sudah re-fetch otomatis dari Phase 114)
- Kalau delete gagal: tampilkan alert-danger di dalam modal yang masih terbuka

### Claude's Discretion
- Cascade delete implementation approach (EF Core cascade vs manual delete)
- Consumer page orphan handling strategy (null checks, LINQ filtering)
- Modal HTML structure details

</decisions>

<specifics>
## Specific Ideas

- Confirmation dialog format: "Hapus Kompetensi [nama]? Ini akan menghapus X Sub Kompetensi, Y Deliverable, Z Progress Coaching, W Sesi Coaching."
- Error di modal: alert-danger inline "Gagal menghapus: [pesan error]"
- Existing delete modal patterns available: silabusDeleteModal (for deliverable rows), guidanceDeleteModal

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `silabusDeleteModal` (Index.cshtml:157): Existing delete confirmation modal pattern for deliverable rows
- `guidanceDeleteModal` (Index.cshtml:248): Another delete modal pattern for guidance files
- View mode action buttons (Index.cshtml:428-434): Existing Nonaktifkan/Aktifkan buttons per Kompetensi row with data-id and data-name attributes

### Established Patterns
- AJAX actions: ProtonDataController uses JSON endpoints with jQuery $.post for mutations
- Silabus tab reload: loadSilabusData() function already exists for refreshing table content
- Row action buttons use btn-sm btn-outline-* with bi-* icons

### Integration Points
- ProtonDataController.cs: New DeleteKompetensi action (pre-check endpoint + delete endpoint)
- Views/ProtonData/Index.cshtml: New delete modal + button in view mode action column
- Cascade chain: ProtonKompetensi -> SubKompetensi -> Deliverable -> ProtonDeliverableProgress -> CoachingSession
- Consumer pages to audit: CDPController (PlanIdp, CoachingProton), ProtonDataController (StatusData)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 115-hard-delete-consumer-audit*
*Context gathered: 2026-03-07*
