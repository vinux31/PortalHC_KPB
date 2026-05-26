# Phase 324: Fix duplicate TrainingRecord auto-create on assessment completion - Context

**Gathered:** 2026-05-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Hapus mekanisme auto-create `TrainingRecord` saat worker selesai assessment (non-PreTest, non-Essay-pending) supaya halaman `/CMP/Records` tidak menampilkan 2 baris untuk 1 event ujian (1 dari `AssessmentSession` + 1 dari `TrainingRecord` auto-copy).

**Regression history:**
- Phase 153-04 (`817b29bd`): introduce auto-create TrainingRecord on exam completion (ASSESS-08)
- `79284609` (2026-03-18): **REMOVE** auto-create karena "caused duplicate entries in RecordWorkerDetail unified view"
- `766011b6` (2026-04-10): **RE-ADD** auto-create dengan try-catch `DbUpdateException` guard — tapi guard dead code (no DB unique index) + tetap menghasilkan visual duplicate. Inilah regression yang Phase 324 perbaiki.

**Yang IN scope:**
- Hapus block insert `TrainingRecord` di 3 lokasi (lihat decisions)
- Hapus cascade `TrainingRecord` di `RegradeAfterEditAsync` (Fail↔Pass flip)
- Cleanup data legacy `TrainingRecord` auto-generated (lokal + Dev/Prod via IT)
- Playwright UAT verifikasi
- HTML handoff IT untuk eksekusi cleanup di Dev

**Yang OUT of scope:**
- Tambah unique index di tabel `TrainingRecord` (gak perlu kalau auto-create dihapus)
- Refactor `GetUnifiedRecords` query
- Touch `TrainingAdminController` (admin manual add tetap utuh)
- Migration schema/model perubahan

</domain>

<decisions>
## Implementation Decisions

### Code Changes
- **D-01:** Hapus block `_context.TrainingRecords.Add(...)` di `Services/GradingService.cs:255-285` (path `GradeAndCompleteAsync`, normal submit flow)
- **D-02:** Hapus block `_context.TrainingRecords.Add(...)` di `Controllers/AssessmentAdminController.cs:3404-3421` (path `FinalizeEssayGrading`, manual essay grading)
- **D-03:** Hapus cascade `TrainingRecord` update/insert di `Services/GradingService.cs:483-562` (path `RegradeAfterEditAsync`, Pass↔Fail flip). `AssessmentSession.IsPassed` + `NomorSertifikat` update tetap; cascade ke `TrainingRecord` seluruhnya dihapus. Records page baca status terbaru dari `AssessmentSession`.

### Data Cleanup
- **D-04:** Cleanup scope = `WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10'`. Filter tanggal untuk hindari hapus row legitimate yang admin pernah ketik manual dengan pattern sama pra-bug.
- **D-05:** Lokal: backup DB lokal via `sqlcmd ... BACKUP DATABASE` SEBELUM eksekusi cleanup script (per `docs/SEED_WORKFLOW.md` mandatory). Catat di `docs/SEED_JOURNAL.md` sebagai temporary classification.
- **D-06:** Dev/Prod: tidak touch langsung. Buat `docs/DB_HANDOFF_IT_2026-05-26.html` dengan template + style mengikuti `docs/DB_HANDOFF_IT_2026-05-13.html` (Pertamina branding). Isi: commit hash, SQL cleanup script, prerequisite backup, verification query, rollback plan. IT yang eksekusi.

### Testing
- **D-07:** Playwright UAT automated mengikuti pattern Phase 322. Spec coverage minimum:
  - Worker submit assessment biasa (non-essay) → assert `/CMP/Records` hanya tampil 1 row "Assessment Online" (bukan 2)
  - PreTest tetap skip TR (regression guard existing behavior)
  - Essay flow finalize → assert tidak insert TR
  - HC `AkhiriUjian` (force-end single) → assert grading tetap jalan, tidak insert TR
  - HC `AkhiriSemuaUjian` (bulk) → assert grading tetap jalan untuk semua, tidak insert TR
  - HC `RegradeAfterEdit` Pass→Fail flip → `AssessmentSession.IsPassed` update, tidak ada TR cascade
  - HC `RegradeAfterEdit` Fail→Pass flip → sertifikat generate via `AssessmentSession.NomorSertifikat`, tidak ada TR cascade

### Verification
- **D-08:** Pre-fix repro lokal: bikin assessment baru, submit sebagai worker, buka `/CMP/Records`, capture screenshot 2-row state (proof of bug).
- **D-09:** Post-fix verify lokal: ulang flow, capture screenshot 1-row state (proof of fix).
- **D-10:** SQL verify count: `SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND CreatedAt >= '2026-04-10';` sebelum dan sesudah cleanup.

### Claude's Discretion
- Naming Playwright spec file + folder structure (ikut convention existing `tests/e2e/`)
- SQL script file naming + folder (saran: `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql`)
- HTML handoff content structure (selama follow template 2026-05-13)
- Logger statement format saat hapus block (kalau ada log yang relevan untuk audit removal)

### Folded Todos
[None — no pending todos matched Phase 324 scope]

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Workflow & Standards
- `CLAUDE.md` — Bahasa Indonesia response + Develop Workflow (Lokal→Dev→Prod, IT promo) + Seed Data Workflow
- `docs/DEV_WORKFLOW.md` — environment map, SOP migration, IT notification checklist
- `docs/SEED_WORKFLOW.md` — DB backup/restore SOP, journal format
- `docs/SEED_JOURNAL.md` — log temporary seed/cleanup activities

### Template / Pattern References
- `docs/DB_HANDOFF_IT_2026-05-13.html` — template Pertamina-branded HTML handoff IT untuk DB operation (Phase 324 bikin versi `2026-05-26.html` ikut template ini)

### Source Code (target edit)
- `Services/GradingService.cs:49-327` — `GradeAndCompleteAsync` (D-01 edit) + `RegradeAfterEditAsync:446-571` (D-03 edit)
- `Controllers/AssessmentAdminController.cs:3263-3470` — `FinalizeEssayGrading` (D-02 edit)
- `Services/WorkerDataService.cs:28-82` — `GetUnifiedRecords` (NO edit, sumber visual duplicate dari union query — pastikan tetap tampil assessment via AssessmentSession branch)

### Source Code (regression guard reference — JANGAN diubah)
- `Controllers/CMPController.cs:1727` — call site `GradeAndCompleteAsync` dari SubmitExam worker
- `Controllers/AssessmentAdminController.cs:3767,3847` — call site `GradeAndCompleteAsync` dari AkhiriUjian + AkhiriSemuaUjian
- `Controllers/AssessmentAdminController.cs:2948` — call site `RegradeAfterEditAsync`
- `Controllers/TrainingAdminController.cs:318,353,384,1060` — admin manual add TR (tidak terkait, tidak touch)
- `Data/ApplicationDbContext.cs:143-170` — TrainingRecord entity config (no unique index pada UserId+Judul+Tanggal — confirms why dead-code catch handler)

### Historical Context Commits
- `817b29bd` (Phase 153-04): original auto-create introduction
- `79284609` (2026-03-18): first removal — commit msg menjelaskan root cause visual duplicate
- `766011b6` (2026-04-10): regression re-add — yang Phase 324 perbaiki
- `b8b40358`: chore commit yang menyentuh related area

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GradingService.cs` already has status guard via `ExecuteUpdateAsync(WHERE Status != "Completed")` — tidak perlu diubah, hanya hapus block TR yang ada di setelah guard
- `WorkerDataService.GetUnifiedRecords` sudah include AssessmentSession sebagai source "Assessment Online" (line 44-56) — Records page tetap tampil assessment tanpa perlu TR copy
- Playwright pattern Phase 322 sudah established di `tests/e2e/` — helper convention + selector pattern reusable

### Established Patterns
- DB cleanup pattern: backup → script SQL → verify → restore opsi (per SEED_WORKFLOW)
- IT handoff pattern: HTML doc dengan Pertamina branding (red `#e30613`, navy `#1e3a8a`) — template di `docs/DB_HANDOFF_IT_2026-05-13.html`
- Phase commit pattern: `feat(324-XX): <description>` per task (atomic commits per gsd-executor)

### Integration Points
- `/CMP/Records` view (`Views/CMP/Records.cshtml`) — display surface, no edit needed
- `/Admin/AssessmentMonitoringDetail` — HC view yang touch AkhiriUjian/AkhiriSemuaUjian
- `/Admin/RegradeAfterEdit` flow — touch UI edit jawaban + cert cascade

</code_context>

<specifics>
## Specific Ideas

- User ingin HTML handoff IT versi `2026-05-26.html` ikut template `2026-05-13.html` (Pertamina-branded, struktur sama)
- Cleanup scope `>= 2026-04-10` (date when bug regression masuk via commit 766011b6)
- Records page test post-fix: harus tampil EXACTLY 1 row per assessment completion, bukan 2

</specifics>

<deferred>
## Deferred Ideas

- **Tambah unique index `(UserId, Judul, Tanggal)` di TrainingRecord** — tidak diperlukan setelah auto-create dihapus. Tapi kalau di masa depan admin manual add Excel import bisa generate duplicate, pertimbangkan unique index sebagai defensive measure. Phase masa depan.
- **Refactor `GetUnifiedRecords` query** — kalau di masa depan ada perubahan source-of-truth (misal pindah ke materialized view), boleh refactor. Tidak Phase 324.
- **Audit TR legacy dengan Judul similar pattern tapi admin manual** — kalau ternyata ada admin yang ketik manual "Assessment: ..." pre-bug, audit terpisah. Phase masa depan kalau diperlukan.

</deferred>

---

*Phase: 324-fix-duplicate-trainingrecord-auto-create-on-assessment-compl*
*Context gathered: 2026-05-26*
