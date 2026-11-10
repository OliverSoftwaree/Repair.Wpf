using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Integracion;
using Repair.Core.Modelos;

namespace Repair.Core.Logica
{
    public sealed class OrdenServicio : IConsultaOrdenes
    {
        private readonly IOrdenRepositorio repositorio;

        public OrdenServicio(IOrdenRepositorio repositorio)
        {
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        public Task<IReadOnlyList<OrdenTrabajo>> BuscarAsync(string texto)
        {
            texto = (texto ?? "").Trim();
            if (texto.Length > 100) throw new ArgumentException("La búsqueda admite hasta 100 caracteres.");
            return repositorio.BuscarAsync(texto);
        }

        public Task<OrdenTrabajo> ObtenerAsync(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Selecciona una orden válida.");
            return repositorio.ObtenerAsync(id);
        }

        public Task<int> RegistrarAsync(OrdenTrabajo orden)
        {
            if (orden == null) throw new ArgumentNullException(nameof(orden));
            var copia = new OrdenTrabajo
            {
                ClienteId = orden.ClienteId,
                Estado = (orden.Estado ?? "Pendiente").Trim(),
                Diagnostico = (orden.Diagnostico ?? "").Trim(),
                Observaciones = (orden.Observaciones ?? "").Trim(),
                CreadoUtc = DateTime.UtcNow
            };
            var errores = new Dictionary<string, string>();
            if (copia.ClienteId <= 0) errores["ClienteId"] = "Selecciona un cliente válido.";
            if (string.IsNullOrWhiteSpace(copia.Estado)) errores["Estado"] = "El estado es obligatorio.";
            if (errores.Count > 0) throw new ValidacionException(errores);
            return repositorio.AgregarAsync(copia);
        }

        public async Task GuardarDiagnosticoAsync(int id, string diagnostico, string observaciones)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "La orden debe ser válida.");
            if (string.IsNullOrWhiteSpace(diagnostico)) throw new ValidacionException(new Dictionary<string, string> { { "Diagnostico", "Escribe el diagnóstico encontrado." } });
            var orden = await repositorio.ObtenerAsync(id);
            if (orden == null) throw new InvalidOperationException("No existe esa orden.");
            var textoDiag = (diagnostico ?? "").Trim();
            var textoObs = (observaciones ?? "").Trim();
            await repositorio.GuardarDiagnosticoAsync(id, textoDiag, textoObs);
        }
    }
}
