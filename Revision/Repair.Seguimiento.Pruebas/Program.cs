using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Repair.Seguimiento.Pruebas
{
    internal static class Program
    {
        private static int aprobadas;

        [STAThread]
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                ProbarReglas();
                ProbarVentana(args.Length == 1 ? args[0] : null);
                ProbarRevisionIntegrada();
                Console.WriteLine($"{aprobadas} comprobaciones correctas con órdenes ficticias y fuente SQL simulada para la vista.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void ProbarReglas()
        {
            var fechaUtc = new DateTime(2026, 11, 15, 14, 38, 22, DateTimeKind.Utc);
            int lecturasDeReloj = 0;
            var demo = new SeguimientoDemostracion(() => { lecturasDeReloj++; return fechaUtc; });
            var inicial = demo.Consultar(1001);
            Comprobar(demo.Consultar(9999) == null, "El número inexistente no devuelve fila");
            Comprobar(inicial.Estado == FlujoOrden.EnDiagnostico && inicial.EntregadaUtc == null, "Orden diagnosticada sin fecha de entrega");
            Comprobar(!demo.IntentarAvanzar(1001, FlujoOrden.EnDiagnostico, FlujoOrden.Entregada, out var rechazada) && rechazada == inicial,
                "Bloquea la entrega antes de la reparación");
            Comprobar(!demo.IntentarAvanzar(1001, FlujoOrden.EnDiagnostico, FlujoOrden.Lista, out _), "Bloquea saltar a lista");
            Comprobar(!demo.IntentarAvanzar(1002, FlujoOrden.Pendiente, FlujoOrden.EnReparacion, out _), "Bloquea orden pendiente");
            Comprobar(!demo.IntentarAvanzar(1003, FlujoOrden.EnDiagnostico, FlujoOrden.EnReparacion, out _), "Bloquea orden sin equipo vinculado");
            Comprobar(FlujoOrden.Siguiente(new OrdenSeguimiento(1, "Prueba", "Equipo", "  ", "", FlujoOrden.EnDiagnostico)) == null,
                "Un estado diagnosticado con texto vacío no permite avanzar");
            Comprobar(demo.IntentarAvanzar(1001, FlujoOrden.EnDiagnostico, FlujoOrden.EnReparacion, out var reparacion) &&
                reparacion.Estado == FlujoOrden.EnReparacion && !reparacion.EntregadaUtc.HasValue, "Registra inicio de reparación");
            Comprobar(!demo.IntentarAvanzar(1001, FlujoOrden.EnDiagnostico, FlujoOrden.EnReparacion, out var reciente) && reciente == reparacion,
                "Rechaza el estado previo de una pantalla desactualizada");
            Comprobar(!demo.IntentarAvanzar(1001, FlujoOrden.EnReparacion, FlujoOrden.Entregada, out _), "Bloquea entregar desde reparación");
            Comprobar(demo.IntentarAvanzar(1001, FlujoOrden.EnReparacion, FlujoOrden.Lista, out var lista) &&
                lista.Estado == FlujoOrden.Lista && !lista.EntregadaUtc.HasValue, "Marca lista para entrega sin fecha de cierre");
            Comprobar(demo.IntentarAvanzar(1001, FlujoOrden.Lista, FlujoOrden.Entregada, out var entregada) &&
                entregada.Estado == FlujoOrden.Entregada && entregada.EntregadaUtc == fechaUtc && entregada.EntregadaUtc.Value.Kind == DateTimeKind.Utc,
                "Registra estado y fecha UTC al entregar");
            Comprobar(!demo.IntentarAvanzar(1001, FlujoOrden.Lista, FlujoOrden.Entregada, out var duplicada) &&
                duplicada.EntregadaUtc == fechaUtc && lecturasDeReloj == 1, "La entrega repetida conserva la primera fecha");
            var consulta = demo.Consultar(1001);
            Comprobar(consulta.Estado == FlujoOrden.Entregada && consulta.EntregadaUtc == fechaUtc &&
                consulta.Cliente == inicial.Cliente && consulta.Diagnostico == inicial.Diagnostico &&
                consulta.Observaciones == inicial.Observaciones && consulta.Equipo == inicial.Equipo,
                "Volver a buscar en la sesión conserva estado, fecha, equipo y diagnóstico");
        }

        private static void ProbarVentana(string evidencias)
        {
            int numeroSql = 0;
            var ventana = new SeguimientoWindow(numero =>
            {
                numeroSql = numero;
                if (numero == 1001)
                {
                    return Task.FromException<OrdenSeguimiento>(new ConfigurationErrorsException("Fallo simulado"));
                }

                return Task.FromResult(new OrdenSeguimiento(numero, "Cliente ficticio SQL", null, "Diagnóstico de ejemplo", null,
                    FlujoOrden.EnDiagnostico));
            });

            Comprobar(ventana.OrdenTitulo.Text == "Orden 1001" && ventana.Cliente.Text == "Camila Prueba" &&
                (string)ventana.AvanzarOrden.Content == "Iniciar reparación", "La ventana inicia con el caso ficticio 1001");
            if (!string.IsNullOrEmpty(evidencias)) Renderizar(ventana, evidencias, "01-demostracion.png", 880, 690);
            Pulsar(ventana.AvanzarOrden);
            Comprobar(ventana.Estado.Text == FlujoOrden.EnReparacion && (string)ventana.AvanzarOrden.Content == "Marcar lista para entrega",
                "La ventana habilita solo la siguiente acción");
            Pulsar(ventana.AvanzarOrden);
            Pulsar(ventana.AvanzarOrden);
            Comprobar(ventana.Estado.Text == FlujoOrden.Entregada && ventana.Entrega.Visibility == Visibility.Visible &&
                ventana.Entrega.Text.StartsWith("Entrega registrada") && ventana.AvanzarOrden.Visibility == Visibility.Collapsed,
                "La ventana muestra la entrega registrada y retira el botón");
            if (!string.IsNullOrEmpty(evidencias)) Renderizar(ventana, evidencias, "02-entregada.png", 650, 650);
            Buscar(ventana, "abc");
            Comprobar(ventana.Detalle.Visibility == Visibility.Collapsed && ventana.Mensaje.Text.Contains("mayor que cero"),
                "Entrada inválida oculta la orden anterior");
            Buscar(ventana, "9999");
            Comprobar(ventana.Detalle.Visibility == Visibility.Collapsed && ventana.Mensaje.Text.Contains("No encontramos"), "Número inexistente en la ventana");
            Buscar(ventana, "1002");
            Comprobar(ventana.Estado.Text == FlujoOrden.Pendiente && ventana.AvanzarOrden.Visibility == Visibility.Collapsed, "La vista bloquea el avance de una orden pendiente");
            Buscar(ventana, "1003");
            Comprobar(ventana.Equipo.Text == "Sin equipo vinculado" && ventana.AvanzarOrden.Visibility == Visibility.Collapsed,
                "La vista bloquea el avance cuando falta el vínculo");
            Buscar(ventana, "1001");
            Comprobar(ventana.Estado.Text == FlujoOrden.Entregada && ventana.Entrega.Visibility == Visibility.Visible, "La vista recupera la entrega en la misma sesión");
            ventana.ModoSql.IsChecked = true;
            Pulsar(ventana.ModoSql);
            Comprobar(ventana.Detalle.Visibility == Visibility.Collapsed && ventana.NumeroOrden.Text == "" && ventana.ModoDetalle.Text.Contains("Solo lectura"),
                "Cambiar a SQL limpia el resultado anterior");
            Buscar(ventana, "42");
            Comprobar(numeroSql == 42 && ventana.OrdenTitulo.Text == "Orden 42" && ventana.Cliente.Text == "Cliente ficticio SQL" &&
                ventana.Equipo.Text == "Sin equipo vinculado" && ventana.AvanzarOrden.Visibility == Visibility.Collapsed,
                "El resultado SQL simulado se muestra sin acciones de escritura");
            if (!string.IsNullOrEmpty(evidencias)) Renderizar(ventana, evidencias, "03-consulta-sql-simulada.png", 880, 690);
            Buscar(ventana, "1001");
            Comprobar(numeroSql == 1001 && ventana.Detalle.Visibility == Visibility.Collapsed && ventana.ModoSql.IsChecked == true &&
                ventana.Mensaje.Text.Contains("RepairSql"), "Un fallo en SQL no carga ninguna orden de demostración");
            ventana.Close();
        }

        private static void Buscar(SeguimientoWindow ventana, string numero)
        {
            ventana.NumeroOrden.Text = numero;
            Pulsar(ventana.BuscarOrden);
        }

        private static void ProbarRevisionIntegrada()
        {
            var inicio = new RevisionWindow();
            Comprobar(inicio.Title.Contains("Revisión local"), "El inicio propio de la copia de revisión carga su interfaz");
            inicio.Close();

            var fechaUtc = new DateTime(2026, 11, 15, 14, 38, 22, DateTimeKind.Utc);
            var ventana = new SeguimientoWindow(numero => Task.FromResult(
                new OrdenSeguimiento(numero, "Cliente ficticio", "Equipo ficticio", "Revisión", "", FlujoOrden.Entregada, fechaUtc)), soloSql: true);
            Comprobar(ventana.ModoSql.IsChecked == true && ventana.SelectorModo.Visibility == Visibility.Collapsed && ventana.NumeroOrden.Text == "",
                "La vista integrada inicia en consulta SQL, sin cargar demostración");
            Buscar(ventana, "42");
            Comprobar(ventana.Entrega.Text.StartsWith("Entrega registrada") && ventana.AvanzarOrden.Visibility == Visibility.Collapsed,
                "La vista integrada muestra la fecha SQL simulada y ninguna acción de escritura");
            ventana.Close();

            foreach (string baseProtegida in new[] { "RepairAcademico", "RepairClientesDesarrollo_20261030" })
            {
                bool rechazada = false;
                try
                {
                    new PersistenciaSeguimientoSql("Data Source=.;Integrated Security=True;Initial Catalog=" + baseProtegida);
                }
                catch (InvalidOperationException)
                {
                    rechazada = true;
                }
                Comprobar(rechazada, "El escritor rechaza " + baseProtegida + " antes de abrir la conexión");
            }
        }

        private static void Pulsar(ButtonBase boton)
        {
            boton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private static void Renderizar(SeguimientoWindow ventana, string carpeta, string archivo, int ancho, int alto)
        {
            var raiz = (FrameworkElement)ventana.Content;
            var tamano = new Size(ancho, alto);
            raiz.Measure(tamano);
            raiz.Arrange(new Rect(tamano));
            raiz.UpdateLayout();
            var imagen = new RenderTargetBitmap(ancho, alto, 96, 96, PixelFormats.Pbgra32);
            imagen.Render(raiz);
            var archivoPng = new PngBitmapEncoder();
            archivoPng.Frames.Add(BitmapFrame.Create(imagen));
            Directory.CreateDirectory(carpeta);
            using (var salida = File.Create(Path.Combine(carpeta, archivo))) archivoPng.Save(salida);
        }

        private static void Comprobar(bool condicion, string caso)
        {
            if (!condicion) throw new InvalidOperationException(caso);
            aprobadas++;
            Console.WriteLine("OK · " + caso);
        }
    }
}
