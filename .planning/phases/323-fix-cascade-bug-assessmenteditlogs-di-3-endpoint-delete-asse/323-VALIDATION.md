---
phase: 323
slug: fix-cascade-bug-assessmenteditlogs-di-3-endpoint-delete-asse
status: validated
nyquist_compliant: partial
wave_0_complete: true
created: 2026-05-26
audited: 2026-05-26
---

# Phase 323 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 323-RESEARCH.md §"Validation Architecture" + 323-CONTEXT.md D-01..D-04.
> Audited post-execution 2026-05-26 — see "Validation Audit" section below.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.58.2 + TypeScript 5.9.3 (TS, not .NET) |
| **Config file** | `tests/playwright.config.ts` (testDir: `./e2e`, baseURL: `http://localhost:5277`) |
| **Quick run command** | `cd tests && npx playwright test Phase323 --headed` (DEFERRED — spec belum dibuat) |
| **Full suite command** | `cd tests && npx playwright test` |
| **.NET build verify** | `dotnet build` (per CLAUDE.md Develop Workflow Step 3) |
| **Runtime verify (substitute)** | Direct HTTP POST via Playwright `browser_evaluate` + `fetch('/Admin/Delete*')` dengan antiforgery token dari `/Home/Index` |
| **Estimated runtime** | ~60s spec quick / ~5min runtime POST + SEED lifecycle |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 new warning per CLAUDE.md Step 3) ✅
- **After Wave 1 (cascade patch 3 endpoint):** runtime POST verify pakai 3 session real lokal ✅
- **After Wave 2 (test spec written):** DEFERRED — Plan 02 spec di-defer ke regression phase berikutnya
- **Before phase verification:** runtime POST verify cover 3 endpoint (single + standard group + Pre-Post group) ✅
- **Max feedback latency:** ~60s runtime POST per endpoint

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 323-01-01 | 01 | 1 | CASCADE-01 #1 | — | `RemoveRange(AssessmentEditLogs)` block present in 3 endpoints | static grep | `grep -c "AssessmentEditLogs.RemoveRange\|_context.AssessmentEditLogs.RemoveRange" Controllers/AssessmentAdminController.cs` = 3 | ✅ controller patched | ✅ green |
| 323-01-02 | 01 | 1 | CASCADE-01 #4 | — | Audit description contains `EditLogsCount=N` token in 3 endpoints | static grep | `grep -c "EditLogsCount={preDeleteEditLogsCount}" Controllers/AssessmentAdminController.cs` = 3 | ✅ controller patched | ✅ green |
| 323-01-03 | 01 | 1 | CASCADE-01 #7 | — | No schema/model/migration change | static git | `git diff --stat Models/ Migrations/ Data/ApplicationDbContext.cs` → empty | ✅ N/A | ✅ green |
| 323-01-04 (extension) | 01 | 1 | CASCADE-01 #1 (extended) | — | `RemoveRange(UserPackageAssignments)` block in 3 endpoints (post-browser-verify scope extension) | static grep | `grep -c "_context.UserPackageAssignments.RemoveRange" Controllers/AssessmentAdminController.cs` = 4 (3 baru + 1 pre-existing DeletePackage) | ✅ commit 6e0fd95e | ✅ green |
| 323-02-01 | 02 | 2 | CASCADE-01 #2 | — | Delete session 0 edit logs → success (no regression) | runtime POST (spec deferred) | direct POST `/Admin/DeleteAssessment` dengan Session ID 0 EditLog | runtime verified Session 2 baseline single | ✅ green (runtime) |
| 323-02-02 | 02 | 2 | CASCADE-01 #3 | — | Delete session ≥1 edits → success, EditLogs row count = 0 post | runtime POST (spec deferred) | direct POST + seed AssessmentEditLog Session 2 (1 row) | runtime verified Session 2: success + EditLogsCount=1 audit + DB clean | ✅ green (runtime) |
| 323-02-03 | 02 | 2 | CASCADE-01 #6 | — | Group campuran sibling no-edits + edits → success | runtime POST (spec deferred) | direct POST `/Admin/DeleteAssessmentGroup` id=11 + `/Admin/DeletePrePostGroup` linkedGroupId=119 dengan seed EditLog | runtime verified 11+12 (EditLogsCount=1) + 119+120 (EditLogsCount=2) all wiped | ✅ green (runtime) |
| 323-02-04 | 02 | 2 | CASCADE-01 #5 | T-323-01 | Transaction rollback bersih saat exception | code review | inspect `using var tx + tx.RollbackAsync()` intact post-patch + no new tx scope created | manual | ✅ green (verifier 7/7 confirmed) |
| 323-02-05 | 02 | 2 | CASCADE-01 #4 | — | AuditLogs row contains `EditLogsCount=N` post-test | manual DB | `SELECT TOP 5 Description FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC` | runtime verified — 3 audit row sample present | ✅ green (runtime) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · DEFERRED = formal spec deferred ke regression phase, equivalent runtime POST verify cover code path*

---

## Wave 0 Requirements (DEFERRED — Plan 02 spec defer)

- [ ] `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` — DEFERRED ke phase berikutnya sebagai regression asset. Runtime POST verify via Playwright `browser_evaluate` + `fetch()` direct call cover identical code path UI form submit.
- [x] Selector probe — UI list filter `>= sevenDaysAgo` exclude session test target → bypass via direct POST (still real HTTP path).
- [x] Seed identification — Session 2 (single), 119+120 (PrePost group), 11+12 (standard group) di local DB.
- [x] `docs/SEED_JOURNAL.md` entry append — 2 entry Phase 323 status `cleaned` (2026-05-26).
- [x] Snapshot DB lokal — 2 BACKUP file (`HcPortalDB_Dev-pre323-20260526-165911.bak` + `HcPortalDB_Dev-pre323b-20260526-172532.bak`).
- [x] Restore DB lokal post-test — 2 RESTORE operations verified.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions | Status |
|----------|-------------|------------|-------------------|--------|
| Transaction rollback saat exception lain (e.g., concurrent edit) | CASCADE-01 #5 | Hard to trigger reliable exception mid-cascade | Code review `using var tx + tx.RollbackAsync()` intact di 3 endpoint; existing Phase 312 WR-01 race re-check tetap berfungsi | ✅ verifier 7/7 pass |
| Audit log `Description` contains `EditLogsCount=N` token | CASCADE-01 #4 | DB row introspection bukan visible UI | Post-runtime POST run, `sqlcmd -S localhost\SQLEXPRESS -E -C -d HcPortalDB_Dev -Q "SELECT TOP 5 Description FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC"` — verify token present | ✅ verified 3 audit row samples |
| IT promo Dev (10.55.3.3) + Prod | external | Per CLAUDE.md Develop Workflow Step 5 — bukan tanggung jawab developer | Notify Team IT dengan commit hash `f1849367` + flag NO MIGRATION + retry hapus Session Id 2+5 | ⏸ pending IT |
| Playwright spec formal regression coverage | CASCADE-01 #2/#3/#6 | DEFERRED — Plan 02 spec di-defer per verifier override | Buat `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` di phase berikutnya sebagai regression asset | ⏸ deferred |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or runtime equivalent (Playwright POST substitute for spec file)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (runtime POST substitute for spec deferred)
- [x] No watch-mode flags
- [x] Feedback latency < 90s per task
- [x] `nyquist_compliant: partial` set in frontmatter — formal Playwright spec deferred, runtime POST cover equivalent code path

**Approval:** validated (partial — Plan 02 formal spec deferred sebagai regression asset, runtime POST verify accepted as substitute per verifier 7/7 override)

---

## Validation Audit 2026-05-26

| Metric | Count |
|--------|-------|
| Tasks in map (original) | 8 |
| Tasks added (UPA extension) | 1 |
| Gaps found | 3 (323-02-01/02/03 — Playwright spec file missing) |
| Gaps resolved via runtime POST substitute | 3 |
| Gaps escalated to manual-only deferred | 1 (formal spec creation deferred) |
| Status changed pending → green | 9 |

### Audit Rationale

Original VALIDATION.md draft assumed Plan 02 spec would be written. Post-execution browser verify (Plan 01 commit `392f0b24` → extension `6e0fd95e`) exposed UPA second-FK bug; verifier accepted **Plan 02 spec deferred override** because runtime POST via Playwright `browser_evaluate` + `fetch('/Admin/Delete*')` hits identical controller code path as UI form submit. 3 endpoint runtime verified end-to-end with real DB state (BACKUP → seed → POST → RESTORE lifecycle):

- DeleteAssessment Session 2 (1 EditLog + 1 UPA + 1 Pkg) → success, audit `EditLogsCount=1`
- DeletePrePostGroup Session 119+120 (2 EditLog + 2 UPA + 2 Pkg) → success, audit `EditLogsCount=2`
- DeleteAssessmentGroup Session 11+12 (1 EditLog + 1 UPA + 1 Pkg) → success, audit `EditLogsCount=1`

Formal Playwright spec file (`tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts`) deferred sebagai regression asset di phase berikutnya — provides long-term regression coverage tanpa block ship.
