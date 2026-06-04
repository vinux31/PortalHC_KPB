// Phase 342 Plan 03 — OrganizationController dup-name per-parent + PreviewEditCascade accuracy.
// REQ coverage: ORG-TREE-02 (per-parent uniqueness), ORG-TREE-07 (preview count == actual cascade).
// Strategy: InMemory DB (Guid per test) + UserManager/env null-substitute (Add/Edit/Preview tidak deref).
// Pitfall 5: casing IDENTIK (InMemory case-sensitive vs SQL Server CI) — jangan andalkan case-insensitivity.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    // Asserts ONLY against the pure helper, NEVER against the flat GetOrganizationTree endpoint
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
}
