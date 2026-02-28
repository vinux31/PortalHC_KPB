# Phase 69: ManageWorkers Migration to Admin - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Pindahkan seluruh fitur ManageWorkers (CRUD pekerja, import, export, detail) dari CMPController ke AdminController. Hapus total kode lama di CMP (tanpa redirect). Tambah kartu akses di Kelola Data hub. Ekstrak role-to-SelectedView mapping ke shared helper `UserRoles.GetDefaultView()`.

</domain>

<decisions>
## Implementation Decisions

### Penghapusan Route Lama
- Hapus total semua route ManageWorkers dari CMPController — **tidak ada 301 redirect** (override roadmap SC #2)
- Hapus view folder CMP/ManageWorkers setelah migrasi — clean break, git history menyimpan backup
- Cari dan update semua referensi internal (views, JavaScript, AJAX calls) ke URL baru `/Admin/ManageWorkers`
- Hanya hapus ManageWorkers dari CMPController — CMP punya action lain yang tetap ada

### Navigasi & Kelola Data Hub
- Tambah kartu "Manajemen Pekerja" di **Section A: Master Data**, posisi **pertama** (sebelum KKJ Matrix)
- Style ikon dan deskripsi mengikuti pola kartu lain di hub (Claude sesuaikan)
- Hapus tombol standalone "Kelola Pekerja" dari navbar — tanpa notifikasi transisi
- Akses ManageWorkers hanya untuk role **Admin** dan **HC**
- Claude investigasi apakah ada link lain di aplikasi yang mengarah ke ManageWorkers lama

### Permission & Role Guard
- Admin dan HC punya akses identik (full CRUD) — tidak ada action yang Admin-only
- Ikut controller-level `[Authorize]` attribute di AdminController — tidak perlu attribute terpisah per action

### GetDefaultView() Helper
- Ekstrak mapping role → SelectedView yang ada **tanpa perubahan logic** ke `UserRoles.GetDefaultView(role)`
- Mapping saat ini: Admin→"Admin", HC→"HC", Coach→"Coach", management roles→"Atasan", default→"Coachee"
- Helper dipanggil dari 3 tempat: create worker, edit worker, import worker
- Hanya ada di CMPController saat ini, tidak ada di controller lain

### Import/Export
- Pertahankan format **Excel (.xlsx)** via ClosedXML — tidak ada perubahan format
- Semua fitur ikut migrasi: import, export, download template
- Template import tetap 10 kolom: Nama, Email, NIP, Jabatan, Bagian, Unit, Directorate, Role, Tgl Bergabung, Password

### Migration Scope & Style
- **Pindah + perbaiki** — boleh perbaiki bug kecil yang ditemukan selama migrasi
- URL pattern tetap `/Admin/ManageWorkers/*` — tidak di-rename
- Visual views **disesuaikan dengan style Admin hub** (bukan copy apa adanya)
- Update breadcrumb ke navigasi Admin baru
- Nama file view dipertahankan (CreateWorker.cshtml, EditWorker.cshtml, dll)

### View Structure
- Claude tentukan: flat di Views/Admin/ atau subfolder Views/Admin/ManageWorkers/
- Claude investigasi partial view dependencies

### Testing & Validasi
- Claude boleh jalankan `dotnet build` dan `dotnet run` untuk verifikasi otomatis
- Yang bisa di-automate → Claude test langsung (compile, routing, referensi URL)
- Yang butuh browser → Claude siapkan checklist manual terpisah
- **Wajib:** grep seluruh codebase untuk memastikan tidak ada referensi CMP/ManageWorkers tersisa

### Claude's Discretion
- Ikon dan deskripsi kartu Manajemen Pekerja di hub
- Visibility kartu untuk role non-Admin/HC (sembunyikan atau disable)
- Flat vs subfolder untuk view structure
- Partial view dependencies handling
- Bug fixes yang ditemukan selama migrasi
- Link lain yang perlu diupdate (investigasi saat research)

</decisions>

<specifics>
## Specific Ideas

- Semua label dan bahasa di UI tetap menggunakan Bahasa Indonesia
- Kartu "Manajemen Pekerja" — bukan "Kelola Pekerja" atau "Data Pekerja"
- Clean break dari CMP — tidak ada backward compatibility, tidak ada redirect
- Breadcrumb format: "Admin > Kelola Data > Manajemen Pekerja" (atau serupa)

</specifics>

<deferred>
## Deferred Ideas

- **Bedakan SelectedView untuk Section Head vs Sr Supervisor/Supervisor** — Section Head bisa lihat seluruh section, Sr Supervisor/Supervisor hanya unit-nya. Ini perubahan scope data, bukan hanya dashboard view. Fase terpisah.
- **Integrasi Active Directory Pertamina** — Saat aplikasi naik ke server perusahaan, autentikasi bisa via SSO AD. SelectedView tetap di level aplikasi, tidak terpengaruh AD. Fase terpisah.

</deferred>

---

*Phase: 69-manageworkers-migration-to-admin*
*Context gathered: 2026-02-28*
