---
phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
plan: 02
status: complete
requirements: [PART-04]
migration: false
date: 2026-06-17
---

# Plan 391-02 Summary — Flexible Participant Add Regression Test

**Status:** ✅ Complete · 2/2 tasks · 0 migration · branch `main` · commit `31e71a3e`

## What was built

`HcPortal.Tests/FlexibleParticipantAddTests.cs` (new) — xUnit **integration real-SQL** test, disposable `HcPortalDB_Test_{guid}` (pola `PostLisensorPolishTests`), `[Trait("Category","Integration")]`. 4 facts lock PART-04:

| Fact | Behavior locked |
|------|-----------------|
| (a) `AddParticipant_WithInProgressSibling_CreatesNewSession` | Tambah peserta saat ada sesi InProgress → sibling count +1, sesi user baru ada |
| (b) `AddParticipant_NewSession_HasReadyStatus_NotInProgress` | Sesi baru: schedule lampau→`Open`, depan→`Upcoming`; keduanya ≠ `InProgress` |
| (c) `AddParticipant_InProgressSibling_StatusScheduleDurationUnchanged` | Sesi InProgress UNCHANGED (Status/Schedule/Duration); sesi belum-mulai JUSTRU berubah (kontrol-positif → filter selektif, bukan no-op) |
| (d) `AddParticipant_SomeCompleted_NotBlocked_WhileWindowOpen` | `WindowAllowsAddition==true` + sesi baru tercipta saat sebagian Completed + window terbuka |

## Replica match Plan 01

Helper test mereplikasi keputusan controller **byte-identik** (di-verifikasi vs `391-01-SUMMARY`):
- `DeriveReadyStatus(schedule, window)` — `DateTime.UtcNow.AddHours(7)` (WIB), `schedule<=nowWib ? Open : Upcoming`.
- `IsRunning(s)` — `StartedAt != null && CompletedAt == null`.
- `ApplySharedFieldUpdate(...)` — edit-loop yang skip `IsRunning`.
- `WindowAllowsAddition(window)` — fallback `null = boleh tambah` (longgar, sesuai keputusan Plan 01) → di-lock di fact (d).

## Verification

- `dotnet build HcPortal.Tests/HcPortal.Tests.csproj` → **0 error**.
- `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAdd"` → **4/4 green**.
- `dotnet test` (full, incl. Integration; SQLEXPRESS up) → **486/486 green**, 0 fail (no regression).
- Grep acceptance semua PASS (fixture 1×, HcPortalDB_Test_ 2×, Trait 1×, WIB 6×, EnsureDeletedAsync 2×, [Fact] 4×, verify-ctx 4×).
- **Migration = FALSE** — 1 file test baru; `HcPortalDB_Dev` TIDAK tersentuh (disposable DB EnsureDeletedAsync).

## key-files
- created: `HcPortal.Tests/FlexibleParticipantAddTests.cs`
- modified: (none)

## IT Handoff Note
v32.0 Phase 391 = **migration=FALSE** (pure controller logic + test, 0 schema change, 0 view change). Branch `main`. Notify IT dengan commit hash saat phase di-handoff.

## Self-Check: PASSED
