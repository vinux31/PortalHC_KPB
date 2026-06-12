---
phase: 364-restore-baseline-regresi-e2e-exam
plan: 01
type: execute
status: complete
files_modified: []
---

# Plan 364-01 SUMMARY — Baseline Diagnostic (D-10)

Ran both target specs **as-is** (zero edits) @localhost:5277 (AD off). Captured per-flow
results and classified TITLE vs NON-TITLE. Zero source files modified; DB restored clean by
the automatic Playwright lifecycle (BACKUP → seed → RESTORE, journal `cleaned`).

## Baseline Diagnosa (D-10)

### Environment blocker resolved BEFORE diagnosis (infra, not spec)

The first as-is run failed at `auth.ts:9` (HC login) with a **server 500** — `SqlException ... (provider: Named Pipes Provider, error: 40) ---> Win32Exception (53): The network path was not found. Error Number:53` at `UserManager.FindByEmailAsync` → `LocalAuthService.AuthenticateAsync:32` → `AccountController.Login:63`. SQL Server itself was up (sqlcmd via shared-memory worked); the **app** could not Integrated-auth to `localhost\SQLEXPRESS`:

- `SQLBrowser` service was **Stopped** (StartType Manual — stayed down after a reboot). Started it (`Start-Service SQLBrowser`). Did **not** fix login alone.
- Forcing TCP (`tcp:127.0.0.1,51685`) → `Login failed. The login is from an untrusted domain and cannot be used with Integrated authentication` (NTLM loopback). Named pipes → error 53.
- **Working path = shared memory** (same as sqlcmd). Fix: launch app with connection-string env override `ConnectionStrings__DefaultConnection='Server=lpc:localhost\SQLEXPRESS;Database=HcPortalDB_Dev;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30'` (the `lpc:` prefix forces shared memory).

**Non-invasive, session-only.** No file edited (`appsettings*.json` untouched, AD stays on at HEAD, handoff-ready). This mirrors the `Authentication__UseActiveDirectory=false` env-override pattern. After the override, HC login = 302 redirect (success). **Plan 02 / 03 MUST launch the app with this same `lpc:` override** until the local SQL Browser / NTLM-loopback env is repaired (local-machine quirk; not a code bug).

### Serial-cascade limitation (read this before trusting the table)

Both specs are file-wide `test.describe.configure({mode:'serial'})` → the **first** failing test
skips ALL remaining tests in the file. So this baseline can only **empirically** observe the
**first** create per file. The rest are **MASKED** (`did not run`). Their category below is the
**static prediction** from 364-RESEARCH §Flow Inventory (no `Pre Test` prefix → REST-06 reject),
to be confirmed by Plan 02/03 re-runs as upstream blockers clear.

### Per-flow classification

| Flow | Spec | Line | Result (this run) | Failing step | Category | Note / drift hypothesis |
|------|------|------|-------------------|--------------|----------|--------------------------|
| A Legacy | exam-taking | :35 | **FAIL (empirical)** | worker `.check({force})` `input.user-checkbox` | **NON-TITLE** | Line 40 uses `.check({force})` on a custom-hidden checkbox → "Element is not visible". The 9 sibling creates use `.click({force})` (works on hidden input). Fix-in-test D-05 (`.check`→`.click`). Masks rest of file. |
| B Token | exam-taking | :316 | MASKED (serial) | — | TITLE (predicted) | `uniqueTitle('Token Exam')` no prefix → REST-06 reject expected at submit. Uses `.click({force})` worker-select. |
| C ForceClose | exam-taking | :402 | MASKED | — | TITLE (predicted) | no prefix. |
| D Package | exam-taking | :548 | MASKED | — | TITLE (predicted) | no prefix. |
| **E Proton T3** | exam-taking | :702 | MASKED | — | TITLE@create (predicted) / **NON-TITLE@interview (HIGH drift risk)** | **Strongest `test.fixme` candidate.** Proton v25.0 overhaul (Phase 358-363) likely changed Tahun-3 interview form / track selectors. Confirm in Plan 02/03. |
| F Multi Worker | exam-taking | :833 | MASKED | — | TITLE (predicted) | no prefix. |
| G Timer Expired | exam-taking | :963 | MASKED | — | TITLE (predicted) | no prefix. |
| H RealTime Mon | exam-taking | :1057 | MASKED | — | TITLE (predicted) | no prefix; post-create H4/H7 monitoring MED drift risk. |
| I EditTest | exam-taking | :1291 | MASKED | — | TITLE (predicted) | no prefix. |
| J Abandon | exam-taking | :1404 | MASKED | — | TITLE (predicted) | no prefix. |
| Phase 313 block | exam-taking | :1608+ | MASKED | — | NON-VALIDATOR | fixture SQL via clickResumeForFixture, not created via UI → not validator-gated. |
| **W0 SMOKE** | exam-types | :37 | **FAIL (empirical)** | `#successModal.show` 15s timeout (`examTypes.ts:103`) | **TITLE** | Wizard reached submit; validator rejected `[317-SMOKE-W0] Order Verify` (no prefix). **Confirms phase premise.** Masks rest of file. |
| K MA | exam-types | :191 | MASKED | — | TITLE (predicted) | no prefix; also K5/M Results MA = known SURF-317-A prod bug (Pitfall 5, do-not-fix). D-11 LinkedGroupId assertion lands here (Plan 02). |
| L Essay | exam-types | :310 | MASKED | — | TITLE (predicted) | no prefix. |
| M Mixed | exam-types | :435 | MASKED | — | TITLE (predicted) | no prefix; Results MA SURF-317-A. |
| N NoReview | exam-types | :588 | MASKED | — | TITLE (predicted) | no prefix. |
| O ExtraTime | exam-types | :690 | MASKED | — | TITLE (predicted) | no prefix; post-create SignalR hub-state MED drift risk. |
| **P PrePost** | exam-types | :860 | MASKED | — | **EXEMPT-OK** | PrePostTest → REST-06 exempt. Predicted PASS unless non-title drift. Do NOT prefix (Pitfall 2). |
| Q EWCD Past | exam-types | :1065 | MASKED | — | TITLE (predicted) | no prefix; post-create window-close LOW drift. |
| R Cert | exam-types | :1151 | MASKED | — | TITLE (predicted) | no prefix; post-create cert PDF LOW drift. |
| S-TRUE Review | exam-types | :1278 | MASKED | — | TITLE (predicted) | no prefix (titleTrue). |
| S-FALSE NoReview | exam-types | :1364 | MASKED | — | TITLE (predicted) | no prefix (titleFalse). |
| **T Manual** | exam-types | :1457 | MASKED | — | **EXEMPT-OK** | AddManualAssessment controller, no REST-06. Predicted PASS. Do NOT prefix (Pitfall 2). |

## Rollup

- **TITLE failures:** 1 empirical (exam-types W0, `#successModal` timeout = validator reject) + ~18 predicted-masked (all non-exempt standard creates carry no `Pre Test ` prefix). Plan 02 (title prefix) addresses all of these.
- **NON-TITLE failures:** 1 empirical (exam-taking A1 — `.check({force})` on hidden worker checkbox; line 40 inconsistent with 9 siblings using `.click({force})`). This is a **fix-in-test D-05** (Plan 03), and it is a **prerequisite to observing** exam-taking title fixes (serial cascade).
- **EXEMPT-OK:** P (:860 PrePost) + T (:1457 Manual) — predicted PASS, do not edit.
- **`test.fixme` candidates for Plan 03:** FLOW E Proton T3 (:702) — HIGHEST risk, Proton v25.0 overhaul drift on the interview/Tahun-3 step. SURF-317-A (exam-types K5/M Results MA 500) is a known prod bug → record-only, do-not-fix.

## Ordering insight for Plan 02/03 (deviation note)

The plans assume "title is THE first blocker." Reality from the baseline:
- **exam-types** matches that assumption cleanly (W0 fails on title; modern wizard helper).
- **exam-taking** has an **upstream NON-TITLE blocker at A1** (`.check` vs `.click` on the hidden worker checkbox) that fires BEFORE the title validator. Because of serial cascade, exam-taking title fixes **cannot be validated** until that A1 worker-select drift is fixed. Recommend Plan 03's A1 `.check`→`.click` fix be applied (or hoisted) so Plan 02's exam-taking title prefixes become observable. There may be additional flat-form-vs-4-step-wizard structural drift in exam-taking to confirm during the Plan 02 re-run.

## Confirmations

- Run modified **zero source files** — `git status --porcelain tests/ Controllers/ Services/ Views/` clean (only gitignored `test-results/` artifacts).
- DB **restored clean** — both runs: `[teardown] RESTORE OK` + `Layer 4 OK: 0 matrix rows` + `SEED_JOURNAL.md updated → cleaned`. Journal tail = `cleaned`.
- Matrix collision ids 9001-9018 free (setup `Layer 1 OK: sessions=18 ...` each run, no collision abort).
