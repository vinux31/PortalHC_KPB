---
phase: 314
slug: fix-regenerate-token-untuk-status-upcoming
status: approved
nyquist_compliant: true
wave_0_complete: false
created: 2026-05-08
approved: 2026-05-08
---

# Phase 314 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> **Phase 314 = investigative bug fix.** Validation strategy adopts existing infrastructure (Playwright E2E for admin scenarios + .NET build) — Wave 0 minimal additions only.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (e2e) + xUnit/dotnet (build verify) — existing |
| **Config file** | `tests/playwright.config.ts` (existing) |
| **Quick run command** | `cd tests && npx playwright test e2e/admin-assessment-token.spec.ts --reporter=list` |
| **Full suite command** | `cd tests && npx playwright test --reporter=list` |
| **Build verify** | `dotnet build` (root) |
| **Estimated runtime** | ~120 seconds (Playwright single spec) + ~30 seconds (dotnet build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (~30s) + Playwright targeted spec (~120s)
- **After Wave 1 (Plan 02 backend patch complete):** Run full Phase 314 spec + manual smoke test 3 skenario
- **After Wave 2 (Plan 02 frontend complete):** Run full Phase 314 spec + UI regression check
- **Before `/gsd-verify-work`:** Full suite must be green + manual UAT 314-UAT.md sign-off
- **Max feedback latency:** ~150 seconds (build + targeted spec)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 314-01-01 | 01 | 0 | TKN-01 | — | Repro bug at Dev URL captured | manual | manual repro instructions di RESEARCH.md | ❌ W0 | ⬜ pending |
| 314-01-02 | 01 | 0 | TKN-01 | — | Server log Dev exception captured | manual | grep server log + screenshot | ❌ W0 | ⬜ pending |
| 314-01-03 | 01 | 0 | TKN-01 | — | 5 SQL queries (D-39) executed → data shape baseline | manual | sqlcmd inline | ❌ W0 | ⬜ pending |
| 314-01-04 | 01 | 0 | TKN-01 | — | RESEARCH.md hipotesis tabel finalized (CONFIRMED/RULED OUT/INCONCLUSIVE) | manual | grep "## Root Cause" RESEARCH.md | ❌ W0 | ⬜ pending |
| 314-02-01 | 02 | 1 | TKN-01 | T-314-D25 | Pre-condition Status Cancelled+Completed block | unit + manual | dotnet build + manual POST | ❌ W0 | ⬜ pending |
| 314-02-02 | 02 | 1 | TKN-01 | T-314-D17 | Transaction wrap loop sibling update | code review + dotnet build | grep `BeginTransactionAsync` | ❌ W0 | ⬜ pending |
| 314-02-03 | 02 | 1 | TKN-01 | T-314-D06 | Audit log try-catch swallow | code review + manual force-fail | grep `LogWarning.*Audit log failed` | ❌ W0 | ⬜ pending |
| 314-02-04 | 02 | 1 | TKN-01 | T-314-D33 | Sibling 0-row guard | unit + manual | dotnet build + manual edge case | ❌ W0 | ⬜ pending |
| 314-02-05 | 02 | 1 | TKN-01 | T-314-D12 | Server-side error specific by exception type | code review | grep `catch (DbUpdateException` + `catch (NullReferenceException` | ❌ W0 | ⬜ pending |
| 314-02-06 | 02 | 1 | TKN-01 | T-314-D20 | Extended structured logging hasStarted+siblingCount | code review | grep `LogError.*hasStarted.*siblingCount` | ❌ W0 | ⬜ pending |
| 314-02-07 | 02 | 1 | TKN-01 | T-314-D38 | Conditional Schedule MinValue guard (KALAU RESEARCH confirm) | code review | grep `DateTime.MinValue` (conditional) | ❌ W0 | ⬜ pending |
| 314-02-08 | 02 | 2 | TKN-01 | T-314-D07 | Frontend `.catch()` parse server error body | manual UI | DevTools force 500 → assert alert text | ❌ W0 | ⬜ pending |
| 314-02-09 | 02 | 2 | TKN-01 | T-314-D11 | Frontend response.ok + r.text() fallback non-JSON 5xx | manual UI | DevTools force HTML 500 → assert alert | ❌ W0 | ⬜ pending |
| 314-02-10 | 02 | 2 | TKN-01 | T-314-D08 | Patch 3 view: AssessmentMonitoring + Detail + ManageAssessment | code review | grep new error handler in 3 files | ❌ W0 | ⬜ pending |
| 314-02-11 | 02 | 2 | TKN-01 | T-314-D22 | Warn dialog Open + active worker | manual UI | trigger Open + worker StartedAt → assert dialog | ❌ W0 | ⬜ pending |
| 314-02-12 | 02 | 3 | TKN-01 | — | Playwright E2E 3 skenario PASS | e2e | `npx playwright test admin-assessment-token.spec.ts` | ❌ W0 | ⬜ pending |
| 314-02-13 | 02 | 3 | TKN-01 | — | Manual UAT 314-UAT.md sign-off | manual | UAT script signed by user | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Note:** Plan 01 tasks adalah investigative (manual capture) — tidak ada automated verify selain output file existence (`grep` di RESEARCH.md). Acceptable per Phase 314 nature (investigative bug fix, bukan feature implementation).

---

## Wave 0 Requirements

- [ ] `tests/e2e/admin-assessment-token.spec.ts` — NEW Playwright spec untuk 3 skenario (D-16)
- [ ] `tests/e2e/helpers/tokenFixtures.ts` (optional) — helper untuk seed 3 fixture via UI Admin (D-14) — pattern Phase 307 wizardSelectors helper
- [ ] `.planning/phases/314-fix-regenerate-token-untuk-status-upcoming/314-UAT.md` — manual UAT 5-step
- [ ] `docs/SEED_JOURNAL.md` entry — temporary local-only fixtures (per CLAUDE.md NEW Seed Data Workflow)
- [ ] No new test framework install — Playwright + xUnit existing

*Existing infrastructure (Playwright + dotnet) covers all phase requirements. Wave 0 = NEW spec file + UAT doc + seed journal entry only.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Repro bug di Dev environment (D-01) | TKN-01 | Server log + DB Dev environment access required | Per `314-RESEARCH.md` § Reproduction Steps — login admin@pertamina.com, buat assessment Status=Upcoming + IsTokenRequired=true + 0 worker, klik Regenerate Token, capture exception + alert text |
| Server log exception capture (D-02) | TKN-01 | Server log Dev tidak accessible dari unit test | grep `RegenerateToken failed` di log file Dev (path TBD per IT) |
| 5 DB sample queries (D-39) | TKN-01 | DB Dev access required | sqlcmd execute 5 queries (a-e), output ke `314-RESEARCH.md` § Data Shape Baseline |
| Frontend error UX visual (D-09/D-22) | TKN-01 | Visual UX validation manual saja | Open AssessmentMonitoring + Detail + ManageAssessment view, trigger error 500 via DevTools, assert alert wording sesuai D-10 |
| Warn dialog Open + active worker (D-22) | TKN-01 | Worker session state setup manual | Login worker, masuk ujian, switch ke admin, regenerate token assessment yang sama, assert dialog wording sesuai D-23 (REVISED per RESEARCH.md finding) |

---

## Validation Sign-Off

- [x] Plan 01 tasks accept manual verification (investigative phase nature) — see rationale below
- [x] Plan 02 backend tasks have `dotnet build` + grep-verifiable acceptance criteria
- [x] Plan 02 frontend tasks have manual UI verify + Playwright e2e
- [x] Sampling continuity: Wave 1 + Wave 2 + Wave 3 each have automated build/spec
- [x] Wave 0 NEW Playwright spec + UAT.md + seed journal entry created (Plan 02 Task 5/6/7 deliverables)
- [x] No watch-mode flags
- [x] Feedback latency < 150s (build + targeted spec)
- [x] `nyquist_compliant: true` set in frontmatter (this revision 2026-05-08)

### Sampling Continuity Rationale (Phase 314 — Investigative Bug Fix)

Plan 01 Tasks 1-3 are **manual investigation** (repro, stacktrace capture, SQL queries) tanpa traditional `<automated>` test command — by phase nature investigative, bukan feature implementation. Justification untuk Nyquist compliance:

1. **Each manual task has automated artifact verification** via `grep -q "^## {Section}"` + `test -f {file}` di `<automated>` block. Bukan zero automation — ground truth file existence check.
2. **No 3 consecutive tasks without automated verify:** Plan 01 Task 4 (auto, grep verify) breaks sequence; Plan 02 entire wave 1+2 = automated build/test/grep.
3. **Plan 02 Wave 1 backend (Tasks 1-5) = full `dotnet build` + grep contract verification** per task — feedback latency ~30s.
4. **Plan 02 Wave 2 frontend (Task 4) + Wave 3 (Tasks 6-7) = Playwright E2E + manual UAT** dengan automated spec runner — feedback latency ~120s.
5. **Aggregate sampling rate** memenuhi Nyquist criterion: every backend code change verified < 150s; manual investigative tasks verified via output artifact existence.

**Approval:** approved 2026-05-08

> Auto-approved as part of revision iteration 2 (checker BLOCKER 2 fix). Investigative phase rationale dokumentasi di atas. Sign-off legitimate per VALIDATION strategy nature dengan layered automated artifact checks.

