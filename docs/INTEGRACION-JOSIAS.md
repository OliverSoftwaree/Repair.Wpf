# Repair · Integración local de Josias

Esta copia agrega **Seguimiento** al menú de la aplicación WPF que ya conserva Clientes, Equipos, Tipo y CSV. Usa la rama local `desarrollo/josias-seguimiento-20261116` sobre `bbc67c7` y reutiliza la consulta de Seguimiento probada en `571c7a1`. El inicio de revisión que abría dos compilaciones queda disponible en su rama publicada para revisar los avances anteriores.

## Probar el paquete

1. Ejecuta `Repair.Wpf.exe` junto a `Repair.Core.dll` y `Repair.Wpf.exe.config`. El paquete arranca en demostración.
2. Desde el menú lateral, abre **Seguimiento**. Busca `1001` para ver una orden ficticia entregada y `1003` para ver una orden histórica sin vínculo de equipo.
3. Vuelve a **Clientes** o **Equipos** desde el mismo menú. El exportador CSV y el campo Tipo quedan en sus recorridos habituales.

Para compilar, abre `Repair.Wpf.sln` en Visual Studio 2022 con **Desarrollo de escritorio de .NET** y el paquete de destino de **.NET Framework 4.8**. En Release, los tres archivos del paquete se generan en `Repair.Wpf/bin/Release`. Este paquete local sirve para revisión y preparación de la instalación.

La configuración de ejemplo apunta a `RepairSeguimientoDesarrollo_20261115` con autenticación de Windows. Para consultar esa base aislada, configura `Modo=Sql` y `Data Source` en una copia de la configuración de ejecución. La copia SQL que se usó para la prueba local abre las órdenes ficticias `1` y `2`. Este incremento usa el esquema de pruebas existente y se limita a consultarlo.

## Punto de unión con Órdenes y Diagnóstico

- `App.xaml.cs` elige la implementación de `IConsultaSeguimiento` y la entrega a `MainWindow`. El menú abre `SeguimientoControl` dentro del `PageHost` de Repair. Al unir el trabajo de ambos, conservar el botón y la página de Seguimiento junto a la navegación de Diagnóstico.
- `IConsultaSeguimiento.BuscarPorIdAsync(int id)` es el contrato de lectura que recibe la vista. Devuelve `OrdenSeguimiento` con `Id`, `ClienteId`, `EquipoId`, `Estado`, `EntregadaUtc`, diagnóstico y observaciones, junto a los textos de cliente y equipo. Si no hay vínculo histórico fiable se conserva `EquipoId` nulo y la pantalla muestra «Sin equipo vinculado». La consulta SQL también comprueba que el equipo relacionado pertenezca al mismo cliente.
- Actualmente `ConsultaSeguimientoSql` consulta `dbo.Ordenes` por `Id`. Cuando se acuerde el contrato compartido, adaptar el repositorio común a `IConsultaSeguimiento` y sustituir la implementación inyectada en `App.xaml.cs`. Si cambia la representación SQL del diagnóstico, el ajuste queda en ese adaptador. La vista consume el mismo contrato.
- Las transiciones probadas siguen en el verificador aislado de `571c7a1`: **En diagnóstico → En reparación → Lista para entrega → Entregada**. La actualización compara el estado anterior, bloquea saltos, rechaza escrituras desactualizadas y conserva la primera fecha de entrega. Seguimiento dentro de Repair expone solo consulta.

Para abrir la escritura en la versión final, Oliver debe confirmar el contrato y completar los ajustes de Órdenes, Diagnóstico y el alta. La prueba conjunta debe comprobar que guardar diagnóstico conserva un estado avanzado y que una entrega cerrada permite un nuevo ingreso del mismo equipo. También debe cubrir doble entrega, reingreso concurrente y recuperación de la primera fecha desde otra sesión antes de habilitar acciones de avance.

## Verificación

- Visual Studio 2022 compiló Debug y Release. En Release pasaron 13 comprobaciones de Seguimiento integrado, 44 de Tipo y 18 de CSV. Los renderizados de Seguimiento se hicieron a 760 y 1380 píxeles con datos ficticios.
- Desde la ventana de Repair se consultó SQL Server real en la base aislada. La orden 1 mostró «Entregada», su diagnóstico, el equipo vinculado y la fecha. La orden 2 mostró «En diagnóstico» y «Sin equipo vinculado». Ambos casos fueron de solo lectura.
