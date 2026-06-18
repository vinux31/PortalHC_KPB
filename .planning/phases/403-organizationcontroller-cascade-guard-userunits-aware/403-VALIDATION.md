---
phase: 403
slug: organizationcontroller-cascade-guard-userunits-aware
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
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
| 403-01-W0 | 01 | 0 | ORG-01/02 | — | RBAC `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` dipertahankan utuh | unit (scaffold RED) | `dotnet test --filter "FullyQualifiedName~OrganizationController" --nologo` | ❌ W0 (extend `OrganizationControllerTests.cs`) | ⬜ pending |
| 403-rename | 01 | 1 | ORG-01 | T-403 mass-assign | Rename Level≥1 → semua baris `UserUnits.Unit==oldName` ter-rename (incl sekunder) + mirror `ApplicationUser.Unit` konsisten; `IsPrimary` tak disentuh | unit | idem | ❌ W0 | ⬜ pending |
| 403-del-guard | 01 | 1 | ORG-01 | T-403 priv-bypass | DeleteOrganizationUnit (:447) tolak unit dgn membership sekunder aktif (`UserUnits.IsActive`) | unit | idem | ❌ W0 | ⬜ pending |
| 403-deact-guard | 01 | 1 | ORG-01 | — | ToggleOrganizationUnitActive (:391) deactivate-branch tolak unit dgn membership sekunder aktif | unit | idem | ❌ W0 | ⬜ pending |
| 403-split-block | 01 | 1 | ORG-02 | T-403 invariant-1 | Reparent cross-Bagian: worker yg akan terpecah >1 Bagian → BLOCK + pesan sebut NIP/nama | unit | idem | ❌ W0 | ⬜ pending |
| 403-no-split | 01 | 1 | ORG-02 | — | Reparent single-unit worker (no split) → ALLOW + Section ter-update (regresi perilaku existing L235-265) | unit | idem | ❌ W0 | ⬜ pending |
| 403-preview-parity | 01 | 1 | ORG-02 | — | `PreviewEditCascade.affectedUserUnitsCount` == jumlah baris UserUnits yg di-rename aktual (filter identik, +fixture IsActive=false diskriminatif) | unit | idem | ❌ W0 | ⬜ pending |
| 403-uat | 01 | 1 | ORG-01/02 | — | Rename propagasi DB `HcPortalDB_Dev` + delete/reparent tolak + preview cocok modal | manual UAT | `dotnet run` localhost:5277 + cek DB + Playwright | manual (SC#5) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Extend `HcPortal.Tests/OrganizationControllerTests.cs` — tambah ~6 test ORG-01/02 (rename UserUnits, delete-guard, deactivate-guard, split-block, no-split-allow, preview-parity). Reuse `MakeController()` + helper extractor existing.
- [ ] **Decision per Pitfall 1 (InMemory `TransactionIgnoredWarning`):** untuk test yang memanggil `EditOrganizationUnit` (transaction-wrapped pasca-D-04) → opsi B: tambah `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` di `MakeController()`. Untuk split-detection logic murni → boleh opsi A (ekstrak helper non-transaksi & uji helper). Rekomendasi: B untuk parity test, A untuk split-detection.
- [ ] Tidak perlu framework install (xUnit + InMemory sudah ada). Tidak perlu conftest-equivalent (fixture inline via `MakeController`).

*SQL-riil filtered-unique invariant test = **Phase 404 (QA-01..04), JANGAN duplikat di 403.** Logika 403 (rename/guard/split-detect) = correctness-of-query, valid in-memory.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Rename unit propagasi ke baris `UserUnits` + mirror + modal preview cocok | ORG-01/02 (SC#5) | Razor/JS modal dinamis + cek DB riil; grep+build tak cukup (lesson Phase 354) | `dotnet run` localhost:5277 → Admin/ManageOrganization → rename unit ber-anggota-sekunder → cek `HcPortalDB_Dev.UserUnits` ter-rename + `ApplicationUser.Unit` mirror; buka modal Edit → cek baris "X baris keanggotaan unit" == jumlah aktual; coba delete unit sekunder → ditolak; coba reparent unit split → ditolak dgn pesan. Snapshot→seed→RESTORE DB lokal (Seed Workflow). |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (extend `OrganizationControllerTests.cs`)
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
