using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeymuPriceCalculator.Models
{
    public class Pieza
    {
        public double LargoEnVaras { get; set; }
        public double AnchoEnPulgadas { get; set; }
        public double GrosorEnPulgadas { get; set; }

        public double TotalPulgadas
        {
            get
            {
                return LargoEnVaras * 36 * AnchoEnPulgadas * GrosorEnPulgadas;
            }
        }
    }
}