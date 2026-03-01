# Phase 59: Hapus Page ProtonCatalog - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Delete ProtonCatalogController and Views/ProtonCatalog/ entirely. Phase 51 already moved all Proton Catalog CRUD functionality to /Admin/ProtonData (Silabus & Coaching Guidance tabs). ProtonCatalogController was stripped to redirect-only in Phase 51-03. This phase removes the leftover redirect controller and view files, plus cleans up any stale references.

</domain>

<decisions>
## Implementation Decisions

### Redirect behavior
- Delete ProtonCatalogController completely — no redirect preserved
- /ProtonCatalog will return 404 after deletion
- Rationale: Only internal admin users access this, and they already use /Admin/ProtonData via Kelola Data hub. No external bookmarks to preserve.

### Cleanup scope
- Delete ProtonCatalogController.cs
- Delete Views/ProtonCatalog/ directory (all files)
- Remove any navbar/menu links pointing to /ProtonCatalog
- Remove any Admin/Index hub card referencing ProtonCatalog (if still exists)
- Verify no other controllers or views link to /ProtonCatalog
- Do NOT touch /Admin/ProtonData (Phase 51 output) — that stays as-is

### Claude's Discretion
- Exact order of file deletions
- How thorough the stale reference search should be (grep is sufficient)
- Whether to check for any ProtonCatalog-specific CSS/JS (unlikely but worth a quick check)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — straightforward deletion and cleanup.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 59-hapus-page-protoncatalog*
*Context gathered: 2026-03-01*
