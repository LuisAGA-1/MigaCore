using System;
using System.Drawing;
using System.Windows.Forms;
using PanaderiaSystem.Data;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Forms
{
    public class UC_Inventario : UserControl
    {
        private DataGridView dgv;
        private TextBox txtNombre, txtCantidad, txtMin;
        private ComboBox cmbUnidad;
        private Button btnGuardar, btnNuevo, btnEliminar;
        private Ingrediente ingredienteActual;

        public UC_Inventario()
        {
            InitializeComponent();
            Cargar();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 240, 230);

            var lblTitulo = new Label
            {
                Text = "📦 Inventario de Ingredientes",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20),
                Location = new Point(10, 5), AutoSize = true
            };

            // Panel alertas
            var panelAlerta = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(940, 35),
                BackColor = Color.FromArgb(255, 245, 200),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblAlerta = new Label
            {
                Text = "⚠️ Los ingredientes marcados en rojo tienen stock bajo",
                Location = new Point(10, 8), AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 90, 0)
            };
            panelAlerta.Controls.Add(lblAlerta);

            // Grid
            dgv = new DataGridView
            {
                Location = new Point(10, 85),
                Size = new Size(580, 460),
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ingrediente", DataPropertyName = "Nombre" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cantidad", DataPropertyName = "Cantidad", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unidad", DataPropertyName = "Unidad" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mínimo", DataPropertyName = "CantidadMinima" });
            dgv.SelectionChanged += DgvSelectionChanged;
            dgv.CellFormatting += (s, e) => {
                if (e.RowIndex < 0) return;
                var row = dgv.Rows[e.RowIndex];
                var ing = row.DataBoundItem as Ingrediente;
                if (ing?.BajoStock == true)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                }
            };

            // Panel formulario
            var panelForm = new Panel
            {
                Location = new Point(605, 85),
                Size = new Size(350, 460),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            int y = 15;
            var campos = new (string label, Control ctrl)[] {
                ("Nombre del ingrediente:", txtNombre = new TextBox { Width = 300, Font = new Font("Segoe UI", 11) }),
                ("Cantidad actual:", txtCantidad = new TextBox { Width = 300, Font = new Font("Segoe UI", 11) }),
                ("Unidad:", cmbUnidad = new ComboBox { Width = 300, Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList }),
                ("Cantidad mínima (alerta):", txtMin = new TextBox { Width = 300, Font = new Font("Segoe UI", 11) }),
            };

            cmbUnidad.Items.AddRange(new object[] { "kg", "g", "lt", "ml", "pz", "bolsa", "caja", "lb" });
            cmbUnidad.SelectedIndex = 0;

            foreach (var (lbl, ctrl) in campos)
            {
                var l = new Label { Text = lbl, Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(80, 50, 20) };
                ctrl.Location = new Point(15, y + 20);
                panelForm.Controls.Add(l);
                panelForm.Controls.Add(ctrl);
                y += 65;
            }

            // Botones ajuste de cantidad
            var btnAjuste = new Panel
            {
                Location = new Point(15, y + 5),
                Size = new Size(310, 35),
                BackColor = Color.Transparent
            };

            var lblAjuste = new Label { Text = "Ajuste rápido de inventario:", Location = new Point(0, 0), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var txtAjuste = new TextBox { Location = new Point(0, 18), Width = 100, Text = "0", Font = new Font("Segoe UI", 10) };
            var btnSumar = new Button { Text = "➕ Agregar", Location = new Point(110, 16), Width = 90, Height = 28, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnRestar = new Button { Text = "➖ Restar", Location = new Point(205, 16), Width = 85, Height = 28, BackColor = Color.FromArgb(180, 60, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSumar.FlatAppearance.BorderSize = 0;
            btnRestar.FlatAppearance.BorderSize = 0;

            btnSumar.Click += (s, e) => AjustarCantidad(txtAjuste, 1);
            btnRestar.Click += (s, e) => AjustarCantidad(txtAjuste, -1);

            panelForm.Controls.AddRange(new Control[] { lblAjuste, txtAjuste, btnSumar, btnRestar });
            foreach (Control c in new Control[] { lblAjuste, txtAjuste, btnSumar, btnRestar })
            {
                c.Location = new Point(c.Location.X + 15, c.Location.Y + y + 5);
                panelForm.Controls.Add(c);
            }

            y += 70;

            btnNuevo = new Button { Text = "➕ Nuevo", Location = new Point(15, y + 30), Width = 95, Height = 38, BackColor = Color.FromArgb(21, 101, 192), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnGuardar = new Button { Text = "💾 Guardar", Location = new Point(120, y + 30), Width = 100, Height = 38, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEliminar = new Button { Text = "🗑 Eliminar", Location = new Point(230, y + 30), Width = 95, Height = 38, BackColor = Color.FromArgb(180, 60, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            foreach (var b in new[] { btnNuevo, btnGuardar, btnEliminar }) b.FlatAppearance.BorderSize = 0;

            btnNuevo.Click += (s, e) => NuevoIngrediente();
            btnGuardar.Click += (s, e) => Guardar();
            btnEliminar.Click += (s, e) => Eliminar();

            panelForm.Controls.AddRange(new Control[] { btnNuevo, btnGuardar, btnEliminar });

            this.Controls.AddRange(new Control[] { lblTitulo, panelAlerta, dgv, panelForm });
        }

        private void AjustarCantidad(TextBox txtAjuste, int signo)
        {
            if (ingredienteActual == null || !decimal.TryParse(txtAjuste.Text, out decimal adj)) return;
            DatabaseManager.Instance.AjustarInventario(ingredienteActual.Id, signo * adj);
            Cargar();
        }

        private void Cargar()
        {
            dgv.DataSource = DatabaseManager.Instance.ObtenerIngredientes();
        }

        private void DgvSelectionChanged(object s, EventArgs e)
        {
            if (dgv.CurrentRow?.DataBoundItem is Ingrediente ing)
            {
                ingredienteActual = ing;
                txtNombre.Text = ing.Nombre;
                txtCantidad.Text = ing.Cantidad.ToString("N2");
                cmbUnidad.Text = ing.Unidad;
                txtMin.Text = ing.CantidadMinima.ToString("N2");
            }
        }

        private void NuevoIngrediente()
        {
            ingredienteActual = null;
            txtNombre.Clear(); txtCantidad.Text = "0"; txtMin.Text = "0";
            cmbUnidad.SelectedIndex = 0;
            txtNombre.Focus();
        }

        private void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text)) { MessageBox.Show("Ingresa el nombre."); return; }
            if (!decimal.TryParse(txtCantidad.Text, out decimal cant) ||
                !decimal.TryParse(txtMin.Text, out decimal min))
            {
                MessageBox.Show("Verifica los valores numéricos."); return;
            }

            var ing = ingredienteActual ?? new Ingrediente();
            ing.Nombre = txtNombre.Text.Trim();
            ing.Cantidad = cant;
            ing.Unidad = cmbUnidad.Text;
            ing.CantidadMinima = min;

            DatabaseManager.Instance.GuardarIngrediente(ing);
            Cargar();
            MessageBox.Show("Guardado correctamente.", "✅ Listo");
        }

        private void Eliminar()
        {
            if (ingredienteActual == null) return;
            if (MessageBox.Show($"¿Eliminar '{ingredienteActual.Nombre}'?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Simple: no elimina físicamente, podría desactivar
                MessageBox.Show("Para eliminar ingredientes, bórralos directamente en la base de datos.");
            }
        }
    }
}