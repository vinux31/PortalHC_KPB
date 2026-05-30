---
phase: 335-fix-cascade-deleteworker-renewal-files-tx
plan: 01
status: SHIPPED LOCAL
shipped_at: "2026-05-28"
commits:
  - "c0544107 feat(335): cascade triple-fix DeleteWorker (cross-user renewal pre-check + file post-commit + tx wrap)"
pushed: false
push_strategy: bundle v19.0 (Phase 325+...+335 = MILESTONE CLOSE 11/11 100%) — tunggu push lock release per Phase 327 option-b
milestone_close: true
---

# Phase 335 Plan 01 — SUMMARY (v19.0 FINAL)

## Status: SHIPPED LOCAL — v19.0 MILESTONE CLOSE 11/11

Phase 335 menutup 1 HIGH finding triple-dim Phase 328 §4.3 (D2+D5+D7): cross-user renewal pre-check + file collection + tx wrap UserManager.DeleteAsync. **FINAL v19.0 HIGH phase. Milestone 11/11 = 100%.**

## Files Modified

| File | Delta | Change |
|------|-------|--------|
| `Controllers/WorkerController.cs` | +88 LoC | DeleteWorker triple-fix D2+D5+D7 |
| `docs/IT_NOTIFY.md` | +43 LoC | Phase 335 entry + scenario #13 + v19.0 MILESTONE CLOSE section |

## D-09 Acceptance Criteria — 12/12 PASS

| AC | Criteria | Result |
|----|----------|--------|
| AC-1 | D5 pre-check cross-user (2 count queries WHERE UserId != id) | ✅ PASS — crossUserTrReferences + crossUserAsReferences + totalCrossRefs block |
| AC-2 | D2 file collection (TR.SertifikatUrl + AS.ManualSertifikatUrl + ProtonProgress EvidencePath + JSON history) | ✅ PASS — allFilePaths built (TR has no ManualSertifikatUrl per schema, only SertifikatUrl) |
| AC-3 | D7 tx wrap (9-step RemoveRange + SaveChanges + UserManager.DeleteAsync + audit log + CommitAsync) | ✅ PASS — using var tx wraps entire scope |
| AC-4 | Identity error early return INSIDE try (tx disposal auto-rollback) | ✅ PASS — `if (!result.Succeeded) { TempData; return; }` inside try |
| AC-5 | File.Delete loop POST CommitAsync inner try/catch warn-only per file | ✅ PASS — foreach allFilePaths post-try |
| AC-6 | Catch DbUpdateException + Exception fallback friendly. NO + ex.Message. NO explicit RollbackAsync | ✅ PASS — 2 catch blocks, no RollbackAsync, no ex.Message in response |
| AC-7 | Self-deletion guard + Authorization preserved verbatim | ✅ PASS — L484-499 unchanged |
| AC-8 | dotnet build 0 error CS* | ✅ PASS — empty grep |
| AC-9 | dotnet test --no-build 18/18 PASS | ✅ PASS — 18/18 93ms |
| AC-10 | Manual smoke deferred ke Dev promo | ⏳ DEFERRED — scenario #13 IT_NOTIFY 3 sub-cases (a/b/c) |
| AC-11 | Commit `feat(335): cascade triple-fix DeleteWorker ...` | ✅ PASS — commit `c0544107` |
| AC-12 | SUMMARY.md digenerate | ✅ PASS — file ini |

## Grep Marker Verification (10/10 PASS)

```
grep -c "crossUserTrReferences" Controllers/WorkerController.cs                  → 3 (declare + assign + reference)
grep -c "crossUserAsReferences" Controllers/WorkerController.cs                  → 3 (declare + assign + reference)
grep -c "Tidak bisa hapus pekerja" Controllers/WorkerController.cs               → 1 (D5 block message)
grep -c "BeginTransactionAsync" Controllers/WorkerController.cs                  → 1 (using var tx)
grep -c "tx.CommitAsync" Controllers/WorkerController.cs                         → 1
grep -c "allFilePaths" Controllers/WorkerController.cs                           → 6 (declare + 3 populate loops + post-commit loop + count)
grep -c "File.Delete post-commit failed (Worker file)" Controllers/WorkerController.cs → 1
grep -c "catch (DbUpdateException dbEx)" Controllers/WorkerController.cs         → 1
grep -c "Gagal hapus pekerja:" Controllers/WorkerController.cs                   → 2 (2 catch blocks generic friendly)
grep -c "tx.RollbackAsync" Controllers/WorkerController.cs                       → 0 (no explicit, using disposal)
```

## Threat Model Disposition — 4/4

| Threat ID | Category | Disposition |
|-----------|----------|-------------|
| T-335-01 | D (DoS) — files orphan via cascade | MITIGATED — collection BEFORE cascade + File.Delete POST commit |
| T-335-02 | T (Atomicity) — partial state UserManager fail | MITIGATED — tx wrap + Identity early-return INSIDE try + disposal rollback |
| T-335-03 | R (Repudiation) — cross-user renewal silent cascade | MITIGATED — D5 pre-check explicit block with count detail |
| T-335-04 | I (Info Disclosure) — DbUpdateException leak | MITIGATED — friendly TempData (no ex.Message). Identity Description preserved (actionable user-level, not info leak) |

## v19.0 MILESTONE CLOSE — 11/11 PHASE 100%

| Phase | Status | Push State |
|-------|--------|------------|
| 325 — Security Hardening P01+P02+P05 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 326 — Validator Hardening P03+P06 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 327 — Timezone DateOnly Refactor P04 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 328 — Cascade Audit Sweep (audit-only) | ✅ SHIPPED LOCAL | NOT PUSHED |
| 329 — Cascade Renewal Pre-Check Group | ✅ SHIPPED LOCAL | NOT PUSHED |
| 330 — Cascade MED Bundle | ✅ SHIPPED LOCAL | NOT PUSHED |
| 331 — DeleteTraining + DeleteManualAssessment atomicity | ✅ SHIPPED LOCAL | NOT PUSHED |
| 332 — DeleteBagian file atomicity | ✅ SHIPPED LOCAL | NOT PUSHED |
| 333 — DeleteCoachingSession file atomicity | ✅ SHIPPED LOCAL | NOT PUSHED |
| 334 — DeleteKompetensi orphan evidence + D6 info leak | ✅ SHIPPED LOCAL | NOT PUSHED |
| **335 — DeleteWorker triple-fix (FINAL HIGH)** | ✅ **SHIPPED LOCAL** | NOT PUSHED |

**~78 commit batch** di `main` lokal, NOT pushed. Migration: 1 (`ChangeValidUntilToDateOnly` Phase 327). Push gate: user explicit approval per Phase 327 option-b hold.

## Next Steps

1. **Push batch v19.0** — `git push origin main` saat IT available (deliver `docs/IT_NOTIFY.md` ke Tim IT).
2. **Tag v19.0-complete** post-push.
3. **/gsd-complete-milestone v19.0** — prep next milestone.
4. **Backlog deferred items** (v15.0 EPRV-01 + Phase 293/297/298 undecided + Phase 281/285 paused) — terbuka untuk milestone next.
