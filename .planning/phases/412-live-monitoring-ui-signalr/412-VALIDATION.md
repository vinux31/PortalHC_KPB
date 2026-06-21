---
phase: 412
slug: live-monitoring-ui-signalr
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 412 — Validation Strategy

> Draft; difinalisasi saat `/gsd-validate-phase 412`. Sumber: 412-RESEARCH.md §Validation Architecture.
> **Catatan:** mayoritas 412 = UI live + SignalR DOM → **Playwright-verified** (e2e lengkap = Phase 413). 412 = unit/integration untuk bagian assertable + smoke.

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (e2e) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~MonitoringRemovedPanel"` |
| **Full suite command** | `dotnet test` |
| **e2e** | Playwright @localhost:5277 (AD-off) — smoke 412; lengkap 413 |

## Per-Task Verification Map (draft)

| Req | Signal | Test Type | Status |
|-----|--------|-----------|--------|
| PLIV-01 | Action AssessmentMonitoringDetail sediakan removedSessions (RemovedAt!=null + RemovedBy→FullName) untuk panel | integration/unit (assertable) | ⬜ |
| PLIV-02 | Broadcast participantAdded/participantRemoved HANYA post-CommitAsync ke monitor-{batchKey} | integration (assert SendAsync dipanggil setelah commit) / Playwright live | ⬜ |
| PART-05 | Add picker → baris muncul live tanpa reload | Playwright (413 lengkap) | ⬜ |
| PRMV-02 | Modal keras (InProgress+Completed-cert) + examRemoved force-kick worker | Playwright (413) | ⬜ |
| PLIV-01 | Panel "Peserta Dikeluarkan" + Restore 1-klik | Playwright (413) | ⬜ |
| (pitfall) | updateSummaryFromDOM exclude #tbodyRemoved (count aktif benar) | Playwright/unit-DOM | ⬜ |
| (T-409-10) | XSS RemovalReason via textContent/Html.Encode | review + Playwright | ⬜ |

## De-Tautology
- Bagian assertable (removedSessions query, broadcast-post-commit ordering) drive kode ASLI + assert nyata. UI live → Playwright real browser.

## Wave 0 Requirements
- [ ] Test removedSessions query (assertable) + Playwright smoke (opsional 412; lengkap 413).

## Manual-Only / Playwright
| Behavior | Why |
|----------|-----|
| Add live row, remove modal tiers, force-kick worker, panel+Restore live | Live DOM/SignalR → Playwright real browser (UAT 412 + e2e 413) |

*Finalisasi saat validate-phase.*
