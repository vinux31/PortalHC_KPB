---
phase: 323
slug: fix-cascade-bug-assessmenteditlogs-di-3-endpoint-delete-asse
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-26
---

# Phase 323 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 323-RESEARCH.md §"Validation Architecture" + 323-CONTEXT.md D-01..D-04.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.58.2 + TypeScript 5.9.3 (TS, not .NET) |
| **Config file** | `tests/playwright.config.ts` (testDir: `./e2e`, baseURL: `http://localhost:5277`) |
| **Quick run command** | `cd tests && npx playwright test Phase323 --headed` |
| **Full suite command** | `cd tests && npx playwright test` |
| **.NET build verify** | `dotnet build` (per CLAUDE.md Develop Workflow Step 3) |
| **Estimated runtime** | ~60s quick run (3 specs) / full suite varies |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 new warning per CLAUDE.md Step 3)
- **After Wave 1 (cascade patch 3 endpoint):** `npx playwright test Phase323` (3 specs green)
- **After Wave 2 (test spec written):** full Phase323 spec suite + manual UAT localhost browser
- **Before phase verification:** full `npx playwright test` regression — no new failure
- **Max feedback latency:** ~60s (Playwright headed mode)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 323-01-01 | 01 | 1 | CASCADE-01 #1 | — | `RemoveRange(AssessmentEditLogs)` block present in 3 endpoints | static grep | `grep -c "AssessmentEditLogs.RemoveRange\|_context.AssessmentEditLogs" Controllers/AssessmentAdminController.cs` ≥3 | ✅ existing controller | ⬜ pending |
| 323-01-02 | 01 | 1 | CASCADE-01 #4 | — | Audit description contains `EditLogsCount=N` token | static grep | `grep -c "EditLogsCount=" Controllers/AssessmentAdminController.cs` ≥3 | ✅ existing controller | ⬜ pending |
| 323-01-03 | 01 | 1 | CASCADE-01 #7 | — | No schema/model/migration change | static git | `git diff --stat Models/ Migrations/ Data/ApplicationDbContext.cs` → 0 files | ✅ N/A | ⬜ pending |
| 323-02-01 | 02 | 2 | CASCADE-01 #2 | — | Delete session 0 edit logs → success (no regression) | e2e | `npx playwright test Phase323 --grep "no-edits"` | ❌ W0 create spec | ⬜ pending |
| 323-02-02 | 02 | 2 | CASCADE-01 #3 | — | Delete session ≥1 edits → success, EditLogs row count = 0 post | e2e | `npx playwright test Phase323 --grep "with-edits"` | ❌ W0 create spec | ⬜ pending |
| 323-02-03 | 02 | 2 | CASCADE-01 #6 | — | Group campuran sibling no-edits + edits → success | e2e | `npx playwright test Phase323 --grep "group-mixed"` | ❌ W0 create spec | ⬜ pending |
| 323-02-04 | 02 | 2 | CASCADE-01 #5 | T-323-01 | Transaction rollback bersih saat exception | code review | inspect `using var tx + tx.RollbackAsync()` intact post-patch | manual | ⬜ pending |
| 323-02-05 | 02 | 2 | CASCADE-01 #4 | — | AuditLogs row contains `EditLogsCount=N` post-test | manual DB | `SELECT TOP 5 Description FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC` | manual UAT | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` — new spec covering 3 scenarios (no-edits / with-edits / group-mixed)
- [ ] Selector probe — exact button/form text for "Delete" + "Hapus Grup" + "Hapus Pre-Post" in `Views/Admin/ManageAssessment*.cshtml` post-Phase-322 partial split (RESEARCH.md Open Question #1)
- [ ] Seed identification — SELECT query to find 3 session IDs in local DB (1 with 0 edit logs, 1 with ≥1, 1 group rep with mixed siblings)
- [ ] `docs/SEED_JOURNAL.md` entry append — status `active` on seed insert, `cleaned` post-restore (manual gate per SEED_WORKFLOW)
- [ ] Snapshot DB lokal via `sqlcmd BACKUP DATABASE` sebelum seed temporary insert
- [ ] Restore DB lokal post-test (per SEED_WORKFLOW)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Transaction rollback saat exception lain (e.g., concurrent edit) | CASCADE-01 #5 | Hard to trigger reliable exception mid-cascade in E2E | Code review `using var tx + tx.RollbackAsync()` intact di 3 endpoint; existing Phase 312 WR-01 race re-check tetap berfungsi |
| Audit log `Description` contains `EditLogsCount=N` token | CASCADE-01 #4 | DB row introspection bukan visible UI | Post-E2E run, `sqlcmd -S .\SQLEXPRESS -d HcPortalDb -Q "SELECT TOP 5 Description FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC"` — verify token present |
| IT promo Dev (10.55.3.3) + Prod | external | Per CLAUDE.md Develop Workflow Step 5 — bukan tanggung jawab developer | Notify Team IT dengan commit hash + flag no-migration |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (Phase323 spec + selector probe + seed setup)
- [ ] No watch-mode flags (Playwright runs in single-pass, no `--watch`)
- [ ] Feedback latency < 90s per task (Playwright headed ~60s, build ~20s)
- [ ] `nyquist_compliant: true` set in frontmatter after Wave 0 complete + all task entries linked

**Approval:** pending
