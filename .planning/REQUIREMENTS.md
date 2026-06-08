# Requirements — Milestone v24.0: Gambar di Soal Assessment (Manage Package)

**Defined:** 2026-06-06
**Source:** `docs/superpowers/specs/2026-06-06-image-in-assessment-questions-design.md`
**Approach:** Spec-driven (brainstorm → spec → milestone). Skip domain-research.

**Goal:** Admin bisa melampirkan gambar pada soal assessment dan tiap pilihan jawaban
(semua tipe MC/MA/Essay), tampil konsisten di seluruh layar tempat soal muncul.

---

## v24.0 Requirements

### IMG — Upload & Data Gambar

- [x] **IMG-01**: Admin dapat upload 1 gambar (JPG/PNG, ≤5MB) ke sebuah soal _(2MB→5MB per Phase 352 CONTEXT D-03, 2026-06-06)_
- [x] **IMG-02**: Admin dapat upload 1 gambar (JPG/PNG, ≤5MB) ke tiap pilihan jawaban (MC/MA)
- [x] **IMG-03**: Admin dapat mengisi alt text opsional per gambar (soal & opsi)
- [ ] **IMG-04**: Sistem menolak file non-gambar (mis. PDF/exe) via validasi magic-byte image-only
- [x] **IMG-05**: Admin dapat mengganti gambar yang sudah ada (file lama terhapus dari disk)
- [x] **IMG-06**: Admin dapat menghapus gambar dari soal/opsi (checkbox hapus di form)
- [x] **IMG-07**: Saat edit soal, gambar lama tampil (prefill thumbnail) di form admin

### RND — Render di 6 Layar

- [ ] **RND-01**: Peserta melihat gambar soal + opsi saat ujian (StartExam)
- [ ] **RND-02**: Peserta melihat gambar soal + opsi di review sebelum submit (ExamSummary)
- [ ] **RND-03**: Peserta melihat gambar soal + opsi di halaman pembahasan/hasil (Results)
- [ ] **RND-04**: Admin melihat gambar soal + opsi di preview saat bikin/edit soal (_PreviewQuestion)
- [ ] **RND-05**: Admin melihat gambar soal di halaman nilai essay (AssessmentMonitoringDetail)
- [ ] **RND-06**: Admin melihat gambar soal + opsi saat edit jawaban peserta (EditPesertaAnswers)
- [ ] **RND-07**: Gambar tampil responsive (img-fluid + loading=lazy + alt) di semua layar

### SYN — Integritas Data & File

- [x] **SYN-01**: Gambar (ImagePath+ImageAlt) ikut tersalin saat sinkron Pre→Post; copy memakai path file yang sama (shared-file), sync tidak membuat/menghapus file fisik
- [x] **SYN-02**: File gambar terhapus atomic (kumpul path sebelum tx, File.Delete setelah CommitAsync, pola Phase 333) saat soal/opsi dihapus atau gambar di-replace

### TST — Test & UAT

- [ ] **TST-01**: xUnit cover upload valid (JPG/PNG tersimpan) + invalid (non-image ditolak) + SyncPackagesToPost menyalin ImagePath + DeleteQuestion menghapus file gambar
- [ ] **TST-02**: Playwright UAT — admin upload gambar soal+opsi → peserta lihat di StartExam → lihat di Results (pembahasan)

---

## Out of Scope (v24.0)

| Item | Alasan |
|------|--------|
| Bulk import gambar (zip/URL kolom Excel) | Keputusan brainstorm #4 — manual-only; import Excel tetap teks |
| Banyak gambar per soal/opsi (galeri) | Keputusan brainstorm #2 — 1 gambar per soal + 1 per opsi |
| Edit/crop gambar dalam aplikasi | Di luar kebutuhan inti; admin siapkan gambar sebelum upload |
| Image CDN / srcset multi-resolusi | `img-fluid` + lazy-load cukup untuk skala internal |
| Perbaikan XSS `@QuestionText` render bare existing | Aman untuk teks; gambar via `src` encoded tak nambah surface; keputusan terpisah (spec §8) |

---

## Future Requirements (deferred)

- Bulk import gambar bila kebutuhan volume tinggi muncul.
- Multi-gambar/galeri per soal bila tipe soal kompleks dibutuhkan.

---

## Traceability

> Revised 2026-06-06: milestone dikompresi 5 → 4 phase. Old Phase 353 (Admin CRUD) + old Phase 354 (Sync/Cleanup) MERGED → Phase 353 "Admin Backend Gambar". Old 355 Render → 354, old 356 Test/UAT → 355. 17/17 REQ tetap mapped (none dropped); Phase 353 kini memegang 9 REQ.

| REQ | Phase | Status |
|-----|-------|--------|
| IMG-04 | Phase 352 | Pending |
| IMG-01 | Phase 353 | Complete |
| IMG-02 | Phase 353 | Complete |
| IMG-03 | Phase 353 | Complete |
| IMG-05 | Phase 353 | Complete |
| IMG-06 | Phase 353 | Complete |
| IMG-07 | Phase 353 | Complete |
| RND-04 | Phase 353 | Pending |
| SYN-01 | Phase 353 | Complete |
| SYN-02 | Phase 353 | Complete |
| RND-01 | Phase 354 | Pending |
| RND-02 | Phase 354 | Pending |
| RND-03 | Phase 354 | Pending |
| RND-05 | Phase 354 | Pending |
| RND-06 | Phase 354 | Pending |
| RND-07 | Phase 354 | Pending |
| TST-01 | Phase 355 | Pending |
| TST-02 | Phase 355 | Pending |

**Coverage: 17/17 mapped ✓ — Orphans: 0 — Duplicates: 0**

**Phase grouping rationale (4-phase revised):**
- **Phase 352** (Data Foundation + Image-Only Upload): IMG-04 (helper magic-byte image-only) + migration 4 kolom + entity (fondasi semua phase).
- **Phase 353** (Admin Backend Gambar — CRUD + Sync + Atomic Delete): IMG-01/02/03/05/06/07 (form upload/alt/replace/remove/prefill) + RND-04 (preview admin, cohere ke form CRUD via `_PreviewQuestion`) + SYN-01 (Gap 1 shared-file Pre→Post) + SYN-02 (atomic file delete pola Phase 333). **Sengaja lebih besar (gabungan old 353+354)** tetapi kohesif: semuanya menyentuh satu file `AssessmentAdminController.cs` (CRUD ~L6067-6377, JSON prefill L6214, SyncPackagesToPost L5337, DeleteQuestion L6377) dan keduanya memang sequential-strict. 9 REQ, 7 success criteria.
- **Phase 354** (Render): RND-01/02/03/05/06/07 (6 layar + responsive) + Gap 2 ViewModel. Menulis `CMPController.cs` + ViewModels + 6 view. Depends 353 (shared-file path final).
- **Phase 355** (Test/UAT): TST-01 (xUnit konsolidasi) + TST-02 (Playwright UAT end-to-end). Test logic-bearing tetap di-fold incremental ke 352/353/354; 355 = suite final + UAT lintas-stack.

_(Diisi roadmapper saat membuat ROADMAP.md — 2026-06-06; direvisi 5→4 phase 2026-06-06.)_
