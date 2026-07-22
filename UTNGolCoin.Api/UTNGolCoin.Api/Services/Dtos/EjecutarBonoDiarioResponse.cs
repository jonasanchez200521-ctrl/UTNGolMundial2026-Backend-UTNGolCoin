namespace UTNGolCoin.Api.Services.Dtos
{
    public class EjecutarBonoDiarioResponse
    {
        public DateOnly Fecha { get; set; }
        public int CantidadBeneficiados { get; set; }
        public List<BeneficiarioBonoResponse> Beneficiarios { get; set; } = new();
    }
}
