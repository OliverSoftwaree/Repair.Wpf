using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Modelos;

namespace Repair.Comprobaciones
{
    // Exclusivo de comprobaciones. No se usa como alternativa automática a SQL Server.
    internal sealed class RepositorioPrueba : IClienteRepositorio
    {
        private readonly List<Cliente> filas = new List<Cliente>();
        internal int Altas { get; private set; }
        internal string UltimaBusqueda { get; private set; }
        internal bool Fallar { get; set; }

        public Task<int> AgregarAsync(Cliente cliente)
        {
            if (Fallar) throw new AccesoDatosException(new InvalidOperationException("Fallo simulado de prueba"));
            var copia = Copiar(cliente);
            copia.Id = filas.Count + 1;
            copia.CreadoUtc = DateTime.UtcNow;
            filas.Add(copia); Altas++;
            return Task.FromResult(copia.Id);
        }

        public Task<IReadOnlyList<Cliente>> BuscarAsync(string texto)
        {
            UltimaBusqueda = texto;
            return Task.FromResult((IReadOnlyList<Cliente>)filas.Where(c => texto.Length == 0 || c.Id.ToString() == texto ||
                Contiene(c.Nombre, texto) || Contiene(c.Telefono, texto) || Contiene(c.Correo, texto))
                .OrderByDescending(c => c.Id).Select(Copiar).ToList());
        }

        public Task<Cliente> ObtenerAsync(int id)
        { return Task.FromResult(Copiar(filas.FirstOrDefault(c => c.Id == id))); }

        private static bool Contiene(string valor, string texto)
        { return (valor ?? "").IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0; }

        private static Cliente Copiar(Cliente c)
        { return c == null ? null : new Cliente { Id = c.Id, Nombre = c.Nombre, Telefono = c.Telefono, Correo = c.Correo, Notas = c.Notas, CreadoUtc = c.CreadoUtc }; }
    }
}
