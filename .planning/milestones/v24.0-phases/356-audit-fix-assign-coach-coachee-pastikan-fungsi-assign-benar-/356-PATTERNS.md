# Phase 356: Audit Fix Assign Coach×Coachee - Pattern Map

**Mapped:** 2026-06-09
**Files analyzed:** 4 (2 MODIFY existing + 2 CREATE new)
**Analogs found:** 4 / 4 (semua exact / in-file)
**Project:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server). Bahasa user-facing = Bahasa Indonesia (CLAUDE.md).

> BROWNFIELD audit-fix phase. Setiap fix punya analog persis di codebase yang sama — sebagian besar di FILE YANG SAMA. Planner/executor menulis task sebagai "ikuti pola X di file:line Y", BUKAN "buat solusi baru". Anti-pattern: meng-handroll deteksi unique-index, transaksi, atau notif — semua sudah ada precedent teruji.

---

## File Classification

| New/Modified File | Status | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|--------|------|-----------|----------------|---------------|
| `Controllers/CoachMappingController.cs` | MODIFY | controller (action) | CRUD + request-response | IN-FILE (`Deactivate`, `Assign`, `AutoCreateProgress`) | exact (same file) |
| `Helpers/CoacheeEligibilityCalculator.cs` | CREATE | utility (static pure) | transform | `Helpers/CertNumberHelper.cs` (static helper) + `CMPController.BuildActualCategories` | exact (role + pure) |
| `HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs` | CREATE | test (unit) | transform | `HcPortal.Tests/BuildActualCategoriesTests.cs` (static) + `OrganizationControllerTests.cs` (InMemory) | exact |
| `Views/Admin/CoachCoacheeMapping.cshtml` | MODIFY | view (Razor + Vanilla JS) | event-driven (DOM) | IN-FILE (`updateAssignmentDefaults()`, `filterCoacheesBySection()`) | exact (same file) |

Per-AF mapping (audit-finding ID, bukan REQ formal):

| AF | Sev | File:Anchor | Analog (file:line) |
|----|-----|-------------|--------------------|
| AF-1 | HIGH | `GetEligibleCoachees` L1277-1334 | `AutoCreateProgressForAssignment` L1342-1367 (unit resolution + `.Trim()` filter) + new helper |
| AF-2 | MED | `CoachCoacheeMapping.cshtml` JS `updateAssignmentDefaults()` L682-711 + markup L407-433 | `units` Set L686-694 (sudah ada) + `filterCoacheesBySection()` L670-680 |
| AF-3 | MED | `MarkMappingCompleted` L1075-1109 | `CoachCoacheeMappingDeactivate` L924-976 (transaksi + cascade + DeactivatedAt) |
| AF-5 | LOW | `ApproveReassignSuggestion` L1614-1638 | `CoachCoacheeMappingDeactivate` notif L949-966 (warn-only `SendAsync`) |
| AF-6 | LOW | `CoachCoacheeMappingAssign` catch L623-628 | `CertNumberHelper.IsDuplicateKeyException` L37-42 |
| AF-7 | INFO | `CoachCoacheeMappingAssign` loop L508-535 | self-refactor (pre-load dict), zero behavior change |
| AF-4 | DEFER | `CoachCoacheeMappingReactivate` ~L1008-1020 | komentar saja, NO fix |
| D-06 | — | `CoachCoacheeMapping` list action L52-55 + L114 | filter `if (!showAll) Where(IsActive)` + proyeksi `IsCompleted` sudah ada |

---

## Pattern Assignments

### `Helpers/CoacheeEligibilityCalculator.cs` (CREATE — utility, static pure) — AF-1

**Analog:** `Helpers/CertNumberHelper.cs` (static class, `public static` methods, namespace `HcPortal.Helpers`) + `CMPController.BuildActualCategories` (di-test sebagai pure function).

**Class shell pattern** (`CertNumberHelper.cs` L1-44):
```csharp
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Shared helper ... (Phase 227 CLEN-04). Extracted from ... to allow reuse ...
    /// </summary>
    public static class CertNumberHelper
    {
        public static bool IsDuplicateKeyException(DbUpdateException ex) { ... }
    }
}
```

**Helper signature (PROPOSAL, Claude's discretion D-13 — keputusan boolean tanpa DbContext):**
```csharp
namespace HcPortal.Helpers
{
    public static class CoacheeEligibilityCalculator
    {
        /// <summary>
        /// AF-1: coachee eligible bila punya TEPAT expectedCount progress untuk unit-nya
        /// dan SEMUANYA Approved. expectedCount <= 0 → tidak eligible (track Tahun 3 tanpa
        /// deliverable ditangani TERPISAH di call-site per D-02, bukan di helper ini).
        /// </summary>
        public static bool IsEligiblePerUnit(IReadOnlyList<string> myProgressStatuses, int expectedCount)
        {
            if (expectedCount <= 0) return false;
            if (myProgressStatuses.Count != expectedCount) return false;
            return myProgressStatuses.All(s => s == "Approved");
        }
    }
}
```

**KRITIS — string status persis:** literal `"Approved"` (case-sensitive) harus IDENTIK dengan yang dipakai di `GetEligibleCoachees` L1321 (`p.Status == "Approved"`) dan `AutoCreateProgressForAssignment` (status awal `"Pending"` L1384). Jangan ubah casing — InMemory test case-sensitive (lihat Pitfall di RESEARCH §371).

---

### `Controllers/CoachMappingController.cs` — AF-1 call-site (`GetEligibleCoachees` L1277-1334)

**Current code (yang DIGANTI — global count, bug):** L1316-1323:
```csharp
// Eligible = has exactly trackDeliverableIds.Count Approved progress records
var eligibleCoacheeIds = assignedCoacheeIds
    .Where(id =>
    {
        var mine = progressRecords.Where(p => p.CoacheeId == id).ToList();
        return mine.Count == trackDeliverableIds.Count && mine.All(p => p.Status == "Approved");
    })
    .ToList();
```
Bug: `trackDeliverableIds.Count` = TOTAL deliverable semua-unit track. Coachee multi-unit track tak pernah punya `mine.Count` == total → tak pernah eligible.

**Fix pattern — mirror unit-resolution dari `AutoCreateProgressForAssignment` L1342-1367 (SUMBER KEBENARAN):**
```csharp
// Resolve unit: AssignmentUnit from active mapping, fallback to User.Unit
var assignmentUnit = await _context.CoachCoacheeMappings
    .Where(m => m.CoacheeId == coacheeId && m.IsActive)
    .Select(m => m.AssignmentUnit)
    .FirstOrDefaultAsync();
var resolvedUnit = assignmentUnit;
if (string.IsNullOrWhiteSpace(resolvedUnit))
    resolvedUnit = await _context.Users.Where(u => u.Id == coacheeId).Select(u => u.Unit).FirstOrDefaultAsync();

// expected deliverable PER UNIT — filter PERSIS dengan .Trim() di dua sisi (L1363-1365):
var deliverableIds = await _context.ProtonDeliverableList
    .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
             && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())
    .Select(d => d.Id).ToListAsync();
```
Lalu untuk tiap coachee: `expectedCount = deliverableIds.Count` (per unit), panggil `CoacheeEligibilityCalculator.IsEligiblePerUnit(mine.Select(p => p.Status).ToList(), expectedCount)`.

**D-02 verbatim (JANGAN ubah):** cabang Tahun-3 / no-deliverable L1298-1307 (`if (!trackDeliverableIds.Any())` → semua assigned eligible) dipertahankan apa adanya. Helper TIDAK menangani Tahun 3 — call-site yang menangani.

**KRITIS Pitfall 1 (RESEARCH §364):** `.Trim()` WAJIB di dua sisi (`Unit.Trim() == resolvedUnit.Trim()`). Lupa `.Trim()` atau pakai `User.Unit` saja → count meleset → bug bergeser bukan hilang. Warning sign: test track id=4 Alkylation 3/3 tetap NOT eligible.

---

### `Controllers/CoachMappingController.cs` — AF-3 (`MarkMappingCompleted` L1075-1109)

**Analog (mirror VERBATIM):** `CoachCoacheeMappingDeactivate` L924-976 — pola transaksi + cascade + `DeactivatedAt`.

**Cascade + transaksi pattern (Deactivate L924-944):**
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    mapping.IsActive = false;
    mapping.EndDate = DateTime.UtcNow;

    // Cascade: deactivate all ProtonTrackAssignments for this coachee
    // FIX-01: stamp DeactivatedAt so reactivation can correlate assignments back to this event
    var deactivationTime = mapping.EndDate.Value;
    var activeAssignments = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
        .ToListAsync();
    foreach (var a in activeAssignments)
    {
        a.IsActive = false;
        a.DeactivatedAt = deactivationTime;   // D-04: JANGAN hapus progress (BUKAN RemoveRange)
    }
    int cascadeCount = activeAssignments.Count;

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    // ... audit log SETELAH commit ...
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "...transaction failed for mapping {Id}", id);
    // return ...
}
```

**Apply ke `MarkMappingCompleted` — current code L1099-1108 (yang dibungkus transaksi):**
```csharp
mapping.IsCompleted = true;
mapping.CompletedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();   // ← saat ini single SaveChanges (D-05: bungkus transaksi)
```
Tambah set `IsActive = false` + `EndDate = DateTime.UtcNow` (D-03, bebaskan unique-index) + cascade deactivate ProtonTrackAssignment (D-04, stamp `DeactivatedAt`, JANGAN hapus progress) + bungkus transaksi (D-05).

**KRITIS Pitfall 4 (RESEARCH §382) — kontrak return BERBEDA:** `MarkMappingCompleted` me-return `RedirectToAction("CoachCoacheeMapping")` + `TempData["Success"/"Error"]` (form POST), BUKAN `Json` seperti Deactivate (AJAX). Pertahankan kontrak existing — hanya pola transaksi yang di-copy, bukan return type. Validasi Tahun-3 (L1081-1098, `tahun3Complete`) tetap DI AWAL / SEBELUM transaksi.

**Kolom verified ADA (no migration, D-12):** `IsActive` (Model L24), `EndDate` (L34), `IsCompleted` (L47), `CompletedAt` (L50). `ProtonTrackAssignment.DeactivatedAt` dipakai di Deactivate L939.

---

### `Controllers/CoachMappingController.cs` — AF-3 D-06 (badge "Graduated" di list)

**Analog (IN-FILE):** list action `CoachCoacheeMapping` L52-55 filter + L114 proyeksi.

**Filter existing (L52-55) — sudah benar, JANGAN ubah:**
```csharp
var query = _context.CoachCoacheeMappings.AsQueryable();
if (!showAll)
    query = query.Where(m => m.IsActive);   // default list: IsActive only → graduated (IsActive=false) tak muncul
```
Setelah AF-3 set `IsActive=false` saat graduate, mapping graduated otomatis HILANG dari list default dan hanya muncul saat `showAll=true`. `IsCompleted` SUDAH di proyeksi (L114: `IsCompleted = r.Mapping.IsCompleted`) → view tinggal render badge "Graduated" saat `IsCompleted == true`. Tidak perlu query baru.

---

### `Controllers/CoachMappingController.cs` — AF-5 (`ApproveReassignSuggestion` L1614-1638)

**Analog (mirror VERBATIM):** notif warn-only `CoachCoacheeMappingDeactivate` L949-966.

**Pattern (Deactivate L949-966 — COACH-03):**
```csharp
// Notify ... about ...
try
{
    var coachUser = await _context.Users.FindAsync(mapping.CoachId);
    var coacheeUser = await _context.Users.FindAsync(mapping.CoacheeId);
    var coachName = coachUser?.FullName ?? coachUser?.UserName ?? mapping.CoachId;
    var coacheeName = coacheeUser?.FullName ?? coacheeUser?.UserName ?? mapping.CoacheeId;

    await _notificationService.SendAsync(mapping.CoachId, "COACH_MAPPING_DEACTIVATED",
        "Mapping Coaching Dinonaktifkan",
        $"Mapping coaching Anda dengan {coacheeName} telah dinonaktifkan",
        "/CDP/CoachingProton");
    // ... recipient kedua ...
}
catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
```

**SendAsync signature** [VERIFIED `Services/INotificationService.cs` L20]:
```csharp
Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null);
```

**Apply ke AF-5 — 3 recipient (microcopy DIKUNCI UI-SPEC §116-120), EVENT_TYPE `COACH_REASSIGNED`:**
| Recipient | userId | Judul | Pesan (body) |
|-----------|--------|-------|--------------|
| Coach lama (dilepas) | `oldCoachId` (L1621) | `Penugasan Coaching Dialihkan` | `Penugasan coaching Anda dengan {coacheeName} telah dialihkan ke coach lain.` |
| Coach baru (ditunjuk) | `newCoachId` (param) | `Coach Ditunjuk` | `Anda ditunjuk sebagai coach untuk {coacheeName}.` |
| Coachee (dipindah) | `mapping.CoacheeId` | `Coach Anda Berubah` | `Coach Anda telah diganti menjadi {coachName}.` |

Resolve `coacheeName`/`coachName` pakai pola `?.FullName ?? ?.UserName ?? id` (Deactivate L954-955). Sisipkan blok try/catch SETELAH `await _context.SaveChangesAsync()` (L1636), sebelum `return Json(...)`. `oldCoachId` sudah ditangkap L1621 — pertahankan. Audit log existing L1625-1634 dipertahankan.

---

### `Controllers/CoachMappingController.cs` — AF-6 (`CoachCoacheeMappingAssign` catch L623-628)

**Analog:** `CertNumberHelper.IsDuplicateKeyException` L37-42 (match index name + SQL error 2601/2627).

**Pattern (`CertNumberHelper.cs` L37-42):**
```csharp
public static bool IsDuplicateKeyException(DbUpdateException ex)
{
    return ex.InnerException?.Message.Contains("IX_AssessmentSessions_NomorSertifikat") == true
        || ex.InnerException?.Message.Contains("2601") == true
        || ex.InnerException?.Message.Contains("2627") == true;
}
```

**Current generic catch (L623-628 — yang ditambahi catch spesifik SEBELUMNYA):**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "CoachCoacheeMappingAssign failed (...)");
    await tx.RollbackAsync();
    return Json(new { success = false, message = "Gagal menyimpan assignment. Operasi dibatalkan." });
}
```

**Fix — tambah `catch (DbUpdateException)` SEBELUM catch generic (urutan WAJIB: spesifik dulu):**
```csharp
catch (DbUpdateException dbEx) when (
    dbEx.InnerException?.Message.Contains("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique") == true
    || dbEx.InnerException?.Message.Contains("2601") == true
    || dbEx.InnerException?.Message.Contains("2627") == true)
{
    _logger.LogWarning(dbEx, "Assign race: coachee already has active coach (unique-index violation)");
    await tx.RollbackAsync();
    return Json(new { success = false,
        message = "Coachee sudah memiliki coach aktif untuk unit ini. Nonaktifkan mapping lama terlebih dahulu." });
}
catch (Exception ex) { /* generic existing tetap di bawah */ }
```
**Catatan (discretion executor, RESEARCH OQ2):** boleh extend `CertNumberHelper` jadi generic (param nama index) ATAU inline `when`-filter. Index AF-6 = `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` [VERIFIED `ApplicationDbContext.cs` L328, `HasFilter("[IsActive] = 1")` L326]. **Security (RESEARCH §507):** JANGAN expose `ex.Message` mentah ke user — pesan ramah tetap, log detail server-only.

---

### `Controllers/CoachMappingController.cs` — AF-7 (`CoachCoacheeMappingAssign` loop L508-535)

**Self-refactor (zero behavior change):** loop progression-warning saat ini ~4 query/coachee.

**Current loop (L510-535) — 3 cabang yang HARUS direproduksi PERSIS:**
```csharp
foreach (var coacheeId in req.CoacheeIds)
{
    // D-11: skip jika sudah ada assignment untuk track ini
    var hasExistingAssignment = await _context.ProtonTrackAssignments
        .AnyAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == req.ProtonTrackId.Value);
    if (hasExistingAssignment) continue;                       // cabang 1: skip

    var prevAssignment = await _context.ProtonTrackAssignments
        .FirstOrDefaultAsync(a => a.CoacheeId == coacheeId && a.ProtonTrackId == prevTrack.Id);
    if (prevAssignment == null) { incompleteCoachees.Add(coacheeId); continue; }  // cabang 2: null → incomplete

    var prevProgressCount = await _context.ProtonDeliverableProgresses
        .CountAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id);
    var allApproved = prevProgressCount > 0 && !await _context.ProtonDeliverableProgresses
        .AnyAsync(p => p.ProtonTrackAssignmentId == prevAssignment.Id && p.Status != "Approved");
    if (!allApproved) incompleteCoachees.Add(coacheeId);       // cabang 3: not-all-approved → incomplete
}
```

**Refactor (RESEARCH Pitfall 3 §376):** pre-load 3 dict batch SEBELUM loop, lalu evaluasi 3 cabang SAMA in-memory:
1. set assignment-for-this-track per coachee (untuk skip),
2. prev-assignment per coachee (`ProtonTrackId == prevTrack.Id`),
3. status progress per prev-assignment-id (`prevProgressCount > 0 && all == "Approved"`).
**KRITIS:** output `incompleteCoachees` HARUS IDENTIK sebelum/sesudah. Cabang `prevProgressCount > 0` (count==0 → NOT allApproved → incomplete) jangan hilang. Warning sign: pesan "X coachee belum menyelesaikan {prevTrack.DisplayName}" muncul/hilang berbeda. D-10: kurangi query count saja, JANGAN ubah perilaku warning. Response warning L538-543 dipertahankan verbatim.

---

### `Controllers/CoachMappingController.cs` — AF-4 (`CoachCoacheeMappingReactivate` ~L1008-1020) — DEFER

**NO fix, NO migration.** Tambah KOMENTAR Bahasa Indonesia yang mendokumentasikan asumsi window ±5s (D-11). Reactivate logic (`duplicateActive` check L991-994, dst) dipertahankan verbatim. Catat di backlog via `/gsd-add-backlog`. Alasan defer: severity LOW-MED + 0-migration leg v24.

---

### `Views/Admin/CoachCoacheeMapping.cshtml` — AF-2 (UI guard 1-unit/batch)

**Analog (IN-FILE):** `updateAssignmentDefaults()` L682-711 (sudah punya `units` Set) + `filterCoacheesBySection()` L670-680 (jangan break `display`).

**Markup baris coachee existing (L412) — `data-unit` SUDAH ada (REUSE):**
```html
<div class="form-check coachee-item" data-section="@coachee.Section" data-unit="@coachee.Unit">
    <input class="form-check-input coachee-checkbox" type="checkbox"
           id="chk-@coachee.Id" value="@coachee.Id" onchange="updateAssignmentDefaults()" />
```

**Hook existing `updateAssignmentDefaults()` (L682-711) — `units` Set sudah dibangun:**
```javascript
function updateAssignmentDefaults() {
    var checked = document.querySelectorAll('.coachee-checkbox:checked');
    if (checked.length === 0) return;          // ← AF-2: ganti early-return jadi RESET (re-enable + sembunyikan hint)

    var sections = new Set();
    var units = new Set();
    checked.forEach(function(cb) {
        var item = cb.closest('.coachee-item');
        var unit = item.getAttribute('data-unit') || '';
        if (unit) units.add(unit);
        // ...
    });
    if (units.size === 1) { /* auto-fill AssignmentSection/AssignmentUnit existing L697-708 */ }
    // Multiple different units or no match — don't auto-fill (L710)
}
```

**Guard pattern (extend hook — disable checkbox unit-berbeda, RESEARCH Pattern 5 §290 / UI-SPEC §74-79):**
```javascript
function updateAssignmentDefaults() {
    var checked = document.querySelectorAll('.coachee-checkbox:checked');
    var hint = document.getElementById('coacheeUnitConstraintHint'); // markup BARU (form-text)

    if (checked.length === 0) {
        // RESET: re-enable semua, hilangkan text-muted, sembunyikan hint
        document.querySelectorAll('#coacheeChecklist .coachee-item').forEach(function (item) {
            item.querySelector('.coachee-checkbox').disabled = false;
            item.classList.remove('text-muted');
        });
        if (hint) hint.style.display = 'none';
        return;
    }

    var lockedUnit = checked[0].closest('.coachee-item').getAttribute('data-unit') || '';
    document.querySelectorAll('#coacheeChecklist .coachee-item').forEach(function (item) {
        var cb = item.querySelector('.coachee-checkbox');
        var sameUnit = (item.getAttribute('data-unit') || '') === lockedUnit;
        cb.disabled = !sameUnit && !cb.checked;             // jangan disable yang sudah dicentang
        item.classList.toggle('text-muted', !sameUnit && !cb.checked);
    });
    if (hint) hint.style.display = '';

    // === auto-fill existing L686-709 tetap jalan verbatim ===
}
```

**KRITIS:** guard HANYA toggle `disabled` + class `text-muted`. JANGAN sentuh `style.display` — itu milik `filterCoacheesBySection()` L675/677 (item ter-filter sembunyi tetap dihormati). Disabled appearance = native Bootstrap 5 `.form-check-input:disabled` (NO CSS baru, UI-SPEC §83-86).

**Hint markup BARU (di bawah `#coacheeChecklist` ~L432, gaya `form-text` existing, Bahasa Indonesia DIKUNCI UI-SPEC §88-93):**
```html
<div id="coacheeUnitConstraintHint" class="form-text text-muted" style="display:none;">
    Satu batch assign hanya untuk satu unit. Coachee dari unit lain dinonaktifkan — kosongkan pilihan untuk berganti unit.
</div>
```

**Backstop opsional (direkomendasikan UI-SPEC §81)** di `submitAssign()` L713: cek `new Set(units).size === 1` sebelum `fetch` (L735). Empty-state L428-431 sudah ada — JANGAN redesign (guard N/A, tak ada checkbox).

---

### `HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs` (CREATE — unit test) — AF-1

**Analog (static helper, pure):** `HcPortal.Tests/BuildActualCategoriesTests.cs` (panggil method static langsung, no DB).

**Pattern (`BuildActualCategoriesTests.cs` L1-37):**
```csharp
using System.Collections.Generic;
using HcPortal.Controllers;     // (Phase 356: ganti → using HcPortal.Helpers;)
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class BuildActualCategoriesTests
{
    [Fact]
    public void DistinctNonEmpty_CaseInsensitive_Ordered()
    {
        var records = new List<UnifiedTrainingRecord> { ... };
        var result = CMPController.BuildActualCategories(records);   // panggil static langsung
        Assert.Equal(new[] { "Legacy Free Text", "OJT" }, result);
    }
}
```

**4 [Fact] minimal (RESEARCH Validation §426, data track id=4):**
```csharp
namespace HcPortal.Tests;

public class CoacheeEligibilityCalculatorTests
{
    [Fact] // unit A: 3/3 Approved (expectedCount=3) → eligible
    public void FullApproved_Eligible() =>
        Assert.True(CoacheeEligibilityCalculator.IsEligiblePerUnit(
            new[] { "Approved", "Approved", "Approved" }, 3));

    [Fact] // unit B: 0 progress (expectedCount=1) → NOT eligible
    public void ZeroProgress_NotEligible() =>
        Assert.False(CoacheeEligibilityCalculator.IsEligiblePerUnit(new string[0], 1));

    [Fact] // sebagian (2/3 Approved, 1 Pending) → NOT eligible
    public void PartialApproved_NotEligible() =>
        Assert.False(CoacheeEligibilityCalculator.IsEligiblePerUnit(
            new[] { "Approved", "Approved", "Pending" }, 3));

    [Fact] // expectedCount==0 → NOT eligible (Tahun 3 ditangani di call-site, bukan helper)
    public void ExpectedCountZero_NotEligible() =>
        Assert.False(CoacheeEligibilityCalculator.IsEligiblePerUnit(new string[0], 0));
}
```

**Opsional (AF-3 cascade / AF-7 parity) — InMemory DbContext:** pola `OrganizationControllerTests.MakeController()` L19-33:
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
var ctx = new ApplicationDbContext(options);
var auditLog = new AuditLogService(ctx);
#pragma warning disable CS8625 // null-substitute deps yang tak di-deref di path test
var ctrl = new CoachMappingController(ctx, null!, auditLog, ...null!...);
#pragma warning restore CS8625
```
**Pitfall 2 (RESEARCH §371):** InMemory case-SENSITIVE — gunakan casing unit IDENTIK dengan seed; jangan andalkan case-insensitivity. Untuk AF-1, **prefer static helper test** (tak bergantung provider).

---

## Shared Patterns

### Transaksi + cascade (AF-3)
**Source:** `CoachCoacheeMappingDeactivate` L924-944 (in-file).
**Apply to:** `MarkMappingCompleted` (AF-3).
`using var transaction = await _context.Database.BeginTransactionAsync();` → mutate → `SaveChangesAsync()` → `CommitAsync()` → audit log POST-commit; `catch → RollbackAsync()`. JANGAN `RemoveRange` progress (D-04 lestari histori). Kontrak return per-action (Redirect+TempData untuk MarkCompleted, Json untuk Deactivate).

### Notifikasi warn-only (AF-5)
**Source:** `CoachCoacheeMappingDeactivate` L949-966 (COACH-03) + `Assign` L637-651 (COACH-01).
**Apply to:** `ApproveReassignSuggestion` (AF-5).
`try { await _notificationService.SendAsync(userId, EVENT_TYPE, title, body, "/CDP/CoachingProton"); ... } catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }` — tidak throw. Resolve nama `?.FullName ?? ?.UserName ?? id`. EVENT_TYPE UPPER_SNAKE (`COACH_REASSIGNED`).

### Deteksi duplicate-key (AF-6)
**Source:** `Helpers/CertNumberHelper.IsDuplicateKeyException` L37-42.
**Apply to:** `CoachCoacheeMappingAssign` catch (AF-6).
Match `InnerException.Message.Contains(indexName | "2601" | "2627")`. Index AF-6 = `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (ApplicationDbContext L328). Catch spesifik SEBELUM catch generic. Pesan ramah (no `ex.Message` leak).

### Unit resolution per-coachee (AF-1)
**Source:** `AutoCreateProgressForAssignment` L1342-1367 (in-file, SUMBER KEBENARAN).
**Apply to:** `GetEligibleCoachees` expectedCount per-unit (AF-1).
`AssignmentUnit` (active mapping) → fallback `User.Unit`; filter `ProtonKompetensi.Unit.Trim() == resolvedUnit.Trim()`. WAJIB `.Trim()` dua sisi.

### Static helper + xUnit (AF-1 verifikasi)
**Source:** `CertNumberHelper.cs` (class shell) + `BuildActualCategoriesTests.cs` (test).
**Apply to:** `CoacheeEligibilityCalculator.cs` + `CoacheeEligibilityCalculatorTests.cs`.
`public static class` di `namespace HcPortal.Helpers`; test panggil method langsung tanpa DB.

### Role guard + anti-forgery (preserve — semua action)
**Source:** existing attributes (`[Authorize(Roles="Admin, HC")]` Assign/Mark/Deactivate; `[Authorize(Roles="Admin")]` ApproveReassign L1612; `[ValidateAntiForgeryToken]` semua POST).
**Apply to:** SEMUA modifikasi. JANGAN longgarkan/ubah attribute (RESEARCH Security V4/V5). DTO `CoachAssignRequest` L1674 dipertahankan (anti mass-assignment).

---

## No Analog Found

Tidak ada. Semua 4 file punya analog exact (in-file atau same-project). Phase ini brownfield audit-fix — controller berkualitas TINGGI sudah punya precedent untuk SETIAP fix.

---

## Planner Flags (dari RESEARCH — keputusan kecil non-blocker)

1. **Data-fix mapping graduated existing (AF-3, RESEARCH OQ1/A2):** jalankan `SELECT Id, CoacheeId, IsActive, IsCompleted FROM CoachCoacheeMappings WHERE IsCompleted = 1` di DB lokal saat planning. Bila ada record `IsActive=1 & IsCompleted=1`, putuskan: task data-fix one-off (local-only SQL UPDATE, BUKAN migration) ATAU dokumentasikan "hanya berlaku graduate baru". Bukan blocker.
2. **Seed track id=4 (D-15, RESEARCH Pitfall 5):** mungkin SUDAH ada di DB lokal (audit data-verified). Verifikasi dulu sebelum seed tambahan. WAJIB SEED_WORKFLOW: snapshot `BACKUP DATABASE` → SEED_JOURNAL `active` → restore `WITH REPLACE` setelah test → tandai `cleaned`. Klasifikasi `temporary + local-only`.
3. **AD auth lokal (RESEARCH A5):** jalankan UAT lokal dengan `Authentication__UseActiveDirectory=false dotnet run` (appsettings handoff AD=true). admin@pertamina.com / 123456 (DB HcPortalDB_Dev SQLEXPRESS).
4. **Migration = FALSE (D-12, verified):** semua kolom AF-3 ADA di `Models/CoachCoacheeMapping.cs` (IsActive L24, EndDate L34, AssignmentUnit L44, IsCompleted L47, CompletedAt L50). Phase 0-migration agar tak nambah beban handoff bundle v19-v24.

---

## Metadata

**Analog search scope:** `Controllers/CoachMappingController.cs` (1694 baris, target utama), `Helpers/CertNumberHelper.cs`, `HcPortal.Tests/{BuildActualCategoriesTests,OrganizationControllerTests}.cs`, `Views/Admin/CoachCoacheeMapping.cshtml`, `Services/INotificationService.cs`, `Models/CoachCoacheeMapping.cs`, `Data/ApplicationDbContext.cs`.
**Files scanned:** 7 (semua read langsung sesi ini, file:line verified).
**Pattern extraction date:** 2026-06-09
**Phase:** 356-audit-fix-assign-coach-coachee
