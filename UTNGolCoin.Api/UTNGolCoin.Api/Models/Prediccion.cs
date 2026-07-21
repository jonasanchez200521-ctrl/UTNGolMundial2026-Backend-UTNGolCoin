namespace UTNGolCoin.Api.Models
{
    public class Prediccion
    {
        public int Id { get; set; }

        // Referencia lógica al usuario que vive en el backend de Estadísticas (Alexis). No es FK local.
        public int UsuarioId { get; set; }

        // Referencia lógica a un partido que vive en el backend de Estadísticas (Alexis). No es FK local.
        public int PartidoId { get; set; }

        // Hora de inicio del partido (en UTC) usada para validar el cierre de apuestas (RF17).
        public DateTime FechaInicioPartido { get; set; }

        // LOCAL, EMPATE, VISITANTE
        public string Pronostico { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public decimal Cuota { get; set; }

        // PENDIENTE, GANADA, PERDIDA
        public string Estado { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }
    }
}
