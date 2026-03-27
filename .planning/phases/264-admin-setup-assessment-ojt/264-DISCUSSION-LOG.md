# Phase 264: Admin Setup Assessment OJT - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-27
**Phase:** 264-admin-setup-assessment-ojt
**Areas discussed:** Skenario test UAT, Data & prasyarat test, Kriteria pass/fail, Penanganan bug

---

## Skenario Test UAT

### Q1: Jumlah variasi assessment

| Option | Description | Selected |
|--------|-------------|----------|
| 1 assessment lengkap | Buat 1 assessment OJT dengan semua field terisi | |
| 2-3 variasi | Variasi token, durasi, passing grade | ✓ |
| Test semua kombinasi | Cover semua opsi konfigurasi | |

**User's choice:** 2-3 variasi
**Notes:** —

### Q2: Variasi yang paling penting

| Option | Description | Selected |
|--------|-------------|----------|
| Dengan token vs tanpa token | IsTokenRequired true/false | ✓ |
| Durasi berbeda | 30 menit vs 60 menit | |
| Passing grade berbeda | 70% vs 80% | ✓ |
| Soal jumlah berbeda | 10 soal vs 30 soal, test pagination | ✓ |

**User's choice:** Token, soal jumlah berbeda, passing grade berbeda (multiselect)
**Notes:** Durasi tidak dipilih

### Q3: Import soal — error case?

| Option | Description | Selected |
|--------|-------------|----------|
| Happy path saja | Download template, isi benar, import berhasil | ✓ |
| Happy path + 1-2 error case | Juga coba file kosong/format salah | |

**User's choice:** Happy path saja
**Notes:** —

---

## Data & Prasyarat Test

### Q4: Sumber data soal

| Option | Description | Selected |
|--------|-------------|----------|
| Siapkan dari nol | Buat template soal baru, isi manual | |
| Pakai data existing | Pakai package soal OJT yang sudah ada di server dev | ✓ |
| Claude buatkan test data | Claude generate file Excel terisi soal dummy | |

**User's choice:** Pakai data existing
**Notes:** —

### Q5: Worker accounts

| Option | Description | Selected |
|--------|-------------|----------|
| Rino saja | 1 worker cukup untuk UAT | |
| 2-3 worker | Assign beberapa worker untuk test daftar peserta | ✓ |

**User's choice:** 2-3 worker
**Notes:** User provided 2 additional accounts: mohammad.arsyad@pertamina.com (Pertamina@2026), moch.widyadhana@pertamina.com (Balikpapan@2026)

---

## Kriteria Pass/Fail

### Q6: Cara verifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Visual check di browser | Cukup lihat di UI | |
| Visual + cek database | UI + query database untuk konfirmasi | ✓ |
| Claude analisa kode dulu | Claude baca kode, identifikasi bug, user verifikasi | |

**User's choice:** Visual + cek database
**Notes:** —

### Q7: Potensi bug dari analisa kode

| Option | Description | Selected |
|--------|-------------|----------|
| Catat dulu, user verifikasi | Claude list potensi bug, user cek dulu | ✓ |
| Langsung fix kalau jelas | Fix obvious bugs tanpa verifikasi | |

**User's choice:** Catat dulu, user verifikasi
**Notes:** —

---

## Penanganan Bug

### Q8: Alur penanganan bug

| Option | Description | Selected |
|--------|-------------|----------|
| Catat semua, fix batch di akhir | Jalankan semua test dulu, fix sekaligus | ✓ |
| Fix langsung per temuan | Fix setiap ketemu bug | |
| Catat saja, fix di phase terpisah | Phase ini murni testing & dokumentasi | |

**User's choice:** Catat semua, fix batch di akhir
**Notes:** —

### Q9: Deploy ulang setelah fix?

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, deploy + re-test | Fix → deploy → re-test | |
| Fix di lokal saja | Cukup fix di project lokal | ✓ |

**User's choice:** Fix di lokal saja
**Notes:** Deploy ke server dev adalah tanggung jawab team IT, bukan scope kita

---

## Claude's Discretion

- Urutan langkah-langkah test spesifik
- Query database untuk verifikasi

## Deferred Ideas

None
