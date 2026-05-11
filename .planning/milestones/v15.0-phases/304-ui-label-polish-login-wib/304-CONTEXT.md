# Phase 304: UI Label Polish (Login + WIB) - Context

**Gathered:** 2026-04-28
**Status:** Ready for planning
**REQ:** AUTH-01, WIZ-02, WIZ-03

<domain>
## Phase Boundary

Polishing UI di 2 view existing untuk audit findings 27 April 2026:

1. **`Views/Account/Login.cshtml`** — Tambah toggle eye-icon untuk visibility password (AUTH-01)
2. **`Views/Admin/CreateAssessment.cshtml` Step 3** — Tambah label "(WIB)" untuk semua input date/time (WIZ-02)
3. **`Views/Admin/CreateAssessment.cshtml` Step 4 summary** — Tambah suffix " WIB" untuk semua datetime di JS populateSummary (WIZ-03)

**Tidak termasuk dalam phase ini:** Migrasi DB, behavior change submit, validation logic, multi-timezone support, EditAssessment update, password auto-revert masked, screen reader text format change.

</domain>

<decisions>
## Implementation Decisions

### AUTH-01: Eye-icon Toggle Password Visibility (Login.cshtml)

- **D-01:** Tombol toggle ditempatkan **append kanan input** di dalam `input-group input-group-lg` password (`Login.cshtml` line 179-183). `<span class="input-group-text"><i class="bi bi-lock-fill">` di kiri **tetap dipertahankan** sebagai visual cue.
- **D-02:** Icon style menggunakan `bi-eye` (state tersembunyi → klik untuk show) dan `bi-eye-slash` (state tampil → klik untuk hide). Bootstrap Icons sudah ter-load via CDN (`Login.cshtml` line 14).
- **D-03:** Tombol pakai `type="button"` (wajib, success criteria #1) supaya tidak men-submit form. Class struktur: `<button type="button" class="btn btn-outline-secondary" id="togglePwd">`.
- **D-04:** Aksesibilitas pakai **aria-label dinamis** + `aria-pressed`:
  - Default: `aria-label="Tampilkan kata sandi"` + `aria-pressed="false"`
  - Setelah klik: `aria-label="Sembunyikan kata sandi"` + `aria-pressed="true"`
  - Toggle keduanya saat click.
- **D-05:** Tombol ditampilkan **di kedua mode** (`UseActiveDirectory=true` dan local). Banner AD info (line 161-167) tidak diubah. UX consistent karena AD mode tetap pakai field password (LDAP bind).
- **D-06:** Initial state saat halaman load: `type="password"`, icon `bi-eye`, `aria-pressed="false"`. Tombol selalu tampil (tidak hide saat input kosong).
- **D-07:** Tidak ada handling khusus untuk browser autofill — toggle berfungsi normal saat password autofilled.
- **D-08:** Touch target inherit dari `input-group-lg` (padding ~0.8rem 1rem dari styling line 83) — sudah memenuhi WCAG 2.5.5 minimum 44x44px. Tidak perlu media query khusus mobile.
- **D-09:** JS placement: **inline `<script>` block** sebelum `</body>` di `Login.cshtml`. Login view sudah self-contained (`Layout = null`, inline CSS). Tidak extract ke `wwwroot/js/` — premature abstraction (YAGNI, hanya 1 form login).

### WIZ-02: Label "(WIB)" di Step 3 Wizard (CreateAssessment.cshtml)

- **D-10 [REVISED]:** Format menggunakan **helper text di bawah input** mengikuti pattern existing `EditAssessment.cshtml`:
  ```html
  <label>...*</label>
  <input type="time" .../>
  <div class="form-text text-muted">Waktu (WIB)</div>
  ```
  - **Pattern reference:** `EditAssessment.cshtml` lines 241, 247, 299, 305, 360, 366, 435, 441 — sudah pakai persis format ini.
  - **Bukan inline** dalam label utama (untuk konsistensi visual antara Create dan Edit).
- **D-11 [SCOPE EXTENSION]:** Apply ke **8 input** (extend dari 6 yang di-list di ROADMAP success criteria — user explicit decision):
  | Line | Field | Helper text |
  |------|-------|-------------|
  | 358 (`schedDateInput`) | Tanggal Jadwal | `Tanggal (WIB)` |
  | 363 (`schedTimeInput`) | Waktu Jadwal | `Waktu (WIB)` |
  | 378 (`ewcdDateInput`) | Tanggal Tutup Ujian | `Tanggal (WIB)` |
  | 385 (`ewcdTimeInput`) | Waktu Tutup Ujian | `Waktu (WIB)` |
  | 405 (`preSchedule`) | Pre-Test Tanggal & Waktu | `Tanggal & Waktu (WIB)` |
  | 413 (`preExamWindowCloseDate`) | Pre Batas Waktu | `Tanggal & Waktu (WIB)` |
  | 426 (`postSchedule`) | Post-Test Tanggal & Waktu | `Tanggal & Waktu (WIB)` |
  | 434 (`postExamWindowCloseDate`) | Post Batas Waktu | `Tanggal & Waktu (WIB)` |
- **D-12:** Tidak ada helper text/banner top-section "Waktu lokal Indonesia (UTC+7)". Helper "(WIB)" per-field sudah cukup.

### WIZ-03: Suffix " WIB" di Step 4 Summary JS (CreateAssessment.cshtml)

- **D-13:** Apply suffix `" WIB"` ke **5 datetime** dalam JS `populateSummary`:
  | Line | Variable | Mode | Existing | Baru |
  |------|----------|------|----------|------|
  | 1126 | `summary-pre-schedule` | PrePost | `preSched.value.replace('T', ' ')` | `... + ' WIB'` |
  | 1130 | `summary-pre-ewcd` | PrePost | `preEwcd.value.replace('T', ' ')` | `... + ' WIB'` |
  | 1132 | `summary-post-schedule` | PrePost | `postSched.value.replace('T', ' ')` | `... + ' WIB'` |
  | 1136 | `summary-post-ewcd` | PrePost | `postEwcd.value.replace('T', ' ')` | `... + ' WIB'` |
  | 1177 | `summary-ewcd` | Standard | `ewcdDateEl.value + ' ' + ewcdTimeEl.value` | `... + ' WIB'` |
  - Line 1164 (Standard `summary-schedule`) sudah punya `+ ' WIB'` — tidak diubah.
- **D-14:** Format string: `value.replace('T', ' ') + ' WIB'` → `'2026-04-28 14:30 WIB'`. Minimal change dari kode existing. Tidak parse ke Date object (tidak format Indonesian-friendly seperti '28 Apr 2026, 14:30 WIB' — defer untuk phase berikutnya jika diminta).
- **D-15:** Tidak extract `populateSummary` ke `wwwroot/js/wizardSummary.js` — edit inline existing JS.

### Validation & Behavior Guards (semua REQ)

- **D-16:** `invalid-feedback` messages (Login, Step 3, Step 4) **tidak diubah**. Hanya label/helper text dan JS output yang berubah.
- **D-17:** Eye-icon tidak interfere jQuery validate. Pakai `type="button"`. aria-pressed/icon swap tidak trigger event validation.
- **D-18:** **DOM ID & `name` attribute tidak berubah**: `inputPassword`, `schedTimeInput`, `ewcdTimeInput`, `preSchedule`, `preDurationMinutes`, `preExamWindowCloseDate`, `postSchedule`, `postDurationMinutes`, `postExamWindowCloseDate`, dst. Server-side model binding tetap.
- **D-19:** Tag helper `asp-for="Schedule"` (line 392), `asp-for="ExamWindowCloseDate"` (line 393), `asp-for="DurationMinutes"` (line 367/370) tetap. Hidden input `schedHidden` & `ewcdHidden` tidak diubah.

### Claude's Discretion

- Detail visual styling tombol toggle (ukuran spesifik, hover state, focus ring) — pilih yang konsisten dengan Bootstrap 5.3 default `btn-outline-secondary` atau `btn-link`.
- Nama variabel JavaScript untuk eye-icon toggle (`togglePwd`, `pwdInput`, dll) — pakai naming readable.
- Order tombol vs input border-radius rounding (apakah pakai `btn` di akhir input-group yang inherit border-radius kanan) — pilih yang menjaga visual seamless dengan input field.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Source & Requirements
- `.planning/ROADMAP.md` §"v15.0 Audit Findings 27 April 2026" — Phase 304 success criteria 5 items
- `.planning/REQUIREMENTS.md` §"Authentication UX" (AUTH-01) + §"Create Assessment Wizard" (WIZ-02, WIZ-03) + §"Out of Scope" (eksklusi)
- `.planning/STATE.md` — current focus & deferred items

### View Files (target perubahan)
- `Views/Account/Login.cshtml` — target AUTH-01 (eye-icon)
  - Lines 14, 122 (Bootstrap Icons CDN)
  - Lines 161-167 (banner AD mode)
  - Lines 169-183 (form email + password)
  - Lines 191-194 (submit button MASUK KE SISTEM)
- `Views/Admin/CreateAssessment.cshtml` — target WIZ-02, WIZ-03
  - Lines 355-388 (Standard mode date/time inputs)
  - Lines 392-393 (hidden combiners `asp-for`)
  - Lines 396-444 (PrePost section pre/post schedule)
  - Lines 1110-1192 (`populateSummary` JS — Step 4)

### Pattern Reference (helper text style untuk WIZ-02)
- `Views/Admin/EditAssessment.cshtml` lines 241, 247, 299, 305, 360, 366, 435, 441 — pattern `<div class="form-text text-muted">Tanggal (WIB)</div>` / `Waktu (WIB)`. **Reuse exact class & text format.**

### Convention References
- `.planning/codebase/STACK.md` — Bootstrap 5.3, Bootstrap Icons (CDN), jQuery + jQuery Validation
- `.planning/codebase/CONVENTIONS.md` §"View Patterns" — Layout single shared, partial views per controller folder

### Out of Scope (eksklusi explicit)
- `Views/Admin/EditAssessment.cshtml` — pattern alignment (defer)
- `Views/CMP/Assessment.cshtml` — sudah konsisten WIB (DateTime.ToString format)
- Worker view & monitoring view — defer ke phase berikutnya jika diminta

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **Bootstrap Icons CDN** sudah ter-load di `Login.cshtml` line 14 dan `_Layout.cshtml` (untuk admin views) — `bi-eye`, `bi-eye-slash`, `bi-lock-fill` semua tersedia tanpa tambahan dependency.
- **Helper text pattern** `<div class="form-text text-muted">…</div>` sudah dipakai di `EditAssessment.cshtml` (8 instance). Reuse class & format persis.
- **jQuery + jQuery Validation** sudah di-include via `_Layout.cshtml` (Admin views) dan tersedia jika diperlukan untuk bind. Untuk eye-icon vanilla JS lebih simple — tidak perlu jQuery.

### Established Patterns

- **Inline `<script>` block** di view: pattern existing di `CreateAssessment.cshtml` (600+ baris JS inline lines 1100+). Konsisten untuk Login.cshtml juga.
- **`<div class="form-text text-muted">{Tanggal|Waktu} (WIB)</div>`** — pattern existing di EditAssessment, replicate persis.
- **Worker view (`Views/CMP/Assessment.cshtml`)** sudah konsisten: `…ToString("dd MMM yyyy, HH:mm", CultureInfo.GetCultureInfo("id-ID")) WIB` (lines 140, 190, 220, 265, 347, 415, 472). Tidak perlu diubah.
- **Convention typing**: input password current `type="password"` (line 181), kontrol via JS `type="text"` ↔ `type="password"`.

### Integration Points

- **Login form submit flow** — Form POST ke `/Account/Login` action via `<form method="post" asp-controller="Account" asp-action="Login">` (line 157). Eye-icon `type="button"` tidak interfere submit.
- **Wizard Step 4 trigger** — `populateSummary` dipanggil saat user navigate ke Step 4 (lihat existing JS context). Perubahan hanya 5 string concatenation, tidak ubah trigger logic.
- **EditAssessment alignment** — Sebagai future phase, ada potensi:
  - Reverse: ubah Edit form-text → inline label (jika consensus team prefer inline)
  - Forward: keep both Create + Edit pakai form-text (current decision)

</code_context>

<specifics>
## Specific Ideas

### Pattern Replication (form-text helper)

User explicit decision: **ikuti pattern existing `EditAssessment.cshtml`** dengan helper text `<div class="form-text text-muted">{Tanggal|Waktu} (WIB)</div>` di bawah input. Bukan inline label.

**Rasionalisasi:**
- Konsistensi visual antara Create & Edit (kedua form punya 8 field date/time yang sama secara semantik).
- Helper text menjadikan WIB sebagai informasi tambahan, bukan part of label utama (lebih clean).
- Sudah ada precedent yang battle-tested di Edit.

### Eye-icon Visual Continuity

User pilih append kanan input — bi-lock-fill kiri tetap. Visual cue "ini field password" tidak hilang. Eye-icon di sisi kanan = pattern yang familiar dari major SaaS (Google, Microsoft, GitHub login forms).

### WIB Format JS Minimalism

User pilih `replace('T', ' ') + ' WIB'` daripada parse Date object. **Why minimal:**
- Kode existing sudah pakai `replace('T', ' ')` (lines 1126, 1130, 1132, 1136).
- Tambah parse Date akan butuh handling timezone offset (datetime-local di browser = local time interpretasi).
- Minimal regression risk. User bisa request Indonesian-friendly format (`28 Apr 2026, 14:30 WIB`) di phase berikutnya jika perlu.

</specifics>

<deferred>
## Deferred Ideas

### Out of Phase 304 Scope

1. **EditAssessment alignment ke pattern lain** — Saat ini Edit pakai form-text (sudah benar per decision). Jika team future ingin shift ke inline label, butuh update Edit + Create bersama. Tidak ada bug user-facing.
2. **Indonesian-friendly date format di Step 4 summary** — Format `'28 Apr 2026, 14:30 WIB'` lebih readable untuk user awam. Saat ini pakai `'2026-04-28 14:30 WIB'`. Defer sampai user request explicit.
3. **Helper text top-of-section "Semua waktu dalam zona WIB (UTC+7)"** — Tidak diadopsi. Bisa direvisit jika audit feedback selanjutnya minta UTC hint untuk user multi-timezone (out-of-scope per REQUIREMENTS).
4. **Password auto-revert masked setelah X detik** — REQUIREMENTS Out of Scope. T1 differentiator yang ditolak untuk milestone v15.0.
5. **Worker view / Monitoring view WIB consistency check** — `Views/CMP/Assessment.cshtml` sudah konsisten. View lain belum dicek (e.g., `AssessmentMonitoring.cshtml`, `AssessmentMonitoringDetail.cshtml`). Jika audit lanjutan menemukan inkonsistensi, buat phase tersendiri.
6. **Eye-icon di view password lain** — Saat ini hanya `Login.cshtml`. Tidak ada Reset/ChangePassword view di repo ini. Jika nanti ditambah, ada potensi extract toggle JS ke `wwwroot/js/password-toggle.js` untuk reuse.

### Reviewed Todos (not folded)

Tidak ada todo yang teridentifikasi di proses cross-reference (todo match-phase tidak dipanggil eksplisit di sesi ini — phase 304 cukup spesifik dari REQUIREMENTS).

</deferred>

---

*Phase: 304-ui-label-polish-login-wib*
*Context gathered: 2026-04-28*
