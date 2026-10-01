using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

// Administra qué bloques del catálogo global de horarios ofrece cada cancha.
public interface ICanchaHorarioRepository
{
    Task Reemplazar(int codCancha, IEnumerable<int> codHorarios);
}

public class CanchaHorarioRepository : ICanchaHorarioRepository
{
    private readonly AppDbContext _db;

    public CanchaHorarioRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task Reemplazar(int codCancha, IEnumerable<int> codHorarios)
    {
        var actuales = _db.CanchaHorarios.Where(ch => ch.Cod_Cancha == codCancha);
        _db.CanchaHorarios.RemoveRange(actuales);

        foreach (var codHorario in codHorarios.Distinct())
            _db.CanchaHorarios.Add(new Cancha_Horario { Cod_Cancha = codCancha, Cod_Horario = codHorario });

        await _db.SaveChangesAsync();
    }
}
