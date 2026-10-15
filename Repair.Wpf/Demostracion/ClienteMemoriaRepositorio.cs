using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Modelos;

namespace Repair.Wpf.Demostracion
{
    // Solo se elige de forma explícita. No recibe datos de bases personales.
    public sealed class ClienteMemoriaRepositorio : IClienteRepositorio
    {
        private readonly List<Cliente> filas = new List<Cliente>();
        public ClienteMemoriaRepositorio()
        {
            Semilla("Ana de Ejemplo", "809-555-0101", "ana@example.test", "Prefiere contacto por teléfono.");
            Semilla("Luis de Ejemplo", "809-555-0102", "luis@example.test", "Datos ficticios para explorar la interfaz.");
            Semilla("Mara de Ejemplo", "809-555-0103", "", "Cliente de muestra sin correo.");
        }
        private void Semilla(string nombre, string telefono, string correo, string notas)
        { filas.Add(new Cliente { Id = filas.Count + 1, Nombre = nombre, Telefono = telefono, Correo = correo, Notas = notas, CreadoUtc = DateTime.UtcNow }); }
        public Task<int> AgregarAsync(Cliente cliente)
        {
            var copia = Copiar(cliente); copia.Id = filas.Count + 1; copia.CreadoUtc = DateTime.UtcNow;
            filas.Add(copia); return Task.FromResult(copia.Id);
        }
        public Task<IReadOnlyList<Cliente>> BuscarAsync(string texto)
        {
            return Task.FromResult((IReadOnlyList<Cliente>)filas.Where(c => texto.Length == 0 || c.Id.ToString() == texto ||
                Contiene(c.Nombre, texto) || Contiene(c.Telefono, texto) || Contiene(c.Correo, texto))
                .OrderByDescending(c => c.Id).Select(Copiar).ToList());
        }
        public Task<Cliente> ObtenerAsync(int id) { return Task.FromResult(Copiar(filas.FirstOrDefault(c => c.Id == id))); }
        private static bool Contiene(string valor, string texto) { return (valor ?? "").IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0; }
        private static Cliente Copiar(Cliente c) { return c == null ? null : new Cliente { Id = c.Id, Nombre = c.Nombre, Telefono = c.Telefono, Correo = c.Correo, Notas = c.Notas, CreadoUtc = c.CreadoUtc }; }
    }
}
