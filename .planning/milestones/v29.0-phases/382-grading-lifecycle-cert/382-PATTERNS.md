# Phase 382: Grading / Lifecycle / Cert - Pattern Map

**Mapped:** 2026-06-14
**Files analyzed:** 6 source MODIFY + 1 test REWRITE + 1 fixture (new) + 1 e2e extend = 9
**Analogs found:** 9 / 9 (all in-repo precedents — bug-fix phase, no new mechanisms)

> Semua analog di sini = pola in-repo yang SUDAH ada (carry-forward). Phase 382 memperluas guard yang kurang lengkap + dedupe-read + balik allowlist timer + null-cert semantics. **Migration=false.** TIDAK ada sub-agent paralel di `CMPController.cs`/`GradingService.cs` (densitas same-method tinggi — lihat Shared Patterns §Same-Method Density).

## File Classification

| File (MODIFY unless noted) | Role | Data Flow | Closest Analog | Match Quality |
|----------------------------|------|-----------|----------------|---------------|
| `Services/GradingService.cs` | service | request-response (grading commit) | self (essay branch L202-211 = STAT-01 guard pattern; ExecuteUpdate L238-246) | exact (self-precedent) |
| `Controllers/CMPController.cs` :: `SubmitExam` L1544-1731 | controller | request-response | self (existing dedup GroupBy L1645-1647; guard L1568) | exact |
| `Controllers/CMPController.cs` :: `SaveAnswer` L345-417 | controller | request-response (upsert) | self (existing ExecuteUpdate-first upsert L371-401) | exact |
| `Controllers/CMPController.cs` :: `AbandonExam` L1241-1271 | controller | request-response (status transition) | `GradingService` ExecuteUpdate guard L238-246 | role-match (cross-file precedent) |
| `Controllers/CMPController.cs` :: `EnsureCanSubmitExamAsync` L4405-4467 | controller (private guard) | request-response (timer enforce) | self (allowlist L4413-4418) | exact |
| `Models/CertificationManagementViewModel.cs` :: `DeriveCertificateStatus` L53-65 | model (static helper) | transform (derive enum) | self (L56-64) | exact |
| `Models/AssessmentConstants.cs` (maybe add `Abandoned`) | config (constants) | — | self (`AssessmentStatus` block L13-21) | exact |
| `HcPortal.Tests/CertificateStatusTests.cs` L31-36 (REWRITE) | test (unit) | transform | self (Theory L17-29) | exact |
| NEW xUnit integration fixtures | test (integration real-SQL) | CRUD/race | `ProtonCompletionFixture` (`ProtonCompletionServiceTests.cs:25-61`) | exact (Phase 365/358 precedent) |
| `tests/e2e/exam-taking.spec.ts` (EXTEND #8-12) | test (e2e) | request-response | self (existing spec) | exact |

**CERT-01 consumers (read-path, ikut otomatis via Status enum — verifikasi tak drift, BUKAN ubah logika):**
| File | Line | Konsumsi |
|------|------|----------|
| `Controllers/AdminBaseController.cs` | L187 build, L200 POST-filter | `DeriveCertificateStatus(a.ValidUntil, null)` + `Status==Expired\|\|AkanExpired` |
| `Controllers/RenewalController.cs` | tally L217/277/300/351, order L262/288/338 | `Count(Status==Expired/AkanExpired)` |
| `Controllers/CDPController.cs` | tally L3734/3793, row L4069 | idem |
| `Controllers/HomeController.cs` | notif L121-126, badge L214-220 | raw `ValidUntil.HasValue` — **SUDAH exclude null (konsisten, JANGAN ubah)** |

---

## Pattern Assignments

### `Services/GradingService.cs` (service, request-response)

**Analog:** self — essay branch (STAT-01 guard already excludes 2 states) + non-essay ExecuteUpdate.

**SAVE-01 dedupe-read** — REPLACE `FirstOrDefault` at L96-97 (MC scoring) and L151-152 (ET scoring). Both already operate on `allResponses` materialized via `.ToListAsync()` (L78-80). Build a final-per-question lookup ONCE before the loops:
```csharp
// allResponses already ToListAsync()'d at L78-80 — in-memory dedupe (EF Core 8 won't translate GroupBy(g => g.First()) of entity)
var finalByQuestion = allResponses
    .Where(r => r.PackageOptionId.HasValue)
    .GroupBy(r => r.PackageQuestionId)
    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());
// then L96-97: var mcResponse = finalByQuestion.TryGetValue(q.Id, out var fr) ? fr : null;
```
> NB: MA branches (L106-118, L160-167) read ALL rows per question (multi-row by design) — do NOT dedupe those; SAVE-01 single-answer = MC only.

**STAT-01 guard expand** — extend the non-essay ExecuteUpdate WHERE at L238-239 (currently only `Status != "Completed"`). Mirror the essay-branch precedent at L202-203 which ALREADY guards `!= Completed && != PendingGrading`:
```csharp
// CURRENT non-essay L238-239 — only Completed
.Where(s => s.Id == session.Id && s.Status != "Completed")
// TARGET — add terminal/non-resurrectable states (use AssessmentConstants, carry-forward v22.0)
using S = AssessmentConstants.AssessmentStatus;
.Where(s => s.Id == session.Id
    && s.Status != S.Completed && s.Status != S.Abandoned
    && s.Status != S.Cancelled && s.Status != S.PendingGrading)
```
**rowsAffected==0 branch already exists** (L248-255) — returns `false` (race/resurrection blocked). Reuse verbatim. Apply the same expansion to the essay branch L202-203 (add Abandoned/Cancelled).

---

### `Controllers/CMPController.cs :: SubmitExam` (controller, request-response)

**Analog:** self. Multi-REQ method (SAVE-01 + STAT-01 + TMR + TOK-02) — single coherent edit.

**SAVE-01** — L1645-1647 currently `GroupBy(...).ToDictionary(g => g.Key, g => g.First())` (arbitrary insertion order). Add OrderBy at this ONE site so SubmitExam Score (SignalR push) == GradingService Score (Pitfall 1):
```csharp
// CURRENT L1645-1647
var existingResponses = allExistingResponses
    .GroupBy(r => r.PackageQuestionId).ToDictionary(g => g.Key, g => g.First());
// TARGET — final-write-wins
.ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());
```

**STAT-01 early guard** — L1568 currently `if (assessment.Status == "Completed")`. Expand to terminal set + BI message + audit:
```csharp
using S = AssessmentConstants.AssessmentStatus;
if (assessment.Status == S.Completed || assessment.Status == S.Abandoned
    || assessment.Status == S.Cancelled || assessment.Status == S.PendingGrading)
{
    // audit "SubmitExamBlocked" (resurrection) — reuse WriteSubmitBlockedAuditAsync pattern, try/catch swallow
    TempData["Error"] = "Sesi ujian ini sudah berakhir dan tidak dapat dikirim ulang.";
    return RedirectToAction("Assessment");
}
```

**TOK-02 gate** — insert after ownership check (L1565), before any mutation:
```csharp
if (assessment.IsTokenRequired && assessment.StartedAt == null) {
    TempData["Error"] = "Ujian belum dimulai. Masukkan token melalui halaman ujian.";
    return RedirectToAction("Assessment");
}
```

**TMR-02 (D-06)** — L1584 `if (!isAutoSubmit && !serverTimerExpired)`: prefer server-computed `serverTimerExpired` (L1576-1582) as authoritative; client `isAutoSubmit` hint must not be the sole gate-bypass.

**Commit order in this method (audit-mandated, no parallel sub-agent):** SAVE-01 read-final → STAT-01 guard → TOK-02 gate → TMR-02 (guards at top, before mutation).

---

### `Controllers/CMPController.cs :: SaveAnswer` (controller, upsert)

**Analog:** self (L371-401 — ExecuteUpdate-first upsert already present).

**TOK-02 gate** — insert after L364 (owner check), as Json (this handler returns Json not Redirect):
```csharp
if (session.IsTokenRequired && session.StartedAt == null)
    return Json(new { success = false, error = "Ujian belum dimulai. Masukkan token melalui halaman ujian." });
```
**SAVE-01 write-harden (best-effort, D-01 discretion):** existing upsert L371-401 is already ExecuteUpdate-first → insert → catch. **Anti-pattern note (Pattern 2):** the `catch (DbUpdateException)` at L391 is DEAD CODE since migration `20260407070949_RemoveUniqueIndexOnPackageUserResponse`. Do NOT rely on it; read-side dedupe (GradingService) is the real mitigation. Keep upsert as-is or wrap in one transaction — do NOT add a unique index (Migration=false).

---

### `Controllers/CMPController.cs :: AbandonExam` (controller, status transition)

**Analog:** `GradingService.cs` L238-260 (ExecuteUpdate + WHERE guard + rowsAffected branch).

**STAT-02** — replace TOCTOU read-check (L1257) + change-tracker `SaveChangesAsync` (L1263-1267) with single atomic guarded UPDATE. **Keep ownership in WHERE** (Pitfall 2 — `UserId` must stay):
```csharp
using S = AssessmentConstants.AssessmentStatus;
var rowsAffected = await _context.AssessmentSessions
    .Where(a => a.Id == id && a.UserId == user.Id
        && (a.Status == S.InProgress || a.Status == S.Open))
    .ExecuteUpdateAsync(a => a
        .SetProperty(x => x.Status, "Abandoned")        // or AssessmentConstants.AssessmentStatus.Abandoned if const added
        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
if (rowsAffected == 0) {
    TempData["Error"] = "Sesi ujian ini tidak dapat dibatalkan karena sudah selesai atau dinilai.";
    return RedirectToAction("Assessment");
}
```
> Current L1245-1254 already loads entity for `UserId == user.Id` Forbid — you may keep early Forbid for 403 semantics AND include UserId in WHERE for atomicity. The `"Abandoned"` literal is currently used at L1264 (string literal, no const).

---

### `Controllers/CMPController.cs :: EnsureCanSubmitExamAsync` (controller, timer guard)

**Analog:** self (L4413-4418 allowlist).

**TMR-01 allowlist inversion** — L4413-4418 currently allowlist `Online/PreTest/PostTest` → "Standard" never matches → dead code (skips). Invert to blocklist (skip ONLY Manual/null):
```csharp
// CURRENT L4413-4418 (allowlist — Standard falls through to `return null` skip)
// TARGET — invert
if (assessment.AssessmentType == AssessmentConstants.AssessmentType.Manual
    || string.IsNullOrEmpty(assessment.AssessmentType))
    return null;  // skip guard
// Standard / Online / PreTest / PostTest → enforce tier-1/tier-2 below (unchanged)
```

**TMR-03 token-not-consumed-before-commit (Pitfall 3):** `TempData.Remove(tempKey)` at L4445 fires BEFORE grading commits. If grading throws, retry loses token → permanent reject. Fix: validate token without removing here; remove only on grading success path (in `SubmitExam` after `GradeAndCompleteAsync` returns true), OR rely on grading idempotency (rowsAffected==0 = already done). Do NOT reject the on-time auto-submit path (`serverApprovedAutoSubmit==true` must reach grading — D-05).

**Audit reuse:** `WriteSubmitBlockedAuditAsync` (L4473-4516) already writes `SubmitExamBlocked` (actionType, try/catch swallow, `event=audit_drop_phase313` log key). Reuse verbatim for STAT-01 reject audit too.

---

### `Models/CertificationManagementViewModel.cs :: DeriveCertificateStatus` (model, transform)

**Analog:** self (L56-64).

**CERT-01 single-source** — L58-59 currently returns `Expired` for non-Permanent null. Flip to `Aktif` (Permanen tanpa kedaluwarsa). Recommendation A3: return `Aktif` (not `.Permanent`, which carries admin `certificateType` meaning):
```csharp
// CURRENT L56-59
if (certificateType == "Permanent") return CertificateStatus.Permanent;
if (validUntil == null) return CertificateStatus.Expired;  // ← FLIP
// TARGET
if (certificateType == "Permanent") return CertificateStatus.Permanent;
if (validUntil == null) return CertificateStatus.Aktif;    // lulus cert tanpa kedaluwarsa = Aktif/Permanen
```
> Single-source benefit: AdminBase L200 / Renewal tally / CDP tally all consume the returned `Status` enum → cert null auto-drops from renewal worklist + tallies WITHOUT touching those files (Pattern 7). Verify no surface independently re-derives status from raw `ValidUntil` (drift check).

---

### `Models/AssessmentConstants.cs` (config)

**Analog:** self (`AssessmentStatus` block L13-21).

**Open Question 1 / A2** — block has Open/Upcoming/Completed/PendingGrading/InProgress/Cancelled but NO `Abandoned`. Recommendation: add for STAT-01/02 single-source (v22.0 discipline):
```csharp
public const string Abandoned = "Abandoned";  // Phase 382 STAT-01/02 — was bare literal at AbandonExam L1264
```

---

### `HcPortal.Tests/CertificateStatusTests.cs` (test, REWRITE)

**Analog:** self (existing Theory L17-29 + Fact L38-43).

**D-08-TEST / Pitfall 4** — `DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsExpired` (L31-36) WILL break. Rewrite the assertion:
```csharp
[Fact]  // was _ReturnsExpired
public void DeriveCertificateStatus_NullValidUntil_NonPermanent_ReturnsAktif()
{
    var result = SertifikatRow.DeriveCertificateStatus(null, null);
    Assert.Equal(CertificateStatus.Aktif, result);
}
```
Keep `_Permanent_ReturnsPermanent` (L38-43) UNCHANGED.

---

### NEW xUnit integration fixtures (test, real-SQL race)

**Analog:** `ProtonCompletionFixture` (`HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61`) — Phase 358/365 disposable real-SQL-Server pattern. InMemory does NOT enforce race/unique → concurrent SaveAnswer + AbandonExam-vs-graded MUST use this.

**Copy the fixture shape verbatim:**
```csharp
public class GradingRaceFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options => _options;
    // ctor: Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true
    // InitializeAsync: MigrateAsync() full pipeline; catch → EnsureDeletedAsync + XunitException
    // DisposeAsync: EnsureDeletedAsync()
}
[Trait("Category", "Integration")]            // CI SQL-less skip via --filter "Category!=Integration"
public class AbandonGuardTests : IClassFixture<GradingRaceFixture> { ... }
```
> Each `[Fact]` uses unique GUID-suffixed entity ids (DB shared across facts in a class) — see `ProtonCompletionMissTests.cs:43` (`$"miss-{Guid.NewGuid():N}"`). New classes per Wave 0 gap: `GradingDedupeTests` (unit, InMemory OK), `SubmitResurrectionTests`, `AbandonGuardTests` (real-SQL), `EnsureCanSubmitStandardTests`, `AutoSubmitTokenRetryTests`, `TokenGateTests`, `CertAlertConsistencyTests`.

---

### `tests/e2e/exam-taking.spec.ts` (test, EXTEND)

**Analog:** self (existing spec) + helpers `examTypes.ts`, `dbSnapshot`. **WAJIB `--workers=1`** (DB isolation, [MEMORY reference_local_e2e_sql_env_fix]); AD lokal off (`Authentication__UseActiveDirectory=false dotnet run`).
Add scenarios #8 (anti-resurrection), #9 (abandon-vs-graded), #10 (concurrent save — integration preferred), #11 (timer Standard StartedAt-mundur seed), #12 (cert null → Results LULUS+PDF + dashboard Aktif/Permanen + badge/notif konsisten).

---

## Shared Patterns

### Race-safe status transition (ExecuteUpdate guard)
**Source:** `Services/GradingService.cs` L238-255 (and essay branch L202-219).
**Apply to:** GradingService STAT-01 (both branches), AbandonExam STAT-02.
```csharp
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == session.Id && /* status guard */)
    .ExecuteUpdateAsync(s => s.SetProperty(...));
if (rowsAffected == 0) { /* race/resurrection blocked */ return false; }  // branch ALREADY exists, reuse
```

### Status string single-source
**Source:** `Models/AssessmentConstants.cs` L13-21 (`AssessmentStatus.*`).
**Apply to:** ALL guards in this phase. `using S = AssessmentConstants.AssessmentStatus;` then `S.Completed`/`S.Abandoned`/`S.Cancelled`/`S.PendingGrading`. Carry-forward v22.0 — do NOT hardcode literals (except existing `"Abandoned"` literal until const added).

### Audit reject (try/catch swallow)
**Source:** `Controllers/CMPController.cs :: WriteSubmitBlockedAuditAsync` L4473-4516.
**Apply to:** STAT-01 reject + TMR-01 `SubmitExamBlocked`. actionType convention `{Action}Blocked`; actor `NIP - FullName`; swallow on failure with `_logger.LogWarning(..., event=audit_drop)`.

### Real-SQL disposable fixture (race tests)
**Source:** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61` (`ProtonCompletionFixture`).
**Apply to:** all concurrent/race integration tests (SAVE-01, STAT-02). `[Trait("Category","Integration")]` + GUID-unique entity ids per fact.

### CERT-01 single-source helper → consumers auto-derive
**Source:** `Models/CertificationManagementViewModel.cs :: DeriveCertificateStatus` (sole authority); consumers read `Status` enum (`AdminBaseController.cs` L187/L200).
**Apply to:** fix helper ONLY; AdminBase/Renewal/CDP tallies follow automatically. HomeController (L124/L215) already filters `ValidUntil.HasValue` — leave untouched, verify no drift.

### Same-Method Density (execution constraint)
`GradeAndCompleteAsync` ← SAVE-01 + STAT-01. `SubmitExam` ← SAVE-01 + STAT-01 + TMR + TOK-02. **NO intra-phase parallel sub-agent on `CMPController.cs`/`GradingService.cs`** (audit explicit). Commit order: SAVE-01 read-final → STAT-01 → STAT-02 → TMR-01/02/03 → TOK-02 → CERT-01. CMPController is `[soft]` overlap with Phase 381 → EXECUTE SERI after 381 lands.

### Migration=false guard
**Source:** Pitfall 5 / D-01-IMPACT. After implementation run `dotnet ef migrations add _verify_382 --no-build` → confirm NO model diff → delete. Update ROADMAP Phase 382 `Migration: false`.

---

## No Analog Found

(none — every target has an in-repo precedent; this is a bug-fix correctness phase, no new mechanisms.)

## Metadata

**Analog search scope:** `Services/`, `Controllers/`, `Models/`, `HcPortal.Tests/`, `tests/e2e/`
**Files scanned:** GradingService.cs, CMPController.cs (4 regions), CertificationManagementViewModel.cs, AssessmentConstants.cs, HomeController.cs, AdminBaseController.cs, CertificateStatusTests.cs, ProtonCompletionServiceTests.cs, ProtonCompletionMissTests.cs
**Pattern extraction date:** 2026-06-14
