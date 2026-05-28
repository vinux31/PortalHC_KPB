# Phase 329: Fix Cascade Renewal Pre-Check Group Endpoints - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Source:** Phase 328 RESEARCH.md §4.4 + §4.5 (HIGH findings, D5 fail). Direct audit-derived fix phase — no separate research needed.

<domain>
## Phase Boundary

Phase 329 menambah **pre-check renewal chain (`RenewsSessionId`)** di 2 endpoint group delete yang Phase 325 P05 D-11 terlewat: `DeleteAssessmentGroup` + `DeletePrePostGroup`. Pattern paralel `DeleteAssessment` L2040-2052 (Phase 325 P05 post-commit `77a9c375`).

**Scope:**
- 1 file: `Controllers/AssessmentAdminController.cs`
- 2 method: `DeleteAssessmentGroup` (L2199-2353) + `DeletePrePostGroup` (L2359-2503)
- ~40 LoC delta total (20 per method)
- No migration, no schema change, no model change, no view change

**Purpose:** Fix HIGH severity bug — bila salah satu sibling/group session jadi renewal source untuk worker lain's TR/AS, current code throws raw FK NoAction violation 500 di tengah cascade. Fix: pre-check `RenewsSessionId` count di TR+AS sebelum buka tx scope, block dengan friendly TempData["Error"] jika count > 0.

Estimated effort: 1 sesi, 2 task plan (1 method per task) atau bundle 1 task multi-method.

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md

Lock seluruh design decision ke audit deliverable Phase 328 commit `41f1eef2`:
- §4.4 DeleteAssessmentGroup HIGH (D5 fail) — pre-check absent untuk `siblingIds`
- §4.5 DeletePrePostGroup HIGH (D5 fail) — pre-check absent untuk `groupIds`
- §8 Phase 323+325 hybrid pattern — verbatim DeleteAssessment L2040-2052 sebagai template

### D-02 Pattern Template — DeleteAssessment L2040-2052 verbatim

Insertion point untuk pre-check = SEBELUM `using var tx = await _context.Database.BeginTransactionAsync()`. Code shape (adapt id → siblingIds/groupIds via `Contains`):

```csharp
// Phase 329: pre-check referencing rows SEBELUM buka tx scope (fail-fast UX friendly).
// Paralel pola Phase 325 P05 DeleteAssessment L2040-2052.
var refTr = await _context.TrainingRecords
    .CountAsync(t => t.RenewsSessionId.HasValue && siblingIds.Contains(t.RenewsSessionId.Value));
var refAs = await _context.AssessmentSessions
    .CountAsync(a => a.RenewsSessionId.HasValue && siblingIds.Contains(a.RenewsSessionId.Value));

if (refTr + refAs > 0)
{
    var total = refTr + refAs;
    TempData["Error"] = $"Tidak bisa hapus grup: {total} sertifikat lain "
                      + "menggunakan salah satu sesi di grup ini sebagai sumber renewal. "
                      + "Hapus atau update sertifikat pemakai terlebih dulu.";
    return RedirectToAction("ManageAssessment");
}
```

**Substitusi per method:**
- DeleteAssessmentGroup: variable list = `siblingIds` (sudah ada L2228)
- DeletePrePostGroup: variable list = `groupIds` (sudah ada L2377)

### D-03 Insertion Point (Exact Line)

- **DeleteAssessmentGroup:** Insert SEBELUM L2231 `using var tx = await _context.Database.BeginTransactionAsync()`. Setelah L2228 `var siblingIds = siblings.Select(s => s.Id).ToList();` — variabel sudah tersedia.
- **DeletePrePostGroup:** Insert SEBELUM L2382 `using var tx = await _context.Database.BeginTransactionAsync()`. Setelah L2377 `var groupIds = groupSessions.Select(s => s.Id).ToList();` — variabel sudah tersedia.

### D-04 Catch Block Refactor (Opt-in bundle)

Phase 328 §4.4 + §4.5 juga catat **D6 ⚠️** karena kedua endpoint pakai `catch (Exception ex)` generic, bukan `catch (DbUpdateException ex)` specific. Sebagai bonus same-LOC-budget, tambah `catch (DbUpdateException dbEx) { await tx.RollbackAsync(); ... }` BEFORE generic catch — paralel `DeleteAssessment` L2180.

Kalau di-eksekusi: ~10 LoC tambahan per method, total +20 LoC. Bundle dalam scope Phase 329.

### D-05 No Migration, No Schema Change

Audit-derived bug fix murni controller-layer. Tidak ada model/view/migration touch. FK constraint `RenewsSessionId NoAction` sudah ada di `Data/ApplicationDbContext.cs:220-228`.

### D-06 Test Strategy — Manual Playwright + dotnet test

- **Auto test:** `dotnet test` existing suite must remain 10/10 pass (Phase 325 baseline).
- **Manual Playwright UAT:** 2 skenario seed-data based per `docs/SEED_WORKFLOW.md`:
  - UAT-329-01: Group dengan 1 session jadi RenewsSessionId source → `DELETE /DeleteAssessmentGroup/{id}` should BLOCK dengan friendly error (BUKAN FK 500).
  - UAT-329-02: PrePost group dengan PreTest session jadi RenewsSessionId source → `DELETE /DeletePrePostGroup/{linkedGroupId}` should BLOCK.
- **Regression:** UAT smoke Phase 326+327 (validator P03+P06 + TimezoneDateOnly) — endpoint terpisah, low risk.

### D-07 IT_NOTIFY: Bundle dengan v19.0 batch push

Phase 329 SHIPPED LOCAL, JANGAN push standalone. Bundle dengan batch v19.0 (325+326+327+328+329+330) saat user release push lock per Phase 327 option-b hold. Append entry ke `docs/IT_NOTIFY.md` existing.

### D-08 Acceptance Criteria (Locked)

1. `DeleteAssessmentGroup` L2231 sebelumnya = `using var tx`, sekarang sebelumnya = pre-check block dengan `siblingIds` Contains pattern.
2. `DeletePrePostGroup` L2382 sebelumnya = `using var tx`, sekarang sebelumnya = pre-check block dengan `groupIds` Contains pattern.
3. `dotnet build` clean. `dotnet test` 10/10 pass (no regression).
4. Repro path: seed 2 session group + 1 child TR.RenewsSessionId → POST delete grup → response = redirect dengan TempData["Error"] friendly (BUKAN 500 FK error).
5. Audit log untuk DeleteAssessmentGroup + DeletePrePostGroup tetap fire ✅ (unchanged).
6. Plan checker iteration ≤ 2.
7. Commit message format: `feat(329): cascade renewal pre-check DeleteAssessmentGroup + DeletePrePostGroup`.
8. SUMMARY.md generated.

### D-09 Scope Discipline — Do NOT Touch

- ❌ Jangan ubah `DeleteAssessment` (L2011, sudah gold standard Phase 325 P05).
- ❌ Jangan refactor cascade RemoveRange block (out-of-scope, scope ke Phase 330+ bundle nanti).
- ❌ Jangan ubah view `Views/Admin/*ManageAssessment*.cshtml` (UI tidak berubah, hanya error message text).
- ❌ Jangan tambah migration, schema field, atau new model.

### D-10 Plan Structure — Single Plan, Bundle 2 Method

1 PLAN.md (`329-01-PLAN.md`) dengan 4 task:
- Task 1: Apply pre-check di `DeleteAssessmentGroup` (~20 LoC, single Edit operation)
- Task 2: Apply pre-check di `DeletePrePostGroup` (~20 LoC)
- Task 3: Optional catch refactor (DbUpdateException specific) — D-04 bundle
- Task 4: Verify `dotnet build` + `dotnet test` + manual UAT seed-data + commit + SUMMARY

Tidak perlu split jadi 2 plan karena scope kecil + dependency linear.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §4.4 + §4.5 + §8 — HIGH findings + verbatim DeleteAssessment template
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-01-SUMMARY.md` — fix proposal Section 9 row 2

### Gold Standard Reference (Pattern Source)
- `Controllers/AssessmentAdminController.cs` L2040-2052 `DeleteAssessment` pre-check block (Phase 325 P05 commit `77a9c375`)
- `Controllers/AssessmentAdminController.cs` L2180 `catch (DbUpdateException ex)` block (D-04 bonus refactor reference)

### Target Files (Phase 329 Modifies)
- `Controllers/AssessmentAdminController.cs` L2199-2353 `DeleteAssessmentGroup`
- `Controllers/AssessmentAdminController.cs` L2359-2503 `DeletePrePostGroup`

### FK Definitions (Cross-Ref)
- `Data/ApplicationDbContext.cs:220-228` — `AssessmentSession.RenewsSessionId` NoAction FK definition
- `Data/ApplicationDbContext.cs:157-165` — `TrainingRecord.RenewsSessionId` NoAction FK definition (cross-table)

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default, dev workflow, seed workflow
- `docs/SEED_WORKFLOW.md` — temporary seed data SOP untuk manual UAT (D-06)

### v19.0 Milestone Context
- `.planning/phases/325-*/325-01..05-PLAN.md` — Phase 325 P05 pre-check pattern origin
- `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` — parent audit spec (commit `02f620be`)
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 329 entry post-ship)

</canonical_refs>

<specifics>
## Specific Ideas

- **Exact code shape:** Copy L2040-2052 verbatim, substitute `id` → `siblingIds` (Group) or `groupIds` (PrePostGroup) via `Contains` clause.
- **Friendly error message:** Customize sedikit dari L2048 (singular "sertifikat ini" → plural "salah satu sesi di grup ini") untuk UX precision.
- **Variable naming:** `refTr` + `refAs` paralel L2040 L2043, JANGAN bikin nama baru.
- **Commit shape:** `feat(329): cascade renewal pre-check DeleteAssessmentGroup + DeletePrePostGroup` — bukan `fix(...)` karena ini new defensive code (pre-check yang sebelumnya tidak ada), bukan fix bug behavior existing.
- **No new test file:** `tests/` directory existing suite hanya re-run. Seed-based manual UAT cukup karena pattern sudah teruji Phase 325.

</specifics>

<deferred>
## Deferred Ideas

- ❌ Auto regenerate Phase 328 RESEARCH §3 audit table (severity HIGH untuk row #3 + #4 sekarang downgrade ke NONE post-Phase-329). DEFERRED — audit deliverable frozen-in-time per D-10 audit. Update saat audit refresh berikutnya (v20.0+ kalau ada).
- ❌ Tambah pre-check serupa di endpoint lain (Worker, Bagian, Kompetensi) — scope phase fix terpisah (P-330+).
- ❌ Test fixture seed-based untuk UAT-329 automated — deferred ke Phase 330+ bila stakeholder request test infra build-out.
- ❌ Catch refactor di DeleteAssessment (L2011) — sudah gold standard, no-op.

</deferred>

---

*Phase: 329-fix-cascade-deleteassessmentgroup-deleteprepostgroup-renewal*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD)*
*Source: Phase 328 RESEARCH commit `41f1eef2` §4.4 + §4.5 + §8*
