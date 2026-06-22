---
phase: 407
slug: worker-self-service-gating-tier-feedback-riwayat-pekerja
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-22
---

# Phase 407 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. UI + endpoint phase. Tier helper = unit (pure); endpoint = controller/integration; leak-safety DOM + lifecycle = Playwright (smoke @407, penuh @408).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (+ EF Core InMemory 8.0.0; real-SQL fixture untuk Integration @localhost\SQLEXPRESS) + Playwright `tests/e2e/` (baseURL localhost:5270, branch ITHandoff) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0, IsTestProject) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (unit-only, SQL-less) |
| **Full suite command** | `dotnet test HcPortal.Tests` (incl Integration real-SQL) |
| **Estimated runtime** | ~unit <20s; full per-run; e2e per-run |

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error + `dotnet test HcPortal.Tests --filter "Category!=Integration"` (tier helper + regresi RetakeRules/RiwayatUnifier hijau).
- **After every plan wave:** `dotnet test HcPortal.Tests` (full incl Integration — pastikan counting/snapshot 405 tidak regresi).
- **Before `/gsd-verify-work`:** full suite hijau + build 0 error + **real-browser smoke leak-safety @5270** (lesson 354/413 — build hijau ≠ leak-safe). Lifecycle Playwright penuh + security audit = Phase 408.
- **Max feedback latency:** unit <20s.

---

## Per-Task Verification Map

> Diperhalus oleh planner. Seed per-RTK dari RESEARCH Test Map:

| Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| RTK-11 (tier `ResolveReviewMode` semua cabang truth table incl pending null) | T-407-leak | Pending/failed+sisa → NO key | unit | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ⚠️ extend RetakeRulesTests.cs | ⬜ pending |
| RTK-13 (CanRetake guards — regresi tak rusak) | — | Exclude PreTest/Manual/Pending/Cancelled | unit | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ✅ RetakeRulesTests.cs | ⬜ pending |
| RTK-09 (RetakeExam ownership reject + re-check + token clear) | T-407-idor / T-407-csrf | Non-owner→Forbid; CanRetakeAsync re-check; TempData.Remove token | controller/integration | `dotnet test --filter "FullyQualifiedName~RetakeExam"` | ❌ Wave 0 (new) | ⬜ pending |
| RTK-09/07 (retake-then-pass → 1 cert; counting no-conflate) | — | guard anti-double-cert | integration | `dotnet test --filter "Category=Integration"` | ⚠️ RetakeServiceTests.cs (counting ada; lifecycle worker = 408) | ⬜ pending |
| RTK-12 (RiwayatUnifier.Build unify+order+IsCurrent regresi) | — | — | unit | `dotnet test --filter "FullyQualifiedName~RiwayatUnifierTests"` | ✅ RiwayatUnifierTests.cs | ⬜ pending |
| RTK-11 leak-safety (DOM ShowWrongFlagsOnly tak ada "(Jawaban Benar)"/list-group-item-success/CorrectAnswer) | T-407-leak | Suppress kunci di :366/:388/:403 | e2e smoke (penuh @408) | Playwright @5270 | ⚠️ smoke 407 / penuh 408 | ⬜ pending |
| RTK-10 (countdown tick + enable@0 + modal POST) | T-407-bypass | Server re-check authoritative | e2e | Playwright @5270 (Phase 408) | ❌ Phase 408 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/RetakeRulesTests.cs` — **TAMBAH** tes `ResolveReviewMode` (semua cabang truth table: full/wrong-flags/score-only × passed/failed/exhausted + **pending null = NO key**) — covers RTK-11.
- [ ] Controller/integration test untuk `RetakeExam`: non-owner→Forbid · not-eligible→redirect (CanRetakeAsync re-check) · sukses→TempData token cleared + redirect StartExam — covers RTK-09 (pola: NoOpHubContext + NullLogger mirror RetakeServiceTests.cs, atau controller unit dengan mocked RetakeService).
- [ ] (Opsional kuat) Playwright smoke leak-safety @5270: assert DOM `ShowWrongFlagsOnly` TIDAK mengandung kunci — covers RTK-11 leak risk lebih awal dari 408.
- [ ] Framework install: TIDAK perlu (xUnit + fixture + Playwright sudah ada).

*Existing infra menutup RTK-13/RTK-12 regresi; gap utama = tier helper unit + RetakeExam endpoint test.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cooldown countdown ticking + auto-enable visual | RTK-10 | Timing/JS runtime visual | Playwright @5270 (smoke 407 / lifecycle 408) — assert tombol disabled saat cooldown, enable saat lewat |
| Full lifecycle gagal→skor+tanda-salah→Ujian Ulang→cooldown→ulang→lulus→cert | RTK-14 | Cross-surface end-to-end | **Phase 408** lifecycle Playwright (di luar scope 407) |

*Sisanya otomatis (unit + controller/integration + smoke e2e).*

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (tier helper test + RetakeExam endpoint test)
- [ ] Razor/JS surfaces verified at runtime (Playwright smoke leak-safety, not grep+build — lesson 354/413)
- [ ] No watch-mode flags
- [ ] `nyquist_compliant: true` set after execution

**Approval:** pending
