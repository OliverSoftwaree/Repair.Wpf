using System;

namespace Repair.Core.Modelos
{
    public sealed class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Notas { get; set; }
        public DateTime CreadoUtc { get; set; }
    }
}
