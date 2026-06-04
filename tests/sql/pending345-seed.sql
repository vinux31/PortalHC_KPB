-- ============================================================
-- Phase 345 CMP06R-05 — Pending-Grade UAT Seed
-- REQ: CMP06R-05
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession Status='Completed' + IsPassed=NULL (essay submit, belum
--   dinilai HC) untuk UAT badge "Menunggu Penilaian" di 3 surface (RecordsWorkerDetail
--   + UserAssessmentHistory + BulkExportPdf). Peserta = coachee (rino.prasetyo).
--
-- SCOPE (WR-01): state Status='Completed' + IsPassed=NULL ADALAH target Phase 345
--   (CONTEXT D-03) — sesi yang LOLOS filter GetUnifiedRecords (Status=='Completed') tapi
--   IsPassed null. Sesi ber-Status='Menunggu Penilaian' MURNI (yang ditulis GradingService)
--   saat ini di-exclude filter itu = cakupan Phase 346 REC-07, BUKAN 345. Seed ini sengaja
--   mereproduksi state Phase-345 yang diperbaiki, bukan men-cover REC-07.
--
-- Klasifikasi: temporary + local-only. Prefix Title '[PENDING345]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\pending345-seed.sql
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [PENDING345] dulu, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di AspNetUsers (coachee fixture).
-- ============================================================

SET NOCOUNT ON;

-- Cleanup fixture lama (idempotent re-run). LIKE '[[]PENDING345]%' = literal '[PENDING345]'.
DELETE FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%';

DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51345, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- Sesi Completed + IsPassed=NULL (essay pending). CompletedAt non-null -> masuk eligibility
-- BulkExportPdf (L4507) + filter Status=='Completed' GetUnifiedRecords (L33). Score=NULL.
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
VALUES
    (@uid, '[PENDING345] Essay Pending', 'OJT', GETDATE(), 60, 'Completed', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL,
     0, '', GETDATE(), 0, 0, 1, 0);

-- Layer 1 echo (informational).
SELECT COUNT(*) AS PendingSeeded FROM AssessmentSessions WHERE Title LIKE '[[]PENDING345]%';
