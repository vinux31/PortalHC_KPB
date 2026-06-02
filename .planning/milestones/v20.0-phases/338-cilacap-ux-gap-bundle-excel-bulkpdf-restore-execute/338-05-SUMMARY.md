---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
plan: 05
subsystem: guardrail-sop
tags: [backup, template, powershell, linkedgroupid, naming-convention, sop]

requires:
  - phase: 336
    provides: NAMING-CONVENTION-SPEC + ROOT_CAUSE (Cilacap incident analysis)
provides:
  - REST-05 DB_HANDOFF_IT template + PowerShell backup script (versioned di repo)
  - REST-06 LinkedGroupId auto-pair via title pattern di CreateAssessment
  - REST-07 DEV_WORKFLOW Pre-Deploy Backup SOP section

affects: [v20.0 milestone CLOSE — Phase 338 final wave]

tech-stack:
  added: []
  patterns:
    - "Versioned Markdown template + PowerShell script di repo: systematize manual SOP"
    - "Regex auto-detect title pattern `{Stage}Test {rest}` untuk counterpart pairing"
    - "TempData[Info] flash message untuk admin notify auto-paired action"
    - ".gitignore exception !scripts/<specific>.ps1 untuk versioned operational script"

key-files:
  created:
    - docs/templates/DB_HANDOFF_IT.template.md
    - scripts/backup-dev-pre-migration.ps1
  modified:
    - Controllers/AssessmentAdminController.cs
    - docs/DEV_WORKFLOW.md
    - .gitignore

key-decisions:
  - "Script PowerShell standalone Windows Auth (-E flag sqlcmd) — NO credential hardcode (T-338-04)"
  - "Template Markdown bukan HTML — versioned-friendly, dev edit langsung di repo"
  - "Auto-pair scope only standard mode (AssessmentTypeInput != PrePostTest) — PrePost mode generate own GroupId"
  - "TryAutoDetectCounterpartGroup search 2 title variant (\"PreTest X\" + \"Pre Test X\") untuk tolerance whitespace inkonsistensi"
  - "Counterpart filter: LinkedGroupId != null (counterpart sudah punya group ID supaya pair-able)"
  - ".gitignore exception !scripts/backup-dev-pre-migration.ps1 — versioned per file, *.ps1 blanket-ignore tetap untuk utility scripts"

patterns-established:
  - "Operational artifact versioned di repo: template (docs/templates/) + script (scripts/) + SOP doc (docs/DEV_WORKFLOW.md)"
  - "Title pattern auto-detect Pre/Post counterpart via regex + DB lookup"

requirements-completed: [REST-05, REST-06, REST-07]

duration: ~30min
completed: 2026-05-30
---

# Phase 338-05 (FINAL): Guardrail + Naming + SOP Summary

**REST-05 + REST-06 + REST-07 SHIPPED LOCAL. 4 commit lokal. Phase 338 CLOSED 10/10 REQ.**

## Performance

- **Duration:** ~30 min (3 task + UAT)
- **Completed:** 2026-05-30
- **Files:** 2 NEW + 3 modified
- **Build status:** PASS 0 error

## Accomplishments

- **REST-05 D-06**: Template + script versioned operational artifact di repo
  - `docs/templates/DB_HANDOFF_IT.template.md` Markdown 6 section (Pre-Deploy Backup MANDATORY + Migration List + Affected Tables + Deployment Steps + Rollback Plan + Deployment Log)
  - `scripts/backup-dev-pre-migration.ps1` standalone PowerShell BACKUP DATABASE
  - `.gitignore` exception supaya script versioned (existing *.ps1 blanket-ignore preserve)
- **REST-06 (336-NAMING-CONVENTION-SPEC)**: LinkedGroupId auto-pair logic
  - CreateAssessment POST L832+ auto-detect counterpart Pre/Post via title regex
  - Private helper `TryAutoDetectCounterpartGroup` 2 variant title search
  - TempData["Info"] notify admin (manual override allowed)
- **REST-07**: DEV_WORKFLOW.md "Pre-Deploy Backup SOP" section append
  - Developer + IT steps end-to-end
  - Reference Files links template + script + naming spec + root cause + existing precedents
  - Lesson Learned section explain 3-layer guard

## Task Commits

1. **T1-338-05: REST-05 template + script + .gitignore** — `33a50580` (feat)
2. **T2-338-05: REST-06 LinkedGroupId auto-pair** — `8237cf13` (feat)
3. **T3-338-05: REST-07 DEV_WORKFLOW SOP** — `5286087b` (docs)

## Files Modified

- `docs/templates/DB_HANDOFF_IT.template.md` NEW (108 LOC) — 6 section Markdown template
- `scripts/backup-dev-pre-migration.ps1` NEW (90 LOC) — PowerShell standalone backup
- `.gitignore` (+2 LOC) — exception untuk backup script
- `Controllers/AssessmentAdminController.cs` (+46 LOC) — auto-pair logic L832 + TryAutoDetectCounterpartGroup helper
- `docs/DEV_WORKFLOW.md` (+68 LOC) — Pre-Deploy Backup SOP section + Lesson Learned

## UAT Verification

| REQ-ID | Status | Evidence |
|--------|--------|----------|
| REST-05 | ✅ PASS | Both files exist verified via ls. PowerShell parse PASS (Test-Path returns True). Template Markdown structure 6 section verified manual. |
| REST-06 | ✅ PASS | grep `TryAutoDetectCounterpartGroup` count=2 (insert call + method def). Build 0 error. Logic code-verified via inspect (regex pattern + DB query + TempData info). |
| REST-07 | ✅ PASS | grep `Pre-Deploy Backup SOP` count=1 di DEV_WORKFLOW.md. Section append verified end of file with proper formatting + Reference Files links + Lesson Learned. |

**Coverage:** 3/3 REQ code-verified. dotnet build PASS 0 error.

## Threats

| Threat ID | Status |
|-----------|--------|
| T-338-04 Backup script credential leak | mitigated (param mandatory + Windows Auth, no hardcoded password) |
| Title regex injection | mitigated (regex pattern char class restricted, EF parameterized query DB lookup) |
| LinkedGroupId orphan pair logic bug | mitigated (counterpart filter LinkedGroupId != null, auto-set only when null, manual override allowed) |
| Template stale info | accept (placeholder design — developer fill per-deployment, no auto-stale risk) |
| Script PATH dependency (sqlcmd) | mitigated (validate sqlcmd available, error gracefully bila not found) |

## Seed Workflow

- No temp seed (template + script + code + doc only)

## Lessons & Surprises

- **`.gitignore` blanket-ignore `*.ps1`** untuk utility scripts mencegah versioning script ini. Solution: `!scripts/backup-dev-pre-migration.ps1` exception — specific file path override blanket rule.
- **CreateAssessment.cshtml** TIDAK punya field `LinkedGroupId` langsung — auto-pair skip UI badge, gunakan TempData["Info"] flash message (existing _Layout render).
- **AssessmentTypeInput == "PrePostTest"** mode generate sendiri LinkedGroupId via Math.Max+1 logic existing — skip auto-pair untuk PrePost mode, focus standard mode (admin manual create 1 session yang berpasangan dengan existing).
- **Pre/Post counterpart** harus sudah punya LinkedGroupId untuk pair-able. Bila kedua belum ada LinkedGroupId, sistem TIDAK auto-create (admin manual decide pair atau standalone).
- Template Markdown vs HTML — pilih Markdown karena dev-friendly edit di IDE, mudah versioned diff, mudah render. HTML existing precedents (2026-05-13 + 2026-05-26) sebagai reference samples (preserve).

## Next

- **Phase 338 COMPLETE 10/10 REQ** (CIL-01..06 + REST-04..07)
- v20.0 milestone status: 4/4 phases SHIPPED LOCAL (335 + 336 + 337 + 338)
- REST-04 data execution (restore Cilacap 13 peserta) → IT promo Dev SEPARATE task
- v20.0 milestone CLOSE candidate → /gsd-complete-milestone v20.0
