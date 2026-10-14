using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Forms
{
    // ==================== PEDIDOS ====================
    public class UC_Pedidos : UserControl
    {
        private DataGridView dgv, dgvDetalles;
        private ComboBox cmbFiltro, cmbCliente, cmbProducto, cmbEstado;
        private DateTimePicker dtpEntrega;
        private TextBox txtAnticipo, txtNotas, txtEspecificaciones;
        private NumericUpDown numCantidad;
        private Label lblTotal;
        private Pedido pedidoActual;
        private List<DetallePedido> detalles = new List<DetallePedido>();

        public UC_Pedidos() { InitializeComponent(); Cargar(); }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);
            var fuente = new Font("Segoe UI", 10);
            var lblTitulo = new Label { Text = "📋 Pedidos", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 20), Location = new Point(10, 5), AutoSize = true };

            cmbFiltro = new ComboBox { Location = new Point(400, 10), Width = 200, Font = fuente, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltro.Items.Add("Todos");
            foreach (var e in Enum.GetValues(typeof(EstadoPedido))) cmbFiltro.Items.Add(e);
            cmbFiltro.SelectedIndex = 0;
            cmbFiltro.SelectedIndexChanged += (s, e) => Cargar();

            dgv = new DataGridView
            {
                Location = new Point(10, 45), Size = new Size(590, 500),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = fuente, AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Folio", DataPropertyName = "Id_pedido" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cliente", DataPropertyName = "ClienteNombre" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Entrega", DataPropertyName = "FechaEntrega", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Estado" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = "Total", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Saldo", DataPropertyName = "SaldoPendiente", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgv.SelectionChanged += (s, e) => { if (dgv.CurrentRow?.DataBoundItem is Pedido p) Mostrar(p); };

            // === Formulario ===
            var panelForm = new Panel { Location = new Point(615, 45), Size = new Size(340, 560), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };
            int y = 10;
            Control Campo(string texto, Control ctrl)
            {
                panelForm.Controls.Add(new Label { Text = texto, Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) });
                ctrl.Location = new Point(15, y + 18);
                ctrl.Font = fuente;
                panelForm.Controls.Add(ctrl);
                y += ctrl.Height + 26;
                return ctrl;
            }

            Campo("Cliente:", cmbCliente = new ComboBox { Width = 295, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Nombre" });
            Campo("Fecha de entrega:", dtpEntrega = new DateTimePicker { Width = 295, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" });
            Campo("Anticipo:", txtAnticipo = new TextBox { Width = 140 });
            Campo("Notas de entrega:", txtNotas = new TextBox { Width = 295 });

            Campo("Producto:", cmbProducto = new ComboBox { Width = 295, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Nombre" });
            Campo("Cantidad:", numCantidad = new NumericUpDown { Width = 80, Minimum = 1, Maximum = 1000, Value = 1 });
            Campo("Especificaciones:", txtEspecificaciones = new TextBox { Width = 295 });

            var btnAgregar = new Button { Text = "➕ Agregar producto", Location = new Point(15, y), Width = 160, Height = 32, BackColor = Color.FromArgb(139, 90, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnQuitar = new Button { Text = "Quitar", Location = new Point(185, y), Width = 80, Height = 32, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAgregar.Click += (s, e) => AgregarDetalle();
            btnQuitar.Click += (s, e) => {
                if (dgvDetalles.CurrentRow == null) return;
                detalles.RemoveAt(dgvDetalles.CurrentRow.Index);
                RefrescarDetalles();
            };
            y += 40;

            dgvDetalles = new DataGridView
            {
                Location = new Point(15, y), Size = new Size(295, 110),
                AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, Font = new Font("Segoe UI", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            y += 115;
            lblTotal = new Label { Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 20) };
            y += 28;

            var btnNuevo = new Button { Text = "➕ Nuevo", Location = new Point(15, y), Width = 90, Height = 38, BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnGuardar = new Button { Text = "💾 Guardar", Location = new Point(115, y), Width = 100, Height = 38, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnNuevo.Click += (s, e) => Limpiar();
            btnGuardar.Click += (s, e) => Guardar();
            y += 48;

            cmbEstado = new ComboBox { Location = new Point(15, y), Width = 160, Font = fuente, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var e in Enum.GetValues(typeof(EstadoPedido))) cmbEstado.Items.Add(e);
            var btnEstado = new Button { Text = "Cambiar estado", Location = new Point(185, y - 2), Width = 125, Height = 32, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEstado.Click += (s, e) => {
                if (pedidoActual == null) { MessageBox.Show("Selecciona un pedido."); return; }
                DatabaseManager.Instance.ActualizarEstadoPedido(pedidoActual.Id_pedido, (EstadoPedido)cmbEstado.SelectedItem);
                Cargar();
            };

            foreach (var b in new[] { btnAgregar, btnNuevo, btnGuardar }) b.FlatAppearance.BorderSize = 0;
            panelForm.Controls.AddRange(new Control[] { btnAgregar, btnQuitar, dgvDetalles, lblTotal, btnNuevo, btnGuardar, cmbEstado, btnEstado });

            this.Controls.AddRange(new Control[] { lblTitulo, cmbFiltro, dgv, panelForm });
        }

        private void Cargar()
        {
            cmbCliente.Items.Clear();
            foreach (var c in DatabaseManager.Instance.ObtenerClientes()) cmbCliente.Items.Add(c);
            cmbProducto.Items.Clear();
            foreach (var p in DatabaseManager.Instance.ObtenerProductos().Where(p => p.EstaDisponible())) cmbProducto.Items.Add(p);

            var filtro = cmbFiltro.SelectedItem as EstadoPedido?;
            dgv.DataSource = DatabaseManager.Instance.ObtenerPedidos(filtro);
            if (dgv.Rows.Count == 0) Limpiar();
        }

        private void Mostrar(Pedido p)
        {
            pedidoActual = p;
            cmbCliente.SelectedItem = cmbCliente.Items.Cast<Cliente>().FirstOrDefault(c => c.Id_cliente == p.Id_cliente);
            dtpEntrega.Value = p.FechaEntrega;
            txtAnticipo.Text = p.Anticipo.ToString("0.00");
            txtNotas.Text = p.Notas;
            cmbEstado.SelectedItem = p.Estado;
            detalles = p.Detalles.ToList();
            RefrescarDetalles();
        }

        private void Limpiar()
        {
            pedidoActual = null;
            cmbCliente.SelectedIndex = -1;
            dtpEntrega.Value = DateTime.Today.AddDays(1).AddHours(8);
            txtAnticipo.Text = "0";
            txtNotas.Clear();
            cmbEstado.SelectedItem = EstadoPedido.Pendiente;
            detalles = new List<DetallePedido>();
            RefrescarDetalles();
        }

        private void AgregarDetalle()
        {
            if (!(cmbProducto.SelectedItem is Producto p)) { MessageBox.Show("Elige un producto."); return; }
            detalles.Add(new DetallePedido
            {
                Id_producto = p.Id_producto,
                ProductoNombre = p.Nombre,
                Cantidad = (int)numCantidad.Value,
                PrecioUnitario = p.Precio,
                Especificaciones = txtEspecificaciones.Text.Trim()
            });
            numCantidad.Value = 1;
            txtEspecificaciones.Clear();
            RefrescarDetalles();
        }

        private void RefrescarDetalles()
        {
            dgvDetalles.DataSource = detalles.Select(d => new { Producto = d.ProductoNombre, d.Cantidad, d.Especificaciones, Subtotal = d.Subtotal.ToString("C2") }).ToList();
            lblTotal.Text = $"Total: {detalles.Sum(d => d.Subtotal):C2}";
        }

        private void Guardar()
        {
            if (!(cmbCliente.SelectedItem is Cliente cliente)) { MessageBox.Show("Elige el cliente."); return; }
            if (detalles.Count == 0) { MessageBox.Show("Agrega al menos un producto."); return; }
            decimal total = detalles.Sum(d => d.Subtotal);
            if (!decimal.TryParse(txtAnticipo.Text, out decimal anticipo) || anticipo < 0 || anticipo > total)
            { MessageBox.Show($"El anticipo debe estar entre $0 y {total:C2}."); return; }

            var p = pedidoActual ?? new Pedido { FechaSolicitud = DateTime.Now, Id_usuario = Session.UsuarioActual.Id_usuario };
            p.Id_cliente = cliente.Id_cliente;
            p.FechaEntrega = dtpEntrega.Value;
            p.Anticipo = anticipo;
            p.Notas = txtNotas.Text.Trim();
            p.Estado = (EstadoPedido)cmbEstado.SelectedItem;
            p.Detalles = detalles.ToList();
            DatabaseManager.Instance.GuardarPedido(p);
            Cargar();
            MessageBox.Show("Pedido guardado.", "✅");
        }
    }
}
