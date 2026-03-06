# Requirements: Portal HC KPB - User Guide

**Defined:** 2026-03-06
**Last Updated:** 2026-03-06 (Updated for Phase 105: User Guide already exists, Phase 105 = improvements & gap completion)
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration and comprehensive user documentation.

## Implementation Status

**✅ User Guide infrastructure already built** (created prior to GSD Phase 105):
- HomeController.Guide() and HomeController.GuideDetail(string module) actions
- Views/Home/Guide.cshtml (hero section, search bar, module cards, FAQ with 32 questions)
- Views/Home/GuideDetail.cshtml (detailed guides per module with accordion steps)
- wwwroot/css/guide.css (complete styling system)
- _Layout.cshtml "Panduan" navbar link (line 65)
- Role-based access control (Admin/HC visibility)
- AOS animations integrated
- Client-side search functionality

**Phase 105 Focus:** Improvements, bug fixes, missing content completion, and UX polish

## v1 Requirements

Requirements for milestone v3.5 User Guide. Each maps to roadmap phases.

### Page Structure & Navigation

- [x] **GUIDE-NAV-01**: User can access Guide page via "Panduan" link in navbar after login ✅ **COMPLETE** (_Layout.cshtml line 65)
- [x] **GUIDE-NAV-02**: Guide page displays hero section with "Panduan Pengguna" title using gradient styling matching dashboard ✅ **COMPLETE** (Guide.cshtml)
- [x] **GUIDE-NAV-03**: Guide page displays 4 tab navigation buttons (CMP, CDP, Account, Admin Panel) ✅ **COMPLETE** (Dashboard excluded per user decision)
- [x] **GUIDE-NAV-04**: User can click tabs to switch between content sections without page refresh ✅ **COMPLETE** (card grid + detail page pattern)
- [x] **GUIDE-NAV-05**: Admin Panel tab is hidden for non-Admin/HC users ✅ **COMPLETE** (role-based visibility in Guide.cshtml)

### Content Organization

- [x] **GUIDE-CONTENT-01**: Each tab displays step-by-step instructions with numbered step cards (Phase 105: Add missing guides)
- [x] **GUIDE-CONTENT-02**: Step cards include icon, title, and description (Phase 105: Add missing guides)
- [ ] **GUIDE-CONTENT-03**: Important information displayed in alert boxes (tips/catatan) (Phase 105: Enhance existing)
- [x] **GUIDE-CONTENT-04**: Content organized using Bootstrap 5 accordion/collapse for sub-sections ✅ **COMPLETE** (GuideDetail.cshtml)
- [x] **GUIDE-CONTENT-05**: FAQ section displays at bottom of page with accordion behavior ✅ **COMPLETE** (Guide.cshtml, 32 FAQs)
- [x] **GUIDE-CONTENT-06**: FAQ includes common questions: login, password reset, CMP vs CDP, assessments, evidence upload, approval flow, coaching progress ✅ **COMPLETE** (32 FAQs covering all topics)

### Role-Based Access

- [x] **GUIDE-ACCESS-01**: Guide page requires authentication (non-logged users redirected to login) ✅ **COMPLETE** (HomeController [Authorize])
- [x] **GUIDE-ACCESS-02**: Role indicator badge displays user's current role at top of page ✅ **COMPLETE** (Guide.cshtml line 22)
- [x] **GUIDE-ACCESS-03**: CMP tab content available to all authenticated users ✅ **COMPLETE** (CMP guide in GuideDetail.cshtml)
- [x] **GUIDE-ACCESS-04**: CDP tab content available to all authenticated users ✅ **COMPLETE** (CDP guide in GuideDetail.cshtml)
- [x] **GUIDE-ACCESS-05**: Account tab content available to all authenticated users ✅ **COMPLETE** (Account guide in GuideDetail.cshtml)
- [x] **GUIDE-ACCESS-06**: Data tab content available only to Admin and HC users ✅ **COMPLETE** (role-based visibility in GuideDetail.cshtml)
- [x] **GUIDE-ACCESS-07**: Admin Panel tab content visible only to Admin and HC users ✅ **COMPLETE** (role-based visibility in GuideDetail.cshtml)

### Styling & UX

- [x] **GUIDE-STYLE-01**: Guide page uses CSS variables matching home.css (--gradient-primary, --shadow-*) ✅ **COMPLETE** (guide.css)
- [x] **GUIDE-STYLE-02**: Page uses Inter font family ✅ **COMPLETE** (guide.css)
- [x] **GUIDE-STYLE-03**: Cards use glassmorphism effect matching existing design system ✅ **COMPLETE** (guide.css)
- [x] **GUIDE-STYLE-04**: Page content animates using AOS library ✅ **COMPLETE** (AOS integrated)
- [x] **GUIDE-STYLE-05**: Step numbers display with gradient badges ✅ **COMPLETE** (guide.css)
- [ ] **GUIDE-STYLE-06**: Tab navigation styling matches existing design patterns (Phase 105: fix CSS bugs)
- [x] **GUIDE-STYLE-07**: Page displays correctly on mobile devices (responsive breakpoints) ✅ **COMPLETE** (guide.css responsive)
- [x] **GUIDE-STYLE-08**: All content is in Indonesian language ✅ **COMPLETE** (all content Indonesian)

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
| Dashboard/Home guide | User explicitly excluded: "dashboard home tidak perlu, hapus bagian ini" |
| Screenshots | Maintenance burden, text instructions sufficient for v3.5 |
| Feedback buttons | User explicitly excluded: "feedback tidak perlu" |
| Video placeholders | User explicitly excluded: "video placeholder hapus" |
| Progress indicators | User explicitly excluded: "progress indicator hapus" |
| Multi-language support | Indonesian only (matches portal language) |
| Anonymous access | Portal requires login for all features |
| Content management system | Static content sufficient, no admin UI needed |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| GUIDE-NAV-01 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-NAV-02 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-NAV-03 | ✅ Complete | Built prior to Phase 105 (4 tabs, Dashboard excluded) |
| GUIDE-NAV-04 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-NAV-05 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-CONTENT-01 | Phase 105 | Pending (add missing guides) |
| GUIDE-CONTENT-02 | Phase 105 | Pending (add missing guides) |
| GUIDE-CONTENT-03 | Phase 105 | Pending (enhance existing) |
| GUIDE-CONTENT-04 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-CONTENT-05 | ✅ Complete | Built prior to Phase 105 (32 FAQs) |
| GUIDE-CONTENT-06 | ✅ Complete | Built prior to Phase 105 (32 FAQs) |
| GUIDE-ACCESS-01 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-ACCESS-02 | ✅ Complete | Built prior to Phase 105 |
| GUIDE-ACCESS-03 | ✅ Complete | Built prior to Phase 105 (CMP guide) |
| GUIDE-ACCESS-04 | ✅ Complete | Built prior to Phase 105 (CDP guide) |
| GUIDE-ACCESS-05 | ✅ Complete | Built prior to Phase 105 (Account guide) |
| GUIDE-ACCESS-06 | ✅ Complete | Built prior to Phase 105 (Data guide, Admin/HC only) |
| GUIDE-ACCESS-07 | ✅ Complete | Built prior to Phase 105 (Admin Panel guide, Admin/HC only) |
| GUIDE-STYLE-01 | ✅ Complete | Built prior to Phase 105 (guide.css with variables) |
| GUIDE-STYLE-02 | ✅ Complete | Built prior to Phase 105 (Inter font) |
| GUIDE-STYLE-03 | ✅ Complete | Built prior to Phase 105 (glassmorphism) |
| GUIDE-STYLE-04 | ✅ Complete | Built prior to Phase 105 (AOS) |
| GUIDE-STYLE-05 | ✅ Complete | Built prior to Phase 105 (gradient badges) |
| GUIDE-STYLE-06 | Phase 105 | Pending (fix CSS bugs) |
| GUIDE-STYLE-07 | ✅ Complete | Built prior to Phase 105 (responsive CSS) |
| GUIDE-STYLE-08 | ✅ Complete | Built prior to Phase 105 (Indonesian content) |

**Coverage:**
- v1 requirements: 24 total
- Already complete: 21 ✅
- Phase 105 improvements: 3
- Unmapped: 0 ✓

**Phase 105 Focus:** Complete remaining gaps (3 requirements) + bug fixes + UX polish

---
*Requirements defined: 2026-03-06*
*Last updated: 2026-03-06 (Updated: User Guide infrastructure exists, Phase 105 = improvements)*
