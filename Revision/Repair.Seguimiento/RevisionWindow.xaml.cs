using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Repair.Seguimiento
{
    public partial class RevisionWindow : Window
    {
        private SeguimientoWindow seguimiento;

        public RevisionWindow()
        {
            InitializeComponent();
        }

        private void AbrirSeguimiento(object sender, RoutedEventArgs e)
        {
            Aviso.Text = "";
            if (seguimiento != null)
            {
                seguimiento.Activate();
                return;
            }

            seguimiento = new SeguimientoWindow(ConsultaOrdenesSql.ConsultarAsync, soloSql: true) { Owner = this };
            seguimiento.Closed += (origen, evento) => seguimiento = null;
            seguimiento.Show();
        }

        private void AbrirDiagnostico(object sender, RoutedEventArgs e) => AbrirRepair("Oliver");

        private void AbrirClientesEquipos(object sender, RoutedEventArgs e) => AbrirRepair("TipoCsv");

        private void AbrirRepair(string area)
        {
            Aviso.Text = "";
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, area);
            string ejecutable = Path.Combine(carpeta, "Repair.Wpf.exe");
            if (!File.Exists(ejecutable))
            {
                Aviso.Text = "Faltan los archivos de esta área. Abre Repair.Revision.exe desde el paquete de revisión.";
                return;
            }

            try
            {
                using (Process.Start(new ProcessStartInfo(ejecutable) { WorkingDirectory = carpeta, UseShellExecute = false })) { }
            }
            catch (Win32Exception)
            {
                Aviso.Text = "No se pudo abrir esta área de Repair.";
            }
        }
    }
}
