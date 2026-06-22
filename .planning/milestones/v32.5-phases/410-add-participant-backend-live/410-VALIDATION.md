---
phase: 410
slug: add-participant-backend-live
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
finalized: 2026-06-21
---

# Phase 410 — Validation Strategy

> Per-phase validation contract. Difinalisasi oleh `/gsd-validate-phase 410`.
> Sumber sinyal: 410-RESEARCH.md §Validation Architecture + 410-CONTEXT.md decisions D-01..D-04.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~FlexibleParticipantAddLive"` |
| **Full suite command** | `dotnet test` |
| **Test file** | `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` |
| **Estimated runtime** | ~15 s (quick), full suite beberapa menit |

---

## Sampling Rate

- **After every task commit:** quick run command.
- **After every plan wave:** full suite.
- **Before `/gsd-verify-work`:** full suite green.

---

## Per-Task Verification Map

### Bagian A — Read-path (InMemory real-controller, `FlexibleParticipantAddLiveEligibleTests`)

| Req/Signal | Behavior | Test Name | Type | Status |
|------------|----------|-----------|------|--------|
| D-01 | `GetEligibleParticipantsToAdd` ASLI exclude user dengan sesi APAPUN di batch — aktif (`RemovedAt==null`) DAN removed (`RemovedAt!=null`); user removed hanya balik via Restore 411 | `Eligible_ExcludesUsersWithAnySession_IncludingRemoved` | unit (InMemory real-controller) | ✅ green |
| D-02 | Eligible TANPA filter unit/section — user beda unit/section tetap muncul; user `IsActive=false` excluded | `Eligible_IgnoresUnitSection_ExcludesInactive` | unit (InMemory real-controller) | ✅ green |
| PART-06 | `GetEligibleParticipantsToAdd` dengan `sessionId` tidak ada → `NotFoundObjectResult` (404) | `Eligible_RepNotFound_Returns404` | unit (InMemory real-controller) | ✅ green |
| PART-06 (idempotency read-side) | User yang sudah punya sesi aktif di batch tidak pernah muncul di daftar eligible — konsisten D-01 | `Eligible_AlreadyInBatch_NeverAppears` | unit (InMemory real-controller) | ✅ green |

### Bagian B — Write-path (SQLEXPRESS disposable, `FlexibleParticipantAddLiveWriteTests`)

| Req/Signal | Behavior | Test Name | Type | Status |
|------------|----------|-----------|------|--------|
| PART-06 (ready-status) | `AddParticipantsLive` ASLI membuat sesi baru Status==Open (schedule lampau, `DeriveReadyStatus`); `StartedAt`/`CompletedAt`/`RemovedAt` == null; NEVER InProgress | `AddParticipantsLive_NewSession_HasReadyStatus_RemovalNull` | integration (SQLEXPRESS) | ✅ green |
| PART-06 (eager UPA, A1) | Batch dengan `AssessmentPackage` + `Questions` + `Options` → `UserPackageAssignment` dibuat EAGER in-tx; `ShuffledQuestionIds != "[]"` + count benar | `AddParticipantsLive_WithPackages_CreatesEagerUserPackageAssignment` | integration (SQLEXPRESS) | ✅ green |
| PART-06 (window-reject) | `ExamWindowCloseDate` sudah lewat (kemarin WIB) → `BadRequestObjectResult` 400 + pesan "Window ujian sudah tutup, tidak bisa tambah peserta." + 0 sesi baru tercipta | `AddParticipantsLive_WindowClosed_Returns400_NoWrite` | integration (SQLEXPRESS) | ✅ green |
| spec §F (Proton-reject) | `Category=="Assessment Proton"` → `BadRequestObjectResult` 400 + kata "Assessment Proton" dalam pesan + 0 sesi baru | `AddParticipantsLive_Proton_Returns400_NoWrite` | integration (SQLEXPRESS) | ✅ green |
| PART-07 (Pre/Post pair) | Batch PreTest+PostTest → `AddParticipantsLive` ASLI membuat PASANGAN 2 sesi; `LinkedSessionId` cross-set dua arah; `LinkedGroupId` diwarisi; config Post (Schedule/window/duration/cert) diwarisi dari sesi Post (WR-01 regression guard), bukan dari Pre | `AddParticipantsLive_PrePost_CreatesPair_WithCrossLink` | integration (SQLEXPRESS) | ✅ green |
| PART-06 (idempotency write) | `userIds` campuran [existing, fresh] → existing masuk `skipped[]` (skippedCount=1), fresh dibuat 1× (addedCount=1); existing TIDAK dobel-create | `AddParticipantsLive_ExistingUser_Skipped_NoDuplicate` | integration (SQLEXPRESS) | ✅ green |

---

## Signal Coverage Matrix

Semua sinyal dari PART-06, PART-07, dan keputusan D-01..D-04 terpetakan:

| Signal | Test | Covered |
|--------|------|---------|
| PART-06: ready-status Open/Upcoming, NEVER InProgress | T5 `_HasReadyStatus_RemovalNull` | ✅ |
| PART-06: `UserPackageAssignment` eager (A1 LOCKED) | T6 `_WithPackages_CreatesEagerUPA` | ✅ |
| PART-06: window-reject 400 + 0-write | T7 `_WindowClosed_Returns400_NoWrite` | ✅ |
| PART-06: idempotent write, skipped[]+added[] D-03 | T10 `_ExistingUser_Skipped_NoDuplicate` | ✅ |
| PART-07: Pre/Post pair + LinkedSessionId cross-set | T9 `_PrePost_CreatesPair_WithCrossLink` | ✅ |
| spec §F: Proton-reject 400 + 0-write | T8 `_Proton_Returns400_NoWrite` | ✅ |
| D-01: eligible exclude sesi APAPUN (incl. removed) | T1 `Eligible_ExcludesUsersWithAnySession_IncludingRemoved` | ✅ |
| D-02: eligible no unit/section filter; exclude inactive | T2 `Eligible_IgnoresUnitSection_ExcludesInactive` | ✅ |
| D-01 (rep absen → 404) | T3 `Eligible_RepNotFound_Returns404` | ✅ |
| D-01 (idempotency read-side) | T4 `Eligible_AlreadyInBatch_NeverAppears` | ✅ |

**Total: 10/10 sinyal covered. 0 gap.**

---

## De-Tautology Confirmation

Sesuai lesson 999.12 (anti-tautology):

- **Tidak ada replica predikat** `WindowAllowsAddition` atau `DeriveReadyStatus` tiruan dalam test.
- **Bagian A** memanggil `GetEligibleParticipantsToAdd` **ASLI** via controller instance nyata di atas InMemory DB — logika LINQ produksi yang dieksekusi.
- **Bagian B** memanggil `AddParticipantsLive` **ASLI** di atas SQLEXPRESS disposable — sesi/UPA yang dibuat adalah output produksi; test mengassert **kolom DB nyata** (Status, LinkedSessionId, ExamWindowCloseDate, DurationMinutes, dll).
- T9 (Pre/Post) mencakup WR-01 regression guard: assert bahwa `newPost.ExamWindowCloseDate == postWindow` (bukan `preWindow`) — membuktikan bahwa implementasi meresolve config Post dari sesi Post, bukan dari single rep.

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` — 10 test (4 InMemory read-path + 6 SQLEXPRESS write-path), semua green.

---

## Test Run Result (Finalisasi)

```
dotnet test --filter "FullyQualifiedName~FlexibleParticipantAddLive" --no-build
Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 13 s
```

Individual:
- `Eligible_ExcludesUsersWithAnySession_IncludingRemoved` ✅
- `Eligible_IgnoresUnitSection_ExcludesInactive` ✅
- `Eligible_RepNotFound_Returns404` ✅
- `Eligible_AlreadyInBatch_NeverAppears` ✅
- `AddParticipantsLive_NewSession_HasReadyStatus_RemovalNull` ✅
- `AddParticipantsLive_WithPackages_CreatesEagerUserPackageAssignment` ✅
- `AddParticipantsLive_WindowClosed_Returns400_NoWrite` ✅
- `AddParticipantsLive_Proton_Returns400_NoWrite` ✅
- `AddParticipantsLive_PrePost_CreatesPair_WithCrossLink` ✅
- `AddParticipantsLive_ExistingUser_Skipped_NoDuplicate` ✅

---

## Manual-Only Verifications

| Behavior | Why Manual | Test Instructions |
|----------|------------|-------------------|
| UI panel tambah peserta + picker modal | Tidak ada UI di 410 — UI = Phase 412 | UAT Playwright live di 413 |
| SignalR broadcast `participantAdded` | DEFER ke Phase 412 (D-04) | Verifikasi di 413 end-to-end |
| Notif ASMT_ASSIGNED sampai ke inbox pekerja | NoopNotificationService di test; real notif via IT deploy | UAT browser di 413 |
