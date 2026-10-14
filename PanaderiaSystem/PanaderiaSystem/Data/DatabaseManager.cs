using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using Npgsql;
using PanaderiaSystem.Models;

namespace PanaderiaSystem.Data
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static readonly object _lock = new object();
        private readonly string _connectionString;

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

        static DatabaseManager()
        {
            // Fechas locales (DateTime.Now) se guardan y leen tal cual en columnas timestamptz
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        private DatabaseManager()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MigaCore"]?.ConnectionString
                ?? throw new ConfigurationErrorsException("Falta la cadena de conexión 'MigaCore' en App.config.");
            InicializarDB();
        }

        private NpgsqlConnection GetConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // Crea lo que falte; sobre una base ya existente solo agrega tablas y columnas nuevas
        private void InicializarDB()
        {
            using (var conn = GetConnection())
            {
                new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS usuarios (
                    usuario_id SERIAL PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password_hash VARCHAR(256) NOT NULL,
                    rol VARCHAR(20) NOT NULL,
                    activo BOOLEAN NOT NULL DEFAULT TRUE,
                    creado_en TIMESTAMPTZ NOT NULL DEFAULT now()
                );

                CREATE TABLE IF NOT EXISTS categorias (
                    categoria_id SERIAL PRIMARY KEY,
                    nombre VARCHAR(50) NOT NULL UNIQUE,
                    color_hex VARCHAR(7) NOT NULL DEFAULT '#D4A857',
                    activa BOOLEAN NOT NULL DEFAULT TRUE
                );

                CREATE TABLE IF NOT EXISTS clientes (
                    cliente_id SERIAL PRIMARY KEY,
                    nombre VARCHAR(120) NOT NULL,
                    telefono VARCHAR(20),
                    direccion TEXT,
                    notas TEXT,
                    activo BOOLEAN NOT NULL DEFAULT TRUE,
                    registrado_en TIMESTAMPTZ NOT NULL DEFAULT now()
                );

                CREATE TABLE IF NOT EXISTS productos (
                    producto_id SERIAL PRIMARY KEY,
                    categoria_id INTEGER REFERENCES categorias(categoria_id),
                    nombre VARCHAR(100) NOT NULL,
                    precio NUMERIC(10,2) NOT NULL,
                    stock INTEGER NOT NULL DEFAULT 0,
                    stock_minimo INTEGER NOT NULL DEFAULT 0,
                    activo BOOLEAN NOT NULL DEFAULT TRUE
                );
                ALTER TABLE productos ADD COLUMN IF NOT EXISTS imagen_path TEXT;

                CREATE TABLE IF NOT EXISTS ingredientes (
                    ingrediente_id SERIAL PRIMARY KEY,
                    nombre VARCHAR(100) NOT NULL UNIQUE,
                    unidad_medida VARCHAR(20) NOT NULL DEFAULT 'kg',
                    cantidad_disponible NUMERIC(10,3) NOT NULL DEFAULT 0,
                    cantidad_minima NUMERIC(10,3) NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS recetas (
                    producto_id INTEGER NOT NULL REFERENCES productos(producto_id),
                    ingrediente_id INTEGER NOT NULL REFERENCES ingredientes(ingrediente_id),
                    cantidad_usada NUMERIC(10,3) NOT NULL,
                    rendimiento_piezas INTEGER NOT NULL DEFAULT 1,
                    PRIMARY KEY (producto_id, ingrediente_id)
                );

                CREATE TABLE IF NOT EXISTS ventas (
                    venta_id SERIAL PRIMARY KEY,
                    usuario_id INTEGER REFERENCES usuarios(usuario_id),
                    cliente_id INTEGER REFERENCES clientes(cliente_id),
                    fecha_venta TIMESTAMPTZ NOT NULL DEFAULT now(),
                    metodo_pago VARCHAR(20) NOT NULL,
                    monto_recibido NUMERIC(10,2) NOT NULL DEFAULT 0,
                    cambio NUMERIC(10,2) NOT NULL DEFAULT 0,
                    total NUMERIC(10,2) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS detalle_ventas (
                    detalle_id SERIAL PRIMARY KEY,
                    venta_id INTEGER NOT NULL REFERENCES ventas(venta_id),
                    producto_id INTEGER NOT NULL REFERENCES productos(producto_id),
                    cantidad INTEGER NOT NULL,
                    precio_unitario NUMERIC(10,2) NOT NULL,
                    subtotal NUMERIC(10,2) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS pedidos (
                    pedido_id SERIAL PRIMARY KEY,
                    cliente_id INTEGER NOT NULL REFERENCES clientes(cliente_id),
                    usuario_id INTEGER REFERENCES usuarios(usuario_id),
                    fecha_solicitud TIMESTAMPTZ NOT NULL DEFAULT now(),
                    fecha_entrega_programada TIMESTAMPTZ NOT NULL,
                    estado VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
                    total NUMERIC(10,2) NOT NULL DEFAULT 0,
                    notas_entrega TEXT
                );
                ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS anticipo NUMERIC(10,2) NOT NULL DEFAULT 0;

                CREATE TABLE IF NOT EXISTS detalle_pedidos (
                    detalle_pedido_id SERIAL PRIMARY KEY,
                    pedido_id INTEGER NOT NULL REFERENCES pedidos(pedido_id),
                    producto_id INTEGER NOT NULL REFERENCES productos(producto_id),
                    cantidad INTEGER NOT NULL,
                    precio_unitario NUMERIC(10,2) NOT NULL,
                    subtotal NUMERIC(10,2) NOT NULL
                );
                ALTER TABLE detalle_pedidos ADD COLUMN IF NOT EXISTS especificaciones TEXT;

                CREATE TABLE IF NOT EXISTS produccion (
                    produccion_id SERIAL PRIMARY KEY,
                    fecha TIMESTAMPTZ NOT NULL,
                    producto_id INTEGER NOT NULL REFERENCES productos(producto_id),
                    cantidad_hecha INTEGER NOT NULL,
                    cantidad_vendida INTEGER NOT NULL DEFAULT 0,
                    usuario_id INTEGER REFERENCES usuarios(usuario_id)
                );

                CREATE TABLE IF NOT EXISTS movimientos_inventario (
                    movimiento_id SERIAL PRIMARY KEY,
                    fecha TIMESTAMPTZ NOT NULL DEFAULT now(),
                    ingrediente_id INTEGER NOT NULL REFERENCES ingredientes(ingrediente_id),
                    tipo VARCHAR(20) NOT NULL,
                    cantidad NUMERIC(10,3) NOT NULL,
                    motivo TEXT
                );

                CREATE TABLE IF NOT EXISTS cortes_caja (
                    corte_id SERIAL PRIMARY KEY,
                    fecha TIMESTAMPTZ NOT NULL,
                    efectivo_esperado NUMERIC(10,2) NOT NULL,
                    efectivo_contado NUMERIC(10,2) NOT NULL,
                    usuario_id INTEGER NOT NULL REFERENCES usuarios(usuario_id)
                );

                -- Tablas creadas a mano pueden no tener estos valores por omisión
                ALTER TABLE usuarios ALTER COLUMN activo SET DEFAULT TRUE, ALTER COLUMN creado_en SET DEFAULT now();
                ALTER TABLE categorias ALTER COLUMN activa SET DEFAULT TRUE, ALTER COLUMN color_hex SET DEFAULT '#D4A857';
                ALTER TABLE clientes ALTER COLUMN activo SET DEFAULT TRUE, ALTER COLUMN registrado_en SET DEFAULT now();
                ALTER TABLE productos ALTER COLUMN activo SET DEFAULT TRUE, ALTER COLUMN stock SET DEFAULT 0, ALTER COLUMN stock_minimo SET DEFAULT 0;
                ALTER TABLE pedidos ALTER COLUMN estado SET DEFAULT 'Pendiente', ALTER COLUMN fecha_solicitud SET DEFAULT now();", conn).ExecuteNonQuery();

                InsertarDatosIniciales(conn);
            }
        }

        private void InsertarDatosIniciales(NpgsqlConnection conn)
        {
            if ((long)new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conn).ExecuteScalar() > 0) return;

            var cmd = new NpgsqlCommand(@"
                INSERT INTO usuarios (nombre, username, password_hash, rol) VALUES
                ('Administrador', 'admin', @pa, 'Administrador'),
                ('Cajero Demo', 'cajero', @pc, 'Cajero');

                INSERT INTO categorias (nombre, color_hex) VALUES
                ('Pan Dulce', '#E8956D'), ('Pan Salado', '#D4A857'), ('Pasteles', '#C17BB5'),
                ('Bebidas', '#7BB5C1'), ('Empaquetados', '#7BC18E')
                ON CONFLICT (nombre) DO NOTHING;

                INSERT INTO productos (nombre, precio, categoria_id)
                SELECT v.nombre, v.precio, c.categoria_id
                FROM (VALUES
                    ('Concha', 8.00, 'Pan Dulce'), ('Polvorón', 6.00, 'Pan Dulce'), ('Cuerno', 7.00, 'Pan Dulce'),
                    ('Mantecada', 5.00, 'Pan Dulce'), ('Bolillo', 2.50, 'Pan Salado'), ('Telera', 3.00, 'Pan Salado'),
                    ('Pan de Hot Dog', 4.00, 'Pan Salado'), ('Dona Glaseada', 12.00, 'Pan Dulce'),
                    ('Pastel de Chocolate (rebanada)', 45.00, 'Pasteles'), ('Café de Olla', 20.00, 'Bebidas')
                ) AS v(nombre, precio, categoria)
                JOIN categorias c ON c.nombre = v.categoria
                WHERE NOT EXISTS (SELECT 1 FROM productos);

                INSERT INTO ingredientes (nombre, cantidad_disponible, unidad_medida, cantidad_minima) VALUES
                ('Harina', 50.0, 'kg', 10.0), ('Azúcar', 20.0, 'kg', 5.0), ('Mantequilla', 10.0, 'kg', 2.0),
                ('Levadura', 5.0, 'kg', 1.0), ('Huevo', 200, 'pz', 30), ('Chocolate', 5.0, 'kg', 1.0),
                ('Leche', 20.0, 'lt', 5.0)
                ON CONFLICT (nombre) DO NOTHING;", conn);
            cmd.Parameters.AddWithValue("pa", HashPassword("admin123"));
            cmd.Parameters.AddWithValue("pc", HashPassword("cajero123"));
            cmd.ExecuteNonQuery();
        }

        // ==================== CONTRASEÑAS ====================
        // PBKDF2-SHA256, guardado como "iteraciones.salt.hash" en base64
        private static string HashPassword(string password)
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var kdf = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                return $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(kdf.GetBytes(32))}";
        }

        private static bool VerificarPassword(string password, string guardado)
        {
            var partes = guardado.Split('.');
            if (partes.Length != 3 || !int.TryParse(partes[0], out int iteraciones)) return false;
            var esperado = Convert.FromBase64String(partes[2]);
            using (var kdf = new Rfc2898DeriveBytes(password, Convert.FromBase64String(partes[1]), iteraciones, HashAlgorithmName.SHA256))
            {
                var calculado = kdf.GetBytes(esperado.Length);
                int diferencia = 0;  // comparación en tiempo constante
                for (int i = 0; i < esperado.Length; i++) diferencia |= esperado[i] ^ calculado[i];
                return diferencia == 0;
            }
        }

        // ==================== USUARIOS ====================
        public List<Usuario> ObtenerUsuarios()
        {
            var lista = new List<Usuario>();
            using (var conn = GetConnection())
            using (var reader = new NpgsqlCommand(
                "SELECT usuario_id, nombre, username, rol, activo FROM usuarios WHERE activo IS NOT FALSE ORDER BY nombre", conn).ExecuteReader())
                while (reader.Read())
                    lista.Add(new Usuario
                    {
                        Id_usuario = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        NombreUsuario = reader.GetString(2),
                        Rol = (RolUsuario)Enum.Parse(typeof(RolUsuario), reader.GetString(3)),
                        Activo = reader.IsDBNull(4) || reader.GetBoolean(4)
                    });
            return lista;
        }

        public Usuario ValidarLogin(string usuario, string password)
        {
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand(
                    "SELECT usuario_id, nombre, username, rol, password_hash FROM usuarios WHERE username=@u AND activo IS NOT FALSE", conn);
                cmd.Parameters.AddWithValue("u", usuario);
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read() && VerificarPassword(password, reader.GetString(4)))
                        return new Usuario
                        {
                            Id_usuario = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            NombreUsuario = reader.GetString(2),
                            Rol = (RolUsuario)Enum.Parse(typeof(RolUsuario), reader.GetString(3))
                        };
            }
            return null;
        }

        /// <summary>Password vacío al editar conserva la contraseña actual.</summary>
        public void GuardarUsuario(Usuario u)
        {
            bool cambiaPassword = !string.IsNullOrEmpty(u.Password);
            if (u.Id_usuario == 0 && !cambiaPassword)
                throw new ArgumentException("El usuario nuevo necesita contraseña.");

            using (var conn = GetConnection())
            {
                NpgsqlCommand cmd;
                if (u.Id_usuario == 0)
                    cmd = new NpgsqlCommand(
                        "INSERT INTO usuarios (nombre, username, password_hash, rol) VALUES (@n,@u,@p,@r)", conn);
                else
                {
                    cmd = new NpgsqlCommand(
                        "UPDATE usuarios SET nombre=@n, username=@u, rol=@r" +
                        (cambiaPassword ? ", password_hash=@p" : "") + " WHERE usuario_id=@id", conn);
                    cmd.Parameters.AddWithValue("id", u.Id_usuario);
                }
                cmd.Parameters.AddWithValue("n", u.Nombre);
                cmd.Parameters.AddWithValue("u", u.NombreUsuario);
                cmd.Parameters.AddWithValue("r", u.Rol.ToString());
                if (cambiaPassword) cmd.Parameters.AddWithValue("p", HashPassword(u.Password));
                cmd.ExecuteNonQuery();
            }
        }

        public void EliminarUsuario(int idUsuario)
        {
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand("UPDATE usuarios SET activo=FALSE WHERE usuario_id=@id", conn);
                cmd.Parameters.AddWithValue("id", idUsuario);
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== CATEGORIAS ====================
        public List<Categoria> ObtenerCategorias()
        {
            var lista = new List<Categoria>();
            using (var conn = GetConnection())
            using (var reader = new NpgsqlCommand(
                "SELECT categoria_id, nombre, color_hex FROM categorias WHERE activa IS NOT FALSE ORDER BY nombre", conn).ExecuteReader())
                while (reader.Read())
                    lista.Add(new Categoria { Id_categoria = reader.GetInt32(0), Nombre = reader.GetString(1), Color = reader.GetString(2) });
            return lista;
        }

        public void GuardarCategoria(Categoria c)
        {
            using (var conn = GetConnection())
            {
                NpgsqlCommand cmd;
                if (c.Id_categoria == 0)
                    cmd = new NpgsqlCommand("INSERT INTO categorias (nombre, color_hex) VALUES (@n,@c)", conn);
                else
                {
                    cmd = new NpgsqlCommand("UPDATE categorias SET nombre=@n, color_hex=@c WHERE categoria_id=@id", conn);
                    cmd.Parameters.AddWithValue("id", c.Id_categoria);
                }
                cmd.Parameters.AddWithValue("n", c.Nombre);
                cmd.Parameters.AddWithValue("c", c.Color);
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== PRODUCTOS ====================
        public List<Producto> ObtenerProductos(bool soloActivos = true)
        {
            var lista = new List<Producto>();
            using (var conn = GetConnection())
            using (var reader = new NpgsqlCommand($@"
                SELECT p.producto_id, p.nombre, p.precio, p.categoria_id, COALESCE(c.nombre,'Sin categoría'),
                       p.activo, COALESCE(p.imagen_path,'')
                FROM productos p LEFT JOIN categorias c ON p.categoria_id=c.categoria_id
                {(soloActivos ? "WHERE p.activo IS NOT FALSE" : "")} ORDER BY p.nombre", conn).ExecuteReader())
                while (reader.Read())
                    lista.Add(new Producto
                    {
                        Id_producto = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Precio = reader.GetDecimal(2),
                        Id_categoria = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        CategoriaNombre = reader.GetString(4),
                        Activo = reader.IsDBNull(5) || reader.GetBoolean(5),
                        ImagenPath = reader.GetString(6)
                    });
            return lista;
        }

        public void GuardarProducto(Producto p)
        {
            using (var conn = GetConnection())
            {
                NpgsqlCommand cmd;
                if (p.Id_producto == 0)
                    cmd = new NpgsqlCommand(
                        "INSERT INTO productos (nombre, precio, categoria_id, activo, imagen_path) VALUES (@n,@pr,@c,TRUE,@img)", conn);
                else
                {
                    cmd = new NpgsqlCommand(
                        "UPDATE productos SET nombre=@n, precio=@pr, categoria_id=@c, activo=@a, imagen_path=@img WHERE producto_id=@id", conn);
                    cmd.Parameters.AddWithValue("id", p.Id_producto);
                    cmd.Parameters.AddWithValue("a", p.Activo);
                }
                cmd.Parameters.AddWithValue("n", p.Nombre);
                cmd.Parameters.AddWithValue("pr", p.Precio);
                cmd.Parameters.AddWithValue("c", NpgsqlTypes.NpgsqlDbType.Integer, p.Id_categoria == 0 ? (object)DBNull.Value : p.Id_categoria);
                cmd.Parameters.AddWithValue("img", p.ImagenPath ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== INGREDIENTES ====================
        public List<Ingrediente> ObtenerIngredientes()
        {
            var lista = new List<Ingrediente>();
            using (var conn = GetConnection())
            using (var reader = new NpgsqlCommand(
                "SELECT ingrediente_id, nombre, cantidad_disponible, unidad_medida, cantidad_minima FROM ingredientes ORDER BY nombre", conn).ExecuteReader())
                while (reader.Read())
                    lista.Add(new Ingrediente
                    {
                        Id_ingrediente = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Cantidad = reader.GetDecimal(2),
                        Unidad = reader.GetString(3),
                        CantidadMinima = reader.GetDecimal(4)
                    });
            return lista;
        }

        public void GuardarIngrediente(Ingrediente ing)
        {
            using (var conn = GetConnection())
            {
                NpgsqlCommand cmd;
                if (ing.Id_ingrediente == 0)
                    cmd = new NpgsqlCommand(
                        "INSERT INTO ingredientes (nombre, cantidad_disponible, unidad_medida, cantidad_minima) VALUES (@n,@c,@u,@m)", conn);
                else
                {
                    cmd = new NpgsqlCommand(
                        "UPDATE ingredientes SET nombre=@n, cantidad_disponible=@c, unidad_medida=@u, cantidad_minima=@m WHERE ingrediente_id=@id", conn);
                    cmd.Parameters.AddWithValue("id", ing.Id_ingrediente);
                }
                cmd.Parameters.AddWithValue("n", ing.Nombre);
                cmd.Parameters.AddWithValue("c", ing.Cantidad);
                cmd.Parameters.AddWithValue("u", ing.Unidad);
                cmd.Parameters.AddWithValue("m", ing.CantidadMinima);
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
        private void RegistrarMovimiento(NpgsqlConnection conn, NpgsqlTransaction tx,
            int idIngrediente, string tipo, decimal cantidad, string motivo)
        {
            var cmd = new NpgsqlCommand(@"
                UPDATE ingredientes SET cantidad_disponible = cantidad_disponible + @c WHERE ingrediente_id=@id;
                INSERT INTO movimientos_inventario (fecha, ingrediente_id, tipo, cantidad, motivo)
                VALUES (@f, @id, @t, @c, @m);", conn, tx);
            cmd.Parameters.AddWithValue("f", DateTime.Now);
            cmd.Parameters.AddWithValue("id", idIngrediente);
            cmd.Parameters.AddWithValue("t", tipo);
            cmd.Parameters.AddWithValue("c", cantidad);
            cmd.Parameters.AddWithValue("m", motivo ?? "");
            cmd.ExecuteNonQuery();
        }

        public List<MovimientoInventario> ObtenerMovimientos(int? idIngrediente = null)
        {
            var lista = new List<MovimientoInventario>();
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand($@"
                    SELECT m.movimiento_id, m.fecha, m.ingrediente_id, i.nombre, m.tipo, m.cantidad, COALESCE(m.motivo,'')
                    FROM movimientos_inventario m JOIN ingredientes i ON m.ingrediente_id=i.ingrediente_id
                    {(idIngrediente.HasValue ? "WHERE m.ingrediente_id=@id" : "")}
                    ORDER BY m.fecha DESC, m.movimiento_id DESC", conn);
                if (idIngrediente.HasValue) cmd.Parameters.AddWithValue("id", idIngrediente.Value);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new MovimientoInventario
                        {
                            Id_movimiento = reader.GetInt32(0),
                            Fecha = reader.GetDateTime(1),
                            Id_ingrediente = reader.GetInt32(2),
                            IngredienteNombre = reader.GetString(3),
                            Tipo = reader.GetString(4),
                            Cantidad = reader.GetDecimal(5),
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
                var cmd = new NpgsqlCommand(@"
                    SELECT r.ingrediente_id, i.nombre, r.cantidad_usada, i.unidad_medida, r.rendimiento_piezas
                    FROM recetas r JOIN ingredientes i ON r.ingrediente_id=i.ingrediente_id
                    WHERE r.producto_id=@p ORDER BY i.nombre", conn);
                cmd.Parameters.AddWithValue("p", idProducto);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        receta.Ingredientes.Add(new RecetaIngrediente
                        {
                            Id_ingrediente = reader.GetInt32(0),
                            IngredienteNombre = reader.GetString(1),
                            CantidadUsada = reader.GetDecimal(2),
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
                var borrar = new NpgsqlCommand("DELETE FROM recetas WHERE producto_id=@p", conn, tx);
                borrar.Parameters.AddWithValue("p", r.Id_producto);
                borrar.ExecuteNonQuery();
                foreach (var ri in r.Ingredientes)
                {
                    var cmd = new NpgsqlCommand(@"
                        INSERT INTO recetas (producto_id, ingrediente_id, cantidad_usada, rendimiento_piezas)
                        VALUES (@p, @i, @c, @r)", conn, tx);
                    cmd.Parameters.AddWithValue("p", r.Id_producto);
                    cmd.Parameters.AddWithValue("i", ri.Id_ingrediente);
                    cmd.Parameters.AddWithValue("c", ri.CantidadUsada);
                    cmd.Parameters.AddWithValue("r", Math.Max(1, r.RendimientoPiezas));
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        // Cantidad de cada ingrediente que consumen los detalles, según su receta (cantidad_usada rinde rendimiento_piezas)
        private Dictionary<int, decimal> CalcularConsumo(NpgsqlConnection conn, NpgsqlTransaction tx, IEnumerable<DetalleVenta> detalles)
        {
            var consumo = new Dictionary<int, decimal>();
            foreach (var d in detalles)
            {
                var cmd = new NpgsqlCommand(
                    "SELECT ingrediente_id, cantidad_usada, rendimiento_piezas FROM recetas WHERE producto_id=@p", conn, tx);
                cmd.Parameters.AddWithValue("p", d.Id_producto);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        decimal porPieza = reader.GetDecimal(1) / Math.Max(1, reader.GetInt32(2));
                        consumo.TryGetValue(id, out decimal acumulado);
                        consumo[id] = acumulado + porPieza * d.Cantidad;
                    }
            }
            return consumo;
        }

        // Dentro de una transacción bloquea las filas (FOR UPDATE) para que dos cajas no gasten el mismo stock
        private List<string> BuscarFaltantes(NpgsqlConnection conn, NpgsqlTransaction tx, Dictionary<int, decimal> consumo)
        {
            var faltantes = new List<string>();
            foreach (var kv in consumo)
            {
                var cmd = new NpgsqlCommand(
                    "SELECT nombre, cantidad_disponible, unidad_medida FROM ingredientes WHERE ingrediente_id=@id" +
                    (tx != null ? " FOR UPDATE" : ""), conn, tx);
                cmd.Parameters.AddWithValue("id", kv.Key);
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read() && reader.GetDecimal(1) < kv.Value)
                        faltantes.Add($"{reader.GetString(0)}: se necesitan {kv.Value:0.###} {reader.GetString(2)}, hay {reader.GetDecimal(1):0.###}");
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
            using (var tx = conn.BeginTransaction())
            {
                var cmdV = new NpgsqlCommand(@"
                    INSERT INTO ventas (fecha_venta, metodo_pago, monto_recibido, cambio, total, usuario_id, cliente_id)
                    VALUES (@f, @m, @e, @ca, @t, @u, @c)
                    RETURNING venta_id", conn, tx);
                cmdV.Parameters.AddWithValue("f", v.Fecha);
                cmdV.Parameters.AddWithValue("m", v.MetodoPago.ToString());
                cmdV.Parameters.AddWithValue("e", v.EfectivoRecibido);
                cmdV.Parameters.AddWithValue("ca", v.Cambio);
                cmdV.Parameters.AddWithValue("t", v.Total);
                cmdV.Parameters.AddWithValue("u", v.Id_usuario);
                cmdV.Parameters.AddWithValue("c", NpgsqlTypes.NpgsqlDbType.Integer, (object)v.Id_cliente ?? DBNull.Value);
                int ventaId = (int)cmdV.ExecuteScalar();

                foreach (var d in v.Detalles)
                {
                    var cmdD = new NpgsqlCommand(@"
                        INSERT INTO detalle_ventas (venta_id, producto_id, cantidad, precio_unitario, subtotal)
                        VALUES (@vid, @pid, @c, @pu, @s)", conn, tx);
                    cmdD.Parameters.AddWithValue("vid", ventaId);
                    cmdD.Parameters.AddWithValue("pid", d.Id_producto);
                    cmdD.Parameters.AddWithValue("c", d.Cantidad);
                    cmdD.Parameters.AddWithValue("pu", d.PrecioUnitario);
                    cmdD.Parameters.AddWithValue("s", d.Subtotal);
                    cmdD.ExecuteNonQuery();
                }

                // Descontar ingredientes según receta; se revalida aquí por si el stock cambió desde el carrito
                var consumo = CalcularConsumo(conn, tx, v.Detalles);
                var faltantes = BuscarFaltantes(conn, tx, consumo);
                if (faltantes.Count > 0)
                    throw new InvalidOperationException("Ingredientes insuficientes:\n" + string.Join("\n", faltantes));
                foreach (var kv in consumo)
                    RegistrarMovimiento(conn, tx, kv.Key, "Venta", -kv.Value, $"Venta #{ventaId}");

                tx.Commit();  // sin Commit, el Dispose de tx hace rollback
                return ventaId;
            }
        }

        // Rango [desde, hasta+1día) en hora local; null = sin límite
        private static string FiltroFechas(NpgsqlCommand cmd, string columna, DateTime? desde, DateTime? hasta)
        {
            string where = "WHERE TRUE";
            if (desde.HasValue) { where += $" AND {columna} >= @desde"; cmd.Parameters.AddWithValue("desde", desde.Value.Date); }
            if (hasta.HasValue) { where += $" AND {columna} < @hasta"; cmd.Parameters.AddWithValue("hasta", hasta.Value.Date.AddDays(1)); }
            return where;
        }

        public List<Venta> ObtenerVentas(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var lista = new List<Venta>();
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand { Connection = conn };
                cmd.CommandText = $@"
                    SELECT v.venta_id, v.fecha_venta, v.metodo_pago, v.monto_recibido, v.usuario_id,
                           COALESCE(u.nombre,''), v.cliente_id
                    FROM ventas v LEFT JOIN usuarios u ON v.usuario_id=u.usuario_id
                    {FiltroFechas(cmd, "v.fecha_venta", fechaInicio, fechaFin)} ORDER BY v.fecha_venta DESC";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new Venta
                        {
                            Id_venta = reader.GetInt32(0),
                            Fecha = reader.GetDateTime(1),
                            MetodoPago = (MetodoPago)Enum.Parse(typeof(MetodoPago), reader.GetString(2)),
                            EfectivoRecibido = reader.GetDecimal(3),
                            Id_usuario = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                            UsuarioNombre = reader.GetString(5),
                            Id_cliente = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                        });

                foreach (var v in lista)
                {
                    var cmdD = new NpgsqlCommand(@"
                        SELECT d.producto_id, p.nombre, d.cantidad, d.precio_unitario
                        FROM detalle_ventas d JOIN productos p ON d.producto_id=p.producto_id
                        WHERE d.venta_id=@id", conn);
                    cmdD.Parameters.AddWithValue("id", v.Id_venta);
                    using (var readerD = cmdD.ExecuteReader())
                        while (readerD.Read())
                            v.Detalles.Add(new DetalleVenta
                            {
                                Id_producto = readerD.GetInt32(0),
                                ProductoNombre = readerD.GetString(1),
                                Cantidad = readerD.GetInt32(2),
                                PrecioUnitario = readerD.GetDecimal(3)
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
                var cmd = new NpgsqlCommand { Connection = conn };
                cmd.CommandText = $@"
                    SELECT COALESCE(SUM(total), 0),
                           COALESCE(SUM(total) FILTER (WHERE metodo_pago='Efectivo'), 0),
                           COALESCE(SUM(total) FILTER (WHERE metodo_pago='Tarjeta'), 0),
                           COALESCE(SUM(total) FILTER (WHERE metodo_pago='Transferencia'), 0),
                           COUNT(*)
                    FROM ventas {FiltroFechas(cmd, "fecha_venta", fecha, fecha)}";
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return (reader.GetDecimal(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetDecimal(3), (int)reader.GetInt64(4));
                }
            }
        }

        public List<(string producto, int cantidad, decimal total)> ObtenerProductosMasVendidos(
            DateTime? desde = null, DateTime? hasta = null, int top = 10)
        {
            var lista = new List<(string, int, decimal)>();
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand { Connection = conn };
                cmd.CommandText = $@"
                    SELECT p.nombre, SUM(d.cantidad) AS total_cant, SUM(d.subtotal) AS total_val
                    FROM detalle_ventas d
                    JOIN ventas v ON d.venta_id=v.venta_id
                    JOIN productos p ON d.producto_id=p.producto_id
                    {FiltroFechas(cmd, "v.fecha_venta", desde, hasta)}
                    GROUP BY p.nombre ORDER BY total_cant DESC LIMIT @top";
                cmd.Parameters.AddWithValue("top", top);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add((reader.GetString(0), (int)reader.GetInt64(1), reader.GetDecimal(2)));
            }
            return lista;
        }

        // Agrupa en C# para usar la hora local de esta PC, no la zona horaria del servidor
        private List<(DateTime fecha, decimal total)> TotalesVentas(DateTime desde, DateTime hasta)
        {
            var lista = new List<(DateTime, decimal)>();
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand { Connection = conn };
                cmd.CommandText = $"SELECT fecha_venta, total FROM ventas {FiltroFechas(cmd, "fecha_venta", desde, hasta)}";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add((reader.GetDateTime(0), reader.GetDecimal(1)));
            }
            return lista;
        }

        public List<(string fecha, decimal total)> ObtenerVentasPorDia(DateTime desde, DateTime hasta)
        {
            return TotalesVentas(desde, hasta)
                .GroupBy(v => v.fecha.Date).OrderBy(g => g.Key)
                .Select(g => (g.Key.ToString("yyyy-MM-dd"), g.Sum(v => v.total))).ToList();
        }

        public List<(int hora, decimal total)> ObtenerVentasPorHora(DateTime fecha)
        {
            return TotalesVentas(fecha, fecha)
                .GroupBy(v => v.fecha.Hour).OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Sum(v => v.total))).ToList();
        }

        // ==================== CLIENTES ====================
        public List<Cliente> ObtenerClientes()
        {
            var lista = new List<Cliente>();
            using (var conn = GetConnection())
            using (var reader = new NpgsqlCommand(@"
                SELECT c.cliente_id, c.nombre, COALESCE(c.telefono,''), COALESCE(c.direccion,''),
                       COALESCE(c.notas,''), COALESCE(c.registrado_en, now()), COALESCE(SUM(v.total),0)
                FROM clientes c
                LEFT JOIN ventas v ON v.cliente_id=c.cliente_id
                WHERE c.activo IS NOT FALSE
                GROUP BY c.cliente_id ORDER BY c.nombre", conn).ExecuteReader())
                while (reader.Read())
                    lista.Add(new Cliente
                    {
                        Id_cliente = reader.GetInt32(0),
                        Nombre = reader.GetString(1),
                        Telefono = reader.GetString(2),
                        Direccion = reader.GetString(3),
                        Notas = reader.GetString(4),
                        FechaRegistro = reader.GetDateTime(5),
                        TotalCompras = reader.GetDecimal(6)
                    });
            return lista;
        }

        public int GuardarCliente(Cliente c)
        {
            using (var conn = GetConnection())
            {
                NpgsqlCommand cmd;
                if (c.Id_cliente == 0)
                {
                    cmd = new NpgsqlCommand(@"
                        INSERT INTO clientes (nombre, telefono, direccion, notas, registrado_en)
                        VALUES (@n,@t,@d,@no,@f) RETURNING cliente_id", conn);
                    cmd.Parameters.AddWithValue("f", c.FechaRegistro == default ? DateTime.Now : c.FechaRegistro);
                }
                else
                {
                    cmd = new NpgsqlCommand(
                        "UPDATE clientes SET nombre=@n, telefono=@t, direccion=@d, notas=@no WHERE cliente_id=@id RETURNING cliente_id", conn);
                    cmd.Parameters.AddWithValue("id", c.Id_cliente);
                }
                cmd.Parameters.AddWithValue("n", c.Nombre);
                cmd.Parameters.AddWithValue("t", c.Telefono ?? "");
                cmd.Parameters.AddWithValue("d", c.Direccion ?? "");
                cmd.Parameters.AddWithValue("no", c.Notas ?? "");
                return (int)cmd.ExecuteScalar();
            }
        }

        // ==================== PEDIDOS ====================
        public int GuardarPedido(Pedido p)
        {
            using (var conn = GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                NpgsqlCommand cmd;
                if (p.Id_pedido == 0)
                    cmd = new NpgsqlCommand(@"
                        INSERT INTO pedidos (fecha_solicitud, fecha_entrega_programada, estado, anticipo, total, notas_entrega, cliente_id, usuario_id)
                        VALUES (@fs, @fe, @e, @a, @t, @n, @c, @u) RETURNING pedido_id", conn, tx);
                else
                {
                    cmd = new NpgsqlCommand(@"
                        UPDATE pedidos SET fecha_solicitud=@fs, fecha_entrega_programada=@fe, estado=@e, anticipo=@a, total=@t,
                               notas_entrega=@n, cliente_id=@c, usuario_id=@u WHERE pedido_id=@id RETURNING pedido_id", conn, tx);
                    cmd.Parameters.AddWithValue("id", p.Id_pedido);
                }
                cmd.Parameters.AddWithValue("fs", p.FechaSolicitud);
                cmd.Parameters.AddWithValue("fe", p.FechaEntrega);
                cmd.Parameters.AddWithValue("e", p.Estado.ToString());
                cmd.Parameters.AddWithValue("a", p.Anticipo);
                cmd.Parameters.AddWithValue("t", p.Total);
                cmd.Parameters.AddWithValue("n", p.Notas ?? "");
                cmd.Parameters.AddWithValue("c", p.Id_cliente);
                cmd.Parameters.AddWithValue("u", p.Id_usuario);
                int idPedido = (int)cmd.ExecuteScalar();

                var borrar = new NpgsqlCommand("DELETE FROM detalle_pedidos WHERE pedido_id=@id", conn, tx);
                borrar.Parameters.AddWithValue("id", idPedido);
                borrar.ExecuteNonQuery();

                foreach (var d in p.Detalles)
                {
                    var cmdD = new NpgsqlCommand(@"
                        INSERT INTO detalle_pedidos (pedido_id, producto_id, cantidad, precio_unitario, subtotal, especificaciones)
                        VALUES (@pid, @prod, @c, @pu, @s, @es)", conn, tx);
                    cmdD.Parameters.AddWithValue("pid", idPedido);
                    cmdD.Parameters.AddWithValue("prod", d.Id_producto);
                    cmdD.Parameters.AddWithValue("c", d.Cantidad);
                    cmdD.Parameters.AddWithValue("pu", d.PrecioUnitario);
                    cmdD.Parameters.AddWithValue("s", d.Subtotal);
                    cmdD.Parameters.AddWithValue("es", d.Especificaciones ?? "");
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
                var cmd = new NpgsqlCommand($@"
                    SELECT p.pedido_id, p.fecha_solicitud, p.fecha_entrega_programada, p.estado, p.anticipo,
                           COALESCE(p.notas_entrega,''), p.cliente_id, c.nombre, COALESCE(p.usuario_id,0)
                    FROM pedidos p JOIN clientes c ON p.cliente_id=c.cliente_id
                    {(estado.HasValue ? "WHERE p.estado=@e" : "")}
                    ORDER BY p.fecha_entrega_programada", conn);
                if (estado.HasValue) cmd.Parameters.AddWithValue("e", estado.Value.ToString());
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new Pedido
                        {
                            Id_pedido = reader.GetInt32(0),
                            FechaSolicitud = reader.GetDateTime(1),
                            FechaEntrega = reader.GetDateTime(2),
                            Estado = (EstadoPedido)Enum.Parse(typeof(EstadoPedido), reader.GetString(3)),
                            Anticipo = reader.GetDecimal(4),
                            Notas = reader.GetString(5),
                            Id_cliente = reader.GetInt32(6),
                            ClienteNombre = reader.GetString(7),
                            Id_usuario = reader.GetInt32(8)
                        });

                foreach (var p in lista)
                {
                    var cmdD = new NpgsqlCommand(@"
                        SELECT d.producto_id, pr.nombre, d.cantidad, d.precio_unitario, COALESCE(d.especificaciones,'')
                        FROM detalle_pedidos d JOIN productos pr ON d.producto_id=pr.producto_id
                        WHERE d.pedido_id=@id", conn);
                    cmdD.Parameters.AddWithValue("id", p.Id_pedido);
                    using (var readerD = cmdD.ExecuteReader())
                        while (readerD.Read())
                            p.Detalles.Add(new DetallePedido
                            {
                                Id_producto = readerD.GetInt32(0),
                                ProductoNombre = readerD.GetString(1),
                                Cantidad = readerD.GetInt32(2),
                                PrecioUnitario = readerD.GetDecimal(3),
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
                var cmd = new NpgsqlCommand("UPDATE pedidos SET estado=@e WHERE pedido_id=@id", conn);
                cmd.Parameters.AddWithValue("e", estado.ToString());
                cmd.Parameters.AddWithValue("id", idPedido);
                cmd.ExecuteNonQuery();
            }
        }

        // ==================== CORTE DE CAJA ====================
        /// <summary>EfectivoEsperado: usar ObtenerResumenDia(fecha).efectivo (ventas en efectivo del día).</summary>
        public int GuardarCorteCaja(CorteCaja c)
        {
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO cortes_caja (fecha, efectivo_esperado, efectivo_contado, usuario_id)
                    VALUES (@f, @es, @co, @u) RETURNING corte_id", conn);
                cmd.Parameters.AddWithValue("f", c.Fecha);
                cmd.Parameters.AddWithValue("es", c.EfectivoEsperado);
                cmd.Parameters.AddWithValue("co", c.EfectivoContado);
                cmd.Parameters.AddWithValue("u", c.Id_usuario);
                return (int)cmd.ExecuteScalar();
            }
        }

        public List<CorteCaja> ObtenerCortesCaja()
        {
            var lista = new List<CorteCaja>();
            using (var conn = GetConnection())
            using (var reader = new NpgsqlCommand(@"
                SELECT k.corte_id, k.fecha, k.efectivo_esperado, k.efectivo_contado, k.usuario_id, COALESCE(u.nombre,'')
                FROM cortes_caja k LEFT JOIN usuarios u ON k.usuario_id=u.usuario_id
                ORDER BY k.fecha DESC", conn).ExecuteReader())
                while (reader.Read())
                    lista.Add(new CorteCaja
                    {
                        Id_corte = reader.GetInt32(0),
                        Fecha = reader.GetDateTime(1),
                        EfectivoEsperado = reader.GetDecimal(2),
                        EfectivoContado = reader.GetDecimal(3),
                        Id_usuario = reader.GetInt32(4),
                        UsuarioNombre = reader.GetString(5)
                    });
            return lista;
        }

        // ==================== PRODUCCION ====================
        public List<RegistroProduccion> ObtenerProduccion(DateTime? fecha = null)
        {
            var lista = new List<RegistroProduccion>();
            using (var conn = GetConnection())
            {
                var cmd = new NpgsqlCommand { Connection = conn };
                cmd.CommandText = $@"
                    SELECT r.produccion_id, r.fecha, r.producto_id, p.nombre,
                           r.cantidad_hecha, r.cantidad_vendida, COALESCE(r.usuario_id,0)
                    FROM produccion r JOIN productos p ON r.producto_id=p.producto_id
                    {FiltroFechas(cmd, "r.fecha", fecha, fecha)} ORDER BY r.fecha DESC";
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        lista.Add(new RegistroProduccion
                        {
                            Id_produccion = reader.GetInt32(0),
                            Fecha = reader.GetDateTime(1),
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
                NpgsqlCommand cmd;
                if (reg.Id_produccion == 0)
                {
                    cmd = new NpgsqlCommand(@"
                        INSERT INTO produccion (fecha, producto_id, cantidad_hecha, cantidad_vendida, usuario_id)
                        VALUES (@f,@pid,@ch,@cv,@u)", conn);
                    cmd.Parameters.AddWithValue("f", reg.Fecha);
                    cmd.Parameters.AddWithValue("pid", reg.Id_producto);
                    cmd.Parameters.AddWithValue("u", reg.Id_usuario);
                }
                else
                {
                    cmd = new NpgsqlCommand(
                        "UPDATE produccion SET cantidad_hecha=@ch, cantidad_vendida=@cv WHERE produccion_id=@id", conn);
                    cmd.Parameters.AddWithValue("id", reg.Id_produccion);
                }
                cmd.Parameters.AddWithValue("ch", reg.CantidadHecha);
                cmd.Parameters.AddWithValue("cv", reg.CantidadVendida);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
