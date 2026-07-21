namespace UTNGolCoin.Api.Services.Dtos
{
    public class LiquidacionRequest
    {
        public int PartidoId { get; set; }

        // Resultado final del partido: LOCAL, EMPATE o VISITANTE.
        // Alexis puede mandar campos extra (ej. fase, grupo); se ignoran, no rompen la petición.
        public string Resultado { get; set; } = string.Empty;
    }
}
