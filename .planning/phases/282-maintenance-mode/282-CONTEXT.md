# Phase 282: Maintenance Mode - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin dapat menempatkan website (seluruhnya atau per halaman/menu tertentu) dalam mode pemeliharaan sehingga non-admin/non-HC tidak bisa mengakses fitur yang sedang di-maintenance. Toggle dan konfigurasi dikelola dari halaman dedicated `/Admin/Maintenance`. Phase 281 (System Settings) di-skip — maintenance mode berdiri sendiri tanpa halaman settings terpusat.

</domain>

<decisions>
## Implementation Decisions

### Lokasi & Navigasi
- **D-01:** Card baru di Admin/Index Section A (Data Management) — klik masuk ke halaman `/Admin/Maintenance`
- **D-02:** Halaman `/Admin/Maintenance` berisi form lengkap: toggle on/off, textarea pesan kustom, datetime picker estimasi selesai

### Aktivasi Form
- **D-03:** Form lengkap — toggle on/off, textarea pesan kustom, datetime picker estimasi waktu selesai
- **D-04:** Admin mengisi pesan dan estimasi waktu sebelum mengaktifkan

### Bypass Access
- **D-05:** Admin + HC bypass maintenance mode (bisa akses semua halaman)
- **D-06:** Semua role lain (User) diarahkan ke halaman maintenance

### Halaman Maintenance (User View)
- **D-07:** Full-screen page — logo PortalHC, pesan kustom dari admin, estimasi waktu selesai
- **D-08:** Tidak ada navbar/sidebar — halaman bersih dan informatif

### Penyimpanan State
- **D-09:** Database table — persistent, survive restart, mendukung audit trail
- **D-10:** Menggunakan AuditLogService existing untuk mencatat aktivasi/deaktivasi

### Scope Maintenance (Per Halaman)
- **D-11:** Admin bisa pilih maintenance seluruh website ATAU per halaman/menu tertentu
- **D-12:** Di halaman `/Admin/Maintenance`, ada checklist halaman/menu mana saja yang di-maintenance (misal: Assessment, Coaching Proton, CDP, dll)
- **D-13:** Halaman yang tidak di-centang tetap bisa diakses user biasa seperti normal
- **D-14:** Halaman maintenance menampilkan info spesifik halaman mana yang sedang di-maintenance

### Claude's Discretion
- Desain exact halaman maintenance (warna, layout, icon)
- Middleware vs action filter untuk intercept request
- Schema tabel database (kolom, tipe data)
- Cara invalidate cache saat toggle berubah
- Daftar halaman/menu yang bisa di-checklist (berdasarkan controller/area yang ada)

</decisions>

<specifics>
## Specific Ideas

- **Research directive:** Researcher harus cari best practice maintenance mode dari website/platform luar — bagaimana website besar handle partial maintenance, UX halaman maintenance, dan granularity toggle per halaman/modul

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above and REQUIREMENTS.md (MAINT-01 through MAINT-05).

### Project references
- `.planning/REQUIREMENTS.md` — MAINT-01 to MAINT-05: full requirement list
- `.planning/ROADMAP.md` — Phase 282 success criteria
- `Controllers/AdminController.cs` — Admin authorization pattern, AuditLogService usage
- `Views/Admin/Index.cshtml` — Section A card layout pattern
- `Services/AuditLogService.cs` — Audit logging interface
- `Data/ApplicationDbContext.cs` — Entity registration and migration pattern

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **AuditLogService**: Sudah ada, bisa langsung pakai untuk log aktivasi/deaktivasi maintenance mode
- **IMemoryCache**: Sudah di-inject di AdminController, bisa cache maintenance state
- **Admin/Index.cshtml Section A**: Pattern card yang bisa di-copy untuk card Maintenance Mode

### Established Patterns
- **Authorization**: `[Authorize(Roles = "Admin")]` untuk halaman admin-only
- **TempData**: Untuk success/error messages setelah redirect
- **Entity + Migration**: Pattern dari AuditLog untuk table baru

### Integration Points
- **Middleware/Filter**: Perlu intercept semua request untuk cek maintenance state
- **Admin/Index**: Tambah card baru di Section A
- **Program.cs**: Register middleware/filter dan service baru

</code_context>

<deferred>
## Deferred Ideas

- Scheduled maintenance (set waktu mulai & selesai otomatis) — MAINT-F01, future milestone
- ~~Partial maintenance per modul~~ — **MASUK SCOPE** (D-11 s/d D-14)

</deferred>

---

*Phase: 282-maintenance-mode*
*Context gathered: 2026-04-01*
