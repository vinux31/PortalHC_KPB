---
title: "Gap UX assessment monitoring — 6 fix bundle (Cilacap incident discovery)"
status: pending
priority: P2
source: "promoted from /gsd-note (2026-05-29-gap-ux-assessment-monitoring-cilacap.md)"
created: 2026-05-29
theme: bug-fix-ux
---

## Goal

Tutup 6 gap UX di flow Admin Assessment Monitoring + History + Export yang ditemukan saat investigasi assessment "Post Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap" (29 Mei 2026). User awal sangka data hilang karena multiple discovery friction.

## Context

Source note: `.planning/notes/2026-05-29-gap-ux-assessment-monitoring-cilacap.md`

Discovery dilakukan via Playwright MCP login admin@pertamina.com ke Dev server 10.55.3.3. Konteks: PostTest Cilacap 20 May 2026, 13 peserta, AssessmentSessionId 9-21, all Pass 75-87, created oleh HC user `D110-240001 Nur Dzakiyyatul Baahirah`. Investigation download: 3 Excel + 13 PNG ke `downloads/Post Test OJT Cilacap/`.

Cross-link incident: lihat note `2026-05-29-pretest-ojt-gast-cilacap-lost.md` — PreTest counterpart hilang dari Dev DB. Backup user Excel ada tapi spider Elemen Teknis tidak (akibat langsung Gap #5). Prioritaskan Gap #5.

## Acceptance Criteria

- [ ] Gap #1: filter default di `/Admin/AssessmentMonitoring` + tab "Assessment Groups" `/Admin/ManageAssessment` ubah ke "Semua Status" ATAU tambah badge counter Closed di tab list — Closed assessment terlihat tanpa user ubah filter manual
- [ ] Gap #2: tab "Assessment Groups" search "Cilacap" Semua Status return parent group (sekarang 0). Investigate query di `AssessmentAdminController` ManageAssessment action — aggregation Title+Category+Schedule.Date harus include Closed group
- [ ] Gap #3: tab History row clickable atau tambah kolom Actions link ke `/CMP/Results/{sessionId}` per attempt — drill-down dari summary attempt ke detail spider+jawaban
- [ ] Gap #4: banner alert di `/CMP/Assessment` "Looking for completed assessments? View your Training Records" — kalau user role Admin/HC, link redirect ke `/Admin/ManageAssessment?tab=history` (bukan `/CMP/Records` personal)
- [ ] Gap #5 (HIGH PRIORITY): `ExportAssessmentResults` Excel tambah sheet kedua "Detail Per Soal" (jawaban tiap peserta + opsi + correctness) + sheet ketiga "Elemen Teknis" (breakdown radar score). Justifikasi: data sudah ada di DB (`PackageUserResponses` + `ElemenTeknisScores`), tinggal serialize. Loss recovery future restore-able.
- [ ] Gap #6: tambah endpoint `/Admin/BulkExportPdf?title=&category=&scheduleDate=` generate ZIP via QuestPDF — 1 PDF per peserta (spider chart + jawaban + skor). Eliminasi manual Ctrl+P per peserta

## Notes Teknis

- Controller: `Controllers/AssessmentAdminController.cs` (ManageAssessment action, ExportAssessmentResults L4077)
- View Results: `Views/CMP/Results.cshtml` (line 277 radar chart canvas, @media print CSS L416)
- ViewModel: `Models/AssessmentResultsViewModel.cs` (QuestionReviews + ElemenTeknisScores + CompetencyGains)
- Export helper: `Utilities/ExcelExportHelper.cs` + QuestPDF lib (sudah ada di project — `dotnet list package` confirm)
- DB tables impacted: `AssessmentSessions`, `AssessmentAttemptHistory`, `PackageUserResponses`, `SessionElemenTeknisScores`

## Risk

- Gap #5 + Gap #6 = perubahan output Excel + new endpoint — breaking change kalau ada integrasi downstream (cek dengan IT apakah Excel format dikonsumsi tool lain)
- Gap #1 filter default change = perubahan default UI yang sudah dikenal user lama — pertimbangkan opt-in via setting per-user instead of hard switch
