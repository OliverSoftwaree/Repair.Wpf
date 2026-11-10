SELECT * FROM dbo.Clientes;

USE [RepairAcademico];
GO

INSERT INTO dbo.Clientes (Nombre, Telefono, Correo, Notas)
VALUES
('Ana de Ejemplo', '809-555-0101', 'ana@test.com', 'Cliente de prueba');
GO

USE [RepairAcademico];
GO

IF OBJECT_ID(N'dbo.Ordenes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ordenes
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ClienteId INT NOT NULL,
        Estado NVARCHAR(50) NOT NULL DEFAULT 'Pendiente',
        Diagnostico NVARCHAR(MAX) NULL,
        Observaciones NVARCHAR(MAX) NULL,
        CreadoUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

INSERT INTO dbo.Ordenes (ClienteId, Estado, Diagnostico, Observaciones)
VALUES (1, 'Pendiente', '', 'Orden de prueba');

USE [RepairAcademico];
GO

IF OBJECT_ID('dbo.Ordenes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ordenes (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ordenes PRIMARY KEY,
        ClienteId INT NOT NULL,
        Estado NVARCHAR(50) NOT NULL CONSTRAINT DF_Ordenes_Estado DEFAULT ('Pendiente'),
        Diagnostico NVARCHAR(MAX) NULL,
        Observaciones NVARCHAR(MAX) NULL,
        CreadoUtc DATETIME2 NOT NULL CONSTRAINT DF_Ordenes_CreadoUtc DEFAULT (SYSUTCDATETIME())
    );
END;
GO

USE [RepairAcademico];
GO

IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipos
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipos PRIMARY KEY,
        ClienteId int NOT NULL,
        Marca nvarchar(60) NOT NULL,
        Modelo nvarchar(100) NOT NULL,
        Serie nvarchar(100) NULL,
        Accesorios nvarchar(300) NULL,
        Condicion nvarchar(500) NOT NULL,
        CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Equipos_CreadoUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Equipos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id)
    );
END;
GO
USE [RepairAcademico];
GO

IF OBJECT_ID(N'dbo.Ordenes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ordenes
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ordenes PRIMARY KEY,
        ClienteId int NOT NULL,
        Estado nvarchar(50) NOT NULL CONSTRAINT DF_Ordenes_Estado DEFAULT ('Pendiente'),
        Diagnostico nvarchar(max) NULL,
        Observaciones nvarchar(max) NULL,
        CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Ordenes_CreadoUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Ordenes_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id)
    );
END;
INSERT INTO dbo.Equipos (ClienteId, Marca, Modelo, Serie, Accesorios, Condicion)
VALUES (1, 'Dell', 'Latitude 5400', 'SN-001', 'Cargador', 'Pantalla con ligero fallo y batería en revisión');

DELETE FROM dbo.Equipos;
DBCC CHECKIDENT('dbo.Equipos', RESEED, 0);

DELETE FROM dbo.Ordenes;
DBCC CHECKIDENT('dbo.Ordenes', RESEED, 0);

DELETE FROM dbo.Clientes;
DBCC CHECKIDENT('dbo.Clientes', RESEED, 0);

SELECT Id, Nombre FROM dbo.Clientes;
SELECT Id, ClienteId, Estado FROM dbo.Ordenes;

INSERT INTO dbo.Ordenes (ClienteId, Estado, Diagnostico, Observaciones)
VALUES (3, N'Pendiente', N'', N'Orden del cliente 3');

DELETE FROM dbo.Ordenes;
DBCC CHECKIDENT('dbo.Ordenes', RESEED, 0);

DELETE FROM dbo.Equipos;
DBCC CHECKIDENT('dbo.Equipos', RESEED, 0);

DELETE FROM dbo.Clientes;
DBCC CHECKIDENT('dbo.Clientes', RESEED, 0);