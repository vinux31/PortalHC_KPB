# Phase 307: Selected Participants Inline View - Research

**Researched:** 2026-04-29
**Domain:** Razor MVC view (single-file inline JS) — DOM helper extraction & real-time list rendering di wizard CreateAssessment.cshtml
**Confidence:** HIGH (semua line numbers, ID, IIFE boundary, dan reset call sites di-verifikasi langsung di file target)

## Summary

Phase 307 mengimplementasikan REQ WIZ-01 dengan menambahkan panel "Peserta Terpilih" real-time di Step 2 wizard `Views/Admin/CreateAssessment.cshtml` (Pilih Peserta), mirror format Step 4 summary panel (badge count + 5 nama pertama + tombol expand). Implementasi adalah **single-file edit** terhadap satu .cshtml view (markup baru sekitar 6-10 baris setelah line 309 + extract helper JS `renderSelectedParticipants()` dari logic existing `populateSummary` line 1062-1103 + extend `updateSelectedCount()` line 1436-1447 + refactor Proton IIFE inline duplicate line 1513-1519 + tambah panel-aware reset di line 1726-1728).

CONTEXT.md sangat detail (19 D-XX + 7 CD-XX) — research ini mengonfirmasi tiap line number, IIFE boundary, dan struktur ID Step 4 yang akan di-mirror, lalu menyoroti **3 gap actionable** yang membutuhkan resolusi planner: (1) `updateSelectedCount` lokal di IIFE main `WizardController` (line 831) tidak accessible dari Proton IIFE (line 1504) — D-18/CD-06 wajib pilih strategi hoisting, (2) reset handler di line 1726-1728 set `cb.checked=false` + manual `badge.textContent='0 terpilih'` **tanpa** memanggil `updateSelectedCount()` — D-17 piggyback strategi gagal di sini kalau tidak di-touch, (3) test E2E `tests/e2e/assessment.spec.ts:45` masih mengasumsikan English text `'2 selected'` (skew dari current Indonesian `'2 terpilih'`) — pre-existing rot, bukan blocker phase 307 tapi catat.

**Primary recommendation:** Pakai **Approach A (Hoist `updateSelectedCount` ke top-level scope di luar kedua IIFE)** untuk resolve CD-06 — risiko regresi minimum, eliminate Proton IIFE duplicate count logic, satu source of truth untuk panel + badge update. Implementasi helper sebagai inline named function tepat di atas `populateSummary` (line 1042 area), dengan signature literal `renderSelectedParticipants(targetEl, checkboxes, opts)` per success criteria #3 verbatim. Render pakai `DocumentFragment` + `setTimeout` debounce 100ms (idiom proven, sudah dipakai di `Views/Home/Guide.cshtml:739`).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Render panel "Peserta Terpilih" Step 2 | Browser / Client | — | Pure DOM update dari client-side checkbox state, tanpa round-trip server |
| Helper `renderSelectedParticipants()` extraction | Browser / Client | — | JS function di view inline, scope terbatas ke wizard CreateAssessment |
| Real-time event wiring (change listener) | Browser / Client | — | Event delegation di container DOM, no SignalR/AJAX |
| Initial state hydration dari `ViewBag.SelectedUserIds` | Frontend Server (Razor) | Browser / Client | Razor render checkbox `checked` attribute saat page load (line 300-302); JS `updateSelectedCount()` initial call (line 1451) sync panel state |
| Pre-existing data-source `#userCheckboxContainer` | Frontend Server (Razor) | — | Razor `@foreach ViewBag.Users` line 295-308 sudah render checkbox + label `<strong>FullName</strong>` |
| Filter visibility (search/section/filterSelectedOnly) | Browser / Client | — | DOM `style.display` toggle, panel sengaja **abaikan filter** (D-10) |
| Proton AJAX checkbox load | Frontend Server + Browser | — | Server return checkbox HTML, client mount ke `#protonUserCheckboxContainer`; event delegation handle dynamic |

**Catatan:** Phase 307 100% **client tier**. Tidak ada perubahan controller, model, ViewBag, route, EF, atau migrasi. Server hanya men-serve markup awal (sudah ada).

## Standard Stack

### Core (sudah ada di codebase, tidak install apa pun)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap | 5.3 | Class `badge bg-primary`, `btn-sm`, `btn-link`, `d-none`, `border rounded p-3 mt-3` | Stack project (STACK.md line 50) — semua class sudah dipakai di view target |
| Bootstrap Icons | (CDN via _Layout) | Optional ikon pada panel header (`bi-people` etc.) | Konsisten dengan Step 2 header (line 257 pakai `bi-people`) |
| Vanilla JavaScript (ES5+) | n/a | Helper, event delegation, DocumentFragment | Pattern existing CreateAssessment.cshtml (831-1865 inline JS pakai vanilla, no jQuery di Step 2 wiring) |

### Web APIs (browser native, no library needed)
| API | Purpose | Browser Support |
|-----|---------|----------------|
| `document.createDocumentFragment()` | Batch DOM build, single reflow saat insert | Universal (IE9+) — sudah dipakai di `Views/Home/Guide.cshtml:739` |
| `Element.replaceChildren(node)` | Atomic clear-and-fill container, no listener leak | Modern (Chrome 86+, Firefox 78+, Safari 14+) — sufficient untuk admin browser baseline |
| `setTimeout` / `clearTimeout` | Debounce render 100ms | Universal |
| `Array.from(NodeList)` | Convert query result ke Array untuk `.map()` | Universal (already dipakai di line 1056-1058) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `replaceChildren()` | `el.innerHTML = ''` + appendChild | innerHTML clear lebih lambat & destroy listeners; replaceChildren lebih ekspresif |
| `setTimeout` debounce | `requestAnimationFrame` | rAF browser-optimal tapi tidak guaranteed 100ms target; setTimeout deterministic untuk performance budget #4 |
| Vanilla querySelector | jQuery `$().filter(':checked')` | jQuery sudah loaded di Layout, tapi pattern Step 2 existing (line 1056, 1387) pakai vanilla — konsisten |
| Inline named function | IIFE module exposing `WizardParticipants.render` | Over-engineering untuk 2 caller; CONTEXT D-05 explicit inline named |

**Installation:** Tidak ada. Semua API native.

**Version verification:** Bootstrap 5.3 sudah tersemat di `_Layout.cshtml` per STACK.md (no version pin di-research lagi — sudah established sejak v11+). Tidak ada package baru.

## Architecture Patterns

### System Architecture Diagram

```
                    Page Load (DOMContentLoaded)
                              │
                              ▼
            ┌─────────────────────────────────┐
            │  Razor render (server-side)     │
            │  - #userCheckboxContainer       │
            │    @foreach ViewBag.Users       │
            │    cb @checked from SelectedIds │
            │  - #selectedCountBadge "0 terpilih"
            │  - <NEW> Panel container empty  │
            └────────────┬────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────────────┐
        │  IIFE WizardController init (line 831) │
        │  - updateSelectedCount() initial call  │
        │    └─ count badge update               │
        │    └─ <NEW> renderSelectedParticipants │
        │       (panel hydrate dari pre-checked) │
        └────────────────┬───────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │  User interaction    │
              └──┬────┬────┬────┬────┘
                 │    │    │    │
       ┌─────────┘    │    │    └─────────┐
       │              │    │              │
       ▼              ▼    ▼              ▼
  ┌─────────┐   ┌────────────┐   ┌─────────────┐
  │ Click 1 │   │ selectAll/ │   │ Proton mode │
  │ checkbox│   │ deselectAll│   │ AJAX load + │
  │ (change)│   │ (button)   │   │ check toggle│
  └────┬────┘   └─────┬──────┘   └──────┬──────┘
       │              │                  │
       │              ▼                  │
       │   manual call updateSelectedCount()
       │              │                  │
       └──────────────┼──────────────────┘
                      │
                      ▼
        ┌─────────────────────────────────┐
        │  updateSelectedCount() [hoisted]│
        │  1. isProton = catEl.value      │
        │  2. checkboxes = querySelector  │
        │     :checked dari container aktif│
        │  3. badge.textContent = N + ' terpilih'   ◄── IMMEDIATE
        │  4. scheduleRender(panel, checkboxes, opts)
        │     └─ debounce 100ms (clearTimeout/setTimeout)
        │        └─ renderSelectedParticipants(...)  ◄── DEBOUNCED
        │           ├─ frag = createDocumentFragment
        │           ├─ build badge + first5 + extra + button
        │           ├─ targetEl.replaceChildren(frag)
        │           └─ if opts.countBadgeEl: textContent
        └─────────────────────────────────┘
                      │
                      ▼
            ┌─────────────────────┐
            │  DOM panel updated  │
            │  count badge updated│
            └─────────────────────┘

  Step 4 (separate caller path):
  goToStep(4) → populateSummary() → renderSelectedParticipants(
                                       targetEl=#summary-peserta-list-container,
                                       checkboxes=...,
                                       opts={ countBadgeEl: #summary-peserta-count, ... })
```

### Component Responsibilities

| Component | File | Lines | Phase 307 Action |
|-----------|------|-------|------------------|
| Step 2 markup (filter bar + container) | Views/Admin/CreateAssessment.cshtml | 253-318 | INSERT panel container after line 309 (sebelum `#protonEligibleSection` line 311) |
| `#selectedCountBadge` (filter bar) | Views/Admin/CreateAssessment.cshtml | 289 | NO CHANGE (D-04 stability) |
| `#userCheckboxContainer` checkbox list | Views/Admin/CreateAssessment.cshtml | 294-309 | NO CHANGE |
| `#protonEligibleSection` + container | Views/Admin/CreateAssessment.cshtml | 311-318 | NO CHANGE |
| Step 4 summary peserta card | Views/Admin/CreateAssessment.cshtml | 616-630 | NO CHANGE markup; ID `summary-peserta-list/expand/extra/count` di-reuse oleh helper |
| `populateSummary()` | Views/Admin/CreateAssessment.cshtml | 1042-1103 | REFACTOR line 1061-1103 → call helper baru |
| `applyFilters()` | Views/Admin/CreateAssessment.cshtml | 1382-1402 | NO CHANGE (panel abaikan filter, D-10) |
| `selectAllBtn` / `deselectAllBtn` | Views/Admin/CreateAssessment.cshtml | 1417-1433 | NO CHANGE (sudah call updateSelectedCount manual) |
| `updateSelectedCount()` | Views/Admin/CreateAssessment.cshtml | 1436-1447 | EXTEND: tambah call helper untuk panel render (D-17); kemungkinan HOIST ke top-level (CD-06) |
| `userContainer.addEventListener('change', ...)` | Views/Admin/CreateAssessment.cshtml | 1449-1450 | NO CHANGE (delegated listener sudah benar) |
| Initial call `updateSelectedCount()` | Views/Admin/CreateAssessment.cshtml | 1451 | NO CHANGE (D-12 hydrate panel ride-along) |
| Main IIFE `WizardController` close | Views/Admin/CreateAssessment.cshtml | 1501 | DEPENDS on CD-06 — kalau hoist `updateSelectedCount`, function di-pindah ke luar IIFE |
| Proton IIFE inline change listener | Views/Admin/CreateAssessment.cshtml | 1513-1520 | REPLACE: panggil `updateSelectedCount` (D-18) |
| Reset handler "Buat lagi" modal | Views/Admin/CreateAssessment.cshtml | 1721-1750 | EDIT line 1726-1728: ganti manual `badge.textContent='0 terpilih'` dengan call `updateSelectedCount()` agar panel auto-refresh ke "Belum ada peserta dipilih" |

### Recommended Project Structure

```
Views/Admin/CreateAssessment.cshtml   # Single touch point — markup + JS
                                      # No new files, no wwwroot/js/ extract
                                      # (per Phase 304 D-09, Phase 305 D-03 rationale)
```

Tidak ada folder baru. Tidak ada partial view baru. Tidak ada helper C# baru.

### Pattern 1: Inline named helper di JS block utama
**What:** Function `renderSelectedParticipants` ditulis sebagai named function declaration tepat di atas (atau dalam scope yang sama dengan) `populateSummary`, sehingga keduanya bisa saling panggil tanpa hoisting concern.

**When to use:** 2-3 caller, scope terbatas ke satu view, tidak ada test unit terpisah dijadwalkan. Phase 304 D-09 dan Phase 305 D-03 sudah men-establish threshold ini (n≥4 callers baru extract ke `wwwroot/js/`).

**Example (signature literal per success criteria #3):**
```javascript
// Source: Phase 307 D-05/D-06 + verifikasi pola existing populateSummary
function renderSelectedParticipants(targetEl, checkboxes, opts) {
    opts = opts || {};
    var maxInline = opts.maxInline || 5;
    var emptyText = opts.emptyText || 'Belum ada peserta dipilih';
    var listIdPrefix = opts.idPrefix || 'selected-participants';

    // Extract names — mirror existing populateSummary line 1061-1068 logic 1:1
    var names = (checkboxes || []).map(function(cb) {
        var label = document.querySelector('label[for="' + cb.id + '"]');
        if (label) {
            var strong = label.querySelector('strong');
            return strong ? strong.textContent : label.textContent.trim();
        }
        return cb.value;
    });

    // Build via DocumentFragment (Phase 307 D-19 + verified pattern di Guide.cshtml:739)
    var frag = document.createDocumentFragment();

    if (names.length === 0) {
        var empty = document.createElement('span');
        empty.className = 'text-muted fst-italic';
        empty.textContent = emptyText;
        frag.appendChild(empty);
    } else {
        var first = names.slice(0, maxInline);
        var listSpan = document.createElement('span');
        listSpan.id = listIdPrefix + '-list';
        listSpan.textContent = first.join(', ');
        frag.appendChild(listSpan);

        if (names.length > maxInline) {
            var remaining = names.slice(maxInline);
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.id = listIdPrefix + '-expand';
            btn.className = 'btn btn-link btn-sm p-0 ms-1';   // mirror Step 4 line 627
            btn.textContent = '...dan ' + remaining.length + ' lainnya';

            var extra = document.createElement('span');
            extra.id = listIdPrefix + '-extra';
            extra.className = 'd-none';

            btn.onclick = function() {
                if (extra.classList.contains('d-none')) {
                    extra.textContent = ', ' + remaining.join(', ');
                    extra.classList.remove('d-none');
                    btn.textContent = 'Sembunyikan';
                } else {
                    extra.classList.add('d-none');
                    btn.textContent = '...dan ' + remaining.length + ' lainnya';
                }
            };

            frag.appendChild(btn);
            frag.appendChild(extra);
        }
    }

    if (targetEl) targetEl.replaceChildren(frag);

    // Optional badge update (Step 4 caller pakai opts.countBadgeEl)
    if (opts.countBadgeEl) {
        opts.countBadgeEl.textContent = names.length + ' peserta';
    }
}
```

### Pattern 2: Debounce wrapper (selective: count immediate, panel debounced)
**What:** Helper render dipanggil via debounce 100ms; count badge update tetap immediate di body `updateSelectedCount`.

**When to use:** Phase 307 D-11 — bulk operations seperti `selectAllBtn` (50+ checkbox toggle in tight loop) idealnya batch render single-call; single click idealnya count badge instant feedback.

**Example:**
```javascript
// Source: Phase 307 D-11 + D-19 (CD-04 picks setTimeout over rAF)
var __renderTimer = null;
function scheduleRenderSelectedPanel(targetEl, checkboxes, opts) {
    if (__renderTimer) clearTimeout(__renderTimer);
    __renderTimer = setTimeout(function() {
        renderSelectedParticipants(targetEl, checkboxes, opts);
        __renderTimer = null;
    }, 100);
}

function updateSelectedCount() {
    var catEl = document.getElementById('Category');
    var isProton = catEl && catEl.value === 'Assessment Proton';
    var sel = isProton
        ? '#protonUserCheckboxContainer .user-checkbox:checked'
        : '#userCheckboxContainer .user-checkbox:checked';
    var checkboxes = Array.from(document.querySelectorAll(sel));

    // Immediate count badge (cheap)
    var badge = document.getElementById('selectedCountBadge');
    if (badge) badge.textContent = checkboxes.length + ' terpilih';

    // Debounced panel render (expensive when bulk)
    var panel = document.getElementById('selected-participants-panel');
    if (panel) scheduleRenderSelectedPanel(panel, checkboxes, {
        idPrefix: 'selected-participants',
        emptyText: 'Belum ada peserta dipilih'
    });
}
```

### Pattern 3: Top-level hoist untuk cross-IIFE accessibility (CD-06 resolution)
**What:** Karena Proton IIFE (line 1504) tidak bisa akses `updateSelectedCount` yang lokal di Main IIFE (line 831), pindahkan declaration ke top-level scope `<script>` block (di luar kedua IIFE).

**When to use:** CD-06. Alternatif (`window.updateSelectedCount = ...` exposure dari main IIFE) tidak konsisten dengan pattern existing. Hoist top-level lebih clean dan eliminate the need untuk window assignment.

**Example:**
```html
<script>
    // Top-level (di antara line 1501 dan 1504, atau di awal sebelum line 831)
    function renderSelectedParticipants(targetEl, checkboxes, opts) { /* ... */ }
    var __renderTimer = null;
    function scheduleRenderSelectedPanel(...) { /* ... */ }
    function updateSelectedCount() { /* ... */ }

    (function WizardController() {
        // line 831-1501 existing — sekarang panggil updateSelectedCount() saja
        // tidak ada redeclare
        // ...
        if (selectAllBtn) selectAllBtn.addEventListener('click', function() {
            // ... existing
            updateSelectedCount();  // ← unchanged, panggil top-level
        });
        // ...
    })();

    (function() {
        // Proton IIFE line 1504-1581 — sekarang juga bisa panggil
        if (protonCbContainer) {
            protonCbContainer.addEventListener('change', updateSelectedCount);  // ← clean
        }
        // ...
    })();
</script>
```

**Risiko regresi:** Sangat rendah karena (a) main IIFE tidak punya state lokal yang dipakai `updateSelectedCount` (function hanya baca DOM), (b) call sites lainnya (line 1423, 1432, 1450, 1451) memanggil dengan nama bareword — sama-sama resolve ke top-level kalau tidak ada local rebinding. Pengecekan: pastikan tidak ada `var updateSelectedCount = ...` lain di main IIFE (sudah verified — hanya satu declaration di line 1436).

### Anti-Patterns to Avoid
- **Innerhtml string concat untuk render:** Berisiko XSS jika nama peserta mengandung HTML (data dari `ApplicationUser.FullName`, sumber AD/LDAP — tidak escape). DocumentFragment + `textContent` aman by default. Existing populateSummary line 1090 pakai `extraEl.textContent = ', ' + remaining.join(', ')` — sudah aman; helper Phase 307 wajib pertahankan disiplin ini.
- **`onclick` di atribut HTML markup:** Pakai `addEventListener` atau direct property assignment (`btn.onclick = fn`) sesuai pattern existing line 1087. Inline onclick di markup susah dibersihkan saat re-render.
- **Per-checkbox change listener:** Fragile saat Proton AJAX load checkbox baru (line 1570 area mount HTML dynamic). Event delegation di container = pattern existing line 1450, harus dipertahankan untuk container Proton (D-09).
- **Filter-aware panel:** D-10 explicit. Kalau panel cuma show "visible filtered" akan bingung user (pilih 20 → filter section → tiba-tiba 5).
- **Hide panel saat 0 dipilih:** D-02 explicit. Layout shift jelek + tidak konsisten dengan badge `#selectedCountBadge` yang selalu visible.
- **Touch existing IDs:** `selectedCountBadge`, `summary-peserta-*` adalah consumer JS contract (line 1070-1075, 1517 reference langsung). Phase 304 D-18 stability principle absolute. Panel baru pakai prefix `selected-participants-*`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Batch DOM build | Manual `appendChild` chain ke targetEl | `document.createDocumentFragment()` | Single reflow vs N reflows; pattern sudah di codebase (`Guide.cshtml:739`) |
| Atomic clear-and-fill container | `el.innerHTML = ''` + loop appendChild | `el.replaceChildren(fragment)` | Built-in atomic, no listener leak risk, lebih ekspresif |
| HTML escaping nama peserta | Manual `.replace(/&/g,'&amp;')...` | `node.textContent = name` (DOM API) | Aman by default; `textContent` tidak parse HTML |
| Debounce | Custom queue / promise / generator | `setTimeout` + `clearTimeout` pair | Universal idiom, deterministic 100ms target |
| Cross-IIFE function sharing | `window.updateSelectedCount = ...` global pollution | Hoist function ke top-level `<script>` scope | Konsisten dengan existing top-level fn pattern di view (line 1768 `checkPreTestWarning` adalah top-level); avoid global namespace pollution |
| Iterate :checked checkbox | Manual loop `for` over `.user-checkbox` | `Array.from(document.querySelectorAll('...:checked'))` | Pattern existing line 1056-1058 + 1387 |
| First-5 truncate + "...dan N lainnya" | Stateful pagination component | Inline slice + button (mirror Step 4 line 1078-1098) | Helper extract dari existing logic — DRY ke parity |

**Key insight:** Domain ini adalah **vanilla DOM manipulation di view inline** — semua "library" yang dibutuhkan adalah Web Platform API. Bahkan Bootstrap class set yang dipakai sudah ter-inventaris di Step 4 markup (line 620, 627, 628). Phase 307 secara harfiah adalah **extract-and-reuse**, bukan build-from-scratch.

## Common Pitfalls

### Pitfall 1: `updateSelectedCount` tidak terjangkau dari Proton IIFE
**What goes wrong:** `(function() { protonCbContainer.addEventListener('change', updateSelectedCount); })();` di line 1504 IIFE akan throw `ReferenceError: updateSelectedCount is not defined` — function hanya hidup di scope `WizardController` IIFE (line 831-1501).
**Why it happens:** IIFE encapsulation. CONTEXT D-18 menyebut "perlu pastikan accessible dari Proton IIFE" tapi belum resolve.
**How to avoid:** Pilih Pattern 3 (top-level hoist). Verifikasi tidak ada local `var updateSelectedCount` lain (sudah verified — hanya line 1436).
**Warning signs:** Buka DevTools Console saat halaman load CreateAssessment dengan Category "Assessment Proton" → kalau muncul ReferenceError saat user toggle Proton checkbox, gap ini belum di-resolve.

### Pitfall 2: Reset handler line 1726-1728 tidak panggil `updateSelectedCount`
**What goes wrong:** User klik "Buat lagi" di modal sukses → reset handler set `cb.checked = false` untuk semua checkbox, lalu manual set `badge.textContent = '0 terpilih'` — tetapi panel **tetap menampilkan list nama lama** karena helper render tidak ter-trigger.
**Why it happens:** Existing reset code DUPLICATES count badge update logic instead of calling `updateSelectedCount()`. Phase 307 D-17 piggyback strategi mengasumsikan reset call site sudah lewati `updateSelectedCount()` — TIDAK BENAR.
**How to avoid:** Plan task wajib include EDIT line 1726-1728: ganti `var badge = document.getElementById('selectedCountBadge'); if (badge) badge.textContent = '0 terpilih';` dengan single call `updateSelectedCount();` (yang sekarang juga refresh panel via D-17).
**Warning signs:** Manual UAT scenario — buat assessment dengan 3 peserta → submit sukses → klik "Buat lagi" → cek apakah panel "Peserta Terpilih" reset ke "Belum ada peserta dipilih". Kalau masih show 3 nama, gap belum di-resolve.

### Pitfall 3: Initial render race dengan Proton AJAX load
**What goes wrong:** Mode edit assessment dengan `Category == 'Assessment Proton'`: panel render saat DOMContentLoaded ambil dari `#userCheckboxContainer` (kosong di Proton mode), lalu Proton AJAX load checkbox dari `protonTrackSelect` change handler (line 1522+) — panel TIDAK refresh karena AJAX load tidak fire `change` event di kontainer.
**Why it happens:** AJAX response inject HTML innerHTML — checkbox baru tidak fire change saat ditambah; baru fire saat user toggle.
**How to avoid:** Setelah AJAX inject (sekitar line 1570 markup gen) **eksplisit panggil** `updateSelectedCount()` agar panel hydrate dengan checkbox pre-checked kalau ada. CONTEXT belum membahas explicit ini — flag untuk planner sebagai task verification.
**Warning signs:** Mode edit Proton assessment (kalau ada flow edit) — panel kosong padahal Proton track punya checkbox checked.

### Pitfall 4: XSS via FullName injection
**What goes wrong:** Kalau `ApplicationUser.FullName` mengandung `<script>` atau `<img onerror>` (sumber AD/LDAP), `innerHTML` assignment akan execute script.
**Why it happens:** AD/LDAP umumnya tidak strict-validate display name; mungkin dari import legacy berisi karakter HTML.
**How to avoid:** Pakai `textContent` (bukan `innerHTML`) di seluruh helper. Pattern existing line 1079, 1086, 1090 sudah pakai `textContent` — pertahankan disiplin ini.
**Warning signs:** Karakter `<` `>` `&` di nama peserta tampil escaped di panel — itu correct behavior, jangan diubah ke innerHTML.

### Pitfall 5: Listener leak saat re-render
**What goes wrong:** Setiap render membuat tombol expand baru dengan `btn.onclick = fn`. Kalau pakai `addEventListener('click', fn)` dengan inline closure, listener tertinggal di GC graph (kecuali targetEl di-replaceChildren).
**Why it happens:** `replaceChildren` mendisconnect node lama dari DOM — listener pada node lama akan di-GC, tapi closure capture variable luar bisa ber-prolong lifetime.
**How to avoid:** Pakai `btn.onclick = ...` (property assignment) — single slot, overwrite. Existing populateSummary line 1087 sudah pattern ini. Helper Phase 307 mirror.
**Warning signs:** Memory profiling DevTools — instantiate panel berulang kali, cek detached DOM node count tidak grow.

### Pitfall 6: aria-live polite di container yang re-render replaceChildren
**What goes wrong:** Screen reader mungkin tidak announce perubahan kalau `aria-live="polite"` ada di container yang seluruh content-nya di-replace (some screen readers butuh node yang stable).
**Why it happens:** ARIA live region behavior bervariasi antar AT (screen reader) — replaceChildren bisa di-treat sebagai full DOM replacement, tidak detect sebagai "change".
**How to avoid:** CD-07 keputusan planner. Best practice: aria-live="polite" di **container parent yang stable** (panel `<div>`), bukan di `<span>` yang di-replace. Atau pakai pola separate `role="status"` div untuk count text.
**Warning signs:** Test dengan NVDA/JAWS — toggle checkbox tidak diumumkan.

## Code Examples

Verified patterns (verbatim atau adapt dari source langsung):

### Existing Step 4 markup (line 616-630, ID prefix `summary-peserta-*`)
```html
<!-- Source: Views/Admin/CreateAssessment.cshtml line 616-630 (verified 2026-04-29) -->
<div class="card mb-3">
    <div class="card-header d-flex align-items-center">
        <strong>Peserta</strong>
        <span class="badge bg-secondary ms-2" id="summary-peserta-count"></span>
        <button type="button" class="btn btn-sm btn-outline-secondary ms-auto edit-from-confirm" data-step="2">
            <i class="bi bi-pencil me-1"></i>Edit
        </button>
    </div>
    <div class="card-body">
        <span id="summary-peserta-list"></span>
        <button type="button" id="summary-peserta-expand" class="btn btn-link btn-sm p-0 d-none ms-1"></button>
        <span id="summary-peserta-extra" class="d-none"></span>
    </div>
</div>
```

### Phase 307 panel markup (mirror prefix `selected-participants-*`, recommended)
```html
<!-- INSERT after line 309 (end of #userCheckboxContainer), before #protonEligibleSection (line 311) -->
<!-- Phase 307 — D-01, D-02, D-13, CD-01, CD-05, CD-07 -->
<div class="border rounded p-3 mt-3" id="selected-participants-panel-wrapper">
    <div class="d-flex align-items-center mb-2">
        <strong><i class="bi bi-people-fill me-1 text-primary"></i>Peserta Terpilih</strong>
        <span class="badge bg-primary ms-2" id="selected-participants-count">0 peserta</span>
    </div>
    <div id="selected-participants-panel" role="status" aria-live="polite" aria-atomic="false">
        <span class="text-muted fst-italic">Belum ada peserta dipilih</span>
    </div>
</div>
```

(Catatan: helper akan replace `#selected-participants-panel` content; `#selected-participants-count` di-update terpisah oleh `updateSelectedCount` agar count immediate, panel debounced. Atau planner bisa pilih helper update keduanya via opts.countBadgeEl — D-07 left flexible.)

### Existing populateSummary refactor (D-15 — replace line 1061-1103 dengan call helper)
```javascript
// BEFORE — Views/Admin/CreateAssessment.cshtml line 1061-1103 (verified 2026-04-29)
var names = checkboxes.map(function(cb) { /* ... */ });
var countBadge = document.getElementById('summary-peserta-count');
if (countBadge) countBadge.textContent = names.length + ' peserta';
var listEl = document.getElementById('summary-peserta-list');
var expandBtn = document.getElementById('summary-peserta-expand');
var extraEl = document.getElementById('summary-peserta-extra');
if (listEl) {
    var first5 = names.slice(0, 5);
    listEl.textContent = first5.join(', ');
}
if (names.length > 5) {
    var remaining = names.slice(5);
    if (expandBtn) {
        expandBtn.classList.remove('d-none');
        expandBtn.textContent = '...dan ' + remaining.length + ' lainnya';
        expandBtn.onclick = function() { /* ... */ };
    }
} else {
    if (expandBtn) expandBtn.classList.add('d-none');
    if (extraEl) extraEl.classList.add('d-none');
}

// AFTER — Phase 307 (single call, helper does everything; mirror identical DOM)
// NOTE: Step 4 punya 3 separate elemen (list, expand button, extra) — helper harus
// support "render-into-existing-elements" mode untuk Step 4, bukan replaceChildren container.
// Two design options:
//   (A) Helper SOLELY does container replaceChildren — Step 4 markup HARUS direvisi
//       jadi single container `<div id="summary-peserta-list-container">` (touch markup line 626-628)
//   (B) Helper detect mode: kalau opts has explicit list/expand/extra IDs, render ke
//       elemen-elemen yang sudah ada (Step 4 backward-compat); kalau tidak,
//       replaceChildren container (Step 2 baru)
// Recommendation: (A) — touch markup minimal (3 baris jadi 1), helper sederhana, parity
// guaranteed. Phase 304 D-18 stability tetap terpenuhi karena ID `summary-peserta-count`
// di header card tidak diubah; hanya body card di-konsolidasi.
populateSummary() {
    // ... (Category, Title, isProton resolve sama seperti existing line 1043-1058)
    var checkboxes = isProton
        ? Array.from(document.querySelectorAll('#protonUserCheckboxContainer .user-checkbox:checked'))
        : Array.from(document.querySelectorAll('#userCheckboxContainer .user-checkbox:checked'));

    renderSelectedParticipants(
        document.getElementById('summary-peserta-list-container'),  // assume markup direfactor (option A)
        checkboxes,
        {
            countBadgeEl: document.getElementById('summary-peserta-count'),
            idPrefix: 'summary-peserta',
            emptyText: 'Belum ada peserta dipilih',
            maxInline: 5
        }
    );
    // ... (rest of populateSummary unchanged: type badge, settings, etc.)
}
```

(Planner pilih A vs B — recommendation A untuk simplicity. Sub-decision tracked sebagai assumption A1 di bawah.)

### Reset handler edit (line 1726-1728)
```javascript
// BEFORE — Views/Admin/CreateAssessment.cshtml line 1726-1728 (verified 2026-04-29)
document.querySelectorAll('.user-checkbox').forEach(function(cb) { cb.checked = false; });
var badge = document.getElementById('selectedCountBadge');
if (badge) badge.textContent = '0 terpilih';

// AFTER — Phase 307 D-17 piggyback (single source of truth)
document.querySelectorAll('.user-checkbox').forEach(function(cb) { cb.checked = false; });
updateSelectedCount();   // updates badge AND panel
```

### Proton IIFE refactor (line 1513-1520)
```javascript
// BEFORE — Views/Admin/CreateAssessment.cshtml line 1513-1520 (verified 2026-04-29)
if (protonCbContainer) {
    protonCbContainer.addEventListener('change', function() {
        var count = protonCbContainer.querySelectorAll('.user-checkbox:checked').length;
        var badge = document.getElementById('selectedCountBadge');
        if (badge) badge.textContent = count + ' terpilih';
    });
}

// AFTER — Phase 307 D-18 (assumes updateSelectedCount hoisted top-level per CD-06)
if (protonCbContainer) {
    protonCbContainer.addEventListener('change', updateSelectedCount);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `el.innerHTML = ''` clear container | `el.replaceChildren(fragment)` | Web standard 2020+ (Chrome 86, FF 78, Safari 14) | Atomic, ekspresif, no manual loop — sudah viable untuk admin browser baseline |
| jQuery `$(el).empty().append(...)` | Native DOM `replaceChildren` + DocumentFragment | Bootstrap 5 dropped jQuery dependency (2021) | Konsisten dengan trend codebase Phase 304/305/306 yang vanilla untuk Step 2/3 wiring |
| Per-element event listener | Event delegation di container parent | Always best practice | Pattern existing line 1450 sudah benar — Phase 307 extend ke container Proton |

**Deprecated/outdated:**
- Tidak ada deprecation yang relevan untuk scope phase ini.
- Bootstrap 4 helper class sudah tidak relevan (Bootstrap 5.3 di project, semua class baru) — `bg-primary`, `btn-link`, `d-none`, `border rounded` sudah BS5 idiom.

## Project Constraints (from CLAUDE.md)

CLAUDE.md project hanya berisi **1 directive aktif**:

| # | Directive | Source |
|---|-----------|--------|
| C-01 | **Selalu respon dalam Bahasa Indonesia.** Semua user-facing text (panel header, empty state, button, error, comment kalau perlu) dalam Bahasa Indonesia. | `./CLAUDE.md` line 3 |

**Verifikasi compliance Phase 307:**
- Panel header: "Peserta Terpilih" ✓
- Empty state: "Belum ada peserta dipilih" ✓ (D-16)
- Expand button: "...dan N lainnya" / "Sembunyikan" ✓ (mirror existing line 1086, 1092, 1095)
- Count format: "N peserta" / "N terpilih" ✓ (existing format)
- Code comment: optional Bahasa Indonesia (existing JS pakai mixed; hybrid OK per Phase 305 D-11 precedent).

## Validation Architecture

`workflow.nyquist_validation: true` di `.planning/config.json` — section ini wajib.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright 1.58.2 (E2E only — no .NET unit test project di repo per inspeksi `tests/`) |
| Config file | `tests/playwright.config.ts` |
| Quick run command | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "selected"` (kalau test baru ditambah dengan tag/grep "selected") |
| Full suite command | `cd tests && npx playwright test` |

**Catatan critical:** TIDAK ADA framework unit test (xUnit/NUnit/MSTest) untuk JS. Tidak ada Jest/Vitest. Test untuk JS helper hanya bisa melalui:
1. **E2E Playwright** — full browser, real DOM, slower, requires app running
2. **Manual UAT browser** — fastest feedback, mandatory untuk Phase 307 success criteria #4 (performance budget < 200ms instrumented dengan `performance.now()`)
3. **Console log instrumentation** — temp logging di JS, removable post-UAT

### Phase Requirements → Test Map
| Req ID | Behavior (success criteria) | Test Type | Automated Command | File Exists? |
|--------|-----------------------------|-----------|-------------------|--------------|
| WIZ-01 #1 | Panel "Peserta Terpilih" muncul setelah userCheckboxContainer dengan badge + 5 nama + tombol expand | E2E (selector + text assertion) | `npx playwright test e2e/assessment.spec.ts --grep "Step 2 panel"` | ❌ Wave 0 (test belum ada) |
| WIZ-01 #2 | Real-time update saat checkbox toggle (event delegation) | E2E | same as #1 with `.click()` + `expect().toContainText()` | ❌ Wave 0 |
| WIZ-01 #3 | DRY: helper extract → Step 2 & Step 4 produce identical DOM untuk same input | E2E parity check (compare `innerHTML` of two containers) | `npx playwright test --grep "parity"` | ❌ Wave 0 |
| WIZ-01 #4 | 50+ peserta render < 200ms (DocumentFragment + debounce) | Manual UAT + `performance.now()` instrumentation | (manual) | ❌ Wave 0 |
| WIZ-01 #5 | Step 2 list = Step 4 summary list (no divergence) | E2E parity check (same as #3) | same as #3 | ❌ Wave 0 |

**Existing test status (`tests/e2e/assessment.spec.ts:45`):**
```typescript
await expect(page.locator('#selectedCountBadge')).toContainText('2 selected');
```
Skew: text aktual sekarang `'2 terpilih'` (Indonesian). Ini **pre-existing rot**, bukan blocker phase 307. Catat sebagai cleanup item — kalau planner sentuh test untuk add scenario Phase 307, sekalian fix line 45 ke `'2 terpilih'`.

### Sampling Rate
- **Per task commit:** Manual smoke browser (load CreateAssessment, toggle 2 checkbox, verify panel update). E2E run optional kalau test baru sudah ditulis.
- **Per wave merge:** Full E2E suite `cd tests && npx playwright test` — verify tidak ada regresi di assessment.spec.ts existing.
- **Phase gate:** Manual UAT 5-step (load page edit mode pre-checked → toggle add 3 → toggle remove 2 → selectAll → "Buat lagi" reset → submit Proton mode) sebelum `/gsd-verify-work`. Performance budget UAT dengan 50+ peserta wajib log `performance.now()` delta.

### Wave 0 Gaps
- [ ] `tests/e2e/assessment.spec.ts` — extend dengan describe block "Phase 307 Selected Participants Panel" covering 5 success criteria
- [ ] `tests/e2e/helpers/wizardSelectors.ts` (jika belum ada) — selector constants `#selected-participants-panel`, `#selected-participants-count`, `#selected-participants-list`, `#selected-participants-expand`, `#selected-participants-extra`
- [ ] Perbaikan pre-existing rot line 45 `'2 selected'` → `'2 terpilih'` (opportunistic fix saat menyentuh file)
- [ ] Manual UAT script Bahasa Indonesia di `.planning/phases/307-.../UAT.md` (mengikuti pattern Phase 306 yang punya 10-step manual UAT)

(Tidak ada framework install needed — Playwright sudah ter-set di `tests/package.json`.)

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Pendekatan A (refactor markup Step 4 line 626-628 jadi single container `<div id="summary-peserta-list-container">`) lebih simple daripada helper bermode dual; tidak break consumer JS lain | Code Examples — populateSummary refactor | Low — tidak ada consumer eksternal yang reference `summary-peserta-list/expand/extra` selain populateSummary itu sendiri (verified via Grep `summary-peserta` di view: hanya 5 occurrences, semua di markup line 620-628 + populateSummary line 1070-1075). Phase 304 D-18 stability principle terjaga karena `summary-peserta-count` (consumer-facing badge) tidak diubah. |
| A2 | Hoist `updateSelectedCount` ke top-level (Pattern 3) tidak break call sites lain karena tidak ada local rebinding di main IIFE | Pattern 3 — Top-level hoist | Low — verified via Grep: hanya satu declaration `function updateSelectedCount()` di line 1436. Call sites (line 1423, 1432, 1450, 1451) panggil bareword yang resolve ke nearest scope; setelah hoist, semua resolve ke top-level. |
| A3 | `Element.replaceChildren()` cukup didukung di browser baseline yang admin pakai (asumsi modern Chrome/Edge/Firefox di lingkungan kantor Pertamina) | Standard Stack — Web APIs | Medium — kalau admin browser legacy (IE11 atau Chrome <86), butuh polyfill atau fallback `el.innerHTML = ''; el.appendChild(fragment)`. CONTEXT tidak menyebut browser baseline. **Recommendation: planner verify atau pakai fallback pattern conservative.** |
| A4 | Performance budget 200ms untuk 50 peserta achievable dengan DocumentFragment + debounce 100ms tanpa optimasi tambahan | Validation Architecture #4 | Low — 50 nodes adalah trivial untuk modern browser. Bottleneck realistic adalah kalau querySelectorAll iterate ribuan checkbox. Untuk 50 peserta, expected < 10ms render. UAT instrumentation akan konfirmasi. |
| A5 | aria-live="polite" di container panel adalah pilihan yang aman untuk screen reader (CD-07) | Code Examples — Phase 307 panel markup | Low — pattern sudah dipakai di codebase (Login.cshtml line 151, Guide.cshtml line 70/151) tanpa report issue. Risiko Pitfall 6 ada tapi minor. |

**Tidak ada claim `[ASSUMED]` lain yang material.** Semua claim utama (line numbers, IDs, IIFE boundary, reset call site behavior, Step 4 markup structure, DocumentFragment precedent) di-VERIFY langsung via Read/Grep tool dalam sesi research ini.

## Open Questions

1. **Performance budget verification methodology — instrumentation di production atau dev only?**
   - What we know: Success criteria #4 explicit "50+ peserta render < 200ms (DocumentFragment + debounce 100ms)". UAT manual butuh `performance.now()` measurement.
   - What's unclear: Apakah instrumentation `console.log('[Phase307] render took', delta, 'ms')` boleh tinggal di production code atau wajib di-strip pre-merge?
   - Recommendation: Tambah instrumentasi via `if (window.location.hostname === 'localhost' || ...)` guard, atau strip pre-merge (planner decide). Best practice = ambient logger gated by debug flag.

2. **Browser baseline — apakah perlu polyfill untuk `Element.replaceChildren()`?**
   - What we know: Admin Pertamina kemungkinan modern Edge/Chrome (Windows 11 corporate baseline lazim).
   - What's unclear: Apakah ada user yang masih pakai IE11 atau Chrome legacy < 86?
   - Recommendation: Plan tambah fallback `if (typeof targetEl.replaceChildren === 'function') { targetEl.replaceChildren(frag); } else { targetEl.innerHTML = ''; targetEl.appendChild(frag); }` — defensive, no risk.

3. **AJAX Proton load — initial panel hydrate setelah inject HTML?**
   - What we know: `protonTrackSelect` change handler load checkbox via AJAX (line 1522+, mount markup line 1570 area).
   - What's unclear: Apakah AJAX response yang inject HTML termasuk pre-checked checkbox (dari edit mode atau saved state)?
   - Recommendation: Pitfall 3 — plan task explicit verify dengan call `updateSelectedCount()` di akhir AJAX success handler. Defer detail implementasi ke planner setelah baca line 1522+ lengkap.

4. **CD-05 panel header layout — label "Peserta Terpilih" di atas atau inline dengan badge?**
   - What we know: CONTEXT CD-05 leaves to planner discretion.
   - What's unclear: Step 4 pattern (line 618-624) pakai `<div class="card-header d-flex">` dengan `<strong>` + badge inline + Edit button auto-end. Step 2 panel tidak butuh Edit button.
   - Recommendation: Mirror pattern card-header tanpa Edit button — `<div class="d-flex align-items-center mb-2"><strong>Peserta Terpilih</strong> <span class="badge bg-primary ms-2">N peserta</span></div>` (lihat code example markup di atas).

## Environment Availability

Phase 307 adalah pure code edit ke single .cshtml file. Tidak ada dependensi tool/runtime baru.

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| .NET 8 SDK | Build & run app saat manual UAT | ✓ (assumed — milestone v15.0 sedang berjalan, build passing per Phase 306 close) | 8.0 LTS | — |
| Browser modern (Chrome/Edge/Firefox) | Manual UAT | ✓ (assumed Pertamina corporate baseline) | Modern (Chrome 86+ baseline target untuk replaceChildren) | Polyfill `innerHTML = ''; appendChild(frag)` |
| Playwright 1.58.2 | E2E test extension Wave 0 | ✓ (verified via `tests/package.json`) | 1.58.2 | — |
| Node.js | Playwright runtime | ✓ (assumed — Playwright sudah dipakai sejak v14.0) | (project unspecified, latest LTS works) | — |

**Missing dependencies with no fallback:** Tidak ada.
**Missing dependencies with fallback:** `Element.replaceChildren` di legacy browser — fallback `innerHTML = ''; appendChild(frag)` aman.

## Sources

### Primary (HIGH confidence)
- `Views/Admin/CreateAssessment.cshtml` — read langsung sesi ini:
  - line 1-30 (header & breadcrumb)
  - line 253-318 (Step 2 markup) ✓ verified line 289 `#selectedCountBadge`, line 294-309 `#userCheckboxContainer`, line 311-318 `#protonEligibleSection`
  - line 610-630 (Step 4 summary peserta card) ✓ verified IDs `summary-peserta-count/list/expand/extra`
  - line 855-885 (updatePills, scope verification IIFE main start)
  - line 1042-1115 (populateSummary) ✓ verified line 1042-1103 logic source
  - line 1380-1452 (filter handlers + selectAll/deselectAll + updateSelectedCount + initial call)
  - line 1500-1581 (IIFE boundaries) ✓ verified main IIFE close 1501, Proton IIFE open 1504, close 1581
  - line 1700-1755 (modal "Buat lagi" reset handler) ✓ verified line 1726-1728 manual badge update
- `.planning/phases/307-selected-participants-inline-view/307-CONTEXT.md` — 19 D-XX + 7 CD-XX
- `.planning/phases/307-selected-participants-inline-view/307-DISCUSSION-LOG.md`
- `.planning/REQUIREMENTS.md` — WIZ-01 line 25
- `.planning/STATE.md` — current focus + carry-over
- `.planning/ROADMAP.md` line 113-121 — 5 success criteria Phase 307
- `.planning/codebase/STACK.md` — Bootstrap 5.3, vanilla JS, no jQuery di Step 2 wiring
- `.planning/codebase/CONVENTIONS.md` §"View Patterns" — inline JS block per view
- `.planning/codebase/STRUCTURE.md` — single Layout shared
- `.planning/phases/304-ui-label-polish-login-wib/304-CONTEXT.md` — D-09/D-15 inline JS pattern, D-18 DOM stability
- `.planning/phases/305-question-type-naming-clarity/305-CONTEXT.md` — D-03 helper extract rationale (n≥4 callers)
- `./CLAUDE.md` — Bahasa Indonesia directive
- `.planning/config.json` — workflow.nyquist_validation = true

### Secondary (MEDIUM confidence)
- `tests/e2e/assessment.spec.ts:45` (Grep) — pre-existing test text drift `'2 selected'` vs aktual `'2 terpilih'`
- `Views/Home/Guide.cshtml:739` (Grep) — `document.createDocumentFragment()` precedent di codebase
- `Views/Account/Login.cshtml:151`, `Views/Home/Guide.cshtml:70/151` (Grep) — `aria-live="polite"` precedent

### Tertiary (LOW confidence)
- Browser support `Element.replaceChildren()` — knowledge from training, marked as Assumption A3.
- Performance estimate 50 nodes < 10ms — knowledge from training, marked as Assumption A4.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua API native + Bootstrap classes sudah live di codebase
- Architecture: HIGH — line numbers, ID, IIFE boundary verified langsung
- Pitfalls: HIGH — Pitfall #1 (cross-IIFE scope), #2 (reset gap) ditemukan dari verifikasi kode aktual sesi ini, BUKAN dari CONTEXT (CONTEXT menyebut #1 sebagai concern di CD-06 tapi tidak verify; #2 adalah temuan baru research)
- Test infrastructure: MEDIUM — Playwright tersedia tapi belum ada test untuk Step 2 panel; pre-existing rot `'2 selected'` perlu cleanup
- Browser baseline: MEDIUM — assumed modern; CONTEXT tidak menyebut explicit baseline
- Performance budget: HIGH untuk N=50 (< 10ms expected); LOW untuk worst-case scaling (N=1000+ tidak diuji oleh CONTEXT)

**Research date:** 2026-04-29
**Valid until:** 2026-05-29 (30 days — single-file scope, low velocity domain, target file struktur stabil sejak Phase 304)

---

*Phase: 307-selected-participants-inline-view*
*Research executed: 2026-04-29*
