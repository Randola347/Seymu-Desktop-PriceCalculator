using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeymuPriceCalculator.Models
{
    public class Pieza
    {
        public string TipoMadera { get; set; } = string.Empty;

        public double Largo { get; set; }
        public double TotalAncho { get; set; }
        public double Grosor { get; set; }
        public double Precio { get; set; }

        public double TotalBase
        {
            get
            {
                return TotalAncho * Largo * Grosor;
            }
        }
        public double Total
        {
            get
            {
                return TotalAncho * Largo * Grosor * Precio;
            }
        }
    }
}