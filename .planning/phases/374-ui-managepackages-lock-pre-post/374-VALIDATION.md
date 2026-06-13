---
phase: 374
slug: ui-managepackages-lock-pre-post
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-13
updated: 2026-06-13
---

# Phase 374 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (net8.0), EF Core 8.0.0 |
| **Config file** | none — konvensi `HcPortal.Tests/*.cs`, fixture `IClassFixture<ProtonCompletionFixture>` (disposable SQL Server DB per-fixture) |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Shuffle"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (baseline 329/329 per MEMORY Phase 373) |
| **Estimated runtime** | ~30s subset Shuffle; full suite menit-an (real-SQL fixtures) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + `dotnet test --filter "FullyQualifiedName~Shuffle"`
- **After every plan wave:** Run `dotnet test --filter "Category!=Integration"` (SQL-less cepat) + Shuffle subset
- **Before `/gsd-verify-work`:** Full suite hijau (`dotnet test`, baseline 329/329) + `dotnet run` cek `http://localhost:5277`
- **Max feedback latency:** ~30s (Shuffle subset per task)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (Wave 0) | — | 0 | SHUF-11 | T-374-lock | POST-replica tidak menulis saat locked; menulis ke semua sibling saat clean | unit + integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ShuffleLockGuard"` | ❌ W0 — `ShuffleLockGuardTests.cs` | ⬜ pending |
| (Wave 0) | — | 0 | SHUF-10 | T-374-csrf | Endpoint propagate flag ke SEMUA sibling grup | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ShuffleUpdateEndpoint"` | ❌ W0 — `ShuffleUpdateEndpointTests.cs` (atau extend `ShufflePropagationTests`) | ⬜ pending |
| (impl) | endpoint | 1+ | SHUF-10 | T-374-csrf | `[Authorize(Roles="Admin, HC")]`+`[ValidateAntiForgeryToken]`+audit+propagate | integration | `dotnet test --filter "FullyQualifiedName~Shuffle"` | ✅ (Wave 0 stubs) | ⬜ pending |
| (impl) | endpoint | 1+ | SHUF-11 | T-374-lock | Server reject POST saat lock-condition true → TempData error, no write | integration | `dotnet test --filter "FullyQualifiedName~ShuffleLockGuard"` | ✅ (Wave 0 stubs) | ⬜ pending |
| (impl) | view | 1+ | SHUF-12 | — | Warning §9 logic (≥2 paket-ber-soal AND Acak Soal OFF AND mismatch) | pure helper (opsional) ATAU manual/Playwright Phase 375 | `dotnet test --filter "FullyQualifiedName~Shuffle"` bila helper diekstrak | ⚠️ opsional | ⬜ pending |
| (impl) | view | 1+ | SHUF-13 | — | Reminder Pre/Post saved-state (Pre OFF & Post ON) render-conditional | manual / Razor-runtime → Playwright Phase 375 | — | n/a 374 | ⬜ pending |
| (impl) | view | 1+ | SHUF-14 | — | Hide toggle Proton Th3 / Manual render-conditional | manual / Razor-runtime → Playwright Phase 375 | — | n/a 374 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ShuffleLockGuardTests.cs` — covers SHUF-11: (a) pure decision `started || assignment` → locked; (b) real-SQL POST-replica reject saat locked (tak menulis), accept saat clean (menulis ke semua sibling). Pola `IClassFixture<ProtonCompletionFixture>` + `[Trait("Category","Integration")]`.
- [ ] `HcPortal.Tests/ShuffleUpdateEndpointTests.cs` (atau extend `ShufflePropagationTests`) — covers SHUF-10: replika `UpdateShuffleSettings` foreach atas grup REAL → assert SEMUA sibling ikut nilai POST (pola `ShufflePropagationTests.Propagation_Standard_AllSiblingsFollowModel`).
- [ ] (opsional) pure helper `IsShuffleLocked` + `ShouldHideShuffleToggle` + warning-predikat diekstrak → unit test cepat tanpa DB (SHUF-11/12/14 decision-logic). Mencegah Pitfall 2 (divergensi GET vs POST lock-condition).

*Framework xUnit sudah terinstall (329/329 baseline) — Wave 0 hanya menambah file test, bukan setup framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Render hide-toggle (Proton Th3 / Manual) | SHUF-14 | Razor `@if` render-conditional; flag-computation pure-testable tapi render butuh runtime | Phase 375 Playwright: buka ManagePackages untuk assessment Proton Tahun 3 → card Pengacakan TIDAK dirender |
| Render reminder Pre/Post (opsi Z) | SHUF-13 | Razor conditional saved-state lintas-halaman | Phase 375 Playwright: set Pre OFF + Post ON → buka Post → reminder `alert-warning` tampil; tidak ada auto-cascade |
| Warning §9 live JS recompute | SHUF-12 | JS DOM behavior saat flip toggle | Phase 375 Playwright: multi-paket + ukuran beda → flip Acak Soal OFF → warning muncul, ON → hilang |
| Toggle disabled saat lock | SHUF-11 (UI layer) | Visual disabled state | Phase 375 Playwright: assessment dgn peserta started → switch + tombol disabled + lock banner. (Server-guard SHUF-11 = automated Wave 0) |
| `dotnet run` smoke ManagePackages | semua | Razor compile + ViewBag wiring | `dotnet run` → `http://localhost:5277` → buka ManagePackages, card render, Simpan PRG sukses (CLAUDE.md verifikasi lokal wajib) |

*Render-conditional (hide/reminder/warning-visibility) sengaja di-defer ke Playwright Phase 375 (SHUF-16) — tidak dipaksakan jadi unit test rapuh. Logic layer (lock-guard, propagate) di-cover real-SQL di 374.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (render-conditional explicitly manual → Phase 375)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (SHUF-10 propagate + SHUF-11 lock-guard)
- [x] No watch-mode flags
- [x] Feedback latency < 30s (Shuffle subset)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-13

---

## Validation Audit 2026-06-13

| Metric | Count |
|--------|-------|
| Requirements | 5 (SHUF-10..14) |
| Gaps found | 0 actionable |
| Resolved (automated) | 4 decision-logic (SHUF-10/11/12/14) |
| Manual-only (justified) | 3 render-conditional (SHUF-13 reminder + SHUF-12/14 visibility) |
| Escalated | 0 |

**Coverage map (tests delivered, all green — full suite 347/347):**
- SHUF-10 → `ShuffleUpdateEndpointTests.UpdateShuffleSettings_PropagatesToAllSiblings` (real-SQL) — COVERED
- SHUF-11 → `ShuffleLockGuardTests` ×3 (real-SQL guard reject/allow) + `ShuffleToggleRulesTests.Lock_OrLogic` ×4 — COVERED
- SHUF-12 → `ShuffleToggleRulesTests.Warning_Predicate` ×5 (logic) — COVERED; live render visibility → manual (Playwright 375)
- SHUF-14 → `ShuffleToggleRulesTests.Hide_ProtonTahun3OrManual` ×5 (logic) — COVERED; @if render → manual (Playwright 375)
- SHUF-13 → reminder Pre/Post saved-state render-conditional — MANUAL-ONLY (Razor-runtime, Playwright 375); browser UAT scenario 5 PASS

**Rationale manual-only:** Render-conditional (Razor `@if` + live-JS DOM) tidak layak unit-test (brittle — RESEARCH Pitfall, anti-pattern). Di-defer ke Phase 375 (SHUF-16 Playwright UAT) by design. Semua sudah browser-verified 7/7 UAT 2026-06-13. Decision-logic 100% automated (18 test). Nyquist-compliant: render-conditional punya justifikasi manual-only eksplisit, tidak ada gap auto-fillable yang dilewati diam-diam.
