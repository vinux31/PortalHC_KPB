---
gsd_state_version: 1.0
milestone: v7.10
milestone_name: RenewalCertificate Bug Fixes & Enhancement
status: unknown
stopped_at: Completed 210-02-PLAN.md
last_updated: "2026-03-21T04:59:25.285Z"
progress:
  total_phases: 3
  completed_phases: 2
  total_plans: 3
  completed_plans: 3
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-20)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 210 — critical-renewal-chain-fixes

## Current Position

Phase: 211
Plan: Not started

## Accumulated Context

### Decisions

- [v7.9]: Grouped view shipped — RenewalCertificate sekarang grouped by judul sertifikat dengan Base64 group-key
- [v7.9]: Lock checkbox per group-key, modal konfirmasi sebelum redirect ke CreateAssessment
- [v7.10]: 14 requirements dibagi 3 phase: FIX-01/02/03 critical chain (210), FIX-05-10 data/display (211), ENH-01/02/03/04 + FIX-04 enhancement (212)
- [v7.10]: FIX-04 (AddTraining renewal FK) dikelompokkan bersama ENH-04 (AddTraining renewal mode) di Phase 212
- [Phase 210-01]: BuildRenewalRowsAsync digunakan sebagai single source of truth untuk badge count Admin/Index
- [Phase 210-01]: FIX-03 tidak perlu ubah kode: Set 2 dan Set 4 sudah benar karena TrainingRecord tidak memiliki field IsPassed
- [Phase 211]: DeriveCertificateStatus pisahkan cek Permanent dan ValidUntil=null agar non-Permanent dengan null expiry → Expired
- [Phase 210]: Per-user FK map dikirim via hidden input JSON untuk menghindari perubahan model binding

### Pending Todos

None.

### Blockers/Concerns

- Phase 210: BulkRenew bug berdampak pada semua user kecuali user[0] — perlu audit loop assignment di AdminController
- Phase 212: Popup pilihan renewal tipe (Assessment vs Training) membutuhkan JS modal baru di RenewalCertificate view

## Session Continuity

Last session: 2026-03-21T04:56:10.359Z
Stopped at: Completed 210-02-PLAN.md
Resume file: None
