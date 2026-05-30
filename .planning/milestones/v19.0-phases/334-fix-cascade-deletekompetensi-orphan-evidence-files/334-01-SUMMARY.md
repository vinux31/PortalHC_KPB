---
phase: 334-fix-cascade-deletekompetensi-orphan-evidence-files
plan: 01
status: SHIPPED LOCAL
shipped_at: "2026-05-28"
commits:
  - "ac66dc55 feat(334): cascade orphan evidence DeleteKompetensi (File.Delete post-commit + catch friendly no info leak)"
pushed: false
push_strategy: bundle v19.0 (Phase 325+...+334) — tunggu push lock release per Phase 327 option-b
---

# Phase 334 Plan 01 — SUMMARY

## Status: SHIPPED LOCAL

Phase 334 menutup 1 HIGH finding Phase 328 §4.8 (D2 orphan EvidencePath + D6 info leak ex.Message): collect evidencePaths INSIDE tx, File.Delete loop POST commit, refactor catch friendly no info leak. Existing 5-step cascade + audit log POST commit preserved verbatim.

## Files Modified

| File | Delta | Change |
|------|-------|--------|
| `Controllers/ProtonDataController.cs` | +40 LoC | DeleteKompetensi: evidencePaths outer var, populate INSIDE tx Step 2, File.Delete loop POST audit log, catch refactor DbUpdateException + Exception fallback NO ex.Message |
| `docs/IT_NOTIFY.md` | +20 LoC | Phase 334 entry + smoke scenario #12 (D6 verify critical) |

## D-09 Acceptance Criteria — 11/11 PASS

| AC | Criteria | Result |
|----|----------|--------|
| AC-1 | evidencePaths declared OUTER tx | ✅ PASS — `List<string>? evidencePaths = null;` |
| AC-2 | evidencePaths populate INSIDE tx SEBELUM RemoveRange | ✅ PASS — Step 2 between ToListAsync dan RemoveRange |
| AC-3 | 5-step cascade order preserved verbatim | ✅ PASS — CoachingSessions→Progresses→Deliverables→SubKompetensi→Kompetensi |
| AC-4 | CommitAsync preserved | ✅ PASS — L1576 |
| AC-5 | Audit log preserved verbatim POST commit | ✅ PASS — L1578-1580 |
| AC-6 | File.Delete loop POST audit log + inner try/catch warn-only | ✅ PASS — implemented post-audit, before return Json |
| AC-7 | Catch refactor: DbUpdateException + Exception fallback friendly. NO + ex.Message. NO explicit RollbackAsync | ✅ PASS — 2 catch blocks, generic messages, no info leak, no RollbackAsync di scope |
| AC-8 | dotnet build 0 error CS* | ✅ PASS — empty grep output |
| AC-9 | dotnet test --no-build 18/18 PASS | ✅ PASS — 18/18 87ms |
| AC-10 | Commit `feat(334): cascade orphan evidence DeleteKompetensi ...` | ✅ PASS — commit `ac66dc55` |
| AC-11 | SUMMARY.md digenerate | ✅ PASS — file ini |

## Grep Marker Verification

```
grep -c "List<string>? evidencePaths = null" Controllers/ProtonDataController.cs      → 1 (outer scope declaration)
grep -c "File.Delete post-commit failed (Kompetensi evidence)" Controllers/ProtonDataController.cs → 1 (warn log marker)
grep -c "catch (DbUpdateException dbEx)" Controllers/ProtonDataController.cs          → 1 (new specific catch)
grep -c "Gagal hapus kompetensi" Controllers/ProtonDataController.cs                  → 2 (2 catch blocks generic friendly)
grep -c "+ ex.Message" Controllers/ProtonDataController.cs                            → 1 (COMMENT marker only — actual code in DeleteKompetensi scope: 0)
grep -c "transaction.RollbackAsync" Controllers/ProtonDataController.cs               → 2 (PRE-EXISTING other endpoints L685+L1068, NOT Phase 334 scope)
```

**Scope verification:** `sed -n '1516,1640p' Controllers/ProtonDataController.cs | grep -c "transaction.RollbackAsync"` → 0 (DeleteKompetensi clean).

## Threat Model Disposition — 4/4

| Threat ID | Category | Disposition |
|-----------|----------|-------------|
| T-334-01 | D (DoS) — file orphan via implicit cascade | MITIGATED — explicit evidencePaths collection + File.Delete POST commit |
| T-334-02 | T (Atomicity) — file gone but DB rollback fails | MITIGATED — File.Delete only POST CommitAsync (DB committed first) |
| T-334-03 | R (Repudiation) — audit POST commit, audit fail = log gone but DB committed | ACCEPTED — out of scope D3 positioning per §4.8 |
| T-334-04 | I (Info Disclosure) — `ex.Message` leak constraint detail | MITIGATED — removed `+ ex.Message`, generic friendly Json messages |

## v19.0 Batch State

| Phase | Status | Push State |
|-------|--------|------------|
| 325 — Security Hardening P01+P02+P05 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 326 — Validator Hardening P03+P06 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 327 — Timezone DateOnly Refactor P04 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 328 — Cascade Audit Sweep (audit-only) | ✅ SHIPPED LOCAL | NOT PUSHED |
| 329 — Cascade Renewal Pre-Check Group | ✅ SHIPPED LOCAL | NOT PUSHED |
| 330 — Cascade MED Bundle | ✅ SHIPPED LOCAL | NOT PUSHED |
| 331 — DeleteTraining + DeleteManualAssessment atomicity | ✅ SHIPPED LOCAL | NOT PUSHED |
| 332 — DeleteBagian file atomicity | ✅ SHIPPED LOCAL | NOT PUSHED |
| 333 — DeleteCoachingSession file atomicity | ✅ SHIPPED LOCAL | NOT PUSHED |
| **334 — DeleteKompetensi orphan evidence + D6 info leak** | ✅ **SHIPPED LOCAL** | NOT PUSHED |

**~74 commit batch** di `main` lokal, NOT pushed. Push gate: user explicit approval per Phase 327 option-b hold.

## Next Steps

1. **Phase 335** — `fix-cascade-deleteworker-renewal-files-tx` (HIGH L, ~200-300 LoC, kompleks UserManager interaction + cross-user renewal pre-check + 9-step cascade tx wrap)
2. **Push batch v19.0** — saat IT available
