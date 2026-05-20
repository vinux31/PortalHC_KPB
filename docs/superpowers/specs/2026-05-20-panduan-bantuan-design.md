# Panduan & Bantuan — Refactor + Audit Konten

**Date**: 2026-05-20
**Status**: Draft (pending user review)
**Scope**: `Views/Home/Guide.cshtml`, `Views/Home/GuideDetail.cshtml`, `Controllers/HomeController.Guide*`

## Goal

1. Refactor konten Panduan & Bantuan dari hardcoded Razor ke struktur data terpisah (model + provider), tetap di kode (no DB).
2. Implementasi RBAC granular per-item (6 RoleGroup) menggantikan toggle biner `isAdminOrHc`.
3. Audit & perbaiki konten existing (role tagging salah, item split, item baru, FAQ update).

## Non-goals

- Tidak pindah konten ke database / CMS.
- Tidak bikin Admin Panel CRUD untuk Guide content (konten tetap diupdate via code + commit).
- Tidak bikin test project (manual UAT only).
- Tidak ubah behavior accordion (tetap multi-open).

---

## Architecture

### Layer baru

```
Models/Guide/
  GuideItem.cs         // record GuideItem
  GuideStep.cs         // record GuideStep
  GuidePdfLink.cs      // record GuidePdfLink
  GuideFaqItem.cs      // record GuideFaqItem
  GuideModule.cs       // enum: Cmp, Cdp, Account, Data, Admin
  RoleGroup.cs         // enum: AdminHC, Manager, Atasan, Coach, Coachee, All
  FaqCategory.cs       // enum: Akun, Assessment, CdpCoaching, Umum, KkjCpdp, AdminData
  GuideViewModel.cs        // VM untuk Guide.cshtml
  GuideDetailViewModel.cs  // VM untuk GuideDetail.cshtml

Services/                 (flat, sesuai konvensi)
  GuideContentProvider.cs  // static seed + GetItems/GetFaqs filter by role
  GuideRoleAccess.cs       // GroupsFor(userRole), CanSee(userRole, RoleGroup[])

Views/Home/
  Guide.cshtml           // loop dari GuideViewModel
  GuideDetail.cshtml     // loop dari GuideDetailViewModel

Views/Shared/_Guide/
  _AccordionItem.cshtml  // partial 1 accordion
  _FaqItem.cshtml        // partial 1 FAQ
  _RoleBadge.cshtml      // partial badge auto dari RoleGroup[]
  _ModuleCard.cshtml     // partial 1 module card

Views/_ViewImports.cshtml
  + @using HcPortal.Models.Guide
```

### Data flow

1. User akses `/Home/Guide` atau `/Home/GuideDetail?module=cmp`.
2. Controller ambil `userRole = userRoles.FirstOrDefault() ?? "User"` (behavior existing, tidak diubah).
3. Controller panggil `GuideContentProvider.BuildHubViewModel(userRole)` atau `BuildDetailViewModel(module, userRole)`.
4. Provider filter `Items` / `Faqs` / `Pdfs` via `GuideRoleAccess.CanSee(userRole, item.Roles)`.
5. View loop ViewModel → render partial accordion / FAQ / badge.

### Cache

Tidak perlu. Provider = static `IReadOnlyList<>` di kode, sudah in-memory, immutable per build.

---

## Data Model

### Records

```csharp
public enum GuideModule { Cmp, Cdp, Account, Data, Admin }

public enum RoleGroup { AdminHC, Manager, Atasan, Coach, Coachee, All }

public enum FaqCategory { Akun, Assessment, CdpCoaching, Umum, KkjCpdp, AdminData }

public record GuideStep(int Number, string Title, string BodyHtml);

public record GuideItem(
    string Id,                  // stable slug: "cmp-library-kkj"
    GuideModule Module,
    string Title,
    RoleGroup[] Roles,
    GuideStep[] Steps,
    string[] Keywords           // optional override
);

public record GuideFaqItem(
    string Id,                  // "akun-login"
    FaqCategory Category,
    string Question,
    string AnswerHtml,
    RoleGroup[] Roles,
    string[] Keywords
);

public record GuidePdfLink(
    GuideModule Module,
    string Title,
    string Description,
    string FilePath,            // "~/documents/guides/Panduan-Lengkap-Assessment.html"
    string CardCssClass,        // "guide-tutorial-card--cmp"
    string BtnColorClass,       // "btn-primary"
    RoleGroup[] Roles
);

public record GuideViewModel(
    string UserRole,
    IReadOnlyList<GuideModuleCardVm> ModuleCards,
    IReadOnlyDictionary<FaqCategory, IReadOnlyList<GuideFaqItem>> FaqsByCategory
);

public record GuideDetailViewModel(
    string UserRole,
    GuideModule Module,
    string ModuleTitle,
    string ModuleIcon,
    GuidePdfLink? Pdf,
    IReadOnlyList<GuideItem> Items
);

public record GuideModuleCardVm(
    GuideModule Module,
    string Title,
    string IconCssClass,
    string CardCssClass,
    int ItemCount,
    RoleGroup[] Roles
);
```

### ID convention

Stable slug `module-topic`, contoh: `cmp-library-kkj`, `cdp-approve-deliverable`, `account-logout`. Bukan auto-number (cmpHeading1, dst). Manfaat: rename tahan, deep-link stabil, tidak ada ID lompat.

Bootstrap collapse target: `id="@($"collapse-{item.Id}")"`.

---

## Role Model

### 6 RoleGroup → 10 Identity role

| RoleGroup | UserRoles members | Level |
|-----------|-------------------|-------|
| `AdminHC` | `Admin`, `HC` | 1, 2 |
| `Manager` | `Direktur`, `VP`, `Manager` | 3 |
| `Atasan` | `SectionHead`, `SrSupervisor` | 4 |
| `Coach` | `Coach`, `Supervisor` | 5 |
| `Coachee` | `Coachee` | 6 |
| `All` | (semua) | — |

### `GuideRoleAccess` API

```csharp
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

    public static string BadgeLabel(RoleGroup[] roles) => roles switch
    {
        var r when r.Contains(RoleGroup.All) => string.Empty,
        var r when r.Length == 1 && r[0] == RoleGroup.AdminHC  => "Admin & HC",
        var r when r.Length == 1 && r[0] == RoleGroup.Atasan   => "Atasan",
        var r when r.Length == 1 && r[0] == RoleGroup.Coach    => "Coach",
        var r when r.Length == 1 && r[0] == RoleGroup.Manager  => "Manager",
        var r when r.Length == 1 && r[0] == RoleGroup.Coachee  => "Coachee",
        var r => string.Join(" & ", r.Select(g => g.ToString()))
    };
}
```

### Access rules

- `AdminHC` implicit lihat semua (inheritance lewat `GroupsFor` return semua grup).
- `Manager` tidak hierarchical lihat Atasan/Coach/Coachee item — Manager hanya lihat `Manager` + `All`. Sesuai existing `RolesReviewerAndAbove` yang exclude Manager.
- Multi-role user: pakai `userRoles.FirstOrDefault()` (behavior existing tidak diubah).
- Guide pakai actual role (bukan `SelectedView`).

---

## Audit Konten

### Role tagging fix

| ID baru | Title | Roles lama (di code) | Roles baru |
|---------|-------|----------------------|-----------|
| `cmp-monitoring-records-tim` | Monitoring Records Tim | AdminHC | **Atasan, AdminHC** |
| `cdp-approve-deliverable` | Approve / Reject Deliverable | AdminHC | **Atasan, AdminHC** |
| `cdp-coaching-proton-dashboard` | Coaching Proton Dashboard | AdminHC | **Manager, AdminHC** |
| FAQ kategori Admin/Kelola Data (8 item) | — | AdminHC | tetap **AdminHC** |

### Accordion split

- `accCollapse2` "Melihat & Edit Profil" → split:
  - `account-view-profile` — Cara Melihat Profil
  - `account-edit-profile` — Cara Edit Profil
- `accCollapse4` "Logout & Memahami Role System" → split:
  - `account-logout` — Cara Logout
  - `account-role-system` — Memahami Role System

### Item baru

| ID | Title | Roles | Module |
|----|-------|-------|--------|
| `cmp-pre-post-test` | Cara Mengerjakan Pre-Post Test | Coachee, All | CMP |
| `cmp-monitoring-manager` | Monitoring Compliance Unit/Section | Manager, AdminHC | CMP |
| `cdp-upload-evidence` | Cara Upload Evidence Deliverable | Coachee | CDP |
| `cdp-coaching-session` | Cara Catat Coaching Session | Coach, AdminHC | CDP |
| `cdp-reviewer-chain` | Memahami Alur Reviewer (SrSV → SectionHead → HC) | Atasan, AdminHC | CDP |
| `cdp-final-assessment` | Cara Submit Final Assessment Proton | Coachee, Coach | CDP |
| `cdp-bottleneck-report` | Cara Lihat Bottleneck Report | Manager, AdminHC | CDP |
| `admin-renewal-management` | Cara Kelola Renewal Sertifikat | AdminHC | Admin |

### PDF link

- Orphan PDF `Panduan-Penggunaan-Website-HC-Portal-KPB.html` → link di module **Account** dengan `Roles: [All]`.

### FAQ update

| FAQ ID | Action |
|--------|--------|
| `faq12-approve-deliverable` | Update jawaban — eksplisit reviewer chain SrSV → SectionHead → HC, cross-link ke `cdp-reviewer-chain` |
| `faq16-role-system` | Keywords tambah: section head, sr supervisor, supervisor, manager, vp, direktur |
| `faq8d-pre-post-test` | Cross-link ke `cmp-pre-post-test` |
| `faq-coach-coachee-self` *(baru)* | "Bagaimana saya tahu siapa coach saya?" — Roles: Coachee |
| `faq-supervisor-vs-coach` *(baru)* | "Apa beda Coach vs Supervisor?" — Roles: All |

### ID cleanup

- Renumber semua `*Heading*` / `*Collapse*` → slug-based.
- Hapus komentar `<!-- CDP 2-4 removed -->`.
- Hapus hardcoded `cmpCount/cdpCount/accountCount/dataCount/adminCount` → auto via `viewModel.Items.Count`.

### Search improvement

- Auto-include role label + module di `data-keywords` (helper `BuildKeywords(item)`).
- Manual `Keywords[]` jadi optional override.

### Delta konten

- Item existing fix tagging: 3
- Item di-split: 2 → 4 (net +2)
- Item baru: 8
- PDF orphan dilink: 1
- FAQ existing update: 3
- FAQ baru: 2
- **Total setelah Tahap 3**: accordion **38** (dari 28), FAQ **34** (dari 32), PDF **5** (dari 4).

---

## PR Sequencing

### PR 1 — Foundation (Models + Provider + Helper)

- `Models/Guide/*.cs` (records + enums + ViewModels)
- `Services/GuideContentProvider.cs` — port konten existing as-is, tag `Roles` sesuai existing `@if` (no audit changes)
- `Services/GuideRoleAccess.cs`
- Update `Views/_ViewImports.cshtml` — `@using HcPortal.Models.Guide`
- **Tidak sentuh `.cshtml` Home/Guide*** — provider ada tapi belum dipakai
- Goal: zero breaking change, foundation ready

### PR 2 — Refactor Views (visual parity)

- `Controllers/HomeController.Guide()` → return `View(viewModel)`
- `Controllers/HomeController.GuideDetail(module)` → return `View(detailViewModel)`
- `Views/Home/Guide.cshtml` → loop dari ViewModel
- `Views/Home/GuideDetail.cshtml` → loop dari ViewModel
- Bikin partial `_AccordionItem.cshtml`, `_FaqItem.cshtml`, `_RoleBadge.cshtml`, `_ModuleCard.cshtml`
- Renumber ID → slug-based
- Hapus hardcoded count
- **Konten identik** dengan PR 1 — UAT Playwright visual regression: screenshot before vs after pixel match
- Keep `data-bs-parent=""` (multi-open behavior preserved)

### PR 3 — Audit Konten

- Update 3 item role tagging fix
- Split 2 accordion → 4
- Tambah 8 item baru (per module commit terpisah untuk minimize merge conflict: CMP commit, CDP commit, Account commit, Admin commit)
- Link orphan PDF
- Update 3 FAQ + tambah 2 FAQ
- Update keyword FAQ16

### PR 4 — Search Improvement (optional)

- Auto-keyword builder (title + role label + module)
- Filter chip per role di Hub (UI tambahan, client-side filter)

### Dependency

PR 1 → PR 2 → PR 3 sequential. PR 4 independent setelah PR 1.

---

## Testing

### Unit test

**Tidak ada** (no test project di repo, scope tidak nambah test project). Manual UAT only.

### Manual UAT — PR 1

- Build OK: `dotnet build` exit 0
- `dotnet run` → akses `/Home/Guide` → halaman tetap render seperti sebelumnya (provider belum dipakai view, no visual change)

### Manual UAT — PR 2 (visual parity)

- Playwright script `tests/playwright/guide-parity.spec.ts`:
  - Login as Admin (`admin@pertamina.com`), screenshot `/Home/Guide`, `/Home/GuideDetail?module=cmp/cdp/account/data/admin`
  - Login as Coachee, screenshot pages yang same
  - Diff with PR 1 baseline → pass kalau identik
- Manual cek search: ketik "assessment" → hasil sama dengan sebelumnya
- Manual cek accordion: expand/collapse jalan, multi-open behavior preserved

### Manual UAT — PR 3 (content audit)

- Login as `Section Head` → CDP accordion harus muncul "Approve / Reject Deliverable" + "Memahami Alur Reviewer"
- Login as `Manager` → harus muncul "Coaching Proton Dashboard" + "Bottleneck Report" + "Monitoring Manager"
- Login as `Coachee` → harus muncul "Upload Evidence" + "Pre-Post Test" + "Final Assessment"
- Login as `Coach` → harus muncul "Coaching Session" + "Final Assessment"
- Search "section head" → FAQ16 muncul
- Module Account → PDF "Panduan Penggunaan Website" muncul
- Login as `Coachee` → item Admin/HC tidak muncul (regression check)

### No DB change → no migration test

---

## Risiko & Mitigasi

| Risiko | Mitigasi |
|--------|---------|
| PR 2 visual berubah tidak sengaja | Playwright pixel-diff baseline before/after |
| Slug ID baru break deep-link existing | Grep confirmed: cmpCollapse/cdpCollapse dst hanya dipakai internal GuideDetail.cshtml, 0 external reference |
| Seed list besar (~800 baris) bikin merge conflict | PR 3 commit per-module terpisah |
| Pre-Post Test / Final Assessment step text tidak akurat | Cross-reference dengan PDF `Panduan-Lengkap-Coaching-Proton.html` saat tulis seed |
| RoleAccess logic salah (Coachee bocor lihat AdminHC) | Manual UAT eksplisit per-role di PR 3, plus Playwright per-role screenshot |
| `UserRoles.FirstOrDefault()` user multi-role | Behavior existing tidak diubah; out of scope |

---

## Out of Scope

- Database migration / CMS UI
- Admin CRUD untuk konten Guide
- Test project (xUnit/NUnit) — manual UAT only
- Multi-role user proper handling (tetap `.FirstOrDefault()`)
- SelectedView integration (tetap actual role)
- Backend search (tetap client-side)
- Internationalization (tetap Bahasa Indonesia only)

---

## Files Affected

### Created

- `Models/Guide/GuideItem.cs`
- `Models/Guide/GuideStep.cs`
- `Models/Guide/GuidePdfLink.cs`
- `Models/Guide/GuideFaqItem.cs`
- `Models/Guide/GuideModule.cs`
- `Models/Guide/RoleGroup.cs`
- `Models/Guide/FaqCategory.cs`
- `Models/Guide/GuideViewModel.cs`
- `Models/Guide/GuideDetailViewModel.cs`
- `Models/Guide/GuideModuleCardVm.cs`
- `Services/GuideContentProvider.cs`
- `Services/GuideRoleAccess.cs`
- `Views/Shared/_Guide/_AccordionItem.cshtml`
- `Views/Shared/_Guide/_FaqItem.cshtml`
- `Views/Shared/_Guide/_RoleBadge.cshtml`
- `Views/Shared/_Guide/_ModuleCard.cshtml`
- `tests/playwright/guide-parity.spec.ts` (PR 2)

### Modified

- `Views/Home/Guide.cshtml` (loop dari ViewModel, slug ID)
- `Views/Home/GuideDetail.cshtml` (loop dari ViewModel, slug ID)
- `Controllers/HomeController.cs` (Guide + GuideDetail return ViewModel)
- `Views/_ViewImports.cshtml` (tambah `@using HcPortal.Models.Guide`)

### Unchanged

- `wwwroot/css/guide.css`
- `wwwroot/documents/guides/*.html` (PDF tutorials)
- `Models/UserRoles.cs`
- Database schema (no migration)
