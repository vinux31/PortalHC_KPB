# Phase 47: KKJ Matrix Manager - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin dapat view, create, edit, dan delete KkjMatrixItem records melalui dedicated management page di AdminController. Ini adalah phase pertama yang mendirikan infrastruktur Admin Portal (controller, nav, index page) yang akan dipakai oleh seluruh v2.3 phases.

</domain>

<decisions>
## Implementation Decisions

### Admin Portal Infrastructure (berlaku untuk semua phases 47-58)
- **Controller baru:** `AdminController` dengan routes `/Admin/*` — semua 12 tool admin di satu controller
- **Nama portal:** "Data Management" / "Kelola Data" — muncul di nav sidebar, page titles, dan headings
- **Navigation:** Menu item baru di sidebar, hanya visible untuk Admin role
- **Admin index page:** `/Admin/Index` — landing page yang daftar semua 12 tools dengan deskripsi singkat
- **Index grouping:** 3 section di index page: **Master Data** (Cat A: MDAT-01–03), **Operational** (Cat B: OPER-01–05), **CRUD Completions** (Cat C: CRUD-01–04)

### Table Layout (read mode)
- Tabel compact: hanya tampilkan `No`, `Indeks`, `Kompetensi`, `SkillGroup` + tombol aksi (Edit/Delete)
- Target_* columns tidak tampil di read mode — terlalu lebar

### Edit & Create Interaction — Spreadsheet/Excel Mode
- **Toggle edit mode:** Tombol "Edit" di atas tabel; saat di-klik, seluruh tabel menjadi editable
- **Edit mode:** Semua 18 kolom ditampilkan (termasuk 13 Target_* columns) dengan horizontal scroll — admin edit langsung di cell input fields
- **Copy-paste support:** Admin bisa copy data dari Excel dan paste ke tabel (multi-row paste via clipboard)
- **Save:** Tombol "Simpan" (Submit) yang menyimpan semua perubahan sekaligus ke server, lalu mengunci tabel kembali ke read mode
- **Create:** Saat edit mode aktif, ada baris kosong di bawah tabel untuk menambah row baru — submitted bersama dengan edits lainnya
- **Cancel:** Tombol "Batal" untuk discard semua perubahan dan kembali ke read mode

### Delete Guard
- Cek referensi ke `UserCompetencyLevel` sebelum delete
- **Jika ada referensi: BLOCK** — tampilkan pesan error dengan jumlah worker yang terpengaruh, tidak bisa dihapus
- **Jika tidak ada referensi:** Delete langsung dengan konfirmasi singkat

### Claude's Discretion
- Implementasi copy-paste multi-row (contenteditable vs input fields, Tab/Enter navigation)
- Exact styling tabel wide mode (column widths untuk Target_* columns)
- Error display positioning (toast vs inline)
- Pagination vs full list (KKJ Matrix items mungkin 50-100+ rows)

</decisions>

<specifics>
## Specific Ideas

- "Saya suka seperti Excel tabel, edit sedikit nyaman, edit copy paste banyak data juga nyaman" — admin sering input data massal dari spreadsheet
- Edit mode: ada tulisan/button "Edit" di atas tabel, klik → tabel aktif editable, klik "Submit" → simpan dan kunci
- Index page adalah hub dengan 12 cards/links yang dikelompokkan per kategori

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 47-kkj-matrix-manager*
*Context gathered: 2026-02-26*
