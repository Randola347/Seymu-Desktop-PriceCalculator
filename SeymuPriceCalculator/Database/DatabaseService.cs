using Microsoft.Data.Sqlite;
using SeymuPriceCalculator.Models;
using System.IO;

namespace SeymuPriceCalculator.Database
{
    public static class DatabaseService
    {
        private static string dbPath = "database.db";

        // ── Conexión ──────────────────────────────────────────
        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={dbPath}");
        }

        // ── Inicializar todas las tablas ──────────────────────
        public static void Initialize()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Clientes (
                    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre   TEXT,
                    Telefono TEXT
                );

                CREATE TABLE IF NOT EXISTS Cotizaciones (
                    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha    TEXT,
                    Cliente  TEXT,
                    Subtotal REAL,
                    IVA      REAL,
                    Total    REAL
                );

                CREATE TABLE IF NOT EXISTS Piezas (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    CotizacionId INTEGER,
                    TipoMadera   TEXT,
                    Anchos       TEXT,
                    Largo        REAL,
                    TotalAncho   REAL,
                    Grosor       REAL,
                    Pulgadas     REAL,
                    Precio       REAL,
                    Total        REAL
                );

                CREATE TABLE IF NOT EXISTS Empresa (
                    Id        INTEGER PRIMARY KEY,
                    Nombre    TEXT NOT NULL DEFAULT '',
                    Ubicacion TEXT NOT NULL DEFAULT '',
                    Telefono  TEXT NOT NULL DEFAULT '',
                    Correo    TEXT NOT NULL DEFAULT '',
                    LogoPath  TEXT NOT NULL DEFAULT ''
                );

                INSERT OR IGNORE INTO Empresa (Id, Nombre, Ubicacion, Telefono, Correo, LogoPath)
                SELECT 1, 'Mi Empresa', '', '', '', ''
                WHERE NOT EXISTS (SELECT 1 FROM Empresa WHERE Id = 1);
            ";

            command.ExecuteNonQuery();
        }

        // ── Empresa: Obtener ──────────────────────────────────
        public static Empresa ObtenerEmpresa()
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                SELECT Id, Nombre, Ubicacion, Telefono, Correo, LogoPath
                FROM Empresa
                WHERE Id = 1;
            ";

            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new Empresa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Ubicacion = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Telefono = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Correo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    LogoPath = reader.IsDBNull(5) ? "" : reader.GetString(5),
                };
            }

            return new Empresa();
        }

        // ── Empresa: Guardar ──────────────────────────────────
        public static void GuardarEmpresa(Empresa empresa)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                UPDATE Empresa SET
                    Nombre    = @nombre,
                    Ubicacion = @ubicacion,
                    Telefono  = @telefono,
                    Correo    = @correo,
                    LogoPath  = @logo
                WHERE Id = 1;
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nombre", empresa.Nombre);
            cmd.Parameters.AddWithValue("@ubicacion", empresa.Ubicacion);
            cmd.Parameters.AddWithValue("@telefono", empresa.Telefono);
            cmd.Parameters.AddWithValue("@correo", empresa.Correo);
            cmd.Parameters.AddWithValue("@logo", empresa.LogoPath);
            cmd.ExecuteNonQuery();
        }
    }
}