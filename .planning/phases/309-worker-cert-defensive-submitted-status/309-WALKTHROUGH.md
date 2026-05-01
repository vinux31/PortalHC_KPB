# Phase 309 — UAT Walkthrough Guide

**Phase:** 309-worker-cert-defensive-submitted-status
**Tujuan:** Step-by-step manual UAT untuk 9 item human verification dari `309-VERIFICATION.md`. Format ini didesain agar reviewer bisa langsung copy-paste perintah, lihat ekspektasi, lalu jawab `pass` atau deskripsi issue di session `/gsd-verify-work 309`.

---

## Pre-conditions (lakukan SEKALI sebelum Test 1)

### A. Build & jalankan app

```powershell
# Pastikan tidak ada HcPortal.exe ter-lock
Get-Process HcPortal -ErrorAction SilentlyContinue | Stop-Process -Force
```

```bash
dotnet build
# Expect: Build succeeded. 0 Error(s), 92 Warning(s)
```

```bash
# Jalankan di background
dotnet run --no-build --launch-profile HcPortal
# Tunggu sampai log "Now listening on: http://localhost:5277"
```

### B. Toggle login lokal (opsional, kalau perlu password 123456)

Edit `appsettings.json`: `"UseActiveDirectory": false`. **Revert setelah UAT selesai (JANGAN commit).**

### C. Seed data DB

| Variabel | Nilai |
|----------|-------|
| Connection | `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev;...` |
| Session uji utama | `Id = 75` (state asli: Status=`Completed`, IsPassed=`true`, UserId=`4a624dbc-3241-4207-92d7-d1d5784c7137`) |
| Session ber-Essay (untuk Test 1, 4) | Cari via SQL: `SELECT TOP 1 s.Id FROM AssessmentSessions s JOIN Assessments a ON s.AssessmentId=a.Id JOIN AssessmentQuestions q ON q.AssessmentId=a.Id WHERE q.Type='Essay' AND s.Status='Open'` |

### D. Akun login

| Role | Akun |
|------|------|
| Worker (NIP 75 owner) | login dengan UserId di Session 75 |
| HC | role HC untuk finalize essay (Test 5) |
| Admin | untuk DB edit / SQL ops |

---

## Test 1 — Worker submit assessment ber-essay → DB Status='Menunggu Penilaian'

**Pre-step:**
1. Login sebagai worker yang punya assessment ber-essay (Status=`Open`, ada minimal 1 soal `Type='Essay'`).
2. Jika tidak ada session siap-submit, gunakan SQL UPDATE simulasi (lihat **Pendekatan Cepat** di bawah) — tetap valid untuk verifikasi GradingService refactor.

**Action — Pendekatan Lengkap (real worker flow):**
1. Worker buka assessment di `/CMP/Take/{sessionId}`.
2. Jawab semua soal MC + MA, isi Essay text apa saja.
3. Klik **Submit**.

**Action — Pendekatan Cepat (DB UPDATE simulasi, sudah dipakai di sesi sebelumnya):**
```sql
-- Snapshot dulu untuk revert
SELECT Id, Status, IsPassed, HasManualGrading, Progress, Score
FROM AssessmentSessions WHERE Id = 75;

-- Simulasi essay submit (yang dilakukan GradingService)
UPDATE AssessmentSessions
SET Status = 'Menunggu Penilaian',
    IsPassed = NULL,
    HasManualGrading = 1,
    Progress = 100,
    Score = 66,
    CompletedAt = GETUTCDATE()
WHERE Id = 75;
```

**Verify (SQL query):**
```sql
SELECT Id, Status, IsPassed, HasManualGrading, Progress, Score
FROM AssessmentSessions WHERE Id = 75;
```

**Expect:**
- `Status = 'Menunggu Penilaian'`
- `IsPassed = NULL`
- `HasManualGrading = 1`
- `Progress = 100`
- `Score` ≠ NULL (interim percentage MC+MA)

**Catatan:** Plan 309-03 hanya refactor literal → constant; SQL hasilnya tetap identik dengan pre-refactor. Sudah PASS via DB UPDATE simulation di Plan 309-02 sesi sebelumnya.

---

## Test 2 — Worker klik 'Lihat Sertifikat' pada session pending → banner BIRU info

**Pre-step:** Session 75 dalam state `Status='Menunggu Penilaian'` (dari Test 1).

**Action:**
1. Login sebagai worker pemilik Session 75.
2. Browse ke `/CMP/Certificate/75` (atau klik tombol **"Lihat Sertifikat"** dari halaman list / Results).

**Expect — PASS jika SEMUA berikut benar:**
- Browser di-redirect ke `/CMP/Results/75`.
- Banner **biru muda** (`alert-info`) muncul dengan teks:
  > **Info:** Sertifikat akan tersedia setelah penilaian essay selesai.
- **TIDAK ada** banner merah (`alert-danger`) berisi `"Error: Assessment belum selesai."` atau `"Assessment not completed yet."`.

**FAIL jika:** muncul banner merah, atau muncul HTTP 500, atau redirect ke halaman selain `/CMP/Results/75`.

---

## Test 3 — Worker hit /CMP/CertificatePdf/{id} pada session pending → redirect Info, no PDF

**Pre-step:** Session 75 masih `Status='Menunggu Penilaian'`.

**Action:**
1. Sebagai worker (login Session 75 owner), navigasi langsung ke `/CMP/CertificatePdf/75` (paste URL di address bar).

**Expect — PASS jika SEMUA berikut benar:**
- Browser di-redirect ke `/CMP/Results/75` (HTTP 302 → 200).
- Banner biru `alert-info` muncul dengan pesan **"Sertifikat akan tersedia setelah penilaian essay selesai."**.
- **TIDAK ada** download PDF (browser tidak pop dialog Save / file unduh).
- **TIDAK ada** HTTP 500 / Error page.

**Tip DevTools:** Network tab → request `/CMP/CertificatePdf/75` harus response status `302 Found` ke `/CMP/Results/75`.

---

## Test 4 — Worker view Results pending mode + Essay items D-08

**Pre-step:** Session 75 masih `Status='Menunggu Penilaian'`. Idealnya gunakan session yang punya soal Essay (kalau Session 75 tidak ada Essay, cari session lain dan UPDATE statusnya).

**Action:**
1. Sebagai worker, navigasi ke `/CMP/Results/{sessionId}` langsung (tanpa redirect dari Test 2/3).

**Expect — checklist visual (SEMUA harus benar):**
- [ ] Banner atas **`alert-info`** dengan label **"Hasil sementara"** (warna biru).
- [ ] Card-header berwarna **abu-abu** (`bg-secondary`), bukan hijau / merah.
- [ ] Badge status besar bertuliskan **"MENUNGGU PENILAIAN"** dengan ikon hourglass (⏳).
- [ ] Tombol **"Lihat Sertifikat"** **HILANG** (tidak muncul di card / tidak ada button cert).
- [ ] Section **"Tinjauan Jawaban"** masih muncul.
- [ ] Item **Essay** di Tinjauan Jawaban **TETAP MUNCUL** (tidak hilang) dengan badge **abu-abu** bertuliskan **"Menunggu Penilaian"** (ini D-08 lock — paling penting).
- [ ] Item **MC / MA** di Tinjauan Jawaban: badge **HIJAU "Benar"** atau **MERAH "Salah"** (rendering normal).
- [ ] Skor sementara muncul (mis. 66%) di area motivasi.

**FAIL jika:** tombol cert masih muncul, Essay items hilang, badge Essay hijau/merah (bukan abu-abu), atau warna card-header bukan abu-abu.

---

## Test 5 — HC finalize essay → worker view Certificate normal (regression-free Completed flow)

**Pre-step:** Session 75 dalam `Status='Menunggu Penilaian'` (dari Test 1-4).

**Action — Pendekatan Lengkap (real HC flow):**
1. Login sebagai HC.
2. Buka `/AssessmentAdmin/EssayGrading` (atau menu HC → Penilaian Essay).
3. Pilih Session 75 → input nilai Essay → klik **Finalize**.

**Action — Pendekatan Cepat (SQL UPDATE):**
```sql
UPDATE AssessmentSessions
SET Status = 'Completed',
    IsPassed = 1,
    Score = 80
WHERE Id = 75;
```

**Verify:**
1. Logout HC, login worker pemilik Session 75.
2. Browse ke `/CMP/Certificate/75`.

**Expect — checklist (SEMUA harus benar):**
- [ ] Halaman sertifikat render NORMAL (tidak ada redirect ke Results).
- [ ] Recipient name **"Rino"** (atau nama worker pemilik) muncul di body sertifikat.
- [ ] Card-header **hijau** (`bg-success`) di Results jika navigasi ke Results.
- [ ] Item Essay di Tinjauan Jawaban (kalau ada) render dengan badge **HIJAU "Benar"** / **MERAH "Salah"** normal.
- [ ] **TIDAK ada** banner Info biru, **TIDAK ada** badge "MENUNGGU PENILAIAN".

**Cleanup setelah Test 5:** Revert Session 75 ke state asli kalau perlu:
```sql
UPDATE AssessmentSessions
SET Status='Completed', IsPassed=1, HasManualGrading=0, Progress=100, Score=66
WHERE Id=75;
```

---

## Test 6 — Exotic Category null → fallback signatory 'HC Manager'

**Pre-step:** Pilih session yang Status='Completed' (mis. Session 75 setelah Test 5 cleanup). Snapshot CategoryId awalnya.

**Action:**
1. Login Admin / SQL Admin.
2. Snapshot dulu:
   ```sql
   SELECT a.CategoryId
   FROM AssessmentSessions s JOIN Assessments a ON s.AssessmentId=a.Id
   WHERE s.Id = 75;
   -- Catat hasilnya, mis. CategoryId = 3
   ```
3. Set jadi NULL:
   ```sql
   UPDATE Assessments
   SET CategoryId = NULL
   WHERE Id = (SELECT AssessmentId FROM AssessmentSessions WHERE Id = 75);
   ```
4. Login sebagai worker pemilik Session 75.
5. Browse ke `/CMP/Certificate/75`.

**Expect:**
- [ ] Sertifikat render NORMAL (TIDAK ada HTTP 500 / Error page).
- [ ] Footer signatory: **Position = "HC Manager"**.
- [ ] Footer signatory: **FullName = ""** (kosong) — TIDAK crash.

**Cleanup WAJIB (jangan persistent state):**
```sql
UPDATE Assessments
SET CategoryId = 3  -- ganti dengan nilai snapshot tadi
WHERE Id = (SELECT AssessmentId FROM AssessmentSessions WHERE Id = 75);
```

---

## Test 7 — Exotic User=null → recipient '(Nama tidak tersedia)'

**Pre-step:** Plan 309-01 sebelumnya menemukan FK NOT NULL constraint blocks `UPDATE UserId=NULL`. Pakai **Opsi B (code-side mock)** seperti sesi sebelumnya.

**Action — Opsi B (sudah didemonstrasikan di Plan 309-01 UAT):**
1. Edit `Controllers/CMPController.cs` di action `Certificate(int id)`, sebelum baris `return View(assessment);`, tambah TEMP:
   ```csharp
   assessment.User = null;  // TEMP UAT Test 7 — REVERT setelah test
   ```
2. Rebuild + restart app:
   ```powershell
   Get-Process HcPortal | Stop-Process -Force
   dotnet build
   dotnet run --no-build --launch-profile HcPortal
   ```
3. Login worker, browse ke `/CMP/Certificate/75`.

**Expect:**
- [ ] Sertifikat render NORMAL (TIDAK ada HTTP 500).
- [ ] Recipient text di body sertifikat: **"(Nama tidak tersedia)"** (literal string).
- [ ] Tidak ada exception di log app.

**Cleanup WAJIB:**
1. Hapus baris `assessment.User = null;` dari controller.
2. Verify clean: `git diff Controllers/CMPController.cs` harus kosong.
3. Rebuild + restart.

---

## Test 8 — Visual styling TempData['Info'] alert-info di _Layout

**Pre-step:** Trigger banner Info via Test 2 atau Test 3 (set Session ke `Menunggu Penilaian` lalu hit `/CMP/Certificate/75`).

**Action:**
1. Setelah banner muncul di `/CMP/Results/75`, **buka DevTools** (F12).
2. Inspect element banner Info.

**Expect — checklist DevTools:**
- [ ] Class element: `alert alert-info alert-dismissible fade show`.
- [ ] Background warna: **biru muda** (Bootstrap `--bs-info-bg-subtle` atau setara).
- [ ] Ikon: `<i class="bi bi-info-circle-fill">` (info circle).
- [ ] Prefix bold: **`<strong>Info:</strong>`**.
- [ ] Tombol close `× ` (`btn-close`) ada dan **functional** (klik → banner hilang dengan fade animation).

**A11y check (opsional):**
- Lighthouse / aXe: tidak ada contrast violation untuk teks di banner.

---

## Test 9 — Post-deploy log monitoring (DEFERRED ke ops)

**Pre-step:** Phase 309 sudah deployed ke production / staging.

**Action (post-deploy task untuk ops):**
1. Buka log sink (Application Insights / Serilog file log / cloud logging).
2. Filter query: pesan `"Certificate view failed for session"`.
3. Selama window observasi (1-7 hari):
   - Catat session ID yang trigger exception.
   - Identifikasi root cause exotic data (User null? Category null? FK rusak?).

**Expect:**
- Log entries muncul dengan structured field `{Id}` ter-resolve ke session ID konkret.
- Severity `Error` (bukan unhandled).

**Status:** ⏳ **Deferred ke ops post-deploy** — SC #7 WCRT-01 explicit defer. Boleh di-mark `skipped: deferred-to-ops` saat verifikasi sekarang.

---

## Cara Menjawab di Session `/gsd-verify-work 309`

Di prompt CHECKPOINT yang muncul setelah ini, ketik:

| Hasil | Ketik |
|-------|-------|
| Test pass | `pass` (atau `yes`, `ok`, kosong) |
| Test fail | deskripsikan apa yang terjadi (mis. "banner masih merah") — severity di-infer otomatis |
| Tidak bisa test sekarang | `skip: <alasan>` |
| Diblock (mis. Session ber-Essay belum tersedia) | `blocked: <alasan>` (mis. "blocked: butuh session ber-essay") |
| Test 9 (post-deploy) | `skip: deferred to ops post-deploy` |

---

## Quick Reference — Path & Command

| Item | Value |
|------|-------|
| Project root | `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Desktop\PortalHC_KPB` |
| App URL base | `http://localhost:5277` |
| DB connection | `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev` |
| Session uji utama | `75` |
| Files affected (refresher) | `Controllers/CMPController.cs`, `Models/AssessmentConstants.cs`, `Models/AssessmentResultsViewModel.cs`, `Services/GradingService.cs`, `Views/CMP/Certificate.cshtml`, `Views/CMP/Results.cshtml`, `Views/Shared/_Layout.cshtml` |
| UAT tracker | `.planning/phases/309-worker-cert-defensive-submitted-status/309-HUMAN-UAT.md` |
| Verifier report | `.planning/phases/309-worker-cert-defensive-submitted-status/309-VERIFICATION.md` |
| Code review | `.planning/phases/309-worker-cert-defensive-submitted-status/309-REVIEW.md` |

---

## Riwayat Validation Pra-Walkthrough (untuk konteks reviewer)

Sebagian besar test sudah di-PASS via Playwright UAT di sesi eksekusi sebelumnya (per `.planning/phases/309-worker-cert-defensive-submitted-status/.continue-here.md`):

| Test | Status sesi sebelumnya |
|------|------------------------|
| 1 | ✓ DB UPDATE simulation (Plan 309-02 Step 1) |
| 2 | ✓ Playwright (Plan 309-02 Step 2) |
| 3 | ✓ Playwright (Plan 309-02 Step 3) |
| 4 | ✓ Playwright (Plan 309-02 Step 4) — **kecuali D-08 Essay items visual** (Session 75 tidak punya Essay; deferred ke session ber-Essay) |
| 5 | ✓ Playwright (Plan 309-02 Step 5 — HC finalize regression) |
| 6 | ✓ Playwright (Plan 309-01 Step 3 — Category=null) |
| 7 | ✓ Playwright + code-side mock Opsi B (Plan 309-01 Step 2) |
| 8 | ✓ Playwright (Plan 309-02 Step 6 — TempData Info Layout) |
| 9 | ⏳ Deferred ke ops post-deploy |

Walkthrough ini reviewer bebas pilih: sign-off cepat (tinggal jawab `pass` per item kalau confirm dari log Playwright), atau eksekusi ulang manual sebagai sanity check.
