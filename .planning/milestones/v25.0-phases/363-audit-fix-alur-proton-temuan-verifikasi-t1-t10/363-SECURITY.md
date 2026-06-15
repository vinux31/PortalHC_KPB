---
phase: 363
slug: audit-fix-alur-proton-temuan-verifikasi-t1-t10
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-11
---

# Phase 363 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Coach/L4 reviewer → CDPController approve/reject endpoints | Authenticated reviewer submits approve/reject; server mutates approval chain state | ProtonDeliverableProgress state (approval status, timestamps, actor IDs) |
| Admin → CoachMappingController reactivation | Admin assigns/reactivates cross-year ProtonTrackAssignment; server enforces year-gate | ProtonTrackAssignment (IsActive, Origin, TahunKe), penanda kelulusan |
| GradingService internal → AssessmentSession cert | System regrading path sets NomorSertifikat; ValidUntil ikut setup sesi | AssessmentSession (NomorSertifikat, ValidUntil, IsPassed) |
| ProtonCompletionService internal → HC notification | Penanda miss surfaced to HC via bell + audit log (actor system) | UserNotifications, AuditLogs (system actor, no PII beyond coachee ref) |
| Developer → local DB (HcPortalDB_Dev) | Temporary UAT seed must be snapshotted + restored per SEED_WORKFLOW | Seed state (temporary, local-only — no Dev/Prod touched) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-363-07 | Tampering (lost update) | ApproveDeliverableCoreAsync concurrent approvers | mitigate | Race-guard reload-fresh AsNoTracking + `stillCanApprove` re-check INSIDE core (CDPController.cs:1012-1027); both endpoints call core (lines 865, 2038) | closed |
| T-363-02a | Tampering (state integrity) | RejectDeliverableCoreAsync full chain reset | mitigate | HC inclusive reset: `HCApprovalStatus="Pending"` + `HCReviewedById=null` + `HCReviewedAt=null` inside RejectDeliverableCoreAsync (CDPController.cs:1089-1091) | closed |
| T-363-01 | Completeness/control gap | ApproveFromProgress missing HC notif | mitigate | `allApproved` computed in core + `DispatchApproveNotificationsAsync` calls `CreateHCNotificationAsync` (COACH_ALL_COMPLETE) when `allApproved==true` (CDPController.cs:1138-1140); ApproveFromProgress calls dispatch (line 2043) | closed |
| T-363-02 | Tampering (state integrity) | RejectFromProgress + resubmit paths | mitigate | RejectFromProgress delegates to `RejectDeliverableCoreAsync` (CDPController.cs:2098); belt-and-braces HC reset in UploadEvidence wasRejected (lines 1361-1363) and SubmitEvidenceWithCoaching isResubmit (lines 2303-2305) | closed |
| T-363-04i | Information Disclosure | PROTON_PENANDA_MISS notif fan-out | mitigate | Broadcast restricted to `RoleLevel == 2 && IsActive` (ProtonCompletionService.cs:58); dedup by exact message prevents duplicate bells | closed |
| T-363-04r | Repudiation | silent penanda miss | mitigate | `_auditLog.LogAsync("system","system/grading","PROTON_PENANDA_MISS",...)` on no-assignment branch (ProtonCompletionService.cs:53); actor="system" | closed |
| T-363-04f | False alarm | already-exists branch | mitigate | Surface confined to no-assignment branch only; idempotent (already-exists) path at line :48 is silent — not touched (ProtonCompletionService.cs:48) | closed |
| T-363-06 | Tampering (data integrity) | certificate validity asymmetry Regrade | mitigate | `AddYears(3)` + `SetProperty(ValidUntil, validUntil)` removed from `RegradeAfterEditAsync`; only `NomorSertifikat` set (GradingService.cs:516-520); comment T6/D-10 present | closed |
| T-363-03 | Elevation of Privilege | reactivation skipping year-gate | mitigate | `activeForRequestedTrack` uses `IsActive` filter (CoachMappingController.cs:522-533); inactive non-bypass candidates fall through to `IsPrevYearPassedAsync` hard-block (:556); `reactExempt` exempts Origin=Bypass (line 547-550) | closed |
| T-363-03i | Information Disclosure | gate error message technical detail | mitigate | Generic count-based message returned to caller; technical detail logged via `_logger.LogWarning` (CoachMappingController.cs:512) | closed |
| T-363-09 | V7 Logging | non-contiguous Urutan silent | mitigate | `LogWarning` "Urutan tidak kontigu" in CoachMappingController.cs:512 (prevTrack null, Urutan>1) + AssessmentAdminController.cs:1356 (prevTahunKe null, protonUrutan>1) | closed |
| T-363-08 | Repudiation (lost trail) | SubmitEvidenceWithCoaching evidence overwrite | mitigate | `AppendEvidencePathHistory` called BEFORE overwrite in SubmitEvidenceWithCoaching (CDPController.cs:2320) and UploadEvidence (line 1342); public static helper, no File.Delete | closed |
| T-363-05 | Information Disclosure (over-scope) | Belum Mulai rows exposure | mitigate | `BuildBelumMulaiRowsAsync` applies same role-scoping as assignment rows: Level<=3 semua, Level=4 join Section, Level=5 coach-owned (CDPController.cs:3504+) | closed |
| T-363-05d | Drift | export vs view divergence Belum Mulai | mitigate | Single shared `BuildBelumMulaiRowsAsync` helper called from both HistoriProton (CDPController.cs:3302) and ExportHistoriProton (line 3455) | closed |
| T-363-uat | Tampering (test data leak) | UAT seed state | mitigate | Snapshot `C:\Temp\HcPortalDB_Dev_pre363uat_20260611.bak` (1962 pages) before seed; RESTORE WITH REPLACE after UAT; spot-check marker PHASE363-UAT=0, mapping arsyad=0 (docs/SEED_JOURNAL.md line 172, status=cleaned) | closed |
| T-363-AUTHZ | Elevation of Privilege | approve/reject authz | accept | Preserved: `[Authorize(RolesReviewerAndAbove)]` + `HasSectionAccess(level==4)` + section-check stay per-endpoint unchanged (A-2 locked); extraction does not touch authz | closed |
| T-363-A2 | Elevation of Privilege | approval semantics | accept | A-2 unchanged: 1 L4 approver suffices, HC not approver; section-check divergence between endpoints intentional (Pitfall 1 documented in 363-01/02 SUMMARY) | closed |
| T-363-04v | V7 Logging | notif error text content | accept | Operational content only (coachee ref/track); no secrets or PII beyond what COACH_ALL_COMPLETE already broadcasts; mirrors existing notification scope | closed |
| T-363-06e | Edge case | Pass→Fail→Pass ValidUntil null | accept | Per D-10 locked decision; verified live UAT Plan 07 (cert KPB/005/VI/2026 ValidUntil=NULL, ikut setup sesi); escalate as separate phase only if real production issue | closed |
| T-363-03e | Authorized exemption | inactive Origin=Bypass | accept | Exempted by design (D-06 permanent bypass stamp, consistent with Phase 360 D-04); test-covered (ProtonYearGateIntegrationTests bypass-exempt fact) | closed |
| T-363-uat-env | Config | AD bypass during UAT | accept | `Authentication__UseActiveDirectory=false` is documented local-only UAT mode (CLAUDE.md); no Dev/Prod server touched; standard operational pattern | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-363-01 | T-363-AUTHZ | Authz layer (RolesReviewerAndAbove + HasSectionAccess) preserved verbatim from pre-363 baseline; extraction is behavior-preserving. No change in who can approve/reject. | Phase 363 Plan 01 decision (A-2 locked) | 2026-06-11 |
| AR-363-02 | T-363-A2 | Single L4 approver semantics unchanged by design. HC role is reviewer only, not approver. Section-check divergence between Deliverable page and CoachingProton modal is intentional (Pitfall 1 — each endpoint has different navigation context). | Phase 363 Plan 02 decision (Pitfall 1) | 2026-06-11 |
| AR-363-03 | T-363-04v | PROTON_PENANDA_MISS notification text contains coachee reference and track name — same data scope as COACH_ALL_COMPLETE already in production. No credentials, tokens, or stack traces exposed. | Phase 363 Plan 03 (D-14 dedup pattern) | 2026-06-11 |
| AR-363-04 | T-363-06e | Regrade Pass→Fail→Pass: ValidUntil=null when session has no configured expiry date. This is correct behavior (paritas GradeAndCompleteAsync). UAT-verified live. Separate phase to handle if business requires non-null ValidUntil on regrade. | Phase 363 Plan 04 (D-10 locked) | 2026-06-11 |
| AR-363-05 | T-363-03e | inactive Origin="Bypass" assignments are exempt from year-gate by permanent design stamp (Phase 360 D-04). Consistent with bypass exemption for all cross-year enforcement. Integration test ProtonYearGateIntegration bypass-exempt fact covers this path. | Phase 363 Plan 05 (360 D-04 alignment) | 2026-06-11 |
| AR-363-06 | T-363-uat-env | AD=false local-only mode is the documented local UAT pattern in CLAUDE.md. No Dev or Prod server was accessed. Seed journal entry 363 confirms snapshot + restore cycle completed and cleaned. | CLAUDE.md Develop Workflow SOP | 2026-06-11 |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-11 | 21 | 21 | 0 | gsd-security-auditor (claude-sonnet-4-6) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-11
