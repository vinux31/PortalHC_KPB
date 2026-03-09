# Requirements: Portal HC KPB — v3.14 Bug Hunting Per Case

**Defined:** 2026-03-09
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.14 Requirements

### Assessment Lifecycle (ASMT)

- [x] **ASMT-01**: Admin dapat create assessment baru dengan question package, assign worker, dan set schedule tanpa error
- [ ] **ASMT-02**: Worker dapat mulai exam, menjawab soal, auto-save berfungsi, dan submit exam berhasil
- [ ] **ASMT-03**: Results ditampilkan dengan benar (score, pass/fail, competency earned) setelah submit
- [ ] **ASMT-04**: Records/Riwayat menampilkan history assessment dan training dengan filter yang benar
- [x] **ASMT-05**: HC monitoring menampilkan live progress, status, dan aksi (reset, force close) berfungsi
- [ ] **ASMT-06**: Notifikasi assessment (assign + group completion) terkirim ke user yang benar

### Coaching Proton Lifecycle (COACH)

- [ ] **COACH-01**: Admin dapat assign/edit/deactivate coaching mapping dan notifikasi terkirim
- [ ] **COACH-02**: Coachee dapat upload evidence dan submit deliverable tanpa error
- [ ] **COACH-03**: Approval chain berfungsi (SrSpv → SectionHead → HC) dengan notifikasi di setiap step
- [ ] **COACH-04**: Export PDF dan Excel dari CoachingProton page berfungsi
- [ ] **COACH-05**: Histori Proton menampilkan timeline yang benar per worker dengan data yang akurat

### PlanIDP & Deliverable (IDP)

- [ ] **IDP-01**: PlanIDP menampilkan Silabus dan Coaching Guidance tabs dengan data yang benar per role
- [ ] **IDP-02**: Deliverable page menampilkan progress tracking yang benar
- [ ] **IDP-03**: CDP Dashboard menampilkan data yang benar per role (Proton Progress + Assessment Analytics)

### Admin Data Management (ADM)

- [ ] **ADM-01**: ManageWorkers CRUD, import template, dan export berfungsi tanpa error
- [ ] **ADM-02**: ProtonData tabs (Silabus, Coaching Guidance, Override) CRUD berfungsi
- [ ] **ADM-03**: ManageAssessment create/edit/delete dan AssessmentMonitoring berfungsi

### General & Cross-cutting (GEN)

- [ ] **GEN-01**: Login flow (local + AD), logout, dan inactive user block berfungsi
- [ ] **GEN-02**: Homepage dashboard menampilkan data yang benar per role
- [ ] **GEN-03**: Notification bell icon, dropdown, mark read, dismiss berfungsi
- [ ] **GEN-04**: Profile view dan settings (edit nama, change password) berfungsi
- [ ] **GEN-05**: Navigasi antar menu konsisten, tidak ada broken link atau unauthorized access

## Out of Scope

| Feature | Reason |
|---------|--------|
| Penambahan fitur baru | Fokus audit bug saja |
| Refactoring/optimization | Hanya fix bug yang ditemukan |
| Mobile responsive audit | Desktop-first untuk v3.14 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ASMT-01 | Phase 133 | Complete |
| ASMT-02 | Phase 133 | Pending |
| ASMT-03 | Phase 133 | Pending |
| ASMT-04 | Phase 133 | Pending |
| ASMT-05 | Phase 133 | Complete |
| ASMT-06 | Phase 133 | Pending |
| COACH-01 | Phase 134 | Pending |
| COACH-02 | Phase 134 | Pending |
| COACH-03 | Phase 134 | Pending |
| COACH-04 | Phase 134 | Pending |
| COACH-05 | Phase 134 | Pending |
| IDP-01 | Phase 135 | Pending |
| IDP-02 | Phase 135 | Pending |
| IDP-03 | Phase 135 | Pending |
| ADM-01 | Phase 136 | Pending |
| ADM-02 | Phase 136 | Pending |
| ADM-03 | Phase 136 | Pending |
| GEN-01 | Phase 137 | Pending |
| GEN-02 | Phase 137 | Pending |
| GEN-03 | Phase 137 | Pending |
| GEN-04 | Phase 137 | Pending |
| GEN-05 | Phase 137 | Pending |

**Coverage:**
- v3.14 requirements: 22 total
- Mapped to phases: 22
- Unmapped: 0

---
*Requirements defined: 2026-03-09*
*Last updated: 2026-03-09 after roadmap creation*
