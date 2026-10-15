using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Repair.Core.Logica;
using Repair.Core.Datos;
using Repair.Core.Modelos;
using Repair.Wpf;
using Repair.Wpf.Demostracion;

namespace Repair.Comprobaciones
{
    // Prueba dentro del proceso: usa el renderizador WPF y controles reales, sin automatizar
    // ventanas ajenas. Las imágenes no son capturas del escritorio ni prueban FPS o foco.
    internal static class PruebasVisuales
    {
        private static int casos;
        public static int Ejecutar(string directorio)
        {
            try
            {
                Directory.CreateDirectory(directorio);
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.InitializeComponent();
                var ventana = new MainWindow(new ClienteServicio(new ClienteMemoriaRepositorio()), true, null, false);
                ventana.Width = 1380; ventana.Height = 880;
                ventana.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Exigir(Get<TextBlock>(ventana, "HomeCount").Text == "03", "Inicio refleja tres personas ficticias");
                Renderizar(ventana, 1380, 880, Path.Combine(directorio, "01-Inicio-1380.png"));

                Click(ventana, "NavClients");
                Exigir(Get<DataGrid>(ventana, "ClientsTable").Items.Count == 3 && Get<Border>(ventana, "TableContainer").Visibility == Visibility.Visible, "Directorio ancho muestra tabla y tres filas");
                Renderizar(ventana, 1380, 880, Path.Combine(directorio, "02-Clientes-1380.png"));
                var columnas = Get<DataGrid>(ventana, "ClientsTable");
                Exigir(columnas.Columns.Sum(c => c.ActualWidth) >= columnas.ActualWidth * 0.95, "Las columnas aprovechan el ancho disponible");
                Click(ventana, "NewClientButton"); Click(ventana, "SaveButton");
                Exigir(Get<TextBlock>(ventana, "NameError").Text.Length > 0 && Get<TextBlock>(ventana, "PhoneError").Text.Length > 0 && Get<DataGrid>(ventana, "ClientsTable").Items.Count == 3, "Alta inválida muestra errores y no agrega una fila");
                Renderizar(ventana, 1380, 880, Path.Combine(directorio, "03-Validaciones-1380.png"));
                Exigir(Get<TextBlock>(ventana, "PhoneError").ActualHeight > 20, "Los mensajes largos se ajustan a varias líneas");

                Get<TextBox>(ventana, "NameInput").Text = "Lucía de Prueba";
                Get<TextBox>(ventana, "PhoneInput").Text = "809-555-0110";
                Get<TextBox>(ventana, "EmailInput").Text = "lucia@example.test";
                Get<TextBox>(ventana, "NotesInput").Text = "Registro ficticio creado durante la comprobación WPF.";
                Click(ventana, "SaveButton");
                Exigir(Get<DataGrid>(ventana, "ClientsTable").Items.Count == 4 && Get<Grid>(ventana, "DrawerOverlay").Visibility == Visibility.Collapsed, "Alta válida desde controles actualiza directorio y cierra panel");
                Get<TextBox>(ventana, "SearchInput").Text = "Lucía"; Click(ventana, "SearchButton");
                Exigir(Get<DataGrid>(ventana, "ClientsTable").Items.Count == 1, "Búsqueda desde controles encuentra el alta");

                var tabla = Get<DataGrid>(ventana, "ClientsTable");
                ((FrameworkElement)ventana.Content).UpdateLayout();
                var abrirFicha = Botones(tabla).First(b => b.DataContext is Repair.Wpf.Presentacion.ClienteFila && Equals(b.Content, "Ver ficha →"));
                abrirFicha.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Exigir(Get<StackPanel>(ventana, "DetailFields").Visibility == Visibility.Visible && Get<TextBlock>(ventana, "DetailName").Text == "Lucía de Prueba", "Consulta recupera la ficha del cliente seleccionado");
                Renderizar(ventana, 1380, 880, Path.Combine(directorio, "04-Ficha-1380.png"));
                Click(ventana, "CloseDrawerButton"); Click(ventana, "ResetSearchButton");
                Renderizar(ventana, 900, 760, Path.Combine(directorio, "05-Clientes-900.png"));
                Exigir(Get<ListBox>(ventana, "ClientsCards").Visibility == Visibility.Visible && Get<ColumnDefinition>(ventana, "SidebarColumn").Width.Value == 80, "A 900 usa tarjetas y navegación compacta");
                Renderizar(ventana, 760, 620, Path.Combine(directorio, "06-Clientes-760.png"));
                Click(ventana, "NewClientButton");
                Renderizar(ventana, 760, 620, Path.Combine(directorio, "07-Alta-760.png"));
                Exigir(Get<Border>(ventana, "DrawerPanel").ActualWidth <= 760 - 80 && Get<Button>(ventana, "SaveButton").ActualHeight >= 40, "Panel de alta cabe a 760 y conserva la acción de guardar");
                Click(ventana, "CloseDrawerButton"); Click(ventana, "NavHome");
                Renderizar(ventana, 760, 620, Path.Combine(directorio, "08-Inicio-760.png"));
                Exigir(Grid.GetRow(Get<Border>(ventana, "DirectoryCard")) == 1, "Inicio compacto apila sus tarjetas");

                Get<TextBox>(ventana, "SearchInput").Text = "Cliente inexistente"; Click(ventana, "NavClients"); Click(ventana, "SearchButton");
                Exigir(Get<Border>(ventana, "EmptyState").Visibility == Visibility.Visible && Get<TextBlock>(ventana, "EmptyTitle").Text.Contains("Sin coincidencias"), "Búsqueda sin resultados presenta un estado vacío explícito");
                Renderizar(ventana, 900, 760, Path.Combine(directorio, "09-Sin-coincidencias.png"));
                Click(ventana, "NavDevices");
                Exigir(Get<Grid>(ventana, "DevicesPage").Visibility == Visibility.Visible, "Equipos abre únicamente su pantalla de módulo pendiente");
                Renderizar(ventana, 1100, 780, Path.Combine(directorio, "10-Equipos-pendiente.png"));
                var sinConexion = new MainWindow(new ClienteServicio(new RepositorioSinConexion()), false, null, false);
                sinConexion.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent)); Click(sinConexion, "NavClients");
                Exigir(Get<TextBlock>(sinConexion, "HomeCount").Text == "—" && Get<TextBlock>(sinConexion, "EmptyTitle").Text.Contains("Reconectemos"), "Un fallo de datos no se presenta como un directorio vacío exitoso");
                Renderizar(sinConexion, 1100, 780, Path.Combine(directorio, "11-Error-de-conexion.png"));
                Click(sinConexion, "NewClientButton");
                Get<TextBox>(sinConexion, "NameInput").Text = "Cliente de prueba"; Get<TextBox>(sinConexion, "PhoneInput").Text = "809-555-0110";
                Click(sinConexion, "SaveButton");
                Exigir(Get<Grid>(sinConexion, "DrawerOverlay").Visibility == Visibility.Visible && Get<TextBox>(sinConexion, "NameInput").Text == "Cliente de prueba" && Get<TextBlock>(sinConexion, "EditorNoticeText").Text.Contains("No se confirmó"), "Un guardado fallido conserva el editor y muestra la falta de confirmación");
                Renderizar(sinConexion, 1100, 780, Path.Combine(directorio, "12-Guardado-no-confirmado.png"));
                Console.WriteLine("RESULTADO WPF: " + casos + " comprobaciones correctas; 12 vistas renderizadas. Sin SQL, sin medir FPS, sin prueba del diseñador ni del foco nativo.");
                app.Shutdown();
                return 0;
            }
            catch (Exception ex) { Console.WriteLine("FALLO WPF: " + ex); return 1; }
        }

        private static T Get<T>(MainWindow ventana, string nombre) where T : class
        { return ventana.FindName(nombre) as T ?? throw new InvalidOperationException("No existe " + nombre); }
        private static void Click(MainWindow ventana, string nombre)
        { Get<Button>(ventana, nombre).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Drenar(); }
        private static void Exigir(bool condicion, string nombre)
        { if (!condicion) throw new InvalidOperationException(nombre); casos++; Console.WriteLine("OK WPF: " + nombre); }
        private static void Drenar()
        {
            // Las operaciones del repositorio temporal se completan sincrónicamente.
            // No se inicia un bucle de presentación ni se evalúan animaciones o foco nativo.
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Send, new Action(() => { }));
        }
        private static System.Collections.Generic.IEnumerable<Button> Botones(DependencyObject raiz)
        {
            if (raiz is Button boton) yield return boton;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(raiz); i++)
                foreach (var hijo in Botones(VisualTreeHelper.GetChild(raiz, i))) yield return hijo;
        }
        private static void Renderizar(MainWindow ventana, int ancho, int alto, string archivo)
        {
            ventana.Width = ancho; ventana.Height = alto;
            // Window no se muestra en el escritorio. Loaded aplica las reglas para este ancho.
            ventana.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            var raiz = (FrameworkElement)ventana.Content;
            raiz.Measure(new Size(ancho, alto));
            raiz.Arrange(new Rect(0, 0, ancho, alto)); raiz.UpdateLayout(); Drenar();
            var mapa = new RenderTargetBitmap(ancho, alto, 96, 96, PixelFormats.Pbgra32);
            mapa.Render(raiz);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(mapa));
            using (var flujo = File.Create(archivo)) png.Save(flujo);
            Console.WriteLine("VISTA: " + Path.GetFileName(archivo) + " · " + ancho + " × " + alto);
        }
        private sealed class RepositorioSinConexion : IClienteRepositorio
        {
            private static AccesoDatosException Error() { return new AccesoDatosException(new InvalidOperationException("Fallo simulado, sin acceso a SQL.")); }
            public Task<IReadOnlyList<Cliente>> BuscarAsync(string texto) { return Task.FromException<IReadOnlyList<Cliente>>(Error()); }
            public Task<Cliente> ObtenerAsync(int id) { return Task.FromException<Cliente>(Error()); }
            public Task<int> AgregarAsync(Cliente c) { return Task.FromException<int>(Error()); }
        }
    }
}
