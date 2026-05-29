---
date: "2026-05-29 21:42"
promoted: true
---

PreTest OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap — DB TERHAPUS saat update code

Konteks:
- Pair counterpart dari PostTest Cilacap (20 May 2026, 13 peserta, sessionId 9-21) — sudah didokumentasikan di nota 2026-05-29-gap-ux-assessment-monitoring-cilacap.md
- PreTest dijalankan SEBELUM update code → DB terhapus / migration loss saat deploy versi baru ke Dev server 10.55.3.3
- Investigasi 29 Mei: 0 record di /Admin/ManageAssessment tab History + 0 entry di /Admin/AuditLog (page 1) untuk title mengandung "Pre"
- Confirmed gone di Dev DB (Title "Pre Test OJT Pekerja GAST..." → 0 row)

Data masih ada di backup user:
- File: PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx
- Tanggal pelaksanaan: 30 Maret 2026 (dari nama file + R4 Excel Jadwal=2026-03-30 08:30)
- Judul aktual di Excel: **`OJT GAST - GTO & SRU RU IV`** (TIDAK mengandung kata "Pre Test" maupun "Cilacap" — itu sebab semua search di Dev 0 hasil)
- Durasi 80 menit, batas kelulusan 70%
- 13 peserta = identik dengan PostTest Cilacap 20 May
- Format: hasil export Excel summary lama (Nama, NIP, Score, Pass/Fail, dst)
- **KEKURANGAN**: spider web Elemen Teknis BUKAN bagian Excel (Phase Gap UX #5 — ExportAssessmentResults Excel summary only, no per-soal breakdown, no Elemen Teknis radar data)
- Konsekuensi: gain score Pre vs Post per Elemen Teknis TIDAK BISA direkonstruksi dari Excel — hanya gain score TOTAL per peserta

Numerical Findings (Pre 30 Mar vs Post Final 20 May, hitung manual dari Excel + Dev DB sessionId 9-21):

| Metrik | Pre | Post Final | Gain |
|---|---|---|---|
| Avg Score | 53.92 | 79.38 | +25.46 |
| Pass count | 1/13 (Mohammad Zafrullah only) | 13/13 (100%) | +12 |

Top gain: Juniawan Okpianus +37 (47→84), Muhammad Muhar Al Iqram +37 (45→82), Maulana Zikra +33 (42→75).
Bottom gain: Mohammad Zafrullah +7 (80→87, sudah Pass Pre), Gibran Rayhan Tiftazani +9 (66→75 attempt #3).

Insight: OJT efektif — 12/13 peserta naik dari Fail ke Pass. Mohammad Zafrullah sudah baseline kompeten Pre Pass, tetap improve marginal.

CSV file saved: downloads/Post Test OJT Cilacap/04-Pre-vs-Post-Comparison.csv (UTF-8 BOM, Excel-ready, 8 kolom).

Root Cause Naming-Divergence Loss (kemungkinan):

1. **Judul beda format** (Pre: `OJT GAST - GTO & SRU RU IV` vs Post: `Post Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap`) — tidak ada auto-pair via `LinkedGroupId` field. Endpoint `ExportGainScoreExcel` butuh `LinkedGroupId` set → tidak akan reconstruct gain dari Dev DB walaupun PreTest restored.
2. **Schema change AssessmentType / LinkedGroupId** kemungkinan diperkenalkan SETELAH 30 Mar 2026. PreTest dibuat di skema lama, migration drop / reset waktu deploy field baru. Confirm via git log Models/AssessmentSession.cs + Migrations/* antara 2026-03-30 dan 2026-05-19.
3. PostTest 19 May dibuat oleh HC user (D110-240001 Nur Dzakiyyatul Baahirah) di skema baru — sehingga Post tetap di Dev DB sampai sekarang. Pre yg lebih lama → kehapus.

Action items future:
1. Tanggung jawab loss: identifikasi mana migration / DB reset event yg hapus PreTest. Check git log AssessmentSession schema changes antara 30 Mar dan 19 May 2026.
2. Restore strategi pilihan:
   a. Re-import via Excel manual → assessment baru "PreTest...Cilacap (Restored from Excel 20260330)" — score summary only, no elemen teknis. Pakai endpoint AddManualAssessment.
   b. Skip restore, treat hilang. Bandingan Pre/Post pakai score total Excel (manual hitung).
3. Prevent recurrence: SOP migration backup mandatory. Tambah pre-deploy hook backup AssessmentSessions + AssessmentAttemptHistory ke `.bak` sebelum migrate.
4. Linked gap UX #5 (Excel summary only) jadi prioritas — kalau Excel bawa Elemen Teknis breakdown, loss restore-able.

Linked entitas:
- Excel file user lokal: PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx
- PostTest counterpart sessionId Dev: 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 (Cilacap 20 May)
- Creator PostTest: D110-240001 Nur Dzakiyyatul Baahirah (HC user)
- AssessmentType field: kemungkinan Cilacap = Standalone (bukan PostTest dalam pair) karena LinkedGroupId NULL — confirm via DB query atau ExportGainScoreExcel response (return kosong = no pair).
