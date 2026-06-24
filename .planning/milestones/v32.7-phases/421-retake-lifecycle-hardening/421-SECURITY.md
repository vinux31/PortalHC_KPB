---
phase: 421-retake-lifecycle-hardening
type: security-verification
asvs_level: 1
threats_total: 14
threats_closed: 14
threats_open: 0
block_on: high
verdict: SECURED
verified: 2026-06-23
---

# Phase 421 — Retake Lifecycle Hardening: Security Verification

State B verification — no prior SECURITY.md. Threat models extracted from
`421-01/02/03-PLAN.md` `<threat_model>` blocks (T-421-01..14). Each threat verified
against implemented code by declared disposition (mitigate / accept). Implementation
files READ-ONLY — no patches applied. `block_on: high` — no HIGH threat left open.

**Verdict: SECURED — 14/14 threats closed, 0 open.**

## Threat Verification

| Threat ID | Category | Disposition | Evidence |
|-----------|----------|-------------|----------|
| T-421-01 | DoS (self-inflicted dead-end) | mitigate | **CLOSED.** Two-layer D-01. Layer 1 (eligibility): `RetakeRules.CanRetake` window gate `nowUtc.AddHours(7) > examWindowCloseDate.Value` (`Helpers/RetakeRules.cs:52`, before cooldown). Layer 2 (execution): abort-before-destroy in `RetakeService.ExecuteAsync` returns `RetakeResult(false, "Masa ujian sudah ditutup…")` at `Services/RetakeService.cs:96-97` — placed BEFORE `BeginTransactionAsync` (:102) and RemoveRange (:186) → live session intact. UX: `IsInCooldown && !windowClosed` (`CMPController.cs:2500`) no dead-end countdown. UAT live @5270 confirmed no dead-end. |
| T-421-02 | Tampering (client window bypass) | mitigate | **CLOSED.** Server-authoritative `CanRetakeAsync` re-check in RetakeExam POST (`CMPController.cs:2543`) — now window-aware: `CanRetakeAsync` supplies `s.ExamWindowCloseDate` to `RetakeRules.CanRetake` (`RetakeService.cs:251`). Abort defense-in-depth at `RetakeService.cs:96-97`. Countdown/hide = UX only. `[ValidateAntiForgeryToken]` preserved (`CMPController.cs:2532`). |
| T-421-03 | EoP (IDOR — retake other's session) | accept (existing PRESERVED) | **CLOSED.** Ownership guard present + not regressed: `if (assessment.UserId != user.Id) return Forbid();` (`CMPController.cs:2540`), effective-user impersonation-aware (`GetCurrentUserRoleLevelAsync`, :2538). Class-level `[Authorize]` (`CMPController.cs:25`). Not modified by this phase. |
| T-421-04 | Tampering (dangling cert number) | mitigate | **CLOSED.** D-03 `SetProperty(r => r.NomorSertifikat, (string?)null)` in claim ExecuteUpdateAsync (`Services/RetakeService.cs:122`). HC ResetAssessment covered via delegation (no hand-roll). PDF download guard `IsPassed!=true→NotFound` existing. |
| T-421-05 | Info Disclosure (key leak when retake impossible) | accept-by-design / mitigate | **CLOSED.** `windowClosed → attemptsRemaining=false` (`CMPController.cs:2487`) → `ResolveReviewMode` opens full review (`RetakeRules.cs:93-99`, retake impossible = treat exhausted). Non-window cooldown stays `WrongFlagsOnly` — leak-safe A1 (v32.4) preserved: `if (isPassed != true && attemptsRemaining) return ShowWrongFlagsOnly` (`RetakeRules.cs:97`). |
| T-421-06 | Tampering (counting drift over-count) | mitigate | **CLOSED.** D-05 `RetakeCountingRules.CountEraRetakeArchives` snapshot-presence base `archive.Any(a => a.AttemptHistoryId == h.Id)` (`Helpers/RetakeCountingRules.cs:30-34`). Wired 4 sites: RetakeService ExecuteAsync (:156) + CanRetakeAsync (:245), CMPController Results (:2473), AssessmentAdminController warning (MaxInGroupAsync). Parity tests `RetakeCountingRulesTests` 5/5 green; 0 inline-predicate drift remaining. |
| T-421-07 | Info Disclosure (helper DB-aware cross-user) | accept | **CLOSED.** Aggregate count only (`MaxInGroupAsync` returns int — max attempt count in group, `RetakeCountingRules.cs:50-56`); no PII/answers exposed. Called only from `[Authorize(Roles="Admin, HC")]` actions (ManagePackages/UpdateRetakeSettings). |
| T-421-08 | EoP (RBAC — helper count access) | accept (existing PRESERVED) | **CLOSED.** Helper invoked only from `[Authorize(Roles="Admin, HC")]` actions (`AssessmentAdminController.cs:5685` UpdateRetakeSettings, :5753 ManagePackages) or worker Results (ownership). No new endpoint added. |
| T-421-09 | Tampering (forged confirmRemoveWithHistory) | mitigate | **CLOSED.** Flag only suppresses warning: `if ((preHasHistory \|\| postHasHistory \|\| hasAttemptHistory) && !confirmRemoveWithHistory)` (`AssessmentAdminController.cs:1957`) — history guard re-evaluated server-side each POST. Hard-block InProgress/Completed (:1933-1942) NOT bypassable by flag. `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (:1821-1822). View round-trip replays `UserIds` + flag with `@Html.AntiForgeryToken()` (`EditAssessment.cshtml:726-728`). UAT confirmed server-authoritative round-trip. |
| T-421-10 | EoP (RBAC on 3 POST actions) | accept (existing PRESERVED) | **CLOSED.** All three POST actions carry `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`: EditAssessment (`:1821-1822`), UpdateRetakeSettings (`:5685-5686`), ResetAssessment (`:4317-4318`). No new endpoint. |
| T-421-11 | Info Disclosure / Data Integrity (orphan archive) | mitigate | **CLOSED.** Cascade DB `OnDelete(DeleteBehavior.Cascade)` on `AssessmentAttemptResponseArchive.AttemptHistoryId` (`ApplicationDbContext.cs:591-594`) via tracked `RemoveRange(attempts)` (`AssessmentAdminController.cs:1978`) + `SaveChangesAsync` (:2055). No ExecuteDelete/raw on archive (grep 0 match). `ParticipantRemoveGuardTests` Test-4 asserts 0 orphan; UAT live confirmed orphan=0. |
| T-421-12 | Tampering (retroactive MaxAttempts/cooldown lower) | accept-by-design | **CLOSED.** NON-BLOCKING by CONTEXT decision: `SaveChangesAsync` (`AssessmentAdminController.cs:5717`) happens BEFORE D-02 warning (:5722-5725); no return-early; in-flight attempts not cancelled (retroactive behavior preserved). Server clamp `Math.Clamp(maxAttempts,1,5)` / `Math.Clamp(cooldown,0,168)` preserved (:5700-5701). UAT confirmed non-blocking save (D-02/D-07). |
| T-421-13 | DoS (guard AnyAsync N+1) | accept | **CLOSED.** `AnyAsync(h => sessIdsForHistCheck.Contains(h.SessionId))` (`AssessmentAdminController.cs:1955-1956`) — small participant groups; batch rate-limit `NewUserIds.Count > 50` block present (`:919`, `:2080`). No significant user-controlled amplification. |
| T-421-14 | Tampering (mass-reset cert revoke without warning) | mitigate | **CLOSED.** D-04 conditional confirm in `AssessmentMonitoringDetail.cshtml`: `hasCert = session.IsPassed == true \|\| session.NomorSertifikat != null` (:335) → "MENCABUT sertifikat" copy (:337). **Render fix VERIFIED** (commit 8800d0e2): `onsubmit="return confirm('@resetConfirm')"` (:342) uses single-quote JS-string idiom — NOT the broken `Json.Serialize` that quote-broke the attribute. `@Html.AntiForgeryToken()` + hidden `id` preserved (:343-344). Cert-null stays server-side (D-03, `RetakeService.cs:122`). RBAC + antiforgery on ResetAssessment (`:4317-4318`). UAT confirmed confirm renders after fix. |

## Unregistered Flags

None. SUMMARY.md files (421-01/02/03) contain no `## Threat Flags` section — no new
attack surface detected by executor during implementation.

## WIB Convention Integrity (kill-drift check)

All window-time comparisons use the `+7h WIB` convention verbatim (byte-identik with
StartExam `CMPController:956`). No new `DateTime.Now` / `+8h` / `TimeZoneInfo`:
- `RetakeRules.CanRetake`: `nowUtc.AddHours(7)` (`RetakeRules.cs:52`)
- `RetakeRules.CooldownMayExceedWindow`: `nowUtc.AddHours(7)` (`RetakeRules.cs:79`)
- `RetakeService.ExecuteAsync`: `DateTime.UtcNow.AddHours(7)` (`RetakeService.cs:96`)
- `CMPController.Results`: `DateTime.UtcNow.AddHours(7)` (`CMPController.cs:2478`)

Predicate D-02 lives only in `CooldownMayExceedWindow` (pure) — controller + test call
the same code (drift +7h→+8h would fail unit test). Kill-drift verified.

## Verdict

**SECURED.** 14/14 threats closed, 0 open. ASVS Level 1. `block_on: high` satisfied —
the single HIGH (T-421-01, RTK-LOGIC-02 dead-end) is closed two-layer and UAT-confirmed.
No implementation gaps. SECURITY.md is the sole artifact written; no implementation
files modified.
