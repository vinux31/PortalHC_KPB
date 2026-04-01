# Phase 283: User Impersonation - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin dapat melihat aplikasi dari perspektif role (HC/User) atau user spesifik untuk troubleshooting. Impersonation bersifat read-only — semua aksi write diblokir. Setiap impersonation tercatat di audit log dan otomatis berakhir setelah 30 menit.

</domain>

<decisions>
## Implementation Decisions

### Mekanisme Impersonation
- **D-01:** Session flag — admin identity tetap utuh (tidak ganti ClaimsPrincipal). Di session disimpan flag impersonation (role target atau user ID target). Middleware/helper membaca flag untuk mengubah tampilan dan akses.

### Entry Point
- **D-02:** "View As Role" (HC/User) via dropdown di navbar, di bawah nama user yang sedang login
- **D-03:** "Impersonate User Spesifik" via dropdown search di navbar — admin ketik nama user, pilih dari hasil search, langsung impersonate

### Read-Only Enforcement
- **D-04:** Middleware blokir semua HTTP POST/PUT/DELETE + whitelist untuk POST yang read-only (login/logout, search/filter)
- **D-05:** Form tetap ditampilkan (supaya admin bisa lihat untuk troubleshooting), tapi tombol submit di-disable + badge "Mode Read-Only"

### Banner Impersonation
- **D-06:** Banner fixed top di atas navbar (selalu terlihat meski scroll), warna merah (danger), berisi info "Anda melihat sebagai [role/nama user]" + tombol "Kembali ke Admin"

### Scope Navigasi
- **D-07:** Navigasi berubah 100% sesuai role target — admin yang impersonate sebagai User tidak melihat menu "Kelola Data", persis seperti pengalaman user tersebut

### Batasan & Keamanan (dari Requirements)
- **D-08:** Admin tidak bisa impersonate admin lain — hanya role HC dan User (IMP-07)
- **D-09:** Impersonation auto-expire setelah 30 menit (IMP-04)
- **D-10:** Setiap impersonation tercatat di audit log: siapa, sebagai siapa, kapan mulai/selesai (IMP-06)
- **D-11:** Klik "Kembali ke Admin" langsung restore session admin tanpa login ulang (IMP-08)

### Claude's Discretion
- Schema session keys (nama key, format value)
- Whitelist POST endpoints mana yang diizinkan saat impersonation
- Desain exact dropdown search di navbar (autocomplete pattern)
- Tinggi dan padding banner impersonation
- Cara handle SignalR hub saat impersonation aktif
- Apakah perlu tabel database untuk tracking impersonation atau cukup audit log

</decisions>

<specifics>
## Specific Ideas

- **Research directive:** Researcher harus cari best practice impersonation dari framework/platform lain — ASP.NET Core impersonation patterns, bagaimana platform besar (Django, Rails, Laravel) implement "view as user" dengan session flag approach
- Navbar dropdown search bisa pakai pattern autocomplete yang sudah umum di Bootstrap — ketik minimal 2-3 karakter baru muncul hasil

</specifics>

<canonical_refs>
## Canonical References

### Project references
- `.planning/REQUIREMENTS.md` — IMP-01 to IMP-08: full requirement list untuk User Impersonation
- `.planning/ROADMAP.md` — Phase 283 success criteria (5 items)

### Codebase references
- `Controllers/AdminController.cs` — Admin authorization pattern, AuditLogService usage, ManageWorkers action (user list)
- `Views/Shared/_Layout.cshtml` — Navbar structure, role-based menu rendering (`User.IsInRole()`), user dropdown
- `Models/ApplicationUser.cs` — User model fields (FullName, NIP, RoleLevel, SelectedView)
- `Models/UserRoles.cs` — Role constants, `GetRoleLevel()`, `HasFullAccess()` utilities
- `Services/AuditLogService.cs` — Audit logging interface (`LogAsync` method)
- `Models/AuditLog.cs` — Audit log entity schema
- `Program.cs` — Auth/session configuration, middleware registration order
- `Middleware/MaintenanceModeMiddleware.cs` — Pattern untuk middleware yang intercept request (bypass rules, role checks)

### Prior phase context
- `.planning/phases/282-maintenance-mode/282-CONTEXT.md` — Maintenance mode decisions (middleware pattern, bypass rules — reusable pattern)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **AuditLogService**: Sudah ada, langsung pakai untuk log impersonation start/end
- **IMemoryCache**: Sudah di-inject, bisa cache user data saat impersonation
- **MaintenanceModeMiddleware**: Pattern middleware yang bisa di-adapt untuk impersonation read-only enforcement
- **UserRoles utilities**: `GetRoleLevel()` untuk validasi admin tidak impersonate admin lain
- **_Layout.cshtml navbar dropdown**: Sudah ada user dropdown, bisa extend dengan opsi "View As"

### Established Patterns
- **Session**: Sudah dikonfigurasi (8 jam timeout, HttpOnly, Essential) — bisa langsung pakai untuk flag impersonation
- **Authorization**: `[Authorize(Roles = "Admin")]` untuk action admin-only
- **TempData**: Untuk success/error messages
- **Middleware registration**: Pattern di Program.cs (`app.UseMiddleware<>()`)

### Integration Points
- **_Layout.cshtml**: Tambah banner impersonation + dropdown "View As" + search user di navbar
- **Program.cs**: Register ImpersonationMiddleware setelah auth middleware
- **Semua controller**: Middleware harus transparan — controller tidak perlu tahu soal impersonation kecuali untuk read-only enforcement

</code_context>

<deferred>
## Deferred Ideas

- Read/Write mode terpisah (admin bisa melakukan aksi atas nama user) — IMP-F01, future milestone
- Impersonation dari halaman ManageWorkers (tombol per row) — bisa ditambahkan nanti sebagai shortcut tambahan

</deferred>

---

*Phase: 283-user-impersonation*
*Context gathered: 2026-04-01*
