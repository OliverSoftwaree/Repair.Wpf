-- Ejecutar completo en una base exclusiva marcada por Preparar-Repair.exe.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() NOT IN (N'RepairEntregaDesarrollo_20261119',N'RepairEntrega')
    THROW 51100,'Selecciona una base exclusiva de la entrega Repair.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RepairProducto' AND CONVERT(nvarchar(100),value)=N'Repair-Entrega')
    THROW 51101,'La base no pertenece a esta entrega de Repair.',1;
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource=N'Repair-Migracion-RC1',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @Lock<0 THROW 51102,'Otra sesión está preparando el esquema.',1;

IF OBJECT_ID(N'dbo.Clientes',N'U') IS NULL
CREATE TABLE dbo.Clientes(
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
 Nombre nvarchar(100) NOT NULL, Telefono nvarchar(25) NOT NULL, Correo nvarchar(150) NULL, Notas nvarchar(300) NULL,
 CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Clientes_CreadoUtc DEFAULT SYSUTCDATETIME());

IF OBJECT_ID(N'dbo.Equipos',N'U') IS NULL
CREATE TABLE dbo.Equipos(
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipos PRIMARY KEY,
 ClienteId int NOT NULL CONSTRAINT FK_Equipos_Clientes REFERENCES dbo.Clientes(Id),
 Tipo nvarchar(60) NULL, Marca nvarchar(60) NOT NULL, Modelo nvarchar(100) NOT NULL, Serie nvarchar(100) NULL,
 Accesorios nvarchar(300) NULL, Condicion nvarchar(500) NOT NULL,
 CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Equipos_CreadoUtc DEFAULT SYSUTCDATETIME());
IF COL_LENGTH(N'dbo.Equipos',N'Tipo') IS NULL ALTER TABLE dbo.Equipos ADD Tipo nvarchar(60) NULL;

IF OBJECT_ID(N'dbo.Ordenes',N'U') IS NULL
CREATE TABLE dbo.Ordenes(
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ordenes PRIMARY KEY,
 ClienteId int NOT NULL CONSTRAINT FK_Ordenes_Clientes REFERENCES dbo.Clientes(Id),
 Estado nvarchar(50) NOT NULL CONSTRAINT DF_Ordenes_Estado DEFAULT N'Pendiente',
 Diagnostico nvarchar(max) NULL, Observaciones nvarchar(max) NULL,
 CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Ordenes_CreadoUtc DEFAULT SYSUTCDATETIME());
IF COL_LENGTH(N'dbo.Ordenes',N'EquipoId') IS NULL
 ALTER TABLE dbo.Ordenes ADD EquipoId int NULL CONSTRAINT FK_Ordenes_Equipos REFERENCES dbo.Equipos(Id);
IF COL_LENGTH(N'dbo.Ordenes',N'EntregadaUtc') IS NULL ALTER TABLE dbo.Ordenes ADD EntregadaUtc datetime2(7) NULL;

IF OBJECT_ID(N'dbo.Usuarios',N'U') IS NULL
CREATE TABLE dbo.Usuarios(
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
 Usuario nvarchar(60) NOT NULL CONSTRAINT UQ_Usuarios_Usuario UNIQUE,
 Rol nvarchar(30) NOT NULL CONSTRAINT CK_Usuarios_Rol CHECK(Rol IN(N'Administrador',N'Recepcionista',N'Técnico')),
 Activo bit NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT 1,
 Sal varbinary(16) NOT NULL, Hash varbinary(32) NOT NULL,
 Iteraciones int NOT NULL CONSTRAINT CK_Usuarios_Iteraciones CHECK(Iteraciones BETWEEN 100000 AND 1000000));
COMMIT;
GO
-- Segundo lote permite compilar después de crear las columnas aditivas.
SET XACT_ABORT ON;
IF DB_NAME() NOT IN (N'RepairEntregaDesarrollo_20261119',N'RepairEntrega')
    THROW 51100,'Selecciona una base exclusiva de la entrega Repair.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RepairProducto' AND CONVERT(nvarchar(100),value)=N'Repair-Entrega')
    THROW 51101,'La base no pertenece a esta entrega de Repair.',1;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Equipos') AND name=N'UX_Equipos_Identidad')
 CREATE UNIQUE INDEX UX_Equipos_Identidad ON dbo.Equipos(ClienteId,Marca,Modelo,Serie);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Ordenes') AND name=N'UX_Ordenes_EquipoActivo')
 CREATE UNIQUE INDEX UX_Ordenes_EquipoActivo ON dbo.Ordenes(EquipoId) WHERE EquipoId IS NOT NULL AND Estado<>N'Entregada';
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Ordenes') AND name=N'CK_Ordenes_Entrega')
 ALTER TABLE dbo.Ordenes WITH CHECK ADD CONSTRAINT CK_Ordenes_Entrega
 CHECK((Estado=N'Entregada') OR EntregadaUtc IS NULL);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Equipos') AND name=N'UQ_Equipos_IdCliente')
 CREATE UNIQUE INDEX UQ_Equipos_IdCliente ON dbo.Equipos(Id,ClienteId);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.Ordenes') AND name=N'FK_Ordenes_EquipoCliente')
 ALTER TABLE dbo.Ordenes WITH CHECK ADD CONSTRAINT FK_Ordenes_EquipoCliente
 FOREIGN KEY(EquipoId,ClienteId) REFERENCES dbo.Equipos(Id,ClienteId);
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'RepairVersion')
 EXEC sys.sp_addextendedproperty @name=N'RepairVersion',@value=N'1.0-RC1';
ELSE EXEC sys.sp_updateextendedproperty @name=N'RepairVersion',@value=N'1.0-RC1';
COMMIT;
