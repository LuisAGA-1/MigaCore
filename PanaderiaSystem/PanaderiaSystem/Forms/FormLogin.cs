using System;
using System.Drawing;
using System.Windows.Forms;
using PanaderiaSystem.Data;

namespace PanaderiaSystem.Forms
{
    public class FormLogin : Form
    {
        private TextBox txtUsuario, txtPassword;
        private Button btnLogin;
        private Label lblError;

        public FormLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "SoftPan - Iniciar Sesión";
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.Size = new Size(420, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 240, 230);

            // Panel decorativo superior
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.FromArgb(139, 90, 43)
            };

            var picLogo = new PictureBox
            {
                Image = Image.FromStream(GetType().Assembly.GetManifestResourceStream("logo-softpan.png")),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(72, 72),
                Location = new Point((420 - 72) / 2, 12),
                BackColor = Color.Transparent
            };

            var lblTitulo = new Label
            {
                Text = "SOFTPAN",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 90),
                Width = 420
            };

            panelTop.Controls.Add(picLogo);
            panelTop.Controls.Add(lblTitulo);

            // Campos
            var lblU = new Label { Text = "Usuario", Location = new Point(60, 170), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) };
            txtUsuario = new TextBox
            {
                Location = new Point(60, 195),
                Width = 290,
                Height = 35,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblP = new Label { Text = "Contraseña", Location = new Point(60, 245), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) };
            txtPassword = new TextBox
            {
                Location = new Point(60, 270),
                Width = 290,
                Height = 35,
                Font = new Font("Segoe UI", 12),
                PasswordChar = '●',
                BorderStyle = BorderStyle.FixedSingle
            };
            txtPassword.KeyPress += (s, e) => { if (e.KeyChar == 13) Ingresar(); };

            lblError = new Label
            {
                Text = "",
                ForeColor = Color.Red,
                Location = new Point(60, 315),
                Width = 290,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnLogin = new Button
            {
                Text = "INGRESAR",
                Location = new Point(60, 345),
                Width = 290,
                Height = 45,
                BackColor = Color.FromArgb(139, 90, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => Ingresar();

            this.Controls.AddRange(new Control[] {
                panelTop, lblU, txtUsuario, lblP, txtPassword, lblError, btnLogin
            });
        }

        private void Ingresar()
        {
            var usuario = DatabaseManager.Instance.ValidarLogin(txtUsuario.Text.Trim(), txtPassword.Text);
            if (usuario != null)
            {
                if (usuario.DebeCambiarPassword && !PedirNuevaPassword(usuario))
                {
                    txtPassword.Clear();
                    return;
                }
                Session.UsuarioActual = usuario;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblError.Text = "Usuario o contraseña incorrectos";
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        // Contraseña temporal (admin inicial o usuario recién creado): se cambia antes de entrar
        private bool PedirNuevaPassword(Models.Usuario usuario)
        {
            var form = new Form
            {
                Text = "Cambiar contraseña",
                Size = new Size(360, 290),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(245, 240, 230)
            };
            var lblInfo = new Label { Text = "Tu contraseña es temporal. Escribe una nueva (mínimo 6 caracteres).", Location = new Point(20, 15), Size = new Size(300, 40), Font = new Font("Segoe UI", 9) };
            var lblN = new Label { Text = "Nueva contraseña", Location = new Point(20, 60), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var txtNueva = new TextBox { Location = new Point(20, 80), Width = 300, PasswordChar = '●', Font = new Font("Segoe UI", 11) };
            var lblC = new Label { Text = "Confirmar contraseña", Location = new Point(20, 115), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var txtConfirmar = new TextBox { Location = new Point(20, 135), Width = 300, PasswordChar = '●', Font = new Font("Segoe UI", 11) };
            var lblErr = new Label { Location = new Point(20, 170), Size = new Size(300, 20), ForeColor = Color.Red, Font = new Font("Segoe UI", 9) };
            var btnGuardar = new Button
            {
                Text = "GUARDAR",
                Location = new Point(20, 195),
                Size = new Size(300, 38),
                BackColor = Color.FromArgb(139, 90, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) =>
            {
                if (txtNueva.Text != txtConfirmar.Text) { lblErr.Text = "Las contraseñas no coinciden."; return; }
                if (txtNueva.Text == txtPassword.Text) { lblErr.Text = "Debe ser distinta a la temporal."; return; }
                try
                {
                    DatabaseManager.Instance.CambiarPassword(usuario.Id_usuario, txtNueva.Text);
                    form.DialogResult = DialogResult.OK;
                }
                catch (ArgumentException ex) { lblErr.Text = ex.Message; }
            };
            form.AcceptButton = btnGuardar;
            form.Controls.AddRange(new Control[] { lblInfo, lblN, txtNueva, lblC, txtConfirmar, lblErr, btnGuardar });
            using (form) return form.ShowDialog(this) == DialogResult.OK;
        }
    }
}