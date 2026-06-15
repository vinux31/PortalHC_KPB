# Requirements — Milestone v31.0 Hotfix Pra-Ujian Lisensor

**Milestone:** v31.0 — Hotfix Pra-Ujian Lisensor
**Created:** 2026-06-15
**Goal:** Perbaiki 5 temuan readiness yang menghambat ujian lisensor real (~2026-06-17) dalam 1 bundle deploy (Phase 385-386), + bersihkan 9 temuan polish sisa pasca-acara (Phase 387).
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

### Post-Lisensor Polish (PXF-06..14) — Phase 387, dikerjakan PASCA-acara

Batch polish 2 MED + 6 LOW + 1 MED (same-file fold) dari register readiness — tidak menghambat hari-H, deploy IT **kedua** (terpisah dari bundle urgent 385-386). 0 migration.

- [ ] **PXF-06** (F-03, MED): `SubmitEssayScore` **guard status** — edit skor essay pasca-Completed/Failed tidak men-desync `Score`/`IsPassed` tersimpan (block atau recompute). Saat ini save `EssayScore` langsung tanpa cek `session.Status`.
- [ ] **PXF-07** (F-02, MED): Excel "Detail Per Soal" label essay benar/salah pakai helper kanonik `IsQuestionCorrect` (`>0`), **bukan** aturan lama `EssayScore >= ScoreValue/2` (`ExcelExportHelper.cs:111`).
- [ ] **PXF-08** (F-06, LOW): Nomor sertifikat jalur finalize essay pakai **retry-loop + log** (samakan GradingService 3×) — hilangkan single-attempt + catch silent yang bisa loloskan lulus tanpa nomor cert.
- [ ] **PXF-09** (F-19, LOW): Excel BulkExport "Detail Jawaban" **menampilkan skor/teks essay** yang sudah dinilai (bukan selalu "—").
- [ ] **PXF-10** (F-13, LOW): `FinalizeEssayGrading` **broadcast monitor group** agar tab admin lain tidak stale s/d refresh.
- [ ] **PXF-11** (F-11, LOW): a11y — gambar opsi Results/ExamSummary sertakan **label huruf A/B/C/D** pada `AriaContext` (`Results.cshtml:388`).
- [ ] **PXF-12** (F-20, LOW): `SubmitExam` upsert MC **tidak menimpa jawaban tersimpan jadi null** bila soal tak ada di form answers (data-loss laten).
- [ ] **PXF-13** (F-22, LOW): `Hub.SaveTextAnswer` **guard timer-expired** (samakan `SaveMultipleAnswer:205-212`) — essay tak bisa ditulis lewat batas waktu.
- [ ] **PXF-14** (F-DEV-02, MED): Excel "Detail Per Soal" nilai soal **Multiple Answer akurat** (SetEquals semua opsi terpilih, bukan `FirstOrDefault` 1-baris) — `ExcelExportHelper.cs:83`, surface MA-mislabel kedua selain PXF-05/F-17, **same-method dengan PXF-07** (satu kali edit blok 78-115).

---

## Future Requirements (deferred — pasca-acara)

- **F-01** — UI exam MA tak memberi tahu "jawaban sebagian = 0 poin" (mitigasi: briefing peserta; murni UX, backend all-or-nothing benar).

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
| PXF-06 | F-03 | Phase 387 | Pending |
| PXF-07 | F-02 | Phase 387 | Pending |
| PXF-08 | F-06 | Phase 387 | Pending |
| PXF-09 | F-19 | Phase 387 | Pending |
| PXF-10 | F-13 | Phase 387 | Pending |
| PXF-11 | F-11 | Phase 387 | Pending |
| PXF-12 | F-20 | Phase 387 | Pending |
| PXF-13 | F-22 | Phase 387 | Pending |
| PXF-14 | F-DEV-02 | Phase 387 | Pending |

**Coverage: 14/14 ter-map ✓ — Orphans: 0 — Duplicates: 0.**

**Phase mapping (roadmap 2026-06-15):**
- **Phase 385 — Exam-Taking & Image Render Hotfix:** PXF-01 (`_QuestionImage.cshtml` PathBase-aware), PXF-03 (`StartExam.cshtml` flush essay). File view berbeda dari Phase 386 → file-disjoint, aman paralel.
- **Phase 386 — AssessmentAdminController Hardening:** PXF-02 + PXF-04 + PXF-05. Ketiga REQ menyentuh `Controllers/AssessmentAdminController.cs` → **digabung satu fase** untuk hindari konflik write paralel.
- **Phase 387 — Post-Lisensor Assessment Polish:** PXF-06..14 (9 REQ). Dikerjakan PASCA-acara, deploy IT kedua. Mayoritas (`PXF-06/08/09/10`) di `AssessmentAdminController.cs`; PXF-07+PXF-14 same-method `ExcelExportHelper.cs`; PXF-11 `Results.cshtml`; PXF-12 `CMPController.cs`; PXF-13 `Hubs/AssessmentHub.cs`. Satu fase sekuensial → nol konflik write. **Depends on Phase 386** (file-overlap controller).
- Penomoran fase LANJUT dari v30.0 (phase terakhir = 384). Semua **0 migration**.
