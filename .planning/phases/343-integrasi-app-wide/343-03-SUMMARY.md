---
phase: 343-integrasi-app-wide
plan: 03
requirements-completed: [ORG-INTEG-01]
subsystem: views-protondata-admin-worker
tags: [razor, org-label, display-swap, protondata, admin-worker]
key-files:
  modified:
    - Views/ProtonData/Index.cshtml
    - Views/ProtonData/Override.cshtml
    - Views/Admin/CoachCoacheeMapping.cshtml
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml
    - Views/Admin/ManageWorkers.cshtml
    - Views/Admin/WorkerDetail.cshtml
    - Views/Admin/RenewalCertificate.cshtml
    - Views/Admin/Shared/_TrainingRecordsTab.cshtml
metrics:
  tasks: 2
  commits: 2
  files: 9
  build: pass
---

# Phase 343 Plan 03 — Summary

## Objective
Swap hardcoded display label "Bagian"/"Unit" → `@OrgLabels.GetLabel(N)` di area ProtonData + Admin worker-domain (ORG-INTEG-01 portion).

## Commits
| Task | Commit | Description |
|------|--------|-------------|
| 1 | `a7a3a373` | feat(343-03): swap ProtonData + CoachCoacheeMapping display label |
| 2 | `6e5531ef` | feat(343-03): swap Admin worker views display label (6 file) |

## What was built (9 file)
- **ProtonData** (2): Index (form label+placeholder L79/81/85/87 + guidance L230/232/236/238 + JS cascade L408/424), Override (label+placeholder L29/31/35/37 + JS cascade L194).
- **CoachCoacheeMapping** (1): combined-phrase "X Penugasan" th L235/236 + form label L435/445/503/513 (`@OrgLabels.GetLabel(N) Penugasan`, suffix + `<span>*` dipertahankan) + placeholder `— Pilih X —` L437/447/505/515 + JS rebuild L650.
- **Admin worker** (6): CreateWorker/EditWorker (placeholder `-- Pilih X --`; label `asp-for` empty = SKIP; per-view `@inject IConfiguration` untouched), ManageWorkers (filter label+dropdown+th L226/227), WorkerDetail (detail field label `<td class="text-muted">` L89/102 — sel nilai `@Model.Section/@Model.Unit` UNtouched), RenewalCertificate (filter+dropdown+JS cascade L228/275), _TrainingRecordsTab partial (filter+dropdown+th L194; inherit @inject, no re-inject).

## Deviations
- **JS-rendered dropdown-rebuild placeholders + Unit placeholders swapped (extension beyond literal REPLACE table).** ProtonData/Index:408/424, Override:194, CoachCoacheeMapping:650, RenewalCertificate:228/275 (`innerHTML='...Pilih/Semua X...'`) + CoachCoacheeMapping Unit placeholders L447/515 (`— Pilih Unit —`, research table listed only Bagian placeholders). All = user-visible display rendered server-side via `@`. Swapped untuk konsistensi SC3. RISK rendah; build hijau.
- **SKIP dipertahankan ketat:** validation `alert('Pilih Bagian Penugasan.')` + `alert('Pilih Unit Penugasan.')` (CoachCoacheeMapping L723/724/805/806 — bukan rendered dropdown, validation message); JS var/func (`assignmentSection`, `onBagianChange`, `silabusBagian` id); property/data (`@Model.Section`, `@worker.Unit`, sel nilai detail); combined prose (ProtonData "Pilih Bagian, Unit, dan Track"); ImportSilabus import-doc (bukan target); per-view `@inject IConfiguration` CreateWorker.

## Verification
- `dotnet build` → **0 Errors** (21 pre-existing nullable warnings).
- Residual grep 9 file: `Semua Bagian`/`Semua Unit`/`-- Pilih X --`/`— Pilih X —`/`>Bagian</label>`/`<th class="p-3">Bagian</th>`/`<th>X Penugasan</th>`/`<td class="text-muted">Bagian</td>` = **0**.
- SKIP guards: `alert('Pilih Bagian Penugasan.')` STILL present (2×); `@inject ... IConfiguration` STILL present di CreateWorker (1×); 0 duplicate `@inject IOrgLabelService` di _TrainingRecordsTab (inherit).

## Self-Check: PASSED
- 9 file ProtonData + Admin worker-domain label tier dinamis; JS identifier/alert/property/data/per-view-service untouched.
- Build hijau (partial _TrainingRecordsTab inheritance terbukti). ORG-INTEG-01 (ProtonData + worker-domain portion) terpenuhi.
