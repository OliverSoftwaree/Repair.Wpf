using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Core.Modelos;
using Repair.Wpf;
using Repair.Wpf.Demostracion;
using Repair.Wpf.Presentacion;

namespace Repair.Clientes.Verificacion
{
    // Herramienta de evidencia, separada de la aplicación y de las comprobaciones de Oliver.
    internal static class Program
    {
        private const string Base = "RepairClientesDesarrollo_20261030";
        private static readonly string Carpeta = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly List<string> Log = new List<string>();
        private static int casos;

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--capturas-entrega") return CapturasEntrega.Ejecutar(args[1]);
            if (args.Length == 0 || args[0] == "--equipos-sql" || args[0] == "--equipo-leer" || args[0] == "--asincronia")
            {
                MessageBox.Show("Para comprobar SQL de la entrega, ejecuta Preparar-Repair.exe desde el paquete. Esta herramienta conserva las pruebas locales --tipo, --csv y --seguimiento.", "Repair · comprobaciones");
                return 2;
            }
            if (args.Length == 1 && args[0] == "--tipo") return PruebasTipoEquipo.Ejecutar(Carpeta);
            if (args.Length == 1 && args[0] == "--seguimiento") return PruebasSeguimiento.Ejecutar(Carpeta);
            bool lectura = args.Length == 3 && args[0] == "--sql-leer";
            bool soloCsv = args.Length == 1 && args[0] == "--csv";
            if (!soloCsv) return 2;
            string archivo = Path.Combine(Carpeta, lectura ? "sql-segundo-proceso.log" : soloCsv ? "csv.log" : "clientes-sql.log");
            try
            {
                if (lectura)
                    Task.Run(() => LeerPersistencia(int.Parse(args[1], CultureInfo.InvariantCulture), args[2])).GetAwaiter().GetResult();
                else
                {
                    ProbarCsv();
                    ProbarVista();
                    if (!soloCsv) Task.Run(() => ProbarSql()).GetAwaiter().GetResult();
                }
                Log.Add("RESULTADO: " + casos + " comprobaciones correctas. " + (soloCsv ? "Sin SQL; renderizado sin sesión nativa." : "No comprueba Equipos ni el recorrido nativo de la ventana principal."));
                File.WriteAllLines(archivo, Log, new UTF8Encoding(true));
                if (!lectura && !soloCsv) MessageBox.Show("Comprobación terminada. Consulta clientes-sql.log junto a esta herramienta.", "Repair · verificación de clientes");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Add("NO APROBADO: " + ex.GetType().Name + ": " + ex.Message);
                for (var causa = ex.InnerException; causa != null; causa = causa.InnerException)
                    Log.Add("Causa: " + causa.GetType().Name + ": " + causa.Message);
                File.WriteAllLines(archivo, Log, new UTF8Encoding(true));
                if (!lectura && !soloCsv) MessageBox.Show("No se completó la comprobación. Consulta clientes-sql.log. No se usan datos de demostración para aprobar SQL.", "Repair · verificación de clientes");
                return 1;
            }
        }

        private static ClienteFila Fila(string nombre, string telefono = "809-555-0199", string correo = "")
        { return new ClienteFila(new Cliente { Id = 7, Nombre = nombre, Telefono = telefono, Correo = correo, CreadoUtc = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc) }); }

        private static void ProbarCsv()
        {
            var fila = Fila("Ángela, \"Prueba\"\r\nSegunda línea");
            string csv = ClienteCsv.Crear(new[] { fila });
            var campos = LeerCsv(csv);
            Exigir(campos.Count == 2 && campos[1].SequenceEqual(new[] { "CL-0007", fila.Nombre, fila.Telefono, "Sin correo" }), "CSV conserva tildes, comas, comillas y saltos al releer cuatro columnas");
            Exigir(ClienteCsv.Crear(new ClienteFila[0]) == "\"Codigo\",\"Nombre\",\"Telefono\",\"Correo\"\r\n", "CSV vacío conserva únicamente la cabecera");
            foreach (string valor in new[] { "=1+1", "+8095550199", "-1+2", "@SUM(1)", "  =1", "\ttexto", "\rtexto", "\ntexto" })
                Exigir(LeerCsv(ClienteCsv.Crear(new[] { Fila(valor) }))[1][1] == "'" + valor, "CSV trata como texto una entrada potencialmente interpretable por una hoja");
            Exigir(LeerCsv(ClienteCsv.Crear(new[] { Fila("Normal", "+1 8095550199", "uno@example.test") }))[1][2] == "'+1 8095550199", "CSV protege también el teléfono con prefijo internacional");
            var cultura = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-DO");
                string primero = ClienteCsv.Crear(new[] { fila });
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Exigir(primero == ClienteCsv.Crear(new[] { fila }), "CSV conserva su delimitador al cambiar la cultura");
            }
            finally { CultureInfo.CurrentCulture = cultura; }
            string ruta = Path.Combine(Carpeta, "clientes-ficticios.csv");
            File.WriteAllText(ruta, csv, new UTF8Encoding(true));
            Exigir(File.ReadAllBytes(ruta).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }) && File.ReadAllText(ruta) == csv, "Archivo real CSV se guarda en UTF-8 con BOM y se recupera íntegro");
        }

        // Lector de prueba independiente: verifica campos y filas, no compara solo el formato emitido.
        private static List<string[]> LeerCsv(string texto)
        {
            var filas = new List<string[]>(); var campos = new List<string>(); var campo = new StringBuilder(); bool comillas = false;
            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];
                if (c == '"')
                {
                    if (comillas && i + 1 < texto.Length && texto[i + 1] == '"') { campo.Append('"'); i++; }
                    else comillas = !comillas;
                }
                else if (c == ',' && !comillas) { campos.Add(campo.ToString()); campo.Clear(); }
                else if (c == '\r' && !comillas && i + 1 < texto.Length && texto[i + 1] == '\n')
                { campos.Add(campo.ToString()); campo.Clear(); filas.Add(campos.ToArray()); campos.Clear(); i++; }
                else campo.Append(c);
            }
            if (comillas || campo.Length != 0 || campos.Count != 0) throw new Exception("CSV incompleto.");
            return filas;
        }

        private static void ProbarVista()
        {
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown }; app.InitializeComponent();
            var servicio = new ClienteServicio(new ClienteMemoriaRepositorio());
            var ventana = new MainWindow(servicio, true, null, false);
            foreach (int ancho in new[] { 1380, 760 })
            {
                ventana.Width = ancho; ventana.Height = 720;
                ventana.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                ((Button)ventana.FindName("NavClients")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var raiz = (FrameworkElement)ventana.Content;
                raiz.Measure(new Size(ancho, 720)); raiz.Arrange(new Rect(0, 0, ancho, 720)); raiz.UpdateLayout();
                var exportar = (Button)ventana.FindName("ExportClientsButton"); var nuevo = (Button)ventana.FindName("NewClientButton");
                var inicio = exportar.TranslatePoint(new Point(0, 0), raiz); var nuevoInicio = nuevo.TranslatePoint(new Point(0, 0), raiz);
                Exigir(exportar.ActualWidth > 90 && exportar.ActualHeight >= 35 && inicio.X >= 0 && inicio.X + exportar.ActualWidth <= nuevoInicio.X && nuevoInicio.X + nuevo.ActualWidth <= ancho, "Acciones de clientes caben sin solaparse a " + ancho);
                var mapa = new RenderTargetBitmap(ancho, 720, 96, 96, PixelFormats.Pbgra32); mapa.Render(raiz);
                var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(mapa));
                using (var flujo = File.Create(Path.Combine(Carpeta, "Clientes-CSV-" + ancho + ".png"))) png.Save(flujo);
            }
            var tabla = (DataGrid)ventana.FindName("ClientsTable");
            ((TextBox)ventana.FindName("SearchInput")).Text = "Mara";
            ((Button)ventana.FindName("SearchButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var filas = tabla.Items.Cast<ClienteFila>().ToArray();
            Exigir(LeerCsv(ClienteCsv.Crear(filas)).Count == filas.Length + 1 && filas.Length == 1, "CSV usa el resultado filtrado de los controles de clientes");
            ((TextBox)ventana.FindName("SearchInput")).Text = "sin aplicar";
            Exigir(LeerCsv(ClienteCsv.Crear(tabla.Items.Cast<ClienteFila>()))[1][1] == filas[0].Nombre, "Texto aún no buscado no altera la instantánea visible");
            ((TextBox)ventana.FindName("SearchInput")).Text = "Inexistente";
            ((Button)ventana.FindName("SearchButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            ((Button)ventana.FindName("ExportClientsButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Exigir(((TextBlock)ventana.FindName("ToastText")).Text.Contains("No hay clientes"), "Exportar un resultado vacío avisa sin abrir un diálogo ni escribir archivos");
            ventana.Close();
        }

        private static string Cadena(string baseDatos)
        {
            string archivo = Path.Combine(Carpeta, "SqlServidor.local.txt");
            string servidor = File.Exists(archivo) ? File.ReadAllText(archivo).Trim() : Environment.MachineName;
            return new SqlConnectionStringBuilder { DataSource = servidor, InitialCatalog = baseDatos, IntegratedSecurity = true, ConnectTimeout = 5, ApplicationName = "Repair.Clientes.Verificacion" }.ConnectionString;
        }

        private static async Task ProbarSql()
        {
            using (var conexion = new SqlConnection(Cadena("master")))
            {
                await conexion.OpenAsync();
                Log.Add("SQL REAL: conexión Windows establecida; versión " + conexion.ServerVersion + ". Base exclusiva: " + Base);
                using (var comando = new SqlCommand(File.ReadAllText(Path.Combine(Carpeta, "03-Clientes-Desarrollo.sql")), conexion))
                { comando.CommandTimeout = 30; await comando.ExecuteNonQueryAsync(); }
            }
            await ValidarBase();
            var servicio = new ClienteServicio(new ClienteSqlRepositorio(Cadena(Base)));
            string token = Guid.NewGuid().ToString("N");
            string nombre = "Cliente verificación Repair " + token;
            int id = await servicio.RegistrarAsync(new Cliente { Nombre = nombre, Telefono = "809-555-0199", Correo = "", Notas = "Verificación ficticia " + token });
            Exigir(id > 0 && (await servicio.ObtenerAsync(id)).Nombre == nombre, "ClienteServicio y ClienteSqlRepositorio registran y consultan una fila real");
            Exigir((await servicio.BuscarAsync(token)).Any(c => c.Id == id), "Búsqueda SQL real encuentra el registro por su marcador único");
            using (var hijo = Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "--sql-leer " + id.ToString(CultureInfo.InvariantCulture) + " " + token) { UseShellExecute = false }))
            {
                if (hijo == null || !hijo.WaitForExit(30000) || hijo.ExitCode != 0) throw new Exception("No se aprobó la lectura desde un segundo proceso. Consulta sql-segundo-proceso.log.");
            }
            Exigir(true, "Un segundo proceso abre conexiones nuevas y recupera el mismo cliente persistido");
            string configuracion = Path.GetFullPath(Path.Combine(Carpeta, "..", "..", "..", "Repair.Wpf", "bin", "Release", "Repair.Wpf.exe.config"));
            var xml = new XmlDocument(); xml.Load(configuracion);
            xml.SelectSingleNode("/configuration/connectionStrings/add[@name='RepairSql']/@connectionString").Value = Cadena(Base);
            xml.SelectSingleNode("/configuration/appSettings/add[@key='Modo']/@value").Value = "Sql";
            xml.Save(configuracion);
            Log.Add("CONFIGURACIÓN LOCAL: ejecutable WPF Release configurado para la base exclusiva, sin argumentos demo. App.config del repositorio permanece intacto.");
            Log.Add("REGISTRO CONSERVADO: Id=" + id + "; marcador=" + token + ". No se eliminó ni reinicializó ningún dato.");
        }

        private static async Task ValidarBase()
        {
            using (var conexion = new SqlConnection(Cadena(Base)))
            {
                await conexion.OpenAsync();
                using (var comando = new SqlCommand("SELECT COUNT(*) FROM sys.extended_properties WHERE class=0 AND name=N'RepairAvance' AND CONVERT(nvarchar(100),value)=N'Clientes-20261030';", conexion))
                    Exigir(Convert.ToInt32(await comando.ExecuteScalarAsync()) == 1, "La base SQL posee la marca exclusiva de este avance");
                using (var comando = new SqlCommand("SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Clientes') AND ((name=N'Id' AND TYPE_NAME(user_type_id)=N'int' AND is_identity=1 AND is_nullable=0) OR (name=N'Nombre' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=200 AND is_nullable=0) OR (name=N'Telefono' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=50 AND is_nullable=0) OR (name=N'Correo' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=300 AND is_nullable=1) OR (name=N'Notas' AND TYPE_NAME(user_type_id)=N'nvarchar' AND max_length=600 AND is_nullable=1) OR (name=N'CreadoUtc' AND TYPE_NAME(user_type_id)=N'datetime2' AND scale=0 AND is_nullable=0 AND default_object_id<>0));", conexion))
                    Exigir(Convert.ToInt32(await comando.ExecuteScalarAsync()) == 6, "Las seis columnas SQL son compatibles con el repositorio existente");
            }
        }

        private static async Task LeerPersistencia(int id, string token)
        {
            await ValidarBase();
            var servicio = new ClienteServicio(new ClienteSqlRepositorio(Cadena(Base)));
            var cliente = await servicio.ObtenerAsync(id);
            Exigir(cliente != null && cliente.Nombre == "Cliente verificación Repair " + token && cliente.Notas == "Verificación ficticia " + token && cliente.Telefono == "809-555-0199" && cliente.CreadoUtc.Kind == DateTimeKind.Utc, "Proceso independiente recupera todos los campos esperados desde SQL");
            Exigir((await servicio.BuscarAsync(token)).Any(c => c.Id == id), "Proceso independiente confirma la búsqueda persistida");
        }

        private static void Exigir(bool condicion, string mensaje)
        { if (!condicion) throw new Exception(mensaje); casos++; Log.Add("OK: " + mensaje); }
    }
}
