---
phase: 410-add-participant-backend-live
reviewed: 2026-06-21T00:00:00Z
depth: deep
files_reviewed: 2
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - HcPortal.Tests/FlexibleParticipantAddLiveTests.cs
findings:
  critical: 0
  warning: 2
  info: 3
  total: 5
status: issues_found
---

# Phase 410: Code Review Report

**Reviewed:** 2026-06-21
**Depth:** deep (cross-file: CMPController.StartExam eager-UPA, EditAssessment Pre/Post + BULK ASSIGN, ShuffleEngine, SiblingSessionQuery, AssessmentSession model, spec §B1)
**Files Reviewed:** 2
**Status:** issues_found

## Summary

Phase 410 adds two AJAX endpoints (`AddParticipantsLive` POST, `GetEligibleParticipantsToAdd` GET) plus two private helpers (`BuildReadyParticipantSession`, `CreateEagerAssignmentsAsync`) to `AssessmentAdminController`. Build is green (0 errors).

The security posture is solid and the threat model in `410-01-PLAN.md` is faithfully implemented: both endpoints carry `[Authorize(Roles="Admin, HC")]`, the POST has `[ValidateAntiForgeryToken]` (T-410-01/02), the batch is resolved **server-side** from `sessionId` so no `batchKey`/`LinkedGroupId` is trusted from the client (T-410-03), the Proton guard fires before any write (T-410-04), the window guard is server-authoritative with WIB=UTC+7 (T-410-05), the signature binds only primitives so no mass-assignment is possible (T-410-06), and the cap-50 DoS guard is present (T-410-07). The transaction is atomic with rollback, eager-UPA runs inside the tx, and notifications fire only post-commit. The test file is genuinely de-tautological: read-path tests invoke the real `GetEligibleParticipantsToAdd` action and assert real DB-driven JSON; write-path tests drive the real `AddParticipantsLive` against a disposable SQLEXPRESS DB and assert real columns (Status, RemovedAt, UPA, LinkedSessionId).

**No Critical findings.** Two Warnings: (1) the Pre/Post branch inherits Schedule/window/cert flags from a *single* representative for both the new Pre **and** Post sessions, diverging from the established `EditAssessment` pattern that uses distinct Pre/Post reps — new PostTest sessions can get the wrong schedule/window/cert config; (2) test T9 (Pre/Post pair) does not seed a distinct PostTest sibling, so it cannot detect divergence #1. Three Info items.

## Warnings

### WR-01: Pre/Post branch inherits both sessions' config from a single `rep` — new PostTest gets wrong Schedule / ExamWindowCloseDate / GenerateCertificate

**File:** `Controllers/AssessmentAdminController.cs:2391-2401` (Pre/Post branch inside `AddParticipantsLive`) + `:2306-2331` (`BuildReadyParticipantSession`)

**Issue:** In a Pre/Post batch, both the new Pre and new Post sessions are built from the **same** `rep` via `BuildReadyParticipantSession(rep, uid, actorId)`:

```csharp
var newPre  = BuildReadyParticipantSession(rep, uid, actorId);
newPre.AssessmentType = "PreTest";  newPre.LinkedGroupId = linkedGroupId;
var newPost = BuildReadyParticipantSession(rep, uid, actorId);
newPost.AssessmentType = "PostTest"; newPost.LinkedGroupId = linkedGroupId;
```

`BuildReadyParticipantSession` copies `Schedule`, `DurationMinutes`, `ExamWindowCloseDate`, `GenerateCertificate`, and `Status = DeriveReadyStatus(rep.Schedule, rep.ExamWindowCloseDate)` from `rep`. But Pre and Post are designed to differ: the create path validates `PostSchedule > PreSchedule` and takes separate `PreExamWindowCloseDate`/`PostExamWindowCloseDate`/`PreDurationMinutes`/`PostDurationMinutes` (`AssessmentAdminController.cs:1065-1072, 1953-1990`). The original add-participant analog (`EditAssessment` `:1942-1998`) correctly uses **distinct** reps — `repPre = preGroup.First()` and `repPost = postGroup.First()` — and hardcodes `newPre.GenerateCertificate = false` while `newPost.GenerateCertificate = model.GenerateCertificate` (+ `newPost.ValidUntil`).

Concrete consequences when the caller passes the PreTest as `sessionId` (the more likely case since monitoring surfaces a representative):
- New PostTest inherits the **Pre's** Schedule/window/duration → its window may close before the post is meant to be taken, and `Status` is derived from the Pre's schedule, not the Post's.
- New PreTest inherits `rep.GenerateCertificate`; if `rep` is the PostTest (cert=true), the new PreTest is wrongly flagged to generate a certificate (original always forces PreTest cert=false).
- `ValidUntil` is never set on the new PostTest (helper omits it), unlike the original.

The new sessions become inconsistent with their existing batch siblings.

**Fix:** Resolve distinct Pre/Post representatives from the batch before building the pair, mirroring `EditAssessment :1944-1945`, and apply the cert/ValidUntil rules:

```csharp
// inside the Pre/Post branch, before the foreach (resolve once)
var repPre = await _context.AssessmentSessions.FirstOrDefaultAsync(a =>
    a.Title == rep.Title && a.Category == rep.Category &&
    a.Schedule.Date == rep.Schedule.Date && a.AssessmentType == "PreTest") ?? rep;
var repPost = await _context.AssessmentSessions.FirstOrDefaultAsync(a =>
    a.Title == rep.Title && a.Category == rep.Category &&
    a.Schedule.Date == rep.Schedule.Date && a.AssessmentType == "PostTest") ?? rep;
...
var newPre  = BuildReadyParticipantSession(repPre,  uid, actorId);
newPre.AssessmentType = "PreTest";  newPre.LinkedGroupId = linkedGroupId; newPre.GenerateCertificate = false;
var newPost = BuildReadyParticipantSession(repPost, uid, actorId);
newPost.AssessmentType = "PostTest"; newPost.LinkedGroupId = linkedGroupId; newPost.ValidUntil = repPost.ValidUntil;
```

Note: the window guard at Langkah 3 still checks only `rep.ExamWindowCloseDate`; consider also rejecting when the resolved `repPost` window is closed. If a single-rep design is intentionally accepted (since batch config is largely uniform), document it explicitly in the phase notes — it was not acknowledged in CONTEXT/PATTERNS/RESEARCH.

### WR-02: Pre/Post test (T9) cannot detect WR-01 — no distinct PostTest sibling seeded

**File:** `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs:498-544`

**Issue:** `AddParticipantsLive_PrePost_CreatesPair_WithCrossLink` seeds **only** a single `PreTest` rep (`SeedRepSessionAsync(..., assessmentType: "PreTest", linkedGroupId: ...)`) and no existing `PostTest` sibling with a distinct schedule/window. With one rep, the new Pre and new Post necessarily share config, so the test passes regardless of WR-01. The test verifies the pair count, cross-link, LinkedGroupId, and ready-status correctly, but it gives false confidence that Pre/Post config inheritance is correct.

**Fix:** Seed a realistic Pre/Post batch — an existing PreTest **and** an existing PostTest sibling with a *later* `PostSchedule` and a *distinct* `PostExamWindowCloseDate` — then assert the new PostTest inherits the **Post** schedule/window (and `GenerateCertificate`), not the Pre's. Example:

```csharp
// seed both siblings of an existing participant
var preSched  = DateTime.UtcNow.AddHours(7).AddHours(-2);
var postSched = DateTime.UtcNow.AddHours(7).AddHours(-1); // later than pre
await SeedRepSessionAsync(seed, repUser, title, cat, preSched,  S.Open, assessmentType: "PreTest",  linkedGroupId: linkedGroupId);
await SeedRepSessionAsync(seed, repUser, title, cat, postSched, S.Open, assessmentType: "PostTest", linkedGroupId: linkedGroupId);
...
Assert.Equal(postSched.Date, newPost.Schedule.Date);   // new Post takes Post's schedule, not Pre's
```

This converts T9 into a genuine regression guard for WR-01.

## Info

### IN-01: `CreatedBy` set to empty string instead of null when actor unresolved

**File:** `Controllers/AssessmentAdminController.cs:2354-2355` + `BuildReadyParticipantSession :2329`

**Issue:** `actorId = hcUser?.Id ?? ""` and the helper sets `CreatedBy = actorId`, so an unresolved actor stores `""` rather than `null`. The established BULK ASSIGN / Pre/Post analogs use `editUser?.Id` (nullable → `null`). In practice `[Authorize]` guarantees an authenticated principal, so this path is essentially unreachable, but `""` is a less honest "unknown" than `null` and is inconsistent with sibling rows.

**Fix:** Pass a nullable actor id to the helper (e.g. `string? actorId = hcUser?.Id;`) so `CreatedBy` stays `null` when unknown, matching existing rows. Keep the non-null `actorName` for the audit log.

### IN-02: Picker includes Admin/HC accounts (no role filter) — accepted by design, worth a one-line guard later

**File:** `Controllers/AssessmentAdminController.cs:2294-2299`

**Issue:** `GetEligibleParticipantsToAdd` returns every `IsActive` user minus those already in the batch, with no role filter. Admin/HC accounts that happen to be `IsActive` will appear in the picker. This is explicitly accepted (D-02 / threat T-410-08 "accept") and idempotency protects against real harm, so it is not a bug. Flagging only so it is not lost: if an admin is accidentally added they become an exam participant.

**Fix:** None required for 410. If undesired later, exclude role-bearing accounts in the picker query (a `GetUsersInRoleAsync`-based filter), or filter by a "worker" attribute. Leave as-is per D-02 unless a real need surfaces.

### IN-03: `rep` may itself be a soft-removed session (stale-inherit) — accepted (T-410-09), no guard

**File:** `Controllers/AssessmentAdminController.cs:2346` (rep resolve) — `FirstOrDefaultAsync(s => s.Id == sessionId)`

**Issue:** Both endpoints resolve `rep` by raw `sessionId` without checking `RemovedAt == null`. If the caller passes a soft-removed session's id, its config is used to seed new participants. Threat T-410-09 dispositions this as "accept" (config fields don't change on soft-remove), so it is low risk. Noting for completeness; combined with WR-01, picking a removed rep compounds the chance of stale Pre/Post config.

**Fix:** Optional hardening — prefer an active rep when resolving the representative, e.g. `OrderBy(s => s.RemovedAt == null ? 0 : 1)` or filter `RemovedAt == null` with a fallback. Defer unless WR-01 is addressed.

---

_Reviewed: 2026-06-21_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: deep_
