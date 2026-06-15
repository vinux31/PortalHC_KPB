# Phase 368: Delete Records Hygiene Lanjutan - Context

**Gathered:** 2026-06-13
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 368 menutup **7 temuan hygiene (#21-27 spec C)** di alur tetangga delete-records overhaul (367) — utang teknis kecil yang sengaja dipisah dari cascade engine inti. **Migration=false.** Depends 367 (file-overlap `TrainingAdminController.cs` + `ResetAssessment` di `AssessmentAdminController.cs`).

Cakupan tetap (BUKAN capability baru):
- **#21** Edit atomic file replace — `EditTraining` + `EditManualAssessment` (pola Phase 331)
- **#22** Reset bersihkan `SessionElemenTeknisScores` (ET analytics stale)
- **#23** One-time cleanup `AttemptHistory` orphan legacy
- **#24** `ImportTraining` audit log + `AssessmentType` konstanta + `GenerateCertificate=isPassed`
- **#25** `CertificationManagement` dedup (CMP + CDP)
- **#26** `EditTraining` validasi `Renews*Id` (exist + same-user)
- **#27** `BulkBackfill` kosmetik (rename label + `AssessmentType` konstanta)

</domain>

<decisions>
## Implementation Decisions

### #23 — AttemptHistory orphan cleanup (KEPUTUSAN BESAR)
- **D-01:** Mekanisme = **endpoint admin idempotent + preview**. GET preview hitung jumlah orphan dulu → POST eksekusi (aman re-run) + **audit log**. Trigger via UI per-environment, **no direct DB edit** (selaras Dev Workflow proyek). **Migration=false utuh** (BUKAN EF data migration, BUKAN SQL script serah-IT).
- **D-02:** Definisi 'orphan' = baris `AttemptHistory` yang `SessionId`-nya **tak punya `AssessmentSession` induk (FK dangling)** — sesi terhapus di masa lalu sebelum cascade engine 367 ada. Definisi paling sempit + aman: hanya hapus yang benar-benar tak ber-induk valid.
- Acceptance netral: hanya hapus baris tanpa induk valid; preview-count harus tampil sebelum eksekusi; idempotent (re-run kedua = 0 dihapus).

### #25 — CertificationManagement dedup
- **D-03:** Ekstrak GroupBy dedup ke **helper static shared di lokasi NETRAL** — CMP & CDP konsumsi yang SAMA (single-source anti-drift, spirit 367). **BUKAN AdminBaseController** karena `CMPController`/`CDPController` = plain `Controller` (tak inherit AdminBase; hanya Training/AssessmentAdmin yang inherit). Lokasi tepat dipilih planner: static util netral ATAU promote `BuildSertifikatGroups` CMP jadi shared. CMP sudah punya `BuildSertifikatGroups`/`BuildSertifikatRowsAsync`/`BuildGroupViewModel`; CDP punya `CertificationManagement` sendiri (line 3704).

### #26 — EditTraining renewal validation
- **D-04:** Validasi `Renews*Id` (exist + same-user) dijalankan **HANYA saat field renewal berubah** (toleran data legacy). Record legacy ber-Renews invalid tetap bisa diedit field lain (nama/tanggal) tanpa terblokir. Cegah link buruk BARU tanpa merusak edit data lama (selaras spirit no-break legacy milestone).
- **D-05:** Aksi saat invalid (tak exist / beda user) = **ModelState error, tolak save, pesan jelas** ("Sesi renewal tak ditemukan / bukan milik peserta ini"). Pola existing reject (mirip guard duplikat 367). BUKAN auto-null diam-diam (lawan honesty 367).

### #21 — Edit atomic file replace
- **D-06:** Dipasang di **KEDUANYA**: `EditTraining` (`SertifikatUrl`) + `EditManualAssessment` (`ManualSertifikatUrl`). Pola Phase 331: save file baru → SaveChanges → **hapus file lama post-commit warn-only**. Hapus-lama **HANYA jika file baru di-upload**; tak ada upload baru → pertahankan file lama; upload gagal → file lama utuh.

### #22 — Reset ET scores
- **D-07:** Tambahkan `RemoveRange SessionElemenTeknisScores` ke cleanup `ResetAssessment` existing (di dalam tx reset) supaya retake hasilkan ET scores BARU (bukan stale). Tepat sesuai spec #22 — **tak ubah cleanup existing lain**, tak meluas ke analytics lain (cegah scope creep).

### #24 / #27 — Locked by spec (mekanis, tak didiskusikan)
- **D-08:** #24 `ImportTraining` — tambah `_auditLog.LogAsync` per operasi import (ringkasan) + `AssessmentType = AssessmentConstants.AssessmentType.Manual` + `GenerateCertificate = isPassed`.
- **D-09:** #27 `BulkBackfill` — set `AssessmentType` konstanta Manual + rename label UI **"Bulk Import Nilai (Excel)"**. Residu #27 DI-ACCEPT by design: sesi hasil backfill memang identitas baru (`Id` baru, `IsManualEntry=true`) — bukan replika sesi asli, tidak diperbaiki.

### Claude's Discretion
- Lokasi tepat helper static shared #25 (static util vs promote CMP helper) — planner pilih saat planning.
- Wording pesan ModelState #26 (selama jelas + tak leak internal, pola V7 generik 367).
- Bentuk endpoint admin #23 (route, view tombol, partial preview) — planner pilih; kontrak: preview-count + idempotent + audit.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec induk (sumber temuan #21-27)
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` §3.3 — definisi 7 temuan #21-27 ber-tag [368]
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` §3.3b — kontrak pembagian fase (planner 368 TIDAK tarik item [367], dan sebaliknya)
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` §3.4 — testing (minimal [Fact] #21 replace-file atomic, #22 retake ET baru, #24 import ter-audit-log)
- `docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md` §3.5 — out of scope (impersonate 999.6, soft-delete ditolak)

### Preseden pola (Phase 367 — file-overlap + pattern reuse)
- `.planning/phases/367-delete-records-cascade-overhaul-hapus-100-sampai-akar-cascad/367-CONTEXT.md` — keputusan 367 (helper AdminBase, static predicate single-source, honesty)
- `.planning/phases/367-delete-records-cascade-overhaul-hapus-100-sampai-akar-cascad/367-02-SUMMARY.md` — fixture real-SQL `RecordCascadeIntegrationTests.cs` + `SeedRenewalChainAsync` (REUSE untuk integration test 368)
- `.planning/phases/367-delete-records-cascade-overhaul-hapus-100-sampai-akar-cascad/367-07-SUMMARY.md` — pola `ManualDuplicatePredicate` static Expression (preseden validasi #26)

### Preseden atomic-file (Phase 331 / 355)
- `Controllers/TrainingAdminController.cs` — `DeleteTraining` (pola 331 atomic: capture path → tx → File.Delete post-commit warn-only)
- `HcPortal.Tests/` — preseden [Fact] `Replace_NewFileWins_DeletesOldFileOnDisk` (Phase 355) untuk atomic file test #21

### Dev/Seed Workflow (wajib saat eksekusi)
- `docs/DEV_WORKFLOW.md` — environment map (Lokal→Dev→Prod, IT promosi); dasar keputusan #23 endpoint (no direct DB edit)
- `docs/SEED_WORKFLOW.md` — snapshot→seed→restore+journal (wajib untuk test/UAT #23 cleanup di DB lokal)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Fixture real-SQL + `SeedRenewalChainAsync`** (`HcPortal.Tests/RecordCascadeIntegrationTests.cs`, Phase 367 Plan 02) — REUSE untuk integration test #21/#22/#23/#26.
- **Pola atomic-file Phase 331** (`TrainingAdminController.DeleteTraining`) — template langsung #21 (capture-before → tx → File.Delete post-commit warn-only).
- **`AssessmentConstants.AssessmentType.Manual`** — konstanta untuk #24 + #27.
- **`_auditLog.LogAsync`** — pola audit existing untuk #24 + #23 endpoint.
- **CMP `BuildSertifikatGroups`/`BuildSertifikatRowsAsync`/`BuildGroupViewModel`** (`CMPController.cs:3763+`) — kandidat promote shared helper #25.
- **Pola `ManualDuplicatePredicate` static Expression** (AdminBaseController, Phase 367) — pola validasi single-source #26.

### Established Patterns
- **Helper anti-drift**: Phase 367 ekstrak helper ke `AdminBaseController` (untuk Training/AssessmentAdmin yang inherit). #25 TAK bisa pakai ini (CMP/CDP plain Controller) → helper static netral.
- **1-tx + post-commit side-effect warn-only** (Phase 331-334, 367): mutasi dalam tx, File.Delete/audit POST commit. Berlaku #21 (file) + #23 (cleanup).
- **Honesty / no silent failure** (367 L-06): #26 pilih ModelState block, bukan auto-null diam.

### Integration Points
- `Controllers/TrainingAdminController.cs` — `EditTraining` (424/471), `EditManualAssessment` (926/960), `ImportTraining` (1196), `BulkBackfill`/`BulkBackfillAssessment` (762/772). **FILE-OVERLAP 367** — depends 367 selesai.
- `Controllers/AssessmentAdminController.cs` — `ResetAssessment` (3889) untuk #22; ET score queries existing (4454/4701). **FILE-OVERLAP 367** (ResetGuard).
- `Controllers/CMPController.cs` — `CertificationManagement` (3763) #25.
- `Controllers/CDPController.cs` — `CertificationManagement` (3704) #25.
- Endpoint admin baru #23 — lokasi controller dipilih planner (kandidat: AssessmentAdminController/TrainingAdminController/admin maintenance area).

### ⚠️ Koordinasi paralel v27.0
- v27.0 (Phases 372-375) menyentuh `AssessmentAdminController.cs` + `CMPController.cs` — file SAMA dipakai 368. JANGAN plan/execute v27.0 sebelum 368 ship atau koordinasi merge (ROADMAP §koordinasi).

</code_context>

<specifics>
## Specific Ideas

- #23 preview-count = kontrak eksplisit (admin lihat berapa orphan SEBELUM hapus) — pola preview==execute 367.
- #27 label persis: **"Bulk Import Nilai (Excel)"**.
- #21 hapus-lama strictly conditional pada adanya file baru (jangan hapus saat edit metadata-only).

</specifics>

<deferred>
## Deferred Ideas

- Impersonate identity bug → backlog 999.6 (out of scope spec §3.5).
- Soft-delete/undo delete records → opsi C ditolak brainstorm (out of scope).
- #22 perluasan ke analytics stale lain (cache agregat dsb) — tidak diambil; #22 strictly ET scores. Bila riset planning temukan analytics stale lain yang kritis, angkat sebagai temuan baru/backlog, jangan creep ke 368.
- #27 residu identitas sesi backfill (Id baru) — accepted by design, tidak diperbaiki.

</deferred>

---

*Phase: 368-delete-records-hygiene-lanjutan-edit-atomic-file-reset-et-sc*
*Context gathered: 2026-06-13*
