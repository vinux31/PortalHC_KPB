# Phase 304: UI Label Polish (Login + WIB) - Pattern Map

**Mapped:** 2026-04-28
**Files analyzed:** 2 (modified, no new files)
**Analogs found:** 2 / 2 (100% coverage)

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Views/Account/Login.cshtml` | view (Razor) + inline script | request-response (form POST) + DOM toggle | self (existing structure) + `Views/Admin/CreateAssessment.cshtml` (inline `<script>` pattern) | exact (in-place edit) |
| `Views/Admin/CreateAssessment.cshtml` | view (Razor) + inline script | request-response (wizard POST) + DOM populate | `Views/Admin/EditAssessment.cshtml` (helper text pattern) + self (existing JS pattern) | exact (form-text helper); self (JS suffix) |

**Catatan:** Tidak ada file baru yang dibuat. Kedua file existing dimodifikasi in-place. Untuk pattern eye-icon (AUTH-01) tidak ada analog di repo (no existing password toggle), jadi pattern di-derive dari Bootstrap 5.3 + Bootstrap Icons convention yang sudah ter-load di view.

---

## Pattern Assignments

### `Views/Account/Login.cshtml` (view + inline script, request-response + DOM toggle)

**Analog 1 (struktur input-group existing):** `Views/Account/Login.cshtml` itself (lines 177-183)

**Existing markup pattern — HARUS dipertahankan** (lines 177-183):
```html
<div class="mb-4">
    <label class="custom-label" for="inputPassword">Kata Sandi</label>
    <div class="input-group input-group-lg border rounded">
        <span class="input-group-text"><i class="bi bi-lock-fill" aria-hidden="true"></i></span>
        <input type="password" name="password" id="inputPassword" class="form-control" placeholder="••••••••" required>
    </div>
</div>
```

**Pattern yang akan ditambahkan — eye-icon toggle button (append kanan input):**

Append button **setelah** `<input ... id="inputPassword">` di dalam `<div class="input-group input-group-lg border rounded">` yang sama. Pattern referensi (Bootstrap 5.3 input-group + button):
```html
<button type="button" class="btn btn-outline-secondary" id="togglePwd"
        aria-label="Tampilkan kata sandi" aria-pressed="false">
    <i class="bi bi-eye" id="togglePwdIcon" aria-hidden="true"></i>
</button>
```

**Wajib (per D-03, D-04, D-06):**
- `type="button"` — tidak submit form
- `aria-label` initial = `"Tampilkan kata sandi"`, `aria-pressed="false"`
- Icon initial = `bi-eye` (state: password tersembunyi)

**Bootstrap Icons CDN — sudah ter-load** (line 14, jangan tambah lagi):
```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
```

**Inline `<script>` placement** (sebelum `</body>` line 209):

Login.cshtml saat ini **tidak punya `<script>` block**. Pattern inline script harus baru. Reference pattern (vanilla JS, no jQuery — per `code_context` D-09):
```html
<script>
    (function () {
        var btn = document.getElementById('togglePwd');
        var input = document.getElementById('inputPassword');
        var icon = document.getElementById('togglePwdIcon');
        if (!btn || !input || !icon) return;
        btn.addEventListener('click', function () {
            var isHidden = input.type === 'password';
            input.type = isHidden ? 'text' : 'password';
            icon.classList.toggle('bi-eye', !isHidden);
            icon.classList.toggle('bi-eye-slash', isHidden);
            btn.setAttribute('aria-pressed', isHidden ? 'true' : 'false');
            btn.setAttribute('aria-label', isHidden ? 'Sembunyikan kata sandi' : 'Tampilkan kata sandi');
        });
    })();
</script>
```

**Form submit flow tidak berubah** (line 157 — `<form method="post" asp-controller="Account" asp-action="Login">`). `type="button"` memastikan eye-icon tidak interfere submit (D-03, D-17).

**Email field (line 169-175) tidak diubah.** Banner AD info (line 161-167) tidak diubah (D-05).

---

### `Views/Admin/CreateAssessment.cshtml` (view + inline script, request-response + DOM populate)

**Analog 1 (helper text WIB):** `Views/Admin/EditAssessment.cshtml` lines 241, 247, 299, 305, 360, 366, 435, 441

**Pattern persis yang harus di-replicate** (8 instance di EditAssessment):

Excerpt dari `EditAssessment.cshtml` line 360-366 (Standard mode date+time pair):
```html
<input type="date" id="ScheduleDate"
       value="@Model.Schedule.ToString("yyyy-MM-dd")"
       class="form-control" required />
<div class="form-text text-muted">Tanggal (WIB)</div>
```
```html
<input type="time" id="ScheduleTime"
       value="@Model.Schedule.ToString("HH:mm")"
       class="form-control" required />
<div class="form-text text-muted">Waktu (WIB)</div>
```

Excerpt dari `EditAssessment.cshtml` line 241, 247 (Pre-Test pair):
```html
<input type="date" id="PreScheduleDate" ... class="form-control" />
<div class="form-text text-muted">Tanggal (WIB)</div>
```
```html
<input type="time" id="PreScheduleTime" ... class="form-control" />
<div class="form-text text-muted">Waktu (WIB)</div>
```

**Apply ke CreateAssessment 8 input** (per D-11):

| Line di Create | Field Existing | Helper Text yang Ditambahkan |
|----------------|----------------|------------------------------|
| 358 (`schedDateInput`) | `<input type="date" ...>` | `<div class="form-text text-muted">Tanggal (WIB)</div>` |
| 363 (`schedTimeInput`) | `<input type="time" ...>` | `<div class="form-text text-muted">Waktu (WIB)</div>` |
| 378 (`ewcdDateInput`) | `<input type="date" ...>` | `<div class="form-text text-muted">Tanggal (WIB)</div>` |
| 385 (`ewcdTimeInput`) | `<input type="time" ...>` | `<div class="form-text text-muted">Waktu (WIB)</div>` |
| 405 (`preSchedule`) | `<input type="datetime-local" ...>` | `<div class="form-text text-muted">Tanggal & Waktu (WIB)</div>` |
| 413 (`preExamWindowCloseDate`) | `<input type="datetime-local" ...>` | `<div class="form-text text-muted">Tanggal & Waktu (WIB)</div>` |
| 426 (`postSchedule`) | `<input type="datetime-local" ...>` | `<div class="form-text text-muted">Tanggal & Waktu (WIB)</div>` |
| 434 (`postExamWindowCloseDate`) | `<input type="datetime-local" ...>` | `<div class="form-text text-muted">Tanggal & Waktu (WIB)</div>` |

**Posisi insert helper text:** SETELAH `<input>` dan SEBELUM `<div class="invalid-feedback">` (dimana ada). Untuk lines 358, 363, 378, 385 ada `<div class="invalid-feedback" id="...Error">` — helper text masuk **sebelum** invalid-feedback agar invalid-feedback tetap behave normal (Bootstrap validation menampilkan invalid-feedback hanya saat input `:invalid`, helper text selalu visible).

Existing context line 358-360 yang akan dimodifikasi:
```html
<input type="date" id="schedDateInput" name="ScheduleDate" class="form-control" />
<div class="invalid-feedback" id="schedDateError">Tanggal jadwal wajib diisi.</div>
```

Setelah modifikasi:
```html
<input type="date" id="schedDateInput" name="ScheduleDate" class="form-control" />
<div class="form-text text-muted">Tanggal (WIB)</div>
<div class="invalid-feedback" id="schedDateError">Tanggal jadwal wajib diisi.</div>
```

**Existing helper text TIDAK diubah** — line 371 `<div class="form-text text-muted">1–480 menit (maks 8 jam)</div>` (Durasi field) tetap.

---

**Analog 2 (JS suffix WIB):** `Views/Admin/CreateAssessment.cshtml` itself line 1164 (existing pattern)

**Pattern existing yang sudah ada `' WIB'`** (line 1164, BUKAN target modify, sebagai reference konsistensi):
```javascript
if (summSchedule) summSchedule.textContent = schedVal ? (schedVal + ' ' + timeVal + ' WIB') : '-';
```

**Pattern yang akan ditambahkan ke 5 lokasi** (per D-13, D-14):

Excerpt sebelum modifikasi (line 1126):
```javascript
el = document.getElementById('summary-pre-schedule');
if (el) el.textContent = preSched && preSched.value ? preSched.value.replace('T', ' ') : '-';
```

Excerpt setelah modifikasi:
```javascript
el = document.getElementById('summary-pre-schedule');
if (el) el.textContent = preSched && preSched.value ? preSched.value.replace('T', ' ') + ' WIB' : '-';
```

**Apply ke 5 lokasi (PrePost mode + Standard mode ewcd):**

| Line | Variable | Existing | Setelah |
|------|----------|----------|---------|
| 1126 | `summary-pre-schedule` | `preSched.value.replace('T', ' ')` | `preSched.value.replace('T', ' ') + ' WIB'` |
| 1130 | `summary-pre-ewcd` | `preEwcd.value.replace('T', ' ')` | `preEwcd.value.replace('T', ' ') + ' WIB'` |
| 1132 | `summary-post-schedule` | `postSched.value.replace('T', ' ')` | `postSched.value.replace('T', ' ') + ' WIB'` |
| 1136 | `summary-post-ewcd` | `postEwcd.value.replace('T', ' ')` | `postEwcd.value.replace('T', ' ') + ' WIB'` |
| 1177 | `summary-ewcd` | `ewcdDateEl.value + ' ' + (ewcdTimeEl ? ewcdTimeEl.value : '23:59')` | `ewcdDateEl.value + ' ' + (ewcdTimeEl ? ewcdTimeEl.value : '23:59') + ' WIB'` |

**Konsistensi:** line 1164 (Standard `summary-schedule`) sudah punya `+ ' WIB'` — ini reference precedent yang dipakai. Semua 5 perubahan harus match precedent format ini (suffix `' WIB'` dengan leading space).

**Penting (per D-18, D-19):** DOM ID semua summary span (`summary-pre-schedule`, `summary-pre-ewcd`, `summary-post-schedule`, `summary-post-ewcd`, `summary-ewcd`, dst) tidak diubah. Hanya text concatenation yang diubah.

---

## Shared Patterns

### Helper Text "(WIB)" (Bootstrap 5.3)

**Source:** `Views/Admin/EditAssessment.cshtml` lines 241, 247, 299, 305, 360, 366, 435, 441

**Apply to:** 8 input date/time/datetime-local di `Views/Admin/CreateAssessment.cshtml`

**Class & format persis:**
```html
<div class="form-text text-muted">{Tanggal|Waktu|Tanggal & Waktu} (WIB)</div>
```

**Pemilihan teks:**
- Input `type="date"` → `Tanggal (WIB)`
- Input `type="time"` → `Waktu (WIB)`
- Input `type="datetime-local"` → `Tanggal & Waktu (WIB)`

### WIB Suffix di JS String Concat

**Source (precedent existing):** `Views/Admin/CreateAssessment.cshtml` line 1164

**Apply to:** 5 lokasi `populateSummary` function di file yang sama (lines 1126, 1130, 1132, 1136, 1177)

**Format:** `value.replace('T', ' ') + ' WIB'` (untuk datetime-local) atau `dateValue + ' ' + timeValue + ' WIB'` (untuk date+time pair).

### Inline `<script>` Block di View

**Source (precedent existing):** `Views/Admin/CreateAssessment.cshtml` lines 1100+ (600+ baris JS inline) — pattern view-scoped script.

**Apply to:** `Views/Account/Login.cshtml` (tambah `<script>` block baru sebelum `</body>` line 209).

**Rationale (D-09):** Login view self-contained (`Layout = null`, inline CSS). Tidak extract ke `wwwroot/js/` — premature abstraction (YAGNI, hanya 1 form login).

### Bootstrap Icons (sudah ter-load)

**Source:** `Views/Account/Login.cshtml` line 14 (CDN), `Views/Shared/_Layout.cshtml` (admin views)

**Apply to:** Eye-icon toggle (`bi-eye`, `bi-eye-slash`). Icon `bi-lock-fill` (line 180) tetap di kiri sebagai visual cue (D-01).

**Tidak perlu tambah dependency** — semua icon sudah tersedia.

---

## No Analog Found

Tidak ada file dalam kategori ini. Semua perubahan punya analog jelas:
- Helper text WIB → `EditAssessment.cshtml` (8 instance precedent)
- JS suffix WIB → `CreateAssessment.cshtml` line 1164 (1 instance precedent)
- Eye-icon toggle → tidak ada precedent di repo, tapi pattern Bootstrap 5.3 + vanilla JS standar (Login.cshtml self-contained, low-risk)

---

## Cross-Reference Quick Lookup

| Need | Reference File | Line |
|------|---------------|------|
| `<div class="form-text text-muted">Tanggal (WIB)</div>` pattern | `Views/Admin/EditAssessment.cshtml` | 241, 299, 360, 435 |
| `<div class="form-text text-muted">Waktu (WIB)</div>` pattern | `Views/Admin/EditAssessment.cshtml` | 247, 305, 366, 441 |
| WIB suffix JS precedent | `Views/Admin/CreateAssessment.cshtml` | 1164 |
| Inline `<script>` precedent | `Views/Admin/CreateAssessment.cshtml` | 1100+ |
| Bootstrap Icons CDN (no add) | `Views/Account/Login.cshtml` | 14 |
| `input-group input-group-lg` structure | `Views/Account/Login.cshtml` | 179-183 |
| Form POST flow (jangan disrupt) | `Views/Account/Login.cshtml` | 157 |
| Existing helper text yang tidak boleh diubah | `Views/Admin/CreateAssessment.cshtml` | 371 |

---

## Metadata

**Analog search scope:**
- `Views/Account/` (Login.cshtml self-reference)
- `Views/Admin/` (EditAssessment.cshtml, CreateAssessment.cshtml self-reference)
- `Views/CMP/` (Assessment.cshtml — konsistensi context, bukan target)

**Files scanned:** 4 view files
**Pattern extraction date:** 2026-04-28
**Phase:** 304-ui-label-polish-login-wib

---

*Generated by gsd-pattern-mapper. Konsumsi oleh gsd-planner untuk PLAN.md.*
