using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Models;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class LiquidacionService
    {
        private const string TipoTransaccionPremio = "PREMIO";
        private const string EstadoPendiente = "PENDIENTE";
        private const string EstadoGanada = "GANADA";
        private const string EstadoPerdida = "PERDIDA";

        private static readonly HashSet<string> ResultadosValidos = new() { "LOCAL", "EMPATE", "VISITANTE" };

        private readonly AppDbContext _context;

        public LiquidacionService(AppDbContext context)
        {
            _context = context;
        }

        // Idempotente: solo procesa predicciones en PENDIENTE. Una vez liquidadas pasan a
        // GANADA/PERDIDA, así que si Alexis (o alguien manualmente) llama de nuevo con el
        // mismo partidoId, no encuentra pendientes y no paga dos veces.
        public async Task<LiquidacionResponse> LiquidarAsync(int partidoId, string resultado)
        {
            var resultadoNormalizado = resultado?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!ResultadosValidos.Contains(resultadoNormalizado))
            {
                throw new ArgumentException("El resultado debe ser LOCAL, EMPATE o VISITANTE.");
            }

            var pendientes = await _context.Predicciones
                .Where(p => p.PartidoId == partidoId && p.Estado == EstadoPendiente)
                .ToListAsync();

            if (pendientes.Count == 0)
            {
                return new LiquidacionResponse
                {
                    PartidoId = partidoId,
                    Liquidadas = 0,
                    Ganadas = 0,
                    Perdidas = 0,
                    TotalPagado = 0
                };
            }

            var usuarioIds = pendientes.Select(p => p.UsuarioId).Distinct().ToList();
            var billeterasPorUsuario = await _context.Billeteras
                .Where(b => usuarioIds.Contains(b.UsuarioId))
                .ToDictionaryAsync(b => b.UsuarioId);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var ganadas = 0;
            var perdidas = 0;
            var totalPagado = 0m;

            foreach (var prediccion in pendientes)
            {
                if (prediccion.Pronostico == resultadoNormalizado)
                {
                    var billetera = billeterasPorUsuario[prediccion.UsuarioId];
                    var premio = Math.Round(prediccion.Monto * prediccion.Cuota, 2, MidpointRounding.AwayFromZero);

                    billetera.Saldo += premio;

                    _context.Transacciones.Add(new Transaccion
                    {
                        BilleteraId = billetera.Id,
                        Tipo = TipoTransaccionPremio,
                        Monto = premio,
                        SaldoResultante = billetera.Saldo,
                        Referencia = prediccion.Id.ToString(),
                        Fecha = DateTime.UtcNow
                    });

                    prediccion.Estado = EstadoGanada;
                    ganadas++;
                    totalPagado += premio;
                }
                else
                {
                    prediccion.Estado = EstadoPerdida;
                    perdidas++;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new LiquidacionResponse
            {
                PartidoId = partidoId,
                Liquidadas = pendientes.Count,
                Ganadas = ganadas,
                Perdidas = perdidas,
                TotalPagado = totalPagado
            };
        }
    }
}
