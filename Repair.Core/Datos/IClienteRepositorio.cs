using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public interface IClienteRepositorio
    {
        Task<IReadOnlyList<Cliente>> BuscarAsync(string texto);
        Task<Cliente> ObtenerAsync(int id);
        Task<int> AgregarAsync(Cliente cliente);
    }
}
