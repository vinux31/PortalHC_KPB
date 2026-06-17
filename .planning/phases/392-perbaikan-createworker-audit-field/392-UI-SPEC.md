---
phase: 392
slug: perbaikan-createworker-audit-field
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-17
---

# Phase 392 — UI Design Contract

> Kontrak visual & interaksi untuk fix VIEW-ONLY `Views/Admin/CreateWorker.cshtml` ("Tambah Pekerja Baru"). Dihasilkan oleh gsd-ui-researcher, diverifikasi oleh gsd-ui-checker.
>
> **Sifat phase:** Bukan desain baru / bukan redesign. Sistem desain (Bootstrap 5 + Bootstrap Icons + form card-based) SUDAH ADA. Kontrak ini **mengodifikasi konvensi existing** dan **mengunci 5 perubahan kecil** yang sudah diputuskan di `392-CONTEXT.md` (D-01..D-08). Segala sesuatu di luar 5 cluster di bawah = **"pakai pola form existing, tanpa perubahan"**.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (ASP.NET Core MVC + Razor; shadcn N/A — bukan React/Next/Vite) |
| Preset | not applicable |
| Component library | Bootstrap 5.3.0 (CDN `cdn.jsdelivr.net`, `_Layout.cshtml:38`) |
| Form framework | Razor TagHelpers (`asp-for` / `asp-validation-for`) + jquery-validation-unobtrusive (di-aktifkan via D-05) |
| Icon library | Bootstrap Icons 1.10.0 (`bi-*`, `_Layout.cshtml:39`) |
| Font | Inter (Google Fonts wght 300–800, `_Layout.cshtml:41`) |
| Detection evidence | No `components.json`; no `.claude/skills` / `.agents/skills`; `_Layout.cshtml` memuat Bootstrap 5.3.0 + bootstrap-icons 1.10.0 + Inter; `<body class="bg-light">` (`_Layout.cshtml:64`) |

**Catatan kontrak:** Phase ini TIDAK menambah token, komponen, warna, atau font baru. Semua nilai di bawah adalah **token Bootstrap 5 yang SUDAH dipakai** di view, didokumentasikan agar checker/executor punya sumber kebenaran — bukan untuk dibuat ulang.

---

## Spacing Scale

Bootstrap 5 spacing utilities (kelipatan 4px; `$spacer = 1rem = 16px`). Yang DIPAKAI di view (existing, tidak berubah):

| Token | Value | Bootstrap util | Usage di CreateWorker |
|-------|-------|----------------|-----------------------|
| xs | 4px | `g-1`, `me-1`, `mt-1` | gap ikon `bi-*` ↔ teks (`me-1` di info-text); `mt-1` list error |
| sm | 8px | `me-2`, `py-2`, `gap-2` | gap ikon header/CTA (`me-2`); padding alert AD info (`py-2`); jarak tombol footer (`gap-2`) |
| md | 16px | `g-3`, `mb-3`, `py-1`(×?) | gutter grid field (`row g-3`); jarak breadcrumb (`mb-3`) |
| lg | 24px | `mb-4`, `py-4` | jarak antar-card (`mb-4`); padding vertikal container (`py-4`) |
| — | px-4 (24px horizontal) | `px-4` | padding horizontal tombol footer (`Batal` / `Simpan Pekerja`) |

Exceptions: none. **Kontrak: 4 span validasi org baru (D-04) TIDAK menambah margin/padding** — mengikuti span existing yang adalah inline `<span class="text-danger small">` tanpa kelas spacing (mengalir di bawah input dalam `col-md-6`, identik FullName L69 / Email L80).

---

## Typography

Existing (Bootstrap 5 defaults + Inter; tidak berubah). Body font-size Bootstrap = 1rem (16px), line-height 1.5.

| Role | Size | Weight | Line Height | Token di view |
|------|------|--------|-------------|---------------|
| Heading halaman | 32px (`h2`, ~2rem) | 700 (`fw-bold`) | 1.2 | `<h2 class="fw-bold">Tambah Pekerja Baru</h2>` |
| Card header | 16px (`h6`, 1rem) | 700 (`fw-bold`) | 1.2 | `<h6 class="fw-bold">Informasi Pribadi</h6>` |
| Label field | 16px (`.form-label`, 1rem) | 600 (`fw-semibold`) | 1.5 | `<label class="form-label fw-semibold">` |
| Body / input / placeholder | 16px (1rem) | 400 (regular) | 1.5 | `.form-control`, `.form-select` |
| Helper / validation / muted | ~13px (`.small` = 0.875em ≈ 14px; `.form-text` ≈ 14px) | 400 | 1.5 | `.form-text text-info` (info AD); `.text-danger small` (validasi); `.text-muted` (subjudul) |

**Kontrak baru (D-02 + D-04):**
- Reworded info-text AD tetap `form-text` (≈14px, weight 400) — **tidak ada perubahan typography**, hanya teks.
- 4 span validasi org baru WAJIB pakai kelas **persis** `text-danger small` (sama dengan 6 span existing) — TIDAK ada varian ukuran/weight lain.

---

## Color

60/30/10 dipetakan ke palet Bootstrap 5 existing (tidak ada warna baru ditambah).

| Role | Value | Bootstrap token | Usage |
|------|-------|-----------------|-------|
| Dominant (60%) | #f8f9fa | `bg-light` (`<body>`) | Latar halaman/surface |
| Secondary (30%) | #ffffff | `.card` (putih) | Kartu form `Informasi Pribadi` / `Organisasi` / `Role & Kredensial` |
| Accent (10%) | #0d6efd | `text-primary` / `btn-primary` / `--bs-primary` | LIHAT daftar reserved di bawah |
| Info (semantic) | #0dcaf0 | `text-info` / `alert-info` | Info-text AD (`form-text text-info`) + blok info password mode-AD |
| Success (semantic) | #198754 | `text-success` / `bg-success bg-opacity-10` | Header card Organisasi (existing) |
| Warning (semantic) | #ffc107 | `text-warning` / `bg-warning bg-opacity-10` | Header card Role & Kredensial (existing) |
| Destructive | #dc3545 | `text-danger` / `alert-danger` | Validasi error (`text-danger small`) + summary error (`alert-danger`) — TIDAK ada aksi destruktif di view ini |

Accent (#0d6efd primary) reserved for: **ikon header halaman** (`bi-person-plus-fill text-primary`), **header card "Informasi Pribadi"** (`text-primary` + `bg-primary bg-opacity-10`), dan **tombol CTA utama** `Simpan Pekerja` (`btn-primary`). Tidak diperluas ke elemen lain.

**Kontrak warna untuk perubahan phase:**
- **D-01:** Hapus `bg-light` ternary dari input FullName/Email → input render **putih `.form-control` standar** (identik field lain seperti NIP L84). Field TIDAK lagi tampak abu-abu/disabled. Ini SATU-SATUNYA perubahan warna di phase ini.
- **D-02:** Info-text AD tetap `text-info` (#0dcaf0) — warna tidak berubah, hanya teksnya.
- **D-04:** Span org baru tetap `text-danger` (#dc3545) — sama dengan span existing.

---

## Copywriting Contract

Bahasa Indonesia (CLAUDE.md: respons & teks user-facing wajib Bahasa Indonesia).

| Element | Copy |
|---------|------|
| Primary CTA | **"Simpan Pekerja"** (existing, `<button>` L188, ikon `bi-check-lg` — tidak diubah) |
| Secondary action | "Batal" (`btn-light` L186) + "Kembali" (`btn-outline-secondary` header, ikon `bi-arrow-left`) — tidak diubah |
| Info-text AD (D-02, LOCKED) | **"Isi sesuai akun AD Pertamina pekerja. Nama & Email akan diselaraskan otomatis dari Active Directory saat pekerja login pertama kali."** — di-render dalam `<div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>…</div>`, IDENTIK di blok FullName (`@if(isAdMode)`) DAN Email. Di markup tulis `&amp;` untuk ampersand. Menggantikan teks lama "Dikelola oleh AD — akan disinkronkan saat login" (kontradiktif pasca-unlock). |
| Empty state | N/A — form selalu menampilkan field; tidak ada daftar kosong di view ini |
| Error state — summary | Existing (L34-46): heading **"Terdapat kesalahan:"** + `<ul>` daftar error (`alert-danger`, ikon `bi-exclamation-triangle`) — tidak diubah |
| Error state — inline per-field (D-04/D-05) | Teks dari atribut `data-val-*` model (server-authoritative) yang di-render `jquery-validation-unobtrusive`. Field `[Required]` (FullName/Email/Role) → pesan Required model; Email `[EmailAddress]` + `type="email"` → pesan format email; Password `[StringLength min 6]`/ConfirmPassword `[Compare]` → pesan length/cocok. Org fields (Position/Directorate/Section/Unit) = surface pesan SERVER ("Bagian tidak ditemukan" L227 / "Unit tidak valid" L234) — tidak ada copy baru yang ditulis di view. |
| Destructive confirmation | N/A — TIDAK ada aksi destruktif di `/Admin/CreateWorker`. (DeleteWorker = view lain, dipakai HANYA sebagai jalur teardown test D-07.) |

**Catatan copy mode-AD password (existing, tidak diubah):** blok `@else` (mode AD) menampilkan `alert-info` "Password dikelola melalui portal Pertamina. Sistem akan membuat password acak untuk akun ini." — di luar scope perubahan.

---

## Interaction Contract (inti phase — view-only fix)

Bagian ini melengkapi 6 dimensi standar; mengunci 5 perubahan visual/interaksi spesifik phase. Anchor = file live (RESEARCH.md "Verified Line Anchors", 206 baris).

### IC-1 — Field unlock (D-01, WRKR-01)
- Input **FullName** (L62-64) & **Email** (L73-75): HAPUS `class="@(isAdMode ? "bg-light" : "")"` ternary DAN `readonly="@(isAdMode ? "readonly" : null)"`.
- Hasil: keduanya render sebagai `<input class="form-control">` editable normal di **SEMUA mode** (AD & lokal) — visual identik field lain (NIP/Directorate).
- a11y: dengan `readonly` dihapus, tidak ada lagi implikasi `aria-readonly` stale; field jadi peer-input biasa, fokus & keyboard penuh. Label existing (`asp-for` → Display name) tetap terkait via `for`/`id`.

### IC-2 — Info-text AD reworded (D-02, WRKR-01)
- Treatment visual TETAP: `<div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>{copy}</div>`, hanya muncul saat `@if(isAdMode)`, di bawah input FullName & Email.
- Copy = string LOCKED di tabel Copywriting di atas. Sama persis untuk kedua field.

### IC-3 — Email type + inline validation spans (D-03 + D-04, WRKR-02)
- **D-03:** Tambah `type="email"` eksplisit di input Email (L73) → `<input asp-for="Email" type="email" class="form-control" …>`. Model `[EmailAddress]` (bukan `[DataType]`) tidak meng-emit `type`, jadi ini bermakna & tidak dobel.
- **D-04:** Tambah **4** span — placement: SEGERA setelah input/select-nya, di dalam `col-md-6` masing-masing, kelas **`text-danger small`** (identik existing):
  - Position → setelah `<input>` L107
  - Directorate → setelah `<input>` L111
  - Section → setelah `</select>` L117
  - Unit → setelah `</select>` L123
- **Role span = OPSIONAL** (Claude's Discretion; error Required unreachable krn default "Coachee"). Rekomendasi kontrak: **tambah** demi konsistensi visual (setelah `<div class="form-text">` L146), risiko nol.
- **JANGAN duplikasi** 6 span existing (FullName L69, Email L80, NIP L85, JoinDate L90, Password L158, ConfirmPassword L168). Executor WAJIB grep `asp-validation-for` sebelum insert.
- **JANGAN** tambah `required`/asterisk ke field org — semantik model OPTIONAL dipertahankan; span hanya surface pesan, bukan menandai wajib.

### IC-4 — Live validation feedback (D-05, WRKR-02)
- Aktifkan client validation: pindahkan blok `<script>` bawah (L194-205: `shared-cascade.js` + `shared-loading.js` + `initSectionUnitCascade` + `initFormLoading`) ke dalam `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") … }`. Preseden persis: `Views/Account/Settings.cshtml:137-146`.
- Expected interaction (kontrak): dengan unobtrusive aktif, pesan inline `text-danger small` muncul **LIVE saat blur/submit sebelum POST** (bukan hanya pasca-reload). `type="email"` memicu feedback format native + unobtrusive.
- **Tanpa CSS validasi kustom** — reuse default jquery-validation-unobtrusive + kelas existing `text-danger small`. Tidak ada `.input-validation-error` styling baru ditambah.
- CAUTION: partial WAJIB di `@section Scripts` (urutan jQuery footer `_Layout` L241 → section L267), BUKAN inline di body.

### IC-5 — Accessibility
- Label sudah ada untuk semua field (`asp-for` → Display name) — pertahankan.
- 4 span baru otomatis ter-asosiasi ke field-nya via `asp-validation-for` (unobtrusive set `data-valmsg-for`), konsisten dengan span existing. Tidak perlu aria manual tambahan.
- D-01 menghapus `readonly` → tidak ada `aria-readonly`/disabled-state stale yang menyesatkan screen reader (field kini benar-benar editable).
- Konsisten WCAG dengan preseden phase sebelumnya (PXF-11 aria, v31.0). Kontras warna semua dari token Bootstrap default (sudah AA-compliant) — tidak ada warna kustom diperkenalkan.

### Out of scope (kontrak: "unchanged, reuse existing")
NIP, JoinDate (`type="date"`), Role select (level), Password/ConfirmPassword + tombol `togglePassword` (mode lokal), blok info password mode-AD, struktur 3 card, header/ikon card, breadcrumb, tombol footer (`Batal`/`Simpan Pekerja`/`Kembali`), validation summary (L34-46), `@Html.AntiForgeryToken()`, cascade `initSectionUnitCascade` logic. **Tidak diubah** — kecuali pemindahan blok script ke `@section Scripts` (IC-4) yang membungkus, bukan mengubah, logika cascade/loading.

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| none | none | not applicable — bukan proyek shadcn (ASP.NET Core MVC + Bootstrap 5 CDN). Tidak ada registry komponen pihak ketiga. |

Tidak ada registry pihak ketiga dideklarasikan → vetting gate tidak berlaku.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending

---

## UI-SPEC COMPLETE
