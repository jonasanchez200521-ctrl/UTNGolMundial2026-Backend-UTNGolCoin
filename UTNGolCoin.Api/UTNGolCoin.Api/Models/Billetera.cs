namespace UTNGolCoin.Api.Models
{
    public class Billetera
    {
        public int Id { get; set; }

        // Referencia lógica al usuario que vive en el backend de Estadísticas (Alexis). No es FK local.
        public int UsuarioId { get; set; }

        public decimal Saldo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
