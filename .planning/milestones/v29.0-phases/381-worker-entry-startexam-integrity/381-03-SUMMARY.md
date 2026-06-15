---
phase: 381-worker-entry-startexam-integrity
plan: 03
subsystem: assessment-entry
tags: [e2e, playwright, acceptance, prepost, impersonation, uat]
requires: [WSE-04-type-aware-sibling, WSE-05-impersonate-readonly]
provides: [WSE-04-e2e-acceptance, WSE-05-e2e-acceptance, migration-false-proof]
affects: [tests/e2e/exam-taking.spec.ts, tests/e2e/impersonation.spec.ts]
tech-stack:
  added: []
  patterns: [playwright-db-assert, clearcookies-user-switch, force-open-via-sql]
key-files:
  created: []
  modified:
    - tests/e2e/exam-taking.spec.ts
    - tests/e2e/impersonation.spec.ts
key-decisions:
  - "E2E #4 (WSE-04): StartExam Pre same-day → assert qcard count == paket Pre (3) + teks 'PRE-Q' tanpa 'POST-Q'. Full pass/grade DEFERRED pasca-382 (entry-pool only)."
  - "E2E #7 (WSE-05): impersonate StartExam → DB assert no-mutation (StartedAt null, Status Open, 0 UPA) + render preview status<400 + qcard visible (auto-cover T3 Bagian A render, no NRE/500) → stop impersonate → worker asli → StartedAt set + 1 UPA (SC#3)."
  - "Harness fixes: clearCookies() between user-switches (Logout = [HttpPost] → GET goto dead); compliant title '{Stage} Test {Track} {Lokasi}' + future schedule (Standard create validation Phase 336/339); force Status='Open' via queryScalar UPDATE (impersonate precondition tak bisa via worker-GET)."
  - "T3 Bagian B: migration:false PROVEN — `dotnet ef migrations add` → Up()/Down() kosong → removed → snapshot noise (EF 10 ToTable formatting) reverted → tree bersih."
requirements-completed: [WSE-04, WSE-05]
duration: ~70 min (termasuk 4 e2e iterasi: server-lock, title/schedule, clearCookies, render-assert)
completed: 2026-06-15
---

# Phase 381 Plan 03: WSE-04/WSE-05 E2E Acceptance + Migration Gate Summary

Acceptance Phase 381 terbukti **live** via Playwright: e2e #4 (WSE-04 PrePost same-day pool isolation — StartExam Pre hanya soal paket Pre) + e2e #7 (WSE-05 impersonate read-only — no-mutation + render preview tanpa 500/NRE + deferred-start) **3/3 green** dengan setup-snapshot → test → teardown-RESTORE (Layer 4 clean). `migration: false` terbukti via EF scaffold (Up/Down kosong). Tak ada kode produksi disentuh (test-infra only).

## Execution

- **Duration:** ~70 min (4 iterasi e2e — debugging local-harness, bukan kode produksi) · **Tasks:** 3 (2 auto + 1 checkpoint) · **Files:** 2 (e2e specs)
- **Commit:** `aef20f95` (e2e specs). migration check: no commit (scaffold→remove→revert noise).

### Task 1 — E2E #4 PrePost pool isolation (WSE-04)
`exam-taking.spec.ts` describe "Phase 381 WSE-04": HC create PrePost same-day (createPrePostAssessmentViaWizard) → Pre paket 3 soal "PRE-Q*", Post paket 2 soal "POST-Q*" → worker StartExam Pre → assert `[id^="qcard_"]` count == 3 (BUKAN 5) + teks 'PRE-Q' tanpa 'POST-Q'. Membuktikan sibling type-aware (Plan 01) memisahkan pool. Full pass/grade ditandai DEFERRED pasca-382.

### Task 2 — E2E #7 impersonate read-only (WSE-05)
`impersonation.spec.ts` describe "Phase 381 WSE-05": HC create Standard utk worker + paket → force `Status='Open'` (queryScalar UPDATE; StartedAt null) → admin impersonate → StartExam → assert `impResp.status()<400` + qcard visible (preview render D-06, no NRE) + DB no-mutation (StartedAt null, Status Open, 0 UPA) → stopImpersonation → worker asli StartExam → StartedAt set + 1 UPA (SC#3 deferred-start). SignalR absence diverifikasi via DB-assert (broadcast di-gate sama).

### Task 3 — Checkpoint UAT (human-verify, APPROVED)
- **Bagian A** (render impersonate preview, lesson 354/355): AUTO-COVERED oleh assertion e2e #7 (status<400 + qcard visible) — render Razor in-memory (vm.AssignmentId=0) tanpa RuntimeBinderException/500.
- **Bagian B** (migration:false): `dotnet ef migrations add _Phase381MigrationCheck` → `Up()`/`Down()` KOSONG → `dotnet ef migrations remove --force` → snapshot diff (EF 10.0.3 `ToTable("X",(string)null)` formatting, 33 baris, ZERO schema) di-`git checkout` → tree bersih.
- User **approved** 2026-06-15.

## Verification

- E2E: **3 passed** (setup + WSE-04 + WSE-05), 1.2m, `--workers=1`, chromium headless, AD off. Teardown RESTORE Layer-4 OK (0 matrix rows), SEED_JOURNAL cleaned.
- migration:false PROVEN (empty Up/Down; tree clean post-revert).
- xUnit (regression dari Plan 01/02): 391/391 green.
- Tree bersih (hanya SECURITY.md untracked pre-existing).

## Deviations from Plan

**[Test-harness, additive] clearCookies user-switch + force-Open precondition + render assertion** — Found during: e2e debugging. (1) `logout()` helper GET-goto tak berfungsi (Logout = [HttpPost]+antiforgery) → pakai `page.context().clearCookies()` antar user-switch. (2) Standard create kini enforce naming convention (336/339) + schedule-not-past → title compliant + schedule `tomorrow()`. (3) impersonate precondition Status=Open+StartedAt-null tak bisa via worker-GET → force via `queryScalar` UPDATE. (4) e2e #7 diperkuat assert render (status<400 + qcard) → auto-cover T3 Bagian A. Semua di test-layer; zero kode produksi. **Total: 1 (test-infra hardening). Impact:** acceptance live tercapai + render auto-verified.

## Notes

- Local e2e env: SQLBrowser ON + app `Authentication__UseActiveDirectory=false dotnet run` (5277) + `--workers=1` + grep WAJIB sertakan setup test ('verify app is running') agar snapshot/restore lifecycle utuh. Dev-server di-stop pasca-run (hindari build-lock).
- Parallel-session: 380 sudah COMPLETE (secure 7/7 + UAT 5/5) → sesi ini sole writer; STATE advance normal.

## Next

Plan 381-03 selesai → semua 3 plan Phase 381 ada SUMMARY → lanjut phase-level verification (code review gate + regression + verify-work goal-backward) → tutup Phase 381.

## Self-Check: PASSED
- key-files exist on disk: ✓ (exam-taking.spec.ts, impersonation.spec.ts)
- `git log --grep="381-03"` returns 1 commit: ✓ (aef20f95)
- All acceptance criteria re-run green: ✓ (e2e 3/3 + migration:false)
- Checkpoint T3 approved by user: ✓
