---
phase: 406
slug: admin-config-ui-riwayat-hc
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
validated: 2026-06-22
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
| RTK-05 | Card retake ManagePackages (toggle + progressive disclosure + helper) + binding Create/Edit; hide Pre-Test/Manual; POST UpdateRetakeSettings persist+sibling | e2e (Playwright @5270) | `npx playwright test retake-config-406.spec.ts` | ✅ green (retake-config 6/6 @5270, exec-verified) |
| RTK-05 | Warning non-blocking saat MaxAttempts < attempt-terpakai (tidak blokir simpan) | e2e | (retake-config spec sc-5) | ✅ green |
| RTK-08 | Modal riwayat HC: trigger dropdown → modal accordion attempt (archived+current, badge Lulus/Gagal, tanggal, mark current) | e2e | `npx playwright test riwayat-hc-406.spec.ts` | ✅ green (riwayat-hc 5/5 @5270) |
| RTK-08 | Per-soal penuh (teks soal + jawaban + ✓/✗ + skor) di modal; XSS-encode; essay-pending = "—/Menunggu" | e2e + unit | `dotnet test --filter ~RiwayatUnifier` + riwayat-hc spec | ✅ green (unit 6/6 + e2e xss inert) |
| RTK-08 | `RiwayatPercobaan(sessionId)` endpoint RBAC [Authorize(Admin,HC)] + current-attempt via RetakeArchiveBuilder.Build(0,...) shape-identik | unit (pure unifier) + e2e | `dotnet test --filter ~RiwayatUnifier` | ✅ green (RiwayatUnifierTests 6/6, 235ms) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/RiwayatUnifierTests.cs` — pure unifier (archived + current via Build(0,...) → unified DTO), mirror `RetakeArchiveBuilderTests` — **EXTRACTED + 6/6 green** (`Helpers/RiwayatUnifier.cs`)
- [x] `tests/e2e/retake-config-406.spec.ts` (6 sc) + `tests/e2e/riwayat-hc-406.spec.ts` (5 sc) — card toggle/save/propagation + hide Pre-Test + warning + modal open + per-soal render + XSS inert — **11/11 green @5270 (exec-verified)**

*Framework xUnit + Playwright sudah ada — tidak perlu install.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Sibling propagation visual (config tersimpan ke semua sesi batch) | RTK-05 | DB state pasca-POST | Setelah simpan card, cek `sqlcmd` AllowRetake/MaxAttempts/RetakeCooldownHours sama di semua sesi (Title,Category,Schedule.Date) — atau assert via Playwright reload |

*Sisanya otomatis (e2e + unit).*

---

## Validation Sign-Off

- [x] All tasks have automated verify or justified manual-only
- [x] Razor surfaces verified at runtime (Playwright, not grep+build)
- [x] Wave 0 covers MISSING references
- [x] No watch-mode flags
- [x] `nyquist_compliant: true` set after execution

**Approval:** APPROVED 2026-06-22

---

## Validation Audit 2026-06-22

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

State A audit. Semua requirement (RTK-05, RTK-08) sudah COVERED oleh test yang ada — tak perlu auto-gen:
- `RiwayatUnifierTests.cs` 6/6 green (re-run 235ms, build 0 error).
- `retake-config-406.spec.ts` 6 sc + `riwayat-hc-406.spec.ts` 5 sc = 11/11 green @5270 (exec-verified, VERIFICATION 10/10).
- Full xUnit suite 604/0/2 (exec-verified).

NYQUIST-COMPLIANT. Tidak ada test baru di-generate (no gap).
