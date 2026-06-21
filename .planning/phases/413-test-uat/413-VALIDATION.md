---
phase: 413
slug: test-uat
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 413 — Validation Strategy

> Draft; difinalisasi saat `/gsd-validate-phase 413`. Sumber: 413-RESEARCH.md §Validation Architecture + 412-VALIDATION.md handoff.
> 413 = fase Test+UAT — test ITU SENDIRI deliverable-nya (xUnit lifecycle + Playwright e2e 7 sinyal + full regression).

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright 1.58.2 |
| **xUnit quick** | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantLifecycle"` |
| **Full suite** | `dotnet test` (baseline 602) |
| **e2e** | `npx playwright test flexible-participant-412 --workers=1` (app @5277 AD-off) |
| **Seed** | SEED_WORKFLOW: BACKUP → seed InProgress (sqlcmd UPDATE sesi punya-paket) → e2e → RESTORE → journal cleaned |

## Per-Task Verification Map (draft — 7 e2e signals + lifecycle + regression)

| Req | Signal | Test Type | Status |
|-----|--------|-----------|--------|
| PART-05 | Add picker → baris muncul live tanpa reload (2-context) | Playwright multi-context | ⬜ |
| PRMV-02 | Modal keras InProgress | Playwright | ⬜ |
| PRMV-02 | Force-kick worker 2-context (examRemoved modal+redirect) | Playwright multi-context (Flow O) | ⬜ |
| PLIV-01 | Baris pindah ke panel "Peserta Dikeluarkan" live | Playwright | ⬜ |
| D-04 | Restore 1-klik → baris balik live | Playwright | ⬜ |
| Pitfall-2 | updateSummaryFromDOM count aktif turun (exclude #tbodyRemoved) | Playwright DOM | ⬜ |
| PLIV-02 | Multi-observer broadcast (admin A+B lihat perubahan) | Playwright multi-context | ⬜ |
| (lifecycle) | add→StartExam→soft-remove→guard-blocked→restore→StartExam-ok | xUnit integration (SQLEXPRESS, de-taut) | ⬜ |
| (regression) | Full suite hijau + guard 391/398.1 no-regress | `dotnet test` | ⬜ |

## De-Tautology
- xUnit lifecycle: drive controller/StartExam ASLI + assert DB nyata. Playwright: real browser, real SignalR, assert DOM nyata (no mock).

## Wave 0 Requirements
- [ ] `tests/e2e/flexible-participant-412.spec.ts` (NEW, multi-context, per-spec seed/restore).
- [ ] `FlexibleParticipantLifecycleTests.cs` (NEW xUnit, opsional per planner).

## Manual-Only
| Behavior | Why |
|----------|-----|
| (Tidak ada) | 413 = otomatisasi penuh; e2e Playwright = UAT terotomasi (bukan manual). |

*Finalisasi saat validate-phase.*
