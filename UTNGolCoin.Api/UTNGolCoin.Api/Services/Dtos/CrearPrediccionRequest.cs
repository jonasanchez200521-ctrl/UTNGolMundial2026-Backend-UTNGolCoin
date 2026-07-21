namespace UTNGolCoin.Api.Services.Dtos
{
    public class CrearPrediccionRequest
    {
        public int UsuarioId { get; set; }
        public int PartidoId { get; set; }

        // Hora de inicio del partido, en UTC (formato ISO 8601 con "Z", ej. "2026-07-21T22:00:00Z").
        public DateTime FechaInicioPartido { get; set; }

        // LOCAL, EMPATE o VISITANTE
        public string Pronostico { get; set; } = string.Empty;

        public decimal Monto { get; set; }
    }
}
