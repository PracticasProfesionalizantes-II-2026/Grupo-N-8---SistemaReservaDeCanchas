using System.Text.Json.Serialization;

namespace FutbolyaMVC.DTOs;

public record ProductoResponse(
    [property: JsonPropertyName("cod_Producto")] int Cod_Producto,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("cantidad")] int Cantidad,
    [property: JsonPropertyName("precio")] decimal Precio,
    [property: JsonPropertyName("tipo")] string Tipo   // "Bebida" o "Comida"
);

public record ProductoCreateRequest(
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo
);
