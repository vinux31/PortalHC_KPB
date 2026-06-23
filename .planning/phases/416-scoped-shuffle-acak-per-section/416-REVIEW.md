---
phase: 416-scoped-shuffle-acak-per-section
reviewed: 2026-06-23T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Helpers/ShuffleEngine.cs
  - HcPortal.Tests/SectionScopedShuffleTests.cs
  - Controllers/CMPController.cs
  - Controllers/AssessmentAdminController.cs
  - Views/Admin/ManagePackageQuestions.cshtml
  - tests/e2e/scoped-shuffle.spec.ts
findings:
  critical: 0
  warning: 1
  info: 4
  total: 5
status: issues_found
---

# Phase 416: Code Review Report

**Reviewed:** 2026-06-23
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

Phase 416 (Scoped Shuffle — acak per-Section) refactors `ShuffleEngine.BuildQuestionAssignment` to partition questions by `SectionNumber` and run the existing distribution algorithm per-Section, with a new `BuildSectionAwareOptionShuffle` gating option-shuffle per-Section. Four assignment call-sites (StartExam, CreateEagerAssignmentsAsync, ReshufflePackage, ReshuffleAll) were rewired to load `q.Section` and use the section-aware option shuffle.

Overall the implementation is **correct and well-defended**. The key invariants hold:

- **Golden-order all-null path preserved (no RNG drift).** Partitioning (`Distinct().OrderBy().ThenBy()`) and slicing are pure non-RNG operations. For the all-null case there is exactly one section key (`LainnyaKey`), so the sub-function is invoked once over the full pool with `sectionShuffle = shuffleQuestions` — byte-identical to the pre-416 engine. Verified by the `GoldenOrderBaseline = {12, 21}` regression test (`SectionScopedShuffleTests.cs:90,104`).
- **No cross-section leak.** `SlicePackagesBySection` filters each package's `Questions` to the section key, and results are concatenated in section order. The contiguous-block invariant is asserted both at unit level (`ScopedShuffle_NoCrossSectionLeak`) and e2e (`assertContiguousSectionBlocks`).
- **"Lainnya" (null section) last (D-15).** `OrderBy(k => k == LainnyaKey)` (false sorts before true) places the sentinel `int.MinValue` group last despite being the smallest numeric value. Correct and covered (`SectionOrder_LainnyaAlwaysLast`).
- **Option-shuffle gated per-Section (D-416-01).** `BuildSectionAwareOptionShuffle` applies `parentShuffleOptions ∧ q.Section?.ShuffleEnabled`, with null section defaulting to parent (D-15). Grading is by `PackageOption.Id`, so option order never affects scoring — gating is purely presentational.
- **All four call-sites load `q.Section`** via `.Include(p => p.Questions).ThenInclude(q => q.Section)` and use `BuildSectionAwareOptionShuffle`. Worker-index computation is unchanged and uniform across StartExam / Eager / both reshuffle endpoints (drift-free).

The one substantive finding (WR-01) is the **known DEF-416-01**: the ET-coverage warning predicate `DistinctEt > K` is mathematically unreachable for single-package data, making the warning a dead feature. This is already documented in `deferred-items.md` with root-cause and a suggested re-spec, and is non-blocking by design — classified Warning (dead feature, not a correctness/security risk). Remaining items are Info-level (test/coverage notes and minor robustness observations).

## Warnings

### WR-01: ET-coverage warning predicate `DistinctEt > K` is unreachable — dead feature (DEF-416-01)

**File:** `Controllers/AssessmentAdminController.cs:7673-7680`
**Issue:** The warning is computed as:
```csharp
var qs = pkg.Questions.Where(q => q.SectionId == s.Id).ToList();
int k = qs.Count;
int distinctEt = qs.Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                   .Select(q => q.ElemenTeknis!).Distinct().Count();
// ...
}).Where(w => w.DistinctEt > w.K).ToList();
```
`distinctEt` is the count of *distinct* `ElemenTeknis` values drawn from the **same** `qs` set that also defines `k = qs.Count`. Each question carries at most one ET string, so `distinctEt ≤ qs.Count = K` is an algebraic invariant. Therefore `DistinctEt > K` can never be true on a single package, and the `.alert-warning` block in `ManagePackageQuestions.cshtml:192` (and its strongly-typed `SectionEtWarning` record) is effectively dead code. The phase's own e2e (`scoped-shuffle.spec.ts:336-397` S3, and the negative control S3b) confirm at runtime that the alert never renders.

This is **non-security, non-correctness** — the warning is purely an advisory nicety, and the load-bearing part of D-416-03 (warning is non-blocking; narrow sections still allow Kelola/Simpan/StartExam) is correct and runtime-verified. Classified **Warning** (dead feature shipped to production, not "Info" because it represents a feature that silently does nothing — a maintenance/expectation hazard).

**Fix:** As already noted in `deferred-items.md`, the intended semantic is almost certainly a cross-package pool ET count vs the *presented* quota `K`. Redefine `K` as the min question-count for that section across sibling packages (the quota the engine's Phase-1 actually presents), and `DistinctEt` as the distinct ET across the cross-package section pool:
```csharp
// Cross-package: pool questions of this SectionNumber across sibling packages.
// K = min(count per sibling package)  ← the quota Phase-1 presents
// DistinctEt = distinct ET across the whole pool
// warn when pool has more ET groups than the K slots can guarantee.
```
Until then, the dead UI/record could be removed to avoid implying coverage protection that does not exist. Track explicitly so it is not mistaken for working behavior in a future audit.

## Info

### IN-01: Engine partitions by `SectionNumber`; ET-warning groups by `SectionId` — keep semantics aligned if WR-01 is fixed

**File:** `Controllers/AssessmentAdminController.cs:7675` vs `Helpers/ShuffleEngine.cs:75,135`
**Issue:** The ET-warning groups questions by `q.SectionId == s.Id` (FK identity, per-package), while the ShuffleEngine partitions the cross-package pool by `q.Section?.SectionNumber` (logical section number, shared across sibling packages with distinct `AssessmentPackageSection.Id`). For the single-package warning view this is harmless today, but when WR-01 is re-specced to a cross-package pool the grouping key MUST switch to `SectionNumber` to match the engine's actual pooling — otherwise the recomputed warning would diverge from the runtime distribution.
**Fix:** When implementing the cross-package ET-coverage fix, key the pool on `SectionNumber` (via `SectionStructureComparer.KeyOf`) exactly as the engine does, not on per-package `SectionId`.

### IN-02: S4 live-add path is best-effort and can silently no-op — green does not prove AddParticipantsLive parity

**File:** `tests/e2e/scoped-shuffle.spec.ts:441-537`
**Issue:** S4's stated purpose is to prove `CreateEagerAssignmentsAsync` produces block-per-section assignments for live-added participants. However the live-add branch is wrapped in `try { ... } catch { /* soft-fail */ }` with multiple `.catch(() => false)` visibility guards, and the only hard assertion (`expect(order1.length).toBe(6)`) covers the *first* participant via `startExamAsParticipant` — i.e. the StartExam path already exercised by S1, not the eager path. `liveAssignmentVerified` is logged but never asserted. If the "Tambah Peserta" UI selector drifts or the picker changes, S4 passes while testing nothing new about `CreateEagerAssignmentsAsync`. This is a documented best-effort design (Pitfall 5), but it means the eager-assignment call-site at `AssessmentAdminController.cs:2557` has weaker e2e coverage than the other three.
**Fix:** Either (a) assert `liveAssignmentVerified === true` to make the eager path a hard requirement, or (b) add a small unit/integration test that calls the engine the way `CreateEagerAssignmentsAsync` does (it is pure) to lock block-per-section for the eager wiring deterministically. Low priority — engine uniformity makes drift unlikely, and unit suite already proves the engine.

### IN-03: Reshuffle endpoints use `Random.Shared` — re-roll cannot be reproduced, but isolation is what is tested

**File:** `Controllers/AssessmentAdminController.cs:6043,6123` and `Controllers/CMPController.cs:1129`
**Issue:** All call-sites pass `Random.Shared` to the engine. This is correct for the product (each worker/re-roll gets a fresh sequence), and section isolation is independent of the RNG (proved by `Reshuffle_SectionIsolation` across seeds 1/7/99/2026). Noting only that there is no seeded path in production, so any future "reproduce this worker's exact order" requirement would need the persisted `ShuffledQuestionIds` (which is stored) rather than a replayable seed. No action needed — the persisted assignment is the source of truth.

### IN-04: `ResolveSectionShuffle` and `BuildSectionAwareOptionShuffle` both default null/unloaded section to parent — correct, but relies on call-sites loading `q.Section`

**File:** `Helpers/ShuffleEngine.cs:163,199`
**Issue:** Both helpers fall back to `?? true` when `q.Section` is null. This intentionally means "if Section is not loaded, follow the parent flag" (safe global behavior). Correctness therefore depends entirely on every call-site eager-loading `q.Section` — which all four currently do (CMP `:1054`, Eager `:2550`, Reshuffle `:6029`, ReshuffleAll `:6111`). The risk is a *future* call-site forgetting the `.ThenInclude(q => q.Section)`: scoped-shuffle would silently degrade to global behavior with no error. The XML doc comments call this out explicitly (Pitfall 3), so this is well-mitigated documentation-wise.
**Fix:** None required now. If a regression guard is desired later, an integration test asserting a section-bearing package produces contiguous blocks at each call-site would catch a dropped Include. The existing e2e S1/S4 cover StartExam + (best-effort) eager; ReshufflePackage/ReshuffleAll rely on unit coverage only.

---

_Reviewed: 2026-06-23_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
