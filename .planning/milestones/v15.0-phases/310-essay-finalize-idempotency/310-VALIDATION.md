---
phase: 310
slug: essay-finalize-idempotency
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-05-01
---

# Phase 310 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Wave 0 Strategy (Absorbed)

Phase 310 scope tidak warrant separate Wave 0 plan (4 file modify total).
Wave 0 deliverables (Playwright FLOW 9 scaffold + 310-UAT.md draft) di-bundle
ke Plan 02 Task 2 dengan `test.skip` fallback saat fixture absent. Sampling
latency dikompensasi via per-task `<automated>` grep assertions (lihat
Plan 01 verify blocks setelah Fix #4) yang sample behavior literals langsung
di file modified — bukan hanya compile gate.

**Justification:** Bootstrap Wave 0 plan terpisah akan tambah ~10% context
overhead untuk 0 nyata payoff (Playwright scaffold + UAT draft fits dalam
Plan 02 Task 2 budget tanpa context strain). Per-task grep assertions
cover behavior sampling gap yang biasanya filled by Wave 0 test scaffold.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build (compile gate) + Playwright TypeScript E2E (browser UAT) |
| **Config file** | `playwright.config.ts` (existing) + `HcPortal.csproj` (.NET project) |
| **Quick run command** | `dotnet build 2>&1 \| tail -5` (compile + warning baseline ≤ 92) |
| **Full suite command** | `npx playwright test tests/e2e/assessment.spec.ts --grep "Phase 310"` (Playwright spec) |
| **Estimated runtime** | ~10 detik build, ~30 detik Playwright per test |

**Test infrastructure note:** Repo TIDAK punya .NET unit/integration test project (verified via `find . -name "*.Tests.csproj"` returns 0). Per RESEARCH.md finding #3, SC #5 "Task.WhenAll parallel finalize integration test" wajib di-deliver via:
- (a) **Manual UAT concurrent** — buka 2 browser tab admin, klik Finalize bersamaan ke session ID sama, verify state via SQL
- (b) **Playwright sequential test** untuk SC #1 alreadyFinalized branch (klik 2x sequentially via fetch)
- (c) **NOT applicable**: bootstrap .NET test project baru — out of CONTEXT.md scope, defer ke phase tersendiri

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (~10s) — verify compile pass + warning count ≤ 92 (baseline Phase 309)
- **After every plan wave:** Run Playwright spec untuk Phase 310 + manual concurrent UAT
- **Before `/gsd-verify-work`:** Build pass + Playwright Phase 310 specs green + manual concurrent UAT documented
- **Max feedback latency:** ~30 detik (build) atau ~60 detik (Playwright)

---

## Per-Task Verification Map

> Tabel ini placeholder — akan di-isi planner saat PLAN.md generated dengan task IDs konkret. Pattern:

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 310-01-01 | 01 | 1 | ESCG-01 | T-310-01 | API return alreadyFinalized: true saat Status=Completed (no error) | grep | `grep -c 'alreadyFinalized' Controllers/AssessmentAdminController.cs` returns ≥ 1 | ✅ W0-bundled | ⬜ pending |
| 310-01-02 | 01 | 1 | ESCG-01 | T-310-02 | UI button disabled saat Status=Completed | grep | `grep -E 'disabled.*Status.*Completed' Views/Admin/AssessmentMonitoringDetail.cshtml` returns ≥ 1 | ✅ W0-bundled | ⬜ pending |
| 310-01-03 | 01 | 1 | ESCG-01 | T-310-03 | NotifyIfGroupCompleted dedup via UserNotifications.AnyAsync | grep | `grep -F 'AnyAsync(n => n.Type == "ASMT_ALL_COMPLETED"' Services/WorkerDataService.cs` returns ≥ 1 | ✅ W0-bundled | ⬜ pending |
| 310-01-04 | 01 | 1 | ESCG-01 | T-310-04 | Audit log gated rowsAffected > 0 | grep | `grep -F '_auditLog.LogAsync' Controllers/AssessmentAdminController.cs` post line 2790 | ✅ W0-bundled | ⬜ pending |
| 310-02-01 | 02 | 2 | ESCG-01 | T-310-05 | Playwright spec: alreadyFinalized branch UI test | playwright | `npx playwright test --grep "Phase 310 alreadyFinalized"` exit 0 | ✅ W0-bundled | ⬜ pending |
| 310-02-02 | 02 | 2 | ESCG-01 | T-310-06 | Manual concurrent UAT documented | manual | UAT.md filled with concurrent test result + SQL verify queries | ✅ W0-bundled | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Note:** Final task IDs + automated commands akan di-finalize planner. Tabel di atas placeholder.

---

## Wave 0 Requirements

- [ ] `tests/e2e/assessment.spec.ts` — tambah Phase 310 describe block dengan placeholder test (`test.skip('Phase 310: alreadyFinalized branch')`) sebagai stub untuk Wave 1 fill
- [ ] `.planning/phases/310-essay-finalize-idempotency/310-UAT.md` — draft manual concurrent test scenario (dual-tab + SQL queries) sebagai stub untuk Wave 2 fill
- [ ] **Tidak perlu** install framework — Playwright + dotnet build sudah ada

*Wave 0 minimal — infrastructure existing covers Phase 310.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Task.WhenAll parallel finalize tidak corrupt state (SC #5) | ESCG-01 | Repo tidak punya .NET integration test project; concurrent test wajib via dual-browser-tab + SQL query post-test | Login Admin, buka 2 tab `/Admin/AssessmentMonitoringDetail/{id}` dengan session Status=PendingGrading, klik Finalize bersamaan di kedua tab → SQL `SELECT COUNT(*) FROM TrainingRecords WHERE UserId=? AND Judul=? GROUP BY ...` returns 1; `SELECT NomorSertifikat FROM AssessmentSessions WHERE Id=?` distinct value; `SELECT COUNT(*) FROM AuditLogs WHERE Action='FinalizeEssayGrading' AND Detail LIKE '%{id}%'` returns 1; `SELECT COUNT(*) FROM UserNotifications WHERE Type='ASMT_ALL_COMPLETED' AND Message LIKE '%{title}%'` distinct per recipient |
| Visual UI behavior — disabled button + tooltip styling (D-02) | ESCG-01 | Visual / a11y verification (Bootstrap disabled state, tooltip hover, dimming) butuh manual visual inspection | DevTools inspect button: `disabled` attribute present, class includes `disabled`, hover trigger `data-bs-toggle="tooltip"` show "Sudah selesai pada [tanggal] WIB" |
| Toast info biru rendering pada alreadyFinalized response (D-03) | ESCG-01 | Visual color contrast + animation timing | Klik Finalize pada Completed session → toast biru muda (alert-info bg `rgb(207,244,252)`) muncul dengan teks "Penilaian sudah diselesaikan sebelumnya pada [tanggal]"; auto-dismiss 5 detik atau closeable via × |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify (grep) atau Wave 0 dependencies (UAT.md draft)
- [ ] Sampling continuity: build pass setelah setiap task; Playwright pass setelah Wave 1
- [ ] Wave 0 covers all MISSING references (Playwright stub + UAT draft)
- [ ] No watch-mode flags (semua command exit-on-complete)
- [ ] Feedback latency < 30 detik (dotnet build) atau < 60 detik (Playwright)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
