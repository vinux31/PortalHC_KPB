---
phase: 312
plan: 01
subsystem: admin-assessment-management
tags: [security, role-tier-guard, audit-log, ajax-endpoint, cascade-delete]
requires:
  - "Models/AssessmentSession.cs (AssessmentType, NomorSertifikat, LinkedGroupId, Status fields — all existing)"
  - "Models/AssessmentAttemptHistory.cs (SessionId field — NOT AssessmentSessionId)"
  - "Services/AuditLogService.cs (LogAsync signature stable)"
  - "ApplicationDbContext (PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages, AssessmentSessions DbSets)"
provides:
  - "EnsureCanDeleteAsync(actionPrefix, targetId, entityType, sessions) -> IActionResult? — role-tier guard helper"
  - "GetDeleteImpact(int id, string type) -> JSON — AJAX impact preview untuk modal Plan 02"
  - "AuditLog blocked entry pattern: ActionType suffix 'Blocked' (DeleteAssessmentBlocked, DeleteAssessmentGroupBlocked, DeletePrePostGroupBlocked)"
  - "AuditLog success extended: description sertakan 'Status=... ResponseCount=...' (3 delete methods)"
affects:
  - "Controllers/AssessmentAdminController.cs (1 file modified — 224 lines added)"
tech-stack:
  added: []
  patterns:
    - "ASP.NET Core MVC body-method role guard (Admin override + HC reject pattern, returns IActionResult? null/RedirectToAction)"
    - "AJAX HttpGet impact preview endpoint dengan AsNoTracking + branched query (single/group/prepost)"
    - "AuditLog Blocked suffix convention (new naming convention untuk repo)"
key-files:
  created: []
  modified:
    - "Controllers/AssessmentAdminController.cs"
decisions:
  - "Pakai field discriminator AssessmentSession.AssessmentType ('PreTest'/'PostTest') — confirmed di Models/AssessmentSession.cs:154 (Q3 RESOLVED)"
  - "Pakai NomorSertifikat (string, nullable) sebagai cert proxy — Models/AssessmentSession.cs:72"
  - "AssessmentAttemptHistory FK pakai SessionId (Models/AssessmentAttemptHistory.cs:9) — bukan AssessmentSessionId seperti yang tercantum di PATTERNS.md (deviasi minor — pakai field name yang benar)"
  - "Helper EnsureCanDeleteAsync ditempatkan setelah closing #endregion ExtraTime + sebelum class closing brace (locality dengan helpers existing)"
  - "GetDeleteImpact ditempatkan setelah GetAkhiriSemuaCounts (line 3435) untuk grouping locality dengan AJAX endpoint lain"
metrics:
  duration: "~25 menit (termasuk debug worktree path issue)"
  tasks_completed: 2
  files_modified: 1
  lines_added: 224
  commits: 2
  build_warnings_delta: 0
completed: "2026-05-07T11:25:25Z"
---

# Phase 312 Plan 01: Admin Full-Delete Backend Guard Summary

Phase 312 Plan 01 menambah `EnsureCanDeleteAsync` private helper + `GetDeleteImpact` HttpGet AJAX endpoint di `Controllers/AssessmentAdminController.cs`, lalu inject guard call ke 3 delete methods (`DeleteAssessment`, `DeleteAssessmentGroup`, `DeletePrePostGroup`) sebagai PRIMARY mitigation T-312-01 (privilege escalation HC bypass via direct POST). Authorize attributes existing tidak diubah; guard hidup di body method (per CONTEXT.md D-04 lock). AuditLog success description di-extend dengan `Status=... ResponseCount=...` per UI-SPEC line 167-169. Build pass 92 warnings (zero new vs Phase 311 baseline).

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add EnsureCanDeleteAsync helper + GetDeleteImpact AJAX endpoint | `0b9a5e34` | Controllers/AssessmentAdminController.cs |
| 2 | Inject guard call + extend AuditLog success di 3 delete methods | `3e233197` | Controllers/AssessmentAdminController.cs |

## Implementation Details

### Helper Signature & Location

**`EnsureCanDeleteAsync`** ditempatkan di line 5474, setelah `#endregion` ExtraTime block, sebelum class closing brace.

```csharp
private async Task<IActionResult?> EnsureCanDeleteAsync(
    string actionPrefix,
    int targetId,
    string entityType,
    IList<AssessmentSession> sessions)
```

**Logic:**
1. `User.IsInRole("Admin")` → return null (Admin override, lewat semua cek)
2. HC tier: hitung `responseCount` dari `PackageUserResponses` + cek `anyCompleted` dari `sessions.Status == "Completed"`
3. Jika `anyCompleted || responseCount > 0`:
   - Tulis AuditLog blocked entry dengan ActionType `{actionPrefix}Blocked` + description sertakan Status & ResponseCount
   - Try/catch swallow audit failure (logger.LogWarning)
   - Set `TempData["Error"]` Bahasa Indonesia (compliance CLAUDE.md)
   - Return `RedirectToAction("ManageAssessment")`
4. Else return null (pass — caller lanjut cascade)

**Status summary format per action type** (UI-SPEC line 170-172):
- `DeleteAssessment` (single session): `sessions[0].Status` (e.g. `"Completed"`)
- `DeleteAssessmentGroup` (multi sibling): `string.Join("/", group counts)` (e.g. `"2 Open/1 Completed"`)
- `DeletePrePostGroup` (Pre+Post pair): `"PreTest:{status},PostTest:{status}"` (e.g. `"PreTest:Open,PostTest:Completed"`)

### 3 Delete Method Edits

**Predicted line numbers (PLAN.md) vs actual (post-edit):**

| Method | PLAN guard insert | Actual guard line | PLAN audit success | Actual audit line |
|--------|-------------------|-------------------|--------------------|-------------------|
| `DeleteAssessment` | line ~2046 (after PrePost block) | line 2049 | line ~2102 | line 2114 |
| `DeleteAssessmentGroup` | line ~2152 (after siblings load) | line 2172 | line ~2207 | line 2222 |
| `DeletePrePostGroup` | line ~2247 (after groupSessions load) | line 2282 | line ~2299 | line 2316 |

Alasan offset: insertion menambah ~10-20 line per method (snapshot vars + guard call). Cascade order PRESERVED untuk semua 3 method:
- `EnsureCanDeleteAsync` (guard) → `PackageUserResponses.RemoveRange` → `AssessmentAttemptHistory.RemoveRange` → `AssessmentPackages.RemoveRange` → `AssessmentSessions.Remove[Range]` → `SaveChangesAsync` → AuditLog success

**Snapshot vars (pre-cascade capture):**
- DeleteAssessment: `preDeleteStatus` (string) + `preDeleteResponseCount` (int)
- DeleteAssessmentGroup: `preDeleteStatus` (aggregated `string.Join(" / ", group counts)`) + `preDeleteResponseCount` (int) + `preDeleteSessionCount` (int)
- DeletePrePostGroup: `preDeleteStatus` (per-session `"PreTest:X,PostTest:Y"`) + `preDeleteResponseCount` (int)

### GetDeleteImpact Endpoint

**Location:** Controllers/AssessmentAdminController.cs:3441 (setelah `GetAkhiriSemuaCounts`).

**Signature:**
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetDeleteImpact(int id, string type)
```

**JSON Response Contract (success):**
```json
{
  "status": "Open" | "Completed" | "2 Open/1 Completed" | "PreTest:Open,PostTest:Completed",
  "responseCount": 0,
  "certCount": 0,
  "packageCount": 1,
  "attemptCount": 0,
  "sessionCount": 1,
  "prePostBreakdown": null | {
    "pre": { "status": "Open", "responseCount": 0 },
    "post": { "status": "Open", "responseCount": 0 }
  }
}
```

**Error responses:**
- `BadRequest` (400) — invalid type (selain "single", "group", "prepost")
- `NotFound` (404) — session tidak ditemukan
- `StatusCode(500)` — internal exception

**Performance:** Semua read pakai `AsNoTracking()`. Per request total 4 queries (single) atau 5 queries (prepost — extra per-session response count).

### Field Discrimination Decisions

| Field | Source verified | Used for |
|-------|-----------------|----------|
| `AssessmentSession.AssessmentType` (string nullable: `"PreTest"` \| `"PostTest"` \| null) | Models/AssessmentSession.cs:154 | Pre/Post discriminator di GetDeleteImpact + EnsureCanDeleteAsync helper |
| `AssessmentSession.NomorSertifikat` (string nullable) | Models/AssessmentSession.cs:72 | Cert proxy: `sessions.Count(s => !string.IsNullOrEmpty(s.NomorSertifikat))` |
| `AssessmentSession.LinkedGroupId` (int nullable) | Models/AssessmentSession.cs:165 | PrePost grouping query |
| `AssessmentAttemptHistory.SessionId` (int) — **NOT AssessmentSessionId** | Models/AssessmentAttemptHistory.cs:9 | FK untuk attempt count query (deviasi vs PATTERNS.md draft — lihat di bawah) |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Field name di GetDeleteImpact attemptCount query**

- **Found during:** Task 1 implementation (cross-check terhadap existing cascade pattern line 2060)
- **Issue:** PLAN.md Task 1 (line 287) tertulis `_context.AssessmentAttemptHistory.CountAsync(a => sessionIds.Contains(a.AssessmentSessionId))`. Field `AssessmentSessionId` TIDAK exist di model `AssessmentAttemptHistory` — field aktual adalah `SessionId` (Models/AssessmentAttemptHistory.cs:9). Existing cascade di 3 delete methods (lines 2060, 2167, 2264) sudah pakai `h.SessionId`. Jika diikuti literal PLAN, build akan gagal.
- **Fix:** Pakai `h.SessionId` di GetDeleteImpact attemptCount query (line 3514) — match dengan existing cascade convention. Comment inline mendokumentasikan keputusan.
- **Files modified:** Controllers/AssessmentAdminController.cs (line 3511-3514)
- **Commit:** `0b9a5e34`

### Process Notes (non-deviation)

**Worktree path detection during execution:**

Pada awal Task 1, edits saya tertulis ke main repo path (`C:/Users/.../PortalHC_KPB/Controllers/...`) bukan worktree (`.claude/worktrees/agent-.../Controllers/...`). Ini kemungkinan karena Read tool menerima absolute path ke main repo file untuk verifikasi context, lalu Edit tool mereferensikan path absolute yang sama.

**Resolution:** Diff main-repo edits → save sebagai patch → restore main repo file (`git checkout -- <file>` — specific file, bukan blanket reset) → apply patch ke worktree path → commit di worktree branch. Tidak ada destructive operation yang melanggar rule worktree (tidak `git clean`, tidak `git reset --hard` worktree, tidak blanket `git checkout .`).

Untuk Task 2, semua edits langsung ditulis ke worktree path absolute. Tidak ada cleanup diperlukan.

## Verification Results

### Build (dotnet build --no-incremental)

```
Build succeeded.
    92 Warning(s)
    0 Error(s)
```

**Delta vs Phase 311 baseline (92 warnings):** zero new warnings.

### Acceptance Criteria

| Criterion | Expected | Actual |
|-----------|----------|--------|
| `EnsureCanDeleteAsync` declaration count | 1 | 1 ✓ |
| `EnsureCanDeleteAsync` total references (decl + calls) | ≥ 4 | 4 ✓ |
| `GetDeleteImpact` declaration count | 1 | 1 ✓ |
| `User.IsInRole("Admin")` count (in helper) | ≥ 1 | 1 ✓ |
| `[Authorize(Roles = "Admin, HC")]` count (3 existing delete + 1 new endpoint, plus existing 47) | ≥ 4 | 51 ✓ |
| `AsNoTracking` count (existing + ≥ 5 in new endpoint) | ≥ 5 | 14 ✓ |
| `preDeleteStatus` references | ≥ 3 | 6 ✓ |
| `preDeleteResponseCount` references | ≥ 3 | 6 ✓ |
| `Status=...ResponseCount=` description matches | ≥ 3 | 4 ✓ (3 success + 1 helper blocked) |
| `_context.AssessmentSessions.Remove*` cascade preserved | ≥ 3 | 4 ✓ |
| `_context.PackageUserResponses.Remove*` cascade preserved | ≥ 3 | 7 ✓ |
| Guard pre-cascade ordering (3 methods) | guard line < cascade lines | DeleteAssessment 2049<2068<2102; DeleteAssessmentGroup 2172<2190<2220; DeletePrePostGroup 2282<2303<2329 ✓ |
| Existing Authorize attributes line 2020/2125/2230 unchanged | unchanged | Verified via grep ✓ |

### Threat Model Coverage

| Threat ID | Status | Mitigation realized |
|-----------|--------|---------------------|
| T-312-01 (Privilege Escalation: HC direct POST bypass) | mitigated | `EnsureCanDeleteAsync` body guard di 3 delete methods (PRIMARY) — verified via grep |
| T-312-02 (CSRF on destructive POST) | mitigated | `[ValidateAntiForgeryToken]` existing — preserved unchanged |
| T-312-03 (Audit gaps for failed attempts) | mitigated | AuditLog blocked entry dengan ActionType suffix `Blocked` + description reason — verified pada helper line 5524-5536 |
| T-312-04 (Info disclosure via GetDeleteImpact) | accepted | `[Authorize(Roles = "Admin, HC")]` apply ke endpoint — verified line 3439 |

## Self-Check: PASSED

**Files exist:**
- FOUND: Controllers/AssessmentAdminController.cs (5499 lines, post-edit)
- FOUND: .planning/phases/312-admin-full-delete-assessment-room/312-01-SUMMARY.md (this file)

**Commits exist:**
- FOUND: 0b9a5e34 (Task 1: helper + endpoint)
- FOUND: 3e233197 (Task 2: guard injection + audit extension)

**Build status:** dotnet build PASS, 92 warnings (zero new), 0 errors.

## Open Items / Wave 2 Handoff

Plan 02 (Wave 2) akan:
1. Refactor `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (`<form onclick="return confirm(...)">` → modal-based 2-step impact preview flow yang call `GetDeleteImpact`)
2. Apply UI conditional render `@if (User.IsInRole("Admin") || canHcDelete)` (D-01 hide entirely)
3. Append FLOW 12 di `tests/e2e/assessment.spec.ts` (6 skenario smoke)
4. Create `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md`

Backend di Plan 01 sudah ready (helper + endpoint stable), Plan 02 cuma compose UI + tests.

---

*Phase: 312-admin-full-delete-assessment-room*
*Plan: 01 (backend guard + AJAX endpoint)*
*Completed: 2026-05-07*
*Bahasa: Bahasa Indonesia (per CLAUDE.md)*
