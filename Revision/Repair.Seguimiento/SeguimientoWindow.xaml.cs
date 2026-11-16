using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Repair.Seguimiento
{
    public partial class SeguimientoWindow : Window
    {
        private readonly SeguimientoDemostracion demostracion = new SeguimientoDemostracion(() => DateTime.UtcNow);
        private readonly Func<int, Task<OrdenSeguimiento>> consultarSql;
        private OrdenSeguimiento ordenActual;
        private bool ocupado;

        public SeguimientoWindow() : this(ConsultaOrdenesSql.ConsultarAsync)
        {
        }

        internal SeguimientoWindow(Func<int, Task<OrdenSeguimiento>> consultarSql, bool soloSql = false)
        {
            this.consultarSql = consultarSql;
            InitializeComponent();
            NumeroOrden.TextChanged += CambiarNumero;
            if (soloSql)
            {
                ModoSql.IsChecked = true;
                SelectorModo.Visibility = Visibility.Collapsed;
            }

            ActualizarModo();
            if (UsaDemostracion)
            {
                NumeroOrden.Text = "1001";
                MostrarOrden(demostracion.Consultar(1001));
            }
        }

        private bool UsaDemostracion => ModoDemo.IsChecked == true;

        private void CambiarModo(object sender, RoutedEventArgs e)
        {
            ActualizarModo();
            NumeroOrden.Clear();
            LimpiarResultado();
            NumeroOrden.Focus();
        }

        private void ActualizarModo()
        {
            ModoDetalle.Text = UsaDemostracion
                ? "Órdenes ficticias 1001, 1002 y 1003. Los cambios duran hasta cerrar la ventana."
                : "Solo lectura. Busca órdenes en la base de revisión de Repair.";
        }

        private void CambiarNumero(object sender, TextChangedEventArgs e)
        {
            LimpiarResultado();
        }

        private async void BuscarOrden_Click(object sender, RoutedEventArgs e) => await ConsultarOrdenAsync();

        private async void NumeroOrden_Tecla(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await ConsultarOrdenAsync();
        }

        private async Task ConsultarOrdenAsync()
        {
            if (ocupado) return;
            LimpiarResultado();
            if (!int.TryParse(NumeroOrden.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int numero) || numero <= 0)
            {
                Avisar("Escribe un número de orden mayor que cero.", true);
                return;
            }

            MarcarOcupado(true);
            try
            {
                var orden = UsaDemostracion ? demostracion.Consultar(numero) : await consultarSql(numero);
                if (orden == null)
                {
                    Avisar("No encontramos una orden con ese número.");
                    return;
                }

                Mensaje.Visibility = Visibility.Collapsed;
                MostrarOrden(orden);
            }
            catch (SqlException ex) when (ex.Number == 207 || ex.Number == 208)
            {
                Avisar("La base seleccionada no tiene el esquema de órdenes de Repair publicado en GitHub.", true);
            }
            catch (SqlException)
            {
                Avisar("No se pudo consultar SQL Server. Revisa la conexión y los permisos de lectura.", true);
            }
            catch (TypeInitializationException)
            {
                Avisar("Windows no pudo iniciar el acceso a SQL Server en esta sesión.", true);
            }
            catch (Exception ex) when (ex is ConfigurationErrorsException || ex is ArgumentException || ex is InvalidOperationException)
            {
                Avisar("No se pudo usar la configuración de la conexión RepairSql.", true);
            }
            finally
            {
                MarcarOcupado(false);
            }
        }

        private void AvanzarOrden_Click(object sender, RoutedEventArgs e)
        {
            if (ocupado || !UsaDemostracion || ordenActual == null) return;
            string destino = FlujoOrden.Siguiente(ordenActual);
            if (destino == null) return;

            bool seActualizo = demostracion.IntentarAvanzar(ordenActual.Numero, ordenActual.Estado, destino, out var actual);
            if (actual != null) MostrarOrden(actual);
            Avisar(seActualizo ? "Estado actualizado en la demostración." : "La orden cambió. Consulta su estado actual.", !seActualizo);
        }

        private void MostrarOrden(OrdenSeguimiento orden)
        {
            ordenActual = orden;
            OrdenTitulo.Text = $"Orden {orden.Numero}";
            Cliente.Text = orden.Cliente;
            Equipo.Text = string.IsNullOrWhiteSpace(orden.Equipo) ? "Sin equipo vinculado" : orden.Equipo;
            Estado.Text = orden.Estado;
            Diagnostico.Text = string.IsNullOrWhiteSpace(orden.Diagnostico) ? "Diagnóstico pendiente" : orden.Diagnostico;
            Observaciones.Text = orden.Observaciones;
            ObservacionesBloque.Visibility = string.IsNullOrWhiteSpace(orden.Observaciones) ? Visibility.Collapsed : Visibility.Visible;
            Entrega.Visibility = orden.Estado == FlujoOrden.Entregada ? Visibility.Visible : Visibility.Collapsed;
            Entrega.Text = orden.EntregadaUtc.HasValue
                ? $"Entrega registrada · {orden.EntregadaUtc.Value.ToLocalTime():g}"
                : "Fecha de entrega no disponible";

            string destino = UsaDemostracion ? FlujoOrden.Siguiente(orden) : null;
            AvanzarOrden.Content = EtiquetaAccion(destino);
            AvanzarOrden.Visibility = destino != null ? Visibility.Visible : Visibility.Collapsed;
            AccionDetalle.Text = UsaDemostracion && destino == null && orden.Estado != FlujoOrden.Entregada
                ? MotivoSinAvance(orden)
                : null;
            AccionDetalle.Visibility = AccionDetalle.Text?.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            Detalle.Visibility = Visibility.Visible;
            if (SystemParameters.ClientAreaAnimation)
            {
                Detalle.BeginAnimation(OpacityProperty, new DoubleAnimation(0.80, 1, TimeSpan.FromMilliseconds(160)));
            }
        }

        private static string EtiquetaAccion(string destino)
        {
            switch (destino)
            {
                case FlujoOrden.EnReparacion: return "Iniciar reparación";
                case FlujoOrden.Lista: return "Marcar lista para entrega";
                case FlujoOrden.Entregada: return "Registrar entrega";
                default: return null;
            }
        }

        private static string MotivoSinAvance(OrdenSeguimiento orden)
        {
            if (string.IsNullOrWhiteSpace(orden.Equipo)) return "La orden necesita un equipo vinculado para avanzar.";
            if (orden.Estado == FlujoOrden.Pendiente || string.IsNullOrWhiteSpace(orden.Diagnostico)) return "Completa el diagnóstico antes de iniciar la reparación.";
            return "Esta orden no tiene una acción disponible.";
        }

        private void MarcarOcupado(bool valor)
        {
            ocupado = valor;
            BuscarOrden.IsEnabled = !valor;
            NumeroOrden.IsReadOnly = valor;
            ModoDemo.IsEnabled = !valor;
            ModoSql.IsEnabled = !valor;
            if (valor) Avisar("Consultando orden…");
        }

        private void LimpiarResultado()
        {
            ordenActual = null;
            Detalle.Visibility = Visibility.Collapsed;
            AvanzarOrden.Visibility = Visibility.Collapsed;
            Mensaje.Visibility = Visibility.Collapsed;
        }

        private void Avisar(string texto, bool error = false)
        {
            Mensaje.Text = texto;
            Mensaje.Foreground = error ? Brushes.Firebrick : Brushes.DarkSlateGray;
            Mensaje.Visibility = Visibility.Visible;
        }
    }
}
