using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public sealed class ClienteSqlRepositorio : IClienteRepositorio
    {
        private readonly string cadena;
        private const string Campos = "Id, Nombre, Telefono, Correo, Notas, CreadoUtc";

        public ClienteSqlRepositorio(string cadenaConexion)
        {
            if (string.IsNullOrWhiteSpace(cadenaConexion)) throw new ArgumentException("Falta la cadena de conexión.");
            cadena = cadenaConexion;
        }

        public async Task<IReadOnlyList<Cliente>> BuscarAsync(string texto)
        {
            var lista = new List<Cliente>();
            // ESCAPE permite buscar %, _, [ y ~ como texto literal.
            string patron = "%" + texto.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
            int id;
            int.TryParse(texto, out id);
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand("SELECT " + Campos + @" FROM dbo.Clientes
WHERE @Texto = N'' OR Id = @Id OR Nombre LIKE @Patron ESCAPE N'~'
OR Telefono LIKE @Patron ESCAPE N'~' OR Correo LIKE @Patron ESCAPE N'~' ORDER BY Id DESC;", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Texto", SqlDbType.NVarChar, 100).Value = texto;
                    comando.Parameters.Add("@Patron", SqlDbType.NVarChar, 202).Value = patron;
                    comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
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

        public async Task<Cliente> ObtenerAsync(int id)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand("SELECT " + Campos + " FROM dbo.Clientes WHERE Id = @Id;", conexion))
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

        public async Task<int> AgregarAsync(Cliente cliente)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                using (var comando = new SqlCommand(@"INSERT INTO dbo.Clientes (Nombre, Telefono, Correo, Notas)
OUTPUT INSERTED.Id VALUES (@Nombre, @Telefono, @Correo, @Notas);", conexion))
                {
                    comando.CommandTimeout = 10;
                    comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = cliente.Nombre;
                    comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, 25).Value = cliente.Telefono;
                    comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 150).Value = Nulo(cliente.Correo);
                    comando.Parameters.Add("@Notas", SqlDbType.NVarChar, 300).Value = Nulo(cliente.Notas);
                    await conexion.OpenAsync();
                    return Convert.ToInt32(await comando.ExecuteScalarAsync());
                }
            }
            catch (SqlException ex) { throw new AccesoDatosException(ex); }
            catch (InvalidOperationException ex) { throw new AccesoDatosException(ex); }
            catch (TypeInitializationException ex) { throw new AccesoDatosException(ex); }
        }

        private static object Nulo(string valor) { return string.IsNullOrEmpty(valor) ? (object)DBNull.Value : valor; }

        private static Cliente Leer(SqlDataReader lector)
        {
            return new Cliente
            {
                Id = lector.GetInt32(0), Nombre = lector.GetString(1), Telefono = lector.GetString(2),
                Correo = lector.IsDBNull(3) ? "" : lector.GetString(3),
                Notas = lector.IsDBNull(4) ? "" : lector.GetString(4),
                CreadoUtc = DateTime.SpecifyKind(lector.GetDateTime(5), DateTimeKind.Utc)
            };
        }
    }
}
