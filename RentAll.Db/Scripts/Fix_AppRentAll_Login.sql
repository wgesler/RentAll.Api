-- Script to create/fix the app_rentall login and user
-- Run this script as a SQL Server administrator (sa or sysadmin)

USE [master];
GO

-- Check if the login exists, create it if it doesn't
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'app_rentall')
BEGIN
    CREATE LOGIN [app_rentall] WITH PASSWORD = 'JnPYaeQZ0L6nr8P';
    PRINT 'Login app_rentall created successfully.';
END
ELSE
BEGIN
    -- Update the password if login exists (in case password changed)
    ALTER LOGIN [app_rentall] WITH PASSWORD = 'JnPYaeQZ0L6nr8P';
    PRINT 'Login app_rentall already exists. Password updated.';
END
GO

-- Switch to the RentAll database
USE [RentAll];
GO

-- Check if app_rentall exists as a ROLE (this is the problem!)
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'app_rentall' AND type_desc = 'DATABASE_ROLE')
BEGIN
    PRINT 'WARNING: app_rentall exists as a DATABASE_ROLE. This must be removed first.';
    PRINT 'Removing members from the role before dropping it...';
    
    -- Remove all members from the role first
    DECLARE @sql NVARCHAR(MAX) = '';
    SELECT @sql = @sql + 'ALTER ROLE [app_rentall] DROP MEMBER [' + dp.name + '];' + CHAR(13)
    FROM sys.database_role_members rm
    JOIN sys.database_principals dp ON rm.member_principal_id = dp.principal_id
    WHERE rm.role_principal_id = (SELECT principal_id FROM sys.database_principals WHERE name = 'app_rentall' AND type_desc = 'DATABASE_ROLE');
    
    IF @sql <> ''
    BEGIN
        EXEC sp_executesql @sql;
        PRINT 'Members removed from app_rentall role.';
    END
    
    -- Drop the role
    DROP ROLE [app_rentall];
    PRINT 'DATABASE_ROLE app_rentall has been dropped.';
END

-- Check if the user exists in the database, create it if it doesn't
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'app_rentall' AND type_desc = 'SQL_USER')
BEGIN
    CREATE USER [app_rentall] FOR LOGIN [app_rentall];
    PRINT 'User app_rentall created in RentAll database.';
END
ELSE
BEGIN
    PRINT 'User app_rentall already exists in RentAll database.';
END
GO

-- Grant necessary permissions
-- Grant db_datareader and db_datawriter for basic CRUD operations
ALTER ROLE [db_datareader] ADD MEMBER [app_rentall];
ALTER ROLE [db_datawriter] ADD MEMBER [app_rentall];

-- Grant execute permission on stored procedures
GRANT EXECUTE ON SCHEMA::[dbo] TO [app_rentall];

PRINT 'Permissions granted to app_rentall user.';
GO

-- Verify the setup - Check login at server level
PRINT '=== Checking Login at Server Level ===';
SELECT 
    name AS LoginName,
    type_desc AS LoginType,
    is_disabled AS IsDisabled,
    create_date AS CreateDate
FROM sys.server_principals 
WHERE name = 'app_rentall';

-- Verify the setup - Check user at database level
PRINT '=== Checking User at Database Level ===';
SELECT 
    name AS UserName,
    type_desc AS UserType,
    default_schema_name AS DefaultSchema,
    create_date AS CreateDate
FROM sys.database_principals 
WHERE name = 'app_rentall';

-- Verify the setup - Check if login is mapped to user
PRINT '=== Checking Login to User Mapping ===';
SELECT 
    sp.name AS LoginName,
    dp.name AS UserName,
    dp.default_schema_name AS DefaultSchema,
    CASE 
        WHEN sp.sid = dp.sid THEN 'Mapped Correctly'
        ELSE 'NOT MAPPED - SID Mismatch'
    END AS MappingStatus
FROM sys.server_principals sp
LEFT JOIN sys.database_principals dp ON sp.sid = dp.sid
WHERE sp.name = 'app_rentall';

-- Check permissions
PRINT '=== Checking Permissions ===';
SELECT 
    dp.name AS UserName,
    r.name AS RoleName
FROM sys.database_principals dp
JOIN sys.database_role_members rm ON dp.principal_id = rm.member_principal_id
JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
WHERE dp.name = 'app_rentall';

PRINT '=== Setup Verification Complete ===';
GO

