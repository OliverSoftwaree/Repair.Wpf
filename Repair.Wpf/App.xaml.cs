using System;
using System.Configuration;
using System.Linq;
using System.Windows;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Wpf.Demostracion;

namespace Repair.Wpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool demo = e.Args.Contains("--demo") || string.Equals(ConfigurationManager.AppSettings["Modo"], "Demostracion", StringComparison.OrdinalIgnoreCase);
            IClienteRepositorio repositorio;
            if (demo) repositorio = new ClienteMemoriaRepositorio();
            else
            {
                var configuracion = ConfigurationManager.ConnectionStrings["RepairSql"];
                if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.ConnectionString))
                {
                    MessageBox.Show("Falta RepairSql en App.config. Consulta la guía de inicio.", "Repair", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1); return;
                }
                repositorio = new ClienteSqlRepositorio(configuracion.ConnectionString);
            }
            // Registrar la implementación WPF de IModuloEquipos.
            Repair.Wpf.Integracion.IModuloEquipos modulo;
            if (demo) modulo = new Repair.Wpf.Integracion.ModuloEquipos(true, null);
            else modulo = new Repair.Wpf.Integracion.ModuloEquipos(false, ConfigurationManager.ConnectionStrings["RepairSql"]?.ConnectionString);
            MainWindow = new MainWindow(new ClienteServicio(repositorio), demo, modulo);
            MainWindow.Show();
        }
    }
}
