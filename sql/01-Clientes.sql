-- Repair.Wpf v0.2. Esquema de clientes conservado de v0.1; ejecución manual en SQL Server.
-- Base nueva y exclusiva del proyecto. No usa ni modifica bases ajenas.
USE [master];
GO
IF DB_ID(N'RepairAcademico') IS NULL
    EXEC(N'CREATE DATABASE [RepairAcademico];');
GO
USE [RepairAcademico];
GO
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
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
END;
GO
-- Si ya había una tabla, validar sus columnas antes de ejecutar la aplicación.
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'Clientes'
ORDER BY ORDINAL_POSITION;
GO
-- No hay datos personales de ejemplo ni tabla de equipos en este avance.
