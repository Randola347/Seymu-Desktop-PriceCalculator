using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;

namespace SeymuPriceCalculator.Database
{
    public static class DatabaseService
    {
        private static string dbPath = "database.db";

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={dbPath}");
        }

        public static void Initialize()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"

            CREATE TABLE IF NOT EXISTS Clientes(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT,
                Telefono TEXT
            );

            CREATE TABLE IF NOT EXISTS Cotizaciones(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Fecha TEXT,
                Cliente TEXT,
                Subtotal REAL,
                IVA REAL,
                Total REAL
            );

            CREATE TABLE IF NOT EXISTS Piezas(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CotizacionId INTEGER,
                TipoMadera TEXT,
                Anchos TEXT,
                Largo REAL,
                TotalAncho REAL,
                Grosor REAL,
                Pulgadas REAL,
                Precio REAL,
                Total REAL
            );

            ";

            command.ExecuteNonQuery();
        }
    }
}