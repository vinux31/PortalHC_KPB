# Project Research Summary

**Project:** PortalHC KPB — v3.9 ProtonData Enhancement
**Domain:** Admin tooling for Proton silabus management
**Researched:** 2026-03-07
**Confidence:** HIGH

## Executive Summary

This milestone enhances the existing ProtonData admin page with four features: a Status tab showing silabus/guidance completeness as a tree checklist, a Target column on the silabus table, a hard delete button for Kompetensi records, and an audit of silabus consumer connections. All four features build on the existing ProtonDataController and require zero new dependencies — the current stack (ASP.NET Core 8, EF Core, Bootstrap 5, vanilla JS) handles everything.

The recommended approach is to build in order of risk: Target column first (simple migration, zero risk), Status tab second (read-only aggregation, medium complexity), then hard delete last (destructive operation requiring manual cascade due to FK Restrict constraints). The audit task folds into the delete phase as a prerequisite step rather than a standalone phase.

The primary risk is the hard delete feature. All Proton entity FK relationships use `DeleteBehavior.Restrict`, so deletion must be done bottom-up in application code within a transaction. More importantly, deleting a Kompetensi permanently destroys coachee progress records. The safest approach is to block deletion when progress records exist, limiting hard delete to incorrectly entered master data with no user progress attached.

## Key Findings

### Recommended Stack

No new packages needed. All features use patterns already established in the codebase.

**Core technologies (unchanged):**
- EF Core 8.0: Migration for Target column, manual cascade delete logic
- Bootstrap 5 accordion: Nested tree UI for Status tab (3 levels deep, no JS tree library needed)
- Vanilla JS: AJAX fetch for Status data endpoint, server-first delete pattern

### Expected Features

**Must have (table stakes):**
- Target column on ProtonSubKompetensi — free-text field, nullable, displayed between SubKompetensi and Deliverable columns
- Hard delete for Kompetensi — bottom-up cascade in transaction, only for inactive Kompetensi with zero progress records
- Confirmation dialog with impact counts before any hard delete

**Should have (differentiators):**
- Status tab as first/default tab — tree checklist showing silabus + guidance completeness per Bagian/Unit/Track
- Summary counts at each tree level (e.g., "RFCC: 4/18 complete")

**Defer (v2+):**
- Bulk hard delete (too dangerous, single-item only)
- Undo/restore for hard-deleted data (soft-delete already covers reversible removal)
- Evidence file cleanup on hard delete (orphan files are acceptable for now)

### Architecture Approach

All new features stay within ProtonDataController. Status tab uses a new AJAX endpoint (`StatusData`) returning JSON, rendered client-side as a Bootstrap accordion tree. Target column adds a nullable string to ProtonSubKompetensi with propagation through SilabusRowDto and SilabusSave. Hard delete adds a `SilabusKompetensiDelete` POST action with explicit bottom-up removal in a DB transaction.

**Major components:**
1. ProtonDataController.StatusData — JSON endpoint returning completeness tree aggregated from ProtonKompetensi hierarchy + CoachingGuidanceFiles
2. ProtonSubKompetensi.Target — nullable string column (max 500 chars), integrated into existing SilabusSave batch upsert
3. ProtonDataController.SilabusKompetensiDelete — manual cascade delete with pre-check for progress records, uses existing SilabusKompetensiRequest DTO

### Critical Pitfalls

1. **FK Restrict blocks naive delete** — All Proton FKs use Restrict. Must delete bottom-up: Progress -> Deliverable -> SubKompetensi -> Kompetensi, wrapped in a transaction.
2. **Hard delete destroys coachee progress permanently** — Block delete when ProtonDeliverableProgress records exist. Only allow hard delete on Kompetensi with zero progress.
3. **Target column NULL handling** — Use nullable string (`string?`), display empty input for NULL in edit mode, dash in view mode. Existing rows get NULL automatically.
4. **Status tab performance** — Filter by Bagian/Unit/Track (same dropdowns as Silabus tab). Use projection queries, not Include chains.
5. **Delete button JS state desync** — Call server first, update DOM only on success response. Never optimistically remove rows.

## Implications for Roadmap

### Phase 1: Target Column + Migration
**Rationale:** Lowest risk, simplest change, immediate value for silabus editing. Unblocks any UI work that depends on the updated table structure.
**Delivers:** New `Target` nullable string column on ProtonSubKompetensi; updated SilabusSave to persist Target; updated silabus table UI with Target column.
**Addresses:** Target column feature
**Avoids:** NULL handling pitfall (use nullable string, handle in UI)

### Phase 2: Status Tab
**Rationale:** Read-only feature with no data mutation risk. Can be built independently after Phase 1.
**Delivers:** New Status tab (first/default tab) with tree checklist showing silabus + guidance completeness per Bagian/Unit/Track. AJAX endpoint returning JSON aggregation.
**Addresses:** Status tab feature, completeness visibility
**Avoids:** Performance pitfall (filtered queries, projection DTOs, no ViewBag)

### Phase 3: Hard Delete + Consumer Audit
**Rationale:** Highest risk feature, must come last. Audit of consumer connections is a prerequisite step folded into this phase rather than a separate phase.
**Delivers:** Hard delete button for inactive Kompetensi (blocked when progress exists); consumer audit confirming no broken references; confirmation dialog with impact counts.
**Addresses:** Delete button feature, audit connections feature
**Avoids:** FK Restrict pitfall (manual cascade), progress data loss pitfall (block when progress exists), JS desync pitfall (server-first pattern)

### Phase Ordering Rationale

- Target column first because it is a simple additive migration with zero risk and no dependencies on other features
- Status tab second because it is read-only and independent, but slightly more complex (new endpoint + JS tree rendering)
- Hard delete last because it is destructive, needs the consumer audit as input, and benefits from the developer having already worked in the ProtonData codebase during phases 1-2

### Research Flags

Phases with standard patterns (skip research-phase):
- **Phase 1 (Target Column):** Standard EF Core migration + column add. Well-established pattern in this codebase.
- **Phase 2 (Status Tab):** Standard AJAX endpoint + Bootstrap accordion. No novel patterns.
- **Phase 3 (Hard Delete):** The cascade logic is well-documented in this research. The audit findings are already captured in FEATURES.md. No further research needed.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | No new packages; all patterns already in codebase |
| Features | HIGH | All based on direct codebase analysis of models, controllers, FK config |
| Architecture | HIGH | Extends existing ProtonDataController with established patterns |
| Pitfalls | HIGH | FK Restrict behavior confirmed from ApplicationDbContext.cs; cascade order verified |

**Overall confidence:** HIGH

### Gaps to Address

- **"Complete" definition for Status tab:** Research identified ambiguity. Recommendation: define "complete" as silabus exists (active Kompetensi with children) AND guidance files exist for that Bagian/Unit/Track. Confirm with user during Phase 2 planning.
- **Target column type:** FEATURES.md says free-text string, PITFALLS.md mentions `int?` in some examples. Recommendation: use `string?` (nvarchar 500) as FEATURES.md and ARCHITECTURE.md consistently recommend. The `int` references in PITFALLS.md appear to be generic examples.
- **Evidence file cleanup on hard delete:** Deferred. Orphaned files on disk are acceptable for now; can be addressed in a future cleanup phase.

## Sources

### Primary (HIGH confidence)
- Direct codebase analysis: `Models/ProtonModels.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProtonDataController.cs`, `Controllers/CDPController.cs`
- FK configuration: ApplicationDbContext.cs lines 279-331 (all Restrict)
- OrganizationStructure: `Models/OrganizationStructure.cs`
- Existing patterns: `SilabusSave`, `SilabusDelete`, `SilabusKompetensiToggle` actions

---
*Research completed: 2026-03-07*
*Ready for roadmap: yes*
