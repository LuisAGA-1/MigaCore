using System;
using System.Drawing;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Forms
{
    public class UC_Catalogo : UserControl
    {
        private DataGridView dgv;
        private TextBox txtNombre, txtPrecio;
        private ComboBox cmbCategoria;
        private CheckBox chkActivo;
        private Button btnGuardar, btnNuevo;
        private Producto productoActual;

        public UC_Catalogo()
        {
            InitializeComponent();
            Cargar();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);

            var lblTitulo = new Label
            {
                Text = "🍞 Catálogo de Productos",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(10, 5), AutoSize = true
            };

            dgv = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(580, 500),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(220, 210, 190)
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Nombre" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", DataPropertyName = "Precio", DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Categoría", DataPropertyName = "CategoriaNombre" });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Activo", DataPropertyName = "Activo" });
            dgv.SelectionChanged += DgvSelectionChanged;

            // Colorear inactivos
            dgv.CellFormatting += (s, e) => {
                if (e.RowIndex < 0) return;
                var p = (dgv.Rows[e.RowIndex].DataBoundItem as Producto);
                if (p != null && !p.Activo)
                    dgv.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Gray;
            };

            // Panel formulario
            var panelForm = new Panel
            {
                Location = new Point(605, 45),
                Size = new Size(350, 500),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var flds = new (string lbl, Control ctrl)[] {
                ("Nombre del producto:", txtNombre = new TextBox { Width = 300, Font = new Font("Segoe UI", 11) }),
                ("Precio ($):", txtPrecio = new TextBox { Width = 300, Font = new Font("Segoe UI", 11) }),
                ("Categoría:", cmbCategoria = new ComboBox { Width = 300, Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList }),
            };

            int y = 15;
            foreach (var (lbl, ctrl) in flds)
            {
                panelForm.Controls.Add(new Label { Text = lbl, Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) });
                ctrl.Location = new Point(15, y + 20);
                panelForm.Controls.Add(ctrl);
                y += 65;
            }

            // Cargar categorías
            cmbCategoria.Items.Add(new Categoria { Id = 0, Nombre = "-- Sin categoría --" });
            foreach (var c in DatabaseManager.Instance.ObtenerCategorias())
                cmbCategoria.Items.Add(c);
            cmbCategoria.DisplayMember = "Nombre";
            cmbCategoria.SelectedIndex = 0;

            chkActivo = new CheckBox
            {
                Text = "Producto activo (visible en ventas)",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Checked = true
            };
            panelForm.Controls.Add(chkActivo);
            y += 40;

            // Gestión de categorías
            var panelCat = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(315, 85),
                BackColor = Color.FromArgb(248, 245, 238),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblCatTit = new Label { Text = "Nueva categoría:", Location = new Point(8, 8), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var txtNuevaCat = new TextBox { Location = new Point(8, 26), Width = 185, Font = new Font("Segoe UI", 10) };
            var btnAddCat = new Button { Text = "➕ Agregar", Location = new Point(200, 24), Width = 105, Height = 30, BackColor = Color.FromArgb(139, 90, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAddCat.FlatAppearance.BorderSize = 0;
            btnAddCat.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtNuevaCat.Text)) return;
                DatabaseManager.Instance.GuardarCategoria(new Categoria { Nombre = txtNuevaCat.Text.Trim() });
                cmbCategoria.Items.Clear();
                cmbCategoria.Items.Add(new Categoria { Id = 0, Nombre = "-- Sin categoría --" });
                foreach (var c in DatabaseManager.Instance.ObtenerCategorias()) cmbCategoria.Items.Add(c);
                cmbCategoria.SelectedIndex = cmbCategoria.Items.Count - 1;
                txtNuevaCat.Clear();
            };
            panelCat.Controls.AddRange(new Control[] { lblCatTit, txtNuevaCat, btnAddCat });
            panelForm.Controls.Add(panelCat);
            y += 100;

            var btnNuevo2 = new Button { Text = "➕ Nuevo", Location = new Point(15, y), Width = 90, Height = 38, BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnGuardar = new Button { Text = "💾 Guardar", Location = new Point(115, y), Width = 100, Height = 38, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnToggle = new Button { Text = "⚡ Activar/Desactivar", Location = new Point(225, y), Width = 100, Height = 38, BackColor = Color.FromArgb(120, 60, 180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            foreach (var b in new[] { btnNuevo2, btnGuardar, btnToggle }) b.FlatAppearance.BorderSize = 0;

            btnNuevo2.Click += (s, e) => NuevoProducto();
            btnGuardar.Click += (s, e) => Guardar();
            btnToggle.Click += (s, e) => ToggleActivo();

            panelForm.Controls.AddRange(new Control[] { btnNuevo2, btnGuardar, btnToggle });

            this.Controls.AddRange(new Control[] { lblTitulo, dgv, panelForm });
        }

        private void Cargar()
        {
            dgv.DataSource = DatabaseManager.Instance.ObtenerProductos(soloActivos: false);
        }

        private void DgvSelectionChanged(object s, EventArgs e)
        {
            if (dgv.CurrentRow?.DataBoundItem is Producto p)
            {
                productoActual = p;
                txtNombre.Text = p.Nombre;
                txtPrecio.Text = p.Precio.ToString("N2");
                chkActivo.Checked = p.Activo;
                for (int i = 0; i < cmbCategoria.Items.Count; i++)
                    if ((cmbCategoria.Items[i] as Categoria)?.Id == p.CategoriaId)
                    { cmbCategoria.SelectedIndex = i; break; }
            }
        }

        private void NuevoProducto()
        {
            productoActual = null;
            txtNombre.Clear(); txtPrecio.Text = "0"; cmbCategoria.SelectedIndex = 0; chkActivo.Checked = true;
            txtNombre.Focus();
        }

        private void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text)) { MessageBox.Show("Ingresa el nombre del producto."); return; }
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio)) { MessageBox.Show("Precio inválido."); return; }

            var p = productoActual ?? new Producto();
            p.Nombre = txtNombre.Text.Trim();
            p.Precio = precio;
            p.Activo = chkActivo.Checked;
            p.CategoriaId = (cmbCategoria.SelectedItem as Categoria)?.Id ?? 0;

            DatabaseManager.Instance.GuardarProducto(p);
            Cargar();
            MessageBox.Show("Producto guardado.", "✅ Listo");
        }

        private void ToggleActivo()
        {
            if (productoActual == null) return;
            productoActual.Activo = !productoActual.Activo;
            DatabaseManager.Instance.GuardarProducto(productoActual);
            Cargar();
        }
    }
}