# Phase 260: Auto-cascade perubahan nama OrganizationUnit ke semua user records dan template - Context

**Gathered:** 2026-03-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Ketika admin mengubah nama atau memindah (reparent) OrganizationUnit di ManageOrganization, semua data terkait yang menyimpan nama organisasi sebagai string (denormalized) harus ikut terupdate secara otomatis. Termasuk memperbaiki hardcoded nama Bagian di template import.

</domain>

<decisions>
## Implementation Decisions

### Cascade Timing
- **D-01:** Cascade terjadi **langsung saat rename** dalam satu transaksi database. Tidak perlu konfirmasi tambahan atau background job.

### Scope Field yang Di-cascade
- **D-02:** `ApplicationUser.Section` dan `ApplicationUser.Unit` — field utama yang dipakai filtering di seluruh aplikasi.
- **D-03:** `CoachCoacheeMapping.AssignmentSection` dan `CoachCoacheeMapping.AssignmentUnit` — field override yang bisa berbeda dari user.Section.
- **D-04:** `ApplicationUser.Directorate` tetap **free-text** — tidak terhubung ke OrganizationUnit, tidak perlu cascade.

### Template Import Dinamis
- **D-05:** Hardcoded nama Bagian di `DownloadImportTemplate` (`"RFCC / DHT / HMU / NGP / GAST"`) diganti dengan **query dinamis** dari `GetAllSectionsAsync()`.

### Notifikasi
- **D-06:** Setelah rename/reparent, tampilkan **TempData flash message**: "Nama diubah. X user dan Y mapping terupdate." Tidak perlu audit trail ke database.

### Deactivate Handling
- **D-07:** **Blokir deactivate** OrganizationUnit jika masih ada user aktif yang terdaftar di unit tersebut. Admin harus pindahkan semua user terlebih dahulu.

### Validasi Runtime
- **D-08:** **Tidak perlu** validasi saat login/akses. Dengan cascade rename dan blokir deactivate, data user selalu sinkron.

### Reparent (Pindah Unit)
- **D-09:** Saat admin pindah Unit (Level 2) dari Bagian A ke Bagian B, field `Section` semua user di Unit tersebut **auto-update** ke nama Bagian baru. Konsisten dengan hierarki.

### Claude's Discretion
- Pendekatan teknis untuk cascade query (raw SQL vs LINQ bulk update)
- Error handling jika cascade gagal (rollback transaksi)
- Urutan operasi dalam transaksi

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Organization Management
- `Controllers/AdminController.cs` — EditOrganizationUnit, ToggleOrganizationUnitActive, DeleteOrganizationUnit actions
- `Models/OrganizationUnit.cs` — Hierarchical model (ParentId, Level, IsActive)
- `Data/ApplicationDbContext.cs` — GetAllSectionsAsync(), GetUnitsForSectionAsync(), GetSectionUnitsDictAsync()

### Affected Fields
- `Models/ApplicationUser.cs` — Section, Unit, Directorate fields
- `Models/CoachCoacheeMapping.cs` — AssignmentSection, AssignmentUnit fields

### Template Import
- `Controllers/AdminController.cs` (DownloadImportTemplate action ~line 5456) — hardcoded Bagian names

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GetAllSectionsAsync()` — sudah ada method untuk query Level 1 OrganizationUnits aktif
- `GetUnitsForSectionAsync(name)` — query Level 2 units berdasarkan nama parent
- `EditOrganizationUnit` action — sudah ada logic untuk update nama dan parent, tinggal tambah cascade logic

### Established Patterns
- OrganizationUnit menggunakan parent-child hierarchy (ParentId), bukan level-specific tables
- User fields (Section, Unit) adalah **denormalized strings**, bukan FK ke OrganizationUnit
- TempData digunakan untuk flash messages di seluruh AdminController

### Integration Points
- `EditOrganizationUnit` POST action — titik utama untuk menambah cascade logic
- `ToggleOrganizationUnitActive` — perlu ditambah validasi blokir jika ada user
- `DownloadImportTemplate` — perlu diubah dari hardcoded ke dinamis

</code_context>

<specifics>
## Specific Ideas

- Tab di KkjMatrix dan CpdpFiles sudah dinamis (dari GetAllSectionsAsync), jadi menambah Bagian baru otomatis muncul sebagai tab baru
- Cascade harus handle baik rename (ubah nama) maupun reparent (pindah parent)
- Flash message harus informatif: berapa user dan mapping yang terupdate

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 260-auto-cascade-perubahan-nama-organizationunit-ke-semua-user-records-dan-template*
*Context gathered: 2026-03-26*
