---
phase: 97
slug: authentication-authorization-audit
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 97 — Validation Strategy

> Per-phase validation contract for authentication & authorization audit.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing + code review audit |
| **Config file** | none |
| **Quick run command** | n/a (manual verification) |
| **Full suite command** | n/a (manual verification) |
| **Estimated runtime** | ~30-45 minutes for spot checks |

---

## Sampling Rate

- **After every task commit:** Manual verification of specific bug fix
- **After every plan wave:** Code review audit summary review
- **Before `/gsd:verify-work`:** All audit findings documented, all bugs fixed
- **Max feedback latency:** ~5 minutes per spot check

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Verification Method | Status |
|---------|------|------|-------------|-----------|---------------------|--------|
| 97-01-01 | 01 | 1 | AUTH-01 | code-review | Authorization matrix review | ⬜ pending |
| 97-01-02 | 01 | 1 | AUTH-02 | code-review | Cookie settings audit | ⬜ pending |
| 97-02-01 | 02 | 2 | AUTH-03 | browser-test | Login flow spot check (local) | ⬜ pending |
| 97-02-02 | 02 | 2 | AUTH-04 | browser-test | AccessDenied page spot check | ⬜ pending |
| 97-02-03 | 02 | 2 | AUTH-05 | browser-test | Return URL redirect spot check | ⬜ pending |
| 97-03-01 | 03 | 3 | AUTH-01 | code-review+browser | Multi-role resolution test | ⬜ pending |
| 97-03-02 | 03 | 3 | AUTH-02 | code-review+browser | Session expiration behavior | ⬜ pending |
| 97-04-01 | 04 | 4 | AUTH-01-AUTH-05 | regression | Verify fixes don't break flows | ⬜ pending |

*Status: ⬜ pending · ✅ verified · ❌ bug found · ⚠️ needs investigation*

---

## Wave 0 Requirements

- [ ] Authorization audit checklist prepared (Excel template with columns: Controller, Action, Role Gate Type, Expected Roles, Notes)
- [ ] Browser test users identified (1 user per role: Admin, HC, SrSpv, SectionHead, Coach, Coachee)
- [ ] Cookie security baseline documented (ASP.NET Core Identity default settings)

*Existing infrastructure: No automated test framework required for audit phase.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Login flow (local mode) | AUTH-01 | Requires browser session | 1. Navigate to /Account/Login 2. Enter valid credentials 3. Verify redirect to returnUrl or Home 4. Check auth cookie in DevTools |
| Inactive user block | AUTH-01 | Requires database state | 1. Set user.IsActive = false in DB 2. Attempt login 3. Verify "account inactive" error 4. Reset IsActive = true |
| AccessDenied page | AUTH-04 | Requires auth redirect | 1. Login as Coachee 2. Direct URL to /Admin/Index 3. Verify redirect to /Account/AccessDenied |
| Multi-role resolution | AUTH-01 | Requires role assignment | 1. Assign user to both Admin and HC roles 2. Login 3. Verify access to both Admin and HC-only pages |
| Return URL security | AUTH-05 | Requires open redirect test | 1. Login with returnUrl=http://evil.com 2. Verify redirect blocked (fallback to Home) |
| Cookie security settings | AUTH-02 | Requires DevTools inspection | 1. Login 2. Check auth cookie in DevTools 3. Verify httpOnly, secure, sameSite attributes |

---

## Validation Sign-Off

- [ ] Authorization matrix complete and reviewed
- [ ] All manual spot checks completed
- [ ] All identified bugs fixed and verified
- [ ] Regression test passed
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
