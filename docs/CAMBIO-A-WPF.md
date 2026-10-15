# Cambio autorizado de interfaz — 15/10/2026

El usuario eligió WPF después de revisar las alternativas a Windows Forms. Pidió expresamente una aplicación de escritorio con apariencia moderna, diseño adaptable y animaciones, conservando la esencia de Repair. Esta instrucción actualiza la decisión anterior de usar WinForms; no cambia el alcance funcional del primer avance.

| Aspecto | v0.1 Windows Forms | v0.2 WPF |
| --- | --- | --- |
| Interfaz | Formularios y controles estándar, con `.Designer.cs` y `.resx`. | Ventana XAML, recursos de tema, navegación y panel lateral. |
| Plataforma | C# / .NET Framework 4.8 / VS2022. | Se conserva C# / .NET Framework 4.8 / VS2022. |
| Diseño | Colocación de controles WinForms. | Contenedores adaptables, tabla/tarjetas, iconos vectoriales, estilo propio y transiciones de presentación. |
| Estructura | Lógica y datos en la aplicación de escritorio. | Núcleo separado en `Repair.Core`, interfaz `Repair.Wpf`, comprobaciones independientes. |
| Clientes | Alta, listado, búsqueda, ficha y validaciones. | Se conserva el alcance y se reconstruyen las interacciones en WPF. |
| SQL Server | Destino previsto, sin conexión comprobada. | Se conserva el esquema/acceso. Persiste la limitación de conexión; no se migraron bases. |
| Equipos | Contrato con propietario WinForms. | Contrato con `System.Windows.Window`; desarrollo sigue reservado como propuesta para Oliver. |
| Pruebas | 22 de lógica y observación parcial de WinForms. | 28 de lógica/presentación ejecutadas sobre la nueva solución y 15 de controles WPF con 12 vistas renderizadas. No equivalen a persistencia ni FPS comprobados. |

La carpeta y el ZIP de v0.1 se conservaron. El prototipo web anterior y el expediente documental permanecen intactos. No se trasladaron EF Core 10, ASP.NET Core Identity ni tecnologías incompatibles a .NET Framework 4.8.

Este registro se entrega con el código para que Josias pueda llevarlo al chat «Proyecto integrador I» y actualizar después informe/diapositivas. No se editó el expediente desde aquí ni se enviaron mensajes a ese chat.

Continúan pendientes equipos, órdenes, diagnóstico/seguimiento, entrega, reportes, seguridad completa y respaldo. Facturación, cobros e inventario de repuestos siguen excluidos. No se acredita un porcentaje de avance ni una aprobación académica. El inicio del Gantt del 15/09/2026 es planificado; la fecha límite registrada sigue siendo 06/12/2026.
