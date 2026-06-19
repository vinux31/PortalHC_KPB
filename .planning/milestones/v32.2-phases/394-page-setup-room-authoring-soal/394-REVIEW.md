---
phase: 394
status: clean
reviewed: 2026-06-18
findings_total: 4
critical: 0
high: 0
medium: 1
low: 3
---

# Phase 394 — Code Review: Inject Assessment Manual wizard page

**Scope reviewed:** `Controllers/InjectAssessmentController.cs`, `ViewModels/InjectAssessmentViewModel.cs`, `Views/Admin/InjectAssessment.cshtml`, `Views/Admin/_InjectQuestionForm.cshtml`, `Views/Admin/Index.cshtml` (Section-C card). Cross-checked against `Models/InjectAssessmentDtos.cs`, `Services/InjectAssessmentService.cs` (Phase 393), `Controllers/AdminBaseController.cs`, and `AssessmentAdminController.CheckTitleAvailability`.

**Verdict:** No Critical/High findings → `status: clean`. RBAC, CSRF, XSS, and the VM→InjectRequest mapping are all correct. One Medium wizard-navigation logic bug and three Low items.

## Security posture (verified OK — not findings)

- **RBAC:** GET + POST both carry `[Authorize(Roles = "Admin, HC")]` on top of the class-level `[Authorize]` from `AdminBaseController`. Index.cshtml gates the entry card with `User.IsInRole("Admin") || User.IsInRole("HC")`. Cross-controller link `CheckTitleAvailability` (AssessmentAdmin) has matching `Admin, HC` RBAC. No gap.
- **CSRF:** POST has `[ValidateAntiForgeryToken]`; the form emits `@Html.AntiForgeryToken()`. OK.
- **XSS:** All dynamic client text uses `.textContent` (worker names, question text, title-check matches, summary fields). No `@Html.Raw` on user input anywhere. Razor auto-encodes the server-rendered picker/option lists. OK.
- **JSON deserialize safety:** `ParseQuestionVms` wraps `JsonSerializer.Deserialize` in `try/catch (JsonException)` with a `null`-guard and a safe fallback to `vm.Questions`. The flat VM model has no recursion/polymorphism risk. OK.
- **Mapping correctness (UserId→NIP, cert gating, Order):** `MapToRequest` resolves picker `user.Id` → `NIP` via a single dictionary query (Pitfall 2 honored); `ManualCertNumber` is attached only when `CertMode == Manual`; `CertPermanent → CertValidUntil = null` (D-10) is correct; `Order` is re-indexed by enumeration position (deterministic, matches the service's `OrderBy(q => q.Order)`). OK.
- **D-07 (no DB write in 394):** Confirmed intentional — POST builds `InjectRequest` and returns a TempData message without calling `_injectService`. Not flagged.

## Findings

### MED-01 — `returnToConfirm` flag not reset on Prev / pill navigation (stale forward-jump)

**Severity:** Medium
**File:** `Views/Admin/InjectAssessment.cshtml:623,628-639,640-647`

**Problem:** `returnToConfirm` is set to `true` by any `.edit-from-confirm` button (step 6 "Edit") but is cleared **only** inside the `.btn-next` handler (line 623). If the user clicks "Edit" from Konfirmasi (jumping to e.g. step 2), then navigates away with the **Prev** button (line 635-639) or by clicking a **visited pill** (line 640-647) instead of "Selanjutnya", the flag stays `true`. The next time the user clicks any "Selanjutnya", the handler takes the `returnToConfirm` branch and jumps straight to step 6 — silently skipping all forward steps and their `validateStep` checks. Result: confusing navigation and the ability to land on Konfirmasi without re-walking edited intermediate steps. (No data corruption — the Phase 393 server preflight is authoritative — hence Medium not High.)

**Suggested fix:** Reset the flag in the Prev handler and the pill handler so it only survives an immediate Next:

```js
document.querySelectorAll('.btn-prev').forEach(function (btn) {
    btn.addEventListener('click', function () {
        returnToConfirm = false;            // cancel pending return-to-confirm
        goToStep(currentStep - 1);
    });
});
// ...and in the pill click handler:
if (pill) pill.addEventListener('click', function () {
    if (visitedSteps.has(n)) { returnToConfirm = false; goToStep(n); }
});
```

---

### LOW-01 — Konfirmasi summary omits the certificate number and validity the user entered

**Severity:** Low
**File:** `Views/Admin/InjectAssessment.cshtml:610-616` (`populateSummary`)

**Problem:** The cert summary (`#sum-cert`) only shows the selected radio *label* ("Tanpa Sertifikat" / "Generate Otomatis (Nomor Resmi)" / "Input Nomor Manual"). When mode = Manual it never echoes the entered `ManualCertNumber`, and for Auto/Manual it never shows the `CertValidUntil` / "permanen" choice. The Konfirmasi step's purpose is to let the reviewer verify exactly what will be injected, so the most error-prone fields (a hand-typed cert number, a backdated validity) are invisible at review time. Pure UX/completeness gap — the values still post correctly.

**Suggested fix:** Append validity + manual number to the cert summary line, e.g.:

```js
if (certChecked && certChecked.value === 'Manual') {
    var num = val('NomorSertifikat');
    if (num) certLabel += ' — No. ' + num;
}
if (certChecked && certChecked.value !== 'None') {
    var perm = document.getElementById('CertPermanent');
    certLabel += (perm && perm.checked) ? ' · Permanen'
        : (val('CertValidUntil') ? ' · s/d ' + val('CertValidUntil') : ' · tanpa batas');
}
```

---

### LOW-02 — POST user lookup does not filter `IsActive`, unlike the picker feed

**Severity:** Low
**File:** `Controllers/InjectAssessmentController.cs:56-58` vs `:144-148` (`PopulateFeedAsync`)

**Problem:** `PopulateFeedAsync` only renders `IsActive` users in the picker, but the POST resolution `_context.Users.Where(u => userIds.Contains(u.Id))` has no `IsActive` filter. A crafted POST (or a user deactivated between GET and POST) could therefore resolve a deactivated worker into the request. Low impact in 394 because there is no DB write and the Phase 393 preflight re-resolves/validates by NIP at commit; flagging so Phase 395 doesn't inherit a silent "inject for deactivated worker" path.

**Suggested fix:** Mirror the feed filter at resolution time, or defer to the 395 preflight explicitly:

```csharp
var userIdToNip = await _context.Users
    .Where(u => userIds.Contains(u.Id) && u.IsActive)
    .ToDictionaryAsync(u => u.Id, u => u.NIP ?? "");
```

(If deactivated-but-historical workers should be injectable, leave as-is and add a note that 393 preflight is the gate.)

---

### LOW-03 — Step pills lack `aria-current`/role semantics for the active step

**Severity:** Low
**File:** `Views/Admin/InjectAssessment.cshtml:78-111`, `updatePills` `:522-549`

**Problem:** The step indicator pills are real `<button>`s with `disabled` toggling (good keyboard/AT behavior), but the active step is conveyed only via color classes and an icon swap — there is no `aria-current="step"` on the active pill and no live announcement when `goToStep` switches panels. Screen-reader users get no programmatic "current step" cue. Note: this is not a regression vs the mirrored `CreateAssessment.cshtml`, which is also class-only — hence Low.

**Suggested fix:** In `updatePills`, set `pill.setAttribute('aria-current', i === currentStep ? 'step' : 'false')` (or `removeAttribute` when not current). Optionally give `#wizardStepNav` an `aria-label="Langkah wizard"`.

---

## Items explicitly checked and found NOT to be issues

- **POST does not persist** — intentional (D-07). Not flagged.
- **Client-side `validateStep` can be bypassed** — convenience-only; 393 preflight is server-authoritative. Not flagged.
- **Authoring never posts to the per-question create endpoint** — by design (Pitfall 1); soal held in `injQuestions[]` and serialized to `#QuestionsJson` on submit. Correct.
- **`Order = i` overwrites JSON `Order`** — deterministic and consistent with the service's `OrderBy(q => q.Order)`. Correct.
- **`asp-for="Category"` + explicit `id="Category"`** — explicit id wins; no duplicate-id emitted; same `name`. Fine.
- **`data-pass-percentage` on category options** — present for a future auto-fill hook; no dead JS depends on it failing. Fine.
- **VM fields missing vs DTO (StartedAt/Schedule/LinkedGroupId/LinkedSessionId/image/maxChars)** — DTO also defaults these; intentional surface mirror. Not flagged (per review brief).

---

_Reviewed: 2026-06-18 — Claude (gsd-code-reviewer), depth: standard (cross-file)._
