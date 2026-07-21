using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Models;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class BilleteraService
    {
        private const decimal MontoBonoBienvenida = 10m;
        private const string TipoTransaccionBienvenida = "BIENVENIDA";

        private readonly AppDbContext _context;

        public BilleteraService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BilleteraResponse> CrearBilleteraAsync(int usuarioId)
        {
            var yaExiste = await _context.Billeteras.AnyAsync(b => b.UsuarioId == usuarioId);
            if (yaExiste)
            {
                throw new InvalidOperationException($"El usuario {usuarioId} ya tiene una billetera.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var billetera = new Billetera
            {
                UsuarioId = usuarioId,
                Saldo = MontoBonoBienvenida,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Billeteras.Add(billetera);
            await _context.SaveChangesAsync();

            var transaccionBienvenida = new Transaccion
            {
                BilleteraId = billetera.Id,
                Tipo = TipoTransaccionBienvenida,
                Monto = MontoBonoBienvenida,
                SaldoResultante = billetera.Saldo,
                Referencia = null,
                Fecha = DateTime.UtcNow
            };
            _context.Transacciones.Add(transaccionBienvenida);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return MapearResponse(billetera);
        }

        public async Task<BilleteraResponse?> ObtenerPorUsuarioIdAsync(int usuarioId)
        {
            var billetera = await _context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);
            return billetera is null ? null : MapearResponse(billetera);
        }

        private static BilleteraResponse MapearResponse(Billetera billetera)
        {
            return new BilleteraResponse
            {
                Id = billetera.Id,
                UsuarioId = billetera.UsuarioId,
                Saldo = billetera.Saldo,
                FechaCreacion = billetera.FechaCreacion
            };
        }
    }
}
