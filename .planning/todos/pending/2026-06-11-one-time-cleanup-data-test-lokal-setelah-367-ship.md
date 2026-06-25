---
created: 2026-06-11T12:48:09.290Z
title: One-time cleanup data test/audit lokal setelah Phase 367 ship
area: database
files:
  - Controllers/AssessmentAdminController.cs (DeleteAssessment :2184, DeleteAssessmentGroup :2372, DeletePrePostGroup :2558 — akan dirombak 367)
  - docs/superpowers/specs/2026-06-10-delete-input-records-full-cascade-design.md (spec C — cascade engine)
---

## Problem

DB lokal (HcPortalDB_Dev) penuh sesi assessment bekas test/audit fase-fase lama yang kini TAMPIL di default view setelah Phase 370 hapus window 7-hari (URG-02): "Legacy Exam 1775xxx" (×6), "Timer Expired 1775xxx" (×4), "Phase 314 Repro Token Bug", "UAT v14 Standard/PrePost", "ojt v1.9/v1.10/v14.2", "UAT ET Distribution Test", "OJT Token Test Q1-2026", "Multi Worker 1775xxx", dll — ±50 sesi + 20 Closed historis. User minta dibersihkan biar rekap HC bersih (konteks 2026-06-11, UAT Phase 370).

JANGAN hapus sebelum Phase 367 ship:
1. Endpoint delete saat ini cascade BOCOR (28 temuan spec C) — ninggalin orphan: EditLogs, PackageUserResponses, AttemptHistory, UserPackageAssignments, notifikasi, penanda Proton Origin='Exam', PendingProtonBypass, pasangan LinkedSessionId, file sertifikat.
2. Data legacy masih dipakai fixture UAT (370 baru pakai; sesi 364 e2e jalan paralel).
3. DB lokal shared dengan worktree main.

## Solution

Setelah Phase 367 (Delete Records Cascade Overhaul) SHIPPED:
1. Snapshot DB dulu (`sqlcmd ... BACKUP DATABASE` per SEED_WORKFLOW) + catat SEED_JOURNAL.
2. Hapus sesi test/audit via fitur delete cascade baru (preview konfirmasi) — sekalian jadi UAT real-data Phase 367.
3. Verifikasi zero orphan (assert per tabel — integration test 367 SC1 sudah sediakan pola).
4. Koordinasi: Phase 368 punya one-time cleanup AttemptHistory orphan legacy — bisa digabung satu sesi.
5. Lingkup = DB LOKAL saja. Dev (10.55.3.3) = jalur IT, jangan edit langsung.

## Update 2026-06-21 — recon + keputusan (DITUNDA, tetap pending)

**Prasyarat OK:** Phase 367 SUDAH ship (cascade endpoints ada: `DeleteAssessment` :2288, `DeleteAssessmentGroup` :2424, `DeletePrePostGroup` :2570 di `AssessmentAdminController.cs`). Siap dieksekusi kapan saja.

**Recon DB lokal `HcPortalDB_Dev` (SQLEXPRESS):** 45 judul sesi, ±70 baris `AssessmentSessions`. `SeedData.cs` TIDAK seed AssessmentSession (count 0) → semua sesi runtime, hapus tak bentrok re-seed.

**Keputusan user (2026-06-21):** DB lokal terpisah total dari server Dev (data asli ada di Dev/IT, bukan lokal). Semua sesi lokal = bekas test user → **HAPUS SEMUA** (Bucket A test jelas + Bucket B yg dulu diragukan: "Post Test training lisensor SRU Samarinda", "Pre Test OJT GAST Cilacap", "OJT Proses Alkylation Q*", "Assessment Proton Tahun 1/3", "OJT Semarang"). Tak ada data asli yg hilang.

**Satu-satunya hati2:** sebagian sesi mungkin masih dipakai fixture test otomatis (Playwright e2e) → backup dulu, kalau ada test error tinggal restore.

**Langkah eksekusi siap (saat dilanjut):**
1. Stop semua Kestrel (main 5277 + worktree).
2. Snapshot: `sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Temp\HcPortalDB_Dev.cleanup.bak' WITH INIT"`.
3. Catat `SEED_JOURNAL.md` status `active` (tag `cleanup + local-only`).
4. Hapus SEMUA sesi via fitur cascade UI (preview konfirmasi) = sekalian UAT real-data 367.
5. Verify zero orphan per tabel (PackageQuestions/Options/UserResponses/AttemptHistory/EditLogs/UserPackageAssignments/SessionElemenTeknisScores) + file cert.
6. Verify OK → **keep bersih** (JANGAN restore, nanti junk balik), journal→`cleaned`, hapus `.bak`. Kalau salah → restore dari `.bak`.
7. ⚠ Ini cleanup PERMANEN (beda dari seed temporary) — snapshot = asuransi rollback, BUKAN restore terjadwal. Gabung sekalian Phase 368 cleanup AttemptHistory orphan.
