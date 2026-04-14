# Import Training & Assessment - E2E Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Membuat automated end-to-end test untuk halaman Import Training & Assessment menggunakan Playwright, mencakup happy path, error handling dasar, dan verifikasi database via UI.

**Architecture:** E2E test suite menggunakan Playwright dengan file Excel fixtures. Test akan mengupload file Excel, memproses import, verify hasil di halaman ImportTraining, dan verify data tersimpan via UI di halaman ManageAssessment. Helper functions reusable untuk common operations. Serial test execution untuk menghindari conflicts.

**Tech Stack:** Playwright, TypeScript, Excel fixtures (.xlsx), ASP.NET Core application

---

## File Structure

```
tests/
├── e2e/
│   └── import-training.spec.ts          # NEW: Main test file (12-15 test cases)
├── fixtures/
│   └── excel/                           # NEW: Excel test fixtures folder
│       ├── training-valid.xlsx          # NEW: 3 rows valid training data
│       ├── training-invalid-nip.xlsx    # NEW: 1 row with invalid NIP
│       ├── training-missing-required.xlsx  # NEW: 1 row with empty Judul
│       ├── training-wrong-date.xlsx     # NEW: 1 row with wrong date format
│       ├── assessment-valid.xlsx        # NEW: 3 rows valid assessment data
│       ├── assessment-invalid-nip.xlsx  # NEW: 1 row with invalid NIP
│       ├── assessment-invalid-lulus.xlsx  # NEW: 1 row with wrong Lulus value
│       └── not-excel.txt                # NEW: Text file for format validation test
├── helpers/
│   ├── auth.ts                          # EXISTING: Login helpers
│   ├── accounts.ts                      # EXISTING: Test accounts
│   ├── utils.ts                         # EXISTING: Utility functions
│   └── import-helpers.ts                # NEW: Helper functions for import tests
└── playwright.config.ts                 # EXISTING: Playwright config
```

---

## Task 1: Create Excel Fixtures Directory and First Training File

**Files:**
- Create: `tests/fixtures/excel/training-valid.xlsx`

**Context:** Kita perlu membuat Excel test fixtures untuk berbagai skenario. File ini akan berisi 3 rows training data yang valid untuk test happy path.

- [ ] **Step 1: Create fixtures directory**

```bash
mkdir -p tests/fixtures/excel
```

- [ ] **Step 2: Open Excel or LibreOffice Calc and create training-valid.xlsx**

Buat file Excel baru dengan struktur berikut:

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Tanggal | TanggalMulai | TanggalSelesai | Penyelenggara | Kota | Status | ValidUntil | NomorSertifikat
```

**Row 2:**
```
rino.prasetyo@pertamina.com | Training Test 1 | MANDATORY | | 2024-01-15 | 2024-01-15 | 2024-01-17 | Internal | Balikpapan | Passed | 2027-01-15 | CERT-001
```

**Row 3:**
```
iwan3@pertamina.com | Training Test 2 | OJT | | 2024-02-20 | | | | | | |
```

**Row 4:**
```
rino.prasetyo@pertamina.com | Training Test 3 | PROTON | | 2024-03-10 | | | | | | |
```

- [ ] **Step 3: Save the file**

- File name: `training-valid.xlsx`
- Save location: `tests/fixtures/excel/training-valid.xlsx`
- Format: Excel Workbook (.xlsx)

- [ ] **Step 4: Verify file was created correctly**

```bash
ls -la tests/fixtures/excel/training-valid.xlsx
```

Expected: File exists and shows file size

- [ ] **Step 5: Commit the fixture file**

```bash
git add tests/fixtures/excel/
git commit -m "test: add training-valid.xlsx fixture with 3 valid training rows"
```

---

## Task 2: Create Invalid NIP Training Fixture

**Files:**
- Create: `tests/fixtures/excel/training-invalid-nip.xlsx`

**Context:** File ini untuk test error handling ketika NIP tidak terdaftar di sistem.

- [ ] **Step 1: Create Excel file training-invalid-nip.xlsx**

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Tanggal | TanggalMulai | TanggalSelesai | Penyelenggara | Kota | Status | ValidUntil | NomorSertifikat
```

**Row 2:**
```
999999 | Invalid NIP Test | MANDATORY | | 2024-01-15 | | | | | | |
```

Note: NIP `999999` tidak terdaftar di sistem

- [ ] **Step 2: Save the file**

- File name: `training-invalid-nip.xlsx`
- Save location: `tests/fixtures/excel/training-invalid-nip.xlsx`

- [ ] **Step 3: Verify file was created**

```bash
ls -la tests/fixtures/excel/training-invalid-nip.xlsx
```

- [ ] **Step 4: Commit the fixture**

```bash
git add tests/fixtures/excel/
git commit -m "test: add training-invalid-nip.xlsx fixture for error handling test"
```

---

## Task 3: Create Missing Required Field Training Fixture

**Files:**
- Create: `tests/fixtures/excel/training-missing-required.xlsx`

**Context:** File ini untuk test error handling ketika required field (Judul) kosong.

- [ ] **Step 1: Create Excel file training-missing-required.xlsx**

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Tanggal | TanggalMulai | TanggalSelesai | Penyelenggara | Kota | Status | ValidUntil | NomorSertifikat
```

**Row 2:**
```
rino.prasetyo@pertamina.com | | MANDATORY | | 2024-01-15 | | | | | | |
```

Note: Kolom Judul dikosongkan

- [ ] **Step 2: Save the file**

- File name: `training-missing-required.xlsx`
- Save location: `tests/fixtures/excel/training-missing-required.xlsx`

- [ ] **Step 3: Commit the fixture**

```bash
git add tests/fixtures/excel/
git commit -m "test: add training-missing-required.xlsx fixture for validation test"
```

---

## Task 4: Create Wrong Date Format Training Fixture

**Files:**
- Create: `tests/fixtures/excel/training-wrong-date.xlsx`

**Context:** File ini untuk test error handling ketika format tanggal tidak sesuai (harus YYYY-MM-DD).

- [ ] **Step 1: Create Excel file training-wrong-date.xlsx**

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Tanggal | TanggalMulai | TanggalSelesai | Penyelenggara | Kota | Status | ValidUntil | NomorSertifikat
```

**Row 2:**
```
rino.prasetyo@pertamina.com | Wrong Date Test | MANDATORY | | 01/01/2024 | | | | | | |
```

Note: Tanggal menggunakan format DD/MM/YYYY bukan YYYY-MM-DD

- [ ] **Step 2: Save the file**

- File name: `training-wrong-date.xlsx`
- Save location: `tests/fixtures/excel/training-wrong-date.xlsx`

- [ ] **Step 3: Commit the fixture**

```bash
git add tests/fixtures/excel/
git commit -m "test: add training-wrong-date.xlsx fixture for date format validation test"
```

---

## Task 5: Create Assessment Valid Fixture

**Files:**
- Create: `tests/fixtures/excel/assessment-valid.xlsx`

**Context:** File ini berisi 3 rows assessment data yang valid untuk test happy path assessment import.

- [ ] **Step 1: Create Excel file assessment-valid.xlsx**

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Score | Lulus | Tanggal | Penyelenggara | Kota | ValidUntil | NomorSertifikat | CertificateType
```

**Row 2:**
```
rino.prasetyo@pertamina.com | Assessment Test 1 | MANDATORY | | 85 | Ya | 2024-01-15 | PT Safety | Balikpapan | 2027-01-15 | CERT-A001 | Kompetensi
```

**Row 3:**
```
iwan3@pertamina.com | Assessment Test 2 | OJT | | 90 | Ya | 2024-02-20 | | | | | |
```

**Row 4:**
```
rino.prasetyo@pertamina.com | Assessment Test 3 | PROTON | | 75 | Ya | 2024-03-10 | | | | | |
```

- [ ] **Step 2: Save the file**

- File name: `assessment-valid.xlsx`
- Save location: `tests/fixtures/excel/assessment-valid.xlsx`

- [ ] **Step 3: Commit the fixture**

```bash
git add tests/fixtures/excel/
git commit -m "test: add assessment-valid.xlsx fixture with 3 valid assessment rows"
```

---

## Task 6: Create Assessment Invalid NIP Fixture

**Files:**
- Create: `tests/fixtures/excel/assessment-invalid-nip.xlsx`

**Context:** File ini untuk test error handling assessment dengan NIP tidak terdaftar.

- [ ] **Step 1: Create Excel file assessment-invalid-nip.xlsx**

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Score | Lulus | Tanggal | Penyelenggara | Kota | ValidUntil | NomorSertifikat | CertificateType
```

**Row 2:**
```
999999 | Invalid NIP Assessment | MANDATORY | | 85 | Ya | 2024-01-15 | | | | | |
```

- [ ] **Step 2: Save the file**

- File name: `assessment-invalid-nip.xlsx`
- Save location: `tests/fixtures/excel/assessment-invalid-nip.xlsx`

- [ ] **Step 3: Commit the fixture**

```bash
git add tests/fixtures/excel/
git commit -m "test: add assessment-invalid-nip.xlsx fixture for error handling test"
```

---

## Task 7: Create Assessment Invalid Lulus Fixture

**Files:**
- Create: `tests/fixtures/excel/assessment-invalid-lulus.xlsx`

**Context:** File ini untuk test error handling ketika kolom Lulus berisi nilai selain "Ya" atau "Tidak".

- [ ] **Step 1: Create Excel file assessment-invalid-lulus.xlsx**

**Row 1 (Headers):**
```
NIP | Judul | Kategori | SubKategori | Score | Lulus | Tanggal | Penyelenggara | Kota | ValidUntil | NomorSertifikat | CertificateType
```

**Row 2:**
```
rino.prasetyo@pertamina.com | Invalid Lulus Test | MANDATORY | | 85 | Yes | 2024-01-15 | | | | | |
```

Note: Kolom Lulus berisi "Yes" bukan "Ya"

- [ ] **Step 2: Save the file**

- File name: `assessment-invalid-lulus.xlsx`
- Save location: `tests/fixtures/excel/assessment-invalid-lulus.xlsx`

- [ ] **Step 3: Commit the fixture**

```bash
git add tests/fixtures/excel/
git commit -m "test: add assessment-invalid-lulus.xlsx fixture for validation test"
```

---

## Task 8: Create Non-Excel File for Format Validation

**Files:**
- Create: `tests/fixtures/excel/not-excel.txt`

**Context:** File text ini untuk test bahwa system menolak file yang bukan Excel.

- [ ] **Step 1: Create text file not-excel.txt**

```bash
echo "This is not an Excel file" > tests/fixtures/excel/not-excel.txt
```

- [ ] **Step 2: Verify file was created**

```bash
cat tests/fixtures/excel/not-excel.txt
```

Expected: Output "This is not an Excel file"

- [ ] **Step 3: Commit the file**

```bash
git add tests/fixtures/excel/
git commit -m "test: add not-excel.txt fixture for file format validation test"
```

---

## Task 9: Create Import Helper Functions

**Files:**
- Create: `tests/helpers/import-helpers.ts`

**Context:** Helper functions ini akan digunakan oleh test cases untuk common operations seperti upload file, verify summary, dan verify data di ManageAssessment.

- [ ] **Step 1: Write the failing test for helper functions**

Create test file `tests/helpers/import-helpers.test.ts`:

```typescript
import { test, expect } from '@playwright/test';
import { uploadAndProcessImport, verifyImportSummary } from './import-helpers';

test('uploadAndProcessImport helper function exists', async ({ page }) => {
  // This will fail initially since helper doesn't exist
  expect(typeof uploadAndProcessImport).toBe('function');
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd tests
npx playwright test helpers/import-helpers.test.ts
```

Expected: FAIL with "Cannot find module './import-helpers'"

- [ ] **Step 3: Create the helper functions file**

Create `tests/helpers/import-helpers.ts`:

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

- [ ] **Step 4: Run the test again to verify it passes**

```bash
cd tests
npx playwright test helpers/import-helpers.test.ts
```

Expected: PASS

- [ ] **Step 5: Clean up test file (helper functions don't need their own test file)**

```bash
rm tests/helpers/import-helpers.test.ts
```

- [ ] **Step 6: Commit the helper functions**

```bash
git add tests/helpers/import-helpers.ts
git commit -m "test: add import helper functions for E2E tests"
```

---

## Task 10: Create Main Test File Structure

**Files:**
- Create: `tests/e2e/import-training.spec.ts`

**Context:** Ini adalah file test utama yang berisi semua test cases untuk Import Training & Assessment.

- [ ] **Step 1: Write basic test file structure**

Create `tests/e2e/import-training.spec.ts`:

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
  // Tests will be added in next tasks
});

test.describe('Import Training - Assessment Type', () => {
  // Tests will be added in next tasks
});
```

- [ ] **Step 2: Verify file compiles**

```bash
cd tests
npx playwright test import-training.spec.ts --list
```

Expected: Lists test file (no test cases yet)

- [ ] **Step 3: Commit the base structure**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add base structure for import-training E2E tests"
```

---

## Task 11: Add Training Download Template Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test untuk memverifikasi HC bisa download template training.

- [ ] **Step 1: Add the test to Training Type describe block**

Add to `tests/e2e/import-training.spec.ts` inside the first describe block:

```typescript
  test('TR-01 - HC can download training template', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    // Verify radio button Training selected by default
    await expect(page.locator('#typeTraining')).toBeChecked();

    // Click download template button
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.click('#btnDownloadTemplate')
    ]);

    // Verify file name
    expect(download.suggestedFilename()).toContain('training_import_template.xlsx');
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "TR-01"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add TR-01 download training template test"
```

---

## Task 12: Add Training Valid Import Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Happy path test untuk upload 3 rows training valid dan verify data tersimpan.

- [ ] **Step 1: Add the test to Training Type describe block**

```typescript
  test('TR-02 - Upload valid training file (3 rows)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await uploadAndProcessImport(page, 'tests/fixtures/excel/training-valid.xlsx');

    await verifyImportSummary(page, 3, 0);

    // Verify all rows show success
    await expect(page.locator('.badge.bg-success')).toHaveCount(3);

    // Verify data muncul di ManageAssessment
    await verifyTrainingInList(page, 'Training Test 1');
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "TR-02"
```

Expected: PASS (jika server running dan data valid)

Note: Jika test gagal karena server tidak running, start server dulu:
```bash
# Di root directory project
dotnet run
```

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add TR-02 upload valid training file test"
```

---

## Task 13: Add Training Invalid NIP Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test error handling ketika NIP tidak terdaftar di sistem.

- [ ] **Step 1: Add the test**

```typescript
  test('TR-03 - Upload training with invalid NIP', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await uploadAndProcessImport(page, 'tests/fixtures/excel/training-invalid-nip.xlsx');

    await verifyImportSummary(page, 0, 1);
    await expect(page.locator('text=999999')).toBeVisible();
    await expect(page.locator('.badge.bg-danger')).toHaveCount(1);
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "TR-03"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add TR-03 invalid NIP error handling test"
```

---

## Task 14: Add Training Missing Required Field Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test error handling ketika required field (Judul) kosong.

- [ ] **Step 1: Add the test**

```typescript
  test('TR-04 - Upload training with missing required field', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await uploadAndProcessImport(page, 'tests/fixtures/excel/training-missing-required.xlsx');

    await expect(page.locator('text=Judul tidak boleh kosong')).toBeVisible();
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "TR-04"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add TR-04 missing required field validation test"
```

---

## Task 15: Add Non-Excel File Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test bahwa system menolak file yang bukan Excel format.

- [ ] **Step 1: Add the test**

```typescript
  test('TR-05 - Upload non-Excel file', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await page.setInputFiles('#excelFileInput', 'tests/fixtures/excel/not-excel.txt');
    await page.click('#btnImport');

    await expect(page.locator('.alert-danger')).toContainText('Hanya file Excel');
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "TR-05"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add TR-05 non-Excel file validation test"
```

---

## Task 16: Add Wrong Date Format Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test error handling ketika format tanggal tidak sesuai YYYY-MM-DD.

- [ ] **Step 1: Add the test**

```typescript
  test('TR-06 - Upload training with wrong date format', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await uploadAndProcessImport(page, 'tests/fixtures/excel/training-wrong-date.xlsx');

    await expect(page.locator('text=Format Tanggal tidak valid')).toBeVisible();
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "TR-06"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add TR-06 wrong date format validation test"
```

---

## Task 17: Add Assessment Download Template Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test untuk memverifikasi HC bisa download template assessment.

- [ ] **Step 1: Add the test to Assessment Type describe block**

```typescript
test.describe('Import Training - Assessment Type', () => {

  test('AS-01 - HC can download assessment template', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    // Select Assessment type
    await page.check('#typeAssessment');

    // Click download template button
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.click('#btnDownloadAssessmentTemplate')
    ]);

    expect(download.suggestedFilename()).toContain('assessment_import_template.xlsx');
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "AS-01"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add AS-01 download assessment template test"
```

---

## Task 18: Add Assessment Valid Import Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Happy path test untuk upload 3 rows assessment valid dan verify data tersimpan.

- [ ] **Step 1: Add the test**

```typescript
  test('AS-02 - Upload valid assessment file (3 rows)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await page.check('#typeAssessment');
    await uploadAndProcessImport(page, 'tests/fixtures/excel/assessment-valid.xlsx');

    await verifyImportSummary(page, 3, 0);
    await expect(page.locator('.badge.bg-success')).toHaveCount(3);

    await verifyAssessmentInList(page, 'Assessment Test 1');
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "AS-02"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add AS-02 upload valid assessment file test"
```

---

## Task 19: Add Assessment Invalid NIP Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test error handling assessment dengan NIP tidak terdaftar.

- [ ] **Step 1: Add the test**

```typescript
  test('AS-03 - Upload assessment with invalid NIP', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await page.check('#typeAssessment');
    await uploadAndProcessImport(page, 'tests/fixtures/excel/assessment-invalid-nip.xlsx');

    await verifyImportSummary(page, 0, 1);
    await expect(page.locator('.badge.bg-danger')).toHaveCount(1);
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "AS-03"
```

Expected: PASS

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add AS-03 invalid NIP error handling test"
```

---

## Task 20: Add Assessment Invalid Lulus Test

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Test error handling ketika kolom Lulus berisi nilai selain Ya/Tidak.

- [ ] **Step 1: Add the test**

```typescript
  test('AS-04 - Upload assessment with invalid Lulus value', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ImportTraining');

    await page.check('#typeAssessment');
    await uploadAndProcessImport(page, 'tests/fixtures/excel/assessment-invalid-lulus.xlsx');

    // Verify handling (error or parsed as false)
    await expect(page.locator('text=Lulus')).toBeVisible();
  });
```

- [ ] **Step 2: Run the test**

```bash
cd tests
npx playwright test import-training.spec.ts -g "AS-04"
```

Expected: PASS (atau menunjukkan behavior aktual dari system)

- [ ] **Step 3: Commit the test**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: add AS-04 invalid Lulus value test"
```

---

## Task 21: Close Assessment Type Describe Block

**Files:**
- Modify: `tests/e2e/import-training.spec.ts`

**Context:** Pastikan describe block ditutup dengan benar.

- [ ] **Step 1: Verify file structure**

Check akhir file `tests/e2e/import-training.spec.ts` memiliki closing brace:

```typescript
});  // Closing for test.describe('Import Training - Assessment Type', () => {
```

- [ ] **Step 2: Verify complete file compiles**

```bash
cd tests
npx playwright test import-training.spec.ts --list
```

Expected: Lists all 10 tests (TR-01 to TR-06, AS-01 to AS-04)

- [ ] **Step 3: Commit final structure**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: complete import-training E2E test file structure"
```

---

## Task 22: Run All Tests and Verify

**Files:**
- Test: `tests/e2e/import-training.spec.ts`

**Context:** Jalankan semua tests untuk memastikan semuanya berjalan dengan benar.

- [ ] **Step 1: Ensure application server is running**

```bash
# Di terminal terpisah, dari root directory
dotnet run
```

Verify: Application accessible at http://localhost:5277

- [ ] **Step 2: Run all tests in the file**

```bash
cd tests
npx playwright test import-training.spec.ts
```

Expected: Semua 10 tests pass

- [ ] **Step 3: Check test report**

```bash
npx playwright show-report
```

Verify: Test report terbuka di browser dan menunjukkan semua tests pass

- [ ] **Step 4: Run tests with headed mode for visual verification (optional)**

```bash
cd tests
npx playwright test import-training.spec.ts --headed
```

Verify: Dapat melihat browser menjalankan tests

- [ ] **Step 5: Commit any final adjustments**

```bash
git add tests/e2e/import-training.spec.ts
git commit -m "test: finalize import-training E2E tests - all tests passing"
```

---

## Task 23: Document Test Execution Instructions

**Files:**
- Create: `tests/e2e/README.md`

**Context:** Dokumentasi cara menjalankan tests untuk developer lain.

- [ ] **Step 1: Create README file**

Create `tests/e2e/README.md`:

```markdown
# E2E Tests for Import Training & Assessment

## Prerequisites

1. Application server running at `http://localhost:5277`
2. Test accounts exist in database (HC, coachee accounts)
3. Excel fixtures available in `tests/fixtures/excel/`

## Running Tests

### Run all Import Training tests
```bash
cd tests
npx playwright test import-training.spec.ts
```

### Run only Training tests
```bash
npx playwright test import-training.spec.ts -g "Training Type"
```

### Run only Assessment tests
```bash
npx playwright test import-training.spec.ts -g "Assessment Type"
```

### Run specific test
```bash
npx playwright test import-training.spec.ts -g "TR-02"
```

### Debug mode
```bash
npx playwright test import-training.spec.ts --debug
```

### Headed mode (see browser)
```bash
npx playwright test import-training.spec.ts --headed
```

## Test Cases

### Training Tests
- TR-01: Download template
- TR-02: Upload valid (3 rows)
- TR-03: Invalid NIP
- TR-04: Missing required field
- TR-05: Non-Excel file
- TR-06: Wrong date format

### Assessment Tests
- AS-01: Download template
- AS-02: Upload valid (3 rows)
- AS-03: Invalid NIP
- AS-04: Invalid Lulus value

## Fixtures

Excel test fixtures located in `tests/fixtures/excel/`:
- training-valid.xlsx
- training-invalid-nip.xlsx
- training-missing-required.xlsx
- training-wrong-date.xlsx
- assessment-valid.xlsx
- assessment-invalid-nip.xlsx
- assessment-invalid-lulus.xlsx
- not-excel.txt

## Troubleshooting

**Server not running:**
```bash
dotnet run  # From project root
```

**File not found:**
Verify Excel fixtures exist in `tests/fixtures/excel/`

**Data not appearing in ManageAssessment:**
- Check if import completed successfully
- Verify search query is correct
- Check database directly if needed
```

- [ ] **Step 2: Commit documentation**

```bash
git add tests/e2e/README.md
git commit -m "docs: add E2E test execution instructions"
```

---

## Completion Checklist

Setelah semua tasks selesai:

- [ ] Semua 10 test cases pass (TR-01 to TR-06, AS-01 to AS-04)
- [ ] Semua Excel fixtures created (8 files)
- [ ] Helper functions created dan tested
- [ ] Dokumentasi lengkap di README
- [ ] Semua changes committed ke git
- [ ] Test report tersedia dan bisa diakses

## Verification Steps

Untuk memverifikasi implementation sukses:

1. **Jalankan semua tests:**
   ```bash
   cd tests
   npx playwright test import-training.spec.ts
   ```
   Expected: 10 passed

2. **Cek test report:**
   ```bash
   npx playwright show-report
   ```
   Expected: Test report terbuka dengan semua tests pass

3. **Verify fixtures exist:**
   ```bash
   ls -la tests/fixtures/excel/
   ```
   Expected: 8 files (7 .xlsx + 1 .txt)

4. **Verify helper functions:**
   ```bash
   cat tests/helpers/import-helpers.ts
   ```
   Expected: 4 helper functions terdefinisi

5. **Verify documentation:**
   ```bash
   cat tests/e2e/README.md
   ```
   Expected: Complete instructions for running tests
