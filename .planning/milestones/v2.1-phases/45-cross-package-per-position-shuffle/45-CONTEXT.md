# Phase 45: Cross-Package Per-Position Shuffle - Context

**Gathered:** 2026-02-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Replace single-package assignment shuffle with per-position cross-package selection. Instead of assigning one worker to one full package, each question position independently and randomly draws from which package's question to show — so workers receive a unique mix of questions from multiple packages.

Grading, auto-save, session resume, and reshuffle eligibility rules are unchanged. Only the shuffle generation logic changes.

</domain>

<decisions>
## Implementation Decisions

### Question position ordering
- Display positions are always sequential: Soal No. 1, 2, 3, ... N (never reordered)
- What is randomized: which PACKAGE each position's question comes from
- When position i picks Package X → take Package X's question at index i (same row, different column)
- Example (3 packages, 3 questions each):
  - Worker Andi: No.1→PackageA, No.2→PackageC, No.3→PackageB
  - Worker Budi: No.1→PackageB, No.2→PackageA, No.3→PackageC
  - Worker Cici: No.1→PackageC, No.2→PackageB, No.3→PackageA
- This is "shuffle menyamping" — same question number slot, different package column

### Cross-package distribution — guaranteed even spread
- Distribution is merata (even): 2 packages = 50/50, 3 packages = 33/33/34, 4 packages = 25/25/25/25
- If questions don't divide evenly (e.g. 10 questions, 3 packages = 3+3+4): remainder allocated randomly to one package
- Each worker's distribution is independent — no coordination between workers
- Result: ShuffledQuestionIds JSON contains question IDs from potentially different packages, one per position

### Option shuffle (A/B/C/D)
- **Removed** — options are no longer shuffled per worker
- Options displayed in original DB order
- ShuffledOptionIdsPerQuestion field becomes unused/deprecated
- Grading still uses PackageOption.Id (unchanged)

### Package count validation
- Validated at import time: when importing questions to Package B, system checks if count matches Package A (and any other existing packages with questions)
- Empty packages (0 questions) are excluded from validation — HC can build packages one at a time
- If count mismatch detected at import: block with message "Jumlah soal tidak sama dengan paket lain. Paket A: 10 soal. Harap masukkan 10 soal."
- Safety fallback at StartExam: if mismatch still exists at exam start (edge case), use minimum question count across all packages

### Single-package behavior (1 package)
- 1 package = questions shown in original DB order 1→N (no shuffle at all)
- No cross-package selection to perform, no Fisher-Yates applied
- Workers all see the same question order

### HC visibility on management page
- ManagePackages page displays a package summary panel: package name + question count + status (OK ✓ / Warning ⚠️) per package
- Mode indicator shown: "Single Package" or "Multi-Package (N paket)"
- If count mismatch exists: warning shown but HC can still open the exam (system uses minimum count)
- HC monitoring (AssessmentMonitoringDetail) unchanged — no per-position package breakdown shown

### Import workflow
- No change to HC workflow — same Excel/text import process
- Validation added at import time: if importing to Package B and count differs from Package A (or any other non-empty package), import is blocked
- Error message: "Jumlah soal tidak sama dengan paket lain. Paket A: 10 soal. Harap masukkan 10 soal."
- Empty packages (0 questions) are excluded from validation — HC can build packages one at a time

### Migration on deploy
- All UserPackageAssignment records deleted on deploy — no exceptions (Completed, InProgress, Not Started all wiped)
- InProgress workers at deploy time will lose progress and restart with a new assignment under the new logic
- Clean break — no backward compatibility with old shuffle format

### Reshuffle behavior
- Reshuffle eligibility unchanged: only "Not started" workers can be reshuffled
- On reshuffle: regenerate cross-package selection using new logic (new independent draw per position)
- Single reshuffle and bulk reshuffle both use new logic

### Claude's Discretion
- How to handle `UserPackageAssignment.AssessmentPackageId` field (no longer meaningful — could store first package ID, null, or be left as nullable)
- Algorithm implementation for guaranteed-even distribution (e.g. populate list with N/packages count per package, shuffle the list, assign positions)
- Whether to keep `ShuffledOptionIdsPerQuestion` as empty JSON or remove from new assignments
- Error handling if all packages are empty when exam starts

</decisions>

<specifics>
## Specific Ideas

- N packages with K questions each: build assignment list [A×(K/N), B×(K/N), ...], shuffle the list, then for position i: take question at position i from the package assigned to that slot
- "Lupakan system shuffle yang lama" — this is a full replacement, not a backward-compatible extension
- The user explicitly confirmed: all packages must have the same question count (validated at import)

</specifics>

<deferred>
## Deferred Ideas

- Button "Start assessment" (manual HC trigger to open exam) — new capability, belongs in its own phase

</deferred>

---

*Phase: 45-cross-package-per-position-shuffle*
*Context gathered: 2026-02-25*
