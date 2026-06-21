---
phase: 411
slug: remove-restore-backend-live
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
finalized: 2026-06-21
---

# Phase 411 — Validation Strategy

> Difinalisasi saat `/gsd-validate-phase 411`. Sumber: 411-RESEARCH.md §Validation Architecture.
> Semua 16 [Fact] GREEN (`dotnet test --filter "FullyQualifiedName~FlexibleParticipantRemove"` — 16/16 PASS).
> Gap: 0. nyquist_compliant: true. wave_0_complete: true.

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~FlexibleParticipantRemove"` |
| **Full suite command** | `dotnet test` |
| **Test file** | `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (816 baris, 16 [Fact]) |
| **Test classes** | `FlexibleParticipantRemoveReadTests` (5 InMemory) + `FlexibleParticipantRemoveWriteTests` (11 SQLEXPRESS) |
| **Infra note** | Mini-DI service-provider stub (`BuildCascadeServiceProvider` + `MakeLiveControllerWithCascade`) untuk jalur hard-delete via `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()` |
| **Last run** | 2026-06-21 — 16/16 PASS, 0 failed, 0 skipped |

## Per-Task Verification Map

| Req | Behavior (signal) | Test Name | Type | Status |
|-----|-------------------|-----------|------|--------|
| PRMV-01 | Not-started + 0-response → hard-delete (baris hilang dari AssessmentSessions) | `RemoveNotStarted_HardDeletes_RowGone` (C1) | Integration SQLEXPRESS + mini-DI | ✅ |
| PRMV-01 | UPA eager (410) TIDAK dihitung "data" (D-01) — bersih+UPA → tetap hard-delete; UPA ikut hilang (cascade :221-222) | `RemoveWithEagerUPA_StillHardDeletes_UpaGone` (C2) | Integration SQLEXPRESS + mini-DI | ✅ |
| PRMV-01 | In-progress (StartedAt!=null) → soft-remove (RemovedAt set NYATA; Score/IsPassed/Status UNCHANGED; response utuh) | `RemoveInProgress_SoftRemoves_PreservesData` (B1) | Integration SQLEXPRESS | ✅ |
| PRMV-01 | Completed + NomorSertifikat + ManualSertifikatUrl → soft-remove (cert/score/status UNCHANGED) | `RemoveCertified_SoftRemoves_PreservesCert` (B2) | Integration SQLEXPRESS | ✅ |
| PRMV-01 | Idempotent InMemory: RemovedAt!=null → JsonResult mode="noop" (sebelum actor-resolve) | `RemoveParticipantLive_AlreadyRemoved_NoOp` (A2) | Unit InMemory | ✅ |
| PRMV-01 | Idempotent write: remove ×2 → panggilan ke-2 mode="noop"; RemovedAt+reason pertama tak tertimpa | `RemoveInProgress_Idempotent_NoOp` (B4) | Integration SQLEXPRESS | ✅ |
| PRMV-01 | sessionId tak ada → 404 | `RemoveParticipantLive_NotFound_404` (A3) | Unit InMemory | ✅ |
| PRMV-04 | Restore soft-removed: RemovedAt/RemovedBy/RemovalReason di-clear NYATA; restored=true | `Restore_SoftRemoved_ClearsColumns` (B5) | Integration SQLEXPRESS | ✅ |
| PRMV-04 | Restore guard: sesi aktif (RemovedAt==null) → 400 "Sesi ini tidak dalam keadaan dihapus." | `RestoreParticipantLive_NotRemoved_Rejected400` (A4) | Unit InMemory | ✅ |
| PRMV-04 | Restore sessionId tak ada → 404 | `RestoreParticipantLive_NotFound_404` (A5) | Unit InMemory | ✅ |
| PRMV-05 | Pre/Post salah satu berdata → soft keduanya via LinkedSessionId; JSON linkedSessionId==partnerId; peserta LAIN di batch TIDAK ter-remove (Pitfall 1) | `RemovePrePost_OneHasData_SoftBoth` (B6) | Integration SQLEXPRESS | ✅ |
| PRMV-05 | Pre/Post keduanya bersih → hard keduanya; kedua baris HILANG; peserta LAIN MASIH ada | `RemovePrePost_BothClean_HardBoth` (C3) | Integration SQLEXPRESS + mini-DI | ✅ |
| PRMV-04+05 | Restore Pre/Post pair simetris: RestoreParticipantLive(preId) → KEDUA partner (Pre+Post) RemovedAt/RemovedBy/RemovalReason==null NYATA | `RestorePrePost_Pair_ClearsBothPartners` (B7/IN-03) | Integration SQLEXPRESS | ✅ |
| PLIV-03 | Audit row tertulis: AuditLogs.AnyAsync(ActionType="RemoveParticipantLive" && TargetId==sessionId)==true | `Remove_WritesAuditRow` (B8) | Integration SQLEXPRESS | ✅ |
| PLIV-03 / D-02 | Soft-remove tanpa reason → 400 "Alasan penghapusan wajib diisi." + 0-write (RemovedAt tetap null) | `RemoveSoft_NoReason_Rejected400` (B3) | Integration SQLEXPRESS | ✅ |
| spec §F | Category=="Assessment Proton" → 400 + 0-write | `RemoveParticipantLive_Proton_Rejected400` (A1) | Unit InMemory | ✅ |

**Total: 16 sinyal → 16 test → 16 GREEN. Gap: 0.**

## De-Tautology

- `grep "SessionHasDataAsync" FlexibleParticipantRemoveTests.cs` → 4 hits, **semua di komentar** (0 di kode fungsional).
- `grep "ExecuteAsync" FlexibleParticipantRemoveTests.cs` → 3 hits, **semua di komentar** (0 panggil cascade langsung).
- Hard-delete dibuktikan via `AnyAsync == false` atas DB SQLEXPRESS nyata (C1, C2, C3).
- Sanity pre-assert C2: UPA memang ada SEBELUM remove → membuktikan assert `AnyAsync==false` bukan tautologi.
- Setiap test drive action `RemoveParticipantLive`/`RestoreParticipantLive` ASLI + assert kolom DB nyata (reload ctx baru).

## Wave 0 Requirements

- [x] `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (816 baris, 16 [Fact]) — ada dan hijau.
- [x] Mini-DI service-provider stub (`BuildCascadeServiceProvider`/`MakeLiveControllerWithCascade`) — ada (baris :286-306); hard-delete jalur `HttpContext.RequestServices` tertutup.
- [x] Seed helpers: `SeedUserAsync`, `SeedRepSessionAsync`, `SeedResponseAsync`, `SeedUpaAsync` — ada (baris :309-364).
- [x] Framework install: xUnit + fixture existing (reuse `FlexibleParticipantAddFixture` dari 410).

*Semua Wave 0 requirements terpenuhi.*

## Manual-Only

| Behavior | Why Manual |
|----------|------------|
| Force-kick SignalR + modal keras | UI/SignalR = Phase 412 |
| Panel "Peserta Dikeluarkan" (collapsible) | UI render = Phase 412 |
| RBAC [Authorize] runtime + antiforgery 403/400 via browser | Atribut diverifikasi kode review (VERIFICATION.md Truth-4); Playwright e2e = Phase 413 |
| Playwright e2e live Monitoring Detail | Phase 413 |
| DeleteAssessmentPeserta redirect variant + form un-hidden | Verified kode (VERIFICATION.md Truth-8); browser UAT = Phase 413 |

## Validation Summary

```
Phase 411 — Remove + Restore Backend Live
Nyquist gap analysis: 2026-06-21

Requirement signals audited  : 16
Tests covering signals       : 16
Gaps identified              : 0
Tests generated (new)        : 0
Tests run                    : 16
Tests passing                : 16
Tests failing                : 0

nyquist_compliant : true
wave_0_complete   : true
```

Command konfirmasi:
```
dotnet test HcPortal.Tests --filter "FullyQualifiedName~FlexibleParticipantRemove" --no-build
# → Passed! - Failed: 0, Passed: 16, Skipped: 0, Total: 16
```
