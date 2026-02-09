-- =============================================
-- Verify and Check AspNetUsers Table
-- =============================================

USE HcPortalDb_Dev;
GO

-- Check if table exists
IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NOT NULL
    PRINT '✅ Table AspNetUsers EXISTS'
ELSE
    PRINT '❌ Table AspNetUsers DOES NOT EXIST'
GO

-- Check record count
SELECT COUNT(*) AS TotalUsers FROM dbo.AspNetUsers;

-- Show all users if any
SELECT 
    Id,
    UserName,
    Email,
    FullName,
    Position,
    Section,
    Unit,
    RoleLevel
FROM dbo.AspNetUsers
ORDER BY RoleLevel;

-- Check all Identity tables
SELECT 'AspNetUsers' AS TableName, COUNT(*) AS RecordCount FROM dbo.AspNetUsers
UNION ALL
SELECT 'AspNetRoles', COUNT(*) FROM dbo.AspNetRoles
UNION ALL
SELECT 'AspNetUserRoles', COUNT(*) FROM dbo.AspNetUserRoles;
