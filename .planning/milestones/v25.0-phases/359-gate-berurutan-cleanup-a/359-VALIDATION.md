---
phase: 359
slug: gate-berurutan-cleanup-a
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-12
---

# Phase 359 — Validation Strategy

> Per-phase validation contract (reconstruct State B dari artefak — VALIDATION.md retroaktif).
> Gate logic security-critical (PCOMP-06/07/08/09) ter-cover otomatis; UI level-hiding (PCOMP-10) manual-only by nature.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests, net8.0) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter FullyQualifiedName~ProtonYearGate` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~12 s (filter ProtonYearGate, incl. real-SQL integration) |

**Catatan:** `ProtonYearGateTests` = unit predicate murni; `ProtonYearGateIntegrationTests` = real-SQL `ProtonCompletionFixture`. Graduation gate (PCOMP-09) juga di-lock lintas-fase oleh `MarkMappingCompletedTests` (Phase 365).

---

## Sampling Rate

- **After task commit:** `dotnet test --filter FullyQualifiedName~ProtonYearGate`.
- **After wave / before sign-off:** `dotnet test` full suite hijau.
- **UI (PCOMP-10):** Playwright @5277 manual visual (render CDP/Histori tanpa level/grafik) — by design.

---

## Per-Requirement Verification Map

| REQ | Behavior | Test / Verifier | Type | Status |
|-----|----------|-----------------|------|--------|
| PCOMP-06 | CreateAssessment server-side gate (deliverable 100%) — worker tak bisa nyelip via POST manual | `ProtonYearGateIntegrationTests` (Exempt_BypassOrigin_GateSeratusPersen, NoBypass_NormalAssignment_Keblok) | integration (real-SQL) | ✅ green |
| PCOMP-07 | Cross-year gate predicate `ProtonYearGate.IsAllowed` (Tahun1 allowed; TahunN butuh N-1 lulus penanda) | `ProtonYearGateTests` (6: Year1/YearN/EmptyPassed/NullPassed/Whitespace) + `..IntegrationTests.IsPrevYearPassed_*` | unit + integration | ✅ green |
| PCOMP-08 | Bypass-exempt + reactivation lewat gate yang sama; Tahun3 0-deliverable fallback; renewal lewat gate | `ProtonYearGateIntegrationTests` (Exempt_BypassOrigin_LolosCrossYear, Reactivation_* ×3) | integration | ✅ green (core); loop-skip/TempData summary → manual UAT |
| PCOMP-09 | CoachMapping cross-year hard-block (assign) + graduation gate (MarkMappingCompleted Tahun3) | Graduation: `MarkMappingCompletedTests` (Phase 365, 7 [Fact]); cross-year predikat: `ProtonYearGate*` | integration | ✅ green (core); endpoint assign hard-block → manual UAT |
| PCOMP-10 | Matikan tampilan level kompetensi (CDP/Histori render badge saja, drop grafik tren) | Playwright @5277 visual | manual | ⬜ manual-only |

*Status: ⬜ pending/manual · ✅ green · ❌ red · ⚠️ flaky*

**Bukti run:** `dotnet test --filter FullyQualifiedName~ProtonYearGate` → **13/13 Passed (0 failed, 12 s)** (6 unit + 7 integration). Full suite saat ini 236/236.

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/ProtonYearGateTests.cs` — predicate unit (Plan 01, PCOMP-07).
- [x] `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` — real-SQL gate/exempt/reactivation (PCOMP-06/07/08).

*Infrastruktur xUnit + ProtonCompletionFixture sudah ada — tidak perlu install.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Tampilan level kompetensi hilang (badge saja, grafik tren drop) di CDP dashboard/Histori | PCOMP-10 | Render Razor/Chart.js visual — tak ada behavior unit-testable | Playwright @5277: buka CDP dashboard + HistoriProton → konfirmasi 0 angka level + 0 grafik tren + badge Lulus/Belum/Selesai tetap render tanpa error |
| Loop CreateAssessment skip worker tak-eligible + TempData ringkasan skip (eligible tetap dapat session, bukan all-or-nothing) | PCOMP-08 | Orkestrasi loop + TempData = jalur UI; predikat gate-nya sudah unit/integration-tested | UAT @5277: CreateAssessment batch campuran eligible/tidak → cek hanya eligible dapat session + TempData ringkas skip |
| Assign endpoint hard-block (drop ConfirmProgressionWarning escape) | PCOMP-09 | Endpoint-level; predikat penanda sudah integration-tested | UAT @5277: assign Tahun N saat N-1 belum lulus → hard-block tanpa opsi override |

---

## Validation Audit 2026-06-12 (State B reconstruct)

| Metric | Count |
|--------|-------|
| Requirements | 5 (PCOMP-06..10) |
| Automated-covered (gate logic) | 4 (PCOMP-06/07/08/09 core) |
| Manual-only (documented) | PCOMP-10 (UI) + endpoint-orchestration bits of 08/09 |
| MISSING (automatable but absent) | 0 |

Security-critical gate logic (cross-year + 100%-deliverable + bypass-exempt + graduation) ter-cover 13 ProtonYearGate tests + 365 graduation tests. PCOMP-10 UI level-hiding legitimately manual (Razor visual). Tidak ada gap automatable-but-missing → auditor TIDAK di-spawn.

---

## Validation Sign-Off

- [x] Semua REQ punya verifikasi otomatis ATAU manual-only terdokumentasi
- [x] Gate logic security-critical (PCOMP-06..09) automated + hijau (13/13)
- [x] Manual-only (PCOMP-10 + orchestration) didokumentasi dengan rationale + instruksi
- [x] No watch-mode flags
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-12
