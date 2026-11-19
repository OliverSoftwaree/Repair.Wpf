using System;

namespace Repair.Core.Modelos
{
    public sealed class OrdenSeguimiento
    {
        public OrdenSeguimiento(int id, int clienteId, string cliente, int? equipoId, string equipo,
            string estado, string diagnostico, string observaciones, DateTime? entregadaUtc)
        {
            Id = id;
            ClienteId = clienteId;
            Cliente = cliente;
            EquipoId = equipoId;
            Equipo = equipo;
            Estado = estado;
            Diagnostico = diagnostico;
            Observaciones = observaciones;
            EntregadaUtc = entregadaUtc;
        }

        public int Id { get; }
        public int ClienteId { get; }
        public string Cliente { get; }
        public int? EquipoId { get; }
        public string Equipo { get; }
        public string Estado { get; }
        public string Diagnostico { get; }
        public string Observaciones { get; }
        public DateTime? EntregadaUtc { get; }
    }
}
