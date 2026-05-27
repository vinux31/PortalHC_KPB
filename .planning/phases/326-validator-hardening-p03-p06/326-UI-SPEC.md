---
phase: 326
slug: validator-hardening-p03-p06
status: draft
shadcn_initialized: false
preset: none
created: 2026-05-27
scope: minor-view-tweak
---

# Phase 326 — UI Design Contract

> Visual and interaction contract NARROW SCOPE. Phase 326 = bug fix validator hardening (P03 + P06). UI-side hanya tambah 1 section read-only renewal display + 1 clear button + field-level error display di `Views/Admin/EditTraining.cshtml`. TIDAK ada redesign, NO new design system, NO new dependency.

**Bahasa UI:** Bahasa Indonesia (konsisten dengan project-wide CLAUDE.md instruction + existing Razor copy).

---

## Design System

Pre-existing project stack — tidak ada perubahan.

| Property | Value | Source |
|----------|-------|--------|
| Tool | none (no shadcn — bukan React project) | ASP.NET Core 8 + Razor Views |
| Preset | not applicable | — |
| Component library | Bootstrap 5 (already loaded project-wide) | `wwwroot/lib/bootstrap` + layout `_AdminLayout.cshtml` |
| Icon library | Bootstrap Icons (`bi-*` classes) | Existing usage L22/L44/L129/L171 EditTraining.cshtml |
| Font | System sans-serif (Bootstrap default — Segoe UI on Windows clients) | Project-wide, no override |
| CSS approach | Bootstrap utility classes only, **NO new CSS file** | Constraint per spec §6 + CONTEXT D-07 |
| JS approach | Inline `onclick=""` handler (no jQuery, no external script) | CONTEXT.md Claude's Discretion bullet 2 |

---

## Scope Boundary (Wajib Dibaca Sebelum Implement)

**In-scope UI delta:**

1. Section card baru di `EditTraining.cshtml` (conditional render kalau renewal FK present).
2. Clear button "Hapus link renewal" dengan inline onclick handler.
3. 2 hidden inputs `RenewsTrainingId` + `RenewsSessionId` passthrough.
4. Field-level error display untuk P06 via `<span asp-validation-for="ValidUntil">` (kemungkinan sudah implicit dari existing pattern — confirm di plan-phase).

**Out-of-scope UI (jangan dibikin):**

- Full picker UI (radio TR/AS + dropdown typeahead) — explicit rejected option B di CONTEXT Q1 follow-up.
- AJAX async cycle-check client-side.
- Toast/modal redesign — pakai existing `TempData["Error"]` + `<div asp-validation-summary="All">` pattern existing L28-31.
- Typography scale, color tokens, animation, dark mode, responsive overhaul.
- Touching AddTraining.cshtml renewal-mode UI — already perfect, reference only.

---

## Spacing Scale

Bootstrap 5 utility scale (existing project standard, no override). Phase 326 hanya pakai sebagian.

| Token | Bootstrap class | Pixel | Usage di Phase 326 |
|-------|-----------------|-------|--------------------|
| sm | `mb-2`, `me-2`, `py-2` | 8px | Icon gap di card header, button icon margin |
| md | `mb-3`, `g-3`, `py-3` | 16px | Card header padding, row gutter |
| lg | `mb-4` | 24px | Card-to-card vertical spacing (section break) |

**Exceptions:** none. Section card baru pakai `mb-4` mirror existing card "Data Training" L42 + "Data Sertifikat" L127.

**Insertion point:** Section card baru ditempatkan **antara** card "Data Training" (L42-125) DAN card "Data Sertifikat" (L127-179) — semantik flow: training info → renewal link info → sertifikat data. Mirror flow logical.

---

## Typography

Project-wide Bootstrap defaults — tidak ada token baru.

| Role | Bootstrap class | Usage di Phase 326 |
|------|-----------------|--------------------|
| Card header heading | `h5.fw-bold` | "Renewal Source" card header (mirror L44/L129) |
| Label/Display value | `form-label.fw-bold` (label) + plain text (value) | "Renewal dari:" label + source title text |
| Helper text | `small.text-muted` | Optional clarifier "Hapus link bila salah set" |
| Validation error | `text-danger.small` | Field-level `<span asp-validation-for="ValidUntil">` (existing pattern L52/L59/L78) |

**Heading hierarchy lokal:** Page H2 (L21) → card H5 (L44, L129, **new section H5**). Tidak ada heading H3/H4 — keep flat per existing convention.

---

## Color

Bootstrap 5 contextual classes (existing). Tidak ada custom hex.

| Role | Bootstrap class | Usage Phase 326 |
|------|-----------------|------------------|
| Surface dominant | `bg-white` (card-header) + default body bg | Card background — mirror existing |
| Accent primary | `text-primary` (icon di card-header) | `<i class="bi-arrow-repeat text-primary">` di header section baru |
| Warning context | `alert alert-warning` (rejected — too loud) | NOT USED — renewal display di Edit context = info read-only, bukan warning. Pakai card neutral, BUKAN alert. |
| Destructive | `btn-outline-danger` | "Hapus link renewal" button — semantik clear/destructive action terhadap data renewal link |
| Validation error | `text-danger` (existing L50 `<span class="text-danger">*</span>` + validation span pattern) | Field error P06 di ValidUntil |
| Info muted | `text-muted` | Helper text optional, source title sub-info |

**Accent reserved for:**
- Icon `bi-arrow-repeat` di card-header (text-primary)
- Primary CTA "Simpan Perubahan" L186 (existing, no change)

**Destructive (`btn-outline-danger`) reserved for:**
- "Hapus link renewal" button (clear renewal FK = destructive terhadap parent-child relationship record)

**Color comparison vs AddTraining renewal-mode alert (L26-33):**
AddTraining pakai `alert alert-warning` karena renewal-mode = transient operational state (create flow). EditTraining display read-only = persistent record state (existing data), pakai card neutral lebih semantik tepat. **Jangan copy-paste `alert-warning` pattern dari AddTraining.**

---

## Component Markup Contract (Razor)

Lokasi: insert **setelah closing `</div>` card "Data Training" (L125)** dan **sebelum opening `<div class="card border-0 shadow-sm mb-4">` card "Data Sertifikat" (L127)**.

**Conditional render guard:**
```razor
@if (Model.RenewsTrainingId.HasValue || Model.RenewsSessionId.HasValue)
{
    <div id="renewalSourceSection" class="card border-0 shadow-sm mb-4">
        <div class="card-header bg-white py-3">
            <h5 class="mb-0 fw-bold">
                <i class="bi bi-arrow-repeat me-2 text-primary"></i>Renewal Source
            </h5>
        </div>
        <div class="card-body">
            <div class="row g-3 align-items-end">
                <div class="col-md-9">
                    <label class="form-label fw-bold">Renewal dari</label>
                    <p class="form-control-plaintext mb-0">
                        @(Model.RenewalSourceTitle ?? "(sertifikat sumber tidak ditemukan)")
                    </p>
                </div>
                <div class="col-md-3 text-md-end">
                    <button type="button"
                            class="btn btn-outline-danger btn-sm"
                            onclick="document.getElementById('RenewsTrainingId').value='';document.getElementById('RenewsSessionId').value='';document.getElementById('renewalSourceSection').style.display='none';return false;">
                        <i class="bi bi-x-circle me-1"></i>Hapus link renewal
                    </button>
                </div>
            </div>
            <input type="hidden" id="RenewsTrainingId" name="RenewsTrainingId" value="@Model.RenewsTrainingId" />
            <input type="hidden" id="RenewsSessionId" name="RenewsSessionId" value="@Model.RenewsSessionId" />
        </div>
    </div>
}
```

**Catatan implementasi:**
- `form-control-plaintext` = Bootstrap 5 class untuk read-only text display dengan label alignment konsisten input (parity dengan field lain di form).
- `align-items-end` di row → button align bottom dengan label+value sebelah kiri.
- `text-md-end` → button right-aligned desktop, default left-aligned mobile (Bootstrap responsive default).
- Hidden input `value="@Model.RenewsTrainingId"` — Razor null int? → empty string (acceptable, backend bind null OK).
- `return false` di onclick → prevent any default button behavior even though `type="button"` already prevents form submit (defense-in-depth).

---

## Interaction State Machine

Hanya 2 state untuk section ini:

| State | Trigger | Visual | Hidden inputs |
|-------|---------|--------|---------------|
| **Visible (initial)** | `Model.RenewsTrainingId != null \|\| Model.RenewsSessionId != null` saat GET render | Card section visible, button "Hapus link renewal" actionable | `RenewsTrainingId` + `RenewsSessionId` = nilai existing |
| **Hidden (after click)** | User click button "Hapus link renewal" | `style.display='none'` (section disappear, layout reflow) | Both hidden inputs `.value=''` (kosong) — submit time bind null backend |

**No "undo" state.** User tidak bisa restore link renewal setelah dihapus tanpa reload page (GET refresh repopulate dari DB). Acceptable per CONTEXT D-07 minimal-effort design. Kalau user salah klik, tinggal `Batal` button + reload.

**No optimistic UI / no AJAX call.** Server-side state berubah hanya saat form submit (Simpan Perubahan).

---

## Copywriting Contract

Locked verbatim — tidak boleh diubah executor tanpa user confirmation.

| Element | Copy ID-locale | Source |
|---------|----------------|--------|
| Card header heading | `Renewal Source` | English term — match domain convention (`RenewsTrainingId` field name) |
| Label | `Renewal dari` | CONTEXT D-07 verbatim |
| Empty fallback (source not found) | `(sertifikat sumber tidak ditemukan)` | CONTEXT code_context bullet GetTraining Edit lookup |
| Clear button | `Hapus link renewal` | CONTEXT D-07 verbatim |
| Field error P06 (di ValidUntil) | `Sertifikat Permanent tidak boleh punya tanggal expired.` | CONTEXT D-03 locked verbatim spec line 234 |
| Summary error P03 monotonic | `Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.` | CONTEXT D-03 locked verbatim |
| Summary error P03 self-renewal | `Sertifikat tidak boleh renewal dirinya sendiri.` | CONTEXT D-03 locked verbatim |

**Wording note "Hapus link renewal":**
- `Hapus` = consistent dengan Bahasa Indonesia destructive verb di Portal HC (cek tombol `Hapus` di ManageAssessment).
- `link renewal` = informal term akademik OK karena admin user (HC team), bukan end-worker. Alternatif lebih formal "Putuskan tautan renewal" considered tapi rejected — terlalu kaku untuk admin tooling.

**No empty state copy needed** — kalau record bukan renewal, seluruh section section tidak di-render (guard `@if`).

**No destructive confirmation modal** — clear button = soft action, user masih harus klik `Simpan Perubahan` untuk persist. Cancel via `Batal` button trivial. Modal overkill untuk 1-click reversible action.

---

## Error Display Pattern

Existing pattern reuse — tidak ada widget baru.

### P03 errors (summary-level, key=`""`)

| Pattern | Lokasi tampil |
|---------|---------------|
| `ModelState.AddModelError("", "Tanggal renewal harus...")` | Tampil di `<div asp-validation-summary="All" class="alert alert-danger">` existing **L28-31 EditTraining.cshtml** + L43-47 AddTraining.cshtml |
| `ModelState.AddModelError("", "Sertifikat tidak boleh renewal dirinya sendiri.")` | Sama — summary-level |

### P06 error (field-level, key=`"ValidUntil"`)

| Pattern | Lokasi tampil |
|---------|---------------|
| `ModelState.AddModelError("ValidUntil", "Sertifikat Permanent...")` | Tampil di `<span asp-validation-for="ValidUntil" class="text-danger small">` |

**Action needed plan-phase:** EditTraining.cshtml L153 area saat ini tidak punya `<span asp-validation-for="ValidUntil">`. **TAMBAH** span ini setelah `<input asp-for="ValidUntil">`:

```razor
<input asp-for="ValidUntil" type="date" class="form-control" />
<span asp-validation-for="ValidUntil" class="text-danger small"></span>
```

AddTraining.cshtml — confirm di plan-phase (file >100 baris belum di-scan full). Bila absent, tambah sama pattern.

**Tradeoff Add vs Edit error display (per CONTEXT code_context):**
- AddTraining: `return View(model)` → ModelState preserved → field-level span work native. P06 langsung tampil dekat input.
- EditTraining: `TempData["Error"] = firstError + RedirectToAction` → ModelState lost di redirect → P06 tampil sebagai toast (compressed firstError saja), BUKAN dekat input. **Acceptable per CONTEXT L177-178** — Edit UX convention existing.

---

## Accessibility Minimum

Bootstrap 5 + ASP.NET tag helper default sudah accessible. Phase 326 spesifik:

| Requirement | Implementation |
|-------------|----------------|
| Form label association | `<label class="form-label">` pasangan dengan input (existing pattern) |
| Hidden input bukan tab-stop | `<input type="hidden">` native skip dari tab order |
| Button explicit type | `type="button"` di clear button — prevent accidental form submit |
| Icon-only decoration | Icon `bi-arrow-repeat` + `bi-x-circle` decorative — text label always present, no `aria-hidden` needed |
| Color contrast destructive | `btn-outline-danger` Bootstrap default WCAG AA compliant |
| Focus state | Bootstrap default focus ring — no override |

**Tidak perlu:**
- ARIA live region (no async update)
- `aria-describedby` (no helper text inline)
- Skip link (existing layout sudah handle)

---

## Responsive Behavior

Bootstrap grid default. Phase 326:

| Breakpoint | Layout |
|------------|--------|
| `<768px` (mobile) | Label+value full width (col-md-9 collapse → col-12), button full width below |
| `≥768px` (md+) | 75/25 split (col-md-9 + col-md-3), button right-aligned |

Tidak ada custom media query. Tidak ada hide/show breakpoint-specific element.

---

## Registry Safety

Tidak applicable — no shadcn, no third-party component registry, no npm/NuGet baru.

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| n/a | none | not applicable — pure Razor markup + Bootstrap utility classes existing project |

**Existing dependencies leveraged:**
- Bootstrap 5 (already in `wwwroot/lib/bootstrap` — Layout shared)
- Bootstrap Icons (`bi-*` already used project-wide)

No new external CSS/JS load. Inline JS scope = 1 onclick handler, no global namespace pollution.

---

## Verification Checklist (untuk Checker/Auditor)

- [ ] **Spacing:** Section card pakai `mb-4` konsisten dengan card existing (L42, L127). Row gutter `g-3` konsisten.
- [ ] **Typography:** Card header `h5.fw-bold`, label `form-label.fw-bold`, value `form-control-plaintext`. No font-size override.
- [ ] **Color:** Card neutral (`bg-white` + default body), button `btn-outline-danger` untuk destructive intent. NO `alert-warning` (rejected — beda semantik vs AddTraining renewal-mode).
- [ ] **Copywriting:** 7 string locked verbatim per table — match byte-for-byte di kode.
- [ ] **Markup contract:** Conditional render guard `@if (Model.RenewsTrainingId.HasValue || Model.RenewsSessionId.HasValue)`. Hidden inputs id+name match handler bind. Inline onclick = single line, no multiline.
- [ ] **Error display:** P03 → summary alert (existing). P06 Add → field-level span. P06 Edit → TempData toast (acceptable existing UX).
- [ ] **Plan-phase confirm:** AddTraining.cshtml ValidUntil span exists or tambah; EditTraining.cshtml L153 area tambah span.
- [ ] **No new file:** No CSS, no JS, no Image, no NuGet, no npm.
- [ ] **Scope respected:** Total UI delta ≤ 35 baris Razor di 1 file (EditTraining.cshtml) + 1 span baris di 1 file (kemungkinan 2 file kalau AddTraining juga tambah).

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS — 7 string verbatim CONTEXT-locked
- [ ] Dimension 2 Visuals: PASS — Bootstrap card + utility, parity existing sections
- [ ] Dimension 3 Color: PASS — Bootstrap contextual only, accent reserved-for explicit
- [ ] Dimension 4 Typography: PASS — Bootstrap default, no custom scale
- [ ] Dimension 5 Spacing: PASS — Bootstrap 8px-based utility (mb-2/3/4, g-3, py-3)
- [ ] Dimension 6 Registry Safety: PASS — N/A, no new dependency

**Approval:** pending (awaits gsd-ui-checker)

---

## Open Items for Plan-Phase / Executor

1. **AddTraining.cshtml ValidUntil span existence** — confirm via Grep `asp-validation-for="ValidUntil"` di Views/Admin/AddTraining.cshtml. Bila absent, tambah pattern sama.
2. **GetTraining Edit handler `RenewalSourceTitle` populate** — backend concern (bukan UI), tapi UI section dependent. Plan-phase pastikan VM field di-populate sebelum view render, atau fallback string `(sertifikat sumber tidak ditemukan)` tampil.
3. **Manual UAT SC-6 (CONTEXT D-09)** — verifikasi visual: section render kalau renewal record, button click hide section, submit → backend null clear. Browser lokal `http://localhost:5277/Admin/EditTraining/{id}` untuk record yang IS renewal.
