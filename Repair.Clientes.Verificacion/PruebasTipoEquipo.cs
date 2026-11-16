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
using Repair.Core.Logica;
using Repair.Core.Modelos;
using Repair.Wpf;
using Repair.Wpf.Demostracion;
using Repair.Wpf.Presentacion;

namespace Repair.Clientes.Verificacion
{
    internal static class PruebasTipoEquipo
    {
        private static int casos;
        private static readonly List<string> log = new List<string>();
        public static int Ejecutar(string carpeta)
        {
            try
            {
                Dominio().GetAwaiter().GetResult();
                Controles(carpeta);
                log.Add("RESULTADO: " + casos + " comprobaciones correctas. Demostración en memoria; no acredita persistencia SQL.");
                File.WriteAllLines(Path.Combine(carpeta, "tipo-equipo.log"), log, new UTF8Encoding(true));
                return 0;
            }
            catch (Exception ex)
            {
                log.Add("FALLO: " + ex);
                File.WriteAllLines(Path.Combine(carpeta, "tipo-equipo.log"), log, new UTF8Encoding(true));
                return 1;
            }
        }

        private static Equipo Valido(string tipo = "Laptop")
        { return new Equipo { ClienteId = 1, Tipo = tipo, Marca = "Lenovo", Modelo = "ThinkPad T14", Serie = "TIPO-PRUEBA", Condicion = "No enciende" }; }

        private static async Task Dominio()
        {
            Exigir(TiposEquipo.Opciones.Count == 18 && TiposEquipo.Opciones.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 18 &&
                new[] { "Laptop", "Monitor", "Teléfono", "Audífonos" }.All(TiposEquipo.Opciones.Contains), "Catálogo compartido con 18 opciones predefinidas sin duplicados");
            try { ((IList<string>)TiposEquipo.Opciones).Add("Cambio externo"); throw new Exception("Catálogo modificable."); }
            catch (NotSupportedException) { Exigir(true, "El catálogo no admite modificaciones externas"); }
            var repo = new EquipoMemoriaRepositorio(); var servicio = new EquipoServicio(repo);
            Exigir((await servicio.BuscarAsync("")).All(e => e.Tipo == TiposEquipo.SinEspecificar), "Equipos anteriores permanecen Sin especificar, sin inventar una clasificación");
            foreach (string tipo in new[] { null, "", " ", "Tipo inventado", "Sin especificar" })
            {
                try { await servicio.RegistrarAsync(Valido(tipo)); throw new Exception("Se aceptó un tipo inválido."); }
                catch (ValidacionException ex) { Exigir(ex.Errores.ContainsKey("Tipo") && (await servicio.BuscarAsync("")).Count == 3, "Alta rechaza un tipo ausente o fuera del catálogo sin guardar"); }
            }
            var entrada = Valido("  laptop  "); int id = await servicio.RegistrarAsync(entrada);
            Exigir((await servicio.ObtenerAsync(id)).Tipo == "Laptop" && entrada.Tipo == "  laptop  ", "Alta normaliza el tipo y lo conserva sin modificar la entrada");
            Exigir((await servicio.BuscarAsync("Laptop")).Single().Id == id, "Búsqueda encuentra el tipo de equipo en memoria");
            var equipo = await servicio.ObtenerAsync(id); equipo.Tipo = "Monitor"; await servicio.ActualizarAsync(equipo);
            Exigir((await servicio.ObtenerAsync(id)).Tipo == "Monitor" && (await servicio.BuscarAsync("Monitor")).Any(e => e.Id == id), "Actualizar conserva y permite consultar el nuevo tipo");
            equipo.Tipo = "Audífonos";
            Exigir((await servicio.ObtenerAsync(id)).Tipo == "Monitor", "Una copia externa no modifica el tipo guardado");
            foreach (string tipo in new[] { null, "Sin especificar", "Tipo inventado" })
            {
                equipo.Tipo = tipo;
                try { await servicio.ActualizarAsync(equipo); throw new Exception("Actualización inválida aceptada."); }
                catch (ValidacionException ex) { Exigir(ex.Errores.ContainsKey("Tipo") && (await servicio.ObtenerAsync(id)).Tipo == "Monitor", "Actualizar exige un tipo válido y conserva el registro ante errores"); }
            }
            foreach (string tipo in TiposEquipo.Opciones)
            {
                var e = Valido(tipo); e.Serie = "CAT-" + tipo;
                int nuevo = await servicio.RegistrarAsync(e);
                Exigir((await servicio.ObtenerAsync(nuevo)).Tipo == tipo, "Alta y consulta conservan la opción: " + tipo);
            }
        }

        private static void Controles(string carpeta)
        {
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown }; app.InitializeComponent();
            var clientes = new ClienteServicio(new ClienteMemoriaRepositorio());
            var repo = new EquipoMemoriaRepositorio();
            var control = new EquiposControl(); control.Initialize(new EquipoServicio(repo), clientes);
            control.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            var tipos = Get<ComboBox>(control, "TipoSelector");
            var lista = Get<ListView>(control, "Listado");
            Exigir(tipos.SelectedIndex == -1 && tipos.Items.Cast<string>().SequenceEqual(TiposEquipo.Opciones), "Formulario integrado usa el catálogo y no preselecciona un tipo");
            Exigir(((GridView)lista.View).Columns.Any(c => Equals(c.Header, "Tipo de equipo")), "El listado integrado incluye la columna Tipo de equipo");
            Rellenar(control, "Lenovo", "ThinkPad T14", "UI-TIPO", "No enciende");
            Click(control, "GuardarButton");
            Exigir(Get<TextBlock>(control, "MensajeText").Text.Contains("Selecciona un tipo") && repo.BuscarAsync("").Result.Count == 3, "Formulario muestra la validación de tipo y no guarda sin selección");
            tipos.SelectedItem = "Laptop"; Click(control, "GuardarButton");
            var guardado = repo.BuscarAsync("UI-TIPO").Result.Single();
            Exigir(guardado.Tipo == "Laptop", "Guardar desde el formulario conserva Tipo junto a marca y modelo");
            lista.SelectedItem = lista.Items.Cast<Equipo>().First(e => e.Id == guardado.Id);
            Exigir(Equals(tipos.SelectedItem, "Laptop"), "Seleccionar una fila recupera su tipo para edición");
            tipos.SelectedItem = "Computadora de escritorio"; Click(control, "ActualizarButton");
            Exigir(repo.ObtenerAsync(guardado.Id).Result.Tipo == "Computadora de escritorio", "Actualizar desde el formulario conserva el tipo elegido");
            lista.SelectedItem = lista.Items.Cast<Equipo>().First(e => e.Id == 1);
            Exigir(tipos.SelectedIndex == -1, "Una fila anterior sin tipo requiere elegirlo al editar");

            var repoVentana = new EquipoMemoriaRepositorio();
            var ventana = new EquiposWindow(new EquipoServicio(repoVentana), clientes);
            ventana.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Get<ComboBox>(ventana, "TipoSelector").SelectedItem = "Monitor";
            Get<TextBox>(ventana, "MarcaInput").Text = "Lenovo";
            Get<TextBox>(ventana, "ModeloInput").Text = "ThinkVision";
            Get<TextBox>(ventana, "SerieInput").Text = "VENTANA-TIPO";
            Get<TextBox>(ventana, "CondicionInput").Text = "Sin imagen";
            Get<Button>(ventana, "GuardarButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Exigir(repoVentana.BuscarAsync("VENTANA-TIPO").Result.Single().Tipo == "Monitor", "La ventana separada también guarda y lista el tipo");
            var raizVentana = (FrameworkElement)ventana.Content;
            raizVentana.Measure(new Size(900, 600)); raizVentana.Arrange(new Rect(0, 0, 900, 600)); raizVentana.UpdateLayout();
            var botonVentana = Get<Button>(ventana, "GuardarButton");
            var listaVentana = Get<ListView>(ventana, "Listado");
            Exigir(botonVentana.TranslatePoint(new Point(0, botonVentana.ActualHeight), raizVentana).Y <= listaVentana.TranslatePoint(new Point(0, 0), raizVentana).Y, "La fila nueva no solapa los botones y el listado en la ventana separada");
            ventana.Close();

            foreach (int ancho in new[] { 1380, 760 })
            {
                var principal = new MainWindow(clientes, true, null, false) { Width = ancho, Height = 880 };
                principal.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Get<Button>(principal, "NavDevices").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var equipos = Get<EquiposControl>(principal, "EquiposControl");
                equipos.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Get<ComboBox>(equipos, "TipoSelector").SelectedItem = "Laptop";
                Rellenar(equipos, "Lenovo", "ThinkPad T14", "VISTA-TIPO", "No enciende");
                Click(equipos, "GuardarButton");
                var listadoVista = Get<ListView>(equipos, "Listado");
                listadoVista.SelectedItem = listadoVista.Items.Cast<Equipo>().First(e => e.Serie == "VISTA-TIPO");
                Renderizar(principal, ancho, 880, Path.Combine(carpeta, "Tipo-equipo-" + ancho + ".png"));
                var tipoVista = Get<ComboBox>(equipos, "TipoSelector"); var marcaVista = Get<TextBox>(equipos, "MarcaInput");
                var tipoPunto = tipoVista.TranslatePoint(new Point(0, 0), (FrameworkElement)principal.Content);
                var marcaPunto = marcaVista.TranslatePoint(new Point(0, 0), (FrameworkElement)principal.Content);
                Exigir(tipoPunto.Y < marcaPunto.Y && Math.Abs(tipoPunto.X - marcaPunto.X) < 0.01 && tipoPunto.X + tipoVista.ActualWidth <= ancho, "Tipo aparece antes de Marca, alineado y visible a " + ancho);
                principal.Close();
            }
        }

        private static void Rellenar(EquiposControl c, string marca, string modelo, string serie, string condicion)
        {
            Get<TextBox>(c, "MarcaInput").Text = marca; Get<TextBox>(c, "ModeloInput").Text = modelo;
            Get<TextBox>(c, "SerieInput").Text = serie; Get<TextBox>(c, "CondicionInput").Text = condicion;
        }
        private static T Get<T>(FrameworkElement c, string nombre) where T : class
        { return c.FindName(nombre) as T ?? throw new Exception("No existe " + nombre); }
        private static void Click(EquiposControl c, string nombre)
        { Get<Button>(c, nombre).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
        private static void Renderizar(MainWindow ventana, int ancho, int alto, string archivo)
        {
            var raiz = (FrameworkElement)ventana.Content; raiz.Measure(new Size(ancho, alto));
            raiz.Arrange(new Rect(0, 0, ancho, alto)); raiz.UpdateLayout();
            var mapa = new RenderTargetBitmap(ancho, alto, 96, 96, PixelFormats.Pbgra32); mapa.Render(raiz);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(mapa));
            using (var flujo = File.Create(archivo)) png.Save(flujo);
        }
        private static void Exigir(bool valor, string mensaje)
        { if (!valor) throw new Exception(mensaje); casos++; log.Add("OK: " + mensaje); }
    }
}
