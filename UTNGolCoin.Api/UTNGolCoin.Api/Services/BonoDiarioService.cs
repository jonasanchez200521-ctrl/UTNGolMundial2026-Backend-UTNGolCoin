using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Data;
using UTNGolCoin.Api.Models;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Services
{
    public class BonoDiarioService
    {
        private const decimal MontoBonoDiario = 1m;
        private const string TipoTransaccionBonoDiario = "BONO_DIARIO";

        private readonly AppDbContext _context;

        public BonoDiarioService(AppDbContext context)
        {
            _context = context;
        }

        // Idempotente por fecha: la tabla BonosDiarios tiene un índice único (UsuarioId, Fecha),
        // así que un usuario no puede recibir dos bonos para la misma fecha aunque se ejecute
        // el proceso varias veces. Mandar una fecha distinta simula "otro día" para la demo.
        public async Task<EjecutarBonoDiarioResponse> EjecutarBonoDiarioAsync(DateOnly? fecha)
        {
            var fechaObjetivo = fecha ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var usuariosYaBeneficiados = await _context.BonosDiarios
                .Where(b => b.Fecha == fechaObjetivo)
                .Select(b => b.UsuarioId)
                .ToListAsync();

            var billeterasEnBancarrota = await _context.Billeteras
                .Where(b => b.Saldo <= 0 && !usuariosYaBeneficiados.Contains(b.UsuarioId))
                .ToListAsync();

            var beneficiarios = new List<BeneficiarioBonoResponse>();

            if (billeterasEnBancarrota.Count > 0)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                foreach (var billetera in billeterasEnBancarrota)
                {
                    billetera.Saldo += MontoBonoDiario;

                    _context.Transacciones.Add(new Transaccion
                    {
                        BilleteraId = billetera.Id,
                        Tipo = TipoTransaccionBonoDiario,
                        Monto = MontoBonoDiario,
                        SaldoResultante = billetera.Saldo,
                        Referencia = null,
                        Fecha = DateTime.UtcNow
                    });

                    _context.BonosDiarios.Add(new BonoDiario
                    {
                        UsuarioId = billetera.UsuarioId,
                        Fecha = fechaObjetivo
                    });

                    beneficiarios.Add(new BeneficiarioBonoResponse
                    {
                        UsuarioId = billetera.UsuarioId,
                        SaldoNuevo = billetera.Saldo
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return new EjecutarBonoDiarioResponse
            {
                Fecha = fechaObjetivo,
                CantidadBeneficiados = beneficiarios.Count,
                Beneficiarios = beneficiarios
            };
        }

        public async Task<EstadoBonoResponse> ObtenerEstadoAsync(int usuarioId)
        {
            var billetera = await _context.Billeteras.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);
            if (billetera is null)
            {
                throw new KeyNotFoundException($"El usuario {usuarioId} no tiene una billetera creada.");
            }

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var yaRecibioBonoHoy = await _context.BonosDiarios
                .AnyAsync(b => b.UsuarioId == usuarioId && b.Fecha == hoy);

            return new EstadoBonoResponse
            {
                UsuarioId = usuarioId,
                Saldo = billetera.Saldo,
                EnBancarrota = billetera.Saldo <= 0,
                YaRecibioBonoHoy = yaRecibioBonoHoy,
                Fecha = hoy
            };
        }
    }
}
