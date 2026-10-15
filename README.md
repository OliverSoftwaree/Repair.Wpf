# Repair · escritorio WPF

**v0.2 · 15/10/2026 · C# / WPF / .NET Framework 4.8 / Visual Studio 2022.**

Repair es el proyecto académico del Grupo B para gestionar un taller de reparación de equipos de cómputo. Esta versión cambia la interfaz de Windows Forms a **WPF**, por decisión del usuario, con una identidad visual basada en grafito, acero, superficies claras y naranja. El avance sigue limitado a **inicio y clientes**. Equipos queda reservado como propuesta de trabajo para Oliver; la pantalla visible solo informa que el módulo está pendiente.

![Inicio de Repair con personas ficticias](docs/vistas/01-Inicio-1380.png)

La imagen es una vista de los controles reales renderizada por WPF con datos ficticios. No es una captura del escritorio ni prueba la persistencia de SQL Server. Más vistas: [directorio](docs/vistas/02-Clientes-1380.png), [ficha](docs/vistas/04-Ficha-1380.png), [ventana compacta](docs/vistas/06-Clientes-760.png), [alta compacta](docs/vistas/07-Alta-760.png).

## Abrir y ver el diseño

1. Extrae el ZIP completo en una carpeta propia.
2. Abre **Repair.Wpf.sln** en Visual Studio 2022.
3. Establece **Repair.Wpf** como proyecto de inicio.
4. Selecciona **Debug / Any CPU**, compila y pulsa **F5**.
5. El proyecto lleva el argumento de depuración **`--demo`** en Debug: inicia con tres personas ficticias y una banda visible de **DEMOSTRACIÓN**. Puedes registrar, buscar y consultar sin preparar una base. Todo se pierde al cerrar.
6. Si tu configuración de depuración sustituye ese argumento, escribe `--demo` en **Propiedades del proyecto → Depurar → Argumentos de la línea de comandos**. También puedes elegir explícitamente `Demostracion` en la clave `Modo` de `App.config` y volver a compilar.

No se necesitan paquetes NuGet ni bibliotecas visuales de terceros. Se utiliza .NET Framework 4.8 porque su Developer Pack y VS2022 están disponibles en el equipo. **WPF y .NET Framework son decisiones diferentes:** la nueva interfaz está hecha en XAML, conservando el entorno de compilación disponible.

Al ejecutar el programa directamente **sin argumentos**, con la configuración de ejemplo `Modo=Sql`, se utiliza SQL Server. Nunca se cambia automáticamente a memoria cuando una consulta falla. Release tampoco incorpora argumentos de demostración.

## Qué puedes hacer

- **Inicio:** entrar al directorio o registrar un cliente; el conteo procede de la consulta, sin cifras inventadas de equipos u órdenes.
- **Clientes:** alta, listado, búsqueda por nombre/teléfono/correo/código numérico y ficha de solo lectura. Nombre y teléfono son obligatorios; correo y notas son opcionales.
- **Panel lateral:** datos y errores junto a cada campo, cierre con Cancelar o Esc, acciones accesibles incluso con poca altura mediante desplazamiento.
- **Ventana adaptable:** barra lateral compacta, tarjetas en ventanas estrechas y tabla en ventanas amplias. El tamaño mínimo es 760 × 600 unidades lógicas WPF.
- **Movimiento:** transiciones breves entre páginas y al abrir/cerrar el panel, con desvanecimiento y desplazamiento suavizado. Respetan la preferencia de animaciones de Windows.
- **Teclado:** Ctrl+N abre el alta, Ctrl+F enfoca la búsqueda, Enter busca o abre una ficha seleccionada, Esc cierra el panel. El foco nativo y los atajos aún requieren comprobación manual.

Esta etapa no incluye edición/eliminación de clientes, equipos, órdenes, diagnóstico, reportes, login, roles ni respaldo. Facturación, cobros e inventario de repuestos permanecen fuera del alcance del proyecto.

## Conectar SQL Server

1. Confirma la instancia de desarrollo instalada. La conexión todavía **no se pudo comprobar** desde esta sesión restringida.
2. Revisa `sql/01-Clientes.sql`. Crea la base exclusiva **RepairAcademico** y `dbo.Clientes` si faltan; no migra tablas existentes ni inserta ejemplos. Si ese nombre ya corresponde a otro trabajo, acuerda otro nombre exclusivo y ajusta script/configuración antes de ejecutarlo.
3. Ejecuta el script manualmente en una instancia de desarrollo autorizada. Esta entrega no lo ejecutó ni creó bases.
4. Ajusta `Repair.Wpf/App.config`: `Data Source` a tu instancia, `Initial Catalog=RepairAcademico`, autenticación Windows. El ejemplo usa `.`; `.\SQLEXPRESS` solo corresponde si esa instancia existe.
5. Mantén `Modo=Sql`. Borra `--demo` de los argumentos al depurar para probar SQL, o ejecuta el programa compilado directamente sin argumentos.
6. Comprueba consulta exitosa, alta real, búsqueda/ficha y permanencia al cerrar/reabrir. No presentes la demostración como esa prueba.

El esquema de clientes conserva los campos de v0.1. No se altera ni se migra la base del prototipo web. No hay credenciales en el paquete; no adjuntes contraseñas ni bases personales al devolver cambios.

## Dónde trabajar

| Proyecto / archivo | Función |
| --- | --- |
| `Repair.Core` | Modelos, reglas y acceso SQL de clientes, separados de la interfaz. |
| `Repair.Wpf/MainWindow.xaml` | Composición visual: inicio, directorio, pantalla pendiente y panel lateral. Abrir con **Ver diseñador** o en el editor XAML. |
| `Repair.Wpf/MainWindow.xaml.cs` | Navegación, eventos, estados y animaciones; llama al servicio. |
| `Repair.Wpf/Temas/Repair.xaml` | Colores, estilos, botones, campos, tabla, barras e iconos vectoriales propios. |
| `Repair.Wpf/Presentacion` | Presentación de cada cliente y reglas de cambio de diseño según ancho. |
| `Repair.Wpf/Demostracion` | Repositorio temporal, elegido explícitamente para explorar el diseño. |
| `Repair.Wpf/Integracion/IModuloEquipos.cs` | Contrato WPF que implementará Oliver. |
| `Repair.Wpf/App.xaml.cs` | Inicio, selección explícita de datos y lugar para registrar equipos. |
| `Repair.Comprobaciones` | Comprobaciones de lógica, controles WPF y generación de vistas. |

En WPF la interfaz se describe en `.xaml` y el comportamiento en `.xaml.cs`. Los `.Designer.cs` y `.resx` del avance Windows Forms no se trasladan a esta solución. La apertura visual del diseñador de VS2022 no se consiguió comprobar desde aquí; el XAML sí se compiló y renderizó con el motor WPF.

## Pruebas y documentación

La compilación Release terminó con **0 errores y 0 advertencias**. Pasaron **28 comprobaciones de lógica/presentación** y **15 comprobaciones dentro del proceso sobre los controles WPF**, generando **12 vistas** a distintos tamaños y estados. Las imágenes se revisaron para corregir recortes, tamaños de campos y distribución.

La fluidez real, el foco/teclado y el comportamiento de la ventana con ratón y varios monitores **no están comprobados**. Las pruebas visuales usan renderizado por software y animaciones desactivadas para producir imágenes estables; no miden FPS ni representan una sesión completa de uso nativo. SQL Server tampoco está verificado.

Después de compilar, desde PowerShell en la carpeta de la solución:

```powershell
# Lógica y reglas de presentación (28 casos)
& '.\Repair.Comprobaciones\bin\Debug\Repair.Comprobaciones.exe'

# Controles WPF y vistas; no abre ventanas en el escritorio (15 casos)
& '.\Repair.Comprobaciones\bin\Debug\Repair.Comprobaciones.exe' --visual '.\vistas-de-prueba'

# Sondeo SQL de solo lectura; no crea bases
& '.\Repair.Comprobaciones\bin\Debug\Repair.Comprobaciones.exe' --sql-probe
```

El código de salida de las comprobaciones es 0 si pasan y 1 si fallan. El sondeo devuelve 1 si no se verificó ninguna conexión.

- [Diseño y arquitectura](docs/DISENO-Y-ARQUITECTURA.md).
- [Registro de pruebas y límites](docs/PRUEBAS.md).
- [Guía de trabajo y devolución para Oliver](docs/GUIA-OLIVER.md).
- [Registro del cambio tecnológico](docs/CAMBIO-A-WPF.md).

La versión anterior WinForms se conserva como historial. Esta entrega no usa sus resultados de interfaz como pruebas de WPF, ni las 36 comprobaciones del prototipo web como evidencia nueva. No acredita un porcentaje académico ni una aprobación docente. Josias compartirá el ZIP con Oliver por chat; no se envió nada mediante herramientas.
