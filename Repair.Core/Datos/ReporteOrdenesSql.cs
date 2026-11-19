using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Repair.Core.Datos
{
    public sealed class ReporteOrdenesSql
    {
        private readonly string cadena;
        public ReporteOrdenesSql(string cadena) { this.cadena = cadena; }
        public async Task<DataTable> ConsultarAsync(string estado, DateTime? desdeLocal, DateTime? hastaLocal, bool entregas)
        {
            if (desdeLocal.HasValue && hastaLocal.HasValue && desdeLocal.Value.Date > hastaLocal.Value.Date)
                throw new ArgumentException("La fecha inicial debe ser anterior o igual a la final.");
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(@"SELECT o.Id AS Orden,c.Nombre AS Cliente,
COALESCE(e.Tipo,N'') AS Tipo,COALESCE(e.Marca+N' '+e.Modelo,N'Sin equipo vinculado') AS Equipo,
o.Estado,o.CreadoUtc,o.EntregadaUtc,o.Diagnostico,o.Observaciones
FROM dbo.Ordenes o JOIN dbo.Clientes c ON c.Id=o.ClienteId
LEFT JOIN dbo.Equipos e ON e.Id=o.EquipoId AND e.ClienteId=o.ClienteId
WHERE (@Estado=N'' OR o.Estado=@Estado)
AND (@Entregas=0 OR o.EntregadaUtc IS NOT NULL)
AND (@Desde IS NULL OR CASE WHEN @Entregas=1 THEN o.EntregadaUtc ELSE o.CreadoUtc END>=@Desde)
AND (@Hasta IS NULL OR CASE WHEN @Entregas=1 THEN o.EntregadaUtc ELSE o.CreadoUtc END<@Hasta)
ORDER BY o.Id DESC;", conexion))
            {
                comando.CommandTimeout = 15;
                comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 50).Value = estado ?? "";
                comando.Parameters.Add("@Entregas", SqlDbType.Bit).Value = entregas;
                comando.Parameters.Add("@Desde", SqlDbType.DateTime2).Value = desdeLocal.HasValue ? (object)desdeLocal.Value.Date.ToUniversalTime() : DBNull.Value;
                comando.Parameters.Add("@Hasta", SqlDbType.DateTime2).Value = DBNull.Value;
                comando.Parameters["@Hasta"].Value = hastaLocal.HasValue ? (object)hastaLocal.Value.Date.AddDays(1).ToUniversalTime() : DBNull.Value;
                await conexion.OpenAsync();
                using (var lector = await comando.ExecuteReaderAsync())
                {
                    var tabla = new DataTable(); tabla.Load(lector); return tabla;
                }
            }
        }
    }
}
