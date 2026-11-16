using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Core.Modelos;

namespace Repair.Clientes.Verificacion
{
    internal static class PruebasEquiposSql
    {
        private const string Base = "RepairClientesDesarrollo_20261030";
        private static string carpeta;
        private static int casos;
        private static readonly List<string> log = new List<string>();

        public static int Ejecutar(string[] args, string directorio)
        {
            carpeta = directorio;
            bool lectura = args.Length == 3 && args[0] == "--equipo-leer";
            bool soloAsync = args.Length == 1 && args[0] == "--asincronia";
            string archivo = Path.Combine(carpeta, lectura ? "equipo-segundo-proceso.log" : soloAsync ? "asincronia.log" : "equipos-sql.log");
            try
            {
                if (lectura) Task.Run(() => Leer(int.Parse(args[1], CultureInfo.InvariantCulture), args[2])).GetAwaiter().GetResult();
                else
                {
                    ProbarContextoWpf();
                    if (!soloAsync) Task.Run(() => CompletarSql()).GetAwaiter().GetResult();
                }
                log.Add("RESULTADO: " + casos + " comprobaciones correctas. " + (soloAsync ? "Contexto WPF con tareas realmente diferidas; sin SQL." : "SQL Server real; no se usaron repositorios de memoria para aprobar persistencia."));
                File.WriteAllLines(archivo, log, new UTF8Encoding(true));
                if (!lectura && !soloAsync) MessageBox.Show("La conexión y las operaciones SQL de Equipos se comprobaron. Abre Repair.Wpf.exe de esta carpeta. Evidencia: equipos-sql.log.", "Repair · SQL Server");
                return 0;
            }
            catch (Exception ex)
            {
                log.Add("NO APROBADO: " + ex);
                File.WriteAllLines(archivo, log, new UTF8Encoding(true));
                if (!lectura && !soloAsync) MessageBox.Show("La preparación o las pruebas SQL no terminaron. Consulta equipos-sql.log. No se cambia a demostración.", "Repair · SQL Server");
                return 1;
            }
        }

        private static string Cadena(string baseDatos)
        {
            string local = Path.Combine(carpeta, "SqlServidor.local.txt");
            string servidor = File.Exists(local) ? File.ReadAllText(local).Trim() : Environment.MachineName;
            return new SqlConnectionStringBuilder { DataSource = servidor, InitialCatalog = baseDatos,
                IntegratedSecurity = true, ConnectTimeout = 5, ApplicationName = "Repair.Sql.Verificacion" }.ConnectionString;
        }

        private static async Task EjecutarScript(string baseDatos, string nombre)
        {
            using (var conexion = new SqlConnection(Cadena(baseDatos)))
            {
                await conexion.OpenAsync();
                using (var comando = new SqlCommand(File.ReadAllText(Path.Combine(carpeta, nombre)), conexion))
                { comando.CommandTimeout = 30; await comando.ExecuteNonQueryAsync(); }
            }
        }

        private static async Task<int> Escalar(string consulta)
        {
            using (var conexion = new SqlConnection(Cadena(Base)))
            {
                await conexion.OpenAsync();
                using (var comando = new SqlCommand(consulta, conexion))
                    return Convert.ToInt32(await comando.ExecuteScalarAsync());
            }
        }

        private static async Task ValidarBase()
        {
            Exigir(await Escalar("SELECT COUNT(*) FROM sys.extended_properties WHERE class=0 AND name=N'RepairAvance' AND CONVERT(nvarchar(100),value)=N'Clientes-20261030';") == 1, "Base exclusiva identificada sin tocar otras bases");
            Exigir(await Escalar("SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.Equipos') AND referenced_object_id=OBJECT_ID(N'dbo.Clientes') AND delete_referential_action=0 AND is_disabled=0 AND is_not_trusted=0;") >= 1, "Relación real Equipos-Clientes activa, validada y sin borrado en cascada");
            Exigir(await Escalar("SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Equipos') AND name=N'UX_Equipos_ClienteMarcaModeloSerie' AND is_unique=1 AND is_disabled=0;") == 1, "SQL protege de duplicados además de la validación del servicio");
        }

        private static async Task CompletarSql()
        {
            await EjecutarScript("master", "03-Clientes-Desarrollo.sql");
            int clientesAntes = await Escalar("SELECT COUNT(*) FROM dbo.Clientes;");
            await EjecutarScript(Base, "02-Equipos.sql");
            await EjecutarScript(Base, "04-Equipos-Tipo.sql");
            await ValidarBase();
            var clientes = new ClienteServicio(new ClienteSqlRepositorio(Cadena(Base)));
            var listaClientes = await clientes.BuscarAsync("");
            string token = Guid.NewGuid().ToString("N");
            int clienteId = listaClientes.Count > 0 ? listaClientes[0].Id :
                await clientes.RegistrarAsync(new Cliente { Nombre = "Cliente prueba SQL Repair", Telefono = "809-555-0199", Notas = "Preparación ficticia de SQL" });
            var repositorio = new EquipoSqlRepositorio(Cadena(Base)); var servicio = new EquipoServicio(repositorio);
            var equipo = new Equipo { ClienteId = clienteId, Tipo = "Laptop", Marca = "Lenovo", Modelo = "ThinkPad T14",
                Serie = "SQL-" + token, Accesorios = "Cargador", Condicion = "No enciende · prueba SQL" };
            int id = await servicio.RegistrarAsync(equipo);
            var leido = await servicio.ObtenerAsync(id);
            Exigir(leido != null && leido.Tipo == "Laptop" && leido.ClienteId == clienteId && leido.Serie == equipo.Serie &&
                leido.Marca == "Lenovo" && leido.Modelo == "ThinkPad T14" && leido.Accesorios == "Cargador" &&
                leido.Condicion == equipo.Condicion && leido.CreadoUtc.Kind == DateTimeKind.Utc, "Alta y consulta reales conservan tipo, propietario y todos los datos");
            Exigir((await servicio.BuscarAsync("Laptop")).Any(e => e.Id == id), "Búsqueda SQL por tipo recupera el equipo");
            try { await servicio.RegistrarAsync(equipo); throw new Exception("Duplicado admitido."); }
            catch (ValidacionException ex) { Exigir(ex.Errores.ContainsKey("Duplicado"), "Duplicado rechazado por el servicio sin otra alta"); }
            try { await repositorio.AgregarAsync(equipo); throw new Exception("Duplicado SQL admitido."); }
            catch (AccesoDatosException ex) { Exigir(EsSql(ex, 2601, 2627), "Índice SQL rechaza duplicados directos"); }
            var invalido = new Equipo { ClienteId = int.MaxValue, Tipo = "Monitor", Marca = "Prueba",
                Modelo = "Relación inexistente", Serie = "FK-" + token, Condicion = "Sin imagen" };
            try { await repositorio.AgregarAsync(invalido); throw new Exception("Cliente inexistente admitido."); }
            catch (AccesoDatosException ex) { Exigir(EsSql(ex, 547), "SQL rechaza un equipo con cliente inexistente"); }

            leido.Tipo = "Monitor"; leido.Condicion = "Sin imagen · actualizada";
            await servicio.ActualizarAsync(leido);
            var actualizado = await servicio.ObtenerAsync(id);
            Exigir(actualizado.Tipo == "Monitor" && actualizado.Condicion == leido.Condicion && actualizado.Serie == equipo.Serie, "Edición SQL conserva tipo y condición actualizados");
            Exigir((await servicio.BuscarAsync("SQL-" + token)).Single().Id == id, "Búsqueda recupera el registro actualizado");
            var perdido = new Equipo { Id = int.MaxValue, ClienteId = clienteId, Tipo = "UPS", Marca = "Prueba",
                Modelo = "Ausente", Serie = "NO-" + token, Condicion = "No enciende" };
            try { await repositorio.ActualizarAsync(perdido); throw new Exception("Se confirmó una actualización inexistente."); }
            catch (AccesoDatosException ex) { Exigir(ex.InnerException is InvalidOperationException, "Actualizar una fila inexistente no comunica un éxito"); }

            var temporal = new Equipo { ClienteId = clienteId, Tipo = "Audífonos", Marca = "Prueba eliminable",
                Modelo = "Solo test", Serie = "BORRAR-TEST-" + token, Condicion = "Dato ficticio para verificar eliminación" };
            int temporalId = await servicio.RegistrarAsync(temporal);
            Exigir((await servicio.ObtenerAsync(temporalId)).Serie == temporal.Serie, "La prueba de eliminación identifica su propio registro ficticio");
            await servicio.EliminarAsync(temporalId);
            Exigir(await servicio.ObtenerAsync(temporalId) == null, "Eliminar funciona solo sobre el equipo ficticio creado para esa prueba");
            try { await repositorio.EliminarAsync(temporalId); throw new Exception("Se confirmó una eliminación inexistente."); }
            catch (AccesoDatosException ex) { Exigir(ex.InnerException is InvalidOperationException, "Eliminar una fila inexistente no comunica un éxito"); }

            using (var hijo = Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,
                "--equipo-leer " + id.ToString(CultureInfo.InvariantCulture) + " " + token) { UseShellExecute = false }))
            {
                if (hijo == null || !hijo.WaitForExit(30000) || hijo.ExitCode != 0)
                    throw new Exception("Falló la lectura persistida en otro proceso. Ver equipo-segundo-proceso.log.");
            }
            Exigir(true, "Otro proceso y conexiones nuevas recuperan el mismo equipo actualizado desde SQL");
            int cantidad = (await servicio.BuscarAsync("")).Count;
            await EjecutarScript(Base, "02-Equipos.sql");
            await EjecutarScript(Base, "04-Equipos-Tipo.sql");
            Exigir((await servicio.BuscarAsync("")).Count == cantidad && (await servicio.ObtenerAsync(id)).Tipo == "Monitor", "Repetir los scripts conserva los registros y su tipo");
            Exigir(await Escalar("SELECT COUNT(*) FROM dbo.Clientes;") == clientesAntes + (listaClientes.Count == 0 ? 1 : 0), "Los clientes anteriores se conservaron");
            string config = Path.Combine(carpeta, "Repair.Wpf.exe.config");
            if (File.Exists(config))
            {
                var xml = new XmlDocument(); xml.Load(config);
                xml.SelectSingleNode("/configuration/connectionStrings/add[@name='RepairSql']/@connectionString").Value = Cadena(Base);
                xml.SelectSingleNode("/configuration/appSettings/add[@key='Modo']/@value").Value = "Sql"; xml.Save(config);
                log.Add("Repair.Wpf.exe de esta carpeta configurado en SQL real con autenticación Windows. Sin contraseñas.");
            }
            File.WriteAllText(Path.Combine(carpeta, "equipo-persistido.txt"), id.ToString(CultureInfo.InvariantCulture) + "\r\n" + token, new UTF8Encoding(false));
            log.Add("EQUIPO CONSERVADO: Id=" + id + ", serie=SQL-" + token + ". Solo se eliminó el registro temporal creado por esta prueba; no se modificaron registros anteriores.");
        }

        private static async Task Leer(int id, string token)
        {
            await ValidarBase();
            var equipo = await new EquipoServicio(new EquipoSqlRepositorio(Cadena(Base))).ObtenerAsync(id);
            Exigir(equipo != null && equipo.Tipo == "Monitor" && equipo.Marca == "Lenovo" && equipo.Modelo == "ThinkPad T14" &&
                equipo.Serie == "SQL-" + token && equipo.Condicion == "Sin imagen · actualizada" && equipo.Accesorios == "Cargador",
                "Proceso independiente recupera todos los campos actualizados, incluido Tipo, desde SQL real");
        }

        private static bool EsSql(Exception ex, params int[] numeros)
        { var sql = ex.InnerException as SqlException; return sql != null && numeros.Contains(sql.Number); }

        private static void ProbarContextoWpf()
        {
            var anterior = SynchronizationContext.Current;
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            try
            {
                var servicio = new EquipoServicio(new RepositorioDiferido());
                var tarea = servicio.RegistrarAsync(new Equipo { ClienteId = 1, Tipo = "Laptop", Marca = "Prueba", Modelo = "Asíncrono", Condicion = "No enciende" });
                Exigir(!tarea.IsCompleted, "El alta devuelve el control a WPF mientras la consulta sigue pendiente");
                var marco = new DispatcherFrame();
                var temporizador = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                temporizador.Tick += (s, e) => marco.Continue = false;
                tarea.ContinueWith(t => dispatcher.BeginInvoke(new Action(() => marco.Continue = false)));
                temporizador.Start(); Dispatcher.PushFrame(marco); temporizador.Stop();
                Exigir(tarea.IsCompleted && tarea.GetAwaiter().GetResult() == 17, "La continuación asíncrona termina en contexto WPF sin bloquear la ventana");
            }
            finally { SynchronizationContext.SetSynchronizationContext(anterior); }
        }
        private sealed class RepositorioDiferido : IEquipoRepositorio
        {
            public async Task<IReadOnlyList<Equipo>> BuscarAsync(string texto) { await Task.Yield(); return new Equipo[0]; }
            public Task<int> AgregarAsync(Equipo e) { return Task.FromResult(17); }
            public Task<Equipo> ObtenerAsync(int id) { return Task.FromResult<Equipo>(null); }
            public Task ActualizarAsync(Equipo e) { return Task.CompletedTask; }
            public Task EliminarAsync(int id) { return Task.CompletedTask; }
        }
        private static void Exigir(bool valor, string mensaje)
        { if (!valor) throw new Exception(mensaje); casos++; log.Add("OK: " + mensaje); }
    }
}
