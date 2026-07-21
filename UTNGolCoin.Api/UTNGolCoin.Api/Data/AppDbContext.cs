using Microsoft.EntityFrameworkCore;
using UTNGolCoin.Api.Models;

namespace UTNGolCoin.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Billetera> Billeteras => Set<Billetera>();
        public DbSet<Transaccion> Transacciones => Set<Transaccion>();
        public DbSet<Prediccion> Predicciones => Set<Prediccion>();
        public DbSet<BonoDiario> BonosDiarios => Set<BonoDiario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Billetera>()
                .Property(b => b.Saldo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SaldoResultante)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Prediccion>()
                .Property(p => p.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Prediccion>()
                .Property(p => p.Cuota)
                .HasPrecision(9, 2);

            modelBuilder.Entity<BonoDiario>()
                .HasIndex(b => new { b.UsuarioId, b.Fecha })
                .IsUnique();
        }
    }
}
