# Repair · Revisión de Seguimiento y entrega

La aplicación de revisión da acceso a **Diagnóstico**, **Clientes y Equipos con Tipo y CSV** y **Seguimiento y entrega**. Abre Diagnóstico y Clientes/Equipos en sus respectivas compilaciones de Repair. Seguimiento se abre desde el inicio de revisión en modo consulta. Las tres ventanas de este paquete apuntan a una base de desarrollo exclusiva.

El Diagnóstico corresponde al commit `5f5b2c91d8c4310fce9e2064df9666d689493f3a` de Oliver. La rama `avance/tipo-csv-para-revision-20261116` contiene el código y los scripts del avance local `22d94c6`, sobre su base original. Sus evidencias y archivos de prueba locales se conservan fuera de GitHub. La combinación actual usa ambas compilaciones a través del inicio de revisión.

## Compilar con Visual Studio 2022

En Windows, instala la carga de trabajo **Desarrollo de escritorio de .NET** y el paquete de destino de **.NET Framework 4.8**. Clona el repositorio y prepara las tres carpetas desde PowerShell:

```powershell
git clone --branch revision/seguimiento-20261116 https://github.com/OliverSoftwaree/Repair.Wpf.git repair-revision
Set-Location repair-revision
git worktree add --detach ../repair-oliver 5f5b2c91d8c4310fce9e2064df9666d689493f3a
git worktree add --detach ../repair-tipo-csv origin/avance/tipo-csv-para-revision-20261116
```

El script usa MSBuild de Visual Studio 2022 en Release y prepara las cuatro configuraciones de ejecución en el destino. Acepta la instancia local con `-Servidor '.'` o un nombre de instancia como `-Servidor '.\SQLEXPRESS'`. Usa autenticación de Windows.

```powershell
.\Revision\Preparar-Revision.ps1 -FuenteOliver ..\repair-oliver `
    -FuenteTipoCsv ..\repair-tipo-csv -Destino ..\repair-paquete -Servidor '.'
```

Para revisar en el IDE, las soluciones son `Revision/Repair.Seguimiento.sln` y `Repair.Wpf.sln` en cada una de las carpetas hermanas. La estructura del paquete coloca `Repair.Revision.exe` y `Verificar-Repair-Seguimiento.exe` en la raíz, con las compilaciones de Repair en `Oliver` y `TipoCsv`.

## Probar con SQL Server

1. En el paquete, ejecuta `Verificar-Repair-Seguimiento.exe` desde tu sesión de Windows. El verificador prepara únicamente `RepairSeguimientoDesarrollo_20261115`. Si ya existe, comprueba su marca de propiedad antes de ejecutar el esquema o insertar datos. La primera ejecución requiere permiso para crear la base.
2. Consulta `resultado-sql.txt` en el paquete. El verificador imprime dos números de órdenes ficticias, una entregada y vinculada con su equipo y otra sin vínculo. Esos registros quedan en la base de pruebas para repetir la consulta.
3. Ejecuta `Repair.Revision.exe` y entra en **Seguimiento y entrega · consulta**. Busca ambos números. La orden entregada muestra su estado y fecha. La orden sin vínculo muestra «Sin equipo vinculado». Cierra la ventana y vuelve a buscar para comprobar la lectura.

## Validación y alcance

El 15 de noviembre de 2026, SQL Server aprobó el flujo completo en la base aislada. Se bloquearon los saltos y los estados desactualizados. Dos intentos concurrentes dieron lugar a una sola entrega. Un segundo proceso recuperó el diagnóstico, el estado y la primera fecha de entrega. Las 30 comprobaciones automatizadas de Seguimiento verifican la lógica con datos ficticios, la interfaz con una respuesta SQL simulada y la restricción del escritor a la base de pruebas.

Seguimiento queda en consulta dentro de Repair. Las transiciones de prueba solo se ejecutan desde el verificador aislado. La propuesta para habilitar el flujo en una futura integración incluye:

- Ajustar el guardado de diagnóstico para conservar el estado y la fecha de una orden avanzada y mostrar el estado confirmado al recargar.
- Ajustar el alta de equipo para que una orden entregada cuente como inactiva. Cuando corresponda una orden nueva, vincularla con el equipo recién guardado y conservar Tipo y CSV al unificar los recorridos.
- Confirmar el esquema de órdenes y probar ambos recorridos juntos antes de habilitar el cambio de estado desde Repair. Las órdenes antiguas sin un vínculo fiable conservan el equipo vacío.
