using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Repair.Wpf.Presentacion
{
    public partial class EquiposControl : UserControl
    {
        private Repair.Core.Modelos.Equipo seleccionado;
        private Repair.Core.Logica.EquipoServicio servicio;
        private Repair.Core.Integracion.IConsultaClientes clientesConsulta;
        public EquiposControl()
        {
            InitializeComponent();
        }

        public void Initialize(Repair.Core.Logica.EquipoServicio servicio, Repair.Core.Integracion.IConsultaClientes clientes)
        {
            this.servicio = servicio ?? throw new System.ArgumentNullException(nameof(servicio));
            this.clientesConsulta = clientes ?? throw new System.ArgumentNullException(nameof(clientes));
            Loaded += EquiposControl_Loaded;
            // Si el control ya está cargado cuando se inicializa (por ejemplo, si Initialize se llama desde Window_Loaded),
            // invocamos el handler manualmente para asegurar que la lista de clientes se cargue.
            if (IsLoaded) EquiposControl_Loaded(this, null);
        }

        private async void EquiposControl_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarClientesAsync();
            await CargarListadoAsync();
        }

        // Se expone para que la ventana principal pueda solicitar un refresco cuando se añada un cliente.
        public System.Threading.Tasks.Task RefrescarClientesAsync() => CargarClientesAsync();

        private async System.Threading.Tasks.Task CargarClientesAsync()
        {
            try
            {
                var lista = await clientesConsulta.BuscarAsync("");
                ClienteSelector.Items.Clear();
                foreach (var c in lista) ClienteSelector.Items.Add(new ComboBoxItem { Content = $"{c.Id} - {c.Nombre}", Tag = c.Id });
                if (ClienteSelector.Items.Count > 0) { ClienteSelector.SelectedIndex = 0; GuardarButton.IsEnabled = true; ClienteSelector.IsEnabled = true; MensajeText.Visibility = Visibility.Collapsed; }
                else { MensajeText.Text = "Aún no hay clientes creados. Registra un cliente antes de agregar un equipo."; MensajeText.Foreground = System.Windows.Media.Brushes.DarkOrange; MensajeText.Visibility = Visibility.Visible; GuardarButton.IsEnabled = false; ClienteSelector.IsEnabled = false; }
            }
            catch (System.Exception ex)
            {
                MensajeText.Text = "No se pudo cargar la lista de clientes: " + ex.Message;
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
                GuardarButton.IsEnabled = false;
            }
            finally
            {
                // Asegurar que los botones de edición empiecen deshabilitados hasta que el usuario seleccione un equipo.
                ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false;
            }
        }

        private async System.Threading.Tasks.Task CargarListadoAsync()
        {
            try
            {
                var datos = await servicio.BuscarAsync("");
                Listado.ItemsSource = datos;
                if (datos == null || datos.Count == 0)
                {
                    MensajeText.Text = "No se encontraron equipos.";
                    MensajeText.Foreground = System.Windows.Media.Brushes.DarkGray;
                    MensajeText.Visibility = Visibility.Visible;
                    ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false;
                }
                else
                {
                    MensajeText.Visibility = Visibility.Collapsed;
                    ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false;
                    // Conectar selección para habilitar Actualizar/Eliminar cuando el usuario seleccione un ítem.
                    Listado.SelectionChanged -= Listado_SelectionChanged;
                    Listado.SelectionChanged += Listado_SelectionChanged;
                }
            }
            catch (System.Exception ex)
            {
                MensajeText.Text = "Error al consultar equipos: " + ex.Message;
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
            }
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            MensajeText.Visibility = Visibility.Collapsed;
            int clienteId = 0;
            if (ClienteSelector.SelectedItem is ComboBoxItem item)
            {
                // ComboBoxItem.Tag puede ser string si viene del XAML o int si fue rellenado en tiempo de ejecución.
                if (item.Tag is int id) clienteId = id;
                else if (item.Tag is string s && int.TryParse(s, out var parsed)) clienteId = parsed;
            }
            var equipo = new Repair.Core.Modelos.Equipo
            {
                ClienteId = clienteId,
                Marca = MarcaInput.Text,
                Modelo = ModeloInput.Text,
                Serie = SerieInput.Text,
                Accesorios = AccesoriosInput.Text,
                Condicion = CondicionInput.Text
            };
            try
            {
                int nuevoId = await servicio.RegistrarAsync(equipo);
                await CargarListadoAsync();
                MensajeText.Text = "Guardado correctamente (Id: " + nuevoId + ")";
                MensajeText.Foreground = System.Windows.Media.Brushes.DarkGreen;
                MensajeText.Visibility = Visibility.Visible;
                // Desactivar botones de edición tras crear nuevo equipo y limpiar formulario
                ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false; MarcaInput.Clear(); ModeloInput.Clear(); SerieInput.Clear(); AccesoriosInput.Clear(); CondicionInput.Clear();
                // Seleccionar automáticamente el nuevo equipo en la lista si está presente
                var datos = await servicio.BuscarAsync("");
                var nuevo = datos.FirstOrDefault(d => d.Id == nuevoId);
                if (nuevo != null) { Listado.SelectedItem = nuevo; Listado.ScrollIntoView(nuevo); }
            }
            catch (Repair.Core.Logica.ValidacionException vex)
            {
                MensajeText.Text = string.Join("\n", vex.Errores.Values);
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
                // Si el error es duplicado, habilitar selección para revisión
                if (vex.Errores.ContainsKey("Duplicado")) { ActualizarButton.IsEnabled = true; EliminarButton.IsEnabled = true; }
            }
            catch (Repair.Core.Datos.AccesoDatosException aex)
            {
                MensajeText.Text = aex.Message + " (consulta el listado y reintenta).";
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                MensajeText.Text = "No se pudo guardar: " + ex.Message;
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
            }
        }

        private void Listado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            seleccionado = Listado.SelectedItem as Repair.Core.Modelos.Equipo;
            if (seleccionado != null)
            {
                ActualizarButton.IsEnabled = true; EliminarButton.IsEnabled = true;
                // Rellenar formulario con datos seleccionados
                MarcaInput.Text = seleccionado.Marca; ModeloInput.Text = seleccionado.Modelo; SerieInput.Text = seleccionado.Serie; AccesoriosInput.Text = seleccionado.Accesorios; CondicionInput.Text = seleccionado.Condicion;
                // Asegurar cliente seleccionado si está presente en ComboBox
                for (int i = 0; i < ClienteSelector.Items.Count; i++)
                {
                    if (ClienteSelector.Items[i] is ComboBoxItem item && item.Tag is int id && id == seleccionado.ClienteId) { ClienteSelector.SelectedIndex = i; break; }
                }
            }
            else { ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false; }
        }

        private async void ActualizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (seleccionado == null) return;
            int clienteId = 0;
            if (ClienteSelector.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is int id) clienteId = id; else if (item.Tag is string s && int.TryParse(s, out var parsed)) clienteId = parsed;
            }
            seleccionado.ClienteId = clienteId;
            seleccionado.Marca = MarcaInput.Text; seleccionado.Modelo = ModeloInput.Text; seleccionado.Serie = SerieInput.Text; seleccionado.Accesorios = AccesoriosInput.Text; seleccionado.Condicion = CondicionInput.Text;
            try
            {
                await servicio.ActualizarAsync(seleccionado);
                await CargarListadoAsync();
                MensajeText.Text = "Actualizado correctamente."; MensajeText.Foreground = System.Windows.Media.Brushes.DarkGreen; MensajeText.Visibility = Visibility.Visible;
                ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false;
            }
            catch (Repair.Core.Logica.ValidacionException vex)
            {
                MensajeText.Text = string.Join("\n", vex.Errores.Values); MensajeText.Foreground = System.Windows.Media.Brushes.Red; MensajeText.Visibility = Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                MensajeText.Text = "No se pudo actualizar: " + ex.Message; MensajeText.Foreground = System.Windows.Media.Brushes.Red; MensajeText.Visibility = Visibility.Visible;
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (seleccionado == null) return;
            try
            {
                await servicio.EliminarAsync(seleccionado.Id);
                await CargarListadoAsync();
                MensajeText.Text = "Eliminado correctamente."; MensajeText.Foreground = System.Windows.Media.Brushes.DarkGreen; MensajeText.Visibility = Visibility.Visible;
                ActualizarButton.IsEnabled = false; EliminarButton.IsEnabled = false;
                MarcaInput.Clear(); ModeloInput.Clear(); SerieInput.Clear(); AccesoriosInput.Clear(); CondicionInput.Clear();
            }
            catch (System.Exception ex)
            {
                MensajeText.Text = "No se pudo eliminar: " + ex.Message; MensajeText.Foreground = System.Windows.Media.Brushes.Red; MensajeText.Visibility = Visibility.Visible;
            }
        }
    }
}
