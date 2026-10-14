using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Data
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static readonly object _lock = new object();
        private string _connectionString;

        public static DatabaseManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null) _instance = new DatabaseManager();
                    return _instance;
                }
            }
        }

        private DatabaseManager()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PanaderiaSystem", "panaderia.db");

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            _connectionString = $"Data Source={dbPath};Version=3;";
            InicializarDB();
        }

        private SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void InicializarDB()
        {
            using (var conn = GetConnection())
            {
                MigrarIdsGenericos(conn);

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Usuarios (
                    Id_usuario INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Usuario TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Rol INTEGER NOT NULL DEFAULT 1,
                    Activo INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Categorias (
                    Id_categoria INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Color TEXT DEFAULT '#D4A857'
                );

                CREATE TABLE IF NOT EXISTS Productos (
                    Id_producto INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Precio REAL NOT NULL,
                    Id_categoria INTEGER,
                    Activo INTEGER NOT NULL DEFAULT 1,
                    ImagenPath TEXT,
                    FOREIGN KEY (Id_categoria) REFERENCES Categorias(Id_categoria)
                );

                CREATE TABLE IF NOT EXISTS Ingredientes (
                    Id_ingrediente INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Cantidad REAL NOT NULL DEFAULT 0,
                    Unidad TEXT NOT NULL DEFAULT 'kg',
                    CantidadMinima REAL NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Recetas (
                    Id_producto INTEGER,
                    Id_ingrediente INTEGER,
                    CantidadUsada REAL NOT NULL,
                    RendimientoPiezas INTEGER NOT NULL DEFAULT 1,
                    PRIMARY KEY (Id_producto, Id_ingrediente),
                    FOREIGN KEY (Id_producto) REFERENCES Productos(Id_producto),
                    FOREIGN KEY (Id_ingrediente) REFERENCES Ingredientes(Id_ingrediente)
                );

                CREATE TABLE IF NOT EXISTS Ventas (
                    Id_venta INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    MetodoPago INTEGER NOT NULL DEFAULT 0,
                    EfectivoRecibido REAL NOT NULL DEFAULT 0,
                    Id_usuario INTEGER,
                    Id_cliente INTEGER,
                    FOREIGN KEY (Id_usuario) REFERENCES Usuarios(Id_usuario),
                    FOREIGN KEY (Id_cliente) REFERENCES Clientes(Id_cliente)
                );

                CREATE TABLE IF NOT EXISTS DetallesVenta (
                    Id_detalle_venta INTEGER PRIMARY KEY AUTOINCREMENT,
                    Id_venta INTEGER NOT NULL,
                    Id_producto INTEGER NOT NULL,
                    ProductoNombre TEXT NOT NULL,
                    Cantidad INTEGER NOT NULL,
                    PrecioUnitario REAL NOT NULL,
                    FOREIGN KEY (Id_venta) REFERENCES Ventas(Id_venta),
                    FOREIGN KEY (Id_producto) REFERENCES Productos(Id_producto)
                );

                CREATE TABLE IF NOT EXISTS Clientes (
                    Id_cliente INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Telefono TEXT,
                    Direccion TEXT,
                    Notas TEXT,
                    FechaRegistro TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Produccion (
                    Id_produccion INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    Id_producto INTEGER NOT NULL,
                    ProductoNombre TEXT NOT NULL,
                    CantidadHecha INTEGER NOT NULL,
                    CantidadVendida INTEGER NOT NULL DEFAULT 0,
                    Id_usuario INTEGER,
                    FOREIGN KEY (Id_producto) REFERENCES Productos(Id_producto),
                    FOREIGN KEY (Id_usuario) REFERENCES Usuarios(Id_usuario)
                );

                CREATE TABLE IF NOT EXISTS MovimientosInventario (
                    Id_movimiento INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    Id_ingrediente INTEGER NOT NULL,
                    Tipo TEXT NOT NULL,
                    Cantidad REAL NOT NULL,
                    Motivo TEXT,
                    FOREIGN KEY (Id_ingrediente) REFERENCES Ingredientes(Id_ingrediente)
                );

                CREATE TABLE IF NOT EXISTS Pedidos (
                    Id_pedido INTEGER PRIMARY KEY AUTOINCREMENT,
                    FechaSolicitud TEXT NOT NULL,
                    FechaEntrega TEXT NOT NULL,
                    Estado INTEGER NOT NULL DEFAULT 0,
                    Anticipo REAL NOT NULL DEFAULT 0,
                    Notas TEXT,
                    Id_cliente INTEGER NOT NULL,
                    Id_usuario INTEGER,
                    FOREIGN KEY (Id_cliente) REFERENCES Clientes(Id_cliente),
                    FOREIGN KEY (Id_usuario) REFERENCES Usuarios(Id_usuario)
                );

                CREATE TABLE IF NOT EXISTS DetallesPedido (
                    Id_detalle_pedido INTEGER PRIMARY KEY AUTOINCREMENT,
                    Id_pedido INTEGER NOT NULL,
                    Id_producto INTEGER NOT NULL,
                    ProductoNombre TEXT NOT NULL,
                    Cantidad INTEGER NOT NULL,
                    PrecioUnitario REAL NOT NULL,
                    Especificaciones TEXT,
                    FOREIGN KEY (Id_pedido) REFERENCES Pedidos(Id_pedido),
                    FOREIGN KEY (Id_producto) REFERENCES Productos(Id_producto)
                );

                CREATE TABLE IF NOT EXISTS CortesCaja (
                    Id_corte INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    EfectivoEsperado REAL NOT NULL,
                    EfectivoContado REAL NOT NULL,
                    Id_usuario INTEGER NOT NULL,
                    FOREIGN KEY (Id_usuario) REFERENCES Usuarios(Id_usuario)
                );
                ";
                cmd.ExecuteNonQuery();

                // Insertar datos por defecto si no existen
                InsertarDatosIniciales(conn);
            }
        }

        // Bases creadas antes de renombrar los Id genéricos: se renombran las columnas conservando los datos
        private void MigrarIdsGenericos(SQLiteConnection conn)
        {
            var renombres = new[]
            {
                ("Usuarios", "Id", "Id_usuario"),
                ("Categorias", "Id", "Id_categoria"),
                ("Productos", "Id", "Id_producto"), ("Productos", "CategoriaId", "Id_categoria"),
                ("Ingredientes", "Id", "Id_ingrediente"),
                ("Recetas", "ProductoId", "Id_producto"), ("Recetas", "IngredienteId", "Id_ingrediente"),
                ("Ventas", "Id", "Id_venta"), ("Ventas", "UsuarioId", "Id_usuario"), ("Ventas", "ClienteId", "Id_cliente"),
                ("DetallesVenta", "Id", "Id_detalle_venta"), ("DetallesVenta", "VentaId", "Id_venta"), ("DetallesVenta", "ProductoId", "Id_producto"),
                ("Clientes", "Id", "Id_cliente"),
                ("Produccion", "Id", "Id_produccion"), ("Produccion", "ProductoId", "Id_producto"), ("Produccion", "UsuarioId", "Id_usuario"),
            };
            foreach (var (tabla, viejo, nuevo) in renombres)
            {
                var existe = new SQLiteCommand("SELECT COUNT(*) FROM pragma_table_info(@t) WHERE name=@c", conn);
                existe.Parameters.AddWithValue("@t", tabla);
                existe.Parameters.AddWithValue("@c", viejo);
                if ((long)existe.ExecuteScalar() > 0)
                    new SQLiteCommand($"ALTER TABLE {tabla} RENAME COLUMN {viejo} TO {nuevo}", conn).ExecuteNonQuery();
            }
        }

        private void InsertarDatosIniciales(SQLiteConnection conn)
        {
            // Admin por defecto
            var checkAdmin = new SQLiteCommand("SELECT COUNT(*) FROM Usuarios WHERE Usuario='admin'", conn);
            if ((long)checkAdmin.ExecuteScalar() == 0)
            {
                new SQLiteCommand(@"
                    INSERT INTO Usuarios (Nombre, Usuario, Password, Rol) VALUES 
                    ('Administrador', 'admin', 'admin123', 0);
                    INSERT INTO Usuarios (Nombre, Usuario, Password, Rol) VALUES 
                    ('Cajero Demo', 'cajero', 'cajero123', 1);

                    INSERT INTO Categorias (Nombre, Color) VALUES 
                    ('Pan Dulce', '#E8956D'),
                    ('Pan Salado', '#D4A857'),
                    ('Pasteles', '#C17BB5'),
                    ('Bebidas', '#7BB5C1'),
                    ('Empaquetados', '#7BC18E');

                    INSERT INTO Productos (Nombre, Precio, Id_categoria) VALUES 
                    ('Concha', 8.00, 1),
                    ('Polvorón', 6.00, 1),
                    ('Cuerno', 7.00, 1),
                    ('Mantecada', 5.00, 1),
                    ('Bolillo', 2.50, 2),
                    ('Telera', 3.00, 2),
                    ('Pan de Hot Dog', 4.00, 2),
                    ('Dona Glaseada', 12.00, 1),
                    ('Pastel de Chocolate (rebanada)', 45.00, 3),
                    ('Café de Olla', 20.00, 4);

                    INSERT INTO Ingredientes (Nombre, Cantidad, Unidad, CantidadMinima) VALUES
                    ('Harina', 50.0, 'kg', 10.0),
                    ('Azúcar', 20.0, 'kg', 5.0),
                    ('Mantequilla', 10.0, 'kg', 2.0),
                    ('Levadura', 5.0, 'kg', 1.0),
                    ('Huevo', 200, 'pz', 30),
                    ('Chocolate', 5.0, 'kg', 1.0),
                    ('Leche', 20.0, 'lt', 5.0);
                ", conn).ExecuteNonQuery();
            }
        }

        // ==================== USUARIOS ====================
        public List<Usuario> ObtenerUsuarios()
        {
            var lista = new List<Usuario>();
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand("SELECT * FROM Usuarios WHERE Activo=1 ORDER BY Nombre", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Usuario
                    {
                        Id_usuario = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        NombreUsuario = reader.GetString(2),
                        Password = reader.GetString(3),
                        Rol = (RolUsuario)reader.GetInt32(4),
                        Activo = reader.GetInt32(5) == 1
                    });
                }
            }
            return lista;
        }

        public Usuario ValidarLogin(string usuario, string password)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(
                    "SELECT * FROM Usuarios WHERE Usuario=@u AND Password=@p AND Activo=1", conn);
                cmd.Parameters.AddWithValue("@u", usuario);
                cmd.Parameters.AddWithValue("@p", password);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Usuario
                    {
                        Id_usuario = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        NombreUsuario = reader.GetString(2),
                        Rol = (RolUsuario)reader.GetInt32(4)
                    };
                }
            }
            return null;
        }

        public void GuardarUsuario(Usuario u)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (u.Id_usuario == 0)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO Usuarios (Nombre, Usuario, Password, Rol, Activo) VALUES (@n,@u,@p,@r,1)", conn);
                }
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Usuarios SET Nombre=@n, Usuario=@u, Password=@p, Rol=@r WHERE Id_usuario=@id", conn);
                    cmd.Parameters.AddWithValue("@id", u.Id_usuario);
                }
                cmd.Parameters.AddWithValue("@n", u.Nombre);
                cmd.Parameters.AddWithValue("@u", u.NombreUsuario);
                cmd.Parameters.AddWithValue("@p", u.Password);
                cmd.Parameters.AddWithValue("@r", (int)u.Rol);
                cmd.ExecuteNonQuery();
            }
        }

        public void EliminarUsuario(int id)
        {
            using (var conn = GetConnection())
            {
                new SQLiteCommand($"UPDATE Usuarios SET Activo=0 WHERE Id_usuario={id}", conn).ExecuteNonQuery();
            }
        }

        // ==================== CATEGORIAS ====================
        public List<Categoria> ObtenerCategorias()
        {
            var lista = new List<Categoria>();
            using (var conn = GetConnection())
            {
                var reader = new SQLiteCommand("SELECT * FROM Categorias ORDER BY Nombre", conn).ExecuteReader();
                while (reader.Read())
                    lista.Add(new Categoria { Id_categoria = reader.GetInt32(0), Nombre = reader.GetString(1), Color = reader.GetString(2) });
            }
            return lista;
        }

        public void GuardarCategoria(Categoria c)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (c.Id_categoria == 0)
                    cmd = new SQLiteCommand("INSERT INTO Categorias (Nombre, Color) VALUES (@n,@c)", conn);
                else
                {
                    cmd = new SQLiteCommand("UPDATE Categorias SET Nombre=@n, Color=@c WHERE Id_categoria=@id", conn);
                    cmd.Parameters.AddWithValue("@id", c.Id_categoria);
                }
                cmd.Parameters.AddWithValue("@n", c.Nombre);
                cmd.Parameters.AddWithValue("@c", c.Color);
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== PRODUCTOS ====================
        public List<Producto> ObtenerProductos(bool soloActivos = true)
        {
            var lista = new List<Producto>();
            using (var conn = GetConnection())
            {
                string where = soloActivos ? "WHERE p.Activo=1" : "";
                var cmd = new SQLiteCommand($@"
                    SELECT p.Id_producto, p.Nombre, p.Precio, p.Id_categoria, IFNULL(c.Nombre,'Sin categoría'), p.Activo, IFNULL(p.ImagenPath,'')
                    FROM Productos p LEFT JOIN Categorias c ON p.Id_categoria=c.Id_categoria
                    {where} ORDER BY p.Nombre", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new Producto
                    {
                        Id_producto = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Precio = (decimal)reader.GetDouble(2),
                        Id_categoria = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        CategoriaNombre = reader.GetString(4),
                        Activo = reader.GetInt32(5) == 1,
                        ImagenPath = reader.GetString(6)
                    });
            }
            return lista;
        }

        public void GuardarProducto(Producto p)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (p.Id_producto == 0)
                    cmd = new SQLiteCommand(
                        "INSERT INTO Productos (Nombre, Precio, Id_categoria, Activo, ImagenPath) VALUES (@n,@pr,@c,1,@img)", conn);
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Productos SET Nombre=@n, Precio=@pr, Id_categoria=@c, Activo=@a, ImagenPath=@img WHERE Id_producto=@id", conn);
                    cmd.Parameters.AddWithValue("@id", p.Id_producto);
                    cmd.Parameters.AddWithValue("@a", p.Activo ? 1 : 0);
                }
                cmd.Parameters.AddWithValue("@n", p.Nombre);
                cmd.Parameters.AddWithValue("@pr", (double)p.Precio);
                cmd.Parameters.AddWithValue("@c", p.Id_categoria == 0 ? (object)DBNull.Value : p.Id_categoria);
                cmd.Parameters.AddWithValue("@img", p.ImagenPath ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== INGREDIENTES ====================
        public List<Ingrediente> ObtenerIngredientes()
        {
            var lista = new List<Ingrediente>();
            using (var conn = GetConnection())
            {
                var reader = new SQLiteCommand("SELECT * FROM Ingredientes ORDER BY Nombre", conn).ExecuteReader();
                while (reader.Read())
                    lista.Add(new Ingrediente
                    {
                        Id_ingrediente = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Cantidad = (decimal)reader.GetDouble(2),
                        Unidad = reader.GetString(3),
                        CantidadMinima = (decimal)reader.GetDouble(4)
                    });
            }
            return lista;
        }

        public void GuardarIngrediente(Ingrediente ing)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (ing.Id_ingrediente == 0)
                    cmd = new SQLiteCommand(
                        "INSERT INTO Ingredientes (Nombre, Cantidad, Unidad, CantidadMinima) VALUES (@n,@c,@u,@m)", conn);
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Ingredientes SET Nombre=@n, Cantidad=@c, Unidad=@u, CantidadMinima=@m WHERE Id_ingrediente=@id", conn);
                    cmd.Parameters.AddWithValue("@id", ing.Id_ingrediente);
                }
                cmd.Parameters.AddWithValue("@n", ing.Nombre);
                cmd.Parameters.AddWithValue("@c", (double)ing.Cantidad);
                cmd.Parameters.AddWithValue("@u", ing.Unidad);
                cmd.Parameters.AddWithValue("@m", (double)ing.CantidadMinima);
                cmd.ExecuteNonQuery();
            }
        }

        public void AjustarInventario(int idIngrediente, decimal cantidad, string motivo = "Ajuste manual")
        {
            using (var conn = GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                RegistrarMovimiento(conn, tx, idIngrediente, cantidad >= 0 ? "Entrada" : "Salida", cantidad, motivo);
                tx.Commit();
            }
        }

        // Cambia el stock y deja el movimiento en el historial, siempre juntos
        private void RegistrarMovimiento(SQLiteConnection conn, SQLiteTransaction tx,
            int idIngrediente, string tipo, decimal cantidad, string motivo)
        {
            var cmd = new SQLiteCommand(@"
                UPDATE Ingredientes SET Cantidad = Cantidad + @c WHERE Id_ingrediente=@id;
                INSERT INTO MovimientosInventario (Fecha, Id_ingrediente, Tipo, Cantidad, Motivo)
                VALUES (@f, @id, @t, @c, @m);", conn, tx);
            cmd.Parameters.AddWithValue("@f", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@id", idIngrediente);
            cmd.Parameters.AddWithValue("@t", tipo);
            cmd.Parameters.AddWithValue("@c", (double)cantidad);
            cmd.Parameters.AddWithValue("@m", motivo ?? "");
            cmd.ExecuteNonQuery();
        }

        public List<MovimientoInventario> ObtenerMovimientos(int? idIngrediente = null)
        {
            var lista = new List<MovimientoInventario>();
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(@"
                    SELECT m.Id_movimiento, m.Fecha, m.Id_ingrediente, IFNULL(i.Nombre,''), m.Tipo, m.Cantidad, IFNULL(m.Motivo,'')
                    FROM MovimientosInventario m LEFT JOIN Ingredientes i ON m.Id_ingrediente=i.Id_ingrediente
                    WHERE @id IS NULL OR m.Id_ingrediente=@id
                    ORDER BY m.Fecha DESC, m.Id_movimiento DESC", conn);
                cmd.Parameters.AddWithValue("@id", idIngrediente.HasValue ? (object)idIngrediente.Value : DBNull.Value);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new MovimientoInventario
                    {
                        Id_movimiento = reader.GetInt32(0),
                        Fecha = DateTime.Parse(reader.GetString(1)),
                        Id_ingrediente = reader.GetInt32(2),
                        IngredienteNombre = reader.GetString(3),
                        Tipo = reader.GetString(4),
                        Cantidad = (decimal)reader.GetDouble(5),
                        Motivo = reader.GetString(6)
                    });
            }
            return lista;
        }

        // ==================== RECETAS ====================
        public Receta ObtenerReceta(int idProducto)
        {
            var receta = new Receta { Id_producto = idProducto, RendimientoPiezas = 1 };
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(@"
                    SELECT r.Id_ingrediente, i.Nombre, r.CantidadUsada, i.Unidad, r.RendimientoPiezas
                    FROM Recetas r JOIN Ingredientes i ON r.Id_ingrediente=i.Id_ingrediente
                    WHERE r.Id_producto=@p ORDER BY i.Nombre", conn);
                cmd.Parameters.AddWithValue("@p", idProducto);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    receta.Ingredientes.Add(new RecetaIngrediente
                    {
                        Id_ingrediente = reader.GetInt32(0),
                        IngredienteNombre = reader.GetString(1),
                        CantidadUsada = (decimal)reader.GetDouble(2),
                        Unidad = reader.GetString(3)
                    });
                    receta.RendimientoPiezas = reader.GetInt32(4);
                }
            }
            return receta;
        }

        // Reemplaza la receta completa del producto
        public void GuardarReceta(Receta r)
        {
            using (var conn = GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                var borrar = new SQLiteCommand("DELETE FROM Recetas WHERE Id_producto=@p", conn, tx);
                borrar.Parameters.AddWithValue("@p", r.Id_producto);
                borrar.ExecuteNonQuery();
                foreach (var ri in r.Ingredientes)
                {
                    var cmd = new SQLiteCommand(@"
                        INSERT INTO Recetas (Id_producto, Id_ingrediente, CantidadUsada, RendimientoPiezas)
                        VALUES (@p, @i, @c, @r)", conn, tx);
                    cmd.Parameters.AddWithValue("@p", r.Id_producto);
                    cmd.Parameters.AddWithValue("@i", ri.Id_ingrediente);
                    cmd.Parameters.AddWithValue("@c", (double)ri.CantidadUsada);
                    cmd.Parameters.AddWithValue("@r", Math.Max(1, r.RendimientoPiezas));
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        // Cantidad de cada ingrediente que consumen los detalles, según su receta (CantidadUsada rinde RendimientoPiezas)
        private Dictionary<int, decimal> CalcularConsumo(SQLiteConnection conn, SQLiteTransaction tx, IEnumerable<DetalleVenta> detalles)
        {
            var consumo = new Dictionary<int, decimal>();
            foreach (var d in detalles)
            {
                var cmd = new SQLiteCommand(
                    "SELECT Id_ingrediente, CantidadUsada, RendimientoPiezas FROM Recetas WHERE Id_producto=@p", conn, tx);
                cmd.Parameters.AddWithValue("@p", d.Id_producto);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        decimal porPieza = (decimal)reader.GetDouble(1) / Math.Max(1, reader.GetInt32(2));
                        consumo.TryGetValue(id, out decimal acumulado);
                        consumo[id] = acumulado + porPieza * d.Cantidad;
                    }
            }
            return consumo;
        }

        private List<string> BuscarFaltantes(SQLiteConnection conn, SQLiteTransaction tx, Dictionary<int, decimal> consumo)
        {
            var faltantes = new List<string>();
            foreach (var kv in consumo)
            {
                var cmd = new SQLiteCommand(
                    "SELECT Nombre, Cantidad, Unidad FROM Ingredientes WHERE Id_ingrediente=@id", conn, tx);
                cmd.Parameters.AddWithValue("@id", kv.Key);
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read() && (decimal)reader.GetDouble(1) < kv.Value)
                        faltantes.Add($"{reader.GetString(0)}: se necesitan {kv.Value:0.###} {reader.GetString(2)}, hay {reader.GetDouble(1):0.###}");
            }
            return faltantes;
        }

        /// <summary>Lista vacía si alcanzan los ingredientes para todos los detalles; si no, qué falta.</summary>
        public List<string> ValidarIngredientes(IEnumerable<DetalleVenta> detalles)
        {
            using (var conn = GetConnection())
                return BuscarFaltantes(conn, null, CalcularConsumo(conn, null, detalles));
        }

        // ==================== VENTAS ====================
        public int GuardarVenta(Venta v)
        {
            using (var conn = GetConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var cmdV = new SQLiteCommand(@"
                            INSERT INTO Ventas (Fecha, MetodoPago, EfectivoRecibido, Id_usuario, Id_cliente)
                            VALUES (@f, @m, @e, @u, @c);
                            SELECT last_insert_rowid();", conn, tx);
                        cmdV.Parameters.AddWithValue("@f", v.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmdV.Parameters.AddWithValue("@m", (int)v.MetodoPago);
                        cmdV.Parameters.AddWithValue("@e", (double)v.EfectivoRecibido);
                        cmdV.Parameters.AddWithValue("@u", v.Id_usuario);
                        cmdV.Parameters.AddWithValue("@c", v.Id_cliente.HasValue ? (object)v.Id_cliente.Value : DBNull.Value);

                        int ventaId = (int)(long)cmdV.ExecuteScalar();

                        foreach (var d in v.Detalles)
                        {
                            var cmdD = new SQLiteCommand(@"
                                INSERT INTO DetallesVenta (Id_venta, Id_producto, ProductoNombre, Cantidad, PrecioUnitario)
                                VALUES (@vid, @pid, @pn, @c, @pu)", conn, tx);
                            cmdD.Parameters.AddWithValue("@vid", ventaId);
                            cmdD.Parameters.AddWithValue("@pid", d.Id_producto);
                            cmdD.Parameters.AddWithValue("@pn", d.ProductoNombre);
                            cmdD.Parameters.AddWithValue("@c", d.Cantidad);
                            cmdD.Parameters.AddWithValue("@pu", (double)d.PrecioUnitario);
                            cmdD.ExecuteNonQuery();
                        }

                        // Descontar ingredientes según receta; se revalida aquí por si el stock cambió desde el carrito
                        var consumo = CalcularConsumo(conn, tx, v.Detalles);
                        var faltantes = BuscarFaltantes(conn, tx, consumo);
                        if (faltantes.Count > 0)
                            throw new InvalidOperationException("Ingredientes insuficientes:\n" + string.Join("\n", faltantes));
                        foreach (var kv in consumo)
                            RegistrarMovimiento(conn, tx, kv.Key, "Venta", -kv.Value, $"Venta #{ventaId}");

                        tx.Commit();
                        return ventaId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<Venta> ObtenerVentas(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var lista = new List<Venta>();
            using (var conn = GetConnection())
            {
                string where = "WHERE 1=1";
                if (fechaInicio.HasValue) where += $" AND date(v.Fecha) >= '{fechaInicio.Value:yyyy-MM-dd}'";
                if (fechaFin.HasValue) where += $" AND date(v.Fecha) <= '{fechaFin.Value:yyyy-MM-dd}'";

                var cmd = new SQLiteCommand($@"
                    SELECT v.Id_venta, v.Fecha, v.MetodoPago, v.EfectivoRecibido, v.Id_usuario, 
                           IFNULL(u.Nombre,''), v.Id_cliente
                    FROM Ventas v LEFT JOIN Usuarios u ON v.Id_usuario=u.Id_usuario
                    {where} ORDER BY v.Fecha DESC", conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var venta = new Venta
                    {
                        Id_venta = reader.GetInt32(0),
                        Fecha = DateTime.Parse(reader.GetString(1)),
                        MetodoPago = (MetodoPago)reader.GetInt32(2),
                        EfectivoRecibido = (decimal)reader.GetDouble(3),
                        Id_usuario = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        UsuarioNombre = reader.GetString(5),
                        Id_cliente = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                    };
                    lista.Add(venta);
                }

                // Cargar detalles
                foreach (var v in lista)
                {
                    var cmdD = new SQLiteCommand(
                        "SELECT Id_producto, ProductoNombre, Cantidad, PrecioUnitario FROM DetallesVenta WHERE Id_venta=@id", conn);
                    cmdD.Parameters.AddWithValue("@id", v.Id_venta);
                    var readerD = cmdD.ExecuteReader();
                    while (readerD.Read())
                        v.Detalles.Add(new DetalleVenta
                        {
                            Id_producto = readerD.GetInt32(0),
                            ProductoNombre = readerD.GetString(1),
                            Cantidad = readerD.GetInt32(2),
                            PrecioUnitario = (decimal)readerD.GetDouble(3)
                        });
                }
            }
            return lista;
        }

        public (decimal total, decimal efectivo, decimal tarjeta, decimal transferencia, int numVentas)
            ObtenerResumenDia(DateTime fecha)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand($@"
                    SELECT 
                        IFNULL(SUM(d.Cantidad * d.PrecioUnitario), 0),
                        IFNULL(SUM(CASE WHEN v.MetodoPago=0 THEN d.Cantidad*d.PrecioUnitario ELSE 0 END), 0),
                        IFNULL(SUM(CASE WHEN v.MetodoPago=1 THEN d.Cantidad*d.PrecioUnitario ELSE 0 END), 0),
                        IFNULL(SUM(CASE WHEN v.MetodoPago=2 THEN d.Cantidad*d.PrecioUnitario ELSE 0 END), 0),
                        COUNT(DISTINCT v.Id_venta)
                    FROM Ventas v JOIN DetallesVenta d ON v.Id_venta=d.Id_venta
                    WHERE date(v.Fecha)='{fecha:yyyy-MM-dd}'", conn);
                // using: un lector sin cerrar deja la base bloqueada para la siguiente escritura
                using (var reader = cmd.ExecuteReader())
                if (reader.Read())
                    return ((decimal)reader.GetDouble(0), (decimal)reader.GetDouble(1),
                            (decimal)reader.GetDouble(2), (decimal)reader.GetDouble(3), reader.GetInt32(4));
            }
            return (0, 0, 0, 0, 0);
        }

        public List<(string producto, int cantidad, decimal total)> ObtenerProductosMasVendidos(
            DateTime? desde = null, DateTime? hasta = null, int top = 10)
        {
            var lista = new List<(string, int, decimal)>();
            using (var conn = GetConnection())
            {
                string where = "WHERE 1=1";
                if (desde.HasValue) where += $" AND date(v.Fecha) >= '{desde.Value:yyyy-MM-dd}'";
                if (hasta.HasValue) where += $" AND date(v.Fecha) <= '{hasta.Value:yyyy-MM-dd}'";

                var cmd = new SQLiteCommand($@"
                    SELECT d.ProductoNombre, SUM(d.Cantidad) as total_cant, SUM(d.Cantidad*d.PrecioUnitario) as total_val
                    FROM DetallesVenta d JOIN Ventas v ON d.Id_venta=v.Id_venta
                    {where}
                    GROUP BY d.ProductoNombre ORDER BY total_cant DESC LIMIT {top}", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add((reader.GetString(0), reader.GetInt32(1), (decimal)reader.GetDouble(2)));
            }
            return lista;
        }

        public List<(string fecha, decimal total)> ObtenerVentasPorDia(DateTime desde, DateTime hasta)
        {
            var lista = new List<(string, decimal)>();
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand($@"
                    SELECT date(v.Fecha), SUM(d.Cantidad*d.PrecioUnitario)
                    FROM Ventas v JOIN DetallesVenta d ON v.Id_venta=d.Id_venta
                    WHERE date(v.Fecha) BETWEEN '{desde:yyyy-MM-dd}' AND '{hasta:yyyy-MM-dd}'
                    GROUP BY date(v.Fecha) ORDER BY date(v.Fecha)", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add((reader.GetString(0), (decimal)reader.GetDouble(1)));
            }
            return lista;
        }

        public List<(int hora, decimal total)> ObtenerVentasPorHora(DateTime fecha)
        {
            var lista = new List<(int, decimal)>();
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand($@"
                    SELECT strftime('%H', v.Fecha) as hora, SUM(d.Cantidad*d.PrecioUnitario)
                    FROM Ventas v JOIN DetallesVenta d ON v.Id_venta=d.Id_venta
                    WHERE date(v.Fecha)='{fecha:yyyy-MM-dd}'
                    GROUP BY hora ORDER BY hora", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add((int.Parse(reader.GetString(0)), (decimal)reader.GetDouble(1)));
            }
            return lista;
        }

        // ==================== CLIENTES ====================
        public List<Cliente> ObtenerClientes()
        {
            var lista = new List<Cliente>();
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(@"
                    SELECT c.Id_cliente, c.Nombre, IFNULL(c.Telefono,''), IFNULL(c.Direccion,''), 
                           IFNULL(c.Notas,''), c.FechaRegistro,
                           IFNULL(SUM(d.Cantidad*d.PrecioUnitario),0) as total
                    FROM Clientes c
                    LEFT JOIN Ventas v ON v.Id_cliente=c.Id_cliente
                    LEFT JOIN DetallesVenta d ON d.Id_venta=v.Id_venta
                    GROUP BY c.Id_cliente ORDER BY c.Nombre", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new Cliente
                    {
                        Id_cliente = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Telefono = reader.GetString(2),
                        Direccion = reader.GetString(3),
                        Notas = reader.GetString(4),
                        FechaRegistro = DateTime.Parse(reader.GetString(5)),
                        TotalCompras = (decimal)reader.GetDouble(6)
                    });
            }
            return lista;
        }

        public int GuardarCliente(Cliente c)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (c.Id_cliente == 0)
                {
                    cmd = new SQLiteCommand(@"
                        INSERT INTO Clientes (Nombre, Telefono, Direccion, Notas, FechaRegistro)
                        VALUES (@n,@t,@d,@no,@f);
                        SELECT last_insert_rowid();", conn);
                    cmd.Parameters.AddWithValue("@f", c.FechaRegistro.ToString("yyyy-MM-dd"));
                }
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Clientes SET Nombre=@n, Telefono=@t, Direccion=@d, Notas=@no WHERE Id_cliente=@id;SELECT @id;", conn);
                    cmd.Parameters.AddWithValue("@id", c.Id_cliente);
                }
                cmd.Parameters.AddWithValue("@n", c.Nombre);
                cmd.Parameters.AddWithValue("@t", c.Telefono ?? "");
                cmd.Parameters.AddWithValue("@d", c.Direccion ?? "");
                cmd.Parameters.AddWithValue("@no", c.Notas ?? "");
                return (int)(long)cmd.ExecuteScalar();
            }
        }

        // ==================== PEDIDOS ====================
        public int GuardarPedido(Pedido p)
        {
            using (var conn = GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                SQLiteCommand cmd;
                if (p.Id_pedido == 0)
                    cmd = new SQLiteCommand(@"
                        INSERT INTO Pedidos (FechaSolicitud, FechaEntrega, Estado, Anticipo, Notas, Id_cliente, Id_usuario)
                        VALUES (@fs, @fe, @e, @a, @n, @c, @u);
                        SELECT last_insert_rowid();", conn, tx);
                else
                {
                    cmd = new SQLiteCommand(@"
                        UPDATE Pedidos SET FechaSolicitud=@fs, FechaEntrega=@fe, Estado=@e, Anticipo=@a, Notas=@n,
                               Id_cliente=@c, Id_usuario=@u WHERE Id_pedido=@id;
                        DELETE FROM DetallesPedido WHERE Id_pedido=@id;
                        SELECT @id;", conn, tx);
                    cmd.Parameters.AddWithValue("@id", p.Id_pedido);
                }
                cmd.Parameters.AddWithValue("@fs", p.FechaSolicitud.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@fe", p.FechaEntrega.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@e", (int)p.Estado);
                cmd.Parameters.AddWithValue("@a", (double)p.Anticipo);
                cmd.Parameters.AddWithValue("@n", p.Notas ?? "");
                cmd.Parameters.AddWithValue("@c", p.Id_cliente);
                cmd.Parameters.AddWithValue("@u", p.Id_usuario);
                int idPedido = (int)(long)cmd.ExecuteScalar();

                foreach (var d in p.Detalles)
                {
                    var cmdD = new SQLiteCommand(@"
                        INSERT INTO DetallesPedido (Id_pedido, Id_producto, ProductoNombre, Cantidad, PrecioUnitario, Especificaciones)
                        VALUES (@pid, @prod, @pn, @c, @pu, @es)", conn, tx);
                    cmdD.Parameters.AddWithValue("@pid", idPedido);
                    cmdD.Parameters.AddWithValue("@prod", d.Id_producto);
                    cmdD.Parameters.AddWithValue("@pn", d.ProductoNombre);
                    cmdD.Parameters.AddWithValue("@c", d.Cantidad);
                    cmdD.Parameters.AddWithValue("@pu", (double)d.PrecioUnitario);
                    cmdD.Parameters.AddWithValue("@es", d.Especificaciones ?? "");
                    cmdD.ExecuteNonQuery();
                }
                tx.Commit();
                return idPedido;
            }
        }

        public List<Pedido> ObtenerPedidos(EstadoPedido? estado = null)
        {
            var lista = new List<Pedido>();
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(@"
                    SELECT p.Id_pedido, p.FechaSolicitud, p.FechaEntrega, p.Estado, p.Anticipo, IFNULL(p.Notas,''),
                           p.Id_cliente, IFNULL(c.Nombre,''), IFNULL(p.Id_usuario,0)
                    FROM Pedidos p LEFT JOIN Clientes c ON p.Id_cliente=c.Id_cliente
                    WHERE @e IS NULL OR p.Estado=@e
                    ORDER BY p.FechaEntrega", conn);
                cmd.Parameters.AddWithValue("@e", estado.HasValue ? (object)(int)estado.Value : DBNull.Value);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new Pedido
                    {
                        Id_pedido = reader.GetInt32(0),
                        FechaSolicitud = DateTime.Parse(reader.GetString(1)),
                        FechaEntrega = DateTime.Parse(reader.GetString(2)),
                        Estado = (EstadoPedido)reader.GetInt32(3),
                        Anticipo = (decimal)reader.GetDouble(4),
                        Notas = reader.GetString(5),
                        Id_cliente = reader.GetInt32(6),
                        ClienteNombre = reader.GetString(7),
                        Id_usuario = reader.GetInt32(8)
                    });

                foreach (var p in lista)
                {
                    var cmdD = new SQLiteCommand(@"
                        SELECT Id_producto, ProductoNombre, Cantidad, PrecioUnitario, IFNULL(Especificaciones,'')
                        FROM DetallesPedido WHERE Id_pedido=@id", conn);
                    cmdD.Parameters.AddWithValue("@id", p.Id_pedido);
                    var readerD = cmdD.ExecuteReader();
                    while (readerD.Read())
                        p.Detalles.Add(new DetallePedido
                        {
                            Id_producto = readerD.GetInt32(0),
                            ProductoNombre = readerD.GetString(1),
                            Cantidad = readerD.GetInt32(2),
                            PrecioUnitario = (decimal)readerD.GetDouble(3),
                            Especificaciones = readerD.GetString(4)
                        });
                }
            }
            return lista;
        }

        public void ActualizarEstadoPedido(int idPedido, EstadoPedido estado)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand("UPDATE Pedidos SET Estado=@e WHERE Id_pedido=@id", conn);
                cmd.Parameters.AddWithValue("@e", (int)estado);
                cmd.Parameters.AddWithValue("@id", idPedido);
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== CORTE DE CAJA ====================
        /// <summary>EfectivoEsperado: usar ObtenerResumenDia(fecha).efectivo (ventas en efectivo del día).</summary>
        public int GuardarCorteCaja(CorteCaja c)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(@"
                    INSERT INTO CortesCaja (Fecha, EfectivoEsperado, EfectivoContado, Id_usuario)
                    VALUES (@f, @es, @co, @u);
                    SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@f", c.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@es", (double)c.EfectivoEsperado);
                cmd.Parameters.AddWithValue("@co", (double)c.EfectivoContado);
                cmd.Parameters.AddWithValue("@u", c.Id_usuario);
                return (int)(long)cmd.ExecuteScalar();
            }
        }

        public List<CorteCaja> ObtenerCortesCaja()
        {
            var lista = new List<CorteCaja>();
            using (var conn = GetConnection())
            {
                var reader = new SQLiteCommand(@"
                    SELECT k.Id_corte, k.Fecha, k.EfectivoEsperado, k.EfectivoContado, k.Id_usuario, IFNULL(u.Nombre,'')
                    FROM CortesCaja k LEFT JOIN Usuarios u ON k.Id_usuario=u.Id_usuario
                    ORDER BY k.Fecha DESC", conn).ExecuteReader();
                while (reader.Read())
                    lista.Add(new CorteCaja
                    {
                        Id_corte = reader.GetInt32(0),
                        Fecha = DateTime.Parse(reader.GetString(1)),
                        EfectivoEsperado = (decimal)reader.GetDouble(2),
                        EfectivoContado = (decimal)reader.GetDouble(3),
                        Id_usuario = reader.GetInt32(4),
                        UsuarioNombre = reader.GetString(5)
                    });
            }
            return lista;
        }

        // ==================== PRODUCCION ====================
        public List<RegistroProduccion> ObtenerProduccion(DateTime? fecha = null)
        {
            var lista = new List<RegistroProduccion>();
            using (var conn = GetConnection())
            {
                string where = fecha.HasValue ? $"WHERE date(p.Fecha)='{fecha.Value:yyyy-MM-dd}'" : "";
                var cmd = new SQLiteCommand($@"
                    SELECT p.Id_produccion, p.Fecha, p.Id_producto, p.ProductoNombre, 
                           p.CantidadHecha, p.CantidadVendida, IFNULL(p.Id_usuario,0)
                    FROM Produccion p {where} ORDER BY p.Fecha DESC", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new RegistroProduccion
                    {
                        Id_produccion = reader.GetInt32(0),
                        Fecha = DateTime.Parse(reader.GetString(1)),
                        Id_producto = reader.GetInt32(2),
                        ProductoNombre = reader.GetString(3),
                        CantidadHecha = reader.GetInt32(4),
                        CantidadVendida = reader.GetInt32(5),
                        Id_usuario = reader.GetInt32(6)
                    });
            }
            return lista;
        }

        public void GuardarProduccion(RegistroProduccion reg)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (reg.Id_produccion == 0)
                    cmd = new SQLiteCommand(@"
                        INSERT INTO Produccion (Fecha, Id_producto, ProductoNombre, CantidadHecha, CantidadVendida, Id_usuario)
                        VALUES (@f,@pid,@pn,@ch,@cv,@u)", conn);
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Produccion SET CantidadHecha=@ch, CantidadVendida=@cv WHERE Id_produccion=@id", conn);
                    cmd.Parameters.AddWithValue("@id", reg.Id_produccion);
                }
                cmd.Parameters.AddWithValue("@f", reg.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@pid", reg.Id_producto);
                cmd.Parameters.AddWithValue("@pn", reg.ProductoNombre);
                cmd.Parameters.AddWithValue("@ch", reg.CantidadHecha);
                cmd.Parameters.AddWithValue("@cv", reg.CantidadVendida);
                cmd.Parameters.AddWithValue("@u", reg.Id_usuario);
                cmd.ExecuteNonQuery();
            }
        }
    }
}