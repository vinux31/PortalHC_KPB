# Phase 328: Cascade Audit Sweep — Delete* Endpoints (Audit-Only) - Context

**Gathered:** 2026-05-27
**Status:** Ready for planning
**Source:** Brainstorm session 2026-05-27 → spec `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` (commit `02f620be`)

<domain>
## Phase Boundary

Phase 328 produces a single audit deliverable: `328-RESEARCH.md`. It enumerates every `Delete*` method in `Controllers/*.cs` + `Services/*.cs` and grades each against a 7-dimension cascade-safety checklist with severity tagging.

**No code is modified.** No migration. No test code. No phase fix spawn. The output is an internal dev artifact that becomes the input for follow-up fix-phase planning decisions (separate user-driven phases, not part of 328).

Estimated effort: 1-2 hours single session, read-only audit.

</domain>

<decisions>
## Implementation Decisions

### D-01 Phase Type — Audit-Only

This phase is read-only. No `.cs` / `.cshtml` / `.json` / `.sql` files are modified. No migrations generated. No tests authored. The single deliverable is markdown: `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md`.

### D-02 Endpoint Enumeration Scope

In-scope match pattern: `public async.*Delete\w+\(` (case-sensitive) executed against:
- `Controllers/*.cs`
- `Services/*.cs`

Out-of-scope (eksplisit in audit report's section 7):
- `UserManager.DeleteAsync` (Identity framework, separate concern)
- Soft-delete / `IsDeleted` flag (product decision, not audit)
- Concurrency / optimistic locking (v20.0+ concern)
- Idempotency / re-delete behavior
- Cascade in stored procedures (none confirmed in codebase)

Estimated row count: 15–25 endpoints. Final count is part of deliverable acceptance.

### D-03 7-Dimension Checklist (Locked)

Every endpoint row evaluates these 7 dimensions:

| # | Dimension | Pass criteria |
|---|-----------|---------------|
| 1 | Cascade FK risk | All `OnDelete(Restrict\|NoAction)` FKs pointing to target entity are handled app-level (null-clear or pre-delete) |
| 2 | File–DB atomicity | DB save before file delete, OR transaction wrap with rollback |
| 3 | AuditLog presence | `_auditLog.LogAsync(...)` call with action `"Delete"` + entity name + ID |
| 4 | Role/auth check | `[Authorize(Roles = "...")]` attribute present with appropriate role |
| 5 | Renewal chain null-clear | For `TrainingRecord`/`AssessmentSession`: child rows referencing target via `RenewsTrainingId`/`RenewsSessionId` are null-cleared before `Remove` |
| 6 | Error handling | `try/catch (DbUpdateException)` returning friendly `TempData["Error"]` |
| 7 | Transaction wrap | `BeginTransactionAsync()` for multi-step deletes (file + DB + cascade pre-clear) |

Per-row evidence cell: `file.cs:line` reference for each ✅/❌/⚠️ verdict.

### D-04 Severity Rubric (Locked)

- **HIGH** — Dim 1, 2, or 5 fail. Justification: data loss possible (FK violation 500; orphan file or row).
- **MED** — Dim 3, 4, or 6 fail. Justification: UX degradation (generic error, missing audit, missing role check).
- **LOW** — Dim 7 fail. Justification: hygiene only, rarely user-visible.

### D-05 Reference Gold Standard

Compare each endpoint pattern against `Controllers/AssessmentAdminController.DeleteAssessment` post-commit `f1849367` (Phase 323 final). Canonical remediation snippet copied into `328-RESEARCH.md` section 8 (`Remediation Pattern Template`).

### D-06 Pre-Audit HIGH Findings (Confirmed)

These 2 are already verified HIGH during brainstorm (acceptance gate #4):
- `Controllers/TrainingAdminController.cs:559` `DeleteTraining` — renewal chain bug + file-DB atomicity broken (drift +32 baris dari Phase 326 validators + Phase 327 DateOnly refactor; original brainstorm cite L527-548)
- `Controllers/TrainingAdminController.cs:793` `DeleteManualAssessment` — same pattern (drift +57 baris; original brainstorm cite L736-756)

These MUST appear under section 4 (HIGH Findings) with full evidence + repro path + remediation snippet.

**Note untuk auditor:** Re-grep fresh saat audit (line ref di atas dari refresh 2026-05-28). Untuk method body, baca ~20 baris dari signature L559 / L793 ke bawah sampai closing brace.

### D-07 Deliverable Document Structure (Locked)

```
# Phase 328 — Cascade Audit Sweep RESEARCH

## 1. Methodology
## 2. Endpoint Inventory (bash count + grep raw output)
## 3. Audit Table (flat row per endpoint × 7-dim + severity + remediation outline)
## 4. HIGH Findings (Detailed per endpoint with evidence + repro + fix snippet)
## 5. MED Findings (Summary table)
## 6. LOW Findings (Summary table)
## 7. Out-of-Scope Statement
## 8. Remediation Pattern Template (Phase 323 gold standard)
## 9. Recommended Next Phases (proposal only, not auto-spawned)
```

### D-08 Acceptance Criteria (Locked, all required)

1. `grep -E "public async.*Delete\w+\(" Controllers/*.cs Services/*.cs | wc -l` equals row count in section 3.
2. 7-dim cells filled per row with ✅/❌/⚠️ + 1-line evidence `file:line` ref.
3. Severity tag per row (HIGH / MED / LOW / NONE).
4. Section 4 contains ≥1 HIGH finding (renewal chain `DeleteTraining` pre-confirmed).
5. Section 7 lists Identity, soft-delete, concurrency, idempotency, stored procs.
6. Section 8 contains Phase 323 canonical fix pattern.
7. Final commit hash appended to v19.0 ROADMAP entry for phase 328.

### D-09 No Research Subagent Spawn

This phase does NOT spawn `gsd-phase-researcher`. The brainstorm spec (commit `02f620be`) IS the research artifact. Methodology already locked in D-01..D-08 above. RESEARCH.md is the AUDIT OUTPUT, not an upstream input.

### D-10 No Fix Phase Spawn

The audit's section 9 (Recommended Next Phases) is a PROPOSAL list, not an auto-spawn instruction. User decides phase fix priority after reading audit. Each fix phase (if pursued) is a separate `/gsd-add-phase` + `/gsd-plan-phase` cycle.

### D-11 Preview/Impact Pattern Noise (Auditor Methodology)

Refresh 2026-05-28: grep `public async.*Delete\w+\(` di Controllers/*.cs + Services/*.cs returns **19 hits** (estimate 15-25 di D-02 valid). Breakdown:

- **14 true delete endpoints** — actual cascade-modifying handlers
- **5 read-only preview/impact endpoints** — match regex tapi NOT delete operation:
  - `Controllers/ProtonDataController.cs:559` `SilabusDeletePreview`
  - `Controllers/ProtonDataController.cs:571` `SubKompetensiDeletePreview`
  - `Controllers/ProtonDataController.cs:586` `KompetensiDeletePreview`
  - `Controllers/CoachMappingController.cs:1114` `CoachCoacheeMappingDeletePreview`
  - `Controllers/AssessmentAdminController.cs:3911` `GetDeleteImpact`

**Auditor MUST include all 19 di Section 3 audit table** (per D-08 acceptance #1 `grep | wc -l == row count`). Tag 5 preview row sebagai severity **NONE** dengan evidence note `"read-only preview/impact — no DB delete, exempt from 7-dim eval"`. Tidak refine grep pattern (preserve acceptance criterion executable check).

**True endpoint count untuk 7-dim eval = 14.**

### Claude's Discretion

- Exact bash command shape for enumeration (PowerShell vs git-bash) — pick whichever works on Windows shell.
- Whether to group rows in audit table by controller file vs by severity — planner's choice based on readability.
- Whether to include line-of-code count per endpoint — optional metadata.
- Audit table column ordering as long as all 7 dims + severity + remediation are present.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Spec & Brainstorm
- `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` — full design spec (commit `02f620be`), sections 1-9
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` — parent v19.0 milestone spec

### Gold Standard Reference
- `Controllers/AssessmentAdminController.cs` (post-commit `f1849367`) — Phase 323 final cascade fix pattern; section 8 of audit report copies pattern from here
- `.planning/phases/323-fix-cascade-bug-assessmenteditlogs-di-3-endpoint-delete-asse/323-CONTEXT.md:122` — origin of "v19.0 Cascade Audit Sweep" defer note

### Source Files (Audit Targets)
- `Controllers/*.cs` (all controllers) — Delete* method enumeration scope
- `Services/*.cs` (all services with `_context.Remove`/`RemoveRange`) — Delete* method enumeration scope
- `Data/ApplicationDbContext.cs` — FK relationship definitions (Restrict/NoAction/Cascade) for D-03 Dim 1 evaluation

### Pre-Audit Evidence (Confirmed HIGH) — refreshed 2026-05-28 post-Phase-327
- `Controllers/TrainingAdminController.cs:559` — `DeleteTraining` (renewal chain + atomicity bug) [orig brainstorm 527-548, drift +32 baris]
- `Controllers/TrainingAdminController.cs:793` — `DeleteManualAssessment` (same pattern) [orig 736-756, drift +57 baris]
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml:327, 342` — UI surface exposing both endpoints (Input Records tab) [verified 2026-05-28]
- `Data/ApplicationDbContext.cs:157-165, 220-228` — renewal chain FK NoAction definitions (root cause of D-06)

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default; dev workflow; seed workflow

</canonical_refs>

<specifics>
## Specific Ideas

- Audit run is bash-grep + per-file Read tool reads. No agent spawn beyond planner + checker.
- Use Grep tool (ripgrep) with pattern `public async.*Delete\w+\(` for enumeration.
- For each endpoint, the auditor reads `Controllers/<File>.cs` around the method body and cross-references `Data/ApplicationDbContext.cs` to verify Dim 1 (FK risk).
- Final RESEARCH.md commit message format: `docs(328): cascade audit sweep RESEARCH — N endpoint, M HIGH, K MED, L LOW`.

</specifics>

<deferred>
## Deferred Ideas

- Auto-spawn fix phases per HIGH finding — DEFERRED (user-decided post-audit, see D-10).
- HTML stakeholder report — DEFERRED (audit is internal dev artifact, see Q4=A in brainstorm).
- IT_NOTIFY.md generation — DEFERRED (no code change, no IT promo needed).
- Identity `UserManager.DeleteAsync` audit — OUT-OF-SCOPE (framework concern).
- Soft-delete adoption analysis — DEFERRED (product decision, v20.0+).
- Concurrency / optimistic locking audit — DEFERRED (v20.0+).
- Stored procedure cascade audit — OUT-OF-SCOPE (none confirmed in codebase).

</deferred>

---

*Phase: 328-cascade-audit-sweep-delete-endpoints*
*Context gathered: 2026-05-27 via brainstorm + spec → CONTEXT.md generation*
*Source spec commit: `02f620be`*
*Refresh 2026-05-28: D-06 + canonical_refs file:line refreshed (drift dari Phase 326+327 edits); D-11 added (preview/impact pattern noise auditor methodology). Phase 327 SHIPPED LOCAL precondition met.*
