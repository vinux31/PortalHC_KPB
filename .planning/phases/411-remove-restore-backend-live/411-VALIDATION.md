---
phase: 411
slug: remove-restore-backend-live
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 411 — Validation Strategy

> Draft; difinalisasi saat `/gsd-validate-phase 411`. Sumber: 411-RESEARCH.md §Validation Architecture.

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantRemove"` |
| **Full suite command** | `dotnet test` |
| **Test file** | `HcPortal.Tests/FlexibleParticipantRemoveLiveTests.cs` (NEW) |
| **Infra note** | Hard-delete butuh service-provider stub (mini-DI) untuk `RecordCascadeDeleteService` (HttpContext.RequestServices) — gap khusus 411 |

## Per-Task Verification Map (draft)

| Req | Secure Behavior | Test Type | Status |
|-----|-----------------|-----------|--------|
| PRMV-01 | Not-started + 0-response → hard-delete (baris hilang dari DB; UPA ikut terhapus cascade) | integration SQLEXPRESS + SP-stub | ⬜ |
| PRMV-01 | In-progress/Completed/has-data → soft-remove (RemovedAt set; Score/IsPassed/NomorSertifikat/response UTUH; Status tak berubah) | integration SQLEXPRESS | ⬜ |
| PRMV-01 | Idempoten — RemovedAt!=null → no-op sukses | integration | ⬜ |
| PRMV-04 | Restore — RemovedAt=null + clear RemovedBy/Reason; soft-removed-only | integration | ⬜ |
| PRMV-05 | Pre/Post pair via LinkedSessionId — salah satu ada-data → soft keduanya; keduanya bersih → hard keduanya | integration | ⬜ |
| PLIV-03 | Audit Remove/Restore (siapa/kapan/alasan) + RBAC Admin,HC + antiforgery | integration (InMemory real-controller) | ⬜ |
| D-02 | reason kosong saat soft-remove → 400 | integration | ⬜ |
| spec §F | Proton sesi → tolak | integration | ⬜ |
| D-04 | DeleteAssessmentPeserta delegasi → outcome sama (redirect) | integration | ⬜ |

## De-Tautology
- Drive action ASLI (RemoveParticipantLive/RestoreParticipantLive/DeleteAssessmentPeserta) + assert kolom/baris DB nyata. JANGAN replica predikat hybrid-by-state.

## Wave 0 Requirements
- [ ] `FlexibleParticipantRemoveLiveTests.cs` (NEW) + service-provider stub mini-DI untuk hard-delete path.

*Finalisasi map + nyquist_compliant saat validate-phase.*

## Manual-Only
| Behavior | Why Manual |
|----------|------------|
| Force-kick SignalR + modal keras + panel | UI/SignalR = Phase 412; UAT Playwright 413 |
