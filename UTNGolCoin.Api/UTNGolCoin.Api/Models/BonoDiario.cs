namespace UTNGolCoin.Api.Models
{
    // Controla que a un usuario no se le otorgue más de un bono diario el mismo día.
    public class BonoDiario
    {
        public int Id { get; set; }

        // Referencia lógica al usuario que vive en el backend de Estadísticas (Alexis). No es FK local.
        public int UsuarioId { get; set; }

        public DateOnly Fecha { get; set; }
    }
}
