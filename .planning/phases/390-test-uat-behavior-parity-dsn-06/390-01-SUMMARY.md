---
phase: 390-test-uat-behavior-parity-dsn-06
plan: 01
status: complete
completed: 2026-06-17
requirements: [DSN-06]
tags: [test, e2e, parity, playwright]
---

# Phase 390-01 Summary — Escalate Playwright parity specs + import fixture (DSN-06 non-destructive layer)

## What was built

Autonomous/deterministic half of DSN-06: escalated the two existing Playwright specs from smoke → strong
non-destructive parity asserts, and built the Excel import fixture Plan 02 consumes. **0 production code, 0 backend,
0 migration** — test/fixture only.

- **Task 1 — D-07 build gate + import fixture:** `dotnet build` → **0 errors** (25 pre-existing nullable warnings).
  Generated `tests/fixtures/import-mapping-390.xlsx` via exceljs 4.4.0 — row1 headers `NIP Coach` / `NIP Coachee`,
  row2 NIP pair `123456` (A2, coach slot) / `29007720` (B2, coachee slot).
- **Task 2 — escalate `coachcoacheemapping-389.spec.ts` (14 → 17 V-tests):** promoted V-10 (added `#editCoachSelect`
  non-empty + `#editStartDate` date-format asserts), V-11 (delete-preview route fired + `#deleteCoacheeName` rendered,
  not "Memuat...", stays non-destructive), V-13 kept (PathBase-aware `hitPath` assert). Added V-15 export download
  (`CoachCoacheeMapping.xlsx`), V-16 template download (`coach_coachee_import_template.xlsx`), V-17 console-error gate.
- **Task 3 — escalate `coachworkload-388.spec.ts` (5 → 8 tests):** added W-EXP export download (`coach_workload.xlsx`),
  W-THR threshold modal fields-filled non-destructive (Admin role-gate, close via "Batal" without saving), W-ERR
  console-error gate. Then ran BOTH escalated specs green.

## Import fixture NIP pair (Plan 02 needs this)

| Slot | Cell | NIP | User | Role |
|------|------|-----|------|------|
| Coach | A2 | `123456` | iwan3@pertamina.com (Iwan) | Coachee |
| Coachee | B2 | `29007720` | rino.prasetyo@pertamina.com (Rino) | Coachee |

**⚠ Local-data caveat for Plan 02:** only **2** Users in the local DB have a non-null NIP, and **both are
Coachee-role** — no coach in the local DB has a NIP (the active mapping's coach "Rustam Santiko" + the `rustam.nugroho`
coach account both have NULL NIP). The fixture uses the only two existing non-null NIPs. The import requires both
NIPs to exist in Users (satisfied). Plan 02's manual import UAT should interpret the import-results card accordingly:
if the controller hard-requires the A2 user to be coach-eligible, the row may report "Error"/"Dilewati" rather than
"Berhasil Dibuat" — that is a **local-data limitation, not a code defect**. The card rendering + flow is the parity
signal. If Plan 02 wants a clean "Berhasil Dibuat", assign a NIP to a coach inside the snapshot window first.

## Test counts (final)

- `coachcoacheemapping-389.spec.ts`: **17** V-tests (V-01..V-17) + parses clean (`--list`).
- `coachworkload-388.spec.ts`: **8** tests (5 existing + W-EXP/W-THR/W-ERR) + parses clean.

## Combined run (verification gate)

`E2E_BASE_URL=http://localhost:5270 npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1`
→ **21 passed / 5 skipped / 0 FAILED (1.5m).**

Port note: 5277 was occupied by a stale dotnet process (kept untouched per user); app launched fresh on **5270**
from current HEAD (`Authentication__UseActiveDirectory=false` + `Server=lpc:Lenovo\SQLEXPRESS` shared-mem). baseURL
override via the `E2E_BASE_URL` config hook added earlier this session.

**389 spec:** green V-01/02/03/04/06/07/08/09/10/14/15/16/17 (13); skipped V-05/11/12/13 (4, data-guard — need ≥2
coaches / disposable+graduated rows → Plan 02 live mutation).
**388 spec:** green DSN-04(×2)/DSN-05/parity-filter/W-EXP/W-THR/W-ERR (7); skipped DSN-06 approve/skip (1, no coach
overload in local DB).
**Acceptance met:** W-EXP, W-THR, V-15, V-16 all **PASS** (not skip); console-error gates V-17/W-ERR green (0 errors).

DB integrity: global.setup BACKUP → matrix seed (Layer 1 OK 18/10/30/80) → globalTeardown RESTORE (Layer 4 OK = 0
matrix rows) → SEED_JOURNAL active→cleaned → snapshot deleted. No seed pollution; non-destructive (no submit clicks).

## Commits

- `c05e378a` test(390-01): add import fixture import-mapping-390.xlsx (Task 1)
- `628ee705` test(390-01): escalate coachcoacheemapping-389 spec V-10/11/13 + V-15/16/17 (Task 2)
- `634fcb12` test(390-01): escalate coachworkload-388 spec W-EXP/W-THR/W-ERR (Task 3)

## Deviations from plan

- **DB table name `Users`, not `AspNetUsers`:** the plan's example NIP query used `AspNetUsers`; the app's Identity
  schema uses a custom `Users` table. Adjusted the query — not a behavior deviation.
- **Coach NIP unavailable locally:** no coach has a non-null NIP (documented above). Fixture uses the only 2 existing
  non-null NIPs (both Coachee-role). Task 1 acceptance (correct headers + non-empty existing NIP pair) is still met.

## Self-Check: PASSED

- FOUND: `tests/fixtures/import-mapping-390.xlsx` (A1=NIP Coach, B1=NIP Coachee, A2=123456, B2=29007720)
- FOUND: `coachcoacheemapping-389.spec.ts` 17 V-tests; `coachworkload-388.spec.ts` 8 tests
- FOUND commits: `c05e378a`, `628ee705`, `634fcb12`
- Combined run 21 passed / 5 skipped / 0 failed

## Next: Plan 02 (checkpoints)

Live-mutation UAT roundtrip (Playwright MCP, snapshot/restore) C1-C6 + W1/W3/W4/W5, manual Excel import C7
(fixture above), regression sign-off (dotnet test + Playwright + 0-backend/0-migration confirm). Non-autonomous —
3 blocking human-verify checkpoints.

---
*Phase: 390-test-uat-behavior-parity-dsn-06*
*Completed: 2026-06-17*
