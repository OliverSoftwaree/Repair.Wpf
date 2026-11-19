using System;
using System.Threading.Tasks;
using System.Windows;
using Repair.Core.Seguridad;

namespace Repair.Wpf
{
    public partial class AccesoWindow : Window
    {
        private readonly AutenticacionSql autenticacion;
        private bool ocupada;
        public SesionUsuario Sesion { get; private set; }
        public AccesoWindow(string cadena) { InitializeComponent(); autenticacion = new AutenticacionSql(cadena); }
        private async void Entrar_Click(object sender, RoutedEventArgs e)
        {
            if (ocupada) return;
            ocupada = true; Entrar.IsEnabled = false; Mensaje.Text = "Comprobando acceso…";
            try
            {
                string usuario = Usuario.Text, clave = Clave.Password, nueva = NuevaClave.Password;
                var sesion = await Task.Run(() => autenticacion.IniciarAsync(usuario, clave));
                if (sesion == null) { Mensaje.Text = "Usuario o contraseña incorrectos, o cuenta desactivada."; return; }
                if (CambiarClave.IsChecked == true) await autenticacion.CambiarClaveAsync(sesion, clave, nueva);
                Sesion = sesion; DialogResult = true;
            }
            catch (ArgumentException ex) { Mensaje.Text = ex.Message; }
            catch (Exception) { Mensaje.Text = "No se pudo iniciar sesión. Comprueba la configuración y prepara la base con Preparar-Repair.exe."; }
            finally { Clave.Clear(); NuevaClave.Clear(); ocupada = false; Entrar.IsEnabled = true; }
        }
    }
}
