SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'RepairSeguimientoDesarrollo_20261115' OR NOT EXISTS (
    SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = N'RepairAvance'
    AND CONVERT(nvarchar(100), value) = N'Seguimiento-20261115')
    THROW 51002, 'La base existente no pertenece a Seguimiento. No se modificó.', 1;

BEGIN TRANSACTION;
DECLARE @bloqueo int;
EXEC @bloqueo = sys.sp_getapplock @Resource = N'EsquemaRepairSeguimiento',
    @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
IF @bloqueo < 0 THROW 51003, 'El esquema se está preparando en otra sesión.', 1;

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
    CREATE TABLE dbo.Clientes (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
        Nombre nvarchar(100) NOT NULL,
        Telefono nvarchar(25) NOT NULL,
        Correo nvarchar(150) NULL,
        Notas nvarchar(300) NULL,
        CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Clientes_CreadoUtc DEFAULT SYSUTCDATETIME()
    );

IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
    CREATE TABLE dbo.Equipos (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipos PRIMARY KEY,
        ClienteId int NOT NULL CONSTRAINT FK_Equipos_Clientes REFERENCES dbo.Clientes(Id),
        Tipo nvarchar(60) NULL,
        Marca nvarchar(60) NOT NULL,
        Modelo nvarchar(100) NOT NULL,
        Serie nvarchar(100) NULL,
        Accesorios nvarchar(300) NULL,
        Condicion nvarchar(500) NOT NULL,
        CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Equipos_CreadoUtc DEFAULT SYSUTCDATETIME()
    );

IF OBJECT_ID(N'dbo.Ordenes', N'U') IS NULL
    CREATE TABLE dbo.Ordenes (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ordenes PRIMARY KEY,
        ClienteId int NOT NULL CONSTRAINT FK_Ordenes_Clientes REFERENCES dbo.Clientes(Id),
        Estado nvarchar(50) NOT NULL CONSTRAINT DF_Ordenes_Estado DEFAULT N'Pendiente',
        Diagnostico nvarchar(max) NULL,
        Observaciones nvarchar(max) NULL,
        CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Ordenes_CreadoUtc DEFAULT SYSUTCDATETIME()
    );

IF COL_LENGTH(N'dbo.Ordenes', N'EquipoId') IS NULL
    ALTER TABLE dbo.Ordenes ADD EquipoId int NULL CONSTRAINT FK_Ordenes_Equipos REFERENCES dbo.Equipos(Id);

IF COL_LENGTH(N'dbo.Ordenes', N'EntregadaUtc') IS NULL
    ALTER TABLE dbo.Ordenes ADD EntregadaUtc datetime2(7) NULL;

COMMIT TRANSACTION;
