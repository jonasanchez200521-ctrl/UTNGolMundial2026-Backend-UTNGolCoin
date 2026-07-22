namespace UTNGolCoin.Api.Services.Dtos
{
    public class EstadoBonoResponse
    {
        public int UsuarioId { get; set; }
        public decimal Saldo { get; set; }
        public bool EnBancarrota { get; set; }
        public bool YaRecibioBonoHoy { get; set; }
        public DateOnly Fecha { get; set; }
    }
}
