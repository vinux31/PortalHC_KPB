# Phase 236: Audit Completion - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Memastikan fase akhir perjalanan coachee (final assessment, coaching sessions, HistoriProton, lifecycle 3-tahun) akurat dan tidak bisa menghasilkan data duplikat atau inkonsisten. Audit + fix bugs yang ditemukan.

</domain>

<decisions>
## Implementation Decisions

### Final Assessment Duplikasi & Accuracy (COMP-01)
- **D-01:** Claude investigasi apakah unique constraint sudah ada di DB untuk ProtonTrackAssignmentId — jika belum, tambah migration + controller guard
- **D-02:** Claude audit existing CompetencyLevelGranted logic dan pastikan accuracy-nya
- **D-03:** Final assessment hanya bisa di-create oleh HC/Admin — role guard harus benar
- **D-04:** Jika final assessment sudah ada untuk assignment, block + pesan error "Final assessment sudah ada untuk assignment ini" — tidak boleh create duplikat

### Coaching Sessions Linkage (COMP-02)
- **D-05:** Session hanya di-create via SubmitEvidenceWithCoaching — tidak ada standalone session creation flow
- **D-06:** Claude audit existing action items dan pastikan status tracking konsisten — tidak perlu tambah state baru
- **D-07:** Session bisa diedit/dihapus tapi setiap perubahan harus tercatat di audit log
- **D-08:** Setiap session wajib ter-link ke 1 ProtonDeliverableProgressId — tidak boleh orphan session

### HistoriProton Timeline Accuracy (COMP-03)
- **D-09:** Claude investigasi apakah ada legacy CoachingLog data yang masih direferensikan di HistoriProton view
- **D-10:** Claude audit HistoriProtonDetail completeness — identifikasi gap/duplikasi di timeline
- **D-11:** Audit view DAN export — data di ExportHistoriProton harus konsisten dengan data di view
- **D-12:** Multi-year coachee: tampilkan data terpisah per tahun (Tahun 1 selesai, Tahun 2 ongoing sebagai section tersendiri)

### Lifecycle Tahun 1→2→3 (COMP-04)
- **D-13:** Completion criteria: semua ProtonDeliverableProgress di track status Approved **DAN** final assessment proton tahun tersebut sudah selesai/lulus
- **D-14:** Transisi antar tahun manual oleh HC/Admin — assign track tahun berikutnya secara manual
- **D-15:** Setelah coachee selesai Tahun 3 (completion criteria terpenuhi), mapping ditandai completed/graduated
- **D-16:** Competency level per tahun independen — setiap tahun punya CompetencyLevelGranted sendiri dari final assessment-nya

### Claude's Discretion
- Implementasi unique constraint migration detail
- Audit log mechanism untuk session edit/delete (existing AuditLog service atau tambahan)
- Completion status marker implementation (field baru atau status existing)
- Legacy CoachingLog handling approach berdasarkan investigasi
- HistoriProton gap fix detail berdasarkan audit findings

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone & Requirements
- `.planning/ROADMAP.md` — Phase 236 success criteria, dependency chain Phase 235→236
- `.planning/REQUIREMENTS.md` — COMP-01 through COMP-04 requirement definitions

### Phase 233 Research (lens untuk audit)
- `docs/audit-v7.7.html` — Dokumen riset perbandingan coaching platform, gap analysis completion flow
- `.planning/phases/233-riset-perbandingan-coaching-platform/233-CONTEXT.md` — Keputusan riset Phase 233

### Predecessor Decisions
- `.planning/phases/234-audit-setup-flow/234-CONTEXT.md` — Transaction patterns, progression validation (D-09: warning only), completion definition (D-10)
- `.planning/phases/235-audit-execution-flow/235-CONTEXT.md` — Race condition first-write-wins (D-10), evidence flow, approval chain, StatusHistory patterns

### Existing Code (audit targets)
- `Controllers/CDPController.cs` — ProtonFinalAssessments queries (L365, L494, L2800, L2943, L3093), CoachingSessions CRUD (L707, L2177, L2263, L2438), HistoriProton (L2754), HistoriProtonDetail (L3054), ExportHistoriProton (L2899)
- `Models/ProtonModels.cs` — ProtonFinalAssessment model, CompetencyLevelGranted field
- `Models/CoachingSession.cs` — CoachingSession model
- `Models/ActionItem.cs` — ActionItem model
- `Models/HistoriProtonViewModel.cs` — HistoriProton list view model
- `Models/HistoriProtonDetailViewModel.cs` — HistoriProton detail view model
- `Views/CDP/HistoriProton.cshtml` — HistoriProton list view
- `Views/CDP/HistoriProtonDetail.cshtml` — HistoriProton detail view
- `Controllers/AdminController.cs` — CoachCoacheeMapping management (completion status target)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RecordStatusHistory` helper (CDPController:3021) — reusable untuk tracking state changes
- AuditLog service — sudah tersedia di semua controller, bisa dipakai untuk session edit/delete logging
- Transaction pattern dari Phase 234/235 — `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`
- `GetSectionUnitsDictAsync()` — sudah dipakai di HistoriProton untuk org structure filtering

### Established Patterns
- ProtonFinalAssessments query di CDPController menggunakan `.Include()` chain untuk track dan assignment data
- HistoriProton grouping per TahunKe sudah ada di L2825-2835 (per-tahun progress check)
- CompetencyLevelGranted sebagai nullable field di ProtonFinalAssessment — per assessment independen
- Coaching session creation terikat ke evidence submission via SubmitEvidenceWithCoaching

### Integration Points
- `ProtonFinalAssessment.ProtonTrackAssignmentId` — target unique constraint
- `CoachingSession.ProtonDeliverableProgressId` — link ke deliverable (orphan check target)
- `CoachCoacheeMapping` — target completion/graduated status marker
- `HistoriProtonDetail` — aggregasi data dari multiple tables (assignments, progress, sessions, final assessments)

</code_context>

<specifics>
## Specific Ideas

- Completion = deliverable Approved + final assessment lulus — kedua syarat harus terpenuhi
- Mapping "completed/graduated" setelah Tahun 3 — explicit status marker
- HistoriProton per-tahun terpisah — user ingin clear separation antar tahun di timeline

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 236-audit-completion*
*Context gathered: 2026-03-23*
