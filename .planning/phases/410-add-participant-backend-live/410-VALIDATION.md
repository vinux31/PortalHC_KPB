---
phase: 410
slug: add-participant-backend-live
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 410 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail map difinalisasi saat `/gsd-validate-phase 410`. Sumber: 410-RESEARCH.md §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | existing test project (PortalHC_KPB.Tests) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAdd"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30–90 s (quick), full suite beberapa menit |

---

## Sampling Rate

- **After every task commit:** quick run command.
- **After every plan wave:** full suite.
- **Before `/gsd-verify-work`:** full suite green.

---

## Per-Task Verification Map

| Req | Secure Behavior | Test Type | Pola Analog | Status |
|-----|-----------------|-----------|-------------|--------|
| PART-06 | Sesi baru ready-status (Open/Upcoming, NEVER InProgress) + UPA dibuat | integration (SQLEXPRESS disposable) | `FlexibleParticipantAddFixture` | ⬜ pending |
| PART-06 | Idempotent — user dengan sesi aktif di-skip + report count/nama | integration (InMemory real-controller) | `ParticipantRemovalExcludeTests` | ⬜ pending |
| PART-06 | Window tutup (`ExamWindowCloseDate` lewat) → 400 + pesan | integration | window-guard | ⬜ pending |
| PART-07 | Batch Pre/Post → pasangan Pre+Post tercipta | integration | Pre/Post create `:1942` | ⬜ pending |
| (spec §F) | Sesi Proton → endpoint tolak | integration | Proton reject | ⬜ pending |
| (D-01/D-02) | Eligible = semua pekerja IsActive minus yang punya sesi APAPUN di batch | integration (InMemory) | eligible-source `:655` | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `FlexibleParticipantAddTests.cs` — NEW (anti-tautology 999.12: read-path lewat InMemory real-controller, write-path lewat SQLEXPRESS disposable; JANGAN tiru REPLICA predikat).

*Finalisasi map + nyquist_compliant saat validate-phase.*

---

## Manual-Only Verifications

| Behavior | Why Manual | Test Instructions |
|----------|------------|-------------------|
| (410 backend-only) | Tidak ada UI di 410 — UAT Playwright live di 412/413 | N/A untuk 410 |
