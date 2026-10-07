using System;
using System.Drawing;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Forms
{
    // ==================== CLIENTES ====================
    public class UC_Clientes : UserControl
    {
        private DataGridView dgv;
        private TextBox txtNombre, txtTel, txtDir, txtNotas;
        private Cliente clienteActual;

        public UC_Clientes() { InitializeComponent(); Cargar(); }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);
            var lblTitulo = new Label { Text = "👥 Clientes Frecuentes", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 20), Location = new Point(10, 5), AutoSize = true };

            dgv = new DataGridView
            {
                Location = new Point(10, 45), Size = new Size(590, 500),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Nombre" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Teléfono", DataPropertyName = "Telefono" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total compras", DataPropertyName = "TotalCompras", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgv.SelectionChanged += (s, e) => {
                if (dgv.CurrentRow?.DataBoundItem is Cliente c)
                {
                    clienteActual = c;
                    txtNombre.Text = c.Nombre; txtTel.Text = c.Telefono;
                    txtDir.Text = c.Direccion; txtNotas.Text = c.Notas;
                }
            };

            var panelForm = new Panel { Location = new Point(615, 45), Size = new Size(340, 500), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            int y = 15;
            var flds = new (string lbl, TextBox tb)[] {
                ("Nombre:", txtNombre = new TextBox { Width = 295, Font = new Font("Segoe UI", 11) }),
                ("Teléfono:", txtTel = new TextBox { Width = 295, Font = new Font("Segoe UI", 11) }),
                ("Dirección:", txtDir = new TextBox { Width = 295, Font = new Font("Segoe UI", 11) }),
                ("Notas:", txtNotas = new TextBox { Width = 295, Height = 80, Multiline = true, Font = new Font("Segoe UI", 11) }),
            };

            foreach (var (lbl, tb) in flds)
            {
                panelForm.Controls.Add(new Label { Text = lbl, Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) });
                tb.Location = new Point(15, y + 20);
                panelForm.Controls.Add(tb);
                y += tb.Height + 35;
            }

            var btnNuevo = new Button { Text = "➕ Nuevo", Location = new Point(15, y), Width = 90, Height = 38, BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnGuardar = new Button { Text = "💾 Guardar", Location = new Point(115, y), Width = 100, Height = 38, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnNuevo.FlatAppearance.BorderSize = 0; btnGuardar.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += (s, e) => { clienteActual = null; txtNombre.Clear(); txtTel.Clear(); txtDir.Clear(); txtNotas.Clear(); txtNombre.Focus(); };
            btnGuardar.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtNombre.Text)) { MessageBox.Show("Ingresa el nombre."); return; }
                var c = clienteActual ?? new Cliente { FechaRegistro = DateTime.Today };
                c.Nombre = txtNombre.Text.Trim(); c.Telefono = txtTel.Text.Trim();
                c.Direccion = txtDir.Text.Trim(); c.Notas = txtNotas.Text.Trim();
                DatabaseManager.Instance.GuardarCliente(c);
                Cargar(); MessageBox.Show("Cliente guardado.", "✅");
            };
            panelForm.Controls.AddRange(new Control[] { btnNuevo, btnGuardar });

            this.Controls.AddRange(new Control[] { lblTitulo, dgv, panelForm });
        }

        private void Cargar() => dgv.DataSource = DatabaseManager.Instance.ObtenerClientes();
    }

    // ==================== PRODUCCION ====================
    public class UC_Produccion : UserControl
    {
        private DataGridView dgv;
        private ComboBox cmbProducto;
        private TextBox txtHecho, txtVendido;
        private DateTimePicker dtpFecha;

        public UC_Produccion() { InitializeComponent(); Cargar(); }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);
            var lblTitulo = new Label { Text = "🏭 Registro de Producción", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 20), Location = new Point(10, 5), AutoSize = true };

            dgv = new DataGridView
            {
                Location = new Point(10, 45), Size = new Size(590, 500),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha", DataPropertyName = "Fecha", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Producto", DataPropertyName = "ProductoNombre" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hecho", DataPropertyName = "CantidadHecha" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Vendido", DataPropertyName = "CantidadVendida" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sobrante", DataPropertyName = "Sobrante" });

            // Colorear sobrante alto
            dgv.CellFormatting += (s, e) => {
                if (e.RowIndex < 0 || e.ColumnIndex != 4) return;
                var reg = dgv.Rows[e.RowIndex].DataBoundItem as RegistroProduccion;
                if (reg != null && reg.Sobrante > 20)
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 180);
            };

            var panelForm = new Panel { Location = new Point(615, 45), Size = new Size(340, 350), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            panelForm.Controls.Add(new Label { Text = "Fecha:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            dtpFecha = new DateTimePicker { Location = new Point(15, 35), Width = 290, Font = new Font("Segoe UI", 10) };
            panelForm.Controls.Add(dtpFecha);

            panelForm.Controls.Add(new Label { Text = "Producto:", Location = new Point(15, 70), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            cmbProducto = new ComboBox { Location = new Point(15, 90), Width = 290, Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var p in DatabaseManager.Instance.ObtenerProductos()) cmbProducto.Items.Add(p);
            cmbProducto.DisplayMember = "Nombre";
            if (cmbProducto.Items.Count > 0) cmbProducto.SelectedIndex = 0;
            panelForm.Controls.Add(cmbProducto);

            panelForm.Controls.Add(new Label { Text = "Cantidad horneada:", Location = new Point(15, 125), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtHecho = new TextBox { Location = new Point(15, 145), Width = 130, Font = new Font("Segoe UI", 12), Text = "0" };
            panelForm.Controls.Add(txtHecho);

            panelForm.Controls.Add(new Label { Text = "Cantidad vendida:", Location = new Point(160, 125), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            txtVendido = new TextBox { Location = new Point(160, 145), Width = 130, Font = new Font("Segoe UI", 12), Text = "0" };
            panelForm.Controls.Add(txtVendido);

            var btnGuardar = new Button { Text = "💾 Registrar Producción", Location = new Point(15, 195), Width = 290, Height = 42, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => {
                if (cmbProducto.SelectedItem == null) return;
                if (!int.TryParse(txtHecho.Text, out int hecho) || !int.TryParse(txtVendido.Text, out int vendido)) { MessageBox.Show("Verifica los valores."); return; }
                var p = cmbProducto.SelectedItem as Producto;
                DatabaseManager.Instance.GuardarProduccion(new RegistroProduccion
                {
                    Fecha = dtpFecha.Value, ProductoId = p.Id, ProductoNombre = p.Nombre,
                    CantidadHecha = hecho, CantidadVendida = vendido, UsuarioId = Session.UsuarioActual.Id
                });
                Cargar(); MessageBox.Show("Producción registrada.", "✅");
            };
            panelForm.Controls.Add(btnGuardar);

            // Info tip
            var lblTip = new Panel { Location = new Point(615, 405), Size = new Size(340, 140), BackColor = Color.FromArgb(230, 245, 255), BorderStyle = BorderStyle.FixedSingle };
            lblTip.Controls.Add(new Label
            {
                Text = "💡 Tip: Las filas en amarillo indican productos con sobrante mayor a 20 piezas. Considera ajustar tu producción para esos días.",
                Location = new Point(10, 10), Width = 315, Height = 120,
                Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(0, 80, 140),
                AutoSize = false
            });

            this.Controls.AddRange(new Control[] { lblTitulo, dgv, panelForm, lblTip });
        }

        private void Cargar() => dgv.DataSource = DatabaseManager.Instance.ObtenerProduccion();
    }

    // ==================== USUARIOS ====================
    public class UC_Usuarios : UserControl
    {
        private DataGridView dgv;
        private TextBox txtNombre, txtUsuario, txtPassword;
        private ComboBox cmbRol;
        private Usuario usuarioActual;

        public UC_Usuarios() { InitializeComponent(); Cargar(); }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);
            var lblTitulo = new Label { Text = "👤 Gestión de Usuarios", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 20), Location = new Point(10, 5), AutoSize = true };

            if (!Session.EsAdmin)
            {
                this.Controls.Add(lblTitulo);
                this.Controls.Add(new Label { Text = "⛔ Solo el administrador puede gestionar usuarios.", Location = new Point(10, 60), AutoSize = true, Font = new Font("Segoe UI", 13), ForeColor = Color.Red });
                return;
            }

            dgv = new DataGridView
            {
                Location = new Point(10, 45), Size = new Size(560, 500),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Nombre" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Usuario", DataPropertyName = "Usuario" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rol", DataPropertyName = "Rol" });
            dgv.SelectionChanged += (s, e) => {
                if (dgv.CurrentRow?.DataBoundItem is Usuario u)
                {
                    usuarioActual = u;
                    txtNombre.Text = u.Nombre; txtUsuario.Text = u.Usuario;
                    txtPassword.Text = u.Password; cmbRol.SelectedIndex = (int)u.Rol;
                }
            };

            var panelForm = new Panel { Location = new Point(585, 45), Size = new Size(370, 400), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            int y = 15;
            var flds = new (string lbl, Control ctrl)[] {
                ("Nombre completo:", txtNombre = new TextBox { Width = 320, Font = new Font("Segoe UI", 11) }),
                ("Nombre de usuario:", txtUsuario = new TextBox { Width = 320, Font = new Font("Segoe UI", 11) }),
                ("Contraseña:", txtPassword = new TextBox { Width = 320, Font = new Font("Segoe UI", 11), PasswordChar = '●' }),
                ("Rol:", cmbRol = new ComboBox { Width = 320, Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList }),
            };
            cmbRol.Items.AddRange(new object[] { "Administrador", "Cajero", "Producción" });
            cmbRol.SelectedIndex = 1;

            foreach (var (lbl, ctrl) in flds)
            {
                panelForm.Controls.Add(new Label { Text = lbl, Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) });
                ctrl.Location = new Point(15, y + 20);
                panelForm.Controls.Add(ctrl);
                y += 65;
            }

            var btnNuevo = new Button { Text = "➕ Nuevo", Location = new Point(15, y + 10), Width = 90, Height = 38, BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnGuardar = new Button { Text = "💾 Guardar", Location = new Point(115, y + 10), Width = 100, Height = 38, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnElim = new Button { Text = "🗑 Eliminar", Location = new Point(225, y + 10), Width = 95, Height = 38, BackColor = Color.FromArgb(180, 60, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            foreach (var b in new[] { btnNuevo, btnGuardar, btnElim }) b.FlatAppearance.BorderSize = 0;

            btnNuevo.Click += (s, e) => { usuarioActual = null; txtNombre.Clear(); txtUsuario.Clear(); txtPassword.Clear(); cmbRol.SelectedIndex = 1; txtNombre.Focus(); };
            btnGuardar.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtUsuario.Text)) { MessageBox.Show("Llena todos los campos."); return; }
                var u = usuarioActual ?? new Usuario();
                u.Nombre = txtNombre.Text.Trim(); u.Usuario = txtUsuario.Text.Trim();
                u.Password = txtPassword.Text; u.Rol = (RolUsuario)cmbRol.SelectedIndex;
                DatabaseManager.Instance.GuardarUsuario(u);
                Cargar(); MessageBox.Show("Usuario guardado.", "✅");
            };
            btnElim.Click += (s, e) => {
                if (usuarioActual == null) return;
                if (usuarioActual.Usuario == "admin") { MessageBox.Show("No puedes eliminar el administrador principal."); return; }
                if (MessageBox.Show($"¿Eliminar a {usuarioActual.Nombre}?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
                { DatabaseManager.Instance.EliminarUsuario(usuarioActual.Id); Cargar(); }
            };
            panelForm.Controls.AddRange(new Control[] { btnNuevo, btnGuardar, btnElim });

            this.Controls.AddRange(new Control[] { lblTitulo, dgv, panelForm });
        }

        private void Cargar()
		{
   			 if (dgv != null)
       			 dgv.DataSource = DatabaseManager.Instance.ObtenerUsuarios();
		}
        // Workaround: uso de propiedad nula
        private void Cargar2()
        {
            if (dgv != null)
                dgv.DataSource = DatabaseManager.Instance.ObtenerUsuarios();
        }
    }
}

// Extension para el let pattern de C#
public static class Extensions
{
    public static void let<T>(this T obj, Action<T> action) where T : class
    {
        if (obj != null) action(obj);
    }
}