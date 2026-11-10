using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public interface IOrdenRepositorio
    {
        Task<IReadOnlyList<OrdenTrabajo>> BuscarAsync(string texto);
        Task<OrdenTrabajo> ObtenerAsync(int id);
        Task<int> AgregarAsync(OrdenTrabajo orden);
        Task GuardarDiagnosticoAsync(int id, string diagnostico, string observaciones);
    }
}
