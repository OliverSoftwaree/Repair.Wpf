using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Repair.Core.Seguridad;

namespace Repair.Instalacion
{
    internal static class Program
    {
        internal const string BasePruebas = "RepairEntregaDesarrollo_20261119";
        internal static string Carpeta => AppDomain.CurrentDomain.BaseDirectory;
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string ruta = Path.Combine(Carpeta, "Resultado-preparacion.txt");
            var informe = new StringBuilder();
            Action<string> anotar = s => { Console.WriteLine(s); informe.AppendLine(s); File.WriteAllText(ruta,informe.ToString(),new UTF8Encoding(true)); };
            try
            {
                var config = new XmlDocument(); config.Load(Path.Combine(Carpeta,"Repair.Wpf.exe.config"));
                var cadena = new SqlConnectionStringBuilder(config.SelectSingleNode("/configuration/connectionStrings/add[@name='RepairSql']").Attributes["connectionString"].Value);
                if (cadena.InitialCatalog != BasePruebas && cadena.InitialCatalog != "RepairEntrega") throw new InvalidOperationException("El destino debe ser una base exclusiva de la entrega.");
                if (!cadena.IntegratedSecurity || !string.IsNullOrEmpty(cadena.AttachDBFilename)) throw new InvalidOperationException("Usa autenticación de Windows y una base del motor SQL Server.");
                if (cadena.DataSource == ".") cadena.DataSource = Environment.MachineName;
                if (args.Length == 3 && args[0] == "--reabrir")
                    return PruebasEntrega.LeerDesdeOtroProceso(cadena.ConnectionString, int.Parse(args[1]), long.Parse(args[2]));
                if (args.Length != 0 && !(args.Length==1 && args[0]=="--instalar")) throw new ArgumentException("Argumentos válidos: --instalar o ninguno para preparar y probar.");
                anotar("Repair 1.0 RC1 · Preparación · " + DateTimeOffset.Now.ToString("o"));
                PrepararAsync(cadena).GetAwaiter().GetResult();
                SembrarUsuariosAsync(cadena.ConnectionString).GetAwaiter().GetResult();
                config.SelectSingleNode("/configuration/connectionStrings/add[@name='RepairSql']").Attributes["connectionString"].Value = cadena.ConnectionString;
                config.Save(Path.Combine(Carpeta,"Repair.Wpf.exe.config"));
                anotar("OK · Base exclusiva y usuarios de demostración disponibles. Se conservaron las cuentas y datos existentes.");
                if (cadena.InitialCatalog==BasePruebas && args.Length==0)
                    PruebasEntrega.EjecutarAsync(cadena.ConnectionString,anotar).GetAwaiter().GetResult();
                else anotar("Instalación completada. Pruebas de escritura disponibles en la base de desarrollo.");
                return 0;
            }
            catch(Exception ex)
            {
                anotar("PENDIENTE · " + ex.GetType().Name + ": " + ex.Message);
                return 1;
            }
        }

        internal static async Task PrepararAsync(SqlConnectionStringBuilder destino)
        {
            string nombre = destino.InitialCatalog;
            var master = new SqlConnectionStringBuilder(destino.ConnectionString) { InitialCatalog="master" };
            using(var conexion=new SqlConnection(master.ConnectionString))
            using(var comando=conexion.CreateCommand())
            {
                comando.CommandTimeout=45;
                comando.CommandText=$@"IF DB_ID(N'{nombre}') IS NULL
BEGIN
EXEC(N'CREATE DATABASE [{nombre}]');
EXEC [{nombre}].sys.sp_addextendedproperty @name=N'RepairProducto',@value=N'Repair-Entrega';
END;";
                await conexion.OpenAsync(); await comando.ExecuteNonQueryAsync();
            }
            string esquema;
            using(var recurso=Assembly.GetExecutingAssembly().GetManifestResourceStream("Repair.Instalacion.05-Entrega-RC1.sql"))
            using(var lector=new StreamReader(recurso)) esquema=lector.ReadToEnd();
            using(var conexion=new SqlConnection(destino.ConnectionString))
            {
                await conexion.OpenAsync();
                foreach(string lote in Regex.Split(esquema,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
                using(var comando=new SqlCommand(lote,conexion)){comando.CommandTimeout=45;await comando.ExecuteNonQueryAsync();}
            }
        }

        internal static async Task SembrarUsuariosAsync(string cadena)
        {
            foreach(var u in new[]{new[]{"admin.demo","Administrador","Repair-Admin-2026!"},new[]{"recepcion.demo","Recepcionista","Repair-Recepcion-2026!"},new[]{"tecnico.demo","Técnico","Repair-Tecnico-2026!"}})
            {
                byte[] sal=new byte[16]; using(var rng=RandomNumberGenerator.Create())rng.GetBytes(sal);
                using(var conexion=new SqlConnection(cadena))
                using(var comando=new SqlCommand(@"SET XACT_ABORT ON; BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WITH(UPDLOCK,HOLDLOCK) WHERE Usuario=@Usuario)
INSERT dbo.Usuarios(Usuario,Rol,Sal,Hash,Iteraciones) VALUES(@Usuario,@Rol,@Sal,@Hash,210000);
COMMIT;",conexion))
                {
                    comando.Parameters.Add("@Usuario",SqlDbType.NVarChar,60).Value=u[0];
                    comando.Parameters.Add("@Rol",SqlDbType.NVarChar,30).Value=u[1];
                    comando.Parameters.Add("@Sal",SqlDbType.VarBinary,16).Value=sal;
                    comando.Parameters.Add("@Hash",SqlDbType.VarBinary,32).Value=AutenticacionSql.Derivar(u[2],sal,210000);
                    await conexion.OpenAsync();await comando.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
