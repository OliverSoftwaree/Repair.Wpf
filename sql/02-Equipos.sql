-- Ejecutar en RepairClientesDesarrollo_20261030, preparada con 03-Clientes-Desarrollo.sql.
-- Instalación aditiva: conserva filas, no borra ni reinicializa tablas ni identidades.
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() <> N'RepairClientesDesarrollo_20261030'
    THROW 51000, 'Selecciona la base de desarrollo exclusiva de Repair.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0
    AND name=N'RepairAvance' AND CONVERT(nvarchar(100),value)=N'Clientes-20261030')
    THROW 51001, 'La base no posee la marca de este proyecto. No se modifico.', 1;
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
    THROW 51002, 'Falta Clientes. Preparar primero la base del proyecto.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Equipos
        (
            Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipos PRIMARY KEY,
            ClienteId int NOT NULL,
            Tipo nvarchar(60) NULL,
            Marca nvarchar(60) NOT NULL,
            Modelo nvarchar(100) NOT NULL,
            Serie nvarchar(100) NULL,
            Accesorios nvarchar(300) NULL,
            Condicion nvarchar(500) NOT NULL,
            CreadoUtc datetime2(0) NOT NULL CONSTRAINT DF_Equipos_CreadoUtc DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CK_Equipos_Marca CHECK (LEN(LTRIM(RTRIM(Marca))) >= 1),
            CONSTRAINT CK_Equipos_Modelo CHECK (LEN(LTRIM(RTRIM(Modelo))) >= 1),
            CONSTRAINT CK_Equipos_Condicion CHECK (LEN(LTRIM(RTRIM(Condicion))) >= 5)
        );
    END;
    ELSE IF COL_LENGTH(N'dbo.Equipos', N'Tipo') IS NULL
        ALTER TABLE dbo.Equipos ADD Tipo nvarchar(60) NULL;

    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Equipos') AND
        ((name=N'Id' AND TYPE_NAME(user_type_id)=N'int' AND is_identity=1 AND is_nullable=0)
        OR (name=N'ClienteId' AND TYPE_NAME(user_type_id)=N'int' AND is_nullable=0)
        OR (name=N'Tipo' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=120)
        OR (name=N'Marca' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=120 AND is_nullable=0)
        OR (name=N'Modelo' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=200 AND is_nullable=0)
        OR (name=N'Serie' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=200)
        OR (name=N'Accesorios' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=600)
        OR (name=N'Condicion' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=1000 AND is_nullable=0)
        OR (name=N'CreadoUtc' AND TYPE_NAME(user_type_id)=N'datetime2' AND scale=0 AND default_object_id<>0 AND is_nullable=0))) <> 9
        THROW 51003, 'Equipos existente no coincide con el contrato. Revisar sin borrar datos.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id
        WHERE fk.parent_object_id=OBJECT_ID(N'dbo.Equipos')
          AND fk.referenced_object_id=OBJECT_ID(N'dbo.Clientes')
          AND COL_NAME(fc.parent_object_id,fc.parent_column_id)=N'ClienteId'
          AND COL_NAME(fc.referenced_object_id,fc.referenced_column_id)=N'Id')
        ALTER TABLE dbo.Equipos WITH CHECK ADD CONSTRAINT FK_Equipos_Clientes
            FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id);
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.Equipos')
        AND referenced_object_id=OBJECT_ID(N'dbo.Clientes') AND (delete_referential_action<>0 OR is_disabled=1 OR is_not_trusted=1))
        THROW 51004, 'La relacion existente no es segura para este avance. Revisar sin modificar filas.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Equipos')
        AND name=N'UX_Equipos_ClienteMarcaModeloSerie')
        CREATE UNIQUE INDEX UX_Equipos_ClienteMarcaModeloSerie
            ON dbo.Equipos(ClienteId,Marca,Modelo,Serie);
    -- Si había datos incompatibles o duplicados, SQL detiene y revierte esta preparación.
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
