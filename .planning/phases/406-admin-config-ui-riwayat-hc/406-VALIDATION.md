---
phase: 406
slug: admin-config-ui-riwayat-hc
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 406 — Validation Strategy

> Per-phase validation contract. UI phase — Playwright @5270 primary; xUnit for any extracted pure helper.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) `HcPortal.Tests/` + Playwright `tests/e2e/` (baseURL localhost:5270, branch ITHandoff) |
| **Quick (unit)** | `dotnet test --filter "FullyQualifiedName~RiwayatUnifier"` (if RiwayatUnifier helper extracted) |
| **E2E (Playwright)** | `npx playwright test tests/e2e/retake-admin-406.spec.ts --workers=1` (AD off; admin@pertamina.com) |
| **Full suite** | `ASPNETCORE_ENVIRONMENT=Development dotnet test` |
| **Build** | `dotnet build` (0 error) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error (Razor compiles).
- **After view tasks:** Playwright spec for that surface (Razor dynamic WAJIB runtime-verify — Lesson Phase 354; grep+build tak cukup).
- **Before `/gsd-verify-work`:** build 0 error + full suite green + Playwright 406 spec green @5270.
- **Max feedback latency:** unit <15s; e2e per-run.

---

## Per-Task Verification Map

> Diisi/diperhalus oleh planner + `/gsd-validate-phase`. Seed per-RTK:

| RTK | Requirement | Test Type | Automated Command | Status |
|-----|-------------|-----------|-------------------|--------|
| RTK-05 | Card retake ManagePackages (toggle + progressive disclosure + helper) + binding Create/Edit; hide Pre-Test/Manual; POST UpdateRetakeSettings persist+sibling | e2e (Playwright @5270) | `npx playwright test retake-admin-406.spec.ts` | ⬜ pending |
| RTK-05 | Warning non-blocking saat MaxAttempts < attempt-terpakai (tidak blokir simpan) | e2e | (same spec) | ⬜ pending |
| RTK-08 | Modal riwayat HC: trigger dropdown → modal accordion attempt (archived+current, badge Lulus/Gagal, tanggal, mark current) | e2e | (same spec) | ⬜ pending |
| RTK-08 | Per-soal penuh (teks soal + jawaban + ✓/✗ + skor) di modal; XSS-encode; essay-pending = "—/Menunggu" | e2e + unit | `dotnet test --filter ~RiwayatUnifier` + spec | ⬜ pending |
| RTK-08 | `RiwayatPercobaan(sessionId)` endpoint RBAC [Authorize(Admin,HC)] + current-attempt via RetakeArchiveBuilder.Build(0,...) shape-identik | unit (pure unifier) + e2e | `dotnet test --filter ~RiwayatUnifier` | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/RiwayatUnifierTests.cs` — pure unifier (archived + current via Build(0,...) → unified DTO), mirror `RetakeArchiveBuilderTests` (IF planner extracts a pure unifier helper)
- [ ] `tests/e2e/retake-admin-406.spec.ts` — card toggle/save/propagation + hide Pre-Test + warning + modal open + per-soal render (seed temp via SEED_WORKFLOW: assessment + failed attempt + archived attempt)

*Framework xUnit + Playwright sudah ada — tidak perlu install.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Sibling propagation visual (config tersimpan ke semua sesi batch) | RTK-05 | DB state pasca-POST | Setelah simpan card, cek `sqlcmd` AllowRetake/MaxAttempts/RetakeCooldownHours sama di semua sesi (Title,Category,Schedule.Date) — atau assert via Playwright reload |

*Sisanya otomatis (e2e + unit).*

---

## Validation Sign-Off

- [ ] All tasks have automated verify or justified manual-only
- [ ] Razor surfaces verified at runtime (Playwright, not grep+build)
- [ ] Wave 0 covers MISSING references
- [ ] No watch-mode flags
- [ ] `nyquist_compliant: true` set after execution

**Approval:** pending
