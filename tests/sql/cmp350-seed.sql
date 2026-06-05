-- ============================================================
-- Phase 350 (SF-01 / SF-06) — Team View assessment-title search UAT Seed
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession Status='Completed' dengan judul DISTINCT
--   '[PENDING350] OJT v14.2 Migas' + Category='OJT' untuk worker rino.prasetyo.
--   SEBELUM SF-01: search judul assessment "ojt v14.2" di Team View (Lingkup
--   "Keduanya") → 0 worker (bug 999.2). SETELAH SF-01: worker pemilik sesi ini
--   HARUS muncul. Category='OJT' membuat jalur SF-06 (export Category-narrow)
--   bisa diuji. (Beda dari seed 346 yang Status='Menunggu Penilaian'/IsPassed=NULL.)
--
-- Klasifikasi: temporary + local-only. Prefix Title '[PENDING350]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\cmp350-seed.sql
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [PENDING350] dulu, lalu INSERT.
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture,
--   section ter-akses login manager/hc Playwright — sama dgn cmp346-seed.sql).
-- ============================================================

SET NOCOUNT ON;

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%';

DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51350, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- Sesi Status='Completed' + judul searchable 'OJT v14.2' + Category='OJT' (SF-01 repro + SF-06 path).
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
VALUES
    (@uid, '[PENDING350] OJT v14.2 Migas', 'OJT', GETDATE(), 60, 'Completed', 100, 'bg-primary',
     70, 1, 0, 1, GETDATE(), 80,
     0, '', GETDATE(), 0, 0, 0, 0);

SELECT COUNT(*) AS Pending350Seeded FROM AssessmentSessions WHERE Title LIKE '[[]PENDING350]%';
