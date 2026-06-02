# Design — ManageOrganization Overhaul + Level Label CRUD

**Date:** 2026-06-02
**Status:** Brainstorm complete, awaiting user review
**Target milestone:** v21.0 (after v20.0 CMP/Records overhaul ships)
**Scope:** 3 bagian — fix bug page ManageOrganization + tabel CRUD label tier + integrasi label seluruh app
**Effort estimate:** ~5 hari kerja

---

## 1. Konteks & Motivasi

Page `/Admin/ManageOrganization` adalah halaman pengelolaan struktur organisasi (Bagian → Unit → Sub-unit) yang dipakai HC + Admin. Page ini hasil refactor di milestone v13.0 (Phases 292-295, shipped 2026-04-06) dari 520-baris view ke shell + AJAX tree.

Setelah pemakaian beberapa minggu, user melaporkan 2 issue utama:
- Tree row tidak menampilkan tier label ("ini Bagian / Unit / Sub-unit?"). Hanya warna ikon yang berbeda, tanpa keterangan teks.
- Dropdown "Induk" di modal Tambah/Edit Unit tampil tidak terurut secara hierarkis — anak dari Bagian-A campur dengan anak dari Bagian-B karena sort `level → displayOrder` flat per level, bukan pre-order DFS.

Audit follow-up menemukan 6 bug tambahan + 4 inovasi UX worth doing. Plus user minta fitur baru: **HC/Admin bisa rename label tier sendiri** ("Bagian" → "Direktorat" misalnya) tanpa edit kode.

Cascade rename di `OrganizationController.cs:198-228` memetakan:
- Level 0 → `User.Section`, `ProtonKompetensiList.Bagian`, `CoachingGuidanceFiles.Bagian`
- Level 1+ → `User.Unit`, `ProtonKompetensiList.Unit`, `CoachingGuidanceFiles.Unit`

Semantik tier (data model `User.Section` + `User.Unit`) **tidak berubah** — label CRUD hanya rename **display string**. Backend tetap hardcoded level 0 vs 1+.

---

## 2. Goals & Non-Goals

### Goals

1. Setiap tree row menampilkan badge tier yang jelas (Bagian / Unit / Sub-unit).
2. Dropdown "Induk" terurut pre-order DFS (parent → semua keturunannya → sibling parent berikutnya).
3. Validasi nama unit unique **per-parent**, bukan global.
4. Parent nonaktif tetap visible di dropdown (greyed + suffix "(nonaktif)").
5. Modal Edit tampilkan warning kuantitatif berapa user + mapping kena dampak cascade saat rename/reparent.
6. Modal tampil path breadcrumb induk yang dipilih ("RFCC → LPG Treating → ...").
7. Label tier ("Bagian", "Unit", "Sub-unit") tersimpan di DB, di-CRUD via page baru `/Admin/ManageOrgLevelLabels` oleh role Admin + HC.
8. Label tier auto-applied di **semua page** Portal HC KPB yang menampilkan terminologi tier (tidak hanya page ManageOrganization).
9. Tidak ada regresi pada fungsi existing (add/edit/delete/toggle/drag-reorder).

### Non-Goals

- **Search box di tree** — tidak masuk scope (deferred).
- **Drag-reparent across siblings** — drag tetap reorder dalam parent saja (existing behavior).
- **Stats breakdown by status (Aktif/Nonaktif)** — deferred.
- **Refactor User model dari flat `Section + Unit` ke FK `OrganizationUnitId`** — out of scope. Cascade rename approach dipertahankan.
- **ExecuteUpdateAsync optimization** untuk cascade rename — premature optimization untuk usage actual (rename event langka).
- **i18n / multi-language label** — out of scope. Label single-string per level.
- **Audit log message label dynamic** — log internal pakai literal "Bagian"/"Unit" supaya stabil untuk debug.
- **Multi-server cache invalidation strategy** — Portal HC KPB single-server, in-memory cache cukup.

---

## 3. Arsitektur Solusi

### 3.1 Komponen baru

```
Models/
  OrganizationLevelLabel.cs           // entity tabel baru

Services/
  IOrgLabelService.cs
  OrgLabelService.cs                  // read + cache + invalidate

Controllers/
  OrgLabelController.cs               // CRUD page + API endpoint

Views/Admin/
  ManageOrgLevelLabels.cshtml         // page CRUD

Data/
  SeedData.cs                         // seed default label rows
  ApplicationDbContext.cs             // DbSet<OrganizationLevelLabel>

Migrations/
  YYYYMMDDHHMMSS_AddOrganizationLevelLabel.cs
```

### 3.2 Komponen yang berubah

```
Views/Admin/ManageOrganization.cshtml      // tambah legend + dynamic title
wwwroot/js/orgTree.js                      // fix sort + path preview + cascade warning + escape fix + level cap
Controllers/OrganizationController.cs      // fix dup-name per-parent + inactive parent + cascade preview API

Views (semua page sebut "Bagian"/"Unit"):  // ganti hardcode jadi @inject IOrgLabelService
  Views/CMP/*.cshtml
  Views/CDP/*.cshtml
  Views/Worker/*.cshtml
  Views/CoachMapping/*.cshtml
  Views/ProtonData/*.cshtml
  Views/Renewal/*.cshtml
  Views/DocumentAdmin/*.cshtml

Controllers (string literal di response/log untuk display):
  CMPController.cs, CDPController.cs, WorkerController.cs,
  CoachMappingController.cs, ProtonDataController.cs,
  RenewalController.cs, DocumentAdminController.cs
```

### 3.3 Data flow label

```
[HC ubah label di /Admin/ManageOrgLevelLabels]
        ↓
[OrgLabelController.Update → UPDATE tabel + AuditLog + cache.Invalidate]
        ↓
[Next request → OrgLabelService.GetLabel(level) → MemoryCache miss → DB read → cache set]
        ↓
[View @inject IOrgLabelService → render label di breadcrumb/badge/title]
[JS fetch GET /Admin/GetLevelLabels → render di dropdown/legend/modal]
```

---

## 4. Detail Komponen

### 4.1 Tabel `OrganizationLevelLabels`

| Kolom | Tipe | Constraint | Catatan |
|---|---|---|---|
| `Level` | `int` | PK | 0, 1, 2, ... |
| `Label` | `nvarchar(50)` | NOT NULL | "Bagian", "Unit", dst |
| `UpdatedAt` | `datetime2` | NOT NULL | UTC |
| `UpdatedBy` | `nvarchar(450)` | NOT NULL | UserId Identity |

**Unique constraint:** `Label` unique across all rows (mencegah dua level pakai label sama). Implementasi via EF Fluent API `HasIndex(e => e.Label).IsUnique()` di migration.

**Migration seed (Phase 1):**
```sql
INSERT INTO OrganizationLevelLabels (Level, Label, UpdatedAt, UpdatedBy) VALUES
  (0, 'Bagian',    SYSUTCDATETIME(), 'system'),
  (1, 'Unit',      SYSUTCDATETIME(), 'system'),
  (2, 'Sub-unit',  SYSUTCDATETIME(), 'system');
```

Seed klasifikasi: **permanent + prod-required** → masuk `Data/SeedData.cs` per `docs/SEED_WORKFLOW.md`.

### 4.2 Service `OrgLabelService`

```csharp
public interface IOrgLabelService
{
    string GetLabel(int level);
    IReadOnlyDictionary<int, string> GetAll();
    Task UpdateAsync(int level, string label, string userId);
    Task AddAsync(int level, string label, string userId);
    Task DeleteAsync(int level, string userId);
    int GetMaxConfiguredLevel();
    int GetMaxUsedLevel(); // MAX(Level) di OrganizationUnits
}
```

**Caching:**
- `IMemoryCache` singleton, key `"OrgLabels:All"`, no expiration (manual invalidate).
- `GetLabel(level)`: bila level > max configured, fallback `"Level {level}"` (mis: level 5 not configured → return `"Level 5"`).
- Invalidation: setiap `UpdateAsync`/`AddAsync`/`DeleteAsync` → `_cache.Remove("OrgLabels:All")`.

**Auto-detect depth (rule):**
- `GetMaxUsedLevel()` query `_context.OrganizationUnits.MaxAsync(u => u.Level)`.
- UI Page `/Admin/ManageOrgLevelLabels` tampilkan baris `0..max(used, configured) + 1` (buffer 1 untuk allow add child baru di max+1).

### 4.3 Page `/Admin/ManageOrgLevelLabels`

**Layout:**
```
Kelola Data / Kelola Label Tier Organisasi

Info banner: ℹ️ Label ini hanya nama tampilan tier. Mengubah label
             TIDAK mengubah struktur data atau tier. Cascade backend
             tetap berlaku berdasarkan level numerik.

Tabel:
| Level | Label saat ini | Aksi          |
| 0     | Bagian         | [Edit]        |
| 1     | Unit           | [Edit]        |
| 2     | Sub-unit       | [Edit]        |
| 3     | (belum diset)  | [Tambah]      |   ← auto-detect: ada unit level 3 atau buffer slot

Tombol: [+ Tambah Level Baru]
```

**Edit modal (per row):**
- Input `Label` required, max 50 char.
- Validation: unique across all rows (kecuali row sendiri).
- Validation: tidak boleh kosong / whitespace only.
- Submit → `UpdateAsync` → toast success + reload page.

**Delete (level tertinggi only):**
- Hanya tombol `Delete` muncul untuk level paling atas YANG TIDAK DIPAKAI unit manapun. Mencegah orphan label reference.
- Check: `OrganizationUnits.AnyAsync(u => u.Level == levelToDelete)` → bila true, disable + tooltip "Tidak bisa dihapus, masih dipakai".

**Permission:** `[Authorize(Roles = "Admin, HC")]`.

**Audit log:** setiap UPDATE/INSERT/DELETE catat ke existing `AuditLog` table (cek pattern di `OrganizationController.cs` existing).

### 4.4 Validation rules

| Aturan | Reasoning |
|---|---|
| Label required, non-whitespace | Mencegah row "" yang break UI |
| Label unique across levels | Mencegah "Bagian" di level 0 dan 2 bingung user |
| Label max 50 char | Cukup untuk semua kasus terminologi org |
| Delete hanya untuk level tertinggi & tidak dipakai | Mencegah orphan reference |

### 4.5 Integrasi ke view existing

**Pattern Razor:**
```csharp
@inject IOrgLabelService OrgLabels

<span class="badge">@OrgLabels.GetLabel(0)</span>  // "Bagian"
<label>@OrgLabels.GetLabel(1)</label>              // "Unit"
```

**Pattern JS:**
```js
// orgTree.js init
const labels = await ajaxGet('/Admin/GetLevelLabels');
// { 0: "Bagian", 1: "Unit", 2: "Sub-unit" }

function getLabelForLevel(level, labels) {
    return labels[level] ?? `Level ${level}`;
}
```

**Endpoint API:**
```
GET /Admin/GetLevelLabels
Auth: any authenticated user (label public)
Response: { "0": "Bagian", "1": "Unit", "2": "Sub-unit" }
```

### 4.6 Page ManageOrganization changes

**Tambah legend di card header:**
```
┌──────────────────────────────────────────────────┐
│ ● Hierarki Organisasi          [Drag reorder]    │
│ ┌─ Legend ─────────────────────────────────────┐ │
│ │ ▣ Bagian  ▣ Unit  ▣ Sub-unit                 │ │
│ └──────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘
```

**Badge per tree row:**
```
RFCC                [Bagian] 2 unit  [Aktif] ⋮
  └─ LPG Treating   [Unit] 0 unit    [Aktif] ⋮
```

**Modal title dynamic:**
- Tambah unit di root → "Tambah Bagian"
- Tambah unit di bawah level 0 → "Tambah Unit"
- Tambah unit di bawah level 1 → "Tambah Sub-unit"
- Tambah unit di bawah level N → label level N+1 (dengan fallback "Level N+1")

**Dropdown induk — pre-order DFS sort (fix Bug B):**
```js
function flattenTreePreOrder(roots) {
    const out = [];
    function walk(node, depth) {
        out.push({ ...node, depth });
        (node.children || []).forEach(c => walk(c, depth + 1));
    }
    roots.forEach(r => walk(r, 0));
    return out;
}

function populateParentDropdown(excludeId) {
    const select = document.getElementById('unitModalParent');
    select.innerHTML = '<option value="">— Tidak ada (root) —</option>';
    const excludeIds = excludeId ? getDescendantIds(excludeId) : new Set();
    if (excludeId) excludeIds.add(excludeId);

    const roots = buildTree(_flatUnits);
    flattenTreePreOrder(roots)
        .filter(u => !excludeIds.has(u.id))
        .forEach(u => {
            const indent = ' '.repeat(u.depth * 4);
            const suffix = u.isActive ? '' : ' (nonaktif)';
            const opt = document.createElement('option');
            opt.value = u.id;
            opt.textContent = indent + u.name + suffix;
            if (!u.isActive) opt.style.color = '#999';
            select.appendChild(opt);
        });
}
```

Bug #2 (inactive parent) fix: hapus `.filter(u => u.isActive)`, ganti dengan suffix + style.

**Path preview saat pilih induk (Innovation #7):**
- Saat `select#unitModalParent` change → tampilkan breadcrumb di bawah dropdown.
- Layout: `<div class="text-muted small mt-1" id="unitModalPath"></div>`
- Render: "Path: RFCC → LPG Treating → (unit baru di sini)".

**Cascade warning saat rename/reparent (Innovation #10, #11):**
- Saat user submit Edit modal **dan** ada perubahan `Name` atau `ParentId` → controller hitung impacted records sebelum apply.
- Tambah endpoint:
  ```
  POST /Admin/PreviewEditCascade { id, name, parentId }
  Response: {
    nameChanged: bool,
    parentChanged: bool,
    affectedUsersCount: int,
    affectedMappingsCount: int,
    affectedKompetensiCount: int,
    affectedGuidanceCount: int
  }
  ```
- Frontend pattern:
  1. User klik Simpan di Edit modal.
  2. JS panggil `PreviewEditCascade` dulu.
  3. Bila ada `affected*Count > 0`, tampilkan modal konfirmasi:
     > "Perubahan ini akan mempengaruhi:
     > - 23 user (Section/Unit)
     > - 5 mapping coach-coachee
     > - 12 kompetensi PROTON
     > - 3 file panduan
     >
     > Lanjutkan?" [Batal] [Lanjut Simpan]
  4. Bila user lanjut → panggil `EditOrganizationUnit` seperti biasa.

**Escape fix (Bug #3):**
- Hapus inline string interpolation `'${escapeHtml(node.name)}'` di `renderNode`.
- Pakai `data-name` attribute + event delegation:
  ```html
  <a class="dropdown-item text-danger js-delete-trigger"
     data-id="${node.id}"
     data-name="${escapeHtml(node.name)}"
     data-child-count="${hasChildren ? node.children.length : 0}">
    Hapus
  </a>
  ```
  ```js
  container.addEventListener('click', e => {
    const del = e.target.closest('.js-delete-trigger');
    if (!del) return;
    e.preventDefault();
    openDeleteModal(
      parseInt(del.dataset.id),
      del.dataset.name,        // plain string, no JS-string-escape needed
      parseInt(del.dataset.childCount)
    );
  });
  ```

**Level cap icon fix (Bug #4):**
- Existing: `level <= 2 ? level-${level} : 'level-2'`.
- Fix: generate warna dynamic via HSL berdasarkan level modulo palette, atau add `level-3`/`level-4` CSS class.
- Pragmatic: palette 6 warna cycling.
  ```css
  .org-node-icon.level-0 { background: rgba(13,110,253,0.1); color: #0d6efd; }
  .org-node-icon.level-1 { background: rgba(102,126,234,0.1); color: #667eea; }
  .org-node-icon.level-2 { background: rgba(13,202,240,0.1); color: #0dcaf0; }
  .org-node-icon.level-3 { background: rgba(25,135,84,0.1); color: #198754; }
  .org-node-icon.level-4 { background: rgba(255,193,7,0.1); color: #b45309; }
  .org-node-icon.level-5 { background: rgba(220,53,69,0.1); color: #dc3545; }
  ```
  Level 6+ fallback ke `level-5`.

### 4.7 Controller fixes

**Bug #1 — dup-name per-parent:**

Existing `AddOrganizationUnit:85`:
```csharp
bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim());
```

Fix:
```csharp
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId);
```

Existing `EditOrganizationUnit:149`:
```csharp
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.Id != id);
```

Fix:
```csharp
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId && u.Id != id);
```

Catatan: pasangan rename + reparent dalam 1 submit aman karena Edit pakai `parentId` baru dari form, bukan `unit.ParentId` lama.

**Cascade preview endpoint (baru):**
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PreviewEditCascade(int id, string name, int? parentId)
{
    var unit = await _context.OrganizationUnits.FindAsync(id);
    if (unit == null) return Json(new { error = "Unit tidak ditemukan." });

    var trimmed = (name ?? "").Trim();
    bool nameChanged = !string.IsNullOrEmpty(trimmed) && unit.Name != trimmed;
    bool parentChanged = unit.ParentId != parentId;
    if (!nameChanged && !parentChanged)
        return Json(new { nameChanged = false, parentChanged = false });

    // Reuse logic dari EditOrganizationUnit Section/Unit branch
    int affectedUsers = 0, affectedMappings = 0, affectedKompetensi = 0, affectedGuidance = 0;
    string oldName = unit.Name;

    if (nameChanged)
    {
        if (unit.Level == 0)
        {
            affectedUsers += await _context.Users.CountAsync(u => u.Section == oldName);
            affectedMappings += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentSection == oldName);
            affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Bagian == oldName);
            affectedGuidance += await _context.CoachingGuidanceFiles.CountAsync(g => g.Bagian == oldName);
        }
        else
        {
            affectedUsers += await _context.Users.CountAsync(u => u.Unit == oldName);
            affectedMappings += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentUnit == oldName);
            affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Unit == oldName);
            affectedGuidance += await _context.CoachingGuidanceFiles.CountAsync(g => g.Unit == oldName);
        }
    }

    if (parentChanged && unit.Level >= 1)
    {
        // Reparent affected users count (same Unit-name population, count once)
        if (!nameChanged)
            affectedUsers += await _context.Users.CountAsync(u => u.Unit == oldName);
    }

    return Json(new {
        nameChanged,
        parentChanged,
        affectedUsersCount = affectedUsers,
        affectedMappingsCount = affectedMappings,
        affectedKompetensiCount = affectedKompetensi,
        affectedGuidanceCount = affectedGuidance
    });
}
```

Catatan implementasi: preview menggunakan COUNT query (single round-trip), bukan reload entitas. Mengurangi overhead untuk preview-only operation. Aktual rename tetap pakai existing loop di `EditOrganizationUnit`.

### 4.8 Integrasi label seluruh app (Bagian 3)

**Scoping:**
- View Razor: ganti hardcoded string "Bagian"/"Unit" jadi `@OrgLabels.GetLabel(N)`.
- Controller string literal yang masuk ke response/view (mis: `TempData["Success"] = "Bagian X berhasil dihapus"`) → ganti pakai `_orgLabels.GetLabel(N)`.
- **TIDAK** ganti:
  - Audit log message body (stabil untuk debug).
  - DB column name / model property name (mis: `User.Section`, `User.Unit` — schema unchanged).
  - Migration script literals.
  - Test strings (Playwright + xUnit — pakai label final saat test ditulis, deterministik).

**Page-by-page audit (perlu manual review saat Phase implementation):**

| Controller | View | Sebutan tier yang perlu diganti |
|---|---|---|
| `CMPController.cs` | `Views/CMP/*.cshtml` | Filter "Bagian", grouping "Unit" |
| `CDPController.cs` | `Views/CDP/*.cshtml` | Assignment "Bagian"/"Unit", scope filter |
| `WorkerController.cs` | `Views/Worker/*.cshtml` | Field label "Bagian"/"Unit" di form |
| `CoachMappingController.cs` | `Views/CoachMapping/*.cshtml` | Mapping scope label |
| `ProtonDataController.cs` | `Views/ProtonData/*.cshtml` | Kompetensi "Bagian"/"Unit" filter |
| `RenewalController.cs` | `Views/Renewal/*.cshtml` | Scope label di list |
| `DocumentAdminController.cs` | `Views/DocumentAdmin/*.cshtml` | KKJ/CPDP file org reference |

Saat implementation phase, Grep `Bagian|Unit` per file + manual decide setiap occurrence: apakah display string (ganti) atau identifier/literal (skip).

---

## 5. Dependency Registration

`Program.cs`:
```csharp
builder.Services.AddSingleton<IMemoryCache>(/* default */);
builder.Services.AddScoped<IOrgLabelService, OrgLabelService>();
```

`_ViewImports.cshtml`:
```csharp
@inject HcPortal.Services.IOrgLabelService OrgLabels
```

---

## 6. Edge Cases

| Case | Behavior |
|---|---|
| Tabel `OrganizationLevelLabels` kosong | `GetLabel(level)` → fallback `"Level {level}"`. UI page label tampil "(belum ada label)" + tombol Add. |
| Level > max configured | `GetLabel(N)` → fallback `"Level {N}"`. |
| HC delete label level yang masih dipakai | UI block (disable button + tooltip). Server-side double-check di endpoint. |
| User concurrent edit label | Last-write-wins. Audit log preserve history. Tidak ada optimistic lock (low-conflict scenario). |
| Cache stale antar tab user yang sama | User klik refresh dapat label baru. Tidak ada WebSocket push. |
| Migration ke Dev/Prod | Klasifikasi seed `permanent + prod-required`. Update `docs/IT_NOTIFY.md` saat ready promo. |
| Tom Select integration | Innovation #7 path preview pakai vanilla dropdown + custom path render di bawahnya, **bukan** Tom Select (tetap minimal lib footprint). Tom Select bisa di-defer ke future bila >100 unit. |
| Label CRUD page meta-label | Page title "Kelola Label Tier Organisasi" hardcoded — tidak referensi ke service sendiri (avoid recursion saat tabel kosong). |
| Multi-tenant future | Out of scope. Tabel single-row-per-level. |

---

## 7. Testing Strategy

### Unit tests (xUnit)
- `OrgLabelService.GetLabel` fallback behavior.
- `OrgLabelService.Update` invalidate cache.
- `OrgLabelController` permission denial untuk non-Admin/non-HC.
- Pre-order DFS sort correctness untuk dropdown.
- Dup-name check per-parent (existing test diperluas).

### Integration tests
- Migration apply + seed default.
- `PreviewEditCascade` count accuracy vs aktual `EditOrganizationUnit` impact.

### Playwright E2E (per `docs/DEV_WORKFLOW.md` step 3)
1. Page `/Admin/ManageOrganization` load + legend visible + badge per row.
2. Tambah Unit modal: dropdown induk pre-order + path preview + inactive parent shown greyed.
3. Edit Unit rename → cascade warning modal muncul dengan count.
4. Page `/Admin/ManageOrgLevelLabels` CRUD: edit "Bagian" → "Direktorat" → reload tree → badge tampil "Direktorat".
5. Page integrasi (CMP/CDP/Worker) tampilkan label baru setelah rename.

### Manual UAT (5 scenario)
1. HC ubah label "Bagian" → "Direktorat" → cek di 7 controller area + ManageOrganization.
2. Admin tambah Bagian baru → cek title modal dinamis.
3. Buat unit "Operations" di 2 Bagian beda → tidak ditolak (per-parent unique).
4. Nonaktifkan Bagian → masih bisa edit anaknya, pilih parent lain.
5. Edit Bagian dengan banyak user → warning muncul dengan count benar.

---

## 8. Migration & Deployment

### Step urut

1. EF migration `AddOrganizationLevelLabel` — buat tabel + seed default 3 rows (Bagian/Unit/Sub-unit).
2. Local verify: `dotnet ef database update` + run lokal di `http://localhost:5277`.
3. Snapshot DB lokal sebelum + sesudah migration.
4. Update `docs/IT_NOTIFY.md`:
   - Migration name + commit hash.
   - DB Dev backup wajib sebelum apply.
   - Promo step + verifikasi page baru `/Admin/ManageOrgLevelLabels`.
5. Push origin/main → notify IT.
6. IT apply migration di Dev → verify → promo Prod.

### Rollback plan

- Migration down: drop tabel `OrganizationLevelLabels`.
- Service registration: kondisional null-safe — bila tabel hilang, fallback hardcoded label di `OrgLabelService` constructor (try/catch query awal).

---

## 9. Phase Breakdown (untuk planning)

Spec ini akan dipecah jadi 4-5 phase di v21.0:

| Phase | Scope | Effort |
|---|---|---|
| P1 — Foundation | Tabel + Migration + Service + Cache + API endpoint | 1 hari |
| P2 — Label CRUD Page | View + Controller + audit log + page CRUD lengkap | 1 hari |
| P3 — ManageOrganization Fixes | Sort + dup-name + inactive parent + escape + level cap + path preview + cascade warning + legend + dynamic title | 1.5 hari |
| P4 — Integrasi App-wide | Ganti hardcoded "Bagian"/"Unit" di 7 controller + view | 1.5 hari |
| P5 — Test + UAT | Playwright + xUnit + manual UAT 5 scenario | 1 hari (parallel-able) |

Total: ~5 hari sequential, bisa P3+P4 paralel hemat 1 hari.

Dependency:
- P2 depends P1 (butuh service).
- P3 depends P1 (butuh service untuk dynamic title).
- P4 depends P1 (butuh service untuk @inject).
- P5 depends P1+P2+P3+P4.

---

## 10. Open Questions

(Tidak ada — semua decision sudah locked saat brainstorm.)

---

## 11. Sumber & Referensi

- `Views/Admin/ManageOrganization.cshtml` — view existing
- `wwwroot/js/orgTree.js` — JS tree existing
- `Controllers/OrganizationController.cs` — CRUD + cascade rename logic
- `Models/OrganizationUnit.cs` — entity model
- `docs/DEV_WORKFLOW.md` — deploy SOP
- `docs/SEED_WORKFLOW.md` — seed klasifikasi rule
- `CLAUDE.md` — project workflow rules
- Memory: `singkatan_portal_hc` (PROTON/CMP/CDP/BP/KKJ verified)
- Memory: `v19.0_portal_hc_bug_fixes_planned` (cascade patterns reference)
- Memory: `cmp_records_audit_2026_05_27` (v20.0 lock — explains v21.0 placement)
