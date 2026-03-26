# Phase 261: Validasi Konsistensi Field Organisasi di CoachCoacheeMapping dan Directorate - Research

**Researched:** 2026-03-26
**Domain:** Data consistency validation - ASP.NET Core / EF Core
**Confidence:** HIGH

## Summary

Phase ini mencakup dua bagian: (1) one-time cleanup data CoachCoacheeMapping yang AssignmentSection/AssignmentUnit tidak konsisten dengan OrganizationUnit aktif, dan (2) runtime validation pada create/edit/import agar inkonsistensi tidak terjadi lagi.

Helper `GetSectionUnitsDictAsync()` sudah tersedia dan mengembalikan `Dictionary<string, List<string>>` dari Section->Units aktif. Helper ini sudah dipakai di 13+ lokasi di codebase. Validasi cukup memanggil helper ini lalu cek `dict.ContainsKey(section) && dict[section].Contains(unit)`.

**Primary recommendation:** Gunakan `GetSectionUnitsDictAsync()` untuk semua validasi. Cleanup diimplementasikan sebagai admin action (bukan migration) agar hasilnya bisa dilaporkan via TempData.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** One-time cleanup data existing + runtime validation pada create/edit/import — keduanya dikerjakan
- **D-02:** Hanya `CoachCoacheeMapping.AssignmentSection` dan `AssignmentUnit` yang divalidasi. `ApplicationUser.Directorate` di-skip.
- **D-03:** Scan semua CoachCoacheeMapping yang AssignmentSection atau AssignmentUnit tidak cocok dengan OrganizationUnit aktif
- **D-04:** Auto-fix dari coachee's current User record (Section/Unit yang sudah di-cascade Phase 260)
- **D-05:** Jika coachee's User record juga invalid, masukkan ke report — tidak auto-fix
- **D-06:** Report hasil cleanup: jumlah auto-fixed + daftar yang tidak bisa di-fix
- **D-07:** Saat create (CoachCoacheeMappingAssign): validasi AssignmentSection & AssignmentUnit exist di OrganizationUnit aktif
- **D-08:** Saat edit (CoachCoacheeMappingEdit): validasi sama seperti create
- **D-09:** Saat import (ImportCoachCoacheeMapping): validasi Section/Unit coachee exist di OrganizationUnit aktif sebelum assign ke mapping

### Claude's Discretion
- Mekanisme cleanup (migration script, admin action, atau startup task)
- Format report inkonsistensi (log, TempData, atau file)
- Exact error messages untuk runtime validation

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Architecture Patterns

### Validation Helper Pattern
Gunakan `GetSectionUnitsDictAsync()` yang sudah ada:

```csharp
// Source: Data/ApplicationDbContext.cs line 102
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
bool isValid = sectionUnitsDict.TryGetValue(section, out var units) && units.Contains(unit);
```

### Insertion Points

**CoachCoacheeMappingAssign (line ~4104):** Setelah check `IsNullOrWhiteSpace`, tambah validasi:
```csharp
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
if (!sectionUnitsDict.TryGetValue(req.AssignmentSection!.Trim(), out var validUnits)
    || !validUnits.Contains(req.AssignmentUnit!.Trim()))
    return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });
```

**CoachCoacheeMappingEdit (line ~4313):** Sebelum assignment ke mapping, validasi jika Section/Unit non-null:
```csharp
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
var sec = req.AssignmentSection?.Trim();
var unit = req.AssignmentUnit?.Trim();
if (!string.IsNullOrEmpty(sec) && !string.IsNullOrEmpty(unit))
{
    if (!sectionUnitsDict.TryGetValue(sec, out var validUnits) || !validUnits.Contains(unit))
        return Json(new { success = false, message = "Section/Unit tidak ditemukan di data organisasi aktif." });
}
```

**ImportCoachCoacheeMapping (line ~4038):** Sebelum set `AssignmentSection`/`AssignmentUnit` dari coachee user, validasi:
```csharp
// Load sectionUnitsDict sekali di awal method, di luar loop
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
// Di dalam loop, sebelum create mapping:
if (string.IsNullOrEmpty(coacheeUser.Section) || string.IsNullOrEmpty(coacheeUser.Unit)
    || !sectionUnitsDict.TryGetValue(coacheeUser.Section, out var vu) || !vu.Contains(coacheeUser.Unit))
{
    result.Status = "Error";
    result.Message = $"Section/Unit coachee ('{coacheeUser.Section}'/'{coacheeUser.Unit}') tidak valid di OrganizationUnit aktif";
    results.Add(result);
    continue;
}
```

### Cleanup sebagai Admin Action (Rekomendasi)

Implementasi sebagai POST action `CleanupCoachCoacheeMappingOrg` di AdminController:
1. Load semua CoachCoacheeMapping aktif
2. Load `GetSectionUnitsDictAsync()`
3. Untuk setiap mapping yang Section/Unit tidak valid:
   a. Load coachee user record
   b. Jika coachee user Section/Unit valid di dict -> auto-fix mapping
   c. Jika tidak -> tambah ke "unfixable" list
4. SaveChanges + audit log
5. Return report via TempData (JSON)

**Kenapa admin action, bukan migration:** User bisa melihat report langsung. Bisa dijalankan ulang. Tidak perlu migration file.

### Anti-Patterns to Avoid
- **Jangan validasi per-query ke DB:** Gunakan satu kali `GetSectionUnitsDictAsync()` per request, jangan query OrganizationUnit per-row
- **Jangan skip null AssignmentSection/Unit pada import:** Import selalu set dari coachee user — jika user data invalid, harus error bukan silent null

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Section/Unit lookup | Custom query per validasi | `GetSectionUnitsDictAsync()` | Sudah ada, sudah dipakai 13+ lokasi |
| Audit logging | Manual insert | `_auditLog.LogAsync()` | Pattern yang sudah established |
| Transaction handling | Manual try/catch | Ikuti pattern ImportCoachCoacheeMapping | Sudah ada contoh atomic transaction |

## Common Pitfalls

### Pitfall 1: Null AssignmentSection/Unit pada Mapping Lama
**What goes wrong:** Mapping lama mungkin punya `AssignmentSection = null` (sebelum field ini wajib)
**How to avoid:** Cleanup harus handle null — null dianggap "needs fix from user record"

### Pitfall 2: Case Sensitivity dan Whitespace
**What goes wrong:** "RFCC NHT" vs "RFCC NHT " (trailing space) gagal match
**How to avoid:** Trim semua value sebelum compare. `GetSectionUnitsDictAsync()` return nama langsung dari DB — pastikan mapping value juga di-trim.

### Pitfall 3: Import Reactivation Tidak Update Section/Unit
**What goes wrong:** Line 4021-4024 reactivate mapping lama tapi TIDAK update AssignmentSection/Unit
**How to avoid:** Saat reactivate, juga update Section/Unit dari coachee user (setelah validasi). Ini termasuk scope D-09.

## Code Examples

### Cleanup Action Pattern
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CleanupCoachCoacheeMappingOrg()
{
    var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
    var mappings = await _context.CoachCoacheeMappings.Where(m => m.IsActive).ToListAsync();
    var userDict = await _context.Users
        .Select(u => new { u.Id, u.Section, u.Unit })
        .ToDictionaryAsync(u => u.Id);

    int autoFixed = 0;
    var unfixable = new List<object>();

    foreach (var m in mappings)
    {
        var sec = m.AssignmentSection?.Trim();
        var unit = m.AssignmentUnit?.Trim();
        bool isValid = !string.IsNullOrEmpty(sec) && !string.IsNullOrEmpty(unit)
            && sectionUnitsDict.TryGetValue(sec, out var vu) && vu.Contains(unit);
        if (isValid) continue;

        // Try fix from coachee user
        if (userDict.TryGetValue(m.CoacheeId, out var user))
        {
            var uSec = user.Section?.Trim();
            var uUnit = user.Unit?.Trim();
            bool userValid = !string.IsNullOrEmpty(uSec) && !string.IsNullOrEmpty(uUnit)
                && sectionUnitsDict.TryGetValue(uSec, out var uvu) && uvu.Contains(uUnit);
            if (userValid)
            {
                m.AssignmentSection = uSec;
                m.AssignmentUnit = uUnit;
                autoFixed++;
                continue;
            }
        }
        unfixable.Add(new { m.Id, m.CoacheeId, m.AssignmentSection, m.AssignmentUnit });
    }

    await _context.SaveChangesAsync();
    // audit + TempData report
}
```

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Sources

### Primary (HIGH confidence)
- Codebase langsung: AdminController.cs (CoachCoacheeMappingAssign line 4092-4257, Edit line 4282-4400, Import line 3901-4090)
- Codebase langsung: ApplicationDbContext.cs (GetSectionUnitsDictAsync line 102-117)
- Codebase langsung: CoachCoacheeMapping.cs, OrganizationUnit.cs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - semua menggunakan pattern dan helper yang sudah ada di codebase
- Architecture: HIGH - insertion points sudah diidentifikasi dengan line numbers
- Pitfalls: HIGH - ditemukan dari analisis kode langsung (null handling, reactivation gap)

**Research date:** 2026-03-26
**Valid until:** 2026-04-26 (stable - internal codebase)
