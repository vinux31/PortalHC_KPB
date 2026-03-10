# Pitfalls Research

**Domain:** Sub-competency tagging + radar chart on existing assessment system
**Researched:** 2026-03-10
**Confidence:** HIGH (based on codebase analysis of existing models and patterns)

## Critical Pitfalls

### Pitfall 1: Sub-competency score calculation ignores cross-package question sharing

**What goes wrong:**
Users in different packages may get different questions for the same sub-competency, or the same question appears in multiple packages. If sub-competency scoring assumes all users answer the same questions per sub-competency, radar charts become misleading — comparing users who answered different question sets.

**Why it happens:**
The system uses cross-package question assignment and Fisher-Yates shuffle. A sub-competency might have 5 questions in Paket A but 3 in Paket B. Developers calculate percentage scores without normalizing for question count per sub-competency per package.

**How to avoid:**
Always calculate sub-competency scores as percentage (correct/total for that sub-competency within that user's assigned package). Never use raw counts for comparison. The radar chart must show percentages, not absolute scores.

**Warning signs:**
- Radar chart axes have different max values across users
- Users with fewer questions in a sub-competency appear to score lower

**Phase to address:**
Score calculation phase (the phase that computes per-sub-competency results at exam submission time).

---

### Pitfall 2: Adding SubCompetency column without handling NULL for existing data

**What goes wrong:**
Migration adds `SubCompetency` to `PackageQuestion` but existing rows (from all prior assessments) have NULL. Results page crashes or shows empty radar chart for historical sessions. Admin re-import is required to backfill, but the fingerprint dedup system skips already-imported questions.

**Why it happens:**
The Excel import uses fingerprint-based deduplication. If you add SubCompetency to the model but the import fingerprint does not include it, re-importing the same questions with sub-competency tags will be treated as duplicates and skipped.

**How to avoid:**
1. Use a nullable `string? SubCompetency` column — do NOT make it required.
2. Update the import fingerprint hash to include SubCompetency so re-imports with new sub-competency data are recognized as updates (not new rows) and the field gets populated.
3. Results page must gracefully handle sessions where SubCompetency is NULL — show "Sub-competency data not available" instead of an empty/broken chart.

**Warning signs:**
- Re-importing Excel with sub-competency column does not update existing questions
- Historical assessment results show broken chart

**Phase to address:**
DB migration phase + import logic update phase (must be coordinated).

---

### Pitfall 3: Radar chart with too many or too few axes

**What goes wrong:**
Chart.js radar chart becomes unreadable with >8 axes (sub-competencies) or degenerates with <3. Some assessment sessions may have only 1-2 sub-competencies tagged, producing a meaningless line or dot instead of a polygon.

**Why it happens:**
Sub-competency count varies per assessment package. Developer builds a one-size-fits-all radar chart without considering edge cases.

**How to avoid:**
- Minimum 3 sub-competencies required to show radar chart; below that, show a bar chart or table only.
- For >8 sub-competencies, consider grouping or showing top-level competency aggregation with drill-down.
- Always show the summary table alongside the chart as a fallback.

**Warning signs:**
- Radar chart looks like a triangle with 3 points (acceptable) or a line with 2 points (broken)
- Chart is so dense with 12+ axes that labels overlap

**Phase to address:**
Results page UI phase (radar chart rendering).

---

### Pitfall 4: Shuffle breaks sub-competency grouping assumption

**What goes wrong:**
Developer assumes questions are displayed grouped by sub-competency (for UX or analysis), but Fisher-Yates shuffle randomizes all questions regardless of sub-competency. If someone later wants to show "section-by-section" analysis or the exam UI groups by sub-competency, the shuffle must be sub-competency-aware.

**Why it happens:**
The existing shuffle is a flat shuffle of all questions. Sub-competency is metadata for analysis, not for display grouping. But a future request to "show questions grouped by sub-competency" would conflict with the anti-cheating shuffle.

**How to avoid:**
Keep the flat shuffle as-is. Sub-competency tagging is purely for post-exam analysis. Do NOT change the exam flow or question ordering. Document this decision explicitly to prevent future confusion.

**Warning signs:**
- Requirements mentioning "show sub-competency sections during exam"
- Attempts to modify the shuffle algorithm

**Phase to address:**
Architecture decision — document in the first phase, enforce in all subsequent phases.

---

### Pitfall 5: Import template breaking change for existing users

**What goes wrong:**
Adding a "Sub Kompetensi" column to the Excel import template breaks existing workflows if the column is made mandatory. HC users who have saved old templates get import errors.

**Why it happens:**
Developer makes SubCompetency a required import column without considering backwards compatibility.

**How to avoid:**
Make the "Sub Kompetensi" column optional in the import parser. If absent or empty, set SubCompetency to NULL. Update the downloadable template to include the column, but don't reject uploads that lack it.

**Warning signs:**
- Import fails on old-format Excel files
- HC users complaining about template changes

**Phase to address:**
Import logic update phase.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Store sub-competency as free-text string | No master table needed, fast to implement | Typos create duplicate sub-competencies ("Komunikasi" vs "komunikasi") | Acceptable if import is the only entry point and you normalize casing during import |
| Calculate scores on-the-fly at Results page load | No storage migration for scores | Slow for large sessions, recalculated every page view | Never for production — cache or store at submit time |
| Hard-code Chart.js config in cshtml | Fast to ship | Cannot reuse for other charts, hard to maintain | Acceptable for v1 if only one chart exists |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Chart.js radar in existing Results page | Loading Chart.js globally, conflicting with existing scripts | Load Chart.js only on Results page, use specific version, check for conflicts with existing JS |
| Excel import with new column | Positional column parsing breaks when new column is inserted | Use header-name-based column lookup (match "Sub Kompetensi" header), not column index |
| PackageUserResponse join to get sub-competency | N+1 query: load responses, then load question, then load sub-competency | Single query: join PackageUserResponse -> PackageQuestion, group by SubCompetency |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Calculating sub-competency scores per-request | Results page loads slowly (>2s) | Pre-compute at exam submission, store in a summary table or JSON field | >50 questions per session |
| Loading all PackageQuestions with Options eagerly for score calc | Memory spike on server | Use projection query (SELECT only needed fields) | >500 questions per assessment |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Showing radar chart without context | Users don't know what 60% on "Komunikasi" means | Show benchmark/passing line on radar chart, or at minimum show passing threshold per axis |
| Radar chart only, no table | Users can't extract exact numbers | Always pair radar chart with summary table (Sub Kompetensi, Benar, Total, %) |
| Showing sub-competency analysis for incomplete exams | Misleading partial data | Only show analysis for completed/submitted exams |

## "Looks Done But Isn't" Checklist

- [ ] **Sub-competency scores:** Often missing normalization to percentage — verify scores are % not raw count
- [ ] **Radar chart:** Often missing the case where all sub-competencies score 0% (chart collapses to center point) — verify it still renders
- [ ] **Import template:** Often missing the downloadable template update — verify the new template includes "Sub Kompetensi" column
- [ ] **Historical data:** Often missing graceful degradation — verify Results page works for sessions imported before sub-competency was added
- [ ] **Case sensitivity:** Often missing normalization — verify "komunikasi" and "Komunikasi" are treated as the same sub-competency

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Fingerprint dedup blocks re-import with sub-competency | LOW | Update fingerprint logic, re-import affected packages |
| Free-text typos in sub-competency names | MEDIUM | Write a data cleanup migration to normalize existing values |
| Scores stored as raw counts instead of percentages | MEDIUM | Add migration to recompute stored scores; update display logic |
| Chart.js version conflict | LOW | Pin Chart.js version, use noConflict or module scope |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| NULL SubCompetency on existing data | DB Migration phase | Query for NULL SubCompetency rows; confirm Results page handles them |
| Import fingerprint not updated | Import Logic phase | Re-import same Excel with added sub-competency; verify field is populated |
| Import template backwards compatibility | Import Logic phase | Upload old-format Excel; verify no errors |
| Cross-package score normalization | Score Calculation phase | Compare radar charts for users in different packages with different question counts per sub-competency |
| Radar chart edge cases (<3 or >8 axes) | Results UI phase | Test with 1, 2, 3, 8, 12 sub-competencies |
| Case-insensitive sub-competency matching | Import Logic phase | Import "komunikasi" and "Komunikasi"; verify they merge |

## Sources

- Codebase analysis: `Models/AssessmentPackage.cs` (PackageQuestion has no SubCompetency field yet)
- Codebase analysis: `Models/PackageUserResponse.cs` (grading uses PackageOptionId, ID-based)
- Codebase analysis: existing Fisher-Yates shuffle in cross-package assignment (phase 45 docs)
- Chart.js radar chart documentation (training data, HIGH confidence — well-established library)

---
*Pitfalls research for: Sub-competency tagging + radar chart analysis on existing assessment portal*
*Researched: 2026-03-10*
