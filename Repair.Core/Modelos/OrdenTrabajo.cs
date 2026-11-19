using System;

namespace Repair.Core.Modelos
{
    public sealed class OrdenTrabajo
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int? EquipoId { get; set; }
        public DateTime? EntregadaUtc { get; set; }
        public string Estado { get; set; }
        public string Diagnostico { get; set; }
        public string Observaciones { get; set; }
        public DateTime CreadoUtc { get; set; }
    }
}
