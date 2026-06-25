# Phase 420: EditQuestion Identity-Based Option Editing — Research

**Researched:** 2026-06-25
**Stack:** ASP.NET Core MVC (net8.0) + EF Core + SQL Server. Migration=FALSE.
**Source of truth read:** `Controllers/AssessmentAdminController.cs` (CreateQuestion `:7707-7872`, EditQuestion GET `:7876-7921`, EditQuestion POST `:7923-8262`), `Models/OptionInput.cs`, `Views/Admin/ManagePackageQuestions.cshtml` (form `:390-446`, populate `:690-731`, reletter/add/remove `:762-891`), `Helpers/OptionShrinkGuard.cs`, `Helpers/QuestionOptionValidator.cs`, `Data/ApplicationDbContext.cs:561-564`, `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs`, `HcPortal.Tests/EditShrinkGuardLogicTests.cs`.

---

## 1. Root cause confirmed (reproduces on main)

`EditQuestion` POST upsert loop (`:8126-8159`) is **positional**: `existing = q.Options.OrderBy(o=>o.Id)`, then `for i in 0..bound`: `existing[i]` updated from `options[i]`. Deleting a MIDDLE option compacts the form (A,B,C,D → delete B → submit A,C,D), so `existing[1]` (record B) gets `OptionText="C"`, `existing[2]`→"D", only `existing[3]` (D) removed. The guard's `removedOptionIds` (`:8036-8052`) is also positional/tail-only (`i >= keep || blank text`) → record B's Id never enters `removedOptionIds` → guard (`OptionShrinkGuard.FindBlockedOptionIds`) doesn't fire even though B is answered. `PackageUserResponse` stores `PackageOptionId` only (no text snapshot — confirmed `Models/PackageUserResponse.cs`), so the participant's answer that pointed at "B" now silently points at text "C". Grading-by-Id stays technically consistent; business meaning changes silently.

FK `PackageUserResponse → PackageOption` = **Restrict** (`ApplicationDbContext.cs:561-564`) — the 999.14 hazard (deleting an answered option → DbUpdateException 500). The guard exists to prevent that; this phase makes it fire for ANY position.

---

## 2. The fix — identity-based upsert (implements CONTEXT D-01..D-04)

### 2a. Carrier (D-01): `Models/OptionInput.cs` gets `int? Id`
Add `public int? Id { get; set; }` to `OptionInput`. **This deliberately revises the T-418-06 comment** ("JANGAN tambah properti Id … tidak boleh disuplai client"). The original rationale (Id server-determined, positional match) no longer holds — identity REQUIRES the client to supply Id. Replace the comment with the new contract: *Id is client-supplied for identity matching and MUST be validated server-side (`Id ∈ q.Options` of this question) before use; foreign Id → reject (fail-closed). This neutralizes mass-assignment: a forged Id cannot touch another question's options.*

Binding: ASP.NET Core indexed binding `options[i].Id` binds `int?` cleanly. A row with no `options[i].Id` input (new option) binds to `null` — no gotcha. Existing fields (`Text/IsCorrect/Image/ImageAlt/RemoveImage`) unchanged.

### 2b. GET JSON (`:7908-7914`) must emit `id`
Currently the options projection emits `optionText, isCorrect, imagePath, imageAlt` — **no Id**. Add `id = o.Id` so the form can populate the hidden carrier. (Order already `OrderBy(o=>o.Id)`.)

### 2c. Razor form + JS (`ManagePackageQuestions.cshtml`)
Add a hidden carrier per option row, e.g. inside `.option-row`:
```html
<input type="hidden" class="opt-id-input" name="options[@i].Id" id="option_@(letter)_id" value="" />
```
- **`reletterRows()` (`:769-835`)** must also rename the hidden input: `idInput.name = 'options[' + i + '].Id'; idInput.id = 'option_' + L + '_id';` (mirror the text-input rename block at `:779-785`). The hidden VALUE is preserved (reletter only renames, never clears).
- **`populateEditForm()` (`:708-722`)** must set each row's hidden Id from `opts[i].id` (now present in GET JSON): `var idEl = document.getElementById('option_'+L+'_id'); if (idEl) idEl.value = (opt.id != null) ? String(opt.id) : '';`. Rows padded beyond `opts.length` (MIN_OPTIONS) get empty Id (new).
- ⚠️ **CRITICAL GOTCHA — `addOptionRow()` (`:838-863`) clone reset** clears only `type==='text'|'radio'|'checkbox'|'file'`. A `type='hidden'` Id input is **NOT cleared**, so a cloned new row would inherit row[0]'s existing Id → the new option would silently UPDATE option A (or duplicate an Id). The clone reset loop MUST add: `if (inp.type === 'hidden') inp.value = '';` (or explicitly clear `.opt-id-input`). This is the highest-risk implementation detail — a Playwright "add option then save" test must catch it.
- New rows added via `addOptionRow` → empty Id → ADD path. Deleting a row (`removeOptionRow`, `:866-878`) removes the node entirely → that option's Id is simply absent from the submit → becomes a removed candidate. Correct by construction.

### 2d. Controller algorithm (replaces positional loop AND guard removed-set; D-01b/D-01c/D-03)
After `ResolveCorrectness` + existing type/score/option validators (unchanged, run first), BEFORE mutation/SaveChanges:

```
existing      = q.Options.OrderBy(o=>o.Id).ToList()
existingIds   = existing.Select(o=>o.Id).ToHashSet()

// (D-01a) ANTI-TAMPER — fail-closed, BEFORE any mutation
submittedIds  = options.Where(o=>o.Id.HasValue).Select(o=>o.Id.Value).ToList()
if submittedIds.Any(id => !existingIds.Contains(id)):
    TempData["Error"]="Opsi yang diubah tidak valid untuk soal ini."; return Redirect(ManagePackageQuestions)
if submittedIds has duplicates:
    TempData["Error"]="Opsi duplikat terdeteksi."; return Redirect

// kept = existing option whose Id appears in a submit row WITH non-blank text
keptIds = options.Where(o=>o.Id.HasValue && !IsNullOrWhiteSpace(o.Text)).Select(o=>o.Id.Value).ToHashSet()
newRows = options.Where(o=>!o.Id.HasValue && !IsNullOrWhiteSpace(o.Text)).ToList()

// (D-01c) removed = set-difference by Id (Essay = all). KILL-DRIFT: guard + upsert use THIS one set.
removedOptionIds = (questionType=="Essay") ? existingIds.ToList() : existingIds.Except(keptIds).ToList()

// (D-418-02 guard, D-03 answered = ALL responses any status — same query shape as :8055)
answered = PackageUserResponses.Where(r=>r.PackageOptionId.HasValue && removedOptionIds.Contains(r.PackageOptionId.Value))
                               .Select(r=>r.PackageOptionId.Value).Distinct().ToListAsync()
blocked  = OptionShrinkGuard.FindBlockedOptionIds(removedOptionIds, answered)   // signature UNCHANGED
if blocked.Count>0: build D-04 message → return Redirect

// UPSERT (identity)
if Essay: RemoveRange(all existing) (+image delete candidates) — unchanged Essay branch
else:
  foreach o in existing:
    if keptIds.Contains(o.Id):
        row = options.First(r=>r.Id==o.Id)
        o.OptionText = row.Text.Trim(); o.IsCorrect = row.IsCorrect
        await ApplyOptionImageIntent(o, row.Image, row.ImageAlt, row.RemoveImage, packageId, imagePathsToDelete)
    else:
        if !IsNullOrEmpty(o.ImagePath): imagePathsToDelete.Add(o.ImagePath)
        _context.PackageOptions.Remove(o); q.Options.Remove(o)   // safe — passed guard, no FK-Restrict
  foreach row in newRows:
    var n = new PackageOption { OptionText=row.Text.Trim(), IsCorrect=row.IsCorrect }
    await ApplyOptionImageIntent(n, row.Image, row.ImageAlt, row.RemoveImage, packageId, imagePathsToDelete)
    q.Options.Add(n)
```
Notes:
- `ResolveCorrectness(questionType, options, correctIndex)` (`:7942`) sets `IsCorrect` per submitted-position; it only touches `IsCorrect`, never `Id` — safe. The correct row (by correctIndex position in the submitted list) carries IsCorrect into its matched/added option.
- **D-02 (edit answered text/correctness allowed):** no new gate. An answered option whose Id is in `keptIds` is simply UPDATEd (text+IsCorrect). The existing `affectedSessions` modal (D-09, fed by GET JSON `affectedSessions` `:7901`) already warns HC. Nothing to add.
- Ordering: new options append (higher Id) → display last via the codebase-wide `OrderBy(o=>o.Id)`. Reorder is OUT of scope.

### 2e. D-04 blocked message (stored-order letter + text snippet)
Replace `:8063-8071`. For each blocked Id, letter = position in `existing` (already OrderBy Id) mapped to A–F; snippet = truncated `OptionText` of that existing option (it may be gone from the form). Keep the friendly tone:
`$"Opsi \"{letter}\" (\"{snippet}\") sudah dijawab peserta dan tidak bisa dihapus. Batalkan perubahan atau pertahankan opsi ini."` — join multiple with `; `.

---

## 3. Backward-compat & test impact (IMPORTANT)

The existing controller-level tests `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` call `EditQuestion(...)` with **`OptionInput` rows that have NO Id** (legacy positional contract: shrink by blank text / fewer rows). Under the new identity algorithm:
- **TEST 1** (`EditShrinkGuard_AnsweredOption_NotRemoved_NoException`): sends A,"",C,D null-Id. `submittedIds` empty → `keptIds` empty → `removedOptionIds` = all existing. `blocked` = answered ∩ all = {B}. → still redirects with error containing "B" and B stays. **Still passes** (assertions: no-throw, redirect, "sudah dijawab", "B", B count=4 retained). ✅
- **TEST 2** (`EditShrinkGuard_UnansweredOption_Removed_Succeeds`): sends A,B,C null-Id, answered=A. New algorithm: `keptIds` empty → `removedOptionIds` = all existing incl. A → `blocked` = {A} → BLOCKED. **Test asserts SUCCESS → would now FAIL.** ❌

**Resolution (same-phase, required):** UPDATE both existing integration tests to the identity contract — pass `new OptionInput { Id = optionIds[k], Text = "…" }` for kept options, omit the deleted one's row. This is correct: the controller's contract changes this phase. TEST 2 becomes "submit A,B,C WITH their Ids, omit D's row → D removed, A (answered, kept) untouched → success". TEST 1 becomes "submit A,C,D WITH Ids, omit B's row → B in removed, B answered → blocked".

`EditShrinkGuardLogicTests.cs` tests the **pure helper** `OptionShrinkGuard.FindBlockedOptionIds(removed, answered)` directly — signature & semantics UNCHANGED → **no change needed** (verify still green).

`CreateQuestion` POST (`:7707-7872`) builds all-new options (`foreach inp.Take(6) if !blank Add`) and ignores Id entirely → adding `OptionInput.Id` is inert there → **OPTEDIT-05 safe by construction** (still want a regression test asserting create still works).

---

## 4. New tests for VRF-01 (proof of fix)

Reuse the harness in `EditShrinkGuardIntegrationTests.cs` verbatim (`SectionFixture` real-SQL, `MakeController`, `SeedSessionPackageAsync`, `SeedFourOptionQuestionAsync`). Add tests (new file `IdentityOptionEditTests.cs` or extend the existing integration class):

1. **MIDDLE-DELETE answered → BLOCKED (the headline bug):** seed 4-opt A,B,C,D; answer B; call EditQuestion submitting rows for A(id),C(id),D(id) (omit B). Assert: no-throw, redirect, error contains "B" + snippet "B"; DB still 4 options; B's text still "B" (NOT relabeled); response→B intact. This RED-on-main (relabel) → GREEN.
2. **MIDDLE-DELETE unanswered → SUCCESS + no relabel (OPTEDIT-01):** seed 4-opt, answer A; delete B (submit A,C,D with Ids). Assert success; DB = 3 options A,C,D with original Ids/text preserved (C still "C", not shifted); option B's Id gone.
3. **EDIT answered option text/correctness (OPTEDIT-03):** seed 4-opt, answer B; submit all 4 with Ids but change B's text to "B-revised" and flip correctIndex. Assert success; response still points at B's Id; B's Id unchanged; text updated. (Semantic identity preserved.)
4. **999.14 regression-lock (OPTEDIT-04):** (a) convert answered MC→Essay → blocked, no 500; (b) shrink answered option from tail with Id omitted → blocked, no 500. Assert no DbUpdateException.
5. **Anti-tamper (D-01a):** submit a row with `Id` = an option Id from a DIFFERENT question/package → reject, no mutation. Pure-ish but needs DB (foreign Id). Assert redirect + "tidak valid", DB unchanged.
6. **ADD option (regression, OPTEDIT-05):** seed 4-opt unanswered; submit 4 existing (with Ids) + 1 new row (Id null, text "E") → 5 options, new one added, existing untouched.

Optionally a pure-logic test for the new `removedOptionIds` set-difference if extracted to a helper (Claude's discretion — may stay inline; if inline, integration tests cover it).

**Playwright e2e (VRF-01, lesson 354 — real browser authoring form):** existing e2e specs live under the Playwright project (search `*.spec.*`/`playwright` config; prior phases 412/413/418/419 added authoring/exam specs). Scenario: open ManagePackageQuestions for a package, edit a 4-option question, delete the middle option, save → assert friendly error if answered / correct persistence if not; AND add-option-then-save → assert the new option is added (NOT a silent overwrite of A) — this catches the §2c hidden-Id clone gotcha that controller tests cannot. Combined run `--workers=1` (memory `reference_local_e2e_sql_env_fix`).

---

## 5. Validation Architecture (Nyquist — REQUIRED)

| Success Criterion / REQ | Observable signal | Test layer | Test |
|---|---|---|---|
| OPTEDIT-01 middle-delete unanswered keeps correct record/text | DB: deleted Id gone, survivors keep Id+text (no shift) | controller integration (real-SQL) | New test #2 |
| OPTEDIT-02 delete answered any position → blocked, answer unchanged | no-throw, redirect, `TempData["Error"]` has letter+snippet; DB option+response intact | controller integration | New test #1 (middle) + updated TEST 1 (tail) |
| OPTEDIT-03 edit answered text/correctness → same option by Id | success; response PackageOptionId unchanged; option Id stable, text/IsCorrect updated | controller integration | New test #3 |
| OPTEDIT-04 MC/MA→Essay + shrink answered → blocked no 500 | `Record.ExceptionAsync` == null; redirect+error; no DbUpdateException | controller integration | New test #4 (a,b) + updated TEST 1/2 |
| OPTEDIT-05 Create/edit-unanswered/import unaffected | CreateQuestion adds question+options; add-option edit works | controller integration + Playwright | New test #6 + e2e add-option |
| D-01a anti-tamper foreign Id | reject, no mutation | controller integration | New test #5 |
| VRF-01 reproduce silent-relabel then prove blocked + real-browser | RED-on-main (relabel) → GREEN (blocked); browser save behaves | controller integration #1 + Playwright | #1 + e2e |
| Pure guard intersection unchanged | helper returns intersection | xUnit pure | existing EditShrinkGuardLogicTests (verify green) |
| Form hidden-Id survives reindex/add (clone gotcha §2c) | new option added not overwriting A; reletter preserves Id | Playwright real-browser | e2e add-option-then-save |

**Minimum test set:** 6 new controller-integration tests + update 2 existing integration tests to identity contract + verify 2 pure-logic test files still green + 1–2 Playwright e2e (delete-middle-answered + add-option). Build green + `dotnet test` (Integration trait needs SQLEXPRESS) + local run @5277 + Playwright `--workers=1` (SEED_WORKFLOW snapshot→seed→test→restore).

---

## 6. Constraints / out-of-scope (reconfirmed)
- Migration=FALSE. Do NOT change the FK or add a text-snapshot column (Out of Scope). Do NOT add reorder-options (deferred). No change to validator min-2/max-6, `correctIndex` contract, or `ResolveCorrectness`. Inject form path untouched (client-side new questions, not EditQuestion upsert).
- Files in scope: `Models/OptionInput.cs`, `Controllers/AssessmentAdminController.cs` (EditQuestion GET+POST), `Views/Admin/ManagePackageQuestions.cshtml`, `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` (update) + new test file, Playwright spec. `Helpers/OptionShrinkGuard.cs` + `Helpers/QuestionOptionValidator.cs` = read-only reuse.

## RESEARCH COMPLETE
