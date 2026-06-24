# Phase 419: Export Label Section + Polish + Test/UAT Milestone - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 9 (4 modify production + 3 new xUnit + 4 new e2e — production touch is minimal; 419 = integrasi/polish, bukan greenfield)
**Analogs found:** 9 / 9 (semua punya analog in-repo — TIDAK ada file tanpa preseden)

> CATATAN KRITIS lintas-file: `Section` di banyak titik controller = org-unit pekerja (`u.Section`, kolom string).
> `AssessmentPackageSection` (grup-soal) diakses via `PackageQuestion.SectionId` / nav `q.Section` → `.SectionNumber` + `.Name`.
> JANGAN konflasikan saat memilih analog. Semua analog di bawah memakai `q.Section` (grup-soal), bukan `u.Section`.

---

## File Classification

| File (modify/new) | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/ExcelExportHelper.cs` `AddDetailPerSoalSheet` (MODIFY ~:50) | helper (export/render) | transform / file-I/O (read→.xlsx) | same file `AddElemenTeknisSheet` (:119) + `CreateSheet` header styling (:14-23) | exact (role+flow, same file) |
| `Controllers/AssessmentAdminController.cs` `GeneratePerPesertaPdf` (MODIFY :5703) | controller (PDF doc-builder inline) | transform / file-I/O (read→.pdf) | same method per-question loop (:5816-5843) | exact (in-method) |
| `Controllers/AssessmentAdminController.cs` export LOAD sites (MODIFY :5425-5428 Excel, :5673-5676 PDF) | controller (read/query) | request-response (read) | `ManagePackageQuestions` `.ThenInclude(q=>q.Section)` (:7647-7648 lacks it) + `CMPController.StartExam` guard load `.ThenInclude(q=>q.Section)` (:1088-1092) | exact (Include pattern) |
| `Controllers/AssessmentAdminController.cs` ET-warning predicate (MODIFY :7673-7680) | controller GET (ViewBag compute) | request-response (read, non-blocking signal) | existing predicate :7673-7680 (re-spec) + sibling-load pattern `CMPController.StartExam:1088-1092` | exact (re-spec in place) |
| `Services/InjectAssessmentService.cs` D-02 guard (MODIFY, insert in `InjectBatchAsync` after `ResolveLinkContextAsync` ~:135 / or in `PreflightValidateAsync`) | service (write-guard, server-authoritative) | request-response (validate→reject-all) | `CMPController.StartExam:1098-1119` (`SectionStructureComparer` call pattern + `guardAnySections` skip-legacy :1095) | exact (verbatim comparer usage) |
| `HcPortal.Tests/ExportSectionLabelTests.cs` (NEW) | test (unit/integration) | request-response (assert) | `SectionMismatchGuardTests.cs` (fixture + `AddPackageWithSectionsAsync` :172-201) | role-match (data-layer xUnit) |
| `HcPortal.Tests/LinkPrePostSectionGuardTests.cs` (NEW) | test (integration, real-DB) | request-response (assert reject/pass) | `SectionMismatchGuardTests.cs` (drive REAL service over SQLEXPRESS, no logic replica) | exact (same guard-test shape) |
| `HcPortal.Tests/SectionEtWarningTests.cs` (NEW — positive test) | test (integration) | request-response (assert ViewBag) | `SectionMismatchGuardTests.cs` + `SectionFixture` | role-match |
| `tests/e2e/section-lifecycle-419.spec.ts` + `inject-section-419` + `linkprepost-section-419` + `addremove-section-419` (NEW ×4) | test (e2e Playwright) | event-driven (real-browser UI) | `scoped-shuffle.spec.ts` (D-04.1/.4) · `inject-assessment-397.spec.ts` (D-04.2/.3) · `flexible-participant-412.spec.ts` (D-04.4) · `export-per-peserta.spec.ts` (download-assert) | exact (per-scenario analog) |

---

## Pattern Assignments

### `Helpers/ExcelExportHelper.cs` → `AddDetailPerSoalSheet` (helper, transform→.xlsx) — PAG-04 Excel

**Analog:** same file — `AddDetailPerSoalSheet` (current, :50-112) for the body; `AddElemenTeknisSheet` (:119) + `CreateSheet` (:14) for header-styling idiom.

**Current ordering to REPLACE** (`ExcelExportHelper.cs:58`):
```csharp
// CURRENT (flat by Order) — replace with (SectionNumber, Order, Id):
var sortedQuestions = questions.OrderBy(q => q.Order).ThenBy(q => q.Id).ToList();
```
Replace with the canonical milestone ordering (Pattern 1 below; "Lainnya" last via `?? int.MaxValue`):
```csharp
var sortedQuestions = questions
    .OrderBy(q => q.Section?.SectionNumber ?? int.MaxValue)   // null = Lainnya, last
    .ThenBy(q => q.Order)
    .ThenBy(q => q.Id)
    .ToList();
```
> Requires the LOAD site to `.Include(q => q.Section)` (see controller load section). Without it, `q.Section` is null → all "Lainnya" silently (Pitfall 1).

**Header-styling idiom to copy** (`ExcelExportHelper.cs:71-74`) — reuse bold+fill+center for the band-header row:
```csharp
var headerRange = ws.Range(1, 1, 1, col);
headerRange.Style.Font.Bold = true;
headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
```

**Band-header (merged) — NEW pattern** (RESEARCH §Code Examples, ClosedXML `Range().Merge()`):
```csharp
// Insert ABOVE the column-header row. Merge covers the Jawaban..Benar? columns of THAT Section
// (2 cols per soal). No/Nama/NIP cols (1-3) stay un-banded.
var band = ws.Range(bandRow, startCol, bandRow, endCol);
band.Merge();
band.Value = sectionNumber.HasValue ? $"Section {sectionNumber}: {sectionName}" : "Lainnya";
band.Style.Font.Bold = true;
band.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
band.Style.Fill.BackgroundColor = XLColor.LightBlue;   // Claude's Discretion D-01
```

**Off-by-one warning (Pitfall 4):** current data starts `rowIdx = 2` (:76), `FreezeRows(1)` (:111). Adding a band row above the column-header shifts everything down: band = row 1, column-header = row 2, data = row 3+. Recompute `rowIdx` start and `FreezeRows(2)` ONLY when a band is rendered. **Backward-compat (Claude's Discretion D-01):** when ALL questions are null-Section, render NO band → output byte-similar to today (`rowIdx = 2`, `FreezeRows(1)`).

**Kill-drift (DO NOT change):** keep the existing per-cell `BuildAnswerCell` / `IsQuestionCorrect` calls (`:93-94`) verbatim — they return OptionText (letter-agnostic) so A–F dynamic (Phase 418) is automatically consistent. Changing them = drift vs web Results/PDF (forbidden v30.0/386).

---

### `Controllers/AssessmentAdminController.cs` → `GeneratePerPesertaPdf` (:5703) — PAG-04 PDF

**Analog:** the method's own per-question loop (`:5816-5843`).

**Current ordering to REPLACE** (`:5732-5735`):
```csharp
var sessionQuestions = allQuestions
    .Where(q => sessionPkgIds.Contains(q.AssessmentPackageId))
    .OrderBy(q => q.Order).ThenBy(q => q.Id)   // → add Section grouping
    .ToList();
```

**Heading-between-blocks — NEW pattern** (wrap existing loop; RESEARCH §Code Examples mirrors QuestPDF `Column().Item().Text()` at `:5807,5834-5840`):
```csharp
foreach (var grp in sessionQuestions
        .GroupBy(q => q.Section?.SectionNumber)
        .OrderBy(g => g.Key ?? int.MaxValue))   // Lainnya last
{
    var label = grp.Key.HasValue ? $"Section {grp.Key}: {grp.First().Section?.Name}" : "Lainnya";
    col.Item().PaddingTop(6).Text(label).Bold().FontSize(12)
       .FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);   // Claude's Discretion D-01 layout
    foreach (var q in grp.OrderBy(q => q.Order).ThenBy(q => q.Id))
    {
        // ... EXISTING per-question block :5818-5841 UNCHANGED (BuildAnswerCell + IsQuestionCorrect)
    }
}
```
> Backward-compat (D-01): when only 1 group with null key → suppress the "Lainnya" heading (Claude's Discretion) so legacy PDF looks identical.

**Existing block to PRESERVE verbatim** (`:5823-5825` — kill-drift):
```csharp
var responsesForQ = sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList();
bool? correct = AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ);   // D-09 ALL types
string jawaban = AssessmentScoreAggregator.BuildAnswerCell(q, responsesForQ);    // D-10
```

---

### `Controllers/AssessmentAdminController.cs` → export LOAD sites (:5425-5428 Excel, :5673-5676 PDF) — Pitfall 1 fix

**Analog:** `ManagePackageQuestions:7647-7648` and `CMPController.StartExam:1088-1092` both use `.ThenInclude(q => q.Section)`.

**Current (BUG — no Section eager-load)** at BOTH sites `:5425-5428` and `:5673-5676`:
```csharp
var allQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => packageIds.Contains(q.AssessmentPackageId))
    .ToListAsync();
```
**Fix — add `.Include(q => q.Section)`** (mirror the StartExam load idiom `:1088-1092`):
```csharp
var allQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Include(q => q.Section)              // ← REQUIRED for PAG-04; else q.Section null → all "Lainnya" silent
    .Where(q => packageIds.Contains(q.AssessmentPackageId))
    .ToListAsync();
```
> **Warning sign (Pitfall 1):** helper unit-test green (Section set in-memory) but real-browser UAT shows all "Lainnya" = Include forgotten. MIRROR Pitfall 3 of Phase 416.

---

### `Controllers/AssessmentAdminController.cs` → ET-warning predicate (:7673-7680) — D-03 / IN-01 / DEF-416-01

**Analog:** the existing predicate (`:7673-7680`, re-spec in place) + sibling-load idiom from `CMPController.StartExam:1088-1092`.

**Current dead-code predicate** (`:7673-7680`) — `DistinctEt ≤ K` always (each PackageQuestion = 1 ET), within 1 package:
```csharp
ViewBag.SectionEtWarnings = sections.Select(s =>
{
    var qs = pkg.Questions.Where(q => q.SectionId == s.Id).ToList();   // ← within 1 package only
    int k = qs.Count;
    int distinctEt = qs.Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                       .Select(q => q.ElemenTeknis!).Distinct().Count();
    return new SectionEtWarning(s.SectionNumber, s.Name, k, distinctEt);
}).Where(w => w.DistinctEt > w.K).ToList();
```

**Re-spec (D-03):** load sibling packages (Pitfall 3 — GET currently loads 1 package), group cross-sibling by **`SectionNumber`** (IN-01, NOT `SectionId` — siblings differ in Id, share number):
- `DistinctEt` = distinct ElemenTeknis across ALL questions with `SectionNumber == N` across ALL sibling packages in the assessment.
- `K` = `min(count of questions with SectionNumber == N per sibling package)` (the quota actually presented).
- Fire when `DistinctEt > K`.

**Sibling-load idiom to copy** (mirror `CMPController.StartExam:1088-1092`):
```csharp
// After resolving pkg.AssessmentSessionId, load ALL packages in the same assessment:
var siblingPkgs = await _context.AssessmentPackages
    .Include(p => p.Questions).ThenInclude(q => q.Section)
    .Where(p => p.AssessmentSessionId == pkg.AssessmentSessionId && p.Questions.Any())
    .ToListAsync();
// Group cross-sibling by SectionNumber (IN-01) → pool ET + K=min(count per package).
```
> **Constraints:** stays NON-BLOCKING (D-416-03 load-bearing). Keep the `SectionEtWarning` record shape (`:7686`) — only the data source for `K`/`DistinctEt` changes. Add a POSITIVE test (previously only S3/S3b proved non-blocking — DEF-416-01 gap).
> **Warning sign (Pitfall 3):** predicate still "never fires" after re-spec → still comparing within 1 package (forgot sibling load).

---

### `Services/InjectAssessmentService.cs` → D-02 LinkPrePost × Section guard

**Analog:** `CMPController.StartExam:1098-1119` (verbatim `SectionStructureComparer` usage) + `guardAnySections` skip-legacy gate (`:1094-1096`).

**Insertion point:** inside `InjectBatchAsync`, after `ResolveLinkContextAsync` resolves `rep`/`targetRoomSessions` (~`:112-121`), OR in `PreflightValidateAsync` (`:430`) which uses `ResolveLinkContextAsync` at `:532-536`. Server-authoritative (V4): compare server-resolved target room counts vs inject batch counts — NEVER client `LinkedGroupId` (Tampering guard T-397-06).

**`guardAnySections` skip-legacy gate to copy** (`CMPController.StartExam:1094-1096`) — **CRITICAL for D-02 / Open Q-1 resolution = D-02 "skip bila ada sisi all-Lainnya"**:
```csharp
// Fire guard ONLY when BOTH sides have a real Section. Inject package = always all-Lainnya
// (InjectQuestionSpec has NO SectionId — Models/InjectAssessmentDtos.cs:15-25; InjectBatchAsync
// creates PackageQuestion without SectionId at :207-216). If either side is all-Lainnya → SKIP.
bool injectHasSection = injectQuestions.Any(q => q.SectionId != null);
bool targetHasSection = targetRoomQuestions.Any(q => q.SectionId != null);
if (injectHasSection && targetHasSection) { /* run comparer below */ }
```

**Comparer call pattern to copy** (`CMPController.StartExam:1103-1119` + RESEARCH §Pattern 2 — DO NOT write a new predicate, anti-pattern):
```csharp
var injectCounts = injectQuestions
    .GroupBy(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber))   // null → LainnyaKey
    .ToDictionary(g => g.Key, g => g.Count());
var targetCounts = targetRoomQuestions
    .GroupBy(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber))
    .ToDictionary(g => g.Key, g => g.Count());
var mismatched = SectionStructureComparer.MismatchedSections(injectCounts, targetCounts);
if (mismatched.Any())
{
    foreach (var sn in mismatched)
    {
        int x = injectCounts.GetValueOrDefault(sn);
        int y = targetCounts.GetValueOrDefault(sn);
        errors.Add(new InjectRowError {   // reject-all path (D-03), reuse existing InjectRowError
            Message = $"Section {SectionStructureComparer.SectionLabel(sn)}: room target punya {y} soal, " +
                      $"batch inject punya {x} soal (struktur harus sama untuk ditautkan)."
        });
    }
}
```
> **Message style (D-02 / SEC-04):** name SectionNumber + expected (target/Pre) vs actual (inject) count.
> **Reject-all (V5):** collect ALL section errors (no stop-at-first) — mirror existing 396/397 reject-all path (`InjectBatchAsync:49-68`).
> **Pitfall 2 / Open Q-1 (RESOLVED by CONTEXT D-02 + research recommendation opsi a):** without the `guardAnySections` skip, EVERY inject-Pre↔room-ber-Section link is blocked (inject is always all-Lainnya). D-02 explicitly says skip when either side all-Lainnya. The gate above implements that.

---

### `HcPortal.Tests/*` (NEW xUnit) — D-05 unit/integration

**Analog:** `SectionMismatchGuardTests.cs` — drives REAL controller/service over real SQLEXPRESS via `SectionFixture` (de-tautology: NO logic replica in test; the real guard blocks/passes).

**Class shape** (`SectionMismatchGuardTests.cs:33-40`):
```csharp
[Trait("Category", "Integration")]
public class LinkPrePostSectionGuardTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public LinkPrePostSectionGuardTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
}
```

**Seed helper to REUSE** (`SectionMismatchGuardTests.cs:172-201` — `AddPackageWithSectionsAsync(ctx, sessionId, pkgNumber, dist)` where `dist` = `(int? sectionNumber, int count)[]`, null=Lainnya; creates `AssessmentPackageSection` row per non-null key + `PackageQuestion` with `SectionId`). Copy this helper into each new test file (or extract to a shared helper).

**Fixture** (`SectionFixture.cs`) — disposable `HcPortalDB_Test_{guid}`, `MigrateAsync` in `InitializeAsync` (runs 415 `AddAssessmentPackageSection`), `EnsureDeletedAsync` in `DisposeAsync`. HcPortalDB_Dev untouched. Use for ALL 3 new data-layer test files.

**Per new file:**
- `ExportSectionLabelTests.cs` — call `AddDetailPerSoalSheet` (+ seeded Section questions) → assert workbook cell value / merge range + ordering by (SectionNumber, Order); a `NoSection_BackwardCompat` case (all-null → no band, output identical). Reuse `PdfAnswerCellTests`/`IsQuestionCorrectTests` (existing) as kill-drift regression — DO NOT duplicate.
- `LinkPrePostSectionGuardTests.cs` — seed inject batch + target room (use `AddPackageWithSectionsAsync`), drive `InjectBatchAsync`/`PreflightValidateAsync`, assert reject (mismatch both-Section) / pass (match OR either-side all-Lainnya per D-02 skip).
- `SectionEtWarningTests.cs` — POSITIVE test: seed sibling packages where `DistinctEt(pool) > K(min)` so predicate FIRES (DEF-416-01 gap) + `GroupBySectionNumber` (IN-01) cross-sibling + a NON-BLOCKING assert.

---

### `tests/e2e/*-419.spec.ts` (NEW ×4 Playwright) — D-04 real-browser UAT @5277

**Shared analog harness** (`scoped-shuffle.spec.ts:32-61`): `test.describe.configure({ mode: 'serial' })`; `beforeAll` BACKUP `HcPortalDB_Dev` via `helpers/dbSnapshot` → `afterAll` RESTORE + unlink (SEED_WORKFLOW); `createAssessmentViaWizard`/`addQuestionViaForm` from `./helpers/examTypes`; seed Section via `execSql` SQL UPDATE on wizard-created records; `--workers=1` (NTLM loopback); admin `admin@pertamina.com` / `123456`; coachee `rino.prasetyo@pertamina.com`.

| New spec | D-04 scenario | Primary analog | What to reuse |
|----------|---------------|----------------|---------------|
| `section-lifecycle-419.spec.ts` | D-04.1 lifecycle (create→assign Section→ujian→render A–F→resume→export label) | `scoped-shuffle.spec.ts` | wizard create + SQL `UPDATE ... SectionId`; StartExam DOM order; + download-assert from `export-per-peserta.spec.ts:1-8` (`.xlsx` download + content-type + size) |
| `inject-section-419.spec.ts` | D-04.2 Inject v32.2 × Section + opsi 5–6 | `inject-assessment-395/396/397.spec.ts` | `/Admin/InjectAssessment` wizard; commit + DB assert (IsManualEntry, cert#) |
| `linkprepost-section-419.spec.ts` | D-04.3 LinkPrePost 397 × Section (same=success, diff=rejected — D-02) | `inject-assessment-397.spec.ts:1-14` | "Cari Room" picker, link Pre↔Post, online-untouched assert; ADD: seed mismatched Section → assert reject message; matched → success |
| `addremove-section-419.spec.ts` | D-04.4 Add/Remove v32.5 × Section + pagination | `flexible-participant-412.spec.ts:1-19` | multi-context SignalR, flip session InProgress, `AddParticipantsLive` picker, force-kick; ADD Section + pagination active, assert eager-assign per-section consistent |

> **Download-assert idiom** (`export-per-peserta.spec.ts:1-8`): trigger Export → assert `.xlsx`/`.zip` download event + content-type + size. Optionally parse content for Section band/heading.
> **Cleanup (D-06):** after all UAT, restore DB snapshot (SEED_WORKFLOW snapshot→restore), mark `docs/SEED_JOURNAL.md` `cleaned`. Fold pending todo `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md`.

---

## Shared Patterns

### Pattern 1 — Section-aware canonical ordering (milestone-wide)
**Source:** `ShuffleEngine`/`SectionPaginator` (Phase 416/417) idiom; codified in RESEARCH §Pattern 1.
**Apply to:** Excel reorder (`AddDetailPerSoalSheet`), PDF grouping (`GeneratePerPesertaPdf`).
```csharp
var ordered = questions
    .OrderBy(q => q.Section?.SectionNumber ?? int.MaxValue)   // Lainnya last
    .ThenBy(q => q.Order)
    .ThenBy(q => q.Id)
    .ToList();
var groups = ordered.GroupBy(q => q.Section?.SectionNumber)   // null = "Lainnya"
                    .OrderBy(g => g.Key ?? int.MaxValue);
```

### Pattern 2 — SEC-04 structure comparison (REUSE — never re-implement)
**Source:** `Helpers/SectionStructureComparer.cs` (full) — `KeyOf` (null→`LainnyaKey` sentinel), `MismatchedSections`, `SectionLabel`.
**Apply to:** D-02 LinkPrePost guard. Caller pattern verbatim at `CMPController.StartExam:1098-1119`.
```csharp
var leftCounts  = left .GroupBy(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber)).ToDictionary(g => g.Key, g => g.Count());
var rightCounts = right.GroupBy(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber)).ToDictionary(g => g.Key, g => g.Count());
var mismatched = SectionStructureComparer.MismatchedSections(leftCounts, rightCounts);
```

### Pattern 3 — `guardAnySections` skip-legacy gate
**Source:** `CMPController.StartExam:1094-1096`.
**Apply to:** D-02 guard (skip when either side all-Lainnya — inject is always all-Lainnya).
```csharp
bool anySections = pkgs.SelectMany(p => p.Questions).Any(q => q.SectionId != null);
// gate the comparer behind "both sides have Section"
```

### Pattern 4 — Kill-drift answer/correctness (DO NOT modify)
**Source:** `Helpers/AssessmentScoreAggregator.cs` — `BuildAnswerCell` (returns OptionText, letter-agnostic → A–F auto-consistent) / `IsQuestionCorrect` (MC IsCorrect; MA all-or-nothing SetEquals; Essay >0; pending=null).
**Apply to:** Excel (`ExcelExportHelper.cs:93-94`) + PDF (`AssessmentAdminController.cs:5823-5825`). Forbidden to change (drift vs web Results, v30.0/386, D-12 no per-Section score).

### Pattern 5 — Eager-load Section before grouping (Pitfall 1)
**Source:** `CMPController.StartExam:1088-1092` / `ManagePackageQuestions:7647-7648` (`.ThenInclude(q => q.Section)`).
**Apply to:** BOTH export load sites + ET-warning sibling load.
```csharp
.Include(q => q.Options).Include(q => q.Section)   // q.Section null without this → silent all-Lainnya
```

### Pattern 6 — Real-SQLEXPRESS data-layer test (de-tautology)
**Source:** `SectionFixture.cs` + `SectionMismatchGuardTests.cs` (`IClassFixture<SectionFixture>`, `AddPackageWithSectionsAsync` :172-201). Drive REAL guard/service, no logic replica.
**Apply to:** all 3 new xUnit files.

### Pattern 7 — Serial Playwright UAT with DB snapshot/restore (SEED_WORKFLOW, lesson 354)
**Source:** `scoped-shuffle.spec.ts:32-61` / `inject-assessment-397.spec.ts:1-14` / `flexible-participant-412.spec.ts:1-19`.
**Apply to:** all 4 new e2e specs. `mode:'serial'`, `dbSnapshot` backup/restore, `--workers=1`, port 5277, `Authentication__UseActiveDirectory=false`.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none) | — | — | Every production touch and test extends an existing in-repo pattern. 419 is integrasi/polish — zero greenfield surface. SEC-06 sync (`SyncPackagesToPost:6576`) is AUDIT-ONLY (already clones Section via nav remap at :6621-6645 — Pitfall 8 aware); no new code. |

---

## Metadata

**Analog search scope:** `Helpers/` (ExcelExportHelper, SectionStructureComparer, AssessmentScoreAggregator), `Controllers/` (AssessmentAdminController, CMPController), `Services/InjectAssessmentService.cs`, `Models/InjectAssessmentDtos.cs`, `HcPortal.Tests/` (SectionFixture, SectionMismatchGuardTests), `tests/e2e/` (scoped-shuffle, inject-397, flexible-participant-412, export-per-peserta).
**Files scanned (read):** 11
**Verified directly against codebase:**
- Pitfall 1 (export load lacks `.Include(q=>q.Section)`) — confirmed `:5425-5428` & `:5673-5676`.
- Pitfall 2 (inject package all-Lainnya — `InjectQuestionSpec` no SectionId `Models/InjectAssessmentDtos.cs:15-25`; `PackageQuestion` created without SectionId `InjectAssessmentService.cs:207-216`).
- Pitfall 3 (ET-warning GET loads 1 package `:7646-7649`; predicate dead-code `:7680`).
- SEC-04 comparer usage pattern (`CMPController.StartExam:1098-1119`) + `guardAnySections` (`:1094-1096`).
- SEC-06 audit-only (Section cloned via nav remap `SyncPackagesToPost:6621-6645`).
**Pattern extraction date:** 2026-06-24
**migration:** FALSE (no schema change; all Section data exists since Phase 415).
