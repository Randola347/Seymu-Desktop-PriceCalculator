namespace SeymuPriceCalculator.Models
{
    public class SyncResult
    {
        public bool Exitoso { get; set; }
        public int Subidos { get; set; }
        public int Descargados { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class CotizacionSync
    {
        public string Uuid { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Cliente { get; set; } = "";
        public double Subtotal { get; set; }
        public double IVA { get; set; }
        public double Total { get; set; }
    }

    public class PiezaSync
    {
        public string Uuid { get; set; } = "";
        public string CotizacionUuid { get; set; } = "";
        public string TipoMadera { get; set; } = "";
        public string Anchos { get; set; } = "";
        public double Largo { get; set; }
        public double TotalAncho { get; set; }
        public double Grosor { get; set; }
        public double Pulgadas { get; set; }
        public double Precio { get; set; }
        public double Total { get; set; }
    }
}