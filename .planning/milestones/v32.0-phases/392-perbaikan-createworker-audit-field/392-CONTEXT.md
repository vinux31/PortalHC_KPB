# Phase 392: Perbaikan CreateWorker + Audit Field - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Halaman `/Admin/CreateWorker` ("Tambah Pekerja Baru") kembali **bisa dipakai membuat akun pekerja di semua environment** dan **terverifikasi end-to-end**: field Nama Lengkap & Email tidak lagi terkunci saat mode AD (bisa diketik), field memvalidasi + menampilkan pesan error per-field (live, sebelum submit), cascade Bagian→Unit jalan, dan submission create pekerja baru sukses (record tersimpan → redirect ke daftar pekerja).

**VIEW-ONLY.** Hanya `Views/Admin/CreateWorker.cshtml` yang diubah. `Controllers/WorkerController.cs` (`CreateWorker`) & `Models/ManageUserViewModel.cs` **TIDAK diubah** (git 0-diff wajib). **0 migration.**

**Bukan scope:** perubahan controller/model CreateWorker, AD-provisioning / membuat login AD benar-benar jalan, EditWorker.cshtml, migration. (Penambahan peserta assessment = Phase 391, file-disjoint.)

### Batas penting (non-goal yang HARUS didokumentasikan)
Phase 392 membuat form **bisa dipakai + tervalidasi**; ia TIDAK (dan tak bisa, view-only) menjamin pekerja baru bisa **login di mode AD**. Di server (AD mode, `Authentication:UseActiveDirectory=true`), login divalidasi oleh **server AD Pertamina** (`LdapAuthService` bind) — halaman ini hanya menulis baris Identity lokal + role + password acak buang. Saat login AD pertama, `FullName`/`Email` di-overwrite dari data AD (`AccountController.cs:86-116`). **Keputusan user:** dalam praktik akun yang dibuat di sini **selalu untuk pekerja yang sudah ada di AD Pertamina**, jadi login akan jalan **asalkan Email yang diketik cocok dengan akun AD**. Membuat login AD benar-benar jalan untuk akun baru = kerja auth/provisioning terpisah (OUT of scope; sudah tercatat di REQUIREMENTS Out of Scope). Di mode lokal (non-AD) pekerja baru bisa login penuh pakai email+password form.
</domain>

<decisions>
## Implementation Decisions

### Buka kunci field + UX mode AD (WRKR-01)
- **D-01:** Hapus `readonly="@(isAdMode ? "readonly" : null)"` **unconditional** dari input FullName (~L62-64) & Email (~L73-75) → editable di SEMUA mode. Hapus juga `bg-light` ternary (`class="@(isAdMode ? "bg-light" : "")"`) agar field tak tampak disabled. AD auth tetap aktif. (F-VIEW-07 HIGH = bug utama: di AD mode kedua field read-only padahal akun baru WAJIB Nama+Email → form tak terpakai.)
- **D-02:** Reword teks info `@if(isAdMode)` pada FullName (~L65-68) & Email (~L76-79) — dari "Dikelola oleh AD — akan disinkronkan saat login" (yang kini kontradiktif krn field editable) menjadi pengingat yang akurat, mis. **"Isi sesuai akun AD Pertamina; data akan diselaraskan otomatis saat pekerja login."** Alasan: setelah unlock, teks lama menyesatkan; DAN saat login AD pertama Nama/Email memang di-overwrite oleh data AD (`AccountController.cs:86-116`) → wording wajib jujur + mengingatkan mencocokkan akun AD. (Konsekuensi keputusan user: akun selalu untuk pekerja yang sudah ada di AD.)

### Email type + validasi inline per-field (WRKR-02)
- **D-03:** Tambah `type="email"` eksplisit pada input Email (~L73-75). Model pakai `[EmailAddress]` (BUKAN `[DataType(DataType.EmailAddress)]`) → `asp-for` TIDAK auto-render type=email, jadi penambahan eksplisit ini bermakna. Verifikasi tak ada atribut `type` dobel.
- **D-04:** Tambah `<span asp-validation-for="X" class="text-danger small">` **HANYA** ke 4 field organisasi yang belum punya: Position (~L106-108), Directorate (~L110-112), Section (~L113-117 `<select>`), Unit (~L119-123 `<select>`). **JANGAN** tambah/duplikasi span untuk FullName/Email/NIP/JoinDate/Password/ConfirmPassword — **sudah ada** (L69/80/85/90/158/168). Executor WAJIB grep `asp-validation-for` existing sebelum insert (hindari span dobel — F-NEW-01 HIGH). Field org tetap **OPTIONAL** (jangan tambah `required` — pertahankan semantik model; FullName/Email/Role sudah `[Required]` server-side). Span Role = Claude's Discretion (error Required-nya unreachable, default "Coachee").

### Validasi live / client-side (TEMUAN AUDIT — user approved)
- **D-05:** **Aktifkan validasi client-side** agar pesan error muncul live saat mengetik (sebelum submit), bukan hanya setelah POST gagal + reload. Saat ini halaman TIDAK memuat validator JS sama sekali (F-NEW-03 HIGH) → SEMUA inline span (existing & baru) cuma terisi setelah server reject. Fix = **murni view**: tambah `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") ... }` DAN **pindahkan blok `<script>` bawah** (shared-cascade.js + `initSectionUnitCascade` + `initFormLoading`, ~L194-205) **ke dalam section itu** agar jQuery (footer `_Layout` ~L241) + jquery-validation ter-load sebelum cascade/init jalan. Preseden persis: `Views/Account/Settings.cshtml:137-146`. **CAUTION (F-NEW-10):** taruh partial di `@section Scripts` (BUKAN inline di body) demi urutan load jQuery. Efek: FullName-required / Email-format / Password-minlength / ConfirmPassword-compare enforce in-browser + span org jadi fungsional.

### Verifikasi (WRKR-03)
- **D-06:** Playwright e2e di mode **lokal AD-OFF** (`Authentication__UseActiveDirectory=false` — satu-satunya mode login lokal yang jalan, lihat [[reference_local_e2e_sql_env_fix]]; combined run WAJIB `--workers=1`): buktikan Nama/Email **bisa diketik** + isi semua field + **cascade Bagian→Unit** jalan runtime + **create submission sukses** (record tersimpan → redirect `ManageWorkers` + `TempData["Success"]`). **PLUS** guard **statik source-grep** (BUKAN assertion runtime) bahwa `CreateWorker.cshtml` hasil-fix **tak lagi punya** `readonly=` dan `bg-light` ternary di input FullName/Email. Alasan (F-NEW-04 MED): run AD-off tak bisa menguji bug readonly-mode-AD (di AD-off, readonly sudah absen → cek runtime lolos hampa); source-grep membuktikan penghapusan unconditional yang menjamin editable-saat-AD **by construction**. (Lesson Phase 354: Razor + cascade JS dinamis → Playwright runtime wajib, grep+build tak cukup untuk render/cascade.)
- **D-07:** Cleanup data test (user pilih email-unik + hapus-baris): pakai email **unik per-run** (mis. `e2e-cw-{timestamp}@local.test`), teardown via jalur **DeleteWorker POST** (Identity cascade hapus `AspNetUserRoles` otomatis — F-NEW-07; jangan raw-SQL delete yang skip cascade), teardown jalan **walau test gagal**. Catat 1 baris di `docs/SEED_JOURNAL.md` (CLAUDE.md Seed Workflow). Lebih ringan dari snapshot/restore penuh — cukup untuk 1 baris transient self-cleaning.

### Scope
- **D-08:** Scope = `Views/Admin/CreateWorker.cshtml` **SAJA**. `WorkerController.cs` + `ManageUserViewModel.cs` UNCHANGED (verify `git diff` 0-diff). `EditWorker.cshtml` **TIDAK disentuh** (user pilih CreateWorker-only; kunci AD di Edit defensible krn sumber sync sudah ada). 0 migration. Divergensi create-editable / edit-locked = keputusan sadar.

### Claude's Discretion
- Wording final teks info AD (D-02) — rekonsiliasi: editable + diselaraskan-AD-saat-login + ingatkan-cocokkan-akun-AD.
- Apakah tambah span Role (D-04) — error unreachable; boleh skip atau tambah demi konsistensi.
- Format persis email-unik + mekanisme teardown (D-07).
- Apakah catatan pengingat AD ditulis terpisah atau cukup nyatu di teks info ter-reword (D-02 sudah menutup ini).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing. RE-READ `CreateWorker.cshtml` FRESH — line anchor bisa bergeser; treat audit JSON sebagai hipotesis, verifikasi ke file live (F-NEW-12).**

### Requirements
- `.planning/REQUIREMENTS.md` — WRKR-01..03 + Out of Scope (controller/model CreateWorker unchanged, AD-sync/provisioning, migration).

### Target edit (VIEW-ONLY)
- `Views/Admin/CreateWorker.cshtml` — satu-satunya file diubah. Anchor saat ini: readonly+bg-light FullName ~L62-64 & Email ~L73-75 (D-01); teks info AD ~L65-68 / ~L76-79 (D-02); org fields Position ~L106-108 / Directorate ~L110-112 / Section ~L113-117 / Unit ~L119-123 (D-04); span existing L69/80/85/90/158/168 (JANGAN duplikasi); blok script bawah ~L194-205 (D-05 pindah ke @section Scripts).

### FROZEN (baca utk konteks, JANGAN ubah)
- `Controllers/WorkerController.cs` — `CreateWorker` GET ~L194 / POST L208-310. Server ModelState keys: Email (L252 "sudah terdaftar"), Section (L227 "tidak ditemukan"), Unit (L234 "tidak valid"), Password (L218 local-only). Redirect L300 `ManageWorkers` + `TempData["Success"]`. `ViewBag.SectionUnitsJson` L204/243. AD password auto-generate L278.
- `Models/ManageUserViewModel.cs` — `[Required]` FullName (L15) + Email (L20) + Role (L47); `[EmailAddress]` Email (L21); `[Compare("Password")]` ConfirmPassword; Position/Directorate/Section/Unit/NIP/JoinDate/Password = optional.

### Pola reuse (D-05 validasi live)
- `Views/Account/Settings.cshtml:137-146` — preseden persis `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") ... }`.
- `Views/Shared/_ValidationScriptsPartial.cshtml` — partial yang di-include; lib jquery-validation ada di `wwwroot/lib/jquery-validation*/dist/`.
- `Views/Shared/_Layout.cshtml` — jQuery footer ~L241, `@RenderSectionAsync("Scripts", required:false)` ~L267 (konteks urutan load).
- `wwwroot/js/shared-cascade.js` — `initSectionUnitCascade` (cascade Section→Unit, sudah benar) + `togglePassword`. Catatan: placeholder "-- Pilih Unit --" hard-coded di JS (shared file, BUKAN diubah di phase ini).

### Konteks login (batas scope, JANGAN ubah)
- `Controllers/AccountController.cs` ~L63-116 — login flow + AD profil-sync overwrite FullName/Email saat login.
- `Services/HybridAuthService.cs` + `LdapAuthService.cs` + `LocalAuthService.cs` — non-admin = AD-only bind; login AD divalidasi server AD (bukan halaman ini).

### Verifikasi & cleanup
- [[reference_local_e2e_sql_env_fix]] — SQL env lokal, AD-off, Playwright combined WAJIB `--workers=1`.
- `HcPortal.Tests` / `tests/e2e/` — harness Playwright.
- `docs/SEED_JOURNAL.md` + `docs/SEED_WORKFLOW.md` — journal cleanup (D-07).
- Lesson Phase 354 (Razor dinamis + cascade JS → Playwright runtime wajib).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Span inline existing (FullName/Email/NIP/JoinDate/Password/ConfirmPassword) — reuse class `text-danger small`; JANGAN dobel.
- `_ValidationScriptsPartial` + pola `Settings.cshtml` — reuse untuk D-05 (validasi live).
- `shared-cascade.js initSectionUnitCascade` — cascade Section→Unit sudah benar (F-VIEW-10); jaga saat dipindah ke `@section Scripts`.
- POST handler mempersist SEMUA field; `UserName=Email`, `EmailConfirmed=true`, redirect `ManageWorkers` + `TempData["Success"]` (F-CTRL-02).

### Established Patterns
- Mode AD vs lokal di-branch via `Config.GetValue<bool>("Authentication:UseActiveDirectory")` (view L6) — password block disembunyikan saat AD (`@if(!isAdMode)`), AD info-text muncul saat AD.
- Server menambah ModelState error keyed Email/Section/Unit/Password → span inline + `<div asp-validation-summary>` (existing L34-46) surface-kan. Identity error key "" → summary saja.
- Validasi org server-side (Section harus ada di OrganizationUnits, Unit valid utk Section) — span Section/Unit = jaring pengaman pesan ini.

### Integration Points
- `_Layout` `@RenderSectionAsync("Scripts")` ~L267 — titik mount D-05.
- POST re-render mempertahankan `ViewBag.SectionUnitsJson` (L243-244) → cascade tetap valid pasca reject.
- Sukses → redirect keluar view ini ke `ManageWorkers` (pesan sukses dirender di sana, bukan CreateWorker).
</code_context>

<specifics>
## Specific Ideas

- User menegaskan tujuan halaman: **buat akun pekerja agar bisa login** — dan mengklarifikasi akun **selalu untuk pekerja yang sudah ada di AD Pertamina**. Maka kunci sukses praktis = Email yang diketik **cocok dengan akun AD** (D-02 mengingatkan ini).
- User pilih **validasi live aktif** (pengalaman form lebih baik) + **scope CreateWorker saja** + cleanup **email-unik+hapus** (ringan).
- Audit multi-agent (4 agen, 2026-06-17) mengoreksi 3 asumsi keliru: FullName SUDAH `[Required]`; mayoritas field SUDAH punya span (cuma 4 org + Role yang kurang); validasi client-side BISA diaktifkan view-only (bukan out-of-scope).
</specifics>

<deferred>
## Deferred Ideas

- **AD provisioning / membuat login AD benar-benar jalan untuk akun baru** — kerja auth/provisioning terpisah (REQUIREMENTS Out of Scope). User konfirmasi akun pre-exist di AD → tak mendesak.
- **EditWorker.cshtml** punya pola readonly+bg-light identik (L69-70/79-80) — **sengaja TIDAK diperbaiki** (user pilih CreateWorker-only; kunci AD di Edit defensible krn sumber sync ada). Catat sebagai divergensi sadar; buka bila ada keluhan UX.
- **shared-cascade.js placeholder "-- Pilih Unit --" hard-coded** (i18n inconsistency vs `@OrgLabels.GetLabel(1)`) — file JS shared, bukan view-only phase ini.
- **Email↔AD-username mapping risk** (LdapAuthService bind pakai email; AD Pertamina sering harap UPN/`DOMAIN\samAccountName`) — laten, butuh kerja auth (OUT of scope).

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" (area: database, score 0.6) — **tidak di-fold**: housekeeping Phase 367, tak terkait CreateWorker. Tetap di backlog.

[Tidak ada scope-creep lain — diskusi tetap dalam batas phase view-only.]
</deferred>

---

*Phase: 392-perbaikan-createworker-audit-field*
*Context gathered: 2026-06-17*
