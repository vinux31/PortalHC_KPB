# Phase 393: Backend core inject - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input ke agent plan/research/execute.
> Keputusan final ada di CONTEXT.md — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-17
**Phase:** 393-backend-core-inject
**Areas discussed:** Guard duplikat/anti-double-cert, Perilaku batch saat error/NIP invalid, Sesi essay (lengkap vs pending), Validasi nilai & tanggal, Sertifikat tidak-lulus, Nomor cert manual (unik+ValidUntil), Cakupan AuditLog

---

## Guard duplikat / anti-double-cert

| Option | Description | Selected |
|--------|-------------|----------|
| Soft-block + override | Peringatan + checkbox lanjut (pola CreateAssessment) | |
| Hard-block (tolak) | Tolak, wajib hapus sesi lama dulu | |
| Skip + lapor | Lewati duplikat, inject sisanya, lapor | ✓ |

**User's choice:** Skip + lapor. Duplikat = sama jika **judul room / certificate (bila generate-cert on) + tanggal sama**.

| Option (key duplikat) | Description | Selected |
|--------|-------------|----------|
| UserId + Judul(norm) + Kategori + Tanggal | Paling presisi, mirror soft-block + per-pekerja | ✓ |
| UserId + Judul(norm) + Tanggal | Abaikan kategori | |
| UserId + Judul + CompletedAt persis | Pola BulkBackfill (sensitif jam) | |

**User's choice:** UserId + Judul(norm) + Kategori + Tanggal + **tombol "Cek"** seperti form create assessment.
**Notes:** Cert-aware. Tombol Cek = UI Phase 394.

## Perilaku batch saat error / NIP invalid

| Option | Description | Selected |
|--------|-------------|----------|
| Pre-flight, tolak semua | Validasi dulu; ada invalid → tolak seluruh submit + error per-baris, nol tulisan; tx rollback safety-net | ✓ |
| Partial: inject valid, skip+lapor | Inject valid, lewati & lapor invalid | |

**User's choice:** Pre-flight, tolak semua.
**Notes:** Berbeda kategori dari duplikat (skip+lapor). Invalid = NIP tak dikenal / opsi salah / skor out-of-range / essay tanpa skor / tanggal tak masuk akal.

## Sesi essay: wajib lengkap vs boleh pending

| Option | Description | Selected |
|--------|-------------|----------|
| Wajib skor → Completed | HC wajib isi EssayScore; Status=Completed | ✓ |
| Boleh pending | Boleh ditinggal "Menunggu Penilaian" | |
| Campur per-pekerja | Boleh keduanya | |

**User's choice:** Wajib skor → Completed. Essay tanpa skor = invalid (pre-flight tolak).

## Validasi nilai & tanggal

| Option (tanggal) | Description | Selected |
|--------|-------------|----------|
| Wajib ≤ hari ini | Tolak masa depan + tanggal absurd | ✓ |
| Bebas (boleh masa depan) | Terima kapan saja | |

| Option (range skor essay) | Description | Selected |
|--------|-------------|----------|
| 0..ScoreValue soal | Poin mentah sesuai bobot soal, identik online | ✓ |
| 0..100 bebas | Input 0..100 (mismatch model) | |

**User's choice:** Backdate ≤ hari ini; EssayScore 0..ScoreValue.
**Notes:** User minta klarifikasi beda 2 opsi range skor → dijelaskan EssayScore = poin-soal (0..ScoreValue) yang di-aggregate jadi persen; 0..100 mismatch & korup skor. User pilih 0..ScoreValue.

## Sertifikat untuk yang TIDAK lulus

| Option | Description | Selected |
|--------|-------------|----------|
| Suppress (hanya lulus) | Cert hanya isPassed=true, mirror online | ✓ |
| Tetap buat | Paksa cert walau tidak lulus | |
| Tolak (invalid) | Anggap inconsistent → pre-flight tolak | |

**User's choice:** Suppress (hanya lulus).

## Nomor cert manual: unik & ValidUntil

| Option (unik) | Description | Selected |
|--------|-------------|----------|
| Wajib unik (collision → tolak) | UNIQUE index; bentrok → pre-flight tolak | ✓ |
| Terima apa adanya | Tak cek (risiko error DB) | |

| Option (ValidUntil) | Description | Selected |
|--------|-------------|----------|
| Manual opsional; auto ikut online | HC boleh isi/kosong; auto ikut online | (lihat catatan) |
| Wajib diisi (manual) | Paksa isi | |
| Selalu kosong | Abaikan ValidUntil | |

**User's choice:** Nomor manual wajib unik. ValidUntil = **fleksibel, HC bisa isi ValidUntil atau set ke permanent** (null = permanent), manual & auto.

## Cakupan AuditLog inject

| Option | Description | Selected |
|--------|-------------|----------|
| Sukses + skip | ManualInject per sukses + log skip duplikat | |
| Sukses + skip + reject | Tambah log submit ditolak pre-flight (compliance maks) | ✓ |
| Sukses saja | Minimal INJ-02 | |

**User's choice:** Sukses + skip + reject (compliance maksimal). ActionType terpisah agar count ManualInject tetap = jumlah sesi sukses.

## Claude's Discretion (teknis → researcher/planner)

- Mekanisme reuse `FinalizeEssayGrading` (controller action, bukan service) — extract helper vs replikasi data-level (pola xUnit Phase 387); wajib nol-duplikasi semantik.
- Kontrak `InjectAssessmentService` (DTO/signature).
- Nilai field sesi inject (`AccessToken="INJECT"`, `IsTokenRequired=false`, `AssessmentType` Standard/Pre/Post bukan "Manual", `AllowAnswerReview=true`) + verifikasi `/CMP/Results` & `GetUnifiedRecords` tak branch `AssessmentType="Manual"`.
- Anchor paket sentinel + alokasi cert dalam transaction (rollback reclaim, sekuens resmi sama online).

## Deferred Ideas

- Essay boleh "Menunggu Penilaian" (ditolak 393, D-05).
- Partial-batch untuk baris invalid (ditolak, D-03).
- Auto-generate skor target → Phase 395. Excel → 396. Link Pre/Post → 397. Page/UI → 394.
- Multi-paket per room / import gambar Excel / edit massal inject → out of scope milestone.
