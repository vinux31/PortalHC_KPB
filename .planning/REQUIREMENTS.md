# Requirements — Milestone v32.2 Inject Hasil Assessment Manual ("Seakan Online")

**Milestone:** v32.2 — Inject Hasil Assessment Manual ("Seakan Online")
**Created:** 2026-06-17
**Goal:** HC/Admin dapat meng-inject hasil assessment manual (ujian offline/kertas, data migrasi, acara lisensor luring) yang **identik dengan hasil online** — muncul di riwayat pekerja, rincian jawaban per-soal, breakdown elemen teknis, dan sertifikat opsional — via page baru `/Admin/InjectAssessment` (Kelola Data Section C, Admin+HC).
**Sumber:** Brainstorm 2026-06-17 + design spec `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` (research codebase: BulkBackfill, GradingService, room model, /CMP/Results, anchor sentinel).
**Konteks:** Reuse mesin existing (authoring soal + `GradingService`/`FinalizeEssayGrading` + `CertNumberHelper`), NOL duplikasi logic → skor/lulus/cert dihitung identik online. Anchor paket = pola sentinel (`CMPController.cs:1034-1090`); `GetUnifiedRecords` (`WorkerDataService.cs:28`) tak filter `IsManualEntry` → visibility /CMP/Records gratis. **0 migration** (semua tabel sudah ada). Branch main; verifikasi lokal `dotnet build` + Playwright. Penomoran fase LANJUT dari v32.0 (392) → mulai 393 (tidak reset).

---

## Requirements v32.2

### Backend & Integritas Inject (INJ) — fase 393

- [x] **INJ-01**: Sistem membangun sesi assessment manual **lengkap** per pekerja (`AssessmentSession` + `UserPackageAssignment` ber-`ShuffledQuestionIds` + `PackageUserResponses` + `SessionElemenTeknisScore` + sertifikat opsional) dengan melewatkan jawaban ke **pipeline grading yang sama** dengan online (`GradingService.GradeAndCompleteAsync` / `FinalizeEssayGrading`) — skor, kelulusan, elemen teknis, nomor sertifikat **dihitung** (bukan ditulis tangan). Operasi atomic per-batch (rollback semua bila ada NIP invalid / error).
- [x] **INJ-02**: Setiap sesi inject ditandai `IsManualEntry=true` dan tercatat di **AuditLog** (`ActionType="ManualInject"`, actor, NIP, sessionId, skor) — terlihat sebagai "Assessment Online" ke pekerja, tetapi terlacak penuh & dapat dibedakan oleh Admin (transparansi/compliance).

### Page, Setup Room & Authoring (INJ) — fase 394

- [x] **INJ-03**: Admin & HC dapat membuka page `/Admin/InjectAssessment` dari menu Kelola Data (Section C: Assessment & Training), dengan RBAC `Admin,HC`.
- [x] **INJ-04**: HC dapat mengatur **setting room inject** mirip `CreateAssessment` — judul, kategori, jadwal/`CompletedAt` (di-backdate ke tanggal ujian luring), durasi, `PassPercentage`, `AllowAnswerReview`, tipe (`Standard`/`Pre`/`Post`).
- [x] **INJ-05**: HC dapat **menulis soal** (MultipleChoice / MultipleAnswer / Essay) beserta opsi + kunci (`IsCorrect`) + `ScoreValue` + `ElemenTeknis` + `Rubrik` langsung di alur page inject (reuse komponen authoring `ManagePackages`).
- [x] **INJ-06**: HC dapat memilih pekerja penerima inject (reuse worker picker; NIP wajib ada di sistem).
- [x] **INJ-07**: HC dapat mengatur sertifikat **per-room via toggle**: auto-generate nomor resmi (`KPB/xxx/ROMAN/year` via `CertNumberHelper`) / input nomor manual / tanpa sertifikat.

### Mode Jawaban (INJ) — fase 395

- [x] **INJ-08**: HC dapat **menginput jawaban asli** tiap pekerja per soal via form — MC/MA memilih opsi, Essay mengisi teks + skor manual — lalu sistem menghitung skor lewat pipeline grading.
- [x] **INJ-09**: HC dapat **auto-generate pola jawaban dari skor target** (MC/MA dibuat pola benar/salah konsisten dengan skor; Essay di-set skor langsung), dengan **skor final aktual ditampilkan sebelum commit** (memperhitungkan pembulatan).

### Import Excel & Retire BulkBackfill (INJ) — fase 396

- [ ] **INJ-10**: HC dapat **import jawaban/skor batch via Excel** — template ter-generate dari paket soal yang sudah di-authored; format matrix (baris = NIP, kolom = soal); validasi atomic (NIP valid, opsi valid, rollback bila error).
- [ ] **INJ-11**: Tool lama **BulkBackfill** ("Bulk Import Nilai (Excel)", `TrainingAdminController.cs:787/836`) **dipensiunkan / diarahkan** ke page inject baru — tidak ada dua tool duplikat yang melakukan hal sama.

### Link Pre/Post ke Room Existing (INJ) — fase 397

- [ ] **INJ-12**: HC dapat **mencari & memilih assessment room existing** untuk menautkan sesi inject Pre/Post (`LinkedGroupId` + `LinkedSessionId`, reuse query `ManageAssessmentTab_Assessment`) — mendukung skenario **silang inject↔online** (mis. Pre di-inject, Post = assessment online asli, atau sebaliknya).

### Verifikasi "Seakan Online" (INJ) — fase 398

- [ ] **INJ-13**: Hasil inject **terverifikasi identik online** end-to-end — muncul di `/CMP/Records` pekerja (label "Assessment Online"), rincian jawaban per-soal benar/salah + elemen teknis di `/CMP/Results`, dan sertifikat dapat di-download — dikunci E2E + regression test + audit milestone.

---

## Future Requirements (deferred)

- **Multi-paket variasi per room inject** — saat ini 1 paket per room (semua pekerja paket sama). Variasi paket (Paket A/B) ditangguhkan.
- **Import gambar soal via Excel** — gambar soal/opsi hanya via UI authoring; tidak via Excel batch.
- **Edit massal sesi inject pasca-buat** — edit/hapus dasar sudah tersedia via tab "Input Records" existing; bulk-edit khusus inject ditangguhkan.

---

## Out of Scope (eksklusi eksplisit)

- **Duplikasi logic grading/authoring** — wajib reuse mesin existing; tidak membuat engine grading/authoring terpisah (cegah drift).
- **Notifikasi/broadcast SignalR untuk inject** — inject = data historis, tidak perlu real-time monitor.
- **Membuat akun pekerja baru** — inject hanya untuk pekerja (NIP) yang sudah ada di `AspNetUsers`.
- **Migration / perubahan skema DB** — milestone ini **0 migration** (semua tabel sudah ada).

---

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| INJ-01 | Phase 393 | Complete |
| INJ-02 | Phase 393 | Complete |
| INJ-03 | Phase 394 | Complete |
| INJ-04 | Phase 394 | Complete |
| INJ-05 | Phase 394 | Complete |
| INJ-06 | Phase 394 | Complete |
| INJ-07 | Phase 394 | Complete |
| INJ-08 | Phase 395 | Complete |
| INJ-09 | Phase 395 | Complete |
| INJ-10 | Phase 396 | Pending |
| INJ-11 | Phase 396 | Pending |
| INJ-12 | Phase 397 | Pending |
| INJ-13 | Phase 398 | Pending |

**Coverage: 13/13 v32.2 requirements mapped ✓ — Orphans: 0 — Duplicates: 0**

- **Phase 393 — Backend core inject:** INJ-01, INJ-02
- **Phase 394 — Page + Setup Room + authoring soal:** INJ-03, INJ-04, INJ-05, INJ-06, INJ-07
- **Phase 395 — Mode jawaban (input asli + auto-generate):** INJ-08, INJ-09
- **Phase 396 — Import Excel + retire BulkBackfill:** INJ-10, INJ-11
- **Phase 397 — Link Pre/Post ke room existing:** INJ-12
- **Phase 398 — Test + UAT "seakan online":** INJ-13

---

## Previous Milestone — v32.0 Manajemen Peserta (SHIPPED local 2026-06-17, history)

**Goal:** Penambahan peserta fleksibel saat ujian berjalan (391) + perbaikan `/Admin/CreateWorker` + audit field view-only (392). 7/7 REQ, 0 migration.

| REQ-ID | Phase | Status |
|--------|-------|--------|
| PART-01..04 | Phase 391 | Complete |
| WRKR-01..03 | Phase 392 | Complete |
