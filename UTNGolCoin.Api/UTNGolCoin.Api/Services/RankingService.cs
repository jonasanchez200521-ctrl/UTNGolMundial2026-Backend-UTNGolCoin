using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class RankingService
    {
        private const string EstadoGanada = "GANADA";

        private readonly AppDbContext _context;

        public RankingService(AppDbContext context)
        {
            _context = context;
        }

        // Orden: primero por saldo descendente (métrica principal del juego), y como
        // desempate por aciertos descendente (para distinguir a los más "acertados"
        // entre usuarios que quedaron con el mismo saldo).
        public async Task<List<RankingItemResponse>> ObtenerRankingAsync(int? top = null)
        {
            var billeteras = await _context.Billeteras.ToListAsync();

            var statsPorUsuario = await _context.Predicciones
                .GroupBy(p => p.UsuarioId)
                .Select(g => new
                {
                    UsuarioId = g.Key,
                    Aciertos = g.Count(p => p.Estado == EstadoGanada),
                    Total = g.Count()
                })
                .ToDictionaryAsync(x => x.UsuarioId);

            var ranking = billeteras
                .Select(b =>
                {
                    statsPorUsuario.TryGetValue(b.UsuarioId, out var stats);
                    return new RankingItemResponse
                    {
                        UsuarioId = b.UsuarioId,
                        Saldo = b.Saldo,
                        Aciertos = stats?.Aciertos ?? 0,
                        TotalPredicciones = stats?.Total ?? 0
                    };
                })
                .OrderByDescending(r => r.Saldo)
                .ThenByDescending(r => r.Aciertos)
                .ToList();

            if (top.HasValue && top.Value > 0)
            {
                ranking = ranking.Take(top.Value).ToList();
            }

            return ranking;
        }
    }
}
