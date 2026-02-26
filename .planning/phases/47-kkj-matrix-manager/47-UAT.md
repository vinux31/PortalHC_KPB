---
status: diagnosed
phase: 47-kkj-matrix-manager
source: [47-01-SUMMARY.md, 47-02-SUMMARY.md]
started: 2026-02-26T10:00:00Z
updated: 2026-02-26T11:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Kelola Data nav link (Admin only)
expected: Log in as Admin — "Kelola Data" link with gear icon appears in nav. Log in as non-Admin — link is absent.
result: pass

### 2. Admin authorization guard
expected: While NOT logged in as Admin (or not logged in at all), navigate to /Admin/Index. You are redirected away (to login or home) — NOT shown a 403 error page or the admin page content.
result: pass

### 3. Admin hub page — 12 tool cards
expected: Navigate to /Admin/Index. You see a page titled "Kelola Data" with 3 sections: "Master Data", "Operasional", and "Kelengkapan CRUD". There is 1 active clickable card (KKJ Matrix) and 11 cards marked "Segera" (coming soon stubs).
result: pass

### 4. KkjMatrix read-mode table
expected: Click the KKJ Matrix card (or navigate to /Admin/KkjMatrix). A table loads listing all KkjMatrixItem rows with columns: No, Indeks, Kompetensi, SkillGroup, and an action column. All existing rows from the database are shown.
result: issue
reported: "tapi sebetulnya untuk KKJ matrix itu ada 4 tipe tabel berdasarkan bagian. untuk bagian RFCC,GAST,NGP,DHT/HMU, dan buatkan juga fitur untuk menambah bagian, dan untuk tabelnya saya ingin bisa edit keseluruhan termasuk headernya"
severity: major

### 5. Edit mode toggle
expected: On the KkjMatrix page, click the "Edit" button. The compact read table is replaced by a wide input table where every cell becomes an editable field. The table has horizontal scroll and the first two columns (No and one identifier column) stay sticky/frozen as you scroll right. An "Edit" toolbar with Simpan, Batal, and + buttons appears.
result: issue
reported: "pass, dengan tambahan: 1. ketika tidak dalam mode edit, saya ingin table viewnya full sesuai dengan real tabelnya. 2. apakah ada fasilitas untuk delete row, semisal saya ingin delete row tertentu."
severity: major

### 6. Add empty row
expected: While in edit mode, click the "+" (Add Row) button. A new empty row appears at the bottom of the edit table with blank input fields ready to type into.
result: issue
reported: "saya ingin tombol add row juga seperti delete row, jadi setiap baris ada fasilitas itu, ketika mau menambahkan row tapi di tengah tengah tabel tetap bisa"
severity: major

### 7. TSV clipboard paste from Excel
expected: In Excel (or any spreadsheet), select and copy multiple rows of data (Ctrl+C). In the KkjMatrix edit table, click into a row to focus it, then paste (Ctrl+V). The pasted rows fill into the table starting from the focused row — each tab-separated column goes into the correct input field, and new rows are appended if the pasted data exceeds existing rows.
result: issue
reported: "bisa paste 1 cell, namun tidak bisa klik atau select multi cell. saya ingin seperti excel. bisa select multi cell buat copy paste or delete data"
severity: major

### 8. Bulk save (Simpan)
expected: While in edit mode (with some changes — edit a value, add a row, or paste data), click "Simpan". The page does NOT do a full reload (no visible flash/navigation). The save succeeds, a brief confirmation appears, and the table returns to read mode showing the updated data.
result: issue
reported: "konfirmasi singkat tidak muncul."
severity: minor

### 9. Cancel edit (Batal)
expected: While in edit mode, make some changes (edit a field or add a row), then click "Batal". No save occurs. The table returns to read mode showing the original unchanged data.
result: pass

### 10. Delete with usage guard
expected: In read mode, click the delete button on a KkjMatrixItem that is currently assigned to at least one worker (has UserCompetencyLevel records). You see a blocked/error message that includes the count of workers using it — the item is NOT deleted. Then find an item with no assignments and delete it — a brief confirmation prompt appears, and after confirming, the row disappears from the table without a full page reload.
result: pass

## Summary

total: 10
passed: 5
issues: 5
pending: 0
skipped: 0

## Gaps

- truth: "KkjMatrix page shows separate tables per bagian (RFCC, GAST, NGP, DHT/HMU), with ability to add a new bagian, and all table content including headers is editable"
  status: failed
  reason: "User reported: tapi sebetulnya untuk KKJ matrix itu ada 4 tipe tabel berdasarkan bagian. untuk bagian RFCC,GAST,NGP,DHT/HMU, dan buatkan juga fitur untuk menambah bagian, dan untuk tabelnya saya ingin bisa edit keseluruhan termasuk headernya"
  severity: major
  test: 4
  root_cause: "KkjMatrixItem has no Bagian field; no KkjBagian entity exists in DB; controller returns flat unsegmented list; view renders single table with hardcoded static <th> text (not editable inputs); CpdpItem.Section proves the pattern is known and works elsewhere"
  artifacts:
    - path: "Models/KkjModels.cs"
      issue: "KkjMatrixItem has no Bagian/Section property; no KkjBagian entity defined"
    - path: "Data/ApplicationDbContext.cs"
      issue: "No DbSet<KkjBagian> — entity does not exist in DB"
    - path: "Controllers/AdminController.cs"
      issue: "KkjMatrix GET returns flat list with no grouping; no bagian management endpoints"
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "Single flat @foreach table; edit-mode <thead> has 20 static <th> text (not inputs)"
  missing:
    - "KkjBagian entity (Id, Name, DisplayOrder + header label columns for each Target_* field)"
    - "Bagian string field (or FK) on KkjMatrixItem"
    - "EF migration for new entities"
    - "Per-bagian grouped view — one table section per bagian"
    - "Editable <th> inputs in edit mode (persisted via new KkjBagianSave endpoint)"
    - "Add Bagian button creating new KkjBagian row with default header names"
  debug_session: ".planning/debug/kkj-matrix-bagian-sections.md"

- truth: "After clicking Simpan, a brief success confirmation is shown before returning to read mode"
  status: failed
  reason: "User reported: konfirmasi singkat tidak muncul."
  severity: minor
  test: 8
  root_cause: "btnSave AJAX success callback calls location.reload() immediately with no feedback; Bootstrap 5 Toast infrastructure already exists in layout but is never triggered from client-side save handler"
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "response.success branch on line ~305 only calls location.reload() — no toast shown"
  missing:
    - "Bootstrap Toast shown before reload: new bootstrap.Toast(...).show() then setTimeout(() => location.reload(), 1500)"
  debug_session: ".planning/debug/kkjmatrix-save-no-toast.md"

- truth: "Edit table supports Excel-like multi-cell selection (click+drag or Shift+click), with copy/paste and delete operating on the selected range"
  status: failed
  reason: "User reported: bisa paste 1 cell, namun tidak bisa klik atau select multi cell. saya ingin seperti excel. bisa select multi cell buat copy paste or delete data"
  severity: major
  test: 7
  root_cause: "Native <input type='text'> elements own their mousedown/click events internally for text caret, making drag/Shift+click multi-cell selection architecturally impossible without a custom selection overlay or library. No selectedCells array, no drag-selection listeners, no range copy/delete logic exists anywhere in the view JS."
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "makeRow() uses <input type=text> cells; paste handler uses document.activeElement (single cell only); no selectedCells variable or selection event listeners exist"
  missing:
    - "Custom selection overlay: selectedCells array, mousedown/mousemove on <td>, transparent highlight div, Ctrl+C range-copy, Ctrl+V paste-to-range, Delete range-clear"
    - "Alternative: replace bespoke input table with jSpreadsheet/Handsontable (adds dependency but handles selection natively)"
  debug_session: ".planning/debug/kkj-multi-cell-selection.md"

- truth: "Each row in edit mode has inline + (insert below) and delete buttons — rows can be inserted at any position, not just appended at bottom"
  status: failed
  reason: "User reported: saya ingin tombol add row juga seperti delete row, jadi setiap baris ada fasilitas itu, ketika mau menambahkan row tapi di tengah tengah tabel tetap bisa"
  severity: major
  test: 6
  root_cause: "makeRow() builds exactly 20 data-input <td> cells with no action column; edit table <thead> has no Aksi column; btnAddRow only does appendChild() (always at bottom, no insertBefore logic); feature was never implemented"
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "makeRow() missing 21st action <td>; kkjEditTbl <thead> missing Aksi <th>; btnAddRow handler only appends at bottom"
  missing:
    - "21st 'Aksi' <th> column in edit table <thead>"
    - "Action <td> in makeRow() with insert-below button (tr.parentNode.insertBefore(makeEmptyRow(), tr.nextSibling)) and inline delete button (tr.remove() for Id=0 rows, confirm+AJAX for saved rows)"
  debug_session: ".planning/debug/kkj-edit-table-per-row-insert.md"

- truth: "Read mode shows full table with all columns (not compact 4-column view), and edit mode has a per-row delete button"
  status: failed
  reason: "User reported: ketika tidak dalam mode edit, saya ingin table viewnya full sesuai dengan real tabelnya. apakah ada fasilitas untuk delete row, semisal saya ingin delete row tertentu."
  severity: major
  test: 5
  root_cause: "Read-mode <thead> intentionally hardcoded to 5 columns (No, Indeks, Kompetensi, SkillGroup, Aksi) — 16 columns missing (SubSkillGroup + 15 Target_*). makeRow() in JS builds 20 data cells but no Aksi/delete column for edit mode."
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "Read-mode <thead> and <tbody> only render 5 of 21 columns; makeRow() has no action cell for delete in edit mode"
  missing:
    - "Add 16 missing columns to read-mode <thead> and @foreach <tbody> (SubSkillGroup + Target_1 through Target_15)"
    - "Delete button <td> in makeRow() (covered by gap for per-row insert/delete above)"
  debug_session: ".planning/debug/kkj-read-mode-full-table-delete.md"
