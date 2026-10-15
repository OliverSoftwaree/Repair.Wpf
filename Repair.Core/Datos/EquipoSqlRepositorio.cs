using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public sealed class EquipoSqlRepositorio : IEquipoRepositorio
    {
        private readonly string cadena;
        private const string Campos = "Id, ClienteId, Marca, Modelo, Serie, Accesorios, Condicion, CreadoUtc";

        public EquipoSqlRepositorio(string cadenaConexion)
        {
            if (string.IsNullOrWhiteSpace(cadenaConexion)) throw new ArgumentException("Falta la cadena de conexión.");
            cadena = cadenaConexion;
        }

        public async Task<IReadOnlyList<Equipo>> BuscarAsync(string texto)
        {
            var lista = new List<Equipo>();
            string patron = "%" + (texto ?? "").Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
            int id;
            int.TryParse(texto, out id);
            int clienteId;
            int.TryParse(texto, out clienteId);
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand("SELECT " + Campos + " FROM dbo.Equipos\nWHERE @Texto = N'' OR Id = @Id OR ClienteId = @ClienteId OR Marca LIKE @Patron ESCAPE N'~' OR Modelo LIKE @Patron ESCAPE N'~' OR Serie LIKE @Patron ESCAPE N'~' OR Accesorios LIKE @Patron ESCAPE N'~' OR Condicion LIKE @Patron ESCAPE N'~' ORDER BY Id DESC;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Texto", SqlDbType.NVarChar, 100).Value = (texto ?? "");
                    comando.Parameters.Add("@Patron", SqlDbType.NVarChar, 602).Value = patron;
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

        public async Task<Equipo> ObtenerAsync(int id)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand("SELECT " + Campos + " FROM dbo.Equipos WHERE Id = @Id;", conexion))
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

        public async Task<int> AgregarAsync(Equipo equipo)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"INSERT INTO dbo.Equipos (ClienteId, Marca, Modelo, Serie, Accesorios, Condicion)\nOUTPUT INSERTED.Id VALUES (@ClienteId, @Marca, @Modelo, @Serie, @Accesorios, @Condicion);", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@ClienteId", SqlDbType.Int).Value = equipo.ClienteId;
                    comando.Parameters.Add("@Marca", SqlDbType.NVarChar, 60).Value = equipo.Marca;
                    comando.Parameters.Add("@Modelo", SqlDbType.NVarChar, 100).Value = equipo.Modelo;
                    comando.Parameters.Add("@Serie", SqlDbType.NVarChar, 100).Value = Nulo(equipo.Serie);
                    comando.Parameters.Add("@Accesorios", SqlDbType.NVarChar, 300).Value = Nulo(equipo.Accesorios);
                    comando.Parameters.Add("@Condicion", SqlDbType.NVarChar, 500).Value = equipo.Condicion;
                    await conexion.OpenAsync();
                    return Convert.ToInt32(await comando.ExecuteScalarAsync());
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        public async Task ActualizarAsync(Equipo equipo)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"UPDATE dbo.Equipos SET ClienteId=@ClienteId, Marca=@Marca, Modelo=@Modelo, Serie=@Serie, Accesorios=@Accesorios, Condicion=@Condicion WHERE Id=@Id;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@ClienteId", SqlDbType.Int).Value = equipo.ClienteId;
                    comando.Parameters.Add("@Marca", SqlDbType.NVarChar, 60).Value = equipo.Marca;
                    comando.Parameters.Add("@Modelo", SqlDbType.NVarChar, 100).Value = equipo.Modelo;
                    comando.Parameters.Add("@Serie", SqlDbType.NVarChar, 100).Value = Nulo(equipo.Serie);
                    comando.Parameters.Add("@Accesorios", SqlDbType.NVarChar, 300).Value = Nulo(equipo.Accesorios);
                    comando.Parameters.Add("@Condicion", SqlDbType.NVarChar, 500).Value = equipo.Condicion;
                    comando.Parameters.Add("@Id", SqlDbType.Int).Value = equipo.Id;
                    await conexion.OpenAsync();
                    await comando.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        public async Task EliminarAsync(int id)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"DELETE FROM dbo.Equipos WHERE Id=@Id;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    await conexion.OpenAsync();
                    await comando.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        private static object Nulo(string valor) { return string.IsNullOrEmpty(valor) ? (object)DBNull.Value : valor; }

        private static Equipo Leer(SqlDataReader lector)
        {
            return new Equipo
            {
                Id = lector.GetInt32(0), ClienteId = lector.GetInt32(1), Marca = lector.GetString(2), Modelo = lector.GetString(3),
                Serie = lector.IsDBNull(4) ? "" : lector.GetString(4),
                Accesorios = lector.IsDBNull(5) ? "" : lector.GetString(5),
                Condicion = lector.IsDBNull(6) ? "" : lector.GetString(6),
                CreadoUtc = DateTime.SpecifyKind(lector.GetDateTime(7), DateTimeKind.Utc)
            };
        }
    }
}
