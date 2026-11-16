using System;

namespace Repair.Seguimiento
{
    internal sealed class OrdenSeguimiento
    {
        public OrdenSeguimiento(int numero, string cliente, string equipo, string diagnostico,
            string observaciones, string estado, DateTime? entregadaUtc = null)
        {
            Numero = numero;
            Cliente = cliente;
            Equipo = equipo;
            Diagnostico = diagnostico;
            Observaciones = observaciones;
            Estado = estado;
            EntregadaUtc = entregadaUtc;
        }

        public int Numero { get; }
        public string Cliente { get; }
        public string Equipo { get; }
        public string Diagnostico { get; }
        public string Observaciones { get; }
        public string Estado { get; }
        public DateTime? EntregadaUtc { get; }
    }

    internal static class FlujoOrden
    {
        public const string Pendiente = "Pendiente";
        public const string EnDiagnostico = "En diagnóstico";
        public const string EnReparacion = "En reparación";
        public const string Lista = "Lista para entrega";
        public const string Entregada = "Entregada";

        public static string Siguiente(OrdenSeguimiento orden)
        {
            if (orden == null || orden.EntregadaUtc.HasValue ||
                string.IsNullOrWhiteSpace(orden.Diagnostico) || string.IsNullOrWhiteSpace(orden.Equipo))
            {
                return null;
            }

            return SiguienteEstado(orden.Estado);
        }

        public static string SiguienteEstado(string estado)
        {
            switch (estado)
            {
                case EnDiagnostico: return EnReparacion;
                case EnReparacion: return Lista;
                case Lista: return Entregada;
                default: return null;
            }
        }
    }
}
