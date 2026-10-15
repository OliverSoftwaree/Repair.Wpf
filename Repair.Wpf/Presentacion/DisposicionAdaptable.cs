using System;

namespace Repair.Wpf.Presentacion
{
    // Reglas de diseño independientes del estado de clientes; unidades lógicas WPF.
    public sealed class DisposicionAdaptable
    {
        public bool MenuCompacto { get; private set; }
        public bool ListaCompacta { get; private set; }
        public bool MostrarIlustracion { get; private set; }
        public bool InicioEnColumnas { get; private set; }
        public double AnchoPanel { get; private set; }
        public static DisposicionAdaptable Para(double ancho)
        {
            return new DisposicionAdaptable
            {
                MenuCompacto = ancho < 1100,
                ListaCompacta = ancho < 1180,
                MostrarIlustracion = ancho >= 1280,
                InicioEnColumnas = ancho >= 1000,
                AnchoPanel = Math.Min(480, Math.Max(340, ancho - 88))
            };
        }
    }
}
