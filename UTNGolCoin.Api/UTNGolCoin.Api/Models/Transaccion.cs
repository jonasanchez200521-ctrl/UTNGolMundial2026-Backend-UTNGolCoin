namespace UTNGolCoin.Api.Models
{
    // Ledger: solo se inserta, nunca se edita ni se borra.
    public class Transaccion
    {
        public int Id { get; set; }

        public int BilleteraId { get; set; }

        // BIENVENIDA, PREDICCION, PREMIO, BONO_DIARIO
        public string Tipo { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public decimal SaldoResultante { get; set; }

        // Ej. id de predicción o de partido
        public string? Referencia { get; set; }

        public DateTime Fecha { get; set; }
    }
}
