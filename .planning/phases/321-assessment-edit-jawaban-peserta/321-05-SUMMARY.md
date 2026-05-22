---
phase: 321-assessment-edit-jawaban-peserta
plan: 05
type: execute
wave: 5
status: partial
completed_at: 2026-05-22
commits:
  - 7b676899
  - 75242556
deferred:
  - "Task 2 Playwright run + SEED_WORKFLOW pre/post"
  - "Task 3 BLOCKING — 4 manual UAT + tag + merge main + push origin + IT notify final"
---

# PLAN 05 — Activity Log + Playwright + UAT (SUMMARY, PARTIAL)

## Commits

| Hash | Message |
|------|---------|
| `7b676899` | feat(v17.0-p321): Activity Log Edit History tab (lazy-load partial, D-05 reason label mapper) |
| `75242556` | test(v17.0-p321): Playwright spec 4 test (auth-gate + happy-path + concurrency-stale + flip-preview-AJAX) |

## Deviations from Plan

1. **Playwright fixture import path** — plan refer `'./fixtures/accounts'` + uppercase `ACCOUNTS`. Actual: `tests/helpers/accounts.ts` lowercase `accounts` named export. Fixed via Phase 320 pattern: `import { accounts, AccountKey } from '../helpers/accounts';`

2. **Login form input names** — plan refer `input[name="Email"]` + `"Password"` (capitalized). Actual: `name="email"` + `"password"` lowercase (verified via export-per-peserta.spec.ts:31-32).

3. **Login helper pattern** — plan `login(page, role)`. Replaced with `loginAny(page, accountKey)` matching Phase 320 pattern: accepts any redirect away dari `/Account/Login` (BUKAN wait `**/Home/**` karena HC/Coachee redirect ke /CMP atau /CDP).

4. **Existing `showActivityLog` integration** — plan add separate `btn-activity-log` handler. Actual existing handler line 950-958 already calls `showActivityLog(sid, wname)`. Integrated: existing handler extended to also set `modal.dataset.currentSessionId` + reset Edit History tab cache + reset tab ke Timeline default.

5. **Modal line drift** — plan refer line 540-559. Actual modal-body line 574-583, modal line 564. Pattern match unique, edit applied OK.

6. **Task 2 run + SEED_WORKFLOW pre/post DEFERRED** — user manual (DB snapshot/restore + npx playwright + journal tracking).

7. **Task 3 [BLOCKING] DEFERRED ENTIRELY** — semua user action irreversible (tag + merge main + push origin + IT notify). Interactive mode tidak boleh trigger production push tanpa explicit per-step user confirm.

## Threat Mitigations Verified (Task 1)

| ID | Threat | Verification |
|----|--------|--------------|
| T-321-10 | Audit visibility cross-session leak | `EditHistoryPartial` strict filter `Where(l => l.AssessmentSessionId == sessionId)` + `[Authorize(Roles="Admin, HC")]` |
| T-321-05b | XSS in Edit History partial | Razor `@` auto-encode ALL user-controlled fields (QuestionTextSnapshot, OldAnswerTextSnapshot, NewAnswerTextSnapshot, ActorName, ReasonText). NO `@Html.Raw`. |

## Files Created

- `Views/Admin/_EditHistoryPartial.cshtml` (40 lines)
- `tests/e2e/edit-peserta-answers.spec.ts` (171 lines, 4 test)

## Files Modified

- `Views/Admin/AssessmentMonitoringDetail.cshtml` (+72 lines — modal body 2-tab refactor + lazy-load JS + btn-activity-log handler extension)
- `Controllers/AssessmentAdminController.cs` (+11 lines — EditHistoryPartial action)

## Outstanding User Actions (Task 2 + Task 3)

### Task 2 — Playwright run

```bash
# Step 0a — Snapshot DB
sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -C -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:/temp/HcPortalDB_Dev_phase321_seed.bak' WITH INIT, COMPRESSION"

# Step 0b — Find/seed Completed eligible session
sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -C -Q "
  SELECT TOP 5 Id, Title, Category, TahunKe, Status, IsPassed, NomorSertifikat
  FROM AssessmentSessions
  WHERE Status='Completed' AND IsManualEntry=0
    AND NOT (Category='Assessment Proton' AND TahunKe='Tahun 3')
  ORDER BY Id DESC
"

# Step 0c — Catat docs/SEED_JOURNAL.md (klasifikasi temporary + local-only)

# Step 1 — Terminal 1: dotnet run
dotnet run

# Step 2 — Terminal 2: run spec dengan env COMPLETED_PASS_SESSION_ID
COMPLETED_PASS_SESSION_ID={id} npx playwright test tests/e2e/edit-peserta-answers.spec.ts

# Step 3 — Post-run: stop dotnet run, restore DB
sqlcmd -S "localhost\SQLEXPRESS" -d master -C -Q "
  ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:/temp/HcPortalDB_Dev_phase321_seed.bak' WITH REPLACE;
  ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;
"

# Step 4 — Update docs/SEED_JOURNAL.md → status: cleaned (timestamp + verifier)
```

**HARD GATE:** 4/4 pass. Kalau fail, fix root cause sebelum lanjut Task 3.

### Task 3 — Manual UAT + tag + merge + push + IT notify

4 UAT checklist (user manual sqlcmd + browser):
- [ ] **UAT 1** — DB cascade flip Pass↔Fail (SEED_WORKFLOW snapshot/restore)
- [ ] **UAT 2** — SignalR 2-tab cross-tab live update + 8s toast timeout (D-07 LOCKED)
- [ ] **UAT 3** — Activity Log Edit History tab (verbose labels D-05, sort DESC, format correct)
- [ ] **UAT 4** — Migration rollback re-verify (down + up)

Setelah 4/4 UAT pass:
```bash
git tag -a v17.0-p321-complete -m "Milestone v17.0 Phase 321: Edit Jawaban Peserta complete"
git checkout main
git pull --ff-only origin main
git merge --no-ff feature/phase-321-edit-jawaban -m "Merge phase 321: edit jawaban peserta (v17.0)"
git push origin main
git push origin v17.0-p321-complete
```

IT notify final (template di plan 05 line 728-743).

## Build Status

0 error setelah Task 1 commit. Task 2 spec file = TypeScript, no dotnet build impact.

## Phase 321 Status

**LOKAL CODE COMPLETE — Pending user: Playwright run + 4 UAT + tag + merge + push + IT notify final.**

Total commit Phase 321 di branch `feature/phase-321-edit-jawaban` (siap merge ke main):
- PLAN 01: 5 commit (4 task + 1 SUMMARY)
- PLAN 02: 3 commit (2 task + 1 SUMMARY)
- PLAN 03: 5 commit (4 task + 1 SUMMARY)
- PLAN 04: 5 commit (3 task + 1 fix deviation + 1 SUMMARY)
- PLAN 05: 2 commit (2 task — Task 3 deferred)

Total: **20 commit** di feature branch (pending SUMMARY commit ini = 21).

## Self-Check: PASSED (partial — code complete, manual + irreversible actions deferred)

- Task 1 code complete, 0 build error, T-321-10/T-321-05b mitigations grep-verified.
- Task 2 spec file complete dengan import fix (lowercase accounts dari ../helpers/).
- Deferred items explicitly dokumentasi untuk user pickup.
