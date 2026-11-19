using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using Repair.Core.Datos;

namespace Repair.Wpf.Presentacion
{
    public partial class ReportesControl : UserControl
    {
        private ReporteOrdenesSql consulta;
        private DataTable resultado;
        private string filtros;
        private bool consultando;
        public ReportesControl() { InitializeComponent(); }
        internal void Inicializar(ReporteOrdenesSql consulta) { this.consulta = consulta; }
        private async void Consultar_Click(object sender, RoutedEventArgs e)
        {
            if (consulta == null || consultando) return;
            Consultar.IsEnabled = false; Exportar.IsEnabled = false; Imprimir.IsEnabled = false; consultando = true;
            Tabla.ItemsSource = null; resultado = null;
            try
            {
                string estado = Estado.SelectedIndex == 0 ? "" : ((ComboBoxItem)Estado.SelectedItem).Content.ToString();
                resultado = await consulta.ConsultarAsync(estado, Desde.SelectedDate, Hasta.SelectedDate, SoloEntregas.IsChecked == true);
                foreach (string columna in new[] { "CreadoUtc", "EntregadaUtc" })
                {
                    foreach (DataRow fila in resultado.Rows)
                        if (fila[columna] != DBNull.Value) fila[columna] = DateTime.SpecifyKind((DateTime)fila[columna], DateTimeKind.Utc).ToLocalTime();
                    resultado.Columns[columna].ColumnName = columna == "CreadoUtc" ? "IngresoLocal" : "EntregaLocal";
                }
                filtros = $"Estado: {(estado == "" ? "Todos" : estado)} · Desde: {Desde.SelectedDate:d} · Hasta: {Hasta.SelectedDate:d} · {(SoloEntregas.IsChecked == true ? "Fecha de entrega" : "Fecha de ingreso")}";
                Tabla.ItemsSource = resultado.DefaultView;
                Mensaje.Text = $"{resultado.Rows.Count} órdenes. {filtros}";
                Exportar.IsEnabled = Imprimir.IsEnabled = resultado.Rows.Count > 0;
            }
            catch (ArgumentException ex) { Mensaje.Text = ex.Message; }
            catch (Exception) { Mensaje.Text = "No se pudo consultar el reporte. Comprueba la conexión."; }
            finally { Consultar.IsEnabled = true; consultando = false; }
        }

        public static string CrearCsv(DataTable tabla)
        {
            string Celda(object dato)
            {
                string texto = dato is DateTime fecha ? fecha.ToString("yyyy-MM-dd HH:mm:ss") : Convert.ToString(dato);
                if (texto.TrimStart().Length > 0 && "=+-@".Contains(texto.TrimStart()[0])) texto = "'" + texto;
                return "\"" + texto.Replace("\"", "\"\"") + "\"";
            }
            var salida = new StringBuilder(string.Join(",", tabla.Columns.Cast<DataColumn>().Select(c => Celda(c.ColumnName))) + "\r\n");
            foreach (DataRow fila in tabla.Rows) salida.AppendLine(string.Join(",", fila.ItemArray.Select(Celda)));
            return salida.ToString();
        }
        private void Exportar_Click(object sender, RoutedEventArgs e)
        {
            if (resultado == null || resultado.Rows.Count == 0) return;
            var dialogo = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"Repair-ordenes-{DateTime.Now:yyyyMMdd-HHmmss}.csv", AddExtension = true };
            if (dialogo.ShowDialog() != true) return;
            try { File.WriteAllText(dialogo.FileName, CrearCsv(resultado), new UTF8Encoding(true)); Mensaje.Text = "Reporte exportado. " + filtros; }
            catch (Exception) { Mensaje.Text = "No se pudo escribir el reporte en la carpeta seleccionada."; }
        }
        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            if (resultado == null || resultado.Rows.Count == 0) return;
            try
            {
                var impresion = new PrintDialog();
                if (impresion.ShowDialog() != true) return;
                var documento = new FlowDocument { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 11, ColumnWidth = double.PositiveInfinity, PagePadding = new Thickness(35) };
                documento.Blocks.Add(new Paragraph(new Bold(new Run("Repair · Órdenes y entregas"))) { FontSize = 19 });
                documento.Blocks.Add(new Paragraph(new Run(filtros)));
                foreach (DataRow fila in resultado.Rows)
                    documento.Blocks.Add(new Paragraph(new Run($"Orden {fila["Orden"]} · {fila["Estado"]}\n{fila["Cliente"]} · {fila["Tipo"]} {fila["Equipo"]}\nIngreso: {fila["IngresoLocal"]} · Entrega: {fila["EntregaLocal"]}\nDiagnóstico: {fila["Diagnostico"]}\nObservaciones: {fila["Observaciones"]}")));
                documento.PageWidth = impresion.PrintableAreaWidth;
                documento.PageHeight = impresion.PrintableAreaHeight;
                impresion.PrintDocument(((IDocumentPaginatorSource)documento).DocumentPaginator, "Repair - Reporte de órdenes");
            }
            catch (Exception) { Mensaje.Text = "No se pudo imprimir el reporte."; }
        }
    }
}
