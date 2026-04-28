# Phase 304: UI Label Polish (Login + WIB) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-28
**Phase:** 304-ui-label-polish-login-wib
**Areas discussed:** Eye-icon UX pattern, Format '(WIB)' di label Step 3, WIB di Step 4 PrePost summary, JS extraction strategy, AD mode vs Local mode UX, Browser autofill & touch target, WIB helper text/format konsistensi, Validation & form behavior unchanged

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Eye-icon UX pattern | Position icon dalam input-group, bentuk icon, accessibility approach | ✓ |
| Format '(WIB)' di label Step 3 | Inline vs separate small text untuk 6 input time WIZ-02 | ✓ |
| WIB di Step 4 PrePost summary | Scope WIB suffix di lines 1126/1130/1132/1136/1177 | ✓ |
| JS extraction strategy | Inline vs extract ke wwwroot/js | ✓ |

**User's choice:** Pilih semua 4 area, lalu setelah konfirmasi awal pilih 4 area tambahan (AD mode, Browser autofill, WIB helper text, Validation behavior).

---

## Area 1: Eye-icon UX pattern

### Q1.1: Position tombol toggle eye-icon

| Option | Description | Selected |
|--------|-------------|----------|
| Append kanan (Recommended) | Tombol di kanan input, pertahankan bi-lock-fill di kiri | ✓ |
| Replace bi-lock-fill di kiri | Ganti icon kiri lock→eye, hilangkan visual cue field password | |
| Tombol terpisah di bawah input | Checkbox/button di bawah field, lebih jelas tapi vertical space | |

**User's choice:** Append kanan
**Notes:** Visual cue "field password" lewat bi-lock-fill kiri dipertahankan.

### Q1.2: Icon style

| Option | Description | Selected |
|--------|-------------|----------|
| bi-eye ↔ bi-eye-slash (Recommended) | Pattern paling umum, Bootstrap Icons standar | ✓ |
| bi-eye-fill ↔ bi-eye-slash-fill | Versi solid, kontras dengan bi-lock-fill kiri | |
| Text 'Tampilkan'/'Sembunyikan' | Tanpa icon, pakai teks | |

**User's choice:** bi-eye ↔ bi-eye-slash

### Q1.3: A11y label

| Option | Description | Selected |
|--------|-------------|----------|
| aria-label dinamis (Recommended) | aria-label di-toggle saat klik + aria-pressed | ✓ |
| Static aria-label + visually-hidden text | aria-label tetap, span visually-hidden berubah | |
| title attribute saja | Tooltip native browser | |

**User's choice:** aria-label dinamis + aria-pressed

---

## Area 2: Format '(WIB)' di label Step 3

### Q2.1: Format display label

| Option | Description | Selected (initial) | Selected (final) |
|--------|-------------|---------------------|-------------------|
| Inline dalam label utama (Recommended) | "Waktu Jadwal (WIB) *" | ✓ (revisi) | |
| Small muted suffix di bawah label | <span class='form-text small text-muted'>(WIB)</span> | | ✓ (final) |
| Badge kecil di samping label | <span class='badge text-bg-light ms-1'>WIB</span> | | |

**User's choice (initial):** Inline dalam label utama
**User's choice (REVISED setelah Area 7):** Pakai pattern `<div class="form-text text-muted">Tanggal (WIB)</div>` mengikuti EditAssessment existing
**Notes:** Revisi terjadi setelah grep mengungkap EditAssessment.cshtml sudah pakai pattern form-text helper di 8 lokasi (lines 241, 247, 299, 305, 360, 366, 435, 441). Konsistensi visual Create vs Edit jadi prioritas.

### Q2.2: Scope WIB

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya time/datetime (6 input) (Recommended) | Field dengan komponen jam saja | |
| Semua field tanggal+waktu | Termasuk Tanggal Jadwal & Tanggal Tutup (8 input total) | ✓ |

**User's choice:** Semua field tanggal+waktu (extension dari ROADMAP)
**Notes:** Scope diperluas dari 6 → 8 input. Date-only field (Tanggal Jadwal line 358, Tanggal Tutup Ujian line 378) juga dapat helper "Tanggal (WIB)".

---

## Area 3: WIB di Step 4 PrePost summary

### Q3.1: WIB scope di summary

| Option | Description | Selected |
|--------|-------------|----------|
| Semua 5 datetime (Recommended) | Standard 1177 + 4 PrePost (1126/1130/1132/1136) | ✓ |
| Hanya Standard line 1177 | Cukup line 1177 saja | |
| Standard + hanya schedule | 1177 + 1126/1132 schedule, EWCD lines tidak | |

**User's choice:** Semua 5 datetime

### Q3.2: Format string output

| Option | Description | Selected |
|--------|-------------|----------|
| Replace T → space + ' WIB' (Recommended) | Minimal change dari kode existing | ✓ |
| Format Indonesian-friendly | Parse jadi '28 Apr 2026, 14:30 WIB' | |
| Append ' WIB' tanpa replace T | Inkonsisten dengan existing | |

**User's choice:** Replace T → space + ' WIB'

---

## Area 4: JS extraction strategy

### Q4.1: JS Login

| Option | Description | Selected |
|--------|-------------|----------|
| Inline <script> di Login.cshtml (Recommended) | ~10 baris inline, no new file | ✓ |
| Extract ke wwwroot/js/login.js | File terpisah dengan PathBase handling | |
| Extract ke shared js/password-toggle.js | Reusable jika nanti ada Reset/ChangePassword | |

**User's choice:** Inline <script>

### Q4.2: Wizard JS

| Option | Description | Selected |
|--------|-------------|----------|
| Edit inline existing JS (Recommended) | Tambah ' WIB' suffix di 5 lokasi existing | ✓ |
| Extract Step 4 summary ke wizardSummary.js | Pisahkan logic populateSummary | |

**User's choice:** Edit inline existing

---

## Area 5: AD mode vs Local mode UX

### Q5.1: Eye-icon di AD mode

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, di kedua mode (Recommended) | UX consistent, AD pakai field password (LDAP bind) | ✓ |
| Hanya local mode | Hide saat isAdMode=true | |

**User's choice:** Ya di kedua mode

### Q5.2: Banner AD adjustment

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak ada perubahan banner (Recommended) | Banner AD info tetap | ✓ |
| Tambah hint 'Password = password Pertamina' | Hint tambahan di banner | |

**User's choice:** Tidak ada perubahan banner

---

## Area 6: Browser autofill & touch target

### Q6.1: Initial state eye-icon

| Option | Description | Selected |
|--------|-------------|----------|
| bi-eye + aria-pressed='false' (Recommended) | Default tampilkan icon meski field kosong | ✓ |
| Hide tombol saat input kosong | Show saat user mulai ketik | |

**User's choice:** bi-eye + aria-pressed='false'

### Q6.2: Browser autofill

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak ada handling khusus (Recommended) | Toggle berfungsi normal saat autofilled | ✓ |
| Disable eye sampai user fokus field | Defensive against shoulder surfing | |

**User's choice:** Tidak ada handling khusus

### Q6.3: Touch target & responsive

| Option | Description | Selected |
|--------|-------------|----------|
| Inherit input-group-lg sizing (Recommended) | Padding 0.8rem 1rem sudah ≥44px | ✓ |
| Eksplisit min-width/height 44px | CSS defensive redundant | |

**User's choice:** Inherit input-group-lg sizing

---

## Area 7: WIB helper text/format konsistensi

### Q7.1: Helper text top-section

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak — cukup '(WIB)' di label (Recommended) | Label sudah cukup, hindari noise | ✓ |
| Ya — 1x di top Step 3 | <small> di awal section schedule | |
| Ya — di setiap field (form-text) | Verbose, repetitive | |

**User's choice:** Tidak

### Q7.2: WIB di view lain

| Option | Description | Selected |
|--------|-------------|----------|
| Hanya di Step 4 summary CreateAssessment (Recommended) | Scope phase 304 strictly | |
| Cek view lain & extend kalau ada | Grep singkat di Views/Admin/ | ✓ |

**User's choice:** Cek view lain & extend kalau ada

**Grep result:**
- `EditAssessment.cshtml` — sudah pakai `<div class="form-text text-muted">Tanggal (WIB)</div>` di 8 lokasi
- `Views/CMP/Assessment.cshtml` — sudah konsisten `WIB` suffix via `DateTime.ToString` (8+ lokasi)
- Tidak ada inkonsistensi user-facing

### Q7.3: Pattern conflict resolution

| Option | Description | Selected |
|--------|-------------|----------|
| Ikuti pattern existing EditAssessment (Recommended) | Pakai form-text helper untuk Create | ✓ |
| Tetap inline di Create + update Edit | Inline both, scope expand | |
| Tetap inline di Create, biarkan Edit | Defer Edit alignment | |

**User's choice:** Ikuti pattern existing EditAssessment (REVISI Area 2)

### Q7.4: Edit scope

| Option | Description | Selected |
|--------|-------------|----------|
| Defer EditAssessment update (Recommended) | Phase 304 fokus Create + Login | ✓ |
| Include EditAssessment di phase 304 | Apply ke Edit juga | |

**User's choice:** Defer EditAssessment update

---

## Area 8: Validation & form behavior unchanged

### Q8.1: Behavior guards (multi-select)

| Option | Description | Selected |
|--------|-------------|----------|
| invalid-feedback messages tidak diubah (Recommended) | Pesan error tetap original | ✓ |
| Eye-icon tidak interfere jQuery validate (Recommended) | type='button', tidak trigger validation | ✓ |
| DOM ID & name tidak berubah (Recommended) | id='inputPassword', name='ScheduleTime', dst | ✓ |
| asp-for binding tetap (Recommended) | Tag helper Razor tidak diubah | ✓ |

**User's choice:** Semua 4 (multi-select)

---

## Claude's Discretion

- Detail visual styling tombol toggle (ukuran spesifik, hover state, focus ring)
- Nama variabel JavaScript (`togglePwd`, `pwdInput`, dll)
- Order tombol vs input border-radius rounding di end of input-group

## Deferred Ideas

1. EditAssessment alignment ke pattern lain (saat ini Edit & Create akan sama-sama pakai form-text)
2. Indonesian-friendly date format di Step 4 summary
3. Helper text top-of-section "Semua waktu dalam zona WIB"
4. Password auto-revert masked setelah X detik (T1 differentiator)
5. Worker/Monitoring view WIB consistency audit
6. Extract eye-icon JS ke shared password-toggle.js (jika nanti ada Reset/ChangePassword)
