using System;
using System.Windows;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Wpf.Demostracion;

namespace Repair.Wpf.Integracion
{
    public sealed class ModuloEquipos : IModuloEquipos
    {
        private readonly bool demo;
        private readonly string conexion;

        public ModuloEquipos(bool demo, string conexion)
        {
            this.demo = demo;
            this.conexion = conexion;
        }

        public void Abrir(Window propietario, Repair.Core.Integracion.IConsultaClientes clientes)
        {
            if (propietario == null) throw new ArgumentNullException(nameof(propietario));
            if (clientes == null) throw new ArgumentNullException(nameof(clientes));
            IEquipoRepositorio repo = demo ? (IEquipoRepositorio)new EquipoMemoriaRepositorio() : new EquipoSqlRepositorio(conexion);
            var servicioEquipos = new EquipoServicio(repo);
            var ventana = new Repair.Wpf.EquiposWindow(servicioEquipos, clientes) { Owner = propietario };
            // Abrir en grande para comportarse similar a las páginas internas.
            try { ventana.WindowStartupLocation = WindowStartupLocation.CenterOwner; ventana.WindowState = WindowState.Maximized; } catch { }
            ventana.ShowDialog();
        }
    }
}
