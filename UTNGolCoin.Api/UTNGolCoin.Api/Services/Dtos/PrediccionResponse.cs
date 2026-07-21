namespace UTNGolCoin.Api.Services.Dtos
{
    public class PrediccionResponse
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int PartidoId { get; set; }
        public string Pronostico { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public decimal Cuota { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
