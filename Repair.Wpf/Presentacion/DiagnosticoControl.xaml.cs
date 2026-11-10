using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Repair.Wpf.Presentacion
{
    public partial class DiagnosticoControl : UserControl
    {
        private Repair.Core.Logica.OrdenServicio servicio;
        private Repair.Core.Integracion.IConsultaClientes clientesConsulta;
        private Repair.Core.Modelos.OrdenTrabajo ordenActual;
        private Repair.Core.Modelos.Cliente clienteActual;

        public DiagnosticoControl()
        {
            InitializeComponent();
        }

        public async void Initialize(Repair.Core.Logica.OrdenServicio servicio, Repair.Core.Integracion.IConsultaClientes clientes)
        {
            this.servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            this.clientesConsulta = clientes ?? throw new ArgumentNullException(nameof(clientes));
            await CargarClientesAsync();
        }

        private async Task CargarClientesAsync()
        {
            if (clientesConsulta == null) return;
            try
            {
                var clientes = await clientesConsulta.BuscarAsync("");
                ClienteSelector.ItemsSource = clientes;
                ClienteSelector.DisplayMemberPath = "Nombre";
                ClienteSelector.SelectedIndex = clientes.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo cargar la lista de clientes: " + ex.Message, Brushes.Red);
            }
        }

        private async Task CargarOrdenesClienteAsync(int clienteId)
        {
            if (servicio == null) return;
            try
            {
                var ordenes = await servicio.BuscarAsync("");
                var delCliente = ordenes.Where(o => o.ClienteId == clienteId).OrderByDescending(o => o.Id).ToList();
                OrdenSelector.ItemsSource = delCliente;
                OrdenSelector.DisplayMemberPath = "Id";
                OrdenSelector.SelectedIndex = delCliente.Count > 0 ? 0 : -1;
                if (delCliente.Count == 0)
                {
                    OrdenInfoText.Text = "No hay órdenes para este cliente.";
                    EstadoText.Text = "Estado: --";
                    ClienteInfoText.Text = "Cliente: " + (ClienteSelector.SelectedItem as Repair.Core.Modelos.Cliente)?.Nombre ?? "--";
                    OrdenInput.Text = "";
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo cargar las órdenes del cliente: " + ex.Message, Brushes.Red);
            }
        }

        private async Task CargarOrdenAsync(int id)
        {
            if (servicio == null) return;
            try
            {
                ordenActual = await servicio.ObtenerAsync(id);
                if (ordenActual == null)
                {
                    ordenActual = null;
                    clienteActual = null;
                    OrdenInfoText.Text = "No existe una orden con ese identificador.";
                    ClienteInfoText.Text = "Cliente: --";
                    EstadoText.Text = "Estado: --";
                    MostrarMensaje("No se encontró ninguna orden con ese ID.", Brushes.DarkOrange);
                    return;
                }

                clienteActual = await clientesConsulta.ObtenerAsync(ordenActual.ClienteId);
                OrdenInfoText.Text = "Orden: " + ordenActual.Id + " · ID cliente: " + ordenActual.ClienteId;
                ClienteInfoText.Text = "Cliente: " + (clienteActual != null ? clienteActual.Nombre : "Cliente no encontrado") + " (ID " + ordenActual.ClienteId + ")";
                EstadoText.Text = "Estado: " + (string.IsNullOrWhiteSpace(ordenActual.Estado) ? "Pendiente" : ordenActual.Estado);
                DiagnosticoInput.Text = ordenActual.Diagnostico ?? "";
                ObservacionesInput.Text = ordenActual.Observaciones ?? "";
                OrdenInput.Text = ordenActual.Id.ToString();
                MensajeText.Text = "La orden está lista. Completa el diagnóstico técnico.";
                MensajeText.Foreground = Brushes.DarkBlue;
                MensajeText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo buscar la orden: " + ex.Message, Brushes.Red);
            }
        }

        private async void ClienteSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cliente = ClienteSelector.SelectedItem as Repair.Core.Modelos.Cliente;
            if (cliente == null) return;
            await CargarOrdenesClienteAsync(cliente.Id);
        }

        private async void OrdenSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var orden = OrdenSelector.SelectedItem as Repair.Core.Modelos.OrdenTrabajo;
            if (orden == null) return;
            await CargarOrdenAsync(orden.Id);
        }

        private async void BuscarOrdenButton_Click(object sender, RoutedEventArgs e)
        {
            if (servicio == null) return;
            if (!string.IsNullOrWhiteSpace(OrdenInput.Text))
            {
                try
                {
                    int id;
                    if (!int.TryParse(OrdenInput.Text.Trim(), out id))
                    {
                        MostrarMensaje("El número de orden debe ser un valor numérico válido.", Brushes.Red);
                        return;
                    }

                    await CargarOrdenAsync(id);
                    return;
                }
                catch (Exception ex)
                {
                    MostrarMensaje("No se pudo buscar la orden: " + ex.Message, Brushes.Red);
                    return;
                }
            }

            var ordenSeleccionada = OrdenSelector.SelectedItem as Repair.Core.Modelos.OrdenTrabajo;
            if (ordenSeleccionada != null)
            {
                await CargarOrdenAsync(ordenSeleccionada.Id);
                return;
            }

            MostrarMensaje("Elige un cliente y una orden para continuar o escribe el ID manualmente.", Brushes.Red);
        }

        private async void GuardarDiagnosticoButton_Click(object sender, RoutedEventArgs e)
        {
            if (servicio == null || ordenActual == null)
            {
                MostrarMensaje("Primero busca una orden válida para guardar su diagnóstico.", Brushes.Red);
                return;
            }

            try
            {
                var detalle = "";
                if (SeveridadCombo != null && SeveridadCombo.SelectedItem is ComboBoxItem severidadItem)
                    detalle += "Severidad: " + severidadItem.Content + ". ";
                if (AccionCombo != null && AccionCombo.SelectedItem is ComboBoxItem accionItem)
                    detalle += "Acción: " + accionItem.Content + ". ";

                var textoCompleto = (DiagnosticoInput.Text ?? "").Trim();
                var resumen = string.IsNullOrWhiteSpace(textoCompleto) ? "Sin diagnóstico registrado." : textoCompleto;
                var observaciones = (ObservacionesInput.Text ?? "").Trim();
                var textoFinal = detalle + resumen;

                await servicio.GuardarDiagnosticoAsync(ordenActual.Id, textoFinal, observaciones);
                ordenActual.Estado = "En diagnóstico";
                EstadoText.Text = "Estado: En diagnóstico";
                MostrarMensaje("Diagnóstico guardado correctamente en SQL Server.", Brushes.DarkGreen);
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo guardar el diagnóstico: " + ex.Message, Brushes.Red);
            }
        }

        private void MostrarMensaje(string texto, Brush color)
        {
            MensajeText.Text = texto;
            MensajeText.Foreground = color;
            MensajeText.Visibility = Visibility.Visible;
        }
    }
}
