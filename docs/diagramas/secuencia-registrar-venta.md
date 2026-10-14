# Diagrama de secuencia: Registrar venta

Refleja el código actual (`UC_PuntoVenta` y `DatabaseManager`) con la base en PostgreSQL. GitHub dibuja el diagrama automáticamente.

```mermaid
sequenceDiagram
    actor Cajero
    participant PV as :UC_PuntoVenta
    participant S as :Session
    participant DB as :DatabaseManager
    participant PG as PostgreSQL (migacore)

    Cajero->>PV: 1: abrirPuntoDeVenta()
    PV->>DB: 2: ObtenerProductos()
    DB->>PG: 3: SELECT ... FROM productos WHERE activo IS NOT FALSE
    PG-->>DB: filas
    DB-->>PV: lista de productos
    Note over PV: filtra con EstaDisponible();<br/>sin resultados muestra "Producto no encontrado"

    loop mientras haya productos por agregar
        Cajero->>PV: 4: seleccionarProducto(p)
        PV->>DB: 5: ValidarIngredientes(carrito + p)
        DB->>PG: SELECT ... FROM recetas / ingredientes
        PG-->>DB: filas
        DB-->>PV: faltantes
        alt faltan ingredientes
            PV-->>Cajero: "Ingredientes insuficientes"
        else alcanzan
            PV->>PV: 6: AgregarAlCarrito(p) / ActualizarCarrito()
        end
    end

    Cajero->>PV: 7: clic Cobrar → ProcesarVenta()
    PV->>S: 8: UsuarioActual
    S-->>PV: usuario
    PV->>DB: 9: GuardarVenta(venta)
    DB->>PG: 10: BEGIN
    DB->>PG: 11: INSERT INTO ventas ... RETURNING venta_id
    PG-->>DB: ventaId
    loop por cada detalle
        DB->>PG: 12: INSERT INTO detalle_ventas
    end
    DB->>PG: 13: SELECT ... FROM ingredientes ... FOR UPDATE
    alt ingredientes suficientes
        loop por cada ingrediente de las recetas
            DB->>PG: 14: UPDATE ingredientes (cantidad_disponible - consumo)
            DB->>PG: 15: INSERT INTO movimientos_inventario (tipo 'Venta')
        end
        DB->>PG: 16: COMMIT
        DB-->>PV: ventaId
        PV->>PV: 17: MostrarTicket(venta, ventaId)
        PV-->>Cajero: ticket y cambio
    else faltan ingredientes o error
        DB->>PG: 16': ROLLBACK
        DB-->>PV: excepción
        PV-->>Cajero: "No se pudo guardar la venta"
    end
```
