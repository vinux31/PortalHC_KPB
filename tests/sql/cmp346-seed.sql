-- ============================================================
-- Phase 346 Plan 06 (REC-07) — Include PendingGrading UAT Seed
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession Status='Menunggu Penilaian' (MURNI PendingGrading, IsPassed=NULL)
--   untuk coachee (rino.prasetyo). Sesi ini SEBELUM Phase 346 di-EXCLUDE dari My Records +
--   team history (filter lama Status=='Completed'); SETELAH REC-07 (WHERE include
--   AssessmentConstants.AssessmentStatus.PendingGrading) HARUS muncul berlabel "Menunggu Penilaian".
--   (Beda dari seed Phase 345 yang pakai Status='Completed'+IsPassed=NULL.)
--
-- Klasifikasi: temporary + local-only. Prefix Title '[PENDING346]' untuk Layer 1 seeded-check
--   + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\cmp346-seed.sql
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [PENDING346] dulu, lalu INSERT.
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di AspNetUsers (coachee fixture).
-- ============================================================

SET NOCOUNT ON;

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]PENDING346]%';

DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51346, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- Sesi Status='Menunggu Penilaian' MURNI + IsPassed=NULL (REC-07 target).
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
VALUES
    (@uid, '[PENDING346] Essay Murni Pending', 'OJT', GETDATE(), 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL,
     0, '', GETDATE(), 0, 0, 1, 0);

SELECT COUNT(*) AS Pending346Seeded FROM AssessmentSessions WHERE Title LIKE '[[]PENDING346]%';
