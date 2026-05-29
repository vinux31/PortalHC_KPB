---
date: "2026-05-29 21:34"
promoted: true
---

add Gap UX hasil investigasi assessment "Post Test OJT Cilacap" 29 Mei 2026:

1. Filter default `Open+Upcoming` di /Admin/AssessmentMonitoring dan tab "Assessment Groups" /Admin/ManageAssessment sembunyi Closed assessment. User jadi kira data hilang. Fix: kasih option default "Semua Status" atau tambah badge counter Closed di tab list.

2. Tab "Assessment Groups" search "Cilacap" return 0 grup padahal session ada (ID 9-21, status Closed). Bug filter atau aggregation Title+Category+Schedule.Date tidak meliputi Closed group. Investigate query di AssessmentAdminController ManageAssessment action.

3. Tab History row tidak clickable. User lihat 27 attempt di Riwayat Assessment tapi gak bisa drill-down. Fix: tiap row tambah link ke /CMP/Results/{sessionId} via kolom Actions atau row clickable.

4. Banner alert di /CMP/Assessment "Looking for completed assessments? View your Training Records" link ke /CMP/Records (personal worker). Admin nyari assessment user lain salah jalur. Fix: kalau role Admin/HC, link redirect ke /Admin/ManageAssessment?tab=history.

5. ExportAssessmentResults Excel cuma summary (Nama, NIP, QuestionCount, Status, Score, Pass/Fail, CompletedAt). Tidak include breakdown Elemen Teknis + jawaban per soal padahal data ada. Fix: tambah sheet kedua "Detail Per Soal" + sheet ketiga "Elemen Teknis".

6. Tidak ada bulk PDF export per peserta (1 ZIP berisi PDF detail tiap worker). Saat ini admin harus open Results page satu-satu + browser Ctrl+P. Fix: tambah endpoint /Admin/BulkExportPdf?title=&category=&scheduleDate= generate ZIP via QuestPDF.

Konteks: investigasi 14 attempt Post Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap (20 May 2026, 13 peserta, AssessmentSessionId 9-21, all Pass score 75-87, created oleh D110-240001 Nur Dzakiyyatul Baahirah HC user). Download 3 Excel + 13 PNG ke downloads/Post Test OJT Cilacap/.

Cross-link: lihat note `2026-05-29-pretest-ojt-gast-cilacap-lost.md` — incident PreTest Cilacap 30 Mar 2026 hilang dari Dev DB. Backup user Excel ada tapi spider Elemen Teknis tidak. **Bukti konkret dampak Gap #5**: kalau ExportAssessmentResults dulu sudah bawa breakdown Elemen Teknis di Excel, restore PreTest jadi mungkin (tidak terbatas score summary). Prioritaskan Gap #5 untuk cegah recurring loss.
