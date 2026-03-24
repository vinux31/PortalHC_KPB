# UAT Checklist — Phase 245: Proton Assessment

Dibuat dari hasil code review Plan 01. Semua 4 requirement PROT-01 s/d PROT-04 tercakup.

**Catatan Seed Data:**
- Login Rino: gunakan akun sesuai seed (worker level 6)
- Token Proton Tahun 1: `UAT-PROTON-T1`
- Token Proton Tahun 3: lihat seed SeedData.cs (AccessToken Tahun 3)
- PROT-02 ISSUE: DurationMinutes seed Tahun 3 = 120 (bukan 0) — ini minor issue, tidak mempengaruhi alur interview

---

## PROT-01 — Proton Tahun 1 Online Exam

*Requirement: Worker Rino dapat mengikuti ujian Proton Tahun 1 dengan flow yang sama seperti reguler*

- [ ] **[HV-01]** Login sebagai Rino (worker) → buka halaman Assessment → assessment "Operator - Tahun 1" tampil dalam daftar
- [ ] **[HV-02]** Klik assessment → masukkan token `UAT-PROTON-T1` → ujian dimulai → verifikasi halaman soal muncul (flow sama dengan ujian reguler CMPController)
- [ ] **[HV-03]** Verifikasi soal muncul dengan opsi pilihan, pagination antar soal berfungsi, auto-save bekerja
- [ ] **[HV-04]** Submit jawaban Proton Tahun 1 → ExamSummary muncul dengan skor dan status Lulus/Tidak Lulus

---

## PROT-02 — Proton Tahun 3 Assessment (tanpa paket soal)

*Requirement: Assessment Tahun 3 tersimpan tanpa paket soal, tanpa timer*

- [ ] **[HV-05]** Gunakan seed: assessment "Operator - Tahun 3" Rino sudah tersedia → konfirmasi assessment dengan kategori "Assessment Proton" dan TahunKe "Tahun 3" ada di sistem
- [ ] **[HV-06]** Verifikasi tidak ada timer/countdown muncul saat HC membuka AssessmentMonitoringDetail untuk session Tahun 3 (DurationMinutes seed = 120 tapi tidak mempengaruhi alur karena ditentukan oleh TahunKe)
- [ ] **[HV-07]** *(Opsional)* Admin membuat assessment Proton Tahun 3 baru via CreateAssessment → Category "Assessment Proton" → track → DurationMinutes bisa diisi 0 tanpa validation error

---

## PROT-03 — HC Input Interview Results (4 skenario)

*Requirement: HC dapat input hasil interview Tahun 3 dengan 5 aspek skor 1-5, judges, notes, IsPassed*

- [ ] **[HV-08]** Login HC → buka AssessmentMonitoringDetail untuk batch yang berisi session Proton Tahun 3 Rino → verifikasi form interview 5 aspek muncul (bukan form soal reguler)
- [ ] **[HV-09]** **Skenario LULUS:** Isi 5 dropdown aspek (Pengetahuan Teknis, Kemampuan Operasional, Keselamatan Kerja, Komunikasi & Kerjasama, Sikap Profesional) skor 1-5, isi Judges, isi Notes, centang IsPassed=true → klik Submit → verifikasi pesan sukses "Hasil interview berhasil disimpan." muncul
- [ ] **[HV-10]** **Skenario GAGAL:** Buat session baru atau gunakan session berbeda → isi aspek → IsPassed=false → submit → verifikasi session.Status = "Completed" tapi ProtonFinalAssessment TIDAK ter-create
- [ ] **[HV-11]** **Skenario UPLOAD:** Upload dokumen pendukung (PDF/DOC/JPG maks 10MB) → verifikasi file tersimpan di `/uploads/interviews/` dan nama file format `{sessionId}_{epoch}{ext}`
- [ ] **[HV-12]** **Skenario EDIT:** Submit ulang form interview yang sudah di-submit → verifikasi data ter-update (bukan duplikat baru)

---

## PROT-04 — ProtonFinalAssessment Auto-Create + Akses Worker + Idempotency

*Requirement: ProtonFinalAssessment dibuat otomatis saat Tahun 3 ditandai lulus; tidak dibuat saat gagal; worker bisa akses*

- [ ] **[HV-13]** Setelah HV-09 (skenario lulus): Login Rino → buka CDP → HistoriProton → verifikasi ProtonFinalAssessment record ada dengan status "Completed"
- [ ] **[HV-14]** Verifikasi HV-10 (skenario gagal): Login Rino → CDP → HistoriProton → ProtonFinalAssessment untuk session gagal TIDAK ada
- [ ] **[HV-15]** **Idempotency:** Submit ulang hasil interview lulus yang sudah punya ProtonFinalAssessment → verifikasi tidak ada duplikat record di DB (idempotency guard aktif)
- [ ] **[HV-16]** Worker (Rino) hanya bisa melihat data ProtonFinalAssessment miliknya sendiri (authorization level 6 guard)

---

## Catatan dari Code Review (Issues & Open Items)

| ID | Status | Deskripsi | Dampak |
|----|--------|-----------|--------|
| PROT-01 | OK | CreateAssessment mendeteksi Proton Tahun 1/2 via ProtonTrack.TahunKe | Tidak ada |
| PROT-02 | ISSUE minor | Seed DurationMinutes Tahun 3 = 120 (bukan 0); server override benar | Tidak mempengaruhi fungsionalitas interview |
| PROT-03 | OK | 5 aspek collected, ViewBag.GroupTahunKe di-set, form interview lengkap | Tidak ada |
| PROT-04 | OK | Idempotency guard AnyAsync ada, ProtonFinalAssessment hanya dibuat jika IsPassed=true | Tidak ada |

**Sertifikat Proton:** Tidak ada PDF — completion ditunjukkan via record ProtonFinalAssessment di HistoriProtonDetail timeline. Ini desain intentional.

---

*Generated: 2026-03-24 | Plan 245-02 Task 1*
