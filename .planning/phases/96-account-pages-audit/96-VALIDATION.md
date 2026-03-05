---
phase: 96
slug: account-pages-audit
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-05
---

# Phase 96 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | None (manual testing only) |
| **Config file** | N/A — no automated test infrastructure |
| **Quick run command** | N/A |
| **Full suite command** | N/A |
| **Estimated runtime** | Manual testing (~5-10 min) |

---

## Sampling Rate

- **After every task commit:** Manual smoke test (verify specific bug fix)
- **After every plan wave:** N/A — no automated test suite
- **Before `/gsd:verify-work`:** Full suite must be green (manual browser verification)
- **Max feedback latency:** ~2 minutes (manual smoke test per bug)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 96-01-01 | 01 | 1 | ACCT-01 | Manual (code review) | Review Profile.cshtml logic | ❌ No automated tests | ⬜ pending |
| 96-01-02 | 01 | 1 | ACCT-04 | Manual (code review) | Review avatar initials logic | ❌ No automated tests | ⬜ pending |
| 96-02-01 | 02 | 1 | ACCT-02 | Manual (browser) | Smoke test password change flow | ❌ No automated tests | ⬜ pending |
| 96-02-02 | 02 | 1 | ACCT-03 | Manual (browser) | Smoke test EditProfile form | ❌ No automated tests | ⬜ pending |
| 96-03-01 | 03 | 2 | ACCT-01 | Manual (code review) | Verify phone numeric validation added | ❌ No automated tests | ⬜ pending |
| 96-03-02 | 03 | 2 | ACCT-02 | Manual (browser) | Verify password form hidden in AD mode | ❌ No automated tests | ⬜ pending |
| 96-03-03 | 03 | 2 | All | Manual (browser) | Verify alerts auto-dismiss after 5s | ❌ No automated tests | ⬜ pending |
| 96-04-01 | 04 | 2 | All | Manual (browser) | Smoke test all fixed bugs | ❌ No automated tests | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No automated test framework needed for this manual testing phase.

- Existing seed data from Phases 83 and 87 provides adequate test coverage
- No automated test framework (xUnit, NUnit) present in project
- Smoke test approach sufficient for bug fixes (code review + browser verify)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Profile displays correct user data | ACCT-01 | Browser verification required | 1. Login as test user<br>2. Navigate to /Account/Profile<br>3. Verify Nama, NIP, Email, Position, Unit all display correctly |
| Change password works | ACCT-02 | Browser flow verification | 1. Login as test user<br>2. Navigate to /Account/Settings<br>3. Change password using ChangePassword form<br>4. Verify success message and can login with new password |
| Profile edit saves correctly | ACCT-03 | Browser flow verification | 1. Login as test user<br>2. Edit FullName and Position in EditProfile form<br>3. Submit and verify data persists (refresh page) |
| Avatar initials display correctly | ACCT-04 | Visual verification | 1. Test with multi-word names (e.g., "John Doe" → "JD")<br>2. Test with single-word names (e.g., "Admin" → "A")<br>3. Test with empty names (should show "?") |

---

## Validation Sign-Off

- [ ] All tasks have manual verify steps (no automated infrastructure exists)
- [ ] Sampling continuity: manual smoke test per bug fix
- [ ] Wave 0 covers all requirements (existing seed data sufficient)
- [ ] No watch-mode flags
- [ ] Feedback latency < 2 minutes (manual verification)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
