# Phase 339: v20.0-gap-closure-orphan-ui-title-validator — Context

**Gathered:** 2026-06-02
**Status:** Ready for planning
**Source:** Gap closure from `/gsd-audit-milestone v20.0` (2026-06-02) + recheck verified file:line + fix mechanism

<domain>
## Phase Boundary

Surgical UI/validator fix wave closing 3 partial REQ identified by milestone audit Phase 338:

1. **CIL-06** orphan endpoint — wire UI link to existing `BulkExportPdf`
2. **REST-04** orphan route — add discoverable nav to existing `BulkBackfill`
3. **REST-06** Title regex validation missing — block invalid naming convention at create time

**NOT in scope:**
- New endpoints / new business logic (semua endpoint sudah ada di Phase 338)
- Refactor controller architecture
- VERIFICATION.md backfill untuk Phase 336/337/338 (handle via `/gsd-verify-work` terpisah)
- REQUIREMENTS.md checkbox sync (handle via `/gsd-complete-milestone v20.0` housekeeping)
- MILESTONES.md log + config.json bump (housekeeping)

</domain>

<decisions>
## Implementation Decisions (LOCKED via audit recheck 2026-06-02)

### D-01: CIL-06 button location = `_AssessmentGroupsTab.cshtml:278` (right after Export Excel dropdown-item)

**Why:** Audit recheck confirms `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:278` already has `<a class="dropdown-item">Export Excel</a>` linking to `ExportAssessmentResults`. Plan 338-04 L766 originally marked this button "opsional" → never built. Adding sibling `dropdown-item` is mechanically symmetric and discoverable from same per-group action menu.

**Action verbatim:**
```cshtml
<a class="dropdown-item"
   href="@Url.Action("BulkExportPdf", "AssessmentAdmin", new {
       title = (string)group.Title,
       category = (string)group.Category,
       scheduleDate = ((DateTime)group.Schedule).Date.ToString("yyyy-MM-dd")
   })">
    <i class="bi bi-file-zip me-2"></i>Bulk Export PDF (ZIP)
</a>
```

Place: `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` immediately after line 280 (closing `</a>` of Export Excel item).

### D-02: REST-04 nav link location = `_AssessmentGroupsTab.cshtml` per-group dropdown OR Admin top nav

**Decision:** Add SECONDARY entry di `_AssessmentGroupsTab.cshtml` dropdown (same dropdown sebagai D-01) sebagai dropdown-divider + link "Bulk Backfill Restore (Admin)". Plus PRIMARY entry di admin top nav via `Views/Shared/_Layout.cshtml` (atau partial admin nav kalau ada — discover saat task).

**Why:** Per-group dropdown gives contextual discovery saat admin sedang look-at assessment group. Top nav gives global discovery for one-time mass restore ops. Both safe — endpoint `[Authorize(Roles = "Admin")]` only.

**Action:**
- Dropdown variant (in same dropdown post-CIL-06 button):
  ```cshtml
  <div class="dropdown-divider"></div>
  <a class="dropdown-item" href="@Url.Action("BulkBackfill", "TrainingAdmin")">
      <i class="bi bi-arrow-counterclockwise me-2"></i>Bulk Backfill (Restore Lost Data)
  </a>
  ```
- Top nav: locate admin menu partial via grep, append link `/Admin/BulkBackfill` w/ icon + role check `@if (User.IsInRole("Admin"))`.

### D-03: REST-06 fix mechanism = conditional ModelState.AddModelError in `AssessmentAdminController.cs:835` (NOT data annotation on entity)

**Why:** `AssessmentSession` entity (`Models/AssessmentSession.cs:13` plain `public string Title { get; set; } = "";`) is shared across ALL assessment flows (CMP/IHT/Licencor/OTS), NOT just Cilacap Pre/Post tests. Adding `[RegularExpression]` on entity Title = break legacy data + non-test categories. ViewModel approach (`AssessmentCreateViewModel`) tidak ada — controller binds direct ke entity at `CreateAssessment(AssessmentSession model, ...)` line 822.

**Best fix:** Conditional check INSIDE controller POST L835 area (SAME guard sebagai existing auto-pair: `AssessmentTypeInput != "PrePostTest"`).

**Action verbatim (insert after existing auto-pair block L833-845, before token validation L847):**
```csharp
// Phase 339 REST-06 (336-NAMING-CONVENTION-SPEC): Validate Title pattern for standard Pre/Post tests
if (AssessmentTypeInput != "PrePostTest"
    && !string.IsNullOrEmpty(model.Title)
    && !System.Text.RegularExpressions.Regex.IsMatch(model.Title, @"^(Pre|Post)\s*Test\s+.+$"))
{
    ModelState.AddModelError("Title",
        "Title harus pola '{Stage} Test {Track} {Lokasi}' (Pre Test atau Post Test diikuti track + lokasi). " +
        "Contoh valid: 'Pre Test OJT GAST Cilacap'. Reference: 336-NAMING-CONVENTION-SPEC.");
}
```

Plus verify (or add) `<span asp-validation-for="Title" class="text-danger"></span>` di `Views/Admin/CreateAssessment.cshtml` right after line 188 input. If missing, add.

### D-04: Regex pattern = `^(Pre|Post)\s*Test\s+.+$`

**Why:** Honors existing `TryAutoDetectCounterpartGroup` tolerance pattern (cited in Phase 338-05 SUMMARY: "tolerance for whitespace inconsistency in existing DB data"). Matches: `PreTest OJT GAST`, `Pre Test OJT GAST`, `Post Test IHT Refinery`. Rejects: `Quiz Mandatory HSSE`, `Assessment OJT`, empty Track/Lokasi after "Test".

### D-05: NO new tests required

**Why:** Surgical UI + 1 controller line addition. No business logic branch. Manual UAT via Playwright sufficient (admin login → CreateAssessment with invalid title → see error → fix title → see success + auto-pair). Existing test suite `dotnet test` must still PASS 18/18 (regression smoke).

### D-06: NO new threat surface

**Why:**
- D-01 + D-02 UI links only — endpoints already exist with `[Authorize]` from Phase 338
- D-03 server-side validation = defensive HARDENING (rejects invalid before DB insert), reduces attack surface bukan tambah
- No new file uploads, no new query params, no new auth surface

### D-07: Single plan, single wave, 3 task

**Why:** Effort S (~half day). Task 1 (D-01 CIL-06 button), Task 2 (D-02 REST-04 nav — dual entry), Task 3 (D-03 REST-06 validator + view validation span). Zero file overlap between Task 1+2 (same file but different lines, sequential safe) vs Task 3 (separate controller + view files). 1 plan 1 wave acceptable.

### Claude's Discretion

- Exact icon class choice (Bootstrap Icons): `bi-file-zip`, `bi-arrow-counterclockwise`, etc. — match existing convention di `_AssessmentGroupsTab.cshtml`
- Exact wording of error message + dropdown labels — Bahasa Indonesia per project convention
- Whether top nav admin link goes in `_Layout.cshtml` global vs `_AdminNav` partial — discover during task

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Audit Source
- `.planning/v20.0-MILESTONE-AUDIT.md` — Audit report with 3 partial REQ findings + integration checker mapping table
- `.planning/ROADMAP.md` (L673-690 Coverage Validation v20.0 + Phase 339 entry) — Locked goal + REQ assignment

### Phase 338 Source-of-Truth (closes against Phase 339 fixes)
- `.planning/phases/338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute/338-04-SUMMARY.md` — CIL-06 + REST-04 endpoint delivery (button marked "opsional" L40)
- `.planning/phases/338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute/338-05-SUMMARY.md` — REST-06 auto-pair delivery (validation gap)
- `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-NAMING-CONVENTION-SPEC.md` — Title regex authoritative spec (cited di error message)

### Code Surface (LOCKED file:line, verified 2026-06-02)
- `Controllers/AssessmentAdminController.cs:822` — `CreateAssessment(AssessmentSession model, ...)` POST entry
- `Controllers/AssessmentAdminController.cs:833-845` — existing auto-pair block (REST-06 validator inserts AFTER L845)
- `Controllers/AssessmentAdminController.cs:4489` — `BulkExportPdf(string title, string category, DateTime scheduleDate)` endpoint (CIL-06 target)
- `Controllers/AssessmentAdminController.cs:6589` — `TryAutoDetectCounterpartGroup` private helper (reference for regex tolerance pattern)
- `Controllers/TrainingAdminController.cs:720` — `GET /Admin/BulkBackfill` form endpoint (REST-04 target)
- `Controllers/TrainingAdminController.cs:733` — `POST /Admin/BulkBackfillAssessment` execute endpoint
- `Models/AssessmentSession.cs:13` — `public string Title { get; set; } = "";` (DO NOT add `[RegularExpression]` here — breaks legacy)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:278-280` — existing Export Excel dropdown-item (D-01 + D-02 insert AFTER)
- `Views/Admin/CreateAssessment.cshtml:185-188` — Title `<label>` + `<input asp-for="Title">` (D-03 validation span AFTER L188)
- `Views/Admin/BulkBackfill.cshtml` — existing form view (no edit, just confirm exists)
- `Views/Shared/_Layout.cshtml` — admin top nav location (D-02 secondary nav surface)

### Project Workflow
- `CLAUDE.md` (project root) — Bahasa Indonesia responses, develop workflow lokal → Dev → Prod via Team IT
- `docs/DEV_WORKFLOW.md` — IT promotion SOP (Phase 339 bundle masuk v20.0 batch yang sudah pending push)
- `docs/SEED_WORKFLOW.md` — N/A (no seed data changes)

</canonical_refs>

<specifics>
## Specific Files To Modify

| File | Lines | Change | REQ |
|------|-------|--------|-----|
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | after L280 | Insert `dropdown-item` "Bulk Export PDF (ZIP)" → `BulkExportPdf` | CIL-06 |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | after CIL-06 insert | Insert `dropdown-divider` + `dropdown-item` "Bulk Backfill" → `BulkBackfill` | REST-04 |
| `Views/Shared/_Layout.cshtml` (or admin nav partial — discover) | admin menu section | Add `Bulk Backfill` global nav link w/ `@if (User.IsInRole("Admin"))` | REST-04 |
| `Controllers/AssessmentAdminController.cs` | after L845 (after auto-pair block, before token validation L847) | Insert conditional regex validator + `ModelState.AddModelError` | REST-06 |
| `Views/Admin/CreateAssessment.cshtml` | after L188 (Title input) | Verify or add `<span asp-validation-for="Title" class="text-danger">` | REST-06 |

## Acceptance Criteria (grep-verifiable)

- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` contains `BulkExportPdf` AND `BulkBackfill`
- One file (admin nav surface) contains `asp-action="BulkBackfill"` outside `Views/Admin/BulkBackfill.cshtml` and outside `_AssessmentGroupsTab.cshtml`
- `Controllers/AssessmentAdminController.cs` contains `Regex.IsMatch(model.Title, @"^(Pre|Post)\s*Test\s+.+$")` (or equivalent regex literal)
- `Controllers/AssessmentAdminController.cs` contains `ModelState.AddModelError("Title"` near the regex check
- `Views/Admin/CreateAssessment.cshtml` contains `asp-validation-for="Title"`
- `dotnet build` → 0 error
- `dotnet test` → 18/18 PASS (no regression)
- Manual UAT Playwright: admin → `/Admin/ManageAssessment` → group action dropdown shows both new entries → click PDF → ZIP download starts; admin → `/Admin/CreateAssessment` → title `"Quiz Random"` → submit → validation error on Title field; same form title `"Pre Test OJT GAST Cilacap"` → submit → save success + TempData Info auto-pair (if counterpart exists)

</specifics>

<deferred>
## Deferred Ideas

- VERIFICATION.md backfill for Phase 336/337/338 — separate `/gsd-verify-work` invocation, not gap closure scope
- REQUIREMENTS.md `[ ]` → `[x]` checkbox sync for all 39 v20.0 REQ — `/gsd-complete-milestone v20.0` housekeeping
- MILESTONES.md log entries for v16.0 + v19.0 + v20.0 — `/gsd-complete-milestone v20.0` housekeeping
- `.planning/config.json milestone_version` bump v16.0 → v20.0 — cosmetic, tool warns unknown anyway
- Tom Select UX pre-existing regression — separate v21.0 backlog
- Title validator as data annotation w/ ViewModel refactor — over-engineering for 3-line fix, defer indefinitely
- Top nav admin link UX polish (icon set, grouping, sidebar collapse state) — out of scope, ship minimal viable nav entry

</deferred>

---

*Phase: 339-v20.0-gap-closure-orphan-ui-title-validator*
*Context gathered: 2026-06-02 from audit recheck (skipped discuss-phase — decisions pre-locked)*
