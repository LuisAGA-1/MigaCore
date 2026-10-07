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
            this.Text = "Sistema Panadería - Iniciar Sesión";
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

            var lblIcono = new Label
            {
                Text = "🥐",
                Font = new Font("Segoe UI Emoji", 42),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 15),
                Width = 420
            };

            var lblTitulo = new Label
            {
                Text = "PANADERÍA SISTEMA",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 90),
                Width = 420
            };

            panelTop.Controls.Add(lblIcono);
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

            var lblDemo = new Label
            {
                Text = "Admin: admin / admin123  |  Cajero: cajero / cajero123",
                Location = new Point(20, 405),
                Width = 380,
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.AddRange(new Control[] {
                panelTop, lblU, txtUsuario, lblP, txtPassword, lblError, btnLogin, lblDemo
            });
        }

        private void Ingresar()
        {
            var usuario = DatabaseManager.Instance.ValidarLogin(txtUsuario.Text.Trim(), txtPassword.Text);
            if (usuario != null)
            {
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
    }
}