# Phase 307: Selected Participants Inline View - Context

**Gathered:** 2026-04-29
**Status:** Ready for planning
**REQ:** WIZ-01 (maps Audit Temuan 4)

<domain>
## Phase Boundary

Menambahkan panel "Peserta Terpilih" real-time di **Step 2** wizard `Views/Admin/CreateAssessment.cshtml` (Pilih Peserta), mirroring tampilan Step 4 summary panel (badge count + nama 5 pertama + tombol expand "...dan N lainnya"). Phase ini melakukan extract helper JS `renderSelectedParticipants(targetEl, checkboxes, opts)` agar Step 2 dan Step 4 share single source of truth (success criteria #3 explicit DRY).

**Surface yang di-update:**
1. **`Views/Admin/CreateAssessment.cshtml`** — single file (markup Step 2 + JS block).
   - Markup baru: panel container setelah `#userCheckboxContainer` (line 309) dan/atau di-include di area Proton mode `#protonEligibleSection` (line 311-318).
   - JS: extract `renderSelectedParticipants()` dari logic existing populateSummary (line 1062-1103); extend `updateSelectedCount()` (line 1436); refactor Proton IIFE inline duplicate (line 1515-1519).

**Tidak termasuk dalam phase ini:**
- Migrasi DB, perubahan model `PackageExamViewModel`, ViewBag schema baru.
- Perubahan flow submit form atau validasi server-side.
- Edit `Views/Admin/EditAssessment.cshtml` (target Edit assessment) — defer kalau audit follow-up minta.
- Multi-language i18n untuk wording empty state.
- Pengambilan email/Section di panel display (sudah tersedia di list checkbox, panel hanya nama).
- Extract JS ke `wwwroot/js/wizardParticipants.js` (per Phase 304 D-09 & Phase 305 D-03 rationale: 2 caller belum justify file split).
- Refactor scope wider seperti consolidate semua duplicate `:checked` queries di file (Phase 307 hanya consolidate yang berkaitan dengan participant view + count badge).

</domain>

<decisions>
## Implementation Decisions

### WIZ-01: Penempatan & visibility panel di Step 2

- **D-01:** Panel inline tepat setelah `<div id="userCheckboxContainer">` (line 309) — sebelum `<div id="protonEligibleSection">` (line 311). Flow visual Step 2: filter bar → list checkbox → panel terpilih. Single insertion point untuk normal + Proton mode (panel show data dari container yang aktif berdasarkan `isProton` flag, bukan duplikasi panel di-dalam masing-masing section).
- **D-02:** Empty state: panel **selalu visible** (tidak hide saat 0 dipilih), menampilkan teks "Belum ada peserta dipilih" sebagai placeholder. Konsisten dengan `#selectedCountBadge` line 289 yang juga selalu visible (default text "0 terpilih"). Tidak ada layout shift saat user mulai centang.
- **D-03:** Single panel, switch data source by `isProton` flag — mirror pattern `populateSummary` line 1053-1058. Saat `Category == 'Assessment Proton'` baca `#protonUserCheckboxContainer .user-checkbox:checked`, else baca `#userCheckboxContainer .user-checkbox:checked`. Satu DOM node panel, satu render path.
- **D-04:** Keep `#selectedCountBadge` (line 289) separate — TIDAK dihapus, TIDAK dipindah. Panel baru additive di bawah container; badge di filter bar tetap untuk quick reference saat user scroll list panjang. Phase 304 D-18 stability principle dipertahankan (tidak break consumer JS lain yang reference `#selectedCountBadge`).

### WIZ-01: Strategi extract helper `renderSelectedParticipants`

- **D-05:** Helper sebagai **inline named function** di CreateAssessment.cshtml dalam JS block utama (~line 1042 area populateSummary). Konsisten dengan Phase 304 D-09 / D-15 (inline preferred). Tidak extract ke `wwwroot/js/` karena 2 caller (Step 2 + Step 4) belum justify file split per Phase 305 D-03 rationale (n≥4 callers).
- **D-06:** Signature: **imperative** `renderSelectedParticipants(targetEl, checkboxes, opts)` — match wording success criteria #3 verbatim.
  - `targetEl`: HTMLElement panel container (kosongkan + render isi)
  - `checkboxes`: Array (atau NodeList) of `.user-checkbox:checked`
  - `opts`: `{ countBadgeEl?: HTMLElement, expandText?: string, emptyText?: string, maxInline?: number }` — opt-in extension untuk berbagai use case.
- **D-07:** Helper menerima `opts.countBadgeEl` opsional untuk update count badge.
  - Step 2 caller: pass `opts.countBadgeEl = null` (count badge `#selectedCountBadge` di-handle oleh `updateSelectedCount()` existing — see D-17).
  - Step 4 caller (populateSummary refactor): pass `opts.countBadgeEl = #summary-peserta-count` agar fungsi sama yang update Step 4 badge.
- **D-08:** Tombol expand "...dan N lainnya": helper render button + bind `onclick` setiap call (mirror exact pattern populateSummary line 1087). innerHTML clear (atau replaceChildren) sebelum render baru → no listener leak. Konsisten zero behavior change dari Step 4 existing.

### WIZ-01: Real-time wiring & filter interaction

- **D-09:** Event delegation: tambah `change` listener pada `#userCheckboxContainer` (sudah ada line 1450, perlu di-extend) dan `#protonUserCheckboxContainer` (refactor existing line 1515-1519). Listener handler call `updateSelectedCount()` — yang setelah extend (D-17) juga akan trigger `renderSelectedParticipants()`. Match success criteria #2.
- **D-10:** Panel reflect **SEMUA** selected across filter — query `:checked` dari container tanpa peduli `display:none` items dari `#sectionFilter` / `#userSearchInput` / `#filterSelectedOnlyBtn` lainnya. Konsisten dengan `populateSummary` line 1056-1058 (tidak filter visible). User pilih 20 lalu filter sub-section: panel tetap show 20.
- **D-11:** Debounce 100ms hanya untuk **render panel DOM**; count badge update **immediate** (cheap textContent assignment). Best UX:
  - Click 1 checkbox → count langsung update, panel render in next debounce tick.
  - "Pilih Semua" trigger 50 sequential `cb.checked = true` → satu `updateSelectedCount()` call (existing line 1423 manual call); panel render single batch dalam debounce window.
  - **Pattern:** `setTimeout`/`clearTimeout` pair atau `requestAnimationFrame` (planner pilih — CD).
- **D-12:** Initial render on DOMContentLoaded — panggil `updateSelectedCount()` sekali saat init (existing line 1451 sudah pattern ini, tinggal extend untuk juga render panel via D-17). Mode edit yang prefill `SelectedUserIds` dari `ViewBag.SelectedUserIds` (line 297-302) langsung tampil correct di panel saat page load.

### WIZ-01: Display detail & parity Step 4

- **D-13:** Format display Step 2 = **exact match Step 4** summary:
  - `<span class="badge bg-primary" id="..."><N> peserta</span>` (badge count)
  - `<span id="...-list">{first 5 names joined by ', '}</span>` (inline names)
  - `<button id="...-expand" class="btn btn-sm btn-link">...dan N lainnya</button>` (expand toggle, hidden saat ≤5)
  - `<span id="...-extra" class="d-none">{remaining names}</span>` (hidden until expand)
  - **Why parity:** Success criteria #5 explicit "Step 2 list = Step 4 summary list (no divergence)". Helper `renderSelectedParticipants()` produce identical DOM untuk both targets.
- **D-14:** Konten per item: **nama saja** (FullName) joined by `', '` — sama persis dengan output `populateSummary` line 1063-1068 (querySelector('strong') return FullName). Email/Section sudah ada di list checkbox (line 304); panel adalah quick summary, bukan detailed view.
- **D-15:** Sumber data nama: **reuse `label[for=cb.id] querySelector('strong').textContent`** (mirror existing line 1062-1067). Zero schema change. Helper menjadi extract logic existing 1:1 — populateSummary tinggal call helper, deduplikasi natural.
- **D-16:** Wording empty state: **"Belum ada peserta dipilih"** — neutral, friendly, hint actionable. Berlaku untuk normal mode + Proton mode (peserta = generic Indonesian term). Future kalau audit minta bedakan "coachee" untuk Proton, bisa pass via `opts.emptyText`.

### WIZ-01: Wiring strategy & refactor existing code

- **D-17:** **Extend `updateSelectedCount()` (line 1436-1447)** — fungsi yang sama akan **juga** call `renderSelectedParticipants(panelEl, checkboxes, opts)` setelah update count badge. Single function = single source of truth.
  - Keuntungan: `selectAllBtn` (line 1423) dan `deselectAllBtn` (line 1432) sudah manual call `updateSelectedCount()` → panel auto-refresh tanpa modify selectAll handler. Programmatic `cb.checked = true` (yang TIDAK fire `change` event) tetap trigger panel via existing manual call site.
  - Implementasi: tambah variable `panelEl = document.getElementById('selected-participants-panel')` di scope updateSelectedCount, kemudian call helper di akhir fungsi.
- **D-18:** **Replace Proton IIFE inline change listener (line 1513-1520)** dengan call `updateSelectedCount()`:
  ```js
  // BEFORE (line 1513-1520):
  if (protonCbContainer) {
      protonCbContainer.addEventListener('change', function() {
          var count = protonCbContainer.querySelectorAll('.user-checkbox:checked').length;
          var badge = document.getElementById('selectedCountBadge');
          if (badge) badge.textContent = count + ' terpilih';
      });
  }

  // AFTER:
  if (protonCbContainer) {
      protonCbContainer.addEventListener('change', updateSelectedCount);
  }
  ```
  Hilangkan duplikat count logic. **Note:** `updateSelectedCount` adalah function di IIFE main (line 1414-1501) — perlu pastikan accessible dari Proton IIFE (line 1503-...) atau move function ke shared scope. Plan akan resolve scope issue (kemungkinan move `updateSelectedCount` ke top-level atau pass via window).
- **D-19:** Render panel pakai **DocumentFragment** + **debounce 100ms** (per success criteria #4 verbatim).
  - Helper internal: build `DocumentFragment`, append nodes, single `targetEl.replaceChildren(fragment)` (atau equivalent).
  - Debounce wrapper (planner pilih: setTimeout/clearTimeout vs requestAnimationFrame).
  - Performance budget: 50+ peserta < 200ms — verify saat UAT dengan Performance.now() instrumentation.

### Claude's Discretion

- **CD-01:** Visual styling panel exact (border / padding / spacing / mt-3) — pilih konsisten dengan Bootstrap 5.3 idiom dan style filter bar (line 260-291). Pakai `<div class="border rounded p-3 mt-3">` atau pattern `card` mini — planner pilih based on visual fit.
- **CD-02:** Class tombol expand — `btn-link` (text-only) vs `btn-sm btn-outline-secondary`. populateSummary existing tidak set class explicit di line 1087 (button mungkin sudah ber-class di markup). Planner cek markup Step 4 untuk konsistensi.
- **CD-03:** Format inline join — `', '` (mirror existing line 1079 textContent join) vs separator dot `' • '` — pilih existing pattern (`', '` recommended).
- **CD-04:** Implementasi debounce — `setTimeout`/`clearTimeout` pattern (universal, simple) vs `requestAnimationFrame` (browser-optimal). Untuk 100ms target, setTimeout cukup. Pilih saat plan/execute.
- **CD-05:** Header panel — apakah tampilkan label static "Peserta Terpilih" + badge count, atau hanya badge standalone. populateSummary Step 4 existing pattern: pisahkan elemen header text dan badge. Mirror untuk konsistensi.
- **CD-06:** Resolusi scope `updateSelectedCount` (D-18) — move function ke top-level di luar IIFE main, atau wire Proton IIFE untuk listen via `document.addEventListener` custom event. Planner pilih based on minimal regression risk (verify existing exposure via `window.updateSelectedCount` jika ada).
- **CD-07:** Apakah perlu `aria-live="polite"` pada container panel agar screen reader announce perubahan count secara real-time. Phase 304 sudah include accessibility considerations — Phase 307 panel adalah informational, pakai `aria-live="polite"` recommended tapi planner discretion.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Source & Requirements
- `.planning/ROADMAP.md` §"v15.0 Audit Findings 27 April 2026" — Phase 307 entry (line 113-121), 5 success criteria
- `.planning/REQUIREMENTS.md` line 25 (WIZ-01 acceptance criteria), line 83 (status mapping)
- `.planning/STATE.md` — current focus, Phase 306 closed, 8 carry-over items, Phase 307 next

### Target File (single touch point)
- `Views/Admin/CreateAssessment.cshtml` — REQ WIZ-01 target
  - **Markup Step 2:** lines 253-318 (filter bar, normalFilterBar, userCheckboxContainer, protonEligibleSection)
    - Line 289 — `#selectedCountBadge` (existing, KEEP unchanged)
    - Line 294-309 — `#userCheckboxContainer` + checkbox `.user-checkbox` + label `<strong>FullName</strong>`
    - Line 312-318 — `#protonEligibleSection` + `#protonUserCheckboxContainer`
  - **JS populateSummary (extract source):** lines 1042-1103 — logic baca checkboxes, query strong, slice first-5, render expand button. Phase 307 helper extract dari sini.
  - **JS event wiring (existing, di-extend):** lines 1414-1451
    - Line 1417-1424 — `selectAllBtn` handler (manual call `updateSelectedCount()`)
    - Line 1426-1433 — `deselectAllBtn` handler (manual call `updateSelectedCount()`)
    - Line 1436-1447 — `updateSelectedCount()` function (Phase 307 EXTEND di sini)
    - Line 1449-1451 — change listener `#userCheckboxContainer` + initial call
  - **JS Proton IIFE (existing, di-refactor):** lines 1503-1520
    - Line 1513-1520 — duplicate count listener (Phase 307 REPLACE dengan call `updateSelectedCount()`)
  - **Other reset call sites:** lines 1726-1727 (kemungkinan call updateSelectedCount via cb.checked=false), 1733 (filter reset)

### Pattern References (parity & inheritance)
- `Views/Admin/CreateAssessment.cshtml` line 1042-1103 — `populateSummary` adalah pattern source untuk panel Step 4. Phase 307 helper output IDENTIK dengan apa yang `populateSummary` produce.
- `.planning/phases/304-ui-label-polish-login-wib/304-CONTEXT.md` — D-09 / D-15 (inline JS preferred, no premature extract); D-18 (DOM ID stability)
- `.planning/phases/305-question-type-naming-clarity/305-CONTEXT.md` — D-03 helper extraction rationale (n≥4 callers justify; Phase 307 n=2 → exception via success criteria explicit DRY)
- `.planning/phases/306-score-editable-per-question-type/306-CONTEXT.md` — pattern audit log + try/catch defensive (tidak relevan langsung untuk Phase 307, hanya inheritance check)

### Convention References
- `.planning/codebase/STACK.md` — Bootstrap 5.3, vanilla JS (CreateAssessment), no jQuery di Step 2 wiring
- `.planning/codebase/CONVENTIONS.md` §"View Patterns" — inline JS block per view dengan IIFE encapsulation
- `.planning/codebase/STRUCTURE.md` — Views/Admin/ untuk admin view, single Layout shared

### Out of Scope (eksklusi explicit)
- `Views/Admin/EditAssessment.cshtml` — pattern alignment defer
- `Models/PackageExamViewModel.cs`, ViewBag schema — tidak diubah
- Migrasi DB, controller endpoint baru — tidak ada
- Mobile-specific layout — Bootstrap responsive default cukup
- Accessibility expanded (lebih dari `aria-live`) — defer ke phase A11Y khusus jika diminta

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`updateSelectedCount()` function** (line 1436-1447) — sudah handle isProton flag + update `#selectedCountBadge`. Phase 307 EXTEND di sini (D-17), bukan duplikasi.
- **Pattern populateSummary line 1042-1103** — logic first-5 + expand button proven works di Step 4. Helper extract dari sini → identik UX.
- **`label[for=cb.id] querySelector('strong')`** — pattern existing untuk extract FullName dari checkbox (line 1062-1067). Reuse di helper baru.
- **selectAllBtn/deselectAllBtn manual call pattern** (line 1423, 1432) — handle programmatic `cb.checked` toggle yang TIDAK fire `change` event. Phase 307 piggyback via D-17 (extend updateSelectedCount).
- **Bootstrap 5.3 badge + button classes** — `bg-primary`, `btn-sm`, `btn-link`, `d-none` semua sudah dipakai di view, tinggal reuse.
- **DocumentFragment** — native API, no library needed.

### Established Patterns
- **Inline JS block di .cshtml** — pattern existing CreateAssessment.cshtml (1700+ baris JS inline). Phase 304/305/306 semua honor pattern ini. Phase 307 lanjut.
- **IIFE encapsulation** — main IIFE line 1414-1501, Proton IIFE line 1503-...; main IIFE expose `updateSelectedCount` (Phase 307 perlu pastikan accessible dari Proton IIFE — CD-06).
- **Event delegation pattern** — line 1450 `userContainer.addEventListener('change', updateSelectedCount)` adalah pattern delegated listener di parent container. Phase 307 EXTEND ke Proton container (D-09, D-18).
- **DOM ID stability** — Phase 304 D-18 absolute. Panel baru pakai ID baru (`selected-participants-panel`, `selected-participants-list`, dll), TIDAK ubah existing ID.

### Integration Points
- **Step 4 summary panel** — `summary-peserta-count`, `summary-peserta-list`, `summary-peserta-expand`, `summary-peserta-extra` (line 1070-1103). populateSummary akan call helper baru untuk render → konsolidasi success criteria #5 parity.
- **Filter handlers** — `#userSearchInput`, `#sectionFilter` (line 262, 276) ubah `display` items di list. D-10 keputusan: panel ABAIKAN filter visibility, query `:checked` directly. Tidak konflik.
- **Reset call sites** — line 1726-1727 reset all `.user-checkbox` saat Category change (atau similar). Setelah D-17, reset trigger updateSelectedCount → panel auto-refresh ke "Belum ada peserta dipilih".
- **Proton AJAX load** — `protonTrackSelect` change handler (line 1522+) load Proton checkboxes dynamic via AJAX (line 1570 markup gen). Setelah D-18, change listener di `protonCbContainer` listen change events di checkbox baru → panel update real-time.

### Risks & Caveats
- **Programmatic `cb.checked = true` tidak fire `change`** — handled via D-17 (selectAllBtn/deselectAllBtn manual call updateSelectedCount).
- **Scope `updateSelectedCount`** dari Proton IIFE — perlu hoist atau expose globally (CD-06). Plan akan resolve.
- **Performance 50+ peserta** — DocumentFragment + debounce 100ms (D-19). Verify dengan instrumentasi `performance.now()` saat UAT.
- **File conflict serialization** — Phase 304 sudah merge, Phase 307 next, Phase 308 setelah. Per ROADMAP line 182 strict sequential. Phase 307 mulai dari clean state Phase 306.
- **populateSummary Step 4 refactor** — saat helper di-extract, populateSummary line 1062-1103 di-replace dengan call ke helper. Risk: Step 4 behavior change. Mitigation: helper output IDENTIK dengan existing logic (D-13, D-14, D-15), tidak ada divergence yang user-visible.

</code_context>

<specifics>
## Specific Ideas

### Why match Step 4 exact format
User memilih parity absolute (D-13) bukan visual variant (chips/pills/two-column). Rationale:
- **Success criteria #5 explicit:** "Step 2 list = Step 4 summary list (no divergence)"
- **Single source of truth helper** = identical DOM output untuk Step 2 dan Step 4
- **Cognitive load minimal:** user yang sudah pakai wizard familiar dengan format Step 4 → Step 2 langsung intuitive
- **Implementation simplicity:** helper extract 1:1 dari existing populateSummary, tidak ada divergent visual variant

### Why piggyback updateSelectedCount(), bukan wrapper baru
User pilih extend updateSelectedCount() (D-17) bukan wrapper refreshSelectedView() yang call both:
- **Minimal touch points:** call sites existing (line 1423, 1432, 1450, 1451, 1727 etc) tidak perlu di-rename / di-update
- **Single function source of truth:** programmer cari "di mana count update" → satu function, bukan dua-layer
- **Pattern existing:** `updateSelectedCount()` sudah well-named dan handle isProton — extend natural fit
- **Risk regresi minimum:** programmatic toggle path (selectAll, reset) sudah work via existing manual call, panel join the ride

### DocumentFragment use case
Walaupun first-5 inline rendering "secukupnya" pakai textContent (per success criteria mention DocumentFragment yang lebih relevan untuk full-list / expanded state), implementasi tetap pakai DocumentFragment uniformly:
- **Forward compatibility:** kalau future requirement minta render full list (semua 50+ peserta sebagai chips/list items), DocumentFragment sudah ada
- **Reduce reflow:** single appendChild = 1 reflow vs N appendChild = N reflows (walaupun N=5 negligible, pattern hygiene)
- **Match success criteria literal:** #4 sebut DocumentFragment explicit

### Empty state wording pure Indonesian
"Belum ada peserta dipilih" (D-16) match style flash message Indonesian existing di codebase. Pattern app pure Indonesia (label, button, error). Tidak ada kebutuhan English fallback.

### File serialization confidence
Phase 304 (label/WIB) sudah merge clean ke main, Phase 307 mulai dari clean state. Phase 308 (PrePost validation) menunggu Phase 307 selesai per ROADMAP line 182. Tidak ada conflict risk dengan parallel work.

</specifics>

<deferred>
## Deferred Ideas

### Out of Phase 307 Scope
1. **Different empty wording per mode** — "Belum ada coachee dipilih" untuk Proton mode. Defer kalau audit follow-up minta. Saat ini "peserta" generic cukup.
2. **Chips/pills format atau two-column tabel** — out per parity Step 4 absolute. Kalau audit follow-up minta visual rich, butuh juga update Step 4 → coordinate phase tersendiri.
3. **Email/Section di panel display** — out per parity nama saja (D-14). User punya akses detail di list checkbox line 304.
4. **Extract JS ke wwwroot/js/wizardParticipants.js** — out per Phase 304 D-09 / Phase 305 D-03. Future kalau ada caller ke-3 (mis. EditAssessment.cshtml minta panel similar), justify extract; Phase 307 tetap inline.
5. **Tooltip pada hover untuk email/section** — defer (mobile no hover, slight value-add).
6. **Filter-aware panel section "Terpilih (visible)" + "Terpilih (filtered out)"** — out per D-10. Kompleksitas tidak justify saat ini.
7. **EditAssessment.cshtml panel similar** — defer ke phase berikutnya kalau audit minta. Phase 307 hanya CreateAssessment.cshtml.
8. **A/B test atau feature flag toggle** — out, atomic implementation.
9. **i18n / multi-language** — out per project pure Indonesian.
10. **Aria-live region untuk screen reader announce count** — CD-07 leaves implementer discretion. Recommended yes, but not strictly required for phase 307 success criteria.

### Reviewed Todos (not folded)
- `realtime-assessment.md` (1 pending todo) — `todo match-phase 307` returned 0 matches. Inspeksi manual: tidak overlap dengan WIZ-01 selected participants. Defer review ke phase yang lebih relevan.

</deferred>

---

*Phase: 307-selected-participants-inline-view*
*Context gathered: 2026-04-29*
