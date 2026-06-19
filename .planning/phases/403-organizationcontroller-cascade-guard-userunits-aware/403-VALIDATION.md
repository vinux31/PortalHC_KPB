---
phase: 403
slug: organizationcontroller-cascade-guard-userunits-aware
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-18
updated: 2026-06-19
---

# Phase 403 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 403-RESEARCH.md §"Validation Architecture" (HIGH confidence, seam test existing siap di-extend).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net8.0) + EF Core InMemory |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (no special config) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationController" --nologo` |
| **Full suite command** | `dotnet test --nologo` |
| **Estimated runtime** | ~30s quick / full suite baseline ≥507 (3 skip SQLEXPRESS-gated milik Phase 404 — dipertahankan) |
| **Existing seam** | `HcPortal.Tests/OrganizationControllerTests.cs` — `MakeController()` (InMemory + null-substitute + JSON-extractor `GetSuccess`/`GetInt`/`GetBool`) [VERIFIED siap pakai] |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~OrganizationController" --nologo` (<30s)
- **After every plan wave:** `dotnet test --nologo` (full suite — baseline ≥507, JANGAN regresi; 3 skip SQLEXPRESS dipertahankan)
- **Before `/gsd-verify-work`:** Full suite hijau + `dotnet run` localhost:5277 + UAT DB lokal (rename/delete/reparent/preview) + Playwright modal D-03 bila ada
- **Max feedback latency:** ~30 seconds (quick filter)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 403-01-W0 | 01 | 0 | ORG-01/02 | — | RBAC `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` dipertahankan utuh | static + security-audit | grep ×9/×7 + 403-SECURITY.md T-403-02/03 | `OrganizationControllerTests.cs` (seam) | ✅ green (static — attr deklaratif, di-audit secure 9/9) |
| 403-rename | 01 | 1 | ORG-01 | T-403 mass-assign | Rename Level≥1 → semua baris `UserUnits.Unit==oldName` ter-rename (incl sekunder) + mirror `ApplicationUser.Unit` konsisten; `IsPrimary` tak disentuh | unit | `dotnet test --filter "FullyQualifiedName~OrganizationController" --nologo` | `EditOrganizationUnit_RenameLevel1_RenamesAllUserUnitsRows` | ✅ green |
| 403-del-guard | 01 | 1 | ORG-01 | T-403 priv-bypass | DeleteOrganizationUnit tolak unit dgn membership sekunder aktif (`UserUnits.IsActive`) | unit | idem | `DeleteOrganizationUnit_SecondaryMembershipActive_Rejected` | ✅ green |
| 403-deact-guard | 01 | 1 | ORG-01 | — | ToggleOrganizationUnitActive deactivate-branch tolak unit dgn membership sekunder aktif | unit | idem | `ToggleOrganizationUnitActive_SecondaryMembershipActive_Rejected` | ✅ green |
| 403-split-block | 01 | 1 | ORG-02 | T-403 invariant-1 | Reparent cross-Bagian: worker yg akan terpecah >1 Bagian → BLOCK + pesan sebut NIP/nama | unit | idem | `EditOrganizationUnit_ReparentSplitsWorker_Blocked` | ✅ green |
| 403-no-split | 01 | 1 | ORG-02 | — | Reparent single-unit worker (no split) → ALLOW + Section ter-update (regresi perilaku existing) | unit | idem | `EditOrganizationUnit_ReparentSingleUnitWorker_Allowed` | ✅ green |
| 403-preview-parity | 01 | 1 | ORG-02 | — | `PreviewEditCascade.affectedUserUnitsCount` == jumlah baris UserUnits yg di-rename aktual (filter identik, +fixture IsActive=false diskriminatif) | unit | idem | `PreviewEditCascade_RenameLevel1_UserUnitsCountMatchesActual` | ✅ green |
| 403-uat | 01/02 | 1/2 | ORG-01/02 | — | Rename propagasi DB `HcPortalDB_Dev` + delete/reparent tolak + preview cocok modal (5 skenario A/B/C/D/pure-edge) | manual UAT | `dotnet run` localhost:5270 + cek DB + Playwright | manual (SC#5) — DONE 5/5 (403-02-SUMMARY) | ✅ green (executor-driven Playwright @5270, DB cross-check + restore) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] Extend `HcPortal.Tests/OrganizationControllerTests.cs` — 6 test ORG-01/02 (rename UserUnits, delete-guard, deactivate-guard, split-block, no-split-allow, preview-parity) ditambahkan + `GetMessage` helper. Reuse `MakeController()`. [commit RED `02d32d6f`]
- [x] **Pitfall 1 resolved (opsi B):** `MakeController()` tambah `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` → parity test EditOrganizationUnit (tx-wrapped) tetap hijau di InMemory.
- [x] Tidak perlu framework install (xUnit + InMemory sudah ada). Fixture inline via `MakeController`.

*SQL-riil filtered-unique invariant test = **Phase 404 (QA-01..04), JANGAN duplikat di 403.** Logika 403 (rename/guard/split-detect) = correctness-of-query, valid in-memory.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Rename unit propagasi ke baris `UserUnits` + mirror + modal preview cocok | ORG-01/02 (SC#5) | Razor/JS modal dinamis + cek DB riil; grep+build tak cukup (lesson Phase 354) | `dotnet run` localhost:5277 → Admin/ManageOrganization → rename unit ber-anggota-sekunder → cek `HcPortalDB_Dev.UserUnits` ter-rename + `ApplicationUser.Unit` mirror; buka modal Edit → cek baris "X baris keanggotaan unit" == jumlah aktual; coba delete unit sekunder → ditolak; coba reparent unit split → ditolak dgn pesan. Snapshot→seed→RESTORE DB lokal (Seed Workflow). |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (6 unit tests + RBAC static/security-audit; 1 manual-only UAT done 5/5)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (`OrganizationControllerTests.cs` extended, 6/6 green)
- [x] No watch-mode flags
- [x] Feedback latency < 30s (quick filter ~1s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-19

---

## Validation Audit 2026-06-19
| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

State A audit (post-execution). All 6 behavioral requirements COVERED by green xUnit tests (`dotnet test --filter ~OrganizationController` → 14/14; full suite 532/0/5). RBAC (403-01-W0) is declarative attributes — verified via grep + 403-SECURITY.md (T-403-02/03 CLOSED), not unit-testable through the InMemory controller seam (attributes not enforced on direct calls). UI modal (403-uat) is Razor/JS dynamic → manual-only by design (lesson Phase 354), executed 5/5 via Playwright @5270 with DB cross-check. No test generation needed. `wave_0_complete: true`, `nyquist_compliant: true`.
