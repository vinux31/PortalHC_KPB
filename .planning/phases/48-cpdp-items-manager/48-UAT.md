---
status: complete
phase: 48-cpdp-items-manager
source: [48-01-SUMMARY.md, 48-02-SUMMARY.md, 48-03-SUMMARY.md]
started: 2026-02-26T14:20:00Z
updated: 2026-02-26T14:50:00Z
---

## Current Test
<!-- OVERWRITE each test - shows where we are -->

[testing complete]

## Tests

### 1. Admin Index card activation
expected: Go to /Admin. The CPDP Items card should now be titled "KKJ-IDP Mapping Editor", have a working link to /Admin/CpdpItems, and have no "Segera" badge or dimmed opacity.
result: pass

### 2. Navigate to CpdpItems read-mode page
expected: Clicking the card (or going to /Admin/CpdpItems) loads the page. Breadcrumb shows "Kelola Data > KKJ-IDP Mapping Editor". A section dropdown (RFCC/GAST/NGP/DHT) is visible. A read-mode table shows columns: No, Nama Kompetensi, Indikator Perilaku, Aksi. Row count ("N baris") is displayed.
result: issue
reported: "seharusnya tabelnya muncul 'No    Nama Kompetensi    Indikator Perilaku (Level)    Detail Indikator Perilaku    Individual Development Plan / Silabus    Target Deliverable'"
severity: major

### 3. Section dropdown filtering
expected: Selecting a section (e.g., RFCC) in the dropdown instantly filters the table to show only rows with that section — no page reload. The row count updates to reflect the filtered count. Selecting a different section switches the view.
result: pass

### 4. Enter and exit edit mode
expected: Clicking the Edit button hides the read-mode table and shows a 7-column edit table (No, Nama Kompetensi, Indikator Perilaku, Detail Indikator, Silabus, Target Deliverable, Status + Aksi) with horizontal scroll. The section filter still works in edit mode. Clicking Batal returns to read mode.
result: pass

### 5. Insert new row and bulk save
expected: In edit mode, clicking the insert-below button (on any row) adds a new empty row below it with the correct section pre-filled. Filling in the fields and clicking Simpan shows a Bootstrap Toast ("Berhasil disimpan" or similar) for ~2 seconds, then the page reloads with the new row appearing in the read table.
result: pass

### 6. Delete a row with reference guard
expected: In edit mode, clicking the Hapus/delete icon on a row with Id>0 sends an AJAX delete request. If the item is referenced by an IdpItem, the delete should be blocked with an error message. If not referenced, it is deleted successfully and the row disappears (page reloads or row removed).
result: issue
reported: "pass, tapi saya ingin tanya, kamu bilang jika item direferensikan oleh IDPItem delete diblokir, apakah HC tetap tidak bisa delete?, saya ingin HC tetap bisa delete"
severity: major

### 7. CMP/Mapping section selector uses dropdown
expected: Navigate to /CMP/Mapping (or the section selection page). Instead of animated cards, there is now a compact dropdown + "Lihat" button. The Lihat button is disabled until a section is selected. Selecting a section and clicking Lihat navigates to the correct /CMP/Mapping?section=X URL.
result: pass

### 8. Multi-cell selection — click and shift+click
expected: In edit mode, clicking a cell's input highlights it with a blue background. Shift+clicking another cell selects the rectangular range between the two — all cells in that range get the blue highlight. Clicking a cell elsewhere clears the previous selection.
result: pass

### 9. Clipboard copy (Ctrl+C)
expected: Select a range of cells in edit mode. Press Ctrl+C. The selected cell values should be copied to the clipboard as tab-separated values (TSV). Pasting into Excel or a text editor should show the values arranged in columns/rows matching the selection.
result: pass

### 10. Clipboard paste (Ctrl+V)
expected: Copy a tab-separated block of text (e.g., from Excel). In edit mode, click a cell to set the starting position. Press Ctrl+V. The pasted values should fill cells starting from the selected cell, flowing right then down, matching the TSV layout.
result: pass

### 11. Delete key clears selected cells
expected: In edit mode, select one or more cells (blue highlight). Press the Delete key (not Backspace). The selected cells' input values should be cleared/emptied without removing the rows.
result: issue
reported: "pass, dengan catatan: jika saya select lebih dari 1 cell terus tekan delete, yang terhapus adalah cell paling awal saja, lainnya tidak"
severity: major

### 12. Excel export button
expected: On the read-mode page, an "Export Excel" button (green outline) is visible in the toolbar. Clicking it downloads a .xlsx file named CPDP_Items_All.xlsx (or CPDP_Items_{section}.xlsx when a section is filtered). The file should contain all 8 data columns with a dark header row.
result: pass

## Summary

total: 12
passed: 9
issues: 3
pending: 0
skipped: 0

## Gaps

- truth: "Read-mode table shows all 6 data columns: No, Nama Kompetensi, Indikator Perilaku (Level), Detail Indikator Perilaku, Individual Development Plan / Silabus, Target Deliverable"
  status: failed
  reason: "User reported: seharusnya tabelnya muncul 'No    Nama Kompetensi    Indikator Perilaku (Level)    Detail Indikator Perilaku    Individual Development Plan / Silabus    Target Deliverable'"
  severity: major
  test: 2
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "HC (Admin) dapat menghapus CpdpItem meskipun direferensikan oleh IdpItem — tidak ada reference guard yang memblokir delete"
  status: failed
  reason: "User reported: saya ingin HC tetap bisa delete meskipun item direferensikan oleh IDPItem"
  severity: major
  test: 6
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Menekan Delete pada multi-cell selection menghapus isi semua cell yang dipilih, bukan hanya cell pertama"
  status: failed
  reason: "User reported: jika saya select lebih dari 1 cell terus tekan delete, yang terhapus adalah cell paling awal saja, lainnya tidak"
  severity: major
  test: 11
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
