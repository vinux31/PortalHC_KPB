# Runbook Test Manual E2E — Assessment

> **Tujuan:** Uji end-to-end pembuatan assessment + ujian + penilaian, lewat UI, dikerjakan manual oleh tester.
> **Tanggal dibuat:** 2026-06-15
> **Environment:** Lokal `http://localhost:5277`
> **Akun:** `admin@pertamina.com` (pembuat **dan** peserta ujian)

Admin yang buat assessment, admin juga yang ikut ujian sebagai **peserta nyata** (bukan impersonation — impersonation read-only, tak bisa submit/dinilai).

Dua assessment dibuat dari nol:

| | **Assessment A — Campur** | **Assessment B — Pilihan Ganda** |
|---|---|---|
| Soal | 1 Single + 1 Multiple + 1 Essay | 3 Single Answer |
| Token | OFF | **ON** (uji Kode Akses) |
| Hasil | PendingGrading → nilai essay manual → baru keluar | Auto-grade langsung Completed |
| Sertifikat | ON | ON |

---

## BAGIAN 0 — Persiapan

### 0.1 Jalankan app lokal
PowerShell di folder project. **WAJIB** matikan Active Directory biar bisa login lokal:

```powershell
$env:Authentication__UseActiveDirectory = "false"
dotnet run
```

Tunggu sampai muncul `Now listening on: http://localhost:5277`.

### 0.2 (Opsional) Snapshot DB sebelum test
Kalau mau bisa balikin DB ke kondisi awal setelah test, backup dulu (lihat `docs/SEED_WORKFLOW.md` untuk command BACKUP/RESTORE SQL Server). Test ini bikin data baru (assessment + jawaban + sertifikat) — restore kalau tak mau nempel.

### 0.3 Login
- Buka `http://localhost:5277/Account/Login`
- Email: `admin@pertamina.com`, password: (dari seed).
- ✅ **Checkpoint:** masuk ke beranda, nama admin muncul di pojok.

---

## BAGIAN 1 — Assessment A (Campur): Buat

### 1.1 Wizard Create (4 step)
Buka `http://localhost:5277/Admin/CreateAssessment`.

**Step 1 — Kategori & Judul**
| Field | Isi |
|---|---|
| Kategori Assessment | pilih salah satu (mis. `OJT` / `Training`) — **jangan** "Assessment Proton" |
| Judul Assessment | `TEST E2E Campur 2026-06-15` (klik **Cek Judul** → pastikan tidak duplikat) |
| Tipe Assessment | `Standard` |

→ klik **Selanjutnya**.

**Step 2 — Peserta**
- Cari `admin` di kotak search.
- ☑ **Centang `admin@pertamina.com`** (ini kunci: admin jadi peserta nyata).
- ✅ Pastikan badge "Peserta Terpilih" = 1.

→ **Selanjutnya**.

**Step 3 — Settings**
| Field | Isi | Alasan |
|---|---|---|
| Tanggal Jadwal | **hari ini** | biar ujian bisa langsung dimulai |
| Waktu Jadwal | `00:01` (jam lampau) | supaya `now ≥ jadwal` → status Open aktif |
| Durasi (Menit) | `30` | cukup, bukan 0 (kalau 0 ditolak) |
| Tanggal Tutup Ujian | **besok** | window masih terbuka |
| Waktu Tutup Ujian | `23:59` | |
| Status | `Open` | langsung tersedia |
| Pass Percentage (%) | `50` | rendah biar lulus → uji sertifikat |
| Wajib token | **OFF** | A tanpa token |
| Acak Soal / Acak Pilihan | biarkan default (ON) | |
| Terbitkan Sertifikat | **ON** | uji generate sertifikat |
| Tanggal Expired Sertifikat | kosong (boleh) | |
| Izinkan Review Jawaban | ON | |

→ **Selanjutnya**.

**Step 4 — Konfirmasi**
- Cek ringkasan: kategori, judul, peserta=admin, Status=Open, Sertifikat=ON.
- klik **Buat Assessment**.
- ✅ **Checkpoint:** redirect ke `/Admin/ManagePackages?assessmentId={ID}` + pesan sukses. **Catat `{ID}` dari URL.**

### 1.2 Buat Paket Soal
Di halaman ManagePackages:
- Isi nama paket (mis. `Paket A`) → klik **buat paket / Tambah Paket**.
- ✅ Paket muncul di daftar.
- klik paket itu → masuk **Kelola Soal** (`/Admin/ManagePackageQuestions?packageId={PID}`).

### 1.3 Tambah 3 Soal (campur)

**Soal 1 — Single Answer**
| Field | Isi |
|---|---|
| Tipe Soal | `Single Answer (1 jawaban benar)` |
| Teks Soal | `Ibukota Indonesia adalah?` |
| Opsi A | `Jakarta` → tandai **BENAR** (radio) |
| Opsi B | `Bandung` |
| Opsi C | `Surabaya` |
| Opsi D | `Medan` |
| Skor | `10` |

→ Simpan. ✅ Soal muncul, badge "Single Answer".

**Soal 2 — Multiple Answer**
| Field | Isi |
|---|---|
| Tipe Soal | `Multiple Answer (≥2 jawaban benar)` |
| Teks Soal | `Manakah yang termasuk bahan bakar minyak?` |
| Opsi A | `Solar` → ☑ benar |
| Opsi B | `Bensin` → ☑ benar |
| Opsi C | `Air` |
| Opsi D | `Pertalite` → ☑ benar |
| Skor | `10` |

→ Simpan. ✅ Badge "Multiple Answer". (Validasi: harus ≥2 benar.)

**Soal 3 — Essay**
| Field | Isi |
|---|---|
| Tipe Soal | `Essay` |
| Teks Soal | `Jelaskan pentingnya keselamatan kerja di kilang.` |
| Rubrik | `Sebut min. 2 poin: APD, prosedur, mitigasi risiko.` (wajib) |
| Max Karakter | `2000` |
| Skor | `10` |

→ Simpan. ✅ Badge "Essay", tidak ada opsi A–D.

- ✅ **Checkpoint Bagian 1:** paket berisi 3 soal (Single, Multiple, Essay).

---

## BAGIAN 2 — Assessment A: Ujian (sebagai peserta)

> Masih login sebagai admin (= peserta). Tak perlu logout.

### 2.1 Mulai ujian
- Buka `http://localhost:5277/CMP/Assessment`.
- Tab **Open** → cari kartu `TEST E2E Campur 2026-06-15`.
- ✅ **Checkpoint:** kartu muncul, tombol **Start Assessment** (tanpa badge token).
- klik **Start Assessment**.
- ✅ Masuk layar ujian, timer jalan (≈30:00), "0/3 answered".

### 2.2 Jawab 3 soal
- Soal 1 (Single): pilih `Jakarta`.
- Soal 2 (Multiple): centang `Solar`, `Bensin`, `Pertalite`.
- Soal 3 (Essay): ketik jawaban, mis. `Pakai APD, ikuti prosedur SOP, mitigasi risiko kebakaran.`
- ✅ Tiap jawab: badge kanan-bawah "saved" / "Tersimpan", panel "Daftar Soal" badge hijau.
- ✅ Counter jadi "3/3 answered".

### 2.3 Review & submit
- klik **Review and Submit** → halaman ringkasan jawaban.
- ✅ Cek jawaban tampil benar (Single huruf+teks, Multiple beberapa huruf, Essay teks penuh).
- klik **Kirim Jawaban**.

### 2.4 Checkpoint hasil
- Redirect ke `/CMP/Results/{ID}`.
- ✅ **Checkpoint:** status **MENUNGGU PENILAIAN** (karena ada essay → PendingGrading). Nilai belum final, sertifikat belum muncul. **Ini benar.**

---

## BAGIAN 3 — Assessment A: Nilai Essay (admin)

### 3.1 Buka halaman penilaian essay
- Buka `http://localhost:5277/Admin/ManageAssessment` → cari assessment A → klik masuk monitoring/detail.
- Cari tombol/menu **Penilaian Essay** (halaman worker-list).
- Atau langsung: `/Admin/EssayGrading?sessionId={ID}` (isi title/category/scheduleDate bila diminta).
- ✅ Daftar peserta dengan essay belum dinilai (admin, status "Belum Dinilai").

### 3.2 Beri skor
- Buka essay admin → soal 3 muncul + rubrik + jawaban.
- Isi skor (mis. `10` dari max 10) → **Simpan Skor**.
- ✅ Status soal jadi "Dinilai", pendingCount berkurang ke 0.

### 3.3 Finalize
- klik **Finalize / Selesaikan Penilaian**.
- ✅ Status sesi → **Completed**, nilai final dihitung.

### 3.4 Checkpoint sertifikat
- Buka lagi `/CMP/Results/{ID}`.
- ✅ **Checkpoint:** status **LULUS**, "Nilai Anda" terisi, **Nomor Sertifikat** muncul (format `KPB/.../.../2026`).

---

## BAGIAN 4 — Assessment B (Pilihan Ganda): Buat

Ulangi **Bagian 1**, beda:

**Step 1:** Judul `TEST E2E Pilihan Ganda 2026-06-15` (Cek Judul).
**Step 2:** centang `admin@pertamina.com`.
**Step 3 (Settings):** sama seperti A **TAPI**:
- **Wajib token = ON** → klik **Generate** → **catat token 6-karakter** (mis. `A3K9PZ`).
- Sertifikat = ON, Pass% = `50`.

**Paket & Soal:** buat `Paket B`, isi **3 soal Single Answer** (semua punya 1 jawaban benar). Contoh:
1. `2 + 2 = ?` → A `4`(benar) B `3` C `5` D `22`
2. `Warna langit cerah?` → A `Merah` B `Biru`(benar) C `Hijau` D `Hitam`
3. `Hari setelah Senin?` → A `Minggu` B `Rabu` C `Selasa`(benar) D `Jumat`

- ✅ **Checkpoint:** assessment B dibuat, paket 3 soal Single, **token tercatat**.

---

## BAGIAN 5 — Assessment B: Ujian + Token

### 5.1 Mulai dengan token
- `/CMP/Assessment` → tab Open → kartu B punya badge **Token Required** (ikon gembok).
- klik **Mulai / Start** → muncul modal **Security Check**.
- ✅ **Checkpoint:** modal minta token 6-digit.
- Masukkan token yang dicatat (case-insensitif — huruf kecil pun di-uppercase otomatis).
- klik **Verify & Launch**.
- ✅ Masuk ujian. (Coba token salah dulu kalau mau → harus muncul "Token tidak valid. Silakan periksa dan coba lagi.")

### 5.2 Jawab + submit
- Jawab 3 soal Single (pilih jawaban benar semua biar lulus).
- **Review and Submit** → **Kirim Jawaban**.

### 5.3 Checkpoint auto-grade
- Redirect `/CMP/Results/{ID2}`.
- ✅ **Checkpoint:** **langsung Completed** (tak ada PendingGrading karena tanpa essay), status **LULUS**, nilai keluar otomatis, **Nomor Sertifikat** langsung muncul — **tanpa** penilaian manual.

---

## BAGIAN 6 — Verifikasi Akhir & Cleanup

### 6.1 Monitoring
- `/Admin/AssessmentMonitoringDetail?title=...&category=...&scheduleDate=...` (via ManageAssessment).
- ✅ Kedua peserta (admin di A & B) status **Completed**, nilai tampil.

### 6.2 Ringkasan checkpoint
| # | Yang diuji | Lolos kalau |
|---|---|---|
| 1 | Buat assessment 4-step | redirect ke ManagePackages |
| 2 | 3 tipe soal (Single/Multiple/Essay) | badge benar, validasi MA ≥2 |
| 3 | Admin = peserta nyata | kartu muncul di /CMP/Assessment, bisa Start |
| 4 | Submit + autosave | jawaban tersimpan, summary benar |
| 5 | Essay → PendingGrading | hasil "MENUNGGU PENILAIAN" |
| 6 | Nilai essay + finalize | status Completed, LULUS, sertifikat |
| 7 | Token (Assessment B) | salah=ditolak, benar=masuk |
| 8 | Auto-grade pilihan ganda | langsung Completed + sertifikat |

### 6.3 Cleanup (kalau snapshot tadi)
Restore DB lokal ke snapshot 0.2 (lihat `docs/SEED_WORKFLOW.md`).

---

## Troubleshooting

| Gejala | Sebab / Fix |
|---|---|
| Login gagal 500 / error | env `Authentication__UseActiveDirectory=false` belum diset sebelum `dotnet run`. Stop, set, run ulang. (Lihat juga `reference_local_e2e_sql_env_fix`.) |
| Admin tak muncul di daftar Peserta | akun harus `IsActive=true`. Cek tabel User. |
| Kartu assessment tak muncul di /CMP/Assessment | Status bukan `Open`, atau Jadwal di masa depan. Pastikan Tanggal Jadwal=hari ini, Waktu=jam lampau, Status=Open. |
| "Ujian sudah ditutup" | `Tanggal Tutup Ujian` < sekarang. Set ke besok. |
| "Durasi ujian belum diatur" | Durasi = 0. Set ≥1. |
| "Ujian belum siap — belum ada soal" | paket kosong / belum ada soal. Tambah soal dulu (Bagian 1.3). |
| Tombol Penilaian Essay tak ada | hanya muncul kalau ada soal Essay (HasManualGrading=true). Pastikan A punya essay. |
| Token ditolak walau benar | token disimpan UPPERCASE; ketik ulang, spasi diabaikan otomatis. |
| Tidak LULUS | jawaban salah atau Pass% terlalu tinggi. Jawab benar / set Pass%=50. |

---

*Catatan: Test ini di lokal sesuai `CLAUDE.md` (jangan edit DB Dev/Prod langsung). Kalau mau diuji di Dev, koordinasi Team IT — jangan kotori data Dev.*
