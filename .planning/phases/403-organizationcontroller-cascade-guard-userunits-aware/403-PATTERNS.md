# Phase 403: OrganizationController Cascade/Guard UserUnits-Aware - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 4 (semua MODIFY; tak ada file baru)
**Analogs found:** 4 / 4 (semua exact / in-file mirror — analog ada PERSIS di baris sebelahnya)

> Catatan kunci: fase ini ~90% reuse pola scalar yang sudah teruji **di file yang sama**. Setiap penambahan `UserUnits` adalah mirror struktural dari operasi scalar `Users.Unit`/`Users.Section` yang sudah ada. Risiko bukan "cara menulis" tapi **parity preview==actual** + **transaksi InMemory** (lihat Shared Patterns + Pitfall).
>
> Line numbers di bawah sudah **diverifikasi langsung** dari file aktual (2026-06-18). RESEARCH.md menyebut beberapa nomor yang melenceng ~1-3 baris; gunakan nomor di tabel ini.

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Controllers/OrganizationController.cs` | controller (MVC) | CRUD + transform (cascade/guard) | **dirinya sendiri** — operasi scalar `Users.Unit`/`Section` di baris sebelahnya + `WorkerController.cs:665` (tx idiom) | exact (in-file mirror) |
| `Views/Admin/ManageOrganization.cshtml` | view (Razor) | request-response (read-only display) | `#cascadeConfirmModal` `<ul>` L221-226 (baris `<li>` existing) | exact (in-file mirror) |
| `wwwroot/js/orgTree.js` | client (vanilla JS, AJAX) | request-response (read-only display + sum) | `showCascadeConfirm()` L361-364 + total-sum L426-427 (in-file) | exact (in-file mirror) |
| `HcPortal.Tests/OrganizationControllerTests.cs` | test (xUnit) | CRUD assertion | `MakeController()` L19-33 + preview-parity test L94-174 (in-file) | exact (seam siap pakai) |

---

## Pattern Assignments

### `Controllers/OrganizationController.cs` (controller, CRUD + transform)

**Analog:** dirinya sendiri (operasi scalar) + `WorkerController.cs` (tx idiom). 4 sub-perubahan:

#### Sub-1: Rename cascade ke `UserUnits.Unit` (ORG-01, D-04)

**Analog excerpt — scalar rename Level≥1** (`OrganizationController.cs:218-220`, di dalam `else` dari `if (oldName != name.Trim())`):
```csharp
var affectedUsers = await _context.Users.Where(u => u.Unit == oldName).ToListAsync();
foreach (var u in affectedUsers) u.Unit = name.Trim();
cascadedUsers += affectedUsers.Count;
```

**Copy/extend:** Tepat SETELAH blok scalar (sekitar L230, masih di dalam `else` Level≥1), tambah mirror junction:
```csharp
// NEW 403 (ORG-01, D-04): rename SEMUA baris UserUnits (incl sekunder, incl IsActive=false).
// IsPrimary TIDAK disentuh → baris primary ikut rename = mirror Users.Unit (L219) tetap konsisten (Invariant #3).
var affectedUnitRows = await _context.UserUnits.Where(uu => uu.Unit == oldName).ToListAsync();
foreach (var uu in affectedUnitRows) uu.Unit = name.Trim();
int cascadedUserUnits = affectedUnitRows.Count;   // dipakai pesan + parity ke preview (Sub-4)
```
**Aturan filter (KRITIKAL — D-03a, A1):** rename **TIDAK** memfilter `IsActive` (mirror perilaku scalar L218 yang juga tak filter). Preview di Sub-4 WAJIB pakai filter identik.

#### Sub-2: Reparent split-detection + hard-block (ORG-02, D-01/D-01a)

**Analog excerpt — root-ancestor walk yang menghitung `newSectionName`** (`OrganizationController.cs:235-247`, sudah ada inline):
```csharp
if (oldParentId != parentId && unit.Level >= 1)
{
    string newSectionName = "";
    if (parentId.HasValue)
    {
        var ancestor = await _context.OrganizationUnits.FindAsync(parentId.Value);
        while (ancestor != null && ancestor.Level > 0 && ancestor.ParentId.HasValue)
            ancestor = await _context.OrganizationUnits.FindAsync(ancestor.ParentId.Value);
        if (ancestor != null) newSectionName = ancestor.Name;
    }
    // ... L249-264: cascade Section existing (DIPERTAHANKAN) ...
}
```

**Insert split-detect SEBELUM mutasi Section existing (L249), reuse `newSectionName`:**
```csharp
// NEW 403 (ORG-02, D-01a): blok HANYA bila ada pekerja yang akan terpecah >1 Bagian.
// Berbasis UserUnits AKTIF (D-01a). Pakai _context.UserUnits correlated, BUKAN nav-prop u.UserUnits (lesson 400-01).
var memberIds = await _context.UserUnits
    .Where(uu => uu.Unit == oldName && uu.IsActive).Select(uu => uu.UserId).Distinct().ToListAsync();
var dict = await _context.GetSectionUnitsDictAsync();              // Bagian → List<unitName> (ApplicationDbContext.cs:121)
var unitToSection = dict.SelectMany(kv => kv.Value.Select(u => (Unit: u, Section: kv.Key)))
                        .ToDictionary(x => x.Unit, x => x.Section); // balik jadi unit → Bagian
var otherRows = await _context.UserUnits
    .Where(uu => memberIds.Contains(uu.UserId) && uu.IsActive && uu.Unit != oldName).ToListAsync();
var splitUserIds = otherRows
    .Where(uu => unitToSection.TryGetValue(uu.Unit, out var sec) && sec != newSectionName)
    .Select(uu => uu.UserId).Distinct().ToList();
if (splitUserIds.Any())
{
    var names = await _context.Users.Where(u => splitUserIds.Contains(u.Id))
        .Select(u => (u.NIP ?? "") + " - " + (u.FullName ?? u.UserName)).ToListAsync();
    var blokMsg = "Tidak dapat memindahkan unit ke Bagian lain: " + names.Count +
        " pekerja akan terpecah ke >1 Bagian (" + string.Join("; ", names) +
        "). Selesaikan keanggotaan lintas-Bagian pekerja tersebut terlebih dahulu.";
    // return early — di dalam tx (Sub-3) → dispose tanpa Commit = rollback otomatis (belum ada mutasi ter-commit)
    if (IsAjaxRequest()) return Json(new { success = false, message = blokMsg });
    TempData["Error"] = blokMsg; return RedirectToAction("ManageOrganization", new { editId = id });
}
```
**Filter:** split-detect pakai `&& uu.IsActive` (D-01a) — kebalikan dari rename. **Reparent TIDAK ubah baris `UserUnits`** (D-01b) — hanya Section mirror (existing).

#### Sub-3: Transaksi wrap (ORG-01, D-04)

**Analog excerpt** (`WorkerController.cs:665-688`, idiom 399 — JANGAN reuse helper-nya, hanya tiru idiom):
```csharp
using var uuTx = await _context.Database.BeginTransactionAsync();
// ... mutasi write-through + UpdateAsync ...
await _context.SaveChangesAsync();
await uuTx.CommitAsync();
```

**Copy:** Bungkus seluruh cascade `EditOrganizationUnit` dari awal mutasi (sekitar L180 / sebelum rename) sampai `SaveChangesAsync()` (L268). Pakai `using var tx = await _context.Database.BeginTransactionAsync(); ... await _context.SaveChangesAsync(); await tx.CommitAsync();`. Return-early split-block (Sub-2) keluar tanpa `Commit` → rollback otomatis. **PERINGATAN test:** InMemory tolak transaksi — lihat Shared Patterns "Transaksi + InMemory".

#### Sub-4: PreviewEditCascade `affectedUserUnitsCount` (ORG-02, D-03/D-03a)

**Analog excerpt — count block scalar** (`OrganizationController.cs:298-324`):
```csharp
if (nameChanged) {
    if (unit.Level == 0) { affectedUsers += await _context.Users.CountAsync(u => u.Section == oldName); /* ... */ }
    else { affectedUsers += await _context.Users.CountAsync(u => u.Unit == oldName); /* ... */ }
}
```
**Return shape** (`OrganizationController.cs:326-333`):
```csharp
return Json(new {
    nameChanged, parentChanged,
    affectedUsersCount = affectedUsers, affectedMappingsCount = affectedMappings,
    affectedKompetensiCount = affectedKompetensi, affectedGuidanceCount = affectedGuidance
});
```

**Copy/extend:** Tambah hitung + field baru. Filter HARUS identik dengan Sub-1 (no `IsActive`):
```csharp
int affectedUserUnits = 0;
if (nameChanged && unit.Level >= 1)                                  // Level 0 → UserUnits tak tersentuh (D-03a)
    affectedUserUnits = await _context.UserUnits.CountAsync(uu => uu.Unit == oldName);  // ALL rows = match Sub-1
// reparent tanpa rename → 0 (D-01b)
return Json(new {
    nameChanged, parentChanged,
    affectedUsersCount = affectedUsers, affectedMappingsCount = affectedMappings,
    affectedKompetensiCount = affectedKompetensi, affectedGuidanceCount = affectedGuidance,
    affectedUserUnitsCount = affectedUserUnits        // NEW (D-03)
});
```

#### Sub-5: Delete guard scan `UserUnits` (ORG-01, D-02)

**Analog excerpt** (`OrganizationController.cs:447-454`):
```csharp
bool hasUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name || u.Unit == unit.Name);
if (hasUsers) {
    if (IsAjaxRequest()) return Json(new { success = false, message = "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu." });
    TempData["Error"] = "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu.";
    return RedirectToAction("ManageOrganization");
}
```
**Copy/extend:** Tambah OR-clause + pesan spesifik sekunder (D-02a):
```csharp
bool hasUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name || u.Unit == unit.Name);
bool hasUnitMembership = await _context.UserUnits.AnyAsync(uu => uu.Unit == unit.Name && uu.IsActive);  // NEW (D-02b)
if (hasUsers || hasUnitMembership) {
    var why = (!hasUsers && hasUnitMembership)
        ? "Unit ini masih menjadi keanggotaan (sekunder) sejumlah pekerja yang tidak terlihat di unit utama mereka. Lepaskan keanggotaan unit ini dari pekerja terlebih dahulu."
        : "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu.";
    if (IsAjaxRequest()) return Json(new { success = false, message = why });
    TempData["Error"] = why; return RedirectToAction("ManageOrganization");
}
```
**Edge Level 0:** nama Bagian (Level 0) tak pernah match `UserUnits.Unit` → scan no-op aman (A2). Tak perlu cabang Level.

#### Sub-6: Deactivate guard scan `UserUnits` (ORG-01, D-02)

**Analog excerpt** (`OrganizationController.cs:385-399`, deactivate-branch `if (unit.IsActive)`):
```csharp
bool hasActiveUsers;
if (unit.Level == 0) hasActiveUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name);
else                 hasActiveUsers = await _context.Users.AnyAsync(u => u.Unit == unit.Name);
if (hasActiveUsers) {
    if (IsAjaxRequest()) return Json(new { success = false, message = "Tidak dapat menonaktifkan unit. Masih ada user aktif ... Pindahkan semua user terlebih dahulu." });
    // ... TempData ...
}
```
**Copy/extend:** Tambah scan `UserUnits` aktif (cabang Level≥1, atau OR unconditional). Pesan spesifik sekunder seperti Sub-5.

**Authz/CSRF (WAJIB pertahankan, jangan hapus):** keempat action sudah punya `[Authorize(Roles="Admin, HC")]` + `[HttpPost]` + `[ValidateAntiForgeryToken]` (Edit L127-128, Preview L281-282, Toggle L362-363, Delete L414-415).

---

### `Views/Admin/ManageOrganization.cshtml` (view, read-only display)

**Analog:** `<li>` baris existing di `#cascadeConfirmModal` (`ManageOrganization.cshtml:221-226`):
```html
<ul class="mb-2">
    <li><strong id="cascadeUsers">0</strong> user</li>
    <li><strong id="cascadeMappings">0</strong> mapping coach-coachee</li>
    <li><strong id="cascadeKompetensi">0</strong> kompetensi PROTON</li>
    <li><strong id="cascadeGuidance">0</strong> file panduan</li>
</ul>
```
**Copy/extend:** Tambah satu `<li>` (line terpisah, D-03) di dalam `<ul>` (mis. setelah L222):
```html
<li><strong id="cascadeUserUnits">0</strong> baris keanggotaan unit</li>
```
Ikut idiom `id="cascade*"` agar dipopulasi oleh `orgTree.js`.

---

### `wwwroot/js/orgTree.js` (client, read-only display + sum)

> **Pitfall 4 (kritis):** file ini DI LUAR 2 file yang sempat disebut CONTEXT, tapi WAJIB disentuh — tanpa edit ini, baris UserUnits tak terisi & tak ikut `total` (modal mungkin tak muncul saat satu-satunya dampak = UserUnits). 400/401 tidak menyentuh `orgTree.js` → aman untuk isolasi Wave-1 (O1 = YA).

**Analog excerpt — populate di `showCascadeConfirm()`** (`orgTree.js:361-364`):
```javascript
document.getElementById('cascadeUsers').textContent = pv.affectedUsersCount || 0;
document.getElementById('cascadeMappings').textContent = pv.affectedMappingsCount || 0;
document.getElementById('cascadeKompetensi').textContent = pv.affectedKompetensiCount || 0;
document.getElementById('cascadeGuidance').textContent = pv.affectedGuidanceCount || 0;
```
**Copy:** tambah 1 baris:
```javascript
document.getElementById('cascadeUserUnits').textContent = pv.affectedUserUnitsCount || 0;
```

**Analog excerpt — total sum di `submitUnitModal()`** (`orgTree.js:426-427`):
```javascript
const total = (pv.affectedUsersCount || 0) + (pv.affectedMappingsCount || 0)
            + (pv.affectedKompetensiCount || 0) + (pv.affectedGuidanceCount || 0);
```
**Copy/extend:** tambah term ke-5 agar modal muncul saat dampak UserUnits-only:
```javascript
const total = (pv.affectedUsersCount || 0) + (pv.affectedMappingsCount || 0)
            + (pv.affectedKompetensiCount || 0) + (pv.affectedGuidanceCount || 0)
            + (pv.affectedUserUnitsCount || 0);
```

---

### `HcPortal.Tests/OrganizationControllerTests.cs` (test, xUnit)

**Analog — test seam siap pakai** (`OrganizationControllerTests.cs:19-49`): `MakeController()` (InMemory `Guid` per-test + `null!` substitute untuk `_userManager`/`_env` + `X-Requested-With` header → JSON) + extractor `GetSuccess`/`GetInt`/`GetBool`.

**Analog — pola preview-parity** (`OrganizationControllerTests.cs:94-174`, `PreviewEditCascade_RenameLevel1_CountMatchesActual`): seed OrganizationUnits + denormalized fields → ACT1 `PreviewEditCascade` → ACT2 `EditOrganizationUnit` pada **ctx yang sama** → assert `preview == actual`.

**Copy/extend:** Tambah ~6 test (Wave-0 gap) meniru struktur di atas:
1. `EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows` (incl baris sekunder + `IsActive=false` discriminating) — assert semua baris ter-rename + mirror `Users.Unit` baris primary konsisten.
2. `DeleteOrganizationUnit_SecondaryMembershipActive_Rejected` — seed `UserUnits` sekunder aktif tanpa scalar → assert `success==false`.
3. `ToggleOrganizationUnitActive_SecondaryMembershipActive_Rejected` — idem deactivate-branch.
4. `EditOrganizationUnit_ReparentSplitsWorker_Blocked` — pekerja punya unit-lain di Bagian beda → assert blok + pesan sebut NIP/nama.
5. `EditOrganizationUnit_ReparentSingleUnitWorker_Allowed` — no split → assert Section ter-update (regresi existing dipertahankan).
6. `PreviewEditCascade_RenameLevel1_UserUnitsCountMatchesActual` — seed baris `IsActive=false` (discriminating, Pitfall 2) → assert `affectedUserUnitsCount == jumlah baris UserUnits ter-rename`.

**Casing (Pitfall 5):** seed fixture casing IDENTIK (InMemory case-sensitive).

---

## Shared Patterns

### Transaksi atomic + InMemory caveat (Pitfall 1 — paling kritis bagi planner)
**Source idiom:** `WorkerController.cs:665-688`. **Apply to:** `EditOrganizationUnit` (Sub-3).
**Caveat:** EF Core InMemory **menolak** `BeginTransactionAsync` (TransactionIgnoredWarning → error). Test existing memanggil `ctrl.EditOrganizationUnit(...)` langsung pada InMemory ctx → setelah D-04 akan **gagal** kecuali ditangani. Pilih:
- **(B, untuk preview-parity & rename test yang panggil Edit)** Tambah 1 baris ke `MakeController()`:
  ```csharp
  .UseInMemoryDatabase(Guid.NewGuid().ToString())
  .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
  ```
  (InMemory abaikan tx = no-op; operasi tetap jalan → parity teruji; atomicity = domain SQL Phase 404).
- **(A, untuk split-detect logic murni)** uji sebagai helper non-transaksi bila diekstrak.
`PreviewEditCascade` sendiri TAK pakai tx → aman tanpa perubahan.

### Filter `IsActive` — aturan tegas (Pitfall 2, anti-drift)
**Apply to:** semua query `UserUnits` di fase ini.
- **Rename (Sub-1) + Preview count (Sub-4) = TANPA `IsActive`** (`uu.Unit == oldName`) — parity WAJIB identik.
- **Split-detect (Sub-2) + Delete guard (Sub-5) + Deactivate guard (Sub-6) = `&& uu.IsActive`**.

### `_context.UserUnits` correlated, BUKAN nav-prop (lesson 400-01)
**Apply to:** semua akses `UserUnits`. `ApplicationUser` TIDAK punya nav-prop `UserUnits` (CS1061). Selalu `_context.UserUnits.Where/AnyAsync/CountAsync(...)`.

### Response AJAX/non-AJAX
**Source:** pola `IsAjaxRequest() ? Json(...) : TempData+Redirect` (seluruh `OrganizationController`). **Apply to:** semua jalur respons baru (split-block, guard sekunder).

### Authz + CSRF (jangan dihapus)
**Source:** `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` di Edit/Preview/Toggle/Delete. **Apply to:** verification — pastikan tak ada atribut hilang dari action yang disentuh.

### Map unit→Bagian (don't hand-roll)
**Source:** `ApplicationDbContext.GetSectionUnitsDictAsync()` (`ApplicationDbContext.cs:121-136`) → `Dictionary<Bagian, List<unitName>>`, balik jadi `unit→Bagian`. **Apply to:** split-detect (Sub-2). Hindari walk ParentId manual per-unit (N+1).

---

## No Analog Found

Tidak ada. Keempat file punya analog exact (in-file mirror atau idiom proyek 399). Tabel kosong by design.

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| — | — | — | semua tercakup analog existing |

## Metadata

**Analog search scope:** `Controllers/OrganizationController.cs`, `Controllers/WorkerController.cs`, `HcPortal.Tests/OrganizationControllerTests.cs`, `wwwroot/js/orgTree.js`, `Views/Admin/ManageOrganization.cshtml`, `Data/ApplicationDbContext.cs`.
**Files scanned:** 6 (semua diverifikasi langsung; line numbers di atas = aktual 2026-06-18, mengoreksi drift ~1-3 baris di RESEARCH.md).
**Verified line numbers:** rename scalar L218-220 · reparent walk L235-247 · cascade Section L249-264 · preview count L298-324 · preview return L326-333 · Toggle guard L385-399 · Delete guard L447-454 · tx idiom WorkerController L665-688 · test seam L19-49 · preview-parity test L94-174 · JS populate L361-364 · JS sum L426-427 · view `<ul>` L221-226 · GetSectionUnitsDictAsync ApplicationDbContext L121-136.
**Pattern extraction date:** 2026-06-18
