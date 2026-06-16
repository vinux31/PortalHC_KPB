# Runbook Gladi-Bersih — Ujian Lisensor (Tipe Standard, Manual)

> **Tujuan:** pastikan ujian REAL lisensor (tipe **Standard**, ≤30 peserta, 4 jenis soal **termasuk bergambar**, full lifecycle + monitoring) berjalan lancar. Dikerjakan **manual oleh tester di browser**.
> **Dibuat:** 2026-06-15 · **Sumber temuan:** `.planning/notes/2026-06-15-readiness-ujian-lisensor.md`
> **Base mekanik wizard** (kalau butuh detail langkah klik): lihat `docs/test-manual-assessment-e2e.md`.

Runbook ini **bukan cuma "apakah jalan"**, tapi sekaligus **menguji 6 temuan** hasil audit kode (F-01, F-02, F-04, F-09 + verifikasi MA scoring & passing 70%). Tiap checkpoint ditandai **[VERIF F-xx]**.

---

## ⚠️ PENTING — 2 environment, 2 tujuan beda

| Akses | URL | Untuk apa |
|---|---|---|
| **Lokal bare** | `http://localhost:5277/...` | Uji **fungsi** (lifecycle, scoring, grading, cert). Gambar **tampil normal** di sini. |
| **Lokal ber-prefix** | `http://localhost:5277/KPB-PortalHC/...` | **Simulasi Dev** untuk **[VERIF F-09]** — gambar bisa **broken** (lihat Bagian 7). |
| **Dev (real)** | `http://10.55.3.3/KPB-PortalHC/...` | UAT final gambar sebelum hari-H (butuh koordinasi IT + akun Dev). |

> F-09 = bug gambar yang **HANYA muncul saat URL ber-prefix `/KPB-PortalHC`** (kondisi Dev). Lokal bare tidak akan menampakkannya. **Wajib** diuji minimal via lokal ber-prefix (Bagian 7).

---

## BAGIAN 0 — Persiapan

### 0.1 Jalankan app lokal
PowerShell di folder project. **WAJIB** matikan Active Directory:
```powershell
$env:Authentication__UseActiveDirectory = "false"
dotnet run
```
Tunggu `Now listening on: http://localhost:5277`.

### 0.2 Snapshot DB (disarankan)
Test ini bikin data baru (assessment + jawaban + sertifikat). Backup dulu biar bisa di-restore (`docs/SEED_WORKFLOW.md`).

### 0.3 Siapkan 2 file gambar
Siapkan 2 file `.jpg`/`.png` kecil di Desktop (mis. `soal1.png`, `opsi-a.png`) — untuk soal & opsi bergambar.

### 0.4 Login
`http://localhost:5277/Account/Login` → `admin@pertamina.com` (password seed).
✅ Masuk beranda, nama admin di pojok. Admin = pembuat **dan** peserta nyata (bukan impersonation).

---

## BAGIAN 1 — Buat Assessment Lisensor Standard (+ soal bergambar)

### 1.1 Wizard Create
`http://localhost:5277/Admin/CreateAssessment` (detail per-step: base runbook Bagian 1.1).

**Step 1:** Kategori = **Training** (atau kategori lisensor kalian); Judul = `GLADI LISENSOR 2026-06-15` (klik **Cek Judul**, pastikan tak duplikat); Tipe = **Standard**.
**Step 2:** centang `admin@pertamina.com` (Peserta Terpilih = 1).
**Step 3 — Settings (nilai REAL lisensor):**
| Field | Isi | Alasan |
|---|---|---|
| Tanggal/Waktu Jadwal | hari ini / `00:01` | status Open aktif |
| Durasi | `30` | |
| Tutup Ujian | besok / `23:59` | window terbuka |
| Status | `Open` | |
| **Pass Percentage** | **`70`** | **[VERIF passing]** = nilai lisensor sebenarnya |
| Wajib token | OFF (boleh ON kalau ujian real pakai token) | |
| Acak Soal / Pilihan | default ON | |
| **Terbitkan Sertifikat** | **ON** | uji cert lisensor |
| Izinkan Review Jawaban | **ON** | perlu untuk lihat Benar/Salah per soal |
**Step 4:** Buat Assessment → catat `{ID}` dari URL ManagePackages.

### 1.2 Buat paket + 4 soal (4 jenis, ada gambar)
Buat `Paket Lisensor` → masuk Kelola Soal.

**Soal 1 — Single Answer + GAMBAR SOAL**
- Tipe `Single Answer`; Teks `Komponen ini berfungsi sebagai?`; **upload gambar soal** (`soal1.png`).
- Opsi A `Pompa` **BENAR** · B `Katup` · C `Tangki` · D `Pipa`. Skor `10`.
- ✅ preview thumbnail gambar muncul di form sebelum simpan.

**Soal 2 — Multiple Answer (benar = A, C, D)** — kunci uji scoring
- Tipe `Multiple Answer`; Teks `Manakah produk kilang? (pilih semua yang benar)`.
- Opsi A `Bensin` ☑ · B `Air` (salah) · C `Solar` ☑ · D `Avtur` ☑. Skor `10`.
- → benar = **A, C, D** (3 dari 4). Validasi ≥2 benar.

**Soal 3 — Single Answer + OPSI BERGAMBAR**
- Tipe `Single Answer`; Teks `Pilih simbol APD yang benar`.
- **Upload gambar di opsi A** (`opsi-a.png`), tandai A **BENAR**; B/C/D teks biasa. Skor `10`.

**Soal 4 — Essay**
- Tipe `Essay`; Teks `Jelaskan prosedur keselamatan saat operasi kilang.`; Rubrik `Min 2 poin: APD + SOP`. Max `2000`. Skor `10`.

✅ **Checkpoint:** paket = 4 soal (Single+gambar, Multiple A/C/D, Single+opsi-gambar, Essay). maxScore = 40.

---

## BAGIAN 2 — Ujian sebagai peserta (run UTAMA: jawab BENAR semua)

`http://localhost:5277/CMP/Assessment` → tab Open → kartu `GLADI LISENSOR` → **Start**.

### 2.1 [VERIF F-09 lokal-bare] gambar tampil
- ✅ Soal 1: **gambar soal tampil** (tidak broken-icon). Klik → lightbox zoom.
- ✅ Soal 3: **gambar di opsi A tampil**. Klik gambar → zoom, **TIDAK** ikut men-toggle radio.
- (Di lokal bare ini harus OK. Uji kondisi Dev di Bagian 7.)

### 2.2 [VERIF F-01] cek instruksi MA
- Soal 2 (Multiple): ✅ ada teks **"Pilih semua yang benar"**.
- ⚠️ **[VERIF F-01]** Perhatikan: **TIDAK ada peringatan** "jawaban sebagian = 0 poin". Catat: apakah ini perlu ditambah/ disampaikan lisan ke peserta? (temuan F-01).

### 2.3 Jawab BENAR semua
- S1 `Pompa` · S2 centang **Bensin + Solar + Avtur** (A,C,D persis) · S3 opsi A (gambar) · S4 essay isi `Pakai APD lengkap, ikuti SOP, mitigasi risiko.`
- ✅ tiap jawab badge "Tersimpan"; counter 4/4.
- **Review & Submit** → cek ringkasan → **Kirim Jawaban**.
- ✅ **Checkpoint:** `/CMP/Results/{ID}` status **MENUNGGU PENILAIAN** (ada essay → PendingGrading). Belum ada nilai/cert. **Benar.**

---

## BAGIAN 3 — Nilai Essay + Finalize

`http://localhost:5277/Admin/EssayGrading?sessionId={sessionId}` (atau via ManageAssessment → monitoring → Penilaian Essay).
- ✅ admin muncul di worker-list essay, status "Belum Dinilai".
- Buka essay → beri skor **`10`** (dari 10) → **Simpan Skor**. ✅ status "Dinilai", pending → 0, tombol **Selesaikan Penilaian** muncul.
- Klik **Selesaikan Penilaian**. ✅ sesi → **Completed**, nilai dihitung.

### 3.1 [VERIF passing + cert] hasil run utama
- Buka `/CMP/Results/{ID}`.
- ✅ **LULUS** (skor 40/40 = 100% ≥ 70). "Nilai Anda" terisi. **Nomor Sertifikat** `KPB/.../.../2026` muncul.
- ✅ **[VERIF MA benar]** di review: Soal 2 (MA, jawab A,C,D persis) ditandai **Benar (hijau)**, masuk hitungan "(X/Y benar)".

---

## BAGIAN 4 — [VERIF MA all-or-nothing] run PARSIAL

> Tujuan: buktikan jawaban MA **sebagian = 0**. Butuh sesi kedua.
> **Cara termudah:** buat assessment ke-2 mini (`GLADI MA PARSIAL`, Standard, Pass% 70, sertifikat ON, **1 soal saja = Soal 2 MA benar A,C,D** skor 10) — ulang Bagian 1 ringkas. Lalu ujian, jawab MA **HANYA Bensin + Solar (A,C)** — sengaja kurang D.

- Submit → `/CMP/Results/{ID2}`.
- ✅ **[VERIF F-MA]** Soal MA ditandai **Salah / tidak masuk "benar"**, kontribusi **0 poin** → total **0/10 = 0%** → **TIDAK LULUS**, **tanpa** sertifikat.
- Ulangi sekali lagi jawab **A saja** → tetap **0**. Hanya **persis A,C,D** yang dapat 10.
- ➡️ Ini mengonfirmasi keputusan all-or-nothing bekerja benar. (Kalau lisensor mau partial credit → itu GAP requirement, bukan bug.)

---

## BAGIAN 5 — [VERIF F-04] run ESSAY KOSONG (dead-end finalize)

> Tujuan: cek bug F-04 — worker mengosongkan essay → finalize macet.
> Buat assessment ke-3 mini (`GLADI ESSAY KOSONG`, Standard, sertifikat ON, **1 Single + 1 Essay**), ujian, jawab Single saja, **biarkan essay KOSONG** (jangan ketik apa pun), Submit.

- Buka `/Admin/EssayGrading?sessionId={ID3}`.
- ⚠️ **[VERIF F-04]** Amati:
  - Apakah tombol **"Selesaikan Penilaian" TIDAK muncul** (karena pending dihitung > 0)?
  - Apakah essay kosong **tak bisa dinilai** (klik simpan → "Jawaban tidak ditemukan")?
  - Di monitoring detail, apakah peserta malah tampak **"Siap difinalisasi"**?
- **Jika ya ketiganya → F-04 CONFIRMED** (sesi terjebak tak bisa difinalisasi → peserta tak pernah dapat hasil/sertifikat). **Catat sebagai blocker bila ujian real memungkinkan essay dikosongkan.**
- (Workaround sementara: minta peserta isi essay walau singkat. Fix permanen = keputusan terpisah.)

---

## BAGIAN 6 — [VERIF F-02] konsistensi export Excel vs Web/PDF

> Tujuan: cek label Benar/Salah essay konsisten antar surface saat skor **parsial**.
> Pakai run mana pun yang punya essay, tapi beri **skor parsial** (mis. essay skor `3` dari 10) lalu finalize.

- Web `/CMP/Results/{ID}` review: essay skor 3 → ✅ ditandai **Benar** (karena >0).
- Export **PDF** per-peserta (tombol di Results/Admin): essay → ✅ **Benar** (konsisten).
- Export **Excel** hasil assessment (`/Admin/...ExportAssessmentResults`, sheet **"Detail Per Soal"**): kolom "Soal N Benar?".
- ⚠️ **[VERIF F-02]** Bandingkan: kalau Excel tandai essay **"Salah"** padahal web/PDF **"Benar"** → **F-02 CONFIRMED** (drift label, skor parsial `1..4`). Bukan salah hitung nilai — hanya label di sheet sekunder.

---

## BAGIAN 7 — [VERIF F-09] GAMBAR di kondisi Dev (prefix `/KPB-PortalHC`)

> **Headline finding.** Gambar pakai path leading-slash → di bawah prefix `/KPB-PortalHC` lari ke host-root → 404 (broken).

**Cara A — lokal simulasi Dev (tanpa akses Dev):**
1. App lokal masih jalan. Buka ujian/Results yang ada **soal bergambar** TAPI lewat URL **ber-prefix**:
   `http://localhost:5277/KPB-PortalHC/CMP/Assessment` → Start → layar soal bergambar.
2. ⚠️ **[VERIF F-09]** Amati Soal 1 & Soal 3:
   - **Gambar broken / icon rusak / kosong?** → **F-09 CONFIRMED** (reproduksi kondisi Dev).
   - Buka DevTools (F12) → Network → cari request gambar `/uploads/...` → **404**? URL tanpa `/KPB-PortalHC`? → konfirmasi.
   - Bandingkan: buka URL **bare** `http://localhost:5277/CMP/...` → gambar **tampil** (kontras membuktikan akar = PathBase).

**Cara B — Dev nyata (UAT final, koordinasi IT):**
3. `http://10.55.3.3/KPB-PortalHC/...` → layar StartExam bergambar → ✅/❌ gambar tampil.

➡️ Kalau F-09 confirmed: **soal bergambar tak akan tampil di ujian real Dev** → **blocker**. Perlu fix (`_QuestionImage` src PathBase-aware) + re-deploy IT sebelum hari-H. (Fix = keputusan terpisah, di luar runbook ini.)

---

## BAGIAN 8 — Monitoring real-time + Ringkasan

### 8.1 Monitoring saat ujian
- Buka 2 tab: Tab A `/Admin/AssessmentMonitoringDetail?title=...&category=...&scheduleDate=...`; Tab B ujian peserta.
- Jawab soal di Tab B → ✅ progress peserta di Tab A **update live** (tanpa refresh) — SignalR push.
- (Catatan F-13: setelah finalize essay, badge di tab monitoring lain bisa stale s/d refresh — dampak rendah 1-operator.)

### 8.2 Ringkasan checkpoint → mapping temuan
| # | Checkpoint | Lolos kalau | Temuan |
|---|---|---|---|
| 1 | Buat assessment Standard + 4 jenis soal + gambar | paket 4 soal, gambar ter-upload | — |
| 2 | Gambar tampil (lokal bare) | soal & opsi bergambar render + zoom | — |
| 3 | Instruksi MA | ada "Pilih semua yang benar"; **tak ada** warn "sebagian=0" | **F-01** |
| 4 | Essay → PendingGrading → finalize | hasil "Menunggu Penilaian" → Completed | — |
| 5 | Passing 70% + sertifikat | run benar semua = 100% LULUS + No. Sertifikat | passing |
| 6 | MA all-or-nothing | jawab A,C (parsial) → 0; hanya A,C,D = penuh | **F-MA** |
| 7 | Essay kosong | finalize macet / tak bisa dinilai | **F-04** |
| 8 | Export Excel vs Web/PDF | label essay parsial konsisten? (kalau beda = bug) | **F-02** |
| 9 | Gambar via prefix `/KPB-PortalHC` | broken? (kalau ya = bug Dev) | **F-09** |
| 10 | Monitoring live | progress update tanpa refresh | — |

### 8.3 Cleanup
Restore DB lokal ke snapshot 0.2 (`docs/SEED_WORKFLOW.md`). Tandai selesai.

---

## Troubleshooting
Lihat tabel di `docs/test-manual-assessment-e2e.md` (login 500 = AD belum off; kartu tak muncul = status/jadwal; token; dll). Tambahan:

| Gejala | Sebab / Fix |
|---|---|
| Upload gambar gagal | cek ukuran/format file; folder `wwwroot/uploads` writable. |
| Gambar tak tampil di lokal **bare** | beda dari F-09 — cek path tersimpan di DB & file ada di `wwwroot/uploads/questions/...`. |
| Gambar broken hanya di prefix `/KPB-PortalHC` | **itu F-09** (expected sampai di-fix). |
| Tombol Selesaikan Penilaian tak muncul (essay terisi) | semua essay harus "Dinilai" dulu. Kalau essay KOSONG → itu F-04. |

---

*Test utama di lokal sesuai `CLAUDE.md` (jangan edit DB Dev/Prod langsung). UAT gambar di Dev = koordinasi Team IT. Temuan F-xx → keputusan fix terpisah (report-first).*
