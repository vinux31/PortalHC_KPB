# Phase 276: Navigasi soal di StartExam - Research

**Researched:** 2026-04-01
**Domain:** Frontend UI Enhancement - Navigation Panel
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Grid layout (multi-kolom) untuk menampilkan semua nomor soal
- **D-02:** Grid 10 kolom — soal 1-10 dalam satu baris (sejajar dengan 10 soal per halaman)
- **D-03:** 2 warna saja — Hijau (answered/sudah diisi jawaban), Abu-abu (unanswered/belum diisi)
- **D-04:** Klik nomor soal → langsung loncat ke halaman + scroll ke soal tersebut, tanpa animasi (immediate jump)
  - Klik soal di halaman yang sama: scroll ke soal tersebut
  - Klik soal di halaman berbeda: switch page terlebih dahulu, lalu scroll ke soal
- **D-05:** Desktop — panel kanan tetap visible (col-lg-3). Mobile — panel collapsed/hidden dengan toggle button
- **D-06:** Header panel text: "Daftar Soal" (menggantikan "Questions this page")
- **D-07:** Current question tidak perlu penandaan khusus (border, bold, atau warna berbeda). User bisa melihat posisi dari scroll position
- **D-08:** Modify existing `updatePanel()` function untuk render ALL questions, bukan hanya current page

### Claude's Discretion
- **D-09:** Auto-scroll ke current question saat page change — panel otomatis scroll menampilkan questions di halaman saat ini. Ini membantu user tetap oriented terutama untuk assessment dengan banyak soal.
- CSS styling detail (padding, gap, border-radius untuk badge grid)
- Exact implementation untuk auto-scroll behavior (smooth vs instant scroll)

### Deferred Ideas (OUT OF SCOPE)
- "Jump to first unanswered" button — fitur tambahan yang bisa jadi phase terpisah
- "Jump to last answered" button — fitur tambahan yang bisa jadi phase terpisah
- Question search/filter di panel — enhancement untuk future phase
- Progress percentage indicator di panel — enhancement untuk future phase
</user_constraints>

## Summary

Phase ini adalah enhancement navigasi soal di halaman StartExam untuk menampilkan SELURUH nomor soal (1 sampai N) dalam panel navigasi dengan grid layout 10 kolom, menggantikan implementasi saat ini yang hanya menampilkan soal di halaman aktif. User dapat langsung klik nomor soal untuk loncat ke lokasi soal tersebut (switch halaman jika perlu, lalu scroll ke posisi soal). Indikator visual menggunakan 2 warna: hijau untuk soal yang sudah dijawab, abu-abu untuk belum dijawab.

Implementasi memerlukan modifikasi fungsi `updatePanel()` yang sudah ada untuk mengiterasi SEMUA question IDs dari `Model.Questions` (bukan hanya `pageQuestionIds[currentPage]`), mengubah layout panel dari flexbox menjadi CSS Grid 10 kolom, menambahkan fungsi `jumpToQuestion(qId)` untuk handle click event, dan implementasi auto-scroll panel saat page change untuk menjaga orientasi user.

**Primary recommendation:** Modify existing `updatePanel()` function to render ALL questions with CSS Grid layout, add `jumpToQuestion()` function with page-switch-then-scroll logic, and implement auto-scroll for panel container on page changes.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | 5.x (project default) | Badge styling, responsive grid (col-lg-3), utility classes | Project sudah menggunakan Bootstrap 5 untuk seluruh UI |
| CSS Grid | Native CSS | Layout 10 kolom untuk badge numbers | Modern CSS, lebih efisien daripada manual positioning/table |
| Vanilla JavaScript | ES6+ | Navigation logic, scroll behavior | Project tidak menggunakan framework JS frontend |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Badges | 5.x | Visual indicator (bg-success, bg-secondary) | Untuk menandai status answered/unanswered |
| scrollIntoView() | Native API | Jump ke posisi soal saat klik nomor | Gunakan `behavior: 'auto'` untuk instant scroll |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| CSS Grid 10 kolom | Bootstrap row dengan 10 col-* | Grid lebih clean, tidak perlu hitung persentase manual |
| scrollIntoView() | Manual scrollTop calculation | scrollIntoView() lebih reliable cross-browser |
| Instant scroll | Smooth scroll | Instant lebih sesuai dengan kebutuhan user (tanpa animasi) |

**Installation:**
Tidak perlu instalasi library baru. Semua teknologi yang dibutuhkan sudah ada di project:
- Bootstrap 5 sudah ada di project
- CSS Grid adalah native browser feature (ES 2017+)
- Vanilla JavaScript dan scrollIntoView() adalah native API

**Version verification:** Tidak perlu — menggunakan native browser APIs dan existing Bootstrap 5 dari project.

## Architecture Patterns

### Recommended Project Structure
```
Views/CMP/StartExam.cshtml (existing file, modified)
├── HTML structure (lines 159-177)
│   ├── #questionPanelWrapper (col-lg-3, sticky-top)
│   │   ├── Header text "Daftar Soal" (line 163)
│   │   └── #questionPanel (card container)
│   │       └── #panelNumbers (grid container untuk badges)
│   └── #togglePanelBtn (collapse/expand panel)
│
├── JavaScript functions (modified/new)
│   ├── updatePanel() (MODIFIED - line 772)
│   │   └── Loop ALL questions (bukan hanya current page)
│   ├── jumpToQuestion(qId, pageNumber) (NEW)
│   │   ├── Switch page jika perlu
│   │   └── Scroll ke element #qcard_{qId}
│   └── performPageSwitch() (MODIFIED - line 715)
│       └── Add auto-scroll panel ke current page questions
│
└── CSS styling (inline <style> block, line 1000+)
    ├── #panelNumbers { display: grid; grid-template-columns: repeat(10, 1fr); gap: 0.5rem; }
    ├── .question-badge { cursor: pointer; transition: transform 0.1s; }
    ├── .question-badge:hover { transform: scale(1.1); }
    └── @media (max-width: 991px) { #questionPanelWrapper display logic }
```

### Pattern 1: CSS Grid untuk 10-Kolom Badge Layout
**What:** Menggunakan CSS Grid dengan `grid-template-columns: repeat(10, 1fr)` untuk membuat layout 10 kolom otomatis
**When to use:** Menampilkan banyak badge numbers dalam grid yang rapi dan responsive
**Example:**
```css
/* Source: CSS Grid specification (W3C) */
#panelNumbers {
    display: grid;
    grid-template-columns: repeat(10, 1fr); /* 10 kolom sama lebar */
    gap: 0.5rem; /* Jarak antar badges */
}

.question-badge {
    font-size: 0.85rem;
    padding: 6px 10px;
    cursor: pointer;
    transition: transform 0.1s ease;
}

.question-badge:hover {
    transform: scale(1.1); /* Sedikit zoom saat hover */
}
```

### Pattern 2: jumpToQuestion() dengan Page Switch + Scroll
**What:** Fungsi JavaScript untuk menangani klik nomor soal: switch halaman jika target soal di page berbeda, lalu scroll ke posisi soal
**When to use:** User klik nomor soal di panel navigasi untuk loncat ke lokasi soal tersebut
**Example:**
```javascript
// Source: Existing code pattern from StartExam.cshtml
function jumpToQuestion(qId, targetPage) {
    // Case 1: Soal di halaman berbeda — switch page dulu
    if (targetPage !== currentPage) {
        // Switch page (existing pattern from performPageSwitch)
        document.getElementById('page_' + currentPage).style.display = 'none';
        currentPage = targetPage;
        document.getElementById('page_' + currentPage).style.display = 'block';
        updatePanel();
        window.scrollTo(0, 0);
    }

    // Case 2: Scroll ke soal (instant, tanpa animasi)
    const qcard = document.getElementById('qcard_' + qId);
    if (qcard) {
        qcard.scrollIntoView({ behavior: 'auto', block: 'start' });
    }
}
```

### Pattern 3: Auto-Scroll Panel saat Page Change
**What:** Panel otomatis scroll menampilkan questions di halaman saat ini setelah page switch
**When to use:** Menjaga orientasi user, terutama untuk assessment dengan banyak soal (>50)
**Example:**
```javascript
// Source: Extension of existing performPageSwitch pattern
function performPageSwitch(newPage) {
    document.getElementById('page_' + currentPage).style.display = 'none';
    currentPage = newPage;
    document.getElementById('page_' + currentPage).style.display = 'block';
    updatePanel();

    // NEW: Auto-scroll panel ke questions di halaman ini
    autoScrollPanelToCurrentPage();

    saveSessionProgress();
    window.scrollTo(0, 0);

    // ... existing SignalR logging ...
}

function autoScrollPanelToCurrentPage() {
    // Cari question ID pertama di halaman ini
    const pageQIds = pageQuestionIds[currentPage] || [];
    if (pageQIds.length === 0) return;

    const firstQId = pageQIds[0];
    const firstBadge = document.querySelector('[data-question-id="' + firstQId + '"]');
    if (firstBadge) {
        // Scroll panel supaya first badge visible di bagian atas panel
        firstBadge.scrollIntoView({ behavior: 'auto', block: 'nearest' });
    }
}
```

### Pattern 4: Mobile Responsive Panel
**What:** Panel navigasi default collapsed/hidden di mobile, dengan toggle button untuk show/hide
**When to use:** Layar kecil (mobile) di mana panel navigasi mengambil terlalu banyak space
**Example:**
```css
/* Source: Existing media query pattern from StartExam.cshtml line 1053 */
@media (max-width: 991px) {
    #questionPanelWrapper {
        display: none; /* Default hidden di mobile */
    }
    #questionPanelWrapper.visible {
        display: block; /* Show saat toggle active */
        order: -1; /* Pindahkan ke atas content */
        margin-bottom: 1rem;
    }
}
```

### Anti-Patterns to Avoid
- **Jangan gunakan smooth scroll untuk jump:** User request "immediate jump" tanpa animasi. Gunakan `behavior: 'auto'`
- **Jangan render soal satu per satu di DOM:** Render SEMUA badges sekaligus di `updatePanel()`, jangan lazy load. Jumlah soal biasanya <100, performance impact negligible
- **Jangan gunakan tabel untuk layout:** Tabel rigid dan tidak responsive. Gunakan CSS Grid
- **Jangan override existing Bootstrap classes:** Tambahkan custom class atau inline style, jangan modify `.badge` global

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| 10-kolom layout | Manual positioning dengan absolute/left% | CSS Grid `repeat(10, 1fr)` | Grid handles responsiveness, spacing, alignment automatically |
| Scroll behavior | Manual scrollTop calculation dengan offset | `scrollIntoView({ behavior: 'auto' })` | Cross-browser compatibility, handles viewport edge cases |
| Badge styling | Custom CSS untuk rounded corners, colors, spacing | Bootstrap classes `.badge`, `.rounded-pill`, `.bg-success`, `.bg-secondary` | Consistent dengan existing UI, less custom CSS |
| Panel collapse/expand | Manual show/hide dengan custom state | Existing `togglePanel()` function pattern | Reuse existing code, consistent UX |

**Key insight:** Semua building blocks yang dibutuhkan SUDAH ADA di codebase. Ini adalah pure enhancement, tidak ada new library atau external dependencies. Fokus pada memodifikasi existing patterns dengan tepat.

## Runtime State Inventory

> Phase ini adalah greenfield enhancement (tampilan baru), bukan rename/refactor/migration. Tidak ada runtime state yang perlu di-inventory.

**Step 2.5: SKIPPED (greenfield enhancement phase - no runtime state impact)**

## Common Pitfalls

### Pitfall 1: Off-by-One Error di Question Number Mapping
**What goes wrong:** Question ID di database tidak sama dengan display number yang dilihat user (misal: QuestionId=45 tapi DisplayNumber="10")
**Why it happens:** Code menggunakan `q.QuestionId` untuk ID cards dan tracking, tapi user lihat `q.DisplayNumber` di UI
**How to avoid:** Di `updatePanel()`, extract display number dari `.badge.bg-primary` text di dalam `#qcard_{qId}`, jangan gunakan QuestionId langsung
**Warning signs:** Panel shows wrong numbers, clicking number jumps to wrong question
```javascript
// WRONG: langsung gunakan QuestionId
btn.innerText = qId;

// CORRECT: extract DisplayNumber dari card badge
const qcard = document.getElementById('qcard_' + qId);
const badge = qcard.querySelector('.badge.bg-primary');
btn.innerText = badge ? badge.innerText.trim() : qId;
```

### Pitfall 2: Scroll ke Element yang Belum Visible
**What goes wrong:** `scrollIntoView()` dipanggil untuk soal di halaman yang masih `display: none`, sehingga scroll tidak bekerja
**Why it happens:** Page switch dan scroll terjadi secara asynchronous, atau urutan salah (scroll dulu, baru switch page)
**How to avoid:** Di `jumpToQuestion()`, PASTIKAN page switch SELESAI (element visible) baru panggil `scrollIntoView()`
**Warning signs:** Click nomor soal di page lain, page berubah tapi tidak scroll ke soal
```javascript
// WRONG: scroll dan page switch terpisah
changePage(targetPage);
qcard.scrollIntoView(); // qcard mungkin belum visible!

// CORRECT: switch dulu, tunggu render, baru scroll
if (targetPage !== currentPage) {
    performPageSwitch(targetPage); // Ini set display: block
}
// Sekarang baru scroll (qcard sudah visible)
qcard.scrollIntoView({ behavior: 'auto' });
```

### Pitfall 3: Panel Scroll di Mobile Membuat Confusion
**What goes wrong:** Di mobile, panel navigasi scrollable tapi user tidak sadar ada more content di bawah fold
**Why it happens:** Panel height terbatas di mobile, CSS `overflow` tidak set dengan benar
**How to avoid:** Tambahkan visual indicator (shadow/gradient) di bottom panel jika ada content scrollable, atau limit panel height dan set `overflow-y: auto`
**Warning signs:** User mobile hanya melihat 10 soal pertama, tidak sadar bisa scroll panel untuk lihat soal lain
```css
/* Tambahkan di mobile styles */
@media (max-width: 991px) {
    #questionPanel {
        max-height: 200px;
        overflow-y: auto;
    }
    /* Shadow indicator untuk scrollable content */
    #questionPanel::after {
        content: '';
        position: sticky;
        bottom: 0;
        height: 20px;
        background: linear-gradient(transparent, rgba(0,0,0,0.1));
        pointer-events: none;
    }
}
```

### Pitfall 4: updatePanel() Dipanggil Sebelum DOM Ready
**What goes wrong:** `updatePanel()` mencari `#qcard_{qId}` elements yang belum di-render, sehingga display numbers tidak tampil
**Why it happens:** Function dipanggil terlalu awal (sebelum `<body>` fully parsed), atau dipanggil dari script yang di-load sebelum DOM
**How to avoid:** Pastikan `updatePanel()` dipanggil setelah DOM ready, atau wrap di `DOMContentLoaded`
**Warning signs:** Panel badges show QuestionId numbers (45, 67, dll) bukannya DisplayNumber (1, 2, 3)
```javascript
// WRONG: langsung panggil di inline script
updatePanel();

// CORRECT: tunggu DOM ready
document.addEventListener('DOMContentLoaded', function() {
    updatePanel();
});
// ATAU: panggil di akhir body (existing pattern di StartExam.cshtml line 797)
```

### Pitfall 5: Lupa Update Panel Setelah Jawab Soal
**What goes wrong:** User menjawab soal, tapi badge di panel tidak berubah warna (tetap abu-abu, tidak hijau)
**Why it happens:** `saveAnswer()` atau `updateProgressStatus()` tidak memanggil `updatePanel()` setelah update `answeredQuestions` Set
**How to avoid:** Tambahkan `updatePanel()` call di existing flow setelah `answeredQuestions.add(qId)`
**Warning signs:** Panel colors stale, tidak sinkron dengan actual answer status
```javascript
// Di updateProgressStatus(qId) atau setelah saveAnswer success
function updateProgressStatus(qId) {
    answeredQuestions.add(String(qId)); // Existing code
    document.getElementById('answeredProgress').innerText = ...; // Existing
    updatePanel(); // ADD THIS LINE
}
```

## Code Examples

Verified patterns from existing codebase:

### Mengakses Semua Question IDs (bukan per page)
```javascript
// Source: StartExam.cshtml line 334-339 (existing pageQuestionIds pattern)
// MODIFY: Flatten pageQuestionIds untuk mendapatkan semua questions

// OLD (existing - hanya current page):
const ids = pageQuestionIds[currentPage] || [];

// NEW (modified - semua questions):
const allQuestionIds = [];
pageQuestionIds.forEach(function(pageIds) {
    allQuestionIds = allQuestionIds.concat(pageIds);
});
// allQuestionIds sekarang berisi [q1, q2, ..., qN] untuk semua soal
```

### Extract Display Number dari Question Card
```javascript
// Source: StartExam.cshtml line 782-787 (existing pattern di updatePanel)
const qcard = document.getElementById('qcard_' + qId);
let displayNum = qId;
if (qcard) {
    const badge = qcard.querySelector('.badge.bg-primary');
    if (badge) displayNum = badge.innerText.trim();
}
btn.innerText = displayNum;
```

### Check Answered Status
```javascript
// Source: StartExam.cshtml line 331 (existing answeredQuestions Set)
const answered = answeredQuestions.has(String(qId));
btn.className = 'badge rounded-pill ' + (answered ? 'bg-success' : 'bg-secondary');
```

### Page Switch Pattern
```javascript
// Source: StartExam.cshtml line 715-729 (existing performPageSwitch)
function performPageSwitch(newPage) {
    document.getElementById('page_' + currentPage).style.display = 'none';
    currentPage = newPage;
    document.getElementById('page_' + currentPage).style.display = 'block';
    updatePanel();
    saveSessionProgress();
    window.scrollTo(0, 0);
}
```

### Scroll ke Element
```javascript
// Source: MDN Web Docs - scrollIntoView API
// NEW function untuk jump to question
function jumpToQuestion(qId, targetPage) {
    // Switch page jika perlu
    if (targetPage !== currentPage) {
        document.getElementById('page_' + currentPage).style.display = 'none';
        currentPage = targetPage;
        document.getElementById('page_' + currentPage).style.display = 'block';
        updatePanel();
        saveSessionProgress();
    }

    // Scroll ke question card (instant, tanpa animasi)
    const qcard = document.getElementById('qcard_' + qId);
    if (qcard) {
        qcard.scrollIntoView({ behavior: 'auto', block: 'start' });
    }
}
```

### Auto-Scroll Panel untuk Current Page
```javascript
// Source: Extension of existing patterns
// NEW function untuk auto-scroll panel ke current page questions
function autoScrollPanelToCurrentPage() {
    const pageQIds = pageQuestionIds[currentPage] || [];
    if (pageQIds.length === 0) return;

    const firstQId = pageQIds[0];
    const firstBadge = document.querySelector('[data-question-id="' + firstQId + '"]');
    if (firstBadge) {
        // Scroll panel supaya first visible badge untuk page ini
        firstBadge.scrollIntoView({ behavior: 'auto', block: 'nearest' });
    }
}

// Integrate ke performPageSwitch
function performPageSwitch(newPage) {
    document.getElementById('page_' + currentPage).style.display = 'none';
    currentPage = newPage;
    document.getElementById('page_' + currentPage).style.display = 'block';
    updatePanel();
    autoScrollPanelToCurrentPage(); // ADD THIS LINE
    saveSessionProgress();
    window.scrollTo(0, 0);
    // ... existing code ...
}
```

### Grid Layout CSS
```css
/* Source: CSS Grid specification */
#panelNumbers {
    display: grid;
    grid-template-columns: repeat(10, 1fr);
    gap: 0.5rem;
}

.question-badge {
    text-align: center;
    user-select: none; /* Mencegah text selection saat rapid click */
}

.question-badge:active {
    transform: scale(0.95); /* Visual feedback saat click */
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Page-only navigation panel | All-questions navigation panel | Phase 276 (in progress) | User bisa langsung jump ke sembarang soal tanpa klik next/prev berulang |
| Flexbox layout (flex-wrap) | CSS Grid 10-kolom | Phase 276 (in progress) | Layout lebih presisi, alignment otomatis, easier responsive |
| Manual scroll positioning | scrollIntoView() API | Phase 276 (in progress) | Cross-browser compatible, handles edge cases |

**Deprecated/outdated:**
- Tabel untuk layout: Digantikan CSS Grid (lebih flexible dan semantic)
- Manual scrollTop calculation: Digantikan scrollIntoView() (lebih reliable)

## Open Questions

1. **Auto-scroll panel behavior: Smooth atau instant?**
   - What we know: Decision D-09 minta auto-scroll untuk orientasi user
   - What's unclear: User tidak specify smooth vs instant untuk panel scroll
   - Recommendation: Gunakan `behavior: 'auto'` (instant) untuk konsisten dengan jump-to-question behavior. User request "immediate jump" untuk question navigation, sebaiknya konsisten untuk panel scroll juga

2. **Panel height limit di mobile?**
   - What we know: Mobile panel perlu collapsible (D-05), tapi tidak ada batas height yang disebutkan
   - What's unclear: Apakah panel perlu max-height dengan scroll, atau full height tapi collapseable
   - Recommendation: Set max-height 200px dengan `overflow-y: auto` di mobile. Ini memberi balance antara visibility dan screen real estate. User bisa scroll panel untuk lihat semua soal tanpa mengambil terlalu banyak space

3. **Handler untuk rapid click?**
   - What we know: User mungkin klik beberapa nomor soal berurutan dengan cepat
   - What's unclear: Apakah perlu debouncing atau rate limiting
   - Recommendation: Tidak perlu debouncing. `scrollIntoView()` dengan `behavior: 'auto'` adalah instant operation, tidak ada queuing effect. Rapid click akan langsung responsive

## Environment Availability

> Phase ini adalah pure frontend enhancement (HTML/CSS/JavaScript modification di existing file). Tidak ada external dependencies, tools, atau services yang diperlukan.

**Step 2.6: SKIPPED (no external dependencies - code-only change)**

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT di browser (sesuai project pattern) |
| Config file | None - manual testing di server development |
| Quick run command | `http://10.55.3.3/KPB-PortalHC/CMP/Assessment` → login worker → start exam |
| Full suite command | Complete flow: mulai exam → jawab soal → test navigasi panel → submit |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| D-01 | Panel menampilkan semua nomor soal (1-N) dalam grid layout | Visual | Manual: Start exam, cek panel shows all numbers | N/A |
| D-02 | Grid 10 kolom (soal 1-10 sebaris) | Visual | Manual: Cek alignment, harus 10 badges per row | N/A |
| D-03 | Warna badge: hijau (answered), abu-abu (unanswered) | Visual | Manual: Jawab soal, cek badge berubah hijau | N/A |
| D-04 | Klik nomor soal → jump ke lokasi soal (instant) | Interaction | Manual: Klik nomor di panel, verifikasi jump & scroll | N/A |
| D-04a | Klik soal di halaman berbeda → switch page + scroll | Interaction | Manual: Klik soal page 2 saat di page 1, verifikasi | N/A |
| D-05 | Desktop: panel visible. Mobile: panel collapsed | Responsive | Manual: Test di desktop (visible) dan mobile (hidden) | N/A |
| D-06 | Header text "Daftar Soal" | Visual | Manual: Cek header panel text | N/A |
| D-07 | Tidak ada penandaan khusus current question | Visual | Manual: Cek tidak ada border/bold khusus | N/A |
| D-08 | updatePanel() render ALL questions | Logic | Manual: Inspect DOM, pastikan semua badges ada | N/A |
| D-09 | Auto-scroll panel saat page change | Interaction | Manual: Ganti halaman, cek panel scroll otomatis | N/A |

### Sampling Rate
- **Per task commit:** Manual UAT di browser — test navigasi panel dengan beberapa soal
- **Per wave merge:** Full navigation flow test — test dengan 30+ soal, klik berbagai nomor, verify jump behavior
- **Phase gate:** Manual UAT complete — semua requirements di atas verified di server development

### Wave 0 Gaps
- **None** — Ini adalah pure UI enhancement, tidak ada test files atau framework yang dibutuhkan. Validation dilakukan via manual UAT di browser sesuai pattern project (Phase 265-268 semua manual UAT).

## Sources

### Primary (HIGH confidence)
- **Existing codebase analysis** — `Views/CMP/StartExam.cshtml` (lines 1-1057):
  - Question cards dengan `id="qcard_{qId}"` untuk scroll target
  - `pageQuestionIds` array mapping page index ke question IDs
  - `answeredQuestions` Set untuk tracking jawaban
  - Existing `updatePanel()` function yang perlu dimodifikasi
  - Existing `performPageSwitch()` function pattern
  - `.badge.bg-primary` elements untuk DisplayNumber extraction
  - Responsive layout dengan `col-lg-3` dan `@media (max-width: 991px)`

### Secondary (MEDIUM confidence)
- **CSS Grid specification** — W3C CSS Grid Layout Level 1:
  - `grid-template-columns: repeat(10, 1fr)` untuk 10-kolom equal-width layout
  - `gap` property untuk spacing antar grid items
  - Standard browser support (Chrome 57+, Firefox 52+, Safari 10.1+)

- **MDN Web Docs — Element.scrollIntoView()**:
  - API signature: `element.scrollIntoView(options)`
  - `{ behavior: 'auto' }` untuk instant scroll (no animation)
  - `{ block: 'start' }` untuk align element ke top viewport
  - Cross-browser compatible (semua modern browsers)

### Tertiary (LOW confidence)
- **Bootstrap 5 Documentation** — Badge components:
  - `.badge` base class
  - `.rounded-pill` untuk pill-shaped badges
  - `.bg-success` dan `.bg-secondary` untuk color variants
  - Note: Bootstrap patterns sudah well-established di project, confidence level upgrade ke HIGH

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - semua teknologi adalah native browser APIs atau existing project dependencies (Bootstrap 5)
- Architecture: HIGH - patterns diambil dari existing codebase dengan analisis langsung ke source files
- Pitfalls: HIGH - berdasarkan analisis existing code dan common frontend issues
- Implementation details: HIGH - semua examples diambil dari existing code patterns di StartExam.cshtml

**Research date:** 2026-04-01
**Valid until:** 30 days — frontend patterns dan CSS Grid API stable, tidak ada breaking changes yang diharapkan

---

**Phase Research Complete**
Total research time: ~15 minutes
Sources analyzed: 1 primary (existing codebase), 2 secondary (CSS Grid spec, MDN docs), 1 tertiary (Bootstrap docs)
Confidence level: HIGH — semua findings berbasis analisis langsung ke codebase dan well-documented web standards
