---
phase: 321-assessment-edit-jawaban-peserta
plan: 04
type: execute
wave: 4
status: complete
completed_at: 2026-05-22
commits:
  - 71480095
  - 96f1fff8
  - 5d8bd6f9
  - 4eeb7925
---

# PLAN 04 — POST + Dropdown + SignalR (SUMMARY)

## Commits

| Hash | Message |
|------|---------|
| `71480095` | feat(v17.0-p321): POST SubmitEditAnswers (tx + audit + regrade + SignalR workerAnswerEdited) |
| `96f1fff8` | feat(v17.0-p321): per-user action dropdown ⋮ hybrid (bi-icons + ARIA + IsEditableShallow gating) |
| `5d8bd6f9` | fix(v17.0-p321): IsEditableShallow overload for MonitoringSessionViewModel (relaxed UI gate, server GET enforces full) |
| `4eeb7925` | feat(v17.0-p321): SignalR workerAnswerEdited handler + showAssessmentToast delayMs (D-07 8s LOCKED) |

## Deviations from Plan

1. **AuditLog field names** — plan written against assumed schema; actual fields renamed:
   - `UserId` → `ActorUserId`
   - `EntityType` → `TargetType`
   - `EntityId = session.Id.ToString()` → `TargetId = session.Id` (string→int)
   - Required `ActorName` added (else EF Required violation)

2. **`FindFirstValue` extension missing** — replaced with `User.FindFirst(...)?.Value` (no `using System.Security.Claims;` import needed).

3. **`IsEditableShallow` accepts `MonitoringSessionViewModel` not `AssessmentSession`** in view loop — added helper overload (relaxed: only `Status == "Completed"` check; IsManualEntry/Proton T3 not in VM, server GET endpoint enforces full eligibility per T-321-02 defense-in-depth).

4. **SignalR handler insertion** — plan refer line 1244 (workerSubmitted start), actual end-of-block line 1300 (after `if (allDone)`). Inserted there.

5. **Smoke UAT browser SKIPPED** — per user decision (interactive mode). Build sebagai gate.

## Threat Mitigations Verified

| ID | Threat | Verification |
|----|--------|--------------|
| T-321-04 | Auth elevation | `[Authorize(Roles = "Admin, HC")]` di POST SubmitEditAnswers |
| T-321-05 | CSRF + XSS | `[ValidateAntiForgeryToken]` + server-side reason validation (whitelist + Lainnya text required) |
| T-321-06 | Transaction integrity | `BeginTransactionAsync` + try/catch + `RollbackAsync` (multi-table write atomic) |
| T-321-07 | Concurrency race | `Math.Abs((current - form.UpdatedAt).TotalSeconds) > 1` (±1s tolerance) + service ExecuteUpdateAsync status guard PLAN 02 |
| T-321-09 | UI dropdown bypass | `IsEditableShallow(vm)` server-side conditional `@if (canEdit)` Razor — bukan JS hide. Defense-in-depth: GET endpoint full `IsEditableAsync` gate. |

## D-07 8s LOCKED Preservation

- `wwwroot/js/assessment-hub.js showToast` signature extended: `function showToast(message, linkUrl, linkText, delayMs)` — `delayMs` default 5000ms (backward compatible).
- Edit handler invoke `window.showAssessmentToast(msg, null, null, 8000)` → D-07 8 detik LOCKED.
- Backward compat verified: existing internal call sites (`onreconnecting:58`, `onreconnected:62`, `onclose:73`) pass max 3 args → default 5000ms preserved.

## Files Modified

- `Controllers/AssessmentAdminController.cs` — +1 action POST SubmitEditAnswers (169 lines, lines 2758-2926 area)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — refactor action column (lines 286-338, +72 -48) + SignalR handler (after line 1300, +33 lines)
- `wwwroot/js/assessment-hub.js` — showToast signature +1 param (delayMs), backward compatible
- `Helpers/AssessmentEditEligibility.cs` — +1 overload `IsEditableShallow(MonitoringSessionViewModel vm)`

## Build Status

0 error setiap final task commit. 22 warning (pre-existing, tidak ditambah PLAN 04).

## Handoff ke PLAN 05

- Endpoint POST + Dropdown + SignalR handler siap.
- BELUM ada:
  - Activity Log Edit History tab (modal AssessmentMonitoringDetail line 540-559 ditambah tab + partial `_EditHistoryPartial.cshtml`)
  - Playwright spec `tests/e2e/edit-peserta-answers.spec.ts`
  - Manual UAT full session (happy path + flip + concurrency + Worker 403)
  - Tag `v17.0-p321-complete`
  - IT handoff final (final notify, beda dgn preemptif PLAN 01)

## Self-Check: PASSED

- 3 task + 1 fix commit atomic.
- T-321-04/05/06/07/09 mitigations grep-verified.
- D-07 8s LOCKED preserved + backward compat.
- 0 compile error final.
- 5 deviations dari plan dokumentasi di section "Deviations from Plan".
