using System;
using System.Collections.Generic;

namespace Repair.Seguimiento
{
    internal sealed class SeguimientoDemostracion
    {
        private readonly Func<DateTime> relojUtc;
        private readonly Dictionary<int, OrdenSeguimiento> ordenes;

        public SeguimientoDemostracion(Func<DateTime> relojUtc)
        {
            this.relojUtc = relojUtc;
            ordenes = new Dictionary<int, OrdenSeguimiento>
            {
                [1001] = new OrdenSeguimiento(1001, "Camila Prueba", "Laptop · Lenovo ThinkPad T14",
                    "El ventilador no gira durante la prueba de carga.", "Severidad media. Sustituir el ventilador y verificar temperaturas.",
                    FlujoOrden.EnDiagnostico),
                [1002] = new OrdenSeguimiento(1002, "Iván Prueba", "Monitor · Dell P2422H", "", "", FlujoOrden.Pendiente),
                [1003] = new OrdenSeguimiento(1003, "Camila Prueba", null, "El equipo pierde la señal bajo carga.",
                    "Orden histórica con el cliente registrado.", FlujoOrden.EnDiagnostico)
            };
        }

        public OrdenSeguimiento Consultar(int numero)
        {
            lock (ordenes)
            {
                return ordenes.TryGetValue(numero, out var orden) ? orden : null;
            }
        }

        public bool IntentarAvanzar(int numero, string estadoEsperado, string destino, out OrdenSeguimiento actual)
        {
            lock (ordenes)
            {
                if (!ordenes.TryGetValue(numero, out actual) || actual.Estado != estadoEsperado ||
                    destino == null || destino != FlujoOrden.Siguiente(actual))
                {
                    return false;
                }

                DateTime? entregadaUtc = destino == FlujoOrden.Entregada ? (DateTime?)relojUtc() : null;
                actual = new OrdenSeguimiento(actual.Numero, actual.Cliente, actual.Equipo, actual.Diagnostico,
                    actual.Observaciones, destino, entregadaUtc);
                ordenes[numero] = actual;
                return true;
            }
        }
    }
}
