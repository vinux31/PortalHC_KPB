-- Phase 324 D-04..D-06 cleanup: hapus TrainingRecord auto-generated antara 2026-04-10 sampai hari ini.
-- Sumber bug: commit 766011b6 (2026-04-10) re-introduce auto-create TR yang menyebabkan visual duplicate di /CMP/Records.
-- Fix di code: Phase 324 Plan 01 hapus block insert TR di 3 lokasi production (GradingService + AssessmentAdminController).
-- Script ini idempotent: re-run aman, post-count = 0 = nothing to do.
-- Filter column: TanggalSelesai (TrainingRecords model TIDAK ada CreatedAt column - per RESEARCH A3 resolved).
-- Safety cap: 5000 row.
-- Database name: HcPortalDB_Dev (lokal). IT adjust ke HcPortalDB saat run di Prod via USE statement.
--
-- ENV NOTE:
-- - Script ini PRIMARY untuk Dev/Prod (data post-regression Apr 10).
-- - Untuk LOKAL recovery legacy data pre-Mar-18 (era ASSESS-08), pakai inline command dengan filter
--   pattern-only (no date filter). Lihat docs/SEED_JOURNAL.md entry Phase 324 untuk detail.

USE HcPortalDB_Dev;
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION cleanup324;

    -- 1. Pre-count
    DECLARE @preCount INT;
    SELECT @preCount = COUNT(*)
    FROM TrainingRecords
    WHERE Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10';
    PRINT CONCAT('Phase 324 cleanup pre-count: ', @preCount);

    -- 2. Safety cap (defensive guard)
    IF @preCount > 5000
    BEGIN
        ROLLBACK TRANSACTION cleanup324;
        RAISERROR('Safety cap exceeded (%d > 5000). Investigate before bumping cap.', 16, 1, @preCount);
        RETURN;
    END

    -- 3. (SKIPPED - RESEARCH OQ #3 resolved: orphan count = 0 di lokal verified Task 2)
    --    Tidak perlu null-clear RenewsTrainingId pre-step karena tidak ada child TR yang
    --    reference parent TR auto-generated. Kalau IT temui orphan > 0 di Dev/Prod via
    --    pre-check query, uncomment block berikut:
    -- UPDATE TrainingRecords
    -- SET RenewsTrainingId = NULL
    -- WHERE RenewsTrainingId IN (
    --     SELECT Id FROM TrainingRecords
    --     WHERE Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10'
    -- );
    -- DECLARE @nullClearedCount INT = @@ROWCOUNT;
    -- PRINT CONCAT('Phase 324 null-cleared RenewsTrainingId in child rows: ', @nullClearedCount);

    -- 4. DELETE
    DELETE FROM TrainingRecords
    WHERE Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10';
    DECLARE @deletedCount INT = @@ROWCOUNT;
    PRINT CONCAT('Phase 324 cleanup deleted: ', @deletedCount);

    -- 5. Post-count verify
    DECLARE @postCount INT;
    SELECT @postCount = COUNT(*)
    FROM TrainingRecords
    WHERE Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10';
    PRINT CONCAT('Phase 324 cleanup post-count: ', @postCount);

    IF @postCount > 0
    BEGIN
        ROLLBACK TRANSACTION cleanup324;
        RAISERROR('Phase 324 post-count not zero (%d). Rolled back.', 16, 1, @postCount);
        RETURN;
    END

    COMMIT TRANSACTION cleanup324;
    PRINT 'Phase 324 cleanup committed successfully.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION cleanup324;
    DECLARE @errMsg NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT CONCAT('Phase 324 cleanup failed: ', @errMsg);
    THROW;
END CATCH;
GO

-- Sanity check sample (IT bisa uncomment untuk visual inspection pre-run):
-- SELECT TOP 20 Id, UserId, LEFT(Judul, 50) AS Judul, Kategori, Tanggal, TanggalSelesai, Penyelenggara, Status
-- FROM TrainingRecords
-- WHERE Judul LIKE 'Assessment:%' AND TanggalSelesai >= '2026-04-10'
-- ORDER BY Id DESC;
