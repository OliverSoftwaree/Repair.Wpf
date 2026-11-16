using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Repair.Core.Datos;
using Repair.Core.Integracion;
using Repair.Core.Modelos;

namespace Repair.Wpf.Presentacion
{
    public partial class SeguimientoControl : UserControl
    {
        private IConsultaSeguimiento consulta;
        private bool consultando;

        public SeguimientoControl()
        {
            InitializeComponent();
            NumeroOrden.TextChanged += (_, __) => LimpiarResultado();
        }

        internal void Inicializar(IConsultaSeguimiento origen)
        {
            consulta = origen;
            BuscarOrden.IsEnabled = consulta != null;
        }

        private async void BuscarOrden_Click(object sender, RoutedEventArgs e) => await ConsultarAsync();

        private async void NumeroOrden_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await ConsultarAsync();
        }

        private async Task ConsultarAsync()
        {
            if (consulta == null || consultando) return;
            LimpiarResultado();
            if (!int.TryParse(NumeroOrden.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int numero) || numero <= 0)
            {
                Avisar("Escribe un número de orden mayor que cero.", true);
                return;
            }

            consultando = true;
            BuscarOrden.IsEnabled = false;
            NumeroOrden.IsReadOnly = true;
            Avisar("Consultando orden…");
            try
            {
                var orden = await consulta.BuscarPorIdAsync(numero);
                if (orden == null) Avisar("No encontramos una orden con ese número.");
                else Mostrar(orden);
            }
            catch (AccesoDatosException)
            {
                Avisar("No se pudo consultar la orden. Revisa la conexión y el esquema de la base de Repair.", true);
            }
            finally
            {
                consultando = false;
                BuscarOrden.IsEnabled = true;
                NumeroOrden.IsReadOnly = false;
            }
        }

        private void Mostrar(OrdenSeguimiento orden)
        {
            TituloOrden.Text = $"Orden {orden.Id}";
            ClienteOrden.Text = orden.Cliente;
            EquipoOrden.Text = !orden.EquipoId.HasValue ? "Sin equipo vinculado"
                : string.IsNullOrWhiteSpace(orden.Equipo) ? "Vínculo de equipo sin verificar" : orden.Equipo;
            EstadoOrden.Text = orden.Estado;
            DiagnosticoOrden.Text = string.IsNullOrWhiteSpace(orden.Diagnostico) ? "Diagnóstico pendiente" : orden.Diagnostico;
            ObservacionesOrden.Text = orden.Observaciones;
            BloqueObservaciones.Visibility = string.IsNullOrWhiteSpace(orden.Observaciones) ? Visibility.Collapsed : Visibility.Visible;
            FechaEntrega.Visibility = orden.EntregadaUtc.HasValue || orden.Estado == "Entregada" ? Visibility.Visible : Visibility.Collapsed;
            FechaEntrega.Text = orden.EntregadaUtc.HasValue
                ? $"Entrega registrada · {orden.EntregadaUtc.Value.ToLocalTime():g}" : "Fecha de entrega no registrada";
            MensajeConsulta.Visibility = Visibility.Collapsed;
            ResultadoOrden.Visibility = Visibility.Visible;
        }

        private void LimpiarResultado()
        {
            ResultadoOrden.Visibility = Visibility.Collapsed;
            MensajeConsulta.Visibility = Visibility.Collapsed;
        }

        private void Avisar(string mensaje, bool error = false)
        {
            MensajeConsulta.Text = mensaje;
            MensajeConsulta.Foreground = error ? Brushes.Firebrick : (Brush)FindResource("Muted");
            MensajeConsulta.Visibility = Visibility.Visible;
        }
    }
}
