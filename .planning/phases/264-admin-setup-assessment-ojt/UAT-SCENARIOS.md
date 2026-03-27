# UAT Scenarios: Admin Setup Assessment OJT

**Phase:** 264
**Server:** http://10.55.3.3/KPB-PortalHC/
**Admin Account:** admin@pertamina.com
**Tanggal Test:** 2026-03-28

---

## Skenario 1 -- Buat Assessment OJT dengan Token (SETUP-01)

### Langkah

1. Buka http://10.55.3.3/KPB-PortalHC/Account/Login
2. Login sebagai **admin@pertamina.com**
3. Navigasi: Kelola Data > klik card **Manajemen Assessment** atau buka langsung http://10.55.3.3/KPB-PortalHC/Admin/ManageAssessment
4. Klik tombol **Buat Assessment Baru** (link ke http://10.55.3.3/KPB-PortalHC/Admin/CreateAssessment)
5. Isi form:
   - **Kategori:** Assessment OJ (pilih dari dropdown kategori yang ada)
   - **Judul:** `UAT OJT Test 1 - Token`
   - **Token Required:** Centang (IsTokenRequired = true)
   - **Access Token:** Biarkan auto-generated atau isi manual (harus uppercase)
   - **Durasi (menit):** `30`
   - **Pass Percentage:** `70`
   - **Jadwal:** Pilih tanggal besok (2026-03-29)
   - **Allow Answer Review:** Centang
6. Assign 3 worker (cari di dropdown user):
   - rino.prasetyo@pertamina.com
   - mohammad.arsyad@pertamina.com
   - moch.widyadhana@pertamina.com
7. Klik **Submit / Buat Assessment**

### Expected Result

- Redirect ke halaman CreateAssessment dengan popup sukses (TempData["CreatedAssessment"])
- Popup menampilkan: judul, kategori, jadwal, 3 user yang di-assign, dan access token
- Assessment muncul di ManageAssessment list dengan status **Open**

### Query Database Verifikasi

```sql
-- Cek session dibuat untuk 3 user
SELECT s.Id, s.Title, s.Category, s.Status, s.IsTokenRequired, s.AccessToken,
       s.DurationMinutes, s.PassPercentage, s.Schedule, u.Email
FROM AssessmentSessions s
JOIN AspNetUsers u ON s.UserId = u.Id
WHERE s.Title = 'UAT OJT Test 1 - Token'
ORDER BY s.Id;
```

Harus ada 3 row, semua Status = 'Open', IsTokenRequired = 1, DurationMinutes = 30, PassPercentage = 70.

---

## Skenario 2 -- Buat Assessment OJT tanpa Token (SETUP-01)

### Langkah

1. Dari ManageAssessment, klik **Buat Assessment Baru**
2. Isi form:
   - **Kategori:** Assessment OJ
   - **Judul:** `UAT OJT Test 2 - No Token`
   - **Token Required:** Jangan centang (IsTokenRequired = false)
   - **Durasi (menit):** `60`
   - **Pass Percentage:** `80`
   - **Jadwal:** Pilih tanggal besok (2026-03-29)
   - **Allow Answer Review:** Centang
3. Assign 3 worker yang sama:
   - rino.prasetyo@pertamina.com
   - mohammad.arsyad@pertamina.com
   - moch.widyadhana@pertamina.com
4. Klik **Submit / Buat Assessment**

### Expected Result

- Popup sukses muncul, tanpa AccessToken (karena token not required)
- Assessment muncul di ManageAssessment list, status **Open**
- IsTokenRequired = false, AccessToken = "" (kosong)

### Query Database Verifikasi

```sql
SELECT s.Id, s.Title, s.Status, s.IsTokenRequired, s.AccessToken,
       s.DurationMinutes, s.PassPercentage, u.Email
FROM AssessmentSessions s
JOIN AspNetUsers u ON s.UserId = u.Id
WHERE s.Title = 'UAT OJT Test 2 - No Token'
ORDER BY s.Id;
```

Harus ada 3 row, semua Status = 'Open', IsTokenRequired = 0, AccessToken = '', PassPercentage = 80.

---

## Skenario 3 -- Download Template dan Import Soal (SETUP-02, SETUP-03)

### Prasyarat

Assessment dari Skenario 1 atau 2 sudah dibuat.

### Langkah

1. Buka http://10.55.3.3/KPB-PortalHC/Admin/ManageAssessment
2. Cari assessment **"UAT OJT Test 2 - No Token"** di list (klik row untuk expand detail)
3. Cari link/tombol untuk mengelola paket soal (AssessmentPackage). Setiap assessment punya minimal 1 package ("Paket A").
4. Klik link **Download Template** atau buka langsung http://10.55.3.3/KPB-PortalHC/Admin/DownloadQuestionTemplate
5. Verifikasi file Excel yang didownload:
   - Nama file: `question_import_template.xlsx`
   - Header kolom: Question | Option A | Option B | Option C | Option D | Correct | Elemen Teknis
   - Row 2: contoh soal (italic, abu-abu)
   - Row 3-4: instruksi (merah)

6. **Isi template dengan 15 soal** (supaya > 10, untuk test pagination di Phase 265):
   - Kolom Question: teks pertanyaan
   - Kolom Option A-D: 4 pilihan jawaban
   - Kolom Correct: huruf A, B, C, atau D
   - Kolom Elemen Teknis: opsional (bisa dikosongkan atau isi nama elemen)
   - **Hapus row contoh** (row 2) dan row instruksi (row 3-4) sebelum import, atau mulai isi dari row 5 (contoh dan instruksi akan di-skip jika Question kosong)

7. Buka halaman ImportPackageQuestions untuk package yang sesuai:
   - URL: http://10.55.3.3/KPB-PortalHC/Admin/ImportPackageQuestions?packageId={PACKAGE_ID}
   - (PACKAGE_ID bisa dilihat dari link di halaman ManageAssessment detail)
8. Upload file Excel yang sudah diisi
9. Klik **Import**

### Expected Result

- Redirect kembali ke ImportPackageQuestions dengan pesan sukses: "{N} soal berhasil diimport"
- Soal muncul di daftar pertanyaan package
- Jumlah soal = 15 (atau sesuai yang diisi)

### Query Database Verifikasi

```sql
-- Cek package dan jumlah soal
SELECT p.Id AS PackageId, p.PackageName, p.AssessmentSessionId,
       COUNT(q.Id) AS QuestionCount
FROM AssessmentPackages p
LEFT JOIN PackageQuestions q ON q.AssessmentPackageId = p.Id
WHERE p.AssessmentSessionId IN (
    SELECT Id FROM AssessmentSessions WHERE Title = 'UAT OJT Test 2 - No Token'
)
GROUP BY p.Id, p.PackageName, p.AssessmentSessionId;

-- Cek detail soal dan opsi
SELECT q.Id, q.QuestionText, q.[Order], q.ElemenTeknis,
       o.OptionText, o.IsCorrect
FROM PackageQuestions q
JOIN PackageOptions o ON o.PackageQuestionId = q.Id
WHERE q.AssessmentPackageId = {PACKAGE_ID}
ORDER BY q.[Order], o.Id;
```

Harus ada 15 soal, masing-masing punya 4 opsi, tepat 1 opsi IsCorrect = 1 per soal.

**Catatan:** Lakukan juga import soal untuk assessment "UAT OJT Test 1 - Token" (minimal 5 soal cukup, untuk test flow basic di Phase 265).

---

## Skenario 4 -- Verifikasi Assign Worker (SETUP-04)

### Langkah

1. Buka http://10.55.3.3/KPB-PortalHC/Admin/ManageAssessment
2. Cari assessment **"UAT OJT Test 1 - Token"** -- klik untuk expand detail
3. Verifikasi daftar peserta:
   - rino.prasetyo@pertamina.com -- Status: Open
   - mohammad.arsyad@pertamina.com -- Status: Open
   - moch.widyadhana@pertamina.com -- Status: Open
4. Ulangi untuk **"UAT OJT Test 2 - No Token"**
5. Verifikasi bahwa setiap worker memiliki session terpisah (bukan satu session shared)

### Expected Result

- Setiap assessment menampilkan 3 peserta di detail
- Setiap peserta punya row AssessmentSession sendiri (Id berbeda)
- Semua status = **Open**

### Query Database Verifikasi

```sql
-- Assessment 1: Token
SELECT s.Id, s.Title, s.Status, u.FullName, u.Email
FROM AssessmentSessions s
JOIN AspNetUsers u ON s.UserId = u.Id
WHERE s.Title = 'UAT OJT Test 1 - Token'
ORDER BY u.Email;

-- Assessment 2: No Token
SELECT s.Id, s.Title, s.Status, u.FullName, u.Email
FROM AssessmentSessions s
JOIN AspNetUsers u ON s.UserId = u.Id
WHERE s.Title = 'UAT OJT Test 2 - No Token'
ORDER BY u.Email;
```

Masing-masing harus 3 row, semua Status = 'Open'.

---

## Potensi Bug dari Analisa Kode (D-07)

Berdasarkan analisa kode AdminController.cs, berikut potensi masalah yang perlu diperhatikan saat test:

### 1. Kategori "Assessment OJ" harus ada di database

Kategori assessment diambil dari tabel `AssessmentCategories`. Jika kategori "Assessment OJ" belum ada atau tidak aktif (`IsActive = false`), dropdown kategori tidak akan menampilkannya.

**Verifikasi:**
```sql
SELECT Id, Name, IsActive, ParentId FROM AssessmentCategories WHERE Name LIKE '%OJ%';
```
Jika tidak ada, admin perlu membuat kategori terlebih dahulu via ManageAssessment > tab Kategori.

### 2. AssessmentPackage tidak otomatis dibuat saat CreateAssessment

Dari analisa kode `CreateAssessment POST`, **tidak ada kode yang membuat AssessmentPackage otomatis**. Package harus dibuat secara manual atau mungkin ada mekanisme lain (cek UI ManageAssessment apakah ada tombol "Tambah Paket").

**Implikasi:** Setelah buat assessment, mungkin perlu membuat Package terlebih dahulu sebelum bisa import soal.

**Verifikasi:**
```sql
SELECT * FROM AssessmentPackages WHERE AssessmentSessionId IN (
    SELECT Id FROM AssessmentSessions WHERE Title LIKE 'UAT OJT%'
);
```

### 3. Schedule validation: tanggal tidak boleh di masa lalu

Kode validasi: `model.Schedule < DateTime.Today` -- artinya jadwal harus hari ini atau besok. Jika server timezone berbeda dari lokal, bisa terjadi edge case.

### 4. Redirect setelah CreateAssessment POST

Setelah sukses, kode redirect ke `CreateAssessment` (bukan `ManageAssessment`). Ini berarti admin tetap di halaman form create, dengan popup sukses. Bukan bug, tapi perlu diketahui saat testing.

### 5. Cross-package question count validation

Saat import soal ke package kedua (misalnya Paket B), sistem akan validasi jumlah soal harus sama dengan Paket A. Ini hanya relevan jika assessment punya lebih dari 1 package.

---

## Ringkasan Coverage Requirement

| Requirement | Skenario | Deskripsi |
|-------------|----------|-----------|
| SETUP-01 | Skenario 1, 2 | Buat assessment OJT (dengan & tanpa token) |
| SETUP-02 | Skenario 3 | Download template soal |
| SETUP-03 | Skenario 3 | Import soal ke assessment |
| SETUP-04 | Skenario 4 | Verifikasi worker assignment |
