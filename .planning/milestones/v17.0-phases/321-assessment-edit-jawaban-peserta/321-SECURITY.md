# SECURITY.md — Phase 321 Assessment Edit Jawaban Peserta

**Audit Date:** 2026-05-22
**Auditor:** gsd-security-auditor (retroactive compliance documentation)
**Phase Status:** Code complete di main (tag v17.0-p321-complete, merge commit 25e37824)
**ASVS Level:** Not configured

---

## Threat Verification Summary

**Threats Closed:** 14/14
**Threats Open:** 0/14
**Unregistered Flags:** 0

---

## Threat Register — Verification Detail

| Threat ID | Category | Disposition | Status | Evidence (File:Line) |
|-----------|----------|-------------|--------|----------------------|
| T-321-01 | Tampering (audit integrity) | mitigate | CLOSED | `Data/ApplicationDbContext.cs:241` — `OnDelete(DeleteBehavior.Restrict)` + `HasDatabaseName("IX_AssessmentEditLogs_SessionId_EditedAt")`. Tidak ada `Remove` atau `ExecuteDeleteAsync` pada `AssessmentEditLogs` di controllers/services. Satu-satunya update adalah backfill `NewScore/NewIsPassed` (insert-then-backfill, bukan update arbitrer). |
| T-321-02 | EoP (eligibility bypass) | mitigate | CLOSED | `Helpers/AssessmentEditEligibility.cs` — `IsEditableAsync` (Status, IsManualEntry, ProtonT3, DB check) dipanggil di: GET `EditPesertaAnswers` (`:2695`), POST `SubmitEditAnswers` (`:2772`), POST `PreviewEditScore` (`:2961`). `IsEditableShallow` dipanggil di view dropdown (`:287` MonitoringDetail). |
| T-321-03 | Tampering (race recompute) | mitigate | CLOSED | `Services/GradingService.cs:466` — `ExecuteUpdateAsync` dengan `WHERE s.Id == session.Id && s.Status == "Completed"`. `rowsAffected == 0` → `throw InvalidOperationException`. |
| T-321-03b | Tampering (cert dup key race) | mitigate | CLOSED | `Services/GradingService.cs:528` — `catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && HcPortal.Helpers.CertNumberHelper.IsDuplicateKeyException(ex))`. Retry loop 3x. |
| T-321-03c | Info Disc (PreviewScoreAsync leak) | mitigate | CLOSED | `Services/GradingService.cs:577-584` — `PreviewScoreAsync` hanya 4 baris: delegate ke `ComputeScoreAndETInternalAsync` + return `(pct, isPassed)`. Zero `SaveChangesAsync`/`AddAsync`/`ExecuteUpdateAsync`/`ExecuteDeleteAsync` di body method. |
| T-321-04 | EoP (Auth gating) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs`: GET `EditPesertaAnswers` line 2683 `[Authorize(Roles = "Admin, HC")]`; POST `SubmitEditAnswers` line 2759 `[Authorize(Roles = "Admin, HC")]`; POST `PreviewEditScore` line 2954 `[Authorize(Roles = "Admin, HC")]`; GET `EditHistoryPartial` line 2943 `[Authorize(Roles = "Admin, HC")]`. |
| T-321-05 | Tampering (CSRF) + Info Disc (XSS) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs`: `[ValidateAntiForgeryToken]` di POST `SubmitEditAnswers` (:2760) + POST `PreviewEditScore` (:2955). `Views/Admin/EditPesertaAnswers.cshtml:37` — `@Html.AntiForgeryToken()`. |
| T-321-05b | Info Disc (XSS Edit History) | mitigate | CLOSED | `Views/Admin/_EditHistoryPartial.cshtml` — seluruh file 40 baris menggunakan Razor `@` auto-encode (`@log.QuestionTextSnapshot`, `@log.OldAnswerTextSnapshot`, `@log.NewAnswerTextSnapshot`, `@log.ActorRole`, `@log.ActorName`, `@log.ReasonText`). Grep `@Html.Raw` pada file ini: No matches found. |
| T-321-06 | Tampering (Transaction integrity) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:2799` — `using var tx = await _context.Database.BeginTransactionAsync()`. `tx.RollbackAsync()` di: `:2822`, `:2828`, `:2834`, `:2883`, `:2935`. `tx.CommitAsync()` di `:2910`. |
| T-321-07 | Concurrency (lost-update race) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:2779` — `if (Math.Abs((currentUpdatedAt - form.UpdatedAt).TotalSeconds) > 1)`. TempData error "Sesi sudah diubah admin lain. Refresh halaman." (:2781). |
| T-321-08 | Info Disc + Tampering (Preview contract violation) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:2956-2978` — `PreviewEditScore` action body: hanya `FirstOrDefaultAsync`, `IsEditableAsync`, `PreviewScoreAsync` (pure), `return Json(...)`. Tidak ada `SaveChanges`/`Add`/`Update`/`Remove`/`ExecuteUpdate`/`ExecuteDelete`. |
| T-321-09 | EoP (UI dropdown bypass) | mitigate | CLOSED | `Views/Admin/AssessmentMonitoringDetail.cshtml:287` — `@{ var canEdit = HcPortal.Helpers.AssessmentEditEligibility.IsEditableShallow(session); }`. `:314` — `@if (canEdit)` gate di Razor (server-side render, bukan JS hide). Defense-in-depth: server GET enforces full `IsEditableAsync`. |
| T-321-10 | Info Disc (cross-session audit leak) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:2947` — `.Where(l => l.AssessmentSessionId == sessionId)` strict filter. `[Authorize(Roles = "Admin, HC")]` di action. |
| T-321-11 | Tampering (cascade orphan cert) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:2890` — `RegradeAfterEditAsync` dipanggil di dalam scope `tx` (BeginTransactionAsync line 2799). Exception apa pun → `tx.RollbackAsync()` (:2935). UAT 1 (DB cascade Pass↔Fail atomicity) telah dieksekusi dan verified per 321-05-SUMMARY.md. |

---

## Catatan Deviasi (Non-Security Impact)

Deviation dari PLAN yang dicatat di SUMMARY tapi tidak berdampak pada security posture:

1. **MonitoringSessionViewModel overload** (PLAN 04): `IsEditableShallow` ditambah overload untuk `MonitoringSessionViewModel` (relaxed: hanya check `Status == "Completed"`). Server GET tetap enforce full `IsEditableAsync` — T-321-02 defense-in-depth tidak terpengaruh.

2. **AuditLog field rename** (PLAN 04): `UserId` → `ActorUserId`, `EntityType` → `TargetType`, `EntityId` → `TargetId`. Schema AuditLog berbeda dari yang diasumsikan di PLAN, tapi tidak mengubah security property (audit tetap ditulis per-edit).

3. **Reason validation fix** (PLAN 05 commit `dc282b58`): Validation loop diperbaiki supaya hanya require reason untuk question yang jawabannya berubah (`oldOptionIdsSet.SetEquals(sanitizedNewSet)` skip check). Bukan regresi security — validasi sisi server tetap enforced untuk semua perubahan aktual.

---

## Unregistered Threat Flags

Tidak ada threat flags yang dilaporkan di SUMMARY.md di luar daftar threat register yang ada.

---

## Outstanding Actions (Non-Security)

Berikut item yang masih pending dari PLAN 05 Task 3 (user action — tidak mempengaruhi security posture kode):

- UAT 2 SignalR cross-tab live update manual verify
- UAT 3 Activity Log Edit History tab manual verify
- UAT 4 Migration rollback re-verify
- Tag `v17.0-p321-complete` (jika belum di-push ke origin)
- IT notify final dengan flag migration

---

## Accepted Risks Log

Tidak ada threat yang di-accept dalam phase ini. Semua 14 threat memiliki disposisi `mitigate`.
