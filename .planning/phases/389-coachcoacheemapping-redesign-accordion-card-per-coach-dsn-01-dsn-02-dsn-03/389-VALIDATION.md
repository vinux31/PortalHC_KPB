---
phase: 389
slug: coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03
status: validated-partial
nyquist_compliant: partial
wave_0_complete: true
created: 2026-06-17
validated: 2026-06-17
automated_green: 10
deferred_to_390: 4
---

# Phase 389 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `389-RESEARCH.md` §Validation Architecture (14-assertion Playwright parity plan).
> Phase 354 lesson: Razor dynamic + a11y MUST be Playwright-runtime-asserted — grep + build are NOT enough.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright Test (`@playwright/test`) |
| **Config file** | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel:false`, project `chromium` deps `setup`) |
| **Quick run command** | `cd tests; npx playwright test coachcoacheemapping-389 --workers=1` |
| **Full suite command** | `cd tests; npx playwright test --workers=1` |
| **C# build gate** | `dotnet build` 0 error (Razor compile) — run BEFORE Playwright |
| **App prerequisite** | `dotnet run` with `Authentication__UseActiveDirectory=false` (app does NOT auto-start; local admin login) |
| **Login helper** | `loginAny(page, 'admin')` via `/Account/Login` + `accounts.admin` (reuse from sibling spec `coachworkload-388.spec.ts`) |
| **Estimated runtime** | ~30–60 seconds (single spec, workers=1) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + `npx playwright test coachcoacheemapping-389 -g "<task-scope>" --workers=1`
- **After every plan wave:** `npx playwright test coachcoacheemapping-389 --workers=1` (full phase spec)
- **Before `/gsd-verify-work`:** `dotnet build` 0 error + full spec 389 green + visual smoke (`dotnet run` localhost:5277 — card render, collapse, modal)
- **Max feedback latency:** ~60 seconds
- **Note:** FULL data-mutation parity (assign/import/export end-to-end) = **Phase 390**. Phase 389 spec = structural + smoke parity (hooks present, collapse/modal open, AJAX path hit). Use `test.skip` pattern (sibling 388 L73) where local DB lacks disposable data.

---

## Per-Task Verification Map

> Task IDs assigned by planner. Each row = one runtime assertion from RESEARCH §Validation Architecture. `❌ W0` = spec file created in Wave 0.

| # | Requirement | Secure/Parity Behavior | Test Type | Automated Command (grep filter) | File | Status |
|---|-------------|------------------------|-----------|---------------------------------|------|--------|
| V-01 | DSN-01 | Each coach = 1 `.card.shadow-sm`; header has `.avatar-initial` + coach name | e2e | `-g "card per coach + header"` | `tests/e2e/coachcoacheemapping-389.spec.ts:44` | ✅ green |
| V-02 | DSN-01 | Badge color follows threshold `<5 bg-info / >=5 bg-warning / >=8 bg-danger` (correlate count↔class via `evaluate`) | e2e | `-g "badge threshold"` | `...389.spec.ts:59` | ✅ green |
| V-03 | DSN-02 | Default ALL CLOSED: `.collapse.show` count == 0 on load; headers `aria-expanded="false"` | e2e | `-g "default closed"` | `...389.spec.ts:83` | ✅ green |
| V-04 | DSN-02 | Click header → `#collapse-0` gets `.show`/visible; `aria-expanded="true"`; chevron rotates. Click again → closes | e2e | `-g "collapse buka tutup"` | `...389.spec.ts:97` | ✅ green |
| V-05 | DSN-02 | INDEPENDENT: open card 0 + card 1 → both `.show` simultaneously (no `data-bs-parent`). Skip if <2 coaches | e2e (data-guard) | `-g "independent multi-open"` | `...389.spec.ts:115` | ⏭️ defer-390 |
| V-06 | DSN-02 | Mini-table `thead th` count == 9; NO `th` "Coachee Aktif" (D-07) | e2e | `-g "9 kolom"` | `...389.spec.ts:127` | ✅ green |
| V-07 | DSN-02 | a11y (Phase 354): focus header → Enter opens, Space toggles; `role=button` or `<button>`; `aria-controls` == body id | e2e | `-g "a11y header toggle"` | `...389.spec.ts:140` | ✅ green |
| V-08 | DSN-03 | "Tambah Mapping" = `.btn-primary` solo; Excel buttons grouped `.btn-group`, all `.btn-sm` | e2e | `-g "toolbar seragam"` | `...389.spec.ts:170` | ✅ green |
| V-09 | DSN-03 | Dead `onclick` gone: Tambah Mapping `getAttribute('onclick')` null/empty; click still opens `#assignModal` | e2e | `-g "tambah mapping buka modal"` | `...389.spec.ts:187` | ✅ green |
| V-10 | DSN-06* | Edit → `#editModal` visible + `#editCoacheeName` set (proves `openEditModal` 7-arg) | e2e | `-g "edit modal"` | `...389.spec.ts:198` | ✅ green |
| V-11 | DSN-06* | Hapus → `#deleteModal` opens; if disposable data: submit → `tr[data-mapping-id]` removed from DOM (H-1) | e2e (data-guard) | `-g "delete hapus row"` | `...389.spec.ts:215` | ⏭️ defer-390 |
| V-12 | DSN-06* | Aksi branch renders per state: `IsCompleted` (Graduated badge) checked BEFORE `IsActive` (Phase 356 D-06) | e2e (data-guard) | `-g "aksi branch"` | `...389.spec.ts:231` | ⏭️ defer-390 |
| V-13 | DSN-06* | AJAX via `appUrl`: route-intercept `**/Admin/CoachCoacheeMapping*` → request path correct, no 404 under sub-path | e2e (route intercept, data-guard) | `-g "ajax appUrl subpath"` | `...389.spec.ts:257` | ⏭️ defer-390 |
| V-14 | DSN-06* | Filter Seksi + Cari + Tampilkan Semua + pagination still work (`resetPageAndSubmit`) | e2e | `-g "filter pagination"` | `...389.spec.ts:282` | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · ⏭️ defer-390 (test exists, `test.skip` data-guard — full mutation parity deferred to Phase 390)*
*DSN-06 rows = smoke parity in 389; FULL data-mutation parity = Phase 390.*
*Run evidence (389-02-SUMMARY, 2026-06-17): `coachcoacheemapping-389 --workers=1` → 11 passed (10 V + setup) / 4 skipped / 0 FAILED + browser UAT (Task 4) APPROVED.*

---

## Wave 0 Requirements

- [x] `tests/e2e/coachcoacheemapping-389.spec.ts` — new spec covering V-01..V-14 (model on `tests/e2e/coachworkload-388.spec.ts`: login + parity + `test.skip` data-guard). CREATED Plan 01 (commits `190b2b19` + `275b3a42`); finalized Plan 02 (`2ca83c2a`). `--list` = 15 tests (14 V + setup).
- [x] Login helper / admin creds — REUSE existing (`tests/helpers/accounts.ts` + `loginAny` pattern from spec 388). No new helper.
- [x] Framework install — NOT needed (Playwright already installed; 28 existing specs).

*Wave 0 = create the spec file with V-01..V-14 stubs before markup tasks land.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual / Deferred | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual polish (card spacing, chevron animation, avatar legibility) | DSN-01/02 | Subjective rendering quality not assertable by selector | `dotnet run` localhost:5277 → /Admin/CoachCoacheeMapping → eyeball card rhythm, open/close animation. **DONE** — browser UAT 2026-06-17 (Task 4): spacing/avatar/chevron rapi-OK |
| Full Excel import/export round-trip with real file | DSN-06 | Requires file upload + disposable DB rows; deferred to Phase 390 UAT | Phase 390: import .xlsx → verify results card; export → open file |
| **V-05** independent multi-open | DSN-02 | `test.skip` data-guard: needs ≥2 coach groups; local DB has 1. Test EXISTS (`...389.spec.ts:115`), runs green once data present | Phase 390 (seeded ≥2 coaches): `npx playwright test coachcoacheemapping-389 -g "independent multi-open" --workers=1` |
| **V-11** delete hapus row | DSN-06 | `test.skip` data-guard: needs disposable mapping row. Test EXISTS (`...389.spec.ts:215`) | Phase 390 (disposable data): `-g "delete hapus row"` |
| **V-12** aksi branch (IsCompleted before IsActive) | DSN-06 | `test.skip` data-guard: needs coachee row + graduated row. Test EXISTS (`...389.spec.ts:231`) | Phase 390 (graduated + active rows): `-g "aksi branch"` |
| **V-13** AJAX appUrl sub-path | DSN-06 | `test.skip` data-guard: needs deletable row to trigger `confirmDelete` fetch. Test EXISTS (`...389.spec.ts:257`) | Phase 390 (deletable row): `-g "ajax appUrl subpath"` |

*Data-mutating actions (assign/deactivate/graduate/reactivate end-to-end) verified manually in 389 smoke (browser UAT Task 4, 1-coach DB) + fully automated in Phase 390 once disposable test data is seeded. V-05/11/12/13 tests are written and parse-able now — they go green automatically when the data-guard precondition is met; no new test code needed in 390, only data.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers the spec file (`coachcoacheemapping-389.spec.ts`) — CREATED + parse-confirmed (15 tests)
- [x] No watch-mode flags (`--workers=1`, no `--ui`/`--watch`)
- [x] Feedback latency < 60s (single spec ~30–60s)
- [~] `nyquist_compliant: partial` — 10/14 automated-green; 4 (V-05/11/12/13) test-written + data-guarded, deferred to Phase 390 (no MISSING tests; all 14 requirement rows have a parse-able test)

**Approval:** validated-partial 2026-06-17 (user gate: mark deferred-to-390). Browser UAT Task 4 APPROVED. Full mutation parity → Phase 390.

---

## Validation Audit 2026-06-17

State A (existing VALIDATION.md audited). Spec `coachcoacheemapping-389.spec.ts` parse-confirmed (`--list` = 15 tests). Statuses reconciled against 389-02-SUMMARY run evidence (11 passed / 4 skipped / 0 FAILED) + browser UAT Task 4.

| Metric | Count |
|--------|-------|
| Requirements (V-rows) | 14 |
| COVERED (automated green) | 10 |
| Deferred to Phase 390 (test-written, data-guarded `test.skip`) | 4 |
| MISSING (no test) | 0 |
| Gaps found | 0 |
| Resolved (auditor) | 0 |
| Escalated | 0 |

**Verdict:** PARTIAL by design. No MISSING tests — every requirement has a parse-able runtime assertion. V-05/11/12/13 skip only on the data-guard precondition (≥2 coaches / disposable rows) and go green automatically once Phase 390 seeds disposable data. No `gsd-nyquist-auditor` spawn required (auto-filling would duplicate Phase 390 scope + require temp seed, contradicting the phase deferral decision).

### Live re-run confirmation 2026-06-17 (port 5270)

Re-validated against a **fresh live app build** (5277 occupied by a stale `dotnet` PID 15480; launched current HEAD on free port **5270** via `Authentication__UseActiveDirectory=false` + `Server=lpc:Lenovo\SQLEXPRESS` shared-mem override). `playwright.config.ts` made `E2E_BASE_URL`-overridable (backward-compatible, defaults 5277).

```
E2E_BASE_URL=http://localhost:5270 npx playwright test coachcoacheemapping-389 --workers=1
→ 11 passed (10 V + setup) / 4 skipped / 0 FAILED (46.2s)
```

- `dotnet build` → 0 error (25 pre-existing nullable warnings).
- Green: V-01/02/03/04/06/07/08/09/10/14. Skipped (data-guard): V-05/11/12/13. **Identical to 389-02-SUMMARY** — result reproducible on a clean build.
- DB integrity: globalSetup BACKUP → matrix seed (Layer 1 OK 18/10/30/80) → globalTeardown RESTORE (Layer 4 OK = 0 matrix rows) → SEED_JOURNAL `active`→`cleaned` → snapshot deleted. Local DB verified clean post-run (0 matrix rows, state file removed). No seed pollution.
