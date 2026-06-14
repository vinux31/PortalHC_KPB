---
phase: 364-restore-baseline-regresi-e2e-exam
plan: 03
type: execute
status: complete
files_modified:
  - tests/e2e/exam-taking.spec.ts
  - tests/e2e/exam-types.spec.ts
  - docs/SEED_JOURNAL.md
---

# Plan 364-03 SUMMARY — Drift triage, final gate, backlog findings

Triaged every remaining NON-TITLE failure after Plan 02 into fix-in-test (D-05) or
`test.fixme` + backlog (D-06), ran the final gate (D-15), and recorded the honest outcome.
**Zero production code / migration / helper changes; `utils.ts` untouched; FLOW P/T untouched.**

## Gate outcome (D-15) — HONEST (D-09)

| Gate half | Command | Result |
|-----------|---------|--------|
| e2e (D-15.1) | `npx playwright test exam-taking.spec.ts exam-types.spec.ts --workers=1` | **78 passed, 77 skipped, 0 failed** |
| unit (D-15.2) | `dotnet test HcPortal.Tests` | **227 / 227 passed, 0 failed** |

**Classification (D-08/D-09):** NOT "PASS penuh @5277" — the run contains `test.fixme` skips, so it
is the **SC#4 alternative: "failure terdokumentasi bukan-karena-judul."** The `dotnet test` half is
fully green. No skip is counted as a pass.

App run AD-off with the `lpc:` shared-memory connection-string env override (Plan 01 env fix). No
file edited (appsettings untouched; AD on at HEAD, handoff-ready).

## Per-flow final status

**exam-types — RESTORED (the restorable spec):**
- ✅ **78 passed**: W0.1/W0.2/W0.T0, FLOW K (incl. **D-11 `LinkedGroupId IS NULL` ✓**), L1–L5, FLOW M, N, O (SignalR extra-time), **P (PrePostTest exempt ✓ — SC#2 evidence)**, Q (EWCD reject), R (cert PDF + NomorSertifikat), S (review true/false), **T (Manual CRUD ✓ — exempt)**, U, V, W (analytics), X, Y. K5/M5 essay-MA used the existing SURF-317-A DB workaround and pass single-worker.
- 🟡 **L6 `test.fixme` → backlog 999.8**: essay-only finalize leaves `AssessmentSessions.Score = 0` despite a confirmed per-question grade (80, badge "Sudah Dinilai") + finalize in L5. Suspected essay-aggregation regression. **Not fixed here** (production untouched, D-06).

**exam-taking — DEFERRED (structural wizard drift):**
- 🟡 **Flows A–J (all 10) `test.fixme` → backlog 999.7**: `CreateAssessment` is now a **4-step wizard** (`1.Kategori`→`2.Peserta[disabled]`→`3.Settings`→`4.Konfirmasi`); the specs' flat-form create approach (direct worker-select on step 2 + `#Title` + `#submitBtn`) is obsolete — worker checkbox is `display:none` in the disabled step. Needs a wizard-navigation migration of all 10 flows. Confirmed via the A1 page snapshot. Flow E (Proton T3) carries an additional Proton-v25.0 interview-drift risk to re-check during migration.
- ⚪ **Phase 313 block (313.1–313.7) skipped by design** — self-skips when `.planning/seeds/313-timer-fixtures.sql` is not seeded (own REQ TMR-01, separate from the matrix seed; out of this phase's scope). Not a fixme cascade.

## Deviations / findings

1. **ENV (Plan 01)** — app login 500'd: `SqlException error 53` (Named Pipes) because `SQLBrowser` was Stopped + Integrated-auth over TCP loopback fails NTLM ("untrusted domain"). Fix = `lpc:` shared-memory connection-string env override (session-only). **Recommend IT/dev: set `SQLBrowser` StartType=Automatic** (or document the lpc override) for local e2e.
2. **Backlog 999.7** — exam-taking e2e: migrate 10 create flows to the 4-step `CreateAssessment` wizard (reuse `createAssessmentViaWizard`-style nav). Re-check Flow E Proton T3 interview form against v25.0.
3. **Backlog 999.8** — production: essay-only finalize does not aggregate the manual essay score into `AssessmentSessions.Score` (reads 0 after grade+finalize; MA auto-grade writes the same column fine). Diagnose `GradingService`/finalize essay path (v25.0 358 hook suspected).
4. **Test-harness finding** — `tests/playwright.config.ts` sets **no `workers`** value → multi-file runs default to 2+ workers, which **breaks the single-DB BACKUP/seed/RESTORE isolation** (combined run mis-scored M5 = 70 vs 100). The gate MUST run `--workers=1`. **Recommend pinning `workers: 1` in playwright.config.ts** for this shared-DB harness.
5. **Plan count correction (Plan 02)** — plan said 21 standard titles (exam-types 11); actual = **20** (10 + 10). Applied the real 20.
6. **A1 `.check`→`.click` hoist** tried in Plan 02 then **reverted** — insufficient (wizard drift, not a method issue). A1 is verbatim again and fixme'd with its flow.

## D-16 baseline-available note

Baseline e2e exam **tersedia (available) — on-demand**: `exam-types.spec.ts` is a working regression
baseline (run with `--workers=1`); `exam-taking.spec.ts` is title-compliant but its create flows are
fixme-deferred to the wizard migration (999.7). Use these specs when a phase touches the exam area —
**BUKAN gate wajib tiap fase.** Always launch the app with the `lpc:` conn override until SQLBrowser
is fixed locally.

## Confirmations

- **Zero production**: `git status --porcelain Controllers/ Services/ Views/ Migrations/ Data/` EMPTY.
- `git diff tests/helpers/utils.ts` EMPTY (D-03).
- FLOW P (`[318-P] PrePost Exam`) + FLOW T (`[319-T] Manual CRUD`) titles unchanged (exempt).
- Every `test.fixme(...)` carries a non-empty quoted reason ending in a backlog ref (999.7 / 999.8).
- DB restored clean each run — `Layer 4 OK: 0 matrix rows`, `SEED_JOURNAL.md updated → cleaned`.
- No separate FINDINGS file (D-07) — only this SUMMARY + the SEED_JOURNAL Phase 364 entry.
