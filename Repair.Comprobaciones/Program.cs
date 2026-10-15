using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Core.Modelos;
using Repair.Wpf.Presentacion;
using Repair.Wpf.Demostracion;
namespace Repair.Comprobaciones
{
    internal static class Program
    {
        private static int correctas;
        private static int fallidas;
        [STAThread]
        private static int Main(string[] args)
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);
            if (args.Length == 2 && args[0] == "--visual") return PruebasVisuales.Ejecutar(args[1]);
            if (args.Length == 1 && args[0] == "--sql-probe") return ProbarSqlAsync().GetAwaiter().GetResult();
            EjecutarAsync().GetAwaiter().GetResult();
            InterfazLogicaAsync().GetAwaiter().GetResult();
            EjecutarEquiposAsync().GetAwaiter().GetResult();
            Console.WriteLine("RESULTADO: " + correctas + " correctas; " + fallidas + " fallidas. Lógica y reglas de presentación; no acredita renderizado ni persistencia SQL.");
            return fallidas == 0 ? 0 : 1;
        }
        private static async Task InterfazLogicaAsync()
        {
            await Caso("Diseño a 760: menú compacto, tarjetas y bloques apilados", () =>
            {
                var d = DisposicionAdaptable.Para(760);
                Exigir(d.MenuCompacto && d.ListaCompacta && !d.InicioEnColumnas && !d.MostrarIlustracion, "Disposición mínima incorrecta.");
                return Task.CompletedTask;
            });
            await Caso("Cambios de diseño en 1100, 1180 y 1280", () =>
            {
                Exigir(!DisposicionAdaptable.Para(1100).MenuCompacto && DisposicionAdaptable.Para(1100).ListaCompacta, "Menú intermedio incorrecto.");
                Exigir(!DisposicionAdaptable.Para(1180).ListaCompacta && DisposicionAdaptable.Para(1280).MostrarIlustracion, "Tabla o ilustración incorrecta.");
                return Task.CompletedTask;
            });
            await Caso("Panel lateral cabe en todos los anchos admitidos", () =>
            {
                foreach (double ancho in new double[] { 760, 900, 1100, 1380 })
                { var d = DisposicionAdaptable.Para(ancho); Exigir(d.AnchoPanel <= ancho - 80 && d.AnchoPanel >= 340, "Panel desbordado."); }
                return Task.CompletedTask;
            });
            await Caso("Presentación de código, inicial y correo ausente", () =>
            {
                var fila = new ClienteFila(new Cliente { Id = 7, Nombre = "Ángel", Telefono = "8095550100", CreadoUtc = DateTime.UtcNow });
                Exigir(fila.Codigo == "CL-0007" && fila.Inicial == "Á" && fila.Correo == "Sin correo", "Presentación incorrecta.");
                return Task.CompletedTask;
            });
            await Caso("Demostración: tres personas ficticias y copias aisladas", async () =>
            {
                var repo = new ClienteMemoriaRepositorio(); var datos = await repo.BuscarAsync("");
                Exigir(datos.Count == 3, "Semilla incorrecta."); int id = datos[0].Id; string nombre = datos[0].Nombre;
                datos[0].Nombre = "Cambio externo"; Exigir((await repo.ObtenerAsync(id)).Nombre == nombre, "La consulta modificó el repositorio.");
            });
            await Caso("Demostración: alta, búsqueda literal y consulta por ID", async () =>
            {
                var s = new ClienteServicio(new ClienteMemoriaRepositorio()); var c = Valido(); c.Nombre = "Cliente %_[]~ de prueba";
                int id = await s.RegistrarAsync(c); Exigir(id == 4, "ID de muestra incorrecto.");
                Exigir((await s.BuscarAsync("%_[]~")).Count == 1 && (await s.ObtenerAsync(id)).Nombre == c.Nombre, "Recorrido de muestra incorrecto.");
            });
        }
        private static Cliente Valido()
        { return new Cliente { Nombre = "  Cliente de prueba  ", Telefono = "  809-555-0100  ", Correo = "  prueba@example.test  ", Notas = "  Equipo académico  " }; }

        // ---------- Pruebas para EquipoServicio ----------
        private sealed class RepoEquipoPrueba : Repair.Core.Datos.IEquipoRepositorio
        {
            internal int Altas { get; private set; }
            public Task<int> AgregarAsync(Repair.Core.Modelos.Equipo equipo)
            {
                Altas++; return Task.FromResult(Altas);
            }
            public Task<System.Collections.Generic.IReadOnlyList<Repair.Core.Modelos.Equipo>> BuscarAsync(string texto) => Task.FromResult((System.Collections.Generic.IReadOnlyList<Repair.Core.Modelos.Equipo>)new System.Collections.Generic.List<Repair.Core.Modelos.Equipo>());
            public Task<Repair.Core.Modelos.Equipo> ObtenerAsync(int id) => Task.FromResult<Repair.Core.Modelos.Equipo>(null);
            public Task ActualizarAsync(Repair.Core.Modelos.Equipo equipo) => Task.CompletedTask;
            public Task EliminarAsync(int id) => Task.CompletedTask;
        }

        private static async Task RechazaEquipo(string nombre, System.Action<Repair.Core.Modelos.Equipo> cambio, string campo)
        {
            await Caso(nombre, async () =>
            {
                var repo = new RepoEquipoPrueba(); var servicio = new Repair.Core.Logica.EquipoServicio(repo); var e = new Repair.Core.Modelos.Equipo { ClienteId = 1, Marca = "M", Modelo = "Mod", Condicion = "Bueno" };
                cambio(e);
                try { await servicio.RegistrarAsync(e); throw new Exception("Se aceptó una entrada inválida."); }
                catch (Repair.Core.Logica.ValidacionException ex) { Exigir(ex.Errores.ContainsKey(campo), "No señaló el campo."); }
                Exigir(repo.Altas == 0, "La validación alcanzó el repositorio.");
            });
        }

        private static async Task EjecutarEquiposAsync()
        {
            await Caso("Equipos: alta válida y delega en repo", async () =>
            {
                var repo = new RepoEquipoPrueba(); var servicio = new Repair.Core.Logica.EquipoServicio(repo);
                var e = new Repair.Core.Modelos.Equipo { ClienteId = 1, Marca = "MarcaX", Modelo = "ModeloY", Condicion = "Funciona correctamente" };
                int id = await servicio.RegistrarAsync(e); Exigir(id == 1, "ID inválido o no delegó"); Exigir(repo.Altas == 1, "No delegó en el repositorio");
            });
            await RechazaEquipo("ClienteId inválido", c => c.ClienteId = 0, "ClienteId");
            await RechazaEquipo("Marca vacía", c => c.Marca = "", "Marca");
            await RechazaEquipo("Modelo vacío", c => c.Modelo = "", "Modelo");
            await RechazaEquipo("Condición corta", c => c.Condicion = "Ok", "Condicion");
        }

        private static async Task Caso(string nombre, Func<Task> accion)
        {
            try { await accion(); correctas++; Console.WriteLine("OK: " + nombre); }
            catch (Exception ex) { fallidas++; Console.WriteLine("FALLO: " + nombre + " - " + ex.Message); }
        }
        private static void Exigir(bool condicion, string mensaje)
        { if (!condicion) throw new Exception(mensaje); }

        private static async Task Rechaza(string nombre, Action<Cliente> cambio, string campo)
        {
            await Caso(nombre, async () =>
            {
                var repo = new RepositorioPrueba(); var servicio = new ClienteServicio(repo); var c = Valido(); cambio(c);
                try { await servicio.RegistrarAsync(c); throw new Exception("Se aceptó una entrada inválida."); }
                catch (ValidacionException ex) { Exigir(ex.Errores.ContainsKey(campo), "No señaló el campo."); }
                Exigir(repo.Altas == 0, "La validación alcanzó el repositorio.");
            });
        }

        private static async Task EjecutarAsync()
        {
            await Caso("Alta válida y normalización sin modificar la entrada", async () =>
            {
                var servicio = new ClienteServicio(new RepositorioPrueba()); var entrada = Valido();
                int id = await servicio.RegistrarAsync(entrada); var c = await servicio.ObtenerAsync(id);
                Exigir(id == 1 && c.Nombre == "Cliente de prueba" && c.Telefono == "809-555-0100" && c.Correo == "prueba@example.test" && c.Notas == "Equipo académico", "No normalizó.");
                Exigir(entrada.Nombre.StartsWith("  ") && entrada.Id == 0, "Modificó la entrada.");
            });
            await Rechaza("Nombre vacío", c => c.Nombre = "   ", "Nombre");
            await Rechaza("Nombre de un carácter", c => c.Nombre = "A", "Nombre");
            await Rechaza("Nombre mayor de 100", c => c.Nombre = new string('A', 101), "Nombre");
            await Rechaza("Nombre sin letras", c => c.Nombre = "1234", "Nombre");
            await Rechaza("Nombre con salto de línea", c => c.Nombre = "Ana\nPrueba", "Nombre");
            await Rechaza("Teléfono vacío", c => c.Telefono = null, "Telefono");
            await Rechaza("Teléfono con letras", c => c.Telefono = "809ABC0100", "Telefono");
            await Rechaza("Teléfono con menos de 7 dígitos", c => c.Telefono = "555010", "Telefono");
            await Rechaza("Teléfono con más de 15 dígitos", c => c.Telefono = "1234567890123456", "Telefono");
            await Rechaza("Teléfono mayor de 25 caracteres", c => c.Telefono = "1 ( 2 ) ( 3 ) ( 4 ) ( 5 ) ( 6 ) 7", "Telefono");
            await Rechaza("Correo inválido", c => c.Correo = "no-es-correo", "Correo");
            await Rechaza("Correo con nombre de presentación", c => c.Correo = "Ana <ana@example.test>", "Correo");
            await Rechaza("Correo mayor de 150", c => c.Correo = new string('a', 140) + "@example.test", "Correo");
            await Rechaza("Notas mayores de 300", c => c.Notas = new string('n', 301), "Notas");
            await Caso("Correo y notas opcionales", async () =>
            {
                var s = new ClienteServicio(new RepositorioPrueba()); var c = Valido(); c.Correo = null; c.Notas = null;
                var id = await s.RegistrarAsync(c); var ficha = await s.ObtenerAsync(id);
                Exigir(ficha.Correo == "" && ficha.Notas == "", "Opcionales incorrectos.");
            });
            await Caso("Límites válidos y nombre con tilde/apóstrofo", async () =>
            {
                var s = new ClienteServicio(new RepositorioPrueba()); var c = Valido();
                c.Nombre = "Ángel O'Prueba".PadRight(100, 'a'); c.Telefono = "+123456789012345"; c.Notas = new string('n', 300);
                c.Correo = new string('a', 64) + "@" + new string('b', 63) + "." + new string('c', 21);
                Exigir(c.Correo.Length == 150, "Preparación del límite incorrecta.");
                Exigir(await s.RegistrarAsync(c) == 1, "Rechazó el límite válido.");
            });
            await Caso("Lista vacía y búsqueda nula", async () =>
            { var s = new ClienteServicio(new RepositorioPrueba()); Exigir((await s.BuscarAsync(null)).Count == 0, "No inició vacío."); });
            await Caso("Búsqueda por código, nombre, teléfono, correo y sin coincidencias", async () =>
            {
                var s = new ClienteServicio(new RepositorioPrueba()); await s.RegistrarAsync(Valido());
                foreach (var texto in new[] { "1", "CLIENTE", "555", "example.test" }) Exigir((await s.BuscarAsync(texto)).Count == 1, "Búsqueda falló: " + texto);
                Exigir((await s.BuscarAsync("ausente")).Count == 0, "Inventó coincidencias.");
            });
            await Caso("Limpieza y límite de búsqueda; texto especial llega como dato", async () =>
            {
                var repo = new RepositorioPrueba(); var s = new ClienteServicio(repo);
                await s.BuscarAsync("  O'Prueba %_[]~  "); Exigir(repo.UltimaBusqueda == "O'Prueba %_[]~", "Cambió el texto.");
                await s.BuscarAsync(new string('x', 100));
                try { await s.BuscarAsync(new string('x', 101)); throw new Exception("Aceptó búsqueda larga."); } catch (ArgumentException) { }
            });
            await Caso("Consulta inexistente y código inválido", async () =>
            {
                var s = new ClienteServicio(new RepositorioPrueba()); Exigir(await s.ObtenerAsync(99) == null, "Inventó cliente.");
                try { await s.ObtenerAsync(0); throw new Exception("Aceptó ID cero."); } catch (ArgumentOutOfRangeException) { }
            });
            await Caso("Fallo del repositorio no devuelve un alta exitosa", async () =>
            {
                var repo = new RepositorioPrueba { Fallar = true }; var s = new ClienteServicio(repo);
                try { await s.RegistrarAsync(Valido()); throw new Exception("Simuló un éxito."); } catch (AccesoDatosException) { }
                Exigir(repo.Altas == 0, "Contó un alta fallida.");
            });
        }

        private static async Task<int> ProbarSqlAsync()
        {
            Console.WriteLine("Sondeo de solo lectura: master. No crea ni modifica bases.");
            bool alguna = false;
            foreach (string servidor in new[] { ".", @".\SQLEXPRESS", "tcp:localhost,1433" })
            {
                var cadena = new SqlConnectionStringBuilder { DataSource = servidor, InitialCatalog = "master", IntegratedSecurity = true, ConnectTimeout = 3, ApplicationName = "Repair.Comprobaciones" };
                try
                {
                    using (var c = new SqlConnection(cadena.ConnectionString))
                    using (var cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128));", c))
                    { await c.OpenAsync(); Console.WriteLine("CONECTADO " + servidor + ": " + await cmd.ExecuteScalarAsync()); alguna = true; }
                }
                catch (SqlException ex) { Console.WriteLine("NO COMPROBADO " + servidor + ": SqlException " + ex.Number); }
                catch (TypeInitializationException) { Console.WriteLine("NO COMPROBADO " + servidor + ": el entorno restringido impide inicializar SqlClient (contadores de rendimiento)."); }
            }
            Console.WriteLine("Esto no comprueba aún el esquema ni el alta/listado real de RepairAcademico.");
            return alguna ? 0 : 1;
        }
    }
}
