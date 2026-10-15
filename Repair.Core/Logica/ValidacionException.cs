using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Repair.Core.Logica
{
    public sealed class ValidacionException : Exception
    {
        public IReadOnlyDictionary<string, string> Errores { get; private set; }
        public ValidacionException(IDictionary<string, string> errores)
            : base("Revisa los campos señalados. No se ha guardado el cliente.")
        { Errores = new ReadOnlyDictionary<string, string>(errores); }
    }
}
