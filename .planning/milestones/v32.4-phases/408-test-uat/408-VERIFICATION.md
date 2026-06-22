---
phase: 408-test-uat
verified: 2026-06-22T07:10:00Z
status: passed
score: 4/4
overrides_applied: 0
verified_by: milestone-autopilot (orchestrator, capstone test phase — evidence = green suites + UAT)
---

# Phase 408: Test & UAT — Verification (RTK-14)

**Phase Goal:** Buktikan seluruh kemampuan ujian ulang benar end-to-end — unit + integration (retake-then-pass 1 cert, counting no-conflate) + Playwright lifecycle penuh + security. 0 migration.

## Success Criteria (ROADMAP 1-4)

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | xUnit RetakeRules/ArchiveBuilder/Service semua cabang | VERIFIED | RetakeRulesTests 22, ArchiveBuilder 4, RiwayatUnifier 6, RetakeService 8 — green (regresi, full suite 614/0/2) |
| 2 | Integration: retake-then-pass → 1 cert + counting (UserId,Title,Category) no-conflate | VERIFIED | **GAP-1 `RetakeThenPassCertTests`** green real-SQL (Assert.Equal(1, certCount) + format `^KPB/\d{3}/[IVX]+/\d{4}$`); counting via existing `Counting_PrePostSameTitle_NoConflate` (referenced, D-02) |
| 3 | Playwright lifecycle penuh @5270 + security | VERIFIED | **GAP-3 `retake-lifecycle-408.spec.ts`** green: gagal→leak-safe→Ujian Ulang→StartExam→jawab benar→LULUS 100%→cert# terbit; security `gsd-secure-phase 408` SECURED 13/13 threats_open:0 |
| 4 | build 0 error + dotnet test hijau (retake + no regresi) + UAT sign-off | VERIFIED | build 0 error; full xUnit **614/0/2**; e2e **19/19** (lifecycle + 407×6 + 406×11); 408-UAT.md PASS |

**Score:** 4/4 — RTK-14 fully covered.

## Capstone value
Lifecycle e2e awalnya RED → menyingkap **fixture-bug** (seed `QuestionType='SingleAnswer'` invalid → grade-on-submit skip → 0%). Produk BENAR (data nyata = `MultipleChoice`; 408-01 xUnit + lifecycle pasca-fix = LULUS+cert). Fixed seed + assertion. Bukti nilai capstone real-browser (lesson 354/413): bug fixture/integrasi tak terlihat di test per-surface 405-407.

## Catatan (Info, non-blocking → backlog)
- IN-408-A: grade switch (GradingService/SubmitExam) silent-zero untuk QuestionType tak dikenal — pertimbangkan default-throw/log (defensive; data valid tak terpicu).
- IN-408-B: seed 406/407 juga pakai label 'SingleAnswer' (display-only, aman) — selaraskan saat sentuh berikutnya.

**Status:** PASSED. 0 migration.
