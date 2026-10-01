namespace FutbolYaAPI.Logica.DTOs;

// Body de PATCH /api/productos/{id}/stock y /api/materiales/{id}/stock.
// Ajuste != 0: positivo = ingreso (RestaurarStock), negativo = egreso (DescontarStock).
public record AjustarStockDto(int Ajuste);
