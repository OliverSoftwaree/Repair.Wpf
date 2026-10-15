using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Integracion
{
    // Solo lectura: permite consultar equipos sin exponer la lógica de registro.
    public interface IConsultaEquipos
    {
        Task<IReadOnlyList<Equipo>> BuscarAsync(string texto);
        Task<Equipo> ObtenerAsync(int id);
    }
}
