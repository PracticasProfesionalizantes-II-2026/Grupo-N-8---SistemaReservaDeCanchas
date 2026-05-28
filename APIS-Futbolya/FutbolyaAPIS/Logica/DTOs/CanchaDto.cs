
namespace FutbolyaAPIS.Logica.Dtos;

public record CanchaDto(
    int id, 
    string nombre, 
    string Descripcion, 
    bool estado
    
);


public record CachaCreateDto(string Descripcion);