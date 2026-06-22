# Requirements: Portal HC KPB — v32.6 Section + Scoped Shuffle + Section Pagination + Opsi Dinamis

**Defined:** 2026-06-22
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Design spec:** `docs/superpowers/specs/2026-06-22-section-scoped-shuffle-pagination-dynamic-options-design.md` (15 keputusan D-01..D-15 + §15 addendum re-check)
**Migration:** TRUE — tabel `AssessmentPackageSection` + kolom `PackageQuestion.SectionId` nullable (fondasi). Sisanya migration=FALSE.

## v1 Requirements

Requirements milestone v32.6. Tiap REQ map ke satu fase roadmap (fase mulai 415).

### Section (SEC)

- [ ] **SEC-01**: HC dapat membuat, mengubah, menghapus, dan mengurutkan Section (No. Section + Nama) pada tiap paket lewat UI web.
- [ ] **SEC-02**: HC dapat mengatur toggle "Mulai Halaman Baru" dan "Acak" per-Section, termasuk tombol cepat "semua Section mulai halaman baru".
- [ ] **SEC-03**: HC dapat menetapkan Section pada soal lewat No. Section; soal tanpa Section masuk grup "Lainnya" di urutan terakhir (perilaku global lama, kompatibel-mundur).
- [ ] **SEC-04**: Sistem menolak (hard-block) menyimpan/memulai ujian bila struktur Section antar-paket dalam satu assessment tidak identik (jumlah soal per Section berbeda), dengan pesan jelas.
- [ ] **SEC-05**: Daftar & preview soal admin menampilkan soal dikelompokkan per Section dengan header.
- [ ] **SEC-06**: Saat sinkronisasi Pre→Post (SamePackage), struktur Section + opsi ikut tersalin ke PostTest.

### Scoped Shuffle (SHF)

- [ ] **SHF-01**: Pengacakan soal hanya terjadi di dalam Section (soal tidak melompat antar-Section); assessment tanpa Section berperilaku sama seperti sekarang.
- [ ] **SHF-02**: HC dapat menyalakan/mematikan acak per-Section; toggle level-assessment berfungsi sebagai induk, toggle per-Section sebagai anak.
- [ ] **SHF-03**: Untuk >1 paket, tiap Section diisi dari gabungan Section padanan lintas-paket lalu diacak/di-sampling dalam batas Section; jaminan cakupan Elemen Teknis berlaku per-Section.
- [ ] **SHF-04**: Reshuffle (per-paket & semua peserta) menghormati batas Section.

### Section Pagination (PAG)

- [ ] **PAG-01**: Default tampilan ujian = 10 soal/halaman mengalir, dengan header Section saat berganti Section.
- [ ] **PAG-02**: Section ber-"Mulai Halaman Baru" dimulai di halaman baru; Section panjang otomatis terpecah per 10 soal.
- [ ] **PAG-03**: Resume ujian (LastActivePage) tetap mengarah ke halaman yang benar saat pagination Section aktif.
- [ ] **PAG-04**: Export per-soal (Excel/PDF) menampilkan label/header Section.

### Dynamic Options (OPT)

- [ ] **OPT-01**: HC dapat membuat/mengubah soal dengan 2–6 opsi jawaban (bukan terkunci A–D) lewat form authoring web dan form Inject.
- [ ] **OPT-02**: Layar ujian, preview, dan hasil menampilkan huruf opsi A–F dinamis sesuai jumlah opsi, dan penilaian tetap benar.
- [ ] **OPT-03**: Kolom "Jawaban Benar" (form & import) menerima huruf A–F; minimal 2 dan maksimal 6 opsi ditegakkan.

### Import Excel (IMP)

- [ ] **IMP-01**: Template & parser import soal mendukung kolom No. Section, Nama Section, dan Opsi A–F.
- [ ] **IMP-02**: Dual-format kompatibel-mundur — file template lama 9-kolom (Opsi A–D, tanpa Section) tetap bisa di-import.
- [ ] **IMP-03**: Import memvalidasi jumlah soal per-Section antar-paket (tolak keras) dan fingerprint anti-duplikat menyertakan Section + opsi 5–6.

## v2 Requirements

Deferred — diketahui tapi di luar roadmap v32.6.

### Section Sampling (SAMP)

- **SAMP-01**: HC dapat set "ambil N soal acak dari M" per-Section (subset sampling manual). *(Ditolak saat brainstorm Q4 — variasi anti-nyontek sudah dari pooling antar-paket; buka bila perlu.)*

### Section Reporting (SREP)

- **SREP-01**: Hasil/sertifikat menampilkan breakdown skor per-Section. *(D-12 = tidak untuk v32.6; breakdown Elemen Teknis sudah cukup.)*

## Out of Scope

| Feature | Reason |
|---------|--------|
| Excel "zero-config": dropdown Data Validation + import skor per-soal | Milestone quick-win terpisah; tak menghalangi Section. |
| Tipe soal baru (Matching / File Upload / Text-Media) | Butuh engine penilaian + UI + storage baru = scope besar; tetap Single/Multiple/Essay. |
| Breakdown skor per-Section di hasil/sertifikat (D-12) | Breakdown Elemen Teknis sudah ada & ortogonal; tambah hanya bila diminta (→ SREP-01). |
| Page-number per-soal (kolom ClassMarker) | Rusak oleh acak; kontrol halaman di tingkat Section (D-11). |
| Hapus/Tambah peserta | Sudah selesai di v32.5 (Flexible Add/Remove Participant). |
| Ubah transaksi/audit soft-remove (warning MARS) | Di luar tema; reliabel di semua tes; tak disentuh. |

## Traceability

Diisi oleh roadmapper saat pembuatan roadmap.

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEC-01..06 | TBD | Pending |
| SHF-01..04 | TBD | Pending |
| PAG-01..04 | TBD | Pending |
| OPT-01..03 | TBD | Pending |
| IMP-01..03 | TBD | Pending |

**Coverage:**
- v1 requirements: 20 total
- Mapped to phases: 0 (roadmap pending)
- Unmapped: 20 ⚠️ (akan dipetakan roadmapper)

---
*Requirements defined: 2026-06-22*
*Last updated: 2026-06-22 after initial definition (milestone v32.6)*
