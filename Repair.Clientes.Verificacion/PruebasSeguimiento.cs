using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Repair.Core.Datos;
using Repair.Core.Integracion;
using Repair.Core.Logica;
using Repair.Core.Modelos;
using Repair.Wpf;
using Repair.Wpf.Demostracion;

namespace Repair.Clientes.Verificacion
{
    internal static class PruebasSeguimiento
    {
        internal static int Ejecutar(string carpeta)
        {
            var log = new List<string>();
            try
            {
                ProbarRuta(carpeta, log);
                log.Add($"RESULTADO: {log.Count} comprobaciones correctas con proveedor ficticio. La prueba de interfaz no usa SQL Server.");
                return 0;
            }
            catch (Exception ex)
            {
                log.Add("NO APROBADO: " + ex);
                return 1;
            }
            finally
            {
                File.WriteAllLines(Path.Combine(carpeta, "seguimiento-integrado.log"), log, new UTF8Encoding(true));
            }
        }

        private static void ProbarRuta(string carpeta, List<string> log)
        {
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            var origen = new ConsultaFicticia();
            var ventana = new MainWindow(new ClienteServicio(new ClienteMemoriaRepositorio()), true, null, false, origen);
            var pagina = Elemento<FrameworkElement>(ventana, "SeguimientoPage");
            var raiz = (FrameworkElement)ventana.Content;
            var nav = Elemento<Button>(ventana, "NavSeguimiento");
            var buscar = Elemento<Button>(pagina, "BuscarOrden");
            var numero = Elemento<TextBox>(pagina, "NumeroOrden");
            var resultado = Elemento<Border>(pagina, "ResultadoOrden");
            var aviso = Elemento<TextBlock>(pagina, "MensajeConsulta");

            Exigir(pagina.Visibility == Visibility.Collapsed, "Repair abre en Inicio antes de usar Seguimiento", log);
            Pulsar(nav);
            Exigir(pagina.Visibility == Visibility.Visible && nav.Tag as string == "Active" &&
                Elemento<FrameworkElement>(ventana, "HomePage").Visibility == Visibility.Collapsed &&
                Elemento<TextBlock>(ventana, "Breadcrumb").Text == "SEGUIMIENTO", "El botón de navegación abre Seguimiento en la misma ventana", log);

            numero.Text = " 31 "; Pulsar(buscar);
            Exigir(origen.Ids.SequenceEqual(new[] { 31 }) && resultado.Visibility == Visibility.Visible &&
                Elemento<TextBlock>(pagina, "TituloOrden").Text == "Orden 31" &&
                Elemento<TextBlock>(pagina, "ClienteOrden").Text == "Cliente ficticio", "Consulta por Id y muestra la orden solicitada", log);
            Exigir(Elemento<TextBlock>(pagina, "EquipoOrden").Text == "Laptop de prueba" &&
                Elemento<TextBlock>(pagina, "DiagnosticoOrden").Text == "Ventilador averiado" &&
                Elemento<TextBlock>(pagina, "FechaEntrega").Text.StartsWith("Entrega registrada"), "Muestra diagnóstico, equipo y fecha de entrega", log);
            Exigir(typeof(IConsultaSeguimiento).GetMethods().Single().Name == "BuscarPorIdAsync" &&
                Botones(pagina).SequenceEqual(new[] { buscar }), "La interfaz de datos y la pantalla ofrecen solo consulta", log);

            foreach (int ancho in new[] { 1380, 760 })
            {
                ventana.Width = ancho;
                ventana.Height = 720;
                ventana.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var tamano = new Size(ancho, 720);
                raiz.Measure(tamano);
                raiz.Arrange(new Rect(tamano));
                raiz.UpdateLayout();
                var esquina = buscar.TranslatePoint(new Point(), raiz);
                Exigir(buscar.ActualWidth > 90 && numero.ActualWidth > 90 && esquina.X + buscar.ActualWidth <= ancho,
                    "La consulta cabe en una ventana de " + ancho, log);
                var imagen = new RenderTargetBitmap(ancho, 720, 96, 96, PixelFormats.Pbgra32);
                imagen.Render(raiz);
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(imagen));
                using (var archivo = File.Create(Path.Combine(carpeta, "Seguimiento-integrado-" + ancho + ".png"))) png.Save(archivo);
            }

            numero.Text = "32"; Pulsar(buscar);
            Exigir(Elemento<TextBlock>(pagina, "EquipoOrden").Text == "Sin equipo vinculado" &&
                Elemento<TextBlock>(pagina, "FechaEntrega").Visibility == Visibility.Collapsed, "Una orden antigua no toma prestado un equipo del cliente", log);
            numero.Text = "0"; Pulsar(buscar);
            Exigir(origen.Ids.SequenceEqual(new[] { 31, 32 }) && resultado.Visibility == Visibility.Collapsed &&
                aviso.Text.Contains("mayor que cero"), "Un número inválido oculta la consulta previa y no se envía al origen", log);
            numero.Text = "999"; Pulsar(buscar);
            Exigir(resultado.Visibility == Visibility.Collapsed && aviso.Text.Contains("No encontramos"), "Informa que no existe la orden", log);
            numero.Text = "503"; Pulsar(buscar);
            Exigir(resultado.Visibility == Visibility.Collapsed && aviso.Text.Contains("No se pudo consultar") && buscar.IsEnabled,
                "Un error de datos se muestra y permite reintentar sin usar demostración como sustituto", log);

            Pulsar(Elemento<Button>(ventana, "NavClients"));
            Exigir(pagina.Visibility == Visibility.Collapsed && Elemento<Button>(ventana, "ExportClientsButton").Visibility == Visibility.Visible,
                "Se puede volver a Clientes y acceder a Exportar CSV", log);
            Pulsar(Elemento<Button>(ventana, "NavDevices"));
            Exigir(Elemento<FrameworkElement>(ventana, "DevicesPage").Visibility == Visibility.Visible,
                "Equipos sigue accesible desde la misma navegación", log);
            ventana.Close();
        }

        private static IEnumerable<Button> Botones(DependencyObject nodo)
        {
            if (nodo is Button boton) yield return boton;
            foreach (var hijo in LogicalTreeHelper.GetChildren(nodo).OfType<DependencyObject>())
                foreach (var descendiente in Botones(hijo)) yield return descendiente;
        }

        private static T Elemento<T>(FrameworkElement contenedor, string nombre) where T : FrameworkElement => (T)contenedor.FindName(nombre);
        private static void Pulsar(Button boton) => boton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        private static void Exigir(bool condicion, string mensaje, List<string> log)
        {
            if (!condicion) throw new InvalidOperationException(mensaje);
            log.Add("OK · " + mensaje);
        }

        private sealed class ConsultaFicticia : IConsultaSeguimiento
        {
            internal List<int> Ids { get; } = new List<int>();

            public Task<OrdenSeguimiento> BuscarPorIdAsync(int id)
            {
                Ids.Add(id);
                if (id == 503) return Task.FromException<OrdenSeguimiento>(new AccesoDatosException(new InvalidOperationException()));
                if (id == 31) return Task.FromResult(new OrdenSeguimiento(id, 5, "Cliente ficticio", 8, "Laptop de prueba",
                    "Entregada", "Ventilador averiado", "Se cambió el ventilador", new DateTime(2026, 11, 15, 14, 38, 22, DateTimeKind.Utc)));
                return Task.FromResult(id == 32 ? new OrdenSeguimiento(id, 5, "Cliente ficticio", null, null, "En diagnóstico", null, null, null) : null);
            }
        }
    }
}
