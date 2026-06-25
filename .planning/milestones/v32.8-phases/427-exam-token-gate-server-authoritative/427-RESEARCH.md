# Phase 427: Exam Token-Gate Server-Authoritative — Research

**Phase:** 427 · **Requirement:** EXSEC-01 · **Migration:** TRUE (`AddTokenVerifiedAt`)
**Researched:** 2026-06-24 (orchestrator-authored; verified directly against source — CMPController.cs, RetakeService.cs, AssessmentSession.cs, test fixtures)

## RESEARCH COMPLETE

---

## 1. Summary — what to build

Add nullable DB column `AssessmentSession.TokenVerifiedAt` (`DateTime?`). Server-authoritative token gate:
1. **Model + migration** — add column; `dotnet ef migrations add AddTokenVerifiedAt`; regen snapshot (R-2); apply local DB.
2. **VerifyToken (CMPController)** — on token-required success (line ~899-900), stamp `assessment.TokenVerifiedAt = DateTime.UtcNow; await _context.SaveChangesAsync();` (D-03). Remove `TempData[...]=true` (both returns, D-02).
3. **StartExam gate (CMPController)** — replace `var tokenVerified = TempData.Peek($"TokenVerified_{id}"); if (tokenVerified == null)` (line 963-964) with `if (assessment.TokenVerifiedAt == null)`. Keep outer guard `assessment.IsTokenRequired && assessment.UserId == user.Id && assessment.StartedAt == null` (line 961) verbatim (D-02; StartedAt==null protects legacy InProgress — SC#4).
4. **Reset single source (RetakeService.ExecuteAsync)** — add `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` to the existing `ExecuteUpdateAsync` reset chain (~line 115, alongside `StartedAt`) (D-01). Covers worker `RetakeExam` + HC `ResetAssessment` (both call ExecuteAsync).
5. **Cleanup TempData (D-02)** — remove `TempData.Remove($"TokenVerified_{id}")` in `CMPController.RetakeExam` (line 2585) and `AssessmentAdminController.ResetAssessment` (line 4411). Update stale comment in RetakeService.cs (line 38-39) noting reset is now in-service for the DB column.

## 2. Verified facts (source-checked)

### Model — `Models/AssessmentSession.cs:96-105`
`IsTokenRequired` (bool, line 96), `AccessToken` (string, line 105). Add `public DateTime? TokenVerifiedAt { get; set; }` after AccessToken (~line 105-106). Audit fields follow at 107-109.

### VerifyToken — `Controllers/CMPController.cs:864-902`
- `[HttpPost][ValidateAntiForgeryToken]`. Authz: owner/Admin/HC (line 881). Token compare via `AccessTokenMatches` (line 894).
- Not-required branch line 886-891: sets `TempData[...]=true`, returns success+redirect. **D-02:** drop TempData line; **D-03:** do NOT stamp here.
- Token-required success line 899-901: sets `TempData[...]=true`, returns success+redirect. **D-03:** stamp `TokenVerifiedAt=UtcNow` + `SaveChangesAsync` here; **D-02:** drop TempData line.

### StartExam gate — `Controllers/CMPController.cs:961-969`
```csharp
if (assessment.IsTokenRequired && assessment.UserId == user.Id && assessment.StartedAt == null)
{
    var tokenVerified = TempData.Peek($"TokenVerified_{id}");   // ← REPLACE
    if (tokenVerified == null) { TempData["Error"]=...; return RedirectToAction("Assessment"); }
}
```
New inner: `if (assessment.TokenVerifiedAt == null) { TempData["Error"] = "Ujian ini membutuhkan token akses. Silakan masukkan token terlebih dahulu."; return RedirectToAction("Assessment"); }`. Outer guard unchanged.

### RetakeService.ExecuteAsync — `Services/RetakeService.cs:69+`
Existing reset uses `ExecuteUpdateAsync(...SetProperty(r => r.StartedAt, (DateTime?)null)...)` (~line 115). Add `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)`. Both worker `RetakeExam` (CMPController:2569,2577) and HC `ResetAssessment` (AssessmentAdminController:4340+) funnel through ExecuteAsync. Comment line 38-39 ("TempData clear = caller responsibility") is now partly obsolete for the DB column — update.

### Reset sites — TempData.Remove
- `CMPController.RetakeExam:2585` — remove (D-02).
- `AssessmentAdminController.ResetAssessment:4411` — remove (D-02).

## 3. Migration mechanics (R-2 — CRITICAL)

- `dotnet ef migrations add AddTokenVerifiedAt` (after model change). Generates migration + updates `Migrations/ApplicationDbContextModelSnapshot.cs`.
- Column: `ALTER TABLE AssessmentSessions ADD TokenVerifiedAt datetime2 NULL` — nullable, **no backfill** (null = not-yet-verified; legacy InProgress bypass via `StartedAt != null`).
- R-2: migration timestamp will naturally be > all existing migrations on this branch. **Do NOT edit older migrations.** Snapshot regen is automatic via `migrations add`.
- Apply: `dotnet ef database update`. Verify column present: `sqlcmd -C -I -S localhost\SQLEXPRESS -d <localdb> -Q "SELECT COL_LENGTH('AssessmentSessions','TokenVerifiedAt')"` (non-null result = column exists).
- Branch ITHandoff dev port 5270. Notify IT migration=TRUE at promotion.

## 4. ⚠️ Gotchas / pitfalls

1. **Keep `StartedAt == null` outer guard.** Without it, a legacy InProgress session (token verified via old TempData, `TokenVerifiedAt` still null, `StartedAt` set) would be re-blocked → lockout. The guard already bypasses such sessions (SC#4). Do not "simplify" it away.
2. **VerifyToken stamp must persist.** `assessment` is fetched via `FindAsync` (tracked) at line 869 — set property + `await _context.SaveChangesAsync()`. POST → write is fine (not GET idempotency concern; that's Phase 428's StartExam scope).
3. **Do NOT touch write-on-GET StartExam (Upcoming→Open, line 920-928).** That refactor is Phase 428 (EXSEC-02). 427 only changes the token-gate read at 961-969.
4. **Do NOT touch GRDF-01 (line 944-956) or time-gate.** Out of scope.
5. **Full replacement (D-02):** after removing all TempData token lines, grep for `TokenVerified_` must return ZERO hits in Controllers/ + Services/ (except possibly the new column name). No dead TempData token code left.
6. **R-1 merge note (not this phase's work, but be aware):** StartExam is the branch-divergence conflict zone vs main. 427 edits the token-gate inner predicate only — keep edits surgical to ease later merge.

## Validation Architecture

**Test layer:** xUnit, **real-SQL** (`[Trait("Category","Integration")]`) — the new column + RetakeService reset + endpoint gate all need a migrated DB. Reuse `RetakeServiceFixture` (disposable DB `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync()` FULL chain — automatically applies the new `AddTokenVerifiedAt` migration once added). Pure-helper `VerifyTokenTests.cs` (AccessTokenMatches) stays unchanged.

**Reusable assets:**
- `RetakeServiceFixture` (RetakeServiceTests.cs:34) — real-SQL DB, `MigrateAsync` full chain. After adding the migration, `TokenVerifiedAt` column exists in the fixture DB automatically.
- `RetakeExamEndpointTests` CMPController factory (`MakeController(ctx, signedInUser)` at line 102) + `FakeUserStore`/`MakeUserManager` (line 47-99) + `StubSession` + `ClaimsPrincipal(NameIdentifier)` → build CMPController with real ctx/userManager/impersonation/retakeService for endpoint-level VerifyToken/StartExam tests.

**Required test cases (Dimension-8 coverage for EXSEC-01):**

| # | Test | Layer | Asserts | SC |
|---|------|-------|---------|-----|
| T1 | `StartExam_TokenRequired_TokenVerifiedAtNull_Blocks` | endpoint (real-SQL) | session IsTokenRequired=true, StartedAt=null, TokenVerifiedAt=null → StartExam returns RedirectToAction("Assessment") with TempData["Error"] token message (gate reads column, not TempData) | SC#1 |
| T2 | `StartExam_TokenRequired_TokenVerifiedAtSet_Proceeds` | endpoint | same session but TokenVerifiedAt=UtcNow → StartExam does NOT redirect-to-Assessment-with-token-error (proceeds past gate) | SC#1 |
| T3 | `VerifyToken_CorrectToken_StampsTokenVerifiedAt` | endpoint | POST VerifyToken(id, correctToken) → success JSON; reload ctx → `assessment.TokenVerifiedAt != null` (persisted) | SC#2 |
| T4 | `RetakeService_Execute_ResetsTokenVerifiedAtNull` | service (real-SQL) | session with TokenVerifiedAt set → ExecuteAsync → reload → `TokenVerifiedAt == null` (re-arm). Covers both retake paths via single source (D-01) | SC#3 |
| T5 | `StartExam_LegacyInProgress_StartedAtSet_TokenVerifiedAtNull_NotLocked` | endpoint | IsTokenRequired=true, StartedAt=set, TokenVerifiedAt=null → StartExam proceeds (StartedAt!=null bypass) — no lockout | SC#4 |

**Migration verification (SC#4 column-exists):** `dotnet ef database update` locally + `sqlcmd -C -I -S localhost\SQLEXPRESS -d <localdb> -Q "SELECT COL_LENGTH('AssessmentSessions','TokenVerifiedAt')"` returns non-null. Also covered implicitly: RetakeServiceFixture.MigrateAsync would throw if the migration is malformed → T4 failing fast.

**Run command:** `dotnet test HcPortal.Tests --filter "Category=Integration"` (needs SQLEXPRESS + SQLBrowser; loopback shared-memory `lpc:` if NTLM fails — see memory `reference_local_e2e_sql_env_fix`). Non-integration suite (`Category!=Integration`) must also stay green (no regression).

**Coverage check:** EXSEC-01 → SC#1 (T1,T2 gate reads column), SC#2 (T3 stamp persisted), SC#3 (T4 reset single-source), SC#4 (T5 no-lockout + sqlcmd column check). All four SC have a discriminating test.
