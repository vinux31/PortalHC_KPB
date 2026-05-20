# Panduan & Bantuan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `Views/Home/Guide.cshtml` + `GuideDetail.cshtml` dari hardcoded Razor ke model+provider pattern, implementasi 6-RoleGroup RBAC, lalu audit konten (fix role tagging salah, split accordion gabungan, tambah 8 item baru, link orphan PDF, update 5 FAQ).

**Architecture:** Static content provider di kode (no DB). Records di `Models/Guide/` + service `Services/GuideContentProvider.cs` + `Services/GuideRoleAccess.cs`. Controller pass ViewModel ke view. View loop ViewModel via partial (DRY).

**Tech Stack:** ASP.NET Core 8.0 MVC, Razor, Bootstrap 5 accordion/collapse, Playwright (visual regression).

**Testing:** Manual UAT only (no test project di repo, per spec decision Q6). PR 2 pakai Playwright visual regression baseline.

**Reference:** [`docs/superpowers/specs/2026-05-20-panduan-bantuan-design.md`](../specs/2026-05-20-panduan-bantuan-design.md)

---

## File Structure

### Phase 1 — Foundation (PR 1)

**Created:**
- `Models/Guide/GuideModule.cs` — enum 5 module
- `Models/Guide/RoleGroup.cs` — enum 6 grup
- `Models/Guide/FaqCategory.cs` — enum 6 kategori FAQ
- `Models/Guide/GuideStep.cs` — record 1 step
- `Models/Guide/GuideItem.cs` — record 1 accordion item
- `Models/Guide/GuideFaqItem.cs` — record 1 FAQ
- `Models/Guide/GuidePdfLink.cs` — record 1 PDF card
- `Models/Guide/GuideModuleCardVm.cs` — VM 1 module card
- `Models/Guide/GuideViewModel.cs` — VM Guide.cshtml
- `Models/Guide/GuideDetailViewModel.cs` — VM GuideDetail.cshtml
- `Services/GuideRoleAccess.cs` — `GroupsFor`, `CanSee`, `BadgeLabel`
- `Services/GuideContentProvider.cs` — static seed + filter

**Modified:**
- `Views/_ViewImports.cshtml` — tambah `@using HcPortal.Models.Guide`

### Phase 2 — Refactor Views (PR 2)

**Created:**
- `Views/Shared/_Guide/_RoleBadge.cshtml`
- `Views/Shared/_Guide/_ModuleCard.cshtml`
- `Views/Shared/_Guide/_AccordionItem.cshtml`
- `Views/Shared/_Guide/_FaqItem.cshtml`
- `tests/playwright/guide-parity.spec.ts`

**Modified:**
- `Controllers/HomeController.cs:324-355` — Guide & GuideDetail return ViewModel
- `Views/Home/Guide.cshtml` — loop ViewModel + slug ID
- `Views/Home/GuideDetail.cshtml` — loop ViewModel + slug ID

### Phase 3 — Audit Konten (PR 3, multi-commit per module)

**Modified:**
- `Services/GuideContentProvider.cs` — update 3 role tagging + split 2 + tambah 8 + 1 PDF + 5 FAQ

### Phase 4 — Search Improvement (PR 4, optional)

**Modified:**
- `Services/GuideContentProvider.cs` — auto-keyword
- `Views/Home/Guide.cshtml` — filter chip UI
- `wwwroot/css/guide.css` — chip style

---

# PHASE 1 — Foundation (PR 1)

Tujuan: bikin model + provider port konten existing as-is. Tidak sentuh view, tidak ada visual change.

## Task 1: Enums

**Files:**
- Create: `Models/Guide/GuideModule.cs`
- Create: `Models/Guide/RoleGroup.cs`
- Create: `Models/Guide/FaqCategory.cs`

- [ ] **Step 1.1: Create `Models/Guide/GuideModule.cs`**

```csharp
namespace HcPortal.Models.Guide;

public enum GuideModule
{
    Cmp,
    Cdp,
    Account,
    Data,
    Admin
}
```

- [ ] **Step 1.2: Create `Models/Guide/RoleGroup.cs`**

```csharp
namespace HcPortal.Models.Guide;

public enum RoleGroup
{
    AdminHC,
    Manager,
    Atasan,
    Coach,
    Coachee,
    All
}
```

- [ ] **Step 1.3: Create `Models/Guide/FaqCategory.cs`**

```csharp
namespace HcPortal.Models.Guide;

public enum FaqCategory
{
    Akun,
    Assessment,
    CdpCoaching,
    Umum,
    KkjCpdp,
    AdminData
}
```

- [ ] **Step 1.4: Build verify**

Run: `dotnet build`
Expected: build succeeded, no warnings related to new files.

---

## Task 2: Records (data carriers)

**Files:**
- Create: `Models/Guide/GuideStep.cs`
- Create: `Models/Guide/GuideItem.cs`
- Create: `Models/Guide/GuideFaqItem.cs`
- Create: `Models/Guide/GuidePdfLink.cs`

- [ ] **Step 2.1: Create `Models/Guide/GuideStep.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuideStep(int Number, string Title, string BodyHtml);
```

- [ ] **Step 2.2: Create `Models/Guide/GuideItem.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuideItem(
    string Id,
    GuideModule Module,
    string Title,
    RoleGroup[] Roles,
    GuideStep[] Steps,
    string[] Keywords
);
```

- [ ] **Step 2.3: Create `Models/Guide/GuideFaqItem.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuideFaqItem(
    string Id,
    FaqCategory Category,
    string Question,
    string AnswerHtml,
    RoleGroup[] Roles,
    string[] Keywords
);
```

- [ ] **Step 2.4: Create `Models/Guide/GuidePdfLink.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuidePdfLink(
    GuideModule Module,
    string Title,
    string Description,
    string FilePath,
    string CardCssClass,
    string BtnColorClass,
    RoleGroup[] Roles
);
```

- [ ] **Step 2.5: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 3: ViewModels

**Files:**
- Create: `Models/Guide/GuideModuleCardVm.cs`
- Create: `Models/Guide/GuideViewModel.cs`
- Create: `Models/Guide/GuideDetailViewModel.cs`

- [ ] **Step 3.1: Create `Models/Guide/GuideModuleCardVm.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuideModuleCardVm(
    GuideModule Module,
    string Title,
    string IconCssClass,
    string CardCssClass,
    int ItemCount,
    RoleGroup[] Roles
);
```

- [ ] **Step 3.2: Create `Models/Guide/GuideViewModel.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuideViewModel(
    string UserRole,
    IReadOnlyList<GuideModuleCardVm> ModuleCards,
    IReadOnlyDictionary<FaqCategory, IReadOnlyList<GuideFaqItem>> FaqsByCategory
);
```

- [ ] **Step 3.3: Create `Models/Guide/GuideDetailViewModel.cs`**

```csharp
namespace HcPortal.Models.Guide;

public record GuideDetailViewModel(
    string UserRole,
    GuideModule Module,
    string ModuleTitle,
    string ModuleIcon,
    string ModuleBreadcrumb,
    string ModuleCategory,
    GuidePdfLink? Pdf,
    IReadOnlyList<GuideItem> Items
);
```

- [ ] **Step 3.4: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 4: GuideRoleAccess service

**Files:**
- Create: `Services/GuideRoleAccess.cs`

- [ ] **Step 4.1: Create `Services/GuideRoleAccess.cs`**

```csharp
using HcPortal.Models;
using HcPortal.Models.Guide;

namespace HcPortal.Services;

public static class GuideRoleAccess
{
    public static RoleGroup[] GroupsFor(string userRole) => userRole switch
    {
        UserRoles.Admin or UserRoles.HC
            => new[] { RoleGroup.AdminHC, RoleGroup.Manager, RoleGroup.Atasan,
                       RoleGroup.Coach, RoleGroup.Coachee, RoleGroup.All },

        UserRoles.Direktur or UserRoles.VP or UserRoles.Manager
            => new[] { RoleGroup.Manager, RoleGroup.All },

        UserRoles.SectionHead or UserRoles.SrSupervisor
            => new[] { RoleGroup.Atasan, RoleGroup.All },

        UserRoles.Coach or UserRoles.Supervisor
            => new[] { RoleGroup.Coach, RoleGroup.All },

        UserRoles.Coachee
            => new[] { RoleGroup.Coachee, RoleGroup.All },

        _ => new[] { RoleGroup.All }
    };

    public static bool CanSee(string userRole, RoleGroup[] itemRoles)
        => GroupsFor(userRole).Any(g => itemRoles.Contains(g));

    public static string BadgeLabel(RoleGroup[] roles)
    {
        if (roles.Contains(RoleGroup.All)) return string.Empty;
        if (roles.Length == 0) return string.Empty;

        var labels = roles.Select(g => g switch
        {
            RoleGroup.AdminHC => "Admin & HC",
            RoleGroup.Manager => "Manager",
            RoleGroup.Atasan  => "Atasan",
            RoleGroup.Coach   => "Coach",
            RoleGroup.Coachee => "Coachee",
            _                 => g.ToString()
        });
        return string.Join(" & ", labels);
    }
}
```

- [ ] **Step 4.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 5: GuideContentProvider — skeleton + module cards

**Files:**
- Create: `Services/GuideContentProvider.cs`

Goal: skeleton structure dengan seed module cards + 1 sample item per module untuk smoke test. Seed lengkap nanti di Task 6-8.

- [ ] **Step 5.1: Create `Services/GuideContentProvider.cs` (skeleton)**

```csharp
using HcPortal.Models.Guide;

namespace HcPortal.Services;

public static class GuideContentProvider
{
    public static readonly IReadOnlyList<GuideItem> Items = new List<GuideItem>
    {
        // Seed di Task 6
    };

    public static readonly IReadOnlyList<GuideFaqItem> Faqs = new List<GuideFaqItem>
    {
        // Seed di Task 7
    };

    public static readonly IReadOnlyList<GuidePdfLink> Pdfs = new List<GuidePdfLink>
    {
        // Seed di Task 8
    };

    public static IReadOnlyList<GuideItem> GetItems(GuideModule module, string userRole)
        => Items
            .Where(i => i.Module == module && GuideRoleAccess.CanSee(userRole, i.Roles))
            .ToList();

    public static IReadOnlyDictionary<FaqCategory, IReadOnlyList<GuideFaqItem>> GetFaqsByCategory(string userRole)
        => Faqs
            .Where(f => GuideRoleAccess.CanSee(userRole, f.Roles))
            .GroupBy(f => f.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GuideFaqItem>)g.ToList()
            );

    public static GuidePdfLink? GetPdf(GuideModule module, string userRole)
        => Pdfs.FirstOrDefault(p => p.Module == module && GuideRoleAccess.CanSee(userRole, p.Roles));

    public static IReadOnlyList<GuideModuleCardVm> GetModuleCards(string userRole)
    {
        var allModules = new[]
        {
            (GuideModule.Cmp,     "CMP — Competency Management Platform", "bi-journal-bookmark-fill", "card-cmp",     new[] { RoleGroup.All }),
            (GuideModule.Cdp,     "CDP — Competency Development Platform", "bi-graph-up-arrow",        "card-cdp",     new[] { RoleGroup.All }),
            (GuideModule.Account, "Akun & Profil",                         "bi-person-circle",         "card-account", new[] { RoleGroup.All }),
            (GuideModule.Data,    "Kelola Data",                           "bi-database-fill",         "card-data",    new[] { RoleGroup.AdminHC }),
            (GuideModule.Admin,   "Admin Panel",                           "bi-gear-wide-connected",   "card-admin",   new[] { RoleGroup.AdminHC }),
        };

        return allModules
            .Where(m => GuideRoleAccess.CanSee(userRole, m.Item5))
            .Select(m => new GuideModuleCardVm(
                Module: m.Item1,
                Title: m.Item2,
                IconCssClass: m.Item3,
                CardCssClass: m.Item4,
                ItemCount: GetItems(m.Item1, userRole).Count + (GetPdf(m.Item1, userRole) != null ? 1 : 0),
                Roles: m.Item5
            ))
            .ToList();
    }
}
```

- [ ] **Step 5.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 6: Port Items existing as-is (port verbatim)

**Files:**
- Modify: `Services/GuideContentProvider.cs` — isi `Items` list

Goal: port 28 item accordion existing dari `Views/Home/GuideDetail.cshtml` ke `Items` list. **Tagging Roles ikut existing `@if` (cuma `All` atau `AdminHC`)** — audit tagging baru nanti di Phase 3. Step text salin verbatim dari .cshtml.

**Reference source:** `Views/Home/GuideDetail.cshtml` line 187-689.

**Mapping ID lama → slug baru:**

| Lama (line) | Slug baru | Module | Roles |
|-------------|-----------|--------|-------|
| cmpCollapse1 (191) | `cmp-library-kkj` | Cmp | All |
| cmpCollapse2 (208) | `cmp-mapping-kkj-cpdp` | Cmp | All |
| cmpCollapse6 (225) | `cmp-training-records` | Cmp | All |
| cmpCollapse7 (244, AdminHC gate) | `cmp-monitoring-records-tim` | Cmp | AdminHC |
| cdpCollapse1 (266) | `cdp-plan-idp-silabus` | Cdp | All |
| cdpCollapse5 (287, AdminHC gate) | `cdp-approve-deliverable` | Cdp | AdminHC |
| cdpCollapse6 (308, AdminHC gate) | `cdp-coaching-proton-dashboard` | Cdp | AdminHC |
| cdpCollapse7 (327) | `cdp-daftar-deliverable` | Cdp | All |
| cdpCollapse8 (345) | `cdp-historik-proton-export` | Cdp | All |
| accCollapse1 (366) | `account-login` | Account | All |
| accCollapse2 (383) | `account-profile-view-edit` | Account | All |
| accCollapse3 (400) | `account-change-password` | Account | All |
| accCollapse4 (417) | `account-logout-role-system` | Account | All |
| dataCollapse1 (437) | `data-silabus` | Data | AdminHC |
| dataCollapse2 (454) | `data-guidance-files` | Data | AdminHC |
| dataCollapse3 (471) | `data-override-proton` | Data | AdminHC |
| admCollapse1 (490) | `admin-kelola-pekerja` | Admin | AdminHC |
| admCollapse2 (508) | `admin-upload-kkj-cpdp` | Admin | AdminHC |
| admCollapse3 (525) | `admin-mapping-coach-coachee` | Admin | AdminHC |
| admCollapse4 (542) | `admin-bank-soal` | Admin | AdminHC |
| admCollapse5 (561) | `admin-create-assessment` | Admin | AdminHC |
| admCollapse6 (580) | `admin-assessment-monitoring` | Admin | AdminHC |
| admCollapse7 (599) | `admin-add-training` | Admin | AdminHC |
| admCollapse8 (618) | `admin-audit-log` | Admin | AdminHC |
| admCollapse9 (637) | `admin-kelola-units` | Admin | AdminHC |
| admCollapse11 (656) | `admin-notifikasi-system` | Admin | AdminHC |
| admCollapse12 (674) | `admin-maintenance-impersonate` | Admin | AdminHC |

- [ ] **Step 6.1: Port CMP items (4 item)**

Edit `Services/GuideContentProvider.cs`, ganti `Items` list jadi:

```csharp
public static readonly IReadOnlyList<GuideItem> Items = new List<GuideItem>
{
    // ───── CMP ─────
    new GuideItem(
        Id: "cmp-library-kkj",
        Module: GuideModule.Cmp,
        Title: "Cara Melihat Library KKJ (Kebutuhan Kompetensi Jabatan)",
        Roles: new[] { RoleGroup.All },
        Steps: new[]
        {
            new GuideStep(1, "Akses CMP", "Klik menu <b>CMP</b> di navbar atas."),
            new GuideStep(2, "Buka Tab Library", "Klik tab <b>Library KKJ</b>. Anda akan melihat daftar kompetensi khusus untuk posisi Anda.")
        },
        Keywords: new[] { "kkj", "library", "kebutuhan kompetensi jabatan" }
    ),
    new GuideItem(
        Id: "cmp-mapping-kkj-cpdp",
        Module: GuideModule.Cmp,
        Title: "Cara Melihat Mapping KKJ — CPDP",
        Roles: new[] { RoleGroup.All },
        Steps: new[]
        {
            new GuideStep(1, "Akses CMP", "Klik menu <b>CMP</b> di navbar atas."),
            new GuideStep(2, "Buka Tab Mapping", "Klik tab <b>CPDP Mapping</b> untuk melihat pemetaan pengembangan kompetensi Anda.")
        },
        Keywords: new[] { "mapping", "cpdp", "kkj" }
    ),
    new GuideItem(
        Id: "cmp-training-records",
        Module: GuideModule.Cmp,
        Title: "Cara Melihat Riwayat Training (Capability Building Records)",
        Roles: new[] { RoleGroup.All },
        Steps: new[]
        {
            new GuideStep(1, "Buka Tab Training Records", "Di menu CMP, klik tab <b>Training Records</b> (Riwayat Pelatihan)."),
            new GuideStep(2, "Review Riwayat", "Semua pelatihan baik internal maupun eksternal yang di-input oleh HC akan tampil di tabel riwayat beserta jam pelatihannya.")
        },
        Keywords: new[] { "training", "riwayat", "records", "capability building" }
    ),
    new GuideItem(
        Id: "cmp-monitoring-records-tim",
        Module: GuideModule.Cmp,
        Title: "Cara Monitoring Records Tim",
        Roles: new[] { RoleGroup.AdminHC },
        Steps: new[]
        {
            new GuideStep(1, "Tab Records Team", "Di menu CMP, klik tab <b>Records Team</b>."),
            new GuideStep(2, "List Anggota Tim", "Anda akan melihat semua anggota tim di bawah supervisi Anda beserta persentase kepatuhan (compliance) dan jam pelatihannya."),
            new GuideStep(3, "Detail Pekerja", "Klik nama pekerja untuk melihat lebih detail riwayat assessment dan training-nya.")
        },
        Keywords: new[] { "monitoring", "records team", "compliance" }
    ),
};
```

Verify build: `dotnet build` → success.

- [ ] **Step 6.2: Port CDP items (5 item)**

Append ke list di `Services/GuideContentProvider.cs` setelah CMP block. Salin step text dari `Views/Home/GuideDetail.cshtml` line 266-359 verbatim. Tetap ada 3 base (All) + 2 admin (AdminHC).

Pattern sama dengan Step 6.1: `Id`, `Module: GuideModule.Cdp`, `Title`, `Roles`, `Steps[]` (verbatim from cshtml), `Keywords` (extract dari `data-keywords` original kalau ada).

Items: `cdp-plan-idp-silabus`, `cdp-approve-deliverable`, `cdp-coaching-proton-dashboard`, `cdp-daftar-deliverable`, `cdp-historik-proton-export`.

Verify build: `dotnet build` → success.

- [ ] **Step 6.3: Port Account items (4 item)**

Append Account items: `account-login`, `account-profile-view-edit`, `account-change-password`, `account-logout-role-system`. All `Roles: [All]`. Salin step text dari cshtml line 365-430.

Verify build.

- [ ] **Step 6.4: Port Data items (3 item, AdminHC)**

Append Data items: `data-silabus`, `data-guidance-files`, `data-override-proton`. All `Roles: [AdminHC]`. Salin dari line 432-485.

Verify build.

- [ ] **Step 6.5: Port Admin items (11 item, AdminHC)**

Append Admin items 11 buah sesuai mapping table. All `Roles: [AdminHC]`. Salin dari line 487-688.

Items: `admin-kelola-pekerja`, `admin-upload-kkj-cpdp`, `admin-mapping-coach-coachee`, `admin-bank-soal`, `admin-create-assessment`, `admin-assessment-monitoring`, `admin-add-training`, `admin-audit-log`, `admin-kelola-units`, `admin-notifikasi-system`, `admin-maintenance-impersonate`.

Verify build: `dotnet build` → success.

- [ ] **Step 6.6: Sanity check — total 27 items**

Quick smoke check di REPL atau temporary console code (boleh dihapus setelah verify):

```csharp
// Verifikasi count manual: CMP 4 + CDP 5 + Account 4 + Data 3 + Admin 11 = 27
// (Existing 28 di .cshtml minus 1 yang akan di-split nanti di Phase 3 jadi 27 dulu, normal)
```

Atau cukup grep manual:
Run: `grep -c "new GuideItem" Services/GuideContentProvider.cs`
Expected: `27`

---

## Task 7: Port FAQ existing (32 item)

**Files:**
- Modify: `Services/GuideContentProvider.cs` — isi `Faqs` list

Goal: port 32 FAQ dari `Views/Home/Guide.cshtml` line 172-530 ke `Faqs` list. Tagging Roles ikut existing (`All` untuk 24 base, `AdminHC` untuk 8 admin category).

**Reference source:** `Views/Home/Guide.cshtml` line 172-530.

**Mapping FAQ:**

| Old ID | Category | Slug | Roles |
|--------|----------|------|-------|
| faq1 | Akun | `faq-akun-login` | All |
| faq2 | Akun | `faq-akun-lupa-password` | All |
| faq3 | Akun | `faq-akun-daftar-baru` | All |
| faq4 | Akun | `faq-akun-ganti-password` | All |
| faq5 | Akun | `faq-akun-akses-menu` | All |
| faq6 | Assessment | `faq-assessment-apa-itu` | All |
| faq7 | Assessment | `faq-assessment-batas-waktu` | All |
| faq8 | Assessment | `faq-assessment-ulang` | All |
| faq8b | Assessment | `faq-assessment-kategori` | All |
| faq8d | Assessment | `faq-assessment-pre-post` | All |
| faq8c | Assessment | `faq-assessment-sertifikat-expired` | All |
| faq9 | CdpCoaching | `faq-cdp-cmp-vs-cdp` | All |
| faq10 | CdpCoaching | `faq-cdp-idp` | All |
| faq11 | CdpCoaching | `faq-cdp-coaching-proton` | All |
| faq12 | CdpCoaching | `faq-cdp-approve-deliverable` | All |
| faq13 | Umum | `faq-umum-browser` | All |
| faq14 | Umum | `faq-umum-hp-mobile` | All |
| faq15 | Umum | `faq-umum-kendala-hubungi` | All |
| faq16 | Umum | `faq-umum-role-system` | All |
| faq17 | Umum | `faq-umum-data-aman` | All |
| faq18 | KkjCpdp | `faq-kkj-matrix-apa` | All |
| faq19 | KkjCpdp | `faq-kkj-lihat-saya` | All |
| faq20 | KkjCpdp | `faq-cpdp-apa-itu` | All |
| faq21 | KkjCpdp | `faq-kkj-upload-file` | All |
| faq22 | AdminData | `faq-admin-tambah-pekerja` | AdminHC |
| faq23 | AdminData | `faq-admin-import-pekerja` | AdminHC |
| faq24 | AdminData | `faq-admin-mapping-coach` | AdminHC |
| faq25 | AdminData | `faq-admin-audit-log` | AdminHC |
| faq26 | AdminData | `faq-admin-silabus-proton` | AdminHC |
| faq27 | AdminData | `faq-admin-analytics` | AdminHC |
| faq28 | AdminData | `faq-admin-monitoring-ujian` | AdminHC |
| faq29 | AdminData | `faq-admin-export-laporan` | AdminHC |

- [ ] **Step 7.1: Port FAQ kategori Akun (5)**

Edit `Services/GuideContentProvider.cs`, isi `Faqs` list dengan 5 item kategori Akun. Salin `Question` & `AnswerHtml` verbatim dari cshtml. Keywords dari `data-keywords` attr.

Contoh pattern:

```csharp
public static readonly IReadOnlyList<GuideFaqItem> Faqs = new List<GuideFaqItem>
{
    // ───── Akun & Login ─────
    new GuideFaqItem(
        Id: "faq-akun-login",
        Category: FaqCategory.Akun,
        Question: "Bagaimana cara login ke HC Portal?",
        AnswerHtml: "Buka halaman login di browser Anda, masukkan <strong>email</strong> dan <strong>password</strong> yang sudah terdaftar di sistem, lalu klik <strong>Login</strong>. Pastikan akun Anda sudah aktif dan diverifikasi oleh admin.",
        Roles: new[] { RoleGroup.All },
        Keywords: new[] { "login", "cara", "masuk", "akun" }
    ),
    // ... 4 lagi
};
```

Verify build.

- [ ] **Step 7.2: Port FAQ kategori Assessment (6)**

Append 6 item Assessment. Pattern sama.

Verify build.

- [ ] **Step 7.3: Port FAQ kategori CdpCoaching (4)**

Append 4 item CdpCoaching.

Verify build.

- [ ] **Step 7.4: Port FAQ kategori Umum (5)**

Append 5 item Umum.

Verify build.

- [ ] **Step 7.5: Port FAQ kategori KkjCpdp (4)**

Append 4 item KkjCpdp.

Verify build.

- [ ] **Step 7.6: Port FAQ kategori AdminData (8, AdminHC)**

Append 8 item AdminData dengan `Roles: [RoleGroup.AdminHC]`.

Verify build.

- [ ] **Step 7.7: Sanity count**

Run: `grep -c "new GuideFaqItem" Services/GuideContentProvider.cs`
Expected: `32`

---

## Task 8: Port PDF tutorials (4 item)

**Files:**
- Modify: `Services/GuideContentProvider.cs` — isi `Pdfs` list

**Reference source:** `Views/Home/GuideDetail.cshtml` line 79-180.

- [ ] **Step 8.1: Isi Pdfs list (4 PDF)**

```csharp
public static readonly IReadOnlyList<GuidePdfLink> Pdfs = new List<GuidePdfLink>
{
    new GuidePdfLink(
        Module: GuideModule.Cmp,
        Title: "Panduan Lengkap Assessment",
        Description: "Tutorial end-to-end mengerjakan assessment: dari mulai ujian, mengerjakan soal, submit, melihat hasil, hingga download sertifikat.",
        FilePath: "~/documents/guides/Panduan-Lengkap-Assessment.html",
        CardCssClass: "guide-tutorial-card--cmp",
        BtnColorClass: "btn-primary",
        Roles: new[] { RoleGroup.All }
    ),
    new GuidePdfLink(
        Module: GuideModule.Cdp,
        Title: "Panduan Lengkap Coaching Proton",
        Description: "Tutorial end-to-end untuk Coachee, Coach, dan Atasan: dari melihat silabus, upload evidence, approval, hingga Final Assessment.",
        FilePath: "~/documents/guides/Panduan-Lengkap-Coaching-Proton.html",
        CardCssClass: "guide-tutorial-card--cdp",
        BtnColorClass: "btn-success",
        Roles: new[] { RoleGroup.All }
    ),
    new GuidePdfLink(
        Module: GuideModule.Data,
        Title: "Panduan Buat Assessment & Input Soal",
        Description: "Tutorial lengkap membuat assessment, mengelola paket soal, dan mengimpor soal dari Excel — untuk Admin & HC.",
        FilePath: "~/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html",
        CardCssClass: "guide-tutorial-card--data",
        BtnColorClass: "btn-primary",
        Roles: new[] { RoleGroup.AdminHC }
    ),
    new GuidePdfLink(
        Module: GuideModule.Admin,
        Title: "Panduan Konfigurasi Active Directory",
        Description: "Tutorial lengkap konfigurasi Active Directory untuk integrasi autentikasi HC Portal.",
        FilePath: "~/documents/guides/ActiveDirectory-Guide.html",
        CardCssClass: "guide-tutorial-card--admin",
        BtnColorClass: "btn-danger",
        Roles: new[] { RoleGroup.AdminHC }
    ),
};
```

Verify build: `dotnet build` → success.

---

## Task 9: Update _ViewImports.cshtml

**Files:**
- Modify: `Views/_ViewImports.cshtml`

- [ ] **Step 9.1: Tambah `@using HcPortal.Models.Guide`**

Buka `Views/_ViewImports.cshtml`. Tambah baris:

```cshtml
@using HcPortal.Models.Guide
```

(Append setelah `@using` lain yang sudah ada.)

- [ ] **Step 9.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 10: Smoke test runtime (no visual change)

- [ ] **Step 10.1: Run app, akses Guide**

Run: `dotnet run`

Buka browser: `http://localhost:5277/Home/Guide`

Login as `admin@pertamina.com`.

Expected: halaman render persis sama seperti sebelum perubahan PR 1 (provider belum dipakai view). Tidak ada error 500.

- [ ] **Step 10.2: Akses GuideDetail per module**

Test URL berikut, semua harus render OK:
- `http://localhost:5277/Home/GuideDetail?module=cmp`
- `http://localhost:5277/Home/GuideDetail?module=cdp`
- `http://localhost:5277/Home/GuideDetail?module=account`
- `http://localhost:5277/Home/GuideDetail?module=data`
- `http://localhost:5277/Home/GuideDetail?module=admin`

Expected: semua render seperti before.

- [ ] **Step 10.3: Stop app**

Ctrl+C di terminal `dotnet run`.

---

## Task 11: Commit PR 1

- [ ] **Step 11.1: Stage files**

```bash
git add Models/Guide/ Services/GuideContentProvider.cs Services/GuideRoleAccess.cs Views/_ViewImports.cshtml
```

- [ ] **Step 11.2: Status verify**

Run: `git status`
Expected: 14 file new + 1 modified, no other unrelated.

- [ ] **Step 11.3: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat(guide): add GuideContentProvider + RoleAccess foundation

- Models/Guide/: enums (GuideModule, RoleGroup, FaqCategory),
  records (GuideStep, GuideItem, GuideFaqItem, GuidePdfLink),
  ViewModels (GuideViewModel, GuideDetailViewModel, GuideModuleCardVm)
- Services/GuideRoleAccess.cs: 6-RoleGroup → 10 Identity role mapping
- Services/GuideContentProvider.cs: static seed (27 items, 32 FAQs, 4 PDFs)
  ported verbatim dari Views/Home/Guide(Detail).cshtml
- Views/_ViewImports.cshtml: @using HcPortal.Models.Guide

Provider belum dipakai view (zero breaking change).
Refactor view di PR berikutnya.

Spec: docs/superpowers/specs/2026-05-20-panduan-bantuan-design.md

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 11.4: Verify commit**

Run: `git log -1 --stat`
Expected: 1 commit, 14+1 files changed.

---

# PHASE 2 — Refactor Views (PR 2)

Tujuan: view jadi loop ViewModel + partial. Konten dan tampilan visual identik dengan PR 1.

## Task 12: Partial _RoleBadge.cshtml

**Files:**
- Create: `Views/Shared/_Guide/_RoleBadge.cshtml`

- [ ] **Step 12.1: Create partial**

```cshtml
@model HcPortal.Models.Guide.RoleGroup[]
@using HcPortal.Services

@{
    var label = GuideRoleAccess.BadgeLabel(Model);
}

@if (!string.IsNullOrEmpty(label))
{
    <span class="guide-role-badge ms-1">@label</span>
}
```

- [ ] **Step 12.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 13: Partial _ModuleCard.cshtml

**Files:**
- Create: `Views/Shared/_Guide/_ModuleCard.cshtml`

- [ ] **Step 13.1: Create partial**

```cshtml
@model HcPortal.Models.Guide.GuideModuleCardVm
@{
    var moduleSlug = Model.Module.ToString().ToLowerInvariant();
}

<a asp-action="GuideDetail"
   asp-route-module="@moduleSlug"
   class="guide-module-card @Model.CardCssClass searchable-card text-decoration-none"
   data-keywords="@string.Join(" ", Model.Title.ToLowerInvariant().Split(' '))"
   data-aos="fade-up"
   role="article"
   aria-label="Panduan @Model.Title - @Model.ItemCount panduan tersedia">
    <div class="guide-card-header">
        <div class="guide-card-header-icon icon-@moduleSlug"><i class="bi @Model.IconCssClass" aria-hidden="true"></i></div>
        <div class="guide-card-header-text">
            <h5>
                @Model.Title
                <partial name="_Guide/_RoleBadge" model="Model.Roles" />
            </h5>
            <p>@Model.ItemCount panduan tersedia</p>
        </div>
        <i class="bi bi-chevron-right guide-card-chevron" aria-hidden="true"></i>
    </div>
</a>
```

- [ ] **Step 13.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 14: Partial _AccordionItem.cshtml

**Files:**
- Create: `Views/Shared/_Guide/_AccordionItem.cshtml`

- [ ] **Step 14.1: Create partial**

```cshtml
@model HcPortal.Models.Guide.GuideItem
@{
    var collapseId = $"collapse-{Model.Id}";
    var headingId  = $"heading-{Model.Id}";

    var stepVariant = Model.Module switch
    {
        HcPortal.Models.Guide.GuideModule.Cdp     => "step-variant-green",
        HcPortal.Models.Guide.GuideModule.Account => "step-variant-teal",
        HcPortal.Models.Guide.GuideModule.Data    => "step-variant-orange",
        HcPortal.Models.Guide.GuideModule.Admin   => "step-variant-blue",
        _ => ""
    };

    var btnVariant = Model.Module switch
    {
        HcPortal.Models.Guide.GuideModule.Cdp     => "btn-cdp",
        HcPortal.Models.Guide.GuideModule.Account => "btn-account",
        HcPortal.Models.Guide.GuideModule.Data    => "btn-data",
        HcPortal.Models.Guide.GuideModule.Admin   => "btn-admin",
        _ => ""
    };

    var keywordList = string.Join(" ", Model.Keywords).ToLowerInvariant();
}

<div class="guide-list-item accordion-item">
    <h2 class="accordion-header" id="@headingId">
        <button class="accordion-button collapsed guide-list-btn @btnVariant"
                type="button" data-bs-toggle="collapse"
                data-bs-target="#@collapseId" aria-expanded="false">
            @Model.Title
            <partial name="_Guide/_RoleBadge" model="Model.Roles" />
        </button>
    </h2>
    <div id="@collapseId" class="accordion-collapse collapse" data-bs-parent="">
        <div class="accordion-body guide-list-body" data-keywords="@keywordList">
            <ul class="guide-steps">
                @foreach (var step in Model.Steps)
                {
                    <li class="guide-step-item @stepVariant">
                        <div class="guide-step-badge">@step.Number</div>
                        <div class="guide-step-text">
                            <strong>@step.Title</strong>
                            <span>@Html.Raw(step.BodyHtml)</span>
                        </div>
                    </li>
                }
            </ul>
        </div>
    </div>
</div>
```

**Note:** `data-bs-parent=""` (empty) preserved — multi-open behavior per Q7 keputusan.

- [ ] **Step 14.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 15: Partial _FaqItem.cshtml

**Files:**
- Create: `Views/Shared/_Guide/_FaqItem.cshtml`

- [ ] **Step 15.1: Create partial**

```cshtml
@model HcPortal.Models.Guide.GuideFaqItem
@{
    var collapseId = $"collapse-{Model.Id}";
    var keywordList = string.Join(" ", Model.Keywords).ToLowerInvariant();
}

<div class="faq-item searchable-faq" data-keywords="@keywordList">
    <button class="faq-question" data-bs-toggle="collapse" data-bs-target="#@collapseId" aria-expanded="false">
        @Model.Question
        <i class="bi bi-chevron-down faq-chevron"></i>
    </button>
    <div id="@collapseId" class="collapse faq-answer">
        @Html.Raw(Model.AnswerHtml)
    </div>
</div>
```

- [ ] **Step 15.2: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 16: Refactor HomeController

**Files:**
- Modify: `Controllers/HomeController.cs:324-355`

- [ ] **Step 16.1: Update Guide() action**

Edit `Controllers/HomeController.cs`, replace `Guide()` method (line 324-331):

```csharp
public async Task<IActionResult> Guide()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var userRoles = await _userManager.GetRolesAsync(user);
    var userRole = userRoles.FirstOrDefault() ?? "User";

    var vm = new GuideViewModel(
        UserRole: userRole,
        ModuleCards: GuideContentProvider.GetModuleCards(userRole),
        FaqsByCategory: GuideContentProvider.GetFaqsByCategory(userRole)
    );

    ViewBag.UserRole = userRole; // backward-compat untuk skrip lain (kalau ada)
    return View(vm);
}
```

- [ ] **Step 16.2: Update GuideDetail() action**

Replace `GuideDetail(string module)` method (line 333-355):

```csharp
public async Task<IActionResult> GuideDetail(string module)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var userRoles = await _userManager.GetRolesAsync(user);
    var userRole = userRoles.FirstOrDefault() ?? "User";

    module = module?.ToLowerInvariant() ?? "";

    // Parse module string to enum
    GuideModule moduleEnum;
    string moduleTitle, moduleIcon, moduleBreadcrumb, moduleCategory;
    switch (module)
    {
        case "cmp":     moduleEnum = GuideModule.Cmp;     moduleTitle = "CMP — Competency Management Platform"; moduleIcon = "bi-journal-bookmark-fill"; moduleBreadcrumb = "CMP"; moduleCategory = "cmp"; break;
        case "cdp":     moduleEnum = GuideModule.Cdp;     moduleTitle = "CDP — Competency Development Platform"; moduleIcon = "bi-graph-up-arrow"; moduleBreadcrumb = "CDP"; moduleCategory = "cdp"; break;
        case "account": moduleEnum = GuideModule.Account; moduleTitle = "Akun & Profil"; moduleIcon = "bi-person-circle"; moduleBreadcrumb = "Akun"; moduleCategory = "account"; break;
        case "data":    moduleEnum = GuideModule.Data;    moduleTitle = "Kelola Data"; moduleIcon = "bi-database-fill"; moduleBreadcrumb = "Kelola Data"; moduleCategory = "data"; break;
        case "admin":   moduleEnum = GuideModule.Admin;   moduleTitle = "Admin Panel"; moduleIcon = "bi-gear-wide-connected"; moduleBreadcrumb = "Admin Panel"; moduleCategory = "admin"; break;
        default: return RedirectToAction("Guide");
    }

    // Role access guard
    var allModules = new[] { GuideModule.Data, GuideModule.Admin };
    if (allModules.Contains(moduleEnum) && !GuideRoleAccess.CanSee(userRole, new[] { RoleGroup.AdminHC }))
        return RedirectToAction("Guide");

    var vm = new GuideDetailViewModel(
        UserRole: userRole,
        Module: moduleEnum,
        ModuleTitle: moduleTitle,
        ModuleIcon: moduleIcon,
        ModuleBreadcrumb: moduleBreadcrumb,
        ModuleCategory: moduleCategory,
        Pdf: GuideContentProvider.GetPdf(moduleEnum, userRole),
        Items: GuideContentProvider.GetItems(moduleEnum, userRole)
    );

    ViewBag.UserRole = userRole;
    ViewBag.Module = module;
    return View(vm);
}
```

- [ ] **Step 16.3: Tambah using statements**

Di top `Controllers/HomeController.cs`, tambah:

```csharp
using HcPortal.Models.Guide;
using HcPortal.Services;
```

(kalau belum ada).

- [ ] **Step 16.4: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 17: Refactor Guide.cshtml

**Files:**
- Modify: `Views/Home/Guide.cshtml` (full rewrite)

- [ ] **Step 17.1: Backup file lama**

```bash
cp Views/Home/Guide.cshtml Views/Home/Guide.cshtml.bak
```

(Backup ini akan dihapus setelah PR 2 verify OK.)

- [ ] **Step 17.2: Replace Guide.cshtml dengan ViewModel-driven version**

Replace entire `Views/Home/Guide.cshtml` content dengan:

```cshtml
@model HcPortal.Models.Guide.GuideViewModel
@{
    ViewData["Title"] = "Panduan & Bantuan";
    var printDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

    // FAQ category labels (display order + icon)
    var faqCategoryDisplay = new[]
    {
        (HcPortal.Models.Guide.FaqCategory.Akun,        "Akun & Login",                  "bi-lock-fill",            "akun"),
        (HcPortal.Models.Guide.FaqCategory.Assessment,  "Assessment",                    "bi-pencil-square",        "assessment"),
        (HcPortal.Models.Guide.FaqCategory.CdpCoaching, "CDP & Coaching",                "bi-bar-chart-fill",       "cdp"),
        (HcPortal.Models.Guide.FaqCategory.Umum,        "Umum",                          "bi-question-circle-fill", "umum"),
        (HcPortal.Models.Guide.FaqCategory.KkjCpdp,     "KKJ & CPDP",                    "bi-file-earmark-text-fill", "kkj"),
        (HcPortal.Models.Guide.FaqCategory.AdminData,   "Admin & Kelola Data",           "bi-shield-fill-gear",     "admin")
    };
}

<link rel="stylesheet" href="~/css/guide.css" />

<a href="#guide-search" class="skip-link">Lompat ke pencarian</a>
<a href="#guide-modules" class="skip-link">Lompat ke modul</a>
<a href="#guide-faq" class="skip-link">Lompat ke FAQ</a>

<main role="main" id="main-content" data-print-date="@printDate">
<div class="guide-hero" data-aos="fade-down">
    <div class="guide-hero-content">
        <div class="row align-items-center">
            <div class="col-12">
                <p class="mb-2" style="opacity:.8; font-size:.9rem; font-weight:600; letter-spacing:.05em; text-transform:uppercase;">
                    <i class="bi bi-book me-1"></i> Pusat Bantuan
                </p>
                <h1 class="mb-2">Panduan & Bantuan</h1>
                <p class="mb-3">Pelajari cara menggunakan setiap fitur HC Portal secara lengkap, langkah demi langkah.</p>
                <span class="guide-role-badge">
                    <i class="bi bi-person-badge-fill"></i>
                    Anda login sebagai: <strong>@Model.UserRole</strong>
                </span>
            </div>
        </div>
    </div>
</div>

<nav role="navigation" aria-label="Breadcrumb navigasi" class="guide-breadcrumb mb-3" data-aos="fade-down">
    <ol class="breadcrumb">
        <li class="breadcrumb-item">
            <a asp-controller="Home" asp-action="Index" class="text-decoration-none text-muted">
                <i class="bi bi-house-door me-1" aria-hidden="true"></i>Beranda
            </a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Panduan</li>
    </ol>
</nav>

<div class="guide-search-wrapper" id="guide-search" data-aos="fade-up" data-aos-delay="100">
    <i class="bi bi-search guide-search-icon" aria-hidden="true"></i>
    <input type="text" id="guideSearchInput" class="guide-search-input"
           aria-label="Cari panduan"
           aria-describedby="search-hint keyboard-help"
           placeholder="Cari panduan... (contoh: assessment, coaching, IDP, upload)" autocomplete="off" />
    <span id="search-hint" class="sr-only">Ketik untuk mencari panduan dan FAQ</span>
    <div id="search-results" aria-live="polite" aria-atomic="true" class="sr-only"></div>
    <div id="keyboard-help" class="sr-only">Gunakan Tab untuk navigasi, Enter/Space untuk membuka, Escape untuk menutup pencarian</div>
</div>

<section aria-labelledby="modules-heading">
<div class="guide-section-heading" data-aos="fade-right" id="modules-heading">
    <div class="guide-section-icon"><i class="bi bi-grid-fill" aria-hidden="true"></i></div>
    <h2>Panduan Penggunaan Fitur</h2>
</div>

<div class="guide-card-grid" id="guide-modules" role="list">
    @foreach (var card in Model.ModuleCards)
    {
        <partial name="_Guide/_ModuleCard" model="card" />
    }
</div>
</section>

<div class="no-results-msg" id="noResultsMsg" role="status" aria-live="polite">
    <i class="bi bi-search fs-2 mb-2 d-block" aria-hidden="true"></i>
    Tidak ada panduan atau FAQ yang cocok dengan pencarian Anda.
</div>

<section aria-labelledby="faq-heading" class="guide-faq-section" id="guide-faq" data-aos="fade-up">
    <div class="guide-section-heading mb-2">
        <div class="guide-section-icon"><i class="bi bi-chat-square-quote-fill" aria-hidden="true"></i></div>
        <h2 id="faq-heading">Frequently Asked Questions</h2>
    </div>
    <p class="faq-subtitle">Pertanyaan yang sering diajukan. Klik pertanyaan untuk melihat jawabannya.</p>

    <div class="d-flex justify-content-end mb-3">
        <button type="button" class="btn btn-outline-secondary btn-sm" id="faqToggleAll" onclick="toggleAllFaq()">
            <i class="bi bi-arrows-expand me-1"></i> Buka Semua
        </button>
    </div>

    @foreach (var (cat, label, icon, slug) in faqCategoryDisplay)
    {
        if (!Model.FaqsByCategory.TryGetValue(cat, out var faqs) || faqs.Count == 0)
            continue;

        <div class="faq-category" id="faqGroup-@slug">
            <div class="faq-category-title"><i class="bi @icon me-1"></i> @label</div>
            @foreach (var faq in faqs)
            {
                <partial name="_Guide/_FaqItem" model="faq" />
            }
        </div>
    }
</section>

<button type="button" class="guide-back-to-top" id="backToTopBtn" aria-label="Kembali ke atas" title="Kembali ke atas">
    <i class="bi bi-chevron-up"></i>
</button>
</main>

@section Scripts {
<script>
// Script section identical dengan original Guide.cshtml line 540-758
// (back-to-top, toggleAllFaq, search, highlight) — SALIN VERBATIM dari Guide.cshtml.bak
// ... [salin dari backup verbatim, tidak ada perubahan logic]
</script>
}
```

**Penting Step 17.2:** untuk `<script>` section, salin verbatim dari `Guide.cshtml.bak` line 540-758 (logic tidak berubah).

- [ ] **Step 17.3: Build verify**

Run: `dotnet build`
Expected: success, no Razor compile error.

---

## Task 18: Refactor GuideDetail.cshtml

**Files:**
- Modify: `Views/Home/GuideDetail.cshtml` (full rewrite)

- [ ] **Step 18.1: Backup file lama**

```bash
cp Views/Home/GuideDetail.cshtml Views/Home/GuideDetail.cshtml.bak
```

- [ ] **Step 18.2: Replace GuideDetail.cshtml**

Replace entire `Views/Home/GuideDetail.cshtml` content dengan:

```cshtml
@model HcPortal.Models.Guide.GuideDetailViewModel

<link rel="stylesheet" href="~/css/guide.css" />

<main role="main" id="main-content">
<nav role="navigation" aria-label="Breadcrumb navigasi" class="guide-breadcrumb mb-3" data-aos="fade-down">
    <ol class="breadcrumb">
        <li class="breadcrumb-item">
            <a asp-controller="Home" asp-action="Index" class="text-decoration-none text-muted">
                <i class="bi bi-house-door me-1" aria-hidden="true"></i>Beranda
            </a>
        </li>
        <li class="breadcrumb-item">
            <a asp-controller="Home" asp-action="Guide" class="text-decoration-none text-muted">Panduan</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">@Model.ModuleBreadcrumb</li>
    </ol>
</nav>

<div class="guide-detail-header guide-header-@Model.ModuleCategory" data-aos="fade-up" data-aos-delay="100">
    <div class="guide-detail-header-icon">
        <i class="bi @Model.ModuleIcon"></i>
    </div>
    <div class="guide-detail-header-text">
        <h2 class="mb-1">@Model.ModuleTitle</h2>
        <p class="mb-0 text-white-50">Panduan langkah demi langkah penggunaan fitur</p>
    </div>
</div>

@if (Model.Pdf != null)
{
    var outlineBtn = Model.Pdf.BtnColorClass.Replace("btn-", "btn-outline-");
    <div class="guide-tutorial-card @Model.Pdf.CardCssClass" data-aos="fade-up" data-aos-delay="150">
        <div class="guide-tutorial-inner d-flex align-items-center justify-content-between flex-wrap gap-3">
            <div class="d-flex align-items-center gap-3">
                <div class="guide-tutorial-icon">
                    <i class="bi bi-file-earmark-pdf-fill fs-4"></i>
                </div>
                <div>
                    <h5 class="guide-tutorial-title mb-1 fw-bold">@Model.Pdf.Title</h5>
                    <p class="mb-0 text-muted small">@Model.Pdf.Description</p>
                </div>
            </div>
            <div class="d-flex gap-2">
                <a href="@Url.Content(Model.Pdf.FilePath)" target="_blank" class="btn @outlineBtn btn-sm">
                    <i class="bi bi-eye me-1"></i> Lihat
                </a>
                <a href="@Url.Content(Model.Pdf.FilePath)" download class="btn @Model.Pdf.BtnColorClass btn-sm">
                    <i class="bi bi-download me-1"></i> Download
                </a>
            </div>
        </div>
    </div>
}

<div class="guide-detail-content" data-aos="fade-up" data-aos-delay="200">
    <div class="guide-list-container accordion" id="guideAccordion">
        @foreach (var item in Model.Items)
        {
            <partial name="_Guide/_AccordionItem" model="item" />
        }
    </div>

    <div class="text-center mt-4 mb-3">
        <a asp-controller="Home" asp-action="Guide" class="btn btn-outline-secondary btn-sm">
            <i class="bi bi-arrow-left me-1"></i> Kembali ke Panduan
        </a>
    </div>
</div>

<button type="button" class="guide-back-to-top" id="backToTopBtn" aria-label="Kembali ke atas" title="Kembali ke atas">
    <i class="bi bi-chevron-up"></i>
</button>
</main>

@section Scripts {
<script>
// Salin verbatim dari GuideDetail.cshtml.bak line 706-719 (back-to-top script)
(function () {
    var btn = document.getElementById('backToTopBtn');
    if (!btn) return;
    window.addEventListener('scroll', function () {
        btn.classList.toggle('visible', window.scrollY > 300);
    });
    btn.addEventListener('click', function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
})();
</script>
}
```

- [ ] **Step 18.3: Build verify**

Run: `dotnet build`
Expected: success.

---

## Task 19: Manual UAT visual parity

- [ ] **Step 19.1: Run app**

Run: `dotnet run`

- [ ] **Step 19.2: Login as Admin**

Buka `http://localhost:5277/Account/Login`
Login dengan `admin@pertamina.com` (lihat memory `reference_dev_credentials.md`).

- [ ] **Step 19.3: Visual cek tiap halaman**

Buka tiap URL, bandingkan side-by-side dengan screenshot dari `.bak` files atau git history sebelum PR 2:

- `/Home/Guide` — hub
- `/Home/GuideDetail?module=cmp`
- `/Home/GuideDetail?module=cdp`
- `/Home/GuideDetail?module=account`
- `/Home/GuideDetail?module=data`
- `/Home/GuideDetail?module=admin`

Cek visual identik:
- Layout cards
- FAQ accordion render OK
- Step list nomor + text
- Badge "Admin & HC" muncul di tempat yang sama
- Color variant per module benar (cdp=green, account=teal, data=orange, admin=blue)
- Search bar berfungsi
- Toggle "Buka Semua" berfungsi
- Multi-open accordion behavior (klik 2 item → 2 terbuka)
- Back-to-top button muncul saat scroll

- [ ] **Step 19.4: Test as Coachee (role berbeda)**

Logout, login as user dengan role Coachee (atau impersonate via Admin Panel).

Cek `/Home/Guide`:
- Tidak ada module card "Kelola Data" / "Admin Panel"
- FAQ kategori "Admin & Kelola Data" tidak muncul
- Item accordion CDP/CMP yang tagged AdminHC tidak muncul

Cek `/Home/GuideDetail?module=data` → redirect ke `/Home/Guide` (role guard jalan).

- [ ] **Step 19.5: Stop app**

Ctrl+C.

---

## Task 20: Playwright visual regression

**Files:**
- Create: `tests/playwright/guide-parity.spec.ts`
- Create: `tests/playwright/playwright.config.ts` (kalau belum ada)
- Create: `tests/playwright/package.json` (kalau belum ada)

**Note:** Cek apakah Playwright setup sudah ada di repo. Kalau belum, init dulu.

- [ ] **Step 20.1: Cek Playwright setup**

Run: `ls tests/playwright/ 2>/dev/null || echo "not setup"`

Kalau "not setup", init:
```bash
mkdir -p tests/playwright
cd tests/playwright
npm init -y
npm install -D @playwright/test
npx playwright install chromium
```

- [ ] **Step 20.2: Buat config**

Create `tests/playwright/playwright.config.ts`:

```typescript
import { defineConfig } from '@playwright/test';

export default defineConfig({
    testDir: '.',
    use: {
        baseURL: 'http://localhost:5277',
        screenshot: 'only-on-failure',
        trace: 'retain-on-failure',
    },
    expect: {
        toHaveScreenshot: {
            maxDiffPixels: 50,  // toleransi minor font rendering diff
        },
    },
});
```

- [ ] **Step 20.3: Buat spec file**

Create `tests/playwright/guide-parity.spec.ts`:

```typescript
import { test, expect, Page } from '@playwright/test';

async function login(page: Page, email: string, password: string) {
    await page.goto('/Account/Login');
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="Password"]', password);
    await page.click('button[type="submit"]');
    await page.waitForURL('**/Home/**');
}

test.describe('Guide page visual parity', () => {
    test('Guide hub — Admin', async ({ page }) => {
        await login(page, 'admin@pertamina.com', 'AdminPassword123!'); // password sesuai dev
        await page.goto('/Home/Guide');
        await page.waitForLoadState('networkidle');
        await expect(page).toHaveScreenshot('guide-hub-admin.png', { fullPage: true });
    });

    test('Guide hub — Coachee', async ({ page }) => {
        await login(page, 'coachee@pertamina.com', 'CoacheePassword123!'); // ganti sesuai dev
        await page.goto('/Home/Guide');
        await page.waitForLoadState('networkidle');
        await expect(page).toHaveScreenshot('guide-hub-coachee.png', { fullPage: true });
    });

    for (const module of ['cmp', 'cdp', 'account']) {
        test(`Guide detail ${module} — Admin`, async ({ page }) => {
            await login(page, 'admin@pertamina.com', 'AdminPassword123!');
            await page.goto(`/Home/GuideDetail?module=${module}`);
            await page.waitForLoadState('networkidle');
            await expect(page).toHaveScreenshot(`guide-detail-${module}-admin.png`, { fullPage: true });
        });
    }

    for (const module of ['data', 'admin']) {
        test(`Guide detail ${module} — Admin`, async ({ page }) => {
            await login(page, 'admin@pertamina.com', 'AdminPassword123!');
            await page.goto(`/Home/GuideDetail?module=${module}`);
            await page.waitForLoadState('networkidle');
            await expect(page).toHaveScreenshot(`guide-detail-${module}-admin.png`, { fullPage: true });
        });
    }
});
```

- [ ] **Step 20.4: Capture baseline dari PR 1 state**

PENTING: baseline screenshot harus dari PR 1 state (before PR 2 view changes), supaya PR 2 di-compare.

Cara: checkout PR 1 commit, run app, generate baseline:

```bash
# Simpan PR 2 work-in-progress dulu
git stash

# Checkout PR 1 commit
git checkout <PR1-commit-sha>
dotnet run &
sleep 5

# Generate baseline screenshots
cd tests/playwright
npx playwright test --update-snapshots
cd ../..

# Kill app
pkill -f "dotnet run"

# Kembali ke PR 2 work
git checkout main  # or wherever PR 2 work was
git stash pop
```

Commit baseline screenshots ke git.

- [ ] **Step 20.5: Run Playwright diff**

```bash
dotnet run &
sleep 5
cd tests/playwright
npx playwright test
cd ../..
pkill -f "dotnet run"
```

Expected: all tests PASS. Kalau ada diff > toleransi 50px, inspect `test-results/` untuk diff image dan adjust view supaya match.

- [ ] **Step 20.6: Cleanup .bak files**

Setelah PASS:

```bash
rm Views/Home/Guide.cshtml.bak Views/Home/GuideDetail.cshtml.bak
```

---

## Task 21: Commit PR 2

- [ ] **Step 21.1: Stage**

```bash
git add Views/Shared/_Guide/ Views/Home/Guide.cshtml Views/Home/GuideDetail.cshtml Controllers/HomeController.cs tests/playwright/
```

- [ ] **Step 21.2: Commit**

```bash
git commit -m "$(cat <<'EOF'
refactor(guide): loop ViewModel + partial, slug-based IDs

Views/Home/Guide.cshtml + GuideDetail.cshtml refactored:
- Loop dari GuideViewModel / GuideDetailViewModel
- Partial _AccordionItem, _FaqItem, _RoleBadge, _ModuleCard (DRY)
- Slug-based collapse IDs (cmp-library-kkj vs cmpHeading1)
- Hardcoded cmpCount/cdpCount/dst dihapus → auto via ViewModel.Items.Count
- Komentar "CDP 2-4 removed" dihapus
- Behavior preserved: multi-open accordion (data-bs-parent=""), search,
  toggle all, back-to-top, badge layout

HomeController.Guide/GuideDetail pass ViewModel + retain ViewBag untuk
backward-compat skrip lain.

Visual parity verified via Playwright (tests/playwright/guide-parity.spec.ts).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

# PHASE 3 — Audit Konten (PR 3, multi-commit per module)

Tujuan: fix role tagging salah, split accordion gabungan, tambah 8 item baru, link orphan PDF, update FAQ.

## Task 22: Role tagging fix (3 items)

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 22.1: Update `cmp-monitoring-records-tim`**

Find di `Items` list:
```csharp
Id: "cmp-monitoring-records-tim",
...
Roles: new[] { RoleGroup.AdminHC },
```

Ganti ke:
```csharp
Roles: new[] { RoleGroup.Atasan, RoleGroup.AdminHC },
```

- [ ] **Step 22.2: Update `cdp-approve-deliverable`**

Ganti `Roles: new[] { RoleGroup.AdminHC }` → `Roles: new[] { RoleGroup.Atasan, RoleGroup.AdminHC }`.

- [ ] **Step 22.3: Update `cdp-coaching-proton-dashboard`**

Ganti `Roles: new[] { RoleGroup.AdminHC }` → `Roles: new[] { RoleGroup.Manager, RoleGroup.AdminHC }`.

- [ ] **Step 22.4: Build verify**

Run: `dotnet build`
Expected: success.

- [ ] **Step 22.5: Manual UAT**

`dotnet run`. Login as `Section Head` (perlu user dengan role itu, atau impersonate dari Admin).
- Buka `/Home/GuideDetail?module=cmp` → "Monitoring Records Tim" harus muncul (sebelumnya tidak)
- Buka `/Home/GuideDetail?module=cdp` → "Approve / Reject Deliverable" harus muncul (sebelumnya tidak)

Login as `Manager`:
- Buka `/Home/GuideDetail?module=cdp` → "Coaching Proton Dashboard" harus muncul (sebelumnya tidak)

Login as `Coachee`:
- Item-item di atas tetap tidak muncul (regression check).

- [ ] **Step 22.6: Commit fix tagging**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
fix(guide): role tagging — reviewer chain & manager monitoring

- cmp-monitoring-records-tim: AdminHC → Atasan + AdminHC
  (label sudah "Atasan / HC" tapi gate cuma AdminHC)
- cdp-approve-deliverable: AdminHC → Atasan + AdminHC
  (FAQ12 menjelaskan chain SrSV → SectionHead → HC, Atasan harus
   dapat panduan)
- cdp-coaching-proton-dashboard: AdminHC → Manager + AdminHC
  (Manager butuh read-only monitoring)

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 23: Split Account accordion gabungan

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 23.1: Split `account-profile-view-edit` jadi 2**

Find di `Items` list `Id: "account-profile-view-edit"`. Replace dengan 2 item:

```csharp
new GuideItem(
    Id: "account-view-profile",
    Module: GuideModule.Account,
    Title: "Cara Melihat Profil",
    Roles: new[] { RoleGroup.All },
    Steps: new[]
    {
        new GuideStep(1, "Buka Profil", "Klik avatar profil Anda di pojok kanan atas layar, lalu pilih <b>My Profile</b>."),
        new GuideStep(2, "Review Data Personal", "Halaman profil menampilkan informasi: nama, NIP, email, jabatan, unit kerja, dan role. Pastikan semua data sudah benar.")
    },
    Keywords: new[] { "profil", "lihat", "my profile", "view" }
),
new GuideItem(
    Id: "account-edit-profile",
    Module: GuideModule.Account,
    Title: "Cara Edit Profil",
    Roles: new[] { RoleGroup.All },
    Steps: new[]
    {
        new GuideStep(1, "Masuk Halaman Profil", "Klik avatar profil → pilih <b>My Profile</b>."),
        new GuideStep(2, "Klik Tombol Edit", "Klik tombol pensil/Edit di halaman profil."),
        new GuideStep(3, "Simpan Perubahan", "Edit informasi yang diperlukan, lalu tekan <b>Simpan</b>. Beberapa field (mis: NIP, role) tidak bisa diedit user — hubungi Admin/HC kalau perlu update.")
    },
    Keywords: new[] { "edit", "ubah", "profil", "nama" }
),
```

- [ ] **Step 23.2: Split `account-logout-role-system` jadi 2**

Find `Id: "account-logout-role-system"`. Replace dengan:

```csharp
new GuideItem(
    Id: "account-logout",
    Module: GuideModule.Account,
    Title: "Cara Logout",
    Roles: new[] { RoleGroup.All },
    Steps: new[]
    {
        new GuideStep(1, "Buka Menu Profil", "Klik avatar profil Anda di pojok kanan atas navbar."),
        new GuideStep(2, "Pilih Logout", "Pilih opsi <b>Logout</b> untuk keluar dari sistem dengan aman. Anda akan diarahkan kembali ke halaman login.")
    },
    Keywords: new[] { "logout", "keluar", "sign out" }
),
new GuideItem(
    Id: "account-role-system",
    Module: GuideModule.Account,
    Title: "Memahami Role System",
    Roles: new[] { RoleGroup.All },
    Steps: new[]
    {
        new GuideStep(1, "Lihat Role di Navbar", "Role Anda ditampilkan di navbar kanan atas sebagai badge (mis: <b>HC</b>, <b>Coachee</b>, <b>Coach</b>)."),
        new GuideStep(2, "Pahami 6 Level Akses", "Sistem punya 6 level: <b>1</b> Admin, <b>2</b> HC, <b>3</b> Direktur/VP/Manager, <b>4</b> Section Head/Sr Supervisor, <b>5</b> Coach/Supervisor, <b>6</b> Coachee. Setiap level punya akses menu berbeda."),
        new GuideStep(3, "Minta Update Role", "Kalau role tidak sesuai jabatan Anda, hubungi <b>Admin atau HC</b> untuk update via Admin Panel → Kelola Pekerja.")
    },
    Keywords: new[] { "role", "level", "akses", "section head", "supervisor", "manager", "vp", "direktur" }
),
```

- [ ] **Step 23.3: Build verify**

Run: `dotnet build`
Expected: success.

- [ ] **Step 23.4: Manual UAT**

`dotnet run`. Login any user. Buka `/Home/GuideDetail?module=account`.
Expected: 6 accordion item (sebelumnya 4): Login, View Profil, Edit Profil, Change Password, Logout, Role System.

- [ ] **Step 23.5: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): split Account accordion gabungan jadi 4 item terpisah

- account-profile-view-edit → account-view-profile + account-edit-profile
- account-logout-role-system → account-logout + account-role-system

Account module: 4 → 6 accordion item.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 24: Tambah CMP items baru (2)

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 24.1: Tambah `cmp-pre-post-test`**

Append ke `Items` list (di section CMP):

```csharp
new GuideItem(
    Id: "cmp-pre-post-test",
    Module: GuideModule.Cmp,
    Title: "Cara Mengerjakan Pre-Post Test",
    Roles: new[] { RoleGroup.All },
    Steps: new[]
    {
        new GuideStep(1, "Buka Tab Assessment", "Di menu CMP, buka <b>Assessment</b>. Pre-Test akan tampil sebelum jadwal pelatihan dimulai."),
        new GuideStep(2, "Kerjakan Pre-Test", "Klik <b>Mulai Pre-Test</b>. Kerjakan soal seperti assessment biasa — tidak ada batas pengulangan, jawab sesuai pemahaman awal Anda."),
        new GuideStep(3, "Ikuti Pelatihan", "Setelah Pre-Test selesai, ikuti program pelatihan sesuai jadwal yang ditentukan."),
        new GuideStep(4, "Kerjakan Post-Test", "Setelah pelatihan selesai, buka kembali tab Assessment dan klik <b>Mulai Post-Test</b>. Sistem akan otomatis menghitung <b>Gain Score</b> (selisih Pre vs Post).")
    },
    Keywords: new[] { "pre", "post", "pretest", "posttest", "gain score", "test" }
),
```

- [ ] **Step 24.2: Tambah `cmp-monitoring-manager`**

```csharp
new GuideItem(
    Id: "cmp-monitoring-manager",
    Module: GuideModule.Cmp,
    Title: "Monitoring Compliance Unit/Section",
    Roles: new[] { RoleGroup.Manager, RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Buka Analytics Dashboard", "Di menu CMP, klik <b>Analytics Dashboard</b>."),
        new GuideStep(2, "Filter per Unit/Section", "Gunakan filter Unit, Section, atau Kategori Assessment untuk fokus ke scope yang Anda monitor."),
        new GuideStep(3, "Review Compliance Chart", "Lihat persentase kepatuhan assessment, distribusi nilai, dan daftar pekerja yang belum complete assessment wajib."),
        new GuideStep(4, "Export Laporan", "Klik <b>Export Excel</b> untuk download laporan compliance untuk meeting unit/section.")
    },
    Keywords: new[] { "monitoring", "compliance", "unit", "section", "dashboard", "manager" }
),
```

- [ ] **Step 24.3: Build verify + manual UAT**

Run: `dotnet build`. `dotnet run`.

Test login:
- Coachee → `/Home/GuideDetail?module=cmp` harus muncul "Pre-Post Test" (tidak muncul "Monitoring Manager")
- Manager → muncul "Monitoring Manager" (tidak muncul "Pre-Post Test" karena role Coachee)

Wait — Manager juga butuh lihat "Pre-Post Test" (dia juga pekerja yang assessment)? Cek `Roles: [All]` → yes Manager lihat juga (All include semua). OK consistent.

- [ ] **Step 24.4: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): tambah CMP items — Pre-Post Test + Monitoring Manager

- cmp-pre-post-test (All): step-by-step Pre/Post Test + Gain Score
- cmp-monitoring-manager (Manager + AdminHC): dashboard compliance per
  unit/section, export laporan

CMP module: 4 → 6 accordion item.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 25: Tambah CDP items baru (5)

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 25.1: Tambah `cdp-upload-evidence`**

Append ke section CDP:

```csharp
new GuideItem(
    Id: "cdp-upload-evidence",
    Module: GuideModule.Cdp,
    Title: "Cara Upload Evidence Deliverable",
    Roles: new[] { RoleGroup.Coachee },
    Steps: new[]
    {
        new GuideStep(1, "Buka Deliverable Saya", "Di menu CDP, klik <b>Deliverable</b>. Pilih deliverable yang sedang dikerjakan."),
        new GuideStep(2, "Siapkan File Evidence", "Format yang didukung: PDF, DOCX, XLSX, JPG, PNG. Maksimal ukuran file sesuai konfigurasi (umumnya 10 MB)."),
        new GuideStep(3, "Klik Upload Evidence", "Klik tombol <b>Upload Evidence</b>, pilih file, tambah catatan singkat (opsional)."),
        new GuideStep(4, "Submit untuk Review", "Klik <b>Submit</b>. Status berubah jadi <i>Pending Approval</i>. Notifikasi akan dikirim ke reviewer chain (Sr Supervisor → Section Head → HC).")
    },
    Keywords: new[] { "upload", "evidence", "deliverable", "coachee", "submit" }
),
```

- [ ] **Step 25.2: Tambah `cdp-coaching-session`**

```csharp
new GuideItem(
    Id: "cdp-coaching-session",
    Module: GuideModule.Cdp,
    Title: "Cara Catat Coaching Session",
    Roles: new[] { RoleGroup.Coach, RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Buka Daftar Coachee", "Di menu CDP, buka <b>My Coachee</b>. Pilih coachee yang sudah sesi coaching."),
        new GuideStep(2, "Tambah Coaching Session", "Klik <b>+ Tambah Session</b>. Isi tanggal, durasi, dan topik yang dibahas."),
        new GuideStep(3, "Catat Outcome", "Isi outcome diskusi: insight yang dipelajari, action item, dan link ke deliverable yang dibahas (kalau ada)."),
        new GuideStep(4, "Save", "Klik <b>Save</b>. Histori session ini akan muncul di Histori Proton coachee.")
    },
    Keywords: new[] { "coaching", "session", "catat", "log", "coach" }
),
```

- [ ] **Step 25.3: Tambah `cdp-reviewer-chain`**

```csharp
new GuideItem(
    Id: "cdp-reviewer-chain",
    Module: GuideModule.Cdp,
    Title: "Memahami Alur Reviewer (Sr Supervisor → Section Head → HC)",
    Roles: new[] { RoleGroup.Atasan, RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Notifikasi Evidence Masuk", "Saat coachee submit evidence, notifikasi muncul di bell icon. Pertama-tama review oleh <b>Sr Supervisor</b>."),
        new GuideStep(2, "Sr Supervisor Review", "Sr Supervisor buka deliverable → cek evidence → klik <b>Approve</b> atau <b>Reject</b> dengan catatan."),
        new GuideStep(3, "Section Head Review", "Setelah Sr Supervisor approve, naik ke <b>Section Head</b>. Section Head review final dari sisi unit."),
        new GuideStep(4, "HC Review", "Terakhir review oleh <b>HC</b>. HC verifikasi kelengkapan dan compliance program."),
        new GuideStep(5, "Reject Reset Chain", "Kalau salah satu reviewer reject, seluruh chain di-reset. Coachee harus upload evidence baru, mulai dari Sr Supervisor lagi.")
    },
    Keywords: new[] { "reviewer", "chain", "approval", "sr supervisor", "section head", "hc", "alur" }
),
```

- [ ] **Step 25.4: Tambah `cdp-final-assessment`**

```csharp
new GuideItem(
    Id: "cdp-final-assessment",
    Module: GuideModule.Cdp,
    Title: "Cara Submit Final Assessment Proton",
    Roles: new[] { RoleGroup.Coachee, RoleGroup.Coach },
    Steps: new[]
    {
        new GuideStep(1, "Pastikan Semua Deliverable Approved", "Final Assessment hanya bisa di-submit kalau semua deliverable di periode Proton sudah <i>Approved</i>."),
        new GuideStep(2, "Buka Final Assessment", "Di menu CDP, klik <b>Final Assessment</b>. Daftar kompetensi akan muncul beserta target level."),
        new GuideStep(3, "Isi Self-Assessment (Coachee)", "Coachee isi nilai self-assessment per kompetensi. Tambah refleksi pembelajaran selama periode."),
        new GuideStep(4, "Validasi Coach", "Coach validasi nilai coachee, koreksi kalau perlu, tambah komentar."),
        new GuideStep(5, "Submit Final", "Coach klik <b>Submit Final</b>. Hasil masuk ke Histori Proton dan tidak bisa diedit lagi.")
    },
    Keywords: new[] { "final", "assessment", "proton", "submit", "coachee", "coach" }
),
```

- [ ] **Step 25.5: Tambah `cdp-bottleneck-report`**

```csharp
new GuideItem(
    Id: "cdp-bottleneck-report",
    Module: GuideModule.Cdp,
    Title: "Cara Lihat Bottleneck Report",
    Roles: new[] { RoleGroup.Manager, RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Buka Coaching Proton Dashboard", "Di menu CDP, klik <b>Coaching Proton Dashboard</b>."),
        new GuideStep(2, "Pilih Tab Bottleneck", "Klik tab <b>Bottleneck Report</b>. Tabel menampilkan deliverable yang stuck (lama tidak progress) per coachee/unit."),
        new GuideStep(3, "Filter & Drill Down", "Filter berdasarkan section, status (Pending Approval > X hari, Rejected, dll), atau coach. Klik baris untuk lihat detail histori deliverable."),
        new GuideStep(4, "Eskalasi", "Identifikasi akar bottleneck (reviewer lambat, coachee pasif, scope unclear) → eskalasi ke pihak terkait via internal comms.")
    },
    Keywords: new[] { "bottleneck", "report", "stuck", "delay", "deliverable", "manager" }
),
```

- [ ] **Step 25.6: Build + UAT**

`dotnet build`. `dotnet run`.

Test login:
- Coachee → CDP harus muncul `cdp-upload-evidence` + `cdp-final-assessment`
- Coach → CDP muncul `cdp-coaching-session` + `cdp-final-assessment`
- Section Head/Sr Supervisor → CDP muncul `cdp-approve-deliverable` (sudah dari Task 22) + `cdp-reviewer-chain`
- Manager → CDP muncul `cdp-coaching-proton-dashboard` (Task 22) + `cdp-bottleneck-report`

- [ ] **Step 25.7: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): tambah CDP items — coverage Coachee/Coach/Atasan/Manager

- cdp-upload-evidence (Coachee): submit evidence step-by-step
- cdp-coaching-session (Coach + AdminHC): catat sesi coaching
- cdp-reviewer-chain (Atasan + AdminHC): alur SrSV → SectionHead → HC
- cdp-final-assessment (Coachee + Coach): self-assessment + validasi
- cdp-bottleneck-report (Manager + AdminHC): identify stuck deliverable

CDP module: 5 → 10 accordion item.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 26: Tambah Admin item baru (1)

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 26.1: Tambah `admin-renewal-management`**

Append ke section Admin:

```csharp
new GuideItem(
    Id: "admin-renewal-management",
    Module: GuideModule.Admin,
    Title: "Cara Kelola Renewal Sertifikat",
    Roles: new[] { RoleGroup.AdminHC },
    Steps: new[]
    {
        new GuideStep(1, "Buka Renewal Management", "Pilih <b>Admin Panel → Renewal Management</b>. Halaman menampilkan daftar sertifikat assessment yang mendekati masa kadaluarsa."),
        new GuideStep(2, "Filter Sertifikat", "Filter berdasarkan rentang tanggal expired (mis: 30 / 60 / 90 hari ke depan), kategori assessment, atau unit."),
        new GuideStep(3, "Buat Jadwal Renewal", "Pilih sertifikat → klik <b>Schedule Renewal</b>. Sistem otomatis create assessment baru dengan paket soal sama, assign ke pekerja terkait."),
        new GuideStep(4, "Notifikasi Pekerja", "Pekerja akan dapat notifikasi assessment renewal. Setelah lulus, sertifikat baru terbit dan masa berlaku otomatis di-update.")
    },
    Keywords: new[] { "renewal", "sertifikat", "expired", "kadaluarsa", "perpanjangan" }
),
```

- [ ] **Step 26.2: Build + UAT**

`dotnet build`. Login as Admin → `/Home/GuideDetail?module=admin` → harus muncul item baru "Cara Kelola Renewal Sertifikat".

- [ ] **Step 26.3: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): tambah Admin item — Renewal Sertifikat

admin-renewal-management (AdminHC): kelola sertifikat yang expired,
schedule renewal assessment, notifikasi pekerja.

Admin module: 11 → 12 accordion item.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 27: Link orphan PDF

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 27.1: Tambah PDF entry**

Append ke `Pdfs` list:

```csharp
new GuidePdfLink(
    Module: GuideModule.Account,
    Title: "Panduan Penggunaan Website HC Portal KPB",
    Description: "Tutorial umum penggunaan HC Portal: navigasi, fitur dasar, dan tips & trik untuk semua role.",
    FilePath: "~/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html",
    CardCssClass: "guide-tutorial-card--account",
    BtnColorClass: "btn-info",
    Roles: new[] { RoleGroup.All }
),
```

**Note:** Provider `GetPdf(module, userRole)` returns `.FirstOrDefault()`. Module Account sebelumnya tidak punya PDF → ini PDF pertama Account. OK.

- [ ] **Step 27.2: Cek CSS class `guide-tutorial-card--account` ada**

Run: `grep -c "guide-tutorial-card--account" wwwroot/css/guide.css`

Kalau 0, perlu tambah ke CSS:

```css
/* Tambah di wwwroot/css/guide.css */
.guide-tutorial-card--account {
    border-left: 4px solid var(--account-accent, #20c997);
}
```

(Adjust color sesuai palette CSS existing untuk module Account.)

- [ ] **Step 27.3: Build + UAT**

`dotnet build`. `dotnet run`. Login any user. Buka `/Home/GuideDetail?module=account`.
Expected: PDF card "Panduan Penggunaan Website HC Portal KPB" muncul di atas accordion.

- [ ] **Step 27.4: Commit**

```bash
git add Services/GuideContentProvider.cs wwwroot/css/guide.css
git commit -m "$(cat <<'EOF'
feat(guide): link orphan PDF Panduan Penggunaan Website ke Account

Orphan PDF wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html
sebelumnya tidak ter-link dari Guide page. Link ke module Account
dengan Roles=All.

Tambah CSS guide-tutorial-card--account variant.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 28: FAQ update

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 28.1: Update FAQ12 jawaban + cross-link**

Find `Id: "faq-cdp-approve-deliverable"`. Replace `AnswerHtml`:

```csharp
AnswerHtml: "Deliverable melewati <strong>rantai approval bertingkat</strong>: setelah coachee upload evidence, deliverable di-review oleh <strong>Sr. Supervisor</strong> → <strong>Section Head</strong> → <strong>HC Review</strong>. Jika salah satu reviewer menolak, <em>seluruh rantai approval di-reset</em> dan coachee harus upload ulang evidence. Mapping coach-coachee dikelola oleh Admin/HC. Lihat panduan lengkap di <a href=\"@Url.Action(\"GuideDetail\", \"Home\", new { module = \"cdp\" })#collapse-cdp-reviewer-chain\"><strong>CDP → Memahami Alur Reviewer</strong></a>.",
```

**Note:** Razor di seed `string` literal tidak bisa pakai `@Url.Action`. Pakai relative URL string biasa:

```csharp
AnswerHtml: "Deliverable melewati <strong>rantai approval bertingkat</strong>: setelah coachee upload evidence, deliverable di-review oleh <strong>Sr. Supervisor</strong> → <strong>Section Head</strong> → <strong>HC Review</strong>. Jika salah satu reviewer menolak, <em>seluruh rantai approval di-reset</em> dan coachee harus upload ulang evidence. Mapping coach-coachee dikelola oleh Admin/HC. Lihat panduan lengkap di <a href=\"/Home/GuideDetail?module=cdp#collapse-cdp-reviewer-chain\"><strong>CDP → Memahami Alur Reviewer</strong></a>.",
```

- [ ] **Step 28.2: Update FAQ16 keywords**

Find `Id: "faq-umum-role-system"`. Replace `Keywords` field:

```csharp
Keywords: new[] {
    "role", "saya", "apa", "posisi",
    "admin", "hc", "coachee",
    "section head", "sr supervisor", "supervisor",
    "manager", "vp", "direktur"
},
```

- [ ] **Step 28.3: Update FAQ8d cross-link**

Find `Id: "faq-assessment-pre-post"`. Append link ke jawaban:

```csharp
AnswerHtml: "Pre-Post Test adalah tipe assessment khusus untuk mengukur <strong>peningkatan kompetensi</strong> sebelum dan sesudah program pelatihan. Peserta mengerjakan <strong>Pre-Test</strong> sebelum pelatihan dan <strong>Post-Test</strong> setelah pelatihan, dengan jadwal yang ditentukan oleh Admin. Hasil kedua test dibandingkan untuk mengukur <strong>Gain Score</strong> (selisih peningkatan nilai). Lihat panduan step-by-step di <a href=\"/Home/GuideDetail?module=cmp#collapse-cmp-pre-post-test\"><strong>CMP → Cara Mengerjakan Pre-Post Test</strong></a>.",
```

- [ ] **Step 28.4: Build + UAT**

`dotnet build`. `dotnet run`. Buka `/Home/Guide`:
- Search "section head" → FAQ16 (Role System) muncul di hasil
- Klik FAQ12 → expand → link "Memahami Alur Reviewer" jalan
- Klik FAQ8d → expand → link "Pre-Post Test" jalan

- [ ] **Step 28.5: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
fix(guide): update FAQ — cross-link + keyword coverage

- FAQ12 (approve deliverable): eksplisit "coachee upload" + cross-link
  ke cdp-reviewer-chain
- FAQ16 (role system): keywords tambah section head, sr supervisor,
  supervisor, manager, vp, direktur (sebelumnya search "section head"
  return 0 hit)
- FAQ8d (pre-post test): cross-link ke cmp-pre-post-test

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 29: Tambah FAQ baru (2)

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 29.1: Tambah `faq-coach-coachee-self`**

Append ke `Faqs` list di kategori `CdpCoaching`:

```csharp
new GuideFaqItem(
    Id: "faq-coach-coachee-self",
    Category: FaqCategory.CdpCoaching,
    Question: "Bagaimana saya tahu siapa coach saya?",
    AnswerHtml: "Buka menu <strong>CDP</strong> → halaman utama menampilkan <strong>Mapping Coach-Coachee</strong> Anda di bagian atas. Nama coach dan unit terlihat jelas. Kalau belum ada mapping, hubungi Admin/HC untuk assignment.",
    Roles: new[] { RoleGroup.Coachee },
    Keywords: new[] { "coach", "saya", "siapa", "mapping" }
),
```

- [ ] **Step 29.2: Tambah `faq-supervisor-vs-coach`**

Append:

```csharp
new GuideFaqItem(
    Id: "faq-supervisor-vs-coach",
    Category: FaqCategory.CdpCoaching,
    Question: "Apa beda Coach dan Supervisor?",
    AnswerHtml: "<strong>Coach</strong> dan <strong>Supervisor</strong> punya level akses sama (Level 5). Bedanya: <strong>Coach</strong> punya mapping ke coachee tertentu (bertanggung jawab langsung untuk coaching session + validasi final assessment). <strong>Supervisor</strong> punya akses sistem yang sama tapi tidak punya mapping coachee — biasanya untuk role pengawas yang tidak terlibat langsung coaching.",
    Roles: new[] { RoleGroup.All },
    Keywords: new[] { "coach", "supervisor", "beda", "perbedaan", "level" }
),
```

- [ ] **Step 29.3: Build + UAT**

`dotnet build`. `dotnet run`. Buka `/Home/Guide`.

Login as Coachee:
- FAQ "Bagaimana saya tahu siapa coach saya?" muncul di kategori CDP & Coaching

Login as Admin:
- FAQ "Apa beda Coach dan Supervisor?" muncul
- FAQ "Bagaimana saya tahu siapa coach saya?" juga muncul (AdminHC implicit lihat semua)

- [ ] **Step 29.4: Commit**

```bash
git add Services/GuideContentProvider.cs
git commit -m "$(cat <<'EOF'
feat(guide): tambah 2 FAQ baru kategori CDP & Coaching

- faq-coach-coachee-self (Coachee): "Bagaimana saya tahu siapa coach saya?"
- faq-supervisor-vs-coach (All): "Apa beda Coach dan Supervisor?"

FAQ total: 32 → 34.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 30: Full regression UAT Phase 3

- [ ] **Step 30.1: Run app**

Run: `dotnet run`

- [ ] **Step 30.2: Per-role checklist**

Test login tiap role, verify visibility:

**Admin / HC** — semua item harus muncul:
- Module cards: 5 (CMP, CDP, Account, Data, Admin)
- CMP: 6 accordion item + 1 PDF
- CDP: 10 accordion item + 1 PDF
- Account: 6 accordion item + 1 PDF (orphan-now-linked)
- Data: 3 accordion item + 1 PDF
- Admin: 12 accordion item + 1 PDF
- FAQ: 34 total (semua kategori)

**Manager (Direktur/VP/Manager)** — terbatas:
- Module cards: 3 (CMP, CDP, Account)
- CMP: 4 (3 base + monitoring-manager)
- CDP: 5 (3 base + dashboard + bottleneck)
- Account: 6
- FAQ: 26 (semua kecuali kategori AdminData)

**Atasan (SectionHead/SrSupervisor)** — fokus reviewer:
- Module cards: 3 (CMP, CDP, Account)
- CMP: 4 (3 base + monitoring-records-tim)
- CDP: 5 (3 base + approve + reviewer-chain)
- Account: 6
- FAQ: 26

**Coach / Supervisor** — fokus coaching daily:
- Module cards: 3 (CMP, CDP, Account)
- CMP: 3 (base only)
- CDP: 5 (3 base + coaching-session + final-assessment)
- Account: 6
- FAQ: 26

**Coachee** — basic user:
- Module cards: 3 (CMP, CDP, Account)
- CMP: 4 (3 base + pre-post-test)
- CDP: 5 (3 base + upload-evidence + final-assessment)
- Account: 6
- FAQ: 27 (26 base + faq-coach-coachee-self)

- [ ] **Step 30.3: Search regression**

Login any user. Test search:
- "section head" → FAQ16 muncul ✓
- "evidence" → FAQ + accordion cdp-upload-evidence muncul (kalau role allowed)
- "renewal" → admin-renewal-management muncul (Admin only)

- [ ] **Step 30.4: Stop app**

Ctrl+C.

- [ ] **Step 30.5: No final commit needed**

Sudah commit per task. Phase 3 selesai dengan 8 commit terpisah (per audit category).

---

# PHASE 4 — Search Improvement (PR 4, OPTIONAL)

Skip phase ini kalau scope sudah cukup. Phase 4 tidak dependency PR 3 — bisa di-skip atau dikerjakan terpisah nanti.

## Task 31: Auto-keyword builder

**Files:**
- Modify: `Services/GuideContentProvider.cs`

- [ ] **Step 31.1: Tambah helper di provider**

Append ke `GuideContentProvider`:

```csharp
public static string BuildKeywords(GuideItem item)
{
    var parts = new List<string>
    {
        item.Title.ToLowerInvariant(),
        item.Module.ToString().ToLowerInvariant(),
        GuideRoleAccess.BadgeLabel(item.Roles).ToLowerInvariant(),
    };
    parts.AddRange(item.Steps.Select(s => s.Title.ToLowerInvariant()));
    parts.AddRange(item.Keywords);
    return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public static string BuildKeywords(GuideFaqItem faq)
{
    var parts = new List<string>
    {
        faq.Question.ToLowerInvariant(),
        faq.Category.ToString().ToLowerInvariant(),
        GuideRoleAccess.BadgeLabel(faq.Roles).ToLowerInvariant(),
    };
    parts.AddRange(faq.Keywords);
    return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
}
```

- [ ] **Step 31.2: Update partial _AccordionItem.cshtml**

Edit `Views/Shared/_Guide/_AccordionItem.cshtml`, ganti:

```cshtml
var keywordList = string.Join(" ", Model.Keywords).ToLowerInvariant();
```

Jadi:

```cshtml
var keywordList = HcPortal.Services.GuideContentProvider.BuildKeywords(Model);
```

- [ ] **Step 31.3: Update partial _FaqItem.cshtml**

Sama, ganti pakai `BuildKeywords(Model)`.

- [ ] **Step 31.4: Build + UAT**

`dotnet build`. `dotnet run`.

Test search baru yang dulu miss:
- "atasan" → semua item dengan Roles Atasan muncul
- "coach" → semua item Coach + FAQ Coach muncul
- "manager" → manager items muncul

- [ ] **Step 31.5: Commit**

```bash
git add Services/GuideContentProvider.cs Views/Shared/_Guide/_AccordionItem.cshtml Views/Shared/_Guide/_FaqItem.cshtml
git commit -m "$(cat <<'EOF'
feat(guide): auto-keyword builder include role + module + step titles

GuideContentProvider.BuildKeywords(item/faq) generate keywords dari
title + module + role label + step titles + manual Keywords[].
Partial accordion/faq pakai helper ini.

Eliminasi miss search untuk role names (atasan, coach, manager dst)
tanpa perlu manual keyword tagging per item.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 32: Filter chip per role (UI)

Goal: chip clickable di Hub page untuk filter konten by role (mis: klik "Coachee" → tampil hanya item + FAQ untuk Coachee).

- [ ] **Step 32.1: Tambah HTML chip di Guide.cshtml**

Insert di `Views/Home/Guide.cshtml` setelah search wrapper:

```cshtml
<div class="guide-filter-chips" data-aos="fade-up" data-aos-delay="120">
    <span class="filter-label">Filter role:</span>
    <button type="button" class="filter-chip active" data-role-filter="all">Semua</button>
    <button type="button" class="filter-chip" data-role-filter="coachee">Coachee</button>
    <button type="button" class="filter-chip" data-role-filter="coach">Coach</button>
    <button type="button" class="filter-chip" data-role-filter="atasan">Atasan</button>
    <button type="button" class="filter-chip" data-role-filter="manager">Manager</button>
    <button type="button" class="filter-chip" data-role-filter="adminhc">Admin & HC</button>
</div>
```

- [ ] **Step 32.2: Tambah CSS chip**

Edit `wwwroot/css/guide.css`, append:

```css
.guide-filter-chips {
    display: flex;
    align-items: center;
    gap: .5rem;
    flex-wrap: wrap;
    margin: 1rem 0;
}
.guide-filter-chips .filter-label {
    font-size: .85rem;
    color: var(--guide-muted, #6c757d);
    font-weight: 600;
}
.guide-filter-chips .filter-chip {
    background: transparent;
    border: 1px solid var(--guide-border, #dee2e6);
    border-radius: 16px;
    padding: .25rem .75rem;
    font-size: .85rem;
    cursor: pointer;
    transition: all 0.15s ease;
}
.guide-filter-chips .filter-chip:hover {
    background: var(--guide-hover-bg, #f8f9fa);
}
.guide-filter-chips .filter-chip.active {
    background: var(--guide-primary, #0d6efd);
    color: white;
    border-color: var(--guide-primary, #0d6efd);
}
.role-filtered-out {
    display: none !important;
}
```

- [ ] **Step 32.3: Tambah JS filter logic**

Append ke `<script>` section di `Views/Home/Guide.cshtml`:

```javascript
// ─── Role filter chips ───
(function () {
    var chips = document.querySelectorAll('.filter-chip');
    if (chips.length === 0) return;

    chips.forEach(function(chip) {
        chip.addEventListener('click', function() {
            chips.forEach(c => c.classList.remove('active'));
            this.classList.add('active');

            var role = this.dataset.roleFilter;

            // Filter module cards + faq items by data-keywords containing role label
            document.querySelectorAll('.searchable-card, .searchable-faq').forEach(function(el) {
                if (role === 'all') {
                    el.classList.remove('role-filtered-out');
                    return;
                }
                var kw = (el.dataset.keywords || '').toLowerCase();
                var match = kw.includes(role) || kw.includes('all');
                el.classList.toggle('role-filtered-out', !match);
            });

            // Hide empty FAQ categories
            document.querySelectorAll('.faq-category').forEach(function(cat) {
                var visible = cat.querySelectorAll('.searchable-faq:not(.role-filtered-out)').length;
                cat.style.display = visible > 0 ? '' : 'none';
            });
        });
    });
})();
```

- [ ] **Step 32.4: Build + UAT**

`dotnet build`. `dotnet run`. Buka `/Home/Guide`.

Klik chip "Coachee" → cuma module/FAQ untuk Coachee yang tampil. Klik "Semua" → semua kembali tampil.

- [ ] **Step 32.5: Commit**

```bash
git add Views/Home/Guide.cshtml wwwroot/css/guide.css
git commit -m "$(cat <<'EOF'
feat(guide): filter chip per role di Hub page

Chip clickable di Guide hub: Semua / Coachee / Coach / Atasan /
Manager / Admin & HC. Filter module cards + FAQ items client-side
berdasarkan data-keywords role label.

Bergantung pada auto-keyword (PR 4 Task 31) yang include role label.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

# Wrap-up

## Total commits

- PR 1: 1 commit (foundation)
- PR 2: 1 commit (refactor + Playwright)
- PR 3: 8 commits (audit per category)
- PR 4: 2 commits (optional)

**Total: 10-12 commits**, masing-masing atomic, mudah review.

## Promote ke Dev/Prod

Per `CLAUDE.md` workflow:
1. Push branch ke remote
2. Notify IT team dengan commit hash range + flag: no DB migration
3. IT promosi ke Dev (10.55.3.3) + Prod sesuai schedule mereka

## Files touched summary

- **Created**: 12 model files, 2 service files, 4 partial, 1 Playwright spec = **19 file baru**
- **Modified**: 1 controller, 1 layout import, 2 view = **4 file modified**
- **No DB migration**
- **No package dependency baru** (Playwright via npm di subfolder, tidak masuk .csproj)

## What this plan does NOT cover

- Test project xUnit/NUnit (per Q6 decision: no)
- DB-backed content (per Q-A decision: no, tetap kode)
- Admin Panel CRUD untuk Guide (out of scope)
- Multi-role user proper handling (tetap `.FirstOrDefault()`)
- SelectedView integration (tetap actual role)
- Internationalization (Bahasa Indonesia only)
