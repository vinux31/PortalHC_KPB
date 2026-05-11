# Phase 308: PrePost Wizard Validation Fix - Research

**Researched:** 2026-04-29
**Domain:** ASP.NET Core MVC wizard validation (server `ModelState` + client custom JS validation)
**Confidence:** HIGH (semua line numbers verified via grep, semua claim di-cross-check via Read tool)

## Summary

Phase 308 menyelesaikan bug REQ WIZ-04 (Audit Temuan 11): saat Admin/HC submit assessment Pre-Post Test, server-side `[Required] Status` di model `AssessmentSession` memicu `ModelState` invalid → controller `return View(model)` (line 940) → view re-render fresh → init JS `goToStep(1)` (line 1458) → wizard reset ke Step 1 dengan TempData kosong, sehingga user kehilangan input dan kebingungan (Status field di-hide untuk PrePost mode tapi server tetap minta value).

Fix two-layer:
1. **Client (`Views/Admin/CreateAssessment.cshtml`)** — extend existing JS handler `change` di line 1872-1889 (`Pre-Post Test mode toggle — Phase 297`) untuk: (a) saat `value === 'PrePostTest'` set `Status='Upcoming'`, (b) saat switch back Standard clear `Status=''`. Plus optional re-parse trigger.
2. **Server (`Controllers/AssessmentAdminController.cs`)** — tambah `if (isPrePostMode) ModelState.Remove("Status")` setelah line 779, mirror pattern existing `ModelState.Remove("UserId")` (line 742) dan `ModelState.Remove("AccessToken")` (line 756).

**Primary recommendation:** Anchor JS edit di **line 1876** (`if (this.value === 'PrePostTest')`) di blok handler line 1872-1889 — BUKAN line 1790-1807 (ROADMAP refs sudah stale). Anchor server edit di **line 780** (setelah line 779 `bool isPrePostMode`). Form ID adalah **`#createAssessmentForm`** (BUKAN `#createForm` seperti tertulis di CONTEXT D-07/CD-01 — ini MUST-FIX di plan).

**TEMUAN KRITIS YANG MEMENGARUHI CONTEXT D-07/D-08/D-09:** Form ini `novalidate` (line 102) dan **TIDAK** include `_ValidationScriptsPartial`. Artinya jQuery validate plugin tidak aktif di form ini. Validation murni custom JS via `validateStep(n)` (line 902) — yang sudah punya guard `if (!statusWrapper.classList.contains('d-none'))` di line 998. Bagian "jQuery validate re-parse" di ROADMAP success criteria #3 dan CONTEXT D-07 sebenarnya **tidak applicable** — fix utama tetap server-side `ModelState.Remove` + client value assignment. Re-parse jQuery hanya menjadi defensive no-op (atau di-skip total).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|--------------|----------------|-----------|
| Status default value untuk PrePost mode | Browser (JS) | API (controller fallback) | UX immediacy: set value saat user pilih, sebelum submit |
| Skip Status validation server-side untuk PrePost | API (controller `ModelState.Remove`) | — | Server adalah authoritative validator (`[Required]` attribute di model) |
| Status field show/hide visual | Browser (JS `classList.toggle('d-none')`) | — | Pure UI behavior, tidak perlu round-trip server |
| Mode-switch state preservation (S↔PP) | Browser (JS) | — | Form-level interaction, instan tanpa server |
| Wizard step navigation post-error | Browser (JS init `goToStep(1)`) | — | Client owns wizard state |
| Regression: Standard mode tetap require Status | Browser (custom JS validateStep line 996-1004) + API (model `[Required]`) | — | Defense-in-depth — client UX + server enforcement |

## Standard Stack

### Core (existing — TIDAK ADA library/package baru di Phase 308)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 9.0 (project target) | Server-side rendering + `ModelState` validation | Existing stack — `[CITED: Project STACK]` |
| Vanilla JS (ES5 style) | — | Client-side wizard logic + validation | Existing pattern di `CreateAssessment.cshtml` (1957 lines, ES5 `var` style) `[VERIFIED: file read]` |
| Bootstrap 5.3 | 5.3.x | UI components (form-select, classList API) | Existing stack `[CITED: STACK.md]` |

### Supporting (NOT NEEDED — relevan hanya untuk verifikasi negatif)
| Library | Version | Purpose | Status |
|---------|---------|---------|--------|
| jQuery | 3.7.1 (CDN) | Loaded oleh `_Layout.cshtml` line 230 | Available di global scope `[VERIFIED: grep _Layout.cshtml]` |
| jquery.validate | — | Tidak loaded di CreateAssessment.cshtml | `_ValidationScriptsPartial` TIDAK di-include `[VERIFIED: grep CreateAssessment.cshtml — 0 matches untuk _ValidationScriptsPartial]` |
| jquery.validate.unobtrusive | — | Tidak loaded | Sama — tidak di-include `[VERIFIED]` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `ModelState.Remove("Status")` | Conditional `[ValidateNever]` attribute via `if/else ViewModel` | Lebih invasif (butuh 2 ViewModel atau custom binder); pattern `Remove` sudah established di file (3 instance: line 742, 756, 821, 835, 870) |
| Set `Status='Upcoming'` di JS | Set via `Schedule.cshtml` partial conditional value | Markup-time decision tidak bisa react ke runtime mode-switch; JS handler reactive lebih flexible |
| Hidden input `<input type="hidden" name="Status" value="Upcoming">` saat PrePost | DOM swap via JS | Lebih risky: existing `<select>` tetap submit empty value bersamaan dengan hidden, model binding ambiguous |

**Installation:** Tidak ada — phase ini zero new dependencies.

**Version verification:** Tidak applicable — tidak ada package baru.

## Architecture Patterns

### System Architecture Diagram (Submit Flow PrePost — Current vs Fixed)

```
CURRENT FLOW (BUG):
┌──────────────────────────────────────────────────────────────────┐
│ User: Step 1 → Pilih "Pre-Post Test" → Step 2/3/4 → Submit       │
│                          │                                        │
│   JS handler line 1876:  ↓                                        │
│   - statusWrapper.classList.add('d-none')   [✓ hide visual]      │
│   - Status <select> value tetap "" (default)                     │
│                          │                                        │
│   POST CreateAssessment  ↓                                        │
│   - line 779: isPrePostMode = true                               │
│   - line 914: ModelState.IsValid → FALSE (Status [Required])     │
│   - line 940: return View(model)                                  │
│                          │                                        │
│   View re-render         ↓                                        │
│   - line 1458: goToStep(1)  ← USER LOSES PROGRESS               │
│   - asp-validation-summary tampil "Status field is required"     │
│   - User bingung: Status sudah hidden, kok masih diminta?        │
└──────────────────────────────────────────────────────────────────┘

FIXED FLOW (Phase 308):
┌──────────────────────────────────────────────────────────────────┐
│ User: Step 1 → Pilih "Pre-Post Test" → ...                       │
│                          │                                        │
│   JS handler line 1876   ↓                                        │
│   [D-01] + statusEl.value = 'Upcoming'                           │
│   [D-09 defensive] re-parse no-op (jQuery validate not loaded)   │
│                          │                                        │
│   POST CreateAssessment  ↓                                        │
│   - line 779: isPrePostMode = true                               │
│   [D-04] line 780: ModelState.Remove("Status")                   │
│   - line 914: ModelState.IsValid → TRUE                          │
│   - Continue to session creation (line 1078: Status="Upcoming")  │
│                          │                                        │
│   Success                ↓                                        │
│   - TempData["SuccessMessage"] = "Assessment Pre-Post berhasil"  │
│   - RedirectToAction or View dengan TempData                     │
└──────────────────────────────────────────────────────────────────┘

REGRESSION GUARD (Standard mode tetap):
┌──────────────────────────────────────────────────────────────────┐
│ User: Step 1 → Pilih "Standard" (default) → ...Submit tanpa Status │
│                          │                                        │
│   POST CreateAssessment  ↓                                        │
│   - line 779: isPrePostMode = false                              │
│   - line 780: skip ModelState.Remove("Status") [conditional]     │
│   - line 914: ModelState.IsValid → FALSE (Status [Required])     │
│   - line 940: return View → "Status wajib dipilih" tampil       │
│                                                                   │
│   Behavior unchanged dari pre-Phase 308 (REQ #5 satisfied)       │
└──────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | File | Lines (post-Phase 307) | Phase 308 Action |
|-----------|------|-----------------------|------------------|
| `<select asp-for="Status" id="Status">` markup | `Views/Admin/CreateAssessment.cshtml` | 481-485 | UNCHANGED (D-12) |
| `#statusFieldWrapper` div container | `Views/Admin/CreateAssessment.cshtml` | 479 | UNCHANGED |
| Pre-Post Test mode toggle handler | `Views/Admin/CreateAssessment.cshtml` | 1860-1904 (entire IIFE-less block) | EDIT: tambah value assignment di branch 1876-1888 |
| `validateStep(3)` Status visibility guard | `Views/Admin/CreateAssessment.cshtml` | 996-1004 | UNCHANGED (sudah handle hide case) |
| `bool isPrePostMode` declaration | `Controllers/AssessmentAdminController.cs` | 779 | UNCHANGED |
| ModelState validation block | `Controllers/AssessmentAdminController.cs` | 779-878 | INSERT setelah 779: `ModelState.Remove("Status")` conditional |
| Default fallback `Status="Open"` | `Controllers/AssessmentAdminController.cs` | 975-978 | UNCHANGED (D-06 — defensive Standard fallback) |
| PrePost session creation `Status="Upcoming"` | `Controllers/AssessmentAdminController.cs` | 1078, 1112, 1170, 1644, 1663 | UNCHANGED (already correct) |
| Phase 307 helpers (`renderSelectedParticipants` etc) | `Views/Admin/CreateAssessment.cshtml` | 1469-1614 | UNTOUCHED (D-17 — no overlap) |

### Recommended Project Structure
Tidak ada perubahan struktur — Phase 308 single-file edits ke 2 file existing:
```
Views/Admin/
└── CreateAssessment.cshtml      # 1 markup-adjacent JS edit di blok handler line 1860-1889
Controllers/
└── AssessmentAdminController.cs # 1 insert 4-line block setelah line 779
tests/e2e/
├── assessment.spec.ts           # APPEND: FLOW 8 describe block (4 test cases 8.1-8.4)
└── helpers/wizardSelectors.ts   # APPEND: 4 selector baru untuk Phase 308
.planning/phases/308-prepost-wizard-validation-fix/
├── 308-CONTEXT.md               # exists
├── 308-DISCUSSION-LOG.md        # exists
├── 308-RESEARCH.md              # this file
└── 308-UAT.md                   # to be created Wave 0
```

### Pattern 1: Conditional `ModelState.Remove` for context-dependent fields

**What:** Skip server-side `[Required]` validation untuk field yang tidak applicable di mode tertentu, dengan eksplisit `ModelState.Remove(propertyName)` setelah deteksi mode.

**When to use:** Single ViewModel handle multiple submit modes; mode-specific fields punya `[Required]` di model class tapi optional di mode lain. Pattern existing di `CreateAssessment` POST handler:

| Line | ModelState.Remove | Trigger | Pattern |
|------|-------------------|---------|---------|
| 742 | `"UserId"` | Always (model uses `UserIds` list) | Unconditional remove |
| 756 | `"AccessToken"` | `if (!model.IsTokenRequired)` | Conditional remove |
| 821 | `"ExamWindowCloseDate"` | Always (re-validate manually below) | Unconditional remove + re-add |
| 835 | `"ValidUntil"` | Always (re-validate manually below) | Unconditional remove + re-add |
| 870 | `"NomorSertifikat"` | Always (auto-generated) | Unconditional remove |

**Phase 308 sample (D-04, D-05) — VERIFIED ANCHOR:**
```csharp
// Source: Controllers/AssessmentAdminController.cs line 778-782 (verified via grep 2026-04-29)
// Early Pre-Post mode determination (needed before standard field validation)
bool isPrePostMode = AssessmentTypeInput == "PrePostTest";

// === PHASE 308 INSERT (after line 779, before line 781 blank) ===
if (isPrePostMode)
{
    // Status field hidden in PrePost mode — JS sets default 'Upcoming'
    ModelState.Remove("Status");
}
// === END PHASE 308 INSERT ===

// Validate schedule date (skip for Pre-Post — uses PreSchedule/PostSchedule instead)
if (!isPrePostMode)
{
    if (model.Schedule < DateTime.Today)
    ...
```

**Why this position:** Setelah `isPrePostMode` flag set, sebelum schedule validation block. Mirror existing `ModelState.Remove("AccessToken")` (line 756) yang juga conditional dan terjadi awal di method.

### Pattern 2: Existing PrePost mode toggle JS handler (extend, jangan refactor)

**What:** Vanilla JS event listener yang sudah ada di blok `Pre-Post Test mode toggle — Phase 297` untuk show/hide section + status wrapper.

**Verified anchor (post-Phase 307):**
```javascript
// Source: Views/Admin/CreateAssessment.cshtml line 1860-1904 (verified via grep 2026-04-29)
// Pre-Post Test mode toggle — Phase 297
document.addEventListener('DOMContentLoaded', function() {
    var typeSelect = document.getElementById('assessmentTypeInput');
    var pptSection = document.getElementById('ppt-jadwal-section');
    var stdSection = document.getElementById('standard-jadwal-section');
    var preScheduleInput = document.getElementById('preSchedule');
    var postScheduleInput = document.getElementById('postSchedule');
    var samePackageCheck = document.getElementById('samePackageCheck');
    var samePackageBadge = document.getElementById('samePackageBadge');

    if (!typeSelect) return;

    typeSelect.addEventListener('change', function() {
        var statusWrapper = document.getElementById('statusFieldWrapper');
        var certNote = document.getElementById('prePostCertNote');

        if (this.value === 'PrePostTest') {                              // ← line 1876
            pptSection.classList.add('show');
            if (stdSection) stdSection.classList.add('d-none');
            // Hide Status dropdown (Pre-Post always Upcoming)
            if (statusWrapper) statusWrapper.classList.add('d-none');
            // Show cert note
            if (certNote) certNote.classList.remove('d-none');
            // === PHASE 308 INSERT (D-01) ===
            // var statusEl = document.getElementById('Status');
            // if (statusEl) statusEl.value = 'Upcoming';
            // === END PHASE 308 INSERT ===
        } else {                                                          // ← line 1883
            pptSection.classList.remove('show');
            if (stdSection) stdSection.classList.remove('d-none');
            if (statusWrapper) statusWrapper.classList.remove('d-none');
            if (certNote) certNote.classList.add('d-none');
            // === PHASE 308 INSERT (D-02) ===
            // var statusEl = document.getElementById('Status');
            // if (statusEl) statusEl.value = '';
            // === END PHASE 308 INSERT ===
        }
    });
    // ...rest unchanged
});
```

**Why extend (not new handler):** Single source of truth — semua side-effects mode-switch terkonsentrasi di satu listener. Reduce risk listener-order bugs. Match D-03 CONTEXT.

### Anti-Patterns to Avoid

- **Anti-pattern: Tambah handler baru `typeSelect.addEventListener('change', ...)` terpisah** — listener-order tidak deterministic, pisahnya logic membuat debugging susah. **Use instead:** extend existing handler line 1876-1888.
- **Anti-pattern: Hide `<select>` via JS attribute remove + add hidden input duplicate** — model binding ambiguous (dua input `name="Status"` submit). **Use instead:** value assignment ke existing `<select>` element.
- **Anti-pattern: Set `[Required]` jadi conditional via `[RequiredIf]` custom attribute** — invasive di model layer, butuh nuget package atau custom validator. **Use instead:** runtime `ModelState.Remove` mirror pattern existing.
- **Anti-pattern: Trigger `$.validator.unobtrusive.parse()` tanpa cek availability** — di file ini akan throw `TypeError: Cannot read property 'parse' of undefined` karena plugin tidak loaded. **Use instead:** `if (typeof $ !== 'undefined' && $.validator && $.validator.unobtrusive)` defensive guard, atau **skip total** karena plugin tidak applicable di form ini.
- **Anti-pattern: Edit ROADMAP refs line 1790-1807 verbatim** — line numbers stale post-Phase 307 (+47 net lines). **Use instead:** grep anchor `if (this.value === 'PrePostTest')` (line 1876).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Conditional model validation | Custom `[RequiredIf]` attribute | Runtime `ModelState.Remove(prop)` mirror pattern existing | 5 instance pattern existing di file (line 742, 756, 821, 835, 870) — established convention |
| Default Status value | New ViewModel field/computed property | Direct assignment di JS handler `.value = 'Upcoming'` | Match success criteria #1 verbatim; konsisten dengan server line 1078/1112/1170 |
| Mode-switch state machine | Generic state machine library (XState dll) | Existing 2-branch `if/else` di handler line 1876-1888 | YAGNI — 2 mode (Standard/PrePost) tidak justify state machine |
| Validation re-parse | jQuery validate plugin | Sudah ada custom `validateStep(n)` line 902 dengan visibility guard line 996-1004 | Plugin tidak loaded di form ini; custom validator sudah handle hidden Status case |
| Test scaffolding | New framework | Playwright (existing tests/e2e/assessment.spec.ts) | Phase 307 precedent — extend FLOW 8 describe block, mirror 8 selector pattern |

**Key insight:** Phase 308 bukan refactor — ini surgical 2-spot edit. Setiap "do less" decision (CONTEXT D-12 tidak ubah markup, D-06 default fallback unchanged, D-17 tidak touch Phase 307 helpers) menurunkan regression risk. Re-grep anchors WAJIB sebelum execute.

## Verified Line Numbers (post-Phase 307)

**Verifikasi via grep 2026-04-29 — file `Views/Admin/CreateAssessment.cshtml` total 1957 lines, `Controllers/AssessmentAdminController.cs` total 5137 lines.**

### CreateAssessment.cshtml (Views/Admin/)

| Anchor | Line | Verified Pattern | Phase 308 Action |
|--------|------|------------------|------------------|
| `<form id="createAssessmentForm" novalidate>` | 102 | `grep -n "id=\"createAssessmentForm\""` → 1 match | Reference only — form ID = `#createAssessmentForm` (NOT `#createForm`) |
| `<div ... id="statusFieldWrapper">` | 479 | `grep -n "statusFieldWrapper"` → 3 matches (479, 997, 1873) | Reference (markup unchanged D-12) |
| `<select asp-for="Status" id="Status">` | 481 | Within statusFieldWrapper block | Reference (markup unchanged D-12) |
| `validateStep` function with `statusFieldWrapper` visibility check | 902, 996-1004 | `grep -n "function validateStep"` → 1; visibility guard exists | Reference (UNCHANGED — sudah skip Status saat hidden) |
| `if (this.value === 'PrePostTest')` (PrePost mode toggle handler) | **1876** | `grep -n "value === 'PrePostTest'"` → 4 matches (952, 1079, 1442, **1876**) | **PRIMARY ANCHOR** for D-01 (insert after line 1882 inside `if` branch) |
| `else` clause of mode toggle | **1883** | Within same handler block | **PRIMARY ANCHOR** for D-02 (insert after line 1887 inside `else` branch) |
| `var statusWrapper = document.getElementById('statusFieldWrapper')` | 1873 | Inside DOMContentLoaded handler | Reference — Status element fetch nearby |
| `goToStep(1)` init call | 1458 | Final line of WizardController IIFE | Reference (root cause: re-render reset) |
| Phase 307 helpers (top-level scope) | 1469-1614 | `grep -n "function renderSelectedParticipants"` → 1 match (1469) | UNTOUCHED (D-17 — no overlap with Phase 308) |
| `@section Scripts {` | 815 | `grep -n "@section Scripts"` → 1 match | Reference for script block boundary |

**ROADMAP refs comparison:**

| ROADMAP ref (stale) | Verified post-Phase 307 | Delta | Notes |
|---------------------|-------------------------|-------|-------|
| 1790-1807 (JS handler) | **1872-1889** (handler block) / **1876** (anchor `value === 'PrePostTest'`) | **+82 lines** | ROADMAP off by ~+82 (Phase 297 added handler at original ~1790, Phase 304/307 expanded) |
| 778 (controller) | **779** | +1 | Controller not touched by Phase 304/307 — minimal drift |

### AssessmentAdminController.cs (Controllers/)

| Anchor | Line | Verified Pattern | Phase 308 Action |
|--------|------|------------------|------------------|
| `ModelState.Remove("UserId")` | 742 | `grep -n "ModelState.Remove"` → 5 matches (742, 756, 821, 835, 870) | Reference pattern for D-04 |
| `ModelState.Remove("AccessToken")` | 756 | Inside `else` of `if (model.IsTokenRequired)` | Reference pattern (conditional remove) |
| `bool isPrePostMode = AssessmentTypeInput == "PrePostTest"` | **779** | `grep -n "isPrePostMode = AssessmentTypeInput"` → 1 match | **PRIMARY ANCHOR** for D-04 (insert after line 779) |
| `if (!isPrePostMode)` schedule validation | 782 | First conditional after `isPrePostMode` declaration | Reference — D-04 insert WAJIB sebelum line 782 |
| `if (string.IsNullOrEmpty(model.Status)) model.Status = "Open"` (defensive default) | 975-978 | `grep -n "string.IsNullOrEmpty(model.Status)"` → 1 match | UNCHANGED (D-06) |
| PrePost session creation `Status = "Upcoming"` | 1078, 1112, 1170, 1644, 1663 | `grep -n "Status = \"Upcoming\""` → 5 matches | UNCHANGED (already correct — confirms 'Upcoming' is canonical) |
| `if (!ModelState.IsValid) ... return View(model)` | 914-940 | Where reset-to-Step-1 originates | Reference (root cause downstream) |

## Pitfalls + Mitigations

### Pitfall 1: Form ID mismatch — `#createForm` vs `#createAssessmentForm`

**What goes wrong:** CONTEXT D-07/CD-01 menulis `var $form = $('#createForm')`. Form aktualnya ber-ID `createAssessmentForm` (verified line 102).
**Why it happens:** ROADMAP / CONTEXT mengasumsikan generic naming; planner tidak grep verifikasi.
**How to avoid:** Plan WAJIB pakai `#createAssessmentForm` di setiap selector reference. **Atau lebih baik: skip jQuery re-parse total** karena tidak applicable (lihat Pitfall 2).
**Warning signs:** `$('#createForm').length === 0` di console saat handler fire — silent no-op, bug tidak terdeteksi sampai user lapor.
**Confidence:** HIGH `[VERIFIED: grep "id=\"create" Views/Admin/CreateAssessment.cshtml]`

### Pitfall 2: jQuery validate plugin tidak loaded di form ini — re-parse useless

**What goes wrong:** ROADMAP success criteria #3 menulis "jQuery validate re-parse setelah dynamic show/hide statusFieldWrapper". CONTEXT D-07 menulis call `$.validator.unobtrusive.parse($form)`. Plugin tidak available di form ini — call akan throw `TypeError: Cannot read property 'unobtrusive' of undefined` kalau tanpa guard, atau silent no-op kalau dengan guard.
**Why it happens:** Form pakai `novalidate` (line 102) + custom `validateStep(n)` (line 902); `_ValidationScriptsPartial` (yang load `jquery.validate.min.js` + `jquery.validate.unobtrusive.min.js`) TIDAK di-include di view ini. Validation 100% custom JS.
**How to avoid:** **Pilihan A (recommended):** Skip jQuery re-parse — tidak ada efek functional karena plugin tidak ada. **Pilihan B (defensive guard):** Tetap include re-parse code dengan availability check, hilangkan kalau code review minta clean. Plan bisa pilih sesuai discretion — Pilihan A lebih jujur (D-09 defensive guard akan jadi dead code).
**Warning signs:** `console.log(typeof $.validator)` di handler returns `'undefined'` — re-parse tidak melakukan apa-apa.
**Confidence:** HIGH `[VERIFIED: grep "_ValidationScriptsPartial" Views/Admin/CreateAssessment.cshtml → 0 matches; grep "jquery.validate" → 0 matches]`

**Implication for CONTEXT D-07/D-08/D-09:** Decisions ini built on incorrect assumption. Plan-phase WAJIB resolve:
- D-07 (re-parse strategy): **drop** atau ubah jadi "no-op defensive comment"
- D-08 (re-parse timing): **moot** kalau D-07 drop
- D-09 (defensive guard `if typeof $.validator`): WAJIB include kalau D-07 retained — guard mencegah error meskipun re-parse no-op

### Pitfall 3: ROADMAP refs line 1790-1807 stale post-Phase 307

**What goes wrong:** Phase 307 added +47 net lines ke CreateAssessment.cshtml. ROADMAP success criteria #1 cite line 1790-1807 untuk JS handler. Aktualnya handler ada di **1872-1889** (delta +82 lines, lebih besar dari +47 karena Phase 304 juga menambah lines di area ini).
**Why it happens:** ROADMAP authored sebelum Phase 304 dan Phase 307 merge. Refs tidak auto-update saat phases land.
**How to avoid:** Researcher (sudah dilakukan) dan Planner (next) WAJIB grep ulang anchor pattern, bukan trust line numbers. Use anchor patterns di table "Verified Line Numbers" di atas.
**Warning signs:** Edit dengan line refs ROADMAP → kena ke kode yang salah → bug introduced atau no-op.
**Confidence:** HIGH `[VERIFIED: grep "value === 'PrePostTest'" → 4 matches at 952, 1079, 1442, 1876; handler block 1860-1904 verified via Read]`

### Pitfall 4: Mode-switch overwrites user-picked Status (intentional per D-02)

**What goes wrong:** Per D-01, JS set `Status = 'Upcoming'` saat user pilih PrePost. Kalau user sebelumnya pick "Open" di Standard mode lalu switch PrePost — value 'Open' di-overwrite ke 'Upcoming'. Ini intentional (mode-switch reset state) tapi UX-wise harus eksplisit di UAT.
**Why it happens:** Single source of truth: PrePost = Upcoming (server line 1078/1112/1170 sudah set begitu). Tidak ada nilai 'Open' yang valid untuk PrePost karena status dropdown hidden.
**How to avoid:** Tidak perlu avoid — ini correct behavior. UAT Step 4 (PP→S→PP) explicit verify auto-set Upcoming setelah switch back. UAT.md script cantumkan note ini.
**Warning signs:** User komplain "saya pilih Open kok jadi Upcoming setelah switch" — jawab: mode-switch reset state intentional.
**Confidence:** HIGH (per D-02 CONTEXT auto-resolved)

### Pitfall 5: Stale ModelState carryover dalam single request

**What goes wrong:** Hipotesis: kalau user submit PrePost dengan PreSchedule error, lalu controller `return View(model)`, lalu JS reset to Step 1, lalu user switch ke Standard dan resubmit — apakah `ModelState.Remove("Status")` dari request sebelumnya carry over?
**Why it happens:** ASP.NET Core MVC `ModelState` per-request — tidak ada residue antar request. Setiap POST membuat fresh `ModelState` via model binding.
**How to avoid:** Tidak ada masalah aktual. Hipotesis tidak valid — `ModelState` lifecycle bound ke `HttpContext` instance.
**Warning signs:** Tidak applicable.
**Confidence:** HIGH `[CITED: Microsoft ASP.NET Core MVC docs — ModelState lifecycle per-request]`

### Pitfall 6: Programmatic value assignment tidak fire `change` event

**What goes wrong:** `statusEl.value = 'Upcoming'` di JS tidak fire `change` event di Status select. Kalau ada listener di `#Status` yang tergantung `change`, tidak akan trigger.
**Why it happens:** DOM API: assignment via `.value` adalah passive update, browser tidak emit `change` (mengikuti spec, hanya UI interaction yang fire).
**How to avoid:** Verify tidak ada listener `change` di `#Status` element via `grep "Status'?\\)\\.addEventListener\\('change'"`. Hasil verifikasi: tidak ada listener spesifik di `#Status` (hanya pakai validateStep manual). Aman tanpa dispatch.
**Warning signs:** Future code yang attach `change` listener ke `#Status` akan miss programmatic update — prevent dengan code comment dekat assignment.
**Confidence:** HIGH `[VERIFIED: grep "Status').addEventListener\\|Status\").addEventListener" Views/Admin/CreateAssessment.cshtml → 0 matches for #Status specific; hanya general typeSelect di line 1872]`

### Pitfall 7: First-load mode-switch tidak fire (page load default Standard)

**What goes wrong:** Saat page first load, `assessmentTypeInput` default `''` (empty) atau `'Standard'`. JS handler hanya fire saat user `change` — kalau user submit langsung tanpa pilih tipe, branch `if (this.value === 'PrePostTest')` tidak pernah eksekusi. PrePost selection memerlukan eksplisit user action (click), so D-01 selalu fire saat user pilih PrePost. Aman.
**Why it happens:** `change` event semantics — only fire saat value berubah by UI interaction.
**How to avoid:** Tidak applicable. PrePost mode hanya bisa di-pilih via `change` interaction, jadi D-01 path selalu execute.
**Warning signs:** Tidak applicable.
**Confidence:** HIGH

## Code Examples

### Example 1: D-01 + D-02 client value assignment (D-09 defensive guard MERGED)

```javascript
// Source: Views/Admin/CreateAssessment.cshtml line 1872-1889 (verified 2026-04-29)
// PHASE 308 EDIT: extend existing typeSelect change handler

typeSelect.addEventListener('change', function() {
    var statusWrapper = document.getElementById('statusFieldWrapper');
    var certNote = document.getElementById('prePostCertNote');
    var statusEl = document.getElementById('Status');                // [PHASE 308 add]

    if (this.value === 'PrePostTest') {
        pptSection.classList.add('show');
        if (stdSection) stdSection.classList.add('d-none');
        if (statusWrapper) statusWrapper.classList.add('d-none');
        if (certNote) certNote.classList.remove('d-none');
        if (statusEl) statusEl.value = 'Upcoming';                   // [PHASE 308 D-01]
    } else {
        pptSection.classList.remove('show');
        if (stdSection) stdSection.classList.remove('d-none');
        if (statusWrapper) statusWrapper.classList.remove('d-none');
        if (certNote) certNote.classList.add('d-none');
        if (statusEl) statusEl.value = '';                           // [PHASE 308 D-02]
    }
});
```

**Mengapa pattern ini:**
- `if (statusEl)` defensive: paralel dengan existing `if (statusWrapper)` / `if (certNote)` style di handler — konsisten ES5 vanilla
- Single `statusEl` lookup di awal handler — DRY, satu `getElementById` call per change event
- Tidak panggil `dispatchEvent('change')` — sesuai Pitfall 6 (no listener di `#Status` butuh notifikasi)
- jQuery re-parse di-skip total per Pitfall 2 — kalau planner pilih retain D-09 defensive, append snippet di Example 2

### Example 2: D-09 defensive jQuery re-parse (OPTIONAL — recommend SKIP per Pitfall 2)

```javascript
// OPTIONAL — only if planner decides to retain D-07/D-09 from CONTEXT
// Will be no-op since jquery-validate plugin tidak loaded di form ini.
// Safe karena defensive guard mencegah TypeError.

// Append after value assignment (di kedua branch atau outside if/else)
if (typeof $ !== 'undefined' && $.validator && $.validator.unobtrusive) {
    var $form = $('#createAssessmentForm');                          // CORRECT form ID
    if ($form.length) {
        $form.removeData('validator').removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse($form);
    }
}
```

**Recommendation:** SKIP block ini di Wave 1. ROADMAP success criteria #3 wording bisa di-document sebagai "tidak applicable / superseded by ModelState.Remove + custom validateStep guard". Plan-phase memutuskan final.

### Example 3: D-04 + D-05 server ModelState.Remove

```csharp
// Source: Controllers/AssessmentAdminController.cs line 778-792 (verified 2026-04-29)
// PHASE 308 EDIT: insert 4-line block after line 779

// Early Pre-Post mode determination (needed before standard field validation)
bool isPrePostMode = AssessmentTypeInput == "PrePostTest";

// === PHASE 308 INSERT START ===
if (isPrePostMode)
{
    // Status field hidden in PrePost mode — JS sets default 'Upcoming'
    ModelState.Remove("Status");
}
// === PHASE 308 INSERT END ===

// Validate schedule date (skip for Pre-Post — uses PreSchedule/PostSchedule instead)
if (!isPrePostMode)
{
    if (model.Schedule < DateTime.Today)
    {
        ModelState.AddModelError("Schedule", "Schedule date cannot be in the past.");
    }
    // ...rest unchanged
}
```

**Mengapa pattern ini:**
- Insertion antara line 779 dan 782 (sebelum block schedule validation conditional) — `isPrePostMode` flag baru tersedia
- Mirror existing `ModelState.Remove("AccessToken")` (line 756) yang juga conditional
- Comment style match CONTEXT CD-02 — explicit explain why field di-remove
- Tidak set `model.Status = "Upcoming"` server-side karena: (a) JS sudah set, (b) line 975-978 defensive fallback ke "Open" akan kena untuk Standard fallback path saja, (c) line 1078/1112/1170 explicit set "Upcoming" di session creation. Triple defense.

### Example 4: FLOW 8 test scaffold (Wave 0 — Phase 307 pattern reference)

```typescript
// Source: tests/e2e/assessment.spec.ts (extend after FLOW 7 line 174 region)
// Phase 308 — D-13 4 test cases mirror Phase 307 Wave 0 RED scaffold

// ============================================================
// FLOW 8: Phase 308 — PrePost Wizard Validation Fix
// REQ: WIZ-04 (5 success criteria, 4-combination test matrix per D-10)
// ============================================================
test.describe('Assessment - Phase 308 PrePost Wizard Validation', () => {

  test('8.1 - Standard saja submit sukses (regression guard #5)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    // ...fill Step 1 default Standard, Step 2 picks, Step 3 fields including Status='Open', Step 4 submit
    // Expect: success TempData, NO "Status field is required"
  });

  test('8.2 - Switch S→PP→S submit sukses (mode-switch state cleanup D-02)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    // Step 1 default Standard → switch ke PrePost → switch back Standard
    // Verify Status field re-shown empty (D-02 clear)
    // Fill Status='Open', submit → success
  });

  test('8.3 - PP saja submit sukses (D-01 + D-04 main path)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    // Step 1 pilih PrePostTest, Step 2 picks, Step 3 fill PreSchedule + PostSchedule + durations + EWCD
    // Verify statusFieldWrapper hidden (d-none), submit
    // Expect: success, NO "Status field is required", NO reset to Step 1
  });

  test('8.4 - Switch PP→S→PP submit sukses (idempotency D-01 re-fire)', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/CreateAssessment');
    // PrePost → switch Standard → switch back PrePost
    // Verify Status auto-set 'Upcoming' (D-01 re-fire), wrapper hidden
    // Fill Pre/Post fields, submit → success
  });

});
```

**Selectors extension (`tests/e2e/helpers/wizardSelectors.ts` — APPEND, NOT REPLACE):**
```typescript
// Phase 308 selectors — extend existing wizardSelectors module (DRY per Phase 307 D-15)
export const selectors = {
  // ...existing 8 selectors UNCHANGED...

  // Phase 308 additions (verified 2026-04-29)
  assessmentTypeInput: '#assessmentTypeInput',
  statusFieldWrapper: '#statusFieldWrapper',
  statusSelect: '#Status',                  // alias 'statusSelect' to disambiguate dari any 'status' state
  submitBtn: '#createAssessmentForm button[type="submit"]',
  // form selector confirmation (CD-01 resolved):
  createForm: '#createAssessmentForm',      // CORRECT id, not '#createForm'
} as const;
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `[Required]` on every model property + serve same form | Conditional `ModelState.Remove(prop)` for mode-specific fields | ASP.NET Core 2.0+ pattern | Allows single ViewModel for multi-mode forms without `[RequiredIf]` packages |
| `jquery.validate.unobtrusive.parse()` re-parse cycle | Custom JS `validateStep(n)` with visibility-aware guards | Phase 297+ refactor di project ini | No external dependency, full control over wizard step semantics |
| Line-number-based code refs (ROADMAP) | Anchor-pattern grep refs (Phase 308 RESEARCH.md) | Phase 307 SUMMARY noted line drift | Refs survive code shifts; planner re-grep mandatory |

**Deprecated/outdated:**
- ROADMAP line refs 1790-1807 untuk JS handler — **stale** post-Phase 307. Replaced dengan grep anchor `value === 'PrePostTest'` (line 1876).
- CONTEXT D-07 form selector `#createForm` — **wrong**. Replaced dengan verified `#createAssessmentForm`.
- CONTEXT D-07 jQuery validate re-parse — **conceptually misapplied** (plugin tidak loaded). Replaced dengan "skip total" recommendation (Pilihan A) atau defensive guard (Pilihan B).

## Project Constraints (from CLAUDE.md)

Direktif dari `./CLAUDE.md`:

| Constraint | Phase 308 Compliance |
|------------|----------------------|
| Always respond in Bahasa Indonesia | UAT.md script + test names + comments di code WAJIB Bahasa Indonesia. Plan-phase deliverables (308-01-PLAN.md, 308-02-PLAN.md) Bahasa Indonesia. |

## Runtime State Inventory

> Tidak applicable — Phase 308 tidak melibatkan rename, refactor, migration, atau string replacement. Phase ini adalah surgical bug-fix di 2 file source (zero data migration, zero schema change, zero string rename).

**Categories check:**
- Stored data: None — tidak ada DB/storage berbentuk yang menyimpan string "PrePostTest" sebagai key. AssessmentTypeInput stored sebagai value di assessment.AssessmentType column tapi tidak akan berubah.
- Live service config: None — tidak ada external service config.
- OS-registered state: None — tidak ada OS task / launchd / systemd.
- Secrets/env vars: None — tidak ada secret rename.
- Build artifacts: None — tidak ada egg-info / compiled binary rename.

## Environment Availability

> Skip — Phase 308 hanya touch source code (.cshtml + .cs) dan test scaffolding (.ts). Tidak ada external CLI/service/runtime baru. Existing stack (.NET 9 + Playwright) sudah tersedia per Phase 307 verification (build ok, e2e infrastructure ok).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (existing — `tests/e2e/`) `[VERIFIED: tests/e2e/assessment.spec.ts]` |
| Config file | `tests/playwright.config.ts` (per Phase 307 setup) |
| Quick run command | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --reporter=list` |
| Full suite command | `cd tests && npx playwright test e2e/assessment.spec.ts --reporter=list` |
| .NET sanity command | `dotnet build --no-restore` (expect 0 errors, 92 pre-existing warnings) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| WIZ-04 #1 | JS set Status='Upcoming' saat PrePost | E2E | `npx playwright test --grep "8.3"` | ❌ Wave 0 |
| WIZ-04 #2 | Server ModelState.Remove("Status") saat PrePost | E2E (test 8.3 implicit — submit success path tanpa error) | `npx playwright test --grep "8.3"` | ❌ Wave 0 |
| WIZ-04 #3 | jQuery validate re-parse | UNIT/integration **N/A** (plugin tidak loaded — superseded by custom validateStep visibility guard di line 996-1004) | Manual UAT only — verify wrapper toggle d-none + Status reset | ❌ UAT only |
| WIZ-04 #4 test matrix 4 kombinasi | Standard saja, S→PP→S, PP saja, PP→S→PP | E2E (4 test cases) | `npx playwright test --grep "Phase 308"` | ❌ Wave 0 (4 tests 8.1-8.4) |
| WIZ-04 #5 regression | Standard mode tanpa Status tetap "Status wajib dipilih" | E2E + manual UAT | `npx playwright test --grep "8.1"` (positive case) + manual UAT regression check | ❌ Wave 0 + UAT |

### Sampling Rate
- **Per task commit:** `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 308" --reporter=list` (4 tests, < 30s expected)
- **Per wave merge:** `cd tests && npx playwright test e2e/assessment.spec.ts --reporter=list` (full suite untuk regression Phase 304/307)
- **Phase gate:** Full suite green + manual UAT 4-step Bahasa Indonesia approved sebelum `/gsd-verify-work`
- **.NET build sanity:** `dotnet build --no-restore` (0 errors required) — match Phase 307 pre-verify pattern

### Wave 0 Gaps
- [ ] `tests/e2e/assessment.spec.ts` — append FLOW 8 describe block + 4 test cases (8.1, 8.2, 8.3, 8.4) — covers WIZ-04 #1, #2, #4, #5
- [ ] `tests/e2e/helpers/wizardSelectors.ts` — append 4 selector baru (`assessmentTypeInput`, `statusFieldWrapper`, `statusSelect`, `submitBtn`, `createForm`)
- [ ] `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` — manual UAT 4-step Bahasa Indonesia (mirror Phase 307 5-step pattern), include sign-off section
- [ ] Framework install: tidak perlu (Playwright + Phase 307 helpers existing) `[VERIFIED: tests/e2e/helpers/wizardSelectors.ts exists with 8 selectors]`

**Wave 0 ↔ Wave 1 RED→GREEN cycle:** Wave 0 commit 4 RED tests + UAT.md scaffold. Wave 1 implements client + server fix. Pre-verify expects 4 tests transition RED→GREEN (mirror Phase 307 precedent — `7.1, 7.2, 7.3, 7.4` transition).

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V2 Authentication | no | Phase 308 tidak touch auth — existing `[Authorize(Roles="Admin, HC")]` di controller method tidak di-modify |
| V3 Session Management | no | Tidak relevan |
| V4 Access Control | no | Existing role guard `Admin, HC` untuk POST CreateAssessment unchanged |
| V5 Input Validation | yes | Server `ModelState` validation tetap aktif untuk semua field LAIN (Title, Category, UserIds, dll); hanya `Status` yang di-skip conditional. PassPercentage range, Schedule date validation, EWCD validation semua tetap (line 814-832, 880-911) |
| V6 Cryptography | no | Tidak relevan |

### Known Threat Patterns for ASP.NET MVC + vanilla JS wizard

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Mass-assignment via `Status='Open'` server when user intends 'Upcoming' for PrePost | Tampering | `ModelState.Remove("Status")` allows bind, but server line 1078/1112/1170 EXPLICIT set `Status="Upcoming"` for PrePost session creation — JS hint via 'Upcoming' value tidak digunakan untuk authority, hanya UX `[VERIFIED: grep "Status = \"Upcoming\"" → 5 matches in PrePost path]` |
| ModelState.Remove unintentional skip too many fields | Tampering / Information Disclosure | Phase 308 hanya remove `"Status"` — exact field name, bukan glob pattern. Other `[Required]` fields (Title, UserIds, Schedule, etc) tetap validated |
| Client-side Status assignment overriden by malicious user (DevTools) | Tampering | Tidak relevan — server line 1078/1112/1170 hardcode `Status="Upcoming"` untuk PrePost session creation, ignoring submitted `model.Status`. Defense-in-depth `[VERIFIED]` |
| CSRF | Tampering | `@Html.AntiForgeryToken()` line 103 + form `[ValidateAntiForgeryToken]` attribute (existing pattern di controller — assumed unchanged) |
| XSS via Status field | Tampering | `Status` adalah enum string ('Open' / 'Upcoming') rendered via Razor `<select>` options — tidak ada user-supplied untrusted content. Empty string fallback safe |

**Phase 308 security posture:** Net-zero risk. Conditional `ModelState.Remove("Status")` adalah scoped + semantically correct (Status field is mode-irrelevant for PrePost — server overrides anyway). Authority remains server-side at line 1078/1112/1170.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WIZ-04 | Admin dapat submit assessment Pre-Post Test tanpa error "Status field is required" yang me-reset wizard ke Step 1. JS handler set value `Status='Upcoming'` saat PrePost mode + conditional `ModelState.Remove("Status")` server-side; switching mode Standard ↔ PrePost tidak meninggalkan stale validation state. | **Pattern 1** (conditional ModelState.Remove) + **Pattern 2** (existing PrePost mode toggle handler extend) + **Verified Line Numbers** (anchor 1876 client, 779 server) + **Pitfall 2** (jQuery re-parse N/A — supersede with custom validateStep visibility guard already at line 996-1004) + **Pitfall 4** (D-02 mode-switch state cleanup intentional) + **Code Examples 1, 3** (D-01/D-02 client + D-04 server). Test matrix coverage di **Validation Architecture** Wave 0 4 tests 8.1-8.4. |

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Status Field Default Value untuk PrePost Mode:**
- **D-01:** Saat user pilih `value === 'PrePostTest'` di Step 1 type selector, JS handler set `document.getElementById('Status').value = 'Upcoming'`. Konsisten dengan Pre-Post sessions di server line 1078, 1112, 1170 yang semuanya `Status = "Upcoming"`.
- **D-02:** Saat user switch back PrePost → Standard, JS handler clear Status value (`statusEl.value = ''`) agar dropdown kembali default `-- Pilih Status --` dan force user re-pilih.
- **D-03:** `statusFieldWrapper` `.classList.toggle('d-none', isPrePost)` tetap di-handle oleh existing JS (line ~1837+ post Phase 307 shift, original ROADMAP ref line 1790-1807). [**RESEARCH UPDATE:** verified anchor adalah line 1880/1886 dalam handler block 1872-1889; bukan ~1837]

**Server-Side Conditional ModelState Removal:**
- **D-04:** Tambah baris early di POST handler `Controllers/AssessmentAdminController.cs` setelah line 779 (`bool isPrePostMode = AssessmentTypeInput == "PrePostTest";`):
  ```csharp
  if (isPrePostMode)
  {
      ModelState.Remove("Status"); // Status field hidden in PrePost mode — JS sets default 'Upcoming'
  }
  ```
- **D-05:** Posisi insertion: antara line 779 dan line 782 (sebelum block `if (!isPrePostMode) { /* schedule validation */ }`). Mirror pattern existing `ModelState.Remove("UserId")` line 742 dan `ModelState.Remove("AccessToken")` line 756.
- **D-06:** Default value setting di line 975-978 (`if (string.IsNullOrEmpty(model.Status)) model.Status = "Open";`) **TETAP UNCHANGED** — masih dipakai untuk Standard mode kalau Status field hidden tapi belum ke-set oleh JS (defensive). Untuk PrePost mode, line 1078/1112/1170 explicit set `Status = "Upcoming"` yang menang dari fallback "Open".

**jQuery Validate Re-Parse Strategy:**
- **D-07:** Setelah toggle `statusFieldWrapper`, trigger jQuery validate re-parse untuk clear stale validation state. [**RESEARCH FLAG:** jQuery validate plugin TIDAK loaded di form ini — Pitfall 2. Plan-phase WAJIB pilih: Pilihan A SKIP total (recommended), atau Pilihan B retain dengan defensive guard sebagai dead-code documentation.]
- **D-08:** Letakkan re-parse call setelah value assignment D-01/D-02. [**RESEARCH FLAG:** moot kalau D-07 SKIP.]
- **D-09:** Defensive guard wrap re-parse di `if (typeof $.validator !== 'undefined' && $.validator.unobtrusive)`. [**RESEARCH FLAG:** WAJIB include kalau D-07 retained.]

**Mode-Switching State Preservation:**
- **D-10:** Test matrix 4 kombinasi:
  1. Standard saja → success
  2. S→PP→S → Status reset to ''; user re-pick; validation pass
  3. PP saja → Status auto-set 'Upcoming'; ModelState.Remove("Status"); validation pass
  4. PP→S→PP → Status auto-set kembali 'Upcoming'; validation pass

**Regression Guard:**
- **D-11:** Standard mode tanpa pilih Status TETAP menampilkan "Status wajib dipilih" — D-04 conditional fire HANYA saat `isPrePostMode == true`.
- **D-12:** Tidak ada perubahan ke `<select asp-for="Status">` markup line 481-487 — `[Required]` attribute di model tetap.

**Test Scaffolding:**
- **D-13:** Wave 0: extend `tests/e2e/assessment.spec.ts` dengan FLOW 8 describe block + 4 test cases (8.1-8.4). Plus extend `tests/e2e/helpers/wizardSelectors.ts` dengan selector tambahan.
- **D-14:** Wave 1: single-file edit ke `Views/Admin/CreateAssessment.cshtml` (D-01, D-02, optional D-07/D-08/D-09) + single-file edit ke `Controllers/AssessmentAdminController.cs` (D-04, D-05).
- **D-15:** Pre-Wave 1 line number re-verification REQUIRED — DONE per RESEARCH "Verified Line Numbers" table.

**File Conflict Sequencing:**
- **D-16:** Phase 307 sudah COMPLETE (2026-04-29), Phase 308 sekarang unblocked.
- **D-17:** Phase 308 hanya touch existing JS handler line 1872-1889 — TIDAK menyentuh helper Phase 307 (top-level line 1469-1614). Risk regresi Phase 307 minimal.

**Manual UAT:**
- **D-18:** Wave 0 buat `308-UAT.md` 4-step Bahasa Indonesia (mirror Phase 307 5-step UAT pattern).
- **D-19:** Sign-off section + tester name + browser version (pattern existing 307-UAT.md).

### Claude's Discretion

- **CD-01:** Selector untuk form (`#createForm` vs `form#wizard` dll). [**RESEARCH RESOLVED:** verified `#createAssessmentForm` (line 102). Plan-phase WAJIB pakai ID ini.]
- **CD-02:** Comment style untuk `ModelState.Remove("Status")` — match existing pattern `// Token is NOT required - remove from validation and clear value` (line 755). [**RESEARCH SUGGESTION:** `// Status field hidden in PrePost mode — JS sets default 'Upcoming'`]
- **CD-03:** Wave 0 scope decision — apakah include selector `submitBtn` di wizardSelectors atau buat selector test-local. [**RESEARCH RECOMMENDATION:** extend wizardSelectors centrally (DRY per Phase 307 D-15). 4 selectors: assessmentTypeInput, statusFieldWrapper, statusSelect, submitBtn. Plus add `createForm` reference selector untuk verifikasi ID.]

### Deferred Ideas (OUT OF SCOPE)

- **Wizard return-to-step-1 enhancement** — REQUIREMENTS.md Out of Scope explicit. Phase 308 hanya pastikan TIDAK reset; reset behavior untuk error REAL tetap.
- **Refactor wizard state machine** (resumeStep, goToStep generalization) — di luar scope audit fix; defer ke v16+.
- **Test framework migration** (e.g., Jest → Vitest, Playwright→Cypress) — di luar scope.
- **jQuery removal / migration ke vanilla JS validation** — di luar scope (plus jQuery validate plugin tidak loaded di form ini anyway).
- **`realtime-assessment.md` todo** (todo pending sejak 2026-03-09) — tidak relevan Phase 308.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `[Required]` attribute di `Status` property model `AssessmentSession` — bukan `[Required(AllowEmptyStrings=true)]` atau missing | Pitfall 1, D-04 rationale | Kalau `[Required]` ternyata tidak ada, `ModelState.Remove("Status")` jadi no-op (tidak harmful). Kalau pakai `AllowEmptyStrings=true`, juga no-op. Risk LOW. |
| A2 | `<form id="createAssessmentForm" novalidate>` — `novalidate` attr disable HTML5 validation tapi tidak disable `[Required]` server-side | Pattern 2, Pitfall 2 | `[VERIFIED: line 102 grep + W3C HTML5 spec — `novalidate` adalah HTML5 form validation suppression, server-side independent]` Risk minimal. |
| A3 | jQuery 3.7.1 loaded oleh `_Layout.cshtml` line 230 tersedia di global scope saat CreateAssessment.cshtml execute | Pitfall 2 | `[VERIFIED: grep "jquery" Views/Shared/_Layout.cshtml line 230]` Kalau wrong, defensive guard di Example 2 akan no-op (typeof check protect dari TypeError). Risk LOW. |
| A4 | Phase 307 helper top-level functions (`renderSelectedParticipants` di line 1469) tidak akan conflict dengan handler edit di line 1872-1889 (different scope, different IIFE) | D-17 | `[VERIFIED via Read: line 1469-1614 adalah top-level scope di antara main IIFE close (1459) dan PrePost mode toggle handler block start (1860); Phase 308 edits tidak masuk scope ini]` Risk MINIMAL. |
| A5 | Test FLOW numbering — last existing FLOW adalah 7 (Phase 307); Phase 308 logical continuation FLOW 8 | D-13 | `[VERIFIED: grep "FLOW [0-9]" tests/e2e/assessment.spec.ts → FLOW 1, 2, 3, 4, 5, 6, 7]` Risk ZERO — naming convention. |
| A6 | Existing `validateStep(3)` di line 996-1004 dengan visibility guard `if (!statusWrapper.classList.contains('d-none'))` sudah handle case Status hidden untuk PrePost — sehingga client-side validation tidak block submit di PrePost | Pitfall 2 (mengapa jQuery re-parse N/A) | `[VERIFIED via Read line 996-1004]` Existing logic SUDAH benar untuk client side; bug eksklusif server-side `ModelState`. Plan-phase confirm. Risk MINIMAL. |

**Penilaian risiko keseluruhan:** Semua assumption verified atau LOW-risk. Tidak ada `[ASSUMED]` claim yang block implementation. Plan-phase bisa proceed dengan confidence HIGH.

## Open Questions (RESOLVED)

1. **Apakah retain D-07/D-08/D-09 jQuery re-parse code (Pilihan B) atau skip total (Pilihan A)?**
   - What we know: jQuery validate plugin TIDAK loaded di form ini (Pitfall 2 verified). Re-parse call akan no-op meskipun guard pass — tidak ada validator data yang di-clear.
   - What's unclear: Apakah ada future plan menambah `_ValidationScriptsPartial` untuk form ini, sehingga re-parse code menjadi forward-compatible scaffold?
   - Recommendation: **Pilihan A SKIP total** untuk Wave 1. Document di code comment `// Phase 308 note: jQuery validate plugin not loaded di form ini; visibility guard di validateStep line 996-1004 handle equivalent. Re-parse intentionally omitted.` Plan-phase finalize keputusan.
   - **RESOLVED:** Pilihan A SKIP total. Rasional: _ValidationScriptsPartial 0 matches verified di CreateAssessment.cshtml (jquery.validate plugin TIDAK loaded); existing custom validateStep line 996-1004 sudah handle hidden Status correctly via visibility guard if (statusWrapper && !statusWrapper.classList.contains('d-none')). Plans 308-01 dan 308-02 dokumentasi via <context> rationale + acceptance criteria task 1 enforces 0 matches \$\.validator\.unobtrusive dan removeData('validator'). ROADMAP success criteria #3 marked N/A di plan 308-02 success_criteria checklist.

2. **CD-02 comment wording untuk `ModelState.Remove("Status")` — Indonesia atau English?**
   - What we know: Existing comment di line 755 (`// Token is NOT required - remove from validation and clear value`) English. Existing comment di line 821 (`// Remove model binding error first`) English. Server code comments di file ini predominantly English.
   - What's unclear: Project CLAUDE.md "Always respond in Bahasa Indonesia" applies to user-facing text; code comments di server traditionally English.
   - Recommendation: English untuk consistency dengan existing pattern: `// Status field hidden in PrePost mode — JS sets default 'Upcoming' on type-switch`. UAT.md tetap Bahasa Indonesia.
   - **RESOLVED:** English untuk code comments — match existing pattern line 755 (// Token is NOT required - remove from validation and clear value). Comment final di plan 308-02 Task 2: // Phase 308 D-04: Status field hidden in PrePost mode — JS sets default 'Upcoming', server skips [Required] validation. Bahasa Indonesia hanya untuk UAT.md script + test names (per CLAUDE.md C-01) — verified di plan 308-01 Task 3 (UAT 4-step Bahasa Indonesia) dan Task 2 (test names 'Standard saja', 'Switch S→PP→S', dst).

3. **Apakah Wave 0 split ke 2 plans (308-01 + 308-02) seperti Phase 307, atau single plan (308-01)?**
   - What we know: Phase 307 split: 01 (Wave 0 scaffold), 02 (Wave 1 implementation). Phase 304 split: 01 + 02 by REQ. Phase 305 + 306 split similarly.
   - What's unclear: Phase 308 effort lebih kecil (4-line server insert + 2-line client edits + UAT scaffold). Mungkin single plan cukup.
   - Recommendation: **Mirror Phase 307 pattern — split 308-01 (Wave 0) + 308-02 (Wave 1)** untuk RED→GREEN cycle hygiene. Consistent precedent.
   - **RESOLVED:** 2-plan split — 308-01-PLAN.md (Wave 0 test scaffold: 4 selector additions + 4 E2E tests + UAT.md) + 308-02-PLAN.md (Wave 1 implementation: JS edit + server edit + verification + manual UAT checkpoint). Mirror Phase 307 precedent untuk RED→GREEN cycle hygiene. Plan structure: Wave 0 = autonomous true (3 tasks); Wave 1 = autonomous false (4 tasks termasuk 1 checkpoint blocking gate).

## Sources

### Primary (HIGH confidence — VERIFIED via tool)
- `Views/Admin/CreateAssessment.cshtml` (1957 lines, post-Phase 307) — verified anchors at line 102, 479-487, 902, 996-1004, 1458, 1872-1889 via Read + grep
- `Controllers/AssessmentAdminController.cs` (5137 lines) — verified anchors at line 742, 756, 779-792, 821, 835, 870, 875, 914-940, 975-978, 1078, 1112, 1170, 1644, 1663 via Read + grep
- `tests/e2e/helpers/wizardSelectors.ts` (8 selectors, Phase 307) — verified existing structure
- `tests/e2e/assessment.spec.ts` (FLOW 1-7, Phase 307 tests at line 96-170) — verified test pattern
- `Views/Shared/_Layout.cshtml` line 230 — jQuery 3.7.1 CDN load verified
- `Views/Shared/_ValidationScriptsPartial.cshtml` (jquery.validate + jquery.validate.unobtrusive) — verified file exists, NOT included in CreateAssessment.cshtml

### Secondary (MEDIUM-HIGH confidence — CITED)
- `.planning/phases/308-prepost-wizard-validation-fix/308-CONTEXT.md` — D-01 through D-19, CD-01 through CD-03
- `.planning/phases/307-selected-participants-inline-view/307-02-SUMMARY.md` — +47 net lines metric, post-Phase 307 file size 1957
- `.planning/phases/307-selected-participants-inline-view/307-CONTEXT.md` — D-15 DRY helper pattern, file conflict precedent
- `.planning/REQUIREMENTS.md` — REQ WIZ-04 full text + Out of Scope table (T11 differentiator excluded)
- `.planning/ROADMAP.md` line 126-134 — Phase 308 success criteria 5 items
- `.planning/STATE.md` — current position Phase 309 (after Phase 307 close), Phase 308 next phase
- `./CLAUDE.md` — "Always respond in Bahasa Indonesia"

### Tertiary (background reference)
- ASP.NET Core MVC ModelState lifecycle — per-request, no carry-over between requests `[CITED: Microsoft docs (training knowledge)]`
- W3C HTML5 `novalidate` attribute — disable client HTML5 validation, server-side independent `[CITED: HTML5 spec]`

## Metadata

**Confidence breakdown:**
- Verified line numbers: **HIGH** — all 4 PrePost matches grep'd, all 5 ModelState.Remove matches grep'd, all 5 Status="Upcoming" instances grep'd
- Pattern (ModelState.Remove): **HIGH** — 5-instance precedent in same file, exact-match pattern
- Pattern (JS handler extend): **HIGH** — single existing handler block 1872-1889 verified by Read, 2 branch insertion points clear
- Pitfall 2 (jQuery re-parse N/A): **HIGH** — `_ValidationScriptsPartial` 0 matches in CreateAssessment.cshtml verified by grep
- Test scaffolding: **HIGH** — Phase 307 precedent verified (FLOW 7 line 96-170), 8 existing selectors verified
- Form ID correction: **HIGH** — `#createAssessmentForm` verified line 102, `#createForm` 0 matches
- Security posture: **MEDIUM-HIGH** — `[CITED]` Microsoft docs for ModelState lifecycle; defense-in-depth verified via 5-instance Status="Upcoming" hardcode in PrePost path

**Research date:** 2026-04-29
**Valid until:** Phase 308 Wave 1 merge (estimated 1-2 days). After Phase 308 close, line numbers WILL shift again — future phases (309+) WAJIB re-grep.

---

*Phase: 308-prepost-wizard-validation-fix*
*Research completed: 2026-04-29*
*Phase 307 +47 lines net to CreateAssessment.cshtml — RESEARCH.md verified all anchors post-shift*
*KRITIS: Form ID `#createAssessmentForm` (BUKAN `#createForm` per CONTEXT D-07) — Plan-phase WAJIB correct*
*KRITIS: jQuery validate plugin TIDAK loaded di form ini — D-07/D-08/D-09 conceptual misapplication, plan-phase resolve via Pitfall 2*
