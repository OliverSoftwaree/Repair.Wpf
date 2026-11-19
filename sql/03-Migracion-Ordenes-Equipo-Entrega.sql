USE [RepairAcademico];
GO

IF OBJECT_ID(N'dbo.Ordenes', N'U') IS NULL
BEGIN
	THROW 50001, N'No existe dbo.Ordenes. Ejecute primero el script de creación del esquema.', 1;
END;
IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
BEGIN
	THROW 50002, N'No existe dbo.Equipos. Ejecute primero el script de creación del esquema.', 1;
END;
GO

SET XACT_ABORT ON;
BEGIN TRY
	BEGIN TRANSACTION;

	IF COL_LENGTH(N'dbo.Ordenes', N'EquipoId') IS NULL
		ALTER TABLE dbo.Ordenes ADD EquipoId int NULL;

	IF COL_LENGTH(N'dbo.Ordenes', N'EntregadaUtc') IS NULL
		ALTER TABLE dbo.Ordenes ADD EntregadaUtc datetime2(7) NULL;

	IF NOT EXISTS (
		SELECT 1
		FROM sys.foreign_keys
		WHERE parent_object_id = OBJECT_ID(N'dbo.Ordenes')
		  AND name = N'FK_Ordenes_Equipos'
	)
		ALTER TABLE dbo.Ordenes WITH CHECK
			ADD CONSTRAINT FK_Ordenes_Equipos FOREIGN KEY (EquipoId) REFERENCES dbo.Equipos(Id);

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	THROW;
END CATCH;
GO
