# Phase 152: Account Cleanup - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix authorization pattern, client-side validation, phone regex, ViewModel refactor, button label, and UI consistency on Account Profile and Settings pages. No new features — cleanup and bug fixes only.

</domain>

<decisions>
## Implementation Decisions

### Authorization (SEC-01)
- Add class-level `[Authorize]` to AccountController
- Add `[AllowAnonymous]` to: Login GET, Login POST, AccessDenied
- Remove manual `User.Identity?.IsAuthenticated` checks from Profile and Settings actions (now redundant)

### Validation (VAL-01, VAL-02)
- Add `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") }` to Settings.cshtml
- Change phone regex from `^[0-9]+$` to `^[\d\s\-\+\(\)]+$` in EditProfileViewModel

### ViewModel refactor (CODE-01)
- Profile page currently uses `ApplicationUser` as model + `ViewBag.UserRole`
- Change to use a ProfileViewModel that includes Role property
- Or add Role to existing model pattern — Claude's discretion on approach

### Button label (UI-01)
- Change "Edit Profile" button on Profile.cshtml to "Pengaturan"
- Keep the bi-gear icon

### Row spacing (UI-02)
- Standardize both Profile and Settings pages to `mb-3` spacing
- Profile currently uses `mb-2 py-1` — update to `mb-3`

### Claude's Discretion
- ProfileViewModel design (new ViewModel vs extend existing pattern)
- Exact icon choice if bi-gear doesn't fit "Pengaturan"
- Any minor cleanup spotted during implementation

</decisions>

<specifics>
## Specific Ideas

No specific requirements — all fixes are well-defined from the audit.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SettingsViewModel` already exists with composite pattern (EditProfile + ChangePassword sub-models)
- `_ValidationScriptsPartial.cshtml` exists in Views/Shared/

### Established Patterns
- Other controllers (HomeController, CMPController, CDPController, AdminController) use class-level `[Authorize]`
- AccountController is the only one without it

### Integration Points
- Profile.cshtml uses `@model ApplicationUser` — changing model requires updating controller action
- Settings.cshtml already uses `@model SettingsViewModel` — no change needed there
- _Layout.cshtml navbar links to Profile page

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 152-account-cleanup*
*Context gathered: 2026-03-11*
