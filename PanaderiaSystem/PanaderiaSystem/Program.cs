using System;
using System.Windows.Forms;
using PanaderiaSystem.Forms;

namespace PanaderiaSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
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
    }
}