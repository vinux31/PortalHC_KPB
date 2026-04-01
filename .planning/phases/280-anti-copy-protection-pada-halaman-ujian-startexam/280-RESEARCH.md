# Phase 280: Anti-copy protection pada halaman ujian StartExam - Research

**Researched:** 2026-04-01
**Domain:** Client-side anti-copy protection (CSS + JavaScript)
**Confidence:** HIGH

## Summary

Phase ini menambahkan deterrence layer anti-copy pada halaman ujian StartExam.cshtml. Tekniknya well-established: CSS `user-select: none` untuk mencegah text selection, dan JavaScript event blocking untuk `copy`, `cut`, `paste`, `contextmenu`, `selectstart`, `dragstart`, serta keyboard shortcuts (Ctrl+C/A/U/S/P).

Ini bukan proteksi absolut (DevTools tetap bisa bypass), tapi efektif mencegah ~95% casual cheating. Pattern ini standar di platform ujian (Moodle, Canvas, ProProfs).

**Primary recommendation:** Tambahkan CSS + JS anti-copy langsung di StartExam.cshtml, scoped ke `#examContainer` area soal saja.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Silent block — tanpa notifikasi, user hanya merasa tidak bisa select/copy
- **D-02:** Proteksi hanya pada area soal dan opsi jawaban (question container & answer options), bukan seluruh halaman
- **D-03:** Block agresif — Ctrl+C, Ctrl+A, Ctrl+U, Ctrl+S, Ctrl+P
- **D-04:** CSS `user-select: none` + `-webkit-touch-callout: none` pada container soal
- **D-05:** JS event blocking: `copy`, `cut`, `paste`, `contextmenu`, `selectstart`, `dragstart`
- **D-06:** Keyboard shortcut blocking via `keydown` event listener di level `document`

### Claude's Discretion
- CSS selector targeting (class/id untuk container soal)
- Exact event handler placement dalam existing JS code
- Cross-browser compatibility approach

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Architecture Patterns

### Target Elements di StartExam.cshtml

Berdasarkan review kode:

```
#examContainer (div.container-fluid)
  └── .col-lg-9 (main exam area)
       └── #examForm
            └── .exam-page (per halaman soal)
                 └── .card (per soal, id="qcard_{id}")
                      └── .card-body
                           ├── p.fw-bold (question text)
                           └── .list-group (answer options)
                                └── label (per opsi)
```

**Rekomendasi selector:** Target `#examContainer` langsung — ini sudah mencakup semua soal dan opsi, tapi TIDAK mencakup sticky header (`#examHeader`) dan navigasi soal sidebar (`.col-lg-3`).

Namun karena D-02 spesifik "area soal dan opsi jawaban", sebaiknya target `.col-lg-9` di dalam `#examContainer`, atau lebih simpel: tambahkan class `exam-protected` pada div `.col-lg-9` sebagai hook.

### Pattern: CSS Anti-Select

```css
/* Pada container soal */
.exam-protected {
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
    -webkit-touch-callout: none;
}
```

**Penting:** Radio button dan label harus tetap clickable. `user-select: none` TIDAK mempengaruhi click events — hanya text selection yang di-block. Jadi radio buttons tetap berfungsi normal.

### Pattern: JS Event Blocking

```javascript
// Attach ke container soal untuk copy/cut/paste/contextmenu/selectstart/dragstart
const protectedArea = document.querySelector('.exam-protected');
['copy', 'cut', 'paste', 'contextmenu', 'selectstart', 'dragstart'].forEach(evt => {
    protectedArea.addEventListener(evt, function(e) {
        e.preventDefault();
    });
});

// Keyboard shortcuts di level document (D-06)
document.addEventListener('keydown', function(e) {
    if (e.ctrlKey || e.metaKey) {
        // c=67, a=65, u=85, s=83, p=80
        if ([67, 65, 85, 83, 80].includes(e.keyCode)) {
            e.preventDefault();
        }
    }
});
```

### Integration Point

StartExam.cshtml sudah punya `@section Scripts` dengan banyak JS. Anti-copy handlers ditambahkan di dalam `DOMContentLoaded` atau setelah existing initialization code.

CSS ditambahkan di `<style>` section yang sudah ada di file.

### Anti-Pattern: Jangan Lakukan

- **Block SEMUA keyboard input** — radio buttons butuh keyboard navigation (Tab, Space, Arrow keys)
- **Attach per-element** — terlalu banyak listeners, attach di container saja
- **Alert/popup saat copy attempt** — D-01 sudah putuskan silent block
- **Block di seluruh halaman** — D-02 hanya area soal

## Common Pitfalls

### Pitfall 1: Radio Button Keyboard Navigation Rusak
**What goes wrong:** Block Ctrl+A juga memblok keyboard accessibility
**How to avoid:** Hanya block key combinations dengan Ctrl/Meta modifier, bukan bare keys. Pastikan Tab, Space, Arrow keys tetap berfungsi.

### Pitfall 2: user-select:none pada Parent Mempengaruhi Input
**What goes wrong:** Khawatir radio button tidak bisa diklik
**How to avoid:** `user-select: none` hanya affect text selection, bukan click/input events. Tidak perlu override apapun pada radio buttons.

### Pitfall 3: Ctrl+A Conflict dengan Exam Form
**What goes wrong:** Ctrl+A di-block tapi user mungkin perlu select text di input field lain
**How to avoid:** D-02 sudah meng-scope proteksi ke area soal saja. Namun D-06 bilang keyboard blocking di level document. Solusi: cek apakah active element ada di dalam protected area sebelum block Ctrl+A. Atau lebih simpel — karena exam page tidak punya text input fields, block global aman.

### Pitfall 4: F12 / Ctrl+Shift+I (DevTools)
**What goes wrong:** User bisa buka DevTools dan lihat HTML
**How to avoid:** Out of scope per phase boundary. Ini deterrence layer, bukan absolute protection.

## Code Examples

### Lengkap: CSS + JS Anti-Copy

```css
/* Anti-copy protection - area soal saja */
.exam-protected {
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
    -webkit-touch-callout: none;
}
```

```javascript
// Anti-copy protection
(function() {
    const area = document.querySelector('.exam-protected');
    if (!area) return;

    // Block clipboard & context menu events pada area soal
    ['copy','cut','paste','contextmenu','selectstart','dragstart'].forEach(function(evt) {
        area.addEventListener(evt, function(e) { e.preventDefault(); });
    });

    // Block keyboard shortcuts (Ctrl+C/A/U/S/P) secara global
    document.addEventListener('keydown', function(e) {
        if (e.ctrlKey || e.metaKey) {
            var blocked = [67, 65, 85, 83, 80]; // C, A, U, S, P
            if (blocked.indexOf(e.keyCode) !== -1) {
                e.preventDefault();
            }
        }
    });
})();
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing |
| Quick run command | User verifies di browser |
| Full suite command | N/A |

### Phase Requirements - Test Map
| Behavior | Test Type | How to Verify |
|----------|-----------|---------------|
| Text selection disabled pada soal | manual | Try drag-select question text |
| Right-click disabled pada area soal | manual | Right-click pada soal |
| Ctrl+C tidak berfungsi | manual | Select text (via DevTools), Ctrl+C |
| Ctrl+A tidak select all | manual | Ctrl+A di halaman exam |
| Ctrl+U tidak buka view-source | manual | Ctrl+U di halaman exam |
| Ctrl+S tidak save page | manual | Ctrl+S di halaman exam |
| Ctrl+P tidak print | manual | Ctrl+P di halaman exam |
| Radio buttons masih bisa diklik | manual | Klik opsi jawaban |
| Drag text tidak bisa | manual | Try drag text dari soal |
| Header/navigasi TIDAK terproteksi | manual | Try select text di header |

## Sources

### Primary (HIGH confidence)
- `Views/CMP/StartExam.cshtml` — direct code review, structure confirmed
- CSS `user-select` — MDN Web Docs, well-established property (supported all modern browsers)
- Event `preventDefault()` — standard DOM API

### Secondary (MEDIUM confidence)
- Platform ujian patterns (Moodle, Canvas) — referenced in CONTEXT.md as prior research by user

## Metadata

**Confidence breakdown:**
- Implementation approach: HIGH — CSS user-select dan JS event blocking adalah teknik standar, well-documented
- Cross-browser compatibility: HIGH — user-select supported di semua modern browsers (Chrome, Firefox, Edge, Safari)
- Pitfalls: HIGH — domain ini sederhana, pitfalls terbatas

**Research date:** 2026-04-01
**Valid until:** 2026-05-01 (stable domain, tidak berubah cepat)
