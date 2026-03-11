# Requirements: Portal HC KPB — v3.21 Account Profile & Settings Cleanup

**Defined:** 2026-03-11
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.21 Requirements

### Security

- [ ] **SEC-01**: AccountController uses class-level `[Authorize]` with `[AllowAnonymous]` on Login, Login POST, and AccessDenied

### Validation

- [ ] **VAL-01**: Settings.cshtml includes `_ValidationScriptsPartial` for client-side form validation
- [ ] **VAL-02**: Phone number regex accepts international formats (digits, spaces, dashes, plus, parentheses)

### Code Quality

- [ ] **CODE-01**: Profile page receives Role via ViewModel instead of ViewBag

### UI/UX

- [ ] **UI-01**: Profile page "Edit Profile" button label accurately reflects destination (Settings page)
- [ ] **UI-02**: Profile and Settings pages have consistent row spacing/padding

## Out of Scope

| Feature | Reason |
|---------|--------|
| Two-Factor Authentication (2FA) | Too large, not requested |
| Password reset flow | Too large, not requested |
| Login history / session management | Too large, not requested |
| Email change functionality | Admin-managed, not user-facing |
| Account self-deactivation | Admin-only operation by design |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SEC-01 | Phase 152 | Pending |
| VAL-01 | Phase 152 | Pending |
| VAL-02 | Phase 152 | Pending |
| CODE-01 | Phase 152 | Pending |
| UI-01 | Phase 152 | Pending |
| UI-02 | Phase 152 | Pending |

**Coverage:**
- v3.21 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0

---
*Requirements defined: 2026-03-11*
*Last updated: 2026-03-11 after roadmap creation*
