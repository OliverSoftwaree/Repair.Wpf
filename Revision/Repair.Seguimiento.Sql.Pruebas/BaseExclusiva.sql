SET NOCOUNT ON;
IF DB_NAME() <> N'master'
    THROW 51000, 'La preparación debe iniciar en master.', 1;

DECLARE @bloqueo int;
EXEC @bloqueo = sys.sp_getapplock @Resource = N'RepairSeguimientoDesarrollo_20261115',
    @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 10000;
IF @bloqueo < 0 THROW 51001, 'La base se está preparando en otra sesión.', 1;

IF DB_ID(N'RepairSeguimientoDesarrollo_20261115') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [RepairSeguimientoDesarrollo_20261115];');
    EXEC [RepairSeguimientoDesarrollo_20261115].sys.sp_addextendedproperty
        @name = N'RepairAvance', @value = N'Seguimiento-20261115';
END;

EXEC sys.sp_releaseapplock @Resource = N'RepairSeguimientoDesarrollo_20261115', @LockOwner = N'Session';
