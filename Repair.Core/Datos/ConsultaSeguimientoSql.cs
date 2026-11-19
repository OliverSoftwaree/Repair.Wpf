using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Repair.Core.Integracion;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public sealed class ConsultaSeguimientoSql : IConsultaSeguimiento
    {
        private readonly string cadena;

        public ConsultaSeguimientoSql(string cadena)
        {
            if (string.IsNullOrWhiteSpace(cadena)) throw new ArgumentException("Falta la conexión de Seguimiento.", nameof(cadena));
            this.cadena = cadena;
        }

        public async Task<OrdenSeguimiento> BuscarPorIdAsync(int id)
        {
            if (id <= 0) return null;
            try
            {
                using (var conexion = new SqlConnection(cadena))
                {
                    await conexion.OpenAsync().ConfigureAwait(false);
                    var (tieneEquipo, tieneFecha) = await LeerColumnasAsync(conexion).ConfigureAwait(false);
                    using (var comando = conexion.CreateCommand())
                    {
                        comando.CommandTimeout = 8;
                        comando.CommandText = $@"
SELECT o.Id, o.ClienteId, c.Nombre, o.Estado, o.Diagnostico, o.Observaciones,
       {(tieneEquipo ? "o.EquipoId, RTRIM(e.Marca) + N' ' + LTRIM(e.Modelo)" : "CAST(NULL AS int), CAST(NULL AS nvarchar(161))")},
       {(tieneFecha ? "o.EntregadaUtc" : "CAST(NULL AS datetime2)")}
FROM dbo.Ordenes AS o
INNER JOIN dbo.Clientes AS c ON c.Id = o.ClienteId
{(tieneEquipo ? "LEFT JOIN dbo.Equipos AS e ON e.Id = o.EquipoId AND e.ClienteId = o.ClienteId" : "")}
WHERE o.Id = @Id;";
                        comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                        using (var lector = await comando.ExecuteReaderAsync(CommandBehavior.SingleRow).ConfigureAwait(false))
                        {
                            if (!await lector.ReadAsync().ConfigureAwait(false)) return null;
                            int? equipoId = lector.IsDBNull(6) ? (int?)null : lector.GetInt32(6);
                            DateTime? entrega = lector.IsDBNull(8) ? (DateTime?)null : DateTime.SpecifyKind(lector.GetDateTime(8), DateTimeKind.Utc);
                            return new OrdenSeguimiento(lector.GetInt32(0), lector.GetInt32(1), lector.GetString(2),
                                equipoId, LeerTexto(lector, 7), lector.GetString(3), LeerTexto(lector, 4), LeerTexto(lector, 5), entrega);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is ArgumentException || ex is InvalidOperationException || ex is TypeInitializationException)
            {
                throw new AccesoDatosException(ex);
            }
        }

        private static async Task<(bool TieneEquipo, bool TieneFecha)> LeerColumnasAsync(SqlConnection conexion)
        {
            using (var consulta = conexion.CreateCommand())
            {
                consulta.CommandTimeout = 8;
                consulta.CommandText = @"SELECT
    CAST(CASE WHEN COL_LENGTH(N'dbo.Ordenes', N'EquipoId') IS NULL THEN 0 ELSE 1 END AS bit),
    CAST(CASE WHEN COL_LENGTH(N'dbo.Ordenes', N'EntregadaUtc') IS NULL THEN 0 ELSE 1 END AS bit);";
                using (var lector = await consulta.ExecuteReaderAsync(CommandBehavior.SingleRow).ConfigureAwait(false))
                {
                    await lector.ReadAsync().ConfigureAwait(false);
                    return (lector.GetBoolean(0), lector.GetBoolean(1));
                }
            }
        }

        private static string LeerTexto(SqlDataReader lector, int columna) => lector.IsDBNull(columna) ? null : lector.GetString(columna);
    }
}
