---
phase: 349
slug: manageassessment-monitoring-low-polish
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-05
---

# Phase 349 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Pure polish — majority visual (manual/Playwright), minority logic-bearing-ringan (xUnit).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (net8.0) — HcPortal.Tests |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj (existing, ProjectReference HcPortal.csproj) |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ManageAssessmentLowPolishTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **Build gate** | `dotnet build HcPortal.csproj -c Debug` (0 error WAJIB tiap task) |
| **Estimated runtime** | ~15-20 s (full, 98 baseline + baru) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error (+ quick test bila task logic-bearing)
- **After every plan wave:** Full suite green
- **Before `/gsd-verify-work`:** Full suite green + Playwright visual spot-check per surface
- **Max feedback latency:** ~20 s

---

## Per-Task Verification Map

> Planner mengisi detail per-task. Garis besar tier:

| Tier | MAP items | Test Type | Verification |
|------|-----------|-----------|--------------|
| Logic-bearing (testable) | MAP-13 (TotalCount exclude Cancelled), MAP-23 (search Category), MAP-21 (paging.Take — sebagian sudah ter-cover Phase 348 PaginationHelper) | unit | xUnit `ManageAssessmentLowPolishTests.cs` (Wave 0) + grep acceptance |
| Controller/ViewModel (semi) | MAP-10 (AbandonedCount/+pending assignment), MAP-15 (status dropdown during search), MAP-22 (drop dead param History) | build + grep | `dotnet build` 0 error + grep signature/assignment |
| View-only (visual) | MAP-01/02/03/04/05/06/07/08/09/16/17/18/19 | manual/Playwright | build 0 error (Razor compile) + visual spot-check |
| No-op (already done) | MAP-20 (Phase 346 REC-07) | verify | grep konfirmasi badge sudah render |

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ManageAssessmentLowPolishTests.cs` — stubs untuk MAP-13 (TotalCount exclude Cancelled logic) + MAP-23 (search Category match) bila diekstrak ke helper testable. Bila inline-only (tak feasibel extract), skip + andalkan grep + Playwright (dokumentasikan).
- [ ] MAP-21 paging.Take sudah ter-cover `ManageAssessmentMedFixTests` (Phase 348 PaginationHelper clamp) — reuse, no new test.

*Existing xUnit infra (12 file termasuk Phase 348 ManageAssessmentMedFixTests) covers logic-bearing. Mayoritas polish = visual manual-only.*

### Wave 0 gap — DITERIMA SADAR (deliberate trade-off)

Test logic-bearing (`ManageAssessmentLowPolishTests.cs` untuk MAP-13/MAP-23) dibuat di **Wave 5 (Plan 349-05 Task 2)**, SETELAH implementasi di **Wave 3 (Plan 349-03 Task 3)** — bukan TDD murni / bukan Wave 0 sejati. Gap ini **diterima** dengan alasan:
1. **Per-task `dotnet build` gate** hadir di setiap task (tidak ada 3 consecutive task tanpa verify — sampling continuity terjaga).
2. **Logika MAP-13/23 ringan** — LINQ predicate (`g.Count(a => a.Status != ...Cancelled)` + `Category.Contains`) + ViewBag, BUKAN service/domain logic baru. Risiko regresi rendah.
3. **Phase = pure polish** (no migration, no schema, no new behavior) — mayoritas item visual; subset testable kecil.
4. Test tetap dibuat dalam phase yang sama (Wave 5) + Playwright UAT 5 SC sebagai gate akhir → coverage tercapai sebelum phase close.

`nyquist_compliant: false` di frontmatter sengaja jujur menandai gap ini (bukan kelalaian).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| i18n Indonesia render | MAP-01/02 | Teks Razor — visual | Buka Monitoring Detail + History → header/kartu/label Indonesia |
| Chevron rotate + aria | MAP-03/04 | CSS + screen-reader | Toggle collapse → chevron rotate; inspect aria-label; Tab3 tanpa nested-interactive |
| Empty-state filter-aware + "X dari Y" + no-results | MAP-05/07/08 | Conditional render | Filter tanpa hasil → pesan benar + count row + aria-live |
| Skeleton match kolom | MAP-09 | Loading transient | Trigger HTMX load → skeleton kolom = konten |
| Summary cards Total=sum | MAP-10 | Visual + JS sync | Monitoring Detail → Total = jumlah semua kartu (Abandoned + Menunggu Penilaian) |
| Regenerate Token Pre-Post | MAP-17 | UI action + DB | Monitoring list grup Pre-Post token → Regenerate → DB token Pre+Post sama (koord MAM-01) |
| "real-time" subtitle hilang + kategori dobel | MAP-14/16 | Visual | Monitoring list → subtitle + kolom |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify (build gate) or Wave 0 dependencies or manual-justified
- [ ] Sampling continuity: build gate tiap task (no 3 consecutive tanpa verify)
- [ ] Wave 0 covers MAP-13/23 logic bila diekstrak
- [ ] No watch-mode flags
- [ ] Feedback latency < 20s
- [ ] `nyquist_compliant: true` set saat planner finalize

**Approval:** pending
