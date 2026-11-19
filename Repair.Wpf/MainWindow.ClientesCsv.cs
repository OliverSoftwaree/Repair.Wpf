using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Repair.Wpf.Presentacion;

namespace Repair.Wpf
{
    public partial class MainWindow
    {
        private async void ExportClients_Click(object sender, RoutedEventArgs e)
        {
            if (ocupado || panelAbierto) return;
            if (!datosDisponibles || consultaFallida)
            { ShowToast("Consulta los clientes correctamente antes de exportar.", false); return; }
            if (filas.Count == 0)
            { ShowToast("No hay clientes en este resultado para exportar.", false); return; }

            // El texto escrito pero aún no buscado no modifica el resultado exportado.
            var instantanea = filas.ToArray();
            var destino = new SaveFileDialog
            {
                Title = "Exportar el resultado visible de clientes",
                Filter = "Archivo CSV (*.csv)|*.csv",
                DefaultExt = ".csv", AddExtension = true, OverwritePrompt = true,
                FileName = "Repair-clientes" + (demostracion ? "-DEMOSTRACION" : "") + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv"
            };
            if (destino.ShowDialog(this) != true) return;
            SetBusy(true); ExportClientsButton.IsEnabled = false;
            try
            {
                await Task.Run(() => File.WriteAllText(destino.FileName, ClienteCsv.Crear(instantanea), new UTF8Encoding(true)));
                ShowToast(instantanea.Length + " clientes exportados" + (demostracion ? " desde la demostración temporal." : "."));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            { ShowToast("No se pudo guardar el CSV. Revisa la carpeta y que el archivo no esté abierto.", false); }
            finally { SetBusy(false); ExportClientsButton.IsEnabled = true; }
        }
    }
}
