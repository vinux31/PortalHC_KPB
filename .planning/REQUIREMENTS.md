# Requirements: Portal HC KPB

**Defined:** 2026-02-18
**Milestone:** v1.2 UX Consolidation
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v1.2 Requirements

### Assessment Page

- [ ] **ASMT-01**: Worker (Spv ke bawah) hanya melihat assessment dengan status Open atau Upcoming di Assessment page — Completed tidak tampil
- [ ] **ASMT-02**: HC dan Admin melihat tab Management (create, edit, manage questions, delete) di Assessment page
- [ ] **ASMT-03**: HC dan Admin melihat tab Monitoring (semua assessment yang sedang aktif/upcoming di seluruh sistem) di Assessment page

### Training Records

- [ ] **TREC-01**: User dapat melihat riwayat lengkap pengembangan mereka dalam satu tabel tergabung — assessment online yang selesai (dari AssessmentSession) dan training manual (dari TrainingRecord), diurutkan berdasarkan tanggal terbaru
- [ ] **TREC-02**: Tabel tergabung membedakan tipe record dengan kolom yang berbeda — Assessment Online menampilkan Score dan Pass/Fail; Training Manual menampilkan Penyelenggara, Tipe Sertifikat, dan Berlaku Sampai
- [ ] **TREC-03**: HC dan Admin dapat melihat Worker List dengan completion rate yang dihitung dari kedua sumber (assessment selesai + training manual valid)

### Gap Analysis

- [ ] **GAPS-01**: Halaman Gap Analysis dihapus — nav link di CMP Index, link di CPDP Progress, controller action, dan view dihapus seluruhnya

### Dashboard

- [ ] **DASH-01**: CDP Dashboard memiliki dua tab: Tab "Proton Progress" (semua role) dan Tab "Assessment Analytics" (HC/Admin saja)
- [ ] **DASH-02**: Tab Proton Progress menampilkan summary stats di atas, approval queue (jika role punya queue), dan tabel detail per coachee — semua di-scope sesuai role (Coachee: diri sendiri; Spv: unit; SrSpv/SectionHead: section; HC/Admin: semua)
- [ ] **DASH-03**: Tab Assessment Analytics menampilkan KPI cards, filter, tabel hasil assessment, charts, dan export Excel — menggantikan halaman HC Reports yang berdiri sendiri
- [ ] **DASH-04**: Dev Dashboard yang berdiri sendiri dihapus setelah kontennya terserap ke Tab Proton Progress

## v2 Requirements

### Notifications

- **NOTF-01**: Coach dan coachee menerima email notification saat deliverable ditolak
- **NOTF-02**: HC menerima email notification saat coachee menyelesaikan semua deliverable
- **NOTF-03**: Approver menerima email reminder untuk approval yang pending

### UX Enhancements

- **UX-01**: Session templates untuk skenario coaching yang umum
- **UX-02**: Auto-suggest IDP actions berdasarkan competency gaps
- **UX-03**: Mobile-responsive design optimizations

## Out of Scope

| Feature | Reason |
|---------|--------|
| Calendar integration | High complexity OAuth, defer to v2+ |
| AI-generated coaching suggestions | Requires LLM integration, significant stack change |
| Real-time notifications (SignalR) | Not needed for this milestone |
| Mobile app | Web-only |
| Audit logging | Future enhancement |
| Automated testing | Manual QA only |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| GAPS-01 | Phase 9 | Pending |
| TREC-01 | Phase 10 | Pending |
| TREC-02 | Phase 10 | Pending |
| TREC-03 | Phase 10 | Pending |
| ASMT-01 | Phase 11 | Pending |
| ASMT-02 | Phase 11 | Pending |
| ASMT-03 | Phase 11 | Pending |
| DASH-01 | Phase 12 | Pending |
| DASH-02 | Phase 12 | Pending |
| DASH-03 | Phase 12 | Pending |
| DASH-04 | Phase 12 | Pending |

**Coverage:**
- v1.2 requirements: 11 total
- Mapped to phases: 11
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-18*
*Last updated: 2026-02-18 after initial definition*
