using System;

namespace Repair.Core.Integracion
{
    // Propuesta para coordinar con Oliver. No registra, valida ni persiste equipos.
    public sealed class EquipoContrato
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Marca { get; set; }       // Máximo propuesto: 60
        public string Modelo { get; set; }      // 100
        public string Serie { get; set; }       // 100, opcional
        public string Accesorios { get; set; }  // 300, opcional
        public string Condicion { get; set; }   // 500
        public DateTime CreadoUtc { get; set; }
    }
}
