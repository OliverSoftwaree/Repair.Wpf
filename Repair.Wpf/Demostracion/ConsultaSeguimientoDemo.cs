using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Integracion;
using Repair.Core.Modelos;

namespace Repair.Wpf.Demostracion
{
    internal sealed class ConsultaSeguimientoDemo : IConsultaSeguimiento
    {
        private readonly Dictionary<int, OrdenSeguimiento> ordenes = new Dictionary<int, OrdenSeguimiento>
        {
            [1001] = new OrdenSeguimiento(1001, 1, "Camila Prueba", 1, "Lenovo ThinkPad T14", "Entregada",
                "El ventilador no giraba durante la prueba de carga.", "Ventilador sustituido y temperatura comprobada.",
                new DateTime(2026, 11, 15, 14, 38, 22, DateTimeKind.Utc)),
            [1003] = new OrdenSeguimiento(1003, 1, "Camila Prueba", null, null, "En diagnóstico",
                "El equipo pierde la señal bajo carga.", "Orden histórica con el cliente registrado.", null)
        };

        public Task<OrdenSeguimiento> BuscarPorIdAsync(int id) => Task.FromResult(ordenes.TryGetValue(id, out var orden) ? orden : null);
    }
}
