---
phase: 311
slug: manageassessment-performance
status: draft
shadcn_initialized: false
preset: not applicable
created: 2026-05-07
---

# Phase 311 — UI Design Contract

> Visual & interaction contract untuk refactor `Views/Admin/ManageAssessment.cshtml` dari rendering inline ke pattern HTMX lazy-load. **Bukan UI redesign.** Visual struktur existing DIPERTAHANKAN (D-10 backward compat). Spec ini hanya mengikat elemen visual baru yang diperkenalkan oleh arsitektur HTMX:
>
> 1. Skeleton placeholder (3 varian per tab)
> 2. Error template + retry button
> 3. Loading indicator behavior (htmx-request)
> 4. Tab transition + race condition handling
> 5. Filter form interaction feedback

---

## Scope Boundary

| Aspect | Status |
|---|---|
| Halaman target | `Views/Admin/ManageAssessment.cshtml` (shell) + 3 partial views existing |
| Visual existing (header, breadcrumb, nav-tabs, tabel kolom, card, dropdown, pagination, badge categori/status, empty state) | **PRESERVED — out of scope spec ini** (manual UAT verifies parity) |
| Visual baru (skeleton, error, loading state, htmx feedback) | **IN SCOPE — di-spec di sini** |
| Copywriting baru (loading, error, retry) | **IN SCOPE Bahasa Indonesia formal** |
| Copywriting existing (CTA "Buat Assessment", empty state, filter labels) | **PRESERVED** |

---

## Design System

| Property | Value | Source |
|---|---|---|
| Tool | none (no shadcn — ASP.NET Core MVC + Razor stack) | shadcn gate skipped: not React/Next.js/Vite |
| Preset | not applicable | — |
| Component library | Bootstrap 5.3.0 (CDN `cdn.jsdelivr.net`) | `Views/Shared/_Layout.cshtml:38` |
| Icon library | Bootstrap Icons 1.10.0 | `Views/Shared/_Layout.cshtml:39` |
| Font | Bootstrap default (system font stack: `-apple-system, "Segoe UI", Roboto, …`) | Bootstrap 5 default — no override di project |
| HTMX | 2.0.x (vendored ke `wwwroot/lib/htmx/htmx.min.js`) | D-02 |
| jQuery | available (vendored di `wwwroot/lib/jquery/`) | existing |

---

## Spacing Scale

Mengikuti Bootstrap 5 utility scale (`mb-0`..`mb-5`, `gap-1`..`gap-5`, `p-*`, `m-*`). Project sudah konsisten pakai utility class — tidak ada custom spacing token.

| Token | Bootstrap class | Computed value | Usage di Phase 311 |
|---|---|---|---|
| xs | `mb-1`, `gap-1`, `p-1` | 4px | Inline icon gap, badge spacing |
| sm | `mb-2`, `gap-2`, `p-2` | 8px | Compact spacing dalam skeleton row |
| md | `mb-3`, `gap-3`, `p-3` | 16px | Default element spacing (filter row, alert padding) |
| lg | `mb-4`, `gap-4`, `p-4` | 24px | Section padding |
| xl | `mb-5`, `gap-5`, `p-5` | 48px | Empty state vertical padding (existing) |

**Exceptions:** Skeleton row height = ~60px untuk match approximate row height tabel actual (mengurangi layout shift saat content swap). Bukan multiple of 4 strict, karena harus match natural row height tabel `.table` Bootstrap 5 default. Acceptable per Bootstrap component pattern.

---

## Typography

Mengikuti Bootstrap 5 default heading scale + utility class `.fs-*`, `.fw-*`, `.text-muted`, `.small`. Tidak ada custom font stack untuk Phase 311.

| Role | Bootstrap class | Computed | Weight | Line height | Usage di Phase 311 |
|---|---|---|---|---|---|
| Body | (default `<body>`) | 16px (1rem) | 400 | 1.5 (Bootstrap default) | Error message body, retry button label |
| Small | `.small` atau `<small>` | 14px (0.875rem) | 400 | 1.5 | Skeleton placeholder secondary text, loading hint |
| Heading h2 | `.fw-bold` heading existing | 32px (2rem) | 700 | 1.2 | Page header (preserved, not modified) |
| Heading h5 | `.fw-bold` | 20px (1.25rem) | 700 | 1.2 | Empty state headline (preserved) |

**3-4 effective sizes used in Phase 311 visual elements:** 14px (small/hint), 16px (body/error msg), 20px (h5 — preserved), 32px (h2 — preserved).
**Effective weights:** 400 (regular body) + 700 (bold via `.fw-bold` for headings + retry button emphasis).

---

## Color

Mengikuti Bootstrap 5 theme palette default. Tidak ada custom palette untuk Phase 311.

| Role | Value | Bootstrap token | Usage di Phase 311 |
|---|---|---|---|
| Dominant (60%) | `#ffffff` | `bg-white` / page background | Page surface, card backgrounds |
| Secondary (30%) | `#f8f9fa` | `bg-light` | Filter row card background, skeleton placeholder shimmer base |
| Accent (10%) | `#0d6efd` | `bg-primary` / `text-primary` / `btn-primary` | Reserved for: primary CTA "Buat Assessment" (existing), retry link inside error template, primary action emphasis |
| Destructive | `#dc3545` | `alert-danger` / `text-danger` | Reserved for: HTMX error template (`alert-danger`), delete confirms (existing) |
| Info / loading | `#6c757d` | `text-muted` | Skeleton placeholder shimmer, loading hint text |

**Accent reserved for (eksplisit):**
- `btn-primary` "Buat Assessment" header (existing, preserved)
- `btn-primary` "Cari" submit di filter form (existing, preserved)
- Retry link inside HTMX error template (new — D-section 4 below)
- Primary tab active indicator (Bootstrap default — preserved)

**NOT used as accent:**
- Skeleton placeholder (uses `text-muted` neutral, not blue)
- Loading state (no accent color flash)
- htmx-request indicator (opacity/cursor only, no color change)

---

## New Visual Elements (Phase 311)

### 1. Skeleton Placeholder

**Pattern:** Bootstrap 5 native `.placeholder-glow` + `.placeholder` classes (sudah loaded via Bootstrap CSS — no new CSS dep).

**3 variants — match approximate height tabel actual untuk minimize layout shift:**

#### 1a. Assessment Groups tab skeleton

Match struktur tabel `_AssessmentGroupsTab.cshtml`: filter row + stats badge + tabel 5 row × 9 kolom.

```html
<div aria-busy="true" aria-label="Memuat data assessment" class="placeholder-glow">
  <!-- Filter row skeleton -->
  <div class="row mb-4">
    <div class="col-md-6"><span class="placeholder col-12" style="height:38px"></span></div>
  </div>
  <div class="row mb-3">
    <div class="col-md-3"><span class="placeholder col-12" style="height:31px"></span></div>
    <div class="col-md-3"><span class="placeholder col-12" style="height:31px"></span></div>
  </div>
  <!-- Stats badge skeleton -->
  <div class="mb-3"><span class="placeholder col-2" style="height:24px"></span></div>
  <!-- Tabel skeleton: 5 rows × ~60px -->
  <div class="card shadow-sm border-0">
    <div class="card-body">
      @for (int i = 0; i < 5; i++)
      {
        <div class="d-flex gap-2 mb-2 align-items-center" style="height:48px">
          <span class="placeholder col-1"></span>
          <span class="placeholder col-3"></span>
          <span class="placeholder col-2"></span>
          <span class="placeholder col-2"></span>
          <span class="placeholder col-1"></span>
          <span class="placeholder col-1"></span>
          <span class="placeholder col-2"></span>
        </div>
      }
    </div>
  </div>
</div>
```

#### 1b. Training Records tab skeleton

Match struktur `_TrainingRecordsTab.cshtml`: header buttons row + 4-kolom filter card + tabel 5 row.

```html
<div aria-busy="true" aria-label="Memuat data training" class="placeholder-glow">
  <!-- Header skeleton -->
  <div class="d-flex justify-content-between mb-4">
    <span class="placeholder col-2" style="height:24px"></span>
    <div class="d-flex gap-2">
      <span class="placeholder" style="width:140px;height:38px"></span>
      <span class="placeholder" style="width:140px;height:38px"></span>
    </div>
  </div>
  <!-- Filter card skeleton -->
  <div class="card bg-light border-0 mb-4">
    <div class="card-body">
      <div class="row g-3">
        @for (int i = 0; i < 4; i++)
        {
          <div class="col-md-2"><span class="placeholder col-12" style="height:31px"></span></div>
        }
      </div>
    </div>
  </div>
  <!-- Tabel skeleton: 5 rows -->
  @for (int i = 0; i < 5; i++)
  {
    <div class="d-flex gap-2 mb-2" style="height:48px">
      <span class="placeholder col-3"></span>
      <span class="placeholder col-2"></span>
      <span class="placeholder col-2"></span>
      <span class="placeholder col-2"></span>
      <span class="placeholder col-1"></span>
    </div>
  }
</div>
```

#### 1c. History tab skeleton (compact)

Match struktur `_HistoryTab.cshtml`: nested sub-tabs + filter row + tabel compact 4-5 row.

```html
<div aria-busy="true" aria-label="Memuat riwayat" class="placeholder-glow">
  <!-- Nested sub-tabs skeleton -->
  <div class="d-flex gap-3 mb-3 border-bottom pb-2">
    <span class="placeholder col-2" style="height:32px"></span>
    <span class="placeholder col-2" style="height:32px"></span>
  </div>
  <!-- Filter row skeleton -->
  <div class="row g-2 mb-3">
    <div class="col-md-4"><span class="placeholder col-12" style="height:31px"></span></div>
    <div class="col-md-4"><span class="placeholder col-12" style="height:31px"></span></div>
  </div>
  <!-- Tabel skeleton: 4 rows compact -->
  @for (int i = 0; i < 4; i++)
  {
    <div class="d-flex gap-2 mb-2" style="height:40px">
      <span class="placeholder col-3"></span>
      <span class="placeholder col-3"></span>
      <span class="placeholder col-2"></span>
      <span class="placeholder col-2"></span>
    </div>
  }
</div>
```

**Skeleton storage:** Tiga partial baru di `Views/Admin/Shared/_Skeleton{Assessment,Training,History}.cshtml` ATAU inline di shell view di-wrap dalam `<partial>` tag. Planner pilih (Claude's Discretion). Default rekomendasi: inline di shell view (less file proliferation, skeleton hanya dipakai sekali per tab).

**Color:** Bootstrap default `.placeholder` = `currentColor` dengan `opacity:0.5`, di body = abu-abu netral (#6c757d-ish). NO accent color flash di skeleton — neutral only.

**Animation:** Bootstrap `.placeholder-glow` built-in pulse animation (~2s duration, `cubic-bezier(0.4, 0, 0.6, 1)`). No custom keyframes.

**Accessibility:**
- Wrapper `aria-busy="true"` selama skeleton visible
- `aria-label` deskriptif Bahasa Indonesia per tab
- Setelah HTMX swap, atribut `aria-busy` otomatis hilang (skeleton DOM di-replace)

---

### 2. Error Template (HTMX request fail)

**Trigger:** HTMX `htmx:responseError` event (4xx/5xx response) di partial endpoint, ATAU `htmx:sendError` (network failure).

**Implementation pattern (Claude's Discretion locked):**
- Tidak swap target dengan response body kalau response error (default HTMX behavior). Sebagai gantinya, register global event listener `htmx:responseError` + `htmx:sendError` yang inject error template ke target failed request.
- Error template di-inline ke target tab pane via JS handler.

**HTML template (inline JS string atau hidden Razor partial):**

```html
<div role="alert" class="alert alert-danger d-flex align-items-start gap-2 my-3">
  <i class="bi bi-exclamation-triangle-fill flex-shrink-0" style="font-size:1.25rem"></i>
  <div class="flex-grow-1">
    <div class="fw-semibold mb-1">Gagal memuat data</div>
    <div class="small mb-2">Periksa koneksi jaringan Anda lalu coba lagi. Jika masalah berlanjut, hubungi administrator.</div>
    <button type="button" class="btn btn-sm btn-outline-danger" data-htmx-retry>
      <i class="bi bi-arrow-clockwise me-1"></i>Coba Lagi
    </button>
  </div>
</div>
```

**Retry button behavior:**
- Klik tombol `[data-htmx-retry]` → trigger `htmx.trigger(targetElement, 'load')` untuk re-fire request asal.
- Selama retry in-flight, swap konten error → skeleton placeholder lagi (reuse skeleton dari section 1).

**Color:** `alert-danger` (Bootstrap default `#f8d7da` background, `#842029` text, `#f5c2c7` border) + `btn-outline-danger` retry button. Konsisten dengan existing flash messages di `ManageAssessment.cshtml:27-32`.

**Spacing:** `my-3` (16px vertical margin), `gap-2` (8px) antar icon dan content.

**No emoji.** Icon Bootstrap Icons `bi-exclamation-triangle-fill` saja.

---

### 3. Loading Indicator (htmx-request CSS class)

**Behavior:** Saat HTMX request in-flight, target element auto-receives `.htmx-request` class. Pakai class ini untuk visual feedback minimal.

**CSS rules (tambah ke `wwwroot/css/site.css` atau inline `<style>` di shell view):**

```css
/* Filter form: subtle disabled visual selama re-fetch */
#filter-form.htmx-request {
  opacity: 0.7;
  pointer-events: none;
  transition: opacity 0.15s ease-in-out;
}

/* Tab pane: skeleton sudah handle visual selama load awal.
   Untuk re-fetch (filter change, pagination), tambah subtle opacity. */
.tab-pane.htmx-request {
  opacity: 0.85;
  transition: opacity 0.15s ease-in-out;
}

/* Cursor feedback global */
.htmx-request {
  cursor: progress;
}
```

**No spinner overlay, no progress bar.** Bootstrap 5 sudah punya `.spinner-border` tapi TIDAK dipakai di Phase 311 — skeleton + opacity dim sudah cukup. Spinner akan compete visually dengan skeleton.

**No "Memuat..." text overlay.** Skeleton sendiri sudah deklarasi state loading (via `aria-busy`).

---

### 4. Tab Transition & Active Tab Indicator

**Preserved from Bootstrap 5 default:**
- `nav-link.active` styling (border-bottom primary blue) — existing, preserved.
- Tab pane fade transition (`fade show active` classes) — existing, preserved.
- Header "Buat Assessment" buttons show/hide pada `shown.bs.tab` event — existing JS preserved (lines 109-123).

**New behaviors:**

#### 4a. Tab activation lifecycle

```
User klik tab Training:
  1. Bootstrap fire `show.bs.tab` event
  2. Tab pane fade in
  3. Bootstrap fire `shown.bs.tab` event (transition complete)
  4. HTMX listener `hx-trigger="shown.bs.tab from:closest button.nav-link once"` fire
  5. Skeleton sudah visible (rendered at shell time)
  6. HTTP GET → partial endpoint
  7. HTMX swap: skeleton → real content (innerHTML swap)
  8. Subsequent klik same tab: NO re-fetch (`once` modifier)
```

**Active tab pertama (saat shell render):** `hx-trigger="load"` immediate fire setelah HTMX init. Skeleton visible ~50-200ms sebelum data swap di backend cepat (lokal DB), atau seberapa lama partial endpoint balas di wifi kantor.

#### 4b. Race condition handling (cepat klik antar tab)

Locked: `hx-sync="this:replace"` di tiap pane wrapper.

**Effect:** Kalau user klik Tab A → Tab B → Tab A cepat:
- Request A pertama in-flight di-cancel saat user klik Tab B (HTMX abort).
- Request B fire.
- Klik Tab A lagi: kalau request A pertama belum complete, di-cancel; request A baru fire.

**Visual:** Tidak ada flicker glitch. Skeleton tetap visible di tab yang request-nya in-flight.

#### 4c. Filter change → cross-tab cache invalidation (D-05)

**Visual lifecycle saat user ubah filter di tab Assessment aktif:**

```
1. User type "test" di search box (focus tetap di input)
2. Debounce 500ms (typing further resets timer)
3. HTMX fire GET /Admin/ManageAssessmentTab_Assessment?search=test
4. Selama in-flight: 
   - Filter form: opacity 0.7, pointer-events:none (disabled)
   - Pane Assessment: opacity 0.85
   - Cursor: progress
5. Response received → swap → opacity restore
6. JS handler trigger: invalidate "loaded" flag pada tab Training & History
   (e.g., reset `data-loaded` attribute, atau strip cached innerHTML, atau revert ke skeleton)
7. Saat user pindah ke tab Training: skeleton visible → fetch baru fire dengan filter terbaru
```

**Filter form NOT disabled visually saat dropdown change** (instant trigger, response cepat). HANYA debounce search box yang feels like "typing then pause" pattern.

---

### 5. Filter Form Interaction Feedback

**Preserved (existing visual):**
- Search input style: `input-group` dengan icon `bi-search`, primary "Cari" button.
- Category dropdown: `form-select form-select-sm` "Semua Kategori".
- Status dropdown: `form-select form-select-sm` "Aktif (Open/Upcoming)".
- Reset filter button: `btn-sm btn-outline-secondary` muncul kalau filter aktif.

**New behaviors:**

| Element | Trigger | Visual feedback during fetch |
|---|---|---|
| Search input (text) | `input changed delay:500ms` | Form opacity 0.7, cursor progress, search button tidak berubah label |
| Category dropdown | `change` (instant) | Form opacity 0.7 ~1-2 sec |
| Status dropdown | `change` (instant) | Form opacity 0.7 ~1-2 sec |
| Pagination link | `click` (existing-style HTMX `hx-get`) | Pane opacity 0.85, page link tidak active until response |
| Reset filter link | `click` | Standard navigation, full pane re-fetch |

**Search button "Cari":** Tetap visible (existing). Klik manual = bypass debounce (immediate fire). Atau, planner boleh hide kalau pakai pure live search — TBD by planner. Default rekomendasi: keep "Cari" button as fallback for users who hit Enter atau prefer click.

**Empty filter input → reset:** Existing "x" clear button (`bi-x-lg`) preserved. Klik = reset search param + re-fetch tab aktif.

---

## Copywriting Contract

Bahasa Indonesia formal-functional, internal admin tool. **No emojis.** No marketing tone. Concise, helpful.

| Element | Copy | Notes |
|---|---|---|
| Skeleton aria-label (Assessment) | `Memuat data assessment` | screen reader announcement |
| Skeleton aria-label (Training) | `Memuat data training` | screen reader announcement |
| Skeleton aria-label (History) | `Memuat riwayat` | screen reader announcement |
| Error heading | `Gagal memuat data` | Bold, single line |
| Error body | `Periksa koneksi jaringan Anda lalu coba lagi. Jika masalah berlanjut, hubungi administrator.` | Two sentences: actionable + escalation path |
| Retry button | `Coba Lagi` | Title case, with `bi-arrow-clockwise` icon |
| Primary CTA (preserved) | `Buat Assessment` | existing, NOT modified |
| Empty state heading (preserved) | `Tidak ada assessment ditemukan` / `Tidak ada assessment ditemukan untuk "{search}"` | existing, NOT modified |
| Empty state body (preserved) | `Buat assessment pertama untuk memulai.` | existing, NOT modified |
| Destructive confirmations (preserved) | `Tindakan ini akan menghapus kedua sesi (Pre dan Post)…Lanjutkan?` / `Hapus semua assessment dalam grup ini?` | existing, NOT modified — Phase 311 tidak introduce destructive action baru |

**Loading text:** Tidak ada text overlay "Loading..." atau "Memuat...". Skeleton sendiri visual cue + `aria-busy` aria announcement = sufficient.

**Tooltip / progress hints:** Tidak ada tambahan. Bootstrap default tooltip pada elemen yang sudah ada preserved.

---

## Interaction State Matrix

| State | Trigger | Visual | A11y |
|---|---|---|---|
| Initial shell load | GET `/Admin/ManageAssessment` | Header + tabs + skeleton di tab aktif visible instant | `aria-busy=true` di skeleton wrapper |
| Active tab fetch in-flight | `hx-trigger="load"` setelah shell render | Skeleton tetap visible, no overlay | `aria-busy=true` |
| Tab fetch success | HTMX swap response | Skeleton → real content, smooth swap | `aria-busy` removed (skeleton DOM gone) |
| Tab fetch error (4xx/5xx/network) | `htmx:responseError` / `htmx:sendError` | Error template (alert-danger) inline di pane | `role="alert"` |
| Retry click | `[data-htmx-retry]` click | Error → skeleton (reuse) → fetch fire | `aria-busy=true` saat skeleton |
| Tab switch (first time) | `shown.bs.tab` once | Skeleton in target pane visible → swap | `aria-busy` lifecycle same as initial |
| Tab switch (cached) | `shown.bs.tab` after first fetch | Instant content (no fetch, no skeleton) | normal |
| Filter change (search) | `input changed delay:500ms` | Form opacity 0.7, pane opacity 0.85, cursor progress | implicit (no aria change) |
| Filter change (dropdown) | `change` instant | Form opacity 0.7, pane opacity 0.85 | implicit |
| Pagination click | `hx-get` link click | Pane opacity 0.85, link tidak active visual | implicit |
| Race condition (cepat antar tab) | `hx-sync="this:replace"` cancel | Previous skeleton stays / replaced; no flicker | implicit |
| Filter cross-tab invalidation | After filter fetch success | Other tabs reset to skeleton state (lazy re-fetch on click) | `aria-busy=true` saat user buka tab tsb |

---

## Accessibility Baseline

User base = admin internal Chrome/Edge modern. Tidak butuh AA premium tapi tetap baseline accessible:

- **Keyboard navigation:** Bootstrap default (Tab, Enter on tab buttons, arrow keys di nav-tabs) preserved.
- **Screen reader:** `aria-busy`, `aria-label` deskriptif di skeleton wrapper + `role="alert"` di error template.
- **Focus management:** Setelah tab switch + content swap, focus TIDAK auto-move ke pane content (preserve user's keyboard journey). Setelah retry click, focus tetap di retry button location (atau berpindah ke first focusable element kalau button hilang setelah swap — acceptable).
- **Color contrast:** Bootstrap 5 defaults compliant ≥4.5:1 untuk teks body. Error template `alert-danger` color combination Bootstrap default = compliant.
- **No keyboard traps:** Skeleton elements tidak focusable (no `tabindex`, no interactive element inside).
- **`prefers-reduced-motion`:** Bootstrap `.placeholder-glow` honors via Bootstrap's built-in `@media (prefers-reduced-motion: reduce)` rule (animation disabled). No additional override needed.

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|---|---|---|
| shadcn official | none | not applicable (no shadcn in project) |
| Bootstrap 5.3.0 (CDN) | `.placeholder-glow`, `.placeholder`, `.alert-danger`, `.btn-outline-danger`, `.nav-tabs`, `.tab-pane` | not applicable (existing project dep, vendored upstream) |
| Bootstrap Icons 1.10.0 (CDN) | `bi-exclamation-triangle-fill`, `bi-arrow-clockwise` | not applicable (existing project dep) |
| HTMX 2.0.x (vendored local `wwwroot/lib/htmx/`) | core attrs `hx-get`, `hx-trigger`, `hx-target`, `hx-swap`, `hx-include`, `hx-sync` | **Vendoring step**: Plan 02 task harus verify SHA hash dari upstream `https://unpkg.com/htmx.org@2.0/dist/htmx.min.js` sebelum commit (per `311-CONTEXT.md` <specifics>). Tidak ada third-party registry lain. |

**No third-party registries declared.** Registry safety gate (shadcn-specific) not applicable.

---

## Out of Scope (clarification)

- Redesign tabel kolom, ordering, pagination component visual.
- Redesign filter form layout / labels / dropdowns.
- Redesign breadcrumb, page header, action buttons.
- Redesign empty state copy/visual.
- Redesign destructive confirmation copy.
- New color palette / typography overrides.
- shadcn migration (not React stack).
- Custom CSS keyframes (Bootstrap built-ins sufficient).
- Spinner overlays, progress bars, toast notifications.
- Mobile-specific tweaks (admin desktop primary).

---

## Pre-populated Sources

| Decision | Source | Pre-locked? |
|---|---|---|
| Skeleton style = Bootstrap `.placeholder-glow` | CONTEXT.md Claude's Discretion | yes (default rekomendasi) |
| Error template `alert-danger` + retry button | CONTEXT.md Claude's Discretion | yes |
| HTMX swap mode = `innerHTML` | CONTEXT.md Claude's Discretion | yes |
| Active tab `hx-trigger="load"` | CONTEXT.md Claude's Discretion | yes (default `load`) |
| Race condition: `hx-sync="this:replace"` | CONTEXT.md Claude's Discretion | yes |
| Bootstrap 5.3.0 + Bootstrap Icons 1.10.0 | `Views/Shared/_Layout.cshtml` (codebase scan) | yes (existing) |
| HTMX 2.0.x vendored | CONTEXT.md D-02 | yes (locked) |
| Bahasa Indonesia formal copywriting | PROJECT.md principle + CLAUDE.md | yes (locked) |
| No emojis | CLAUDE.md project standard + scope_guidance | yes (locked) |
| Filter debounce 500ms (search) | CONTEXT.md D-03 | yes (locked) |
| Filter dropdown instant `change` | CONTEXT.md D-03 | yes (locked) |
| Cross-tab cache invalidation behavior | CONTEXT.md D-05 | yes (locked) |
| Cache-Control: no-store di partial endpoints | CONTEXT.md D-06 | yes (locked) — does not affect UI spec |
| Visual struktur preserved per tab | CONTEXT.md D-10 + REQUIREMENTS PERF-01 | yes (smoke test parity) |

**No questions asked to user.** All design contract decisions are either:
1. Locked di CONTEXT.md (D-01..D-10 + 6 Claude's Discretion items).
2. Inherited from Bootstrap 5 / project standards (typography, spacing, color).
3. Preserved from existing visual (out of scope for redesign).

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS — Bahasa Indonesia formal, no emojis, error message actionable + escalation path, retry label concise
- [ ] Dimension 2 Visuals: PASS — 3 skeleton variants per tab, error template, loading state, race condition handling
- [ ] Dimension 3 Color: PASS — 60/30/10 split honored, accent reserved list explicit, neutral skeleton, destructive only for error
- [ ] Dimension 4 Typography: PASS — 3-4 sizes (14/16/20/32), 2 weights (400/700) inherited from Bootstrap default
- [ ] Dimension 5 Spacing: PASS — Bootstrap utility scale (4/8/16/24/48), exception documented for skeleton row height
- [ ] Dimension 6 Registry Safety: PASS — no third-party registries, HTMX vendoring SHA verification noted as Plan 02 task

**Approval:** pending
