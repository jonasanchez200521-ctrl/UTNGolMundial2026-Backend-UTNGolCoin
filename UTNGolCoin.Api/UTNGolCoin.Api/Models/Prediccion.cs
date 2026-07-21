namespace UTNGolCoin.Api.Models
{
    public class Prediccion
    {
        public int Id { get; set; }

        // Referencia lógica al usuario que vive en el backend de Estadísticas (Alexis). No es FK local.
        public int UsuarioId { get; set; }

        // Referencia lógica a un partido que vive en el backend de Estadísticas (Alexis). No es FK local.
        public int PartidoId { get; set; }

        // LOCAL, EMPATE, VISITANTE
        public string Pronostico { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public decimal Cuota { get; set; }

        // PENDIENTE, GANADA, PERDIDA
        public string Estado { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }
    }
}
