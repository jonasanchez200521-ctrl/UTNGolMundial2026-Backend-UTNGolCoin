namespace UTNGolCoin.Api.Services.Dtos
{
    public class TransaccionResponse
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public decimal SaldoResultante { get; set; }
        public string? Referencia { get; set; }
        public DateTime Fecha { get; set; }
    }
}
