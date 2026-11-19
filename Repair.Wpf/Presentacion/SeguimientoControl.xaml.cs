using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Repair.Core.Datos;
using Repair.Core.Integracion;
using Repair.Core.Modelos;
using Repair.Core.Logica;

namespace Repair.Wpf.Presentacion
{
    public partial class SeguimientoControl : UserControl
    {
        private IConsultaSeguimiento consulta;
        private bool consultando;
        private SeguimientoServicio entrega;
        private OrdenSeguimiento actual;
        public bool OperacionEnCurso => consultando;

        internal void HabilitarEntrega(SeguimientoServicio servicio) { entrega = servicio; }

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
            actual = orden;
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
            string siguiente = SeguimientoServicio.Siguiente(orden.Estado);
            Avanzar.Content = siguiente == "En reparación" ? "Iniciar reparación" : siguiente == "Lista para entrega" ? "Marcar lista para entrega" : "Registrar entrega";
            Avanzar.Visibility = entrega != null && orden.EquipoId.HasValue && !string.IsNullOrWhiteSpace(orden.Diagnostico) && entrega.PuedeAvanzar(orden.Estado) ? Visibility.Visible : Visibility.Collapsed;
            Reingresar.Visibility = entrega != null && entrega.PuedeReingresar && orden.EquipoId.HasValue && orden.Estado == "Entregada" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LimpiarResultado()
        {
            ResultadoOrden.Visibility = Visibility.Collapsed;
            MensajeConsulta.Visibility = Visibility.Collapsed;
            actual = null;
            Avanzar.Visibility = Reingresar.Visibility = Visibility.Collapsed;
        }

        private async void Avanzar_Click(object sender, RoutedEventArgs e)
        {
            if (consultando || entrega == null || actual == null) return;
            consultando = true; Avanzar.IsEnabled = Reingresar.IsEnabled = BuscarOrden.IsEnabled = false; NumeroOrden.IsReadOnly = true;
            try
            {
                int id = actual.Id;
                bool guardado = await entrega.AvanzarAsync(id, actual.Estado);
                var orden = await consulta.BuscarPorIdAsync(id);
                if (orden == null) { LimpiarResultado(); Avisar("La orden ya no existe.", true); }
                else { Mostrar(orden); Avisar(guardado ? "Estado guardado." : "La orden cambió o no reúne las condiciones para avanzar. Se actualizó la consulta.", !guardado); }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is ArgumentException) { Avisar(ex.Message, true); }
            catch (Exception) { Avisar("No se confirmó el cambio. Busca la orden otra vez antes de reintentar.", true); }
            finally { consultando = false; Avanzar.IsEnabled = Reingresar.IsEnabled = BuscarOrden.IsEnabled = true; NumeroOrden.IsReadOnly = false; }
        }

        private async void Reingresar_Click(object sender, RoutedEventArgs e)
        {
            if (consultando || entrega == null || actual?.EquipoId == null) return;
            consultando = true; Avanzar.IsEnabled = Reingresar.IsEnabled = BuscarOrden.IsEnabled = false; NumeroOrden.IsReadOnly = true;
            try
            {
                int id = await entrega.ReingresarAsync(actual.EquipoId.Value);
                NumeroOrden.Text = id.ToString(CultureInfo.InvariantCulture);
                var orden = await consulta.BuscarPorIdAsync(id);
                if (orden != null) Mostrar(orden);
                Avisar("Orden activa del equipo: " + id + ". Si ya existía un ingreso abierto, se reutilizó esa orden.");
            }
            catch (UnauthorizedAccessException ex) { Avisar(ex.Message, true); }
            catch (Exception) { Avisar("No se confirmó el ingreso. Consulta las órdenes antes de reintentar.", true); }
            finally { consultando = false; Avanzar.IsEnabled = Reingresar.IsEnabled = BuscarOrden.IsEnabled = true; NumeroOrden.IsReadOnly = false; }
        }

        private void Avisar(string mensaje, bool error = false)
        {
            MensajeConsulta.Text = mensaje;
            MensajeConsulta.Foreground = error ? Brushes.Firebrick : (Brush)FindResource("Muted");
            MensajeConsulta.Visibility = Visibility.Visible;
        }
    }
}
