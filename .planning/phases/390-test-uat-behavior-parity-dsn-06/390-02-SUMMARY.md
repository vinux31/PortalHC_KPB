---
phase: 390-test-uat-behavior-parity-dsn-06
plan: 02
status: complete
completed: 2026-06-17
requirements: [DSN-06]
tags: [uat, parity, playwright-mcp, snapshot-restore, regression]
---

# Phase 390-02 Summary — Live-mutation UAT + manual import + regression sign-off (DSN-06)

## What was done

Non-autonomous (checkpoint) half of DSN-06: drove the full D-04 live-mutation roundtrip via Playwright MCP @
localhost:**5270** (5277 held by a stale process, kept untouched per user), wrapped in DB snapshot→mutate→restore,
plus the manual Excel import and the regression sign-off gate. Verifies the v32.1 redesign (388 polish + 389
accordion) did NOT regress any live action. **0 backend / 0 controller / 0 migration** across the milestone.

## Snapshot / restore

- Baseline `COUNT(*) FROM CoachCoacheeMappings` = **1** (Rustam Santiko → Rino, GAST).
- BACKUP → `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\HcPortalDB_Dev-pre390.bak`.
- All mutations (C1-C7 + W3) ran inside this one snapshot window.
- RESTORE WITH REPLACE → post-restore COUNT = **1 == baseline** ✓; disposable Iwan mapping gone (0); threshold
  reverted (full-DB restore). SEED_JOURNAL.md row Status `active`→**cleaned**; snapshot file deleted.

## Roundtrip results (Task 1 — live mutation)

| # | Action | Result |
|---|--------|--------|
| C1 | TAMBAH | ✅ Rustam→Iwan created (assign modal: coach + coachee-checkbox + auto Bagian/Unit + start date), badge 1→2, no 500 |
| C2 | EDIT | ✅ openEditModal 7-arg populated all fields (coachee/coach/bagian/unit/track/date); changed start date 17→20 Jun, persisted |
| C3 | NONAKTIFKAN | ✅ deactivateModal `#deactivateSessionInfo` preloaded ("Tidak ada sesi coaching aktif."), badge 2→1 |
| C6 | AKTIFKAN-KEMBALI | ✅ **after fix** — reactivated (IsActive=1), clean native alert (was Swal error, see Defect 1) |
| C4 | GRADUATED | ✅ (conditional) business-rule gated — "Coachee belum memiliki assignment Tahun 3" friendly error renders (TempData + form POST + redirect intact). Iwan has no Tahun-3 → correctly blocked. No local coachee is Tahun-3, so a green "Graduated" badge is not reachable locally. |
| C5 | HAPUS | ✅ deleteModal preview loaded (Coach: Rustam, Coachee: Iwan, 2 track assignments, 3 progress records — not "Memuat..."), submit "Hapus Permanen" → row removed from DOM + toast "Mapping berhasil dihapus." |
| W1 | FILTER | ✅ section=GAST → URL `?section=GAST`, table + export link filtered; Reset clears |
| W3 | SET THRESHOLD | ✅ modal pre-filled (max=5/warn=4) → changed to 6/5 → Simpan → reload, no 500, badges re-evaluated (1<5 stays Normal) |
| W4/W5 | SETUJUI/LEWATI | ⏭️ conditional-skip — 0 coach overloaded ("Semua coach seimbang" empty-state). Cannot force: only 3 eligible coachees < threshold. Documented (A3 limitation). |

## Manual import (Task 2 — C7)

Uploaded `tests/fixtures/import-mapping-390.xlsx` (A2=`123456` Iwan coach-slot / B2=`29007720` Rino coachee-slot) via
the real Import Excel flow inside the snapshot window. **Result: "Import gagal. Semua perubahan dibatalkan."** —
correct: NIP 123456 (Iwan) is not coach-eligible; the controller rejected and rolled back the whole batch
(all-or-nothing transaction). No 500, error banner renders. This is the Plan-01 caveat realized (no coach has a NIP
in the local DB). The import flow + validation + transactional rollback + error rendering are **parity-intact**;
a "Berhasil Dibuat" outcome is not reachable with local data.

## Regression sign-off (Task 3)

- `dotnet build` → **0 errors** (25 pre-existing nullable warnings).
- `dotnet test` → **482 passed / 0 failed / 0 skipped** (backend suite, no regression).
- `E2E_BASE_URL=http://localhost:5270 npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1`
  → **21 passed / 5 skipped / 0 FAILED** (W-EXP/W-THR/V-15/V-16 PASS; data-guard skips V-05/11/12/13 + 388 approve/skip). DB snapshot/restore clean each run.
- **0-backend / 0-migration confirmed** — `git diff --name-only 64456bd5(origin/ITHandoff, v31.0 base)..HEAD` touches
  ONLY `Views/ tests/ docs/ .planning/`. Zero files under Controllers/ Services/ Helpers/ Data/ Migrations/ Models/.
  Zero new migration files (migration=FALSE). Production views changed: `CoachCoacheeMapping.cshtml`,
  `CoachWorkload.cshtml`, `Results.cshtml` (all view-only).
- AJAX uses `appUrl()` (CoachCoacheeMapping, 9 fetches) / `(window.basePath||'')` (CoachWorkload, 3 fetches) — **0
  hardcoded `/Admin/` fetch paths** → PathBase-safe under sub-path deploy.

## Defects found (D-06)

### Defect 1 — reactivate toast `Swal is not defined` — FIXED inline (view-only)
`reactivateMapping` showAssignPrompt path called `Swal.fire`, but SweetAlert2 is not loaded on this page (not in
`_Layout`, not bundled; `Swal` used only in this view). Surfaced in C6 when reactivating a no-track coachee.
**Pre-existing** (389-02 froze `@section Scripts`; same at `7548c6d0~1`), not a 389 regression. Backend reactivate
succeeded; only the toast failed. Per D-06 (view-only) → replaced `Swal.fire` with a native `alert` + `location.reload`
(info preserved, no CDN dependency — safe for internal/air-gapped Dev/Prod). Commit `5b38f817`. Re-verified live:
deactivate→reactivate now shows clean native alert, mapping reactivated, no error.

### Defect 2 — import "Upload & Proses" never auto-enables — DOCUMENTED, DEFERRED to new v32.1 phase (user decision)
The file-change listener (`@section Scripts` line ~1038) is registered before `#importMappingFile` (line ~1059, in
the import modal lower in the same section) exists in the DOM → `?.addEventListener` no-ops → button stays disabled
→ import unusable via the normal UI. **Pre-existing** (identical arrangement at `7548c6d0~1`, before 389; confirmed
empirically — a native `change` dispatch did not enable the button). Not a 389 regression. Fix is view-only (event
delegation on `document`, or wrap registration in `DOMContentLoaded`). **User decision: document + defer to a NEW
phase within milestone v32.1** (do NOT fix in this parity phase). The C7 import test above was exercised by
force-enabling the button via evaluate (the bug only gates the click, not the underlying import flow).
→ ACTION: create a new v32.1 phase to fix the import-button enable (view-only, 0-backend).

## Commits

- `5b38f817` fix(390-02): reactivate toast Swal.fire → native alert (view-only, no CDN dep)
- (this summary + SEED_JOURNAL cleaned row committed with plan close)

## Self-Check: PASSED

- Roundtrip C1-C6 + W1/W3 executed (W4/W5 conditional-documented; C4 business-gated-documented).
- Manual import C7 exercised; error+rollback path verified.
- DB restored to baseline (COUNT=1), SEED_JOURNAL cleaned, snapshot deleted.
- build 0 / test 482 / specs 21-pass-0-fail; 0-backend/0-migration confirmed.
- Defect 1 fixed+re-verified; Defect 2 documented+deferred (new v32.1 phase).

## Milestone v32.1

Parity-clean and ready for **1 push → notify IT** (migration=FALSE). [notify-IT = closing note, out of execution
scope.] One follow-up phase queued: import-button enable fix (Defect 2, view-only).

---
*Phase: 390-test-uat-behavior-parity-dsn-06*
*Completed: 2026-06-17*
