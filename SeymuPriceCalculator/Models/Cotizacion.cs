using System;
using System.Collections.Generic;
using System.Linq;

namespace SeymuPriceCalculator.Models
{
    public class Cotizacion
    {
        public string TipoMadera { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;

        public List<Pieza> Piezas { get; set; } = new List<Pieza>();

        public double PrecioPorPulgada { get; set; }


        public double Subtotal
        {
            get
            {
                return Piezas.Sum(p => p.Total);
            }
        }

        public double IVA
        {
            get
            {
                return Subtotal * 0.13;
            }
        }

        public double TotalFinal
        {
            get
            {
                return Subtotal + IVA;
            }
        }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}