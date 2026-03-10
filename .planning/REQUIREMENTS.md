# Requirements: Portal HC KPB — v3.18 Homepage Minimalist Redesign

**Defined:** 2026-03-10
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.18 Requirements

Requirements for Homepage Minimalist Redesign. Each maps to roadmap phases.

### Homepage Cleanup

- [ ] **HOME-01**: Homepage tidak menampilkan glass cards (IDP Status, Pending Assessment, Mandatory Training)
- [ ] **HOME-02**: Homepage tidak menampilkan Recent Activity timeline section
- [ ] **HOME-03**: Homepage tidak menampilkan Upcoming Deadlines section
- [ ] **HOME-04**: Controller/ViewModel tidak lagi fetch data yang tidak dipakai (activities, deadlines)

### Hero Redesign

- [ ] **HERO-01**: Hero section menggunakan styling clean tanpa glassmorphism/gradient pseudo-elements
- [ ] **HERO-02**: Hero section tetap menampilkan greeting, nama, position, unit, dan tanggal

### Quick Access

- [ ] **QUICK-01**: Quick Access cards menggunakan Bootstrap card pattern (shadow-sm, border-0) seperti CMP/CDP

### CSS Cleanup

- [x] **CSS-01**: home.css tidak mengandung unused glassmorphism styles (glass-card, backdrop-filter, blur pseudo-elements)
- [x] **CSS-02**: home.css tidak mengandung unused timeline/deadline styles
- [x] **CSS-03**: Homepage tidak menggunakan data-aos animation attributes

## Future Requirements

None for this milestone.

## Out of Scope

| Feature | Reason |
|---------|--------|
| CMP/CDP Index redesign | Focus Homepage only per user request |
| Color scheme change | User confirmed colors stay, only design/layout changes |
| Role-based Quick Access | Differentiator, defer to future milestone |
| Personalized shortcut reordering | Differentiator, defer to future milestone |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| CSS-01 | Phase 148 | Complete |
| CSS-02 | Phase 148 | Complete |
| CSS-03 | Phase 148 | Complete |
| HOME-01 | Phase 149 | Pending |
| HOME-02 | Phase 149 | Pending |
| HOME-03 | Phase 149 | Pending |
| HOME-04 | Phase 149 | Pending |
| HERO-01 | Phase 149 | Pending |
| HERO-02 | Phase 149 | Pending |
| QUICK-01 | Phase 149 | Pending |

**Coverage:**
- v3.18 requirements: 10 total
- Mapped to phases: 10
- Unmapped: 0

---
*Requirements defined: 2026-03-10*
*Last updated: 2026-03-10 after roadmap creation*
