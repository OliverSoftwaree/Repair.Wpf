# Repair 1.0 RC1 · Registro de entrega

Integración realizada el 19 de noviembre de 2026 sobre el aporte local de Josias `0b90430` y el de Oliver `5f5b2c9`. El commit de integración conserva ambas ascendencias. La fecha de entrega académica indicada por Josias es el 6 de diciembre de 2026. Las fechas futuras de revisión y exposición corresponden al plan documental.

## Pantallas y acciones

| Pantalla | Acciones y consultas |
| --- | --- |
| Acceso | Usuario/contraseña, comprobar cuenta activa y rol, cambiar contraseña propia. |
| Inicio | Indicador de conexión, acceso al alta y directorio de clientes, navegación. |
| Clientes | Alta, búsqueda, consulta de ficha, listado adaptable y exportación CSV del resultado visible. |
| Equipos | Selección de cliente, Tipo predefinido, marca/modelo/serie/accesorios/condición, alta con orden automática, consulta, edición y eliminación sujetas a validación y relaciones. |
| Diagnóstico | Selección de cliente/orden o búsqueda por Id, diagnóstico, observaciones, severidad y acción recomendada en el texto persistido. El guardado conserva los estados avanzados. |
| Seguimiento y entrega | Búsqueda por Id, equipo/cliente/diagnóstico/estado/fecha, avance secuencial según rol, entrega y nuevo ingreso del equipo. |
| Reportes | Listado de órdenes por estado y fecha, consulta de entregas por fecha, exportación CSV e impresión. Las fechas visibles son locales. |

## Base de datos final

`RepairEntregaDesarrollo_20261119` corresponde a pruebas y demostración. `RepairEntrega` es el nombre admitido para una instalación nueva. Ambas requieren la marca de propiedad de Repair. El script no vincula órdenes históricas por aproximación.

| Tabla | Campos y tipos |
| --- | --- |
| Clientes | Id int identity PK, Nombre nvarchar(100) obligatorio, Telefono nvarchar(25) obligatorio, Correo nvarchar(150) nullable, Notas nvarchar(300) nullable, CreadoUtc datetime2(0) obligatorio con SYSUTCDATETIME(). |
| Equipos | Id int identity PK, ClienteId int FK obligatorio, Tipo nvarchar(60) nullable para compatibilidad histórica y requerido por el alta, Marca nvarchar(60), Modelo nvarchar(100), Serie nvarchar(100) nullable, Accesorios nvarchar(300) nullable, Condicion nvarchar(500), CreadoUtc datetime2(0). |
| Ordenes | Id int identity PK, ClienteId int FK obligatorio, EquipoId int FK nullable para históricos, Estado nvarchar(50) con Pendiente predeterminado, Diagnostico nvarchar(max) nullable, Observaciones nvarchar(max) nullable, CreadoUtc datetime2(0), EntregadaUtc datetime2(7) nullable. |
| Usuarios | Id int identity PK, Usuario nvarchar(60) UNIQUE obligatorio, Rol nvarchar(30) CHECK Administrador/Recepcionista/Técnico, Activo bit predeterminado 1, Sal varbinary(16), Hash varbinary(32), Iteraciones int entre 100000 y 1000000. |

Equipos referencia Clientes. Ordenes referencia Clientes, Equipos y la combinación EquipoId/ClienteId, que asegura la pertenencia del equipo al mismo cliente. No se configuró borrado en cascada. Un índice único identifica el equipo por cliente/marca/modelo/serie. Otro índice único filtrado admite una sola orden activa por EquipoId cuando el estado difiere de Entregada. Los estados avanzan comparando el valor vigente al actualizar, y la fecha solo se escribe en la primera entrega válida.

## Pruebas del 19/11/2026

- SQL Server real, base aislada, 26 comprobaciones aprobadas. Incluyen autenticación de los tres roles, contraseña incorrecta, cambio de contraseña, cuenta desactivada, permisos de escritura, alta de equipo/orden en transacción, ausencia de orden adicional al registrar cliente, diagnóstico, reparación/lista/entrega, estados antiguos, dos entregas concurrentes, fecha inicial conservada, guardado posterior del diagnóstico, reingresos concurrentes, reporte filtrado, actualización de esquema repetida y lectura desde un segundo proceso.
- Prueba nativa observada desde Repair. Acceso como Administrador, diagnóstico desde el formulario integrado de Oliver, avance de la orden ficticia hasta Entregada, recuperación de fecha, consulta del reporte, filtro Solo entregas y exportación CSV desde su diálogo. Se usaron exclusivamente registros ficticios.
- Las 13 comprobaciones de Seguimiento integrado y las 44 de Tipo utilizan proveedores ficticios/memoria. Las 18 de CSV comprueban exportación y controles. Esas comprobaciones acreditan reglas y renderizado de la interfaz, y se documentan separadas de la persistencia SQL.
- Capturas de las siete pantallas se generaron desde la vista WPF con datos ficticios para documentación. Los nombres y fechas ilustrativos se identifican en la carpeta Capturas. No representan aceptación de personas ni resultados de otra instalación.

La impresión ofrece el diálogo de Windows y la exportación CSV se verificó desde la interfaz. La impresión física, una instalación en una segunda computadora y la aceptación de los usuarios del taller siguen como validaciones pendientes de ejecución. El paquete portable y su preparación SQL quedan disponibles para realizarlas.

## Lecciones e integración

El PDF de avance y el código de la rama principal tenían esquemas distintos. Se tomó como punto de partida el código existente y se conservaron las pantallas y ambos historiales. La integración detectó dos altas de órdenes, una al registrar cliente y otra al recibir equipo. El alta quedó en la recepción del equipo y el reingreso usa esa misma ficha. También se corrigió la regresión del estado al guardar diagnóstico y se trasladó la comprobación del estado previo a una actualización SQL atómica.

La sesión de ejecución automatizada no podía inicializar el cliente SQL. La misma herramienta compilada ejecutada desde Windows comprobó el recorrido contra el motor. Los fallos de esa sesión no se usaron para declarar persistencia correcta. Las configuraciones publicadas usan un servidor genérico y las pruebas conservan sus datos ficticios en la base exclusiva.
