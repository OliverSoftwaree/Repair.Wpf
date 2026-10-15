using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Repair.Core.Datos;
using Repair.Core.Integracion;
using Repair.Core.Modelos;

namespace Repair.Core.Logica
{
    public sealed class ClienteServicio : IConsultaClientes
    {
        private readonly IClienteRepositorio repositorio;
        public ClienteServicio(IClienteRepositorio repositorio)
        { this.repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio)); }

        public Task<IReadOnlyList<Cliente>> BuscarAsync(string texto)
        {
            texto = (texto ?? "").Trim();
            if (texto.Length > 100) throw new ArgumentException("La búsqueda admite hasta 100 caracteres.");
            return repositorio.BuscarAsync(texto);
        }

        public Task<Cliente> ObtenerAsync(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Selecciona un cliente válido.");
            return repositorio.ObtenerAsync(id);
        }

        public Task<int> RegistrarAsync(Cliente entrada)
        {
            if (entrada == null) throw new ArgumentNullException(nameof(entrada));
            // Se crea una copia normalizada para no modificar el objeto del formulario.
            var cliente = new Cliente
            {
                Nombre = (entrada.Nombre ?? "").Trim(), Telefono = (entrada.Telefono ?? "").Trim(),
                Correo = (entrada.Correo ?? "").Trim(), Notas = (entrada.Notas ?? "").Trim()
            };
            var errores = new Dictionary<string, string>();
            if (cliente.Nombre.Length < 2 || cliente.Nombre.Length > 100 ||
                !cliente.Nombre.Any(char.IsLetter) || cliente.Nombre.Any(char.IsControl))
                errores["Nombre"] = "Escribe un nombre de 2 a 100 caracteres, con alguna letra y sin saltos de línea.";
            int digitos = cliente.Telefono.Count(char.IsDigit);
            if (cliente.Telefono.Length > 25 || !Regex.IsMatch(cliente.Telefono, @"^[0-9+() .-]+$") || digitos < 7 || digitos > 15)
                errores["Telefono"] = "Escribe un teléfono de 7 a 15 dígitos; admite +, espacios, paréntesis, punto y guion (máximo 25 caracteres).";
            if (cliente.Correo.Length > 150 || !CorreoValido(cliente.Correo))
                errores["Correo"] = "Escribe un correo válido de hasta 150 caracteres o déjalo vacío.";
            if (cliente.Notas.Length > 300) errores["Notas"] = "Las notas admiten hasta 300 caracteres.";
            if (errores.Count > 0) throw new ValidacionException(errores);
            return repositorio.AgregarAsync(cliente);
        }

        private static bool CorreoValido(string valor)
        {
            if (valor.Length == 0) return true;
            if (valor.Any(char.IsWhiteSpace)) return false;
            try
            {
                var correo = new MailAddress(valor);
                return correo.Address == valor && correo.Host.Contains(".");
            }
            catch (FormatException) { return false; }
        }
    }
}
