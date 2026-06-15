---
phase: 364-restore-baseline-regresi-e2e-exam
plan: 02
type: execute
status: complete
files_modified:
  - tests/e2e/exam-taking.spec.ts
  - tests/e2e/exam-types.spec.ts
---

# Plan 364-02 SUMMARY — Title prefix `Pre Test ` + D-11 LinkedGroupId assertion

Prefixed every standard-create title with `Pre Test ` (REST-06 comply, SC#1) and added the
D-11 `LinkedGroupId IS NULL` DB assertion in exam-types FLOW K (SC#3). `tests/helpers/utils.ts`
untouched (D-03). FLOW P (:860) and FLOW T (:1457) untouched (Pitfall 2). App run with the
`lpc:` shared-memory connection-string env override from Plan 01 (no file edited).

## Edits applied

- **exam-taking.spec.ts** — 10 title prefixes: Legacy, Token, ForceClose, Package, Proton T3 Interview, Multi Worker, Timer Expired, RealTime Mon, EditTest, Abandon. (git diff = ONLY the 10 `uniqueTitle(...)` lines changed.)
- **exam-types.spec.ts** — 10 title prefixes: [317-SMOKE-W0], [317-K] MA, [317-L] Essay, [317-M] Mixed, [317-N] NoReview, [317-O] ExtraTime, [318-Q] EWCD, [318-R] Cert, [318-S-TRUE], [318-S-FALSE]. + D-11 assertion (6 lines) in FLOW K K1.
- Casing verified `Pre Test ` (capital P/T) — 0 lowercase/uppercase variants.
- `npx tsc --noEmit` — zero new errors in exam-taking/exam-types (the only tsc errors are pre-existing, in `manage-org-label.spec.ts` + `proton-bypass.spec.ts`).

## Deviation 1 — plan title count was an off-by-one miscount (21→20, exam-types 11→10)

The plan objective/verify expected **21** total prefixes (exam-types **11**). Grep of the actual
specs shows **20** standard-create call sites: exam-taking **10** + exam-types **10** (P + T are
exempt, not prefixed). There is no 11th exam-types standard-create site. Applied the real **10 + 10
= 20**. The plan's `m!==11` automated check is therefore wrong; the correct count is 10 per file.

## Deviation 2 — A1 `.check`→`.click` hoist tried, then REVERTED (exam-taking is wizard-drifted, not a method issue)

Per the Plan-01 checkpoint we hoisted a one-line A1 `.check({force})`→`.click({force})` fix to
unmask exam-taking title validation. The re-run proved it **insufficient**: A1 still failed
`locator.click: Element is not visible`. The page snapshot revealed **CreateAssessment is now a
4-step wizard** (`1.Kategori` active → `2.Peserta [disabled]` → `3.Settings` → `4.Konfirmasi`).
The worker checkbox lives in the disabled step 2 (`display:none`), so neither `.check` nor `.click`
can reach it from the flat-form approach. The hoist was **reverted to verbatim `.check({force})`**.
Decision (user): exam-taking's 10 flat-form create flows need a **wizard-navigation migration** →
handled in Plan 03 as `test.fixme` + **backlog 999.7** (structural drift, not a title problem).

## Re-run results @5277 (post-edit)

**exam-types — title-create failures RESOLVED (SC#1 ✓, D-11 ✓):**
| Test | Result | Note |
|------|--------|------|
| W0.1 create + 3 MC | ✓ PASS | was the TITLE failure (validator reject) — now creates |
| W0.2 / W0.T0 | ✓ PASS | order + TomSelect smoke |
| FLOW K K1–K5 | ✓ PASS | **K1 includes D-11 `LinkedGroupId IS NULL` assertion → PASS (linkedNull===1)** |
| FLOW L L1–L5 | ✓ PASS | create, package, essay Q, worker submit (PendingGrading), HC grade 80 |
| FLOW L **L6** | ✗ FAIL | NON-TITLE: essay DB score `Expected 80, Received 0` (HC graded 80 in L5, session reads 0). Post-create grading drift → Plan 03. Serial → 65 masked. |

→ **14 passed**, every exam-types **create step now passes** (no validator reject). D-11 green.
The serial cascade now surfaces the next drift (L6 essay grading) for Plan 03 triage.

**exam-taking — title prefixes applied, but blocked upstream by wizard structural drift:**
- A1 fails at worker-selection (step 2 disabled in the wizard). All 10 create flows share this
  blocker. Title prefixes are correct but **unobservable** until the wizard migration (Plan 03 → fixme + 999.7).

## Drift list handed to Plan 03

1. **exam-taking (all 10 create flows)** — CreateAssessment 4-step wizard migration. `test.fixme` + backlog **999.7** (structural, not title). Confirmed via A1 page snapshot.
2. **exam-types L6 (:412)** — essay finalize score reads 0 not 80 (DB verify, post SURF-317-A workaround). Triage: real essay-grading bug (→ fixme + backlog) vs test reads pre-finalize. MA K5 scored 100 fine, so essay-specific.
3. Remaining exam-types flows M–Y masked behind L6 — reveal after L6 triaged.

## Confirmations

- `git diff tests/helpers/utils.ts` EMPTY (D-03 helper untouched).
- FLOW P (`[318-P] PrePost Exam`) + FLOW T (`[319-T] Manual CRUD`) titles intact (grep = 1 each, unprefixed).
- `from '../helpers/dbSnapshot'` import count = 1 (reused, no new import).
- DB restored clean each run (`Layer 4 OK: 0 matrix rows`, journal `cleaned`).
- Only the 2 spec files changed; zero production / migration / helper changes.
