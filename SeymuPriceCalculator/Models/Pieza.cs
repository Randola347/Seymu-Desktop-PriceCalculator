using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeymuPriceCalculator.Models
{
    public class Pieza
    {
        public int Numero { get; set; }

        public string TipoMadera { get; set; } = string.Empty;
        public string Anchos { get; set; } = string.Empty;

        public double Largo { get; set; }
        public double TotalAncho { get; set; }
        public double Grosor { get; set; }
        public double Precio { get; set; }

        public double TotalBase => TotalAncho * Largo * Grosor;

        public double Total => TotalBase * Precio;
    }
}