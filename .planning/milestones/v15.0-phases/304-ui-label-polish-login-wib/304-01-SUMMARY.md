---
phase: 304-ui-label-polish-login-wib
plan: 01
status: complete
requirements:
  - AUTH-01
key-files:
  modified:
    - Views/Account/Login.cshtml
  created: []
completed: 2026-04-28
---

# Plan 304-01 Summary: AUTH-01 Eye-icon Toggle Login

## Goal Achieved

Tombol eye-icon toggle password visibility ditambahkan di halaman `/Account/Login`. User dapat menampilkan/menyembunyikan kata sandi yang diketik via klik tombol atau Space/Enter (keyboard accessible). Tombol `type="button"` — tidak men-submit form.

## What Was Built

1. **Tombol eye-icon** di sebelah kanan input password (append dalam input-group, lock-fill kiri tetap):
   ```html
   <button type="button" class="btn btn-outline-secondary" id="togglePwd"
           aria-label="Tampilkan kata sandi" aria-pressed="false">
       <i class="bi bi-eye" id="togglePwdIcon" aria-hidden="true"></i>
   </button>
   ```

2. **Inline `<script>` block** sebelum `</body>` (vanilla JS, IIFE, no jQuery):
   - Click handler swap `input.type` (password ↔ text)
   - Icon class swap (`bi-eye` ↔ `bi-eye-slash`)
   - aria-label & aria-pressed update dinamis untuk screen reader
   - Guard clause `if (!btn || !input || !icon) return;` defensive

## Verification Results

### Static Checks (all PASS)

| Check | Expected | Actual |
|-------|----------|--------|
| `id="togglePwd"` count | 1 | 1 |
| `id="togglePwdIcon"` count | 1 | 1 |
| `type="button"` count | ≥2 | 2 |
| `aria-label="Tampilkan kata sandi"` count | 1 | 1 |
| `aria-pressed="false"` count | 1 | 1 |
| `class="bi bi-eye"` count | ≥1 | 1 |
| `class="bi bi-lock-fill"` count | 1 (existing) | 1 |
| `inputPassword` total refs (`for=` + `id=`) | 2 | 2 |
| `name="password"` count | 1 | 1 |
| `addEventListener` count | 1 | 1 |
| `<script>` / `</script>` count | 1 / 1 | 1 / 1 |
| `jQuery` / `$(` count | 0 (vanilla only) | 0 |

### Build Check
- `dotnet build -c Debug --nologo --verbosity minimal` → exit 0
- 92 warnings (pre-existing baseline, none reference Login.cshtml)
- 0 errors

### Browser Verification (Playwright @ localhost:5277)

| # | Check | Status |
|---|-------|--------|
| 1-2 | App running, page loaded `/Account/Login` | ✅ |
| 3 | Visual: lock kiri + eye kanan, initial `bi-eye` | ✅ |
| 4 | Toggle round-trip: masked → plaintext → masked | ✅ |
| 5 | Form NOT submitted oleh klik eye-icon (URL stays `/Account/Login`) | ✅ |
| 6 | Keyboard a11y: Tab→button, Space→activate, Tab→rememberMe | ✅ |
| 7 | Submit valid kredensial → login sukses → navigate `/Home/Index` | ✅ |
| 8 | AD mode: banner "Login menggunakan akun Pertamina" tetap | ✅ |
| 9 | Screen reader (NVDA/JAWS) | ⏭ skip (aria values verified via DOM) |

**Aria states verified via DOM evaluate:**
- Initial: `aria-label="Tampilkan kata sandi"`, `aria-pressed="false"`, icon `bi bi-eye`
- After click: `aria-label="Sembunyikan kata sandi"`, `aria-pressed="true"`, icon `bi bi-eye-slash`

### Screenshots
- `phase304-login-initial.png` — initial state (masked password, eye icon)
- `phase304-login-toggled-visible.png` — toggled state (plaintext "test123", eye-slash icon)

## Decisions Honored (CONTEXT.md mapping)

| Decision | Status |
|----------|--------|
| D-01 (append kanan input, lock-fill kiri tetap) | ✅ |
| D-02 (bi-eye / bi-eye-slash icon swap) | ✅ |
| D-03 (`type="button"` — tidak men-submit form) | ✅ |
| D-04 (aria-label dinamis + aria-pressed) | ✅ |
| D-05 (tampil di kedua mode AD + local) | ✅ |
| D-06 (initial state masked, icon bi-eye, aria-pressed=false) | ✅ |
| D-07 (no autofill handling khusus) | ✅ |
| D-08 (touch target inherit input-group-lg ≥44px) | ✅ |
| D-09 (inline `<script>`, vanilla JS, no jQuery) | ✅ |
| D-16 (invalid-feedback messages tidak diubah) | ✅ |
| D-17 (tidak interfere jQuery validate) | ✅ |
| D-18 (DOM ID `inputPassword` & `name="password"` tidak berubah) | ✅ |

## Tasks Completed

- **Task 1** (commit `c8dd1cf8`): Sisipkan tombol eye-icon di input-group password
- **Task 2** (commit `94e20550`): Tambah inline `<script>` block dengan vanilla JS toggle handler
- **Task 3**: Build verification (`dotnet build` exit 0)
- **Task 4**: Human verification checkpoint — 9 browser checks, user "approved"

## Notes

- Tidak ada file baru
- Tidak ada dependency baru (Bootstrap Icons CDN existing line 14 sudah cukup)
- Tidak ada DB schema change
- Tidak ada server-side change (`Account/Login` POST action tidak terpengaruh)
- Tidak ada interfere dengan jQuery validate atau form submit flow
- Threat model: 6 STRIDE entries (T-304-01 sampai T-304-06), no high-risk, ASVS L1 PASS

## Self-Check: PASSED
