---
phase: 389
slug: coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-17
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
| V-01 | DSN-01 | Each coach = 1 `.card.shadow-sm`; header has `.avatar-initial` + coach name | e2e | `-g "card per coach + header"` | ❌ W0 | ⬜ pending |
| V-02 | DSN-01 | Badge color follows threshold `<5 bg-info / >=5 bg-warning / >=8 bg-danger` (correlate count↔class via `evaluate`) | e2e | `-g "badge threshold"` | ❌ W0 | ⬜ pending |
| V-03 | DSN-02 | Default ALL CLOSED: `.collapse.show` count == 0 on load; headers `aria-expanded="false"` | e2e | `-g "default closed"` | ❌ W0 | ⬜ pending |
| V-04 | DSN-02 | Click header → `#collapse-0` gets `.show`/visible; `aria-expanded="true"`; chevron rotates. Click again → closes | e2e | `-g "collapse buka tutup"` | ❌ W0 | ⬜ pending |
| V-05 | DSN-02 | INDEPENDENT: open card 0 + card 1 → both `.show` simultaneously (no `data-bs-parent`). Skip if <2 coaches | e2e | `-g "independent multi-open"` | ❌ W0 | ⬜ pending |
| V-06 | DSN-02 | Mini-table `thead th` count == 9; NO `th` "Coachee Aktif" (D-07) | e2e | `-g "9 kolom"` | ❌ W0 | ⬜ pending |
| V-07 | DSN-02 | a11y (Phase 354): focus header → Enter opens, Space toggles; `role=button` or `<button>`; `aria-controls` == body id | e2e | `-g "a11y header toggle"` | ❌ W0 | ⬜ pending |
| V-08 | DSN-03 | "Tambah Mapping" = `.btn-primary` solo; Excel buttons grouped `.btn-group`, all `.btn-sm` | e2e | `-g "toolbar seragam"` | ❌ W0 | ⬜ pending |
| V-09 | DSN-03 | Dead `onclick` gone: Tambah Mapping `getAttribute('onclick')` null/empty; click still opens `#assignModal` | e2e | `-g "tambah mapping buka modal"` | ❌ W0 | ⬜ pending |
| V-10 | DSN-06* | Edit → `#editModal` visible + `#editCoacheeName` set (proves `openEditModal` 7-arg) | e2e | `-g "edit modal"` | ❌ W0 | ⬜ pending |
| V-11 | DSN-06* | Hapus → `#deleteModal` opens; if disposable data: submit → `tr[data-mapping-id]` removed from DOM (H-1) | e2e (data-guard) | `-g "delete hapus row"` | ❌ W0 | ⬜ pending |
| V-12 | DSN-06* | Aksi branch renders per state: `IsCompleted` (Graduated badge) checked BEFORE `IsActive` (Phase 356 D-06) | e2e | `-g "aksi branch"` | ❌ W0 | ⬜ pending |
| V-13 | DSN-06* | AJAX via `appUrl`: route-intercept `**/Admin/CoachCoacheeMapping*` → request path correct, no 404 under sub-path | e2e (route intercept) | `-g "ajax appUrl subpath"` | ❌ W0 | ⬜ pending |
| V-14 | DSN-06* | Filter Seksi + Cari + Tampilkan Semua + pagination still work (`resetPageAndSubmit`) | e2e | `-g "filter pagination"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*DSN-06 rows = smoke parity in 389; FULL data-mutation parity = Phase 390.*

---

## Wave 0 Requirements

- [ ] `tests/e2e/coachcoacheemapping-389.spec.ts` — new spec covering V-01..V-14 (model on `tests/e2e/coachworkload-388.spec.ts`: login + parity + `test.skip` data-guard).
- [x] Login helper / admin creds — REUSE existing (`tests/helpers/accounts.ts` + `loginAny` pattern from spec 388). No new helper.
- [x] Framework install — NOT needed (Playwright already installed; 28 existing specs).

*Wave 0 = create the spec file with V-01..V-14 stubs before markup tasks land.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual polish (card spacing, chevron animation, avatar legibility) | DSN-01/02 | Subjective rendering quality not assertable by selector | `dotnet run` localhost:5277 → /Admin/CoachCoacheeMapping → eyeball card rhythm, open/close animation |
| Full Excel import/export round-trip with real file | DSN-06 | Requires file upload + disposable DB rows; deferred to Phase 390 UAT | Phase 390: import .xlsx → verify results card; export → open file |

*Data-mutating actions (assign/deactivate/graduate/reactivate end-to-end) verified manually in 389 smoke + fully in Phase 390.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers the MISSING spec file (`coachcoacheemapping-389.spec.ts`)
- [ ] No watch-mode flags (`--workers=1`, no `--ui`/`--watch`)
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter (after planner maps task IDs to V-01..V-14)

**Approval:** pending
