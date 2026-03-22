# Phase 234: Audit Setup Flow - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Memastikan fondasi data Proton (silabus, mapping, assignment) integritas — tidak ada setup yang bisa menghasilkan data corrupt di fase execution berikutnya. Mencakup: silabus delete safety, guidance file management, coach-coachee mapping cascade, track assignment progression validation, dan import/export robustness.

</domain>

<decisions>
## Implementation Decisions

### Silabus Delete Safety
- **D-01:** Hard delete diblokir jika ada progress aktif — hanya soft delete (IsActive=false) yang diizinkan. Hard delete hanya untuk silabus tanpa progress.
- **D-02:** Modal warning menampilkan summary impact count: "Deliverable ini digunakan oleh X coachee dengan Y progress aktif"
- **D-03:** Safety check di semua level: Deliverable, SubKompetensi, dan Kompetensi — setiap level cek apakah ada progress aktif di bawahnya
- **D-04:** Auto orphan cleanup setelah delete: hapus SubKompetensi kosong, lalu Kompetensi kosong — dalam transaction

### Transaction & Cascade Integrity
- **D-05:** CoachCoacheeMappingDeactivate dibungkus dalam explicit DB transaction (BeginTransactionAsync) — rollback semua jika cascade gagal
- **D-06:** SilabusDelete cascade (progress → sessions → action items → orphan cleanup) dibungkus dalam transaction
- **D-07:** GuidanceReplace: upload file baru dulu, update DB, baru delete file lama — jika upload gagal, file lama tetap ada
- **D-08:** CoachCoacheeMappingReactivate: Claude decides — assess risk timestamp matching ±5 detik dan tentukan apakah perlu transaction + matching improvement

### Progression Validation (Tahun 1→2→3)
- **D-09:** Warning only, bukan block — HC/Admin tetap bisa assign Tahun 2/3 langsung setelah konfirmasi warning
- **D-10:** Definisi "selesai": semua ProtonDeliverableProgress di track tersebut status Approved (semua level approval selesai)
- **D-11:** Reactivated coachee boleh langsung assign Tahun 2/3 tanpa harus selesaikan tahun sebelumnya — karena website baru, ada worker yang sudah di tengah perjalanan
- **D-12:** Validasi warning diterapkan di assign action + import — kedua jalur menampilkan warning

### Import/Export Robustness
- **D-13:** All-or-nothing rollback — jika ada 1 baris error, rollback semua. Tidak ada data yang masuk sampai semua baris valid
- **D-14:** Error reporting: per-row status table (No Baris | Status | Pesan Error) tampil di halaman setelah import
- **D-15:** Duplikasi detection: skip + report — jika data sudah ada (active), skip dan report sebagai "Skipped: sudah ada"
- **D-16:** Template validation: server-side cek header kolom Excel cocok dengan template — reject jika kolom salah/kurang

### Claude's Discretion
- Implementasi detail modal warning UI (styling, animation)
- CoachCoacheeMappingReactivate transaction + matching strategy (D-08)
- Exact validation messages dan error wording
- Guidance file type whitelist details (server-side)
- How to surface progression warning di UI (inline alert vs modal)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone & Requirements
- `.planning/ROADMAP.md` — Phase 234 success criteria, dependency chain Phase 233→234
- `.planning/REQUIREMENTS.md` — SETUP-01 through SETUP-05 requirement definitions

### Phase 233 Research (lens untuk audit)
- `docs/audit-v7.7.html` — Dokumen riset perbandingan coaching platform, gap analysis vs 360Learning/BetterUp/CoachHub
- `.planning/phases/233-riset-perbandingan-coaching-platform/233-CONTEXT.md` — Keputusan riset Phase 233

### Existing Code (audit targets)
- `Controllers/ProtonDataController.cs` — SilabusSave (L214), SilabusDelete (L506), SilabusDeactivate (L570), GuidanceUpload/Replace/Delete (L895-1045), ImportSilabus (L720), ExportSilabus (L613)
- `Controllers/AdminController.cs` — CoachCoacheeMappingAssign (L3944), CoachCoacheeMappingDeactivate (L4230), CoachCoacheeMappingReactivate (L4289), ImportCoachCoacheeMapping (L3774), CoachCoacheeMappingExport (L5382)
- `Models/ProtonModels.cs` — ProtonTrack, ProtonTrackAssignment, ProtonDeliverableProgress model definitions
- `Models/CoachCoacheeMapping.cs` — Mapping model definition

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `DeleteKompetensi` (ProtonDataController:1278) — sudah ada pattern BeginTransactionAsync dengan explicit rollback, bisa jadi template untuk transaction lain
- Import pattern di AdminController (ImportWorkers) — pattern download template + upload + process + redirect, bisa reference untuk improve import silabus/mapping
- AuditLog service — sudah tersedia di semua controller, tinggal pastikan semua operation tercatat

### Established Patterns
- Soft delete via IsActive flag — sudah dipakai di SilabusDeactivate/Reactivate, mapping deactivate/reactivate
- File upload: safe filename `{timestamp}_{guid}{ext}` di `/uploads/guidance/`
- Import result: TempData JSON pattern untuk pass results antar request
- Duplicate check sebelum create: sudah ada di mapping assign dan edit

### Integration Points
- ProtonDeliverableProgress — link antara silabus deliverable dan coachee progress (cascade delete target)
- CoachingSessions + ActionItems — child data dari progress (cascade delete target)
- DeliverableStatusHistory — history tracking per progress (cascade delete target)
- ProtonTrackAssignment — link antara mapping dan track (cascade deactivation target)

</code_context>

<specifics>
## Specific Ideas

- Website baru — ada worker yang sudah di tengah perjalanan Tahun 2/3, jadi progression validation harus fleksibel (warning, bukan block)
- All-or-nothing import dipilih karena user lebih memilih data konsistensi daripada partial import

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 234-audit-setup-flow*
*Context gathered: 2026-03-22*
