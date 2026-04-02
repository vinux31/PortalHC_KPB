# Phase 286: AdminBaseController - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Membuat AdminBaseController sebagai abstract base class yang akan diwarisi oleh semua controller domain hasil pecahan AdminController. Base class menyediakan shared DI dan routing prefix `/Admin/`. Setelah phase ini, AdminController yang ada harus tetap berfungsi normal (zero regression).

</domain>

<decisions>
## Implementation Decisions

### Shared DI (4 dependencies di base)
- **D-01:** AdminBaseController menyediakan 4 shared dependencies: `ApplicationDbContext`, `UserManager<ApplicationUser>`, `AuditLogService`, `IWebHostEnvironment`
- **D-02:** Dependencies lain (IMemoryCache, IConfiguration, INotificationService, IHubContext, IWorkerDataService, ImpersonationService) tetap di-inject per domain controller yang membutuhkan
- **D-03:** `ILogger<T>` di-inject di masing-masing controller karena generic per class

### Helper method placement
- **D-04:** Base class TIDAK berisi helper methods — hanya DI
- **D-05:** `Shuffle()` + `BuildCrossPackageAssignment()` ikut pindah ke AssessmentAdminController (Phase 287)
- **D-06:** Proton Progress Helpers ikut pindah ke CoachMappingController (Phase 288)

### Routing strategy
- **D-07:** `[Route("Admin")]` dan `[Route("Admin/[action]")]` ditaruh di AdminBaseController — semua child controller otomatis mendapat prefix `/Admin/` tanpa perlu tulis attribute routing sendiri
- **D-08:** `[Authorize]` class-level tetap di AdminBaseController — semua child otomatis authenticated

### Claude's Discretion
- Nama class: AdminBaseController vs AdminControllerBase (konvensi ASP.NET)
- Apakah base class abstract atau tidak
- Constructor pattern (base constructor call)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — pure infrastructure refactoring. Yang penting:
- Zero regression — AdminController harus tetap berfungsi setelah mewarisi base
- Base class harus sesederhana mungkin — hanya DI + routing

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above.

### Existing code
- `Controllers/AdminController.cs` — Current monolith controller (8,514 baris, 11 DI dependencies, constructor line 34-58)
- `Controllers/AdminController.cs` lines 3821-3967 — Helper Methods region (Shuffle, BuildCrossPackageAssignment)
- `Controllers/AdminController.cs` lines 7352-7450 — Proton Progress Helpers region

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminController constructor (line 34-58): 11 dependencies yang akan dipecah 4 shared + 7 specific
- Existing `[Authorize]` class-level attribute (line 19)

### Established Patterns
- Controller inheritance: semua controller langsung inherit `Controller` — belum ada base class pattern
- Role authorization: mix of `[Authorize(Roles = "Admin, HC")]` per-action dan `[Authorize(Roles = "Admin")]` per-action
- Class-level `[Authorize]` (authenticated only) sudah ada di AdminController

### Integration Points
- AdminController saat ini di-reference oleh Views via `@{ ViewData["Controller"] = "Admin"; }` — ini tidak terpengaruh karena routing tetap `/Admin/`
- Semua `RedirectToAction()` calls dalam AdminController yang redirect ke action lain dalam AdminController — ini perlu perhatian saat pecah di phase berikutnya

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 286-adminbasecontroller*
*Context gathered: 2026-04-02*
