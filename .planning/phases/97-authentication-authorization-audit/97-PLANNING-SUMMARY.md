# Phase 97 Planning Summary

**Phase:** 97 - Authentication & Authorization Audit
**Milestone:** v3.2 Bug Hunting & Quality Audit
**Status:** Planning Complete - Ready for Execution
**Date:** 2026-03-05

## Overview

Phase 97 conducts an exhaustive audit of authentication and authorization systems to identify and fix security bugs. The portal uses ASP.NET Core Identity with dual-mode authentication (local password hash and Active Directory via LDAP). Authorization uses role-based access control (RBAC) with 6 roles (Admin, HC, SrSpv/SectionHead, Coach, Coachee) and role-level attributes (1-6).

## Requirements Coverage

All 5 requirements from REQUIREMENTS.md mapped to plans:

| Requirement | Plan(s) | Verification Method |
|-------------|---------|---------------------|
| AUTH-01: Login flow works correctly (local and AD modes) | 97-01, 97-02, 97-03 | Code review + browser test |
| AUTH-02: Inactive users blocked from login | 97-01, 97-03 | Code review (line 72-76) |
| AUTH-03: AccessDenied page displays | 97-02, 97-04 | Browser verification |
| AUTH-04: Role-based navigation visibility works correctly | 97-01, 97-02, 97-04 | Grep audit + browser test |
| AUTH-05: Return URL redirect secure | 97-01, 97-02 | Code review + browser test |

## Plans Created

### Plan 97-01: Authorization Matrix Audit (Wave 1)
**Dependencies:** None
**Files Modified:**
- `.planning/phases/97-authentication-authorization-audit/97-01-AUDIT-MATRIX.md` (new)

**Tasks:**
1. Grep audit all `[Authorize]` and `User.IsInRole()` declarations
2. Build authorization matrix documenting all controller actions
3. Identify security gaps and recommend fixes

**Output:** Comprehensive authorization matrix with 6 controllers, 100+ actions categorized by access type, cookie security settings documented, security gaps identified

**Duration Estimate:** ~15 minutes

---

### Plan 97-02: Browser Verification - Critical Auth Flows (Wave 2)
**Dependencies:** 97-01
**Files Modified:**
- `.planning/phases/97-authentication-authorization-audit/97-02-VERIFICATION-GUIDE.md` (new)

**Tasks:**
1. Create browser verification guide with 5 test flows
2. Execute browser verification - login and AccessDenied
3. Execute browser verification - navigation and return URL

**Output:** Browser verification guide with step-by-step instructions, test results documenting PASS/FAIL for each flow, any bugs found with reproduction steps

**Duration Estimate:** ~30 minutes

---

### Plan 97-03: Edge Case Testing and Bug Fixes (Wave 3)
**Dependencies:** 97-01, 97-02
**Files Modified:**
- `Controllers/AccountController.cs` (if fixes needed)
- `Views/Shared/_Layout.cshtml` (if fixes needed)
- `Program.cs` (if fixes needed)

**Tasks:**
1. Fix critical security bugs from authorization matrix
2. Fix functional bugs from browser verification
3. Test edge cases - multiple roles, session expiration, role changes

**Output:** All critical and high-severity bugs fixed, edge cases tested with results documented, security-related commits

**Duration Estimate:** ~30 minutes (depends on bugs found)

---

### Plan 97-04: Regression Testing and Phase Summary (Wave 4)
**Dependencies:** 97-01, 97-02, 97-03
**Files Modified:**
- `.planning/phases/97-authentication-authorization-audit/97-PHASE-SUMMARY.md` (new)
- `.planning/STATE.md` (update)
- `.planning/ROADMAP.md` (update)

**Tasks:**
1. Regression testing - re-execute browser verification
2. Verify authorization matrix gaps resolved
3. Create phase summary
4. Update STATE.md and ROADMAP.md

**Output:** All test flows re-executed with PASS status, no regression bugs, comprehensive phase summary documenting all work

**Duration Estimate:** ~20 minutes

---

## Wave Structure

**Wave 1 (Plan 97-01):** Authorization audit - can start immediately
**Wave 2 (Plan 97-02):** Browser verification - depends on wave 1 completion
**Wave 3 (Plan 97-03):** Bug fixes - depends on waves 1-2 completion
**Wave 4 (Plan 97-04):** Regression and summary - depends on waves 1-3 completion

## Key Decisions from CONTEXT.md

### Locked Decisions (Non-Negotiable)

1. **AD Mode Testing Strategy:** Code review only — Phase 87 verified AD path via code review. AD uses same IAuthService interface - logic after authenticate is identical to local path. LdapAuthService has proper error handling, timeout, and LDAP injection prevention. No need to test AD login directly (requires AD connection that may not be available in development).

2. **Authorization Audit Scope:** Exhaustive grep audit — Grep all `[Authorize]` and `User.IsInRole()` in entire codebase. Create authorization matrix: actions → roles → gate type (attribute vs manual check). Verify consistency: Admin/HC-only actions properly gated, public actions correctly open. Document gaps: actions without proper role gates, manual checks that could be replaced with attributes.

3. **Return URL Security Testing:** Code review only — Verify `Url.IsLocalUrl(returnUrl)` exists in AccountController line 112. ASP.NET Core Url.IsLocalUrl is robust against open redirect attacks. Enough to verify implementation exists - no need to test actual attack vectors.

4. **Session & Auth Edge Cases:** Include edge cases — Test scenarios: Multiple roles: User with more than 1 role - verify role resolution; Role change during login: HC changes user role, user must re-login to get new role; Session expiration: What happens when session expires mid-action; Cookie security: Verify httpOnly, secure, sameSite settings.

5. **Test Data Strategy:** Use existing users — Database already has users in various roles. Enough to have 1 user per role for testing: Admin, HC, SrSpv, SectionHead, Coach, Coachee. No need to create seed data action (different from Phase 87/90/95).

6. **Browser Verification Approach:** Code review + spot checks — Audit code thoroughly, browser test only critical flows. Code review: Grep audit, trace authorization logic, verify security settings. Spot checks: Manual test login (local mode), access restricted pages, verify navigation. Faster than manual testing all flows - focus on high-risk areas.

7. **Security Bug Handling:** Fix immediately — Auth/authorization bugs are critical security issues. Fix inline without additional discussion, but commit with clear security-related message. User verify fixes via browser testing after commit.

8. **Cookie Security Verification:** Basic check — Verify minimum security settings in Program.cs ConfigureApplicationCookie: httpOnly: true (prevent XSS cookie theft), secure: true (HTTPS only, if SSL enabled), sameSite: Strict or Lax (prevent CSRF). Skip advanced settings (lifetime, sliding expiration, cookie name, domain, path).

9. **Bug Fix Approach:** Same as Phase 83-85 — Code review first → fix bugs → commit → user verify in browser. Fix bugs regardless of size (security bugs have no size limit). Silent bugs (not visible to user): Fix if easy (<20 lines), otherwise log and skip.

### Claude's Discretion

- Authorization matrix format for exhaustive audit results
- Which spot check scenarios are sufficient for "critical flows"
- How many edge cases are enough for multiple roles/session testing

## Success Criteria

Phase 97 is complete when:

1. **Authorization matrix created** (plan 97-01)
   - All 6 controllers audited (Account, Admin, CMP, CDP, Home, ProtonData)
   - 100+ actions categorized by access type
   - Cookie security settings documented
   - Security gaps identified and prioritized

2. **Browser verification complete** (plan 97-02)
   - 5 critical flows tested (login, AccessDenied, navigation, return URL, multiple roles)
   - Test results documented with PASS/FAIL status
   - Any bugs found documented with reproduction steps

3. **All bugs fixed** (plan 97-03)
   - Critical security bugs resolved
   - High-severity functional bugs resolved
   - Edge cases tested (multiple roles, session expiration, role changes)
   - Security-related commits with clear messages

4. **Regression testing passed** (plan 97-04)
   - All 5 flows re-tested with PASS status
   - No regression bugs introduced
   - Phase summary created documenting all work
   - STATE.md and ROADMAP.md updated

5. **All requirements verified**
   - AUTH-01: Login flow works correctly ✅
   - AUTH-02: Inactive users blocked ✅
   - AUTH-03: AccessDenied page displays ✅
   - AUTH-04: Role-based navigation visibility works ✅
   - AUTH-05: Return URL redirect secure ✅

## Estimated Total Duration

~95 minutes (1.5 hours) across all 4 plans:
- Plan 97-01: ~15 minutes (grep audit + matrix creation)
- Plan 97-02: ~30 minutes (browser testing)
- Plan 97-03: ~30 minutes (bug fixes + edge cases)
- Plan 97-04: ~20 minutes (regression + documentation)

## Next Actions

1. **Execute plan 97-01:** Run grep audits, create authorization matrix
2. **Execute plan 97-02:** Create browser verification guide, execute spot checks
3. **Execute plan 97-03:** Fix any bugs found, test edge cases
4. **Execute plan 97-04:** Regression testing, create phase summary
5. **Move to phase 98:** Data Integrity Audit (DATA-01, DATA-02, DATA-03)

---

**Phase 97 Planning Status:** ✅ COMPLETE - Ready for execution via `/gsd:execute-phase`

All 4 plans created with clear tasks, dependencies, verification criteria, and success metrics. Plans follow user decisions from CONTEXT.md and align with validation strategy from VALIDATION.md.
