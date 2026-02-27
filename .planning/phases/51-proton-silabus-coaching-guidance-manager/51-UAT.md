---
status: complete
phase: 51-proton-silabus-coaching-guidance-manager
source: [51-01-SUMMARY.md, 51-02-SUMMARY.md, 51-03-SUMMARY.md]
started: 2026-02-27T07:30:00Z
updated: 2026-02-27T08:25:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Admin/Index Hub Card
expected: Navigate to /Admin. Section A shows "Silabus & Coaching Guidance" card with bi-book icon. Section B does NOT show old "Proton Track Assignment" card. Clicking the Silabus card navigates to /ProtonData.
result: pass

### 2. ProtonData Page Loads with Two Tabs
expected: /ProtonData loads with two Bootstrap nav-tabs: "Silabus" (active by default) and "Coaching Guidance". Page is accessible when logged in as Admin or HC role.
result: pass

### 3. Silabus Cascade Filter
expected: In Silabus tab, select a Bagian (e.g., "RFCC") — Unit dropdown populates with matching units. Select a Unit and Track — "Muat Data" button triggers page reload with query params in URL (?bagian=...&unit=...&trackId=...).
result: pass

### 4. Silabus Empty State and Edit Mode
expected: With filter applied but no data, page shows empty state message. Click "Edit" — edit toolbar appears with "Tambah Baris" button. Click "Tambah Baris" — a new row with empty input fields appears.
result: pass

### 5. Silabus Add and Save Data
expected: In edit mode, add 3 rows with same Kompetensi "K1", same SubKompetensi "SK1", different Deliverables "D1", "D2", "D3". Click "Simpan Semua" — page reloads and shows the saved data in view mode.
result: pass
note: retry after blocker fix (commits 8bad9e0, 2fb5dce)

### 6. Silabus View Mode Rowspan
expected: After saving 3 rows with same Kompetensi/SubKompetensi, view mode displays K1 merged vertically (rowspan 3) and SK1 merged vertically (rowspan 3), with 3 individual Deliverable rows.
result: pass

### 7. Silabus Inline Add Row
expected: Click Edit. Click the "+" button on any row — a new row is inserted AFTER the current row with Kompetensi and SubKompetensi copied from the current row, empty Deliverable field.
result: pass

### 8. Silabus Delete Row
expected: Click Edit. Click the trash icon on a saved row — Bootstrap modal asks for confirmation. Confirm — row is removed. After saving, the deleted row no longer appears.
result: pass

### 9. Coaching Guidance Filter and Empty State
expected: Switch to "Coaching Guidance" tab. Select Bagian, Unit, and Track — file table area loads (AJAX). With no files uploaded, shows "Belum ada file coaching guidance" message and upload form.
result: pass

### 10. Coaching Guidance File Upload
expected: In Coaching Guidance tab with filter selected, choose a PDF/Word/Excel/PPT file (< 10MB) and click Upload. File appears in the table with columns: Nama File, Ukuran, Tanggal Upload, and action buttons (Download, Replace, Delete).
result: pass

### 11. Coaching Guidance File Download
expected: Click the Download button on an uploaded file. Browser downloads the file with its original filename (not the server-side GUID name).
result: pass

### 12. Coaching Guidance File Replace and Delete
expected: Click Replace (arrow-repeat icon) on a file — modal appears with file input. Upload a replacement file — table updates with new filename/size. Then click Delete (trash icon) on a file — confirmation modal appears. Confirm — file removed from table.
result: pass

### 13. ProtonCatalog Redirect
expected: Navigate to /ProtonCatalog in the browser — page redirects (302) to /ProtonData.
result: pass

## Summary

total: 13
passed: 13
issues: 0
pending: 0
skipped: 0

## Gaps

- truth: "Silabus Save All persists data and reloads page with saved rows"
  status: fixed
  reason: "Two bugs: (1) JS redirect used /ProtonData instead of /ProtonData/Index — 404 due to route default action=Login. (2) Orphan cleanup deleted newly created deliverables — newDelivIds not tracked in savedDelivIds."
  severity: blocker
  test: 5
  root_cause: "Route default action is Login not Index; orphan cleanup missing newDelivIds tracking"
  artifacts:
    - path: "Views/ProtonData/Index.cshtml"
      issue: "Redirect URL missing /Index segment"
    - path: "Controllers/ProtonDataController.cs"
      issue: "newDelivIds not tracked for orphan cleanup"
  missing: []
  debug_session: ""
