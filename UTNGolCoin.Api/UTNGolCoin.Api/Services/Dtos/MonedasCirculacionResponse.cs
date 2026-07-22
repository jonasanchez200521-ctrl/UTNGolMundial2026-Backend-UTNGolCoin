namespace UTNGolCoin.Api.Services.Dtos
{
    public class MonedasCirculacionResponse
    {
        public decimal TotalMonedasEnCirculacion { get; set; }
        public int CantidadBilleteras { get; set; }
        public decimal TotalPagadoEnPremios { get; set; }
    }
}
