using FutbolyaAPIS.Logica.Dtos;
namespace FutbolyaAPIS.Logica;



public interface ICanchaLogica
{
    public Task<IEnumerable<CanchaDto>>ObtenerTodas();

    public Task<CanchaDto>ObtenerPorId(int id);

}


public class CanchaLogica : ICanchaLogica
{
    private readonly ICanchaLogica _repo;

    public CanchaLogica(ICanchaLogica repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CanchaDto>>ObtenerTodas()
    {
        var cancha = await _repo.ObtenerTodas();

        if(cancha != null)
        {
            return cancha.Select(c => new CanchaDto(
                c.id,
                c.nombre,
                c.Descripcion,
                c.estado
            ));
        }
    }

    public async Task<CanchaDto?>ObtenerPorId(int id)
    {
        var cancha = await _repo.ObtenerPorId(id);

    }
}