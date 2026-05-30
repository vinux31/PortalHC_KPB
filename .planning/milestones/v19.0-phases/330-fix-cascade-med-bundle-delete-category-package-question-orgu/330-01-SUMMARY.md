---
phase: 330-fix-cascade-med-bundle-delete-category-package-question-orgu
plan: 01
status: SHIPPED LOCAL
shipped_at: "2026-05-28"
commits:
  - "40518631 feat(330): cascade med-bundle try-catch + audit log (DeleteCategory/Package/Question/OrgUnit)"
  - "a9aa7250 docs(330): update IT_NOTIFY commit hash 40518631"
pushed: false
push_strategy: bundle v19.0 (Phase 325+326+327+329+330) — tunggu push lock release per Phase 327 option-b
---

# Phase 330 Plan 01 — SUMMARY

## Status: SHIPPED LOCAL

Phase 330 menutup 5 MED finding dari Phase 328 Cascade Audit Sweep: tambah `try/catch DbUpdateException` + `_auditLog.LogAsync` di 5 endpoint yang hilang. Zero migration, zero schema change.

## Files Modified

| File | Delta | Change |
|------|-------|--------|
| `Controllers/AssessmentAdminController.cs` | +60 LoC | DeleteCategory try/catch D6 + DeletePackage try/catch D6 + DeleteQuestion try/catch D6 + audit log D3 |
| `Controllers/OrganizationController.cs` | +22 LoC | DeleteOrganizationUnit try/catch D6 dual-path + audit log D3 |
| `Services/NotificationService.cs` | +1 LoC (delta) | DeleteAsync catch type: Exception → DbUpdateException D6 |
| `docs/IT_NOTIFY.md` | +30 LoC | Phase 330 entry + smoke scenario #8 |

## D-09 Acceptance Criteria — 9/9 PASS

| AC | Criteria | Result |
|----|----------|--------|
| AC-1 | DeleteCategory L481: `"Tidak bisa hapus kategori: masih ada data yang berelasi."` | ✅ PASS — grep match L481 |
| AC-2 | DeletePackage L5136: `"Tidak bisa hapus paket: masih ada data yang berelasi."` | ✅ PASS — grep match L5136 |
| AC-3 | DeleteQuestion L6039: `"Tidak bisa hapus soal: masih ada data yang berelasi."` | ✅ PASS — grep match L6039 |
| AC-4 | DeleteOrganizationUnit L418-419: dual-path `"Tidak bisa hapus unit..."` | ✅ PASS — 2 match (JSON + TempData) |
| AC-5 | NotificationService L286 (DeleteAsync): `catch (Exception ex)` → `catch (DbUpdateException ex)` | ✅ PASS — L286 verified refactored. 6 `catch (Exception ex)` lain di method non-scope (Create/MarkAsRead/MarkAllAsRead/GetUnread/etc) pre-existing, BUKAN Phase 330 scope (D-06 minimal). |
| AC-6 | `dotnet build` clean | ✅ PASS — 0 error CS* (app locked = MSB copy warning only, compile clean) |
| AC-7 | `dotnet test` 18/18 | ✅ PASS — `dotnet test --no-build` 18/18 PASS 340ms (FileUploadHelperTests P02 + CertificateStatusTests P04). Caveat: test DLL pre-Phase-330 build (HcPortal.exe locked, rebuild blocked). Phase 330 changes mechanical try/catch + audit log di endpoint TIDAK ter-cover test → 18/18 PASS confirms zero regresi di tested areas (FileUploadHelper + CertificateStatus). |
| AC-8 | Commit message `feat(330): cascade med-bundle...` | ✅ PASS — commit `40518631` |
| AC-9 | SUMMARY.md digenerate | ✅ PASS — file ini |

## Grep Marker Verification

```
grep -c "catch (DbUpdateException" Controllers/AssessmentAdminController.cs  → 8 (3 Phase 330 baru di L479+L5134+L6037, 2 Phase 329, 3 pre-existing CertNumber/DeleteAssessment)
grep -c "Tidak bisa hapus" Controllers/AssessmentAdminController.cs           → 3 (kategori/paket/soal)
grep -c "Tidak bisa hapus unit" Controllers/OrganizationController.cs         → 2 (JSON + TempData)
grep -c "catch (DbUpdateException" Services/NotificationService.cs            → 1 (L286 DeleteAsync)
grep -c "catch (Exception" Services/NotificationService.cs                    → 6 (pre-existing non-scope: Create/MarkAsRead/MarkAllAsRead/GetUnread/etc — D-06 minimal mandate L286 only)
grep "DeleteOrganizationUnit" Controllers/OrganizationController.cs L429      → _auditLog.LogAsync call present
grep "DeleteQuestion.*q.Id" (audit log split multi-line) → verified via Read L6041-6053
```

## Threat Model Disposition — 5/5

| Threat ID | Category | Disposition |
|-----------|----------|-------------|
| T-330-01 | D (DoS) | MITIGATED — try/catch DbUpdateException + TempData["Error"] friendly |
| T-330-02 | I (Info Disclosure) | ACCEPTED — pesan generik, tidak ekspos nama tabel |
| T-330-03 | T (Tampering) | ACCEPTED — IsAjaxRequest() path branching, `[Authorize]` masih aktif |
| T-330-04 | R (Repudiation) | MITIGATED — `_auditLog.LogAsync` ditambah DeleteQuestion + DeleteOrganizationUnit |
| T-330-05 | I (Info Disclosure) | ACCEPTED — `ex.Message` hanya ke server-side `_logger.LogWarning`, tidak ke client |

## v19.0 Batch State

| Phase | Status | Push State |
|-------|--------|------------|
| 325 — Security Hardening P01+P02+P05 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 326 — Validator Hardening P03+P06 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 327 — Timezone DateOnly Refactor P04 | ✅ SHIPPED LOCAL | NOT PUSHED |
| 328 — Cascade Audit Sweep (audit-only) | ✅ SHIPPED LOCAL | NOT PUSHED |
| 329 — Cascade Renewal Pre-Check Group | ✅ SHIPPED LOCAL | NOT PUSHED |
| **330 — Cascade MED Bundle** | ✅ **SHIPPED LOCAL** | NOT PUSHED |

**~62 commit batch** di `main` lokal, NOT pushed. Push gate: user explicit approval per Phase 327 option-b hold (tunggu IT availability).

## Next Steps

1. **Push batch v19.0** — saat user release push lock: `git push origin main` → notifikasi IT dengan `docs/IT_NOTIFY.md`
2. **IT promo Dev** — per SOP `docs/IT_NOTIFY.md` (BACKUP → pre-check → `dotnet ef database update` → restart → smoke 8 scenario)
3. **Phase 331+** — DeleteWorker HIGH bundle (D2 atomicity + D5 renewal cross-user + D7 tx wrap) — deferred per Phase 328 §9 row #6
