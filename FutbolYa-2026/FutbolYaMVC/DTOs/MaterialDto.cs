namespace FutbolYaMVC.DTOs;

public record MaterialResponse(
    int Cod_Material,
    string Nombre,
    int Cant_Material,
    bool Activo
);

public record MaterialCreateRequest(
    string Nombre,
    int Cant_Material
);

// Sin Cant_Material — el stock se edita solo por AjustarStockRequest.
public record MaterialUpdateRequest(
    string Nombre
);
