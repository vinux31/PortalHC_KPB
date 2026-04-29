---
phase: 307-selected-participants-inline-view
reviewed: 2026-04-29T01:51:54Z
depth: standard
files_reviewed: 3
files_reviewed_list:
  - Views/Admin/CreateAssessment.cshtml
  - tests/e2e/assessment.spec.ts
  - tests/e2e/helpers/wizardSelectors.ts
findings:
  critical: 0
  warning: 3
  info: 6
  total: 9
status: issues_found
---

# Phase 307: Code Review Report

**Reviewed:** 2026-04-29T01:51:54Z
**Depth:** standard
**Files Reviewed:** 3
**Status:** issues_found

## Summary

Implementasi Phase 307 (panel "Peserta Terpilih" inline Step 2) telah dilakukan dengan disiplin yang baik:

- Threat model T-307-01 (XSS via FullName) ditangani dengan `textContent` di helper `renderSelectedParticipants` (line 1479, 1490, 1495, 1503, 1511, 1516).
- Threat model T-307-02 (DOM clobbering) ditangani dengan ID prefix unik `selected-participants-*`.
- Threat model T-307-03 (listener leak) ditangani dengan `btn.onclick` property assignment (line 1509) + `replaceChildren` atomic (line 1528) + `clearTimeout` pada debounce (line 1544).
- Hoist `updateSelectedCount` ke top-level berhasil — function declaration di line 1553 di-hoist secara native ke seluruh `<script>` block (line 815-1829), aman dipanggil dari main IIFE line 1395, 1404, 1408, 1409 dan Proton IIFE line 1591.
- Bahasa Indonesia user-facing strings konsisten (CLAUDE.md C-01 dipatuhi).
- Test scaffold Phase 307 (4 test) dan helper selectors lengkap; opportunistic fix line 46 (`'2 selected'` → `'2 terpilih'`) sudah diterapkan.

Tidak ditemukan bug critical atau security regression yang **diintroduksi** oleh Phase 307. Namun ada 3 warning terkait konsistensi reset/hydrate path + 1 pre-existing XSS surface di Proton AJAX hydrate yang berinteraksi dengan helper baru, serta beberapa info-level housekeeping items.

## Warnings

### WR-01: Reset handler "Buat lagi" tidak reset Proton track + tidak hilangkan Proton container

**File:** `Views/Admin/CreateAssessment.cshtml:1795-1824`
**Issue:**
Reset handler di success modal (`modal-create-another-btn` click, line 1795-1824) memanggil `form.reset()` (line 1797) yang tidak akan reset elemen `<select id="protonTrackSelect">` jika user sebelumnya memilih kategori "Assessment Proton" (form.reset hanya bekerja pada elemen `name=` yang ditangkap form; track select punya `name="ProtonTrackId"` jadi sebenarnya OK, tetapi):

1. `applyProtonMode(false)` tidak dipanggil walau `catSelect.dispatchEvent(new Event('change'))` di line 1822 men-trigger ulang category change handler. Akan tetapi line 1822 mengirim event `change` dengan value `''` (kosong, line 1821), sehingga `applyProtonMode(false)` bekerja via path else (line 1254-1281).
2. Order panggil function: line 1802 `updateSelectedCount()` dijalankan **sebelum** `catSelect.dispatchEvent(new Event('change'))` di line 1822. Jika user sebelumnya di mode Proton, saat `updateSelectedCount` dipanggil di line 1802, `catEl.value` mungkin masih `'Assessment Proton'` (belum di-reset). Akibatnya `isProton=true` masih true, query checkbox akan menargetkan `#protonUserCheckboxContainer` yang kemungkinan masih berisi data lama (belum dibersihkan oleh `applyProtonMode` else-branch). Setelah dispatch event di line 1822, `applyProtonMode(false)` membersihkan checkbox proton, tapi panel sudah keburu di-render dengan stale data (debounce 100ms — kemungkinan ter-overwrite, tapi tergantung urutan event loop).

**Fix:**
Pindahkan `updateSelectedCount()` setelah dispatch event category, atau panggil `updateSelectedCount()` sekali lagi di bagian akhir handler:

```javascript
// Line 1818-1823 (current order):
var catSelect = document.getElementById('Category');
if (catSelect) {
    catSelect.value = '';
    catSelect.dispatchEvent(new Event('change'));
}
// Add explicit final refresh setelah category change handler menyelesaikan applyProtonMode(false)
updateSelectedCount();
```

Atau, lebih bersih, swap urutan: reset category dulu (line 1818-1823), lalu `updateSelectedCount()` sekali (line 1802 di-MOVE ke akhir).

---

### WR-02: AJAX hydrate Proton menggunakan `innerHTML` dengan data user dari server

**File:** `Views/Admin/CreateAssessment.cshtml:1642-1643`
**Issue:**
Walau pre-existing (sebelum Phase 307), kode ini sekarang berinteraksi dengan helper `renderSelectedParticipants` baru yang menarik nama dari label DOM via query selector:

```javascript
div.innerHTML = '<input class="form-check-input user-checkbox" type="checkbox" name="UserIds" value="' + u.id + '" id="pu_' + u.id + '" />'
    + '<label class="form-check-label" for="pu_' + u.id + '"><strong>' + (u.fullName || '-') + '</strong> <span class="text-muted">(' + (u.email || '') + ')</span></label>';
```

`u.fullName` dan `u.email` di-interpolasi langsung ke string HTML. Jika ada user dengan `FullName` mengandung `"<img src=x onerror=...>"`, akan dieksekusi (server-trusted user data di Pertamina lazim, tapi audit T-307-01 di plan menyebut FullName sebagai threat surface). Helper `renderSelectedParticipants` menggunakan `textContent` dari label setelah hydrate — artinya jika payload XSS sudah ter-eksekusi saat innerHTML assignment, Phase 307 tidak menambah surface, tetapi tetap men-trust hasil DOM yang sudah ter-injeksi.

**Fix:**
Gunakan `createElement` + `textContent` untuk hydrate (mirror pattern `renderSelectedParticipants` itu sendiri). Out of strict scope Phase 307, tapi worth filing sebagai follow-up:

```javascript
users.forEach(function(u) {
    var div = document.createElement('div');
    div.className = 'form-check user-check-item mb-1';
    div.dataset.name = u.fullName || '';
    div.dataset.section = u.section || '';

    var input = document.createElement('input');
    input.className = 'form-check-input user-checkbox';
    input.type = 'checkbox';
    input.name = 'UserIds';
    input.value = u.id;
    input.id = 'pu_' + u.id;

    var label = document.createElement('label');
    label.className = 'form-check-label';
    label.htmlFor = 'pu_' + u.id;
    var strong = document.createElement('strong');
    strong.textContent = u.fullName || '-';
    label.appendChild(strong);
    label.appendChild(document.createTextNode(' '));
    var emailSpan = document.createElement('span');
    emailSpan.className = 'text-muted';
    emailSpan.textContent = '(' + (u.email || '') + ')';
    label.appendChild(emailSpan);

    div.appendChild(input);
    div.appendChild(label);
    protonCbContainer.appendChild(div);
});
```

---

### WR-03: `applyProtonMode(false)` tidak clear `#selected-participants-panel` body langsung

**File:** `Views/Admin/CreateAssessment.cshtml:1268-1281`
**Issue:**
Saat user switch dari Proton ke kategori non-Proton (atau sebaliknya), function `applyProtonMode` melakukan `cb.checked = false` programmatic untuk uncheck semua checkbox di kedua container (line 1270, 1274-1275, 1277-1278). Programmatic toggle TIDAK fire `change` event (Pitfall 6), sehingga listener `userContainer.addEventListener('change', updateSelectedCount)` (line 1408) dan `protonCbContainer.addEventListener('change', updateSelectedCount)` (line 1591) TIDAK terpicu.

Akibatnya:
- Filter bar badge `#selectedCountBadge` tetap menampilkan count terakhir mode lama
- Panel header badge `#selected-participants-count` tetap stale
- Panel body `#selected-participants-panel` tetap menampilkan nama-nama dari mode lama (atau kosong, tapi tidak konsisten)

Plan menyebut piggyback pattern (Shared Pattern F) yang harus diterapkan ke "semua call site yang programmatic toggle: selectAll, deselectAll, reset 'Buat lagi', future bulk operations" — tapi `applyProtonMode` programmatic toggle juga harus diikuti `updateSelectedCount()` call.

**Fix:**
Tambahkan `updateSelectedCount()` di akhir `applyProtonMode` setelah toggle:

```javascript
// Line 1281 — di akhir applyProtonMode body, setelah branch isProton/else:
if (typeof updateSelectedCount === 'function') {
    updateSelectedCount();
}
```

Catatan: function `updateSelectedCount` adalah top-level (line 1553), accessible via bareword dari dalam IIFE `applyProtonMode`. Guard `typeof === 'function'` defensive untuk kasus script loading order (mungkin tidak perlu — script tag tunggal).

## Info

### IN-01: Module-scope timer name menggunakan double-underscore prefix non-standar

**File:** `Views/Admin/CreateAssessment.cshtml:1542`
**Issue:**
Variable `__selectedParticipantsRenderTimer` menggunakan double-underscore prefix yang biasanya direservasi Python/internal Node convention. Untuk JavaScript di browser context, prefix `_` (single) atau no-prefix lebih lazim. Tidak ada fungsi/scoping issue, tapi tidak konsisten dengan style file (var lain di-file pakai camelCase tanpa prefix).

**Fix:**
Ganti ke `_renderTimer` atau `selectedParticipantsRenderTimer` (cocok dengan style top-level naming di-file).

---

### IN-02: Test 7.3 mendokumentasikan Step 4 parity sebagai "deferred to manual UAT"

**File:** `tests/e2e/assessment.spec.ts:148-150`
**Issue:**
Test 7.3 secara eksplisit defer full Step 2 vs Step 4 visual parity assertion ke manual UAT (line 148-150). Walaupun acceptable untuk Wave 0 scaffold, success criteria #5 (DRY parity) tidak diverifikasi otomatis di CI — regresi DRY dapat lolos test E2E dan hanya tertangkap di manual UAT.

**Fix:**
Plan follow-up: tambahkan helper Playwright untuk advance wizard ke Step 4 dengan minimal valid form fields, lalu compare `textContent` antara `selectors.panelBody` dan `selectors.summaryListContainer`. Dapat dilakukan di Wave 2.

---

### IN-03: Test 7.4 menggunakan `#deselectAllBtn` bukan reset "Buat lagi" sesuai plan A1

**File:** `tests/e2e/assessment.spec.ts:163`
**Issue:**
Test 7.4 menggunakan tombol "Batalkan Semua" (`#deselectAllBtn`, line 163) untuk verifikasi reset path, bukan modal "Buat lagi" yang merupakan fokus Pitfall 2 / item 7 di PATTERNS.md. Pattern reset modal (line 1795+) sebenarnya yang baru di-edit di Wave 1, sementara `#deselectAllBtn` belum di-touch (sudah memanggil `updateSelectedCount()` sejak sebelum Phase 307).

Dengan kata lain: test 7.4 tidak mengetes path baru yang ditambahkan Phase 307; ia mengetes path existing yang kebetulan masih bekerja. Comment di test (line 162 `"Click 'Batalkan Semua' untuk uncheck semua"`) menyebut deselect, tapi judul test (line 153) menyebut "Reset clears panel ke empty state" yang ambiguous.

**Fix:**
Tambahkan test 7.5 yang menguji reset path "Buat lagi" setelah submit sukses (atau setidaknya simulate reset handler invocation). Untuk minimal effort, klarifikasi judul test 7.4 jadi "deselect-all clears panel" dan tambahkan TODO comment untuk modal reset path.

---

### IN-04: `Array.from` polyfill assumption + `String.prototype.includes` untuk filter

**File:** `Views/Admin/CreateAssessment.cshtml:1561, 1371`
**Issue:**
- Line 1561 `Array.from(document.querySelectorAll(sel))` — ES6, tidak available di IE11 native. Helper line 1525-1532 punya defensive fallback untuk `replaceChildren`, tapi `Array.from` tanpa polyfill akan error di IE11 (legacy IE handling tidak konsisten).
- Line 1371 `name.includes(query)` — ES6, sama issue.

Plan claim ES5 style ("Project: ASP.NET Razor + vanilla JS ES5 style"), tetapi sudah ada drift. Bukan bug aktif (Chrome/Edge/Firefox modern semua support), tapi inconsistent dengan stated convention.

**Fix:**
Salah satu: (a) drop pretensi "ES5" di plan/CLAUDE.md karena codebase nyatanya sudah ES6+, (b) tambahkan polyfill loader, atau (c) ganti ke `[].slice.call(querySelectorAll(...))` dan `name.indexOf(query) !== -1`. Recommended: (a) — codebase sudah modern, browser target IE11 tidak realistic untuk Pertamina HCS modern.

---

### IN-05: Comment `D-XX` references tanpa glossary

**File:** `Views/Admin/CreateAssessment.cshtml` (multiple — lines 311, 1462, 1467, 1474, 1484, 1508, 1541, 1551, 1552, 1563, 1567, 1571, 1589, 1647, 1801)
**Issue:**
File source mengandung banyak referensi `D-XX`, `CD-XX`, `T-XX-XX`, `Pitfall N` yang merujuk ke artifacts planning di `.planning/phases/307-...`. Untuk maintainer masa depan yang membaca source code, kode commentary akan opaque tanpa konteks plan files.

**Fix:**
Salah satu: (a) terima sebagai konvensi (developer harus baca plan), (b) ganti comment dengan deskripsi prosa tanpa kode referensi (mis. `// Phase 307 — XSS-safe via textContent` daripada `// (Pitfall 4)`). Tidak urgent — info-level housekeeping.

---

### IN-06: `wizardSelectors.ts` tidak include selector untuk `#deselectAllBtn` walau dipakai di test 7.4

**File:** `tests/e2e/helpers/wizardSelectors.ts:5-19`
**Issue:**
File centralized selector module bertujuan untuk single-source-of-truth. Test 7.4 (line 163 di `assessment.spec.ts`) menggunakan hardcoded `'#deselectAllBtn'` tanpa go via `selectors.*`. Test 7.2 dan 7.3 juga menggunakan `'.user-check-item'` literal (line 115, 131-132). Mengurangi nilai centralisasi.

**Fix:**
Tambahkan ke `selectors`:

```typescript
export const selectors = {
  // ... existing fields
  userCheckItem: '.user-check-item',
  selectAllBtn: '#selectAllBtn',
  deselectAllBtn: '#deselectAllBtn',
} as const;
```

Lalu update test 7.4 line 163 jadi `await page.click(selectors.deselectAllBtn);`.

---

_Reviewed: 2026-04-29T01:51:54Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
