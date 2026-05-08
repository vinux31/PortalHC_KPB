# Phase 314 — Audit Secondary (Path-Style Id Concat)

**Date:** 2026-05-08
**Scope:** Find lain endpoint dengan pattern frontend `fetch(appUrl('/Admin/{action}/' + id))` yang affected post-Phase 287/288 controller split.

## Method

```bash
grep -rnE "fetch\(appUrl\('/Admin/[A-Za-z]+/' \+ " --include="*.cshtml" Views/
```

## Result

| Callsite | Endpoint | Status |
|----------|----------|--------|
| Views/Admin/AssessmentMonitoring.cshtml:400 | `/Admin/RegenerateToken/{id}` | ✅ Fixed (commit `45ab5b47` — backend `[HttpPost("{id:int}")]`) |
| Views/Admin/AssessmentMonitoringDetail.cshtml:1012 | `/Admin/RegenerateToken/{id}` | ✅ Same fix |
| Views/Admin/ManageAssessment.cshtml:456 | `/Admin/RegenerateToken/{id}` | ✅ Same fix |

**Total path-style id concat patterns:** 3 callsites, 1 unique endpoint. **All fixed by single backend patch.**

## Other Patterns (NOT affected, audited)

- **Query string format** (`?id=`, `?sessionId=`, etc): server pakai default model binder — work
- **POST body** (no id concat): work
- **`Url.Action(..., "AssessmentAdmin", ...)`**: 14 references valid — `AssessmentAdminController` exists dengan correct route
- **`Url.Action(..., "Admin", ...)`**: 12 references — mostly `Index` breadcrumbs (valid), `CoachCoacheeMapping` (⚠ moved ke `CoachMappingController` Phase 288, may be broken via routing fallback — DEFER ke audit phase tersendiri, out-of-scope TKN-01)

## Verdict

**Audit secondary CLEAN untuk Phase 314 scope.** Single root cause (RegenerateToken routing) sudah resolved dengan 1 commit. No additional fixes required.

## Recommendation Future

Future audit phase (separate): grep `Url.Action(..., "Admin", ...)` references vs current AdminController action list — flag mismatches dari Phase 287/288/289 split fallout.
