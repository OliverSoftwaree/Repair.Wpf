using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Repair.Core.Seguridad;

namespace Repair.Core.Logica
{
    public sealed class SeguimientoServicio
    {
        private readonly string cadena;
        private readonly SesionUsuario sesion;
        public SeguimientoServicio(string cadena, SesionUsuario sesion) { this.cadena = cadena; this.sesion = sesion; }
        public static string Siguiente(string estado)
        {
            switch (estado)
            {
                case "En diagnóstico": return "En reparación";
                case "En reparación": return "Lista para entrega";
                case "Lista para entrega": return "Entregada";
                default: return null;
            }
        }
        public bool PuedeAvanzar(string estado) => Siguiente(estado) != null && sesion.Permite(estado == "Lista para entrega" ? Permiso.Entrega : Permiso.Reparacion);
        public bool PuedeReingresar => sesion.Permite(Permiso.Recepcion);
        public async Task<bool> AvanzarAsync(int id, string esperado)
        {
            string destino = Siguiente(esperado);
            if (destino == null || id <= 0) throw new ArgumentException("La orden no tiene una transición disponible.");
            sesion.Exigir(esperado == "Lista para entrega" ? Permiso.Entrega : Permiso.Reparacion);
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(@"SET NOCOUNT ON;
UPDATE o SET Estado=@Destino,
EntregadaUtc=CASE WHEN @Destino=N'Entregada' THEN SYSUTCDATETIME() ELSE EntregadaUtc END
FROM dbo.Ordenes AS o
WHERE o.Id=@Id AND o.Estado COLLATE Latin1_General_100_BIN2=@Esperado
AND DATALENGTH(o.Estado)=DATALENGTH(@Esperado) AND o.EntregadaUtc IS NULL
AND NULLIF(LTRIM(RTRIM(o.Diagnostico)),N'') IS NOT NULL
AND EXISTS(SELECT 1 FROM dbo.Equipos e WHERE e.Id=o.EquipoId AND e.ClienteId=o.ClienteId);
SELECT @@ROWCOUNT;", conexion))
            {
                comando.CommandTimeout = 10;
                comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                comando.Parameters.Add("@Esperado", SqlDbType.NVarChar, 50).Value = esperado;
                comando.Parameters.Add("@Destino", SqlDbType.NVarChar, 50).Value = destino;
                await conexion.OpenAsync();
                return (int)await comando.ExecuteScalarAsync() == 1;
            }
        }

        public async Task<int> ReingresarAsync(int equipoId)
        {
            sesion.Exigir(Permiso.Recepcion);
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(@"SET NOCOUNT ON; SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @ClienteId int, @OrdenId int;
SELECT @ClienteId=ClienteId FROM dbo.Equipos WITH(UPDLOCK,HOLDLOCK) WHERE Id=@EquipoId;
IF @ClienteId IS NULL THROW 51010,'El equipo no existe.',1;
SELECT @OrdenId=Id FROM dbo.Ordenes WITH(UPDLOCK,HOLDLOCK) WHERE EquipoId=@EquipoId AND Estado<>N'Entregada';
IF @OrdenId IS NULL
BEGIN
INSERT dbo.Ordenes(ClienteId,EquipoId,Estado) VALUES(@ClienteId,@EquipoId,N'Pendiente');
SET @OrdenId=CONVERT(int,SCOPE_IDENTITY());
END;
COMMIT;
SELECT @OrdenId;", conexion))
            {
                comando.CommandTimeout = 10;
                comando.Parameters.Add("@EquipoId", SqlDbType.Int).Value = equipoId;
                await conexion.OpenAsync();
                return (int)await comando.ExecuteScalarAsync();
            }
        }
    }
}
