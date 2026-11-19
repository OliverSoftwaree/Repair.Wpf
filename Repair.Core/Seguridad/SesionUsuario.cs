using System;

namespace Repair.Core.Seguridad
{
    public enum Permiso { Recepcion, Diagnostico, Reparacion, Entrega, Administracion }

    public sealed class SesionUsuario
    {
        public SesionUsuario(int id, string usuario, string rol)
        {
            if (rol != "Administrador" && rol != "Recepcionista" && rol != "Técnico") throw new ArgumentException("Rol desconocido.");
            Id = id; Usuario = usuario; Rol = rol;
        }
        public int Id { get; }
        public string Usuario { get; }
        public string Rol { get; }
        public bool Permite(Permiso permiso) => Rol == "Administrador" ||
            (Rol == "Recepcionista" && (permiso == Permiso.Recepcion || permiso == Permiso.Entrega)) ||
            (Rol == "Técnico" && (permiso == Permiso.Diagnostico || permiso == Permiso.Reparacion));
        public void Exigir(Permiso permiso)
        {
            if (!Permite(permiso)) throw new UnauthorizedAccessException("Tu rol no permite esta operación.");
        }
    }
}
