// Phase 342 Plan 03 — OrganizationController dup-name per-parent + PreviewEditCascade accuracy.
// REQ coverage: ORG-TREE-02 (per-parent uniqueness), ORG-TREE-07 (preview count == actual cascade).
// Strategy: InMemory DB (Guid per test) + UserManager/env null-substitute (Add/Edit/Preview tidak deref).
// Pitfall 5: casing IDENTIK (InMemory case-sensitive vs SQL Server CI) — jangan andalkan case-insensitivity.
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

public class OrganizationControllerTests
{
    private static (OrganizationController ctrl, ApplicationDbContext ctx) MakeController()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // Pitfall 1: EditOrganizationUnit dibungkus BeginTransactionAsync (Phase 403). InMemory tak
            // dukung transaksi → tanpa suppress, EditOrganizationUnit/preview-parity test lempar
            // TransactionIgnoredWarning. InMemory abaikan tx (no-op) → operasi tetap jalan → parity teruji.
            // Atomicity = domain SQL Phase 404, bukan di-test di sini.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var ctx = new ApplicationDbContext(options);
        var auditLog = new AuditLogService(ctx);
        #pragma warning disable CS8625 // null-substitute: Add/Edit/PreviewEditCascade do not deref _userManager/_env
        var ctrl = new OrganizationController(ctx, null!, auditLog, null!);
        #pragma warning restore CS8625
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";  // → IsAjaxRequest()==true → Json response
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return (ctrl, ctx);
    }

    // ── Phase 426 AUDIT-01: user-aware factory (audit block deref _userManager.GetUserAsync(User)) ──
    // FakeUserStore + MakeUserManager disalin verbatim dari RetakeExamEndpointTests.cs:47-99.
    // MakeControllerWithUser memberi UserManager non-null + ClaimsPrincipal ber-NameIdentifier
    // sehingga blok audit men-resolve actor & menulis baris AuditLog (happy-path content T1-T4).

    private sealed class FakeUserStore : IUserStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
    {
        private readonly Dictionary<string, ApplicationUser> _byId = new();
        public void Add(ApplicationUser u) => _byId[u.Id] = u;

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct)
            => Task.FromResult(_byId.TryGetValue(userId, out var u) ? u : null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
            => Task.FromResult<ApplicationUser?>(null);
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.UserName);
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) { _byId[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) { _byId[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) { _byId.Remove(user.Id); return Task.FromResult(IdentityResult.Success); }
        public void Dispose() { }

        // IUserRoleStore — role kosong (audit block tak gating by role level).
        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.CompletedTask;
        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult<IList<string>>(new List<string>());
        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.FromResult(false);
        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken ct) => Task.FromResult<IList<ApplicationUser>>(new List<ApplicationUser>());
    }

    private static UserManager<ApplicationUser> MakeUserManager(FakeUserStore store)
        => new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

    /// <summary>Factory user-aware: UserManager non-null atas FakeUserStore + ClaimsPrincipal
    /// ber-NameIdentifier=actor.Id, sehingga blok audit Phase 426 menulis baris AuditLog nyata.</summary>
    private static (OrganizationController ctrl, ApplicationDbContext ctx) MakeControllerWithUser(ApplicationUser actor)
    {
        var store = new FakeUserStore();
        store.Add(actor);
        var um = MakeUserManager(store);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var ctx = new ApplicationDbContext(options);
        var auditLog = new AuditLogService(ctx);
        #pragma warning disable CS8625 // null-substitute: EditOrganizationUnit tidak deref _env
        var ctrl = new OrganizationController(ctx, um, auditLog, null!);
        #pragma warning restore CS8625
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";  // → IsAjaxRequest()==true → Json response
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actor.Id) }, "TestAuth"));
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return (ctrl, ctx);
    }

    private static bool GetSuccess(IActionResult r)
    {
        var j = Assert.IsType<JsonResult>(r);
        return (bool)j.Value!.GetType().GetProperty("success")!.GetValue(j.Value)!;
    }
    private static int GetInt(IActionResult r, string prop)
    {
        var j = Assert.IsType<JsonResult>(r);
        return (int)j.Value!.GetType().GetProperty(prop)!.GetValue(j.Value)!;
    }
    private static bool GetBool(IActionResult r, string prop)
    {
        var j = Assert.IsType<JsonResult>(r);
        return (bool)j.Value!.GetType().GetProperty(prop)!.GetValue(j.Value)!;
    }
    private static string GetMessage(IActionResult r)
    {
        var j = Assert.IsType<JsonResult>(r);
        return (string)j.Value!.GetType().GetProperty("message")!.GetValue(j.Value)!;
    }

    // ── ORG-TREE-02: dup-name per-parent ──────────────────────────────────────

    [Fact]
    public async Task AddOrganizationUnit_SameNameDifferentParent_Accepted()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "HSC",  Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 3, Name = "Operations", Level = 1, ParentId = 1, IsActive = true });
        ctx.SaveChanges();
        var result = await ctrl.AddOrganizationUnit("Operations", 2);   // parent beda (HSC) → boleh
        Assert.True(GetSuccess(result));
    }

    [Fact]
    public async Task AddOrganizationUnit_SameNameSameParent_Rejected()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 3, Name = "Operations", Level = 1, ParentId = 1, IsActive = true });
        ctx.SaveChanges();
        var result = await ctrl.AddOrganizationUnit("Operations", 1);   // parent sama → ditolak
        Assert.False(GetSuccess(result));
    }

    [Fact]
    public async Task EditOrganizationUnit_SameNameSameParent_Rejected()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 3, Name = "Operations", Level = 1, ParentId = 1, IsActive = true },
            new OrganizationUnit { Id = 4, Name = "Maintenance", Level = 1, ParentId = 1, IsActive = true });
        ctx.SaveChanges();
        // Rename "Maintenance" (id 4) jadi "Operations" di parent sama (1) → ditolak (dup, exclude self)
        var result = await ctrl.EditOrganizationUnit(4, "Operations", 1);
        Assert.False(GetSuccess(result));
    }

    // ── ORG-TREE-07: PreviewEditCascade count == actual (Pitfall 1) + early-return (D-04) ──

    [Fact]
    public async Task PreviewEditCascade_RenameLevel0_CountMatchesActual()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.Add(new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true });
        // seed denormalized Section/Bagian="RFCC": 3 user, 2 mapping, 4 kompetensi, 1 guidance
        ctx.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "u1", Section = "RFCC" },
            new ApplicationUser { Id = "u2", UserName = "u2", Section = "RFCC" },
            new ApplicationUser { Id = "u3", UserName = "u3", Section = "RFCC" });
        ctx.CoachCoacheeMappings.AddRange(
            new CoachCoacheeMapping { AssignmentSection = "RFCC" },
            new CoachCoacheeMapping { AssignmentSection = "RFCC" });
        ctx.ProtonKompetensiList.AddRange(
            new ProtonKompetensi { Bagian = "RFCC" }, new ProtonKompetensi { Bagian = "RFCC" },
            new ProtonKompetensi { Bagian = "RFCC" }, new ProtonKompetensi { Bagian = "RFCC" });
        ctx.CoachingGuidanceFiles.Add(new CoachingGuidanceFile { Bagian = "RFCC" });
        ctx.SaveChanges();

        // ACT 1: preview SEBELUM mutasi
        var preview = await ctrl.PreviewEditCascade(1, "Refinery Complex", null);
        int pUsers = GetInt(preview, "affectedUsersCount");
        int pMap   = GetInt(preview, "affectedMappingsCount");
        int pKomp  = GetInt(preview, "affectedKompetensiCount");
        int pGuid  = GetInt(preview, "affectedGuidanceCount");

        // ACT 2: edit aktual (mutasi cascade) — share ctx
        await ctrl.EditOrganizationUnit(1, "Refinery Complex", null);
        int aUsers = ctx.Users.Count(u => u.Section == "Refinery Complex");
        int aMap   = ctx.CoachCoacheeMappings.Count(m => m.AssignmentSection == "Refinery Complex");
        int aKomp  = ctx.ProtonKompetensiList.Count(k => k.Bagian == "Refinery Complex");
        int aGuid  = ctx.CoachingGuidanceFiles.Count(g => g.Bagian == "Refinery Complex");

        // ASSERT: preview == actual (predikat tidak drift — Pitfall 1)
        Assert.Equal(aUsers, pUsers);
        Assert.Equal(aMap, pMap);
        Assert.Equal(aKomp, pKomp);
        Assert.Equal(aGuid, pGuid);
        Assert.Equal(3, pUsers);   // sanity
        Assert.Equal(4, pKomp);    // sanity
    }

    [Fact]
    public async Task PreviewEditCascade_RenameLevel1_CountMatchesActual()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        // seed denormalized Unit="Alkylation": 2 user, 1 mapping, 3 kompetensi, 0 guidance
        ctx.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "u1", Unit = "Alkylation" },
            new ApplicationUser { Id = "u2", UserName = "u2", Unit = "Alkylation" });
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { AssignmentUnit = "Alkylation" });
        ctx.ProtonKompetensiList.AddRange(
            new ProtonKompetensi { Unit = "Alkylation" }, new ProtonKompetensi { Unit = "Alkylation" },
            new ProtonKompetensi { Unit = "Alkylation" });
        ctx.SaveChanges();

        // ACT 1: preview (rename Level 1, parent unchanged)
        var preview = await ctrl.PreviewEditCascade(2, "Alkylation New", 1);
        int pUsers = GetInt(preview, "affectedUsersCount");
        int pMap   = GetInt(preview, "affectedMappingsCount");
        int pKomp  = GetInt(preview, "affectedKompetensiCount");
        int pGuid  = GetInt(preview, "affectedGuidanceCount");

        // ACT 2: edit aktual
        await ctrl.EditOrganizationUnit(2, "Alkylation New", 1);
        int aUsers = ctx.Users.Count(u => u.Unit == "Alkylation New");
        int aMap   = ctx.CoachCoacheeMappings.Count(m => m.AssignmentUnit == "Alkylation New");
        int aKomp  = ctx.ProtonKompetensiList.Count(k => k.Unit == "Alkylation New");
        int aGuid  = ctx.CoachingGuidanceFiles.Count(g => g.Unit == "Alkylation New");

        // ASSERT: preview == actual (Level>=1 field-pair, Pitfall 1)
        Assert.Equal(aUsers, pUsers);
        Assert.Equal(aMap, pMap);
        Assert.Equal(aKomp, pKomp);
        Assert.Equal(aGuid, pGuid);
        Assert.Equal(2, pUsers);   // sanity
        Assert.Equal(3, pKomp);    // sanity
    }

    [Fact]
    public async Task PreviewEditCascade_NoChange_ReturnsEarlyFalseFlags()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.Add(new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true });
        ctx.SaveChanges();
        var result = await ctrl.PreviewEditCascade(1, "RFCC", null);   // nama+parent sama (D-04 early-return)
        Assert.False(GetBool(result, "nameChanged"));
        Assert.False(GetBool(result, "parentChanged"));
    }

    // ── TEST-03: pre-order DFS sort (D-05) — OrgTreePreOrder.BuildPreOrder mirrors orgTree.js ──
    // Asserts ONLY against the pure helper, NEVER against the flat tree endpoint output
    // (silent-pass trap). Fixtures are DISCRIMINATING: output != ascending-id-order AND != BFS.

    [Fact]
    public void PreOrder_RootDisplayOrderOutOfIdOrder_EmitsLowerDisplayOrderRootSubtreeFirst()
    {
        // Input is ENDPOINT-ORDERED (Level, DisplayOrder, Name). Root B (id=4) has DisplayOrder=1,
        // root A (id=1) has DisplayOrder=2 → endpoint emits B's tier-row before A's. Correct
        // pre-order = [4, 5, 1, 2, 3] (B subtree first). A flat-by-id or unsorted-insertion bug
        // would yield a different list.
        //   L0: B(id=4,DO=1), A(id=1,DO=2)
        //   L1: B1(id=5,parent=4,DO=1), A1(id=2,parent=1,DO=1)
        //   L2: A1a(id=3,parent=2,DO=1)
        var flat = new (int, int?, int, string)[]
        {
            (4, null, 1, "B"), (1, null, 2, "A"),
            (5, 4, 1, "B1"),  (2, 1, 1, "A1"),
            (3, 2, 1, "A1a"),
        };
        var built = OrgTreePreOrder.BuildPreOrder(flat);
        var order = built.Select(x => x.Id).ToList();
        Assert.Equal(new[] { 4, 5, 1, 2, 3 }, order);   // B, B1, A, A1, A1a
        Assert.Equal(2, built.Single(x => x.Id == 3).Depth);   // A1a is a grandchild → Depth 2
    }

    [Fact]
    public void PreOrder_GrandchildBeforeUncle_ProvesDepthFirstNotBreadthFirst()
    {
        // Root R(id=10). R has two children: C1(id=20) and C2(id=30). C1 has a grandchild G(id=5)
        // whose id is LOWER than uncle C2(id=30). DFS (pre-order) = [10,20,5,30]; BFS = [10,20,30,5];
        // flat-by-id = [5,10,20,30]. Only the correct DFS helper yields [10,20,5,30].
        //   L0: R(id=10,DO=1)
        //   L1: C1(id=20,parent=10,DO=1), C2(id=30,parent=10,DO=2)
        //   L2: G(id=5,parent=20,DO=1)
        var flat = new (int, int?, int, string)[]
        {
            (10, null, 1, "R"),
            (20, 10, 1, "C1"), (30, 10, 2, "C2"),
            (5, 20, 1, "G"),
        };
        var built = OrgTreePreOrder.BuildPreOrder(flat);
        var order = built.Select(x => x.Id).ToList();
        Assert.Equal(new[] { 10, 20, 5, 30 }, order);   // R, C1, G(grandchild), C2(uncle) — DFS
        Assert.NotEqual(new[] { 10, 20, 30, 5 }, order); // NOT BFS
        Assert.Equal(2, built.Single(x => x.Id == 5).Depth);   // grandchild G → Depth 2
    }

    // ── Phase 403 ORG-01/02: OrganizationController sadar junction UserUnits ──────
    // Pitfall 2: fixture diskriminatif WAJIB seed IsActive=false (rename harus ikut, guard tidak).
    // Pitfall 5: casing nama unit IDENTIK (InMemory case-sensitive).

    [Fact]
    public async Task EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        // mirror primary scalar (Invariant #3): u1 primary "Alkylation"
        ctx.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Unit = "Alkylation" });
        ctx.UserUnits.AddRange(
            new UserUnit { UserId = "u1", Unit = "Alkylation", IsPrimary = true,  IsActive = true },   // primary aktif
            new UserUnit { UserId = "u2", Unit = "Lain",       IsPrimary = true,  IsActive = true },   // non-match (jangan ikut rename)
            new UserUnit { UserId = "u2", Unit = "Alkylation", IsPrimary = false, IsActive = true },   // sekunder aktif
            new UserUnit { UserId = "u3", Unit = "Alkylation", IsPrimary = false, IsActive = false }); // diskriminatif (IsActive=false)
        ctx.SaveChanges();

        var result = await ctrl.EditOrganizationUnit(2, "Alkylation New", 1);   // rename Level1, parent unchanged

        Assert.True(GetSuccess(result));
        Assert.Equal(3, ctx.UserUnits.Count(uu => uu.Unit == "Alkylation New"));  // SEMUA 3 baris (incl IsActive=false)
        Assert.Equal(0, ctx.UserUnits.Count(uu => uu.Unit == "Alkylation"));      // 0 tersisa
        Assert.Equal(1, ctx.UserUnits.Count(uu => uu.Unit == "Lain"));            // non-match utuh
        Assert.True(ctx.UserUnits.Single(uu => uu.UserId == "u1" && uu.Unit == "Alkylation New").IsPrimary); // primary tetap
        Assert.Equal("Alkylation New", ctx.Users.Single(u => u.Id == "u1").Unit); // mirror konsisten
    }

    [Fact]
    public async Task DeleteOrganizationUnit_SecondaryMembershipActive_Rejected()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        // kasus murni sekunder: TIDAK ada scalar Users.Unit/Section == "Alkylation"
        ctx.UserUnits.Add(new UserUnit { UserId = "u1", Unit = "Alkylation", IsPrimary = false, IsActive = true });
        ctx.SaveChanges();

        var result = await ctrl.DeleteOrganizationUnit(2);

        Assert.False(GetSuccess(result));
        Assert.Contains("sekunder", GetMessage(result));
        Assert.NotNull(ctx.OrganizationUnits.Find(2));   // unit MASIH ada
    }

    [Fact]
    public async Task ToggleOrganizationUnitActive_SecondaryMembershipActive_Rejected()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = "u1", Unit = "Alkylation", IsPrimary = false, IsActive = true });
        ctx.SaveChanges();

        var result = await ctrl.ToggleOrganizationUnitActive(2);   // deactivate-branch

        Assert.False(GetSuccess(result));
        Assert.Contains("sekunder", GetMessage(result));
        Assert.True(ctx.OrganizationUnits.Find(2)!.IsActive);   // MASIH aktif
    }

    [Fact]
    public async Task EditOrganizationUnit_ReparentSplitsWorker_Blocked()
    {
        var (ctrl, ctx) = MakeController();
        // 2 Bagian + 2 unit anak RFCC (semua IsActive=true → masuk GetSectionUnitsDictAsync)
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 5, Name = "HSC",  Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true },
            new OrganizationUnit { Id = 6, Name = "X",          Level = 1, ParentId = 1, IsActive = true });
        ctx.Users.Add(new ApplicationUser { Id = "p1", UserName = "p1", NIP = "12345", FullName = "Budi", Unit = "Alkylation", Section = "RFCC" });
        ctx.UserUnits.AddRange(
            new UserUnit { UserId = "p1", Unit = "Alkylation", IsPrimary = true,  IsActive = true },
            new UserUnit { UserId = "p1", Unit = "X",          IsPrimary = false, IsActive = true });  // unit-lain Bagian RFCC
        ctx.SaveChanges();

        var result = await ctrl.EditOrganizationUnit(2, "Alkylation", 5);   // reparent Alkylation → HSC (split: "X" tetap RFCC)

        Assert.False(GetSuccess(result));
        var msg = GetMessage(result);
        Assert.True(msg.Contains("12345") || msg.Contains("Budi"), $"Pesan harus sebut NIP/nama: {msg}");
    }

    [Fact]
    public async Task EditOrganizationUnit_ReparentSingleUnitWorker_Allowed()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 5, Name = "HSC",  Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.Users.Add(new ApplicationUser { Id = "p1", UserName = "p1", Unit = "Alkylation", Section = "RFCC" });
        ctx.UserUnits.Add(new UserUnit { UserId = "p1", Unit = "Alkylation", IsPrimary = true, IsActive = true });   // single-unit
        ctx.SaveChanges();

        var result = await ctrl.EditOrganizationUnit(2, "Alkylation", 5);   // reparent → HSC (tanpa split)

        Assert.True(GetSuccess(result));
        Assert.Equal("HSC", ctx.Users.Single(u => u.Id == "p1").Section);   // cascade Section existing dipertahankan
    }

    [Fact]
    public async Task PreviewEditCascade_RenameLevel1_UserUnitsCountMatchesActual()
    {
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.UserUnits.AddRange(
            new UserUnit { UserId = "u1", Unit = "Alkylation", IsPrimary = true,  IsActive = true },
            new UserUnit { UserId = "u2", Unit = "Alkylation", IsPrimary = false, IsActive = true },
            new UserUnit { UserId = "u3", Unit = "Alkylation", IsPrimary = false, IsActive = false }); // diskriminatif (count tanpa filter IsActive)
        ctx.SaveChanges();

        // ACT1: preview (read-only)
        var preview = await ctrl.PreviewEditCascade(2, "Alkylation New", 1);
        int pUserUnits = GetInt(preview, "affectedUserUnitsCount");

        // ACT2: edit aktual pada ctx sama
        await ctrl.EditOrganizationUnit(2, "Alkylation New", 1);
        int aUserUnits = ctx.UserUnits.Count(uu => uu.Unit == "Alkylation New");

        Assert.Equal(aUserUnits, pUserUnits);   // preview == actual
        Assert.Equal(3, pUserUnits);            // SEMUA baris (tanpa filter IsActive)
    }

    // ── Phase 426 AUDIT-01: jejak audit EditOrganizationUnit (T1-T5) ─────────────
    // T1-T4 pakai MakeControllerWithUser (UserManager non-null → audit nulis baris).
    // T5 pakai MakeController() existing (null userManager → audit lempar NRE → swallow).

    private static ApplicationUser SeedActor() =>
        new ApplicationUser { Id = "hc1", UserName = "hc1", NIP = "99001", FullName = "Admin HC" };

    [Fact]
    public async Task EditOrganizationUnit_RenameLevel1_WritesOneAuditRow()   // T1 (SC#1, SC#2)
    {
        var (ctrl, ctx) = MakeControllerWithUser(SeedActor());
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Unit = "Alkylation" });
        ctx.UserUnits.AddRange(
            new UserUnit { UserId = "u1", Unit = "Alkylation", IsPrimary = true,  IsActive = true },
            new UserUnit { UserId = "u2", Unit = "Alkylation", IsPrimary = false, IsActive = true });
        ctx.SaveChanges();

        var result = await ctrl.EditOrganizationUnit(2, "Alkylation New", 1);   // rename Level1, parent unchanged
        Assert.True(GetSuccess(result));

        var rows = ctx.AuditLogs.Where(a => a.ActionType == "EditOrganizationUnit").ToList();
        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal(2, row.TargetId);
        Assert.Equal("OrganizationUnit", row.TargetType);
        Assert.Equal("99001 - Admin HC", row.ActorName);   // {NIP} - {FullName} seeded actor
        Assert.Equal("hc1", row.ActorUserId);
        Assert.Contains("'Alkylation'→'Alkylation New'", row.Description);   // oldName→newName
        Assert.Contains("cascade:", row.Description);                        // SC#2 cascade counts
    }

    [Fact]
    public async Task EditOrganizationUnit_Reparent_WritesParentIdsInDescription()   // T2 (SC#1, D-03)
    {
        var (ctrl, ctx) = MakeControllerWithUser(SeedActor());
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 5, Name = "HSC",  Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.Users.Add(new ApplicationUser { Id = "p1", UserName = "p1", Unit = "Alkylation", Section = "RFCC" });
        ctx.UserUnits.Add(new UserUnit { UserId = "p1", Unit = "Alkylation", IsPrimary = true, IsActive = true });   // single-unit → allowed
        ctx.SaveChanges();

        var result = await ctrl.EditOrganizationUnit(2, "Alkylation", 5);   // reparent → HSC (oldParent 1 → newParent 5)
        Assert.True(GetSuccess(result));

        var rows = ctx.AuditLogs.Where(a => a.ActionType == "EditOrganizationUnit").ToList();
        Assert.Single(rows);
        Assert.Contains("parent 1→5", rows[0].Description);   // raw IDs (D-03)
    }

    [Fact]
    public async Task EditOrganizationUnit_RenameAndReparent_WritesExactlyOneRow()   // T3 (D-02)
    {
        var (ctrl, ctx) = MakeControllerWithUser(SeedActor());
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 5, Name = "HSC",  Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.Users.Add(new ApplicationUser { Id = "p1", UserName = "p1", Unit = "Alkylation", Section = "RFCC" });
        ctx.UserUnits.Add(new UserUnit { UserId = "p1", Unit = "Alkylation", IsPrimary = true, IsActive = true });
        ctx.SaveChanges();

        // rename ("Alkylation"→"Alkylation X") + reparent (1→5) sekaligus
        var result = await ctrl.EditOrganizationUnit(2, "Alkylation X", 5);
        Assert.True(GetSuccess(result));

        var rows = ctx.AuditLogs.Where(a => a.ActionType == "EditOrganizationUnit").ToList();
        Assert.Single(rows);   // SATU baris gabungan, bukan dua (D-02)
        Assert.Contains("'Alkylation'→'Alkylation X'", rows[0].Description);
        Assert.Contains("parent 1→5", rows[0].Description);
    }

    [Fact]
    public async Task EditOrganizationUnit_NoChange_WritesZeroAuditRows()   // T4 (D-01)
    {
        var (ctrl, ctx) = MakeControllerWithUser(SeedActor());
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.SaveChanges();

        // no-op edit: nama + parent IDENTIK → commit sukses TAPI tidak menulis audit
        var result = await ctrl.EditOrganizationUnit(2, "Alkylation", 1);
        Assert.True(GetSuccess(result));
        Assert.Equal(0, ctx.AuditLogs.Count(a => a.ActionType == "EditOrganizationUnit"));   // only-on-change (D-01)
    }

    [Fact]
    public async Task EditOrganizationUnit_AuditFailure_DoesNotBlockEdit()   // T5 (SC#3)
    {
        // Factory EXISTING null-userManager → blok audit lempar NRE → swallow → edit tetap sukses.
        var (ctrl, ctx) = MakeController();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "RFCC", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "Alkylation", Level = 1, ParentId = 1, IsActive = true });
        ctx.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Unit = "Alkylation" });
        ctx.UserUnits.Add(new UserUnit { UserId = "u1", Unit = "Alkylation", IsPrimary = true, IsActive = true });
        ctx.SaveChanges();

        var result = await ctrl.EditOrganizationUnit(2, "Alkylation New", 1);   // rename nyata

        Assert.True(GetSuccess(result));   // respons tak terblokir oleh kegagalan audit (NRE di-swallow)
        Assert.Equal("Alkylation New", ctx.Users.Single(u => u.Id == "u1").Unit);              // cascade sukses
        Assert.Equal(1, ctx.UserUnits.Count(uu => uu.Unit == "Alkylation New"));               // UserUnits ter-rename
        Assert.Equal(0, ctx.AuditLogs.Count(a => a.ActionType == "EditOrganizationUnit"));      // tidak ada baris (audit gagal & di-swallow)
    }
}
