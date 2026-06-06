# Requirements — Milestone v24.0: Gambar di Soal Assessment (Manage Package)

**Defined:** 2026-06-06
**Source:** `docs/superpowers/specs/2026-06-06-image-in-assessment-questions-design.md`
**Approach:** Spec-driven (brainstorm → spec → milestone). Skip domain-research.

**Goal:** Admin bisa melampirkan gambar pada soal assessment dan tiap pilihan jawaban
(semua tipe MC/MA/Essay), tampil konsisten di seluruh layar tempat soal muncul.

---

## v24.0 Requirements

### IMG — Upload & Data Gambar

- [ ] **IMG-01**: Admin dapat upload 1 gambar (JPG/PNG, ≤2MB) ke sebuah soal
- [ ] **IMG-02**: Admin dapat upload 1 gambar (JPG/PNG, ≤2MB) ke tiap pilihan jawaban (MC/MA)
- [ ] **IMG-03**: Admin dapat mengisi alt text opsional per gambar (soal & opsi)
- [ ] **IMG-04**: Sistem menolak file non-gambar (mis. PDF/exe) via validasi magic-byte image-only
- [ ] **IMG-05**: Admin dapat mengganti gambar yang sudah ada (file lama terhapus dari disk)
- [ ] **IMG-06**: Admin dapat menghapus gambar dari soal/opsi (checkbox hapus di form)
- [ ] **IMG-07**: Saat edit soal, gambar lama tampil (prefill thumbnail) di form admin

### RND — Render di 6 Layar

- [ ] **RND-01**: Peserta melihat gambar soal + opsi saat ujian (StartExam)
- [ ] **RND-02**: Peserta melihat gambar soal + opsi di review sebelum submit (ExamSummary)
- [ ] **RND-03**: Peserta melihat gambar soal + opsi di halaman pembahasan/hasil (Results)
- [ ] **RND-04**: Admin melihat gambar soal + opsi di preview saat bikin/edit soal (_PreviewQuestion)
- [ ] **RND-05**: Admin melihat gambar soal di halaman nilai essay (AssessmentMonitoringDetail)
- [ ] **RND-06**: Admin melihat gambar soal + opsi saat edit jawaban peserta (EditPesertaAnswers)
- [ ] **RND-07**: Gambar tampil responsive (img-fluid + loading=lazy + alt) di semua layar

### SYN — Integritas Data & File

- [ ] **SYN-01**: Gambar (ImagePath+ImageAlt) ikut tersalin saat sinkron Pre→Post; copy memakai path file yang sama (shared-file), sync tidak membuat/menghapus file fisik
- [ ] **SYN-02**: File gambar terhapus atomic (kumpul path sebelum tx, File.Delete setelah CommitAsync, pola Phase 333) saat soal/opsi dihapus atau gambar di-replace

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

| REQ | Phase | Status |
|-----|-------|--------|
| IMG-01..07 | TBD (roadmapper) | pending |
| RND-01..07 | TBD (roadmapper) | pending |
| SYN-01..02 | TBD (roadmapper) | pending |
| TST-01..02 | TBD (roadmapper) | pending |

_(Diisi roadmapper saat membuat ROADMAP.md.)_
