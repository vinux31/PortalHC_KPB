-- =============================================
-- HC Portal - Database Setup Script
-- Untuk Development di SQL Server Express Lokal
-- =============================================

-- CARA CONNECT DI SSMS (PENTING!):
-- 1. Server Name: localhost\SQLEXPRESS  (atau hanya: .)
-- 2. Authentication: Windows Authentication
-- 3. Klik "Options" >> tab "Connection Properties"
-- 4. Centang "Trust server certificate"
-- 5. Klik "Connect"
--
-- ATAU gunakan connection string ini di SSMS:
-- Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true

-- 1. Buat Database
USE master;
GO

-- Drop database jika sudah ada (untuk fresh start)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'HcPortalDB_Dev')
BEGIN
    ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HcPortalDB_Dev;
END
GO

-- Buat database baru
CREATE DATABASE HcPortalDB_Dev;
GO

PRINT '✅ Database HcPortalDB_Dev berhasil dibuat';
GO

-- 2. Buat Login untuk Aplikasi
USE master;
GO

-- Drop login jika sudah ada
IF EXISTS (SELECT name FROM sys.server_principals WHERE name = 'hcportal_dev')
BEGIN
    DROP LOGIN hcportal_dev;
END
GO

-- Buat login baru
CREATE LOGIN hcportal_dev 
WITH PASSWORD = 'Dev123456!',
     DEFAULT_DATABASE = HcPortalDB_Dev,
     CHECK_POLICY = OFF,
     CHECK_EXPIRATION = OFF;
GO

PRINT '✅ Login hcportal_dev berhasil dibuat';
GO

-- 3. Buat User di Database dan Berikan Permission
USE HcPortalDB_Dev;
GO

-- Buat user
CREATE USER hcportal_dev FOR LOGIN hcportal_dev;
GO

-- Berikan role db_owner (full access untuk development)
ALTER ROLE db_owner ADD MEMBER hcportal_dev;
GO

PRINT '✅ User hcportal_dev berhasil dibuat dengan permission db_owner';
GO

-- 4. Verifikasi Setup
SELECT 
    'Database' AS Component,
    name AS Name,
    state_desc AS Status,
    recovery_model_desc AS RecoveryModel
FROM sys.databases 
WHERE name = 'HcPortalDB_Dev';

SELECT 
    'Login' AS Component,
    name AS Name,
    type_desc AS Type,
    create_date AS Created
FROM sys.server_principals 
WHERE name = 'hcportal_dev';

SELECT 
    'User' AS Component,
    name AS Name,
    type_desc AS Type,
    create_date AS Created
FROM sys.database_principals 
WHERE name = 'hcportal_dev';

PRINT '';
PRINT '========================================';
PRINT '✅ Setup Database Selesai!';
PRINT '';
PRINT 'Connection String untuk Development:';
PRINT 'Server=localhost\SQLEXPRESS;Database=HcPortalDB_Dev;User Id=hcportal_dev;Password=Dev123456!;TrustServerCertificate=True;MultipleActiveResultSets=true';
PRINT '========================================';
GO
