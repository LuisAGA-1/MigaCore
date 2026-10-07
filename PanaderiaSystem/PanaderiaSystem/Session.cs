using PanaderiaSystem.Models;

namespace PanaderiaSystem
{
    public static class Session
    {
        public static Usuario UsuarioActual { get; set; }

        public static bool EsAdmin => UsuarioActual?.Rol == RolUsuario.Administrador;
        public static bool EsCajero => UsuarioActual?.Rol == RolUsuario.Cajero || EsAdmin;
        public static bool EsProduccion => UsuarioActual?.Rol == RolUsuario.Produccion || EsAdmin;

        public static void CerrarSesion()
        {
            UsuarioActual = null;
        }
    }
}
