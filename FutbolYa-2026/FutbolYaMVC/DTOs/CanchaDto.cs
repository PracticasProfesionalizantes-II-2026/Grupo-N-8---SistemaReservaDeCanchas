namespace FutbolYaMVC.DTOs;

public record CanchaResponse(
    int Cod_Cancha,
    string Nombre,
    string Descripcion,
    string Estado,          // "Disponible" | "Mantenimiento" | "Baja"
    List<int> Cod_Horarios
);

public record CanchaCreateRequest(
    string Descripcion,
    List<int> Cod_Horarios
);

public record CanchaUpdateRequest(
    string Descripcion
);

public record CanchaEstadoUpdateRequest(
    string Estado
);

public record CanchaHorariosUpdateRequest(
    List<int> Cod_Horarios
);

public record CanchaIndexViewModel(
    List<CanchaResponse> Canchas,
    List<HorarioResponse> Horarios
);
