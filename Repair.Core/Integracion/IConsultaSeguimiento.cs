using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Integracion
{
    public interface IConsultaSeguimiento
    {
        Task<OrdenSeguimiento> BuscarPorIdAsync(int id);
    }
}
