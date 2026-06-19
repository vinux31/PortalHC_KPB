---
phase: 398-test-uat-seakan-online
plan: 01
subsystem: testing
tags: [playwright, e2e, inject-assessment, downstream-parity, cmp-records, cmp-results, certificate-pdf, exceljs]

requires:
  - phase: 393-backend-core-inject
    provides: InjectBatchAsync commit grading byte-identik online
  - phase: 395-mode-jawaban
    provides: step-5 input-asli + auto-generate + PreviewInjectScore + #btnInject commit
  - phase: 396-import-excel
    provides: switchToExcel + DownloadInjectTemplate/UploadInjectExcel + injExcelAnswersCache
  - phase: 397-link-pre-post-ke-room-existing
    provides: Cari Room picker + LinkedGroupId cross-grouping inject↔online
provides:
  - "Spec e2e konsolidasi tests/e2e/inject-seakan-online-398.spec.ts (5 skenario downstream parity)"
  - "Bukti repeatable INJ-13: hasil inject tampil seakan-online di Records/Results/Certificate"
  - "Side-by-side parity (Records-row) inject vs online IsManualEntry=0 — tak bisa dibedakan"
  - "Entri SEED_JOURNAL.md Phase 398 (temporary + local-only, status active)"
affects: [398-02-regresi, 398-03-audit-milestone, v32.2-milestone-audit]

tech-stack:
  added: []
  patterns:
    - "Downstream-parity e2e: lanjut SETELAH commit (navigasi 4 surface), bukan berhenti di db.queryScalar"
    - "Helper assert bersama (assertRecordsRowSeakanOnline/assertResultsPerSoal/assertCertPdf) dipakai lintas skenario"
    - "Seed online IsManualEntry=0 via execScript di beforeAll (post-backup) untuk parity, ter-restore di afterAll"

key-files:
  created:
    - "tests/e2e/inject-seakan-online-398.spec.ts"
  modified:
    - "docs/SEED_JOURNAL.md"
    - ".planning/phases/398-test-uat-seakan-online/398-01-PLAN.md (key-link reconcile RecordsWorkerDetail)"

key-decisions:
  - "Surface Records admin→worker = /CMP/RecordsWorkerDetail?workerId= (bukan /CMP/Records yang cuma tampil akun login)"
  - "Parity 'Assessment Online' dibuktikan via data-type='assessment online' (RecordType.ToLower server-side) + badge 'Assessment' + 'Lihat Hasil', BUKAN literal teks row (badge row = 'Assessment')"
  - "Judul test tanpa kata 'Inject'/'Manual' agar assertion parity row-level tak salah-positif (keep prefix 'ZZ %398%' utk cleanup 398-02)"

patterns-established:
  - "Indistinguishable-check: assert ROW-level not.toContainText('Manual'/'Inject') (page-level tak bisa — ada stat 'Training Manual')"

requirements-completed: [INJ-13]

duration: 28min
completed: 2026-06-19
---

# Phase 398 Plan 01: Test "Seakan Online" — Downstream Parity Summary

**Spec Playwright konsolidasi (5 skenario) yang membuktikan hasil inject (393-397) tampil identik dengan assessment online di /CMP/RecordsWorkerDetail + /CMP/Results/{id} + /CMP/CertificatePdf/{id}, dan tak bisa dibedakan dari sesi online asli (side-by-side parity).**

## Performance

- **Duration:** ~28 min
- **Completed:** 2026-06-19T00:38Z
- **Tasks:** 3
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- Spec `inject-seakan-online-398.spec.ts` — 5 skenario (Form+essay+ElemenTeknis / Auto-generate / Excel / Pre-Post linked silang / side-by-side parity) **6 passed (incl global setup), 1.1m**.
- Menutup GAP yang 395/396/397 tak uji: navigasi pasca-commit ke 4 surface downstream (D-02 a/b/c/d).
- Buktikan D-02c "Analisis Elemen Teknis" render (author soal MC ber-`#elemenTeknis`).
- Buktikan essay (Form + Excel) berakhir `Status='Completed'` + `.bi-hourglass-split` count 0 (§13 / Pitfall 3).
- D-03 side-by-side: inject (IsManualEntry=1) + online sibling (IsManualEntry=0) keduanya render "Assessment Online" tanpa penanda pembeda.
- D-04 Pre/Post linked silang: inject Pre & online Post berbagi `LinkedGroupId`, keduanya tampil di Records.
- SEED_JOURNAL entri Phase 398 (status active → 398-02 flip cleaned).
- DB snapshot/restore lifecycle (CLAUDE.md Seed Workflow); afterAll RESTORE OK tiap run.

## Task Commits

1. **Task 1+2+3** (satu file, dibangun & diuji bertahap, 1 commit) - `30ef45bb` (test)

**Plan metadata:** (this SUMMARY) — sequential mode, commit terpisah.

## Files Created/Modified
- `tests/e2e/inject-seakan-online-398.spec.ts` - Spec konsolidasi 5 skenario downstream parity (~510 baris)
- `docs/SEED_JOURNAL.md` - Entri Phase 398 (temporary + local-only, active)
- `.planning/phases/398-test-uat-seakan-online/398-01-PLAN.md` - Selaras key-link → RecordsWorkerDetail

## Decisions Made
- **Route Records:** `/CMP/RecordsWorkerDetail?workerId=` untuk admin melihat record pekerja (controller izinkan roleLevel≤3). `/CMP/Records` hanya tampil record akun login (admin sendiri) → tak memuat baris inject pekerja.
- **Proof "Assessment Online":** via atribut `data-type="assessment online"` (= `RecordType.ToLower()` server-side) + badge "Assessment" + link "Lihat Hasil" (jalur Results). Badge row literal = "Assessment"; teks "Assessment Online" hanya di stat-card heading.
- **Penamaan data uji:** drop kata "Inject"/"Manual" dari judul (pakai 'ZZ Form/AutoGen/Excel/Pre/Side/Online... 398') supaya assertion parity row-level (`not.toContainText('Inject'/'Manual')`) tak salah-positif oleh nama data sendiri, sambil tetap match `LIKE 'ZZ %398%'` untuk cleanup 398-02.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Plan assumption salah] Records surface route**
- **Found during:** Task 1 (Form scenario)
- **Issue:** Plan tulis assertion `page.goto('/CMP/Records')` + key-link pattern `page\.goto\('/CMP/Records'\)`. Tapi `/CMP/Records` controller (CMPController.cs:485) hanya merender record milik akun login — admin login → record admin, BUKAN baris inject pekerja → assertion mustahil hijau.
- **Fix:** Pakai `/CMP/RecordsWorkerDetail?workerId=${uid}` (CMPController.cs:570; admin roleLevel≤3 diizinkan lihat pekerja lain). View `RecordsWorkerDetail.cshtml:224` set `data-type="@item.RecordType.ToLower()"` = "assessment online" → bukti label server-side. Selaraskan frontmatter key-link + Task1 acceptance ke `/CMP/RecordsWorkerDetail`.
- **Files modified:** tests/e2e/inject-seakan-online-398.spec.ts, .planning/phases/398-test-uat-seakan-online/398-01-PLAN.md
- **Verification:** 6 passed; grep `/CMP/RecordsWorkerDetail` = 2.
- **Committed in:** `30ef45bb`

**2. [Rule 2 - Plan assumption salah] Assertion "Assessment Online" + parity teks**
- **Found during:** Task 1 + Task 3
- **Issue:** Plan minta `row.toContainText('Assessment Online')` dan parity `table.not.toContainText('Manual'/'Inject')`. Realita: badge row = "Assessment" (Records.cshtml:210 / RecordsWorkerDetail.cshtml:229); teks "Assessment Online" cuma di stat heading. Page selalu memuat "Training Manual" (stat) → `not.toContainText('Manual')` skala-page mustahil. Judul plan ('ZZ Inject 398…') memuat "Inject" → `not.toContainText('Inject')` gagal oleh nama sendiri.
- **Fix:** Proof label via `data-type` atribut + page-level `getByText('Assessment Online')` (stat heading, valid). Parity di-scope ROW-level (`row.not.toContainText('Manual'/'Inject')`). Judul dibuat tanpa "Inject"/"Manual".
- **Files modified:** tests/e2e/inject-seakan-online-398.spec.ts
- **Verification:** Skenario 5 (side-by-side) + 4 (linked) hijau; parity row-level terbukti.
- **Committed in:** `30ef45bb`

---

**Total deviations:** 2 auto-fixed (2 Rule-2 — koreksi asumsi plan vs DOM/route aktual)
**Impact on plan:** Semua koreksi perlu agar assertion sahih terhadap perilaku nyata (route authz + render DOM). Tujuan must_haves (4 surface + side-by-side parity + essay Completed + ElemenTeknis card) tetap tercapai & terbukti. Tak ada scope creep.

## Issues Encountered
None — kelima skenario hijau pada run penuh (`--workers=1`, 1.1m). DB restore OK tiap run (lifecycle spec + global teardown).

## User Setup Required
None — no external service configuration required. (Pre-req runtime: server localhost:5277 dari MAIN tree, `Authentication__UseActiveDirectory=false`, SQLEXPRESS/SQLBrowser hidup.)

## Next Phase Readiness
- **Plan 398-02** siap: spec ini = subjek rerun + regresi (dotnet test + e2e online) + gate 0-migration + verifikasi restore COUNT `ZZ %398%`=0 (flip SEED_JOURNAL → cleaned) + finalize 398-VALIDATION.md.
- Tidak ada blocker. 0 migration sepanjang Plan ini (hanya file test + docs).

---
*Phase: 398-test-uat-seakan-online*
*Completed: 2026-06-19*
