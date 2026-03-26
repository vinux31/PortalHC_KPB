# Phase 260: Auto-cascade perubahan nama OrganizationUnit - Research

**Researched:** 2026-03-26
**Domain:** Entity Framework Core bulk update, denormalized data cascade
**Confidence:** HIGH

## Summary

Phase ini menambahkan cascade logic ke `EditOrganizationUnit` agar rename/reparent OrganizationUnit otomatis memperbarui semua field denormalized di `ApplicationUser` dan `CoachCoacheeMapping`. Juga menambahkan blokir deactivate jika masih ada user aktif, dan mengubah hardcoded Bagian names di `DownloadImportTemplate` menjadi dinamis.

Secara teknis straightforward: semua operasi bisa dilakukan dengan LINQ bulk update dalam satu `SaveChangesAsync()` call (single transaction). Tidak perlu library tambahan.

**Primary recommendation:** Tambahkan cascade logic langsung di `EditOrganizationUnit` action sebelum `SaveChangesAsync()`, gunakan EF Core change tracking untuk update semua affected records dalam satu transaksi.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Cascade langsung saat rename, satu transaksi, tanpa background job
- D-02: Cascade ke ApplicationUser.Section dan ApplicationUser.Unit
- D-03: Cascade ke CoachCoacheeMapping.AssignmentSection dan AssignmentUnit
- D-04: Directorate tetap free-text, tidak di-cascade
- D-05: Hardcoded Bagian di DownloadImportTemplate diganti query dinamis dari GetAllSectionsAsync()
- D-06: Flash message via TempData: "Nama diubah. X user dan Y mapping terupdate."
- D-07: Blokir deactivate jika masih ada user aktif di unit tersebut
- D-08: Tidak perlu validasi runtime saat login
- D-09: Reparent Unit auto-update Section semua user di Unit tersebut

### Claude's Discretion
- Pendekatan teknis cascade query (raw SQL vs LINQ bulk update)
- Error handling jika cascade gagal (rollback transaksi)
- Urutan operasi dalam transaksi

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Architecture Patterns

### Cascade Logic di EditOrganizationUnit

Ada 3 skenario cascade yang harus ditangani:

**Skenario 1: Rename Bagian (Level 0)**
- Old name "RFCC" -> new name "RFCC Unit"
- Update `ApplicationUser.Section` WHERE Section == oldName
- Update `CoachCoacheeMapping.AssignmentSection` WHERE AssignmentSection == oldName

**Skenario 2: Rename Unit (Level 1+)**
- Old name "RFCC LPG Treating" -> new name "LPG Treating Unit"
- Update `ApplicationUser.Unit` WHERE Unit == oldName
- Update `CoachCoacheeMapping.AssignmentUnit` WHERE AssignmentUnit == oldName

**Skenario 3: Reparent Unit (pindah parent)**
- Unit pindah dari Bagian A ke Bagian B
- Update `ApplicationUser.Section` WHERE Unit == unit.Name to new parent's name
- Update `CoachCoacheeMapping.AssignmentSection` WHERE AssignmentUnit == unit.Name to new parent's name

### Recommended Implementation Approach

Gunakan EF Core LINQ (bukan raw SQL) karena:
1. Sudah pattern yang dipakai di seluruh project
2. Change tracking memastikan semua update dalam satu `SaveChangesAsync()` = satu transaksi
3. Jumlah record kecil (ratusan user, bukan jutaan) — performa bukan masalah

```csharp
// Pseudocode - di dalam EditOrganizationUnit, sebelum SaveChangesAsync
string oldName = unit.Name; // capture sebelum diubah
int? oldParentId = unit.ParentId;

// ... existing validation and reparent logic ...

// Cascade rename
if (oldName != name.Trim())
{
    if (unit.Level == 0) // Bagian
    {
        var affectedUsers = await _context.Users
            .Where(u => u.Section == oldName).ToListAsync();
        foreach (var u in affectedUsers) u.Section = name.Trim();

        var affectedMappings = await _context.CoachCoacheeMappings
            .Where(m => m.AssignmentSection == oldName).ToListAsync();
        foreach (var m in affectedMappings) m.AssignmentSection = name.Trim();
    }
    else // Unit (Level 1+)
    {
        var affectedUsers = await _context.Users
            .Where(u => u.Unit == oldName).ToListAsync();
        foreach (var u in affectedUsers) u.Unit = name.Trim();

        var affectedMappings = await _context.CoachCoacheeMappings
            .Where(m => m.AssignmentUnit == oldName).ToListAsync();
        foreach (var m in affectedMappings) m.AssignmentUnit = name.Trim();
    }
}

// Cascade reparent - update Section for all users in the moved unit
if (oldParentId != parentId && unit.Level == 1)
{
    var newParent = await _context.OrganizationUnits.FindAsync(parentId.Value);
    var affectedUsers = await _context.Users
        .Where(u => u.Unit == unit.Name).ToListAsync();
    foreach (var u in affectedUsers) u.Section = newParent.Name;

    var affectedMappings = await _context.CoachCoacheeMappings
        .Where(m => m.AssignmentUnit == unit.Name).ToListAsync();
    foreach (var m in affectedMappings) m.AssignmentSection = newParent.Name;
}
```

### ToggleOrganizationUnitActive - Blokir Deactivate

Tambahkan cek user aktif sebelum allow deactivate:

```csharp
// Di ToggleOrganizationUnitActive, setelah cek children
if (unit.IsActive)
{
    // Cek user aktif di unit ini (atau di sub-unitnya)
    bool hasActiveUsers;
    if (unit.Level == 0)
        hasActiveUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name);
    else
        hasActiveUsers = await _context.Users.AnyAsync(u => u.Unit == unit.Name);

    if (hasActiveUsers)
    {
        TempData["Error"] = "Masih ada user aktif di unit ini. Pindahkan semua user terlebih dahulu.";
        return RedirectToAction("ManageOrganization");
    }
}
```

### DownloadImportTemplate - Dinamis

Ubah dari:
```csharp
ws.Cell(3, 1).Value = "Kolom Bagian: RFCC / DHT / HMU / NGP / GAST";
```
Menjadi:
```csharp
var sections = await _context.GetAllSectionsAsync();
ws.Cell(3, 1).Value = $"Kolom Bagian: {string.Join(" / ", sections)}";
```

Method harus diubah jadi `async Task<IActionResult>`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Transaction management | Manual transaction scope | EF Core SaveChangesAsync() | Sudah wraps semua changes dalam satu transaction |
| Bulk string update | Raw SQL UPDATE | LINQ + change tracking | Consistent dengan pattern project, jumlah record kecil |

## Common Pitfalls

### Pitfall 1: Capture old name SETELAH diubah
**What goes wrong:** `oldName` di-capture setelah `unit.Name = name.Trim()` sehingga cascade WHERE clause tidak match.
**How to avoid:** Capture `oldName = unit.Name` di awal method, sebelum apapun diubah.

### Pitfall 2: Reparent + Rename bersamaan
**What goes wrong:** Admin bisa rename DAN reparent dalam satu submit. Kedua cascade harus berjalan.
**How to avoid:** Handle rename dan reparent sebagai dua operasi terpisah yang keduanya dieksekusi sebelum SaveChangesAsync.

### Pitfall 3: DownloadImportTemplate saat ini synchronous
**What goes wrong:** `GetAllSectionsAsync()` adalah async, tapi `DownloadImportTemplate` saat ini return `IActionResult` (sync).
**How to avoid:** Ubah signature menjadi `async Task<IActionResult>`.

### Pitfall 4: Reparent level > 1
**What goes wrong:** Jika unit Level 2+ direparent, perlu cascade Section yang benar.
**How to avoid:** Untuk reparent, cari root ancestor (Level 0) dari new parent untuk mendapatkan nama Bagian yang benar.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| D-01/02/03 | Rename Bagian cascades to User.Section + Mapping.AssignmentSection | manual | N/A | N/A |
| D-01/02/03 | Rename Unit cascades to User.Unit + Mapping.AssignmentUnit | manual | N/A | N/A |
| D-09 | Reparent Unit cascades Section to all users | manual | N/A | N/A |
| D-05 | DownloadImportTemplate shows dynamic Bagian names | manual | N/A | N/A |
| D-06 | Flash message shows count of updated users/mappings | manual | N/A | N/A |
| D-07 | Deactivate blocked if active users exist | manual | N/A | N/A |

### Wave 0 Gaps
None -- no automated test infrastructure in project, all testing is manual browser verification.

## Sources

### Primary (HIGH confidence)
- Direct code reading: AdminController.cs (EditOrganizationUnit lines 7789-7845, ToggleOrganizationUnitActive lines 7871-7894, DownloadImportTemplate lines 5417-5470)
- Direct code reading: ApplicationDbContext.cs (GetAllSectionsAsync line 81)
- Direct code reading: OrganizationUnit.cs, ApplicationUser.cs, CoachCoacheeMapping.cs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries needed, all EF Core patterns already in use
- Architecture: HIGH - straightforward LINQ bulk update, all code inspected
- Pitfalls: HIGH - identified from direct code analysis

**Research date:** 2026-03-26
**Valid until:** 2026-04-26 (stable domain, no external dependencies)
