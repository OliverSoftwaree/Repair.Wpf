using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Integracion
{
    // Solo lectura: Oliver puede seleccionar un cliente existente sin duplicar su lógica.
    public interface IConsultaClientes
    {
        Task<IReadOnlyList<Cliente>> BuscarAsync(string texto);
        Task<Cliente> ObtenerAsync(int id);
    }
}
