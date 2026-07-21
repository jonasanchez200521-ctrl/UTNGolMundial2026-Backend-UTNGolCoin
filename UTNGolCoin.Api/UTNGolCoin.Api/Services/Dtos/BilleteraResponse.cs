namespace UTNGolCoin.Api.Services.Dtos
{
    public class BilleteraResponse
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public decimal Saldo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
