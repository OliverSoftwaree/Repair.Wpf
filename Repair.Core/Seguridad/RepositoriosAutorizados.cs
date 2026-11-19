using System.Collections.Generic;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Modelos;

namespace Repair.Core.Seguridad
{
    public sealed class ClientesAutorizados : IClienteRepositorio
    {
        private readonly IClienteRepositorio origen; private readonly SesionUsuario sesion;
        public ClientesAutorizados(IClienteRepositorio origen, SesionUsuario sesion) { this.origen = origen; this.sesion = sesion; }
        public Task<IReadOnlyList<Cliente>> BuscarAsync(string texto) => origen.BuscarAsync(texto);
        public Task<Cliente> ObtenerAsync(int id) => origen.ObtenerAsync(id);
        public Task<int> AgregarAsync(Cliente cliente) { sesion.Exigir(Permiso.Recepcion); return origen.AgregarAsync(cliente); }
    }
    public sealed class EquiposAutorizados : IEquipoRepositorio
    {
        private readonly IEquipoRepositorio origen; private readonly SesionUsuario sesion;
        public EquiposAutorizados(IEquipoRepositorio origen, SesionUsuario sesion) { this.origen = origen; this.sesion = sesion; }
        public Task<IReadOnlyList<Equipo>> BuscarAsync(string texto) => origen.BuscarAsync(texto);
        public Task<Equipo> ObtenerAsync(int id) => origen.ObtenerAsync(id);
        public Task<int> AgregarAsync(Equipo equipo) { sesion.Exigir(Permiso.Recepcion); return origen.AgregarAsync(equipo); }
        public Task ActualizarAsync(Equipo equipo) { sesion.Exigir(Permiso.Recepcion); return origen.ActualizarAsync(equipo); }
        public Task EliminarAsync(int id) { sesion.Exigir(Permiso.Recepcion); return origen.EliminarAsync(id); }
    }
    public sealed class OrdenesAutorizadas : IOrdenRepositorio
    {
        private readonly IOrdenRepositorio origen; private readonly SesionUsuario sesion;
        public OrdenesAutorizadas(IOrdenRepositorio origen, SesionUsuario sesion) { this.origen = origen; this.sesion = sesion; }
        public Task<IReadOnlyList<OrdenTrabajo>> BuscarAsync(string texto) => origen.BuscarAsync(texto);
        public Task<OrdenTrabajo> ObtenerAsync(int id) => origen.ObtenerAsync(id);
        public Task<int> AgregarAsync(OrdenTrabajo orden) { sesion.Exigir(Permiso.Recepcion); return origen.AgregarAsync(orden); }
        public Task GuardarDiagnosticoAsync(int id, string diagnostico, string observaciones)
        { sesion.Exigir(Permiso.Diagnostico); return origen.GuardarDiagnosticoAsync(id, diagnostico, observaciones); }
    }
}
