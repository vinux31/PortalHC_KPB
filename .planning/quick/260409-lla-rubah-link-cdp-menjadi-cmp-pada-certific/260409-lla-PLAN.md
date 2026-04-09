---
phase: quick
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/CertificationManagement.cshtml
  - Views/CMP/Shared/_CertificationManagementTablePartial.cshtml
  - Views/CMP/Index.cshtml
autonomous: true
must_haves:
  truths:
    - "URL /CMP/CertificationManagement menampilkan halaman Certification Management"
    - "Filter, pagination, dan export Excel bekerja via /CMP/ endpoints"
    - "Link di CMP/Index.cshtml mengarah ke /CMP/CertificationManagement"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "CertificationManagement, FilterCertificationManagement, ExportSertifikatExcel, GetCascadeOptions (cert), GetSubCategories (cert), BuildSertifikatRowsAsync, MapKategori"
    - path: "Views/CMP/CertificationManagement.cshtml"
      provides: "View dengan fetch URL ke /CMP/"
    - path: "Views/CMP/Shared/_CertificationManagementTablePartial.cshtml"
      provides: "Partial view untuk tabel sertifikat"
---

<objective>
Pindahkan fitur CertificationManagement dari CDPController ke CMPController sehingga URL menjadi /CMP/CertificationManagement.

Purpose: Menu Certification Management secara real sudah masuk ke CMP, tapi backend masih routing ke CDP.
Output: Semua endpoint CertificationManagement aktif di CMPController, views ada di Views/CMP/, link di CMP/Index.cshtml sudah benar.
</objective>

<context>
@Controllers/CDPController.cs (lines 305-334 untuk GetCascadeOptions + GetSubCategories, lines 3523-3881 untuk CertificationManagement + helpers)
@Controllers/CMPController.cs
@Views/CDP/CertificationManagement.cshtml
@Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
@Views/CMP/Index.cshtml
</context>

<tasks>

<task type="auto">
  <name>Task 1: Copy CertificationManagement actions + helpers ke CMPController</name>
  <files>Controllers/CMPController.cs</files>
  <action>
Tambahkan method-method berikut ke CMPController.cs (copy dari CDPController.cs):

1. **GetCascadeOptions** (CDPController line 308-314) — TAPI perlu cek apakah CMPController sudah punya method ini. Jika sudah ada dengan logic yang sama, skip. Jika belum ada atau logic berbeda, tambahkan dengan nama `GetCertCascadeOptions` untuk menghindari konflik, lalu update fetch URL di view juga.

2. **GetSubCategories** (CDPController line 320-332) — Sama seperti di atas, cek dulu apakah sudah ada.

3. **CertificationManagement** action (CDPController line 3523-3561) — Copy as-is. Method ini memanggil BuildSertifikatRowsAsync dan GetCurrentUserRoleLevelAsync yang keduanya sudah ada/akan ditambahkan di CMPController.

4. **FilterCertificationManagement** (CDPController line 3563-3608) — Copy as-is. Return PartialView path "Shared/_CertificationManagementTablePartial" tetap sama (akan ada di Views/CMP/Shared/).

5. **ExportSertifikatExcel** (CDPController line 3610-3666) — Copy as-is dengan [Authorize(Roles = "Admin, HC")].

6. **BuildSertifikatRowsAsync** private method (CDPController line 3676-3881) — Copy as-is.

7. **MapKategori** private static method (CDPController line 3668-3675) — Copy as-is.

PENTING: CMPController sudah punya GetCurrentUserRoleLevelAsync (line 2360) dan akses ke _context.GetSectionUnitsDictAsync — jadi TIDAK perlu copy helper tersebut.

Pastikan semua using yang dibutuhkan sudah ada di CMPController (SertifikatRow, CertificateStatus, CertificationManagementViewModel, RecordType, PaginationHelper, ExcelExportHelper, UserRoles — cek apakah sudah imported).
  </action>
  <verify>
    <automated>dotnet build --no-restore 2>&1 | tail -5</automated>
  </verify>
  <done>CMPController.cs punya semua 5 action methods + 2 helper methods, dotnet build sukses tanpa error</done>
</task>

<task type="auto">
  <name>Task 2: Copy views ke Views/CMP/ dan update semua fetch URL dari /CDP/ ke /CMP/</name>
  <files>Views/CMP/CertificationManagement.cshtml, Views/CMP/Shared/_CertificationManagementTablePartial.cshtml</files>
  <action>
1. Copy `Views/CDP/CertificationManagement.cshtml` ke `Views/CMP/CertificationManagement.cshtml`
2. Buat folder `Views/CMP/Shared/` lalu copy `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` ke `Views/CMP/Shared/_CertificationManagementTablePartial.cshtml`
3. Di `Views/CMP/CertificationManagement.cshtml`, ganti SEMUA occurrence `/CDP/` menjadi `/CMP/`:
   - Line 223, 364: `/CDP/GetCascadeOptions` → `/CMP/GetCascadeOptions` (atau GetCertCascadeOptions jika Task 1 rename)
   - Line 243: `/CDP/GetSubCategories` → `/CMP/GetSubCategories`
   - Line 295: `/CDP/FilterCertificationManagement` → `/CMP/FilterCertificationManagement`
   - Line 390: `/CDP/ExportSertifikatExcel` → `/CMP/ExportSertifikatExcel`
4. Di partial view, cek apakah ada URL /CDP/ yang perlu diganti juga.
  </action>
  <verify>
    <automated>grep -rn "/CDP/" "Views/CMP/CertificationManagement.cshtml" "Views/CMP/Shared/_CertificationManagementTablePartial.cshtml" 2>/dev/null; echo "Exit: $? (0=found CDP refs=BAD, 1=none found=GOOD)"</automated>
  </verify>
  <done>Kedua view file ada di Views/CMP/, tidak ada reference /CDP/ tersisa di dalamnya</done>
</task>

<task type="auto">
  <name>Task 3: Update link di CMP/Index.cshtml</name>
  <files>Views/CMP/Index.cshtml</files>
  <action>
Di Views/CMP/Index.cshtml line 98, ubah:
```html
<a href="@Url.Action("CertificationManagement", "CDP")" class="btn btn-success w-100">
```
menjadi:
```html
<a href="@Url.Action("CertificationManagement", "CMP")" class="btn btn-success w-100">
```
  </action>
  <verify>
    <automated>grep -n 'CertificationManagement.*CDP' "Views/CMP/Index.cshtml"; echo "Exit: $? (1=no CDP ref=GOOD)"</automated>
  </verify>
  <done>Link mengarah ke CMP controller, bukan CDP</done>
</task>

</tasks>

<verification>
1. `dotnet build` sukses
2. Tidak ada reference `/CDP/` di Views/CMP/CertificationManagement.cshtml
3. Link di CMP/Index.cshtml mengarah ke CMP controller
</verification>

<success_criteria>
- URL /CMP/CertificationManagement bisa diakses dan menampilkan data sertifikat
- Filter, pagination, export bekerja melalui /CMP/ endpoints
- Tombol di halaman CMP Index mengarah ke /CMP/CertificationManagement
</success_criteria>
