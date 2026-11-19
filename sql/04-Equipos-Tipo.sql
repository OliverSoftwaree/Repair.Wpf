-- Ampliación exclusiva del tipo de equipo. Ejecutar en la base de desarrollo de Repair.
-- No crea Equipos, no sustituye el script pendiente de Oliver y no modifica filas.
SET XACT_ABORT ON;
IF DB_NAME() NOT IN (N'RepairAcademico', N'RepairClientesDesarrollo_20261030')
    THROW 51000, 'Selecciona primero la base de desarrollo exclusiva de Repair.', 1;
IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
    THROW 51001, 'Falta dbo.Equipos. Obtener y revisar primero el esquema de Oliver.', 1;

BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.Equipos', N'Tipo') IS NULL
    ALTER TABLE dbo.Equipos ADD Tipo nvarchar(60) NULL;
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Equipos') AND name = N'Tipo'
      AND TYPE_NAME(user_type_id) = N'nvarchar' AND max_length = 120)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51002, 'La columna Tipo existente no es nvarchar(60). No se alteraron datos.', 1;
END;
COMMIT TRANSACTION;
-- Las filas anteriores permanecen NULL y se muestran como Sin especificar.
-- La aplicación exige seleccionar una opción válida en altas y actualizaciones.
