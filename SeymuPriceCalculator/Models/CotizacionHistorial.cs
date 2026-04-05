namespace SeymuPriceCalculator.Models
{
    public class CotizacionHistorial
    {
        public int Id { get; set; }
        public string Fecha { get; set; } = "";
        public string Cliente { get; set; } = "";
        public double Subtotal { get; set; }
        public double IVA { get; set; }
        public double Total { get; set; }
    }
}