---
status: complete
phase: 77-training-record-redirect-fix
source: 77-01-SUMMARY.md, 77-02-SUMMARY.md, 77-03-SUMMARY.md
started: 2026-03-01T12:00:00Z
updated: 2026-03-01T12:30:00Z
---

## Current Test

[testing complete]

## Tests

### 1. ManageAssessment 3-Tab Layout
expected: Navigate to /Admin/ManageAssessment. Page title is "Manage Assessment & Training". Three tabs visible: Assessment Groups, Training Records, History. Assessment Groups tab is active by default. Existing assessment table, search, and pagination work as before.
result: pass

### 2. Training Records Tab — Filter & Worker List
expected: Click "Training Records" tab. Default state shows "Pilih filter untuk menampilkan data pekerja" message. Select a filter (e.g., Bagian/Section) and submit. Worker list table appears with columns: No, Nama, Nopeg, Jabatan, Unit, Status Training, Aksi.
result: pass

### 3. Training Records Tab — Expand Worker Row
expected: Click expand button on a worker row. Inline section appears below the row showing that worker's manual training records in a table with columns: Tanggal, Tipe (green badge), Judul, Penyelenggara, Status, Berlaku Sampai, Aksi. Edit and Delete buttons visible on each record. "Lihat Detail" link also visible.
result: pass

### 4. Tambah Training Form
expected: Click "Tambah Training" button on Training Records tab. Navigates to /Admin/AddTraining page. Breadcrumb shows: Admin > Kelola Data > Manage Assessment & Training > Tambah Training. Form has worker dropdown, all training fields (Judul, Penyelenggara, Kota, Kategori, Tanggal, etc.), certificate upload. Cancel button returns to ManageAssessment?tab=training.
result: pass

### 5. Add Training Record (Submit)
expected: Fill out the AddTraining form with valid data and submit. Redirects to /Admin/ManageAssessment?tab=training with a green success message "Training record berhasil dibuat." The new record appears in the worker's expanded row.
result: pass

### 6. Edit Training Form
expected: From an expanded worker row, click Edit on a manual training record. Navigates to /Admin/EditTraining/{id}. Form is pre-populated with existing data. Worker name shown as read-only. If certificate exists, download link visible. Cancel returns to ManageAssessment?tab=training.
result: pass

### 7. Edit Training Record (Submit)
expected: Modify a field in the EditTraining form and submit. Redirects to /Admin/ManageAssessment?tab=training with success message "Training record berhasil diperbarui." Changes visible in the expanded row.
result: pass

### 8. Delete Training Record
expected: From an expanded worker row, click Delete on a manual training record. Browser confirm dialog appears "Hapus training record ini?". Click OK. Redirects to ManageAssessment?tab=training with success message "Training record berhasil dihapus." Record no longer visible.
result: pass

### 9. History Tab
expected: Click "History" tab. Two sub-tabs visible: Riwayat Assessment and Riwayat Training. Riwayat Assessment shows assessment history table with worker/title filter. Riwayat Training shows training history table. Data populates correctly.
result: pass

### 10. Tab Persistence via URL
expected: Navigate directly to /Admin/ManageAssessment?tab=training — Training Records tab is active. Navigate to /Admin/ManageAssessment?tab=history — History tab is active. Navigate to /Admin/ManageAssessment (no param) — Assessment Groups tab is active.
result: pass

### 11. CMP/Records — Personal View Only
expected: Navigate to /CMP/Records as any role (Admin, HC, or regular user). All users see their personal training records view (Records.cshtml). No worker list management view appears regardless of role.
result: pass

### 12. Hub Card & HC Access
expected: Log in as HC user. Navigate to Admin/Index (Kelola Data hub). The "Manage Assessment & Training" card is visible with updated description mentioning training records. Clicking it navigates to ManageAssessment page. All assessment actions (Create, Edit, Delete, Monitor, Export, Audit Log) are accessible to HC user.
result: pass

### 13. Breadcrumbs Updated
expected: Navigate to Admin/AuditLog, Admin/CreateAssessment, Admin/EditAssessment (any), Admin/AssessmentMonitoringDetail, Admin/UserAssessmentHistory. All breadcrumbs show "Manage Assessment & Training" (not "Manage Assessments"). Back/cancel buttons link to ManageAssessment.
result: pass

## Summary

total: 13
passed: 13
issues: 0
pending: 0
skipped: 0

## Gaps

[none]
