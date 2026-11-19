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
                EquipoId = orden.EquipoId,
                Estado = (orden.Estado ?? "Pendiente").Trim(),
                Diagnostico = (orden.Diagnostico ?? "").Trim(),
                Observaciones = (orden.Observaciones ?? "").Trim(),
                CreadoUtc = DateTime.UtcNow
            };
            var errores = new Dictionary<string, string>();
            if (copia.ClienteId <= 0) errores["ClienteId"] = "Selecciona un cliente válido.";
            if (copia.Estado != "Pendiente") errores["Estado"] = "Una orden nueva debe iniciar Pendiente.";
            if (!copia.EquipoId.HasValue || copia.EquipoId.Value <= 0) errores["EquipoId"] = "Vincula la orden nueva al equipo recibido.";
            if (errores.Count > 0) throw new ValidacionException(errores);
            return repositorio.AgregarAsync(copia);
        }

        public async Task GuardarDiagnosticoAsync(int id, string diagnostico, string observaciones)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "La orden debe ser válida.");
            if (string.IsNullOrWhiteSpace(diagnostico)) throw new ValidacionException(new Dictionary<string, string> { { "Diagnostico", "Escribe el diagnóstico encontrado." } });
            var orden = await repositorio.ObtenerAsync(id);
            if (orden == null) throw new InvalidOperationException("No existe esa orden.");
            if (diagnostico.Length > 4000 || (observaciones ?? "").Length > 4000)
                throw new ArgumentException("Diagnóstico y observaciones admiten 4000 caracteres cada uno.");
            var textoDiag = (diagnostico ?? "").Trim();
            var textoObs = (observaciones ?? "").Trim();
            await repositorio.GuardarDiagnosticoAsync(id, textoDiag, textoObs);
        }
    }
}
