using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Repair.Seguimiento
{
    internal static class ConsultaOrdenesSql
    {
        public static string CadenaConfigurada()
        {
            var cadena = ConfigurationManager.ConnectionStrings["RepairSql"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cadena)) throw new ConfigurationErrorsException("Falta la conexión RepairSql.");
            return cadena;
        }

        public static Task<OrdenSeguimiento> ConsultarAsync(int numero) => ConsultarAsync(CadenaConfigurada(), numero);

        internal static async Task<OrdenSeguimiento> ConsultarAsync(string cadena, int numero)
        {
            using (var conexion = new SqlConnection(cadena))
            {
                await conexion.OpenAsync().ConfigureAwait(false);
                bool tieneEquipo;
                bool tieneFecha;
                using (var esquema = conexion.CreateCommand())
                {
                    esquema.CommandTimeout = 8;
                    esquema.CommandText = @"SELECT
    CAST(CASE WHEN COL_LENGTH(N'dbo.Ordenes', N'EquipoId') IS NULL THEN 0 ELSE 1 END AS bit),
    CAST(CASE WHEN COL_LENGTH(N'dbo.Ordenes', N'EntregadaUtc') IS NULL THEN 0 ELSE 1 END AS bit);";
                    using (var lector = await esquema.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        await lector.ReadAsync().ConfigureAwait(false);
                        tieneEquipo = lector.GetBoolean(0);
                        tieneFecha = lector.GetBoolean(1);
                    }
                }

                const string especificacionEquipo = "RTRIM(e.Marca) + N' ' + LTRIM(e.Modelo)";
                using (var comando = conexion.CreateCommand())
                {
                    comando.CommandTimeout = 8;
                    comando.CommandText = $@"
SELECT o.Id, c.Nombre, o.Diagnostico, o.Observaciones, o.Estado,
       {(tieneEquipo ? especificacionEquipo : "CAST(NULL AS nvarchar(161))")},
       {(tieneFecha ? "o.EntregadaUtc" : "CAST(NULL AS datetime2)")}
FROM dbo.Ordenes AS o
INNER JOIN dbo.Clientes AS c ON c.Id = o.ClienteId
{(tieneEquipo ? "LEFT JOIN dbo.Equipos AS e ON e.Id = o.EquipoId AND e.ClienteId = o.ClienteId" : "")}
WHERE o.Id = @Numero;";
                    comando.Parameters.Add("@Numero", SqlDbType.Int).Value = numero;
                    using (var lector = await comando.ExecuteReaderAsync(CommandBehavior.SingleRow).ConfigureAwait(false))
                    {
                        if (!await lector.ReadAsync().ConfigureAwait(false)) return null;

                        DateTime? fecha = lector.IsDBNull(6) ? (DateTime?)null : DateTime.SpecifyKind(lector.GetDateTime(6), DateTimeKind.Utc);
                        return new OrdenSeguimiento(lector.GetInt32(0), lector.GetString(1), LeerTexto(lector, 5),
                            LeerTexto(lector, 2), LeerTexto(lector, 3), lector.GetString(4), fecha);
                    }
                }
            }
        }

        private static string LeerTexto(SqlDataReader lector, int columna)
        {
            return lector.IsDBNull(columna) ? null : lector.GetString(columna);
        }
    }
}
