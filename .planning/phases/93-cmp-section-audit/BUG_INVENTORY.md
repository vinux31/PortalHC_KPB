# CMP Bug Inventory - Phase 93

**Created:** 2026-03-05
**Source:** Plan 93-01 Code Review
**Status:** Initial (to be populated during 93-01 execution)

## Bug Categories

### 1. Null Safety Issues (Critical/High)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| NS-01 | CMPController.cs:80 | `.First()` without `.Any()` check on availableBagians | Critical | 93-03 |
| NS-02 | CMPController.cs:85 | `.First()` without `.Any()` check on availableBagians | Critical | 93-03 |
| NS-03 | CMPController.cs:927 | `.First()` without `.Any()` check | Critical | 93-03 |

### 2. Date Localization Issues (High)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| LOC-01 | Records.cshtml:142 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-02 | Records.cshtml:188 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-03 | Records.cshtml:194 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-04 | Assessment.cshtml:142 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-05 | Assessment.cshtml:146 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-06 | Assessment.cshtml:209 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-07 | Assessment.cshtml:260 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-08 | Assessment.cshtml:308 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-09 | Kkj.cshtml:105 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-10 | Mapping.cshtml:111 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-11 | Certificate.cshtml:4 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |
| LOC-12 | Results.cshtml:78 | `.ToString("dd MMM yyyy")` without Indonesian culture | High | 93-02 |

### 3. Validation Issues (Medium)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| VAL-01 | TBD | POST action missing ModelState validation | Medium | 93-03 |
| VAL-02 | TBD | POST action missing ModelState validation | Medium | 93-03 |

### 4. Real-time Monitoring Issues (Medium)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| CACHE-01 | TBD | IMemoryCache miss not handled gracefully | Medium | 93-03 |

### 5. Known Gap Issues (Investigation)

| ID | Location | Issue | Severity | Fix Plan |
|----|----------|-------|----------|----------|
| GAP-01 | Results.cshtml | PositionTargetHelper missing for competency display (ASSESS-04) | TBD | 93-01 investigation |

## Severity Definitions

- **Critical:** Crashes, null exceptions, raw errors shown to user, broken core functionality
- **High:** Broken flows, incorrect data displayed, navigation failures, missing validation
- **Medium:** UX issues (unclear text, missing links, confusing UI)
- **Low:** Cosmetic issues, typos, minor inconsistencies

## Notes

- This inventory is created during Plan 93-01 execution
- Plans 93-02, 93-03, 93-04 reference this inventory for bug details
- Update status column as bugs are fixed
- Add new bugs found during browser testing (93-04)

---

*Last updated: 2026-03-05*
