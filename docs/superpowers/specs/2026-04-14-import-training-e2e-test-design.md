# Import Training & Assessment - E2E Test Design

**Date:** 2026-04-14
**Status:** Draft
**Type:** Automated End-to-End Testing
**Tool:** Playwright

---

## 1. Overview

### 1.1 Purpose
Membuat automated end-to-end test untuk halaman Import Training dan Import Assessment di aplikasi Portal HC. Test akan memverifikasi happy path, error handling dasar, dan memastikan data benar-benar tersimpan di database melalui UI verification.

### 1.2 Scope
- **Halaman yang di-test:** `/Admin/ImportTraining`
- **Tipe Import:** Training dan Assessment
- **Fokus Verifikasi:** Happy path + Error handling dasar + Database verification via UI
- **Pendekatan:** E2E Test dengan File Fixtures (Pendekatan 1)

### 1.3 Out of Scope
- Unit test untuk controller logic
- Load/performance testing
- Security testing
- Edge cases yang kompleks (khususnya yang sudah di-cover oleh existing validation)

---

## 2. Test Architecture

### 2.1 File Structure

```
tests/
├── e2e/
│   └── import-training.spec.ts          # Main test file (12-15 test cases)
├── fixtures/
│   └── excel/                           # Excel test fixtures
│       ├── training-valid.xlsx
│       ├── training-invalid-nip.xlsx
│       ├── training-missing-required.xlsx
│       ├── training-wrong-date.xlsx
│       ├── assessment-valid.xlsx
│       ├── assessment-invalid-nip.xlsx
│       ├── assessment-invalid-lulus.xlsx
│       └── not-excel.txt
├── helpers/
│   ├── auth.ts                          # (existing)
│   ├── accounts.ts                      # (existing)
│   ├── utils.ts                         # (existing)
│   └── import-helpers.ts                # NEW: Helper functions
└── playwright.config.ts                 # (existing)
```

### 2.2 Components

1. **Test File (`import-training.spec.ts`)**
   - Berisi semua test cases untuk Training dan Assessment import
   - Menggunakan `test.describe` untuk grouping
   - Serial mode untuk menghindari conflicts

2. **Excel Fixtures (`fixtures/excel/`)**
   - File Excel pre-made untuk berbagai skenario test
   - Dibuat menggunakan Excel/LibreOffice secara manual
   - Mengikuti format dari DownloadImportTrainingTemplate

3. **Helper Functions (`helpers/import-helpers.ts`)**
   - `uploadAndProcessImport()` - Upload file dan tunggu proses
   - `verifyImportSummary()` - Verify summary count
   - `verifyTrainingInList()` - Verify data di ManageAssessment
   - `verifyAssessmentInList()` - Verify assessment di ManageAssessment

---

## 3. Test Cases

### 3.1 Training Import Tests

| Test ID | Test Case | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| **TR-01** | Download Template | HC download training template | File terdownload dengan nama benar |
| **TR-02** | Upload Valid (3 rows) | Upload file dengan 3 training valid | 3 Success, summary muncul, data muncul di UI |
| **TR-03** | Invalid NIP | Upload dengan NIP tidak terdaftar (999999) | Error message, data tidak tersimpan |
| **TR-04** | Missing Required | Upload dengan Judul kosong | Error message: "Judul tidak boleh kosong" |
| **TR-05** | Non-Excel File | Upload file .txt | Error message: "Hanya file Excel" |
| **TR-06** | Wrong Date Format | Upload dengan tanggal "01/01/2024" | Error message: "Format Tanggal tidak valid" |

### 3.2 Assessment Import Tests

| Test ID | Test Case | Description | Expected Result |
|---------|-----------|-------------|-----------------|
| **AS-01** | Download Template | HC download assessment template | File terdownload dengan nama benar |
| **AS-02** | Upload Valid (3 rows) | Upload file dengan 3 assessment valid | 3 Success, summary muncul, data muncul di UI |
| **AS-03** | Invalid NIP | Upload dengan NIP tidak terdaftar | Error message, data tidak tersimpan |
| **AS-04** | Invalid Lulus Value | Upload dengan Lulus = "Yes" (bukan Ya/Tidak) | Error atau parse sebagai false |

### 3.3 Verification Strategy

**Database Verification via UI:**
1. Upload file Excel
2. Verify summary result di halaman ImportTraining
3. Navigate ke halaman ManageAssessment (tab Training atau Assessment)
4. Search data yang baru diimport
5. Verify data muncul di list

**Rationale:**
- Tidak perlu mengubah production code (tidak perlu buat API endpoint)
- Sesuai dengan cara user sebenarnya memakai aplikasi
- Cukup akurat untuk kebutuhan verifikasi basic

---

## 4. Excel Fixtures Specification

### 4.1 Training Template Format

**Kolom Training:**
```
| Kolom | Example | Required? | Notes |
|-------|---------|-----------|-------|
| NIP | rino.prasetyo | Yes | Email yang terdaftar di sistem |
| Judul | Training Test 1 | Yes | Unique untuk test |
| Kategori | MANDATORY | Yes | PROTON / OJT / MANDATORY |
| SubKategori | Safety Induction | No | Opsional |
| Tanggal | 2024-01-15 | Yes | Format: YYYY-MM-DD |
| TanggalMulai | 2024-01-15 | No | Format: YYYY-MM-DD |
| TanggalSelesai | 2024-01-17 | No | Format: YYYY-MM-DD |
| Penyelenggara | Internal | No | Nama penyelenggara |
| Kota | Balikpapan | No | Kota pelaksanaan |
| Status | Passed | No | Passed / Valid / Expired / Failed |
| ValidUntil | 2027-01-15 | No | Format: YYYY-MM-DD |
| NomorSertifikat | CERT-001 | No | Nomor sertifikat |
```

**File Fixtures:**

1. **training-valid.xlsx** (3 rows)
   - Row 1: NIP `rino.prasetyo@pertamina.com`, Judul `Training Test 1`, Kategori `MANDATORY`, Tanggal `2024-01-15`
   - Row 2: NIP `iwan3@pertamina.com`, Judul `Training Test 2`, Kategori `OJT`, Tanggal `2024-02-20`
   - Row 3: NIP `rino.prasetyo@pertamina.com`, Judul `Training Test 3`, Kategori `PROTON`, Tanggal `2024-03-10`

2. **training-invalid-nip.xlsx** (1 row)
   - NIP `999999`, Judul `Invalid NIP Test`, Kategori `MANDATORY`, Tanggal `2024-01-15`

3. **training-missing-required.xlsx** (1 row)
   - NIP `rino.prasetyo@pertamina.com`, Judul **(kosong)**, Kategori `MANDATORY`, Tanggal `2024-01-15`

4. **training-wrong-date.xlsx** (1 row)
   - NIP `rino.prasetyo@pertamina.com`, Judul `Wrong Date Test`, Kategori `MANDATORY`, Tanggal `01/01/2024`

### 4.2 Assessment Template Format

**Kolom Assessment:**
```
| Kolom | Example | Required? | Notes |
|-------|---------|-----------|-------|
| NIP | rino.prasetyo@pertamina.com | Yes | Email yang terdaftar |
| Judul | Assessment Test 1 | Yes | Unique untuk test |
| Kategori | MANDATORY | Yes | Nama kategori |
| SubKategori | K3 Umum | No | Sub-kategori |
| Score | 85 | No | 0-100 |
| Lulus | Ya | Yes | Ya / Tidak |
| Tanggal | 2024-01-15 | Yes | Format: YYYY-MM-DD |
| Penyelenggara | PT Safety | No | Nama penyelenggara |
| Kota | Balikpapan | No | Kota pelaksanaan |
| ValidUntil | 2027-01-15 | No | Format: YYYY-MM-DD |
| NomorSertifikat | CERT-A001 | No | Nomor sertifikat |
| CertificateType | Kompetensi | No | Kompetensi / Profesi / Pelatihan |
```

**File Fixtures:**

1. **assessment-valid.xlsx** (3 rows)
   - Row 1: NIP `rino.prasetyo@pertamina.com`, Judul `Assessment Test 1`, Kategori `MANDATORY`, Score `85`, Lulus `Ya`, Tanggal `2024-01-15`
   - Row 2: NIP `iwan3@pertamina.com`, Judul `Assessment Test 2`, Kategori `OJT`, Score `90`, Lulus `Ya`, Tanggal `2024-02-20`
   - Row 3: NIP `rino.prasetyo@pertamina.com`, Judul `Assessment Test 3`, Kategori `PROTON`, Score `75`, Lulus `Ya`, Tanggal `2024-03-10`

2. **assessment-invalid-nip.xlsx** (1 row)
   - NIP `999999`, Judul `Invalid NIP Test`, Kategori `MANDATORY`, Lulus `Ya`, Tanggal `2024-01-15`

3. **assessment-invalid-lulus.xlsx** (1 row)
   - NIP `rino.prasetyo@pertamina.com`, Judul `Invalid Lulus Test`, Kategori `MANDATORY`, Lulus `Yes`, Tanggal `2024-01-15`

---

## 5. Implementation Details

### 5.1 Creating Excel Fixtures (Opsi 2: Manual dengan Excel/LibreOffice)

**Step-by-Step:**

1. **Buka Excel atau LibreOffice Calc**

2. **Buat file baru untuk training-valid.xlsx:**
   - Row 1 (Header): NIP, Judul, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Penyelenggara, Kota, Status, ValidUntil, NomorSertifikat
   - Row 2: rino.prasetyo@pertamina.com, Training Test 1, MANDATORY, , 2024-01-15, 2024-01-15, 2024-01-17, Internal, Balikpapan, Passed, 2027-01-15, CERT-001
   - Row 3: iwan3@pertamina.com, Training Test 2, OJT, , 2024-02-20, , , , , , ,
   - Row 4: rino.prasetyo@pertamina.com, Training Test 3, PROTON, , 2024-03-10, , , , , , ,

3. **Save as:**
   - Nama file: `training-valid.xlsx`
   - Lokasi: `tests/fixtures/excel/training-valid.xlsx`
   - Format: Excel Workbook (.xlsx)

4. **Ulangi untuk file-file lain** sesuai spesifikasi di Section 4.1 dan 4.2

5. **Buat file not-excel.txt:**
   - Isi dengan teks apa saja
   - Save sebagai `not-excel.txt` di folder yang sama

**Tips:**
- Gunakan email sebagai NIP (sesuai implementasi aplikasi)
- Pastikan format tanggal: YYYY-MM-DD
- Untuk kolom opsional, biarkan kosong atau isi dengan nilai
- Judul harus unique agar tidak conflict dengan tests sebelumnya

### 5.2 Helper Functions Implementation

**File: `tests/helpers/import-helpers.ts`**

```typescript
import { Page, expect } from '@playwright/test';

/**
 * Upload Excel file dan proses import
 * @param page - Playwright Page object
 * @param filePath - Path ke file Excel fixture
 */
export async function uploadAndProcessImport(page: Page, filePath: string) {
  await page.setInputFiles('#excelFileInput', filePath);
  await page.click('#btnImport');

  // Wait for loading to complete
  await page.waitForSelector('text=Memproses import...', { state: 'hidden', timeout: 30000 });

  // Wait for results to appear
  await page.waitForSelector('.card:has-text("Berhasil Dibuat")', { timeout: 5000 });
}

/**
 * Verify summary count setelah import
 * @param page - Playwright Page object
 * @param success - Expected number of successful imports
 * @param error - Expected number of errors
 */
export async function verifyImportSummary(page: Page, success: number, error: number) {
  await expect(page.locator('.card:has-text("Berhasil Dibuat")')).toContainText(success.toString());
  await expect(page.locator('.card:has-text("Error / Gagal")')).toContainText(error.toString());
}

/**
 * Verify training muncul di ManageAssessment list
 * @param page - Playwright Page object
 * @param trainingTitle - Judul training yang dicari
 */
export async function verifyTrainingInList(page: Page, trainingTitle: string) {
  await page.goto('/Admin/ManageAssessment?tab=training');
  await page.waitForLoadState('networkidle');

  const searchInput = page.locator('input[placeholder*="Cari"]').first();
  await searchInput.fill(trainingTitle);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');

  await expect(page.locator(`text=${trainingTitle}`)).toBeVisible();
}

/**
 * Verify assessment muncul di ManageAssessment list
 * @param page - Playwright Page object
 * @param assessmentTitle - Judul assessment yang dicari
 */
export async function verifyAssessmentInList(page: Page, assessmentTitle: string) {
  await page.goto('/Admin/ManageAssessment?tab=assessment');
  await page.waitForLoadState('networkidle');

  const searchInput = page.locator('input[placeholder*="Cari"]').first();
  await searchInput.fill(assessmentTitle);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');

  await expect(page.locator(`text=${assessmentTitle}`)).toBeVisible();
}
```

### 5.3 Test File Structure

**File: `tests/e2e/import-training.spec.ts`**

```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';
import {
  uploadAndProcessImport,
  verifyImportSummary,
  verifyTrainingInList,
  verifyAssessmentInList
} from '../helpers/import-helpers';

// Serial mode untuk menghindari conflicts
test.describe.configure({ mode: 'serial' });

test.describe('Import Training - Training Type', () => {
  // TR-01 sampai TR-06
});

test.describe('Import Training - Assessment Type', () => {
  // AS-01 sampai AS-04
});
```

---

## 6. Running Tests

### 6.1 Prerequisites

Sebelum menjalankan tests:

1. **Server Running**
   ```bash
   # Pastikan aplikasi running
   # URL: http://localhost:5277
   ```

2. **Excel Fixtures Ready**
   - Semua file Excel sudah dibuat di `tests/fixtures/excel/`
   - Format sesuai spesifikasi

3. **Test Accounts Ready**
   - HC account: `meylisa.tjiang@pertamina.com` / `123456`
   - Coachee accounts: `rino.prasetyo@pertamina.com`, `iwan3@pertamina.com`
   - Accounts sudah ada di database

### 6.2 Commands

**Jalankan semua tests:**
```bash
cd tests
npx playwright test import-training.spec.ts
```

**Jalankan hanya training tests:**
```bash
npx playwright test import-training.spec.ts -g "Training Type"
```

**Jalankan hanya assessment tests:**
```bash
npx playwright test import-training.spec.ts -g "Assessment Type"
```

**Jalankan test spesifik:**
```bash
npx playwright test import-training.spec.ts -g "TR-02"
```

**Debug mode:**
```bash
npx playwright test import-training.spec.ts --debug
```

**Headed mode (bisa lihat browser):**
```bash
npx playwright test import-training.spec.ts --headed
```

---

## 7. Data Cleanup Strategy

### 7.1 Approach: Unique Data per Test

Untuk mencegah data test menumpuk di database, gunakan data unik setiap test:

**Option A: Timestamp-based Uniqueness**
```typescript
const uniqueTitle = `Training Test ${Date.now()}`;
// Saat verify, search dengan title yang sama persis
```

**Option B: Random Number**
```typescript
const uniqueTitle = `Training Test ${Math.floor(Math.random() * 10000)}`;
```

**Option C: Test Sequence Number**
```typescript
let testCounter = 1;
const uniqueTitle = `Training Test ${testCounter++}`;
```

### 7.2 Manual Cleanup (Optional)

Jika perlu cleanup manual:

1. Login sebagai HC
2. Buka ManageAssessment
3. Search data test (filter by "Training Test" atau "Assessment Test")
4. Delete satu per satu

Atau via database (jika ada access):
```sql
-- Delete training test records
DELETE FROM TrainingRecords WHERE Judul LIKE 'Training Test%';

-- Delete assessment test records
DELETE FROM AssessmentSessions WHERE Title LIKE 'Assessment Test%' AND IsManualEntry = 1;
```

---

## 8. Success Criteria

Test suite dianggap sukses jika:

1. ✅ Semua happy path tests pass (TR-02, AS-02)
2. ✅ Semua error handling tests pass sesuai expected behavior
3. ✅ File validation tests berjalan dengan benar
4. ✅ Database verification berhasil (data muncul di ManageAssessment)
5. ✅ Tests dapat dijalankan berulang kali tanpa flaky behavior
6. ✅ Excel fixtures mudah dibuat dan di-maintain

---

## 9. Timeline Estimation

| Task | Estimation |
|------|------------|
| Create Excel fixtures (8 files) | 30-60 menit |
| Create helper functions | 15-30 menit |
| Write test cases (Training) | 30-45 menit |
| Write test cases (Assessment) | 20-30 menit |
| Run and debug tests | 30-60 menit |
| **Total** | **2-3.5 jam** |

---

## 10. Next Steps

Setelah design disetujui:

1. **Review spec** dengan user untuk final approval
2. **Create Excel fixtures** sesuai spesifikasi
3. **Implement helper functions** di `tests/helpers/import-helpers.ts`
4. **Write test file** `tests/e2e/import-training.spec.ts`
5. **Run tests** dan debug jika ada issue
6. **Document findings** dan update spec jika perlu

---

## Appendix A: Selector Reference

**Selectors yang digunakan di tests:**

| Element | Selector | Notes |
|---------|----------|-------|
| Training radio button | `#typeTraining` | Default selected |
| Assessment radio button | `#typeAssessment` | |
| File input | `#excelFileInput` | |
| Import button | `#btnImport` | Disabled jika no file |
| Download template button | `#btnDownloadTemplate` | Training |
| Download template button | `#btnDownloadAssessmentTemplate` | Assessment |
| Success count card | `.card:has-text("Berhasil Dibuat")` | |
| Error count card | `.card:has-text("Error / Gagal")` | |
| Success badge | `.badge.bg-success` | |
| Error badge | `.badge.bg-danger` | |
| Search input | `input[placeholder*="Cari"]` | Multiple, use .first() |

---

## Appendix B: Troubleshooting

**Common Issues:**

1. **Test tidak bisa find element**
   - Cek apakah page sudah fully loaded
   - Tambah `await page.waitForLoadState('networkidle')`

2. **File not found**
   - Pastikan path Excel fixture relative dari root project
   - Bisa gunakan absolute path jika perlu

3. **Data tidak muncul di ManageAssessment**
   - Cek apakah import selesai (tunggu loading)
   - Cek apakah search query correct
   - Verify di database langsung jika perlu

4. **Tests flaky**
   - Tambahkan timeout yang lebih panjang
   - Gunakan `waitForLoadState()` dan `waitForSelector()`
   - Pastikan tests tidak saling interfere (gunakan serial mode)

---

**End of Design Document**
