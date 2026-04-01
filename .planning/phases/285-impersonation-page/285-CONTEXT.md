# Phase 285: Dedicated Impersonation Page - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Pindahkan kontrol impersonate dari dropdown profile di navbar ke halaman Admin tersendiri (`/Admin/Impersonate`) dengan UX yang lebih baik — search user lebih lega, card view-as-role, info panel dengan history, dan cleanup _Layout.cshtml. Semua backend (ImpersonationService, middleware, controller actions) sudah ada dari Phase 283 — phase ini murni UI/UX improvement.

</domain>

<decisions>
## Implementation Decisions

### Layout Halaman
- **D-01:** Dua kolom layout — kiri = card "View As Role" (HC/User) + info panel, kanan = search & impersonate user spesifik
- **D-02:** Kolom kiri berisi 2 card role (HC dan User) untuk quick "View As" + info panel di bawahnya
- **D-03:** Kolom kanan berisi search input (nama/NIP) dengan hasil list user — lebih lega dari dropdown navbar

### Akses ke Halaman
- **D-04:** Navbar dropdown: hapus semua kontrol impersonation, ganti dengan satu link "Impersonate" yang menuju `/Admin/Impersonate`
- **D-05:** Tidak perlu entry di Admin/Index hub — cukup dari navbar link saja

### Info Panel
- **D-06:** Info panel berisi 2 bagian: (1) penjelasan singkat aturan impersonation (read-only, max 30 menit, tidak bisa impersonate admin), (2) tabel history impersonation terakhir dari audit log (siapa, sebagai siapa, kapan, durasi)
- **D-07:** History diambil dari AuditLog yang sudah ada — filter by action type impersonation

### Cleanup _Layout.cshtml
- **D-08:** Hapus total section "Lihat Sebagai" (View As Role forms) dari navbar dropdown
- **D-09:** Hapus total section "Impersonate User" (search input + results) dari navbar dropdown
- **D-10:** Hapus JavaScript autocomplete/search impersonation dari _Layout.cshtml
- **D-11:** Ganti dengan satu `<a>` link "Impersonate" di dropdown menu (hanya visible untuk Admin, tidak saat sedang impersonating)

### Claude's Discretion
- Exact card design untuk role cards (icon, warna, hover state)
- Jumlah history entries yang ditampilkan (5-10 terakhir)
- Responsive behavior (collapse ke single column di mobile)
- Apakah search user pakai AJAX real-time atau reuse pattern dari Phase 283
- Padding, spacing, typography halaman

</decisions>

<specifics>
## Specific Ideas

- Reuse `SearchUsersApi` endpoint yang sudah ada dari Phase 283 — tidak perlu endpoint baru
- Reuse `StartImpersonation` dan `StopImpersonation` actions — hanya perlu action baru `Impersonate()` untuk render halaman
- Research directive: Researcher harus cek bagaimana pattern admin page lain di project ini (Admin/Index hub, ManageWorkers) untuk konsistensi visual
- Info panel history bisa pakai DataTables atau simple table — sesuaikan dengan pattern project

</specifics>

<canonical_refs>
## Canonical References

### Phase 283 (dependency — semua backend sudah ada)
- `.planning/phases/283-user-impersonation/283-CONTEXT.md` — Semua keputusan backend impersonation (session flag, middleware, read-only enforcement, audit log)
- `.planning/phases/283-user-impersonation/283-RESEARCH.md` — Research hasil implementasi impersonation
- `.planning/phases/283-user-impersonation/283-UI-SPEC.md` — UI spec Phase 283 (banner, dropdown controls yang akan di-cleanup)

### Requirements
- `.planning/REQUIREMENTS.md` — IMP-01 to IMP-08: requirement list User Impersonation (sudah implemented di Phase 283)

### Codebase references
- `Controllers/AdminController.cs` — `StartImpersonation`, `StopImpersonation`, `SearchUsersApi` actions
- `Services/ImpersonationService.cs` — Session-based impersonation state management
- `Middleware/ImpersonationMiddleware.cs` — Read-only enforcement middleware
- `Views/Shared/_Layout.cshtml` — Navbar dropdown yang akan di-cleanup (lines ~159-188 impersonation controls)
- `Views/Shared/_ImpersonationBanner.cshtml` — Banner yang tetap dipertahankan
- `Models/AuditLog.cs` — Audit log entity untuk history panel
- `Services/AuditLogService.cs` — Service untuk query audit log history

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **SearchUsersApi**: GET endpoint yang return JSON user list (max 10, non-admin) — langsung pakai untuk search di halaman baru
- **StartImpersonation**: POST action yang sudah handle validasi + audit log — form di halaman baru tinggal submit ke sini
- **AuditLogService**: Sudah ada, query history impersonation untuk info panel
- **_ImpersonationBanner.cshtml**: Partial view banner yang tetap dipertahankan apa adanya

### Established Patterns
- **Admin pages**: Pattern `[Authorize(Roles = "Admin")]` action + Razor view
- **AJAX search**: Autocomplete pattern dengan debounce 300ms, min 2 chars — sudah ada di _Layout.cshtml, bisa di-copy ke halaman baru
- **DataTables/simple table**: Pattern tabel data yang sudah umum di project

### Integration Points
- **AdminController.cs**: Tambah action `Impersonate()` (GET) untuk render halaman baru
- **_Layout.cshtml**: Cleanup dropdown — hapus impersonation controls, tambah link
- **Views/Admin/Impersonate.cshtml**: View baru untuk halaman dedicated

</code_context>

<deferred>
## Deferred Ideas

- Read/Write mode terpisah (admin bisa melakukan aksi atas nama user) — IMP-F01, future milestone
- Impersonation dari halaman ManageWorkers (tombol per row) — bisa ditambahkan nanti

</deferred>

---

*Phase: 285-impersonation-page*
*Context gathered: 2026-04-01*
