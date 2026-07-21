namespace UTNGolCoin.Api.Services.Dtos
{
    public class LiquidacionResponse
    {
        public int PartidoId { get; set; }
        public int Liquidadas { get; set; }
        public int Ganadas { get; set; }
        public int Perdidas { get; set; }
        public decimal TotalPagado { get; set; }
    }
}
