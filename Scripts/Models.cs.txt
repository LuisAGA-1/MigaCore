using System;
using System.Linq;
using System.Collections.Generic;

namespace PanaderiaSystem.Models
{
    // ==================== USUARIOS ====================
    public enum RolUsuario { Administrador, Cajero, Produccion }

    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Usuario { get; set; }
        public string Password { get; set; }
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; } = true;
    }

    // ==================== PRODUCTOS ====================
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Color { get; set; } = "#D4A857";
    }

    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }
        public bool Activo { get; set; } = true;
        public string ImagenPath { get; set; }
    }

    // ==================== INVENTARIO ====================
    public class Ingrediente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; }  // kg, lt, pz, etc.
        public decimal CantidadMinima { get; set; }
        public bool BajoStock => Cantidad <= CantidadMinima;
    }

    public class RecetaIngrediente
    {
        public int IngredienteId { get; set; }
        public string IngredienteNombre { get; set; }
        public decimal CantidadUsada { get; set; }
        public string Unidad { get; set; }
    }

    public class Receta
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public int RendimientoPiezas { get; set; }
        public List<RecetaIngrediente> Ingredientes { get; set; } = new List<RecetaIngrediente>();
    }

    // ==================== VENTAS ====================
    public enum MetodoPago { Efectivo, Tarjeta, Transferencia }

    public class DetalleVenta
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }

    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public List<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
        public decimal Total => Detalles.Sum(d => d.Subtotal);
        public MetodoPago MetodoPago { get; set; }
        public decimal EfectivoRecibido { get; set; }
        public decimal Cambio => MetodoPago == MetodoPago.Efectivo ? EfectivoRecibido - Total : 0;
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; }
        public int? ClienteId { get; set; }
    }

    // ==================== CLIENTES ====================
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public string Notas { get; set; }
        public DateTime FechaRegistro { get; set; }
        public decimal TotalCompras { get; set; }
    }

    // ==================== PRODUCCION ====================
    public class RegistroProduccion
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public int CantidadHecha { get; set; }
        public int CantidadVendida { get; set; }
        public int Sobrante => CantidadHecha - CantidadVendida;
        public int UsuarioId { get; set; }
    }
}