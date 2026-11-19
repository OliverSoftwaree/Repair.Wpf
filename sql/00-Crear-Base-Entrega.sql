-- Preparación manual alternativa. Ejecutar desde SSMS con autenticación Windows.
-- Este archivo elige la base de pruebas de la entrega. Para una instalación nueva
-- se puede usar RepairEntrega; nunca sustituirlo por una base personal existente.
USE [master];
SET NOCOUNT ON;
DECLARE @Base sysname=N'RepairEntregaDesarrollo_20261119';
IF @Base NOT IN(N'RepairEntregaDesarrollo_20261119',N'RepairEntrega') THROW 51110,'Nombre de base no permitido para esta entrega.',1;
IF DB_ID(@Base) IS NULL
BEGIN
 EXEC(N'CREATE DATABASE '+QUOTENAME(@Base));
 EXEC(N'USE '+QUOTENAME(@Base)+N'; EXEC sys.sp_addextendedproperty @name=N''RepairProducto'',@value=N''Repair-Entrega'';');
END;
DECLARE @Comprobar nvarchar(max)=N'USE '+QUOTENAME(@Base)+N';
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N''RepairProducto'' AND CONVERT(nvarchar(100),value)=N''Repair-Entrega'')
THROW 51111,''La base existente no pertenece a esta entrega.'',1;';
EXEC(@Comprobar);
-- Ahora seleccionar explícitamente esta base y ejecutar 05-Entrega-RC1.sql.
-- Preparar-Repair.exe instala además las cuentas ficticias de demostración.
