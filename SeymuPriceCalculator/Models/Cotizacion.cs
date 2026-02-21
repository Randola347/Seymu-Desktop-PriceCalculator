using System;
using System.Collections.Generic;
using System.Linq;

namespace SeymuPriceCalculator.Models
{
    public class Cotizacion
    {
        public string TipoMadera { get; set; }
        public string NombreCliente { get; set; }
        public string TelefonoCliente { get; set; }

        public List<Pieza> Piezas { get; set; } = new List<Pieza>();

        public double PrecioPorPulgada { get; set; }

        public double TotalVaras
        {
            get
            {
                return Piezas.Sum(p => p.LargoEnVaras);
            }
        }

        public double TotalPulgadas
        {
            get
            {
                return Piezas.Sum(p => p.TotalPulgadas);
            }
        }

        public double Subtotal
        {
            get
            {
                return TotalPulgadas * PrecioPorPulgada;
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