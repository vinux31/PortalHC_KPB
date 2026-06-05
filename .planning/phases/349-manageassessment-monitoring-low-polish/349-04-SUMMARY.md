---
phase: 349-manageassessment-monitoring-low-polish
plan: 04
subsystem: Assessment Monitoring Detail (view + ViewModel + controller)
tags: [i18n, summary-cards, signalr-sync, conditional-render, razor]
requires: [349-03]
provides:
  - "Monitoring Detail chrome Bahasa Indonesia penuh"
  - "7-kartu summary dengan invariant Total = jumlah 6 kartu lain (MAP-10)"
  - "AbandonedCount field + assign; MenungguPenilaianCount assign di Detail"
  - "JS updateSummaryFromDOM sync Abandoned + Menunggu Penilaian (SignalR push benar)"
  - "Kartu Sedang Mengerjakan bind InProgressCount; dead var dibuang"
  - "Tombol Akhiri Semua Ujian conditional render"
affects:
  - Views/Admin/AssessmentMonitoringDetail.cshtml
  - Models/AssessmentMonitoringViewModel.cs
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: [bootstrap-row-cols-wrap, derive-user-status-count, js-textcontent-sync]
key-files:
  created: []
  modified:
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Models/AssessmentMonitoringViewModel.cs
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "MAP-10: 7 kartu (existing 5 + Abandoned text-dark + Menunggu Penilaian text-warning); row-cols-2/md-4/xl-7 untuk wrap rapi (utility Bootstrap, zero CSS custom)"
  - "MAP-10 JS: status-cell text dari @statusLabel = DeriveUserStatus raw ('Abandoned'/'Menunggu Penilaian'/'InProgress'/'Dibatalkan'/'Completed'/'Not started') -> branch JS match persis (verified statusLabel switch L243-248)"
  - "MAP-10 MenungguPenilaianCount pakai konstanta PendingGrading di controller + @AssessmentConstants.AssessmentStatus.PendingGrading di JS string (Razor render server-side ke 'Menunggu Penilaian')"
  - "MAP-12: modal wording 'belum mulai' (akhiriNotStartedCount via GetAkhiriSemuaCounts) predikat-identik aksi cancel (notStarted) — terverifikasi no divergensi, hanya tambah @if gate"
  - "Cleanup tambahan i18n: komentar L192 + JS sub-table L807 (<th>Nama/Hasil) + komentar L950 diterjemahkan (konsistensi Bahasa Indonesia + acceptance EN-residual=0)"
requirements-completed: [MAP-01, MAP-10, MAP-11, MAP-12]
duration: ~24 min
completed: 2026-06-05
---

# Phase 349 Plan 04: Monitoring Detail Polish Summary

Polish Monitoring Detail (per-grup, SignalR live push) — i18n chrome ID penuh (MAP-01), summary cards LENGKAP 7 kartu dengan invariant Total=sum (MAP-10, item paling kompleks), bind InProgressCount + drop dead var (MAP-11), conditional render "Akhiri Semua Ujian" (MAP-12). 3 file (view + ViewModel + controller Detail action), build 0 error.

**Tasks:** 3 | **Files:** 3 modified | **Duration:** ~24 min

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 2 (data) | `e8abdc90` | feat(349-04): MAP-10 data — AbandonedCount field + Detail action assign |
| 1+2+3 (view) | `72488347` | feat(349-04): Monitoring Detail i18n + 7-kartu summary + Akhiri conditional (MAP-01/10/11/12) |

## What Was Built

- **MAP-01 (i18n):** Header tabel `Name/Progress/Score/Result/Completed At/Actions` → `Nama/Progres/Nilai/Hasil/Selesai Pada/Aksi`; `Back to Monitoring`→`Kembali ke Monitoring`; `Per-User Status`→`Status Per-Peserta`; `Export Results`→`Ekspor Hasil`; `No sessions found`→`Belum ada sesi`. Plus cleanup komentar + JS sub-table `<th>Nama/Hasil</th>` (EN residual = 0).
- **MAP-10 (7-kartu, KRITIS):** ViewModel `AbandonedCount` baru; Detail action assign `AbandonedCount` (UserStatus=="Abandoned") + `MenungguPenilaianCount` (==PendingGrading konstanta). View 7 kartu (id `count-total/completed/inprogress/notstarted/cancelled/abandoned/pending`); `row-cols-2 row-cols-md-4 row-cols-xl-7` wrap. Invariant `Total = Selesai + Sedang Mengerjakan + Belum Mulai + Dibatalkan + Abandoned + Menunggu Penilaian`. JS `updateSummaryFromDOM` tambah cabang `Abandoned` + `@AssessmentConstants.AssessmentStatus.PendingGrading` + update `count-abandoned`/`count-pending` (Pitfall 5 — SignalR push tidak salah masuk notStarted).
- **MAP-11:** Kartu Sedang Mengerjakan bind `@Model.InProgressCount` (bukan inline LINQ `@(Model.Sessions.Count(...))`); dead var `completedPct`/`passRatePct` dibuang (D minimal-risk).
- **MAP-12:** Tombol "Akhiri Semua Ujian" dibungkus `@if (Model.InProgressCount > 0 || Model.GroupStatus == "Open")`; `btn-danger`; modal wording "belum mulai" predikat-identik aksi cancel (notStarted, terverifikasi no divergensi).

## Deviations from Plan

**[Rule 1 — Consistency] Cleanup i18n tambahan** — Found during: Task 1 acceptance | Acceptance "EN residual=0" awalnya gagal (3 sisa: komentar L192 'Export Results', JS sub-table L807 `<th>Name/Result</th>`, komentar L950 'Completed At') | Diterjemahkan semua untuk konsistensi Bahasa Indonesia + acceptance pass | Files: `AssessmentMonitoringDetail.cshtml` | Verification: EN-residual grep=0 | Commit `72488347`.

**[Process] Comment dead-var naming** — komentar MAP-11 awalnya menyebut `completedPct/passRatePct` → grep dead-var=1 (di komentar sendiri). Reworded jadi "dua variabel persentase" → grep=0.

**Total deviations:** 2 (consistency cleanups). **Impact:** none — semua acceptance PASS, build 0 error.

## Verification

- `dotnet build HcPortal.csproj -c Debug` → **0 Error**
- Grep T1: semua string ID present (Nama/Progres/Nilai/Hasil/Selesai Pada/Aksi/Kembali ke Monitoring/Status Per-Peserta/Ekspor Hasil/Belum ada sesi); EN residual=0
- Grep T2: AbandonedCount field=1, controller assign AbandonedCount=1 + MenungguPenilaianCount=1, 7 count-* id (×1 each), InProgress inline LINQ=0, dead var=0, JS `text==='Abandoned'` + count-abandoned present
- Grep T3: Akhiri gate `Model.InProgressCount > 0 || Model.GroupStatus == "Open"`=1
- Phase gate (Plan 05): Playwright browser-verify invariant Total = jumlah 6 kartu lain (card-sum), SignalR push update Abandoned/Pending benar, tombol Akhiri hilang saat no-InProgress & Closed

## Self-Check: PASSED

- key-files modified exist on disk ✓
- `git log --grep="349-04"` → 2 commits ✓
- All `<acceptance_criteria>` re-verified PASS ✓
- build 0 error ✓

Ready for Plan 349-05 (drop param mati History + xUnit test MAP-13/23 + phase gate human-verify).
