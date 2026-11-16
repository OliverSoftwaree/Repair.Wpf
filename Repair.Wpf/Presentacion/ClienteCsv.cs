using System;
using System.Collections.Generic;
using System.Text;

namespace Repair.Wpf.Presentacion
{
    // Exporta la misma instantánea que se muestra en el directorio, sin consultar equipos.
    public static class ClienteCsv
    {
        public static string Crear(IEnumerable<ClienteFila> filas)
        {
            if (filas == null) throw new ArgumentNullException(nameof(filas));
            var texto = new StringBuilder("\"Codigo\",\"Nombre\",\"Telefono\",\"Correo\"\r\n");
            foreach (var fila in filas)
            {
                if (fila == null) throw new ArgumentException("La lista contiene una fila vacía.", nameof(filas));
                texto.Append(Celda(fila.Codigo)).Append(',').Append(Celda(fila.Nombre)).Append(',')
                    .Append(Celda(fila.Telefono)).Append(',').Append(Celda(fila.Correo)).Append("\r\n");
            }
            return texto.ToString();
        }

        private static string Celda(string valor)
        {
            valor = valor ?? "";
            // Las comillas CSV por sí solas no impiden fórmulas al abrir una hoja de cálculo.
            string inicio = valor.TrimStart();
            if ((inicio.Length > 0 && "=+-@".IndexOf(inicio[0]) >= 0) ||
                (valor.Length > 0 && "\t\r\n".IndexOf(valor[0]) >= 0)) valor = "'" + valor;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
    }
}
