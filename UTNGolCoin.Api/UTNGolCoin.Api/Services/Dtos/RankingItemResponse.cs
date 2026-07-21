namespace UTNGolCoin.Api.Services.Dtos
{
    public class RankingItemResponse
    {
        public int UsuarioId { get; set; }
        public decimal Saldo { get; set; }
        public int Aciertos { get; set; }
        public int TotalPredicciones { get; set; }
    }
}
