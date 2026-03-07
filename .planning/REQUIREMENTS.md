# Requirements: Portal HC KPB v3.8

**Defined:** 2026-03-07
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.8 Requirements

### Button Conversion

- [ ] **BTN-01**: Clickable Pending badge di kolom SrSpv berubah menjadi proper button element yang jelas clickable
- [ ] **BTN-02**: Clickable Pending badge di kolom SH berubah menjadi proper button element yang jelas clickable
- [ ] **BTN-03**: Semua status badges (Approved, Rejected, Pending, Reviewed) menampilkan icon yang sesuai

### Button Consistency

- [ ] **CONS-01**: Evidence column memiliki style yang konsisten antara Submit button dan status badges
- [ ] **CONS-02**: "Lihat Detail" button di kolom Detail lebih standout dan tidak washed out
- [ ] **CONS-03**: HC Review button style konsisten antara main table dan Antrian Review panel
- [ ] **CONS-04**: Export, Reset, dan Kembali buttons memiliki style yang polished dan konsisten

### Technical Quality

- [ ] **TECH-01**: Semua JS event handlers tetap berfungsi setelah redesign (btnTinjau, btnSubmitEvidence, btnHcReview, btnHcReviewPanel)
- [ ] **TECH-02**: Modal triggers (data-bs-toggle, data-bs-target) tetap bekerja untuk approval flow
- [ ] **TECH-03**: AJAX innerHTML updates di JS menggunakan class/style baru yang konsisten

## Out of Scope

| Feature | Reason |
|---------|--------|
| Other CDP pages (PlanIdp, Deliverable, HistoriProton) | Scope limited to CoachingProton only |
| Backend/controller changes | Pure frontend redesign |
| New JS libraries or CSS frameworks | Bootstrap 5 is sufficient |
| Table structure/layout changes | Only button/badge visual changes |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| BTN-01 | TBD | Pending |
| BTN-02 | TBD | Pending |
| BTN-03 | TBD | Pending |
| CONS-01 | TBD | Pending |
| CONS-02 | TBD | Pending |
| CONS-03 | TBD | Pending |
| CONS-04 | TBD | Pending |
| TECH-01 | TBD | Pending |
| TECH-02 | TBD | Pending |
| TECH-03 | TBD | Pending |

**Coverage:**
- v3.8 requirements: 10 total
- Mapped to phases: 0
- Unmapped: 10

---
*Requirements defined: 2026-03-07*
