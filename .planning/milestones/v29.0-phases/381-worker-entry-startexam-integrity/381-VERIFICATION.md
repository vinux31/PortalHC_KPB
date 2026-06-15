---
phase: 381-worker-entry-startexam-integrity
verified: 2026-06-15T08:00:00+08:00
status: passed
score: 9/9
overrides_applied: 0
---

# Phase 381: Worker Entry (StartExam Integrity) — Verification Report

**Phase Goal:** Worker masuk ujian dengan paket yang benar — Pre & Post yang dijadwalkan same-day tidak saling memungut paket, dan admin yang membuka/impersonate ujian worker tidak memulai ujian atau membakar waktu/mengunci shuffle worker.
**Verified:** 2026-06-15T08:00:00+08:00
**Status:** passed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Worker StartExam sesi Pre menerima HANYA soal paket Pre (Post tak tercampur) | VERIFIED | `SiblingPrePostAwarePredicate` dipakai di StartExam GET ~1017; e2e WSE-04 assert `qcards.count() == 3` (Pre-only) + `not.toContain('POST-Q')` — live green |
| 2 | Worker StartExam sesi Post menerima HANYA soal paket Post (Pre tak tercampur) | VERIFIED | Sama predicate (isPrePost branch: `s.AssessmentType == assessmentType`); mirror-test opsional ditandai di spec; unit Test 2 (PostCaller_IsolatesPostTest) membuktikan logika |
| 3 | Normal exam (AssessmentType='Standard'/''/null) menerima soal yang sama seperti sebelum fix (zero behavior-change) | VERIFIED | D-09 non-PrePost group: `s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest"` — Standard/''/null satu grup; unit Test 3+4 membuktikan; existing xUnit 391/391 no regression |
| 4 | Reshuffle (ReshufflePackage/ReshuffleAll) menghasilkan workerIndex identik dengan StartExam untuk sesi type yang sama (determinisme Phase 373 terjaga) | VERIFIED | ReshufflePackage `~5192`: pakai `SiblingPrePostAwarePredicate` identik; ReshuffleAll `~5273`: `SiblingKey` + `sortedByKey` dict + `sessionPackages` per-type — deviation "full parity" user-approved, menutup jalur resume terkontaminasi |
| 5 | Admin impersonate buka StartExam worker (Open, StartedAt==null, non-token) tidak ada mutasi DB: StartedAt tetap null, Status tetap Open | VERIFIED | Guard write-site 1 (line 993): `if (justStarted && !_impersonationService.IsImpersonating())`; e2e WSE-05 DB assert queryScalar `StartedAt IS NULL` = 1, `Status='Open'` count = 1 — live green |
| 6 | Admin impersonate tidak ada UserPackageAssignment dibuat, tidak ada SignalR workerStarted, tidak ada LogActivity 'started' | VERIFIED | Guard write-site 2 (line 1002): `if (justStarted && !_impersonationService.IsImpersonating())` — SignalR + LogActivity tidak fire; guard write-site 3 (line 1078): `if (!_impersonationService.IsImpersonating())` persist diblokir; e2e WSE-05 assert `UserPackageAssignments count = 0` — live green |
| 7 | Admin impersonate tetap melihat preview soal read-only (assignment dibangun in-memory tanpa persist) | VERIFIED | D-06: build assignment (ShuffleEngine) tanpa guard, hanya persist yang di-guard; e2e WSE-05 assert `impResp.status() < 400` + `qcard visible` membuktikan render tanpa NRE/500; UAT manual checkpoint T3 Bagian A APPROVED 2026-06-15 |
| 8 | Setelah stop impersonate, worker asli StartExam BARU saat itu StartedAt ter-set + 1 UserPackageAssignment ter-persist (deferred-start SC#3) | VERIFIED | e2e WSE-05: stop impersonate → login worker asli → goto StartExam → DB assert `StartedAt IS NOT NULL = 1` + `UserPackageAssignments count = 1` — live green |
| 9 | migration: false — tidak ada perubahan skema DB | VERIFIED | Semua commit Phase 381 hanya menyentuh `Helpers/`, `Controllers/`, `HcPortal.Tests/`, `tests/e2e/` — tidak ada file `Data/Migrations/`; UAT T3 Bagian B: `dotnet ef migrations add` menghasilkan Up()/Down() kosong → removed → snapshot EF-formatting noise reverted → tree bersih; APPROVED 2026-06-15 |

**Score:** 9/9 truths verified

---

### Deferred Items

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | Full PrePost pass/grade E2E (#4 lanjutan — Score/Lulus benar) | Phase 382 | Phase 382 success criteria mencakup WSE-06 grading/lifecycle/cert; test ditandai `// DEFERRED pasca-382` di exam-taking.spec.ts:1922 |

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/SiblingSessionQuery.cs` | Shared type-aware sibling predicate (D-09) | VERIFIED | Ada, 25 baris; mengandung `SiblingPrePostAwarePredicate`; `isPrePost` branch Pre/Post isolation + non-PrePost group; LinkedGroupId TIDAK ada (D-01) |
| `HcPortal.Tests/SiblingPrePostFilterTests.cs` | 5 unit test: Pre isolation, Post isolation, Standard/''/null group, null caller, key-mismatch | VERIFIED | Ada, 84 baris; 5 `[Fact]`; semua memanggil `SiblingPrePostAwarePredicate`; pre/post assertion + Standard/''/null group + DifferentKey_IsNotSibling |
| `HcPortal.Tests/SiblingDeterminismTests.cs` | 2 unit test: OrderBy invariant + cross-call parity | VERIFIED | Ada, 55 baris; 2 `[Fact]` `WorkerIndex_IsOrderInvariant` + `TwoListsSameContent...`; mengandung `OrderBy` + `IndexOf` |
| `Controllers/CMPController.cs` (StartExam sibling-query) | Pakai `SiblingPrePostAwarePredicate` ~line 1017 | VERIFIED | Line 1017-1019: `.Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(...))` — confirmed |
| `Controllers/CMPController.cs` (write-site guards) | 3 write-site dibungkus `!IsImpersonating()` | VERIFIED | Line 993 (justStarted+status), 1002 (SignalR+Log), 1078 (persist) — semua ada; total 4 occurrence `!IsImpersonating` di file (termasuk precedent 912) |
| `Controllers/AssessmentAdminController.cs` (ReshufflePackage) | Pakai `SiblingPrePostAwarePredicate` ~line 5192 | VERIFIED | Line 5192-5194: `.Where(SiblingSessionQuery.SiblingPrePostAwarePredicate(...))` — confirmed |
| `Controllers/AssessmentAdminController.cs` (ReshuffleAll) | Type-aware workerIndex via `SiblingKey` dict | VERIFIED | Line 5273 `SiblingKey` local func + 5274-5276 `sortedByKey` dict + 5321-5327 per-session `typeSiblingIds`/`sessionPackages` — full parity deviation user-approved |
| `tests/e2e/exam-taking.spec.ts` | E2E #4 WSE-04 pool isolation | VERIFIED | Line 1849: describe "Phase 381 WSE-04"; assert qcards.count()==3 + not.toContain('POST-Q'); full pass/grade DEFERRED ditandai |
| `tests/e2e/impersonation.spec.ts` | E2E #7 WSE-05 no-mutation + deferred-start | VERIFIED | Line 290: describe "Phase 381 WSE-05"; DB assert StartedAt null→set, 0→1 UPA; render assert status<400 + qcard visible |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `CMPController.cs StartExam GET (~1017)` | `SiblingSessionQuery.SiblingPrePostAwarePredicate` | `.Where(predicate)` EF sibling-query | WIRED | Line 1017-1019 confirmed; type params dilewatkan (`assessment.Title, .Category, .Schedule.Date, .AssessmentType`) |
| `AssessmentAdminController.cs ReshufflePackage (~5192)` | `SiblingSessionQuery.SiblingPrePostAwarePredicate` | `.Where(predicate)` EF sibling-query | WIRED | Line 5192-5194 confirmed; params dari `assessment.*` |
| `AssessmentAdminController.cs ReshuffleAll loop (~5321)` | `SiblingKey` + `sortedByKey[siblingKey]` | In-memory type-aware grouping | WIRED | `SiblingKey` didefinisikan line 5273; `sortedByKey` line 5274; per-session `typeSiblingIds` + `workerIndex` line 5321-5323; semantik identik dengan `SiblingPrePostAwarePredicate` |
| `CMPController.cs StartExam write-site 1 (~993)` | `_impersonationService.IsImpersonating()` | `if (justStarted && !IsImpersonating())` | WIRED | Line 993 confirmed; guard mencegah `assessment.Status="InProgress"` + `StartedAt=now` + SaveChanges |
| `CMPController.cs StartExam write-site 2 (~1002)` | `_impersonationService.IsImpersonating()` | `if (justStarted && !IsImpersonating())` | WIRED | Line 1002 confirmed; guard mencegah SignalR `workerStarted` + `LogActivityAsync("started")` |
| `CMPController.cs StartExam write-site 3 (~1078)` | `_impersonationService.IsImpersonating()` | `if (!IsImpersonating())` guard di dalam `if (assignment == null)` | WIRED | Line 1078 confirmed; build assignment tetap jalan (in-memory D-06); hanya `_context.Add` + `SaveChangesAsync` yang di-guard |
| `CMPController.cs VerifyToken (~856)` | (tidak disentuh — D-05) | — | NOT_WIRED (by design) | D-05 confirmed: VerifyToken (line 856-891) tidak mengandung `IsImpersonating`; hanya `TempData` ditulis, bukan mutasi DB worker |
| `AssessmentAdminController.cs UpdateShuffleSettings (~5384)` | (tidak disentuh — D-08) | Sibling filter group-wide tanpa AssessmentType | NOT_WIRED (by design) | D-08 confirmed: line 5384-5389 filter `Title/Category/Schedule.Date` tanpa AssessmentType — lock-detection tetap group-wide sesuai desain |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|-------------------|--------|
| `CMPController.StartExam` sibling pool | `siblingSessionIds` | `_context.AssessmentSessions.Where(SiblingPrePostAwarePredicate(...)).Select(s=>s.Id).ToListAsync()` | EF DB query dengan predicate type-aware | FLOWING |
| `CMPController.StartExam` workerIndex | `workerIndex = sortedSiblingIds.IndexOf(id)` | `siblingSessionIds.OrderBy(x=>x).ToList()` (in-memory sort) | Deterministik dari DB query — Phase 373 invariant | FLOWING |
| `CMPController.StartExam` assignment preview (impersonate) | `assignment` object | `ShuffleEngine.BuildQuestionAssignment(packages, ...)` — in-memory, no DB write | Preview soal real dari packages DB; no persist | FLOWING (in-memory by design D-06) |
| `AssessmentAdminController.ReshuffleAll` `sessionPackages` | Per-session type-filtered packages | `packages.Where(p => typeSiblingIds.Contains(p.AssessmentSessionId))` dari `siblingSessionIds` (all-types) in-memory | Real DB packages, filtered type-aware in-memory | FLOWING |

---

### Behavioral Spot-Checks

Step 7b: SKIPPED — tidak dapat dijalankan tanpa server + DB aktif. E2E Playwright (3/3 green, live, headless) sudah menutup semua behavior yang relevan sebagai pengganti automated spot-check CLI.

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| WSE-04 | 381-01, 381-03 | Worker Pre/Post same-day menerima HANYA paket ujian itu (tidak tercampur) | SATISFIED | `SiblingPrePostAwarePredicate` wired di 3 titik round-robin; unit 5/5 + e2e WSE-04 green; Requirements.md traceability marked Complete |
| WSE-05 | 381-02, 381-03 | Admin impersonate tidak memulai ujian atau membakar waktu/mengunci shuffle worker | SATISFIED | 3 write-site di-guard `!IsImpersonating()`; in-memory preview D-06; e2e WSE-05 DB assert no-mutation + deferred-start green; Requirements.md traceability marked Complete |

**Coverage:** 2/2 requirements Phase 381 — keduanya SATISFIED. REQUIREMENTS.md traceability baris 57-58 menunjukkan `[x]` pada WSE-04 dan WSE-05.

---

### Anti-Patterns Found

Tidak ada anti-pattern blocker ditemukan. Catatan informatif:

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `CMPController.cs:1047` | `var rng = Random.Shared` — RNG tidak di-seed saat impersonate | Info | Preview shuffle berbeda tiap refresh (diskresi by design — CONTEXT §"Claude's Discretion"; tidak memengaruhi worker asli) |
| `AssessmentAdminController.cs ReshuffleAll` | `var siblingSessionIds = sessions.Select(s => s.Id).ToList()` (line 5269) masih ada namun hanya dipakai untuk `packages` query lama, bukan workerIndex | Info | Tidak masalah — `sortedByKey` dict dipakai untuk workerIndex; `siblingSessionIds` dipakai filter packages broad (semua tipe) sebelum per-session filter `sessionPackages`; semantik benar |

---

### Human Verification Required

Semua human verification items dari Plan 03 telah dikerjakan dan di-approve sebelum `/gsd-verify-work`:

1. **Preview impersonate render (Bagian A)** — COMPLETED & APPROVED 2026-06-15. E2E #7 auto-cover: `impResp.status() < 400` + `qcard visible`. Tidak ada NRE/500 (D-06 path `vm.AssignmentId=0`).
2. **migration: false (Bagian B)** — COMPLETED & APPROVED 2026-06-15. `dotnet ef migrations add _Phase381MigrationCheck_DELETE_ME` → Up()/Down() kosong → `migrations remove --force` → snapshot EF-formatting noise reverted → tree bersih.

Tidak ada item human verification yang tersisa.

---

### D-01 thru D-09 Decision Honor Check

| Decision | Requirement | Honored? | Evidence |
|----------|-------------|----------|---------|
| D-01: AssessmentType satu-satunya diskriminator (LinkedGroupId TIDAK dipakai) | WSE-04 | YES | `SiblingSessionQuery.cs` tidak mengandung `LinkedGroupId`; komentar D-01 eksplisit di file |
| D-02/D-08: helper dipakai IDENTIK di StartExam + ReshufflePackage + ReshuffleAll; UpdateShuffleSettings/ManagePackages group-wide | WSE-04 | YES | `SiblingPrePostAwarePredicate` di 3 titik; UpdateShuffleSettings line 5384 dan ManagePackages tidak pakai helper |
| D-04: 3 write-site dibungkus `!IsImpersonating()` | WSE-05 | YES | Line 993, 1002, 1078 confirmed |
| D-05: VerifyToken tidak disentuh | WSE-05 | YES | VerifyToken (line 856-891) bebas `IsImpersonating`; hanya TempData |
| D-06: build in-memory selalu jalan, hanya persist yang di-guard | WSE-05 | YES | `ShuffleEngine` dipanggil sebelum guard; `_context.Add` + `SaveChangesAsync` di dalam `if (!IsImpersonating())` |
| D-07: stale-check tidak terpengaruh (`assessment.StartedAt != null` → false saat impersonate-belum-mulai) | WSE-05 | YES | Stale-check (~line 1098) masih `if (assessment.StartedAt != null && ...)` — tidak diubah |
| D-08: UpdateShuffleSettings/ManagePackages lock-detection group-wide (tidak kena filter AssessmentType) | WSE-04 | YES | UpdateShuffleSettings line 5384 filter tanpa AssessmentType confirmed |
| D-09: semantik type-aware (non-equality): Standard/''/null satu grup | WSE-04 | YES | Klausa `(s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest")` di predicate; unit Test 3+4 membuktikan null masuk non-PrePost grup |

---

### Gaps Summary

Tidak ada gaps. Semua 9 truths verified, semua artifacts exist dan substantive, semua key links wired, semua decisions honored, xUnit 391/391, e2e 3/3 green, migration:false proven, UAT approved.

---

## Summary

Phase 381 mencapai goalnya. Kedua REQ terbukti di codebase aktual:

**WSE-04** — `Helpers/SiblingSessionQuery.SiblingPrePostAwarePredicate` (type-aware D-09) dipakai identik di `CMPController.StartExam` GET + `AssessmentAdminController.ReshufflePackage` + `AssessmentAdminController.ReshuffleAll`. Pre/Post same-day tidak lagi saling memungut paket; Normal exam (Standard/''/null) zero behavior-change; determinisme workerIndex Phase 373 terjaga. Unit test 7/7 (5 filter + 2 determinism) + e2e WSE-04 green.

**WSE-05** — 3 write-site di `StartExam` GET dibungkus `!IsImpersonating()` (mirror precedent Phase 377 line 912): write-site 1 (StartedAt/Status), write-site 2 (SignalR/LogActivity), write-site 3 (persist UserPackageAssignment). Admin impersonate melihat preview soal read-only (assignment in-memory D-06) tanpa mutasi DB. Setelah stop impersonate, worker asli StartExam → barulah StartedAt ter-set + 1 assignment ter-persist (SC#3 deferred-start). E2E WSE-05 DB-assert green + render preview verified (no NRE/500). VerifyToken tidak disentuh (D-05).

Full PrePost pass/grade acceptance (lanjutan E2E #4 Score/Lulus) DEFERRED ke Phase 382 sesuai desain CONTEXT.

---

_Verified: 2026-06-15T08:00:00+08:00_
_Verifier: Claude (gsd-verifier)_
