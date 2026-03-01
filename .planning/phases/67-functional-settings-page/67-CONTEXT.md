# Phase 68: Functional Settings Page - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Settings page berfungsi — change password works, edit profile fields (FullName, Position, PhoneNumber), display read-only fields, cleanup non-functional placeholder items. Halaman Settings.cshtml saat ini sepenuhnya dummy — akan di-rewrite total.

</domain>

<decisions>
## Implementation Decisions

### Page structure
- Single scrollable page, semua section di satu halaman (tidak pakai tabs)
- Urutan: Edit Profile di atas, Change Password di bawahnya, non-functional items paling bawah
- Flat rows, col-md-8 centered — konsisten dengan Phase 67 Profile page, tidak pakai cards
- Heading: "Pengaturan Akun" sebagai h3/h4
- Breadcrumb/link kembali ke Profile page di atas heading

### Form sections
- Setiap section (Edit Profile, Change Password) punya tombol Save sendiri-sendiri
- Edit Profile dan Change Password adalah 2 form terpisah, bukan satu form

### Editable fields (Edit Profile section)
- FullName — editable text input
- Position — editable text input
- PhoneNumber — editable text input (ditambahkan, di luar roadmap original yang hanya FullName + Position)

### Read-only fields (Edit Profile section)
- NIP, Email, Role, Section, Directorate, Unit — semua ditampilkan read-only
- Tampilan: input field disabled (background abu-abu) + hint kecil "Dikelola oleh admin" di bawahnya
- Semua org fields ditampilkan, tidak hanya 4 field dari roadmap

### Password rules
- Ikuti konfigurasi ASP.NET Identity yang sudah ada: minimal 6 karakter, tanpa complexity requirements
- Fields: Password Lama, Password Baru, Konfirmasi Password Baru

### Validation & feedback
- Validasi on-submit saja (standar ASP.NET MVC ModelState), tidak real-time
- Error muncul di bawah field yang bermasalah (asp-validation-for)
- Pesan sukses/error sebagai alert Bootstrap di atas section yang bersangkutan
- Bahasa Indonesia untuk semua pesan: "Profil berhasil diperbarui", "Password berhasil diubah", "Password lama salah"

### Post-save behavior
- Setelah profile save: tetap di Settings, alert sukses muncul
- Setelah password save: tetap di Settings, form password di-reset (kosongkan), alert sukses muncul
- Konfirmasi dialog untuk password change saja ("Yakin ubah password?"), edit profile langsung save

### Non-functional items
- 2FA, Email Notifications, Language — tetap ditampilkan tapi disabled
- Style: label muted text + badge kecil "Segera Hadir" di sebelahnya, toggle/dropdown disabled
- Posisi: section terpisah "Pengaturan Lainnya" di bawah Edit Profile dan Change Password

### Claude's Discretion
- Exact spacing, typography, dan responsive behavior
- Section heading style (uppercase muted, dividers, etc.)
- Alert animation/auto-dismiss timing
- Confirmation dialog implementation (native confirm vs Bootstrap modal)
- Read-only field grouping within Edit Profile section

</decisions>

<specifics>
## Specific Ideas

- Settings page saat ini sepenuhnya dummy — perlu rewrite total (bukan edit incremental)
- Tombol "Save Changes" global dan `alert('Simulation')` harus dihapus
- Style harus konsisten dengan Phase 67 Profile page (flat rows, no cards, professional HRIS feel)
- PhoneNumber editable meskipun di roadmap original hanya FullName + Position

</specifics>

<deferred>
## Deferred Ideas

- 2FA implementation — future phase
- Email notification system — future phase
- Language/i18n switching — future phase

</deferred>

---

*Phase: 68-functional-settings-page*
*Context gathered: 2026-02-27*
