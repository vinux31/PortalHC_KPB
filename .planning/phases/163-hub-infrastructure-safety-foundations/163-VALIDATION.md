---
phase: 163
slug: hub-infrastructure-safety-foundations
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-03-13
---

# Phase 163 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser + DevTools verification (no automated test framework in project) |
| **Config file** | None — manual verification |
| **Quick run command** | Manual: DevTools Network tab + console checks |
| **Full suite command** | Manual UAT per 5 success criteria |
| **Estimated runtime** | ~5 minutes manual |

---

## Sampling Rate

- **After every task commit:** Manual smoke check per task's success criteria
- **After every plan wave:** Full success criteria checklist (5 items)
- **Before `/gsd:verify-work`:** All 5 success criteria TRUE
- **Max feedback latency:** ~60 seconds (manual browser check)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 163-XX-XX | 01 | 1 | INFRA-01 | smoke | Manual: DevTools Network tab, check `/hubs/assessment/negotiate` returns 200 JSON not 302 | N/A | ⬜ pending |
| 163-XX-XX | 01 | 1 | INFRA-02 | smoke | Manual: DevTools Network tab, check signalr.min.js loads without 404 | N/A | ⬜ pending |
| 163-XX-XX | 01 | 1 | INFRA-03 | smoke | Manual: incognito window, check negotiate returns 401 not 302 | N/A | ⬜ pending |
| 163-XX-XX | 01 | 1 | INFRA-04 | smoke | Manual: check startup log for "WAL mode active" or SQLite CLI `PRAGMA journal_mode;` | N/A | ⬜ pending |
| 163-XX-XX | 01 | 1 | INFRA-05 | smoke | Manual: DevTools offline/online toggle, verify auto-rejoin | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements — manual verification sufficient for all INFRA requirements. No test framework install needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SignalR negotiate returns 200 JSON | INFRA-01 | Browser WebSocket handshake | Open DevTools Network, load exam page, filter for `negotiate`, verify 200 response |
| signalr.min.js loads | INFRA-02 | Static file serving | Open DevTools Network, check signalr.min.js returns 200 |
| Negotiate returns 401 when unauthenticated | INFRA-03 | Auth behavior | Open incognito, navigate to `/hubs/assessment/negotiate`, verify 401 |
| WAL mode active | INFRA-04 | Database configuration | Check application startup log or run `PRAGMA journal_mode;` via SQLite CLI |
| Auto-rejoin on reconnect | INFRA-05 | Network simulation | DevTools > Network > Offline toggle on/off, verify console shows reconnect + group rejoin |

---

## Validation Sign-Off

- [x] All tasks have manual verify instructions
- [x] Sampling continuity: manual check per task commit
- [x] Wave 0 covers all MISSING references (none needed)
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
