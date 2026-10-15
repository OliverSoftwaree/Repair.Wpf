using System;
using Repair.Core.Modelos;

namespace Repair.Wpf.Presentacion
{
    public sealed class ClienteFila
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public string Telefono { get; private set; }
        public string Correo { get; private set; }
        public string Inicial { get; private set; }
        public string Codigo { get { return "CL-" + Id.ToString("0000"); } }
        public string Fecha { get; private set; }

        public ClienteFila(Cliente cliente)
        {
            Id = cliente.Id; Nombre = cliente.Nombre; Telefono = cliente.Telefono;
            Correo = string.IsNullOrWhiteSpace(cliente.Correo) ? "Sin correo" : cliente.Correo;
            Inicial = string.IsNullOrWhiteSpace(Nombre) ? "?" : Nombre.Substring(0, 1).ToUpperInvariant();
            Fecha = cliente.CreadoUtc.ToLocalTime().ToString("dd MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-DO"));
        }
    }
}
