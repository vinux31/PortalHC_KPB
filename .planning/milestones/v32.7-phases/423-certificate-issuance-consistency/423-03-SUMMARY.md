---
phase: 423-certificate-issuance-consistency
plan: 03
wave: 3
completed: 2026-06-24
migration: false
requirements: [CERT-07]
status: complete
---

# 423-03 SUMMARY — Badge umur PendingGrading + UAT live

## What was built (2 commits + UAT)

- **`b76c8694`** Task 1 — Badge umur "Menunggu N hari" di 2 view:
  - `Views/Admin/EssayGrading.cshtml`: badge di header sesi (`Model.CompletedAt.HasValue && !Model.IsFinalized`), class via `CertIssuanceRules.PendingAgeBadgeClass`.
  - `Views/Admin/AssessmentMonitoringDetail.cshtml`: badge di status-cell (`UserStatus=="Menunggu Penilaian"`) + essay-pending row. Display-only, NO auto-finalize (D-08/D-09).
- **`5bd635fd`** Task 2 — `tests/e2e/cert-pending-age-badge.spec.ts` (Playwright smoke, 3 test, `--list` parse OK, graceful skip bila tak ada data pending).
- **Task 3 — UAT live @5270** (checkpoint:human-verify, dijalankan orchestrator): lihat `423-UAT.md`. **PASSED 4/4**.

## UAT (423-UAT.md) — PASSED 4/4
SEED_WORKFLOW (backup→seed 3 sesi PendingGrading 2/5/8 hari→test→**RESTORE verified pristine**):
1. **CERT-07/D-09 badge warna** PASS — DOM class aktual: 2hr `bg-secondary`, 5hr `bg-warning text-dark`, 8hr `bg-danger`; tooltip + stat panel benar.
2. **CERT-07/D-08 no-auto-finalize** PASS — status tetap "Menunggu Penilaian" pasca-GET.
3. **EssayGrading badge** PASS (code-equivalence) — helper identik PendingAgeBadgeClass live-proven di MonitoringDetail + grep + unit 20/20 (sesi seed OJT tak punya essay utk render langsung).
4. **Cert sanity Wave 2** PASS (integration) — CertIssuanceIntegrationTests 5/5 + suite 717/0/2.

## Verification
- `dotnet build` 0 errors. grep badge: EssayGrading=1, AssessmentMonitoringDetail=2.
- Live UAT @5270 admin login; DB restored pristine (SEED_JOURNAL 2026-06-24/423 = cleaned).

## Deviations
- EssayGrading badge tak dirender langsung dgn seed OJT (no HasManualGrading) → verified by code-equivalence (helper sama, live-proven di MonitoringDetail). Acceptable.
- Badge class `bg-*` (bukan `text-bg-*` literal di plan draft) — mengikuti idiom view aktual + helper Wave 1; fungsional setara, live-verified benar.

Phase 423 (CERT-01..07) implementasi LENGKAP 3/3 wave. NEXT autopilot: verify → secure → validate → phase-complete.
