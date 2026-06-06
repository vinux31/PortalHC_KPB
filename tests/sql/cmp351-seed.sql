-- ============================================================
-- Phase 351 (SF-03 / SF-04) — Worker Detail + cross-surface filter UAT Seed
-- ============================================================
--
-- Tujuan:
--   Seed 1 TrainingRecord '[PENDING351] Legacy Training Migas' dengan Kategori
--   OFF-MASTER 'Legacy-FreeText-351' (TIDAK ada di master AssessmentCategories)
--   untuk worker rino.prasetyo.
--   SEBELUM SF-04: kategori off-master ini TIDAK muncul di dropdown
--   #categoryFilter (Worker Detail / My Records) karena opsi diambil dari master.
--   SETELAH SF-04: opsi distinct-actual (dari unifiedRecords.Kategori) memunculkan
--   'Legacy-FreeText-351' → baris record bisa di-filter.
--   Record ini juga jadi fixture SF-03: search term non-matching → empty-state.
--
-- Klasifikasi: temporary + local-only. Prefix Judul '[PENDING351]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\cmp351-seed.sql
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [PENDING351] dulu, lalu INSERT.
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture,
--   section ter-akses login manager Playwright — sama dgn cmp350-seed.sql).
-- ============================================================

SET NOCOUNT ON;

DELETE FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%';

DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51351, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- TrainingRecord dengan Kategori off-master (bukan di AssessmentCategories) — SF-04 fixture.
INSERT INTO TrainingRecords (UserId, Judul, Kategori, Tanggal, Status)
VALUES (@uid, '[PENDING351] Legacy Training Migas', 'Legacy-FreeText-351', GETDATE(), 'Valid');

SELECT COUNT(*) AS Pending351Seeded FROM TrainingRecords WHERE Judul LIKE '[[]PENDING351]%';
