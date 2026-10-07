using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using PanaderiaSystem.Models;
using System.Linq;
using System.Collections.Generic;

namespace PanaderiaSystem.Forms
{
    public class UC_Reportes : UserControl
    {
        private DateTimePicker dtpDesde, dtpHasta;
        private DataGridView dgvVentas, dgvProductos;
        private Label lblTotalVentas, lblEfectivo, lblTarjeta, lblTransferencia, lblNumVentas;
        private Panel panelResumen;
        private RichTextBox rtbCorte;

        public UC_Reportes()
        {
            InitializeComponent();
            ActualizarReporte();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);

            var lblTitulo = new Label
            {
                Text = "📊 Reportes y Consultas",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(10, 5), AutoSize = true
            };

            // Filtros
            var panelFiltros = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(940, 50),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            panelFiltros.Controls.AddRange(new Control[] {
                new Label { Text = "📅 Desde:", Location = new Point(10, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                dtpDesde = new DateTimePicker { Location = new Point(80, 12), Width = 150, Font = new Font("Segoe UI", 10), Value = DateTime.Today.AddDays(-7) },
                new Label { Text = "Hasta:", Location = new Point(245, 15), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                dtpHasta = new DateTimePicker { Location = new Point(295, 12), Width = 150, Font = new Font("Segoe UI", 10) },
            });

            var btnHoy = CrearBtnFiltro("Hoy", new Point(460, 10), () => {
                dtpDesde.Value = DateTime.Today; dtpHasta.Value = DateTime.Today; ActualizarReporte();
            });
            var btnSemana = CrearBtnFiltro("Esta semana", new Point(530, 10), () => {
                dtpDesde.Value = DateTime.Today.AddDays(-7); dtpHasta.Value = DateTime.Today; ActualizarReporte();
            });
            var btnMes = CrearBtnFiltro("Este mes", new Point(635, 10), () => {
                dtpDesde.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtpHasta.Value = DateTime.Today; ActualizarReporte();
            });
            var btnBuscar = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(735, 10),
                Width = 90, Height = 30,
                BackColor = Color.FromArgb(139, 90, 43),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => ActualizarReporte();

            var btnCorte = new Button
            {
                Text = "🖨 Corte de caja",
                Location = new Point(835, 10),
                Width = 95, Height = 30,
                BackColor = Color.FromArgb(100, 50, 180),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnCorte.FlatAppearance.BorderSize = 0;
            btnCorte.Click += (s, e) => MostrarCorte();

            panelFiltros.Controls.AddRange(new Control[] { btnHoy, btnSemana, btnMes, btnBuscar, btnCorte });

            // Panel resumen
            panelResumen = new Panel
            {
                Location = new Point(10, 100),
                Size = new Size(940, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            int xCard = 10;
            var tarjetas = new (string id, string titulo, Color color)[]
            {
                ("total", "Total Vendido", Color.FromArgb(46, 125, 50)),
                ("efectivo", "Efectivo", Color.FromArgb(21, 101, 192)),
                ("tarjeta", "Tarjeta", Color.FromArgb(123, 31, 162)),
                ("transfer", "Transferencia", Color.FromArgb(230, 81, 0)),
                ("ventas", "Num. Ventas", Color.FromArgb(0, 121, 107)),
            };

            foreach (var (id, titulo, color) in tarjetas)
            {
                var card = new Panel { Location = new Point(xCard, 5), Size = new Size(175, 68), BackColor = Color.FromArgb(248, 248, 248), BorderStyle = BorderStyle.FixedSingle };
                var lv = new Label { Location = new Point(10, 8), Width = 155, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = color, Text = "$0.00" };
                var lt = new Label { Location = new Point(10, 45), AutoSize = true, Font = new Font("Segoe UI", 8), ForeColor = Color.Gray, Text = titulo };

                if (id == "total") lblTotalVentas = lv;
                else if (id == "efectivo") lblEfectivo = lv;
                else if (id == "tarjeta") lblTarjeta = lv;
                else if (id == "transfer") lblTransferencia = lv;
                else lblNumVentas = lv;

                card.Controls.AddRange(new Control[] { lv, lt });
                panelResumen.Controls.Add(card);
                xCard += 183;
            }

            // Tab ventas y productos
            var tabs = new TabControl
            {
                Location = new Point(10, 190),
                Size = new Size(940, 355),
                Font = new Font("Segoe UI", 10)
            };

            var tabVentas = new TabPage("📋 Historial de Ventas");
            dgvVentas = CrearGrid(new[] {
                ("Folio", "Id", "N0"), ("Fecha", "Fecha", "g"), ("Total", "Total", "C2"),
                ("Método", "MetodoPago", ""), ("Cajero", "UsuarioNombre", "")
            });
            dgvVentas.Dock = DockStyle.Fill;
            tabVentas.Controls.Add(dgvVentas);

            var tabProductos = new TabPage("🏆 Productos más vendidos");
            dgvProductos = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", Width = 40 });
            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", FillWeight = 200 });
            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unidades vendidas" });
            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total generado", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            tabProductos.Controls.Add(dgvProductos);

            tabs.TabPages.Add(tabVentas);
            tabs.TabPages.Add(tabProductos);

            this.Controls.AddRange(new Control[] { lblTitulo, panelFiltros, panelResumen, tabs });
        }

        private Button CrearBtnFiltro(string texto, Point loc, Action accion)
        {
            var btn = new Button
            {
                Text = texto, Location = loc, Width = texto.Length > 6 ? 100 : 65, Height = 30,
                BackColor = Color.FromArgb(200, 175, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => accion();
            return btn;
        }

        private DataGridView CrearGrid((string header, string field, string format)[] cols)
        {
            var dgv = new DataGridView
            {
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(220, 210, 190)
            };
            foreach (var (h, f, fmt) in cols)
            {
                var col = new DataGridViewTextBoxColumn { HeaderText = h, DataPropertyName = f };
                if (!string.IsNullOrEmpty(fmt)) col.DefaultCellStyle.Format = fmt;
                dgv.Columns.Add(col);
            }
            return dgv;
        }

        private void ActualizarReporte()
        {
            var ventas = DatabaseManager.Instance.ObtenerVentas(dtpDesde.Value.Date, dtpHasta.Value.Date);

            dgvVentas.DataSource = ventas;

            decimal total = ventas.Sum(v => v.Total);
            decimal ef = ventas.Where(v => v.MetodoPago == MetodoPago.Efectivo).Sum(v => v.Total);
            decimal tj = ventas.Where(v => v.MetodoPago == MetodoPago.Tarjeta).Sum(v => v.Total);
            decimal tr = ventas.Where(v => v.MetodoPago == MetodoPago.Transferencia).Sum(v => v.Total);

            lblTotalVentas.Text = total.ToString("C2");
            lblEfectivo.Text = ef.ToString("C2");
            lblTarjeta.Text = tj.ToString("C2");
            lblTransferencia.Text = tr.ToString("C2");
            lblNumVentas.Text = ventas.Count.ToString();

            // Productos más vendidos
            var masVendidos = DatabaseManager.Instance.ObtenerProductosMasVendidos(dtpDesde.Value.Date, dtpHasta.Value.Date);
            dgvProductos.Rows.Clear();
            int rank = 1;
            foreach (var (prod, cant, totProd) in masVendidos)
                dgvProductos.Rows.Add(rank++, prod, cant, totProd);
        }

        private void MostrarCorte()
        {
            var resumen = DatabaseManager.Instance.ObtenerResumenDia(dtpDesde.Value.Date);
            var productos = DatabaseManager.Instance.ObtenerProductosMasVendidos(dtpDesde.Value.Date, dtpHasta.Value.Date);

            var sb = new StringBuilder();
            sb.AppendLine("╔════════════════════════════════╗");
            sb.AppendLine("║     🥐 PANADERÍA SISTEMA       ║");
            sb.AppendLine("║      CORTE DE CAJA             ║");
            sb.AppendLine("╚════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Fecha desde:   {dtpDesde.Value:dd/MM/yyyy}");
            sb.AppendLine($"Fecha hasta:   {dtpHasta.Value:dd/MM/yyyy}");
            sb.AppendLine($"Generado por:  {Session.UsuarioActual?.Nombre}");
            sb.AppendLine($"Hora:          {DateTime.Now:HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("════════════════════════════════");
            sb.AppendLine("RESUMEN DE VENTAS");
            sb.AppendLine("════════════════════════════════");
            sb.AppendLine($"{"Ventas totales:",-22} {resumen.total,12:C}");
            sb.AppendLine($"{"  Efectivo:",-22} {resumen.efectivo,12:C}");
            sb.AppendLine($"{"  Tarjeta:",-22} {resumen.tarjeta,12:C}");
            sb.AppendLine($"{"  Transferencia:",-22} {resumen.transferencia,12:C}");
            sb.AppendLine($"{"Número de ventas:",-22} {resumen.numVentas,12}");
            sb.AppendLine();
            sb.AppendLine("════════════════════════════════");
            sb.AppendLine("TOP 10 PRODUCTOS");
            sb.AppendLine("════════════════════════════════");
            int i = 1;
            foreach (var (prod, cant, totP) in productos)
                sb.AppendLine($"{i++,2}. {prod,-20} {cant,5} pzs   {totP,10:C}");
            sb.AppendLine();
            sb.AppendLine("════════════════════════════════");
            sb.AppendLine($"  Firma: ___________________");
            sb.AppendLine($"  Cajero: {Session.UsuarioActual?.Nombre}");

            var form = new Form
            {
                Text = "Corte de Caja",
                Size = new Size(500, 600),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };
            var txt = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Text = sb.ToString(), Font = new Font("Courier New", 10), ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None };
            var btnImpr = new Button { Text = "🖨 Imprimir", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(139, 90, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnImpr.FlatAppearance.BorderSize = 0;
            btnImpr.Click += (s, e) => {
                var pd = new System.Drawing.Printing.PrintDocument();
                pd.PrintPage += (sender, pe) => pe.Graphics.DrawString(sb.ToString(), new Font("Courier New", 9), Brushes.Black, pe.MarginBounds);
                new System.Windows.Forms.PrintPreviewDialog { Document = pd }.ShowDialog();
            };
            form.Controls.Add(txt);
            form.Controls.Add(btnImpr);
            form.ShowDialog();
        }
    }
}