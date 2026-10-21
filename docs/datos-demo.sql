-- Datos de ejemplo para clase o pruebas. NO correr en una panadería real.
-- Uso: psql -U postgres -d migacore -f docs/datos-demo.sql  (después de abrir la app una vez)
-- El usuario cajero de prueba se da de alta desde la pantalla Usuarios.

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
ON CONFLICT (nombre) DO NOTHING;
