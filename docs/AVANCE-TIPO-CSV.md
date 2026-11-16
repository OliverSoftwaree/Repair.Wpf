# Repair · Clientes, Equipos, Tipo y CSV

Esta rama conserva el código de Clientes y Equipos que se verificó en el avance local `22d94c6`, derivado de `d436c4d`. Incluye el tipo de equipo persistido en SQL Server, el ajuste visual de Equipos y la exportación del resultado actual del listado de Clientes a CSV. Las capturas, resultados de ejecución y datos de prueba locales quedan fuera de esta rama.

Abre `Repair.Wpf.sln` en Visual Studio 2022 con la carga de trabajo **Desarrollo de escritorio de .NET** y el paquete de destino de **.NET Framework 4.8**. Para usar esta compilación junto a Diagnóstico y Seguimiento, sigue `Revision/README.md` en la rama `revision/seguimiento-20261116`. El paquete integrado configura su propia base de pruebas y utiliza su propio verificador de esquema.

Para probar este avance por separado, usa la base de desarrollo `RepairClientesDesarrollo_20261030`. Ejecuta `sql/03-Clientes-Desarrollo.sql`, `sql/02-Equipos.sql` y `sql/04-Equipos-Tipo.sql` en ese orden en tu instancia de desarrollo. `Repair.Wpf/App.config` usa autenticación de Windows y una instancia local de ejemplo. Ajusta `Data Source` para tu equipo. Registra un cliente ficticio y un equipo con Tipo, vuelve a buscar el equipo tras reiniciar la aplicación y exporta el listado filtrado de Clientes.

Las verificaciones `Repair.Clientes.Verificacion.exe --tipo` y `Repair.Clientes.Verificacion.exe --csv` prueban las reglas de Tipo, exportación y controles sin escribir en SQL Server.
