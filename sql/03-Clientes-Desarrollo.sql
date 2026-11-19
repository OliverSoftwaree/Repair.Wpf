-- Base exclusiva de este avance. No ejecuta DELETE, DROP, TRUNCATE ni migraciones.
-- Ejecutar el archivo COMPLETO. Si el nombre ya existe sin nuestra marca, se detiene.
USE [master];
SET NOCOUNT ON;
IF DB_ID(N'RepairClientesDesarrollo_20261030') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [RepairClientesDesarrollo_20261030];');
    EXEC(N'USE [RepairClientesDesarrollo_20261030];
        EXEC sys.sp_addextendedproperty @name=N''RepairAvance'', @value=N''Clientes-20261030'';');
END;
IF NOT EXISTS (SELECT 1 FROM [RepairClientesDesarrollo_20261030].sys.extended_properties
    WHERE class = 0 AND name = N'RepairAvance' AND CONVERT(nvarchar(100), value) = N'Clientes-20261030')
    THROW 51000, 'La base existente no pertenece a este avance. No se modifico.', 1;
EXEC(N'USE [RepairClientesDesarrollo_20261030];
IF OBJECT_ID(N''dbo.Clientes'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
        Nombre nvarchar(100) NOT NULL,
        Telefono nvarchar(25) NOT NULL,
        Correo nvarchar(150) NULL,
        Notas nvarchar(300) NULL,
        CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Clientes_CreadoUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Clientes_Nombre CHECK (LEN(LTRIM(RTRIM(Nombre))) >= 2),
        CONSTRAINT CK_Clientes_Telefono CHECK (LEN(LTRIM(RTRIM(Telefono))) >= 7)
    );
END;');
SELECT name, state_desc FROM sys.databases WHERE name = N'RepairClientesDesarrollo_20261030';
-- La tabla Equipos se instala después con 02-Equipos.sql en esta misma base.
