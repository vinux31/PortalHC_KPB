# Requirements: Portal HC KPB - User Guide

**Defined:** 2026-03-06
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration and comprehensive user documentation.

## v1 Requirements

Requirements for milestone v3.5 User Guide. Each maps to roadmap phases.

### Page Structure & Navigation

- [ ] **GUIDE-NAV-01**: User can access Guide page via "Panduan" link in navbar after login
- [ ] **GUIDE-NAV-02**: Guide page displays hero section with "Panduan Pengguna" title using gradient styling matching dashboard
- [ ] **GUIDE-NAV-03**: Guide page displays 5 tab navigation buttons (Dashboard, CMP, CDP, Account, Admin Panel)
- [ ] **GUIDE-NAV-04**: User can click tabs to switch between content sections without page refresh
- [ ] **GUIDE-NAV-05**: Admin Panel tab is hidden for non-Admin/HC users

### Content Organization

- [ ] **GUIDE-CONTENT-01**: Each tab displays step-by-step instructions with numbered step cards
- [ ] **GUIDE-CONTENT-02**: Step cards include icon, title, and description
- [ ] **GUIDE-CONTENT-03**: Important information displayed in alert boxes (tips/catatan)
- [ ] **GUIDE-CONTENT-04**: Content organized using Bootstrap 5 accordion/collapse for sub-sections
- [ ] **GUIDE-CONTENT-05**: FAQ section displays at bottom of page with accordion behavior
- [ ] **GUIDE-CONTENT-06**: FAQ includes common questions: login, password reset, CMP vs CDP, assessments, evidence upload, approval flow, coaching progress

### Role-Based Access

- [ ] **GUIDE-ACCESS-01**: Guide page requires authentication (non-logged users redirected to login)
- [ ] **GUIDE-ACCESS-02**: Role indicator badge displays user's current role at top of page
- [ ] **GUIDE-ACCESS-03**: Dashboard tab content available to all authenticated users
- [ ] **GUIDE-ACCESS-04**: CMP tab content available to all authenticated users
- [ ] **GUIDE-ACCESS-05**: CDP tab content available to all authenticated users
- [ ] **GUIDE-ACCESS-06**: Account tab content available to all authenticated users
- [ ] **GUIDE-ACCESS-07**: Admin Panel tab content visible only to Admin and HC users

### Styling & UX

- [ ] **GUIDE-STYLE-01**: Guide page uses CSS variables matching home.css (--gradient-primary, --shadow-*)
- [ ] **GUIDE-STYLE-02**: Page uses Inter font family
- [ ] **GUIDE-STYLE-03**: Cards use glassmorphism effect matching existing design system
- [ ] **GUIDE-STYLE-04**: Page content animates using AOS library
- [ ] **GUIDE-STYLE-05**: Step numbers display with gradient badges
- [ ] **GUIDE-STYLE-06**: Tab navigation styling matches existing design patterns
- [ ] **GUIDE-STYLE-07**: Page displays correctly on mobile devices (responsive breakpoints)
- [ ] **GUIDE-STYLE-08**: All content is in Indonesian language

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Interactive Features

- **GUIDE-INTER-01**: Video tutorials for each module
- **GUIDE-INTER-02**: Interactive walkthrough tours
- **GUIDE-INTER-03**: Search functionality to find specific topics
- **GUIDE-INTER-04**: Screenshots with annotations
- **GUIDE-INTER-05**: Context-sensitive help from each page

### Advanced Content

- **GUIDE-ADV-01**: Admin/HC-specific detailed guides
- **GUIDE-ADV-02**: Printable PDF versions of each guide
- **GUIDE-ADV-03**: Troubleshooting guides for common issues
- **GUIDE-ADV-04**: Best practices and tips per role

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Video tutorials | High production effort, defer to v2+ |
| Interactive walkthrough tours | Complex implementation, defer to v2+ |
| Search functionality | Static content small enough for navigation, defer to v2+ |
| Screenshots | Maintenance burden, text instructions sufficient for v3.5 |
| Multi-language support | Indonesian only (matches portal language) |
| Anonymous access | Portal requires login for all features |
| Content management system | Static content sufficient, no admin UI needed |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| GUIDE-NAV-01 | Phase 105 | Pending |
| GUIDE-NAV-02 | Phase 105 | Pending |
| GUIDE-NAV-03 | Phase 105 | Pending |
| GUIDE-NAV-04 | Phase 105 | Pending |
| GUIDE-NAV-05 | Phase 105 | Pending |
| GUIDE-CONTENT-01 | Phase 105 | Pending |
| GUIDE-CONTENT-02 | Phase 105 | Pending |
| GUIDE-CONTENT-03 | Phase 105 | Pending |
| GUIDE-CONTENT-04 | Phase 105 | Pending |
| GUIDE-CONTENT-05 | Phase 105 | Pending |
| GUIDE-CONTENT-06 | Phase 105 | Pending |
| GUIDE-ACCESS-01 | Phase 105 | Pending |
| GUIDE-ACCESS-02 | Phase 105 | Pending |
| GUIDE-ACCESS-03 | Phase 105 | Pending |
| GUIDE-ACCESS-04 | Phase 105 | Pending |
| GUIDE-ACCESS-05 | Phase 105 | Pending |
| GUIDE-ACCESS-06 | Phase 105 | Pending |
| GUIDE-ACCESS-07 | Phase 105 | Pending |
| GUIDE-STYLE-01 | Phase 106 | Pending |
| GUIDE-STYLE-02 | Phase 106 | Pending |
| GUIDE-STYLE-03 | Phase 106 | Pending |
| GUIDE-STYLE-04 | Phase 106 | Pending |
| GUIDE-STYLE-05 | Phase 106 | Pending |
| GUIDE-STYLE-06 | Phase 106 | Pending |
| GUIDE-STYLE-07 | Phase 106 | Pending |
| GUIDE-STYLE-08 | Phase 106 | Pending |

**Coverage:**
- v1 requirements: 24 total
- Mapped to phases: 24
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-06*
*Last updated: 2026-03-06 after initial definition*
