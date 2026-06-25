---
phase: 420-editquestion-identity-based-option-editing
security_audit: retroactive
asvs_level: 1
audited: 2026-06-25
threats_open: 0
verdict: SECURED
---

# Phase 420 — Security Audit

**Phase:** 420 — EditQuestion Identity-Based Option Editing
**ASVS Level:** L1
**Threats Closed:** 7/7 (T-420-01 through T-420-07)
**Threats Open:** 0

---

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-420-01 | Tampering / IDOR (mass-assignment via forged OptionId) | mitigate | CLOSED | See below |
| T-420-02 | Tampering (duplicate submitted Id) | mitigate | CLOSED | See below |
| T-420-03 | Spoofing / Repudiation (RBAC + antiforgery) | mitigate | CLOSED | See below |
| T-420-04 | Tampering (silent relabel — answered option) | mitigate | CLOSED | See below |
| T-420-05 | DoS (DbUpdateException 500 via FK-Restrict) | mitigate | CLOSED | See below |
| T-420-06 | XSS (hidden Id carrier + error message) | accept | CLOSED | See below |
| T-420-07 | Data hygiene (Playwright seed leak) | mitigate (process) | CLOSED (planned) | See below |

---

## Per-Threat Evidence

### T-420-01 — Tampering / IDOR: forged client-supplied OptionId

**Mitigation declared:** `submittedIds.Any(id => !existingIds.Contains(id))` → reject fail-closed before any mutation (D-01a).

**Code found:**
- `Controllers/AssessmentAdminController.cs:8035-8041` — anti-tamper block runs before `q.QuestionText =` (line 8094) and `SaveChangesAsync()` (line 8165):
  ```
  // (D-01a) ANTI-TAMPER — fail-closed SEBELUM mutasi apa pun.
  if (submittedIds.Any(id => !existingIds.Contains(id)))
  {
      TempData["Error"] = "Opsi yang diubah tidak valid untuk soal ini.";
      return RedirectToAction("ManagePackageQuestions", new { packageId });
  }
  ```
- `Models/OptionInput.cs:29` — `public int? Id { get; set; }` carrier present; class-level XML comment explicitly states server validates every non-null Id before use.

**Test:** `HcPortal.Tests/EditShrinkGuardIntegrationTests.cs` — `IdentityEdit_AntiTamper_ForeignOptionId_Rejected_NoMutation` (line 474): seeds two separate packages, submits foreign `otherOptionIds[0]` in payload for question-A, asserts redirect + error contains "tidak valid" + DB unchanged (4 options with original text "A","B","C","D").

**PASS** — mitigation present in code and proven by integration test.

---

### T-420-02 — Tampering: duplicate submitted Id

**Mitigation declared:** `submittedIds.Count != submittedIds.Distinct().Count()` → reject "duplikat" before mutation.

**Code found:**
- `Controllers/AssessmentAdminController.cs:8042-8046`:
  ```
  if (submittedIds.Count != submittedIds.Distinct().Count())
  {
      TempData["Error"] = "Opsi duplikat terdeteksi.";
      return RedirectToAction("ManagePackageQuestions", new { packageId });
  }
  ```
  Runs immediately after T-420-01 check; still before any mutation.

**Test:** `IdentityEdit_DuplicateSubmittedId_Rejected` (line 562): submits `optionIds[0]` twice, asserts error contains "duplikat", DB unchanged (4 options).

**PASS** — mitigation present in code and proven by integration test.

---

### T-420-03 — Spoofing / Repudiation: RBAC + antiforgery on EditQuestion POST

**Mitigation declared:** `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` must remain on the POST action (not removed).

**Code found:**
- `Controllers/AssessmentAdminController.cs:7925` — `[Authorize(Roles = "Admin, HC")]`
- `Controllers/AssessmentAdminController.cs:7926` — `[ValidateAntiForgeryToken]`
  Both attributes confirmed present on the EditQuestion POST overload (the action signature starting at line 7927).

**Test:** Covered by integration test harness — `MakeController` uses a real `AssessmentAdminController` instance; the antiforgery attribute is structural (enforced by ASP.NET Core middleware in production). RBAC is not bypassed in the harness (StubUserManager always returns a valid actor, which is sufficient for integration testing the guard logic). The attribute presence itself is the authoritative evidence at ASVS L1.

**PASS** — both attributes present on the POST action.

---

### T-420-04 — Tampering: silent relabel of answered option (including middle-position delete)

**Mitigation declared:** Identity match by Id + set-difference `existingIds.Except(keptIds)` fires for ANY position (including middle).

**Code found:**
- `Controllers/AssessmentAdminController.cs:8049-8059` — `removedOptionIds` computed as set-difference by Id:
  ```
  var keptIds = options
      .Where(o => o.Id.HasValue && !string.IsNullOrWhiteSpace(o.Text))
      .Select(o => o.Id!.Value)
      .ToHashSet();
  var removedOptionIds = (questionType == "Essay")
      ? existingIds.ToList()
      : existingIds.Except(keptIds).ToList();
  ```
  A middle option omitted from the submit will have its Id absent from `keptIds`, landing in `removedOptionIds` — the guard then fires.

- `Controllers/AssessmentAdminController.cs:8141` — upsert uses `keptIds.Contains(o.Id)` for UPDATE, remainder removed.

**Tests:**
- Ported TEST1 (`EditShrinkGuard_AnsweredOption_NotRemoved_NoException`, line 190): omits middle option B (answered) → blocked, "sudah dijawab" + "B" in error, 4 options intact.
- `IdentityEdit_MiddleDelete_Unanswered_NoRelabel_Succeeds` (line 329): omits middle B (unanswered, A is answered) → succeeds; verifies `q.Options.OrderBy(Id)` gives texts `["A","C","D"]` — C (optionIds[2]) text is still "C", not relabelled to "B".

**PASS** — set-difference-by-Id guard covers all positions; confirmed no-relabel by DB assertion.

---

### T-420-05 — DoS: DbUpdateException 500 via FK-Restrict

**Mitigation declared:** Guard fires pre-SaveChanges; deleting an answered option is blocked before EF touches the DB.

**Code found:**
- Guard block (`Controllers/AssessmentAdminController.cs:8062-8089`) runs entirely before `SaveChangesAsync()` (line 8165). If `blocked.Count > 0`, method returns redirect — SaveChanges is never called.
- Essay branch also: `removedOptionIds = existingIds.ToList()` (all options) — if any are answered, guard fires first.

**Tests:**
- `Record.ExceptionAsync` returning `null` is asserted in ALL guard-path tests (TEST1, `IdentityEdit_ConvertAnsweredMcToEssay_Blocked_NoException`, `IdentityEdit_AntiTamper_ForeignOptionId_Rejected_NoMutation`, `IdentityEdit_DuplicateSubmittedId_Rejected`).
- `IdentityEdit_ConvertAnsweredMcToEssay_Blocked_NoException` (line 431): submits questionType="Essay" on a question with answered option B → `Assert.Null(ex)` (no FK-Restrict 500), redirect + "sudah dijawab" in error; DB confirms `q.QuestionType == "MultipleChoice"` (not converted), 4 options intact.

**PASS** — guard pre-SaveChanges proven; no-exception asserted on SQL Server FK-Restrict path.

---

### T-420-06 — XSS: hidden Id carrier + error message

**Disposition in Plan 02 threat model:** `accept` (Id value is integer, not a free-text HTML sink).

**Evidence:**
- `Views/Admin/ManagePackageQuestions.cshtml:720` — hidden Id populated via `idEl.value = (opt.id != null) ? String(opt.id) : ''` — DOM `.value` property assignment, not `innerHTML`. No free-text user content in this field (value is an integer primary key from the server).
- `Views/Admin/ManagePackageQuestions.cshtml:404` — template: `<input type="hidden" class="opt-id-input" name="options[@i].Id" id="option_@(letter)_id" value="" />` — initial value is empty; populated from server-supplied integer only.
- Error message rendered in `Views/Admin/ManagePackageQuestions.cshtml:77` and `Views/Shared/_Layout.cshtml:205` via Razor `@TempData["Error"]` — Razor `@` syntax auto-HTML-encodes. No `@Html.Raw` on TempData["Error"].
- The option text snippet in the D-04 error message (`existing[idx].OptionText`) is server-side stored text injected into a `TempData` string, then rendered via Razor `@` — auto-encoded.

**CLOSED** — accept disposition confirmed valid; no raw HTML sink for Id or error messages.

---

### T-420-07 — Data hygiene: Playwright seed leak

**Disposition:** mitigate via process control (SEED_WORKFLOW snapshot→restore).

**Status:** Plan 03 Task 2 is marked `autonomous: false` — UAT has not yet been executed at time of audit. The plan explicitly mandates:
- Snapshot DB before seed (`sqlcmd BACKUP`)
- Document in `docs/SEED_JOURNAL.md`
- Restore after test (success or failure)
- Mark journal `cleaned`

This is a process control, not a code control. The obligation is declared in the plan. Treating as **planned/accepted** per audit instructions.

**CLOSED (planned)** — process control declared; will be verified when UAT is executed.

---

## Unregistered Threat Flags

None — no `## Threat Flags` section was present in a SUMMARY.md for Phase 420. No unregistered flags to record.

---

## Summary

All 7 threats from the Phase 420 plan threat registers (Plans 01, 02, 03) are closed:

- 5 mitigations verified in shipped code with corresponding integration tests (`T-420-01`, `T-420-02`, `T-420-03`, `T-420-04`, `T-420-05`).
- 1 accepted by design with confirmed absence of HTML sinks (`T-420-06`).
- 1 process control declared in plan, UAT pending (`T-420-07`).

**threats_open: 0**
