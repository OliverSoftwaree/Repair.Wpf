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
GO

-- Datos de prueba opcionales
IF NOT EXISTS (SELECT 1 FROM dbo.Equipos)
BEGIN
	INSERT INTO dbo.Equipos (ClienteId, Marca, Modelo, Serie, Accesorios, Condicion)
	VALUES
		(1, N'Dell', N'Latitude 5400', N'SN-001', N'Cargador', N'Pantalla rota y batería con desgaste'),
		(1, N'HP', N'EliteBook 840', N'SN-002', N'Maleta', N'No enciende y hace ruido');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Ordenes)
BEGIN
	INSERT INTO dbo.Ordenes (ClienteId, Estado, Diagnostico, Observaciones)
	VALUES
		(1, N'Pendiente', N'', N'Orden de prueba para diagnóstico'),
		(1, N'En diagnóstico', N'Pantalla y batería revisadas', N'Pendiente de piezas');
END;
GO
