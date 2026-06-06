---
phase: 343-integrasi-app-wide
plan: 02
requirements-completed: [ORG-INTEG-01]
subsystem: views-cmp-cdp
tags: [razor, org-label, display-swap, cmp, cdp]
key-files:
  modified:
    - Views/CMP/AnalyticsDashboard.cshtml
    - Views/CMP/RecordsTeam.cshtml
    - Views/CDP/CertificationManagement.cshtml
    - Views/CDP/CoachingProton.cshtml
    - Views/CDP/HistoriProton.cshtml
    - Views/CDP/HistoriProtonDetail.cshtml
    - Views/CDP/PlanIdp.cshtml
    - Views/CDP/Shared/_CoachingProtonPartial.cshtml
    - Views/CDP/Shared/_CertificationManagementTablePartial.cshtml
metrics:
  tasks: 3
  commits: 3
  files: 9
  build: pass
---

# Phase 343 Plan 02 — Summary

## Objective
Swap hardcoded display label "Bagian"/"Unit" → `@OrgLabels.GetLabel(N)` di area CMP + CDP (ORG-INTEG-01 portion). Visible-text-only.

## Commits
| Task | Commit | Description |
|------|--------|-------------|
| 1 | `9b691477` | feat(343-02): swap CMP display label (AnalyticsDashboard + RecordsTeam) |
| 2 | `53399a1f` | feat(343-02): swap CDP views display label (5 views) |
| 3 | `80b2b451` | feat(343-02): swap CDP partials display label (inherit @inject) |

## What was built (9 file)
- **CMP** (2): AnalyticsDashboard (filter label L84/94, dropdown L86/96, heading L543, th L548), RecordsTeam (filter label L20/50, dropdown L24/41/52, th L135/136 + JS-rebuild placeholder L215).
- **CDP views** (5): CertificationManagement (label+dropdown+JS cascade L217/265), CoachingProton (dropdown L100/120 + JS cascade L1115), HistoriProton (label+dropdown+th L109 + JS cascade L234), HistoriProtonDetail (dt L36 + small L84 field labels — `<dd>@Model.Unit` value UNtouched), PlanIdp (label+placeholder `-- Pilih X --` + JS cascade L181).
- **CDP partials** (2): _CoachingProtonPartial (label+dropdown), _CertificationManagementTablePartial (th Bagian+Unit). No re-@inject — inherit dari `_ViewImports.cshtml` (Plan 01); build hijau membuktikan inheritance hierarkis ke `Views/CDP/Shared/`.

## Deviations
- **JS-rendered dropdown-rebuild placeholders swapped (extension beyond literal REPLACE table).** RecordsTeam:215, CertificationManagement:217/265, CoachingProton:1115, HistoriProton:234, PlanIdp:181 — `innerHTML = '<option ...>Semua/Pilih X</option>'` di `<script>` Razor. Ini display user-visible yang dirender server-side via `@` (sibling `@Html.Raw` mengkonfirmasi `@` live di tiap script block). Di-swap untuk konsistensi SC3 ("no hardcode display tersisa") + memenuhi acceptance criterion RecordsTeam "does NOT contain `Semua Unit`". RISK rendah: `@OrgLabels.GetLabel(N)` resolve server-side jadi string valid dalam JS literal; build hijau. **SKIP tetap dipertahankan:** identifier (id `filterBagian`, JS var `selectedBagian`/`unitsByBagian`/func `onBagianChange`), property `@Model.Unit`/`@node.Unit`/`@row.Unit`, Razor comment (CoachingProton:95), combined prose (PlanIdp:212 "Pilih Bagian, Unit, dan Track").

## Verification
- `dotnet build` → **0 Errors** (21 pre-existing nullable warnings) setelah Task 1 (CMP) dan setelah Task 2+3 (CDP).
- Residual grep 9 file: `Semua Bagian`/`Semua Unit`/`-- Pilih Bagian/Unit --`/`>Bagian</label>`/`>Unit</th>`/`>Unit</dt>` = **0**.
- SKIP guard CMP: `id="filterBagian"`/`id="filterUnit"`/`id="sectionFilter"`/`id="unitFilter"` STILL present. PlanIdp `id="silabusBagian"` present.
- Partials: 0 duplicate `@inject IOrgLabelService` (inherit).

## Self-Check: PASSED
- 9 file CMP/CDP label tier dinamis via `@OrgLabels.GetLabel(N)`; id/JS-identifier/property/data/comment untouched.
- Build hijau (partial inheritance terbukti). ORG-INTEG-01 (CMP/CDP portion) terpenuhi.
