using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Modelos;

namespace Repair.Wpf.Demostracion
{
    public sealed class EquipoMemoriaRepositorio : IEquipoRepositorio
    {
        private readonly List<Equipo> filas = new List<Equipo>();
        public EquipoMemoriaRepositorio()
        {
            Semilla(1, "MarcaA", "ModeloX", "S123", "Cargador", "Pantalla rota");
            Semilla(2, "MarcaB", "ModeloY", "", "", "No enciende");
            Semilla(1, "MarcaC", "ModeloZ", "SN-999", "Auriculares", "Teclas pegadas");
        }
        private void Semilla(int clienteId, string marca, string modelo, string serie, string accesorios, string condicion)
        {
            filas.Add(new Equipo { Id = filas.Count + 1, ClienteId = clienteId, Marca = marca, Modelo = modelo, Serie = serie, Accesorios = accesorios, Condicion = condicion, CreadoUtc = DateTime.UtcNow });
        }
        public Task<int> AgregarAsync(Equipo equipo)
        {
            var copia = Copiar(equipo); copia.Id = filas.Count + 1; copia.CreadoUtc = DateTime.UtcNow;
            filas.Add(copia); return Task.FromResult(copia.Id);
        }
        public Task<IReadOnlyList<Equipo>> BuscarAsync(string texto)
        {
            texto = (texto ?? "").Trim();
            return Task.FromResult((IReadOnlyList<Equipo>)filas.Where(e => texto.Length == 0 || e.Id.ToString() == texto || e.ClienteId.ToString() == texto ||
                Contiene(e.Marca, texto) || Contiene(e.Modelo, texto) || Contiene(e.Serie, texto) || Contiene(e.Accesorios, texto) || Contiene(e.Condicion, texto))
                .OrderByDescending(e => e.Id).Select(Copiar).ToList());
        }
        public Task<Equipo> ObtenerAsync(int id) { return Task.FromResult(Copiar(filas.FirstOrDefault(e => e.Id == id))); }
        public Task ActualizarAsync(Equipo equipo)
        {
            var existente = filas.FirstOrDefault(f => f.Id == equipo.Id);
            if (existente != null)
            {
                existente.ClienteId = equipo.ClienteId;
                existente.Marca = equipo.Marca;
                existente.Modelo = equipo.Modelo;
                existente.Serie = equipo.Serie;
                existente.Accesorios = equipo.Accesorios;
                existente.Condicion = equipo.Condicion;
            }
            return Task.CompletedTask;
        }
        public Task EliminarAsync(int id)
        {
            filas.RemoveAll(f => f.Id == id);
            return Task.CompletedTask;
        }
        private static bool Contiene(string valor, string texto) { return (valor ?? "").IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0; }
        private static Equipo Copiar(Equipo e)
        {
            if (e == null) return null;
            return new Equipo { Id = e.Id, ClienteId = e.ClienteId, Marca = e.Marca, Modelo = e.Modelo, Serie = e.Serie, Accesorios = e.Accesorios, Condicion = e.Condicion, CreadoUtc = e.CreadoUtc };
        }
    }
}

