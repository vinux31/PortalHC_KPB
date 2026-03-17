# Pitfalls Research

**Domain:** Assessment form revamp — wizard UX, DB-driven categories, clone, ValidUntil, auto-numbering, QuestPDF certificate
**Researched:** 2026-03-17
**Confidence:** HIGH — based on direct inspection of CreateAssessment.cshtml (1023 lines), AssessmentSession model, AdminController, CDPController QuestPDF usage, and existing migration history

---

## Critical Pitfalls

### Pitfall 1: Wizard State Lives Only in JS — Server-Side Validation Gaps on Submit

**What goes wrong:**
A multi-step wizard is implemented by showing/hiding `<div>` panels via JS. Each "Next" button validates only the visible step client-side. On final submit, the entire form posts to the existing `CreateAssessment` action. If someone bypasses the wizard (curl, Postman, or JS disabled) they can skip Step 1 (category) entirely and submit with `Category = ""`. The controller action accepts the model, EF inserts a row with an empty string category, and the monitoring page breaks on null/empty category grouping.

**Why it happens:**
Wizard step validation is purely presentational — it sits in JS, not in model attributes or controller logic. The controller trusts the incoming form data. The existing form uses `[Required]` on `Category`, but if the wizard resets the hidden field between steps incorrectly, it can arrive as empty string (which passes `[Required]`).

**How to avoid:**
Keep `[Required]` on the `Category` binding target. Add an explicit `ModelState` check at the top of the `[HttpPost] CreateAssessment` action: if `Category` is whitespace, add a `ModelState` error and return the view. Never rely solely on client-side wizard guards for required business fields. Treat the client wizard as a UX layer only.

**Warning signs:**
- `AssessmentSession` rows in DB with empty or null `Category`
- Monitoring grouping query throws `NullReferenceException` on category header rendering
- Assessment list shows a blank category badge

**Phase to address:**
Wizard form implementation phase. Establish the rule upfront: wizard JS validates for UX smoothness; server-side `ModelState` is the authoritative gate.

---

### Pitfall 2: Category PassPercentage Defaults Duplicated Between JS and DB — They Diverge

**What goes wrong:**
The current `CreateAssessment.cshtml` contains a hardcoded JS object `categoryDefaults` with PassPercentage values per category name. When categories are moved to the DB, developers add a `DefaultPassPercentage` column to the `AssessmentCategory` table. A new category is added to the DB. But nobody updates the JS `categoryDefaults` object. The wizard now shows the wrong default for that category when the user selects it, because the JS still drives the client-side auto-fill.

**Why it happens:**
There are two sources of truth. The DB becomes authoritative for storage, but the JS object still drives the UI behavior. The two are not connected — the JS was copied-and-pasted from the original form without being replaced.

**How to avoid:**
Eliminate the JS `categoryDefaults` object entirely. When the categories dropdown is rendered, embed the `DefaultPassPercentage` into the `<option>` element as a `data-pass-percentage` attribute:
`<option value="OJT" data-pass-percentage="70">OJT</option>`
The JS `change` handler reads `selectedOption.dataset.passPercentage` instead of looking up a hardcoded map. The DB is the single source of truth. No copy to maintain.

**Warning signs:**
- A newly added DB category shows 70 (the JS fallback default) instead of its configured default when selected
- Editing an existing category's `DefaultPassPercentage` in the DB has no effect on the wizard behavior
- JS `categoryDefaults` object still present in the view source after DB categories are shipped

**Phase to address:**
DB categories phase. Delete `categoryDefaults` in the same PR that wires the `<option data-pass-percentage>` attributes. Never leave both alive simultaneously.

---

### Pitfall 3: Clone Creates Deep Copy of Questions Partially — Leaves Orphaned Question Records

**What goes wrong:**
`AssessmentSession` questions can come from two sources: the old `AssessmentQuestion` table (direct session questions) and the newer `AssessmentPackage → PackageQuestion → PackageOption` hierarchy. A clone action copies `AssessmentSession` fields and iterates `session.Questions` (the `AssessmentQuestion` navigation property), but misses cloning the `AssessmentPackage` tree. The cloned session appears valid in the create flow, but the exam engine that reads from `PackageQuestion` finds nothing, so the exam page renders empty.

**Why it happens:**
The `AssessmentSession` model has two separate question collections: `ICollection<AssessmentQuestion> Questions` (legacy/direct) and `AssessmentPackage` rows linked by `AssessmentPackage.AssessmentSessionId`. A clone that only includes `.Include(s => s.Questions)` is incomplete.

**How to avoid:**
Before writing the clone action, query what question structure the exam engine actually reads. Check the `PackageExam` action in `CMPController` — if it loads `AssessmentPackage` rows, the clone must deep-copy those too with new IDs. Load: `session.Include(s => s.Questions).Include(s => s.Packages).ThenInclude(p => p.Questions).ThenInclude(q => q.Options)`. Create new entity instances for every level — never reuse existing IDs or navigation objects.

**Warning signs:**
- Cloned session shows "0 soal" in the ManageAssessment list
- Exam page for the cloned session renders empty question area
- `AssessmentPackage` rows in DB for cloned session have zero `PackageQuestion` children

**Phase to address:**
Clone phase. Read the exam engine source (`CMPController` exam-taking actions) before writing clone logic to understand the full entity graph that must be duplicated.

---

### Pitfall 4: ValidUntil Added to AssessmentSession — Existing Views Break on Missing Column

**What goes wrong:**
`AssessmentSession` currently has 20+ fields. Adding `ValidUntil DateTime?` with a migration is safe for the DB, but existing LINQ projections that use `.Select(s => new SomeViewModel { ... })` with explicit column lists will silently omit the new field. The Monitoring, Records, Certificate, and Results views all query `AssessmentSession` — any ViewModel or anonymous type that was built with a fixed list will now be stale.

**Why it happens:**
EF Core projections with explicit `.Select()` do not fail at compile time when a new property is added to the entity but not to the projection. The column is added to the DB but invisible in several views because nobody updated those projections.

**How to avoid:**
After running the migration, grep all `.Select(s => new` and `new AssessmentSession` in Controllers for incomplete projections. For views that will eventually need to display `ValidUntil` (Records, Certificate, Monitoring), add the field to the ViewModel and projection in the same PR as the migration, even if the view renders it as "N/A" initially. This prevents silent data loss later.

**Warning signs:**
- Certificate view shows blank or default expiry date even after the field is set in the DB
- Records export omits the `ValidUntil` column when the template explicitly includes it
- `AssessmentMonitoringViewModel` loads correctly but certificate expiry is always null

**Phase to address:**
ValidUntil migration phase. As part of the migration PR: add `ValidUntil` to the entity, add it to every downstream ViewModel that projects `AssessmentSession`, and display "Permanen" where not set.

---

### Pitfall 5: Auto-Numbering Race Condition — Duplicate NomorSertifikat Under Concurrent Submits

**What goes wrong:**
Certificate auto-numbering reads the last-used number, increments it, and writes it back. Under concurrent exam completions (two users finishing at the same second), both reads return the same current max number. Both insert with the same incremented value. The DB unique constraint catches the second insert and throws an unhandled `DbUpdateException`, crashing the exam completion flow for the second user — who gets a 500 error with no indication that their exam score was saved.

**Why it happens:**
Read-increment-write patterns are not atomic in EF Core without explicit locking. This pattern is safe for single-user sequential use but breaks under any concurrency.

**How to avoid:**
Use a DB sequence or a single `INSERT ... SELECT MAX(n)+1` pattern with a retry loop. The pragmatic approach for this system's scale: add a `UNIQUE` constraint on `NomorSertifikat` in the migration, wrap the number generation in a retry loop (up to 3 attempts), and catch `DbUpdateException` specifically to retry with a re-read max. Never generate the number in application memory and trust it to be unique. Alternatively, use a `CertificateSequence` table with a single row and update it using optimistic concurrency (`[ConcurrencyCheck]`).

**Warning signs:**
- Exam completion returns 500 errors during batch assessment events (many workers finishing simultaneously)
- Duplicate certificate numbers in DB (detectable by `GROUP BY NomorSertifikat HAVING COUNT(*) > 1`)
- `DbUpdateException` in logs referencing the unique constraint

**Phase to address:**
Auto-numbering phase. Implement with a retry loop and the unique constraint from day one — do not add the constraint as a "hardening" step later.

---

### Pitfall 6: QuestPDF Certificate Layout Hard-Codes Absolute Positions — Breaks with Long Names

**What goes wrong:**
QuestPDF fluent API uses relative layout by default, but developers coming from HTML/CSS often switch to absolute pixel positions (`element.Absolute(x, y)`) to match a design mockup precisely. The layout looks correct for short names like "Budi S." but breaks for "Retno Wulandari Sulistyaningsih" — the text overflows its container, overlaps the logo, or clips entirely because the container width was not set to grow.

**Why it happens:**
The existing CDPController QuestPDF usage (ProtonProgress export) is a table layout, not a certificate. Tables handle overflow naturally. Certificate layouts require decorative positioning. Developers reach for absolute positioning to match the design — a habit from HTML.

**How to avoid:**
Use QuestPDF's relative layout system: `.Column()`, `.Row()`, `.AlignCenter()`, `.AlignMiddle()`. Set text containers with `.Width(Percent(80))` or use flexible column widths. Test with the longest plausible worker name in your user base (check the Workers table for the longest `FullName` value). Never use `element.Absolute()` for content containers.

**Warning signs:**
- Certificate PDF looks correct in dev (seeded data uses short names) but clips names in production
- Worker name overlaps the organization logo or signature line
- Certificate has a fixed pixel height that causes content cutoff when title text wraps to two lines

**Phase to address:**
QuestPDF certificate phase. Before finalizing the layout, test with a worker whose `FullName` exceeds 40 characters and with an assessment `Title` that wraps to two lines.

---

### Pitfall 7: Wizard's Multi-User Selection State Lost on Browser Back — Duplicate Submissions

**What goes wrong:**
The existing `CreateAssessment` form uses complex AJAX for multi-user selection (search, add, remove, show counts). In a wizard, the user completes Step 1 (category), Step 2 (users), Step 3 (settings), and reaches Step 4 (confirm). They click "Back" to Step 2 to change users. The JS wizard shows Step 2, but the hidden input `selectedUsers` was already populated on the first pass. Without explicit re-initialization, the displayed user list and the hidden input go out of sync. The user removes one person from the visible list but the hidden input still contains the original full list.

**Why it happens:**
Wizard "Back" is typically implemented by just toggling `display:none/block` on step panels. The underlying form state (hidden inputs, JS arrays, rendered lists) retains its last value. The user sees a UI that looks editable but is writing to a stale backing store.

**How to avoid:**
Treat the user selection JS module as having an explicit `getSelectedUsers()` getter and a `setSelectedUsers(array)` setter. On "Back" navigation, call `setSelectedUsers(currentFormState)` to re-initialize the displayed list from the form's hidden input. Verify that the hidden input always reflects the DOM list, not the other way around — the DOM list is ephemeral; the hidden input is the source of truth.

**Warning signs:**
- Submitted assessment has more users than the user saw in the confirmation step
- Removing a user on Step 2 after a "Back" click has no effect — the user appears in the DB-created sessions
- `AssessmentSession` rows are created for workers the admin did not select in the final wizard state

**Phase to address:**
Wizard form implementation phase. Implement Back navigation with explicit state re-sync from the start — do not add it as a fix after the wizard is "complete."

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Keep JS `categoryDefaults` alongside DB categories during transition | No need to wire `data-pass-percentage` on options immediately | Two sources of truth diverge when categories change; bugs manifest silently | Never — delete JS map in the same commit that adds `data-pass-percentage` to options |
| Clone only top-level session fields, skip question deep-copy | Faster to code | Cloned sessions have zero questions; exam engine silently fails | Never — clone is only useful if the cloned session is functional |
| Generate certificate number without a UNIQUE constraint | Simpler migration | Duplicates slip through under concurrency; audit liability for HR | Never for certificate numbering — the constraint is non-negotiable |
| Use absolute positioning in QuestPDF certificate layout | Easier to match design mockup | Long names / long titles overflow silently in production | Never — use relative layout; test with longest data |
| Add `ValidUntil` to model but skip updating downstream ViewModels until "later" | Faster migration phase | Silent null in certificate views; requires re-audit of all AssessmentSession projections | Only acceptable if ViewModel update is a committed follow-up task in the same phase |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| QuestPDF certificate with dynamic content | Copy the CDPController table-based QuestPDF pattern directly | CDPController uses `.Table()` for grid data — certificate layout uses `.Column()` / `.Row()` / `.Text()` with decorative elements; the APIs are the same but the patterns differ; start from QuestPDF Fluent API docs, not from the table pattern |
| DB-driven category dropdown | Load categories in `GET CreateAssessment` only; skip `GET EditAssessment` | Both `CreateAssessment` and `EditAssessment` need `AssessmentCategory` loaded into `ViewBag` / ViewModel; also `CloneAssessment` which prepopulates the form |
| AssessmentSession migration with ValidUntil | `nullable DateTime?` column added — no explicit default needed | Existing rows get `NULL`; queries that check `ValidUntil != null` are correct; views must display "Permanen" for null rather than blank — enforce this pattern in the mapping layer, not in each view separately |
| Clone action with question deep-copy | Use `.AsNoTracking()` on the source query then attach cloned entities | EF tracks original entities by primary key; if you reuse the same entity instances without creating new ones with `Id = 0`, EF attempts to update the originals instead of inserting clones; always `new Entity { Id = 0, ... }` for every cloned child |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Deep-copy clone loads entire question tree for large packages | Clone action times out or is slow | For packages with 100+ questions, use raw SQL `INSERT INTO ... SELECT` within a transaction rather than EF object graph traversal | Packages with >50 questions per session |
| Certificate PDF generation in the HTTP request cycle | Slow response (>3s) for complex PDF layouts with images | QuestPDF is synchronous but fast for simple layouts; avoid embedding large base64 images inline; use file-referenced images via `Image(filePath)` not `ImageFromStream` with large buffers | Layouts with embedded logos > 200KB |
| Wizard step validation on every keypress | Sluggish form on mobile if AJAX fires on `oninput` | Validate on `blur` and on "Next" click; do not fire AJAX on every keystroke in user search | Always wasteful if AJAX-backed |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Clone action does not re-check ownership / role | A Coachee with a guessed session ID can clone any session | Add `[Authorize(Roles = "Admin,HC")]` to the clone action; never expose clone to non-admin roles without ownership verification |
| Certificate PDF action streams file without verifying the requesting user is the session owner or an admin | Worker A downloads Worker B's PDF | Before generating / streaming: verify `session.UserId == currentUser.Id` OR `currentUser.IsInRole("Admin")` OR `currentUser.IsInRole("HC")`; return 403 otherwise |
| Auto-number format leaks session count | Sequential numbers like `SERT-0001` reveal total certificate volume | Use a format with a year prefix and left-pad: `SERT/2026/0001`; still sequential but harder to enumerate across years |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Wizard "Next" button disabled with no explanation when required fields are missing | Admin is confused about why they cannot advance | Show inline validation summary when "Next" is clicked and validation fails; never silently disable "Next" |
| Confirm step shows user count ("5 pekerja") but not names | Admin cannot verify they selected the right people | Show a truncated list (first 5 names + "dan N lainnya") on the confirm step; clicking expands the full list |
| Clone pre-fills the Schedule with the original session's past date | Admin creates an assessment with a date in the past without noticing | Default cloned `Schedule` to today + 7 days; let admin edit; display a warning if the Schedule is in the past before submitting |
| Wizard step indicator is not clickable — admin must use Back/Next buttons | Tedious to jump back to Step 1 from Step 4 | Make completed step indicators clickable (navigate back with state preservation); only prevent skipping forward past the current completed step |
| Certificate PDF downloaded with generic filename `document.pdf` | Hard to identify file in downloads folder | Set `Content-Disposition: attachment; filename="Sertifikat_{NIP}_{Title}_{Year}.pdf"` in the response header |

---

## "Looks Done But Isn't" Checklist

- [ ] **DB categories complete:** `categoryDefaults` JS object has been deleted from `CreateAssessment.cshtml` and `EditAssessment.cshtml` — verify by viewing source; no hardcoded category map present
- [ ] **Clone functional:** Cloned session has the same question count in ManageAssessment list as the source — verify by cloning a session with packages and checking `AssessmentPackage` + `PackageQuestion` rows in DB
- [ ] **ValidUntil in all downstream views:** After adding `ValidUntil` to `AssessmentSession`, confirm Records, Certificate, and AssessmentMonitoring views all display the value (or "Permanen") — not blank
- [ ] **Auto-number uniqueness enforced:** Unique constraint exists on `NomorSertifikat` in the migration — verify with `SHOW INDEX FROM AssessmentSessions` or equivalent; do not rely on application-level uniqueness alone
- [ ] **Wizard Back/Next preserves user selection:** After advancing to Step 3, clicking Back to Step 2, removing a user, and completing the wizard — the removed user does NOT appear in the created `AssessmentSession` rows
- [ ] **QuestPDF certificate tested with long name:** Download a certificate for a worker with a `FullName` longer than 35 characters — verify no text overflow or clipping
- [ ] **PassPercentage default from DB:** Create a new assessment; select each category in sequence; verify the `PassPercentage` field auto-fills with the value from the DB (not a hardcoded JS value) — confirm by temporarily changing a category's `DefaultPassPercentage` in the DB and checking the form behavior

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| JS `categoryDefaults` diverged from DB | LOW | Delete JS object; add `data-pass-percentage` to `<option>` elements in the category dropdown partial; update `change` handler to read `dataset.passPercentage` |
| Clone created incomplete sessions (no questions) | MEDIUM | Write a one-time migration/script that identifies cloned sessions with zero packages and deletes them; re-implement clone with full deep-copy; notify admins who used clone to recreate |
| Duplicate `NomorSertifikat` values in DB | HIGH | Add unique constraint with `CREATE UNIQUE INDEX ... IGNORE ERRORS` to detect existing dupes first; resolve dupes manually with HC; then enforce constraint; add retry logic to generation code |
| ValidUntil silently null in views | LOW | Add `ValidUntil` to missing ViewModel projections; no schema change needed; re-run affected pages |
| QuestPDF layout clips long names | MEDIUM | Switch from absolute to relative layout for affected containers; re-test all certificate templates; no DB change |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Wizard server-side validation gap (Pitfall 1) | Wizard form implementation phase | Submit the form without a category via Postman; confirm `ModelState` error is returned |
| JS/DB category defaults divergence (Pitfall 2) | DB categories phase | Change a category's `DefaultPassPercentage` in DB; reload form; verify auto-fill reflects DB value |
| Clone partial deep-copy (Pitfall 3) | Clone phase | Clone a session with packages; confirm `PackageQuestion` count matches original in DB |
| ValidUntil missing in downstream views (Pitfall 4) | ValidUntil migration phase | Set `ValidUntil` on a session; load Records, Certificate, and Monitoring views; verify value displayed |
| Auto-number race condition (Pitfall 5) | Auto-numbering phase | Verify UNIQUE constraint exists; simulate two concurrent completions in dev; confirm no duplicates |
| QuestPDF overflow on long names (Pitfall 6) | QuestPDF certificate phase | Test PDF download for the worker with the longest `FullName` in the DB |
| Wizard Back/Next user selection desync (Pitfall 7) | Wizard form implementation phase | Add 5 users in Step 2, advance to Step 3, go Back, remove 2 users, submit; confirm 3 sessions created |

---

## Sources

- Direct inspection of `Views/Admin/CreateAssessment.cshtml` — `categoryDefaults` JS object at line 538, `passPercentageManuallySet` flag pattern confirmed
- Direct inspection of `Models/AssessmentSession.cs` — no `ValidUntil` field, `IsPassed bool?`, `GenerateCertificate bool`, dual question sources (`Questions` + no direct `Packages` navigation but linked via `AssessmentPackage.AssessmentSessionId`)
- Direct inspection of `Models/AssessmentPackage.cs` — full three-level hierarchy: `AssessmentPackage → PackageQuestion → PackageOption`
- Direct inspection of `Controllers/CDPController.cs` lines 2225–2256 — existing QuestPDF usage pattern (table-based, landscape A4)
- Direct inspection of `Controllers/AdminController.cs` — `NomorSertifikat` already used in `TrainingRecord` context (lines 5106, 5148) with no current unique constraint enforcement on `AssessmentSession`
- Migration history inspection — no existing `ValidUntil` or `NomorSertifikat` migration for `AssessmentSession`
- `PROJECT.md` v7.5 milestone definition — wizard, DB categories, clone, ValidUntil, auto-numbering, QuestPDF certificate confirmed as target features

---
*Pitfalls research for: Assessment form revamp — wizard UX, DB-driven categories, clone, ValidUntil, auto-numbering, QuestPDF certificate*
*Researched: 2026-03-17*
