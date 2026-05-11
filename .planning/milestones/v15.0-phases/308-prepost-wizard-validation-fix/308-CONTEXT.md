# Phase 308: PrePost Wizard Validation Fix - Context

**Gathered:** 2026-04-29
**Status:** Ready for planning
**Mode:** auto (gray areas auto-resolved with recommended defaults — review and override before plan-phase if needed)

<domain>
## Phase Boundary

Memungkinkan Admin/HC submit assessment Pre-Post Test tanpa error "Status field is required" yang me-reset wizard ke Step 1 (REQ WIZ-04, maps Audit Temuan 11). Fix two-layer: (a) JS handler set `Status='Upcoming'` saat user pilih `value === 'PrePostTest'` di Step 1 type selector, (b) server-side `ModelState.Remove("Status")` saat `isPrePostMode == true`. Plus jQuery validate re-parse setelah dynamic show/hide `statusFieldWrapper` agar stale validation state tidak menempel saat user switch tipe Standard ↔ PrePost.

**Out of scope:**
- Test wizard return-to-step-1 enhancement (T11 differentiator — di REQUIREMENTS.md `Out of Scope` table). Audit hanya minta TIDAK reset; reset behavior tetap untuk error real.
- Refactor wizard validation flow secara umum (touch goToStep, resumeStep). Hanya fix Status field ambiguity untuk PrePost mode.
- Migration / schema change (milestone goal: tanpa migrasi DB).

</domain>

<decisions>
## Implementation Decisions

### Status Field Default Value untuk PrePost Mode
- **D-01:** Saat user pilih `value === 'PrePostTest'` di Step 1 type selector, **JS handler set `document.getElementById('Status').value = 'Upcoming'`** (matches ROADMAP success criteria #1). Konsisten dengan Pre-Post sessions di server line 1078, 1112, 1170 yang semuanya `Status = "Upcoming"`.
- **D-02:** Saat user switch back PrePost → Standard, JS handler **clear Status value** (`statusEl.value = ''`) agar dropdown kembali default `-- Pilih Status --` dan force user re-pilih. Cegah stale "Upcoming" yang belum sengaja dipilih oleh user.
- **D-03:** `statusFieldWrapper` `.classList.toggle('d-none', isPrePost)` tetap di-handle oleh existing JS (line ~1837+ post Phase 307 shift, original ROADMAP ref line 1790-1807). **D-01 + D-02 menambah satu baris value assignment di tempat yang sama.**

### Server-Side Conditional ModelState Removal
- **D-04:** Tambah baris **early di POST handler** `Controllers/AssessmentAdminController.cs` setelah line 779 (`bool isPrePostMode = AssessmentTypeInput == "PrePostTest";`):
  ```csharp
  if (isPrePostMode)
  {
      ModelState.Remove("Status"); // Status field hidden in PrePost mode — JS sets default 'Upcoming'
  }
  ```
- **D-05:** Posisi insertion: **antara line 779 dan line 782** (sebelum block `if (!isPrePostMode) { /* schedule validation */ }`). Mirror pattern existing `ModelState.Remove("UserId")` line 742 dan `ModelState.Remove("AccessToken")` line 756.
- **D-06:** Default value setting di line 975-978 (`if (string.IsNullOrEmpty(model.Status)) model.Status = "Open";`) **TETAP UNCHANGED** — masih dipakai untuk Standard mode kalau Status field hidden tapi belum ke-set oleh JS (defensive). Untuk PrePost mode, line 1078/1112/1170 explicit set `Status = "Upcoming"` yang menang dari fallback "Open".

### jQuery Validate Re-Parse Strategy
- **D-07:** Setelah toggle `statusFieldWrapper` (di JS line ~1837 post Phase 307), **trigger jQuery validate re-parse** untuk clear stale validation state:
  ```javascript
  // After statusFieldWrapper.classList.toggle('d-none', isPrePost)
  var $form = $('#createForm'); // form id verified — main wizard form
  $form.removeData('validator').removeData('unobtrusiveValidation');
  $.validator.unobtrusive.parse($form);
  ```
- **D-08:** Letakkan re-parse call **setelah** value assignment D-01/D-02 — agar Status field punya valid value sebelum validate plugin re-evaluate.
- **D-09:** **Defensive guard:** Wrap re-parse di `if (typeof $.validator !== 'undefined' && $.validator.unobtrusive)` — jaga-jaga jika jQuery validate plugin belum loaded saat handler fire (initial page load timing).

### Mode-Switching State Preservation
- **D-10:** Test matrix 4 kombinasi (per ROADMAP success criteria #4):
  1. **Standard saja** (Standard fresh → submit) → Status user-picked, validation pass.
  2. **S→PP→S** (Standard → switch PrePost → switch back Standard → submit) → Status reset to '' setelah switch back; user re-pick; validation pass tanpa stale state.
  3. **PP saja** (PrePost fresh → submit) → Status auto-set 'Upcoming'; ModelState.Remove("Status"); validation pass.
  4. **PP→S→PP** (PrePost → switch Standard → switch back PrePost → submit) → Status auto-set kembali 'Upcoming' setelah switch back; validation pass.

### Regression Guard
- **D-11:** Standard mode tanpa pilih Status **TETAP** menampilkan "Status wajib dipilih" (per ROADMAP success criteria #5). Cara verify: D-04 conditional `ModelState.Remove("Status")` HANYA fire saat `isPrePostMode == true` — Standard mode validator tetap aktif untuk Status.
- **D-12:** Tidak ada perubahan ke `<select asp-for="Status">` markup line 481-487 — `[Required]` attribute di model tetap, hanya dynamically removed via `ModelState.Remove` untuk PrePost.

### Test Scaffolding (Wave 0 mirror Phase 307 pattern)
- **D-13:** Wave 0: extend `tests/e2e/assessment.spec.ts` dengan describe block **"Phase 308 PrePost Wizard Validation"** dan 4 test cases (test 8.1 Standard saja, 8.2 S→PP→S, 8.3 PP saja, 8.4 PP→S→PP — match D-10 test matrix). Plus extend `tests/e2e/helpers/wizardSelectors.ts` dengan selector tambahan: `assessmentTypeInput`, `statusFieldWrapper`, `Status`, `submitBtn`.
- **D-14:** Wave 1: single-file edit ke `Views/Admin/CreateAssessment.cshtml` (D-01, D-02, D-07, D-08, D-09) + single-file edit ke `Controllers/AssessmentAdminController.cs` (D-04, D-05).
- **D-15:** **Pre-Wave 1 line number re-verification REQUIRED** — Phase 307 added +47 net lines. ROADMAP refs (1790-1807, 778) sudah shifted. Researcher/Planner WAJIB grep ulang anchors:
  - `value === 'PrePostTest'` di JS handler (post-Phase 307 expected ~1860+, original 1790-1807)
  - `bool isPrePostMode = AssessmentTypeInput == "PrePostTest"` di controller (still line 779 — controller tidak di-touch oleh Phase 307)

### File Conflict Sequencing
- **D-16:** ROADMAP Wave Sequencing line 241: `Phase 304 (label) → Phase 307 (peserta list) → Phase 308 (PrePost validation)` — wajib serialize. **Phase 307 sudah COMPLETE (2026-04-29)**, Phase 308 sekarang unblocked. Tidak ada file conflict aktif.
- **D-17:** Phase 308 hanya touch existing `applyProtonMode` / Step 1 type-switch JS handler — **TIDAK** menyentuh helper Phase 307 (`renderSelectedParticipants`, `scheduleRenderSelectedPanel`, `updateSelectedCount` top-level di line 1469-1613). Risk regresi Phase 307 minimal.

### Manual UAT Approach
- **D-18:** Wave 0 buat `308-UAT.md` 4-step Bahasa Indonesia (mirror Phase 307 5-step UAT pattern):
  1. Step 1 — Standard saja: pilih tipe Standard, fill required fields + Status, submit, verify success
  2. Step 2 — Switch S→PP→S: switch tipe Standard → PrePost → kembali Standard, observe Status field re-shown empty, submit, verify success
  3. Step 3 — PrePost saja: pilih PrePost, fill PreSchedule + PostSchedule + durations + EWCD, submit, verify NO error "Status field is required" + NO reset ke Step 1
  4. Step 4 — Switch PP→S→PP: switch ke PrePost → Standard → kembali PrePost, observe Status field hidden + auto-set "Upcoming", submit, verify success
- **D-19:** Sign-off section + tester name + browser version (pattern existing 307-UAT.md).

### Claude's Discretion
- **CD-01:** Selector untuk form (`#createForm` vs `form#wizard` dll) — verify exact `id` saat plan/execute. Default assumption: form punya `id="createForm"` based on antiforgery wrapper pattern, tapi planner re-grep.
- **CD-02:** Comment style untuk `ModelState.Remove("Status")` — match existing pattern `// Token is NOT required - remove from validation and clear value` (line 755).
- **CD-03:** Wave 0 scope decision — apakah include selector `submitBtn` (existing — mungkin sudah ada di filterBarBadge area) di wizardSelectors atau buat selector test-local. Default: extend wizardSelectors centrally (DRY per Phase 307 D-15).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Spec & Audit Source
- `.planning/REQUIREMENTS.md` — REQ WIZ-04 full text + Out of Scope table (T11 differentiator excluded)
- `.planning/ROADMAP.md` line 126-134 — Phase 308 success criteria (5 items, ROADMAP refs line 1790-1807 dan ~778 SHIFTED post Phase 307)
- `.planning/PROJECT.md` line 46 — T11 description original

### Prior Phase Context (file conflict adjacency)
- `.planning/phases/307-selected-participants-inline-view/307-02-SUMMARY.md` — Phase 307 added +47 lines to CreateAssessment.cshtml; line numbers in ROADMAP refs are offset by ~+47
- `.planning/phases/307-selected-participants-inline-view/307-CONTEXT.md` — D-15 DRY helper pattern (relevant: Phase 308 may add selector helper extensions)
- `.planning/phases/304-ui-label-polish-login-wib/` — Phase 304 also touched CreateAssessment.cshtml (verify no overlap dengan Status/PrePost area)

### Code Targets
- `Views/Admin/CreateAssessment.cshtml`:
  - line 200: `<option value="PrePostTest">Pre-Post Test</option>` (assessmentType select option)
  - line 478-487: Status field markup (asp-for + select + invalid-feedback)
  - line 952, 1079, 1442 (post-Phase 307): existing `isPrePost = typeSelect.value === 'PrePostTest'` checks
  - line ~1837+ (post-Phase 307 shift, original ROADMAP ref 1790-1807): `applyProtonMode` / type-switch JS handler — anchor untuk D-01/D-02/D-07 insertion
- `Controllers/AssessmentAdminController.cs`:
  - line 779: `bool isPrePostMode = AssessmentTypeInput == "PrePostTest"` — anchor untuk D-04 insertion (line 780-781)
  - line 742: `ModelState.Remove("UserId");` — pattern reference untuk D-04
  - line 975-978: `if (string.IsNullOrEmpty(model.Status)) model.Status = "Open";` — defensive default UNCHANGED
  - line 1078, 1112, 1170, 1644, 1663: `Status = "Upcoming"` for PrePost session creation — confirms D-01 default value choice

### Test Infrastructure Targets (Wave 0)
- `tests/e2e/assessment.spec.ts` — extend dengan FLOW 8 describe block (4 tests matrix)
- `tests/e2e/helpers/wizardSelectors.ts` — extend dengan `assessmentTypeInput`, `statusFieldWrapper`, `Status`, `submitBtn` selectors
- `tests/helpers/auth.ts` — login helper (existing, no change)

### Style & Convention Refs
- `CLAUDE.md` — Always respond in Bahasa Indonesia (test names + UAT.md script wajib Bahasa Indonesia)
- Phase 307 commit `7d81eecf` — pattern reference untuk file conflict mitigation (single-file edit dengan grep verification)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`tests/e2e/helpers/wizardSelectors.ts`** (Phase 307 Wave 0): existing 8 selectors module. Extend dengan 4 selector baru untuk Phase 308 — DRY single source of truth.
- **`tests/helpers/auth.ts`**: `login(page, 'hc')` helper available — reuse untuk Phase 308 tests.
- **Pattern `ModelState.Remove("X")` di controller**: line 742 (UserId), 756 (AccessToken) — direct model untuk Phase 308 D-04.
- **Pattern `if (statusFieldWrapper) statusFieldWrapper.classList.toggle('d-none', isPrePost)`**: existing dynamic visibility pattern di JS — D-01/D-02 menambah value assignment di tempat yang sama, NO new pattern needed.

### Established Patterns
- **Server-side defensive default + explicit override**: line 975-978 set `Status = "Open"` if empty (defensive Standard mode), line 1078/1112/1170 explicit `Status = "Upcoming"` for PrePost session creation (override). Phase 308 menambah ModelState removal untuk PrePost path agar validation pass sebelum default fallback.
- **JS type-switch handler signature**: existing handler punya `var isPrePost = typeSelect.value === 'PrePostTest'; if (isPrePost) { ... } else { ... }` pattern di line 952, 1079, 1093, 1442. Phase 308 D-01/D-02 nempel di handler yang sama dengan branching ini.
- **Wave 0 test scaffold + Wave 1 implementation**: Phase 307 precedent menunjukkan pattern ini bekerja (RED → GREEN cycle).

### Integration Points
- **applyProtonMode JS handler** (or equivalent type-switch logic): single insertion point untuk D-01, D-02, D-07, D-08, D-09. Re-grep untuk locate exact (Phase 307 +47 line shift).
- **POST `CreateAssessment` controller line 779-782**: single insertion point untuk D-04, D-05.
- **Form `id` (likely `#createForm`)**: jQuery selector untuk D-07 re-parse — verify saat plan.

</code_context>

<specifics>
## Specific Ideas

- **Mirror Phase 307 Wave 0 + Wave 1 split**: Plan structure 308-01 (test scaffold + UAT.md) → 308-02 (implementation single-edit + checkpoint manual UAT). Memberikan RED test untuk verify Wave 1, plus eliminates "test infrastructure ad-hoc" risk.
- **Re-grep before edit**: Phase 307 added +47 lines, ROADMAP line refs (1790-1807) NOT canonical post-Phase 307. Researcher WAJIB locate dengan grep `value === 'PrePostTest'` dan `applyProtonMode` instead of trusting line numbers.
- **`Status = "Upcoming"` choice rationale**: Server line 1078/1112/1170 pakai "Upcoming" untuk PrePost; menjaga consistency client-server selalu. Default "Open" (line 977) hanya defensive Standard fallback.
- **jQuery validate re-parse**: standard pattern dari ASP.NET Razor unobtrusive validation — tidak ada library baru, tidak ada perubahan stack.

</specifics>

<deferred>
## Deferred Ideas

- **Wizard return-to-step-1 enhancement**: REQUIREMENTS.md Out of Scope table eksplisit excludes ini. Phase 308 hanya pastikan tidak reset; reset behavior untuk error REAL tetap.
- **Refactor wizard state machine** (resumeStep, goToStep generalization): di luar scope audit fix; di-defer ke milestone v16+ kalau ada kebutuhan.
- **Test framework migration** (e.g., Jest → Vitest, atau Playwright→Cypress): di luar scope.
- **jQuery removal / migration ke vanilla JS validation**: di luar scope (existing stack), didefer ke milestone v16+.

### Reviewed Todos (not folded)
- `realtime-assessment.md` (todo pending sejak 2026-03-09): tidak relevan dengan Phase 308 (tentang real-time assessment monitoring/streaming, bukan wizard validation). Tetap di backlog.

</deferred>

---

*Phase: 308-prepost-wizard-validation-fix*
*Context gathered: 2026-04-29 (auto mode — gray areas auto-resolved with recommended defaults)*
*Phase 307 completed 2026-04-29; +47 lines net to CreateAssessment.cshtml — re-grep anchors before edit*
