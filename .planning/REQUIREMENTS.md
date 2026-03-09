# Requirements: Portal HC KPB — v3.13 In-App Notifications

**Defined:** 2026-03-09
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v3.13 Requirements

### Infrastructure (INFRA)

- [ ] **INFRA-01**: Bell icon di navbar menampilkan unread notification count badge untuk semua authenticated users
- [ ] **INFRA-02**: Dropdown notification list muncul saat bell icon di-click, menampilkan notifikasi terbaru dengan title, message, dan timestamp
- [ ] **INFRA-03**: User dapat mark notification as read (individual dan mark all as read)
- [ ] **INFRA-04**: User dapat dismiss/hapus notification dari list
- [ ] **INFRA-05**: Notification helper service yang bisa dipanggil dari controller mana saja untuk create UserNotification

### Coaching Proton Triggers (COACH)

- [x] **COACH-01**: Coach menerima notifikasi saat di-assign coachee baru via CoachCoacheeMappingAssign
- [x] **COACH-02**: Coach dan coachee menerima notifikasi saat mapping di-edit (coach/unit berubah)
- [x] **COACH-03**: Coach dan coachee menerima notifikasi saat mapping di-deactivate
- [ ] **COACH-04**: SrSpv/SectionHead menerima notifikasi saat deliverable di-submit oleh coach (perlu review)
- [ ] **COACH-05**: Coachee dan coach menerima notifikasi saat deliverable di-approve (SrSpv/SH)
- [ ] **COACH-06**: Coachee dan coach menerima notifikasi saat deliverable di-reject (perlu resubmit)
- [ ] **COACH-07**: Semua HC users menerima notifikasi saat semua deliverable coachee complete (migrasi dari ProtonNotification ke UserNotification)

### Assessment Triggers (ASMT)

- [ ] **ASMT-01**: Worker menerima notifikasi saat assessment baru di-assign
- [ ] **ASMT-02**: HC/Admin menerima notifikasi saat semua worker dalam satu assessment group selesai ujian

## Out of Scope

| Feature | Reason |
|---------|--------|
| Email notifications | In-app only untuk v3.13 |
| Notification preferences/settings | Semua user terima semua yang relevan |
| Real-time push (WebSocket/SignalR) | Polling/page refresh cukup untuk v3.13 |
| Notification untuk admin/data events | Training records, worker management, silabus — terlalu noisy |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 130 | Pending |
| INFRA-02 | Phase 130 | Pending |
| INFRA-03 | Phase 130 | Pending |
| INFRA-04 | Phase 130 | Pending |
| INFRA-05 | Phase 130 | Pending |
| COACH-01 | Phase 131 | Complete |
| COACH-02 | Phase 131 | Complete |
| COACH-03 | Phase 131 | Complete |
| COACH-04 | Phase 131 | Pending |
| COACH-05 | Phase 131 | Pending |
| COACH-06 | Phase 131 | Pending |
| COACH-07 | Phase 131 | Pending |
| ASMT-01 | Phase 132 | Pending |
| ASMT-02 | Phase 132 | Pending |

**Coverage:**
- v3.13 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0

---
*Requirements defined: 2026-03-09*
*Last updated: 2026-03-09 after roadmap creation — all 14 requirements mapped*
