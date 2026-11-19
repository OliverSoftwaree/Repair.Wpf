using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using Repair.Core.Datos;
using Repair.Core.Integracion;
using Repair.Core.Seguridad;
using Repair.Core.Logica;
using Repair.Wpf.Demostracion;

namespace Repair.Wpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string cadena = ConfigurationManager.ConnectionStrings["RepairSql"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cadena))
            {
                MessageBox.Show("Falta RepairSql en App.config. Consulta la guía de inicio.", "Repair", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1); return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var acceso = new AccesoWindow(cadena);
            if (acceso.ShowDialog() != true) { Shutdown(); return; }
            var sesion = acceso.Sesion;
            IClienteRepositorio repositorio = new ClientesAutorizados(new ClienteSqlRepositorio(cadena), sesion);
            var principal = new MainWindow(new ClienteServicio(repositorio), false, null, animaciones: null,
                consultaSeguimiento: new ConsultaSeguimientoSql(cadena));
            principal.ConfigurarEntrega(cadena, sesion);
            MainWindow = principal;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow.Show();
        }
    }
}
