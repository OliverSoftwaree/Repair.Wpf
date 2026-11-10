using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public sealed class OrdenSqlRepositorio : IOrdenRepositorio
    {
        private readonly string cadena;
        private const string Campos = "Id, ClienteId, Estado, Diagnostico, Observaciones, CreadoUtc";

        public OrdenSqlRepositorio(string cadenaConexion)
        {
            if (string.IsNullOrWhiteSpace(cadenaConexion)) throw new ArgumentException("Falta la cadena de conexión de órdenes.");
            cadena = cadenaConexion;
        }

        public async Task<IReadOnlyList<OrdenTrabajo>> BuscarAsync(string texto)
        {
            var lista = new List<OrdenTrabajo>();
            string patron = "%" + (texto ?? "").Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
            int id;
            int.TryParse(texto, out id);
            int clienteId;
            int.TryParse(texto, out clienteId);

            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"SELECT " + Campos + @" FROM dbo.Ordenes
WHERE @Texto = N'' OR Id = @Id OR ClienteId = @ClienteId OR Estado LIKE @Patron ESCAPE N'~'
OR Diagnostico LIKE @Patron ESCAPE N'~' OR Observaciones LIKE @Patron ESCAPE N'~'
ORDER BY Id DESC;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Texto", SqlDbType.NVarChar, 100).Value = texto ?? "";
                    comando.Parameters.Add("@Patron", SqlDbType.NVarChar, 800).Value = patron;
                    comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    comando.Parameters.Add("@ClienteId", SqlDbType.Int).Value = clienteId;
                    await conexion.OpenAsync();
                    using (var lector = await comando.ExecuteReaderAsync())
                        while (await lector.ReadAsync()) lista.Add(Leer(lector));
                }
                return lista;
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        public async Task<OrdenTrabajo> ObtenerAsync(int id)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand("SELECT " + Campos + " FROM dbo.Ordenes WHERE Id = @Id;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    await conexion.OpenAsync();
                    using (var lector = await comando.ExecuteReaderAsync())
                        return await lector.ReadAsync() ? Leer(lector) : null;
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        public async Task<int> AgregarAsync(OrdenTrabajo orden)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"INSERT INTO dbo.Ordenes (ClienteId, Estado, Diagnostico, Observaciones)
OUTPUT INSERTED.Id VALUES (@ClienteId, @Estado, @Diagnostico, @Observaciones);", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@ClienteId", SqlDbType.Int).Value = orden.ClienteId;
                    comando.Parameters.Add("@Estado", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(orden.Estado) ? "Pendiente" : orden.Estado;
                    comando.Parameters.Add("@Diagnostico", SqlDbType.NVarChar, -1).Value = Nulo(orden.Diagnostico);
                    comando.Parameters.Add("@Observaciones", SqlDbType.NVarChar, -1).Value = Nulo(orden.Observaciones);
                    await conexion.OpenAsync();
                    return Convert.ToInt32(await comando.ExecuteScalarAsync());
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        public async Task GuardarDiagnosticoAsync(int id, string diagnostico, string observaciones)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"UPDATE dbo.Ordenes
SET Diagnostico = @Diagnostico,
    Observaciones = @Observaciones,
    Estado = 'En diagnóstico'
WHERE Id = @Id;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Diagnostico", SqlDbType.NVarChar, -1).Value = Nulo(diagnostico);
                    comando.Parameters.Add("@Observaciones", SqlDbType.NVarChar, -1).Value = Nulo(observaciones);
                    comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    await conexion.OpenAsync();
                    await comando.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        private static object Nulo(string valor)
        {
            return string.IsNullOrEmpty(valor) ? (object)DBNull.Value : valor;
        }

        private static OrdenTrabajo Leer(SqlDataReader lector)
        {
            return new OrdenTrabajo
            {
                Id = lector.GetInt32(0),
                ClienteId = lector.GetInt32(1),
                Estado = lector.IsDBNull(2) ? "Pendiente" : lector.GetString(2),
                Diagnostico = lector.IsDBNull(3) ? "" : lector.GetString(3),
                Observaciones = lector.IsDBNull(4) ? "" : lector.GetString(4),
                CreadoUtc = DateTime.SpecifyKind(lector.GetDateTime(5), DateTimeKind.Utc)
            };
        }
    }
}
