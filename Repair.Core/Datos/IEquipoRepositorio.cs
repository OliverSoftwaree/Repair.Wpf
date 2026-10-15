using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Modelos;

namespace Repair.Core.Datos
{
    public interface IEquipoRepositorio
    {
        Task<IReadOnlyList<Equipo>> BuscarAsync(string texto);
        Task<Equipo> ObtenerAsync(int id);
        Task<int> AgregarAsync(Equipo equipo);
        Task ActualizarAsync(Equipo equipo);
        Task EliminarAsync(int id);
    }
}
