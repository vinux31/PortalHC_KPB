# Project Research Summary

**Project:** PortalHC KPB — v3.17 Assessment Sub-Competency Analysis
**Domain:** Assessment visualization (sub-competency tagging + radar chart)
**Researched:** 2026-03-10
**Confidence:** HIGH

## Executive Summary

This is a small, focused enhancement to an existing ASP.NET Core assessment portal: add a SubCompetency string tag to exam questions, flow it through Excel import, compute per-sub-competency scores server-side, and render a Chart.js radar chart on the Results page. No new packages, tables, controllers, or endpoints are needed. Every technology required is already in the codebase.

The recommended approach is strictly sequential: DB migration first, then import update, then scoring logic and UI. The architecture is intentionally simple — a nullable string field, LINQ GroupBy for scoring, and a conditional Chart.js radar chart. This avoids over-engineering traps like master data CRUD for sub-competencies or client-side score calculation.

The primary risks are (1) breaking backward compatibility for existing Excel templates and historical assessment data with NULL sub-competency values, (2) the import fingerprint dedup system silently blocking re-imports with new sub-competency data, and (3) radar chart edge cases with fewer than 3 or more than 8 axes. All are preventable with nullable fields, fingerprint updates, and conditional rendering logic.

## Key Findings

### Recommended Stack

No additions needed. The existing stack handles everything.

**Core technologies (all existing):**
- **EF Core 8.0:** New migration adding nullable string column to PackageQuestion
- **Chart.js (CDN, already in _Layout.cshtml):** Radar chart type is built-in, no plugins needed
- **Existing Excel library:** Add one column to template and parsing logic

### Expected Features

**Must have (table stakes):**
- SubCompetency field on PackageQuestion (DB migration)
- Excel import parses "Sub Kompetensi" column
- Summary table on Results page (Sub Kompetensi / Benar / Total / %)
- Radar chart on Results page
- Graceful handling of untagged questions (NULL SubCompetency)

**Should have (differentiators, if cheap):**
- Color-coded pass/fail per radar axis (LOW complexity)
- Comparative radar overlay for retake comparison (MEDIUM complexity)

**Defer (v2+):**
- Sub-competency breakdown in AssessmentMonitoring (HIGH complexity, different controller)
- PDF export of analysis card
- Sub-competency master data CRUD (anti-feature — keep as free-text)

### Architecture Approach

Extend existing models and flows with minimal surface area. Add `string? SubCompetency` to PackageQuestion, extend the ViewModel with `List<SubCompetencyScore>`, compute scores in CMPController.Results via LINQ GroupBy, render conditionally in Results.cshtml. Two independent tracks after migration: import update and scoring logic, converging at the Results view.

**Major components modified:**
1. **PackageQuestion model** — add SubCompetency field + migration
2. **AdminController import pipeline** — parse new column, update template
3. **CMPController.Results** — add sub-competency score aggregation
4. **Results.cshtml** — conditional radar chart + summary table

### Critical Pitfalls

1. **NULL handling for existing data** — Use nullable field, show "data not available" for historical sessions, never crash on missing sub-competency
2. **Import fingerprint dedup blocks re-import** — Update fingerprint hash to include SubCompetency so re-imports with new tags are recognized as updates
3. **Radar chart edge cases** — Require minimum 3 sub-competencies for radar; below that show table only. Cap readability at ~8 axes
4. **Case sensitivity in free-text tags** — Normalize casing during import ("komunikasi" vs "Komunikasi" must merge)
5. **Backward compatibility of import template** — Make "Sub Kompetensi" column optional; old 6-column templates must still work

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Data Model + Migration
**Rationale:** Everything depends on the SubCompetency field existing in the database
**Delivers:** PackageQuestion.SubCompetency nullable string column, SubCompetencyScore ViewModel class
**Addresses:** Core data model (table stakes)
**Avoids:** Pitfall 2 (NULL handling) by making field nullable from the start

### Phase 2: Excel Import Update
**Rationale:** Questions enter the system exclusively via import; no point building scoring without data
**Delivers:** Updated template with "Sub Kompetensi" column, updated parsing for both Excel and paste paths, case normalization
**Addresses:** Import feature (table stakes), backward compatibility
**Avoids:** Pitfall 5 (breaking change) by keeping column optional; Pitfall 4 (case sensitivity) by normalizing during import

### Phase 3: Scoring Logic + Results UI
**Rationale:** With data in the DB, compute and display. Scoring and UI are tightly coupled — ship together for end-to-end testability
**Delivers:** Per-sub-competency score calculation in CMPController.Results, radar chart, summary table, edge case handling
**Addresses:** Radar chart + summary table (table stakes), graceful degradation
**Avoids:** Pitfall 1 (cross-package normalization) by using percentages; Pitfall 3 (chart edge cases) by conditional rendering

### Phase Ordering Rationale

- Strictly sequential dependency chain: migration -> import -> scoring -> UI
- Phases 2 and 3 could technically start in parallel after Phase 1, but Phase 3 needs imported data to test end-to-end
- Three phases is the right granularity — fewer would be too large to review, more would be over-splitting a simple feature

### Research Flags

Phases with standard patterns (skip research-phase):
- **All phases:** This is well-trodden territory (EF migration, Excel parsing, Chart.js radar). No phase needs additional research. Patterns are fully documented in ARCHITECTURE.md with exact line numbers and code snippets.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All technologies already in codebase, verified by file inspection |
| Features | HIGH | Small scope, clear requirements, well-understood domain |
| Architecture | HIGH | Extends existing patterns with minimal new surface area |
| Pitfalls | HIGH | Based on direct codebase analysis of import dedup, shuffle, and Results flow |

**Overall confidence:** HIGH

### Gaps to Address

- **Import fingerprint logic:** Exact fingerprint hash implementation needs inspection during Phase 2 planning to determine how to include SubCompetency in dedup
- **Chart.js version:** CDN loads "latest" — should pin to specific version to avoid future breaking changes (minor, address in Phase 3)

## Sources

### Primary (HIGH confidence)
- Direct codebase analysis: `Models/AssessmentPackage.cs`, `Models/AssessmentResultsViewModel.cs`, `Controllers/AdminController.cs`, `Controllers/CMPController.cs`, `Views/CMP/Results.cshtml`
- `Views/Shared/_Layout.cshtml` line 168 — Chart.js CDN confirmed present

### Secondary (MEDIUM confidence)
- Chart.js radar chart API documentation (training data, stable API)

---
*Research completed: 2026-03-10*
*Ready for roadmap: yes*
