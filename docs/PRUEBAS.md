# Evidencia de WPF v0.2 — 15/10/2026

## Resultados reales

| Área | Resultado | Qué acredita |
| --- | --- | --- |
| Compilación | Debug durante construcción y Release del código final. Release: 0 errores y 0 advertencias. | Compilación de núcleo, WPF/XAML y comprobaciones con MSBuild de Visual Studio 2022. |
| Lógica y presentación | 28 correctas, 0 fallidas. | Reglas de clientes, coordinación con repositorio temporal, presentación y cambios de diseño por ancho. |
| Controles WPF | 15 correctas. | Eventos y estados de controles reales dentro del proceso de prueba, con repositorios temporales/fallos simulados. |
| Vistas | 12 PNG producidos por el renderizador WPF por software. Revisados visualmente. | Distribución y estados estáticos en tamaños de 1380, 1100, 900 y 760 de ancho. |
| Ventana nativa | Se lanzó una instancia de demostración y se encontró su ventana. Estaba minimizada; la restauración fue rechazada por el control de Windows al detectar actividad del usuario. | Lanzamiento identificado; no acredita el recorrido nativo de ratón/teclado ni su fluidez. |
| SQL Server | Sondeo final de solo lectura a `master` en `.`, `.\SQLEXPRESS` y `tcp:localhost,1433`: SqlClient no pudo inicializar sus contadores bajo AppContainer. Código 1. | Conexión NO comprobada. No demuestra que SQL Server esté ausente. |
| Esquema/persistencia | No se ejecutó el script ni se creó una base. No se comprobó alta/consulta contra SQL real. | Pendiente. |
| Diseñador VS2022 | No se consiguió validar su apertura visual en esta sesión. | XAML compilado y renderizado; diseñador pendiente. |
| Animación, foco y monitores | Las transiciones están programadas. No se midieron FPS ni se probaron foco, movimiento reducido, arrastre, maximización y escalas en escritorio real. | Pendiente de aceptación manual. |

Los resultados literales están en `evidencias/Logica-20261015.txt`, `evidencias/Vistas-WPF-20261015.txt` y `evidencias/Sondeo-SQL-20261015.txt`.

Para compilar desde la sesión restringida se especificaron las rutas locales del SDK de Windows y de las referencias de .NET Framework 4.8, porque el descubrimiento automático del SDK intentaba leer el Registro y recibía acceso denegado. No se modificó el sistema, y esas rutas de la máquina no están fijadas en los proyectos. La compilación desde la interfaz de Visual Studio queda por reproducir.

## Qué comprueba cada conjunto

Las primeras 22 comprobaciones de lógica se ejecutaron de nuevo sobre `Repair.Core`: alta normalizada, nombres inválidos y límites, teléfonos inválidos y límites, correo, notas, opcionales, caracteres acentuados/apóstrofos, lista vacía, búsqueda, ID inexistente y fallo de alta. Se comprueba que una entrada inválida no llegue al repositorio. Se añaden seis casos: disposición a 760; cambios a 1100/1180/1280; ancho del panel; formato de código/inicial/correo ausente; aislamiento de copias en demostración; alta/búsqueda/consulta en la demostración.

Las 15 comprobaciones WPF verifican:

1. El inicio refleja las tres personas ficticias de la consulta.
2. El directorio ancho muestra tabla y tres filas.
3. Las columnas aprovechan el ancho de la tabla.
4. Un alta inválida muestra errores y no agrega una fila.
5. El mensaje largo de teléfono se ajusta a varias líneas.
6. Guardar desde los controles agrega el cliente temporal y cierra el panel.
7. Buscar desde los controles encuentra ese registro.
8. El botón de ficha de la fila recupera el cliente correcto.
9. A 900 se muestran tarjetas y navegación compacta.
10. A 760 el panel cabe y mantiene disponible Guardar.
11. El inicio compacto apila sus tarjetas.
12. Una búsqueda sin resultados presenta su estado vacío explícito.
13. Equipos abre solo la pantalla de módulo pendiente.
14. Un fallo simulado de datos no aparece como un directorio vacío exitoso.
15. Un guardado fallido conserva el editor y muestra que no hay confirmación.

`PruebasVisuales` construye la aplicación y las vistas sin mostrar una ventana en el escritorio. Desactiva animaciones para las imágenes, aplica medidas/distribución, ejecuta eventos de los controles y usa `RenderTargetBitmap`. No simula entradas de ratón del sistema, no valida el foco de Windows y no mide el rendimiento del renderizador de una ventana visible. El estado de conexión de las imágenes 11 y 12 procede de un doble que falla; no es una conexión SQL real.

Durante esta revisión se corrigieron el fondo del contenedor al renderizar, el espacio del menú compacto, el alto de los campos, los mensajes largos, la distribución de columnas y la superposición de la confirmación al abrir otro panel. Los PNG finales corresponden a la fuente entregada.

## Aceptación manual pendiente

1. Abrir la solución y `MainWindow.xaml` en VS2022; compilar y ejecutar con F5.
2. En modo demostración, recorrer inicio → clientes → alta → validaciones → guardar → búsqueda → ficha. Verificar Esc, Ctrl+N, Ctrl+F y tabulación; el foco debe quedar dentro del panel y volver al cerrarlo.
3. Redimensionar y maximizar/restaurar. Revisar ancho mínimo, poca altura, desplazamiento con rueda/barra y escalas 100/125/150 % si están disponibles. Probar varios monitores solo si se dispone de ellos.
4. Evaluar las transiciones en movimiento real y comprobar que al desactivar las animaciones desde Windows los paneles siguen apareciendo y cerrándose correctamente. Esta entrega no cambió ese ajuste.
5. Configurar la base exclusiva y comprobar consulta, alta, búsqueda (incluidos `%`, `_`, `[`, `~`, tildes y apóstrofos), ficha y permanencia al reiniciar.
6. Confirmar un fallo real de conexión sin éxito inventado ni sustitución automática de SQL por datos temporales.

Las 36 comprobaciones del prototipo web, las pruebas visuales de WinForms y esta evidencia WPF son conjuntos distintos. Esta entrega no acredita por sí sola un 30 % de avance ni una aprobación docente.
