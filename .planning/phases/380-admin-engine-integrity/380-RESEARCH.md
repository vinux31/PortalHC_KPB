# Phase 380: Admin/Engine Integrity - Research

**Researched:** 2026-06-14
**Domain:** ASP.NET Core MVC (C# / EF Core / xUnit / Playwright) — assessment engine + admin controller bug-fix
**Confidence:** HIGH (all anchors verified line-by-line against actual current code in this session)

## Summary

Phase 380 is an **audit-driven bug-fix** phase. The audit (`2026-06-14-E2E-worker-success-FOCUS.md` + master findings) already specified each defect with file:line + recommended fix. This research **re-verified every anchor against the actual current source** (line numbers have NOT drifted — they match the audit exactly) and confirmed all three bugs exist precisely as described. No external libraries, no new dependencies, no migration. All fixes are in-place edits to three existing files plus new tests.

The three fixes are small, surgical, and well-isolated:
- **WSE-01 / SHF-01** — `Helpers/ShuffleEngine.cs:108` ON-path computes `K = packages.Min(p => p.Questions.Count)` without filtering empty packages; the OFF-path at `:53-57` already filters. Mirror the OFF filter into the ON-path. Engine is pure (no DB) → trivially unit-testable. Fix at engine auto-covers all 3 callers (StartExam, ReshufflePackage, ReshuffleAll) — verified.
- **WSE-02 / TOK-01** — `Controllers/CMPController.cs:876` compares `assessment.AccessToken != token.ToUpper()` (only input uppercased). Pre/Post EditAssessment writes token verbatim at `:1812/1916/1937` (no `.ToUpper()`), returns at `:1957` before the single-mode uppercase logic at `:2012`. Defensive both-sides-uppercase at `:876` auto-heals existing lowercase tokens; uppercase-on-write at the 3 edit sites for data cleanliness.
- **WSE-03 / RST-01+RST-04** — `Controllers/AssessmentAdminController.cs:6866` `AddExtraTime` has only `[HttpPost]+[ValidateAntiForgeryToken]`, no `[Authorize(Roles=...)]` (any authenticated user, incl. worker). Add `[Authorize(Roles = "Admin, HC")]` (exact sibling string). Add total-cap so accumulated `ExtraTimeMinutes` ≤ original `DurationMinutes`.

**Primary recommendation:** Implement as two independent work units (ShuffleEngine fix + AddExtraTime authz/cap in AssessmentAdminController) plus the token fix split across CMPController (compare) and AssessmentAdminController (write). Add pure xUnit unit tests mirroring `ShuffleEngineTests.cs` and reflection-authz test mirroring `CDPControllerAuthTests.cs`, plus two Playwright E2E scenarios (#5 token, #6 empty-package). No migration; `migration=false` confirmed.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**TOK-01 — Token Pre/Post (WSE-02)**
- **D-01:** Fix **defensive compare + write** (NOT forward-only, NOT repair-script). Two sides:
  1. **Compare:** `VerifyToken` compares UPPERCASE both sides — `(assessment.AccessToken ?? "").Trim().ToUpper() == (token ?? "").Trim().ToUpper()`. This **auto-heals ALL existing lowercase tokens** without migration/script — currently locked-out workers recover instantly.
  2. **Write:** Still store token uppercase in `EditAssessment` Pre/Post branch (3 write sites: ~1812/1916/1937) for data cleanliness, mirroring `CreateAssessment` pattern (~1107).
- **Rationale:** Zero DB touch (no migration, no IT action), instant heal, defensive against future drift.

**RST-01/04 — AddExtraTime (WSE-03)**
- **D-02:** AddExtraTime → `[Authorize(Roles="Admin,HC")]` (match sibling action ResetAssessment/AkhiriUjian). HC keeps operational access; participant/worker blocked.
- **D-03:** Cap total extra time **≤ original exam duration** (`DurationMinutes`). Accumulated `ExtraTimeMinutes` must not exceed original duration (60-min exam → max +60, total 2×). Reject grants that would exceed, with clear message. Scales automatically with exam length.

**SHF-01 — Empty package + shuffle ON (WSE-01)**
- **D-04:** Fix in **engine** `BuildCrossPackageAssignment` ON-path — filter empty packages (`p.Questions != null && p.Questions.Count > 0`) **before** computing `K = Min(...)`, mirror OFF-path (ShuffleEngine.cs:53-57). Fix in engine → automatically covers **StartExam + ReshufflePackage + ReshuffleAll** (3 callers).
- **D-05:** Edge **all-empty** (all sibling packages 0 questions): StartExam **BLOCKS + friendly message** ("Ujian belum siap / belum ada soal — hubungi admin"), DO NOT write StartedAt/Status=InProgress, DO NOT create UserPackageAssignment, DO NOT auto-grade. Prevent false 0% Fail. (Common case 1-empty-among-several still works: worker gets questions from filled packages.)

### Claude's Discretion
- Exact wording of all-empty message (BI, friendly, direct to admin).
- Placement of all-empty guard (in StartExam after engine returns empty list — before any write).
- Whether the RST-04 cap is checked in the server action only (sufficient) or also hinted in the UI (optional).
- Shape of the cap extra-time rejection message.

### Deferred Ideas (OUT OF SCOPE)
- **One-time cleanup data test/audit lokal pasca-367** (`.planning/todos/pending/2026-06-11-...md`) — local DB cleanup chore, **unrelated** to Phase 380 fix scope. Leave in pending.
- (Milestone-level defer RES-02/GRD-02 noted in REQUIREMENTS.md, not this phase.)
- OUT (roadmap-locked): Proton, essay, multi-answer, exam-taking UI changes, admin data-governance. Worker-entry StartExam mutation (same-day Pre/Post, impersonation) = Phase 381. Grading/lifecycle/cert = Phase 382.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WSE-01 | Worker still receives a non-empty question set when one sibling package is empty + shuffle ON (default). (SHF-01) | Confirmed `ShuffleEngine.cs:108` ON-path lacks empty-filter; OFF-path `:53-57` is exact template. Fix mirrors OFF filter before `K=Min`. All 3 callers verified to consume `BuildQuestionAssignment`/`BuildCrossPackageAssignment`. |
| WSE-02 | Worker with token-required Pre/Post exam can enter using the token after admin edits the token. (TOK-01) | Confirmed `CMPController.cs:876` single-side uppercase compare; Pre/Post write sites `:1812/1916/1937` lack `.ToUpper()`; branch returns `:1957` before uppercase at `:2012`. Client also force-uppercases at `Assessment.cshtml:757`. Defensive both-sides fix is safe (`:876` is the ONLY token compare in the codebase). |
| WSE-03 | Only Admin/HC may grant extra time, and total extra time is capped. (RST-01 + RST-04) | Confirmed `AddExtraTime` (`:6866`) lacks `[Authorize(Roles)]`; sibling `ResetAssessment` (`:3998`) uses `[Authorize(Roles = "Admin, HC")]`. Accumulation at `:6899` has no ceiling; `DurationMinutes` is the original-duration field; `ExtraTimeMinutes` is `int?` accumulator. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Question distribution / shuffle (SHF-01) | Engine (pure helper `ShuffleEngine`) | — | Pure, DB-free domain logic; single source of truth consumed by 3 controller callers. Fix belongs here so all callers heal at once. |
| All-empty StartExam guard (D-05) | API/Backend (`CMPController.StartExam` GET) | — | Decision to block + message + suppress writes is a request-handler concern, not engine concern. Engine just returns `[]`; controller decides how to react. |
| Token verification (TOK-01 compare) | API/Backend (`CMPController.VerifyToken`) | — | Authorization/access-gate logic at the request boundary. |
| Token persistence/normalization (TOK-01 write) | API/Backend (`AssessmentAdminController.EditAssessment`) | — | Data-write normalization at the admin write path. |
| Extra-time authorization + cap (RST-01/04) | API/Backend (`AssessmentAdminController.AddExtraTime`) | — | Role gate (attribute) + server-side business-rule validation at the admin action. |

**Tier sanity note for planner:** All five capabilities live in the C# backend tier (engine + controllers). There is NO client-tier change required (the optional UI hint for the cap, D-discretion, would touch a Razor view but is explicitly optional). The token client-input uppercase already exists (`Assessment.cshtml:757`) and must NOT be removed.

## Standard Stack

No new packages. This is an in-place bug-fix on the existing stack. Versions below are the project's current toolchain (verify with `dotnet --info` if needed).

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET / ASP.NET Core MVC | net8.0 [VERIFIED: `HcPortal.Tests/obj/.../net8.0`] | Web app + controllers | Existing project framework |
| EF Core | (project current) [ASSUMED] | DB access in controllers | Existing data layer; no schema change this phase |
| xUnit | (project current) [VERIFIED: existing `HcPortal.Tests/*.cs` use `using Xunit;`] | Unit/integration tests | Established test framework (60+ test files) |
| Playwright (`@playwright/test`) | (project current) [VERIFIED: `tests/package.json` "test": "npx playwright test"] | E2E browser tests | Established e2e harness with rich helpers |

### Supporting (test templates — reuse, don't reinvent)
| Asset | Path | Purpose |
|-------|------|---------|
| `ShuffleEngineTests.cs` | `HcPortal.Tests/ShuffleEngineTests.cs` | Pure-unit template; `Pkg(...)` in-memory builder; ON/OFF assertions. Add new ON-path empty-package facts HERE (or a sibling file). |
| `CDPControllerAuthTests.cs` | `HcPortal.Tests/CDPControllerAuthTests.cs` | Reflection-authz template — `typeof(X).GetMethod(...).GetCustomAttributes(typeof(AuthorizeAttribute))` → assert `.Roles`. Exact pattern for AddExtraTime authz test. |
| `ShuffleReshuffleTests.cs` | `HcPortal.Tests/ShuffleReshuffleTests.cs` | Pure-unit reshuffle template (option-shuffle). |
| `examTypes.ts` | `tests/e2e/helpers/examTypes.ts` | E2E wizard helpers: `createAssessmentViaWizard` (supports `isTokenRequired`/`accessToken` lines 130-137), `createDefaultPackage`, `addQuestionViaForm`, `submitExamTwoStep`, `addExtraTimeViaModal` (line 535). |
| `dbSnapshot.ts` | `tests/helpers/dbSnapshot.ts` | `backup`, `restore`, `execScript`, `queryScalar`, `queryString` — DB assertions for e2e (verify Score, Status, token persisted). |
| `auth.ts` / `utils.ts` | `tests/helpers/` | `login(page,'hc'|'coachee')`, `uniqueTitle`, `today`. |

**Installation:** None. `dotnet build` + `dotnet test` (xUnit), `npx playwright test` (e2e).

## Architecture Patterns

### Data Flow Diagram — the three fix paths

```
WSE-01 (SHF-01) — empty package + shuffle ON
  HC creates Asm with ≥2 packages, one still has 0 questions (common authoring state)
        │
        ▼
  Worker → GET /CMP/StartExam/{id}
        │  (CMPController.cs:888)
        ├─► [960-967] justStarted? → WRITE Status=InProgress + StartedAt   ◄── D-05 GOTCHA: write happens BEFORE engine call
        ├─► [995-1000] load sibling packages (.Include Questions) — NO empty filter
        ├─► [1019] ShuffleEngine.BuildQuestionAssignment(packages, ShuffleQuestions=ON, …)
        │            │
        │            ▼  ON-path → BuildCrossPackageAssignment [91]
        │            [108] K = packages.Min(p => p.Questions.Count)   ◄── BUG: empty pkg ⇒ K=0
        │            [109-110] if (K==0) return []                    ◄── everyone gets 0 questions
        │   FIX D-04: filter empties BEFORE K (mirror OFF :53-57)
        ├─► [1012-1042] create UserPackageAssignment, SavedQuestionCount=0   ◄── locks empty set
        └─► renders empty exam → submit → maxScore=0 → 0% Fail batch-wide
   FIX D-05: if shuffledIds empty AND ≥1 package exists → BLOCK + message, no assignment write

WSE-02 (TOK-01) — lowercase token lockout
  Admin → POST /Admin/EditAssessment (Pre/Post branch)
        ├─ [1812] s.AccessToken = … (NO .ToUpper)   ┐
        ├─ [1916] new Pre  AccessToken = … (NO .ToUpper)  ├─ stores lowercase
        ├─ [1937] new Post AccessToken = … (NO .ToUpper)  ┘
        └─ [1957] return  (never reaches uppercase at :2012)
  Worker → token modal (input force-UPPERCASE, Assessment.cshtml:757)
        └─ POST /CMP/VerifyToken → [876] assessment.AccessToken != token.ToUpper()
                                          (stored lowercase ≠ uppercase input) ⇒ "Token tidak valid" PERMANENT
   FIX D-01a: [876] compare both sides upper+trim  → auto-heals existing lowercase
   FIX D-01b: [1812/1916/1937] write .ToUpper()    → data cleanliness for new edits

WSE-03 (RST-01/04) — AddExtraTime authz + cap
  Any authenticated user (incl. worker) → POST /Admin/AddExtraTime
        ├─ [6866-6868] [HttpPost][ValidateAntiForgeryToken] ONLY — no role gate   ◄── BUG RST-01
        ├─ [6870] per-call bound 5..120 min only
        └─ [6897-6899] foreach InProgress session: ExtraTimeMinutes += minutes  ◄── BUG RST-04 no total cap
   FIX D-02: add [Authorize(Roles = "Admin, HC")] (mirror :3998)
   FIX D-03: reject if (existing + minutes) > original DurationMinutes
```

### Pattern 1: Mirror the OFF-path empty filter into ON-path (D-04)
**What:** Apply the exact filter the OFF-path already uses, before computing K.
**When to use:** SHF-01 fix in `BuildCrossPackageAssignment`.
**Template (OFF-path, lines 53-57 — VERIFIED current source):**
```csharp
// Source: Helpers/ShuffleEngine.cs:53-57 (OFF-path, the canonical template)
var packagesWithQuestions = packages
    .Where(p => p.Questions != null && p.Questions.Count > 0)   // D-02b: guard SEBELUM modulo
    .OrderBy(p => p.PackageNumber)
    .ToList();
if (packagesWithQuestions.Count == 0) return new List<int>();   // guard
```
**Apply to ON-path (BuildCrossPackageAssignment, before line 108):** filter `packages` to non-empty, early-return `[]` if none remain, then `K = filtered.Min(...)`. NOTE: the single-package early-return (`:97-105`) already handles `Count==1`; the filter must run on the `≥2` fall-through. Recommended: filter at the top of `BuildCrossPackageAssignment` (right after the `packages.Count == 0` guard at :93) so both the single-package branch and the K-min branch operate on non-empty packages — this also makes `packages.Count==1` after filtering correctly hit the single-package shuffle. Verify the planner places the filter so that "2 packages, one empty" collapses to the single-package shuffle path (worker gets the filled package's questions).

### Pattern 2: Defensive both-sides normalization at the gate (D-01a)
**What:** Normalize both stored and input values at the comparison site.
**Template:**
```csharp
// Replace CMPController.cs:876
if (string.IsNullOrEmpty(token)
    || (assessment.AccessToken ?? "").Trim().ToUpper() != (token ?? "").Trim().ToUpper())
{
    return Json(new { success = false, message = "Token tidak valid. Silakan periksa dan coba lagi." });
}
```
This is safe: line 876 is the ONLY token comparison in the entire codebase [VERIFIED: grep across `Controllers/`]. GenerateSecureToken emits uppercase-only; CreateAssessment uppercases; client force-uppercases — so uppercasing the stored side never breaks an already-correct uppercase token, and it heals lowercase ones.

### Pattern 3: Uppercase-on-write mirror (D-01b)
**What:** Compute one normalized token at the top of the Pre/Post branch and assign it at all 3 sites.
**Template (mirror CreateAssessment :1105-1108):**
```csharp
// Near top of Pre-Post branch (after line 1786). Mirror CreateAssessment:1105-1108.
string normalizedToken = (model.IsTokenRequired && !string.IsNullOrWhiteSpace(model.AccessToken))
    ? model.AccessToken.ToUpper()
    : "";
// :1812  s.AccessToken = model.IsTokenRequired ? (normalizedToken != "" ? normalizedToken : (s.AccessToken ?? "")) : "";
// :1916/:1937  AccessToken = model.IsTokenRequired ? normalizedToken : "",
```
**Gotcha:** line 1812 has fallback-to-existing semantics (`?? s.AccessToken ?? ""`) for the shared-field loop — preserve that "keep existing token if model didn't supply one" behavior, but uppercase whatever ends up stored. Do not silently wipe an existing token when `model.AccessToken` is null.

### Pattern 4: Role attribute mirror + server-side business rule (D-02/D-03)
**Template (authz — exact sibling string, VERIFIED ResetAssessment:3998):**
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]      // NOTE: "Admin, HC" with a space — match sibling exactly
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddExtraTime(int assessmentId, int minutes) { … }
```
**Template (cap — D-03, check against original DurationMinutes per session):**
```csharp
// After resolving sessions, BEFORE the foreach accumulation (:6897):
foreach (var session in sessions)
{
    var currentExtra = session.ExtraTimeMinutes ?? 0;
    if (currentExtra + minutes > session.DurationMinutes)
        return Json(new { success = false,
            message = $"Total tambahan waktu tidak boleh melebihi durasi ujian ({session.DurationMinutes} menit). Saat ini sudah +{currentExtra} menit." });
}
foreach (var session in sessions) { session.ExtraTimeMinutes = (session.ExtraTimeMinutes ?? 0) + minutes; }
```
**Gotcha:** AddExtraTime operates on a **batch** (all InProgress sessions matching Title+Category+Date, :6887-6892). Sessions in a batch can in principle have different `DurationMinutes`. Decide the cap semantics: simplest correct rule = reject if ANY session would exceed its own duration (per-session cap). The audit + D-03 phrase it as "≤ original DurationMinutes" — per-session is the faithful reading. Confirm with planner whether to reject the whole batch or skip over-cap sessions; recommend reject-whole-batch with a clear message (atomic, predictable).

### Anti-Patterns to Avoid
- **Fixing SHF-01 only at the StartExam query level (filter empties before calling engine):** the audit offers this as an alternative, but D-04 explicitly locks the fix at the engine so ReshufflePackage/ReshuffleAll heal too. Do NOT filter at the call site instead of the engine.
- **Forward-only token fix (uppercase write only, no compare fix):** rejected by D-01 — would NOT heal already-stored lowercase tokens (workers stay locked out). Must do both sides.
- **Removing the client-side `toUpperCase()` at Assessment.cshtml:757:** leave it; it is part of the established UX invariant and the defensive compare is additive.
- **Adding a cap as a DataAnnotation `[Range]` on the model field:** the model field is a plain accumulator (`int? ExtraTimeMinutes`); the cap is a runtime business rule against `DurationMinutes`, not a static range. Enforce in the action.
- **Writing StartedAt/Status before the empty-set check (D-05):** see Pitfall 1 below — the current code writes StartedAt at :960-967 BEFORE the engine call at :1019. Naive placement of the D-05 guard after :1019 would leave StartedAt already written.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Empty-package filter expression | A new bespoke filter | Copy OFF-path `.Where(p => p.Questions != null && p.Questions.Count > 0)` (`:53-57`) | Guarantees ON/OFF parity; already tested for the OFF case |
| Token normalization | Custom normalizer/setter | Inline `.Trim().ToUpper()` (mirror existing `:1105-1108` / `:2012-2013`) | One-liner; matches established invariant; the model setter must stay plain (other paths rely on it) |
| Role gate | Custom in-body identity check | `[Authorize(Roles = "Admin, HC")]` attribute (mirror `:3998`) | Framework-native; consistent with ~60 sibling actions; testable by reflection |
| Reflection authz test | Custom HTTP integration harness | `CDPControllerAuthTests.cs` pattern | Pure, fast, no DB/server; asserts the attribute is present + correct roles |
| Engine unit test scaffolding | New in-memory model builders | `ShuffleEngineTests.cs` `Pkg(...)` helper | Pure (no DB); already builds packages/questions/options in-memory |

**Key insight:** Every fix has an existing, tested precedent in this codebase (OFF-path filter, CreateAssessment uppercase, ResetAssessment authz). The correct implementation is "make the broken path match the working sibling," not invent anything.

## Runtime State Inventory

> Rename/refactor/migration considerations. Phase 380 is a behavior-fix, not a rename — but TOK-01 has a "stored data already wrong" dimension worth recording.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **Existing lowercase AccessToken values** in `AssessmentSessions.AccessToken` for any Pre/Post assessment an admin edited with a lowercase token. These are the workers currently locked out. | **No data migration needed** — D-01a defensive compare (`(stored??"").Trim().ToUpper() == (input??"").Trim().ToUpper()`) heals them at read-time. No SQL UPDATE, no IT action. (D-01b write-fix only prevents NEW lowercase tokens.) |
| Live service config | None — no external service holds these strings. | None. |
| OS-registered state | None. | None — no scheduled tasks / registrations involved. |
| Secrets/env vars | None — AccessToken is not a secret/env var; it's per-assessment DB data. | None. |
| Build artifacts | None — no package rename, no egg-info/binary naming change. `migration=false` (no EF model change). | None — verify `dotnet ef migrations` is NOT scaffolded (none of these edits touch the model/snapshot). |

**Verification of "no migration":** None of the three fixes change a model property, relationship, or index. `ExtraTimeMinutes` and `DurationMinutes` already exist; AccessToken already exists; ShuffleEngine is pure logic. Confirmed `migration=false` for Phase 380. Planner/executor MUST verify no model-snapshot diff is produced (a stray `dotnet ef migrations add` would be wrong).

## Common Pitfalls

### Pitfall 1: D-05 guard placement — StartedAt/Status is written BEFORE the engine call
**What goes wrong:** The naive reading of D-05 ("no StartedAt/Status write") assumes the write happens after the question set is computed. It does NOT. `CMPController.StartExam` writes `Status="InProgress"` + `StartedAt=DateTime.UtcNow` at **lines 960-967** (`justStarted` block), which executes BEFORE the package load (`:995`) and the engine call (`:1019`). By the time `shuffledIds` is known to be empty, StartedAt is already persisted.
**Why it happens:** The exam-start mutation is intentionally early (timer must start on first GET). The shuffle assignment is built later, lazily.
**How to avoid:** Two viable approaches — flag for planner decision:
  - **(A) Move the empty-check earlier:** load packages + compute the would-be question set (or at least a cheap "any non-empty package?" check) BEFORE the `justStarted` write at :960. If all packages empty → block + message, never write StartedAt. This is the cleanest match to D-05's intent.
  - **(B) Compensating rollback:** keep current order, but if the engine returns empty AND ≥1 package exists, and `justStarted` was true, revert the StartedAt/Status write (set back to prior status, null StartedAt) before redirecting with the message. More fragile (must also suppress the SignalR `workerStarted` broadcast at :970-978 and the activity log at :977).
**Recommendation:** Prefer (A). A cheap guard: after loading `packages` (:995-1000) but the write at :960 is the blocker — so the planner should hoist a "is any package non-empty?" check before line 960, OR restructure so the package load + emptiness check precedes the justStarted write. The all-empty case is rare, so a small extra query (count non-empty sibling packages) before the write is acceptable. **Note the existing `else` at :1198-1203** already handles the "zero packages at all" case (`packages.Any()==false`) with a friendly message — D-05 is the distinct "packages exist but all empty" case that currently falls through to the empty-exam render.
**Warning signs:** A worker hitting an all-empty exam still shows `Status=InProgress`/non-null `StartedAt` in DB after the fix → guard placed too late.

### Pitfall 2: "2 packages, one empty" must collapse to the single-package shuffle, not the empty K-path
**What goes wrong:** If the D-04 filter is placed only right before `K = Min(...)` (after the `:97-105` single-package early-return), then "2 packages where one is empty" → filtered list has 1 package → but the single-package branch was already skipped → falls into K-min logic with a 1-element list (K = that package's count, which is fine) — OR, if filter returns an empty list, returns `[]`. The subtle bug: the single-package early-return at :97 checks `packages.Count == 1` on the UNFILTERED list. After filtering, a 2-package-one-empty input should behave like a single full package (shuffle all its questions).
**How to avoid:** Filter empties at the TOP of `BuildCrossPackageAssignment` (right after the `:93` `Count==0` guard), then let the existing `Count==1` early-return (now operating on the filtered list) handle the collapse. This yields the correct behavior: filtered to 1 package → shuffle all its questions; filtered to ≥2 → K-min across non-empty; filtered to 0 → `[]`.
**Warning signs:** Unit test "2 packages [3 questions, 0 questions], ON" returns fewer than 3 questions (it should return all 3, shuffled) → filter placed wrong.

### Pitfall 3: Cap semantics on a batch with mixed durations
**What goes wrong:** AddExtraTime resolves a batch (Title+Category+Date) and may include sessions with different `DurationMinutes`. A single global cap is ambiguous.
**How to avoid:** Per-session cap (reject if any session's `existing + minutes > its own DurationMinutes`). See Pattern 4. Confirm reject-whole-batch vs skip-over-cap with planner; recommend reject-whole-batch for atomicity.
**Warning signs:** A 30-min session in a mixed batch gets +60 because the cap used another session's 60-min duration.

### Pitfall 4: Authz attribute role string must match exactly ("Admin, HC" with space)
**What goes wrong:** ASP.NET `[Authorize(Roles="...")]` splits on comma and trims, so `"Admin,HC"` and `"Admin, HC"` are functionally equivalent at runtime — BUT the reflection-authz test asserts `authz.Roles == expected` as a STRING. If the test expects `"Admin, HC"` and the code writes `"Admin,HC"`, the test fails on a cosmetic mismatch.
**How to avoid:** Use the exact sibling string `"Admin, HC"` (verified at ResetAssessment:3998) in BOTH the attribute and the test's expected value. CONTEXT.md D-02 writes `"Admin,HC"` (no space) — defer to the actual sibling convention `"Admin, HC"` for consistency, and make the test expectation match whatever is written.
**Warning signs:** Reflection test red on a string-equality assertion despite runtime authz working.

### Pitfall 5: AddExtraTime returns JSON, not a redirect — message format
**What goes wrong:** Sibling reset actions return RedirectToAction with TempData; AddExtraTime returns `Json(new { success, message })` (`:6871, :6909`). A cap-reject must follow the SAME JSON contract the view JS expects, not TempData.
**How to avoid:** Return `Json(new { success = false, message = "…" })` for the cap rejection (matches existing `:6871`/`:6877`/`:6895`). The e2e helper `addExtraTimeViaModal` asserts an `.alert-success` with `/berhasil ditambahkan/i` — the reject path won't show that, so the cap e2e must assert the failure UX instead.

### Pitfall 6: Local AD / SQL env for verification
**What goes wrong:** `dotnet run` locally may fail login (AD loopback) or e2e SQL connection errors (per MEMORY: error 53 / NTLM loopback).
**How to avoid (from project memory):** Local run with `Authentication__UseActiveDirectory=false dotnet run`; combined Playwright run must use `--workers=1` (DB isolation); start SQLBrowser + `lpc:` shared-memory conn override for local e2e SQL. See MEMORY refs `reference_local_e2e_sql_env_fix` and Phase 355 note.

## Code Examples

### Reflection-authz test (mirror CDPControllerAuthTests.cs)
```csharp
// Source pattern: HcPortal.Tests/CDPControllerAuthTests.cs (verified current)
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Controllers;
using Xunit;

public class AddExtraTimeAuthTests
{
    [Fact]
    public void AddExtraTime_RequiresAdminOrHc()
    {
        var method = typeof(AssessmentAdminController)
            .GetMethod(nameof(AssessmentAdminController.AddExtraTime));
        Assert.NotNull(method);
        var authz = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        Assert.NotNull(authz);
        Assert.Equal("Admin, HC", authz!.Roles);   // match the exact string written on the attribute
    }
}
```
Note: `AddExtraTime` is `public async Task<IActionResult>` — reflection-discoverable. (The audit's "reflection-authz → 403" framing is satisfied by asserting the attribute exists; a true 403 needs an integration server test, which is heavier and unnecessary given the attribute test + framework guarantee.)

### Pure unit test (mirror ShuffleEngineTests.cs Pkg helper) for SHF-01
```csharp
// Add to ShuffleEngineTests.cs (or a sibling Shuffle*Tests.cs). Pkg(...) builder already exists there.
[Fact] // WSE-01: ON-path, 2 packages one empty → worker gets the filled package's questions (NOT empty)
public void On_MultiPackage_OneEmpty_ReturnsFilledPackageQuestions()
{
    var p1 = Pkg(1, (10, 1, null), (11, 2, null), (12, 3, null)); // 3 questions
    var p2 = Pkg(2);                                              // EMPTY
    var packages = new List<AssessmentPackage> { p1, p2 };

    var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));

    Assert.NotEmpty(result);                                       // BUG: currently returns [] (K=Min=0)
    Assert.Equal(new HashSet<int> { 10, 11, 12 }, result.ToHashSet());
}

[Fact] // D-05 engine half: ON-path, all packages empty → engine returns empty (controller blocks)
public void On_AllPackagesEmpty_ReturnsEmpty()
{
    var packages = new List<AssessmentPackage> { Pkg(1), Pkg(2) };
    var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
    Assert.Empty(result);
}
```

### E2E token lowercase (Scenario #5) — leverage existing helpers
```typescript
// tests/e2e — uses createAssessmentViaWizard (isTokenRequired), EditAssessment, worker token modal
// 1. HC create token-required Pre/Post, then EDIT and type a LOWERCASE token e.g. 'abc23x'
// 2. login worker → /CMP/Assessment → .btn-start-token → tokenModal → type 'abc23x'
//    (input auto-uppercases to 'ABC23X' per Assessment.cshtml:757)
// 3. POST /CMP/VerifyToken → assert success redirect to StartExam (NOT 'Token tidak valid')
// DB assert via dbSnapshot.queryString: AccessToken stored UPPERCASE after the edit-fix
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Duplicated `BuildCrossPackageAssignment` copies in CMPController | Single pure `ShuffleEngine` (Phase 373) | v27.0 | One engine fix (D-04) heals all 3 callers — confirmed |
| ON/OFF shuffle always-on | Per-assessment `ShuffleQuestions`/`ShuffleOptions` toggles, default ON (`AssessmentSession.cs:39`) | v27.0 (Phase 372/374) | SHF-01 only bites because ON is default AND OFF already got the empty-filter but ON did not (drift) |

**Deprecated/outdated:** Legacy non-package exam path removed (Phase 227 CLEN-02) — `StartExam` `else` branch (`:1198-1203`) now only errors for zero-package sessions. This is the existing "no packages" guard; D-05 is the adjacent "packages exist but all empty" gap.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | EF Core version is the project's current (not separately verified this session) | Standard Stack | Low — no schema change; version irrelevant to logic fixes |
| A2 | Cap semantics = per-session against each session's own `DurationMinutes` (audit/D-03 say "≤ original DurationMinutes", batch may be mixed) | Pattern 4 / Pitfall 3 | Medium — if HC expects a single global cap or skip-over-cap behavior, message/UX differs. Planner should confirm reject-whole-batch vs skip. |
| A3 | D-05 best placement is hoisting the emptiness check before the StartedAt write (approach A) | Pitfall 1 | Medium — if executor uses approach B (rollback), must also suppress SignalR + activity-log side-effects. Either works; A is cleaner. |
| A4 | Authz string should be `"Admin, HC"` (space, matching ResetAssessment) rather than D-02's `"Admin,HC"` | Pitfall 4 | Low — runtime-equivalent; only the test's expected string must match the code. |
| A5 | A reflection-attribute test is sufficient for RST-01 (audit mentions "403"); no full HTTP integration test required | Code Examples | Low — attribute presence + framework guarantee covers the authz requirement; integration 403 test optional. |

## Open Questions

1. **Cap batch semantics (A2)**
   - What we know: AddExtraTime acts on a batch; sessions may have different DurationMinutes.
   - What's unclear: reject-whole-batch vs skip-over-cap-sessions; single message vs per-session.
   - Recommendation: reject-whole-batch with a clear BI message naming the limit; per-session check (any-exceeds → reject).

2. **D-05 guard approach (A3)**
   - What we know: StartedAt/Status write at :960-967 precedes the engine call at :1019.
   - What's unclear: hoist-check-before-write (A) vs compensating-rollback (B).
   - Recommendation: A — add a cheap "any non-empty sibling package?" check before the justStarted write; block all-empty before any mutation/SignalR/log.

3. **Optional UI cap hint (Claude's discretion)**
   - What we know: cap is enforced server-side (sufficient).
   - Recommendation: server-only for this phase; skip the optional UI hint unless trivially cheap. Keep scope tight.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (net8.0) | build + xUnit | ✓ (project builds today) [ASSUMED] | net8.0 | — |
| SQL Server (local `HcPortalDB_Dev`) | e2e DB assertions, `dotnet run` | ✓ per MEMORY (SQLEXPRESS shared) [CITED: MEMORY] | — | unit tests are pure (no DB) — can verify logic without DB |
| Node + Playwright | e2e scenarios | ✓ (`tests/node_modules/playwright`) [VERIFIED] | project current | unit + reflection tests cover logic if e2e blocked |
| Local AD bypass env var | local `dotnet run` login | requires `Authentication__UseActiveDirectory=false` [CITED: MEMORY] | — | documented in project memory |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** E2E requires SQL + AD-bypass env; if local e2e SQL is flaky (error 53), the unit tests (pure, no DB) fully cover the engine + authz logic, and e2e can be run with `--workers=1` + SQLBrowser + `lpc:` override per project memory.

## Validation Architecture

> nyquist_validation is `true` in `.planning/config.json` — this section is mandatory.

### Test Framework
| Property | Value |
|----------|-------|
| Framework (unit/integration) | xUnit — `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0) |
| Framework (e2e) | Playwright `@playwright/test` — `tests/` project |
| Config file | `HcPortal.Tests.csproj` (xUnit); `tests/playwright.config.ts` + `tests/package.json` (e2e) |
| Quick run command (unit, this phase's tests) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ShuffleEngine|FullyQualifiedName~AddExtraTime|FullyQualifiedName~VerifyToken"` |
| Full unit suite | `dotnet test` (from repo root) |
| E2E run (DB-isolated) | `cd tests && npx playwright test exam-taking.spec.ts --workers=1` (or the specific new spec) |
| Build gate | `dotnet build` (0 errors) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| WSE-01 | ON-path, ≥2 packages one empty → returns filled package's questions (non-empty) | unit | `dotnet test --filter "On_MultiPackage_OneEmpty"` | ❌ Wave 0 (add to `ShuffleEngineTests.cs`) |
| WSE-01 | ON-path, all packages empty → returns empty list | unit | `dotnet test --filter "On_AllPackagesEmpty"` | ❌ Wave 0 |
| WSE-01 | StartExam all-empty → BLOCK + message, no StartedAt/Status/assignment write | integration or e2e | e2e scenario #6 variant (all-empty) OR a controller integration test | ❌ Wave 0 |
| WSE-01 | E2E #6: 2 packages one empty + shuffle ON → worker gets questions (count>0) → Score>0 | e2e | `cd tests && npx playwright test --workers=1 -g "empty package"` | ❌ Wave 0 (new spec/case) |
| WSE-02 | VerifyToken: stored lowercase token matches uppercased input (defensive compare) | unit/integration | `dotnet test --filter "VerifyToken"` (controller test w/ in-mem session) | ❌ Wave 0 |
| WSE-02 | EditAssessment Pre/Post: token persisted UPPERCASE at all 3 sites | integration | `dotnet test --filter "EditAssessmentToken"` (DB-backed) | ❌ Wave 0 |
| WSE-02 | E2E #5: admin edits lowercase token → worker enters successfully | e2e | `cd tests && npx playwright test --workers=1 -g "token lowercase"` | ❌ Wave 0 |
| WSE-03 | AddExtraTime carries `[Authorize(Roles="Admin, HC")]` | unit (reflection) | `dotnet test --filter "AddExtraTime_RequiresAdminOrHc"` | ❌ Wave 0 (mirror `CDPControllerAuthTests.cs`) |
| WSE-03 | AddExtraTime cap: existing + minutes > DurationMinutes → rejected | integration | `dotnet test --filter "AddExtraTime_Cap"` (DB-backed action test) | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** quick filtered unit run for the touched area (`dotnet test --filter ...`).
- **Per wave merge:** full `dotnet build` + `dotnet test` (full xUnit suite — must stay green; current baseline per MEMORY is in the 350s/360s passing).
- **Phase gate:** full unit suite green + the two E2E scenarios (#5, #6) green (`--workers=1`) before `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ShuffleEngineTests.cs` — ADD `On_MultiPackage_OneEmpty_ReturnsFilledPackageQuestions` + `On_AllPackagesEmpty_ReturnsEmpty` (covers WSE-01 engine). File exists; add facts.
- [ ] `HcPortal.Tests/AddExtraTimeAuthTests.cs` (NEW) — reflection-authz for WSE-03 (mirror `CDPControllerAuthTests.cs`).
- [ ] `HcPortal.Tests/AddExtraTimeCapTests.cs` (NEW) or integration fixture — cap rejection for WSE-03 (DB-backed; uses existing test DB fixture pattern, e.g. the `[Trait("Category","Integration")]` style seen in cascade tests).
- [ ] VerifyToken defensive-compare test (NEW unit/integration) — WSE-02 compare half. (Controller test with an in-memory/seeded AssessmentSession whose AccessToken is lowercase → assert success.)
- [ ] EditAssessment token-uppercase-on-write test (NEW integration) — WSE-02 write half.
- [ ] `tests/e2e/exam-taking.spec.ts` (or new `entry-integrity.spec.ts`) — ADD Scenario #5 (token lowercase) + Scenario #6 (empty package + shuffle ON). Helpers exist: `createAssessmentViaWizard` (token), `createDefaultPackage`, `addQuestionViaForm`, `submitExamTwoStep`, `dbSnapshot`. Need a path to create a SECOND empty package (call `createDefaultPackage` twice, add questions to only one).
- [ ] No framework install needed (xUnit + Playwright already present).

## Security Domain

> `security_enforcement` not present in config (treat as enabled). Phase 380 is security-relevant (RST-01 is a HIGH authorization hole).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | no | — |
| V2 Authentication | no | (uses existing Identity) |
| V3 Session Management | no | — |
| V4 Access Control | **yes (RST-01)** | `[Authorize(Roles="Admin, HC")]` attribute on AddExtraTime (function-level authorization) |
| V5 Input Validation | **yes (RST-04, TOK-01)** | Server-side cap on ExtraTimeMinutes; token normalization (trim+upper) at the gate |
| V6 Cryptography | no | (token is an access code, not a crypto secret; uppercase charset already used) |
| V11 Business Logic | **yes (RST-04, SHF-01)** | Cap enforces timed-exam integrity; empty-package guard prevents bogus 0% grading |

### Known Threat Patterns for ASP.NET Core MVC (this phase)
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Missing function-level authorization (worker self-extends exam time) — RST-01 | Elevation of Privilege | `[Authorize(Roles="Admin, HC")]` matching sibling actions; reflection test asserts presence |
| Unbounded resource grant (unlimited extra time) — RST-04 | Tampering / business-logic abuse | Server-side ceiling: `existing + minutes ≤ DurationMinutes` |
| Access-gate bypass via data drift (lowercase token lockout) — TOK-01 | (availability/integrity of access gate) | Defensive both-sides normalization at the single comparison site; uppercase-on-write |
| Silent data corruption (0% Fail batch from empty package) — SHF-01 | Tampering / integrity | Engine empty-filter parity (ON=OFF); StartExam block-on-empty (no false grading) |

**Note:** `[ValidateAntiForgeryToken]` (already present on AddExtraTime) stops CSRF but NOT a legitimately-authenticated worker calling the endpoint directly — the role attribute is the actual fix. Confirmed: no global FallbackPolicy in `Program.cs` (audit grep returned zero `AddAuthorization`/`FallbackPolicy`), so the base `[Authorize]` on `AdminBaseController` permits any authenticated user — the per-action role attribute is required.

## Sources

### Primary (HIGH confidence — verified in this session)
- `Helpers/ShuffleEngine.cs` (read full file) — ON-path `:91-223` (`K=Min` at `:108`, no empty filter); OFF-path filter `:53-57`; `BuildQuestionAssignment` entry `:39`.
- `Controllers/CMPController.cs` — `VerifyToken` `:850-884` (compare `:876`); `StartExam` `:888-1213` (justStarted write `:960-967`, sibling+package load `:982-1000`, engine call `:1019-1020`, assignment create `:1012-1042`, no-package `else` `:1198-1203`).
- `Controllers/AssessmentAdminController.cs` — `AddExtraTime` `:6866-6910` (no role attr `:6866-6868`, per-call bound `:6870`, accumulation `:6899`, batch resolve `:6887-6892`); EditAssessment Pre/Post token writes `:1812/1916/1937`, branch return `:1957`, single-mode uppercase `:2010-2017`; CreateAssessment uppercase `:1105-1108`; ReshufflePackage engine call `:5210`; ReshuffleAll `:5308`; sibling `ResetAssessment [Authorize(Roles="Admin, HC")]` `:3998`.
- `Models/AssessmentSession.cs` — `ShuffleQuestions=true` default `:39`; `ExtraTimeMinutes int?` accumulator `:199`; `DurationMinutes` (original duration field).
- `Views/CMP/Assessment.cshtml:757` — client force-uppercase token input.
- `HcPortal.Tests/ShuffleEngineTests.cs`, `CDPControllerAuthTests.cs`, `ShuffleReshuffleTests.cs` — test templates.
- `tests/e2e/helpers/examTypes.ts` (`createAssessmentViaWizard` token support `:130-137`, `addExtraTimeViaModal` `:535`), `tests/helpers/dbSnapshot.ts` exports, `tests/e2e/exam-taking.spec.ts` Flow B `:284`.
- `docs/assessment-audit/2026-06-14-code-audit-findings.md` — SHF-01 (`:341-358`), TOK-01 (`:464-483`), RST-01 (`:320-337`), RST-04 (`:1948-1975`) with adversarial verification.
- `docs/assessment-audit/2026-06-14-E2E-worker-success-FOCUS.md` — fix detail + E2E plan scenarios #5/#6.
- `.planning/config.json` — `nyquist_validation: true`; `.planning/STATE.md` — v29.0 context.

### Secondary (MEDIUM)
- MEMORY refs: `reference_local_e2e_sql_env_fix` (local SQL/AD env), Phase 355 (AD-bypass run), Phase 372/377 patterns.

### Tertiary (LOW)
- None required — all claims verified against source or audit.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new deps; existing toolchain verified (net8.0, xUnit, Playwright).
- Architecture / fix anchors: HIGH — every line:number re-verified against actual current source; matches audit exactly (no drift).
- Pitfalls: HIGH — D-05 write-ordering and the OFF-filter-placement subtleties confirmed by reading the actual control flow.
- Cap semantics (batch mixed durations): MEDIUM — flagged as A2 open question for planner confirmation.

**Research date:** 2026-06-14
**Valid until:** 2026-07-14 (stable internal codebase; re-verify line numbers if CMPController/AssessmentAdminController are edited by a parallel phase before execution — Phase 381/382 touch CMPController).
