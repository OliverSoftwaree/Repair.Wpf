# Repair 1.0 RC1

Aplicación académica de escritorio del Grupo B para recepción y reparación de equipos de cómputo. Integra Clientes, Equipos con Tipo, Órdenes y Diagnóstico, Seguimiento y Entrega, reportes y acceso por roles. Usa C#, WPF, .NET Framework 4.8, Visual Studio 2022 y SQL Server.

## Ejecutar el paquete portable

1. Extrae la carpeta completa en un lugar donde puedas escribir. Windows 10/11 necesita .NET Framework 4.8. Instala o utiliza una instancia de SQL Server y comprueba que tu usuario de Windows pueda conectarse. Para la preparación inicial necesita permiso para crear la base.
2. Abre `Repair.Wpf.exe.config` con un editor de texto. En `RepairSql`, ajusta `Data Source=.` a tu instancia. El punto usa la instancia predeterminada local. SQL Express normalmente usa un nombre como `.\SQLEXPRESS`. Conserva `Integrated Security=True`.
3. Mantén `Initial Catalog=RepairEntregaDesarrollo_20261119` para la demostración. Ejecuta `Preparar-Repair.exe` desde Windows. Instala o actualiza únicamente esa base, crea las cuentas ficticias si faltan y realiza la prueba conjunta. `Resultado-preparacion.txt` debe terminar con el resultado aprobado. Los registros ficticios se conservan para la presentación.
4. Ejecuta `Repair.Wpf.exe`, inicia sesión y usa el menú. Busca en Seguimiento las órdenes que indique el registro de preparación. Los números pueden cambiar al repetir la prueba.

Para una instalación nueva separada de la demostración, se admite `Initial Catalog=RepairEntrega`. Ejecuta `Preparar-Repair.exe --instalar` desde una consola de Windows abierta en la carpeta del paquete. Ese destino instala el esquema y las cuentas ficticias iniciales. Cambia las contraseñas de esas cuentas desde Acceso antes de utilizar información del taller. El programa de preparación conserva las contraseñas de las cuentas existentes.

Si la base ya existe sin la marca `RepairProducto=Repair-Entrega`, la preparación se detiene. La entrega usa su base exclusiva. El acceso a SQL depende de los permisos de Windows y los permisos de la aplicación dependen de la cuenta de Repair.

## Cuentas ficticias iniciales

| Usuario | Contraseña de demostración | Rol |
| --- | --- | --- |
| admin.demo | Repair-Admin-2026! | Administrador |
| recepcion.demo | Repair-Recepcion-2026! | Recepcionista |
| tecnico.demo | Repair-Tecnico-2026! | Técnico |

En Acceso, marca **Cambiar mi contraseña**, escribe la contraseña actual y una nueva de 12 a 200 caracteres, y pulsa Entrar. Las contraseñas se guardan como una derivación PBKDF2-SHA256 con sal aleatoria y 210000 iteraciones. Cierra y abre Repair para cambiar de usuario.

## Flujo de trabajo

Registra el cliente y después su equipo con Tipo, marca, modelo y condición. El ingreso crea una orden pendiente asociada al equipo en la misma transacción. En Diagnóstico, busca la orden, completa diagnóstico, observaciones, severidad y acción recomendada y guarda. En Seguimiento se pasa por **En diagnóstico → En reparación → Lista para entrega → Entregada**. La entrega guarda la fecha UTC y la muestra en hora local.

Guardar otro diagnóstico conserva el estado y la fecha de una entrega existente. Una vez entregada, **Registrar nuevo ingreso** abre una orden pendiente del mismo equipo o recupera la que ya está activa. Las órdenes antiguas sin relación fiable muestran **Sin equipo vinculado**. Sus transiciones quedan bloqueadas hasta resolver esa relación con información verificable.

Reportes permite filtrar por estado y rango de fechas. **Solo entregas** usa la fecha de entrega, y el reporte general usa la fecha de ingreso. La salida puede exportarse como CSV e imprimirse mediante el diálogo de Windows, incluida la impresora PDF si está instalada. El CSV de Clientes conserva el listado filtrado de esa pantalla.

## Roles

| Acción | Administrador | Recepcionista | Técnico |
| --- | --- | --- | --- |
| Consultar clientes, equipos, seguimiento y reportes | Sí | Sí | Sí |
| Registrar clientes y equipos, editar/eliminar equipos | Sí | Sí | Bloqueada |
| Guardar diagnóstico | Sí | Bloqueada | Sí |
| Iniciar reparación y marcar lista | Sí | Bloqueada | Sí |
| Entregar y registrar nuevo ingreso | Sí | Sí | Bloqueada |
| Cambiar su propia contraseña | Sí | Sí | Sí |

Los repositorios autorizados y el servicio de Seguimiento comprueban los permisos al escribir. Las cuentas de Windows con acceso directo de administración a SQL conservan la capacidad de administrar esa base.

## Compilar y comprobar

Abre `Repair.Wpf.sln` en Visual Studio 2022 con **Desarrollo de escritorio de .NET** y el paquete de destino .NET Framework 4.8. Compila Release. `Repair.Wpf/bin/Release` contiene `Repair.Wpf.exe`, su configuración, `Repair.Core.dll` y `Preparar-Repair.exe`. Esos cuatro archivos forman la aplicación portable.

Las pruebas locales se ejecutan con `Repair.Clientes.Verificacion.exe --seguimiento`, `--tipo` y `--csv`. La prueba SQL completa se ejecuta con el preparador en la base exclusiva de desarrollo. Los scripts `sql/00-Crear-Base-Entrega.sql` y `sql/05-Entrega-RC1.sql` ofrecen preparación manual y actualización aditiva idempotente. Los scripts de avances anteriores se conservan como historial y la instalación RC1 usa los dos indicados aquí.

Consulta `docs/ENTREGA-RC1.md` para el inventario de pantallas, el esquema y las pruebas de esta versión. El paquete documental reúne manual, presentación, planificación y diagramas.
