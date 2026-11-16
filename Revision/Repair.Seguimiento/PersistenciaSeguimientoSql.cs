using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Repair.Seguimiento
{
    // Solo se llama desde el verificador, nunca desde la ventana integrada.
    internal sealed class PersistenciaSeguimientoSql
    {
        internal const string BasePruebas = "RepairSeguimientoDesarrollo_20261115";
        internal const string Marca = "Seguimiento-20261115";
        private readonly string cadena;

        public PersistenciaSeguimientoSql(string cadena)
        {
            var configuracion = new SqlConnectionStringBuilder(cadena);
            if (!string.Equals(configuracion.InitialCatalog, BasePruebas, StringComparison.OrdinalIgnoreCase) ||
                !configuracion.IntegratedSecurity || !string.IsNullOrEmpty(configuracion.AttachDBFilename))
            {
                throw new InvalidOperationException("Solo se admite la base exclusiva de Seguimiento con autenticación de Windows.");
            }

            this.cadena = configuracion.ConnectionString;
        }

        public async Task<bool> IntentarAvanzarAsync(int numero, string estadoEsperado, string destino)
        {
            if (numero <= 0 || destino == null || FlujoOrden.SiguienteEstado(estadoEsperado) != destino) return false;

            const string actualizar = @"
SET NOCOUNT ON;
IF DB_NAME() <> @Base OR NOT EXISTS (
    SELECT 1 FROM sys.extended_properties WHERE class = 0 AND name = N'RepairAvance'
    AND CONVERT(nvarchar(100), value) = @Marca)
    THROW 51001, 'La base no pertenece a las pruebas de Seguimiento.', 1;

UPDATE o
SET Estado = @Destino,
    EntregadaUtc = CASE WHEN @Destino = @Entregada THEN SYSUTCDATETIME() ELSE o.EntregadaUtc END
FROM dbo.Ordenes AS o
WHERE o.Id = @Numero
  AND o.Estado COLLATE Latin1_General_100_BIN2 = @Esperado
  AND DATALENGTH(o.Estado) = DATALENGTH(@Esperado)
  AND o.EntregadaUtc IS NULL
  AND NULLIF(LTRIM(RTRIM(o.Diagnostico)), N'') IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.Equipos AS e WHERE e.Id = o.EquipoId AND e.ClienteId = o.ClienteId);
SELECT @@ROWCOUNT;";

            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(actualizar, conexion))
            {
                comando.CommandTimeout = 10;
                comando.Parameters.Add("@Base", SqlDbType.NVarChar, 128).Value = BasePruebas;
                comando.Parameters.Add("@Marca", SqlDbType.NVarChar, 100).Value = Marca;
                comando.Parameters.Add("@Numero", SqlDbType.Int).Value = numero;
                comando.Parameters.Add("@Esperado", SqlDbType.NVarChar, 50).Value = estadoEsperado;
                comando.Parameters.Add("@Destino", SqlDbType.NVarChar, 50).Value = destino;
                comando.Parameters.Add("@Entregada", SqlDbType.NVarChar, 50).Value = FlujoOrden.Entregada;
                await conexion.OpenAsync().ConfigureAwait(false);
                return (int)await comando.ExecuteScalarAsync().ConfigureAwait(false) == 1;
            }
        }
    }
}
