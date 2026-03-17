# Project Research Summary

**Project:** PortalHC KPB — v7.5 Assessment Form Revamp & Certificate Enhancement
**Domain:** Brownfield ASP.NET Core MVC — Wizard Form, DB-Driven Categories, Certificate Auto-Numbering, PDF Download
**Researched:** 2026-03-17
**Confidence:** HIGH

## Executive Summary

This milestone is a well-scoped brownfield enhancement to an existing MVC portal. The goal is to replace a single long-page assessment creation form with a 4-step wizard, move assessment categories from hardcoded view values to a database-driven table, add certificate auto-numbering and expiry tracking to `AssessmentSession`, and introduce a QuestPDF-generated certificate download. All required libraries are already installed — no new NuGet packages are needed. The pattern for every new capability has a direct existing analogue in the codebase (ViewBag-driven dropdowns, QuestPDF in CDPController, admin CRUD pattern, EF migrations).

The recommended implementation approach is strictly incremental: lay down the data layer first (migrations for `AssessmentCategories` table and new `AssessmentSession` columns), then isolate each UI and logic change to its own phase. The wizard must be implemented as a single-page client-side show/hide restructure — NOT as multi-action server round-trips — to preserve the existing 310-line POST action without risky refactoring. Clone, auto-numbering, and PDF certificate are independent and can be phased safely after the data layer is stable.

The primary risks are correctness risks, not capability risks. Category seed data must exactly match the six existing string values used in controller branching logic. Auto-generated certificate numbers need a DB-level UNIQUE constraint from day one plus a retry loop to be safe under concurrent exam completions. Wizard "Back" navigation must explicitly re-sync the multi-user selection hidden input or it will produce sessions for unintended workers. QuestPDF certificate layout must use relative (not absolute) positioning to handle long employee names without clipping.

---

## Key Findings

### Recommended Stack

All required technology is already in the project. No new packages are needed. The stack decisions are about which existing patterns to follow, not which libraries to add.

**Core technologies:**
- ASP.NET Core MVC (net8.0): wizard form POST, clone action, certificate PDF action — all fit the existing MVC action pattern
- EF Core 8.0 + SQL Server: two migrations cover all DB changes (`AssessmentCategories` table; `ValidUntil` + `NomorSertifikat` on `AssessmentSessions`)
- QuestPDF 2026.2.2: certificate PDF download — Community license active in `Program.cs`; inline `Document.Create` lambda pattern already used in `CDPController.cs`
- Bootstrap 5 nav-tabs + jQuery: wizard step UI — zero new dependency; sufficient for a 4-step linear form

**Patterns to follow (not invent):**
- ViewBag category list: follows `ViewBag.ProtonTracks` / `ViewBag.Sections` pattern already in `CreateAssessment GET`
- Admin CRUD for categories: copy ManageWorkers CRUD pattern from `AdminController`
- PDF download action: copy inline QuestPDF pattern from `CDPController.ExportProgressPdf` (lines 2189–2539)

### Expected Features

**Must have (table stakes — all in scope for v7.5):**
- Wizard step indicator and per-step client-side validation — users expect multi-step forms to have progress indicators
- DB-driven category dropdown — HC admins need to add categories without developer deployment
- Clone / duplicate existing assessment — HC admins recreate recurring assessments manually today
- `ValidUntil` field on `AssessmentSession` — certificates need expiry dates like `TrainingRecord` already has
- Auto-generated `NomorSertifikat` on `AssessmentSession` — unique, traceable certificate numbers for online assessments
- QuestPDF certificate download — deterministic PDF needed; browser print-to-PDF is unreliable

**Should have (differentiators — add after core phases validated):**
- Wizard Confirm/Review step showing all selections before submit
- Category-driven default autofill (pass threshold, cert flag) via `data-pass-percentage` on `<option>` elements
- Certificate expiry warning extended to online assessment certs (reuse existing `IsExpiringSoon` pattern)

**Defer to v2+:**
- Server round-trip save between wizard steps (unnecessary complexity for a 2-minute form)
- Editable certificate template via UI (out of scope; use QuestPDF static layout with AppSettings config)
- `AssessmentCategory` hierarchy / subcategories (not confirmed as requirement; adds migration complexity)

### Architecture Approach

All changes fit within the existing project folder structure — no new controllers, no new service layer, no new folders. The boundary is: `AdminController` handles session creation (modify `CreateAssessment GET/POST`, add `CloneAssessment GET`); `CMPController` handles certificate delivery (add `CertificatePdf GET`); `ApplicationDbContext` gains one new `DbSet` and two new columns via a single migration. The wizard is a client-side restructure of `CreateAssessment.cshtml` only — the POST action signature and all server-side validation remain unchanged.

**Major components:**
1. `AssessmentCategory` model + EF migration — DB table replacing hardcoded category strings; seed with 6 exact existing values
2. `AdminController.CreateAssessment GET/POST` (modified) — loads categories from DB; maps `ValidUntil`; auto-generates `NomorSertifikat` in POST loop
3. `AdminController.CloneAssessment GET` (new) — reads existing session; redirects to `CreateAssessment` with pre-fill query string
4. `CreateAssessment.cshtml` (major rewrite) — 4-step wizard via Bootstrap tabs + jQuery; categories from ViewBag
5. `CMPController.CertificatePdf GET` (new) — QuestPDF A4 landscape binary stream; same auth guard as existing `Certificate` action

### Critical Pitfalls

1. **Wizard server-side validation gap** — client-side step validation is UX only; an empty `Category` string passes `[Required]` and corrupts the monitoring view. Add explicit `ModelState` check for whitespace `Category` in the POST action as a backstop.

2. **JS `categoryDefaults` object diverges from DB** — the existing hardcoded `categoryDefaults` JS object in `CreateAssessment.cshtml` must be deleted in the same commit that adds `data-pass-percentage` attributes to `<option>` elements. Never let both exist simultaneously.

3. **Auto-number race condition under concurrent exam completions** — read-increment-write is not atomic. Add a UNIQUE constraint on `NomorSertifikat` from the initial migration and wrap generation in a retry loop (up to 3 attempts catching `DbUpdateException`).

4. **Wizard Back navigation desyncs multi-user selection** — the JS user-selection module must expose a `setSelectedUsers()` setter so that navigating Back to Step 2 re-initializes the displayed list from the hidden input. Implement from the start, not as a later fix.

5. **Clone deep-copy must include `AssessmentPackage` hierarchy** — cloning only `AssessmentSession` fields leaves zero questions; the exam engine reads from `AssessmentPackage → PackageQuestion → PackageOption`. Query and deep-copy the full three-level tree, always creating new entities with `Id = 0`.

6. **QuestPDF certificate layout must use relative positioning** — never use `element.Absolute()` for content containers. Test with the longest `FullName` in the Workers table before finalizing.

---

## Implications for Roadmap

Based on the combined research, the recommended phase structure is 6 phases. The data layer must come first as every other phase depends on the migrations being applied. DB categories must precede the wizard rewrite (otherwise wizard has nothing to bind to). `ValidUntil`/`NomorSertifikat` columns must exist before the wizard can capture them. Clone depends on a stable wizard. PDF certificate only requires Phase 1 (columns must exist) and can be built in parallel with Phases 3–5 if needed.

### Phase 1: Data Layer
**Rationale:** Every subsequent phase depends on the new DB schema. Running migrations first eliminates the risk of partially-built phases breaking because columns don't exist yet.
**Delivers:** `AssessmentCategory` model, `DbSet<AssessmentCategory>` in `ApplicationDbContext`, EF migration creating `AssessmentCategories` table with 6 seeded rows, EF migration adding `ValidUntil DateTime?` and `NomorSertifikat string?` (with UNIQUE constraint) to `AssessmentSessions`.
**Addresses:** DB-driven categories (table stakes), ValidUntil, NomorSertifikat
**Avoids:** Pitfall 4 (ValidUntil missing in downstream views) — update all downstream ViewModel projections in this same phase; Pitfall 5 (race condition) — UNIQUE constraint added from day one

### Phase 2: DB Categories in Form
**Rationale:** Smallest isolated change before tackling the full wizard rewrite. Proves category loading works end-to-end with the existing single-page form before introducing step navigation complexity.
**Delivers:** Modified `CreateAssessment GET` queries `AssessmentCategories` from DB; `CreateAssessment.cshtml` renders `<select>` from `ViewBag.Categories` with `data-pass-percentage` on each `<option>`; hardcoded `categoryDefaults` JS object deleted.
**Avoids:** Pitfall 2 (JS/DB divergence) — both sources cannot coexist; category seed naming must use exact existing string values to preserve `AdminController` branching on `"Assessment Proton"`
**Research flag:** None — standard ViewBag pattern already in this codebase

### Phase 3: Wizard UI
**Rationale:** UI restructure only — no POST action changes. Safe to build after Phase 2 confirms category loading. This is the highest-risk phase for UX correctness (step validation, Back navigation).
**Delivers:** 4-step `CreateAssessment.cshtml` wizard (Bootstrap tabs + jQuery step controller); per-step client-side validation; Confirm/summary step; `ValidUntil` datepicker in Step 3.
**Uses:** Bootstrap 5 nav-tabs, jQuery (both on CDN, no install), existing unobtrusive validation
**Avoids:** Pitfall 1 (server-side validation gap) — add explicit ModelState check for Category whitespace; Pitfall 7 (Back navigation desync) — implement `setSelectedUsers()` from the start
**Research flag:** None — well-established pattern; Bootstrap tabs behavior is fully documented

### Phase 4: ValidUntil + NomorSertifikat on Session
**Rationale:** Depends on Phase 1 (columns exist) and Phase 3 (wizard has the ValidUntil field in Step 3). Isolated to the POST action only — no view changes.
**Delivers:** `CreateAssessment POST` maps `ValidUntil` from model; auto-generates `NomorSertifikat` inside POST loop using count-before-loop + in-memory increment pattern; retry logic on `DbUpdateException` for uniqueness.
**Avoids:** Pitfall 5 (race condition) — retry loop + UNIQUE constraint enforced

### Phase 5: Clone Assessment
**Rationale:** Depends on Phase 3 (stable wizard must exist for pre-fill to land in correct step). Highest implementation risk due to question deep-copy complexity.
**Delivers:** `AdminController.CloneAssessment GET` reads session + full `AssessmentPackage → PackageQuestion → PackageOption` tree; redirects to `CreateAssessment` with pre-fill query string; `CreateAssessment GET` reads clone params from `Request.Query`; "Clone" button on assessment list/detail view.
**Avoids:** Pitfall 3 (partial deep-copy) — inspect `CMPController` exam-taking actions first to confirm full entity graph required
**Research flag:** Read `CMPController` PackageExam action before coding to verify question entity graph depth

### Phase 6: PDF Certificate Download
**Rationale:** Only dependency is Phase 1 (NomorSertifikat and ValidUntil columns must exist). Can be developed in parallel with Phases 3–5 if desired, or sequentially after all others.
**Delivers:** `CMPController.CertificatePdf GET` action; QuestPDF A4 landscape document with relative layout; "Download PDF" button on `Certificate.cshtml`; filename `Sertifikat_{NIP}_{Title}_{Year}.pdf`.
**Uses:** QuestPDF 2026.2.2 (already installed); inline `Document.Create` lambda matching CDPController style
**Avoids:** Pitfall 6 (QuestPDF absolute positioning) — use `.Column()` / `.Row()` / `.AlignCenter()`; test with longest FullName in DB
**Research flag:** None — QuestPDF already used in CDPController; Community license already set

### Phase Ordering Rationale

- Phase 1 before everything: all DB-dependent code needs the schema to exist before end-to-end testing is possible
- Phase 2 before Phase 3: isolates the category wiring risk from the wizard navigation risk; each can be tested independently
- Phase 4 after Phase 3: wizard must exist before the POST can receive `ValidUntil` from Step 3
- Phase 5 after Phase 3: clone pre-fill must land in a stable wizard; unreliable to implement before wizard step layout is final
- Phase 6 any time after Phase 1: fully decoupled from UX changes; can be built last or in parallel

### Research Flags

Phases requiring targeted code-reading during planning (not a full research cycle):
- **Phase 5 (Clone):** Read `CMPController` exam-taking actions (`PackageExam`, `SubmitAnswers`) before writing clone logic to verify whether the exam engine uses `AssessmentQuestion` (legacy), `AssessmentPackage` (current), or both. The deep-copy scope depends on this.

Phases with standard patterns (skip additional research):
- **Phase 1 (Data Layer):** EF Core migration + seed data — fully documented, identical to prior migrations in this project
- **Phase 2 (DB Categories):** ViewBag list rendering — identical to `ProtonTracks` and `Sections` already in `CreateAssessment GET`
- **Phase 3 (Wizard UI):** Bootstrap 5 tabs + jQuery — standard pattern; no third-party dependency
- **Phase 4 (AutoNumber):** Count-before-loop + in-memory increment with retry — self-contained; no external service
- **Phase 6 (PDF):** QuestPDF inline lambda — copy from `CDPController.ExportProgressPdf` structure

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All packages confirmed by direct csproj and source file inspection; no new dependencies required |
| Features | HIGH | All features confirmed by direct model, controller, and view inspection; no ambiguous scope items |
| Architecture | HIGH | All integration points identified by direct source inspection; no speculative components |
| Pitfalls | HIGH | All pitfalls identified from direct inspection of affected code paths (`categoryDefaults` JS object, `AssessmentPackage` hierarchy, QuestPDF CDPController pattern) |

**Overall confidence:** HIGH

### Gaps to Address

- **Clone question graph depth:** Pitfall 3 identifies that `AssessmentSession` has two potential question sources. During Phase 5 planning, read `CMPController.PackageExam` to confirm whether the exam engine uses `AssessmentQuestion`, `AssessmentPackage`, or both. Scope the deep-copy accordingly.
- **Category seed string values:** `ARCHITECTURE.md` flags that `AdminController.CreateAssessment POST` has Proton-specific branching logic keyed on the exact string `"Assessment Proton"`. Confirm the exact six production strings in the DB before writing the migration seed to avoid silent branching breakage.
- **`EditAssessment` view:** Both `PITFALLS.md` and `ARCHITECTURE.md` note that `EditAssessment` (if it exists as a separate view) also needs `ViewBag.Categories` loaded and the `categoryDefaults` JS map removed. Verify whether an `EditAssessment` action/view exists before closing Phase 2.

---

## Sources

### Primary (HIGH confidence)
- `HcPortal.csproj` — package versions confirmed
- `Program.cs` — QuestPDF Community license confirmed at line 8
- `Controllers/AdminController.cs` — `CreateAssessment GET` (L759), `POST` (L795–1104), ManageWorkers CRUD pattern
- `Controllers/CMPController.cs` — `Certificate` action (L2327), unified TrainingRecord helper (L1031)
- `Controllers/CDPController.cs` — `ExportProgressPdf` QuestPDF pattern (L2189–2539)
- `Models/AssessmentSession.cs` — confirmed absence of `ValidUntil`, `NomorSertifikat`; `IsPassed bool?`; `GenerateCertificate bool`
- `Models/AssessmentPackage.cs` — three-level hierarchy (`AssessmentPackage → PackageQuestion → PackageOption`) confirmed
- `Models/TrainingRecord.cs` — `ValidUntil`, `NomorSertifikat`, `IsExpiringSoon` already present
- `Views/Admin/CreateAssessment.cshtml` — hardcoded `categoryDefaults` at line 538; single-page form confirmed
- `Views/CMP/Certificate.cshtml` — A4 landscape HTML layout reference
- `Data/ApplicationDbContext.cs` — `DbSet` inventory; no `AssessmentCategory` present
- `.planning/PROJECT.md` — v7.5 milestone scope confirmed

---
*Research completed: 2026-03-17*
*Synthesized from: STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md*
*Ready for roadmap: yes*
