---
phase: 321
slug: assessment-edit-jawaban-peserta
status: draft
shadcn_initialized: false
preset: none
created: 2026-05-21
language: id-ID
---

# Phase 321 — UI Design Contract

> Halaman edit jawaban admin/HC + dropdown ⋮ hybrid + Activity Log "Edit History" tab + SignalR toast. Sumber kebenaran visual + interaksi untuk planner/executor/checker.
>
> Stack frontend sudah locked (CONTEXT D-12 + RESEARCH §Tech Stack): Bootstrap 5 + vanilla JS + jQuery + Bootstrap Icons CDN. NO framework baru. Semua copy Bahasa Indonesia (CLAUDE.md).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (no shadcn — backend Razor stack) |
| Preset | not applicable |
| Component library | Bootstrap 5 (`dropdown`, `modal`, `toast`, `tab`, `form-check`, `card`, `alert`, `badge`, `list-group`) |
| Icon library | Bootstrap Icons (CDN via `_Layout.cshtml`) — kelas `bi bi-*` |
| Font | Bootstrap 5 default system stack (`-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`) — diwariskan dari `_Layout.cshtml` |
| CSS source | `wwwroot/css/site.css` (existing). Tidak ada file CSS baru — semua override inline `style=""` minimal. |
| Color tokens | Bootstrap CSS variables existing (`--bs-primary`, `--bs-warning`, `--bs-danger`, `--bs-success`, `--bs-secondary`) |
| Toast pattern | Existing `window.showAssessmentToast` dari `wwwroot/js/assessment-hub.js` (kelas `.assessment-toast`). Reuse — JANGAN buat container toast baru. |

**Detection:**
- `components.json` not found — pre-React/Vue stack, gate shadcn N/A.
- `tailwind.config.*` not found — Bootstrap-only.
- Existing CSS: `assessment-hub.css`, `guide.css`, `home.css`, `site.css`. No new CSS file in Phase 321.

---

## Spacing Scale

Mengikuti Bootstrap 5 spacing utilities (kelipatan `$spacer` = 16px). Semua nilai turunan multiples of 4.

| Token | Bootstrap class | Value | Usage di Phase 321 |
|-------|-----------------|-------|--------------------|
| xs | `gap-1`, `m-1`, `p-1` | 4px | Gap antar icon + text di tombol (`me-1`, `gap-1`) |
| sm | `gap-2`, `m-2`, `p-2` | 8px | Gap tombol inline ↔ dropdown ⋮; padding card body kecil |
| md | `gap-3`, `m-3`, `p-3`, `mb-3` | 16px | Default jarak antar `question-card`; card body padding |
| lg | `py-3` container, `mb-4` | 16-24px | Container padding atas-bawah halaman edit |
| xl | `py-4`, `mt-4` | 32px | Pemisah section (header → form, form → footer) |

Exceptions:
- Touch target minimal **40px tinggi** untuk `dropdown-toggle ⋮` di tabel monitoring (Bootstrap `btn-sm` default = 31px; tambah `style="min-height:40px"` pada tombol toggle saja, atau gunakan padding `py-2`). Rationale: A11y WCAG 2.5.5 — touch target 44×44 ideal, 40px acceptable untuk Bootstrap btn-sm context. Tombol inline `View Results` + `🕐` mempertahankan ukuran existing demi konsistensi tabel (no regression).

---

## Typography

Mengikuti Bootstrap 5 default + sistem reems (`1rem = 16px` di root). 4 ukuran efektif yang dipakai Phase 321:

| Role | Bootstrap class | Size | Weight | Line Height | Pemakaian |
|------|----------------|------|--------|-------------|-----------|
| Body | (default) | 16px | 400 (regular) | 1.5 | Question text, option label, modal body, partial Edit History entry |
| Label / Small | `small`, `form-label small` | 14px | 400 | 1.5 | Reason label, session metadata header, timestamp di Edit History, badge metadata |
| Heading | `h3`, `modal-title`/`h5` | 20px | 600 (semibold via `fw-semibold` / default h5) | 1.3 | Page title "Edit Jawaban — {FullName} (NIP {nip})", modal flip title "⚠️ Perubahan Hasil" |
| Strong / Emphasis | `<strong>`, `fw-bold` | inherit | 700 (bold) | inherit | Highlight skor angka, "menggagalkan peserta" / "meluluskan peserta" di modal copy, soal nomor |

Weights efektif **2**: regular (400) + semibold (600). Bold (700) hanya untuk inline `<strong>` di modal warning copy — bukan font weight token mandiri.

Eksepsi:
- Tidak ada custom font-size declaration di CSS Phase 321. Semua via Bootstrap utility classes (`small`, `fw-semibold`, `fw-bold`).

---

## Color

Mengikuti palet Bootstrap 5 default (project konvensi — bukan Pertamina brand red/blue, sesuai precedent Phase 320 D-02 untuk admin tool internal).

| Role | Token | Hex (default Bootstrap) | Usage di Phase 321 |
|------|-------|-------------------------|--------------------|
| Dominant (60%) | `body-bg` / `bg-white` | `#ffffff` | Background halaman edit, card body, modal body |
| Secondary (30%) | `bg-light`, `card` border, `text-muted` | `#f8f9fa` border `#dee2e6`, text `#6c757d` | Card metadata header, alert warning background `#fff3cd`, partial timeline border, navbar/sidebar existing |
| Accent (10%) | `btn-primary`, `text-primary`, `--bs-primary` | `#0d6efd` | (1) Tombol primary `Save & Recompute` di edit form, (2) tombol primary `Lanjutkan, simpan perubahan` di flip modal, (3) tab nav `nav-link.active` di Activity Log, (4) link breadcrumb/back |
| Destructive | `btn-danger`, `text-danger`, `bg-danger` | `#dc3545` | (1) Badge `✗ Salah` di question-card header, (2) result cell `text-danger` Fail, (3) asterisk `*` "wajib", (4) error TempData banner "Sesi sudah diubah admin lain. Refresh halaman." |
| Success (semantic) | `btn-success`, `text-success`, `bg-success` | `#198754` | (1) Badge `✓ Benar` + `Kunci`, (2) result cell `text-success` Pass, (3) arrow "→" di partial Edit History new answer |
| Warning (semantic) | `alert-warning`, `bg-warning text-dark` | `#ffc107` bg `#fff3cd` | (1) Alert "⚠️ Catatan: Edit jawaban akan recompute…" di edit page, (2) modal flip header icon "⚠️", (3) toast info border-left (optional) |
| Info (toast) | reuse `.assessment-toast` (existing custom) | per `assessment-hub.css` | SignalR `workerAnswerEdited` toast top-right |

**Accent reserved for:**
1. Primary submit CTA (`btn-primary` `Save & Recompute`)
2. Primary confirmation CTA (`btn-primary` `Lanjutkan, simpan perubahan`)
3. Active tab indicator (`nav-link.active` underline)
4. Back/breadcrumb link

NEVER untuk: row hover (gunakan `bg-light`), border default (gunakan `border-secondary`), badge non-CTA (gunakan `bg-secondary`).

**Color not sole indicator (A11Y spec §5.7):**
- Pass/Fail: text label "Pass"/"Fail" + class `text-success`/`text-danger` (text + color, bukan color only).
- Benar/Salah badge: prefix icon `✓`/`✗` + text + color (3-channel redundancy).
- Flip modal: ikon `⚠️` di header + heading bold "Perubahan Hasil" + body text eksplisit (icon + text + warning bg).

---

## Copywriting Contract

Semua copy **Bahasa Indonesia** (CLAUDE.md mandatory). Verbose & eksplisit konsekuensi (CONTEXT D-05/D-06/D-07 locked).

### Halaman Edit (`/AssessmentAdmin/EditPesertaAnswers/{id}`)

| Element | Copy |
|---------|------|
| Page title (`<title>` + `<h3>`) | `Edit Jawaban — {FullName} (NIP {NIP})` |
| Back link | `← Back to Monitoring` |
| Metadata header (card) | `{AssessmentTitle} · Kategori: {Category} · Schedule: {dd MMM yyyy HH:mm}` lalu baris kedua `Skor saat ini: {Score}% · Status: {Pass/Fail/—}` |
| Warning banner (alert-warning) | `⚠️ Catatan: Edit jawaban akan recompute skor + spider chart otomatis. Aksi ini di-log audit. Hasil tidak ditampilkan ke peserta.` |
| Soal label | `Soal {N}` + badge `MC` / `MA` + badge `✓ Benar` atau `✗ Salah` |
| Kunci marker | badge hijau `Kunci` di samping opsi yang `IsCorrect == true` |
| Reason field label | `Alasan edit *` (asterisk merah `*` untuk "wajib") |
| Reason placeholder | `— Pilih —` |
| Reason free-text placeholder | `Detail alasan (wajib kalau pilih Lainnya)` |
| Primary CTA | `Save & Recompute` |
| Secondary CTA | `Cancel` |
| Stale concurrency error (TempData) | `Sesi sudah diubah admin lain. Refresh halaman.` |

**Reason dropdown labels (CONTEXT D-05 verbose, code value PascalCase):**

| Code value (DB) | Label (UI) |
|-----------------|-----------|
| `(empty)` | `— Pilih —` |
| `SoalSalah` | `Soal salah / typo` |
| `KunciSalah` | `Kunci jawaban salah` |
| `BugSistem` | `Bug sistem / glitch` |
| `PermintaanPeserta` | `Permintaan koreksi peserta` |
| `Lainnya` | `Lainnya (jelaskan)` |

> Note: RESEARCH Task 6 markup pakai label pendek (`Soal salah`, `Kunci jawaban salah`, dst.). Executor WAJIB pakai versi verbose CONTEXT D-05 di atas — override RESEARCH markup short-form.

### Flip Confirmation Modal (CONTEXT D-06 locked)

| State | Modal title | Body copy | Primary button | Secondary button |
|-------|-------------|-----------|----------------|------------------|
| Pass → Fail | `⚠️ Perubahan Hasil` | `Perubahan ini akan **menggagalkan peserta**. NomorSertifikat akan dicabut dan TrainingRecord di-set Failed. Lanjutkan?` | `Lanjutkan, simpan perubahan` (btn-primary) | `Batal` (btn-outline-secondary) |
| Fail → Pass | `⚠️ Perubahan Hasil` | `Perubahan ini akan **meluluskan peserta**. NomorSertifikat baru akan di-generate (kalau eligible: GenerateCertificate && bukan PreTest). Lanjutkan?` | `Lanjutkan, simpan perubahan` | `Batal` |
| No flip (score only) | `Konfirmasi Perubahan` | `Skor akan berubah dari {oldScore}% → {newScore}%. Status tetap {Pass/Fail}. Lanjutkan?` | `Lanjutkan, simpan perubahan` | `Batal` |

**Konvensi bold:** Frase "menggagalkan peserta" / "meluluskan peserta" pakai `<strong>` untuk emphasis. JANGAN pakai checkbox layer tambahan (CONTEXT D-06 locked).

### Dropdown ⋮ items (CONTEXT locked — dropdown hybrid)

| Position | Item | Icon | Label |
|----------|------|------|-------|
| Inline (button 1) | View Results | `bi-bar-chart-line` (existing) atau text only | `View Results` |
| Inline (button 2) | Activity Log | `bi bi-clock-history` (existing) | `🕐` (icon-only, tooltip `Lihat activity log`) |
| Dropdown ⋮ (toggle) | Aksi lain | (no icon, `⋮` glyph) | `aria-label="Aksi lain untuk {workerName}"` |
| Dropdown item 1 (conditional `IsEditable`) | Edit Jawaban | `bi bi-pencil-square` | `Edit Jawaban` |
| Dropdown item 2 (conditional `Status != Cancelled`) | Reset | `bi bi-arrow-counterclockwise` | `Reset` |
| Dropdown item 3 (conditional `Status == InProgress`) | Akhiri Ujian | `bi bi-x-octagon` | `Akhiri Ujian` |
| Dropdown item 4 (conditional Package mode + `Not Started`/`Abandoned`) | Reshuffle | `bi bi-shuffle` | `Reshuffle` |

> Note: RESEARCH Task 10 markup pakai emoji (`✏️`, `🔄`, `❌`, `🔀`). Executor preferensi `bi bi-*` icons untuk konsistensi dengan existing monitoring page (line 318 `bi-arrow-counterclockwise`, line 336 `bi-clock-history`). Emoji OK sebagai fallback kalau Bootstrap Icons class tidak match — tapi konsisten satu approach per page.

### Toast SignalR `workerAnswerEdited` (CONTEXT D-07 locked)

| Element | Template |
|---------|----------|
| Message (1 line) | `{actorRole} {actorName} edit jawaban {workerName}: Score {oldScore}→{newScore}, {oldResult}→{newResult}` |
| Contoh konkret | `Admin Rino edit jawaban Budi Santoso: Score 65→75, Fail→Pass` |
| Position | Top-right (existing `.assessment-toast` container — reuse `window.showAssessmentToast`) |
| Auto-dismiss | 8 detik |
| Persist on click | Klik toast → cancel auto-dismiss timer (existing pattern) |
| No-flip case (score only) | `{actorRole} {actorName} edit jawaban {workerName}: Score {oldScore}→{newScore}` (tanpa segment ", {flip}") |
| Severity / level | `info` (no error/danger styling; ini event audit normal) |

### Activity Log "Edit History" Tab

| Element | Copy |
|---------|------|
| Tab nav label (existing tab) | `Activity Timeline` |
| Tab nav label (new tab) | `Edit History` |
| Loading placeholder | `Memuat...` |
| Empty state (no edits yet) | `Belum ada edit untuk sesi ini.` |
| Error state (AJAX fail) | `Gagal memuat edit history.` |
| Entry timestamp prefix | `[yyyy-MM-dd HH:mm] Soal #{questionId}` |
| Old → New arrow | `→` (text arrow, accent ke `text-success` di sisi New) |
| Actor line | `oleh {actorRole} ({actorName})` |
| Reason line | `Alasan: {ReasonLabel}` lalu, kalau Lainnya, `— {ReasonText}` |

> Note: Mapping ReasonCode → ReasonLabel di partial WAJIB pakai versi verbose CONTEXT D-05. Tambahkan helper kecil di partial (e.g. `@switch (log.ReasonCode) { ... }` atau static helper `ReasonLabels.Get(code)`).

### Empty / Loading / Error States Summary

| Konteks | Copy |
|---------|------|
| Edit page — session tidak eligible (GET 403/redirect) | TempData error `Sesi ini tidak dapat diedit (status bukan Completed, atau IsManualEntry, atau Assessment Proton Tahun 3).` |
| Edit page — reason kosong (client validation) | Inline error di reason-block: `Pilih alasan edit terlebih dahulu.` (`text-danger small`) |
| Edit page — ReasonCode=Lainnya tapi text kosong | Inline error: `Isi detail alasan untuk opsi Lainnya.` |
| Edit page — server error 500 | Generic banner `Terjadi kesalahan saat menyimpan. Coba lagi atau hubungi administrator.` |
| Preview AJAX gagal | Console + toast `Gagal memuat preview skor.` (info level) |
| Edit history tab empty | `Belum ada edit untuk sesi ini.` (text-muted) |
| Edit history tab error | `Gagal memuat edit history.` (placeholder text-danger) |

### Destructive Actions Inventory

| Aksi | Confirmation strategy |
|------|----------------------|
| Submit edit yang trigger Pass→Fail | Flip modal `⚠️ Perubahan Hasil` (Pass→Fail copy) sebelum POST submit (via PreviewEditScore dry-run AJAX) |
| Submit edit yang trigger Fail→Pass | Flip modal `⚠️ Perubahan Hasil` (Fail→Pass copy) sebelum POST submit |
| Submit edit score-only no flip | Optional konfirmasi sederhana (CONTEXT belum lock — boleh skip atau pakai modal generic "Konfirmasi Perubahan") — recommend: skip kalau no flip + tampilkan diff di toast pasca-save |
| Cancel form dengan dirty state | Browser native `beforeunload` prompt (recommend di CD-03 dirty state JS) atau modal Bootstrap "Perubahan belum disimpan. Yakin batal?" + tombol `[Tetap di halaman]` `[Batal & buang perubahan]` |
| Dropdown ⋮ Reset / Akhiri Ujian | Pertahankan behavior existing (sudah ada confirm dialog) — Phase 321 tidak ubah copy |

---

## Component Inventory

Komponen Bootstrap 5 yang DIPAKAI per UI surface. Semua existing — no new component pattern.

### Surface 1: Edit Page (`Views/AssessmentAdmin/EditPesertaAnswers.cshtml` — CREATE)

| Component | Bootstrap class | Pemakaian |
|-----------|----------------|-----------|
| Container | `.container-fluid py-3` | Page wrapper |
| Back button | `.btn .btn-sm .btn-outline-secondary` | Navigasi balik ke MonitoringDetail |
| Header heading | `<h3 class="mt-3">` | "Edit Jawaban — …" |
| Metadata card | `.card .mb-3 > .card-body.small` | Info sesi (title, kategori, schedule, score, status) |
| Warning alert | `.alert .alert-warning` | "⚠️ Catatan: Edit jawaban akan recompute…" |
| Form | `<form id="editAnswersForm" method="post">` + `@Html.AntiForgeryToken()` | Submit edit batch |
| Hidden inputs | `<input type="hidden">` | `SessionId`, `UpdatedAt` (concurrency token ISO 8601 round-trip `"O"` format) |
| Question card | `.card .mb-3.question-card` (data attrs `data-question-id`, `data-question-type`) | Container per soal |
| Question type badge | `.badge .bg-secondary` (MC/MA) + `.badge .bg-success/.bg-danger` (Benar/Salah current) | Header soal |
| Option (MC) | `.form-check > input.form-check-input.answer-input[type=radio]` + `label.form-check-label` | Radio button per opsi |
| Option (MA) | `.form-check > input.form-check-input.answer-input[type=checkbox]` + `label.form-check-label` | Checkbox per opsi |
| Kunci badge | `.badge .bg-success .ms-2` | Marker opsi yang IsCorrect |
| Reason block | `.reason-block.mt-2.d-none` (toggle `.d-none` saat dirty) | Container reason dropdown + free-text |
| Reason select | `.form-select.form-select-sm.reason-code` | Dropdown 5 preset + Lainnya |
| Reason free-text | `<textarea class="form-control form-control-sm mt-2 reason-text d-none" rows="2">` | Toggle visible saat code=Lainnya |
| Submit button | `.btn .btn-primary` `id="submitEditBtn"` | "Save & Recompute" |
| Cancel button | `.btn .btn-outline-secondary` | "Cancel" — link ke MonitoringDetail |
| Flip confirmation modal | `.modal.fade#flipConfirmModal` + `.modal-dialog > .modal-content > .modal-header/.modal-body/.modal-footer` | Konfirmasi sebelum submit (D-06) |

### Surface 2: Dropdown ⋮ Hybrid (`Views/Admin/AssessmentMonitoringDetail.cshtml` — MODIFY)

| Component | Bootstrap class | Pemakaian |
|-----------|----------------|-----------|
| Action wrapper | `.d-flex .gap-1 .align-items-center` | Container action column per row |
| Inline button 1 | `.btn.btn-sm.btn-outline-primary` (untuk Completed) atau `.btn.btn-success.btn-sm` (existing) | "View Results" |
| Inline button 2 | `.btn.btn-outline-secondary.btn-sm.btn-activity-log` + `<i class="bi bi-clock-history">` | Activity Log icon-only |
| Dropdown wrapper | `.dropdown` | Container Bootstrap dropdown |
| Dropdown toggle | `.btn.btn-sm.btn-outline-secondary.dropdown-toggle` (atau remove `.dropdown-toggle` caret + tampilkan `⋮` glyph) + `aria-label`, `aria-expanded` (auto), `data-bs-toggle="dropdown"` | Tombol ⋮ trigger |
| Dropdown menu | `.dropdown-menu.dropdown-menu-end` | Menu items, auto-flip ke kiri di mobile via Popper.js (Bootstrap default) |
| Dropdown item | `<li><a class="dropdown-item">…</a></li>` (or `<button>`) | Per aksi |

### Surface 3: Activity Log Edit History Tab (`Views/Admin/AssessmentMonitoringDetail.cshtml` — MODIFY, modal lokal line 540)

| Component | Bootstrap class | Pemakaian |
|-----------|----------------|-----------|
| Modal | existing `#activityLogModal` (Bootstrap modal `.modal.fade`) | Modal Activity Log existing — refactor body |
| Tab nav | `.nav.nav-tabs` (role=tablist) | 2 tab horizontal |
| Tab button | `<button class="nav-link" data-bs-toggle="tab">` (active = `.active`) | Tombol switch tab |
| Tab content | `.tab-content` > `.tab-pane.fade` (active = `.show.active`) | Container per pane |
| Edit History pane | `.tab-pane.fade#tab-edit-history-{sessionId}` + `data-load-url="@Url.Action(\"EditHistoryPartial\")"` di tab button | Lazy-load via fetch on `shown.bs.tab` event |
| Loading placeholder | `.edit-history-placeholder.text-muted.small` text `"Memuat..."` | Sebelum AJAX load |
| Empty state | `<p class="text-muted">Belum ada edit untuk sesi ini.</p>` | Render dari partial kalau `!Model.Any()` |
| Entry list | `<ul class="list-unstyled">` > `<li class="border-bottom pb-2 mb-2">` per log entry | Timeline format |
| Timestamp line | `.small.text-muted` | `[yyyy-MM-dd HH:mm] Soal #N` |
| Question snapshot | `.fw-bold` | QuestionTextSnapshot |
| Diff line | `<span class="text-muted">{OldAnswerText}</span> → <span class="text-success">{NewAnswerText}</span>` | Old → New display |
| Actor line | `.small` | "oleh {ActorRole} ({ActorName})" |
| Reason line | `.small.text-muted` | "Alasan: {ReasonLabel} — {ReasonText if any}" |

### Surface 4: SignalR Toast (`wwwroot/js/assessment-hub.js` — REUSE; `Views/Admin/AssessmentMonitoringDetail.cshtml` scripts — MODIFY add handler)

| Component | Source | Pemakaian |
|-----------|--------|-----------|
| Toast container | `.assessment-toast` (existing `assessment-hub.css`) | Top-right toast, fade animation |
| Toast invoker | `window.showAssessmentToast(message)` (existing global function) | Reuse — JANGAN buat fungsi baru |
| SignalR handler | `connection.on("workerAnswerEdited", function(data) { … })` | Tambah handler di scripts section MonitoringDetail (setelah `workerSubmitted` handler existing) |

---

## A11y Contract (spec §5.7 mandatory)

| Concern | Implementation |
|---------|---------------|
| Form labels | Setiap `input` punya `id="opt-{questionId}-{optionId}"` + `label[for]` matching. Reason select & textarea punya `<label class="form-label small">` associated via `for` ↔ `id` (executor WAJIB tambah id ke select/textarea — RESEARCH Task 6 markup belum punya). |
| Required indicator | Reason label suffix `<span class="text-danger">*</span>` + `aria-required="true"` di select + textarea (saat code=Lainnya). |
| Modal a11y | `<div class="modal" id="flipConfirmModal" tabindex="-1" aria-labelledby="flipConfirmModalLabel" aria-describedby="flipModalBody">` + `<h5 class="modal-title" id="flipConfirmModalLabel">` + `<div id="flipModalBody">`. Bootstrap modal auto-handle focus trap + ESC close. JS WAJIB restore focus ke `#submitEditBtn` setelah modal close (CONTEXT a11y mandatory). |
| Dropdown a11y | `aria-haspopup="true"` (Bootstrap auto sets `aria-expanded`), `aria-label="Aksi lain untuk {workerName}"` per toggle. Item dropdown adalah `<a>` atau `<button>` — keyboard nav arrow keys auto via Bootstrap. `dropdown-menu-end` + Popper auto-flip di viewport sempit. |
| Tab a11y | `<ul class="nav nav-tabs" role="tablist">` + `<button role="tab" aria-controls="tab-edit-history-{id}" aria-selected="false">` (Bootstrap auto-update aria-selected on switch). |
| Toast a11y | Existing `.assessment-toast` container WAJIB punya `role="status"` + `aria-live="polite"` + `aria-atomic="true"` (verify; tambah kalau belum). |
| Keyboard nav | ESC close modal & dropdown (Bootstrap default), ENTER submit form, TAB order logical (Back → Form fields → Submit). Auto-focus pertama radio/checkbox setelah load. |
| Color not sole indicator | Pass/Fail = text + class. Benar/Salah = icon `✓`/`✗` + text + class. Flip modal = `⚠️` + heading + body text. |
| Focus visible | Pakai default Bootstrap `:focus-visible` outline. JANGAN override `outline:none` di custom CSS. |
| Touch target | Dropdown toggle ⋮ minimal 40px tinggi (eksepsi spacing scale). Other btn-sm default OK karena hover preview di desktop. |

---

## Interaction Contract (per surface)

### Edit Page Flow
1. **GET** `/AssessmentAdmin/EditPesertaAnswers/{id}` → render form dengan jawaban current pre-selected.
2. **User ubah jawaban di question-card X** → JS deteksi dirty (compare `answer-input` snapshot vs current) → tambah class `.question-dirty` (recommended) + asterisk `*` di "Soal {N}" badge + reveal `.reason-block` (hapus `.d-none`). Disable `#submitEditBtn` selama ada `.question-dirty` dengan reason kosong.
3. **User pilih reason `Lainnya`** → reveal `.reason-text` textarea (hapus `.d-none`) + set `required` attr client-side.
4. **User klik `Save & Recompute`** → JS validate semua dirty question punya reason valid → POST AJAX ke `previewUrl` (PreviewEditScore) → kalau response `oldIsPassed != newIsPassed` (flip), tampilkan flip modal dengan body copy sesuai arah flip → user klik `Lanjutkan, simpan perubahan` → trigger `form.submit()` real (POST `SubmitEditAnswers`).
5. **Kalau no flip** → langsung `form.submit()` (atau optional modal "Konfirmasi Perubahan" — opsional, recommend skip).
6. **Concurrency stale** → controller POST detect `UpdatedAt` mismatch → set TempData error → redirect GET → banner muncul atas form: `Sesi sudah diubah admin lain. Refresh halaman.` (kelas `.alert.alert-danger`).

### Dropdown ⋮ Flow
1. **User klik tombol `⋮`** → Bootstrap dropdown open (`aria-expanded="true"`), menu muncul aligned `dropdown-menu-end`.
2. **Mobile viewport sempit** → Popper auto-flip ke `dropdown-menu-start` atau atas kalau bottom clipped.
3. **User klik item `Edit Jawaban`** → navigate ke `/AssessmentAdmin/EditPesertaAnswers/{id}` (new page).
4. **User klik item `Reset`/`Akhiri Ujian`/`Reshuffle`** → reuse handler existing (Phase 321 tidak ubah behavior, cuma reposisi di dropdown).
5. **ESC atau klik luar dropdown** → close otomatis (Bootstrap default).

### Activity Log Tab Flow
1. **User klik `🕐` button per row** → modal `#activityLogModal` open dengan `data-session-id` + `data-worker-name` populated.
2. **Default tab** = `Activity Timeline` (existing content) — pre-loaded.
3. **User klik tab `Edit History`** → JS listen `shown.bs.tab` event → fetch `EditHistoryPartial?sessionId={id}` → replace `.tab-pane` innerHTML dengan response → set `data-loaded="1"` (cache; jangan re-fetch jika user switch balik).
4. **Empty/error states** sesuai copy contract di atas.

### SignalR Toast Flow
1. **Admin A POST `SubmitEditAnswers` sukses** → server broadcast `Clients.Group("monitor-{batchKey}").SendAsync("workerAnswerEdited", payload)`.
2. **Admin B (tab lain, same group)** → handler trigger:
   - Update `tr[data-session-id="{sid}"]` `.session-score` text + `.session-result` class+text
   - Call `window.showAssessmentToast(message)` dengan template D-07
3. **Toast** auto-dismiss 8 detik; klik untuk persist (cancel timer — existing pattern).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Bootstrap 5 (existing CDN) | dropdown, modal, tab, form-check, alert, badge, card, list-group | not applicable — sudah ada di project, no new package |
| Bootstrap Icons (existing CDN) | `bi-clock-history`, `bi-arrow-counterclockwise`, `bi-pencil-square`, `bi-x-octagon`, `bi-shuffle`, `bi-bar-chart-line` | not applicable — existing CDN |
| shadcn official | none | not applicable — stack bukan React |
| Third-party registry | none | not applicable |

No new NuGet/npm dep (CONTEXT D-12 locked). No shadcn registry — stack adalah Razor/Bootstrap server-rendered.

---

## Source Trace

| Field | Source |
|-------|--------|
| Stack (Bootstrap 5 + vanilla JS, no framework) | CONTEXT D-12, RESEARCH §Tech Stack line 15 |
| Reason labels verbose Bahasa Indonesia | CONTEXT D-05 |
| Flip modal copy (Pass→Fail / Fail→Pass) | CONTEXT D-06 |
| Toast template SignalR | CONTEXT D-07 |
| Dropdown hybrid layout (2 inline + dropdown ⋮) | REQ EDIT-12, CONTEXT (UI surfaces locked), RESEARCH Task 10 |
| Activity Log "Edit History" lazy-load tab | REQ EDIT-11, RESEARCH Task 12 |
| `IsEditable` gating | REQ EDIT-02, RESEARCH Task 2 |
| Concurrency error copy | REQ EDIT-07, CONTEXT (concurrency strategy) |
| Bootstrap default palette (no Pertamina brand) | Phase 320 precedent D-02 + project convention (admin tool internal) |
| A11y patterns (ARIA, focus trap, color-not-sole) | spec `2026-05-20-assessment-admin-power-tools-design.md` §5.7 (canonical_refs CONTEXT) |
| Toast reuse `window.showAssessmentToast` | Codebase scout `wwwroot/js/assessment-hub.js:96` |
| Bootstrap Icons (existing `bi bi-*`) | Codebase scout `Views/Admin/AssessmentMonitoringDetail.cshtml:318,336` |
| Copy Bahasa Indonesia mandatory | CLAUDE.md |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS (Bahasa Indonesia verbose, eksplisit konsekuensi, empty/error/loading states explicit, destructive confirmations explicit)
- [ ] Dimension 2 Visuals: PASS (Bootstrap 5 components reused, icon set konsisten, no new CSS file, 3-channel redundancy untuk semantic states)
- [ ] Dimension 3 Color: PASS (60% white dominant, 30% bg-light/text-muted secondary, 10% btn-primary accent reserved untuk 4 elemen spesifik, destructive bg-danger, color-not-sole-indicator)
- [ ] Dimension 4 Typography: PASS (16/14/20/inherit, 2 weights efektif 400/600, Bootstrap default font stack inherited)
- [ ] Dimension 5 Spacing: PASS (kelipatan 4 via Bootstrap spacing utilities, 1 eksepsi touch target 40px untuk dropdown toggle)
- [ ] Dimension 6 Registry Safety: PASS (no new third-party registry, no shadcn — stack Razor)

**Approval:** pending

---

*Phase: 321-assessment-edit-jawaban-peserta*
*UI Spec created: 2026-05-21*
*Language: Bahasa Indonesia (CLAUDE.md mandatory)*
