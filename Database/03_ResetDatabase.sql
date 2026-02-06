-- =============================================
-- RESET DATABASE untuk Fresh Migration
-- Jalankan script ini di SSMS sebelum 
-- menjalankan dotnet ef migrations add
-- =============================================

USE master;
GO

-- Drop database dan recreate untuk fresh start
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'HcPortalDB_Dev')
BEGIN
    ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HcPortalDB_Dev;
    PRINT '✅ Database lama dihapus';
END
GO

-- Buat database baru (kosong)
CREATE DATABASE HcPortalDB_Dev;
GO

PRINT '✅ Database HcPortalDB_Dev baru dibuat (kosong)';
PRINT '';
PRINT 'Sekarang jalankan di terminal:';
PRINT '1. dotnet ef migrations add InitialSqlServer';
PRINT '2. dotnet ef database update';
PRINT '3. dotnet run';
GO
