---
phase: 195-certificate-signatory-settings
verified: 2026-03-18T10:00:00Z
status: passed
score: 12/12 must-haves verified
re_verification: false
---

# Phase 195: Certificate Signatory Settings — Verification Report

**Phase Goal:** Add hierarchical sub-categories to AssessmentCategory (self-referencing ParentId FK) so categories like "Mandatory HSSE Training" can have sub-categories like "Gas Tester". Admin CRUD on Manage Categories page supports creating/editing sub-categories. Also add per-category signatory name configuration for the certificate "Authorized Sig." field.
**Verified:** 2026-03-18T10:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | AssessmentCategory has nullable ParentId self-referencing FK | VERIFIED | `Models/AssessmentCategory.cs` line 20: `public int? ParentId { get; set; }` |
| 2 | AssessmentCategory has nullable SignatoryUserId FK to AspNetUsers | VERIFIED | `Models/AssessmentCategory.cs` line 23: `public string? SignatoryUserId { get; set; }` |
| 3 | Delete is restricted when category has children (DB-level) | VERIFIED | `Data/ApplicationDbContext.cs` lines 540-543: `HasOne(c => c.Parent).WithMany(c => c.Children)...OnDelete(DeleteBehavior.Restrict)` |
| 4 | SignatoryUserId set to null if referenced user is deleted | VERIFIED | `Data/ApplicationDbContext.cs` lines 546-549: `HasOne(c => c.Signatory)...OnDelete(DeleteBehavior.SetNull)` |
| 5 | Admin sees parent categories with indented child rows in ManageCategories table | VERIFIED | `Views/Admin/ManageCategories.cshtml`: tree rendering with `bi-arrow-return-right`, `ps-4`/`ps-5` indent classes |
| 6 | Admin can set a Parent Category when adding/editing a category | VERIFIED | `ManageCategories.cshtml`: `name="parentId"` in both add and edit forms; `PotentialParents` dropdown |
| 7 | Admin can select a signatory user when adding/editing a category | VERIFIED | `ManageCategories.cshtml`: `name="signatoryUserId"` with `ViewBag.AllUsers` in both forms |
| 8 | P-Sign preview appears below signatory dropdown after selection | VERIFIED | `ManageCategories.cshtml`: `id="psignPreview"` with JS `bindSignatoryPreview` wired to both add/edit forms |
| 9 | CreateAssessment wizard shows optgroup-grouped category dropdown | VERIFIED | `Views/Admin/CreateAssessment.cshtml`: `<optgroup label="@parent.Name">` with `ViewBag.ParentCategories` |
| 10 | EditAssessment shows same optgroup-grouped category dropdown | VERIFIED | `Views/Admin/EditAssessment.cshtml`: `<optgroup label="@parent.Name">` with `ViewBag.ParentCategories` |
| 11 | Delete button is disabled with tooltip when category has children | VERIFIED | `ManageCategories.cshtml` lines 285-289: `<button ... disabled title="Hapus sub-kategori terlebih dahulu...">` |
| 12 | Certificate HTML shows dynamic P-Sign from category signatory with fallback | VERIFIED | `Views/CMP/Certificate.cshtml`: `ViewBag.PSign` cast and rendered; `CMPController.cs`: `ResolveCategorySignatory` with parent chain and "HC Manager" fallback |

**Score:** 12/12 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/AssessmentCategory.cs` | ParentId, SignatoryUserId, navigation properties | VERIFIED | All 5 properties present |
| `Data/ApplicationDbContext.cs` | Fluent API for self-ref FK + signatory FK | VERIFIED | `DeleteBehavior.Restrict` + `DeleteBehavior.SetNull` both present |
| `Migrations/20260318023131_AddParentAndSignatoryToAssessmentCategory.cs` | AddColumn for ParentId and SignatoryUserId + FKs | VERIFIED | Migration file exists with both AddColumn operations and AddForeignKey |
| `Controllers/AdminController.cs` | SetCategoriesViewBag helper, updated CRUD, delete guard | VERIFIED | `SetCategoriesViewBag()` at line 757, `AnyAsync(c => c.ParentId == id)` at line 896 |
| `Views/Admin/ManageCategories.cshtml` | Tree table, parent dropdown, signatory dropdown, P-Sign preview | VERIFIED | All acceptance criteria patterns found |
| `Views/Admin/CreateAssessment.cshtml` | optgroup category dropdown | VERIFIED | `<optgroup label=` present, `ViewBag.ParentCategories` used |
| `Views/Admin/EditAssessment.cshtml` | optgroup category dropdown | VERIFIED | `<optgroup label=` present, `ViewBag.ParentCategories` used |
| `Controllers/CMPController.cs` | ResolveCategorySignatory helper, ViewBag.PSign in Certificate and CertificatePdf | VERIFIED | Private method at line 2373, ViewBag.PSign at line 2369, pSign local at line 2432 |
| `Views/CMP/Certificate.cshtml` | Design A2 header + P-Sign footer, no QR code | VERIFIED | `psign-pertamina.png` x2, `HC PORTAL KPB`, `Human Capital Development Portal`, `ViewBag.PSign`, no QR references |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `AssessmentCategory.ParentId` | `AssessmentCategory.Id` | self-referencing FK | WIRED | `HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId)` in DbContext |
| `AssessmentCategory.SignatoryUserId` | `AspNetUsers.Id` | FK with SetNull delete | WIRED | `HasOne(c => c.Signatory)...OnDelete(DeleteBehavior.SetNull)` in DbContext |
| `AdminController.ManageCategories` | `ViewBag.AllUsers` | user list for signatory dropdown | WIRED | `_userManager.Users...ToListAsync()` then `ViewBag.AllUsers = allUsers` |
| `AdminController.AddCategory` | `AssessmentCategory.ParentId` | form field parentId | WIRED | `ParentId = parentId` in category object initializer |
| `CreateAssessment.cshtml` | `ViewBag.ParentCategories` | optgroup rendering | WIRED | `optgroup label="@parent.Name"` iterates `ViewBag.ParentCategories` |
| `CMPController.Certificate` | `AssessmentCategories` | signatory lookup by category name | WIRED | `FirstOrDefaultAsync(c => c.Name == categoryName)` in `ResolveCategorySignatory` |
| `Certificate.cshtml` | `ViewBag.PSign` | P-Sign rendering in footer | WIRED | `var pSign = (HcPortal.Models.PSignViewModel?)ViewBag.PSign` then rendered |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| R195-1 | 195-01 | AssessmentCategory gains nullable ParentId (self-ref FK) for parent-children hierarchy | SATISFIED | Model property + DbContext Fluent API + Migration all present |
| R195-2 | 195-02 | Admin Manage Categories UI shows parent categories with expandable sub-categories | SATISFIED | Tree table with indented children, parent/delete-disabled logic in ManageCategories.cshtml |
| R195-3 | 195-02 | CreateAssessment wizard category dropdown shows grouped options (parent > sub) | SATISFIED | optgroup-based dropdowns in both CreateAssessment.cshtml and EditAssessment.cshtml |
| R195-4 | 195-01, 195-03 | Per-category SignatoryName field stored in AssessmentCategory, displayed on certificate | SATISFIED | SignatoryUserId FK on model; ResolveCategorySignatory chain; Design A2 certificate footer |

---

### Anti-Patterns Found

None blocking. Build compiles with 0 errors, 70 warnings (all pre-existing platform warnings, none from phase 195 files).

---

### Human Verification Required

#### 1. Tree table renders correctly in browser

**Test:** Navigate to Admin > Manage Categories. Create a parent category, then add a sub-category under it.
**Expected:** Sub-category appears indented with arrow icon below parent row; parent row shows "Parent" badge.
**Why human:** Tree rendering depends on runtime data; cannot verify without seeded parent-child rows.

#### 2. Signatory P-Sign preview appears on selection

**Test:** In the Add Category form, select a user from the Penandatangan dropdown.
**Expected:** P-Sign preview div appears immediately below, showing Pertamina logo + user's Position + FullName.
**Why human:** JavaScript behavior (tom-select + custom bindSignatoryPreview) requires browser interaction.

#### 3. Certificate displays correct signatory for a categorised assessment

**Test:** Assign a signatory to a category, then view the HTML certificate for an assessment in that category.
**Expected:** Footer shows Pertamina logo + signatory's Position + FullName (not "HC Manager").
**Why human:** End-to-end flow requires seeded data and a completed assessment session.

#### 4. Certificate falls back to "HC Manager" when no signatory is set

**Test:** View certificate for assessment whose category has no signatory configured.
**Expected:** Footer shows Pertamina logo + "HC Manager" label with empty name.
**Why human:** Requires a completed assessment with a signatory-free category.

#### 5. optgroup category dropdown pre-selects correctly in EditAssessment

**Test:** Edit an existing assessment that belongs to a sub-category.
**Expected:** The dropdown pre-selects the correct leaf option inside its optgroup.
**Why human:** Selection behaviour with `@(cat.Name == Model.Category)` requires runtime data.

---

## Summary

All 12 must-have truths are verified. All phase artifacts exist and are substantive (no stubs). All key wiring links are confirmed through code inspection. The migration is present and correctly adds both `ParentId` and `SignatoryUserId` columns with appropriate FK constraints. The build is clean (0 errors). Five items require human browser verification to confirm runtime behaviour.

---

_Verified: 2026-03-18T10:00:00Z_
_Verifier: Claude (gsd-verifier)_
