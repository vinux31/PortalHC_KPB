# Phase 252: XSS Escape AJAX Approval Badge - Research

**Researched:** 2026-03-24
**Domain:** Client-side XSS prevention — JavaScript DOM manipulation, HTML encoding
**Confidence:** HIGH

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SEC-02 | Escape `approverName` di `GetApprovalBadgeWithTooltip` CoachingProton.cshtml — ganti `@Html.Raw` dengan HTML-encoded output | Server-side path sudah selesai (Phase 250). Phase ini menutup jalur AJAX yang masih melakukan string interpolasi langsung ke `innerHTML`. |
</phase_requirements>

---

## Summary

Phase 250 telah menyelesaikan server-side path: fungsi Razor `GetApprovalBadgeWithTooltip` kini menggunakan `System.Net.WebUtility.HtmlEncode` sebelum menyisipkan `approverName` ke dalam HTML attribute `title`. Namun jalur AJAX — yang mem-refresh badge langsung dari JavaScript setelah operasi approval — masih melakukan interpolasi string mentah ke `innerHTML` tanpa escaping apapun.

Ada tiga blok JavaScript berbeda di `CoachingProton.cshtml` yang membangun badge via `innerHTML` dari data JSON server:
1. **Tinja modal handler** (baris ~1226-1228): menggunakan `data.approverName` + `data.approvedAt` untuk badge SrSpv/SH.
2. **HC Review button handler** (baris ~1263-1264): menggunakan `data.reviewerName` + `data.reviewedAt` untuk badge HC.
3. **HC Review Panel handler** (baris ~1304-1305): jalur panel terpisah, sama seperti poin 2.

Jika server mengembalikan `approverName` berisi karakter HTML (misalnya `<script>alert(1)</script>` atau `"><img src=x onerror=alert(1)>`), nilai tersebut langsung dimasukkan ke atribut `title` dalam `innerHTML` — membuka XSS.

**Primary recommendation:** Tambahkan fungsi helper JavaScript `escHtml(str)` di scope `<script>` yang bersangkutan, lalu panggil helper tersebut untuk semua field yang bersumber dari server sebelum diinterpolasi ke `innerHTML`.

---

## Standard Stack

### Core

| Teknik | Versi | Tujuan | Standar |
|--------|-------|--------|---------|
| Fungsi `escHtml` JavaScript vanilla | N/A | Encode `&`, `<`, `>`, `"`, `'` sebelum masuk DOM | Standar industri — tidak butuh library |
| `textContent` (alternatif) | N/A | Isi teks-saja tanpa risiko markup | Lebih aman tapi tidak bisa dipakai untuk atribut HTML |

Karena ini adalah halaman Razor ASP.NET Core dengan Bootstrap dan vanilla JS (tanpa framework JS), tidak diperlukan dependensi baru.

**Tidak perlu instalasi paket apapun.**

---

## Architecture Patterns

### Pola yang Benar: Helper `escHtml` JavaScript

Pola paling umum dan portabel untuk sanitasi inline di vanilla JS:

```javascript
// Sumber: OWASP DOM XSS Prevention Cheat Sheet
function escHtml(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
```

Fungsi ini di-define sekali, lalu dipanggil di setiap lokasi interpolasi.

### Penggunaan pada Badge HTML

Sebelum (rentan):
```javascript
const tooltip = `${data.approverName} \u2014 ${data.approvedAt}`;
cell.innerHTML = `<span class="badge bg-success" data-bs-toggle="tooltip" title="${tooltip}">Approved</span>`;
```

Sesudah (aman):
```javascript
const tooltip = `${escHtml(data.approverName)} \u2014 ${escHtml(data.approvedAt)}`;
cell.innerHTML = `<span class="badge bg-success" data-bs-toggle="tooltip" title="${tooltip}">Approved</span>`;
```

### Anti-Patterns

- **Jangan encode di luar template literal saja:** Jika tooltip dimasukkan ke `setAttribute`, perlu pastikan escaping tetap dilakukan sebelum `setAttribute` pun — meski `setAttribute` lebih aman dari `innerHTML`, konsistensi lebih mudah dijaga dengan satu helper.
- **Jangan gunakan DOMParser atau innerHTML sebagai parser:** Beberapa contoh online menggunakan `innerHTML` ke dummy element untuk meng-encode — ini anti-pattern karena bisa memicu eksekusi script di beberapa browser lama.

---

## Don't Hand-Roll

| Problem | Jangan Buat | Gunakan Sebagai Gantinya | Alasan |
|---------|-------------|--------------------------|--------|
| HTML encoding di JS | Library encoding 3rd-party | Fungsi `escHtml` 5 baris vanilla | Dependensi tidak perlu untuk case ini |
| Server-side encoding | Custom string replace | `System.Net.WebUtility.HtmlEncode` (.NET) | Sudah ada di .NET — sudah dipakai Phase 250 |

---

## Lokasi Kode yang Perlu Dimodifikasi

File tunggal: `Views/CDP/CoachingProton.cshtml`

| Baris (approx) | Blok | Field Rentan | Keterangan |
|----------------|------|--------------|------------|
| ~1226 | Tinja modal handler | `data.approverName`, `data.approvedAt` | Badge SrSpv/SH setelah approval |
| ~1228 | Tinja modal handler | Atribut `title` dalam `innerHTML` | Sama — satu template literal |
| ~1263 | HC Review button handler | `data.reviewerName`, `data.reviewedAt` | Badge HC setelah review |
| ~1304 | HC Review Panel handler | `data.reviewerName`, `data.reviewedAt` | Jalur panel — sama seperti baris 1263 |

Fungsi `escHtml` cukup didefinisikan **satu kali** di dekat awal blok `<script>` utama (sekitar baris 1088, area "AJAX helpers"), lalu dipanggil di ke-4 lokasi di atas.

---

## Common Pitfalls

### Pitfall 1: Lupa Salah Satu dari Tiga Blok

**Yang salah:** Hanya memperbaiki blok Tinja modal tapi lupa HC Review dan HC Review Panel.
**Mengapa terjadi:** Tiga blok terpisah dengan handler yang mirip namun berbeda nama field (`approverName` vs `reviewerName`).
**Pencegahan:** Grep untuk semua `innerHTML` yang berisi template literal dengan data dari JSON sebelum menutup PR.
**Tanda peringatan:** Badge HC masih menerima raw string meski badge SrSpv/SH sudah aman.

### Pitfall 2: Encode Terlalu Banyak (Double-Encoding)

**Yang salah:** Encode di sisi server (JSON response) DAN di sisi client — hasilnya `&amp;amp;` ditampilkan sebagai teks literal di tooltip.
**Mengapa terjadi:** Mengira server harus mengembalikan encoded string.
**Pencegahan:** Server tetap mengembalikan string mentah (JSON auto-escape `"` dan `\` saja). Client-side `escHtml` melakukan HTML-encode untuk konteks HTML attribute.

### Pitfall 3: Lupa Field `approvedAt` / `reviewedAt`

**Yang salah:** Hanya encode `approverName`, lupa encode timestamp.
**Mengapa terjadi:** Timestamp tampak "aman" karena formatnya terprediksi. Namun server bisa mengembalikan nilai tidak terduga.
**Pencegahan:** Encode semua field yang diinterpolasi ke HTML, tanpa kecuali.

---

## Code Examples

### Helper `escHtml` yang Direkomendasikan

```javascript
// Source: OWASP DOM XSS Prevention Cheat Sheet
// https://cheatsheetseries.owasp.org/cheatsheets/DOM_based_XSS_Prevention_Cheat_Sheet.html
function escHtml(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
```

### Perbaikan Lengkap — Tinja Modal Handler

```javascript
// SEBELUM (rentan):
const tooltip = `${data.approverName} \u2014 ${data.approvedAt}`;
cell.innerHTML = `<span class="badge ${badgeClass} fw-bold border border-${data.newStatus === 'Approved' ? 'success' : 'danger'}" data-bs-toggle="tooltip" title="${tooltip}">${data.newStatus}</span>`;

// SESUDAH (aman):
const tooltip = `${escHtml(data.approverName)} \u2014 ${escHtml(data.approvedAt)}`;
cell.innerHTML = `<span class="badge ${badgeClass} fw-bold border border-${data.newStatus === 'Approved' ? 'success' : 'danger'}" data-bs-toggle="tooltip" title="${tooltip}">${data.newStatus}</span>`;
```

### Perbaikan Lengkap — HC Review Handler

```javascript
// SEBELUM (rentan):
const tooltip = `${data.reviewerName} \u2014 ${data.reviewedAt}`;
cell.innerHTML = `<span class="badge bg-success fw-bold border border-success" data-bs-toggle="tooltip" title="${tooltip}">Reviewed</span>`;

// SESUDAH (aman):
const tooltip = `${escHtml(data.reviewerName)} \u2014 ${escHtml(data.reviewedAt)}`;
cell.innerHTML = `<span class="badge bg-success fw-bold border border-success" data-bs-toggle="tooltip" title="${tooltip}">Reviewed</span>`;
```

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | UAT manual (browser) — tidak ada test runner otomatis untuk Razor views |
| Config file | none |
| Quick run command | Manual browser test |
| Full suite command | Manual browser test |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SEC-02 | Karakter HTML di `approverName` muncul sebagai teks literal di tooltip, bukan dieksekusi | Manual browser + code review | N/A — verifikasi visual di browser | N/A |

### Sampling Rate

- **Per task commit:** Code review — cari `data.approverName` dan `data.reviewerName` dalam template literal tanpa `escHtml()`
- **Per wave merge:** Manual UAT — jalankan approval dengan nama berisi `<b>test</b>` dan verifikasi tooltip menampilkan `&lt;b&gt;test&lt;/b&gt;`
- **Phase gate:** UAT manual passed sebelum `/gsd:verify-work`

### Wave 0 Gaps

Tidak ada gap infrastruktur — hanya modifikasi JavaScript dalam file Razor yang sudah ada.

---

## Environment Availability

Step 2.6: SKIPPED (perubahan murni JavaScript dalam file Razor — tidak ada dependensi eksternal)

---

## State of the Art

| Pendekatan Lama | Pendekatan Saat Ini | Kapan Berubah | Dampak |
|-----------------|---------------------|---------------|--------|
| Interpolasi langsung ke `innerHTML` | Escape dulu via `escHtml()` | Phase 252 | Menutup XSS di jalur AJAX |
| Server-side only encoding | Defense in depth: server + client encoding | Phase 250 + 252 | Semua jalur aman |

---

## Open Questions

Tidak ada — scope dan pendekatan sudah jelas. Semua lokasi rentan teridentifikasi.

---

## Sources

### Primary (HIGH confidence)

- OWASP DOM XSS Prevention Cheat Sheet — https://cheatsheetseries.owasp.org/cheatsheets/DOM_based_XSS_Prevention_Cheat_Sheet.html
- Kode aktual `Views/CDP/CoachingProton.cshtml` — inspeksi langsung baris 1226, 1263, 1304

### Secondary (MEDIUM confidence)

- OWASP XSS Filter Evasion Cheat Sheet — memverifikasi karakter yang perlu di-encode (`&`, `<`, `>`, `"`, `'`)

---

## Metadata

**Confidence breakdown:**
- Lokasi kode rentan: HIGH — teridentifikasi langsung dari kode
- Pendekatan perbaikan: HIGH — standar industri (OWASP)
- Risk of regression: LOW — perubahan minimal, terlokalisir di satu file

**Research date:** 2026-03-24
**Valid until:** Tidak ada expiry — teknik vanilla JS stabil
