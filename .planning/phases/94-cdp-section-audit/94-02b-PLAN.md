---
wave: 1
depends_on:
  - 94-00
files_modified:
  - Controllers/CDPController.cs
  - Views/CDP/CoachingProton.cshtml
  - Models/ProtonViewModels.cs
autonomous: true
requirements:
  - CDP-05
---

# Plan 94-02b: Coaching Workflow Coachee Scope and Approval Fixes

**Goal:** Verify CoachingProton page shows correct coachee lists and approval workflows work for all roles

**Success criteria:**
1. Coachee dropdown shows only assigned coachees (Coach role) or correct scope (Spv/SH)
2. Coaching session submission flow works end-to-end
3. Approval workflow displays correctly per role (Coach → SrSpv → SectionHead → HC)
4. Pagination works without splitting coachee groups
5. Excel and PDF exports work for authorized roles

---

## Tasks

### Task 94-02b-01: Fix coachee scope and approval workflow bugs

<files>
- Controllers/CDPController.cs
- Views/CDP/CoachingProton.cshtml
</files>

<action>
Ensure coachee dropdown shows correct scope per role and approval workflows work

Steps:
1. Verify coachee scope queries include IsActive filter:
   ```csharp
   // Coach scope
   .Where(m => m.CoachId == coachId && m.IsActive)

   // SrSpv/SH scope
   .Where(c => c.Unit == user.Unit && c.RoleLevel < user.RoleLevel && c.IsActive)
   ```
2. Verify SrSpv/SH (level 4) coachee dropdown exists in view
3. Verify approval workflow displays correctly per role:
   - Coach: Can submit coaching sessions
   - SrSpv: Can approve/reject at Spv level
   - SectionHead: Can approve/reject at SH level
   - HC: Can do final review
4. Verify AJAX approval actions have ValidateAntiForgeryToken:
   ```csharp
   [HttpPost]
   [ValidateAntiForgeryToken]
   public async Task<IActionResult> ApproveFromProgress(int progressId)
   ```
</action>

<verify>
Build passes
Coachee dropdown shows correct scope for all roles
Approval workflow buttons show for correct roles
AJAX POST actions have CSRF protection
</verify>

<done>
Commit created:
fix(cdp): fix coachee scope and approval workflow bugs

- Add IsActive filter to all coachee scope queries
- Ensure SrSpv/SH (level 4) coachee dropdown exists
- Verify approval workflow displays correctly per role
- Add ValidateAntiForgeryToken to AJAX approval actions

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
</done>

---

## Verification Criteria

**Manual browser verification:**
1. Log in as Coach → verify coachee dropdown shows assigned coachees only
2. Log in as Spv → verify coachee dropdown shows unit-level scope
3. Log in as SectionHead → verify coachee dropdown shows section-level scope
4. Log in as HC/Admin → verify full coachee list with all scopes
5. Submit coaching session as Coach → verify status changes to "Submitted"
6. Approve session as Spv → verify SrSpvApprovalStatus changes to "Approved"
7. Approve session as SectionHead → verify ShApprovalStatus changes to "Approved"
8. Test pagination → verify coachee groups are not split across pages
9. Test Excel/PDF export → verify files download correctly

**Expected result:** CDP-05 requirements PASS — coachee lists correct, approval workflows work, session flows complete end-to-end

---

## Must-Haves (Goal-Backward Verification)

If any of these are FALSE, the plan failed:

1. [ ] Coachee dropdown shows correct scope per role (with IsActive filter)
2. [ ] SrSpv/SH (level 4) coachee dropdown exists and works
3. [ ] Approval workflow works: Coach submit → Spv approve → SH approve → HC final
4. [ ] Pagination preserves coachee group boundaries
5. [ ] AJAX approval actions have CSRF protection
6. [ ] Build passes without errors

---

*Plan created: 2026-03-05*
*Phase: 94-cdp-section-audit*
*Requirements: CDP-05*
