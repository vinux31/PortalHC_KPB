---
phase: 310-essay-finalize-idempotency
plan: 01
subsystem: assessment-admin
tags: [idempotency, race-condition, ef-core, executeupdateasync, audit-log, notification-dedup, viewmodel-extend]
requirements: [ESCG-01]
dependency_graph:
  requires:
    - "Models/AssessmentConstants.cs (Phase 309 D-04 — AssessmentStatus.{Completed,PendingGrading,Open,Upcoming} constants)"
    - "Services/GradingService.cs L195-212 (Phase 309-03 canonical capture-rowsAffected pattern — analog reference)"
    - "Services/AuditLogService.cs (existing LogAsync signature)"
    - "Models/UserNotification.cs (Type, Title, Message, UserId, CreatedAt fields — schema verified)"
  provides:
    - "MonitoringSessionViewModel.Status + NomorSertifikat properties (Plan 02 prerequisite untuk D-02 button gate)"
    - "FinalizeEssayGrading idempotent backend (D-03 alreadyFinalized response, D-04 per-status BI rejection, D-06 capture rowsAffected, D-07 audit gated)"
    - "NotifyIfGroupCompleted dedup query (D-05 — UserNotifications.AnyAsync 5-field guard)"
  affects:
    - "Plan 02 (UI Razor button gate + JS handler upgrade) — wajib consume Status + NomorSertifikat ViewModel + alreadyFinalized JSON contract"
    - "Phase 247 NotifyIfGroupCompleted overlap risk — mitigated via D-05 dedup"
tech_stack:
  added: []
  patterns:
    - "EF Core ExecuteUpdateAsync capture int rows-affected → gate side-effects (audit/cert/notif) with rowsAffected > 0"
    - "AnyAsync dedup-before-insert (analog: TrainingRecord guard L2794-2796)"
    - "Switch expression untuk per-status BI message mapping (D-04)"
    - "Try-catch wrap audit log dengan _logger.LogWarning fallback (precedent Phase 306 D-10)"
key_files:
  created: []
  modified:
    - path: "Models/AssessmentMonitoringViewModel.cs"
      lines: "L60-66"
      change: "Append 2 properties Status (string default '') + NomorSertifikat (nullable) ke MonitoringSessionViewModel"
    - path: "Controllers/AssessmentAdminController.cs"
      lines: "L2588-2589 (mapper) + L2715-2900 (FinalizeEssayGrading refactor + XML doc-comment)"
      change: "Mapper extend (Status, NomorSertifikat from entity); FinalizeEssayGrading refactor — D-03 friendly no-op early-Completed branch + race-lost branch, D-04 per-status BI switch, D-06 capture rowsAffected, D-07 audit gated try-catch, return statement extended dengan nomorSertifikat"
    - path: "Services/WorkerDataService.cs"
      lines: "L331-352"
      change: "Tambah dedup AnyAsync 5-field query (UserId + Type + Title exact + Message.Contains + CreatedAt time-window) sebelum SendAsync; skip + log info kalau alreadySent"
decisions:
  - "D-02 prep: ViewModel expose raw Status (bukan UserStatus remap) supaya Plan 02 Razor gate bisa cek Status==Completed langsung"
  - "D-03: Friendly no-op branching DUA layer — early Completed check (cosmetic, fast-path) + race-lost branch (rowsAffected==0 setelah ExecuteUpdateAsync). Kedua return alreadyFinalized:true contract konsisten"
  - "D-04: Switch expression untuk Open/InProgress/Cancelled + fallback 'Status saat ini:' literal (Bahasa Indonesia mandatory)"
  - "D-05: Schema existing UserNotifications cukup — pakai Title exact + Message.Contains(Title) + CreatedAt >= Schedule.Date time-window (cegah false-positive cross-day)"
  - "D-06: Capture rowsAffected return value dari ExecuteUpdateAsync (existing code throw away). Race-lost path return early dengan AsNoTracking reload current state untuk friendly response — no side-effect duplication"
  - "D-07: Audit log Action='FinalizeEssayGrading' (English machine-readable) wrapped try-catch supaya audit failure tidak break primary flow; gated otomatis oleh early-return saat rowsAffected==0"
  - "Audit Description English (consistent existing convention AddCategory L322-328); user-facing strings Bahasa Indonesia"
metrics:
  duration_minutes: 18
  tasks_completed: 3
  commits: 3
  files_changed: 3
  build_warnings: 92
  build_warnings_baseline: 92
  build_errors: 0
  completed_at: "2026-05-02"
---

# Phase 310 Plan 01: Essay Finalize Idempotency (Backend) Summary

Backend idempotency layer untuk Essay finalize flow — adopt canonical Phase 309-03 GradingService pattern (capture rowsAffected + gate side-effects), tambah notification dedup via AnyAsync, dan extend ViewModel sebagai prerequisite Plan 02 UI gate. Klik 2x Finalize sekarang tidak menduplikasi TrainingRecord/NomorSertifikat/AuditLog/UserNotifications.

## Files Modified

### `Models/AssessmentMonitoringViewModel.cs` (commit `040df74c`)

- **L60-66:** Append 2 properti baru di akhir class `MonitoringSessionViewModel`:
  - `public string Status { get; set; } = ""` — mirror raw `AssessmentSession.Status` (bukan `UserStatus` yang sudah remap "Not started"/"InProgress"/"Completed"/"Dibatalkan")
  - `public string? NomorSertifikat { get; set; }` — mirror nullable `AssessmentSession.NomorSertifikat`

### `Controllers/AssessmentAdminController.cs` (commits `040df74c` mapper + `fd58f45e` method refactor)

- **L2588-2589 (mapper extension)**: Append 2 baris ke LINQ projection `return new MonitoringSessionViewModel { ... }` untuk populate `Status = a.Status ?? ""` dan `NomorSertifikat = a.NomorSertifikat` dari entity.
- **L2715-2725 (XML doc-comment + method signature)**: Tambah XML `<summary>` comment menjelaskan Phase 310 D-03/D-04/D-06/D-07 semantics. Atribut `[HttpPost]/[Authorize(Roles="Admin, HC")]/[ValidateAntiForgeryToken]` dipreserve.
- **L2731-2761 (D-03 + D-04 status branching)**: Replace single-line check `if (session == null || session.Status != "Menunggu Penilaian")` dengan:
  - `session==null` → `"Session tidak ditemukan."`
  - `session.Status==Completed` → friendly no-op `alreadyFinalized:true` + `score`/`isPassed`/`nomorSertifikat`
  - `session.Status != PendingGrading` → switch expression Open/InProgress/Cancelled + fallback `"Tidak bisa di-finalize. Status saat ini: {status}."`
- **L2820-2843 (D-06 capture rowsAffected + race-lost branch)**: Replace `await _context.AssessmentSessions...ExecuteUpdateAsync(...)` (throw away return) dengan `var rowsAffected = await ...ExecuteUpdateAsync(...)` + `if (rowsAffected == 0)` block yang reload current state via `AsNoTracking().FirstOrDefaultAsync` + return alreadyFinalized response. WAJIB pakai `AssessmentConstants.AssessmentStatus.{PendingGrading, Completed}` constants (bukan literal).
- **L2879-2898 (D-07 audit log block)**: Insert audit log block setelah cert generation, sebelum reload+notify. Pattern copy verbatim dari AddCategory L322-328 (`_userManager.GetUserAsync` → actorName format `"{NIP} - {FullName}"`). Wrapped try-catch dengan `_logger.LogWarning` fallback (Phase 306 D-10 precedent — audit failure tidak break primary flow). Action `"FinalizeEssayGrading"`, Description English (`"Session {id} ({title}) finalized: score={pct}%, isPassed={bool}"`), TargetType `"AssessmentSession"`.
- **L2905-2911 (final return extend)**: Replace `return Json(new { success = true, score = finalPercentage, isPassed });` dengan multi-line object literal yang include `nomorSertifikat = updatedSession?.NomorSertifikat` (D-03 contract consistency — UI bisa render cert number tanpa extra fetch).

### `Services/WorkerDataService.cs` (commit `4a71d2c6`)

- **L331-352 (D-05 dedup)**: Insert AnyAsync 5-field guard di awal `foreach (var recipientId in recipientIds)` body, SEBELUM `_notificationService.SendAsync` call. Query: `UserNotifications.AnyAsync(n => UserId == recipientId && Type == "ASMT_ALL_COMPLETED" && Title == "Assessment Selesai" && Message.Contains(completedSession.Title) && CreatedAt >= completedSession.Schedule.Date)`. Kalau alreadySent → `_logger.LogInformation` structured + `continue`. Existing SendAsync call dipreserve as-is.

## Decisions Implemented

| Decision | Implementation | Commit |
|----------|----------------|--------|
| **D-02 prep** | ViewModel `Status` + `NomorSertifikat` expose dari entity raw — Plan 02 Razor bisa gate `Status==Completed` langsung | `040df74c` |
| **D-03** | Friendly no-op response 2 branches: early Completed check (cosmetic) + race-lost (rowsAffected==0 setelah ExecuteUpdateAsync). Field contract: `{success:true, alreadyFinalized:true, message, score, isPassed, nomorSertifikat}` | `fd58f45e` |
| **D-04** | Per-status BI rejection switch expression — Open/InProgress/Cancelled + fallback "Status saat ini:" untuk defensif unknown status | `fd58f45e` |
| **D-05** | UserNotifications.AnyAsync 5-field dedup (Type + Title exact + Message.Contains + CreatedAt time-window) — schema existing, no migration | `4a71d2c6` |
| **D-06** | No new lock — andalkan EF WHERE-clause guard atomic + capture int return + early return saat 0. Pattern 100% match GradingService L195-212 | `fd58f45e` |
| **D-07** | Audit log `_auditLog.LogAsync("FinalizeEssayGrading", ...)` — gated otomatis oleh early-return saat rowsAffected==0; wrapped try-catch supaya tidak break primary flow | `fd58f45e` |

## Build Output

- **Final state:** 0 errors, **92 warnings** (matches Phase 309 baseline exact)
- **Warning diff:** 0 (no new warnings introduced)
- **Acceptance build verifies:** Run setelah Task 1 (92), Task 2 (92), Task 3 (92) — semua identical baseline

## Patterns Reused

| Pattern | Source (canonical) | Target (Phase 310) |
|---------|--------------------|---------------------|
| Capture-and-Gate Idempotent ExecuteUpdateAsync | `Services/GradingService.cs` L195-212 (Phase 309-03) | `FinalizeEssayGrading` L2820-2843 |
| AnyAsync Dedup Before Insert | `Controllers/AssessmentAdminController.cs` L2794-2796 (TrainingRecord guard, same file) | `WorkerDataService.NotifyIfGroupCompleted` L334-345 |
| AuditLog actor format `{NIP} - {FullName}` | `Controllers/AssessmentAdminController.cs` L322-328 (AddCategory) | `FinalizeEssayGrading` L2879-2898 |
| Try-catch audit fallback dengan `_logger.LogWarning` | Phase 306 D-10 precedent | `FinalizeEssayGrading` L2890-2898 |
| `AssessmentConstants.AssessmentStatus.*` constants | Phase 309 D-04 lock | Semua status check di FinalizeEssayGrading (Completed, PendingGrading, Open) |

## Open Items untuk Plan 02

1. **UI Razor button gate (D-02)** — `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419: tambah `disabled` attribute + tooltip Bootstrap saat `session.Status == AssessmentConstants.AssessmentStatus.Completed`. ViewModel sudah expose Status.
2. **JS handler upgrade (D-03/D-04)** — `Views/Admin/AssessmentMonitoringDetail.cshtml` L1331-1359: extract `showAlert(type, icon, message)` helper (analog L1383-1389), branch ke alert-info untuk `data.alreadyFinalized` (jangan reload), alert-danger untuk `success==false` (D-04 messages).
3. **Tooltip activation script** — bottom of `<script>` block: `new bootstrap.Tooltip(el)` untuk semua `[data-bs-toggle="tooltip"]`. Wrap disabled button dalam `<span>` parent (Pitfall #6 — disabled button skip mouseenter event).
4. **Playwright E2E tests** — `tests/e2e/assessment.spec.ts` Phase 310 specs SC #1 + SC #2 (klik 2x → assert response.alreadyFinalized + render alert-info; load Completed session → assert button disabled + tooltip attribute present).
5. **Manual UAT (310-UAT.md)** — sign-off Bahasa Indonesia 4-step + SC #3/#4/#5 verification via SQL query (`COUNT(*) FROM AuditLogs WHERE ActionType='FinalizeEssayGrading' AND TargetId=...` expect 1; UserNotifications dedup 1 per recipient; TrainingRecord 1 row; NomorSertifikat distinct).

## Threat Mitigations Applied

| Threat ID | Category | Mitigation |
|-----------|----------|------------|
| T-310-01 | Tampering | EF `ExecuteUpdateAsync` WHERE-clause guard `Status == PendingGrading` — atomic; thread ke-2 dapat 0 rows, return early via race-lost branch (`fd58f45e`) |
| T-310-02 | Repudiation | Audit `_auditLog.LogAsync` gated otomatis oleh early-return saat rowsAffected==0; race-lost branch return SEBELUM reach audit block (`fd58f45e`) |
| T-310-03 | Information Disclosure | Tetap accept — endpoint `[Authorize(Roles="Admin, HC")]` preserved, NomorSertifikat memang authorized untuk Admin/HC |
| T-310-04 | Denial of Service | rowsAffected guard cap audit pada 1 entry; UserNotifications dedup cap notif pada 1 per recipient (`4a71d2c6` time-window 5-field query) |
| T-310-05 | Elevation of Privilege | Tetap accept — `[Authorize]` + `[ValidateAntiForgeryToken]` preserved, no auth surface change |
| T-310-06 | Information Disclosure | Tetap accept — EF Core parameterizes Message.Contains ke `LIKE @p1`, completedSession.Title DB-sourced (no user-input injection vector) |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Behavioral fidelity] Tambah XML doc-comment ke FinalizeEssayGrading**
- **Found during:** Task 2 acceptance verification
- **Issue:** Plan acceptance criterion `grep -F "\"FinalizeEssayGrading\"" Controllers/AssessmentAdminController.cs returns ≥ 2 (XML doc/comment + audit Action string)` requires literal `"FinalizeEssayGrading"` (with quotes) ≥ 2. Setelah Edit C audit Action insertion, hanya 1 occurrence (audit Action). Plan menyebut "XML doc/comment" tapi method tidak punya doc-comment.
- **Fix:** Tambah `<summary>` XML doc-comment di atas method signature L2716-2723 yang menjelaskan Phase 310 D-03/D-04/D-06/D-07 semantics dan referensi nama Action `"FinalizeEssayGrading"`. Doc-comment ini juga improve maintainability (rationale audit + idempotency tertulis di code).
- **Files modified:** Controllers/AssessmentAdminController.cs (post-edit, dalam commit `fd58f45e`)
- **Commit:** `fd58f45e` (rolled into Task 2 commit — didn't separate into deviation commit because change is < 8 lines and pure documentation)

Tidak ada deviation lain. Plan dieksekusi sesuai spesifikasi (Edit A-D semua applied verbatim).

## Self-Check: PASSED

- File `.planning/phases/310-essay-finalize-idempotency/310-01-SUMMARY.md` will be created via Write tool below — verified path matches plan output spec.
- Commit `040df74c`: verified via `git log --oneline` — present (Task 1 ViewModel + mapper).
- Commit `fd58f45e`: verified via `git log --oneline` — present (Task 2 FinalizeEssayGrading refactor).
- Commit `4a71d2c6`: verified via `git log --oneline` — present (Task 3 NotifyIfGroupCompleted dedup).
- Build verification: 92 warnings, 0 errors — matches Phase 309 baseline.
- All 22+ grep acceptance patterns across Tasks 1-3 returned ≥ 1 (verified inline).

---

*Phase: 310-essay-finalize-idempotency*
*Plan: 01 (backend idempotency)*
*Executed: 2026-05-02 by parallel executor agent (worktree)*
*Next: Plan 02 (UI button gate + JS handler + Playwright tests + manual UAT)*
