-- ============================================================
-- Phase 313 Timer Fixtures - Block Manual Submit Saat Waktu Habis
-- REQ: TMR-01
-- ============================================================
--
-- Tujuan:
--   Seed 7 fixture AssessmentSessions dengan StartedAt back-dated (D-07)
--   + dedicated title pattern (D-08) untuk Wave 0 Playwright FLOW 313
--   + Manual UAT 313-UAT.md.
--
-- Cara run (DB lokal saja - JANGAN jalankan di Dev/Prod per CLAUDE.md DEV_WORKFLOW):
--   - SSMS: open file, F5 (execute) terhadap koneksi DB lokal HcPortal
--   - sqlcmd: sqlcmd -S localhost -d HcPortal -E -i .planning\seeds\313-timer-fixtures.sql
--
-- Idempotent:
--   Script ini WIPE-AND-INSERT - re-run aman, akan delete fixture lama (title prefix
--   'Phase 313 Timer Fixture') sebelum INSERT 7 row baru. Tidak akan menyentuh data lain.
--
-- Pre-condition:
--   1. User dengan Email = 'rino.prasetyo@pertamina.com' ada di AspNetUsers (coachee fixture).
--      Verify: SELECT Email FROM AspNetUsers WHERE Email='rino.prasetyo@pertamina.com';
--   2. Skema AssessmentSessions sesuai snapshot 2026-05-08
--      (verified Migrations/ApplicationDbContextModelSnapshot.cs:286-459)
--
-- Anti-pattern Phase 309 (UserId=NULL FK violation):
--   UserId DIWAJIBKAN valid - script pakai subquery + THROW guard kalau user tidak ada.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ============================================================
-- 1. Resolve referensi user + waktu acuan
-- ============================================================
DECLARE @UserId NVARCHAR(450) = (
    SELECT TOP 1 Id
    FROM AspNetUsers
    WHERE Email = 'rino.prasetyo@pertamina.com'
);

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Validation guards (anti-pattern Phase 309 mitigation)
IF @UserId IS NULL
BEGIN
    THROW 50001,
        'User rino.prasetyo@pertamina.com tidak ditemukan di AspNetUsers - abort. Jalankan seed user coachee terlebih dahulu.',
        1;
END;

PRINT N'[Phase 313 seed] UserId resolved: ' + @UserId;
PRINT N'[Phase 313 seed] @Now = ' + CONVERT(NVARCHAR(30), @Now, 126);

-- ============================================================
-- 2. Idempotent cleanup - hapus fixture lama (title prefix)
-- ============================================================
DELETE FROM AssessmentSessions
WHERE Title LIKE 'Phase 313 Timer Fixture%';

PRINT N'[Phase 313 seed] Cleanup selesai (delete fixture lama).';

-- ============================================================
-- 3. Insert 7 fixture (D-07 back-dated + D-08 title pattern)
-- ============================================================
-- Matrix (Duration=60 min untuk semua, ExtraTime=NULL):
--   # | Title (Type, Scenario)                          | AssessmentType | StartedAt offset | Expected
--   1 | Phase 313 Timer Fixture Online ManualBeforeTime | Online         | NOW - 5 min      | Submit OK
--   2 | Phase 313 Timer Fixture Online ManualAfterGrace | Online         | NOW - 61 min     | Tier-1 BLOCK
--   3 | Phase 313 Timer Fixture Online AutoInGrace      | Online         | NOW - 61 min     | Submit OK (Tier-2 grace)
--   4 | Phase 313 Timer Fixture Online AutoAfterGrace   | Online         | NOW - 67 min     | Tier-2 BLOCK
--   5 | Phase 313 Timer Fixture PreTest ManualAfterGrace| PreTest        | NOW - 61 min     | Tier-1 BLOCK
--   6 | Phase 313 Timer Fixture PostTest ManualAfterGrace| PostTest      | NOW - 61 min     | Tier-1 BLOCK
--   7 | Phase 313 Timer Fixture Manual ExcludeVerify    | Manual         | NOW - 161 min    | Submit OK (D-15 exclude)

-- NB: Field NOT NULL yang harus diisi (per ApplicationDbContextModelSnapshot.cs):
--   Title, Category, UserId, AccessToken, BannerColor, Status, Schedule, DurationMinutes,
--   ElapsedSeconds (default 0), GenerateCertificate, HasManualGrading, IsManualEntry,
--   IsTokenRequired, PassPercentage, Progress, SamePackage, AllowAnswerReview, CreatedAt.
-- AssessmentSession.Schedule adalah DateTime (bukan FK Schedules) - pakai @Now.

INSERT INTO AssessmentSessions (
    Title, Category, UserId, AssessmentType,
    DurationMinutes, ExtraTimeMinutes, StartedAt, Status,
    Schedule, AccessToken, BannerColor,
    AllowAnswerReview, GenerateCertificate, HasManualGrading,
    IsManualEntry, IsTokenRequired, SamePackage,
    PassPercentage, Progress, ElapsedSeconds,
    CreatedAt
)
VALUES
    -- 1: Online + Manual + before-time -> submit OK (regression)
    ( 'Phase 313 Timer Fixture Online ManualBeforeTime', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -5, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 300,
      @Now ),

    -- 2: Online + Manual + after-time (in grace) -> Tier-1 BLOCK
    ( 'Phase 313 Timer Fixture Online ManualAfterGrace', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 3: Online + Auto + after-time (in grace) -> submit OK (Tier-2 grace covers)
    ( 'Phase 313 Timer Fixture Online AutoInGrace', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 4: Online + Auto + after-grace -> Tier-2 BLOCK (existing preserved)
    ( 'Phase 313 Timer Fixture Online AutoAfterGrace', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -67, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 4020,
      @Now ),

    -- 5: PreTest + Manual + after-time (in grace) -> Tier-1 BLOCK
    ( 'Phase 313 Timer Fixture PreTest ManualAfterGrace', 'Test Phase 313', @UserId, 'PreTest',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 6: PostTest + Manual + after-time (in grace) -> Tier-1 BLOCK
    ( 'Phase 313 Timer Fixture PostTest ManualAfterGrace', 'Test Phase 313', @UserId, 'PostTest',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 7: Manual + Manual + after-time -> submit OK (D-15 exclude verify)
    ( 'Phase 313 Timer Fixture Manual ExcludeVerify', 'Test Phase 313', @UserId, 'Manual',
      60, NULL, DATEADD(MINUTE, -161, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 9660,
      @Now );

PRINT N'[Phase 313 seed] 7 fixture berhasil di-INSERT.';

-- ============================================================
-- 4. Final verification SELECT
-- ============================================================
SELECT
    Id,
    Title,
    AssessmentType,
    StartedAt,
    DurationMinutes,
    ISNULL(ExtraTimeMinutes, 0) AS ExtraTimeMinutes,
    DATEDIFF(MINUTE, StartedAt, SYSUTCDATETIME()) AS ElapsedMinutes,
    Status,
    UserId
FROM AssessmentSessions
WHERE Title LIKE 'Phase 313 Timer Fixture%'
ORDER BY Title;
