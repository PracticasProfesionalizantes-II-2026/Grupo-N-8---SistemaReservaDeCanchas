using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Datos;
using FutbolYaAPI.Entidades;

namespace FutbolYaAPI.Repositorios;

public interface IReservaHorarioRepository
{
    Task Agregar(Reserva_Horario reservaHorario);
}

public class ReservaHorarioRepository : IReservaHorarioRepository
{
    private readonly AppDbContext _db;

    public ReservaHorarioRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task Agregar(Reserva_Horario reservaHorario)
    {
        _db.ReservasHorarios.Add(reservaHorario);
        await _db.SaveChangesAsync();
    }
}
