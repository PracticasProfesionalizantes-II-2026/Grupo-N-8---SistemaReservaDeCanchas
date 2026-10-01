using FutbolYaAPI.Logica.DTOs;
using FutbolYaAPI.Repositorios;

namespace FutbolYaAPI.Logica;

public interface IEstadisticaLogica
{
    Task<EstadisticaDiaDto> ObtenerVentasDia(DateTime fecha, List<int>? productos);
    Task<EstadisticaSemanaDto> ObtenerVentasSemana(DateTime fecha, List<int>? productos);
    Task<EstadisticaMesDto> ObtenerVentasMes(DateTime fecha, List<int>? productos);
}

// Calcula estadísticas de ventas (día/semana/mes) agregando los detalles de venta obtenidos
// del repositorio, con filtro opcional por producto. Se ubica entre los Endpoints y
// EstadisticaRepository; no realiza validaciones de negocio, solo agregación de datos.
public class EstadisticaLogica : IEstadisticaLogica
{
    private readonly IEstadisticaRepository _repo;

    private static readonly string[] DiasSemana =
        { "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo" };

    public EstadisticaLogica(IEstadisticaRepository repo)
    {
        _repo = repo;
    }

    // ── Reporte Día: agrupa por producto (para el pie chart) ────────────
    public async Task<EstadisticaDiaDto> ObtenerVentasDia(DateTime fecha, List<int>? productos)
    {
        var dia = fecha.Date;
        var filas = await _repo.ObtenerDetalles(dia, dia, productos);

        var porProducto = filas
            .GroupBy(f => new { f.Cod_Producto, f.Nombre_Producto })
            .Select(g => new ProductoVendidoDto(
                g.Key.Cod_Producto,
                g.Key.Nombre_Producto,
                g.Sum(x => x.Cantidad),
                g.Sum(x => x.SubTotal)))
            .OrderByDescending(p => p.Cantidad)
            .ToList();

        return new EstadisticaDiaDto(
            dia,
            porProducto,
            porProducto.Sum(p => p.Cantidad),
            porProducto.Sum(p => p.Total));
    }

    // ── Reporte Semana: agrupa por día (Lunes a Domingo) ─────────────────
    public async Task<EstadisticaSemanaDto> ObtenerVentasSemana(DateTime fecha, List<int>? productos)
    {
        var (desde, hasta) = RangoSemana(fecha);
        var filas = await _repo.ObtenerDetalles(desde, hasta, productos);

        var dias = new List<PuntoDto>();
        for (int i = 0; i < 7; i++)
        {
            var diaActual = desde.AddDays(i);
            var filasDia = filas.Where(f => f.Fecha.Date == diaActual.Date).ToList();
            dias.Add(new PuntoDto(DiasSemana[i], filasDia.Sum(f => f.Cantidad), filasDia.Sum(f => f.SubTotal)));
        }

        return new EstadisticaSemanaDto(desde, hasta, dias, dias.Sum(d => d.Cantidad), dias.Sum(d => d.Total));
    }

    // ── Reporte Mes: agrupa en bloques de 7 días (Semana 1..N) ───────────
    public async Task<EstadisticaMesDto> ObtenerVentasMes(DateTime fecha, List<int>? productos)
    {
        var (desde, hasta) = RangoMes(fecha);
        var filas = await _repo.ObtenerDetalles(desde, hasta, productos);

        var semanas = new List<PuntoDto>();
        var inicioSemana = desde;
        var numero = 1;

        while (inicioSemana <= hasta)
        {
            var finSemana = inicioSemana.AddDays(6);
            if (finSemana > hasta) finSemana = hasta;

            var filasSemana = filas
                .Where(f => f.Fecha.Date >= inicioSemana.Date && f.Fecha.Date <= finSemana.Date)
                .ToList();

            semanas.Add(new PuntoDto($"Semana {numero}", filasSemana.Sum(f => f.Cantidad), filasSemana.Sum(f => f.SubTotal)));

            inicioSemana = finSemana.AddDays(1);
            numero++;
        }

        return new EstadisticaMesDto(desde, hasta, semanas, semanas.Sum(s => s.Cantidad), semanas.Sum(s => s.Total));
    }

    // ── Helpers de rango de fechas ────────────────────────────────────────
    private static (DateTime desde, DateTime hasta) RangoSemana(DateTime fecha)
    {
        // Semana de Lunes a Domingo que contiene a "fecha"
        var diff = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
        var desde = fecha.Date.AddDays(-diff);
        var hasta = desde.AddDays(6);
        return (desde, hasta);
    }

    private static (DateTime desde, DateTime hasta) RangoMes(DateTime fecha)
    {
        var desde = new DateTime(fecha.Year, fecha.Month, 1);
        var hasta = desde.AddMonths(1).AddDays(-1);
        return (desde, hasta);
    }
}
