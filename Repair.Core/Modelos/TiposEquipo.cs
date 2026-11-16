using System;
using System.Collections.Generic;
using System.Linq;

namespace Repair.Core.Modelos
{
    public static class TiposEquipo
    {
        public const string SinEspecificar = "Sin especificar";
        public static IReadOnlyList<string> Opciones { get; } = Array.AsReadOnly(new[]
        {
            "Laptop", "Computadora de escritorio", "Computadora todo en uno", "Monitor",
            "Impresora", "Escáner", "Teléfono", "Tableta", "Audífonos", "Bocinas",
            "Teclado", "Mouse", "Router", "Switch de red", "Proyector", "UPS",
            "Consola de videojuegos", "Otro equipo electrónico"
        });

        public static string Normalizar(string valor)
        {
            valor = (valor ?? "").Trim();
            return Opciones.FirstOrDefault(opcion => string.Equals(opcion, valor, StringComparison.OrdinalIgnoreCase));
        }
    }
}
