# Phase 401: PROTON Unit-Resolution Hardening - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 7 modification clusters (5 controllers/service + 1 view + 1 test-suite) — HARDENING phase, 0 migration, 0 new source files
**Analogs found:** 7 / 7 (semua analog = pola yang SUDAH ada di codebase; 401 = wiring pola existing ke site baru, BUKAN bangun infra baru)

> **Catatan struktur:** Ini phase hardening yang memodifikasi file existing — bukan create file baru. "Closest analog" di sini = pola/behavior EXISTING (kadang di file yang SAMA) yang harus disalin/diperketat ke site target. Planner: tiap cluster di-map ke `file:line` analog + excerpt konkret + apa yang berubah.

---

## File Classification

| Modified File | Cluster Role | Data Flow | Closest Analog (behavior existing) | Match |
|---------------|--------------|-----------|-------------------------------------|-------|
| `Controllers/CoachMappingController.cs` (resolver `:1409-1419`, `:1456-1485`) | resolver (drop-fallback) + skip/audit | request-response (read-path + gate) | `AutoCreateProgress` skip+warn `:1475-1479` (file sama) | exact |
| `Controllers/CoachMappingController.cs` (Assign `:468-474`, Edit `:719-749`, Import `:325-373`) | validator (`∈ UserUnits`) | request-response (write-path) | org-tree reject `:471-474` + `WorkerController.ValidateUnitsInSection:63` | exact |
| `Controllers/CoachMappingController.cs` (Cleanup `:880-907`) | no-clobber guard | batch (mutate-in-loop) | conditional-fix-then-continue `:887-903` (file sama) | exact |
| `Controllers/CoachMappingController.cs` (Reactivate `:1017-1097`, Import-reactivate `:350-362`) | reactivation guard | request-response / batch | window AF-4 `:1052-1076` (preserve, add validasi) | role-match |
| `Controllers/CoachMappingController.cs` (GET `:42-165`) | UI indicator (compute on-demand) | request-response (GET) | `CleanupReport` TempData `:911` + view `:74-95` | exact |
| `Controllers/AssessmentAdminController.cs` (gate `:1411-1414`) | resolver drop-fallback + **BLOCK** + AuditLog | request-response (cert-issuing gate) | resolver `CoachMapping:1409-1419` + `_auditLog.LogAsync:1081/1652` | role-match |
| `Controllers/CDPController.cs` (`:491,:1586,:1596,:4248`; defensive `:515-526,:1708-1719`) | filter-axis swap + defensive resolver | batch (listing query) | batch-mapping-dict `:515-526` (file sama) | exact |
| `Controllers/ProtonDataController.cs` (BypassList `:1517`; BypassSave `:1638`) | filter-axis swap + TargetUnit validator | request-response (listing + write) | non-empty reject `:1632-1639` (file sama) | exact |
| `Services/ProtonBypassService.cs` (`:445-466`; E8 `:104-118` UNTOUCHED) | (optional) defensive validator | request-response (service write) | E8 dual-check `:104-118` + WR-02 guard `:456-466` | exact |
| `Views/Admin/CoachCoacheeMapping.cshtml` (D-01 indicator) | UI indicator render | server-render (Razor) | `CleanupReport` alert `:74-95` (file sama) | exact |
| `HcPortal.Tests/*.cs` (5 file baru) | test (resolver/validator/audit) | unit (EF-InMemory) + integration | `UserUnitsWriteThroughTests` / `RemoveUnitGuardTests` / `CapturingLogger` / `ProtonCompletionFixture` | exact |

---

## Pattern Assignments

### 1. Resolver drop-fallback `?? User.Unit` (PSU-01) — 5 site

**Behavior baru:** Hapus cabang fallback `User.Unit`; resolusi unit HANYA dari `AssignmentUnit` mapping aktif.

**Closest analog (perketat-in-place):** `Controllers/CoachMappingController.cs:1409-1419` (GetEligibleCoachees) — site target itu sendiri. Pola "resolve AssignmentUnit → fallback Users.Unit → skip kalau kosong" muncul **IDENTIK** di 5 tempat; semua di-edit dengan transformasi sama.

**Current shape — GetEligibleCoachees** (`CoachMappingController.cs:1409-1419`, DROP baris bertanda):
```csharp
var assignmentUnit = await _context.CoachCoacheeMappings
    .Where(m => m.CoacheeId == coacheeId && m.IsActive)
    .Select(m => m.AssignmentUnit)
    .FirstOrDefaultAsync();
var resolvedUnit = assignmentUnit;
if (string.IsNullOrWhiteSpace(resolvedUnit))      // <-- DROP cabang fallback (PSU-01)
    resolvedUnit = await _context.Users           // <-- DROP
        .Where(u => u.Id == coacheeId)            // <-- DROP
        .Select(u => u.Unit)                      // <-- DROP
        .FirstOrDefaultAsync();                   // <-- DROP
if (string.IsNullOrWhiteSpace(resolvedUnit)) continue; // <-- jadi "AssignmentUnit kosong saja" (D-02)
```

**Identical analog di file sama** (`AutoCreateProgressForAssignment:1466-1479`) — sudah ada blok `warnings.Add(...)` skip yang dipakai untuk read-path channel:
```csharp
var resolvedUnit = assignmentUnit;
if (string.IsNullOrWhiteSpace(resolvedUnit))
{
    resolvedUnit = await _context.Users
        .Where(u => u.Id == coacheeId)
        .Select(u => u.Unit)
        .FirstOrDefaultAsync();            // <-- DROP blok ini
}
if (string.IsNullOrWhiteSpace(resolvedUnit))
{
    warnings.Add($"Coachee {coacheeId} tidak memiliki AssignmentUnit maupun Unit — progress tidak dibuat.");
    return warnings;                       // <-- pesan diperketat jadi "AssignmentUnit kosong"
}
```

**5 site identik (semua transformasi sama):**
| Site | File:line | Channel skip (D-03) |
|------|-----------|---------------------|
| GetEligibleCoachees (gate) | `CoachMappingController.cs:1409-1419` | **AuditLog persisted + LogWarning** (gate) |
| AutoCreateProgressForAssignment | `CoachMappingController.cs:1466-1479` | **LogWarning / warnings.Add saja** (read-path) |
| CreateAssessment gate | `AssessmentAdminController.cs:1411-1415` | **AuditLog persisted + LogWarning** (gate, BLOCK) |
| CDP defensive ×1 | `CDPController.cs:519-526` (`?? userUnits129.GetValueOrDefault`) | **LogWarning saja** (read-path) |
| CDP defensive ×2 | `CDPController.cs:1712-1719` (`?? userUnits129.GetValueOrDefault`) | **LogWarning saja** (read-path) |

**⚠️ Pitfall 3 (research):** setelah drop fallback, `resolvedUnit == AssignmentUnit`, jadi `IsNullOrWhiteSpace(resolvedUnit)` otomatis jadi "AssignmentUnit kosong saja" = BENAR (D-02). Grep guard: `0` `.Select(u => u.Unit)` tersisa di 5 resolver.

---

### 2. Validasi `AssignmentUnit ∈ coachee.UserUnits` (PSU-03)

**Behavior baru:** Tambah lapis validasi membership junction — `AssignmentUnit` harus salah satu unit aktif coachee.

**Closest analog A (reject-pattern existing — file sama):** `CoachMappingController.cs:471-474` (Assign, validasi org-tree). Ini pola reject membership `X ∈ collection` yang sudah dipakai; validasi `∈ UserUnits` ditambah SETELAH cek ini:
```csharp
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
if (!sectionUnitsDict.TryGetValue(req.AssignmentSection!.Trim(), out var validUnits)
    || !validUnits.Contains(req.AssignmentUnit!.Trim()))
    return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });
```

**Closest analog B (junction query — TIDAK ada nav property):** `WorkerController.SyncUserUnitsAsync:86` — satu-satunya cara baca UserUnits (`ApplicationUser` TAK punya nav collection):
```csharp
var existing = await context.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
```
Untuk validasi 401 (aktif-only): `_context.UserUnits.Where(uu => uu.UserId == coacheeId && uu.IsActive).Select(uu => uu.Unit)`.

**Closest analog C (testable-seam static validator):** `WorkerController.ValidateUnitsInSection:63-74` — pola `public static List<string>` validator (return daftar error, kosong=valid) yang EF-InMemory-testable. Rekomendasi 401: ekstrak `public static Task<bool> ValidateAssignmentUnitInUserUnits(ApplicationDbContext ctx, string coacheeId, string assignmentUnit)`:
```csharp
public static List<string> ValidateUnitsInSection(
    List<string> validUnits, List<string> units, string? primaryUnit, string? sectionName)
{
    var errors = new List<string>();
    var invalid = (units ?? new()).Where(u => !validUnits.Contains(u)).ToList();
    if (invalid.Any())
        errors.Add($"Unit tidak valid untuk '{sectionName}': {string.Join(", ", invalid)}");
    ...
    return errors;
}
```

**Site target validasi `∈ UserUnits` + granularity (Claude's Discretion CONTEXT):**
| Site | File:line | Mode reject |
|------|-----------|-------------|
| Assign | `CoachMappingController.cs:468-474` (setelah org-tree) | `return Json(success=false, message=...)` per-batch |
| Edit | `CoachMappingController.cs:719-726` (sebelum `BeginTransactionAsync:737` — Pitfall 4) | `return Json(...)` |
| Import-new | `CoachMappingController.cs:325-372` (lapisan setelah org-tree `:326-334`) | per-row `result.Status="Error"` |
| Import-reactivate | `CoachMappingController.cs:350-362` | per-row `result.Status="Error"/"Skip"` |
| Bypass TargetUnit | `ProtonDataController.cs:1638` | `return Json(success=false, ...)` |

**Import per-row reject pattern (analog file sama `:326-334`):**
```csharp
if (string.IsNullOrEmpty(coacheeUser.Section) || string.IsNullOrEmpty(coacheeUser.Unit)
    || !sectionUnitsDict.TryGetValue(coacheeUser.Section.Trim(), out var vuImport)
    || !vuImport.Contains(coacheeUser.Unit.Trim()))
{
    result.Status = "Error";
    result.Message = $"Section/Unit coachee ('{coacheeUser.Section}'/'{coacheeUser.Unit}') tidak valid di OrganizationUnit aktif";
    results.Add(result);
    continue;
}
```

---

### 3. No-clobber Cleanup (PSU-04) — gated preserve

**Behavior baru:** `CleanupCoachCoacheeMappingOrg` JANGAN reset `AssignmentUnit` ke primary bila unit existing masih sah `∈ coachee.UserUnits` aktif.

**Closest analog (clobber site itu sendiri — file sama):** `CoachMappingController.cs:887-903`. Pola "isValid?continue : try-fix-from-user-record" sudah ada; target = tambah cek `∈ UserUnits` ke definisi `isValid` (atau gate sebelum clobber `:899-900`):
```csharp
if (isValid) continue;                              // sudah valid vs org-tree → skip

// Try fix from coachee user record
if (userDict.TryGetValue(m.CoacheeId, out var coacheeInfo))
{
    var userSec = coacheeInfo.Section?.Trim();
    var userUnit = coacheeInfo.Unit?.Trim();
    bool userValid = !string.IsNullOrEmpty(userSec) && !string.IsNullOrEmpty(userUnit)
        && sectionUnitsDict.TryGetValue(userSec, out var vuUser) && vuUser.Contains(userUnit);
    if (userValid)
    {
        m.AssignmentSection = userSec;
        m.AssignmentUnit = userUnit;   // <-- CLOBBER ke primary (PSU-04 data-loss vector — GATE INI)
        autoFixed++;
        continue;
    }
}
```
**Transformasi (research Pattern 3):** SEBELUM clobber ke `userUnit` (primary), cek `m.AssignmentUnit` existing masih `∈ coachee.UserUnits` aktif → bila ya: PRESERVE (jangan reset), perbaiki `AssignmentSection` saja bila perlu. "valid vs org-tree" (`:884-885`) ≠ "∈ UserUnits coachee" — multi-unit coachee bisa punya `AssignmentUnit` non-primary yang sah. Junction query: analog #2-B.

---

### 4. Reactivation preserve + validasi (PSU-07) — window AF-4 UTUH

**Behavior baru:** TAMBAH validasi `mapping.AssignmentUnit ∈ coachee.UserUnits` aktif sebelum reaktivasi; PRESERVE `AssignmentUnit`. JANGAN ubah korelasi `DeactivatedAt ±5s`.

**Closest analog A (`CoachCoacheeMappingReactivate` window AF-4 — file sama):** `CoachMappingController.cs:1052-1076`. **PERTAHANKAN UTUH** (D-05 / AF-4 comment `:1043-1051`):
```csharp
if (originalEndDate.HasValue)
{
    inactiveAssignments = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == mapping.CoacheeId
            && !a.IsActive
            && a.DeactivatedAt != null
            && EF.Functions.DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) >= -5
            && EF.Functions.DateDiffSecond(a.DeactivatedAt!.Value, originalEndDate.Value) <= 5)
        .ToListAsync();
}
```
**Transformasi:** sisip validasi `∈ UserUnits` SETELAH duplicate-check `:1025-1029` & SEBELUM `mapping.IsActive = true` (`:1037`) — reject via `return Json(success=false, ...)` bila unit dilepas. `AssignmentUnit` reactivate kontroler ini SUDAH preserved by default (tak di-reset) — confirm tak ada clobber. **Grep guard PSU-07:** `DateDiffSecond(...) >= -5 && <= 5` (`:1059-1060`) UTUH.

**Closest analog B (Import-reactivate CLOBBER — file sama `:350-362`):** BEDA dari A — ADA clobber yang harus DIHAPUS (D-04):
```csharp
if (inactiveMapping != null)
{
    inactiveMapping.IsActive = true;
    inactiveMapping.StartDate = DateTime.Today;
    inactiveMapping.EndDate = null;
    inactiveMapping.AssignmentSection = coacheeUser.Section.Trim();
    inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim();   // <-- HAPUS (clobber→primary). PRESERVE existing + validasi ∈UserUnits
    reactivatedMappings.Add(inactiveMapping);
    result.Status = "Reactivated";
    result.Message = "Mapping diaktifkan kembali";
    results.Add(result);
    continue;
}
```
**Open Q2 research (Import-reactivate PTA `:403-407` "last assignment"):** JANGAN re-architect PTA-selection (sama spirit AF-4) — cukup no-clobber + validasi unit di sisi mapping (D-04/D-05).

---

### 5. UI indicator on-demand (D-01) — reuse CleanupReport idiom

**Behavior baru:** Di `CoachCoacheeMapping` GET, hitung mapping aktif yang `AssignmentUnit` kosong ATAU `∉ coachee.UserUnits` aktif; surface sebagai alert/badge Bootstrap 5.

**Closest analog A (controller compute → TempData, file sama):** `CoachMappingController.cs:911` (`CleanupReport`):
```csharp
TempData["CleanupReport"] = System.Text.Json.JsonSerializer.Serialize(new { autoFixed, unfixable });
return RedirectToAction(nameof(CoachCoacheeMapping));
```
**Transformasi:** compute on-demand DI GET action (sebelum `return View()`), surface via `ViewBag.OrphanUnitMappings` (count + daftar). GET action header analog `:42-58` (sudah load `allUsers` + `mappings`). Junction read = analog #2-B (`_context.UserUnits.Where(uu => coacheeIds.Contains(uu.UserId) && uu.IsActive)`).

**Closest analog B (view render alert — `Views/Admin/CoachCoacheeMapping.cshtml:74-95`):**
```cshtml
@{
    var cleanupJson = TempData["CleanupReport"] as string;
    if (!string.IsNullOrEmpty(cleanupJson))
    {
        var cleanup = System.Text.Json.JsonDocument.Parse(cleanupJson);
        var autoFixed = cleanup.RootElement.GetProperty("autoFixed").GetInt32();
        var unfixable = cleanup.RootElement.GetProperty("unfixable");
        var unfixableCount = unfixable.GetArrayLength();
        <div class="alert alert-info alert-dismissible fade show">
            <i class="bi bi-wrench me-2"></i>
            <strong>Cleanup selesai:</strong> @autoFixed mapping diperbaiki otomatis.
            @if (unfixableCount > 0) { <span class="text-danger">@unfixableCount mapping tidak dapat diperbaiki ...</span> }
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }
}
```
**Bentuk indikator (badge count vs alert daftar) = Claude's Discretion (D-01).** Idiom: `alert alert-warning` + `bi bi-exclamation-triangle` + `btn-close data-bs-dismiss`. Catatan: D-01 dihitung on-demand (bukan TempData lintas-redirect) → bisa langsung `ViewBag` (TempData hanya bila perlu survive redirect).

---

### 6. Gate-eligibility BLOCK + AuditLog persisted (PSU-05 / D-02 / D-03)

**Behavior baru:** Di gate penerbitan session/cert (`AssessmentAdminController:1411-1415` + `GetEligibleCoachees:1409-1419`), bila `AssignmentUnit` kosong → BLOCK (tak terbit) + tulis `AuditLog` persisted + `LogWarning`.

**Closest analog A (gate site itu sendiri — `AssessmentAdminController.cs:1411-1415`):**
```csharp
var resolvedUnit = await _context.CoachCoacheeMappings
    .Where(m => m.CoacheeId == uid && m.IsActive).Select(m => m.AssignmentUnit).FirstOrDefaultAsync();
if (string.IsNullOrWhiteSpace(resolvedUnit))
    resolvedUnit = await _context.Users.Where(u => u.Id == uid).Select(u => u.Unit).FirstOrDefaultAsync(); // <-- DROP
if (string.IsNullOrWhiteSpace(resolvedUnit)) { gateSkippedNotHundred++; continue; } // <-- BLOCK + audit (D-02/D-03)
```

**Closest analog B (`_auditLog.LogAsync` call site — `CoachMappingController.cs:1081`):**
```csharp
await _auditLog.LogAsync(actor.Id, actor.FullName, "Reactivate",
    $"Reactivated coach-coachee mapping #{id} — {reactivatedCount} ProtonTrackAssignment(s) also reactivated",
    targetId: id, targetType: "CoachCoacheeMapping");
```
**Closest analog C (call site di ProtonData/AssessmentAdmin context — `ProtonDataController.cs:1652`):**
```csharp
await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "ProtonBypassSave",
    $"BypassSave {req.Mode} coachee {req.CoacheeId}: ...", targetType: "PendingProtonBypass");
```
**Signature (`Services/AuditLogService.cs:21-27`):** `LogAsync(string actorUserId, string actorName, string actionType, string description, int? targetId=null, string? targetType=null)` — SaveChanges internal. Untuk 401 gate-block: `actionType: "ProtonUnitUnresolved"`, `targetType: "CoachCoacheeMapping"`.

**⚠️ Channel hybrid (D-03):** gate-block (langka) = `_auditLog.LogAsync` + `_logger.LogWarning`. Read-path skip (volume tinggi) = `_logger.LogWarning` SAJA — JANGAN `_auditLog.LogAsync` (banjir `AuditLogs`). `_logger.LogWarning` existing pattern: `CoachMappingController.cs:512`, `AssessmentAdminController.cs:1380`.

---

### 7. Filter-axis swap (PSU-02)

**Behavior baru:** Filter listing/scope coachee berbasis `AssignmentUnit` (active mapping), bukan scalar `User.Unit`.

**Closest analog (batch mapping-unit dictionary — `CDPController.cs:515-526`, file sama):** pola batch-load `unitByCoachee` yang menghindari N+1 (Pitfall 2). INI yang di-reuse untuk swap:
```csharp
var mappingUnits129 = await _context.CoachCoacheeMappings
    .Where(m => m.IsActive && coacheeIdList129.Contains(m.CoacheeId))
    .Select(m => new { m.CoacheeId, m.AssignmentUnit })
    .ToListAsync();
var asnUnitMap129 = asnCoacheeIds129.ToDictionary(
    x => x.Id,
    x => (mappingUnits129.FirstOrDefault(m => m.CoacheeId == x.CoacheeId)?.AssignmentUnit
          ?? userUnits129.GetValueOrDefault(x.CoacheeId))?.Trim() ?? "");  // <-- drop `?? userUnits129` (defensive-filter resolver, PSU-01)
```

**Site target filter-axis swap (`u.Unit == unit` → AssignmentUnit):**
| Site | File:line | Current |
|------|-----------|---------|
| CDP post-filter coachee | `CDPController.cs:491` | `coacheeUsers.Where(u => u.Unit == unit)` |
| CDP scope L≤3 | `CDPController.cs:1586` | `.Where(u => ... u.Unit == unit)` |
| CDP scope L4 | `CDPController.cs:1596` | `.Where(u => ... u.Unit == unit)` |
| CDP unit filter | `CDPController.cs:4248` | `.Where(u => ... u.Unit == unit)` |
| ProtonData BypassList | `ProtonDataController.cs:1517` | `query.Where(x => x.u.Unit == unit)` |

**BypassList analog (`ProtonDataController.cs:1511-1518`):** join `ProtonTrackAssignments × Users × ProtonTracks` — swap filter axis tambah join/subquery ke `CoachCoacheeMappings` aktif:
```csharp
var query = from a in _context.ProtonTrackAssignments
            join u in _context.Users on a.CoacheeId equals u.Id
            join t in _context.ProtonTracks on a.ProtonTrackId equals t.Id
            where a.IsActive
            select new { a, u, t };
if (!string.IsNullOrWhiteSpace(unit)) query = query.Where(x => x.u.Unit == unit);  // <-- swap ke AssignmentUnit
```
**⚠️ A2 (research):** single-active invariant menjamin ≤1 mapping aktif/coachee → `FirstOrDefault(AssignmentUnit)` deterministik. Pertahankan semantik filter, hanya ganti sumbu (Claude's Discretion).

---

### 8. Bypass TargetUnit validasi (PSU-03) + ProtonBypassService defensive

**Behavior baru:** `ProtonDataController.cs:1638` ganti cek non-empty → `TargetUnit ∈ worker.UserUnits` + org-tree. Optional defensive di service (Open Q1).

**Closest analog A (non-empty reject existing — `ProtonDataController.cs:1632-1639`, file sama):**
```csharp
if (string.IsNullOrWhiteSpace(req.CoacheeId))
    return Json(new { success = false, message = "Worker wajib dipilih." });
if (string.IsNullOrWhiteSpace(req.Reason))
    return Json(new { success = false, message = "Alasan wajib diisi." });
if (string.IsNullOrWhiteSpace(req.TargetUnit))           // <-- PERKETAT: + ∈ worker.UserUnits + org-tree
    return Json(new { success = false, message = "Unit tujuan wajib diisi." });
```
Junction query validasi = analog #2-B (`_context.UserUnits.Where(uu => uu.UserId == req.CoacheeId && uu.IsActive)`). Org-tree = `GetSectionUnitsDictAsync()` (analog #2-A).

**Closest analog B (E8 service dual-check existing — `ProtonBypassService.cs:104-118`):** pola validasi defensif di service entry (rollback tx bila gagal) — jadi template bila planner pilih dual-layer (Open Q1):
```csharp
var activeAssignments = await _context.ProtonTrackAssignments
    .Where(a => a.CoacheeId == req.CoacheeId && a.IsActive)
    .ToListAsync();
if (activeAssignments.Count != 1)
{
    await tx.RollbackAsync();
    return new BypassResult(false, $"Worker punya {activeAssignments.Count} assignment aktif (harus tepat 1).");
}
```

**Junction-write yang dilindungi (`ProtonBypassService.cs:449,465`):** validasi `TargetUnit` melindungi 2 site nyata yang menulis `AssignmentUnit = req.TargetUnit`. WR-02 guard existing `:456-466` (TargetUnit kosong jangan timpa unit aktif jadi "") = preseden validasi service-level. **E8 single-active (`:104-118`, `:226-234`) TIDAK disentuh** (invariant #2 — confirm utuh saja).

---

## Shared Patterns

### Junction read UserUnits (TIDAK ada nav property)
**Source:** `WorkerController.SyncUserUnitsAsync:86` ; `ApplicationDbContext.cs:340-359` (`.WithMany()` no-inverse)
**Apply to:** SEMUA validasi/indikator `∈ UserUnits` (cluster #2, #3, #4, #5, #8)
```csharp
var activeUnits = await _context.UserUnits
    .Where(uu => uu.UserId == coacheeId && uu.IsActive)
    .Select(uu => uu.Unit)
    .ToListAsync();
bool unitValid = activeUnits.Any(u =>
    string.Equals(u.Trim(), assignmentUnit.Trim(), StringComparison.OrdinalIgnoreCase));
```
**⚠️ Anti-pattern:** `coachee.UserUnits` / `.Include(u => u.UserUnits)` = COMPILE ERROR (Pitfall 1).

### Org-tree validation primitif
**Source:** `ApplicationDbContext.cs:109-136` ; dipakai `CoachMappingController.cs:471-474`
**Apply to:** Assign / Edit / Import / Bypass TargetUnit (cluster #2, #8)
```csharp
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
bool inOrgTree = sectionUnitsDict.TryGetValue(section.Trim(), out var validUnits)
                 && validUnits.Contains(unit.Trim());
```

### Audit channel hybrid (D-03)
**Source:** `AuditLogService.cs:21-27` (persisted) ; `CoachMappingController.cs:512` (LogWarning)
**Apply to:** read-path skip → `_logger.LogWarning` SAJA ; gate-block → `_auditLog.LogAsync` + `_logger.LogWarning`

### Testable-seam (public static helper + EF-InMemory)
**Source:** `WorkerController.cs:51-163` (`ParseUnitCell`, `ValidateUnitsInSection`, `EvaluateRemoveUnitGuardAsync`)
**Apply to:** ekstrak resolver/validator 401 (PSU-01/03) ke `public static` di controller → unit-test tanpa `InternalsVisibleTo`. AntiForgery + `[Authorize(Roles="Admin, HC")]` di Assign/Edit/Import/Cleanup/Reactivate/Bypass PERTAHANKAN (V4 ASVS).

---

## Test Patterns (Wave 0 — 5 file baru)

| New test file | Analog file | Pattern disalin |
|---------------|-------------|-----------------|
| `ProtonUnitResolveTests.cs` (PSU-01) | `UserUnitsWriteThroughTests.cs` | `InMemoryContext()` Guid-per-test + `SeedUserAsync` + assert resolver skip saat `User.Unit` di-set tapi `AssignmentUnit=null` |
| `AssignmentUnitInUserUnitsTests.cs` (PSU-03) | `RemoveUnitGuardTests.cs` | seed mapping+UserUnits InMemory → call `public static` validator → assert accept/reject |
| `CleanupNoClobberTests.cs` (PSU-04) | `UserUnitsWriteThroughTests.cs` | seed `AssignmentUnit=unit-sekunder-sah` → run cleanup → assert `AssignmentUnit` UNCHANGED |
| `UnitUnresolvedAuditTests.cs` (PSU-05) | `CapturingLogger.cs` + InMemory `AuditLogs.Count()` | read-path: `CapturingLogger` assert LogWarning + `AuditLogs` delta=0 ; gate: delta=1 |
| `ReactivateUnitValidationTests.cs` (PSU-07) | `RemoveUnitGuardTests.cs` | seed inactive mapping unit-dilepas → reactivate reject ; window ±5s utuh |
| (filter-axis smoke) | `ProtonCompletionServiceTests.cs` (`ProtonCompletionFixture`) | `[Trait("Category","Integration")]` SQL-real `HcPortalDB_Test_<guid>` single-active smoke |

**Analog InMemory factory (`UserUnitsWriteThroughTests.cs:22-25`):**
```csharp
private static ApplicationDbContext InMemoryContext() =>
    new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
```
**Analog CapturingLogger (`CapturingLogger.cs:6-14`):** `CapturingLogger<T>.Entries` → `Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning)`.
**Analog SQL-real fixture (`ProtonCompletionServiceTests.cs:25-61`):** `IAsyncLifetime` + `MigrateAsync()` + `EnsureDeletedAsync()` dispose + `[Trait("Category","Integration")]` (skip via `--filter "Category!=Integration"`).
**⚠️ Pitfall 5:** EF-InMemory TIDAK enforce filtered-unique index — single-active/multi-unit assertion WAJIB `[Trait Integration]` (deep test = Phase 404).

---

## No Analog Found

Tidak ada. Semua 7 cluster + test = pola yang SUDAH ada di codebase (sering di file yang sama). 401 = drop-fallback / perketat-skip / tambah-validasi / swap-axis / preserve-no-clobber pada pola existing. Planner gunakan `file:line` analog di atas; TIDAK perlu fallback ke pola RESEARCH.md generik.

**Catatan out-of-scope (JANGAN buat analog):** kolom `Unit` di `ProtonTrackAssignment` (migration, §8) ; true per-unit PTA-match ; re-architect window AF-4 (`DeactivatedByMappingEventId`) ; per-coachee unit-picker UI (CXU-03 Phase 402).

---

## Metadata

**Analog search scope:** `Controllers/` (CoachMapping, AssessmentAdmin, CDP, ProtonData, Worker), `Services/` (ProtonBypass, AuditLog), `Views/Admin/CoachCoacheeMapping.cshtml`, `HcPortal.Tests/` (UserUnitsWriteThrough, RemoveUnitGuard, CapturingLogger, ProtonCompletion)
**Files scanned (read line-by-line):** 11
**Verification:** semua `file:line` analog dibaca & dikonfirmasi vs RESEARCH.md (zero drift — line ranges match)
**Pattern extraction date:** 2026-06-18

---

## PATTERN MAPPING COMPLETE
