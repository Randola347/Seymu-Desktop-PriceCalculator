using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;

namespace SeymuPriceCalculator.Services
{
    public static class CotizacionService
    {
        public static int GuardarCotizacion(string cliente, double subtotal, double iva, double total)
        {
            using var connection = DatabaseService.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
            INSERT INTO Cotizaciones (Fecha, Cliente, Subtotal, IVA, Total)
            VALUES ($fecha,$cliente,$subtotal,$iva,$total);

            SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            command.Parameters.AddWithValue("$cliente", cliente);
            command.Parameters.AddWithValue("$subtotal", subtotal);
            command.Parameters.AddWithValue("$iva", iva);
            command.Parameters.AddWithValue("$total", total);

            return Convert.ToInt32(command.ExecuteScalar());
        }
        public static void GuardarPieza(int cotizacionId, Pieza pieza)
        {
            using var connection = DatabaseService.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
    INSERT INTO Piezas
    (CotizacionId,TipoMadera,Anchos,Largo,TotalAncho,Grosor,Pulgadas,Precio,Total)
    VALUES
    ($cotizacionId,$tipo,$anchos,$largo,$totalAncho,$grosor,$pulgadas,$precio,$total)
    ";

            command.Parameters.AddWithValue("$cotizacionId", cotizacionId);
            command.Parameters.AddWithValue("$tipo", pieza.TipoMadera);
            command.Parameters.AddWithValue("$anchos", pieza.Anchos);
            command.Parameters.AddWithValue("$largo", pieza.Largo);
            command.Parameters.AddWithValue("$totalAncho", pieza.TotalAncho);
            command.Parameters.AddWithValue("$grosor", pieza.Grosor);
            command.Parameters.AddWithValue("$pulgadas", pieza.TotalBase);
            command.Parameters.AddWithValue("$precio", pieza.Precio);
            command.Parameters.AddWithValue("$total", pieza.Total);

            command.ExecuteNonQuery();
        }
    }

}