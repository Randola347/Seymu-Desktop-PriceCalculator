namespace SeymuPriceCalculator.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Ubicacion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Correo { get; set; } = "";
        public string LogoPath { get; set; } = "";
        public string Impresora { get; set; } = "";
    }
}