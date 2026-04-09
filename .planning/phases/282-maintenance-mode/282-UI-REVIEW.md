# Phase 282 — UI Review

**Audited:** 2026-04-09
**Baseline:** 282-UI-SPEC.md (design contract)
**Screenshots:** Partially captured — user login hanya role Coach, halaman Admin tidak bisa diakses. Home/Maintenance redirect ke dashboard karena maintenance tidak aktif. Code-only audit untuk halaman admin.

---

## Pillar Scores

| Pillar | Score | Key Finding |
|--------|-------|-------------|
| 1. Copywriting | 3/4 | Copy sesuai kontrak, tapi error state "Gagal menyimpan..." tidak diimplementasi di controller |
| 2. Visuals | 4/4 | Hierarki visual jelas: breadcrumb, header+badge, card form, info footer |
| 3. Color | 4/4 | Warna sesuai kontrak — warning banner, info badge, primary CTA, secondary badge |
| 4. Typography | 4/4 | Hanya 3 level (h2 fw-bold, body, small) sesuai spec |
| 5. Spacing | 3/4 | Konsisten mb-4/mb-3/mt-3, tapi ada hardcoded spacing di inline style |
| 6. Experience Design | 3/4 | Loading state tidak ada, error state hanya validasi server-side tanpa try-catch |

**Overall: 21/24**

---

## Top 3 Priority Fixes

1. **POST action tidak punya try-catch** — Jika `SaveChangesAsync()` gagal, user melihat unhandled exception page, bukan pesan "Gagal menyimpan pengaturan maintenance." — Tambahkan try-catch di `AdminController.cs:99` yang set `TempData["ErrorMessage"]` dan tampilkan alert-danger di view.

2. **Tidak ada loading/disabled state pada tombol submit** — User bisa double-click "Simpan Pengaturan" dan trigger double POST — Tambahkan `onclick="this.disabled=true;this.form.submit();"` atau JS handler yang disable tombol saat form submit.

3. **Hardcoded color/spacing di inline style pada scope panel** — `border: 1px solid #e9ecef`, `background: #f8fafc`, `border: 1px solid #e5e7eb` di `<style>` block bukan dari Bootstrap token — Ganti ke Bootstrap utility classes (`border`, `bg-light`, `rounded-4`) atau CSS custom properties.

---

## Detailed Findings

### Pillar 1: Copywriting (3/4)

Semua copy dari UI-SPEC.md terimplementasi dengan benar:

| Spec Element | Actual | Match |
|---|---|---|
| Primary CTA "Simpan Pengaturan" | `Maintenance.cshtml:164` | Ya |
| Toggle label | `Maintenance.cshtml:91` "Mode Pemeliharaan" | Ya (tanpa ": Aktif/Nonaktif" suffix) |
| Scope radio "Seluruh Website" / "Halaman Tertentu" | `Maintenance.cshtml:100,104` | Ya |
| Textarea placeholder | `Maintenance.cshtml:155` | Ya |
| Maintenance page title "Sedang Dalam Pemeliharaan" | `Home/Maintenance.cshtml:19` | Ya |
| Footer "Hubungi admin jika ada pertanyaan mendesak." | `Home/Maintenance.cshtml:37` | Ya |
| Success toast activate/deactivate | `AdminController.cs:109-111` | Ya |
| Banner "Mode Pemeliharaan Aktif" | `_Layout.cshtml:86` | Ya |
| **Error state "Gagal menyimpan pengaturan maintenance."** | **Tidak ada** | **Tidak** |

**Gap:** Toggle label di spec adalah "Mode Pemeliharaan: Aktif" / "Mode Pemeliharaan: Nonaktif" — implementasi hanya "Mode Pemeliharaan" (tanpa suffix). Namun badge terpisah (Aktif/Nonaktif) mengompensasi ini secara fungsional. Minor deviation.

**Gap:** Error state copy dari spec tidak diimplementasi — controller POST tidak punya try-catch, sehingga exception akan menampilkan halaman error generic, bukan pesan "Gagal menyimpan pengaturan maintenance. Silakan coba lagi."

### Pillar 2: Visuals (4/4)

- Focal point jelas: header h2 dengan icon + badge status sebagai focal point utama
- Card Admin/Index mengikuti pattern existing (shadow-sm, border-0, icon+label+description)
- Icon `bi-cone-striped` (admin card, header), `bi-tools` (user page), `bi-exclamation-triangle-fill` (banner), `bi-clock` (estimasi), `bi-save` (CTA) — semua sesuai spec
- Breadcrumb navigation present
- Home/Maintenance: full-screen centered layout tanpa navbar, logo 120px, icon 48px — sesuai spec
- Scope panel memiliki visual hierarchy yang baik: group headers bold, items indented, search toolbar, summary badge

### Pillar 3: Color (4/4)

| Spec Role | Expected | Actual |
|---|---|---|
| Dominant 60% | #ffffff | `background:#fff` pada body dan scope group |
| Secondary 30% | bg-light (#f8f9fa) | `#f8fafc` (sangat mirip, acceptable) |
| Accent 10% | primary (#0d6efd) | `btn-primary` CTA, `text-primary` icon tools |
| Warning | bg-warning | `bg-warning text-dark` pada banner |
| Info | bg-info | `badge bg-info text-dark` pada estimasi |
| Success | bg-success | Badge "Aktif" |
| Secondary badge | bg-secondary | Badge "Nonaktif" |

Accent usage terbatas pada: submit button, tools icon di maintenance page, "Pilih semua" outline button — sesuai kontrak. Tidak ada overuse.

Hardcoded colors: `#e9ecef`, `#f8fafc`, `#e5e7eb` di `<style>` block — ini minor karena hanya border/background dekoratif, tapi idealnya menggunakan Bootstrap variables.

### Pillar 4: Typography (4/4)

Implementasi menggunakan level yang sesuai spec:

| Spec Role | Expected | Actual |
|---|---|---|
| Heading h2 | 1.25rem, fw-bold | `h2.fw-bold` (Maintenance.cshtml:53) |
| Display title | 1.75rem, fw-bold | `style="font-size:1.75rem;"` + `fw-bold` (Home/Maintenance.cshtml:19) |
| Body | 1rem, 400 | Default Bootstrap body text |
| Label/Small | 0.875rem, 400 | `<small class="text-muted">` throughout |

Label "fw-bold" digunakan untuk form labels (`form-label fw-bold`) — konsisten. `fw-semibold` digunakan untuk group headers di scope panel — acceptable variation.

### Pillar 5: Spacing (3/4)

Bootstrap spacing classes digunakan konsisten:
- `mb-4` untuk section gaps (6 occurrences)
- `mb-3` untuk inner group spacing
- `mb-2` untuk item spacing
- `mt-3`, `mt-4` untuk vertical rhythm
- `gap-2` untuk inline flex items
- `ms-3` untuk badge offset
- `ms-4` untuk checklist indentation
- `px-3` untuk mobile padding (Home/Maintenance)

**Gap:** Inline `<style>` block menggunakan hardcoded spacing:
- `padding: 1rem` (bisa `p-3`)
- `gap: .75rem` (non-standard, Bootstrap gap scale tidak punya 0.75rem)
- `gap: 1rem` (bisa `gap-3`)
- `margin-bottom: 1rem` (bisa `mb-3`)

Ini tidak serius karena masih dalam rem scale, tapi tidak menggunakan Bootstrap utility classes secara konsisten.

### Pillar 6: Experience Design (3/4)

**State coverage:**

| State | Present | Location |
|---|---|---|
| Success state | Ya | TempData["SuccessMessage"] + alert-success (Maintenance.cshtml:66-72) |
| Error state (validation) | Ya | ModelState + alert-danger + ValidationSummary (Maintenance.cshtml:74-81) |
| Error state (exception) | **Tidak** | Tidak ada try-catch di POST action |
| Loading state | **Tidak** | Tidak ada spinner/disabled pada submit |
| Empty state | N/A | Form selalu tampil (sesuai spec) |
| Confirmation destructive | N/A | Tidak ada aksi destruktif (sesuai spec) |

**Scope validation:** Client-side validation ada (JS alert saat submit tanpa checklist) + server-side ModelState validation. Baik.

**JS interactivity:** Radio toggle show/hide, group checkbox with indeterminate state, search filter, select all/clear all — well-implemented beyond spec requirements.

**Cache invalidation:** `_cache.Remove("MaintenanceMode_State")` present di POST — sesuai spec.

**Missing:** Form submit tidak ada debounce/disable. Jika SaveChangesAsync lambat, user bisa double-submit.

---

## Files Audited

- `Views/Admin/Maintenance.cshtml` — Admin form (toggle, scope, checklist, pesan, estimasi)
- `Views/Home/Maintenance.cshtml` — User-facing full-screen maintenance page
- `Views/Shared/_Layout.cshtml` — Warning banner untuk Admin/HC
- `Views/Admin/Index.cshtml` — Card "Mode Pemeliharaan" di Section A
- `Controllers/AdminController.cs` — Maintenance GET + POST actions
- `Helpers/MaintenanceScopeCatalog.cs` — Scope grouping helper (referenced)
