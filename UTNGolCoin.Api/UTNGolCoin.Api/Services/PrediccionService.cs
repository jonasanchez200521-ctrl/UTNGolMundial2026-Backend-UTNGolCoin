using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Models;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class PrediccionService
    {
        private const string TipoTransaccionPrediccion = "PREDICCION";
        private const string EstadoPendiente = "PENDIENTE";

        // Cuotas fijas por pronóstico. El proyecto no exige cuotas dinámicas.
        private static readonly Dictionary<string, decimal> CuotasPorPronostico = new()
        {
            ["LOCAL"] = 2.0m,
            ["EMPATE"] = 3.0m,
            ["VISITANTE"] = 2.5m
        };

        private readonly AppDbContext _context;

        public PrediccionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PrediccionResponse> CrearPrediccionAsync(int usuarioId, int partidoId, string pronostico, decimal monto)
        {
            if (monto <= 0)
            {
                throw new ArgumentException("El monto debe ser mayor a 0.");
            }

            var pronosticoNormalizado = pronostico?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!CuotasPorPronostico.TryGetValue(pronosticoNormalizado, out var cuota))
            {
                throw new ArgumentException("El pronóstico debe ser LOCAL, EMPATE o VISITANTE.");
            }

            var billetera = await _context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);
            if (billetera is null)
            {
                throw new KeyNotFoundException($"El usuario {usuarioId} no tiene una billetera creada.");
            }

            if (billetera.Saldo < monto)
            {
                throw new ArgumentException(
                    $"Saldo insuficiente. Saldo actual: {billetera.Saldo.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                    $"monto solicitado: {monto.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
            }

            var yaApostoAlPartido = await _context.Predicciones
                .AnyAsync(p => p.UsuarioId == usuarioId && p.PartidoId == partidoId);
            if (yaApostoAlPartido)
            {
                throw new InvalidOperationException($"El usuario {usuarioId} ya tiene una predicción para el partido {partidoId}.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            billetera.Saldo -= monto;

            var prediccion = new Prediccion
            {
                UsuarioId = usuarioId,
                PartidoId = partidoId,
                Pronostico = pronosticoNormalizado,
                Monto = monto,
                Cuota = cuota,
                Estado = EstadoPendiente,
                Fecha = DateTime.UtcNow
            };
            _context.Predicciones.Add(prediccion);
            await _context.SaveChangesAsync();

            var transaccionPrediccion = new Transaccion
            {
                BilleteraId = billetera.Id,
                Tipo = TipoTransaccionPrediccion,
                Monto = -monto,
                SaldoResultante = billetera.Saldo,
                Referencia = prediccion.Id.ToString(),
                Fecha = DateTime.UtcNow
            };
            _context.Transacciones.Add(transaccionPrediccion);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return MapearResponse(prediccion);
        }

        public async Task<List<PrediccionResponse>> ObtenerPorUsuarioIdAsync(int usuarioId)
        {
            var predicciones = await _context.Predicciones
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return predicciones.Select(MapearResponse).ToList();
        }

        private static PrediccionResponse MapearResponse(Prediccion prediccion)
        {
            return new PrediccionResponse
            {
                Id = prediccion.Id,
                UsuarioId = prediccion.UsuarioId,
                PartidoId = prediccion.PartidoId,
                Pronostico = prediccion.Pronostico,
                Monto = prediccion.Monto,
                Cuota = prediccion.Cuota,
                Estado = prediccion.Estado,
                Fecha = prediccion.Fecha
            };
        }
    }
}
