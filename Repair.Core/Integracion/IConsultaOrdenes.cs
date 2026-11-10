using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Integracion
{
    public interface IConsultaOrdenes
    {
        Task<IReadOnlyList<OrdenTrabajo>> BuscarAsync(string texto);
        Task<OrdenTrabajo> ObtenerAsync(int id);
    }
}
