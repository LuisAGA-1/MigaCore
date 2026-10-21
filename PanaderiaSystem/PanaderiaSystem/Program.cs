using System;
using System.IO;
using System.Windows.Forms;
using PanaderiaSystem.Forms;

namespace PanaderiaSystem
{
    internal static class Program
    {
        // %LOCALAPPDATA%\MigaCore\errores.log
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MigaCore", "errores.log");

        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ReportarError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportarError(e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var login = new FormLogin())
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new FormPrincipal());
                }
            }
        }

        // Un error no previsto (p. ej. se cayó PostgreSQL) se registra y se avisa, en vez de cerrar la caja sin mensaje
        private static void ReportarError(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { /* si no se puede escribir el log, al menos se muestra el mensaje */ }

            // PostgresException es un error de SQL (el servidor sí respondió), no de conexión
            var npg = ex as Npgsql.NpgsqlException ?? ex?.InnerException as Npgsql.NpgsqlException;
            string mensaje = npg != null && !(npg is Npgsql.PostgresException)
                ? "No hay conexión con la base de datos. Revisa que el servidor PostgreSQL esté encendido y vuelve a intentar."
                : "Ocurrió un error inesperado: " + ex?.Message;
            MessageBox.Show(mensaje + $"\n\nDetalle guardado en:\n{LogPath}", "MigaCore",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
