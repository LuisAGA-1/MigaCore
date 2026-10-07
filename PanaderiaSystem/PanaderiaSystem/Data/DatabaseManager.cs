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
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Usuarios (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Usuario TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Rol INTEGER NOT NULL DEFAULT 1,
                    Activo INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Categorias (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Color TEXT DEFAULT '#D4A857'
                );

                CREATE TABLE IF NOT EXISTS Productos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Precio REAL NOT NULL,
                    CategoriaId INTEGER,
                    Activo INTEGER NOT NULL DEFAULT 1,
                    ImagenPath TEXT,
                    FOREIGN KEY (CategoriaId) REFERENCES Categorias(Id)
                );

                CREATE TABLE IF NOT EXISTS Ingredientes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Cantidad REAL NOT NULL DEFAULT 0,
                    Unidad TEXT NOT NULL DEFAULT 'kg',
                    CantidadMinima REAL NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Recetas (
                    ProductoId INTEGER,
                    IngredienteId INTEGER,
                    CantidadUsada REAL NOT NULL,
                    RendimientoPiezas INTEGER NOT NULL DEFAULT 1,
                    PRIMARY KEY (ProductoId, IngredienteId),
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id),
                    FOREIGN KEY (IngredienteId) REFERENCES Ingredientes(Id)
                );

                CREATE TABLE IF NOT EXISTS Ventas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    MetodoPago INTEGER NOT NULL DEFAULT 0,
                    EfectivoRecibido REAL NOT NULL DEFAULT 0,
                    UsuarioId INTEGER,
                    ClienteId INTEGER,
                    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
                    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
                );

                CREATE TABLE IF NOT EXISTS DetallesVenta (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VentaId INTEGER NOT NULL,
                    ProductoId INTEGER NOT NULL,
                    ProductoNombre TEXT NOT NULL,
                    Cantidad INTEGER NOT NULL,
                    PrecioUnitario REAL NOT NULL,
                    FOREIGN KEY (VentaId) REFERENCES Ventas(Id),
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
                );

                CREATE TABLE IF NOT EXISTS Clientes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Telefono TEXT,
                    Direccion TEXT,
                    Notas TEXT,
                    FechaRegistro TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Produccion (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    ProductoId INTEGER NOT NULL,
                    ProductoNombre TEXT NOT NULL,
                    CantidadHecha INTEGER NOT NULL,
                    CantidadVendida INTEGER NOT NULL DEFAULT 0,
                    UsuarioId INTEGER,
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id),
                    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
                );
                ";
                cmd.ExecuteNonQuery();

                // Insertar datos por defecto si no existen
                InsertarDatosIniciales(conn);
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

                    INSERT INTO Productos (Nombre, Precio, CategoriaId) VALUES 
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
                        Id = reader.GetInt32(0),
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
                        Id = reader.GetInt32(0),
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
                if (u.Id == 0)
                {
                    cmd = new SQLiteCommand(
                        "INSERT INTO Usuarios (Nombre, Usuario, Password, Rol, Activo) VALUES (@n,@u,@p,@r,1)", conn);
                }
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Usuarios SET Nombre=@n, Usuario=@u, Password=@p, Rol=@r WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", u.Id);
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
                new SQLiteCommand($"UPDATE Usuarios SET Activo=0 WHERE Id={id}", conn).ExecuteNonQuery();
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
                    lista.Add(new Categoria { Id = reader.GetInt32(0), Nombre = reader.GetString(1), Color = reader.GetString(2) });
            }
            return lista;
        }

        public void GuardarCategoria(Categoria c)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (c.Id == 0)
                    cmd = new SQLiteCommand("INSERT INTO Categorias (Nombre, Color) VALUES (@n,@c)", conn);
                else
                {
                    cmd = new SQLiteCommand("UPDATE Categorias SET Nombre=@n, Color=@c WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", c.Id);
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
                    SELECT p.Id, p.Nombre, p.Precio, p.CategoriaId, IFNULL(c.Nombre,'Sin categoría'), p.Activo, IFNULL(p.ImagenPath,'')
                    FROM Productos p LEFT JOIN Categorias c ON p.CategoriaId=c.Id
                    {where} ORDER BY p.Nombre", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new Producto
                    {
                        Id = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Precio = (decimal)reader.GetDouble(2),
                        CategoriaId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
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
                if (p.Id == 0)
                    cmd = new SQLiteCommand(
                        "INSERT INTO Productos (Nombre, Precio, CategoriaId, Activo, ImagenPath) VALUES (@n,@pr,@c,1,@img)", conn);
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Productos SET Nombre=@n, Precio=@pr, CategoriaId=@c, Activo=@a, ImagenPath=@img WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", p.Id);
                    cmd.Parameters.AddWithValue("@a", p.Activo ? 1 : 0);
                }
                cmd.Parameters.AddWithValue("@n", p.Nombre);
                cmd.Parameters.AddWithValue("@pr", (double)p.Precio);
                cmd.Parameters.AddWithValue("@c", p.CategoriaId == 0 ? (object)DBNull.Value : p.CategoriaId);
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
                        Id = reader.GetInt32(0),
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
                if (ing.Id == 0)
                    cmd = new SQLiteCommand(
                        "INSERT INTO Ingredientes (Nombre, Cantidad, Unidad, CantidadMinima) VALUES (@n,@c,@u,@m)", conn);
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Ingredientes SET Nombre=@n, Cantidad=@c, Unidad=@u, CantidadMinima=@m WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", ing.Id);
                }
                cmd.Parameters.AddWithValue("@n", ing.Nombre);
                cmd.Parameters.AddWithValue("@c", (double)ing.Cantidad);
                cmd.Parameters.AddWithValue("@u", ing.Unidad);
                cmd.Parameters.AddWithValue("@m", (double)ing.CantidadMinima);
                cmd.ExecuteNonQuery();
            }
        }

        public void AjustarInventario(int ingredienteId, decimal cantidad)
        {
            using (var conn = GetConnection())
            {
                var cmd = new SQLiteCommand(
                    "UPDATE Ingredientes SET Cantidad = Cantidad + @c WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@c", (double)cantidad);
                cmd.Parameters.AddWithValue("@id", ingredienteId);
                cmd.ExecuteNonQuery();
            }
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
                            INSERT INTO Ventas (Fecha, MetodoPago, EfectivoRecibido, UsuarioId, ClienteId)
                            VALUES (@f, @m, @e, @u, @c);
                            SELECT last_insert_rowid();", conn, tx);
                        cmdV.Parameters.AddWithValue("@f", v.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmdV.Parameters.AddWithValue("@m", (int)v.MetodoPago);
                        cmdV.Parameters.AddWithValue("@e", (double)v.EfectivoRecibido);
                        cmdV.Parameters.AddWithValue("@u", v.UsuarioId);
                        cmdV.Parameters.AddWithValue("@c", v.ClienteId.HasValue ? (object)v.ClienteId.Value : DBNull.Value);

                        int ventaId = (int)(long)cmdV.ExecuteScalar();

                        foreach (var d in v.Detalles)
                        {
                            var cmdD = new SQLiteCommand(@"
                                INSERT INTO DetallesVenta (VentaId, ProductoId, ProductoNombre, Cantidad, PrecioUnitario)
                                VALUES (@vid, @pid, @pn, @c, @pu)", conn, tx);
                            cmdD.Parameters.AddWithValue("@vid", ventaId);
                            cmdD.Parameters.AddWithValue("@pid", d.ProductoId);
                            cmdD.Parameters.AddWithValue("@pn", d.ProductoNombre);
                            cmdD.Parameters.AddWithValue("@c", d.Cantidad);
                            cmdD.Parameters.AddWithValue("@pu", (double)d.PrecioUnitario);
                            cmdD.ExecuteNonQuery();
                        }

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
                    SELECT v.Id, v.Fecha, v.MetodoPago, v.EfectivoRecibido, v.UsuarioId, 
                           IFNULL(u.Nombre,''), v.ClienteId
                    FROM Ventas v LEFT JOIN Usuarios u ON v.UsuarioId=u.Id
                    {where} ORDER BY v.Fecha DESC", conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var venta = new Venta
                    {
                        Id = reader.GetInt32(0),
                        Fecha = DateTime.Parse(reader.GetString(1)),
                        MetodoPago = (MetodoPago)reader.GetInt32(2),
                        EfectivoRecibido = (decimal)reader.GetDouble(3),
                        UsuarioId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        UsuarioNombre = reader.GetString(5),
                        ClienteId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                    };
                    lista.Add(venta);
                }

                // Cargar detalles
                foreach (var v in lista)
                {
                    var cmdD = new SQLiteCommand(
                        "SELECT ProductoId, ProductoNombre, Cantidad, PrecioUnitario FROM DetallesVenta WHERE VentaId=@id", conn);
                    cmdD.Parameters.AddWithValue("@id", v.Id);
                    var readerD = cmdD.ExecuteReader();
                    while (readerD.Read())
                        v.Detalles.Add(new DetalleVenta
                        {
                            ProductoId = readerD.GetInt32(0),
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
                        COUNT(DISTINCT v.Id)
                    FROM Ventas v JOIN DetallesVenta d ON v.Id=d.VentaId
                    WHERE date(v.Fecha)='{fecha:yyyy-MM-dd}'", conn);
                var reader = cmd.ExecuteReader();
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
                    FROM DetallesVenta d JOIN Ventas v ON d.VentaId=v.Id
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
                    FROM Ventas v JOIN DetallesVenta d ON v.Id=d.VentaId
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
                    FROM Ventas v JOIN DetallesVenta d ON v.Id=d.VentaId
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
                    SELECT c.Id, c.Nombre, IFNULL(c.Telefono,''), IFNULL(c.Direccion,''), 
                           IFNULL(c.Notas,''), c.FechaRegistro,
                           IFNULL(SUM(d.Cantidad*d.PrecioUnitario),0) as total
                    FROM Clientes c
                    LEFT JOIN Ventas v ON v.ClienteId=c.Id
                    LEFT JOIN DetallesVenta d ON d.VentaId=v.Id
                    GROUP BY c.Id ORDER BY c.Nombre", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new Cliente
                    {
                        Id = reader.GetInt32(0),
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
                if (c.Id == 0)
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
                        "UPDATE Clientes SET Nombre=@n, Telefono=@t, Direccion=@d, Notas=@no WHERE Id=@id;SELECT @id;", conn);
                    cmd.Parameters.AddWithValue("@id", c.Id);
                }
                cmd.Parameters.AddWithValue("@n", c.Nombre);
                cmd.Parameters.AddWithValue("@t", c.Telefono ?? "");
                cmd.Parameters.AddWithValue("@d", c.Direccion ?? "");
                cmd.Parameters.AddWithValue("@no", c.Notas ?? "");
                return (int)(long)cmd.ExecuteScalar();
            }
        }

        // ==================== PRODUCCION ====================
        public List<RegistroProduccion> ObtenerProduccion(DateTime? fecha = null)
        {
            var lista = new List<RegistroProduccion>();
            using (var conn = GetConnection())
            {
                string where = fecha.HasValue ? $"WHERE date(p.Fecha)='{fecha.Value:yyyy-MM-dd}'" : "";
                var cmd = new SQLiteCommand($@"
                    SELECT p.Id, p.Fecha, p.ProductoId, p.ProductoNombre, 
                           p.CantidadHecha, p.CantidadVendida, IFNULL(p.UsuarioId,0)
                    FROM Produccion p {where} ORDER BY p.Fecha DESC", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(new RegistroProduccion
                    {
                        Id = reader.GetInt32(0),
                        Fecha = DateTime.Parse(reader.GetString(1)),
                        ProductoId = reader.GetInt32(2),
                        ProductoNombre = reader.GetString(3),
                        CantidadHecha = reader.GetInt32(4),
                        CantidadVendida = reader.GetInt32(5),
                        UsuarioId = reader.GetInt32(6)
                    });
            }
            return lista;
        }

        public void GuardarProduccion(RegistroProduccion reg)
        {
            using (var conn = GetConnection())
            {
                SQLiteCommand cmd;
                if (reg.Id == 0)
                    cmd = new SQLiteCommand(@"
                        INSERT INTO Produccion (Fecha, ProductoId, ProductoNombre, CantidadHecha, CantidadVendida, UsuarioId)
                        VALUES (@f,@pid,@pn,@ch,@cv,@u)", conn);
                else
                {
                    cmd = new SQLiteCommand(
                        "UPDATE Produccion SET CantidadHecha=@ch, CantidadVendida=@cv WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", reg.Id);
                }
                cmd.Parameters.AddWithValue("@f", reg.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@pid", reg.ProductoId);
                cmd.Parameters.AddWithValue("@pn", reg.ProductoNombre);
                cmd.Parameters.AddWithValue("@ch", reg.CantidadHecha);
                cmd.Parameters.AddWithValue("@cv", reg.CantidadVendida);
                cmd.Parameters.AddWithValue("@u", reg.UsuarioId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}