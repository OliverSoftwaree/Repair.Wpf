using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Repair.Core.Seguridad
{
    public sealed class AutenticacionSql
    {
        private readonly string cadena;
        public AutenticacionSql(string cadena) { this.cadena = cadena; }

        public async Task<SesionUsuario> IniciarAsync(string usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario) || usuario.Length > 60 || string.IsNullOrEmpty(clave) || clave.Length > 200) return null;
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand("SELECT Id, Usuario, Rol, Sal, Hash, Iteraciones FROM dbo.Usuarios WHERE Usuario=@Usuario AND Activo=1;", conexion))
            {
                comando.CommandTimeout = 10;
                comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 60).Value = usuario.Trim();
                await conexion.OpenAsync().ConfigureAwait(false);
                using (var lector = await comando.ExecuteReaderAsync(CommandBehavior.SingleRow).ConfigureAwait(false))
                {
                    if (!await lector.ReadAsync().ConfigureAwait(false)) { Derivar(clave, new byte[16], 210000); return null; }
                    var esperado = (byte[])lector[4];
                    var actual = Derivar(clave, (byte[])lector[3], lector.GetInt32(5));
                    int diferencia = actual.Length ^ esperado.Length;
                    for (int i = 0; i < Math.Min(actual.Length, esperado.Length); i++) diferencia |= actual[i] ^ esperado[i];
                    return diferencia == 0 ? new SesionUsuario(lector.GetInt32(0), lector.GetString(1), lector.GetString(2)) : null;
                }
            }
        }

        public static byte[] Derivar(string clave, byte[] sal, int iteraciones)
        {
            if (iteraciones < 100000 || iteraciones > 1000000) throw new InvalidOperationException("Parámetros de contraseña incompatibles.");
            using (var derivacion = new Rfc2898DeriveBytes(clave, sal, iteraciones, HashAlgorithmName.SHA256)) return derivacion.GetBytes(32);
        }

        public async Task CambiarClaveAsync(SesionUsuario sesion, string anterior, string nueva)
        {
            if (nueva == null || nueva.Length < 12 || nueva.Length > 200) throw new ArgumentException("Usa una contraseña de 12 a 200 caracteres.");
            var comprobada = await IniciarAsync(sesion.Usuario, anterior);
            if (comprobada == null || comprobada.Id != sesion.Id) throw new UnauthorizedAccessException("La contraseña actual no coincide.");
            byte[] sal = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(sal);
            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand("UPDATE dbo.Usuarios SET Sal=@Sal, Hash=@Hash, Iteraciones=210000 WHERE Id=@Id AND Activo=1;", conexion))
            {
                comando.Parameters.Add("@Id", SqlDbType.Int).Value = sesion.Id;
                comando.Parameters.Add("@Sal", SqlDbType.VarBinary, 16).Value = sal;
                comando.Parameters.Add("@Hash", SqlDbType.VarBinary, 32).Value = Derivar(nueva, sal, 210000);
                await conexion.OpenAsync();
                if (await comando.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("La cuenta ya no está disponible.");
            }
        }
    }
}
