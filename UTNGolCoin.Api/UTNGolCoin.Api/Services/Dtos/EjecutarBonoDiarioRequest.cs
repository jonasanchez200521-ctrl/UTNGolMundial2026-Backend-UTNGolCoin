namespace UTNGolCoin.Api.Services.Dtos
{
    public class EjecutarBonoDiarioRequest
    {
        // Opcional: si no se manda, se usa la fecha actual (UTC). Mandar una fecha
        // distinta permite simular "otro día" en la demo (ej. "2026-07-23").
        public DateOnly? Fecha { get; set; }
    }
}
