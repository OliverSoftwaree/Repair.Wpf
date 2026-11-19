using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Repair.Wpf.Presentacion
{
    public partial class DiagnosticoControl : UserControl
    {
        private static readonly Regex PrefijoDiagnostico = new Regex(
            @"^\s*Severidad:\s*(?<Severidad>[^.]+)\.\s*Acción:\s*(?<Accion>[^.]+)\.\s*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private Repair.Core.Logica.OrdenServicio servicio;
        private Repair.Core.Integracion.IConsultaClientes clientesConsulta;
        private Repair.Core.Integracion.IConsultaEquipos equiposConsulta;
        private Repair.Core.Modelos.OrdenTrabajo ordenActual;
        private Repair.Core.Modelos.Cliente clienteActual;
        private bool actualizandoSelectores;
        private bool guardandoDiagnostico;

        public DiagnosticoControl()
        {
            InitializeComponent();
        }

        public async void Initialize(Repair.Core.Logica.OrdenServicio servicio, Repair.Core.Integracion.IConsultaClientes clientes, Repair.Core.Integracion.IConsultaEquipos equipos)
        {
            this.servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            this.clientesConsulta = clientes ?? throw new ArgumentNullException(nameof(clientes));
            equiposConsulta = equipos ?? throw new ArgumentNullException(nameof(equipos));
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

        public async Task ActualizarDatosAsync()
        {
            if (servicio == null || clientesConsulta == null) return;

            var clienteSeleccionado = ClienteSelector.SelectedItem as Repair.Core.Modelos.Cliente;
            int? clienteId = clienteSeleccionado != null ? (int?)clienteSeleccionado.Id : null;
            try
            {
                var clientes = await clientesConsulta.BuscarAsync("");
                actualizandoSelectores = true;
                ClienteSelector.ItemsSource = clientes;
                ClienteSelector.DisplayMemberPath = "Nombre";
                ClienteSelector.SelectedItem = clientes.FirstOrDefault(c => clienteId.HasValue && c.Id == clienteId.Value)
                    ?? clientes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo actualizar la lista de clientes: " + ex.Message, Brushes.Red);
                return;
            }
            finally
            {
                actualizandoSelectores = false;
            }

            var cliente = ClienteSelector.SelectedItem as Repair.Core.Modelos.Cliente;
            if (cliente == null)
            {
                ordenActual = null;
                clienteActual = null;
                OrdenSelector.ItemsSource = null;
                OrdenInput.Clear();
                OrdenInfoText.Text = "No hay clientes registrados.";
                ClienteInfoText.Text = "Cliente: --";
                EstadoText.Text = "Estado: --";
                EquipoInfoText.Text = "Equipo: --";
                CargarDiagnostico(null);
                ObservacionesInput.Clear();
                return;
            }

            await CargarOrdenesClienteAsync(cliente.Id);
        }

        private async Task CargarOrdenesClienteAsync(int clienteId, int? ordenIdSeleccionada = null)
        {
            if (servicio == null) return;
            try
            {
                var ordenes = await servicio.BuscarAsync("");
                var delCliente = ordenes.Where(o => o.ClienteId == clienteId).OrderByDescending(o => o.Id).ToList();
                var seleccionada = ordenIdSeleccionada.HasValue
                    ? delCliente.FirstOrDefault(o => o.Id == ordenIdSeleccionada.Value)
                    : null;
                if (seleccionada == null && delCliente.Count > 0) seleccionada = delCliente[0];

                actualizandoSelectores = true;
                OrdenSelector.ItemsSource = delCliente;
                OrdenSelector.DisplayMemberPath = "Id";
                OrdenSelector.SelectedItem = seleccionada;
                actualizandoSelectores = false;
                if (seleccionada != null)
                {
                    await CargarOrdenAsync(seleccionada.Id);
                }
                if (delCliente.Count == 0)
                {
                    ordenActual = null;
                    clienteActual = null;
                    OrdenInfoText.Text = "No hay órdenes para este cliente.";
                    EstadoText.Text = "Estado: --";
                    EquipoInfoText.Text = "Equipo: --";
                    ClienteInfoText.Text = "Cliente: " + ((ClienteSelector.SelectedItem as Repair.Core.Modelos.Cliente)?.Nombre ?? "--");
                    OrdenInput.Text = "";
                    CargarDiagnostico(null);
                    ObservacionesInput.Clear();
                    ActualizarVistaPreviaDiagnostico();
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
                    clienteActual = null;
                    OrdenInfoText.Text = "No existe una orden con ese identificador.";
                    ClienteInfoText.Text = "Cliente: --";
                    EquipoInfoText.Text = "Equipo: --";
                    EstadoText.Text = "Estado: --";
                    DiagnosticoInput.Clear();
                    ObservacionesInput.Clear();
                    ActualizarVistaPreviaDiagnostico();
                    MostrarMensaje("No se encontró ninguna orden con ese ID.", Brushes.DarkOrange);
                    return;
                }

                clienteActual = await clientesConsulta.ObtenerAsync(ordenActual.ClienteId);
                OrdenInfoText.Text = "Orden: " + ordenActual.Id + " · ID cliente: " + ordenActual.ClienteId;
                ClienteInfoText.Text = "Cliente: " + (clienteActual != null ? clienteActual.Nombre : "Cliente no encontrado") + " (ID " + ordenActual.ClienteId + ")";
                EstadoText.Text = "Estado: " + (string.IsNullOrWhiteSpace(ordenActual.Estado) ? "Pendiente" : ordenActual.Estado);
                var hallazgosIngreso = await CargarEquipoAsync(ordenActual);
                CargarDiagnostico(ordenActual.Diagnostico);
                ObservacionesInput.Text = string.IsNullOrWhiteSpace(ordenActual.Observaciones)
                    ? hallazgosIngreso ?? ""
                    : ordenActual.Observaciones;
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

        private async Task<string> CargarEquipoAsync(Repair.Core.Modelos.OrdenTrabajo orden)
        {
            if (!orden.EquipoId.HasValue)
            {
                EquipoInfoText.Text = "Equipo: no asociado a esta orden histórica.";
                return null;
            }

            try
            {
                var equipo = await equiposConsulta.ObtenerAsync(orden.EquipoId.Value);
                if (equipo == null)
                {
                    EquipoInfoText.Text = "Equipo ID " + orden.EquipoId.Value + ": no encontrado.";
                    return null;
                }

                var nombreEquipo = string.Join(" ", new[] { equipo.Marca, equipo.Modelo }.Where(parte => !string.IsNullOrWhiteSpace(parte)));
                var detalles = new List<string>();
                var datosIngreso = new List<string>();
                detalles.Add("Equipo: " + (string.IsNullOrWhiteSpace(nombreEquipo) ? "Sin marca/modelo" : nombreEquipo) + " (ID " + equipo.Id + ")");
                if (!string.IsNullOrWhiteSpace(equipo.Serie)) detalles.Add("Serie: " + equipo.Serie);
                if (!string.IsNullOrWhiteSpace(equipo.Condicion))
                {
                    detalles.Add("Hallazgos registrados: " + equipo.Condicion);
                    datosIngreso.Add("Condición al ingreso: " + equipo.Condicion);
                }
                if (!string.IsNullOrWhiteSpace(equipo.Accesorios))
                {
                    detalles.Add("Accesorios: " + equipo.Accesorios);
                    datosIngreso.Add("Accesorios recibidos: " + equipo.Accesorios);
                }
                EquipoInfoText.Text = string.Join(" · ", detalles);
                return datosIngreso.Count == 0 ? null : string.Join(Environment.NewLine, datosIngreso);
            }
            catch (Exception ex)
            {
                EquipoInfoText.Text = "No se pudo cargar la información del equipo: " + ex.Message;
                return null;
            }
        }

        private void CargarDiagnostico(string diagnostico)
        {
            var texto = diagnostico ?? "";
            string severidad = null;
            string accion = null;
            Match prefijo;

            while ((prefijo = PrefijoDiagnostico.Match(texto)).Success)
            {
                severidad = prefijo.Groups["Severidad"].Value.Trim();
                accion = prefijo.Groups["Accion"].Value.Trim();
                texto = texto.Substring(prefijo.Length);
            }

            SeveridadCombo.SelectedIndex = 1;
            AccionCombo.SelectedIndex = 0;
            SeleccionarOpcion(SeveridadCombo, severidad);
            SeleccionarOpcion(AccionCombo, accion);

            texto = texto.Trim();
            if (string.Equals(texto, "Sin diagnóstico registrado.", StringComparison.OrdinalIgnoreCase))
                texto = "";
            DiagnosticoInput.Text = texto;
            ActualizarVistaPreviaDiagnostico();
        }

        private void DiagnosticoPreview_Changed(object sender, RoutedEventArgs e)
        {
            ActualizarVistaPreviaDiagnostico();
        }

        private void ActualizarVistaPreviaDiagnostico()
        {
            if (DiagnosticoPreviewText == null) return;

            var severidad = (SeveridadCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "--";
            var accion = (AccionCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "--";
            var diagnostico = (DiagnosticoInput.Text ?? "").Trim();
            DiagnosticoPreviewText.Text = "Severidad: " + severidad + ". Acción: " + accion + "." +
                (diagnostico.Length == 0 ? " Escribe el diagnóstico técnico aquí arriba." : " " + diagnostico);
        }

        private static void SeleccionarOpcion(ComboBox combo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;

            var opcion = combo.Items.Cast<ComboBoxItem>().FirstOrDefault(item =>
                string.Equals(item.Content as string, valor, StringComparison.OrdinalIgnoreCase));
            if (opcion != null) combo.SelectedItem = opcion;
        }

        private async void ClienteSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actualizandoSelectores) return;
            var cliente = ClienteSelector.SelectedItem as Repair.Core.Modelos.Cliente;
            if (cliente == null) return;
            await CargarOrdenesClienteAsync(cliente.Id);
        }

        private async void OrdenSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actualizandoSelectores) return;
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

                    await BuscarOrdenPorIdAsync(id);
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

        private async Task BuscarOrdenPorIdAsync(int id)
        {
            var orden = await servicio.ObtenerAsync(id);
            if (orden == null)
            {
                await CargarOrdenAsync(id);
                return;
            }

            var clientes = await clientesConsulta.BuscarAsync("");
            actualizandoSelectores = true;
            ClienteSelector.ItemsSource = clientes;
            ClienteSelector.DisplayMemberPath = "Nombre";
            ClienteSelector.SelectedItem = clientes.FirstOrDefault(c => c.Id == orden.ClienteId);
            actualizandoSelectores = false;

            await CargarOrdenesClienteAsync(orden.ClienteId, orden.Id);
        }

        private async void GuardarDiagnosticoButton_Click(object sender, RoutedEventArgs e)
        {
            if (guardandoDiagnostico) return;
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
                if (string.IsNullOrWhiteSpace(textoCompleto))
                {
                    MostrarMensaje("Escribe el diagnóstico encontrado antes de guardar.", Brushes.Red);
                    return;
                }

                var observaciones = (ObservacionesInput.Text ?? "").Trim();
                var textoFinal = detalle + textoCompleto;

                guardandoDiagnostico = true;
                GuardarDiagnosticoButton.IsEnabled = false;
                GuardarDiagnosticoButton.Content = "Guardando…";
                MostrarMensaje("Guardando diagnóstico…", Brushes.DarkBlue);
                var ordenId = ordenActual.Id;
                await servicio.GuardarDiagnosticoAsync(ordenId, textoFinal, observaciones);
                var ordenActualizada = await servicio.ObtenerAsync(ordenId);
                if (ordenActualizada == null) throw new InvalidOperationException("No se pudo recargar la orden después de guardar el diagnóstico.");
                ordenActual = ordenActualizada;
                EstadoText.Text = "Estado: " + (string.IsNullOrWhiteSpace(ordenActual.Estado) ? "Pendiente" : ordenActual.Estado);
                MostrarMensaje("Diagnóstico guardado correctamente en SQL Server.", Brushes.DarkGreen);
            }
            catch (Exception ex)
            {
                MostrarMensaje("No se pudo guardar el diagnóstico: " + ex.Message, Brushes.Red);
            }
            finally
            {
                guardandoDiagnostico = false;
                GuardarDiagnosticoButton.IsEnabled = true;
                GuardarDiagnosticoButton.Content = "Guardar diagnóstico";
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
