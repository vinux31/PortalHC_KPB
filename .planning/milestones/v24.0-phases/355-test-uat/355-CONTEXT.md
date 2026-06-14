# Phase 355: Test & UAT - Context

**Gathered:** 2026-06-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Bukti otomatis & manual bahwa fitur gambar di soal assessment (v24.0) bekerja end-to-end: admin upload → peserta lihat di StartExam → lihat di Results, dengan integritas file & data ter-cover. = **TST-01** (konsolidasi xUnit) + **TST-02** (Playwright UAT end-to-end). Migration: false.

**Konteks kunci (membentuk seluruh phase):** coverage xUnit untuk TST-01 **sudah sebagian besar ada** dari fold incremental Phase 352/353:
- `FileUploadHelperTests.cs` — `ValidateImageFile` valid JPG/PNG/JPEG + invalid (PDF-ext, exe-renamed magic-byte, oversize).
- `PackageImageSyncTests.cs` — `SyncPackagesToPost` copy ImagePath/ImageAlt Pre→Post + shared-path + null-safe.
- `PackageImageDeleteTests.cs` — ref-count delete/skip-shared + CollectsAllNonNullPaths + ReplaceConflict + OptionPreserve.

Karena itu kerja net-baru utama phase ini = **TST-02 Playwright UAT end-to-end**; TST-01 = gap-audit + pastikan suite hijau (bukan tulis ulang).

**Bukan scope ini:** menambah/mengubah fitur gambar (upload/CRUD/sync/delete = Phase 353 DONE; render 6 layar = Phase 354 DONE); migration; refactor produksi.
</domain>

<decisions>
## Implementation Decisions

### Metode UAT (TST-02)
- **D-01:** **Spec Playwright committed** — file baru `tests/e2e/image-in-assessment.spec.ts` (nama final = discretion planner), repeatable & CI-friendly, memakai pola infra existing `global.setup.ts`/`global.teardown.ts`. **Bukan** MCP-only. Alasan: REQ menamai TST-02 sebagai deliverable durable + selaras infra test v16.0 + lesson Phase 354 ("Razor dynamic WAJIB Playwright runtime, build/grep tak cukup"). MCP boleh dipakai ad-hoc saat debugging, tapi artifact wajib = spec committed.

### Scope konsolidasi xUnit (TST-01)
- **D-02:** **Gap-audit + suite hijau**, BUKAN tulis-baru/refactor. Verifikasi 3 file tes existing sudah cover semua butir SC#1; tambah **hanya gap nyata**. Cek spesifik wajib: butir "**replace gambar menghapus file LAMA**" (spec §11) — `ReplaceConflict_NewFileWins_OverRemoveCheckbox` ada, tapi pastikan ada assert eksplisit bahwa **path file lama benar-benar di-`File.Delete`** (bukan hanya path-pemenang). Bila belum → tambah 1 tes. TIDAK konsolidasi kelas (hindari churn tanpa coverage). TIDAK tambah tes integrasi controller-level (di luar scope minimal; lihat Deferred).

### Setup data UAT + fixture gambar
- **D-03:** **Admin-UI-driven create di dalam spec** — spec login admin → buat soal + tiap opsi ber-gambar lewat **form upload NYATA** (`setInputFiles` fixture) → sekaligus menguji jalur upload TST-02 "admin upload → simpan". Lalu login peserta → `StartExam` → `Results`, assert `<img>` tampil.
- **D-04:** **Commit 2 fixture kecil** (1 JPG + 1 PNG) di `tests/fixtures/` (nama/path = discretion planner). xUnit tetap pakai byte-array magic-byte in-memory existing; fixture file hanya untuk upload Playwright.
- **D-05:** **Guardrail Seed Workflow WAJIB** — spec memutasi DB (buat assessment + soal) dan menulis file fisik ke `wwwroot/uploads/questions/{packageId}/`. Karena itu: snapshot/restore DB (pola `global.setup`/`global.teardown` BACKUP→RESTORE) **plus** cleanup file upload yang dibuat + entry `docs/SEED_JOURNAL.md` (per CLAUDE.md Seed Workflow). Tak boleh ada seed/file nyangkut setelah sesi.

### Regression guard (SC#3)
- **D-06:** **Rerun suite existing + assert null** — rerun `dotnet test` penuh + spec exam existing (exam-types/assessment) sebagai baseline regresi, **plus** 1 assert eksplisit "soal **tanpa** gambar → tidak render `<img>`" (guard cabang null RND-07 / L-02 Phase 354). Murah & terarah.

### Claude's Discretion
- Nama persis file spec & fixture, path fixture, dan apakah reuse helper Playwright existing (`examTypes.ts`/`wizardSelectors.ts`) atau buat helper baru kecil.
- **Apakah spec membuat assessment penuh via wizard ATAU memakai paket dasar minimal (seed kecil / existing) lalu hanya upload gambar via UI** — planner/researcher putuskan. Syarat tetap: jalur upload admin diuji live (D-03) + guardrail snapshot/restore (D-05). (Catatan: wizard penuh = lebih faithful tapi lambat/rapuh; paket dasar minimal + upload-via-UI = cukup penuhi SC#2.)
- Bentuk assert `<img>` (cek `src` non-null + `img-fluid` + `loading=lazy` + `alt` + trigger lightbox) — minimal: img tampil + responsif.
- Apakah surface admin essay (`AssessmentMonitoringDetail` RND-05) + `EditPesertaAnswers` (RND-06) ikut di-UAT dalam spec — **opsional**; fokus SC#2 = StartExam + Results. Sisanya nice-to-have (spec §11 menyebut Monitoring essay).

### Locked dari ROADMAP/Spec (jangan re-decide)
- **L-01 (SC#2 alur wajib):** admin upload gambar soal + tiap opsi → simpan → peserta `StartExam` melihat gambar soal+opsi (responsif) → peserta `Results` (pembahasan) melihat gambar soal+opsi.
- **L-02 (SC#1 butir TST-01):** upload valid (JPG/PNG tersimpan) + invalid (non-image ditolak via magic-byte) + `SyncPackagesToPost` salin `ImagePath`/`ImageAlt` Pre→Post + `DeleteQuestion` hapus file gambar soal+opsi (post-commit) + replace hapus file lama.
- **L-03 (SC#3 gate):** `dotnet build` 0 error + `dotnet test` hijau + UAT dijalankan di `localhost:5277` sesuai CLAUDE.md Develop Workflow + tanpa regresi flow ujian existing (MC/MA/Essay tanpa gambar tetap normal).
- **L-04 (no migration):** Phase 355 tidak ada migration (kolom sudah ada sejak Phase 352).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec, Roadmap, Requirements
- `docs/superpowers/specs/2026-06-06-image-in-assessment-questions-design.md` — spec induk v24.0. **§11 Testing** (daftar butir xUnit + Playwright UAT — sumber SC), §13 best-practice (alt/format/responsive). Baca §11.
- `.planning/ROADMAP.md` — entri **Phase 355** (Goal + 3 Success Criteria + Depends 354/353).
- `.planning/REQUIREMENTS.md` — **TST-01** (xUnit upload/sync/delete) + **TST-02** (Playwright UAT end-to-end).

### Tes existing yang DI-AUDIT (TST-01 — jangan tulis ulang)
- `HcPortal.Tests/FileUploadHelperTests.cs` — `ValidateImageFile_*` (valid JPG/PNG/JPEG, invalid PDF-ext / exe-renamed magic-byte / oversize). Cover butir upload valid+invalid.
- `HcPortal.Tests/PackageImageSyncTests.cs` — `SyncCopiesQuestionImagePath` / `SyncCopiesOptionImageAlt` / `SyncSharesSamePath_NoFileDuplication` / `SyncHandlesNullImagePath`. Cover butir sync copy.
- `HcPortal.Tests/PackageImageDeleteTests.cs` — `RefCount_*` / `DeletePackageImage_CollectsAllNonNullPaths` / `ReplaceConflict_NewFileWins_OverRemoveCheckbox` / `OptionPreserve_*`. Cover butir delete + replace (verifikasi gap "hapus file LAMA" — D-02).

### Infra Playwright existing (pola TST-02 + Seed Workflow)
- `tests/e2e/global.setup.ts` — pipeline app-check → **BACKUP DB** → seed SQL → validate → tulis state → append `SEED_JOURNAL.md` (status=active). Pola guardrail D-05.
- `tests/e2e/global.teardown.ts` — RESTORE DB + tandai journal cleaned.
- `tests/e2e/helpers/dbSnapshot.ts` — helper BACKUP/RESTORE SQL Server.
- `tests/e2e/helpers/examTypes.ts`, `tests/e2e/helpers/wizardSelectors.ts` — helper login/flow exam + selector wizard (kandidat reuse).
- `tests/playwright.config.ts` — `baseURL: http://localhost:5277`, project chromium.
- `tests/sql/*-seed.sql` (mis. `assessment-matrix-seed.sql`, `cmp351-seed.sql`) — pola seed SQL bila pilih opsi paket dasar.
- `tests/e2e/edit-peserta-answers.spec.ts`, `tests/e2e/exam-types.spec.ts` — spec referensi flow exam (baseline regresi D-06).

### Surface yang diuji (sumber implementasi 353/354)
- `Views/Admin/ManagePackageQuestions.cshtml` — form upload gambar soal+opsi (Phase 353) = entry point UI upload UAT.
- `Views/Admin/_PreviewQuestion.cshtml` — preview admin (RND-04, render `<img>` referensi).
- `Views/CMP/StartExam.cshtml`, `Views/CMP/Results.cshtml` — render peserta (RND-01/03) = target assert UAT.
- `Views/Shared/_QuestionImage.cshtml` (+ lightbox modal global) — partial reusable Phase 354 D-04 (cabang null RND-07 = guard regresi D-06).
- `Controllers/AssessmentAdminController.cs` (CreateQuestion/EditQuestion/DeleteQuestion/SyncPackagesToPost), `Controllers/CMPController.cs` (StartExam/Results populate).
- `wwwroot/uploads/questions/{packageId}/` — folder file gambar (target cleanup D-05).

### Phase predecessor CONTEXT (pola & keputusan)
- `.planning/phases/353-admin-backend-gambar-crud-sync-atomic-delete/353-CONTEXT.md` — keputusan CRUD/sync/atomic-delete + ref-count.
- `.planning/phases/354-render-gambar-di-6-layar/354-CONTEXT.md` — D-04 partial reusable + L-02 markup `<img>` render-only-if-non-null.
- `.planning/phases/352-data-foundation-image-only-upload/352-CONTEXT.md` — D-04 no-resize + helper image-only magic-byte.

### Workflow & kredensial (UAT lokal)
- `CLAUDE.md` — Develop Workflow (verifikasi lokal build+run+DB) + Seed Workflow (klasifikasi → snapshot → journal → restore).
- `docs/SEED_WORKFLOW.md`, `docs/SEED_JOURNAL.md` — SOP + jurnal seed (D-05).
- `docs/DEV_WORKFLOW.md` — environment map + checklist.
- Dev admin login UAT: `admin@pertamina.com` (memory `reference_dev_credentials`); password dev lokal (admin/coach `123456` per memory phase 341) — verifikasi saat sesi.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 3 file tes gambar xUnit existing (FileUploadHelper/PackageImageSync/PackageImageDelete) — **basis TST-01**, di-audit bukan ditulis ulang.
- `global.setup.ts`/`global.teardown.ts` + `dbSnapshot.ts` — otomasi Seed Workflow (BACKUP→seed→validate→journal→RESTORE) siap pakai untuk guardrail spec baru (D-05).
- Helper Playwright `examTypes.ts` / `wizardSelectors.ts` — login + flow exam + selector wizard.
- Partial `_QuestionImage.cshtml` + lightbox (Phase 354 D-04) — target render yang di-assert + cabang null untuk guard regresi.

### Established Patterns
- Spec Playwright per-phase di `tests/e2e/*.spec.ts`, baseURL `localhost:5277`, project chromium (pola v16.0).
- xUnit pakai byte-array magic-byte in-memory untuk validasi file (FileUploadHelperTests) — fixture file fisik TIDAK dipakai di unit; hanya Playwright butuh file (D-04).
- Seed temporary local-only + snapshot/restore + journal (CLAUDE.md).

### Integration Points
- UI upload: `ManagePackageQuestions.cshtml` form (admin) — entry UAT D-03 (`setInputFiles`).
- Render peserta: `StartExam.cshtml` + `Results.cshtml` — titik assert `<img>` (L-01).
- DB+file mutation: assessment/soal baru + file ke `wwwroot/uploads/questions/` — wajib cleanup (D-05).
</code_context>

<specifics>
## Specific Ideas

- User menjawab "sesuai reko" untuk metode UAT + memilih rekomendasi di 3 area lain → **ikuti rekomendasi tiap area** (spec committed; gap-audit minimal; admin-UI create + fixtures; rerun suite + assert null).
- Penekanan recurring user/projek: jalankan UAT live di `localhost:5277` (CLAUDE.md) + jangan tinggalkan seed/file nyangkut (Seed Workflow).
</specifics>

<deferred>
## Deferred Ideas

- **Tes integrasi controller-level** (CreateQuestion/EditQuestion HTTP-path → ImagePath persist DB) — ditolak demi scope minimal (D-02). Angkat hanya bila gap-audit menemukan butir SC#1 tak tercover unit.
- **Konsolidasi/refactor 3 file tes jadi 1 kelas image** — ditolak (churn tanpa coverage, D-02).
- **UAT MCP-only sebagai deliverable** — ditolak demi spec committed (D-01); MCP tetap boleh ad-hoc saat debugging.
- **UAT essay-monitoring (RND-05) + EditPesertaAnswers (RND-06) dalam spec** — opsional / nice-to-have (Claude's Discretion); fokus deliverable = SC#2 StartExam + Results.

None lain — diskusi tetap dalam scope test/UAT.
</deferred>

---

*Phase: 355-test-uat*
*Context gathered: 2026-06-09*
