using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Forms
{
    public class UC_PuntoVenta : UserControl
    {
        private DataGridView dgvCarrito;
        private FlowLayoutPanel panelProductos;
        private TextBox txtBuscar;
        private Label lblTotal, lblCambio;
        private ComboBox cmbMetodoPago, cmbCliente;
        private TextBox txtEfectivo;
        private Panel panelPago;
        private List<DetalleVenta> carrito = new List<DetalleVenta>();

        public UC_PuntoVenta()
        {
            InitializeComponent();
            CargarProductos();
            CargarClientes();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);

            // === TÍTULO ===
            var lblTitulo = new Label
            {
                Text = "🛒 Punto de Venta",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(10, 5),
                AutoSize = true
            };

            // === PANEL IZQUIERDO - Productos ===
            var panelIzq = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(580, 580),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblBuscarTit = new Label
            {
                Text = "🔍 Buscar producto:",
                Location = new Point(8, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 50, 20)
            };

            txtBuscar = new TextBox
            {
                Location = new Point(130, 5),
                Width = 200,
                Height = 28,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtBuscar.TextChanged += (s, e) => FiltrarProductos(txtBuscar.Text);

            var btnLimpiarBusq = new Button
            {
                Text = "✕",
                Location = new Point(335, 5),
                Width = 30,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGray,
                Cursor = Cursors.Hand
            };
            btnLimpiarBusq.Click += (s, e) => { txtBuscar.Clear(); FiltrarProductos(""); };

            panelProductos = new FlowLayoutPanel
            {
                Location = new Point(0, 38),
                Size = new Size(578, 538),
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 245, 238),
                WrapContents = true
            };

            panelIzq.Controls.AddRange(new Control[] {
                lblBuscarTit, txtBuscar, btnLimpiarBusq, panelProductos
            });

            // === PANEL DERECHO - Carrito ===
            var panelDer = new Panel
            {
                Location = new Point(600, 40),
                Size = new Size(370, 580),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblCarrito = new Label
            {
                Text = "🧾 Carrito",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20)
            };

            var btnLimpiar = new Button
            {
                Text = "🗑 Limpiar",
                Location = new Point(250, 5),
                Width = 100,
                Height = 30,
                BackColor = Color.FromArgb(200, 60, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) => LimpiarCarrito();

            dgvCarrito = new DataGridView
            {
                Location = new Point(5, 40),
                Size = new Size(358, 230),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(230, 220, 200)
            };
            dgvCarrito.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", DataPropertyName = "ProductoNombre", ReadOnly = true });
            dgvCarrito.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant", DataPropertyName = "Cantidad", Width = 50 });
            dgvCarrito.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", DataPropertyName = "PrecioUnitario", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgvCarrito.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = "Subtotal", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });

            var btnQuitarItem = new Button
            {
                Text = "❌ Quitar seleccionado",
                Location = new Point(5, 275),
                Width = 160,
                Height = 30,
                BackColor = Color.FromArgb(180, 60, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnQuitarItem.FlatAppearance.BorderSize = 0;
            btnQuitarItem.Click += (s, e) => QuitarItemSeleccionado();

            // Panel de pago
            panelPago = new Panel
            {
                Location = new Point(5, 310),
                Size = new Size(358, 260),
                BackColor = Color.FromArgb(248, 245, 238),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblMetodo = new Label { Text = "Método de pago:", Location = new Point(8, 8), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            cmbMetodoPago = new ComboBox
            {
                Location = new Point(8, 28),
                Width = 200,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMetodoPago.Items.AddRange(new object[] { "💵 Efectivo", "💳 Tarjeta", "📱 Transferencia" });
            cmbMetodoPago.SelectedIndex = 0;
            cmbMetodoPago.SelectedIndexChanged += (s, e) => {
                txtEfectivo.Enabled = cmbMetodoPago.SelectedIndex == 0;
                CalcularCambio();
            };

            var lblClienteLabel = new Label { Text = "Cliente (opcional):", Location = new Point(8, 65), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            cmbCliente = new ComboBox
            {
                Location = new Point(8, 82),
                Width = 340,
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblEfectivo = new Label { Text = "Efectivo recibido:", Location = new Point(8, 113), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtEfectivo = new TextBox
            {
                Location = new Point(8, 130),
                Width = 130,
                Font = new Font("Segoe UI", 12),
                Text = "0"
            };
            txtEfectivo.TextChanged += (s, e) => CalcularCambio();

            lblCambio = new Label
            {
                Text = "Cambio: $0.00",
                Location = new Point(145, 133),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 125, 50)
            };

            lblTotal = new Label
            {
                Text = "TOTAL: $0.00",
                Location = new Point(8, 165),
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(139, 90, 43)
            };

            var btnCobrar = new Button
            {
                Text = "✅ COBRAR",
                Location = new Point(8, 200),
                Width = 340,
                Height = 50,
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCobrar.FlatAppearance.BorderSize = 0;
            btnCobrar.Click += (s, e) => ProcesarVenta();

            panelPago.Controls.AddRange(new Control[] {
                lblMetodo, cmbMetodoPago, lblClienteLabel, cmbCliente,
                lblEfectivo, txtEfectivo, lblCambio, lblTotal, btnCobrar
            });

            panelDer.Controls.AddRange(new Control[] {
                lblCarrito, btnLimpiar, dgvCarrito, btnQuitarItem, panelPago
            });

            this.Controls.AddRange(new Control[] { lblTitulo, panelIzq, panelDer });
        }

        private void CargarProductos(string filtro = "")
        {
            panelProductos.Controls.Clear();
            var productos = DatabaseManager.Instance.ObtenerProductos();

            if (!string.IsNullOrEmpty(filtro))
                productos = productos.Where(p => p.Nombre.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            var categorias = productos.Select(p => p.CategoriaNombre).Distinct().OrderBy(c => c);
            Color[] coloresCategoria = {
                Color.FromArgb(232, 149, 109),
                Color.FromArgb(212, 168, 87),
                Color.FromArgb(193, 123, 181),
                Color.FromArgb(123, 181, 193),
                Color.FromArgb(123, 193, 142)
            };
            int iColor = 0;

            foreach (var cat in categorias)
            {
                var lblCat = new Label
                {
                    Text = cat,
                    Width = 560,
                    Height = 22,
                    BackColor = coloresCategoria[iColor % coloresCategoria.Length],
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0),
                    Margin = new Padding(0)
                };
                panelProductos.Controls.Add(lblCat);
                iColor++;

                foreach (var p in productos.Where(pr => pr.CategoriaNombre == cat))
                {
                    panelProductos.Controls.Add(CrearBtnProducto(p));
                }
            }
        }

        private Button CrearBtnProducto(Producto p)
        {
            var btn = new Button
            {
                Width = 130,
                Height = 70,
                Margin = new Padding(3),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = p
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(200, 175, 130);
            btn.FlatAppearance.BorderSize = 1;

            btn.Paint += (s, e) => {
                e.Graphics.Clear(btn.BackColor);
                e.Graphics.DrawString(p.Nombre, new Font("Segoe UI", 9, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(60, 35, 10)), new RectangleF(5, 8, 120, 35));
                e.Graphics.DrawString($"${p.Precio:N2}", new Font("Segoe UI", 11, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(139, 90, 43)), new RectangleF(5, 42, 120, 22));
            };

            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(255, 245, 220);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.White;
            btn.Click += (s, e) => AgregarAlCarrito(p);

            return btn;
        }

        private void FiltrarProductos(string filtro)
        {
            CargarProductos(filtro);
        }

        private void CargarClientes()
        {
            cmbCliente.Items.Clear();
            cmbCliente.Items.Add("-- Sin cliente --");
            foreach (var c in DatabaseManager.Instance.ObtenerClientes())
                cmbCliente.Items.Add(c);
            cmbCliente.DisplayMember = "Nombre";
            cmbCliente.SelectedIndex = 0;
        }

        private void AgregarAlCarrito(Producto p)
        {
            var existente = carrito.FirstOrDefault(d => d.ProductoId == p.Id);
            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                carrito.Add(new DetalleVenta
                {
                    ProductoId = p.Id,
                    ProductoNombre = p.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = p.Precio
                });
            }
            ActualizarCarrito();
        }

        private void QuitarItemSeleccionado()
        {
            if (dgvCarrito.CurrentRow == null) return;
            var idx = dgvCarrito.CurrentRow.Index;
            if (idx >= 0 && idx < carrito.Count)
            {
                carrito.RemoveAt(idx);
                ActualizarCarrito();
            }
        }

        private void LimpiarCarrito()
        {
            carrito.Clear();
            ActualizarCarrito();
        }

        private void ActualizarCarrito()
        {
            dgvCarrito.DataSource = null;
            dgvCarrito.DataSource = carrito.Select(d => new {
                d.ProductoNombre, d.Cantidad, d.PrecioUnitario, d.Subtotal
            }).ToList();

            decimal total = carrito.Sum(d => d.Subtotal);
            lblTotal.Text = $"TOTAL: {total:C}";
            CalcularCambio();
        }

        private void CalcularCambio()
        {
            decimal total = carrito.Sum(d => d.Subtotal);
            if (cmbMetodoPago.SelectedIndex == 0 && decimal.TryParse(txtEfectivo.Text, out decimal efectivo))
            {
                decimal cambio = efectivo - total;
                lblCambio.Text = $"Cambio: {cambio:C}";
                lblCambio.ForeColor = cambio >= 0 ? Color.FromArgb(46, 125, 50) : Color.Red;
            }
            else
            {
                lblCambio.Text = "";
            }
        }

        private void ProcesarVenta()
        {
            if (carrito.Count == 0)
            {
                MessageBox.Show("Agrega productos al carrito primero.", "Carrito vacío",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal total = carrito.Sum(d => d.Subtotal);

            if (cmbMetodoPago.SelectedIndex == 0)
            {
                if (!decimal.TryParse(txtEfectivo.Text, out decimal efectivo) || efectivo < total)
                {
                    MessageBox.Show($"El efectivo recibido (${txtEfectivo.Text}) es insuficiente.\nTotal: {total:C}",
                        "Efectivo insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var venta = new Venta
            {
                Fecha = DateTime.Now,
                Detalles = carrito.ToList(),
                MetodoPago = (MetodoPago)cmbMetodoPago.SelectedIndex,
                UsuarioId = Session.UsuarioActual.Id,
                EfectivoRecibido = decimal.TryParse(txtEfectivo.Text, out decimal ef) ? ef : total
            };

            if (cmbCliente.SelectedIndex > 0 && cmbCliente.SelectedItem is Cliente cliente)
                venta.ClienteId = cliente.Id;

            int ventaId = DatabaseManager.Instance.GuardarVenta(venta);

            MostrarTicket(venta, ventaId);
            LimpiarCarrito();
        }

        private void MostrarTicket(Venta v, int ventaId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════╗");
            sb.AppendLine("║      🥐 PANADERÍA        ║");
            sb.AppendLine("║   Sistema de Ventas      ║");
            sb.AppendLine("╚══════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Folio: #{ventaId:D6}");
            sb.AppendLine($"Fecha: {v.Fecha:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Cajero: {Session.UsuarioActual?.Nombre}");
            sb.AppendLine($"Pago: {v.MetodoPago}");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────");
            sb.AppendLine($"{"Producto",-16} {"Cant",4} {"Precio",8} {"Total",9}");
            sb.AppendLine("─────────────────────────────");

            foreach (var d in v.Detalles)
                sb.AppendLine($"{d.ProductoNombre,-16} {d.Cantidad,4} {d.PrecioUnitario,8:C} {d.Subtotal,9:C}");

            sb.AppendLine("─────────────────────────────");
            sb.AppendLine($"{"TOTAL:",-20} {v.Total,10:C}");

            if (v.MetodoPago == MetodoPago.Efectivo)
            {
                sb.AppendLine($"{"Recibido:",-20} {v.EfectivoRecibido,10:C}");
                sb.AppendLine($"{"Cambio:",-20} {v.Cambio,10:C}");
            }

            sb.AppendLine();
            sb.AppendLine("   ¡Gracias por su compra!   ");
            sb.AppendLine("   Vuelva pronto 😊          ");

            var formTicket = new Form
            {
                Text = $"Ticket #{ventaId:D6}",
                Size = new Size(380, 480),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };

            var txtTicket = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Text = sb.ToString(),
                Font = new Font("Courier New", 10),
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };

            var btnImprimir = new Button
            {
                Text = "🖨 Imprimir",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(139, 90, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnImprimir.FlatAppearance.BorderSize = 0;
            btnImprimir.Click += (s, e) => {
                var pd = new System.Drawing.Printing.PrintDocument();
                pd.PrintPage += (sender, pe) => {
                    pe.Graphics.DrawString(sb.ToString(), new Font("Courier New", 9),
                        Brushes.Black, pe.MarginBounds.Left, pe.MarginBounds.Top);
                };
                var ppd = new System.Windows.Forms.PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            };

            formTicket.Controls.Add(txtTicket);
            formTicket.Controls.Add(btnImprimir);
            formTicket.ShowDialog();
        }
    }
}