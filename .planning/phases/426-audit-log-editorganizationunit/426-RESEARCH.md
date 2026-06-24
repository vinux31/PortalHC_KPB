# Phase 426: Audit-Log EditOrganizationUnit — Research

**Phase:** 426 · **Requirement:** AUDIT-01 · **Migration:** FALSE
**Researched:** 2026-06-24 (orchestrator-authored; opus researcher agent died mid-run on API connection error after confirming files — findings below are verified directly against source)

## RESEARCH COMPLETE

---

## 1. Summary — what to build

Mirror the existing `DeleteOrganizationUnit` audit block into `EditOrganizationUnit`, in `Controllers/OrganizationController.cs`. One additive `try/catch` block placed **after** `await tx.CommitAsync();` (line 308), **before** `var msg = ...` (line 310). Guarded so it only writes when something actually changed (D-01). No schema/model change — `AuditLog` table + `AuditLogService` already exist.

## 2. Verified facts (source-checked)

### LogAsync signature — `Services/AuditLogService.cs:21`
```csharp
public async Task LogAsync(
    string actorUserId, string actorName, string actionType, string description,
    int? targetId = null, string? targetType = null)
```
- Writes one row: `_context.AuditLogs.Add(entry); await _context.SaveChangesAsync();` (line 40-41). **SaveChanges is internal** — caller does not save.
- `CreatedAt = DateTime.UtcNow` set inside the service.

### AuditLog model — `Models/AuditLog.cs`
Fields: `Id`, `ActorUserId` `[Required]`, `ActorName` `[Required]`, `ActionType` `[Required][MaxLength(50)]`, `Description` `[Required]`, `TargetId int?`, `TargetType string? [MaxLength(100)]`, `CreatedAt`.
- ⚠️ `ActionType` is **MaxLength(50)** — `"EditOrganizationUnit"` = 19 chars ✓ (well under).

### The pattern to mirror — `Controllers/OrganizationController.cs:539-549` (inside `DeleteOrganizationUnit`)
```csharp
try
{
    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";
    await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteOrganizationUnit",
        $"Deleted organization unit '{unit.Name}' [ID={unit.Id}] (Level={unit.Level})",
        unit.Id, "OrganizationUnit");
}
catch { /* audit log failure tidak block response */ }
```

### Target site — `EditOrganizationUnit` (`OrganizationController.cs:129-317`)
- `oldName` captured at line 139; `oldParentId` at line 140.
- Cascade counters already computed: `cascadedUsers`, `cascadedMappings`, `cascadedUserUnits` (declared line 198-200, populated 209-242, 292-296).
- Commit at line 308: `await tx.CommitAsync();`
- All return-early branches (unit null / blank name / duplicate / circular / cross-Bagian split-block) occur **before** `BeginTransactionAsync` or before commit → no persisted change → correctly produce no audit.

## 3. Implementation guidance (locked by CONTEXT decisions)

Insert immediately after line 308 (`await tx.CommitAsync();`):
```csharp
// AUDIT-01 (Phase 426): mirror DeleteOrganizationUnit audit. Only-on-change (D-01),
// single combined row (D-02), raw parent IDs (D-03). Swallow-on-failure.
if (oldName != name.Trim() || oldParentId != parentId)
{
    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "EditOrganizationUnit",
            $"Edited organization unit '{oldName}'→'{name.Trim()}' [ID={unit.Id}] " +
            $"parent {(oldParentId?.ToString() ?? "null")}→{(parentId?.ToString() ?? "null")} " +
            $"(cascade: {cascadedUsers} users, {cascadedMappings} mappings, {cascadedUserUnits} UserUnits)",
            unit.Id, "OrganizationUnit");
    }
    catch { /* audit log failure tidak block response */ }
}
```
- `name.Trim()` is also what gets assigned at line 306 (`unit.Name = name.Trim();`) — consistent.
- Place the guard OUTSIDE the try (cheap comparison; no reason to swallow it). The try wraps only the audit I/O + actor resolution.

## 4. ⚠️ Gotchas / pitfalls

1. **`_userManager` is null in the existing test harness.** `OrganizationControllerTests.MakeController()` passes `null!` for `_userManager` (param 2) with the comment "Edit do not deref _userManager". After Phase 426, `EditOrganizationUnit` WILL deref `_userManager` in the audit block. **But the `try/catch` swallow absorbs the resulting `NullReferenceException`**, so the edit still returns success and existing Edit tests still pass (data cascade asserted before audit runs). This is the swallow-on-failure contract working as intended — and doubles as live proof of SC#3. **Regression-safety: do NOT change the existing factory; existing Edit tests remain green.**
2. **MaxLength(50) on ActionType** — `"EditOrganizationUnit"` fits. Don't append anything to the ActionType string.
3. **No-op edit is NOT rejected.** Same-name+same-parent on the same unit passes the duplicate check (`u.Id != id` excludes self) and commits successfully with zero cascade. The D-01 guard is what suppresses the audit row, NOT a rejection. → discriminating test (SC: only-on-change).
4. **Raw parent IDs (D-03)** — do NOT query for parent names inside the swallow block (keeps it light, fewer failure points). `null` parent renders as `"null"`.
5. **InMemory ignores transactions** (TransactionIgnoredWarning suppressed in harness) — cascade SaveChanges already persisted; audit LogAsync does a second SaveChanges. Fine for InMemory.

## Validation Architecture

**Test layer:** xUnit controller test, **InMemory DB** — extend `HcPortal.Tests/OrganizationControllerTests.cs` (the canonical analog; already tests `EditOrganizationUnit` cascade). Real SQL is NOT needed: AUDIT-01 is a write-on-change assertion with no SQL-specific behavior (no filtered index; transaction atomicity is InMemory-ignored and irrelevant since audit runs post-commit).

**Reusable asset — UserManager over a fake store** (copy pattern verbatim from `HcPortal.Tests/RetakeExamEndpointTests.cs:47-99`): `FakeUserStore : IUserStore<ApplicationUser>, IUserRoleStore<ApplicationUser>` + `MakeUserManager(store)`. Needed because the audit block resolves the actor via `_userManager.GetUserAsync(User)`, which requires a non-null UserManager + a `ClaimsPrincipal` carrying `ClaimTypes.NameIdentifier = user.Id`.

**New test factory** (add to OrganizationControllerTests): `MakeControllerWithUser(ApplicationUser actor)` →
- `var store = new FakeUserStore(); store.Add(actor);`
- `var um = MakeUserManager(store);`
- `httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]{ new Claim(ClaimTypes.NameIdentifier, actor.Id) }, "TestAuth"));`
- `new OrganizationController(ctx, um, new AuditLogService(ctx), null!)` — keep `X-Requested-With: XMLHttpRequest` header so responses are JSON (reuse existing GetSuccess helper).
- Reads back: `ctx.AuditLogs.Where(a => a.ActionType == "EditOrganizationUnit")`.

**Required test cases (Dimension-8 coverage for AUDIT-01):**

| # | Test | Asserts | SC |
|---|------|---------|-----|
| T1 | `EditOrganizationUnit_RenameLevel1_WritesOneAuditRow` | exactly 1 row ActionType="EditOrganizationUnit"; `TargetId==unitId`; `TargetType=="OrganizationUnit"`; `ActorName=="{NIP} - {FullName}"` of seeded actor; Description contains `'OldName'→'NewName'` | SC#1 |
| T2 | `EditOrganizationUnit_Reparent_WritesParentIdsInDescription` | reparent single-unit worker (allowed path, mirror existing `...ReparentSingleUnitWorker_Allowed`); 1 row; Description contains `parent {oldId}→{newId}` (raw IDs, D-03) | SC#1, SC#2(counts) |
| T3 | `EditOrganizationUnit_RenameAndReparent_WritesExactlyOneRow` | combined rename+reparent → `Count == 1` (NOT 2) — proves single combined row D-02 | SC#1 |
| T4 | `EditOrganizationUnit_NoChange_WritesZeroAuditRows` | `EditOrganizationUnit(id, sameName, sameParent)` → `GetSuccess==true` AND `Count(ActionType=="EditOrganizationUnit")==0` — proves only-on-change D-01 | SC#1 (only-on-change) |
| T5 | `EditOrganizationUnit_AuditFailure_DoesNotBlockEdit` | use the EXISTING null-`_userManager` factory `MakeController()`; perform a real rename; assert `GetSuccess==true` AND `ctx.UserUnits`/`Users` renamed (cascade succeeded) — proves swallow-on-failure (audit threw, response not blocked) | SC#3 |
| T6 (regression) | existing `EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows` + `...ReparentSingleUnitWorker_Allowed` + `PreviewEditCascade_*` | still GREEN after audit block added (swallow protects null-userManager harness) | SC#4 |

T5 + T6 share the mechanism: with a null `_userManager`, the audit block throws and is swallowed, so any Edit-with-change test on the old factory simultaneously proves SC#3/SC#4. T1-T4 use the new user-factory to prove the happy-path audit content.

**Run command (local, ITHandoff):** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"`. No `Category=Integration` trait needed (InMemory, no SQL).

**Coverage check:** AUDIT-01 → SC#1 (T1,T3,T4), SC#2 cascade-counts-in-description (T1,T2 assert counts substring), SC#3 swallow (T5), SC#4 no-behavior-regression (T6). All four success criteria have at least one discriminating test.
