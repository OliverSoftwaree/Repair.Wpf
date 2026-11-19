using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Repair.Core.Logica;
using Repair.Wpf;
using Repair.Wpf.Demostracion;
using Repair.Wpf.Presentacion;

namespace Repair.Clientes.Verificacion
{
    internal static class CapturasEntrega
    {
        internal static int Ejecutar(string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            try
            {
                RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
                var app=new App{ShutdownMode=ShutdownMode.OnExplicitShutdown}; app.InitializeComponent();
                var acceso=new AccesoWindow("Data Source=.;Initial Catalog=RepairEntregaDesarrollo_20261119;Integrated Security=True");
                ((TextBox)acceso.FindName("Usuario")).Text="admin.demo";
                Capturar(acceso,470,600,Path.Combine(carpeta,"01-acceso.png")); acceso.Close();
                var clientes=new ClienteServicio(new ClienteMemoriaRepositorio());
                var ventana=new MainWindow(clientes,true,null,false,new ConsultaDemo());
                ventana.Width=1380; ventana.Height=880;
                ventana.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                ((Border)ventana.FindName("DemoBanner")).Visibility=Visibility.Collapsed;
                ((TextBlock)ventana.FindName("ConnectionText")).Text="Datos ficticios de documentación";
                Capturar(ventana,1380,880,Path.Combine(carpeta,"02-inicio.png"));
                MostrarPagina(ventana,"Clientes"); Capturar(ventana,1380,880,Path.Combine(carpeta,"03-clientes.png"));
                MostrarPagina(ventana,"Equipos");
                var equipos=(FrameworkElement)ventana.FindName("EquiposControl");
                var selectorCliente=(ComboBox)equipos.FindName("ClienteSelector");
                selectorCliente.ItemsSource=new[]{new Repair.Core.Modelos.Cliente{Id=1,Nombre="Camila Prueba",Telefono="809-555-0199"}};
                selectorCliente.DisplayMemberPath="Nombre"; selectorCliente.SelectedIndex=0;
                ((ComboBox)equipos.FindName("TipoSelector")).SelectedItem="Laptop";
                ((TextBox)equipos.FindName("MarcaInput")).Text="Lenovo";
                ((TextBox)equipos.FindName("ModeloInput")).Text="ThinkPad T14";
                ((TextBox)equipos.FindName("SerieInput")).Text="DEMO-1001";
                ((TextBox)equipos.FindName("AccesoriosInput")).Text="Cargador";
                ((TextBox)equipos.FindName("CondicionInput")).Text="El ventilador no gira durante la prueba de carga.";
                Capturar(ventana,1380,880,Path.Combine(carpeta,"04-equipos.png"));
                var diagnostico=(DiagnosticoControl)ventana.FindName("DiagnosticoPage");
                diagnostico.Initialize(new OrdenServicio(new OrdenMemoriaRepositorio()),clientes);
                ((TextBox)diagnostico.FindName("OrdenInput")).Text="2";
                ((Button)diagnostico.FindName("BuscarOrdenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                MostrarPagina(ventana,"Diagnostico"); Capturar(ventana,1380,880,Path.Combine(carpeta,"05-diagnostico.png"));
                MostrarPagina(ventana,"Seguimiento");
                var seguimiento=(FrameworkElement)ventana.FindName("SeguimientoPage");
                typeof(SeguimientoControl).GetMethod("HabilitarEntrega",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(seguimiento,new object[]{new SeguimientoServicio("Data Source=.;Integrated Security=True",new Repair.Core.Seguridad.SesionUsuario(1,"admin.demo","Administrador"))});
                ((TextBox)seguimiento.FindName("NumeroOrden")).Text="1001";
                ((Button)seguimiento.FindName("BuscarOrden")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Capturar(ventana,1380,880,Path.Combine(carpeta,"06-seguimiento-entrega.png"));
                MostrarPagina(ventana,"Reportes");
                var reportes=(FrameworkElement)ventana.FindName("ReportesPage");
                var tabla=new DataTable();
                foreach(string c in new[]{"Orden","Cliente","Tipo","Equipo","Estado","IngresoLocal","EntregaLocal"})tabla.Columns.Add(c);
                tabla.Rows.Add("1001","Camila Prueba","Laptop","Lenovo ThinkPad T14","Entregada","19/11/2026 09:00","19/11/2026 10:30");
                ((DataGrid)reportes.FindName("Tabla")).ItemsSource=tabla.DefaultView;
                ((TextBlock)reportes.FindName("Mensaje")).Text="1 orden ficticia · Filtro de entregas";
                ((Button)reportes.FindName("Exportar")).IsEnabled=true;
                ((Button)reportes.FindName("Imprimir")).IsEnabled=true;
                Capturar(ventana,1380,880,Path.Combine(carpeta,"07-reportes.png"));
                File.WriteAllText(Path.Combine(carpeta,"LEEME.txt"),"Renderizados de las pantallas finales con datos ficticios para documentación. Son evidencia de disposición visual. Las comprobaciones SQL reales constan por separado en Resultado-preparacion.txt y pruebas-ui.txt.",new UTF8Encoding(true));
                ventana.Close();
                return 0;
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(carpeta,"ERROR.txt"),ex.ToString());return 1;}
        }

        private static void MostrarPagina(MainWindow ventana,string pagina) => typeof(MainWindow).GetMethod("ShowPage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ventana,new object[]{pagina,false});
        private static void Capturar(Window ventana,int ancho,int alto,string ruta)
        {
            var raiz=(FrameworkElement)ventana.Content;
            raiz.Measure(new Size(ancho,alto)); raiz.Arrange(new Rect(0,0,ancho,alto)); raiz.UpdateLayout();
            var imagen=new RenderTargetBitmap(ancho,alto,96,96,PixelFormats.Pbgra32);imagen.Render(raiz);
            var salida=new PngBitmapEncoder();salida.Frames.Add(BitmapFrame.Create(imagen));
            using(var archivo=File.Create(ruta))salida.Save(archivo);
        }
        private sealed class ConsultaDemo : Repair.Core.Integracion.IConsultaSeguimiento
        {
            public System.Threading.Tasks.Task<Repair.Core.Modelos.OrdenSeguimiento> BuscarPorIdAsync(int id) =>
                System.Threading.Tasks.Task.FromResult(new Repair.Core.Modelos.OrdenSeguimiento(id,1,"Camila Prueba",1,"Lenovo ThinkPad T14","Entregada","Ventilador sustituido y prueba de temperatura correcta.","Se entregó con su cargador.",new DateTime(2026,11,19,14,30,0,DateTimeKind.Utc)));
        }
    }
}
