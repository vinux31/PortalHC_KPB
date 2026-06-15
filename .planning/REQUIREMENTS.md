# Requirements — Milestone v31.0 Hotfix Pra-Ujian Lisensor

**Milestone:** v31.0 — Hotfix Pra-Ujian Lisensor
**Created:** 2026-06-15
**Goal:** Perbaiki 5 temuan readiness yang menghambat ujian lisensor real (~2026-06-17) dalam 1 bundle deploy.
**Sumber:** `.planning/notes/2026-06-15-readiness-ujian-lisensor.md` (register temuan final, adversarial-verified).
**Konteks:** ujian SA+MA+Essay+gambar, PDF per-peserta = bukti resmi, ≤30 peserta. **0 migration.**

---

## Requirements v31.0

### Pre-eXam Fix (PXF) — must-fix sebelum hari-H

- [ ] **PXF-01** (F-09, HIGH): Gambar pada soal & pilihan **tampil benar** saat aplikasi dijalankan di sub-path (`/KPB-PortalHC` di Dev/Prod) — `<img src>` resolusi PathBase-aware, tidak 404. Berlaku di semua layar render gambar (StartExam, ExamSummary, Results, grading). Confirmed broken di Dev (404, prefix drop).
- [ ] **PXF-02** (F-DEV-01, HIGH): Admin **tidak bisa menyimpan** soal tipe Single/Multiple Answer **tanpa opsi jawaban** — `CreateQuestion` & `EditQuestion` menolak (validasi ≥1 opsi ber-teks, idealnya ≥2 dengan ≥1 benar) dengan pesan jelas, sehingga tak ada soal cacat yang memblokir submit ujian.
- [ ] **PXF-03** (F-21, HIGH): Jawaban **essay tersimpan utuh** saat peserta submit / pindah halaman / waktu habis — di-flush sebelum form dikirim (tidak menunggu debounce 2 detik), sehingga keystroke terakhir tidak hilang dan peserta yang sudah mengisi essay tidak ditolak submit "belum dijawab".
- [ ] **PXF-04** (F-04, MED): HC **bisa menyelesaikan penilaian** assessment walau peserta **mengosongkan essay** (tanpa baris jawaban) — hitungan pending konsisten antar surface, tombol "Selesaikan Penilaian" muncul, tidak dead-end (essay kosong = 0 poin otomatis).
- [ ] **PXF-05** (F-17, MED): **PDF bukti per-peserta** menandai soal **Multiple Answer benar/salah secara akurat** (all-or-nothing / SetEquals, baca semua opsi terpilih) — konsisten dengan penilaian web/Excel, karena PDF dipakai sebagai bukti/arsip resmi lisensor.

---

## Future Requirements (deferred — pasca-acara)

Temuan readiness severity lebih rendah / mitigatable / report-cosmetic, ditunda ke milestone berikutnya:

- **F-02** — Excel "Detail Per Soal" label essay benar/salah pakai aturan lama (`≥SV/2`) vs kanonik (`>0`).
- **F-03** — Edit skor essay pasca-finalize men-desync Score/IsPassed tersimpan.
- **F-01** — UI exam MA tak memberi tahu "jawaban sebagian = 0 poin" (mitigasi: briefing peserta).
- **F-06** — Nomor sertifikat (jalur finalize essay) single-attempt no-retry → collision multi-HC silent null.
- **F-11** — a11y: gambar opsi Results/ExamSummary tanpa label huruf A/B/C/D.
- **F-13** — `FinalizeEssayGrading` tak broadcast monitor group (tab admin lain stale).
- **F-19** — Excel BulkExport "Detail Jawaban" essay selalu "—" (tak tampil skor/teks).
- **F-20** — `SubmitExam` upsert MC bisa timpa jawaban tersimpan jadi null (data-loss laten, happy-path aman).
- **F-22** — `Hub.SaveTextAnswer` tanpa guard timer-expired (essay bisa ditulis lewat batas waktu).

---

## Out of Scope (milestone ini)

- **F-18** (export by-paket bukan ShuffledQuestionIds, ≥2 paket): **kondisional** — diperbaiki HANYA jika ujian pakai >1 paket. Mitigasi operasional: **pakai 1 paket** → tak relevan. Tidak di-fix di v31.0 kecuali konfirmasi multi-paket.
- Migration DB apa pun (semua fix = view/controller/validasi, 0 migration).
- Refactor besar / fitur baru / perbaikan menyeluruh modul export.

---

## Traceability

| REQ-ID | Temuan | Phase | Status |
|--------|--------|-------|--------|
| PXF-01 | F-09 | Phase 385 | Pending |
| PXF-03 | F-21 | Phase 385 | Pending |
| PXF-02 | F-DEV-01 | Phase 386 | Pending |
| PXF-04 | F-04 | Phase 386 | Pending |
| PXF-05 | F-17 | Phase 386 | Pending |

**Coverage: 5/5 ter-map ✓ — Orphans: 0 — Duplicates: 0.**

**Phase mapping (roadmap 2026-06-15):**
- **Phase 385 — Exam-Taking & Image Render Hotfix:** PXF-01 (`_QuestionImage.cshtml` PathBase-aware), PXF-03 (`StartExam.cshtml` flush essay). File view berbeda dari Phase 386 → file-disjoint, aman paralel.
- **Phase 386 — AssessmentAdminController Hardening:** PXF-02 + PXF-04 + PXF-05. Ketiga REQ menyentuh `Controllers/AssessmentAdminController.cs` → **digabung satu fase** untuk hindari konflik write paralel.
- Penomoran fase LANJUT dari v30.0 (phase terakhir = 384). Semua **0 migration**.
