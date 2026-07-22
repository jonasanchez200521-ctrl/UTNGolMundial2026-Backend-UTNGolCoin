using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class ReporteService
    {
        private const string TipoTransaccionPremio = "PREMIO";

        private readonly AppDbContext _context;

        public ReporteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MonedasCirculacionResponse> ObtenerMonedasEnCirculacionAsync()
        {
            var totalMonedas = await _context.Billeteras.SumAsync(b => (decimal?)b.Saldo) ?? 0m;
            var cantidadBilleteras = await _context.Billeteras.CountAsync();
            var totalPagadoEnPremios = await _context.Transacciones
                .Where(t => t.Tipo == TipoTransaccionPremio)
                .SumAsync(t => (decimal?)t.Monto) ?? 0m;

            return new MonedasCirculacionResponse
            {
                TotalMonedasEnCirculacion = totalMonedas,
                CantidadBilleteras = cantidadBilleteras,
                TotalPagadoEnPremios = totalPagadoEnPremios
            };
        }

        public async Task<List<PartidoApostadoResponse>> ObtenerPartidosMasApostadosAsync(int? top)
        {
            var ranking = await _context.Predicciones
                .GroupBy(p => p.PartidoId)
                .Select(g => new PartidoApostadoResponse
                {
                    PartidoId = g.Key,
                    CantidadPredicciones = g.Count()
                })
                .OrderByDescending(r => r.CantidadPredicciones)
                .ToListAsync();

            if (top.HasValue && top.Value > 0)
            {
                ranking = ranking.Take(top.Value).ToList();
            }

            return ranking;
        }
    }
}
