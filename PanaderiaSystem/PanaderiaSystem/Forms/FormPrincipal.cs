using System;
using System.Drawing;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using System.Linq;

namespace PanaderiaSystem.Forms
{
    public class FormPrincipal : Form
    {
        private Panel panelMenu;
        private Panel panelContenido;
        private Panel panelHeader;
        private Label lblUsuarioActual;
        private Button btnActivo;

        public FormPrincipal()
        {
            InitializeComponent();
            CargarResumenDia();
        }

        private void InitializeComponent()
        {
            this.Text = "Sistema Panadería";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 240, 230);
            this.MinimumSize = new Size(1100, 650);

            // === HEADER ===
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(139, 90, 43)
            };

            var lblLogo = new Label
            {
                Text = "🥐 Sistema Panadería",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };

            lblUsuarioActual = new Label
            {
                Text = $"👤 {Session.UsuarioActual?.Nombre}  |  {Session.UsuarioActual?.Rol}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(255, 220, 150),
                AutoSize = true,
                Location = new Point(0, 20)
            };

            var btnSalir = new Button
            {
                Text = "Cerrar Sesión",
                Size = new Size(110, 35),
                Location = new Point(1060, 12),
                BackColor = Color.FromArgb(180, 60, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.Click += (s, e) => {
                Session.CerrarSesion();
                Application.Restart();
            };

            // Centrar lblUsuarioActual
            panelHeader.Resize += (s, e) => {
                lblUsuarioActual.Left = (panelHeader.Width - lblUsuarioActual.Width) / 2;
                btnSalir.Left = panelHeader.Width - 130;
            };

            panelHeader.Controls.AddRange(new Control[] { lblLogo, lblUsuarioActual, btnSalir });

            // === MENÚ LATERAL ===
            panelMenu = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(60, 40, 20)
            };

            string[] menus = {
                "🛒 Punto de Venta",
                "📦 Inventario",
                "🍞 Catálogo",
                "📊 Reportes",
                "👥 Clientes",
                "🏭 Producción",
                "👤 Usuarios",
                "📈 Estadísticas"
            };

            int yPos = 10;
            foreach (var menu in menus)
            {
                var btn = new Button
                {
                    Text = menu,
                    Width = 195,
                    Height = 50,
                    Location = new Point(2, yPos),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 40, 20),
                    ForeColor = Color.FromArgb(220, 190, 140),
                    Font = new Font("Segoe UI", 10),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(15, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = menu
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 65, 30);
                btn.Click += MenuClick;

                // Permisos
                if (menu.Contains("Usuarios") && !Session.EsAdmin)
                    btn.Enabled = false;
                if (menu.Contains("Inventario") && !Session.EsAdmin && !Session.EsProduccion)
                    btn.Enabled = false;

                panelMenu.Controls.Add(btn);
                yPos += 52;
            }

            // === CONTENIDO ===
            panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 240, 230),
                Padding = new Padding(10)
            };

            this.Controls.Add(panelContenido);
            this.Controls.Add(panelMenu);
            this.Controls.Add(panelHeader);
        }

        private void MenuClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            // Resaltar activo
            if (btnActivo != null)
            {
                btnActivo.BackColor = Color.FromArgb(60, 40, 20);
                btnActivo.ForeColor = Color.FromArgb(220, 190, 140);
            }
            btn.BackColor = Color.FromArgb(139, 90, 43);
            btn.ForeColor = Color.White;
            btnActivo = btn;

            panelContenido.Controls.Clear();

            string tag = btn.Tag.ToString();
            UserControl uc = new UC_Inventario();

            if (tag.Contains("Punto de Venta")) uc = new UC_PuntoVenta();
            else if (tag.Contains("Inventario")) uc = new UC_Inventario();
            else if (tag.Contains("Catálogo")) uc = new UC_Catalogo();
            else if (tag.Contains("Reportes")) uc = new UC_Reportes();
            else if (tag.Contains("Clientes")) uc = new UC_Clientes();
            else if (tag.Contains("Producción")) uc = new UC_Produccion();
            else if (tag.Contains("Usuarios")) uc = new UC_Usuarios();
            else if (tag.Contains("Estadísticas")) uc = new UC_Estadisticas();

            if (uc != null)
            {
                uc.Dock = DockStyle.Fill;
                panelContenido.Controls.Add(uc);
            }
        }

        private void CargarResumenDia()
        {
            // Mostrar dashboard inicial
            var panelDash = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 240, 230) };

            var lblBienvenida = new Label
            {
                Text = $"¡Bienvenido, {Session.UsuarioActual?.Nombre}!",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(30, 30),
                AutoSize = true
            };

            var lblFecha = new Label
            {
                Text = $"📅 {DateTime.Now:dddd, dd MMMM yyyy}",
                Font = new Font("Segoe UI", 13),
                ForeColor = Color.Gray,
                Location = new Point(30, 75),
                AutoSize = true
            };

            var resumen = DatabaseManager.Instance.ObtenerResumenDia(DateTime.Today);
            var tarjetas = new (string titulo, string valor, string emoji, Color color)[]
            {
                ("Ventas Hoy", $"${resumen.total:N2}", "💰", Color.FromArgb(46, 125, 50)),
                ("En Efectivo", $"${resumen.efectivo:N2}", "💵", Color.FromArgb(21, 101, 192)),
                ("Con Tarjeta", $"${resumen.tarjeta:N2}", "💳", Color.FromArgb(123, 31, 162)),
                ("Transferencias", $"${resumen.transferencia:N2}", "📱", Color.FromArgb(230, 81, 0)),
                ("Num. Ventas", $"{resumen.numVentas}", "🧾", Color.FromArgb(0, 121, 107)),
            };

            int xCard = 30;
            foreach (var t in tarjetas)
            {
                var card = CrearTarjeta(t.titulo, t.valor, t.emoji, t.color);
                card.Location = new Point(xCard, 130);
                panelDash.Controls.Add(card);
                xCard += 180;
            }

            var lblAcceso = new Label
            {
                Text = "⚡ Acceso rápido:",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(30, 280),
                AutoSize = true
            };

            var botones = new (string texto, string menu, Color color)[] {
                ("🛒 Nueva Venta", "🛒 Punto de Venta", Color.FromArgb(139, 90, 43)),
                ("📊 Ver Reportes", "📊 Reportes", Color.FromArgb(46, 125, 50)),
                ("🍞 Catálogo", "🍞 Catálogo", Color.FromArgb(21, 101, 192)),
            };

            int xBtn = 30;
            foreach (var b in botones)
            {
                var btn = new Button
                {
                    Text = b.texto,
                    Size = new Size(160, 55),
                    Location = new Point(xBtn, 315),
                    BackColor = b.color,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = b.menu
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += MenuClick;
                panelDash.Controls.Add(btn);
                xBtn += 175;
            }

            panelDash.Controls.Add(lblBienvenida);
            panelDash.Controls.Add(lblFecha);
            panelDash.Controls.Add(lblAcceso);
            panelContenido.Controls.Add(panelDash);
        }

        private Panel CrearTarjeta(string titulo, string valor, string emoji, Color color)
        {
            var panel = new Panel
            {
                Size = new Size(165, 115),
                BackColor = Color.White
            };

            panel.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(color), 0, 0, 5, 115);
            };

            var lblEmoji = new Label
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 24),
                Location = new Point(15, 12),
                AutoSize = true
            };

            var lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(15, 55),
                AutoSize = true
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(15, 88),
                AutoSize = true
            };

            panel.Controls.AddRange(new Control[] { lblEmoji, lblValor, lblTitulo });
            return panel;
        }
    }
}