namespace UTNGolCoin.Api.Services.Dtos
{
    public class CrearPrediccionRequest
    {
        public int UsuarioId { get; set; }
        public int PartidoId { get; set; }

        // LOCAL, EMPATE o VISITANTE
        public string Pronostico { get; set; } = string.Empty;

        public decimal Monto { get; set; }
    }
}
