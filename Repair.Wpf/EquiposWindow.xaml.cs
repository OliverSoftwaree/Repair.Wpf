using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Repair.Core.Integracion;
using Repair.Core.Logica;
using Repair.Core.Modelos;

namespace Repair.Wpf
{
    public partial class EquiposWindow : Window
    {
        private readonly EquipoServicio servicio;
        private readonly IConsultaClientes clientesConsulta;
        private readonly ObservableCollection<Equipo> filas = new ObservableCollection<Equipo>();
        private bool operacionEnCurso;

public EquiposWindow(EquipoServicio servicio, IConsultaClientes clientes)
{
    InitializeComponent();
    TipoSelector.ItemsSource = TiposEquipo.Opciones;
    this.servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
    this.clientesConsulta = clientes ?? throw new ArgumentNullException(nameof(clientes));
    Listado.ItemsSource = filas;
    Loaded += EquiposWindow_Loaded;
    Closing += (s, e) => { if (operacionEnCurso) e.Cancel = true; };
}

private async void EquiposWindow_Loaded(object sender, RoutedEventArgs e)
{
    await CargarClientesAsync();
    await CargarListadoAsync();
}

        private async Task CargarClientesAsync()
        {
            try
            {
                var lista = await clientesConsulta.BuscarAsync("");
                ClienteSelector.Items.Clear();
                foreach (var c in lista) ClienteSelector.Items.Add(new ComboBoxItem { Content = $"{c.Id} - {c.Nombre}", Tag = c.Id });
                if (ClienteSelector.Items.Count > 0) { ClienteSelector.SelectedIndex = 0; GuardarButton.IsEnabled = true; ClienteSelector.IsEnabled = true; MensajeText.Visibility = Visibility.Collapsed; }
                else { MensajeText.Text = "Aún no hay clientes creados. Registra un cliente antes de agregar un equipo."; MensajeText.Foreground = System.Windows.Media.Brushes.DarkOrange; MensajeText.Visibility = Visibility.Visible; GuardarButton.IsEnabled = false; ClienteSelector.IsEnabled = false; }
            }
            catch (Exception ex)
            {
                MensajeText.Text = "No se pudo cargar la lista de clientes: " + ex.Message;
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
                GuardarButton.IsEnabled = false;
            }
        }

        private async Task<bool> CargarListadoAsync()
        {
            try
            {
                var datos = await servicio.BuscarAsync("");
                filas.Clear();
                foreach (var e in datos) filas.Add(e);
                if (datos == null || datos.Count == 0)
                {
                    MensajeText.Text = "No se encontraron equipos.";
                    MensajeText.Foreground = System.Windows.Media.Brushes.DarkGray;
                    MensajeText.Visibility = Visibility.Visible;
                }
                else MensajeText.Visibility = Visibility.Collapsed;
                return true;
            }
            catch (Exception ex)
            {
                MensajeText.Text = "Error al consultar equipos: " + ex.Message;
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
                return false;
            }
        }


private async void GuardarButton_Click(object sender, RoutedEventArgs e)
{
            if (operacionEnCurso) return;
            MensajeText.Visibility = Visibility.Collapsed;
            int clienteId = 0;
            if (ClienteSelector.SelectedItem is ComboBoxItem item && item.Tag is int id) clienteId = id;
            var equipo = new Equipo
            {
                ClienteId = clienteId,
                Tipo = TipoSelector.SelectedItem as string,
                Marca = MarcaInput.Text,
                Modelo = ModeloInput.Text,
                Serie = SerieInput.Text,
                Accesorios = AccesoriosInput.Text,
                Condicion = CondicionInput.Text
            };
            operacionEnCurso = true;
            GuardarButton.IsEnabled = false;
            try
            {
                int nuevoId = await servicio.RegistrarAsync(equipo);
                if (!await CargarListadoAsync())
                {
                    MensajeText.Text = "Equipo guardado (Id: " + nuevoId + "), pero no se pudo recuperar el listado. Vuelve a abrir esta ventana para consultarlo.";
                    MensajeText.Foreground = System.Windows.Media.Brushes.DarkOrange; MensajeText.Visibility = Visibility.Visible;
                    return;
                }
                MensajeText.Text = "Guardado correctamente (Id: " + nuevoId + ")";
                MensajeText.Foreground = System.Windows.Media.Brushes.DarkGreen;
                MensajeText.Visibility = Visibility.Visible;
            }
            catch (Repair.Core.Logica.ValidacionException vex)
            {
                // Mostrar errores de validación y mantener el texto del formulario.
                MensajeText.Text = string.Join("\n", vex.Errores.Values);
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
            }
            catch (Repair.Core.Datos.AccesoDatosException aex)
            {
                // Error de SQL o red: no borra el formulario y avisa claramente.
                MensajeText.Text = aex.Message + " (consulta el listado y reintenta).";
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MensajeText.Text = "No se pudo guardar: " + ex.Message;
                MensajeText.Foreground = System.Windows.Media.Brushes.Red;
                MensajeText.Visibility = Visibility.Visible;
            }
            finally { operacionEnCurso = false; GuardarButton.IsEnabled = ClienteSelector.Items.Count > 0; }
        }

private void CerrarButton_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
