using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeymuPriceCalculator.Models
{
    public class ClienteDB
    {
        public int Id { get; set; }

        public string Uuid { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";

        // Para mostrar en ComboBox de MainWindow
        public override string ToString()
            => string.IsNullOrWhiteSpace(Telefono)
               ? Nombre
               : $"{Nombre}  —  {Telefono}";
    }
}