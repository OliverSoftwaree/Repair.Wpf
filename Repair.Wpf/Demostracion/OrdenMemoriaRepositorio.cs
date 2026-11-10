using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Modelos;

namespace Repair.Wpf.Demostracion
{
    public sealed class OrdenMemoriaRepositorio : IOrdenRepositorio
    {
        private readonly List<OrdenTrabajo> filas = new List<OrdenTrabajo>();

        public OrdenMemoriaRepositorio()
        {
            Semilla(1, 1, "En espera", "", "Sin diagnóstico");
            Semilla(2, 1, "En diagnóstico", "Pantalla rota", "Revisión inicial completa.");
            Semilla(3, 2, "Pendiente", "", "Cliente solicita revisión general.");
        }

        private void Semilla(int id, int clienteId, string estado, string diagnostico, string observaciones)
        {
            filas.Add(new OrdenTrabajo { Id = id, ClienteId = clienteId, Estado = estado, Diagnostico = diagnostico, Observaciones = observaciones, CreadoUtc = DateTime.UtcNow.AddDays(-id) });
        }

        public Task<IReadOnlyList<OrdenTrabajo>> BuscarAsync(string texto)
        {
            texto = (texto ?? "").Trim();
            return Task.FromResult((IReadOnlyList<OrdenTrabajo>)filas
                .Where(o => texto.Length == 0 || o.Id.ToString() == texto || o.ClienteId.ToString() == texto || Contiene(o.Estado, texto) || Contiene(o.Diagnostico, texto) || Contiene(o.Observaciones, texto))
                .OrderByDescending(o => o.Id)
                .Select(Copiar)
                .ToList());
        }

        public Task<OrdenTrabajo> ObtenerAsync(int id)
        {
            return Task.FromResult(Copiar(filas.FirstOrDefault(o => o.Id == id)));
        }

        public Task<int> AgregarAsync(OrdenTrabajo orden)
        {
            var copia = Copiar(orden);
            copia.Id = filas.Count + 1;
            copia.CreadoUtc = DateTime.UtcNow;
            filas.Add(copia);
            return Task.FromResult(copia.Id);
        }

        public Task GuardarDiagnosticoAsync(int id, string diagnostico, string observaciones)
        {
            var fila = filas.FirstOrDefault(o => o.Id == id);
            if (fila == null) return Task.CompletedTask;
            fila.Diagnostico = diagnostico ?? "";
            fila.Observaciones = observaciones ?? "";
            fila.Estado = "En diagnóstico";
            return Task.CompletedTask;
        }

        private static bool Contiene(string valor, string texto)
        {
            return (valor ?? "").IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static OrdenTrabajo Copiar(OrdenTrabajo o)
        {
            if (o == null) return null;
            return new OrdenTrabajo { Id = o.Id, ClienteId = o.ClienteId, Estado = o.Estado, Diagnostico = o.Diagnostico, Observaciones = o.Observaciones, CreadoUtc = o.CreadoUtc };
        }
    }
}
