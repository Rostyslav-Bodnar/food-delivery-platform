IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'UserServiceDb')
BEGIN
    CREATE DATABASE UserServiceDb;
END
GO

IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = N'DFUserService')
BEGIN
    CREATE LOGIN DFUserService WITH PASSWORD = 'UserService2025!';
END
GO

USE UserServiceDb;
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'DFUserService')
BEGIN
    CREATE USER DFUserService FOR LOGIN DFUserService;
    ALTER ROLE db_owner ADD MEMBER DFUserService;
END
GO
