using System.Text.Json.Serialization;

namespace FutbolyaMVC.DTOs;

// ── Lectura ────────────────────────────────────────────────────────────

public record VentaDetalladaResponse(
    [property: JsonPropertyName("cod_Venta_Detallada")] int Cod_Venta_Detallada,
    [property: JsonPropertyName("cod_Producto")] int Cod_Producto,
    [property: JsonPropertyName("nombre_Producto")] string Nombre_Producto,
    [property: JsonPropertyName("cantidad")] int Cantidad,
    [property: JsonPropertyName("precio")] decimal Precio,
    [property: JsonPropertyName("subTotal")] decimal SubTotal
);

public record VentaResponse(
    [property: JsonPropertyName("cod_Venta")] int Cod_Venta,
    [property: JsonPropertyName("fecha")] DateTime Fecha,
    [property: JsonPropertyName("hora")] TimeSpan Hora,
    [property: JsonPropertyName("montoTotal")] decimal MontoTotal,
    [property: JsonPropertyName("cod_Usuario")] int Cod_Usuario,
    [property: JsonPropertyName("nombre_Usuario")] string Nombre_Usuario,
    [property: JsonPropertyName("detalle")] List<VentaDetalladaResponse>? Detalle
);

// ── Escritura ──────────────────────────────────────────────────────────

public record VentaDetalladaItemRequest(int Cod_Producto, int Cantidad);

// Lo que llega desde el JS del navegador (sin Cod_Usuario: lo agrega el Controller desde la sesión).
public record VentaCreateRequest(List<VentaDetalladaItemRequest> Detalle);

// Lo que el Service efectivamente envía a la API (con Cod_Usuario ya resuelto).
public record VentaCreateApiRequest(int Cod_Usuario, List<VentaDetalladaItemRequest> Detalle);

// ── ViewModel para la pantalla de listado ────────────────────────────────

public record VentaIndexViewModel(
    List<VentaResponse> Ventas,
    List<ProductoResponse> Productos
);
