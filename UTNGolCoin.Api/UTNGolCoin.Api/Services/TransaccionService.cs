using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class TransaccionService
    {
        private readonly AppDbContext _context;

        public TransaccionService(AppDbContext context)
        {
            _context = context;
        }

        // RF14: historial de transacciones (bonos, predicciones y premios) de un usuario.
        public async Task<List<TransaccionResponse>> ObtenerPorUsuarioIdAsync(int usuarioId)
        {
            var billetera = await _context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);
            if (billetera is null)
            {
                throw new KeyNotFoundException($"El usuario {usuarioId} no tiene una billetera creada.");
            }

            return await _context.Transacciones
                .Where(t => t.BilleteraId == billetera.Id)
                .OrderByDescending(t => t.Fecha)
                .Select(t => new TransaccionResponse
                {
                    Id = t.Id,
                    Tipo = t.Tipo,
                    Monto = t.Monto,
                    SaldoResultante = t.SaldoResultante,
                    Referencia = t.Referencia,
                    Fecha = t.Fecha
                })
                .ToListAsync();
        }
    }
}
