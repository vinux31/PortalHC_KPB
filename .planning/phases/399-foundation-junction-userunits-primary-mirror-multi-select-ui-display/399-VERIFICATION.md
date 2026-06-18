---
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
verified: 2026-06-18T07:05:00Z
status: human_needed
score: 6/6 must-haves verified
overrides_applied: 0
human_verification:
  - test: "MU-07 modal coach-mapping (browser end-to-end)"
    expected: "Edit pekerja multi-unit dengan CoachCoacheeMapping aktif → hapus unit ber-mapping → submit → modal 'Konfirmasi Penghapusan' + tombol 'Ya, Hapus & Nonaktifkan' muncul; klik confirm → mapping.IsActive=false + EndDate ter-set + unit terhapus (1 tx); audit mencatat event deactivate."
    why_human: "Playwright W-09 di-skip fixture-guarded (tidak ada seed deterministik pekerja multi-unit + coach-mapping aktif). Server guard logic (EvaluateRemoveUnitGuardAsync) sudah GREEN di RemoveUnitGuardTests 5/5 dan modal markup ter-verifikasi di EditWorker.cshtml, tapi jalur penuh UI→POST→modal→confirm→deactivate belum pernah dijalankan end-to-end (browser atau e2e). Hanya potongan ini yang tanpa bukti runtime menyeluruh."
  - test: "MU-07 PROTON hard-block (browser end-to-end)"
    expected: "Edit pekerja dengan ProtonTrackAssignment aktif → hapus unit ber-PROTON (resolved via AssignmentUnit ?? oldPrimary) → submit → error merah 'Tidak bisa menghapus Unit ... PROTON tahun-berjalan aktif' di validation summary; form TIDAK tersimpan."
    why_human: "Sama dengan di atas — guard server unit-tested (RemoveUnitGuardTests Blocked 2/2) + markup validation-summary ada, tapi rendering error merah di browser saat submit belum pernah dikonfirmasi visual/e2e (fixture PROTON-aktif absen)."
  - test: "_PSign cetak + render visual badge lintas surface"
    expected: "Kartu tanda tangan _PSign menampilkan SEMUA unit primary-first comma-join (BUKAN primary-only); badge primary hijau+bintang+'Utama' tampil benar (kontras, posisi) di Profile/Settings/WorkerDetail/ManageWorkers/Home hero."
    why_human: "Render visual cetak/badge (warna, kontras hero gelap, layout cell wrap) tak dapat diverifikasi via grep/DOM-assert sepenuhnya; Playwright D-07 mengecek isi teks .psign-label tapi bukan tampilan cetak final. Appearance = inherently human."
---

# Phase 399: Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display Verification Report

**Phase Goal:** Admin/HC dapat menetapkan >1 Unit (semua dalam 1 Bagian) pada akun pekerja, dengan tepat 1 unit PRIMARY ter-mirror ke `ApplicationUser.Unit`, dan seluruh unit pekerja tampil di semua surface — di atas fondasi tabel junction `UserUnits` (migration + backfill) yang menjaga setiap unit anak Bagian pekerja.
**Verified:** 2026-06-18T07:05:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #   | Truth (ROADMAP Success Criteria + load-bearing must-haves) | Status     | Evidence |
| --- | ---------------------------------------------------------- | ---------- | -------- |
| 1   | Migration `AddUserUnitsTable` ada + backfill 1-primary/pekerja idempotent (MU-05) | ✓ VERIFIED | Migration file substantive (CreateTable + 2 index + backfill `WHERE NOT EXISTS`); recorded di `__EFMigrationsHistory`; DB: 6 UserUnits / 6 IsPrimary == 6 users non-null Unit, 0 users >1 primary, 0 rows untuk null-unit users |
| 2   | Filtered-unique `(UserId) WHERE [IsPrimary]=1` enforce tepat-1-primary di DB-level | ✓ VERIFIED | DbContext `HasFilter("[IsPrimary] = 1")` :352; DB index `IX_UserUnits_UserId_PrimaryUnique` filter `([IsPrimary]=(1))` is_unique=1 hidup; + `IX_UserUnits_UserId_Unit_Unique` |
| 3   | Write-through mirror: `SyncUserUnitsAsync` = satu-satunya seam (Create/Edit/Import), `user.Unit = primary`, set-diff audit, scalar anti-pattern dihapus (MU-01/02) | ✓ VERIFIED | Helper :82-112 (replace-set + mirror + set-diff); dipanggil Create :453 / Edit :651 / Import :1286; `if(user.Unit != model.Unit)` hilang (hanya komentar :584); DB mirror_mismatch=0 |
| 4   | Multi-select widget: `initSectionUnitMultiCascade` checkbox-list + primary radio di Create/Edit; POST bind Units + PrimaryUnit (MU-01) | ✓ VERIFIED | `initSectionUnitMultiCascade` :71 shared-cascade.js (state machine substantive); `name="Units"`/`name="PrimaryUnit"` :169-170; `#unitMultiContainer` + `role="group"` di kedua view; `initSectionUnitCascade`+`togglePassword` utuh |
| 5   | Import multi-unit pipe (Cell(6), first=primary, per-unit validasi, backward-compat) (MU-04) | ✓ VERIFIED | `ParseUnitCell` :51 split+trim+dedup; Import `row.Cell(6)` :1185 → `ParseUnitCell`; per-unit validasi ∈ Bagian :1218-1229; Export primary-first comma-join :342-345 (Cell 7) |
| 6   | MU-07 guard asimetris: PTA aktif → hard-block; CoachCoacheeMapping aktif → confirm→auto-deactivate (1 tx); resolve via AssignmentUnit ?? oldPrimary (MU-07) | ✓ VERIFIED | `EvaluateRemoveUnitGuardAsync` :127-163 (4 outcome); dipanggil EditWorker POST :598; tx-atomic BeginTransactionAsync :650 (Sync+UpdateAsync+deactivate+Commit); RemoveUnitGuardTests 5/5 GREEN |
| 7   | Display SEMUA unit (primary ditandai) 7 surface incl _PSign all-units D-07 (MU-03) | ✓ VERIFIED | Profile/Settings/WorkerDetail/ManageWorkers/Home render badge bg-success+bi-star-fill+"Utama" primary-first; _PSign all-units comma-join (BUKAN primary-only); Excel kolom 7; AccountController inject `_context` + populate |

**Score:** 6/6 ROADMAP Success Criteria verified (truth #1-2 = SC4/SC6; #3 = SC2/SC4; #4 = SC1; #5 = SC3; #6 = SC5; #7 = SC1/SC3)

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Models/UserUnit.cs` | Entity junction (Id/UserId/Unit/IsPrimary/IsActive/nav) | ✓ VERIFIED | 32 lines, `[MaxLength(200)]` on Unit, `IsPrimary` bool, nav User |
| `Data/ApplicationDbContext.cs` | DbSet + filtered-unique config | ✓ VERIFIED | `DbSet<UserUnit>` :35; `HasFilter("[IsPrimary] = 1")` :352; both indexes named |
| `Migrations/20260618045427_AddUserUnitsTable.cs` | CreateTable + filtered index + idempotent backfill | ✓ VERIFIED | FK CASCADE, filter index, `NOT EXISTS` backfill, DropTable Down; applied to DB |
| `Controllers/WorkerController.cs` | SyncUserUnitsAsync + wiring + MU-07 guard + import/export | ✓ VERIFIED | All 4 static helpers present; wired Create/Edit/Import; authz/CSRF intact |
| `Models/ManageUserViewModel.cs` | Units/PrimaryUnit/ConfirmedDeactivate/ImpactedMappings | ✓ VERIFIED | Build references confirm; Section tetap scalar |
| `wwwroot/js/shared-cascade.js` | initSectionUnitMultiCascade state machine | ✓ VERIFIED | Function :71 substantive; existing functions preserved |
| `Views/Admin/CreateWorker.cshtml` + `EditWorker.cshtml` | Widget + MU-07 modal | ✓ VERIFIED | `#unitMultiContainer`; `id="unitSelect"` removed; modal `ConfirmedDeactivate=true` |
| `Controllers/AccountController.cs` | Inject _context + populate Units | ✓ VERIFIED | `ApplicationDbContext _context` :21; `_context.UserUnits` Profile :155 / Settings :204 |
| `Views/Shared/_PSign.cshtml` | All-units primary-first comma-join (D-07) | ✓ VERIFIED | `Model.Units` + OrderByDescending + string.Join; scalar fallback retained |
| `Views/Admin/WorkerDetail.cshtml` | Multi-unit badge (primary highlighted) | ✓ VERIFIED | `bi-star-fill` + "Utama" + OrderByDescending from ViewBag.WorkerUnits |

### Key Link Verification

| From | To  | Via | Status | Details |
| ---- | --- | --- | ------ | ------- |
| WorkerController Create/Edit/Import | UserUnits + ApplicationUser.Unit mirror | SyncUserUnitsAsync(context, user, units, primary) | ✓ WIRED | 3 call sites (:453/:651/:1286); helper sets user.Unit=primary |
| WorkerController EditWorker MU-07 guard | ProtonTrackAssignment + CoachCoacheeMapping aktif | AnyAsync IsActive → block/auto-deactivate | ✓ WIRED | EvaluateRemoveUnitGuardAsync queries both; EditWorker :598 dispatches block/confirm/deactivate |
| Create/EditWorker.cshtml | WorkerController POST | name="Units" checkboxes + name="PrimaryUnit" radio | ✓ WIRED | JS emits both names; MVC model-binding to List<string> Units + PrimaryUnit |
| AccountController Profile/Settings GET | UserUnits table | _context.UserUnits.Where(UserId) → VM.Units + nested PSign | ✓ WIRED | :155/:204; nested PSign.Units populated → _PSign all-units branch active |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| Profile/Settings/_PSign | Model.Units / PrimaryUnit | `_context.UserUnits.Where(uu => uu.UserId == user.Id)` (real DB query) | Yes (DB: 6 rows backfilled) | ✓ FLOWING |
| ManageWorkers cells | ViewBag.UserUnitsDict | batch-load `_context.UserUnits.Where(listUserIds.Contains)` :224 | Yes (real query, no static return) | ✓ FLOWING |
| WorkerDetail | ViewBag.WorkerUnits | `_context.UserUnits` in WorkerDetail GET | Yes | ✓ FLOWING |
| Home hero | Model.CurrentUserUnits | HomeController `_context.UserUnits` populate | Yes | ✓ FLOWING |
| Multi-select widget | ViewBag.SectionUnitsJson | `GetSectionUnitsDictAsync()` (real org-tree query) | Yes | ✓ FLOWING |

No HOLLOW/DISCONNECTED artifacts found — all display surfaces trace to real `UserUnits` queries, backfill seeded 6 real rows.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Full unit suite green | `dotnet test --filter Category!=Integration` | 366 passed, 0 failed, 0 skipped | ✓ PASS |
| 6 MU logic classes | `dotnet test --filter WriteThrough\|PrimaryMirror\|AuditDiff\|UnitInSectionValidation\|RemoveUnitGuard\|ImportMultiUnitParse` | 19 passed, 0 failed | ✓ PASS |
| MU-07 guard logic (4 outcomes) | RemoveUnitGuardTests | 5/5 (Blocked×2, NeedConfirm, Deactivated, Allowed) | ✓ PASS |
| Main build | `dotnet build HcPortal.csproj` | Build succeeded, 0 Error | ✓ PASS |
| Migration applied | DB `__EFMigrationsHistory` query | `20260618045427_AddUserUnitsTable` present | ✓ PASS |
| Backfill invariant | DB count query | 6/6 primary, 0 >1-primary, 0 null-unit rows, 0 mirror mismatch | ✓ PASS |
| Filtered-unique index live | DB `sys.indexes` query | `IX_UserUnits_UserId_PrimaryUnique` filter `([IsPrimary]=(1))` is_unique=1 | ✓ PASS |
| MU-07 modal full browser round-trip | Playwright W-09 | SKIP (fixture-guarded) | ? SKIP → human |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| MU-01 | 02, 03 | Multi-select >1 Unit (1 Bagian), cascade Bagian→unit | ✓ SATISFIED | Widget + write-through; round-trip Playwright W-06 PASS |
| MU-02 | 01, 02 | Tepat 1 PRIMARY + mirror write-through + recompute + set-diff audit | ✓ SATISFIED | SyncUserUnitsAsync + filtered-unique + WriteThrough/PrimaryMirror/AuditDiff tests GREEN; DB mirror_mismatch=0 |
| MU-03 | 04 | Seluruh unit tampil 7 surface (primary ditandai) incl _PSign | ✓ SATISFIED | 5 HTML badge + _PSign all-units + Excel; multiunit-display 8/8 |
| MU-04 | 02 | Bulk Import multi-unit (pipe), validasi tiap unit ∈ Bagian | ✓ SATISFIED | ParseUnitCell + Cell(6) + per-unit validasi; ImportMultiUnitParse 3/3 |
| MU-05 | 01, 02 | Migration backfill 1-primary/pekerja + junction-write validasi Unit∈Bagian | ✓ SATISFIED | Migration applied + DB verified; ValidateUnitsInSection + tests GREEN |
| MU-07 | 02 | Hapus unit dirujuk PTA/mapping aktif → block/deactivate (anti-orphan) | ✓ SATISFIED | EvaluateRemoveUnitGuardAsync + EditWorker tx; RemoveUnitGuard 5/5 (browser round-trip → human, see below) |

No ORPHANED requirements: REQUIREMENTS.md maps exactly MU-01/02/03/04/05/07 to Phase 399 (MU-06 → Phase 400, correctly Pending). All 6 accounted for in plans + verified.

### Scope Discipline

| Check | Status | Evidence |
| ----- | ------ | -------- |
| No MU-06 (set-aware listing) leaked | ✓ CLEAN | ManageWorkers `unitFilter` still scalar `u.Unit == unitFilter` :202-204 (comment: set-aware = Phase 400) |
| Section authz untouched | ✓ CLEAN | `IsResultsAuthorized(...user.Section...)` unchanged (CMPController :2503) |
| No PSU/CXU/ORG/QA work | ✓ CLEAN | Only MU-* helpers/views/migration touched; no PROTON/coaching/org-cascade changes |
| Single migration | ✓ CLEAN | Only `20260618045427_AddUserUnitsTable` is new (matches ROADMAP Migration:TRUE) |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs | 76/83/90 | `Assert.True(false, msg)` skip-stubs (xUnit2020 warn) | ℹ️ Info | Integration test scaffold (Category=Integration, excluded from suite); SQL-riil fixture, by-design skip when SQLEXPRESS-test absent — NOT a stub in production path |
| _PSign.cshtml / Profile etc. | — | Scalar `Model.Unit` fallback branch | ℹ️ Info | Intentional backward-compat for callers not yet populating Units (documented D-09); Profile/Settings DO populate Units so all-units branch active — not a stub |

No 🛑 Blocker or ⚠️ Warning anti-patterns. No `return null` / placeholder / hardcoded-empty in the goal path. All display surfaces trace to real DB data (Level 4 FLOWING).

### Human Verification Required

Automated checks all passed (6/6 truths VERIFIED, all artifacts substantive + wired + data flowing, build/tests/DB green). Three items need human confirmation because they involve full browser end-to-end flows the automated suite could not exercise:

1. **MU-07 modal coach-mapping (browser end-to-end)** — Server guard logic GREEN in RemoveUnitGuardTests (5/5) + modal markup verified, but the full UI→POST→modal→confirm→deactivate path was never run end-to-end (Playwright W-09 fixture-skipped). The risk is low (security-load-bearing server guard is tested), but the modal trigger + confirm→deactivate round-trip lacks runtime proof.

2. **MU-07 PROTON hard-block (browser end-to-end)** — Guard `Blocked` outcome unit-tested (2/2), validation-summary markup present, but the red-error rendering on submit in-browser was never confirmed (PROTON-active fixture absent).

3. **_PSign cetak + visual badge appearance** — Playwright D-07 asserts `.psign-label` text content (all-units comma-join), but visual rendering of the print card and badge contrast/layout (especially Home hero on dark gradient, ManageWorkers cell wrapping) is inherently human-verifiable.

### Gaps Summary

No blocking gaps. The phase goal is achieved at the code/data level: junction table exists + backfilled + migration applied (verified directly against the live SQLEXPRESS DB, not just SUMMARY claims), write-through mirror is the single seam with the scalar anti-pattern removed, the multi-select widget binds correctly, import/export handle multi-unit, the MU-07 guard is wired with transactional atomicity, and all 7 display surfaces render all units with primary marking from real data.

The only items not provable by automation are the three browser/visual flows above — chiefly the MU-07 modal end-to-end round-trip (W-09 fixture-skip). This is an **acceptable gap for automated verification** because the load-bearing server-side guard is fully unit-tested (RemoveUnitGuardTests 5/5 covering all 4 outcomes) and the markup is present; only the UI glue/visual lacks e2e coverage. Per the GSD gate rules, the presence of any human-verification item forces status `human_needed` (not `passed`), even though all automated truths are VERIFIED.

**Note for milestone push:** migration=TRUE (`fc015f4d` / `20260618045427_AddUserUnitsTable`) — notify IT with commit hash + migration flag. This is the only migration in v32.3. Branch ITHandoff, not yet pushed (consistent with CLAUDE.md Develop Workflow — IT owns Dev/Prod promotion).

---

_Verified: 2026-06-18T07:05:00Z_
_Verifier: Claude (gsd-verifier)_
