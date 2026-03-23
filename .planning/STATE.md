---
gsd_state_version: 1.0
milestone: v8.4
milestone_name: Alarm Sertifikat Expired
status: Milestone complete
stopped_at: Phase 240 UI-SPEC approved
last_updated: "2026-03-23T13:24:26.834Z"
progress:
  total_phases: 1
  completed_phases: 1
  total_plans: 2
  completed_plans: 2
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-23)

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 240 — alarm-sertifikat-expired

## Current Position

Phase: 240
Plan: Not started

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: —
- Total execution time: —

## Accumulated Context

### Decisions

- v8.3: Single-phase milestone — semua 8 requirements (FILT-01..06, EXP-01..02) masuk Phase 239
- v8.4: Semua 7 requirements (ALRT-01..04, NOTF-01..03) digabung ke 1 fase (Phase 240) karena banner dan bell notification keduanya bergantung pada query expired certs yang sama
- v8.4: CERT_EXPIRING_SOON notification sengaja out-of-scope — akan expired cukup tampil di banner saja
- v8.4: Notifikasi di-generate on page load, tidak perlu background job/scheduler
- v8.4: Infrastruktur tersedia — BuildRenewalRowsAsync() dan NotificationService.SendAsync() sudah ada

### Pending Todos

None.

### Blockers/Concerns

None.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260323-ugm | Terjemahkan label card menu CMP Index ke Bahasa Indonesia | 2026-03-23 | 748a0f21 | [260323-ugm](./quick/260323-ugm-terjemahkan-label-card-menu-cmp-index-ke/) |

## Session Continuity

Last activity: 2026-03-23 - Completed quick task 260323-ugm: Terjemahkan label CMP Index ke Bahasa Indonesia
Stopped at: Phase 240 UI-SPEC approved
Resume file: .planning/phases/240-alarm-sertifikat-expired/240-UI-SPEC.md
