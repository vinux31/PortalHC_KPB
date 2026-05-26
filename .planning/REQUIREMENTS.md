# Requirements — v18.0 Cascade Delete Hardening + Duplicate TrainingRecord Fix

**Milestone:** v18.0
**Started:** 2026-05-26
**Status:** Active

## Goal

1. Tutup oversight Phase 321 (model `AssessmentEditLog` baru, FK Restrict) di Phase 312 cascade (`DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup`).
2. Hapus regression dari commit `766011b6` (2026-04-10) yang re-introduce auto-create `TrainingRecord` di `GradingService.GradeAndCompleteAsync` setelah sebelumnya dihapus `79284609` (2026-03-18) karena visual duplicate di `/CMP/Records`.

## v18.0 Requirements

### Cascade Delete (Phase 323)

- [ ] **CASCADE-01**: Admin/HC dapat menghapus `AssessmentSession` (single, group, atau Pre-Post group) yang sudah pernah di-edit soalnya — `AssessmentEditLogs` ikut ter-cascade tanpa FK Restrict exception.

  **Acceptance criteria:**
  1. `DeleteAssessment(id)`, `DeleteAssessmentGroup(id)`, `DeletePrePostGroup(linkedGroupId)` di `Controllers/AssessmentAdminController.cs` masing-masing tambah `RemoveRange(AssessmentEditLogs)` sebelum cascade existing
  2. Session belum pernah di-edit → tetap sukses (no regression)
  3. Session sudah di-edit ≥1 soal → sukses, `AssessmentEditLogs` ikut terhapus
  4. Audit log `DeleteAssessment` / `DeleteAssessmentGroup` / `DeletePrePostGroup` tetap tercatat normal
  5. Transaction scope existing (line 2040, 2184, 2313) tetap membungkus delete + cascade — rollback bersih saat exception
  6. Smoke test 3 skenario: (a) session no-edits → delete OK, (b) session 1+ edits → delete OK, (c) group dengan campuran sibling no-edits + edits → delete OK
  7. Tidak ubah schema DB, model class, FK definition, atau migration

### Duplicate TrainingRecord Fix (Phase 324)

- [ ] **DUPL-01**: Hapus block auto-create `TrainingRecord` di 3 lokasi production code supaya `AssessmentSession` jadi sole source-of-truth untuk row "Assessment Online" di `/CMP/Records`.

  **Acceptance criteria:**
  1. `Services/GradingService.cs:255-285` block `_context.TrainingRecords.Add(...)` di `GradeAndCompleteAsync` DIHAPUS (lines 255-285 including PreTest gate dan try-catch DbUpdateException dead code)
  2. `Controllers/AssessmentAdminController.cs:3404-3421` block `_context.TrainingRecords.Add(...)` di `FinalizeEssayGrading` DIHAPUS
  3. `Services/GradingService.cs:483-567` cascade `TrainingRecord` Pass↔Fail flip DIHAPUS — TR update Status="Failed" (lines 494-497) + TR insert/update Status="Passed" (lines 540-561) hilang; `AssessmentSession.NomorSertifikat` revoke (lines 488-492) + cert generate retry loop (lines 506-538) TETAP
  4. `dotnet build` 0 Error setelah edit (no compile regression)
  5. Cross-grep `TrainingRecords\.(Add|AddAsync|AddRange)` di `Services/` + `Controllers/AssessmentAdminController.cs` + `Controllers/CMPController.cs` returns 0 hit (TrainingAdminController boleh, out-of-scope)
  6. Tidak ubah schema DB, model class, atau migration

- [ ] **DUPL-02a (Phase 324)**: Playwright E2E UAT cover S1 + S2 sebagai primary regression guard, dengan 7-scenario spec skeleton file untuk Phase 325 continuation.

  **Acceptance criteria:**
  1. `tests/e2e/Phase324_NoDuplicateTrainingRecord.spec.ts` exists dengan 7 test block (S1-S7) — file structure complete
  2. S1: Worker submit assessment biasa (non-essay) → `/CMP/Records` tampil EXACTLY 1 row "Assessment Online" untuk event (bukan 2) — **IMPLEMENTED + GREEN**
  3. S2: PreTest tetap skip TR (regression guard existing behavior) — **IMPLEMENTED + GREEN**
  4. S3-S7: file block exists dengan `test.skip(true, "Implementasi di Phase 325 — butuh fixture seed assessment + sertifikat existing state")` + TODO comment referencing CONTEXT.md D-07b
  5. Full spec run ≤ 3 menit di lokal (`cd tests && npx playwright test e2e/Phase324_NoDuplicateTrainingRecord.spec.ts`)
  6. Helper module `tests/e2e/helpers/phase324.ts` exists dengan fully implemented helpers (no placeholder bodies, no `break; // placeholder` patterns)

- [ ] **DUPL-02b (Phase 325 — DEFERRED)**: Implementasi S3-S7 sebagai dedicated phase berikutnya untuk close UAT coverage.

  **Acceptance criteria:**
  1. S3: Essay flow finalize → tidak insert TR (butuh seed essay assessment + HC FinalizeEssayGrading action)
  2. S4: HC `AkhiriUjian` (force-end single) → grading tetap jalan, tidak insert TR (butuh seed running session + 1 worker yang belum submit)
  3. S5: HC `AkhiriSemuaUjian` (bulk) → grading tetap jalan untuk semua, tidak insert TR (butuh seed running session + multi-worker pending)
  4. S6: HC `RegradeAfterEdit` Pass→Fail → `AssessmentSession.IsPassed=false` + `NomorSertifikat=null`, tidak ada TR cascade (butuh seed Pass session + sertifikat existing)
  5. S7: HC `RegradeAfterEdit` Fail→Pass → `AssessmentSession.NomorSertifikat != null` (kalau `GenerateCertificate && !PreTest`), tidak ada TR cascade (butuh seed Fail session)
  6. `test.skip(true, ...)` annotations di S3-S7 dihapus + actual implementation
  7. Full spec run ≤ 5 menit (toleransi tinggi karena tambah 5 scenario + fixture setup)

  **Phase 325 spawn:** User akan jalankan `/gsd-add-phase` setelah Phase 324 ship. Slug draft: `complete-uat-phase324-s3-to-s7`. Rationale split documented di CONTEXT.md D-07b.

- [ ] **DUPL-03**: Cleanup data legacy `TrainingRecord` auto-generated di DB lokal via SEED_WORKFLOW BACKUP/RESTORE lifecycle.

  **Acceptance criteria:**
  1. Schema verify pre-cleanup: identifikasi filter column yang tepat (RESEARCH A3 — `CreatedAt` TIDAK ada di model; alternative `TanggalSelesai` atau Id-based)
  2. Pre-check: identifikasi orphan risk via `RenewsTrainingId` (RESEARCH Open Question #3) — query count child TR yang me-renew parent target delete; null-clear bila > 0
  3. BACKUP DB lokal via `sqlcmd ... BACKUP DATABASE HcPortalDB_Dev` sebelum eksekusi cleanup
  4. SQL script `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql` exists dengan structure: SET XACT_ABORT ON + transaction + pre-count + safety cap (5000) + DELETE + post-count + COMMIT (atau ROLLBACK on cap exceeded / post-count > 0)
  5. Pre-count > 0 dan post-count = 0 (cleanup berhasil)
  6. Re-run script: post-count tetap 0 (idempotent)
  7. `docs/SEED_JOURNAL.md` append 1 entry dengan classification `temporary + local-only`, status awal `active`, status akhir `cleaned` setelah verifikasi
  8. Tidak hapus `AssessmentSessions` atau dependent (sole source-of-truth tetap utuh)

- [ ] **DUPL-04**: IT handoff HTML doc siap untuk eksekusi cleanup di server Dev/Prod.

  **Acceptance criteria:**
  1. `docs/DB_HANDOFF_IT_2026-05-26.html` exists, fork dari `docs/DB_HANDOFF_IT_2026-05-13.html` (verbatim CSS variables — `--brand: #e30613`, `--navy: #1e3a8a`)
  2. Section: Context (kenapa cleanup) + Commit hash (final Wave 1 hash) + Pre-check sample query (Pitfall 6 — visual sanity 20 row) + Pre-check orphan query (RESEARCH OQ #3) + Backup command (BACKUP DATABASE) + SQL cleanup script embedded (verbatim copy dari `docs/sql/`) + Verification query (post-count) + Rollback plan (RESTORE FROM DISK)
  3. EXPLICIT ordering callout: **Step 1 deploy code dulu (commit hash sudah merge), Step 2 cleanup data** (RESEARCH Pitfall 5 — race condition prevention)
  4. Section "Yang TIDAK perlu IT lakukan" (no schema change, no migration) — sejajar template 2026-05-13
  5. NO schema change flag eksplisit di TL;DR

- [ ] **DUPL-05**: Pre-fix repro + post-fix verify lokal terdokumentasi.

  **Acceptance criteria:**
  1. Pre-fix screenshot `docs/screenshots/phase324/before-fix.png` — `/CMP/Records` 2 row state (1 "Assessment Online" + 1 "Training Manual" untuk event sama)
  2. Post-fix screenshot `docs/screenshots/phase324/after-fix.png` — `/CMP/Records` 1 row state (hanya "Assessment Online")
  3. SQL count verify lokal (D-10): sebelum cleanup `SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND {date_filter}` returns > 0; setelah cleanup returns 0
  4. Cross-grep audit final via `Grep` tool returns 0 hit untuk `TrainingRecords.(Add|AddAsync|AddRange)` di `Services/GradingService.cs`, `Controllers/AssessmentAdminController.cs`, `Controllers/CMPController.cs`

## Future Requirements (deferred)

Tidak ada — milestone hotfix-scope. Bisa expand via `/gsd-add-phase` bila ada bug serupa ditemukan saat audit.

## Out of Scope (explicit)

### Phase 323
- **Audit endpoint delete lain** (DeleteCategory, DeletePackage, DeleteQuestion, DeleteWorker, DeleteTraining, DeleteManualAssessment, DeleteOrganizationUnit, DeleteCoachingSession, BudgetTrainingDelete, dll.) — fokus milestone hanya 3 endpoint `DeleteAssessment*` yang directly affected oleh Phase 321 `AssessmentEditLog`. Audit luas masuk milestone berikutnya bila perlu.
- **Refactor cascade helper** (extract reusable `CascadeAssessmentSessionDependents(sessionIds)` helper) — tidak perlu untuk 3 endpoint; tunggu ada signal pattern reuse.
- **Migration ubah FK Restrict → Cascade DB-level** — endpoint cascade lebih eksplisit + audit-friendly daripada DB cascade silent.
- **UI surface old assessment (filter `>= 7 hari`)** di `ManageAssessmentTab_Assessment` line 115 — separate UX issue, masuk backlog tersendiri.

### Phase 324
- **Tambah unique index `(UserId, Judul, Tanggal)` di TrainingRecord** — tidak diperlukan setelah auto-create dihapus. Phase masa depan defensive measure jika Excel import generate duplicate.
- **Refactor `GetUnifiedRecords` query** — kalau di masa depan ada perubahan source-of-truth (misal pindah ke materialized view), boleh refactor. Tidak Phase 324.
- **Audit TR legacy dengan Judul similar pattern tapi admin manual** — kalau ternyata ada admin yang ketik manual "Assessment: ..." pre-bug, audit terpisah.
- **Touch `TrainingAdminController`** — admin manual add TR tetap utuh, tidak diubah.
- **Schema migration** — Phase 324 = subtract phase, NO migration.

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| CASCADE-01 | 323 | Pending |
| DUPL-01 | 324 | Pending |
| DUPL-02a | 324 | Pending |
| DUPL-02b | 325 | Pending (Phase 325 deferred) |
| DUPL-03 | 324 | Pending |
| DUPL-04 | 324 | Pending |
| DUPL-05 | 324 | Pending |

**Active mapped: 7/7 ✓ — Orphans: 0 — Duplicates: 0** (DUPL-02 split → DUPL-02a Phase 324 + DUPL-02b Phase 325 per checker iter 3 BLOCKER 1)

---

*Requirements created: 2026-05-26 · Phase 324 DUPL-01..05 appended 2026-05-26 saat `/gsd-plan-phase 324`.*
