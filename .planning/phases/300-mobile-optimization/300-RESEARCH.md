# Phase 300: Mobile Optimization - Research

**Researched:** 2026-04-07
**Domain:** Bootstrap 5 Responsive CSS, Mobile Touch UX, ASP.NET Core Razor Views
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
- **D-01:** Sidebar `col-lg-3` disembunyikan di breakpoint < lg. Diganti offcanvas drawer Bootstrap dari kanan, dipicu tombol floating [≡] di sticky footer
- **D-02:** Offcanvas berisi grid nomor soal (sama seperti sidebar desktop). Klik nomor langsung navigate ke soal tersebut dan tutup drawer
- **D-03:** Sidebar desktop tetap ada di lg+ — tidak ada perubahan di desktop
- **D-04:** Mobile (< 768px): list-group-item min-height 48px, padding 12px 16px. form-check-input scale 1.4 (naik dari 1.2). card-body padding 16px
- **D-05:** Desktop tetap tidak berubah — semua perubahan di dalam `@media (max-width: 767.98px)`
- **D-06:** Responsive breakpoint utama: < 992px (lg) untuk sidebar → offcanvas switch
- **D-07:** Sticky footer fixed di bawah layar (< lg) berisi: tombol Prev + tombol offcanvas [≡] + tombol Next/Submit. Selalu terlihat tanpa scroll
- **D-08:** Timer di header mobile: compact — hanya angka timer + badge save status. Label "Time Remaining" dan hub status badge disembunyikan di mobile
- **D-09:** Title assessment di header mobile: truncate dengan ellipsis jika terlalu panjang
- **D-10:** TANPA swipe gesture — navigasi halaman hanya via tombol Prev/Next di sticky footer. Ini menjaga 100% compatibility dengan anti-copy Phase 280
- **D-11:** Anti-copy events (copy/cut/paste/contextmenu/selectstart/dragstart + user-select: none) tidak perlu dimodifikasi — tidak konflik dengan layout responsive
- **D-12:** Biarkan default browser handling untuk virtual keyboard. Tidak ada handling khusus
- **D-13:** Dual optimization — portrait: offcanvas drawer + sticky footer. Landscape: sidebar kembali tampil (seperti desktop mini) karena ada cukup ruang horizontal
- **D-14:** Landscape detection via `@media (orientation: landscape) and (max-width: 991.98px)` — tampilkan sidebar, sembunyikan offcanvas trigger
- **D-15:** Mobile (< lg): 5 soal per halaman (turun dari 10). Desktop tetap 10.
- **D-16:** Page size ditentukan server-side berdasarkan User-Agent atau client-side via JS (Claude's discretion)

### Claude's Discretion
- Implementasi detail offcanvas drawer (animasi, backdrop)
- CSS transition untuk page change
- Exact spacing values selama memenuhi 48dp minimum
- Page size detection mechanism (server-side vs client-side)
- Sticky footer height dan shadow styling
- Landscape sidebar width

### Deferred Ideas (OUT OF SCOPE)
- Swipe gesture navigation
- Fullscreen essay mode
- Pull-to-refresh
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MOB-01 | Area sentuh minimal 48x48dp untuk tombol dan opsi jawaban | D-04: min-height 48px + scale 1.4 pada form-check-input; Bootstrap list-group-item pattern |
| MOB-02 | Swipe kiri/kanan untuk navigasi antar halaman soal | D-10: TIDAK diimplementasi (swipe ditunda) — MOB-02 dipenuhi via sticky footer Prev/Next |
| MOB-03 | Sticky footer (Previous, Next, Submit) + offcanvas drawer navigasi soal | D-07 + D-01: Bootstrap offcanvas component + fixed-bottom footer pattern |
| MOB-04 | Timer tetap terlihat di header mobile saat scroll | D-08: sticky-top header sudah ada; compact timer via d-none responsive class |
| MOB-05 | Anti-copy (Phase 280) tetap berfungsi dengan touch/swipe events | D-11: tidak ada konflik — anti-copy pada clipboard events, bukan touch events |
| MOB-06 | Layout responsif tanpa elemen terpotong di layar kecil | D-04 + D-06: breakpoint Bootstrap standard, media queries yang sudah teruji |
</phase_requirements>

---

## Summary

Phase 300 adalah pure CSS/HTML/JS responsive optimization pada satu file: `Views/CMP/StartExam.cshtml`. Tidak ada library baru yang diperlukan — Bootstrap 5 sudah tersedia via CDN dan menyediakan semua komponen yang dibutuhkan (offcanvas, sticky-top, responsive grid).

Perubahan utama adalah tiga hal: (1) menambah Bootstrap Offcanvas drawer untuk panel navigasi soal di mobile, (2) menambah sticky footer dengan tombol Prev/Next/[≡] yang selalu terlihat di mobile, dan (3) menerapkan CSS responsive untuk touch targets dan header compact. Semua perubahan dibatasi di dalam media queries sehingga desktop tidak terpengaruh.

Challenge terbesar adalah `questionsPerPage` yang saat ini hardcoded di Razor view sebagai `const int questionsPerPage = 10` — perlu diubah menjadi conditional berdasarkan deteksi mobile. Pilihan implementasi (server-side User-Agent vs client-side JS) memiliki tradeoff yang harus diputuskan.

**Primary recommendation:** Gunakan client-side JS untuk page size detection — lebih sederhana, tidak memerlukan perubahan controller, dan cukup akurat untuk use case ini. Server-side User-Agent detection lebih akurat tapi menambah kompleksitas controller.

---

## Standard Stack

### Core (sudah tersedia)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | 5.x (via CDN) | Offcanvas, responsive grid, sticky-top | Sudah dipakai di seluruh project [VERIFIED: codebase grep] |
| Bootstrap Icons | 5.x (via CDN) | Icon tombol (bi-list, bi-chevron-left, dll) | Sudah dipakai — `bi-box-arrow-left` di header [VERIFIED: StartExam.cshtml line 20] |

### Tidak Ada Library Baru
Semua komponen yang dibutuhkan sudah tersedia:
- **Bootstrap Offcanvas** — `<div class="offcanvas offcanvas-end">` + `data-bs-toggle="offcanvas"` [ASSUMED dari Bootstrap 5 docs pattern]
- **Bootstrap `sticky-top`** — sudah dipakai di `#examHeader` (line 10) dan `col-lg-3` sidebar
- **CSS `position: fixed; bottom: 0`** — untuk sticky footer mobile

**Installation:** Tidak ada `npm install` diperlukan.

---

## Architecture Patterns

### Recommended File Changes
Semua perubahan dalam satu file:
```
Views/CMP/StartExam.cshtml
├── HTML: Tambah offcanvas markup (setelah examContainer closing div)
├── HTML: Tambah sticky footer mobile (sebelum closing brace @else block)
├── HTML: Modifikasi #examHeader untuk responsive compact
├── CSS: Tambah @media queries di <style> block (line 1286+)
├── JS: Tambah offcanvas click handler + page size detection
└── JS: Modifikasi disable selector di changePage() agar include sticky footer buttons
```

Opsional (jika page size server-side):
```
Controllers/CMPController.cs
└── StartExam action — tambah IsMobileRequest() check, pass ke ViewBag
```

### Pattern 1: Bootstrap Offcanvas Drawer

**What:** Drawer dari kanan yang berisi grid nomor soal. Dipicu tombol [≡] di sticky footer.
**When to use:** Mobile (< lg breakpoint)

```html
<!-- Source: Bootstrap 5 Offcanvas docs [ASSUMED pattern] -->
<!-- Letakkan setelah #examContainer, sebelum </else> closing -->
<div class="offcanvas offcanvas-end" tabindex="-1"
     id="questionNavDrawer" aria-labelledby="questionNavDrawerLabel">
    <div class="offcanvas-header">
        <h5 class="offcanvas-title" id="questionNavDrawerLabel">Navigasi Soal</h5>
        <button type="button" class="btn-close" data-bs-dismiss="offcanvas"
                aria-label="Tutup"></button>
    </div>
    <div class="offcanvas-body">
        <div id="drawerNumbers" class="d-flex flex-wrap gap-2">
            <!-- Diisi oleh JS — sama seperti #panelNumbers desktop -->
        </div>
    </div>
</div>
```

**JS integration:** Gunakan `bootstrap.Offcanvas.getInstance()` untuk menutup drawer setelah klik nomor soal.

### Pattern 2: Sticky Footer Mobile

**What:** `position: fixed; bottom: 0` footer yang berisi Prev + [≡] + Next/Submit.
**When to use:** Di dalam `@media (max-width: 991.98px)` (hidden di lg+)

```html
<!-- Sticky footer — hidden di desktop via CSS -->
<div id="mobileFooter" class="d-lg-none">
    <div class="d-flex justify-content-between align-items-center gap-2">
        <button type="button" class="btn btn-outline-secondary"
                id="mobilePrevBtn" onclick="changePage(currentPage - 1)">
            <i class="bi bi-chevron-left"></i>
        </button>
        <button type="button" class="btn btn-outline-primary"
                data-bs-toggle="offcanvas" data-bs-target="#questionNavDrawer">
            <i class="bi bi-list"></i>
        </button>
        <button type="button" class="btn btn-primary"
                id="mobileNextBtn" onclick="changePage(currentPage + 1)">
            <i class="bi bi-chevron-right"></i>
        </button>
        <!-- Submit button (visible on last page only, managed by JS) -->
        <button type="button" class="btn btn-success fw-bold d-none"
                id="mobileSubmitBtn" onclick="document.getElementById('reviewSubmitBtn').click()">
            <i class="bi bi-clipboard-check"></i>
        </button>
    </div>
</div>
```

```css
/* Source: CSS standard pattern [ASSUMED] */
#mobileFooter {
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    background: #fff;
    padding: 8px 16px;
    box-shadow: 0 -2px 8px rgba(0,0,0,0.12);
    z-index: 1010; /* di bawah #examHeader (z-index: 1020) */
}
/* Beri ruang scroll agar konten tidak tertutup footer */
@media (max-width: 991.98px) {
    #examContainer { padding-bottom: 72px; }
}
```

### Pattern 3: Responsive Touch Targets

```css
/* Source: D-04 dari CONTEXT.md + Material Design 48dp standard [ASSUMED] */
@media (max-width: 767.98px) {
    .list-group-item {
        min-height: 48px;
        padding: 12px 16px;
    }
    .form-check-input {
        transform: scale(1.4);
    }
    .card-body {
        padding: 16px;
    }
}
```

### Pattern 4: Compact Header Mobile

```css
@media (max-width: 767.98px) {
    /* Truncate title */
    #examHeader h6 {
        max-width: 40vw;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
    /* Sembunyikan "Time Remaining" label dan hub status */
    #examHeader .small.text-muted { display: none; }
    #hubStatusBadge { display: none; }
    /* Sembunyikan "answered" progress di mobile */
    #answeredProgress { display: none; }
}
```

### Pattern 5: Landscape Mode Sidebar

```css
/* D-13, D-14: landscape di mobile kembilkan sidebar */
@media (orientation: landscape) and (max-width: 991.98px) {
    #questionPanelWrapper {
        display: block !important;
        /* Override sidebar-hidden state */
    }
    #questionPanelWrapper .sticky-top {
        /* Landscape sidebar width lebih kecil */
        width: 200px;
    }
    #mobileFooter { display: none !important; }
}
```

### Pattern 6: Mobile Page Size (Client-Side Detection)

Karena `questionsPerPage` adalah Razor `const int` yang menentukan jumlah `<div class="exam-page">`, page size detection HARUS server-side. Alternatif client-side adalah mengatur ulang pembagian halaman via JS setelah render — sangat kompleks.

**Recommendation: Server-side User-Agent**

```csharp
// Controllers/CMPController.cs — StartExam action
// Tambah helper method:
private bool IsMobileRequest() {
    var userAgent = Request.Headers["User-Agent"].ToString();
    return userAgent.Contains("Mobile") || userAgent.Contains("Android")
        || userAgent.Contains("iPhone") || userAgent.Contains("iPad");
}

// Di action StartExam:
ViewBag.QuestionsPerPage = IsMobileRequest() ? 5 : 10;
```

```razor
@* StartExam.cshtml — ubah baris 5-6 *@
@{
    int questionsPerPage = (int)(ViewBag.QuestionsPerPage ?? 10);
    int totalPages = (int)Math.Ceiling((double)Model.TotalQuestions / questionsPerPage);
}
```

**CATATAN PENTING:** Ini mengubah `const int` menjadi `int` biasa. JS constant `QUESTIONS_PER_PAGE` (line 367) dan semua Razor expressions yang memakai `questionsPerPage` harus konsisten.

### Anti-Patterns to Avoid

- **Jangan duplikasi `changePage()` logic** — sticky footer HARUS memanggil `changePage()` yang sudah ada, bukan fungsi baru
- **Jangan tambah event listener `touchstart`/`touchend` untuk swipe** — D-10 melarang swipe, dan ini bisa konflik dengan anti-copy
- **Jangan ubah z-index `#examHeader`** — sudah `z-index: 1020`, sticky footer harus di bawah ini
- **Jangan gunakan Bootstrap `fixed-bottom` class** — gunakan CSS manual untuk kontrol penuh atas z-index dan padding

---

## Don't Hand-Roll

| Problem | Jangan Bangun | Gunakan | Why |
|---------|---------------|---------|-----|
| Drawer/panel mobile | Custom slide-in panel JS | Bootstrap 5 Offcanvas | Sudah ada di project, accessibility built-in (aria, keyboard dismiss, backdrop) |
| Backdrop saat drawer terbuka | Custom overlay div | Bootstrap Offcanvas backdrop otomatis | Bawaan Bootstrap, tidak perlu kode tambahan |
| Sticky header | Custom scroll listener + CSS | Bootstrap `sticky-top` class | Sudah dipakai di `#examHeader`, proven pattern |

---

## Common Pitfalls

### Pitfall 1: `changePage()` disable selector tidak meng-cover sticky footer buttons

**What goes wrong:** `changePage()` saat ini disable tombol via selector `[onclick^="changePage"]`. Sticky footer buttons yang baru memakai event listener (bukan inline onclick) atau `id` yang berbeda akan TIDAK ter-disable saat save sedang berjalan.

**Why it happens:** Selector hanya menangkap elemen dengan attribute `onclick` yang dimulai "changePage".

**How to avoid:** Tambahkan `#mobilePrevBtn, #mobileNextBtn` ke selector disable, ATAU gunakan CSS class `mobile-nav-btn` dan target via class selector.

**Warning signs:** Di mobile, user bisa klik tombol berkali-kali saat save sedang berjalan.

### Pitfall 2: Z-index conflict antara sticky footer dan offcanvas

**What goes wrong:** Bootstrap Offcanvas memiliki z-index tinggi (~1045). Jika sticky footer z-index terlalu tinggi, bisa overlap dengan offcanvas backdrop.

**How to avoid:** Set `#mobileFooter` z-index 1010 (di bawah offcanvas 1045 dan di bawah header 1020).

### Pitfall 3: Content terpotong di bawah sticky footer

**What goes wrong:** Content terakhir di halaman (tombol Previous/Next inline) bisa tertutup sticky footer jika `padding-bottom` tidak ditambahkan ke `#examContainer`.

**How to avoid:** `@media (max-width: 991.98px) { #examContainer { padding-bottom: 72px; } }`

### Pitfall 4: Desktop sidebar rusak saat landscape media query aktif

**What goes wrong:** `@media (orientation: landscape) and (max-width: 991.98px)` bisa aktif di desktop browser yang di-resize sempit, menyebabkan sidebar tampil dengan lebar yang salah.

**How to avoid:** Verifikasi media query di browser desktop dengan resize window ke < 992px landscape.

### Pitfall 5: questionsPerPage Razor constant → server-side ViewBag

**What goes wrong:** Mengubah `const int questionsPerPage = 10` ke `ViewBag.QuestionsPerPage` membutuhkan perubahan di semua tempat yang memakai variable ini — baris 5, 6, 86-87, 392, 401 di StartExam.cshtml.

**How to avoid:** Grep semua penggunaan `questionsPerPage` dan pastikan semua diubah konsisten sebelum testing.

### Pitfall 6: Panel numbers drawer tidak sync dengan desktop panel

**What goes wrong:** Jika drawer menggunakan div terpisah (`#drawerNumbers`), JS `updatePanel()` harus memperbarui KEDUANYA — desktop `#panelNumbers` dan mobile `#drawerNumbers`.

**How to avoid:** Abstraksi `updatePanel()` agar menulis ke dua container sekaligus, atau gunakan DOM reference ke drawer numbers.

---

## Code Examples

### Offcanvas Close Setelah Navigasi (JS)

```javascript
// Source: Bootstrap 5 Offcanvas API [ASSUMED - standard pattern]
// Di dalam click handler nomor soal di drawer:
function navigateFromDrawer(pageNum) {
    var drawerEl = document.getElementById('questionNavDrawer');
    var drawer = bootstrap.Offcanvas.getInstance(drawerEl);
    if (drawer) drawer.hide();
    changePage(pageNum, false);
}
```

### Sync Panel Numbers ke Drawer

```javascript
// Modifikasi updatePanel() yang sudah ada untuk juga update drawer
function updatePanel() {
    // ... existing logic untuk #panelNumbers ...

    // Sync ke mobile drawer
    var drawerNumbers = document.getElementById('drawerNumbers');
    if (drawerNumbers) {
        drawerNumbers.innerHTML = document.getElementById('panelNumbers').innerHTML;
        // Re-attach click handlers (onclick sudah inline di badge, tidak perlu re-attach)
    }
}
```

### Mobile Prev/Next Button State Management

```javascript
// Panggil ini setelah setiap changePage()
function updateMobileNavButtons() {
    var prevBtn = document.getElementById('mobilePrevBtn');
    var nextBtn = document.getElementById('mobileNextBtn');
    var submitBtn = document.getElementById('mobileSubmitBtn');

    if (prevBtn) prevBtn.disabled = (currentPage === 0);

    var isLastPage = (currentPage === TOTAL_PAGES - 1);
    if (nextBtn) {
        nextBtn.style.display = isLastPage ? 'none' : '';
        nextBtn.disabled = isLastPage;
    }
    if (submitBtn) {
        submitBtn.style.display = isLastPage ? '' : 'none';
    }
}
```

---

## State of the Art

| Pendekatan Lama | Pendekatan Saat Ini | Relevansi untuk Phase 300 |
|-----------------|---------------------|--------------------------|
| Sidebar `col-lg-3` selalu tampil | Sidebar hidden < lg, offcanvas di mobile | D-01: ini yang kita implementasi |
| `questionsPerPage = 10` hardcoded | Conditional berdasarkan viewport | D-15, D-16: perlu perubahan |
| Inline Prev/Next buttons per page | Sticky footer mobile + inline desktop | D-07: sticky footer adalah addisi, bukan pengganti |
| `form-check-input scale(1.2)` | Scale 1.4 di mobile | D-04: naik dari 1.2 existing |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Bootstrap Offcanvas API: `bootstrap.Offcanvas.getInstance(el)` untuk menutup secara programmatic | Code Examples | Nama method mungkin berbeda; cek Bootstrap 5 docs sebelum implement |
| A2 | User-Agent string detection di CMPController dengan `Request.Headers["User-Agent"]` | Pattern 6 | ASP.NET Core cara akses header mungkin berbeda; verifikasi di controller |
| A3 | Bootstrap Offcanvas z-index default ~1045 | Pitfall 2 | Perlu verifikasi angka pasti di Bootstrap CSS |
| A4 | `d-lg-none` Bootstrap utility class untuk hide di lg+ | Pattern 2 | Pastikan Bootstrap 5 mendukung ini (BUKAN Bootstrap 4 pattern) |

---

## Open Questions

1. **Client-side vs Server-side page size detection**
   - Yang diketahui: D-16 menyebut keduanya sebagai opsi (Claude's discretion)
   - Yang direkomendasikan: Server-side (lebih akurat untuk 5-soal kondisi yang bergantung pada rendering Razor)
   - Alasan: `questionsPerPage` dipakai di Razor loop HTML generation — nilai ini HARUS diketahui sebelum HTML dirender, tidak bisa client-side

2. **`updatePanel()` sync ke drawer**
   - Yang diketahui: Saat ini `updatePanel()` hanya menulis ke `#panelNumbers`
   - Yang perlu dilakukan: Extend untuk juga mengisi `#drawerNumbers`
   - Pilihan: Clone innerHTML atau share satu DOM container (offcanvas display via absolute position overlay, bukan hidden element)

---

## Environment Availability

Step 2.6: SKIPPED — Phase ini pure CSS/HTML/JS dalam file Razor yang sudah ada. Tidak ada external dependencies baru. Bootstrap 5 sudah tersedia via CDN yang dipakai project.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada automated test untuk Razor views di project ini) |
| Config file | none |
| Quick run command | Jalankan aplikasi, buka StartExam di Chrome DevTools mobile emulation |
| Full suite command | Test di device fisik + Chrome/Firefox/Safari mobile emulation |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MOB-01 | Touch targets min 48px | Manual | Inspect element di DevTools → computed height ≥ 48px | N/A |
| MOB-02 | Navigasi via Prev/Next (no swipe) | Manual | Klik tombol Prev/Next di sticky footer | N/A |
| MOB-03 | Sticky footer + offcanvas drawer | Manual | Scroll ke bawah, footer tetap terlihat; klik [≡] buka drawer | N/A |
| MOB-04 | Timer visible saat scroll | Manual | Scroll ke bawah, timer tetap di header sticky | N/A |
| MOB-05 | Anti-copy tetap berfungsi | Manual | Coba select+copy teks soal di mobile browser | N/A |
| MOB-06 | Layout tidak terpotong | Manual | Test di 320px, 375px, 414px viewport width | N/A |

### Sampling Rate
- **Per task:** Buka StartExam di Chrome DevTools, toggle device toolbar ke iPhone SE (375px)
- **Per wave:** Test semua breakpoints: 320px, 375px, 768px, 992px, landscape 812px
- **Phase gate:** Test di real device (Android atau iOS) sebelum `/gsd-verify-work`

### Wave 0 Gaps
- Tidak ada test files baru yang diperlukan — semua validasi manual

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | — |
| V3 Session Management | no | — |
| V4 Access Control | no | — |
| V5 Input Validation | no | Tidak ada input baru |
| V6 Cryptography | no | — |

**Catatan:** Phase ini adalah pure UI/CSS change. Tidak ada data flow baru, endpoint baru, atau input baru. Anti-copy protection yang sudah ada (Phase 280) tidak diubah.

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Touch event injection untuk bypass anti-copy | Tampering | D-11: tidak modifikasi anti-copy events; `user-select: none` tetap aktif di `.exam-protected` |

---

## Sources

### Primary (HIGH confidence)
- `Views/CMP/StartExam.cshtml` — kode aktual yang akan dimodifikasi, dibaca langsung
- `.planning/phases/300-mobile-optimization/300-CONTEXT.md` — keputusan user yang locked
- `.planning/REQUIREMENTS.md` — definisi MOB-01 s/d MOB-06

### Secondary (MEDIUM confidence)
- Bootstrap 5 docs pattern untuk Offcanvas component [ASSUMED - training knowledge, tidak diverifikasi via Context7 session ini]

### Tertiary (LOW confidence)
- Material Design 48dp touch target standard [ASSUMED - well-known standard tapi tidak diverifikasi khusus untuk session ini]

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru; semua komponen sudah ada di codebase
- Architecture: HIGH — semua perubahan di satu file yang sudah dibaca lengkap
- Pitfalls: HIGH — diidentifikasi dari membaca kode aktual (bukan spekulatif)
- Page size mechanism: MEDIUM — rekomendasi server-side masuk akal tapi satu A2 perlu verifikasi

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (stable stack, Bootstrap 5 tidak berubah cepat)
