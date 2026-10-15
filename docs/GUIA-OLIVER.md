# Oliver: tu módulo de equipos ahora se integra en WPF

**Base de entrega: Repair.Wpf v0.2 · 15/10/2026.** Grupo B: Oliver Ubri Veloz MT-2024-00269 y Josias De La Cruz Baez SD-2024-00033. UFHEC, Proyecto Integrador I, ING 4393; docente César Moya U.

Josias eligió cambiar de Windows Forms a WPF para tener más libertad de diseño. La propuesta de reparto se mantiene: clientes como base del avance y equipos para Oliver. **Josias debe coordinar este reparto contigo; no se afirma que ya lo aceptaste.** No continúes la interfaz WinForms de v0.1 ni el prototipo web si vas a integrar sobre este paquete.

## Qué recibes

Una solución nueva con tres proyectos: `Repair.Core` (datos y reglas), `Repair.Wpf` (pantallas) y `Repair.Comprobaciones` (pruebas). Ya están programados inicio y clientes: alta, listado, búsqueda, validaciones y consulta de ficha. La interfaz usa menú lateral, tabla/tarjetas según tamaño y panel para alta/ficha. `Temas/Repair.xaml` concentra la identidad visual, los iconos vectoriales y los controles.

El esquema solo define clientes. La aplicación accede a SQL Server mediante consultas parametrizadas, pero **la conexión y la persistencia real no se pudieron comprobar**. Hay una demostración explícita con tres personas ficticias, sin archivos ni base; sus datos desaparecen al cerrar. Nunca es una alternativa automática al fallar SQL.

Compilación Release: 0 errores, 0 advertencias. Se ejecutaron 28 comprobaciones de lógica/presentación y 15 de controles WPF dentro del proceso. Se produjeron 12 vistas estáticas con el renderizador WPF. No se comprobó el diseñador de VS, el recorrido nativo con ratón/teclado ni los FPS de las animaciones. Consulta `PRUEBAS.md` antes de citar resultados.

**No hay modelo operativo, formulario, servicio, repositorio ni tabla de equipos.** `EquipoContrato` solo describe los datos propuestos. La pantalla de equipos únicamente informa que el módulo está pendiente.

## Primeros pasos

1. Extrae el ZIP y abre `Repair.Wpf.sln` en Visual Studio 2022.
2. Establece `Repair.Wpf` como inicio y selecciona Debug/Any CPU. Compila y pulsa F5. Debug propone el argumento `--demo`; verifica la banda de demostración.
3. Registra y busca un cliente ficticio para conocer el recorrido. Cierra y abre para confirmar que esa demostración es temporal.
4. Lee `MainWindow.xaml`, `MainWindow.xaml.cs` y `Temas/Repair.xaml`. En WPF se diseñan ventanas con **XAML y archivos asociados `.xaml.cs`**. No se usan los `.Designer.cs`/`.resx` de WinForms para estas pantallas.
5. Identifica `ClienteServicio` en `Repair.Core`: el formulario pide operaciones, el servicio valida y el repositorio consulta/guarda. Tu módulo seguirá esa separación.
6. Coordina campos, longitudes y criterio de relación antes de cambiar el contrato. No agregues órdenes o diagnóstico para completar equipos.

## Tu trabajo exacto propuesto

- Crear `EquiposWindow.xaml` con su `.xaml.cs`, utilizando los recursos visuales compartidos.
- Seleccionar un cliente existente; mostrar su nombre/código y guardar su ID.
- Registrar marca, modelo, serie, accesorios y condición de recepción.
- Listar y consultar equipos, mostrando claramente a qué cliente pertenecen.
- Validar campos y verificar que el cliente todavía existe. Si no hay clientes, orientar a registrar uno; si la consulta falla, mostrar el error sin inventar una lista vacía exitosa.
- Implementar lógica y acceso SQL propios de equipos, con parámetros.
- Entregar `sql/02-Equipos.sql` con `ClienteId NOT NULL` y clave foránea a `dbo.Clientes(Id)`, sin borrado en cascada.
- Implementar `IModuloEquipos` y registrarlo en el inicio de WPF.
- Añadir comprobaciones del módulo y una explicación de los resultados reales.

No incluye órdenes, asignación de técnicos, diagnóstico, cambios de estado, entrega, reportes, usuarios ni respaldos. Facturación, cobros e inventario siguen fuera del proyecto. No hace falta reescribir clientes.

## Contrato de datos propuesto

Se encuentra en `Repair.Core/Integracion/EquipoContrato.cs`. Estas reglas se proponen para tu implementación; no están resueltas por esta entrega.

| Propiedad | Criterio propuesto |
| --- | --- |
| `Id` | Entero generado en SQL. |
| `ClienteId` | Obligatorio, cliente existente, protegido también con clave foránea. |
| `Marca` | Obligatoria, hasta 60 caracteres. |
| `Modelo` | Obligatorio, hasta 100. |
| `Serie` | Opcional, hasta 100. No se acordó aún unicidad. |
| `Accesorios` | Opcionales, hasta 300. |
| `Condicion` | Obligatoria, hasta 500; describe el estado físico al recibir. |
| `CreadoUtc` | Fecha UTC generada en SQL. |

Para consultar clientes recibirás `Repair.Core.Integracion.IConsultaClientes`:

```csharp
Task<IReadOnlyList<Cliente>> BuscarAsync(string texto);
Task<Cliente> ObtenerAsync(int id);
```

`BuscarAsync("")` devuelve todos los clientes de la consulta. `ObtenerAsync(id)` devuelve `null` si no existe, o un error si no se pudo consultar. Son estados diferentes. El selector debe conservar el ID, no depender del texto que se escriba como nombre. Los códigos visuales `CL-0004` representan el ID `4`.

El punto de entrada cambió a **WPF** y está en `Repair.Wpf/Integracion/IModuloEquipos.cs`:

```csharp
void Abrir(System.Windows.Window propietario, IConsultaClientes clientes);
```

Tu implementación creará `EquiposWindow`, asignará `Owner = propietario`, le pasará la consulta de clientes y tu servicio de equipos, y abrirá la ventana. Regístrala en el tercer argumento de `new MainWindow(...)` en `App.xaml.cs`, donde actualmente hay `null`. El menú ya usa ese contrato cuando recibe una implementación; no necesitas sustituir la navegación de clientes.

Los modelos y servicios de `Repair.Core` no deben referenciar WPF ni WinForms. La ventana y el adaptador de integración sí pertenecen a `Repair.Wpf`.

## Archivos que puedes agregar/modificar

| Lugar | Trabajo |
| --- | --- |
| `Repair.Wpf/EquiposWindow.xaml` y `.xaml.cs` | Agregar ventana y comportamiento; usar **Agregar → Ventana (WPF)**. |
| `Repair.Core/Logica/EquipoServicio.cs` | Agregar reglas de equipos. |
| `Repair.Core/Datos/IEquipoRepositorio.cs`, `EquipoSqlRepositorio.cs` | Agregar contrato y consultas SQL. |
| `Repair.Wpf/Integracion` | Agregar implementación de `IModuloEquipos`. |
| `Repair.Wpf/App.xaml.cs` | Registrar tu módulo. |
| `sql/02-Equipos.sql` | Agregar tabla/relación sin cambiar bases de otros proyectos. |
| Archivos `.csproj` | Incluir tus archivos y referencias; VS normalmente lo hace al agregarlos. |
| `Repair.Comprobaciones` | Agregar casos de equipos sin borrar los actuales. |
| `docs/ENTREGA-OLIVER.md` | Resumen, cambios, pruebas, limitaciones y dudas. |

Conserva los servicios y datos de clientes, la composición de inicio/clientes y `sql/01-Clientes.sql`. Reutiliza el tema; coordina cualquier cambio global en `Temas/Repair.xaml`. Si encuentras un error en lo recibido, descríbelo y acuerda la corrección. No envíes una cadena privada en `App.config`.

## Criterios de aceptación

1. La solución abre y compila en VS2022 con .NET Framework 4.8. Tus ventanas abren en el diseñador y llevan `.xaml`/`.xaml.cs` incluidos en el proyecto.
2. El menú abre equipos por `IModuloEquipos` y clientes mantiene su funcionamiento.
3. El diseño utiliza los estilos comunes, admite ventanas de poca altura mediante desplazamiento y mantiene las acciones visibles. No cortes campos al estrechar la ventana.
4. Con un cliente válido se puede registrar, listar y consultar un equipo y su cliente correcto.
5. Cliente inexistente y campos inválidos se rechazan. La clave foránea protege también las escrituras externas al formulario.
6. Falta de clientes, error de consulta y ausencia de resultados tienen mensajes distintos. Un fallo al guardar no se muestra como éxito ni descarta silenciosamente lo escrito.
7. Se comprueban obligatorios, límites, opcionales y relación; se usan parámetros SQL.
8. Con SQL disponible, los registros permanecen al reiniciar. Si no lo puedes probar, lo declaras pendiente con instrucciones para reproducirlo.
9. Se revisan navegación con teclado, tamaños y transiciones en una ventana real. No atribuyas FPS ni resultados nativos a los PNG de pruebas.
10. Las 28 comprobaciones y las 15 de controles de la base siguen pasando, además de tus casos. Documenta cualquier ajuste necesario.

## Devolver por ZIP

1. Guarda los archivos y prepara una **copia de entrega**, conservando tu trabajo original.
2. Incluye solución completa, proyectos actualizados, ventanas, tema si hubo cambios acordados, esquema y documentación.
3. Excluye `.vs`, `bin`, `obj`, `*.user`, `*.suo`, `*.mdf`, `*.ldf`, `*.bak`, paquetes, instaladores, herramientas, contraseñas y datos personales.
4. Abre y compila la copia limpia si tu entorno lo permite. Anota el resultado real y el estado de SQL.
5. Comprime como `Repair-WPF-v0.3-Oliver-equipos-AAAAMMDD.zip`.
6. Devuélvelo por el chat acordado con Josias. Incluye: base usada (`WPF v0.2`), funciones terminadas, archivos agregados/modificados, resultados de compilación/pruebas, SQL comprobado o pendiente y dudas.
7. Josias adjuntará el ZIP a este chat local de código para revisar e integrar. No se envió nada a Oliver ni al chat documental mediante herramientas.

Ambos deben poder explicar el recorrido completo de cliente → equipo. Los resultados se registran como hechos, sin inventar pruebas, aceptación del reparto o aprobación docente.
