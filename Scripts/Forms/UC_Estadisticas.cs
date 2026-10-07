using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PanaderiaSystem.Data;

namespace PanaderiaSystem.Forms
{
    public class UC_Estadisticas : UserControl
    {
        private Panel panelGraficas;
        private ComboBox cmbPeriodo;

        public UC_Estadisticas()
        {
            InitializeComponent();
            GenerarGraficas();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);

            var lblTitulo = new Label
            {
                Text = "📈 Estadísticas e Indicadores",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(10, 5), AutoSize = true
            };

            var lblPeriodo = new Label { Text = "Período:", Location = new Point(10, 44), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            cmbPeriodo = new ComboBox
            {
                Location = new Point(70, 41),
                Width = 200,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPeriodo.Items.AddRange(new object[] { "Últimos 7 días", "Últimos 30 días", "Este mes", "Últimos 3 meses" });
            cmbPeriodo.SelectedIndex = 1;
            cmbPeriodo.SelectedIndexChanged += (s, e) => GenerarGraficas();

            var btnRefresh = new Button
            {
                Text = "🔄 Actualizar",
                Location = new Point(285, 38),
                Width = 110, Height = 32,
                BackColor = Color.FromArgb(139, 90, 43),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => GenerarGraficas();

            panelGraficas = new Panel
            {
                Location = new Point(10, 80),
                Size = new Size(940, 470),
                AutoScroll = true,
                BackColor = Color.FromArgb(245, 240, 230)
            };

            this.Controls.AddRange(new Control[] { lblTitulo, lblPeriodo, cmbPeriodo, btnRefresh, panelGraficas });
        }

        private (DateTime desde, DateTime hasta) ObtenerRango()
        {
            return cmbPeriodo.SelectedIndex switch
            {
                0 => (DateTime.Today.AddDays(-7), DateTime.Today),
                1 => (DateTime.Today.AddDays(-30), DateTime.Today),
                2 => (new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today),
                3 => (DateTime.Today.AddDays(-90), DateTime.Today),
                _ => (DateTime.Today.AddDays(-30), DateTime.Today)
            };
        }

        private void GenerarGraficas()
        {
            panelGraficas.Controls.Clear();

            var (desde, hasta) = ObtenerRango();

            // === GRÁFICA 1: Ventas por día (barras) ===
            var ventasDia = DatabaseManager.Instance.ObtenerVentasPorDia(desde, hasta);
            var grafica1 = new GraficaBarras
            {
                Titulo = "📊 Ventas por Día",
                Datos = ventasDia.Select(v => (v.fecha, v.total)).ToList(),
                Color1 = Color.FromArgb(139, 90, 43),
                Location = new Point(0, 0),
                Size = new Size(470, 220)
            };

            // === GRÁFICA 2: Top productos (barras horizontales) ===
            var topProd = DatabaseManager.Instance.ObtenerProductosMasVendidos(desde, hasta, 8);
            var grafica2 = new GraficaBarrasHorizontal
            {
                Titulo = "🏆 Productos más vendidos",
                Datos = topProd.Select(p => (p.producto, (decimal)p.cantidad)).ToList(),
                Colors = new[] { Color.FromArgb(46, 125, 50), Color.FromArgb(21, 101, 192), Color.FromArgb(123, 31, 162), Color.FromArgb(230, 81, 0), Color.FromArgb(0, 121, 107), Color.FromArgb(183, 28, 28), Color.FromArgb(62, 39, 35), Color.FromArgb(74, 20, 140) },
                Location = new Point(475, 0),
                Size = new Size(460, 220)
            };

            // === GRÁFICA 3: Métodos de pago (pastel) ===
            var ventas = DatabaseManager.Instance.ObtenerVentas(desde, hasta);
            decimal ef = ventas.Where(v => v.MetodoPago == Models.MetodoPago.Efectivo).Sum(v => v.Total);
            decimal tj = ventas.Where(v => v.MetodoPago == Models.MetodoPago.Tarjeta).Sum(v => v.Total);
            decimal tr = ventas.Where(v => v.MetodoPago == Models.MetodoPago.Transferencia).Sum(v => v.Total);

            var grafica3 = new GraficaPastel
            {
                Titulo = "💳 Métodos de Pago",
                Datos = new List<(string, decimal)> { ("Efectivo", ef), ("Tarjeta", tj), ("Transferencia", tr) },
                Colores = new[] { Color.FromArgb(46, 125, 50), Color.FromArgb(21, 101, 192), Color.FromArgb(230, 81, 0) },
                Location = new Point(0, 230),
                Size = new Size(300, 230)
            };

            // === PANEL KPI ===
            var panelKpi = new Panel { Location = new Point(310, 230), Size = new Size(625, 230), BackColor = Color.Transparent };

            decimal totalPeriodo = ventas.Sum(v => v.Total);
            decimal promedioVenta = ventas.Count > 0 ? ventas.Average(v => v.Total) : 0;
            int diasConVenta = ventasDia.Count;
            decimal promedioDia = diasConVenta > 0 ? totalPeriodo / diasConVenta : 0;

            var kpis = new (string label, string valor, string sub, Color color)[]
            {
                ("💰 Total del período", totalPeriodo.ToString("C"), $"{ventas.Count} ventas", Color.FromArgb(46, 125, 50)),
                ("📊 Promedio por venta", promedioVenta.ToString("C"), "por ticket", Color.FromArgb(21, 101, 192)),
                ("📅 Promedio diario", promedioDia.ToString("C"), $"{diasConVenta} días activos", Color.FromArgb(139, 90, 43)),
                ("🏆 Mejor producto", topProd.FirstOrDefault().producto ?? "N/A", topProd.FirstOrDefault().cantidad + " piezas", Color.FromArgb(123, 31, 162)),
            };

            int xKpi = 5, yKpi = 10;
            foreach (var (label, valor, sub, color) in kpis)
            {
                var card = new Panel { Location = new Point(xKpi, yKpi), Size = new Size(295, 95), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                card.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(color), 0, 0, 4, 95);
                card.Controls.Add(new Label { Text = label, Location = new Point(12, 8), AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.Gray });
                card.Controls.Add(new Label { Text = valor, Location = new Point(12, 28), AutoSize = true, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = color });
                card.Controls.Add(new Label { Text = sub, Location = new Point(12, 68), AutoSize = true, Font = new Font("Segoe UI", 8), ForeColor = Color.Gray });
                panelKpi.Controls.Add(card);

                xKpi += 310;
                if (xKpi > 320) { xKpi = 5; yKpi += 110; }
            }

            panelGraficas.Controls.AddRange(new Control[] { grafica1, grafica2, grafica3, panelKpi });
        }
    }

    // ==================== CLASES GRÁFICAS ====================

    public class GraficaBarras : Panel
    {
        public string Titulo { get; set; }
        public List<(string fecha, decimal total)> Datos { get; set; } = new();
        public Color Color1 { get; set; } = Color.SteelBlue;

        public GraficaBarras()
        {
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Título
            g.DrawString(Titulo, new Font("Segoe UI", 11, FontStyle.Bold), new SolidBrush(Color.FromArgb(80, 50, 20)), new PointF(10, 8));

            if (Datos == null || Datos.Count == 0)
            {
                g.DrawString("Sin datos para el período", new Font("Segoe UI", 10), Brushes.Gray, new PointF(50, 100));
                return;
            }

            int margenIzq = 60, margenDer = 20, margenTop = 35, margenBot = 40;
            int areaW = Width - margenIzq - margenDer;
            int areaH = Height - margenTop - margenBot;
            decimal maxVal = Datos.Max(d => d.total);
            if (maxVal == 0) maxVal = 1;

            // Grid lines
            for (int i = 0; i <= 4; i++)
            {
                int y = margenTop + (int)(areaH * (1 - i / 4.0));
                g.DrawLine(Pens.LightGray, margenIzq, y, margenIzq + areaW, y);
                decimal val = maxVal * i / 4;
                g.DrawString($"${val:N0}", new Font("Segoe UI", 7), Brushes.Gray, new PointF(2, y - 7));
            }

            int barW = Math.Max(8, areaW / Datos.Count - 4);
            int gap = (areaW - barW * Datos.Count) / (Datos.Count + 1);

            for (int i = 0; i < Datos.Count; i++)
            {
                var (fecha, total) = Datos[i];
                int barH = (int)(areaH * total / maxVal);
                int x = margenIzq + gap + i * (barW + gap);
                int y = margenTop + areaH - barH;

                using var brush = new LinearGradientBrush(
                    new Point(x, y), new Point(x, y + barH),
                    Color1, ControlPaint.Light(Color1, 0.5f));
                g.FillRectangle(brush, x, y, barW, barH);

                // Fecha label
                if (DateTime.TryParse(fecha, out var dt))
                {
                    var label = dt.ToString("d/M");
                    g.DrawString(label, new Font("Segoe UI", 6), Brushes.DimGray,
                        new PointF(x + barW / 2 - 8, margenTop + areaH + 4));
                }
            }

            // Eje X
            g.DrawLine(new Pen(Color.FromArgb(150, 150, 150)), margenIzq, margenTop + areaH, margenIzq + areaW, margenTop + areaH);
            // Eje Y
            g.DrawLine(new Pen(Color.FromArgb(150, 150, 150)), margenIzq, margenTop, margenIzq, margenTop + areaH);
        }
    }

    public class GraficaBarrasHorizontal : Panel
    {
        public string Titulo { get; set; }
        public List<(string nombre, decimal valor)> Datos { get; set; } = new();
        public Color[] Colors { get; set; }

        public GraficaBarrasHorizontal()
        {
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawString(Titulo, new Font("Segoe UI", 11, FontStyle.Bold), new SolidBrush(Color.FromArgb(80, 50, 20)), new PointF(10, 8));

            if (Datos == null || Datos.Count == 0)
            {
                g.DrawString("Sin datos", new Font("Segoe UI", 10), Brushes.Gray, new PointF(50, 100));
                return;
            }

            decimal maxVal = Datos.Max(d => d.valor);
            if (maxVal == 0) maxVal = 1;

            int margenIzq = 120, margenTop = 35, margenBot = 15, margenDer = 60;
            int areaW = Width - margenIzq - margenDer;
            int areaH = Height - margenTop - margenBot;
            int barH = Math.Max(15, areaH / Datos.Count - 6);
            int gap = (areaH - barH * Datos.Count) / (Datos.Count + 1);

            for (int i = 0; i < Datos.Count; i++)
            {
                var (nombre, valor) = Datos[i];
                int barW = (int)(areaW * valor / maxVal);
                int y = margenTop + gap + i * (barH + gap);
                Color c = Colors != null ? Colors[i % Colors.Length] : Color.SteelBlue;

                using var brush = new LinearGradientBrush(new Point(margenIzq, y), new Point(margenIzq + barW, y), c, ControlPaint.Light(c, 0.4f));
                g.FillRectangle(brush, margenIzq, y, barW, barH);

                var nombre2 = nombre.Length > 16 ? nombre.Substring(0, 14) + "…" : nombre;
                g.DrawString(nombre2, new Font("Segoe UI", 8), Brushes.DimGray, new PointF(5, y + barH / 2 - 7));
                g.DrawString(valor.ToString("N0"), new Font("Segoe UI", 8, FontStyle.Bold), new SolidBrush(c), new PointF(margenIzq + barW + 4, y + barH / 2 - 7));
            }
        }
    }

    public class GraficaPastel : Panel
    {
        public string Titulo { get; set; }
        public List<(string label, decimal valor)> Datos { get; set; } = new();
        public Color[] Colores { get; set; }

        public GraficaPastel()
        {
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawString(Titulo, new Font("Segoe UI", 11, FontStyle.Bold), new SolidBrush(Color.FromArgb(80, 50, 20)), new PointF(10, 8));

            if (Datos == null || Datos.Count == 0 || Datos.Sum(d => d.valor) == 0)
            {
                g.DrawString("Sin datos", new Font("Segoe UI", 10), Brushes.Gray, new PointF(50, 100));
                return;
            }

            decimal total = Datos.Sum(d => d.valor);
            int cx = Width / 2 - 20, cy = Height / 2 + 10, radio = Math.Min(Width, Height) / 2 - 30;
            float angulo = -90f;

            for (int i = 0; i < Datos.Count; i++)
            {
                if (Datos[i].valor <= 0) continue;
                float sweep = (float)(Datos[i].valor / total * 360);
                Color c = Colores != null ? Colores[i % Colores.Length] : Color.Gray;
                g.FillPie(new SolidBrush(c), cx - radio, cy - radio, radio * 2, radio * 2, angulo, sweep);
                g.DrawPie(Pens.White, cx - radio, cy - radio, radio * 2, radio * 2, angulo, sweep);

                // Leyenda
                int lyY = 36 + i * 20;
                g.FillRectangle(new SolidBrush(c), Width - 90, lyY, 12, 12);
                decimal pct = Datos[i].valor / total * 100;
                g.DrawString($"{Datos[i].label} {pct:N0}%", new Font("Segoe UI", 7), Brushes.DimGray, Width - 75, lyY);

                angulo += sweep;
            }
        }
    }
}