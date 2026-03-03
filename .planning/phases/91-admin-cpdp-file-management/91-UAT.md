---
status: complete
phase: 92-admin-cpdp-file-management
source: [92-01-SUMMARY.md, 92-02-SUMMARY.md]
started: 2026-03-03T03:10:00Z
updated: 2026-03-03T03:20:00Z
---

## Current Test

[testing complete]

## Tests

### 1. CPDP Hub Card on Admin Index
expected: Navigate to Admin/Index (/Admin). In the Admin/HC section, a "CPDP File Management" card appears between the KKJ Matrix card and the KKJ-IDP Mapping card.
result: pass

### 2. Navigate to CPDP Files Page
expected: Clicking the CPDP File Management hub card opens /Admin/CpdpFiles. The page shows a tabbed Bootstrap layout with one tab per bagian (unit). Each tab shows a list of uploaded files for that bagian.
result: pass

### 3. Upload a CPDP File
expected: Click the Upload link/button from CpdpFiles page. The CpdpUpload page shows a drag-drop upload form with a bagian selector. Upload a PDF or Excel file (max 10MB). After submit, you are redirected back to CpdpFiles and the file appears in the list for that bagian.
result: pass

### 4. Upload Validation — Reject Invalid File
expected: On the CpdpUpload page, try uploading a file that is NOT PDF or Excel (e.g., a .txt or .png). The upload is rejected with a validation error. Also, files over 10MB should be rejected.
result: pass

### 5. Download a CPDP File
expected: On CpdpFiles page, click the Download button on an uploaded file. The file downloads with the correct content type (PDF opens as PDF, Excel opens as Excel).
result: pass

### 6. Archive (Soft-Delete) a CPDP File
expected: On CpdpFiles page, click Archive on a file. An AJAX call soft-deletes the file (sets IsArchived=true). The file disappears from the active file list without a full page reload.
result: pass

### 7. View Archived Files (CpdpFileHistory)
expected: From CpdpFiles page, click the History link. The CpdpFileHistory page shows previously archived files with Download buttons and a back-link to CpdpFiles (preserving the bagian param).
result: pass

### 8. Bagian Delete Guard — Dual Check
expected: Try to delete a bagian that has CPDP files associated with it. Deletion is blocked with a message showing per-type counts like "(KKJ: X, CPDP: Y)".
result: pass

## Summary

total: 8
passed: 8
issues: 0
pending: 0
skipped: 0

## Gaps

[none]
