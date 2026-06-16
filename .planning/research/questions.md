# Research Questions

Open questions yang butuh investigasi lebih dalam. Append-only.

---

## Readiness Ujian Lisensor (2026-06-15, dari /gsd-explore)

Konteks: persiapan ujian REAL lisensor, tipe standard, ≤30 peserta, full lifecycle +
monitoring. Lihat `.planning/notes/2026-06-15-readiness-ujian-lisensor.md`.

- [ ] **Kapan tanggal ujian real-nya?** Menentukan urgensi & apakah window fix lokal →
  push → re-deploy IT cukup sebelum hari-H.
- [ ] **Konsekuensi kalau gagal di hari-H?** (peserta gagal sertifikasi, audit lisensor,
  reputasi) — menentukan toleransi risiko & kedalaman gladi-bersih.
- [ ] **Template sertifikat lisensor sudah siap & benar?** Layout, data peserta, nomor,
  tanda tangan — terbit otomatis setelah lulus?
- [ ] **Bank soal realistis sudah ada?** Atau perlu seed soal contoh (4 jenis + gambar)
  untuk gladi-bersih? Klasifikasi seed temporary + local-only.
- [x] **Aturan kelulusan lisensor — passing score.** ✅ JAWAB 2026-06-15: pakai **70%**
  (default kode `AssessmentSession.PassPercentage=70`, per-assessment configurable).
  Lulus iff `percentage >= PassPercentage`. Skor akhir = `totalScore/maxScore×100` integer
  truncate; tiap soal kontribusi = `ScoreValue`-nya (linear, tanpa weighting per-tipe).
  TODO verifikasi: assessment lisensor yang dibuat benar set PassPercentage=70.
- [x] **Multiple Answer scoring** ✅ JAWAB 2026-06-15: kode = **ALL-OR-NOTHING**
  (`maSelected.SetEquals(maCorrect)`, `Helpers/AssessmentScoreAggregator.cs:50`). Subset /
  ada salah / extra = **0**; hanya jawaban PERSIS semua-benar dapat poin penuh. Dibuktikan
  4 unit test skenario benar={A,C,D} di `HcPortal.Tests/AssessmentScoreAggregatorTests.cs`
  (10/10 PASS). **DECISION 2026-06-15 (user, setelah diskusi trade-off): PERTAHANKAN
  all-or-nothing** — standar lisensor sah + sudah ter-deploy/ter-test; ubah ke partial
  menjelang ujian = risiko ke kode v30 yang baru stabil (pecah asumsi biner Benar/Salah di
  scoring+display+PDF+monitoring). Partial credit (skema B net-penalti / C per-opsi) =
  milestone PASCA-ujian bila diinginkan. **CHECKPOINT readiness turunan:** verifikasi UI
  exam beri peringatan ke worker "pilih SEMUA jawaban benar — jawaban sebagian bernilai 0".
- [ ] **Essay** dinilai manual HC 0..ScoreValue (default 10/soal), ditambah ke total saat
  finalize. Konfirmasi: rubrik/bobot essay lisensor sudah fix?
