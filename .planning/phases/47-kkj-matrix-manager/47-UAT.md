---
status: diagnosed
phase: 47-kkj-matrix-manager
source: [47-03-SUMMARY.md, 47-04-SUMMARY.md, 47-05-SUMMARY.md]
started: 2026-02-26T12:00:00Z
updated: 2026-02-26T12:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Per-bagian sections in read mode
expected: Navigate to /Admin/KkjMatrix. The page shows 4 separate table sections — one each for RFCC, GAST, NGP, and DHT/HMU. Each section has a bold colored heading with the bagian name and a badge showing how many rows are in it. Items appear under their assigned bagian section.
result: issue
reported: "maksut saya adalah page KKJMatrix tetap menampilkan hanya 1 tabel saja, nah tapi diatas ada fasilitas filter dropdown untuk memilih table bagian mana yang akan ditampilkan. dan juga di kanan filter ini ada fasilitas juga untuk mengedit,hapus,menambah bagian. jadi halaman KKJmatrix hanya menampilkan 1 tabel sesuai filter yang dipilih"
severity: major

### 2. Full 21-column table in read mode
expected: In read mode, each bagian's table has 21 columns — No, Indeks, Kompetensi, SkillGroup, SubSkillGroup, then 15 Target_* columns, then Aksi. You can scroll horizontally to see all columns. All data values are visible (not just the first 5 columns).
result: pass

### 3. Column headers use editable bagian labels
expected: The column headers for the 15 Target_* columns show label text (e.g. "Section Head", "Sr Spv GSH", etc.) that comes from the KkjBagian record — not hardcoded text. Each bagian section could theoretically show different labels.
result: pass

### 4. Edit mode: per-bagian tables with editable header inputs
expected: Click the Edit button. For each bagian (RFCC, GAST, etc.) a separate editable table appears. In the table header row, each of the 15 Target_* column headers is an editable text input (not static text). You can click inside and change the label text.
result: pass

### 5. Add Bagian button creates a new section
expected: While in edit mode, click the "Tambah Bagian" button (in the toolbar). A new table section appears at the bottom of the edit container with the name "Bagian Baru". The new section has its own editable header inputs and an add-row button.
result: pass

### 6. Save persists column header edits
expected: While in edit mode, change one of the column header label inputs (e.g. change "Section Head" to "Section Leader" for RFCC). Click Simpan. After the page reloads, navigate back to edit mode — the header input still shows "Section Leader" (not reverted to the default).
result: issue
reported: "ketika edit Kolom Target, ke Section Leader, muncul error notif 'Error menyimpan baris: Tidak ada data yang diterima.'"
severity: major

### 7. Insert-below button adds row at exact position
expected: While in edit mode, find an existing row in a bagian table. Click the green + (insert below) button in that row's Aksi column. A new empty row appears immediately below that row — NOT appended at the bottom of the table. The new row belongs to the same bagian.
result: issue
reported: "ketika masuk edit mode, yang muncul dan bisa diedit cuman kolom target, kolom data tidak muncul"
severity: major

### 8. Inline delete — unsaved vs saved rows
expected: (a) Click insert-below to create a new empty row (Id=0). Click the red trash button on that new row — the row disappears immediately with NO confirmation dialog. (b) Click the red trash button on an existing saved row (has data, Id > 0) — a confirmation dialog appears. After confirming, the row is removed from the table without a full page reload.
result: skipped
reason: skipped — fixing issues first

### 9. Multi-cell drag selection
expected: In edit mode, find a table cell (click on the cell area, not directly into the input box). Hold and drag across multiple cells. A blue-tinted highlight (.cell-selected) appears on all cells in the selected range. Releasing the mouse keeps the selection highlighted.
result: skipped
reason: skipped — fixing issues first

### 10. Delete key clears selected range
expected: With multiple cells highlighted (from drag selection), press the Delete key. All the selected cells' input values are cleared to empty string. Non-selected cells are unaffected.
result: skipped
reason: skipped — fixing issues first

### 11. Ctrl+C copies selected range as TSV
expected: Select a range of cells using drag or Shift+click. Press Ctrl+C. Open Notepad (or any text editor) and press Ctrl+V. You should see the copied data as tab-separated values — rows separated by newlines, columns by tabs — matching the selected range.
result: skipped
reason: skipped — fixing issues first

### 12. Ctrl+V pastes from clipboard into selected position
expected: Copy a small range from Excel (e.g. 2 rows × 3 columns) using Ctrl+C in Excel. In KkjMatrix edit mode, click on a cell to select it as the anchor. Press Ctrl+V. The clipboard data fills in from that anchor cell — rows go downward, columns go rightward, filling the corresponding input fields.
result: skipped
reason: skipped — fixing issues first

### 13. Bootstrap Toast confirmation after Simpan
expected: While in edit mode, make any change (edit a cell value or change a header label). Click Simpan. A green toast notification appears in the bottom-right corner of the screen showing "Data berhasil disimpan." with a checkmark icon. After about 1.5 seconds, the toast fades and the page reloads showing the updated data.
result: skipped
reason: skipped — fixing issues first

## Summary

total: 13
passed: 4
issues: 3
pending: 0
skipped: 6

## Gaps

- truth: "KkjMatrix page shows one table at a time filtered by selected bagian, with a dropdown to switch bagians and controls to add/edit/delete bagians next to the filter"
  status: failed
  reason: "User reported: maksut saya adalah page KKJMatrix tetap menampilkan hanya 1 tabel saja, nah tapi diatas ada fasilitas filter dropdown untuk memilih table bagian mana yang akan ditampilkan. dan juga di kanan filter ini ada fasilitas juga untuk mengedit,hapus,menambah bagian. jadi halaman KKJmatrix hanya menampilkan 1 tabel sesuai filter yang dipilih"
  severity: major
  test: 1
  root_cause: "Read-mode Razor @foreach renders all bagian sections simultaneously with no filter. No dropdown, no show/hide logic, no bagian CRUD controls in read mode. Controller already provides all needed data (ViewBag.Bagians + kkjBagians JS var) — only the view needs to change. A new KkjBagianDelete controller action is also needed."
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "#readTable uses static Razor @foreach — replace with dropdown toolbar + single #readTablePanel div populated by renderReadTable(bagianName) JS function"
    - path: "Controllers/AdminController.cs"
      issue: "Missing KkjBagianDelete POST action (needed for delete bagian CRUD control)"
  missing:
    - "Replace #readTable Razor multi-section block with dropdown filter + CRUD buttons toolbar + #readTablePanel div"
    - "Add renderReadTable(bagianName) JS function that builds single table from kkjItems/kkjBagians arrays"
    - "Wire bagianFilter change event to call renderReadTable"
    - "Add KkjBagianDelete POST action to AdminController (with guard: block if items assigned to bagian)"
    - "Wire Ubah Nama button to prompt + KkjBagianSave; wire Tambah Bagian to KkjBagianAdd; wire Hapus to KkjBagianDelete"
  debug_session: ""

- truth: "Edit mode menampilkan semua baris data yang ada (kolom No, SkillGroup, SubSkillGroup, Indeks, Kompetensi, Target_* sebagai input fields) untuk setiap bagian"
  status: failed
  reason: "User reported: ketika masuk edit mode, yang muncul dan bisa diedit cuman kolom target, kolom data tidak muncul"
  severity: major
  test: 7
  root_cause: "renderEditRows() filters kkjItems with strict i.Bagian === bagian.Name. Existing KkjMatrixItem records have Bagian = '' (EF migration default) — they don't match any bagian name so zero rows are rendered in every tbody. Read-mode has a Razor 'Tidak Terkategori' fallback but edit-mode JS has no equivalent."
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "renderEditRows() line ~299: strict equality filter drops all items with Bagian='' — need to include orphan items on first bagian or in separate unassigned section"
  missing:
    - "In renderEditRows(), make the first bagian's filter also include items where Bagian is empty or not in any known bagian name (Strategy A — orphans appear in first bagian's edit table so user can assign them)"
    - "Alternatively: after the kkjBagians.forEach loop, render a separate 'Tidak Terkategori' edit section for orphan items"
  debug_session: ""

- truth: "Simpan berhasil menyimpan perubahan header kolom bagian (KkjBagianSave) dan data baris (KkjMatrixSave) tanpa error"
  status: failed
  reason: "User reported: ketika edit Kolom Target, ke Section Leader, muncul error notif 'Error menyimpan baris: Tidak ada data yang diterima.'"
  severity: major
  test: 6
  root_cause: "btnSave always calls KkjMatrixSave unconditionally after KkjBagianSave succeeds, even when collectRows() returns []. AdminController.KkjMatrixSave has a hard guard rejecting empty arrays with 'Tidak ada data yang diterima.' message. Fix is client-side: skip KkjMatrixSave when rows.length === 0 and show success toast directly."
  artifacts:
    - path: "Views/Admin/KkjMatrix.cshtml"
      issue: "btnSave handler calls KkjMatrixSave unconditionally — add rows.length === 0 guard before the AJAX call"
  missing:
    - "In btnSave success callback (after KkjBagianSave), add: if (rows.length === 0) { show toast then reload; return; } before firing KkjMatrixSave"
  debug_session: ""
