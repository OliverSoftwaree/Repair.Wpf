using System;
using System.Windows;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Core.Seguridad;

namespace Repair.Wpf
{
    public partial class MainWindow
    {
        internal void ConfigurarEntrega(string cadena, SesionUsuario usuario)
        {
            sesion = usuario;
            ordenServicio = new OrdenServicio(new OrdenesAutorizadas(new OrdenSqlRepositorio(cadena), usuario));
            repositorioEquipos = new EquiposAutorizados(new EquipoSqlRepositorio(cadena), usuario);
            DiagnosticoPage.Initialize(ordenServicio, servicio);
            var seguimiento = new SeguimientoServicio(cadena, usuario);
            SeguimientoPage.HabilitarEntrega(seguimiento);
            ReportesPage.Inicializar(new ReporteOrdenesSql(cadena));
            NextModuleLabel.Child = new System.Windows.Controls.TextBlock
            {
                Text = usuario.Usuario + " · " + usuario.Rol,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10
            };
            NewClientButton.IsEnabled = usuario.Permite(Permiso.Recepcion);
            NavDiagnostico.IsEnabled = usuario.Permite(Permiso.Diagnostico);
        }

        private void NavDiagnostico_Click(object sender, RoutedEventArgs e)
        {
            if (panelAbierto || ocupado || sesion == null || !sesion.Permite(Permiso.Diagnostico)) return;
            ShowPage("Diagnostico");
        }
        private void NavReportes_Click(object sender, RoutedEventArgs e)
        {
            if (panelAbierto || ocupado) return;
            ShowPage("Reportes");
        }
    }
}
