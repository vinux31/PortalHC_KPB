# Requirements: Portal HC KPB — v8.2 Proton Coaching Ecosystem Audit

**Defined:** 2026-03-22
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v8.2 Requirements

Requirements for Proton Coaching ecosystem audit — end-to-end audit dari setup hingga completion, plus differentiator enhancement.

### Riset & Perbandingan

- [x] **RSCH-01**: Browse langsung demo/website minimal 3 platform coaching (360Learning, BetterUp, CoachHub) — screenshot dan dokumentasi UX/flow
- [x] **RSCH-02**: Dokumen perbandingan UX/flow portal KPB vs platform luar per area Proton (Setup, Execution, Monitoring, Completion)
- [x] **RSCH-03**: Rekomendasi improvement prioritas berdasarkan gap antara portal vs best practices

### Audit Setup Flow

- [x] **SETUP-01**: Audit Silabus delete — tambah impact count warning sebelum hard delete, soft delete jika ada progress aktif
- [x] **SETUP-02**: Audit Coaching Guidance — file management integrity (upload/replace/delete), validasi tipe file
- [x] **SETUP-03**: Audit Coach-Coachee Mapping — tambah explicit DB transaction pada cascade deactivation, validasi duplikasi
- [x] **SETUP-04**: Audit Track Assignment — progression validation Tahun 1→2→3, seed ProtonDeliverableProgress correctness
- [x] **SETUP-05**: Audit Import/Export Silabus dan Mapping — validasi data, error handling, template accuracy

### Audit Execution Flow

- [x] **EXEC-01**: Audit Evidence submission flow end-to-end — upload, reject+resubmit, multi-file handling, verifikasi completeness
- [x] **EXEC-02**: Audit Approval chain — verifikasi state consistency di edge cases (concurrent approve, Override admin, partial approval)
- [x] **EXEC-03**: Audit DeliverableStatusHistory — verifikasi completeness insert di setiap state transition termasuk initial Pending
- [x] **EXEC-04**: Audit Notifikasi — verifikasi semua Proton notification triggers terpanggil (evidence submit, approve, reject, HC review, final assessment)
- [x] **EXEC-05**: Audit PlanIdp view — silabus display accuracy, guidance tabs, role-based access correctness

### Audit Completion

- [x] **COMP-01**: Audit Final Assessment — tambah DB unique constraint pada ProtonTrackAssignmentId, competency level granting accuracy
- [x] **COMP-02**: Audit Coaching Sessions — linkage ke deliverable progress, action items status tracking, session CRUD integrity
- [x] **COMP-03**: Audit HistoriProton — timeline accuracy, legacy CoachingLog coexistence, data completeness
- [x] **COMP-04**: Audit 3-year journey — Tahun 1→2→3 lifecycle end-to-end, assignment transition, completion flow

### Audit Monitoring

- [x] **MON-01**: Audit Dashboard — role-scoped filtering accuracy, stats correctness, Chart.js data integrity
- [x] **MON-02**: Audit CoachingProton tracking — filter cascade, pagination, role-based column visibility
- [x] **MON-03**: Audit Override — validasi status transition rules, audit trail lengkap, admin accountability
- [x] **MON-04**: Audit Export — data accuracy, query optimization (N+1 elimination, projection), semua export actions

### Differentiator Enhancement

- [x] **DIFF-01**: Workload indicator coach — tampilkan jumlah coachee aktif per coach di mapping page dan dashboard
- [x] **DIFF-02**: Batch approval HC Review — approve multiple deliverables sekaligus dari monitoring view
- [x] **DIFF-03**: Bottleneck analysis — identifikasi deliverable paling lama pending, approval bottleneck visibility di dashboard

## Future Requirements (v9+)

- Competency gap heatmap (worker x kompetensi matrix)
- Scheduling integration / calendar untuk coaching sessions
- AI-generated coaching session summaries
- SLA/escalation otomatis untuk approval yang terlalu lama
- Predicted completion date berdasarkan historical pace

## Out of Scope

| Feature | Reason |
|---------|--------|
| SignalR hub baru untuk Proton | Coaching approval tidak time-critical seperti exam monitoring; in-app notification cukup |
| Workflow engine (Elsa, dll) | Approval chain sudah ter-encode via status fields; engine = over-engineering |
| Silabus hierarchy > 3 level | Overhead kognitif; 3 level (Kompetensi-Sub-Deliverable) sudah cukup |
| Notifikasi unifikasi | Dua sistem (ProtonNotification + UserNotification) coexist dengan purpose berbeda; konsolidasi terlalu invasif |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| RSCH-01 | Phase 233 | Complete |
| RSCH-02 | Phase 233 | Complete |
| RSCH-03 | Phase 233 | Complete |
| SETUP-01 | Phase 234 | Complete |
| SETUP-02 | Phase 234 | Complete |
| SETUP-03 | Phase 234 | Complete |
| SETUP-04 | Phase 238 | Complete |
| SETUP-05 | Phase 234 | Complete |
| EXEC-01 | Phase 235 | Complete |
| EXEC-02 | Phase 235 | Complete |
| EXEC-03 | Phase 235 | Complete |
| EXEC-04 | Phase 235 | Complete |
| EXEC-05 | Phase 235 | Complete |
| COMP-01 | Phase 236 | Complete |
| COMP-02 | Phase 238 | Complete |
| COMP-03 | Phase 236 | Complete |
| COMP-04 | Phase 236 | Complete |
| MON-01 | Phase 237 | Complete |
| MON-02 | Phase 237 | Complete |
| MON-03 | Phase 237 | Complete |
| MON-04 | Phase 238 | Complete |
| DIFF-01 | Phase 238 | Complete |
| DIFF-02 | Phase 237 | Complete |
| DIFF-03 | Phase 238 | Complete |

**Coverage:**
- v8.2 requirements: 24 total
- Satisfied: 24
- Pending: 0
- Unmapped: 0

---
*Requirements defined: 2026-03-22*
*Last updated: 2026-03-22 after roadmap definition — all 24 requirements mapped*
