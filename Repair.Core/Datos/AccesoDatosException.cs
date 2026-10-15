using System;

namespace Repair.Core.Datos
{
    public sealed class AccesoDatosException : Exception
    {
        public AccesoDatosException(Exception inner)
            : base("No se pudo completar la operación en SQL Server. Comprueba la instancia, la base RepairAcademico, el esquema y los permisos. No se ha confirmado ningún guardado; consulta el listado antes de reintentar.", inner) { }
    }
}
