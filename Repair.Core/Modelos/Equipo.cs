using System;

namespace Repair.Core.Modelos
{
    public sealed class Equipo
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Serie { get; set; }
        public string Accesorios { get; set; }
        public string Condicion { get; set; }
        public DateTime CreadoUtc { get; set; }
    }
}
