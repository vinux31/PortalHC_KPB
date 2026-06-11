---
phase: 363
slug: audit-fix-alur-proton-temuan-verifikasi-t1-t10
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-11
updated: 2026-06-11
---

# Phase 363 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8.0) — verified csproj |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (isolasi via `[Trait("Category","Integration")]`) |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` + `dotnet build` |
| **Full suite command** | `dotnet test` (butuh `localhost\SQLEXPRESS`) |
| **Estimated runtime** | quick ~15s (180 test); full ~60s (228 test) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + targeted `dotnet test --filter` (dijalankan aktual per task, lihat SUMMARY 01-07)
- **After every plan wave:** suite targeted per plan; full `dotnet test` di gate Plan 07
- **Before `/gsd-verify-work`:** Full suite 228/228 hijau + UAT Playwright @5277 (Plan 07 — human approved)
- **Max feedback latency:** ~60 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-T2 | 01 | 1 | T1/T2/T7 pin (Wave 0) | T-363-07/02a | pin end-state gold-standard pre-rewire | integration | `dotnet test --filter ProtonApproveRejectParity` (4 fact) | ✅ | ✅ green |
| 02-T1 | 02 | 2 | T1 allApproved flag | T-363-01 | approve terakhir → allApproved==true (trigger notif HC) | integration | `ApproveCore_LastDeliverable_ReturnsAllApprovedTrue` / `NotLast` | ✅ | ✅ green |
| 02-T2/T3 | 02 | 2 | T2 chain reset | T-363-02 | SrSpv/SH/HC → Pending + null; resubmit HC reset | integration | `RejectCore_ResetsFullChain_IncludingHC` + `Reject_ThenResubmit_HCStatusBackToPending` + `FromProgress_And_Deliverable_RejectCore_ProduceIdenticalEndState` | ✅ | ✅ green |
| 01-T2 | 01 | 1 | T7 race-guard | T-363-07 | stale second approve ditolak | integration | `ApproveCore_RaceGuard_RejectsStaleSecondApprove` | ✅ | ✅ green |
| 05-T3 | 05 | 2 | T3 gate reaktivasi | T-363-03/03e | inactive Tahun N tanpa N-1 → blocked; Bypass inactive → exempt; active → skip | integration | `dotnet test --filter ProtonYearGateIntegration` (3 fact `Reactivation_*`) | ✅ | ✅ green |
| 03-T2 | 03 | 1 | T4 surface miss | T-363-04i/r/f | miss → AuditLog + notif HC-only; idempotent silent | integration | `dotnet test --filter ProtonCompletionMiss` (2 fact) | ✅ | ✅ green |
| 06-T3 | 06 | 3 | T5 Belum Mulai | T-363-05 | mapping aktif tanpa assignment → set Belum Mulai | integration | `BelumMulai_SetComputation` | ✅ | ✅ green |
| 06-T3 | 06 | 3 | T8 history append | T-363-08 | path lama masuk EvidencePathHistory; no-op kosong | unit | `AppendEvidencePathHistory_AppendsOldPath` + `_NoOp_WhenEmptyPath` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

Full suite pasca-fase: **228/228 PASS** (214 regresi + 14 baru) — dijalankan Plan 07 Task 1, 2026-06-11.

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/ProtonApproveRejectParityTests.cs` — pin gold-standard PASS SEBELUM rewire Plan 02 (commit `3b9f2e9f`, 4/4 green pada kode Plan 01)
- [x] Extend `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` — 3 fact reaktivasi (commit `a79b2a73`)
- [x] `HcPortal.Tests/ProtonCompletionMissTests.cs` — surface + idempotent (commit `bac1305d`)
- [x] Test T5 + T8 di `HcPortal.Tests/ProtonHistoriAndEvidenceTests.cs` (commit `e1a10e94`)
- [x] Framework install: TIDAK perlu — xUnit + real-SQL fixture reuse

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions | Result |
|----------|-------------|------------|-------------------|--------|
| T9 log warning Urutan tidak kontigu | T9 | assert log sulit; nilai rendah utk ILogger mock | code review + grep "Urutan tidak kontigu" 2 file | ✅ verified (review + grep 2 lokasi) |
| T10 komentar by-design | T10 | dokumentasi, bukan behavior | review komentar T10/D-13 di BackfillProtonPenanda | ✅ verified (grep :3971) |
| T6 regrade ValidUntil tidak hardcode | T6 | integration test butuh konstruksi full GradingService graph + seed exam (session/questions/responses/ET) — cross-plan coupling berat utk deletion 2 baris; keputusan diskresioner terdokumentasi di 363-04-PLAN.md §verification | UAT live: EditPesertaAnswers SES 1 Fail→Pass → cert terbit, `ValidUntil` ikut sesi (NULL), bukan +3thn. Backstop: grep AC 5/5 + 180 unit no-regression | ✅ UAT live 2026-06-11 (cert KPB/005/VI/2026, ValidUntil=NULL) |
| T1 notif HC dispatch end-to-end | T1 | `DispatchApproveNotificationsAsync` butuh UserManager — tidak constructible di fixture; allApproved (trigger) sudah automated | UAT live: approve terakhir via modal → `COACH_ALL_COMPLETE` di UserNotifications utk user HC | ✅ UAT live 2026-06-11 (notif id 167 → meylisa) |
| UAT alur lengkap @5277 | T1/T2/T3/T5/T6 | bukti end-to-end UI | Playwright pre-drive + human sign-off (363-07-SUMMARY.md) | ✅ approved 2026-06-11 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-11 (audit retroaktif /gsd-validate-phase)

---

## Validation Audit 2026-06-11

| Metric | Count |
|--------|-------|
| Requirements dipetakan | 10 (T1-T10) |
| Covered automated | 7 (T1-flag/T2/T3/T4/T5/T7/T8 — 14 test baru, suite 228/228) |
| Gaps found | 2 (T6 automated, T1 notif-dispatch) |
| Resolved | 0 (tidak ada test baru dibuat) |
| Manual-only (documented + UAT-verified) | 5 entri (T6, T1-notif, T9, T10, UAT e2e) |
| Escalated | 0 |

Keputusan user: gap T6 + T1-notif → manual-only (UAT live human-approved; keputusan plan-04 terdokumentasi). Tidak ada file test baru.
