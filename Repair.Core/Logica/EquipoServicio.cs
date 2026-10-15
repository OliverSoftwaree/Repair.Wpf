using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Modelos;
using Repair.Core.Integracion;

namespace Repair.Core.Logica
{
    public sealed class EquipoServicio : IConsultaEquipos
    {
        private readonly IEquipoRepositorio repositorio;
        public EquipoServicio(IEquipoRepositorio repositorio)
        {
            this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        public Task<IReadOnlyList<Equipo>> BuscarAsync(string texto)
        {
            texto = (texto ?? "").Trim();
            if (texto.Length > 100) throw new ArgumentException("La búsqueda admite hasta 100 caracteres.");
            return repositorio.BuscarAsync(texto);
        }

        public Task<Equipo> ObtenerAsync(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Selecciona un equipo válido.");
            return repositorio.ObtenerAsync(id);
        }

        public Task<int> RegistrarAsync(Equipo entrada)
        {
            if (entrada == null) throw new ArgumentNullException(nameof(entrada));
            var equipo = new Equipo
            {
                ClienteId = entrada.ClienteId,
                Marca = (entrada.Marca ?? "").Trim(),
                Modelo = (entrada.Modelo ?? "").Trim(),
                Serie = (entrada.Serie ?? "").Trim(),
                Accesorios = (entrada.Accesorios ?? "").Trim(),
                Condicion = (entrada.Condicion ?? "").Trim()
            };
            var errores = new Dictionary<string, string>();
            if (equipo.ClienteId <= 0) errores["ClienteId"] = "Selecciona un cliente válido.";
            if (equipo.Marca.Length < 1 || equipo.Marca.Length > 60 || equipo.Marca.Any(char.IsControl))
                errores["Marca"] = "Escribe la marca (1-60 caracteres, sin saltos de línea).";
            if (equipo.Modelo.Length < 1 || equipo.Modelo.Length > 100 || equipo.Modelo.Any(char.IsControl))
                errores["Modelo"] = "Escribe el modelo (1-100 caracteres, sin saltos de línea).";
            if (equipo.Serie.Length > 100 || equipo.Serie.Any(char.IsControl))
                errores["Serie"] = "La serie admite hasta 100 caracteres y sin saltos de línea.";
            if (equipo.Accesorios.Length > 300) errores["Accesorios"] = "Los accesorios admiten hasta 300 caracteres.";
            if (equipo.Condicion.Length < 5 || equipo.Condicion.Length > 500)
                errores["Condicion"] = "Describe la condición con 5 a 500 caracteres.";
            if (errores.Count > 0) throw new ValidacionException(errores);

            // Prevención básica de duplicados: mismo cliente, misma marca, modelo y serie.
            var existentes = repositorio.BuscarAsync("").GetAwaiter().GetResult();
            bool duplicado = existentes.Any(e => e.ClienteId == equipo.ClienteId &&
                string.Equals(e.Marca, equipo.Marca, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Modelo, equipo.Modelo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Serie ?? string.Empty, equipo.Serie ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (duplicado) throw new ValidacionException(new Dictionary<string, string> { { "Duplicado", "Ya existe un equipo con la misma marca, modelo y serie para este cliente." } });

            return repositorio.AgregarAsync(equipo);
        }

        public async Task ActualizarAsync(Equipo entrada)
        {
            if (entrada == null) throw new ArgumentNullException(nameof(entrada));
            if (entrada.Id <= 0) throw new ArgumentOutOfRangeException(nameof(entrada.Id));
            var equipo = new Equipo
            {
                Id = entrada.Id,
                ClienteId = entrada.ClienteId,
                Marca = (entrada.Marca ?? "").Trim(),
                Modelo = (entrada.Modelo ?? "").Trim(),
                Serie = (entrada.Serie ?? "").Trim(),
                Accesorios = (entrada.Accesorios ?? "").Trim(),
                Condicion = (entrada.Condicion ?? "").Trim()
            };
            var errores = new Dictionary<string, string>();
            if (equipo.ClienteId <= 0) errores["ClienteId"] = "Selecciona un cliente válido.";
            if (equipo.Marca.Length < 1 || equipo.Marca.Length > 60 || equipo.Marca.Any(char.IsControl))
                errores["Marca"] = "Escribe la marca (1-60 caracteres, sin saltos de línea).";
            if (equipo.Modelo.Length < 1 || equipo.Modelo.Length > 100 || equipo.Modelo.Any(char.IsControl))
                errores["Modelo"] = "Escribe el modelo (1-100 caracteres, sin saltos de línea).";
            if (equipo.Serie.Length > 100 || equipo.Serie.Any(char.IsControl))
                errores["Serie"] = "La serie admite hasta 100 caracteres y sin saltos de línea.";
            if (equipo.Accesorios.Length > 300) errores["Accesorios"] = "Los accesorios admiten hasta 300 caracteres.";
            if (equipo.Condicion.Length < 5 || equipo.Condicion.Length > 500)
                errores["Condicion"] = "Describe la condición con 5 a 500 caracteres.";
            if (errores.Count > 0) throw new ValidacionException(errores);

            var existentes = await repositorio.BuscarAsync("");
            bool duplicado = existentes.Any(e => e.Id != equipo.Id && e.ClienteId == equipo.ClienteId &&
                string.Equals(e.Marca, equipo.Marca, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Modelo, equipo.Modelo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Serie ?? string.Empty, equipo.Serie ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (duplicado) throw new ValidacionException(new Dictionary<string, string> { { "Duplicado", "Ya existe otro equipo con la misma marca, modelo y serie para este cliente." } });

            await repositorio.ActualizarAsync(equipo);
        }

        public Task EliminarAsync(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            return repositorio.EliminarAsync(id);
        }
    }
}

