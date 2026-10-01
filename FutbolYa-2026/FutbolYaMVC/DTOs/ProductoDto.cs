namespace FutbolYaMVC.DTOs;

public record ProductoResponse(
    int Cod_Producto,
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo,   // "Bebida" o "Comida"
    bool Activo
);

public record ProductoCreateRequest(
    string Nombre,
    int Cantidad,
    decimal Precio,
    string Tipo
);

// Sin Cantidad — el stock se edita solo por AjustarStockRequest.
public record ProductoUpdateRequest(
    string Nombre,
    decimal Precio,
    string Tipo
);

public record AjustarStockRequest(int Ajuste);
