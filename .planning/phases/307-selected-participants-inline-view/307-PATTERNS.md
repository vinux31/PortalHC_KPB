# Phase 307: Selected Participants Inline View — Pattern Map

**Mapped:** 2026-04-29
**Files analyzed:** 1 source file (5 change units) + 3 Wave 0 test/UAT artifacts
**Analogs found:** 8 / 8 (semua memiliki analog konkret di-codebase yang sama)
**Source repo:** `Views/Admin/CreateAssessment.cshtml` (single touch point untuk source) + `tests/e2e/assessment.spec.ts` (test extend)

---

## File Classification

| New/Modified Code Unit | Role | Data Flow | Closest Analog | Match Quality |
|------------------------|------|-----------|----------------|---------------|
| `CreateAssessment.cshtml` AFTER line 309 — NEW panel markup `#selected-participants-panel-wrapper` | view / Razor markup (Bootstrap card-mini) | server-render-only (state hydrated dari JS) | **Same file** lines 616-630 (Step 4 `summary-peserta-card` block) + filter bar lines 288-291 (badge `bg-primary`) | exact (visual + DOM-id parity) |
| `CreateAssessment.cshtml` HOIST area antara line 1501-1504 — NEW top-level helper `renderSelectedParticipants(targetEl, checkboxes, opts)` | view / inline JS named function (DOM helper) | event-driven render (DocumentFragment + replaceChildren) | **Same file** lines 1061-1103 (`populateSummary` first-5/expand block — extract source) + line 1042 area pattern named `function populateSummary()` declaration | exact (logic 1:1 extract) |
| `CreateAssessment.cshtml` HOIST area antara line 1501-1504 — NEW debounce wrapper `scheduleRenderSelectedPanel()` + module-scope `__renderTimer` | view / inline JS module-scope state | event-driven (debounced 100ms) | **Same file** lines 1395-1402 area (existing `applyFilters` debounce — verify pattern) atau **`Views/Home/Guide.cshtml:739`** (DocumentFragment precedent) | role-match (no exact debounce existing — adopt setTimeout/clearTimeout idiom) |
| `CreateAssessment.cshtml` HOIST `updateSelectedCount` ke top-level — MOVE function dari line 1436-1447 ke antara line 1501 dan 1504 | view / inline JS scope refactor (cross-IIFE accessibility) | event-driven (cross-IIFE shared callable) | **Same file** lines 1768+ area (top-level `function checkPreTestWarning()` precedent — top-level function declaration outside any IIFE) | role-match (precedent ada — top-level function pattern di-file) |
| `CreateAssessment.cshtml` EXTEND lines 1436-1447 (di lokasi baru top-level) — `updateSelectedCount` body extend untuk panggil `scheduleRenderSelectedPanel(panelEl, checkboxes, opts)` setelah update count badge | view / inline JS function body extend | request-response (DOM read → DOM write) | **Same file** lines 1436-1447 (existing function body — pattern target) | exact (extend in-place) |
| `CreateAssessment.cshtml` REPLACE lines 1513-1520 (Proton IIFE inline change listener) dengan single-line `protonCbContainer.addEventListener('change', updateSelectedCount)` | view / inline JS event listener simplification | event-driven (delegated change) | **Same file** line 1450 `userContainer.addEventListener('change', updateSelectedCount)` (existing pattern — exact mirror) | exact |
| `CreateAssessment.cshtml` EDIT lines 1726-1728 (modal "Buat lagi" reset handler) — replace manual badge update dengan `updateSelectedCount()` call | view / inline JS reset handler simplification | event-driven (modal click → DOM reset) | **Same file** lines 1417-1424 + 1426-1433 (`selectAllBtn` / `deselectAllBtn` pattern — manual `cb.checked = X` lalu `updateSelectedCount()`) | exact (mirror selectAll/deselectAll piggyback pattern) |
| `tests/e2e/assessment.spec.ts` EXTEND — NEW describe block `Phase 307 Selected Participants Panel` + opportunistic fix line 45 `'2 selected'` → `'2 terpilih'` | E2E test scaffold | request-response (Playwright browser automation) | **Same file** lines 21-92 existing `Assessment - Admin Creates & Manages` describe block (test pattern + login + locator + expect.toContainText) | exact (extend describe + replicate test idiom) |
| `tests/e2e/helpers/wizardSelectors.ts` (CREATE — file/folder belum ada) | E2E selector constants module | static export | **Pattern reference:** `tests/helpers/auth.ts`, `tests/helpers/utils.ts` (existing top-level helper modules) — adopt similar export const pattern | role-match (no `e2e/helpers/` subfolder yet — apakah place di `tests/helpers/` ATAU `tests/e2e/helpers/` — planner discretion) |
| `.planning/phases/307-selected-participants-inline-view/307-UAT.md` (CREATE manual UAT) | docs / Markdown UAT script | doc-only | **Phase 306** `306-VALIDATION.md` § Manual-Only Verifications + Sampling Rate (10-step UAT precedent) | exact (mirror Phase 306 UAT structure) |

---

## Pattern Assignments

### 1. NEW Panel Markup — `Views/Admin/CreateAssessment.cshtml` setelah line 309

**Action:** INSERT new HTML block setelah `</div>` close `#userCheckboxContainer` line 309, sebelum `<div id="protonEligibleSection">` line 311.

**Analog A (visual structure — Step 4 summary card lines 616-630):**
```html
<!-- Card 2: Peserta -->
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

**Analog B (filter bar badge styling — line 288-291):**
```html
<div class="col-auto ms-auto">
    <span class="badge bg-primary" id="selectedCountBadge">0 terpilih</span>
</div>
```

**Target pattern (NEW — copy idiom dari Analog A header + Analog B badge `bg-primary`; CD-01 `border rounded p-3 mt-3` style mini-card; CD-05 inline header; CD-07 aria-live polite di body):**
```html
<!-- Phase 307 — Panel "Peserta Terpilih" inline (D-01, D-02, D-13, CD-01, CD-05, CD-07) -->
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

**Notes:**
- ID prefix `selected-participants-*` (D-13, NOT touching existing `summary-peserta-*` atau `selectedCountBadge` — Phase 304 D-18 stability absolute).
- `bg-primary` mirror Analog B (filter bar badge), bukan `bg-secondary` (Analog A summary card) — Phase 307 panel adalah focal element Step 2.
- Empty state span di-render server-side sebagai default placeholder. JS helper `renderSelectedParticipants()` akan replace via `replaceChildren()` saat invoked (D-02 always-visible, no layout shift).
- Single insertion point untuk normal + Proton (D-03) — helper reads dari container aktif berdasarkan `isProton` flag.

---

### 2. NEW JS Helper `renderSelectedParticipants()` — top-level antara line 1501-1504

**Action:** INSERT named function declaration di top-level scope (di luar kedua IIFE) — accessible dari main IIFE (line 831) dan Proton IIFE (line 1504).

**Analog (extract source — `populateSummary` lines 1061-1103, verbatim baca dari file):**
```javascript
var names = checkboxes.map(function(cb) {
    var label = document.querySelector('label[for="' + cb.id + '"]');
    if (label) {
        var strong = label.querySelector('strong');
        return strong ? strong.textContent : label.textContent.trim();
    }
    return cb.value;
});

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
        expandBtn.onclick = function() {
            if (extraEl) {
                if (extraEl.classList.contains('d-none')) {
                    extraEl.textContent = ', ' + remaining.join(', ');
                    extraEl.classList.remove('d-none');
                    expandBtn.textContent = 'Sembunyikan';
                } else {
                    extraEl.classList.add('d-none');
                    expandBtn.textContent = '...dan ' + remaining.length + ' lainnya';
                }
            }
        };
    }
} else {
    if (expandBtn) expandBtn.classList.add('d-none');
    if (extraEl) extraEl.classList.add('d-none');
}
```

**Target pattern (NEW helper — extract logic 1:1, signature literal per success criteria #3, DocumentFragment build per D-19):**
```javascript
// Phase 307 — Top-level helper (di antara line 1501 dan line 1504)
// Signature LITERAL per success criteria #3 verbatim (D-06).
function renderSelectedParticipants(targetEl, checkboxes, opts) {
    opts = opts || {};
    var maxInline = opts.maxInline || 5;
    var emptyText = opts.emptyText || 'Belum ada peserta dipilih';

    // Extract names — mirror existing populateSummary line 1061-1068 logic 1:1 (D-15)
    var names = (checkboxes || []).map(function(cb) {
        var label = document.querySelector('label[for="' + cb.id + '"]');
        if (label) {
            var strong = label.querySelector('strong');
            return strong ? strong.textContent : label.textContent.trim();
        }
        return cb.value;
    });

    // DocumentFragment build (D-19 + Guide.cshtml:739 precedent)
    var frag = document.createDocumentFragment();

    if (names.length === 0) {
        var empty = document.createElement('span');
        empty.className = 'text-muted fst-italic';
        empty.textContent = emptyText;
        frag.appendChild(empty);
    } else {
        var first = names.slice(0, maxInline);
        var listSpan = document.createElement('span');
        listSpan.textContent = first.join(', ');
        frag.appendChild(listSpan);

        if (names.length > maxInline) {
            var remaining = names.slice(maxInline);
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-link btn-sm p-0 ms-1';   // mirror Step 4 line 627
            btn.textContent = '...dan ' + remaining.length + ' lainnya';

            var extra = document.createElement('span');
            extra.className = 'd-none';

            // onclick property assignment (mirror line 1087, no listener leak — Pitfall 5)
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

    // replaceChildren atomic (with defensive fallback per A3 — IE11/legacy unlikely tapi safe)
    if (targetEl) {
        if (typeof targetEl.replaceChildren === 'function') {
            targetEl.replaceChildren(frag);
        } else {
            targetEl.innerHTML = '';
            targetEl.appendChild(frag);
        }
    }

    // Optional badge update (D-07) — Step 4 caller pakai opts.countBadgeEl untuk update badge
    if (opts.countBadgeEl) {
        opts.countBadgeEl.textContent = names.length + ' peserta';
    }
}
```

**Notes:**
- `textContent` (NOT `innerHTML`) untuk semua user data — XSS-safe (Pitfall 4).
- `btn.onclick =` property assignment (NOT `addEventListener`) — mirror line 1087, single slot overwrite, no listener leak (Pitfall 5).
- `replaceChildren()` dengan fallback `innerHTML = ''; appendChild(frag)` — A3 defensive untuk legacy browser.
- Helper TIDAK update count badge `#selected-participants-count` di Step 2 (D-11 + D-07: Step 2 caller pass `opts.countBadgeEl = null`, count di-update immediate oleh `updateSelectedCount` body, panel debounced).
- Step 4 caller pass `opts.countBadgeEl = #summary-peserta-count` agar fungsi sama yang update Step 4 badge.

---

### 3. NEW Debounce Wrapper `scheduleRenderSelectedPanel()` — top-level antara line 1501-1504

**Action:** INSERT module-scope timer var + wrapper function.

**Analog (no exact debounce di-file — adopt setTimeout/clearTimeout idiom universal; selectAll/deselectAll lines 1417-1424 / 1426-1433 menunjukkan bulk operation pattern yang butuh debounce):**
```javascript
// Existing line 1417-1424 — bulk toggle 50+ checkbox dalam tight loop:
if (selectAllBtn) selectAllBtn.addEventListener('click', function() {
    document.querySelectorAll('#userCheckboxContainer .user-check-item').forEach(function(item) {
        if (item.style.display !== 'none') {
            item.querySelector('.user-checkbox').checked = true;
        }
    });
    updateSelectedCount();   // ← single call, panel butuh debounce di sini
});
```

**Target pattern (NEW debounce per D-11, D-19 + CD-04 setTimeout):**
```javascript
// Phase 307 — Module-scope timer (di antara line 1501 dan line 1504, sebelum updateSelectedCount hoist)
var __renderTimer = null;
function scheduleRenderSelectedPanel(targetEl, checkboxes, opts) {
    if (__renderTimer) clearTimeout(__renderTimer);
    __renderTimer = setTimeout(function() {
        renderSelectedParticipants(targetEl, checkboxes, opts);
        __renderTimer = null;
    }, 100);
}
```

**Notes:**
- 100ms debounce per success criteria #4 + D-19.
- `clearTimeout` pattern reset on every call → bulk `selectAllBtn` (50 sequential `cb.checked = true` lalu `updateSelectedCount()` once) → satu render call dalam window.
- CD-04 picks setTimeout over rAF — deterministic 100ms target untuk performance budget verification.

---

### 4. HOIST `updateSelectedCount()` — MOVE dari line 1436-1447 ke top-level antara 1501-1504

**Action:** MOVE function declaration dari main IIFE body ke top-level scope. Resolve CD-06 (cross-IIFE accessibility) via Pattern 3 di RESEARCH.md.

**Analog (top-level function precedent — same file area line 1768+, Pattern 3 RESEARCH.md):**
```javascript
// Existing top-level function declaration di file (di luar IIFE):
function checkPreTestWarning() {
    // ... (pattern di-file untuk top-level helper)
}
```

**Existing function body (line 1436-1447 — verbatim):**
```javascript
function updateSelectedCount() {
    var catEl = document.getElementById('Category');
    var isProton = catEl && catEl.value === 'Assessment Proton';
    var count;
    if (isProton) {
        count = document.querySelectorAll('#protonUserCheckboxContainer .user-checkbox:checked').length;
    } else {
        count = document.querySelectorAll('#userCheckboxContainer .user-checkbox:checked').length;
    }
    var badge = document.getElementById('selectedCountBadge');
    if (badge) badge.textContent = count + ' terpilih';
}
```

**Target pattern (HOISTED + EXTENDED per D-17 — single source of truth untuk count + panel):**
```javascript
// Phase 307 — HOISTED top-level (di antara line 1501 dan line 1504)
function updateSelectedCount() {
    var catEl = document.getElementById('Category');
    var isProton = catEl && catEl.value === 'Assessment Proton';

    // Query checkboxes dari container aktif (mirror existing isProton branching)
    var sel = isProton
        ? '#protonUserCheckboxContainer .user-checkbox:checked'
        : '#userCheckboxContainer .user-checkbox:checked';
    var checkboxes = Array.from(document.querySelectorAll(sel));

    // 1. Count badge filter bar — IMMEDIATE (cheap textContent assignment) (D-11)
    var badge = document.getElementById('selectedCountBadge');
    if (badge) badge.textContent = checkboxes.length + ' terpilih';

    // 2. Panel header count badge — IMMEDIATE (D-07: Step 2 update count terpisah dari panel render)
    var panelCount = document.getElementById('selected-participants-count');
    if (panelCount) panelCount.textContent = checkboxes.length + ' peserta';

    // 3. Panel render — DEBOUNCED 100ms (D-11, D-19) — Phase 307 EXTEND
    var panel = document.getElementById('selected-participants-panel');
    if (panel) scheduleRenderSelectedPanel(panel, checkboxes, {
        emptyText: 'Belum ada peserta dipilih'
    });
}
```

**Notes:**
- HOIST ke top-level (Pattern 3) — eliminate Proton IIFE duplicate (item 6 di bawah).
- DELETE existing function declaration line 1436-1447 dari main IIFE body setelah hoist.
- Existing call sites (line 1423 selectAllBtn, line 1432 deselectAllBtn, line 1450 change listener, line 1451 initial call) TIDAK perlu di-update — bareword resolves ke top-level function setelah hoist.
- A2 verified: tidak ada local `var updateSelectedCount` rebinding di main IIFE — hoist aman.

---

### 5. EXTEND `populateSummary()` — Step 4 refactor lines 1042-1103

**Action:** REFACTOR `populateSummary` body lines 1061-1103 untuk delegate ke helper baru. Markup Step 4 (line 626-628) — TWO design options per RESEARCH.md A1.

**Analog (existing populateSummary line 1042-1103 — extract source):** [referensi item 2 di atas]

**Target pattern OPTION A (recommended — refactor markup Step 4 line 625-629 jadi single container):**

Markup edit (line 625-629):
```html
<!-- BEFORE (line 625-629): -->
<div class="card-body">
    <span id="summary-peserta-list"></span>
    <button type="button" id="summary-peserta-expand" class="btn btn-link btn-sm p-0 d-none ms-1"></button>
    <span id="summary-peserta-extra" class="d-none"></span>
</div>

<!-- AFTER (Phase 307 Option A): -->
<div class="card-body" id="summary-peserta-list-container">
    <!-- helper renders here -->
</div>
```

JS edit (lines 1042-1103 — replace body 1052-1103):
```javascript
function populateSummary() {
    // Lines 1043-1051 (Category & Title) — UNCHANGED
    var catEl = document.getElementById('Category');
    var titleEl = document.getElementById('Title');
    var summCat = document.getElementById('summary-category');
    var summTitle = document.getElementById('summary-title');
    if (summCat) summCat.textContent = catEl ? (catEl.options[catEl.selectedIndex] ? catEl.options[catEl.selectedIndex].text : '') : '';
    if (summTitle) summTitle.textContent = titleEl ? titleEl.value : '';

    // Phase 307 — Replace lines 1052-1103 dengan helper call (D-15 DRY)
    var isProton = catEl && catEl.value === 'Assessment Proton';
    var checkboxes = isProton
        ? Array.from(document.querySelectorAll('#protonUserCheckboxContainer .user-checkbox:checked'))
        : Array.from(document.querySelectorAll('#userCheckboxContainer .user-checkbox:checked'));

    renderSelectedParticipants(
        document.getElementById('summary-peserta-list-container'),
        checkboxes,
        {
            countBadgeEl: document.getElementById('summary-peserta-count'),
            emptyText: 'Belum ada peserta dipilih',
            maxInline: 5
        }
    );

    // Lines 1105+ (Tipe badge, Settings, etc.) — UNCHANGED
    // ...
}
```

**Notes:**
- Option A simpler — markup 3 baris jadi 1 baris, helper sederhana (single replaceChildren mode), parity Step 2/Step 4 absolute (success criteria #5).
- A1 verified: 5 occurrences `summary-peserta` di view, semua di markup line 620-628 + populateSummary line 1070-1075. Tidak ada consumer eksternal — refactor aman.
- `summary-peserta-count` (header badge) di-PERTAHANKAN (Phase 304 D-18 stability) — hanya body card di-konsolidasi.

---

### 6. REPLACE Proton IIFE inline change listener — lines 1513-1520

**Action:** REPLACE inline 6-baris dengan single-line panggil `updateSelectedCount`.

**Analog (existing pattern di main IIFE line 1450 — exact mirror):**
```javascript
var userContainer = document.getElementById('userCheckboxContainer');
if (userContainer) userContainer.addEventListener('change', updateSelectedCount);
```

**Existing code to replace (lines 1513-1520, verbatim):**
```javascript
// Event delegation: listen once on container, works for dynamically added checkboxes
if (protonCbContainer) {
    protonCbContainer.addEventListener('change', function() {
        var count = protonCbContainer.querySelectorAll('.user-checkbox:checked').length;
        var badge = document.getElementById('selectedCountBadge');
        if (badge) badge.textContent = count + ' terpilih';
    });
}
```

**Target pattern (Phase 307 D-18 — depends on item 4 hoist completed):**
```javascript
// Event delegation: listen once on container, works for dynamically added checkboxes
if (protonCbContainer) {
    protonCbContainer.addEventListener('change', updateSelectedCount);
}
```

**Notes:**
- DEPENDS on item 4 hoist (CD-06 resolved). Bareword `updateSelectedCount` resolves ke top-level dari Proton IIFE scope.
- Dynamic checkbox added by AJAX (line 1570 area) tetap fire change events via delegation di container — pattern existing OK.
- Pitfall 3 follow-up: planner verify apakah AJAX success handler (sekitar line 1522+ markup gen) perlu explicit call `updateSelectedCount()` after inject — defer ke planner.

---

### 7. EDIT Reset Handler "Buat lagi" — lines 1726-1728

**Action:** REPLACE manual badge update dengan single `updateSelectedCount()` call.

**Analog (existing piggyback pattern di main IIFE line 1417-1424 / 1426-1433 — programmatic toggle + manual updateSelectedCount call):**
```javascript
// selectAllBtn pattern (line 1417-1424):
if (selectAllBtn) selectAllBtn.addEventListener('click', function() {
    document.querySelectorAll('#userCheckboxContainer .user-check-item').forEach(function(item) {
        if (item.style.display !== 'none') {
            item.querySelector('.user-checkbox').checked = true;
        }
    });
    updateSelectedCount();   // ← manual call setelah programmatic toggle
});
```

**Existing code to replace (lines 1726-1728, verbatim):**
```javascript
document.querySelectorAll('.user-checkbox').forEach(function(cb) { cb.checked = false; });
var badge = document.getElementById('selectedCountBadge');
if (badge) badge.textContent = '0 terpilih';
```

**Target pattern (Phase 307 — D-17 piggyback, mirror selectAllBtn pattern):**
```javascript
document.querySelectorAll('.user-checkbox').forEach(function(cb) { cb.checked = false; });
updateSelectedCount();   // updates badge filter bar + panel count + panel body via D-17 single source of truth
```

**Notes:**
- Pitfall #2 dari RESEARCH.md — gap critical: tanpa edit ini, panel "Peserta Terpilih" akan tetap tampil list nama lama meskipun reset clear semua checkbox.
- Edit ini bersifat WAJIB untuk Phase 307 success criteria #2 real-time fidelity (reset adalah event yang harus reflect ke panel).

---

### 8. EXTEND E2E Test — `tests/e2e/assessment.spec.ts` NEW describe block

**Action:** APPEND new describe block setelah existing FLOW 1 (line 92), opportunistic fix line 45 `'2 selected'` → `'2 terpilih'`.

**Analog (existing test pattern lines 21-92 — describe block + login + locator + expect):**
```typescript
test.describe('Assessment - Admin Creates & Manages', () => {

  test('1.2 - HC can create a new assessment for workers', async ({ page }) => {
    assessmentTitle = uniqueTitle('Assessment OJT');
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    const iwanCheckbox = page.locator('.user-check-item', { hasText: 'iwan3' }).locator('input');
    await rinoCheckbox.click({ force: true });
    await iwanCheckbox.click({ force: true });

    await expect(page.locator('#selectedCountBadge')).toContainText('2 selected');   // ← LINE 45 ROT — fix to '2 terpilih'
    // ...
  });
});
```

**Target pattern (NEW describe block setelah line 92):**
```typescript
// ============================================================
// FLOW 7: Phase 307 — Selected Participants Inline Panel (Step 2)
// ============================================================
test.describe('Assessment - Phase 307 Selected Participants Panel', () => {

  test('7.1 - Step 2 panel renders with empty state on initial load', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    // Panel always-visible (D-02), empty state default
    await expect(page.locator('#selected-participants-panel-wrapper')).toBeVisible();
    await expect(page.locator('#selected-participants-count')).toContainText('0 peserta');
    await expect(page.locator('#selected-participants-panel')).toContainText('Belum ada peserta dipilih');
  });

  test('7.2 - Panel updates real-time when user toggles checkbox', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    await rinoCheckbox.click({ force: true });

    // Count immediate, panel debounced 100ms
    await expect(page.locator('#selected-participants-count')).toContainText('1 peserta');
    await page.waitForTimeout(150);  // wait debounce window
    await expect(page.locator('#selected-participants-panel')).not.toContainText('Belum ada peserta dipilih');
    await expect(page.locator('#selected-participants-panel')).toContainText(/Rino|Prasetyo/i);
  });

  test('7.3 - Step 4 summary list parity with Step 2 panel (DRY single source)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');

    const rinoCheckbox = page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input');
    const iwanCheckbox = page.locator('.user-check-item', { hasText: 'iwan3' }).locator('input');
    await rinoCheckbox.click({ force: true });
    await iwanCheckbox.click({ force: true });
    await page.waitForTimeout(150);

    // Capture Step 2 panel text
    const step2Text = await page.locator('#selected-participants-panel').textContent();

    // Navigate to Step 4 (fill required fields then advance)
    // ... fill form, goto step 4
    // const step4Text = await page.locator('#summary-peserta-list-container').textContent();
    // expect(step4Text).toBe(step2Text);

    // Note: full parity assertion requires advancing wizard — planner finalize sequence.
    expect(step2Text).toContain('Rino');
  });

  test('7.4 - Reset "Buat lagi" clears panel to empty state', async ({ page }) => {
    // After successful submit, click Buat lagi modal button
    // Verify panel reverts to "Belum ada peserta dipilih"
    // ... (planner finalize after item 7 reset edit verified)
  });
});
```

**Notes:**
- Mirror existing describe + test idiom (login → goto → locator → expect).
- Use `page.locator('#selected-participants-...')` selectors per ID prefix Phase 307.
- 7.4 dependent on item 7 reset handler edit — sekuens test setelah implementasi merged.
- Opportunistic fix line 45: ganti `'2 selected'` → `'2 terpilih'` (pre-existing rot per RESEARCH.md).

---

### 9. NEW Selector Helper — `tests/e2e/helpers/wizardSelectors.ts`

**Action:** CREATE new file. Folder `tests/e2e/helpers/` belum ada — planner pilih lokasi (option: `tests/helpers/wizardSelectors.ts` atau buat folder baru `tests/e2e/helpers/`).

**Analog (existing helper modules — `tests/helpers/auth.ts`, `tests/helpers/utils.ts`):**

(Dapat ditelusuri planner — pattern named export top-level constants atau function.)

**Target pattern (NEW selectors module):**
```typescript
// tests/e2e/helpers/wizardSelectors.ts (atau tests/helpers/wizardSelectors.ts — planner discretion)
// Phase 307 — DOM ID selectors untuk Step 2 panel

export const selectors = {
  // Phase 307 panel
  panelWrapper: '#selected-participants-panel-wrapper',
  panelBody: '#selected-participants-panel',
  panelCount: '#selected-participants-count',

  // Step 4 summary (parity check target)
  summaryListContainer: '#summary-peserta-list-container',  // Phase 307 markup refactor
  summaryCount: '#summary-peserta-count',

  // Existing Step 2 (Phase 304 D-18 stability — TIDAK di-touch)
  filterBarBadge: '#selectedCountBadge',
  userContainer: '#userCheckboxContainer',
  protonContainer: '#protonUserCheckboxContainer',
};
```

**Notes:**
- Lightweight POM (Page Object Model) — single file dengan `export const`.
- Planner pilih lokasi `tests/helpers/` (existing pattern) ATAU `tests/e2e/helpers/` (RESEARCH.md sebut path ini di Wave 0 Gaps).
- Test file 8 di atas dapat refactor pakai `import { selectors } from './helpers/wizardSelectors'` setelah file baru ada.

---

### 10. NEW Manual UAT Script — `.planning/phases/307-selected-participants-inline-view/307-UAT.md`

**Action:** CREATE new Markdown file. Mirror Phase 306 UAT structure.

**Analog (Phase 306 `306-VALIDATION.md` § Manual-Only Verifications — 10-step UAT precedent):**

| Step | Behavior | Test Instructions |
|------|----------|-------------------|
| Phase 306 step 1 | HTML5 native validation tooltip | "Type 150 di input scoreValue, focus blur atau submit → expect Chrome tooltip 'Value must be ≤ 100'" |
| Phase 306 step 4 | Modal "Peringatan Ubah Skor" muncul | "Edit question dengan completed session, ubah ScoreValue, expect modal popup" |

**Target pattern (NEW UAT script untuk Phase 307 — 5 step minimum per success criteria):**
```markdown
# Phase 307 — Manual UAT Script

**Scenario:** Verify panel "Peserta Terpilih" Step 2 wizard CreateAssessment.

## Pre-conditions
- Login sebagai HC atau Admin
- Browser modern (Chrome/Edge ≥ 86) — buka DevTools Performance tab untuk step 4

## Step 1: Initial render & empty state
1. Goto `/Admin/CreateAssessment`
2. Pilih Category "Assessment OJT" (atau apa pun non-Proton)
3. Scroll ke Step 2 panel "Pilih Peserta"
4. **Expect:** Panel "Peserta Terpilih" tampil setelah list checkbox dengan teks "Belum ada peserta dipilih" + badge "0 peserta"
5. **Expect:** Tidak ada layout shift (panel always-visible per D-02)

## Step 2: Real-time toggle
1. Centang 1 peserta
2. **Expect:** Count badge `#selected-participants-count` langsung jadi "1 peserta" (immediate)
3. **Expect:** Panel body update dalam ~100ms menampilkan nama peserta tersebut
4. Centang 4 peserta lagi (total 5)
5. **Expect:** Panel tampil 5 nama joined `', '`, tanpa tombol expand
6. Centang 1 peserta lagi (total 6)
7. **Expect:** Panel tampil 5 nama + tombol "...dan 1 lainnya"
8. Klik tombol expand
9. **Expect:** Tampil nama ke-6, tombol berubah jadi "Sembunyikan"

## Step 3: Bulk operations (selectAll/deselectAll)
1. Klik tombol "Pilih Semua" (asumsi list 50+ user)
2. **Expect:** Count badge langsung update ke "N terpilih"
3. **Expect:** Panel render single batch dalam debounce window (tidak ada flicker)
4. Klik tombol "Batalkan Semua"
5. **Expect:** Panel revert ke "Belum ada peserta dipilih"

## Step 4: Performance budget (50+ peserta)
1. Buka DevTools → Console
2. Run: `performance.mark('start'); document.getElementById('selectAllBtn').click();`
3. Setelah panel render: `performance.mark('end'); performance.measure('p307', 'start', 'end'); console.log(performance.getEntriesByName('p307')[0].duration);`
4. **Expect:** Duration < 200ms untuk 50 peserta

## Step 5: Reset "Buat lagi" + Proton mode
1. Submit assessment dengan 3 peserta sukses
2. Klik "Buat lagi" di success modal
3. **Expect:** Panel reset ke "Belum ada peserta dipilih" (per item 7 edit reset handler)
4. Switch Category ke "Assessment Proton"
5. Pilih track Proton, tunggu AJAX load checkbox eligible
6. Centang 2 coachee
7. **Expect:** Panel update real-time (item 6 Proton IIFE refactor)
```

**Notes:**
- 5 steps minimum cover 5 success criteria Phase 307.
- Mirror Phase 306 prose style — Bahasa Indonesia, action → expect format.
- Performance budget step 4 wajib (success criteria #4 explicit < 200ms).

---

## Shared Patterns

### A. ID Naming Convention (Phase 307 namespace)
**Source:** Phase 304 D-18 stability principle + Phase 307 D-13.
**Apply to:** Items 1, 4, 5, 9.

ID prefix BARU = `selected-participants-*`:
- `selected-participants-panel-wrapper` (root container card)
- `selected-participants-panel` (body — helper render target)
- `selected-participants-count` (header badge)

ID existing = TIDAK DI-TOUCH:
- `selectedCountBadge` (filter bar — line 289)
- `summary-peserta-count` (Step 4 header — line 620)
- `summary-peserta-list/expand/extra` (Step 4 body — line 626-628; Option A consolidate jadi `summary-peserta-list-container` SAJA, ID lama removed dari markup tapi code referencing existing populateSummary di-replace via helper call — no consumer external)
- `userCheckboxContainer`, `protonUserCheckboxContainer` (data sources)

### B. JS Encapsulation Convention (top-level vs IIFE)
**Source:** Existing CreateAssessment.cshtml pattern + Phase 304 D-09/D-15 + Phase 307 CD-06.
**Apply to:** Items 2, 3, 4.

- **Top-level (di luar IIFE):** Cross-IIFE shared helpers — `renderSelectedParticipants`, `scheduleRenderSelectedPanel`, `updateSelectedCount`, dan precedent `checkPreTestWarning` (existing).
- **Main IIFE (line 831-1501):** WizardController state (selectAllBtn, deselectAllBtn handler, populateSummary, etc.) — TETAP di IIFE, panggil top-level helpers via bareword.
- **Proton IIFE (line 1504-1581):** Proton track AJAX + checkbox container listener — panggil top-level `updateSelectedCount` via bareword.

### C. Event Delegation Pattern
**Source:** Existing line 1450 + Phase 307 D-09, D-18.
**Apply to:** Items 4, 6.

```javascript
container.addEventListener('change', updateSelectedCount);
```

- Single listener di parent container (NOT per-checkbox) → handle dynamic AJAX-mounted checkboxes.
- Apply ke `#userCheckboxContainer` (existing line 1450, NO CHANGE) dan `#protonUserCheckboxContainer` (item 6 REFACTOR ke pattern ini).

### D. XSS Safety via `textContent`
**Source:** Existing pattern line 1079, 1086, 1090 + Pitfall 4 RESEARCH.md.
**Apply to:** Items 2, 5.

- Semua user data (FullName dari ApplicationUser/AD-LDAP) → `node.textContent = name` (NEVER `innerHTML`).
- Helper Phase 307 wajib pertahankan disiplin ini — DocumentFragment + createElement + textContent.

### E. Bahasa Indonesia String Convention
**Source:** CLAUDE.md C-01 + Phase 307 D-16.
**Apply to:** Items 1, 2, 5, 8, 10.

- Panel header: "Peserta Terpilih"
- Empty state: "Belum ada peserta dipilih"
- Expand button: `'...dan ' + N + ' lainnya'`
- Collapse button: "Sembunyikan"
- Count format: `N + ' peserta'` (panel) / `N + ' terpilih'` (filter bar — existing format)

### F. Piggyback Pattern (programmatic toggle + manual call)
**Source:** Existing selectAllBtn / deselectAllBtn pattern lines 1417-1433 + Phase 307 D-17.
**Apply to:** Items 4, 7.

```javascript
// Programmatic checkbox toggle (TIDAK fire 'change' event)
checkboxes.forEach(function(cb) { cb.checked = X; });
updateSelectedCount();   // ← manual call piggyback (single source of truth)
```

- Pattern wajib untuk semua call site yang programmatic toggle: selectAll, deselectAll, reset "Buat lagi", future bulk operations.
- Setelah hoist (item 4) + extend body, single call → count badge + panel count + panel body semua update.

---

## No Analog Found

Tidak ada file dalam scope Phase 307 yang TANPA analog. Semua 10 change units punya analog di-codebase (mayoritas same-file).

**Catatan minor:**
- Item 3 (debounce wrapper) — no exact debounce di-file CreateAssessment.cshtml; adopt setTimeout/clearTimeout idiom universal yang dipakai di Web Platform best practice. Precedent DocumentFragment ada di `Views/Home/Guide.cshtml:739`.
- Item 9 (selectors helper) — folder `tests/e2e/helpers/` belum ada. Pattern POM bisa adopt dari `tests/helpers/auth.ts` (existing module).

---

## Metadata

**Analog search scope:**
- `Views/Admin/CreateAssessment.cshtml` (1755 lines — full file scanned per RESEARCH.md verifikasi)
- `Views/Home/Guide.cshtml` (DocumentFragment precedent)
- `tests/e2e/assessment.spec.ts` (existing test patterns)
- `tests/helpers/*.ts` (helper module pattern)
- `.planning/phases/304-*`, `.planning/phases/305-*`, `.planning/phases/306-*` (precedent phases — DOM stability, helper extract rationale, manual UAT structure)

**Files scanned:** ~12 files (mayoritas already verified by RESEARCH.md line numbers)
**Pattern extraction date:** 2026-04-29
**Confidence:** HIGH — semua line numbers + IDs + IIFE boundaries cross-referenced dengan RESEARCH.md (yang sudah verify direct read di-sesi sebelumnya)

---

*Phase: 307-selected-participants-inline-view*
*Pattern mapping executed: 2026-04-29*
