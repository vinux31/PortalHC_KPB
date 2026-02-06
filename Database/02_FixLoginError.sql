-- =============================================
-- FIX: Reset User dan Password
-- Jalankan script ini di SSMS jika ada error login
-- =============================================

USE master;
GO

-- 1. Drop dan recreate login
IF EXISTS (SELECT name FROM sys.server_principals WHERE name = 'hcportal_dev')
BEGIN
    DROP LOGIN hcportal_dev;
    PRINT '✅ Login lama dihapus';
END
GO

-- 2. Buat login baru dengan password yang benar
CREATE LOGIN hcportal_dev 
WITH PASSWORD = 'Dev123456!',
     DEFAULT_DATABASE = HcPortalDB_Dev,
     CHECK_POLICY = OFF,
     CHECK_EXPIRATION = OFF;
GO

PRINT '✅ Login baru dibuat';
GO

-- 3. Pastikan user di database ada
USE HcPortalDB_Dev;
GO

-- Drop user lama jika ada
IF EXISTS (SELECT name FROM sys.database_principals WHERE name = 'hcportal_dev')
BEGIN
    DROP USER hcportal_dev;
    PRINT '✅ User lama dihapus';
END
GO

-- Buat user baru
CREATE USER hcportal_dev FOR LOGIN hcportal_dev;
GO

-- Berikan permission
ALTER ROLE db_owner ADD MEMBER hcportal_dev;
GO

PRINT '✅ User baru dibuat dengan permission db_owner';
GO

-- 4. Test koneksi
PRINT '';
PRINT '========================================';
PRINT '✅ Setup Selesai!';
PRINT '';
PRINT 'Test koneksi dengan:';
PRINT 'Server=localhost\SQLEXPRESS';
PRINT 'Database=HcPortalDB_Dev';
PRINT 'User Id=hcportal_dev';
PRINT 'Password=Dev123456!';
PRINT '========================================';
GO
