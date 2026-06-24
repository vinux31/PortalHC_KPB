---
phase: 425-cosmetic-naming-tech-debt-cleanup
plan: 04
subsystem: api
tags: [csharp, aspnetcore-mvc, system-text-json, controller-guard, tech-debt]

# Dependency graph
requires:
  - phase: 423-certificate-issuance-consistency
    provides: "Pola pure static EF-free helper (CertIssuanceRules) sbg analog penempatan ControllerGuards"
provides:
  - "Helpers/ControllerGuards.JsonFail(this ControllerBase, string) — guard-helper static minimal untuk respons JSON gagal {success=false, message}"
  - "Konvensi DRY pembentukan respons gagal yang shape-nya byte-identik (camelCase) — diterapkan selektif ke cluster SubmitEssayScore"
  - "Test parity HcPortal.Tests/ControllerGuardsTests.cs (shape JSON byte-identik dgn pola inline existing)"
affects: [milestone-audit-v32.7, future-controller-guard-cleanup]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Guard-helper static EF-free (extension on ControllerBase) membungkus Json(new {success=false, message}) — shape byte-identik, dipanggil DI DALAM action tanpa ubah signature/binding"
    - "Parity test pure (serialisasi JsonResult.Value via camelCase) membuktikan refactor presentasi tidak mengubah shape respons yang dibaca frontend JS"

key-files:
  created:
    - Helpers/ControllerGuards.cs
    - HcPortal.Tests/ControllerGuardsTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "CLN-05 diterapkan SELEKTIF ke satu cluster representatif (SubmitEssayScore, 6 guard) sbg demonstrasi konvensi — BUKAN sweep semua 23 call-site (D-04 minimal)"
  - "Helper = extension on ControllerBase (bukan Controller) agar selaras tipe penerima ControllerBase.Json; output via new JsonResult identik pipeline MVC"
  - "TANPA [ApiController]/DTO ber-anotasi (melanggar D-04); TANPA ubah signature/atribut keamanan action"

patterns-established:
  - "Guard-helper static minimal: bungkus pembentukan respons gagal, shape JSON byte-identik, panggil di dalam action"
  - "Refactor presentasi respons WAJIB dikawal parity test shape byte-identik sebelum disentuh (cegah regresi frontend JS)"

requirements-completed: [CLN-05]

# Metrics
duration: ~18min
completed: 2026-06-24
---

# Phase 425 Plan 04: ControllerGuards.JsonFail (CLN-05 / VAL-07) Summary

**Guard-helper static `ControllerGuards.JsonFail` membungkus pola `Json(new { success=false, message })` dengan shape JSON byte-identik (camelCase), diterapkan selektif ke cluster `SubmitEssayScore` (6 guard) sbg demonstrasi konvensi — bukan sweep, tanpa ubah signature/keamanan.**

## Performance

- **Duration:** ~18 min
- **Started:** 2026-06-24 (sesi eksekusi 425-04)
- **Completed:** 2026-06-24
- **Tasks:** 2
- **Files modified:** 3 (2 created, 1 modified)

## Accomplishments
- `Helpers/ControllerGuards.cs` baru — extension `JsonFail(this ControllerBase, string)` → `new JsonResult(new { success = false, message })`, shape byte-identik `{"success":false,"message":"..."}` camelCase (Program.cs tanpa `AddJsonOptions` kustom → default System.Text.Json).
- `HcPortal.Tests/ControllerGuardsTests.cs` baru — parity test (1 `[Fact]` byte-eksak + 6 `[Theory]` untuk semua pesan cluster, termasuk pesan dinamis interpolasi skor) membuktikan helper == pola inline existing.
- Cluster `SubmitEssayScore` di `AssessmentAdminController.cs` — 6 guard `return Json(new { success=false, message })` dikonversi ke `return this.JsonFail(message)` dengan pesan teks IDENTIK. Call-site lain (17 `success = false` tersisa) TIDAK disentuh.

## Task Commits

Each task was committed atomically:

1. **Task 1 (Wave 0): NEW ControllerGuards.cs + ControllerGuardsTests.cs (shape parity)** - `358fcd26` (feat)
2. **Task 2: Terapkan JsonFail ke cluster SubmitEssayScore** - `06836db1` (feat)

**Plan metadata:** (commit final docs — SUMMARY + STATE + ROADMAP)

_Catatan: Wave-0 ditulis sbg satu commit feat karena test bersifat parity-assertion murni (membandingkan helper vs pola inline existing), bukan RED-fail-by-absence-of-behavior._

## Files Created/Modified
- `Helpers/ControllerGuards.cs` — guard-helper static `JsonFail` (extension on ControllerBase) untuk respons JSON gagal seragam.
- `HcPortal.Tests/ControllerGuardsTests.cs` — parity test shape JSON byte-identik (7 kasus).
- `Controllers/AssessmentAdminController.cs` — 6 guard SubmitEssayScore dikonversi ke `this.JsonFail(...)` (diff 6 insert / 6 delete).

## Decisions Made
- **Subset minimal (D-04):** Hanya cluster `SubmitEssayScore` (6 guard) dikonversi. 17 call-site `success = false` lain di file ini sengaja DIBIARKAN (sweep = di luar "minimal" + risiko regresi frontend). Konversi turun tepat 23 → 17 (berkurang 6).
- **Extension on `ControllerBase`** (bukan `Controller`): selaras tipe penerima `ControllerBase.Json`, dapat dipanggil dari action manapun; output `new JsonResult(...)` lewat pipeline MVC global yang sama → shape identik.
- **Tanpa `[ApiController]`/DTO ber-anotasi** dan tanpa menyentuh signature/atribut — sesuai D-04.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. `using HcPortal.Helpers;` sudah ada di controller (line 13) sehingga tak perlu ditambah. `SubmitEssayScore` masih di line 3684 (tidak drift dari ekspektasi plan); re-grep simbol mengonfirmasi 6 guard di line 3691/3693/3698/3700/3709/3715.

## Security Verification
- Atribut keamanan `SubmitEssayScore` UTUH: `[HttpPost]` / `[Authorize(Roles = "Admin, HC")]` / `[ValidateAntiForgeryToken]` (line 3681-3683) + signature `SubmitEssayScore(int sessionId, int questionId, int score)` tak berubah → menutup T-425-11 (CSRF) & T-425-12 (authz).
- Shape JSON `{success, message}` byte-identik dibuktikan parity test → menutup T-425-13 (integrity / regresi shape respons). Frontend JS (`data.success`/`data.message`) tak melihat perubahan.
- Pesan teks IDENTIK (termasuk interpolasi `Skor harus antara 0 dan {ScoreValue}`) → tak ada info baru (T-425-14 accept).
- `ValidateAntiForgeryToken` di file tetap 35 occurrence; jalur `success = true` (8 site) tak tersentuh.

## Verification Results
- `dotnet build HcPortal.csproj` — **0 error** (24 warning pre-existing, bukan dari perubahan plan ini).
- `dotnet test --filter ~ControllerGuards` — **7/7 passed** (1 Fact + 6 Theory).
- Full suite `dotnet test HcPortal.Tests` — **768 passed / 0 failed / 2 skipped** (baseline 761 + 7 ControllerGuards baru; 0 regresi, ≥761/0/2 terjaga).
- `migration=FALSE` (tidak ada perubahan skema/model/binding).

## Known Stubs
None.

## Next Phase Readiness
- CLN-05 selesai → Phase 425 (P6 CLN) tuntas: 425-01 (CLN-01/03) + 425-02 (CLN-04) + 425-03 (CLN-02) + 425-04 (CLN-05). Plan terakhir milestone v32.7.
- Helper `ControllerGuards.JsonFail` tersedia bila cleanup call-site lain (sweep penuh) ingin dijadwalkan di milestone mendatang (saat ini sengaja TIDAK dilakukan — D-04 minimal).
- migration=FALSE → tak ada notifikasi migration baru untuk plan ini.

## Self-Check: PASSED
- Files: Helpers/ControllerGuards.cs, HcPortal.Tests/ControllerGuardsTests.cs, Controllers/AssessmentAdminController.cs, 425-04-SUMMARY.md — semua FOUND.
- Commits: 358fcd26 (Task 1), 06836db1 (Task 2) — semua FOUND.

---
*Phase: 425-cosmetic-naming-tech-debt-cleanup*
*Completed: 2026-06-24*
