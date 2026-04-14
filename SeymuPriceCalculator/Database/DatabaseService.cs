using Microsoft.Data.Sqlite;
using SeymuPriceCalculator.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SeymuPriceCalculator.Database
{
    public static class DatabaseService
    {
        private static string dbPath = "database.db";

        public static SqliteConnection GetConnection()
            => new SqliteConnection($"Data Source={dbPath}");

        // ═══════════════════════════════════════════════════════
        // INICIALIZAR
        // ═══════════════════════════════════════════════════════
        public static void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Clientes (
                    Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                    Uuid     TEXT,
                    Nombre   TEXT NOT NULL DEFAULT '',
                    Telefono TEXT NOT NULL DEFAULT ''
                );
                CREATE TABLE IF NOT EXISTS Cotizaciones (
                    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    Uuid          TEXT,
                    Fecha         TEXT,
                    Cliente       TEXT,
                    Subtotal      REAL,
                    IVA           REAL,
                    Total         REAL,
                    SyncPendiente INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS Piezas (
                    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                    Uuid           TEXT,
                    CotizacionId   INTEGER,
                    CotizacionUuid TEXT,
                    TipoMadera     TEXT,
                    Anchos         TEXT,
                    Largo          REAL,
                    TotalAncho     REAL,
                    Grosor         REAL,
                    Pulgadas       REAL,
                    Precio         REAL,
                    Total          REAL,
                    SyncPendiente  INTEGER DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS Empresa (
                    Id        INTEGER PRIMARY KEY,
                    Nombre    TEXT NOT NULL DEFAULT '',
                    Ubicacion TEXT NOT NULL DEFAULT '',
                    Telefono  TEXT NOT NULL DEFAULT '',
                    Correo    TEXT NOT NULL DEFAULT '',
                    LogoPath  TEXT NOT NULL DEFAULT ''
                );
                INSERT OR IGNORE INTO Empresa (Id,Nombre,Ubicacion,Telefono,Correo,LogoPath)
                SELECT 1,'Mi Empresa','','','',''
                WHERE NOT EXISTS (SELECT 1 FROM Empresa WHERE Id=1);

                CREATE TABLE IF NOT EXISTS TiposMadera (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Grosores (
                    Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                    Valor REAL NOT NULL UNIQUE
                );
                INSERT OR IGNORE INTO TiposMadera (Nombre) VALUES
                    ('Cedro'),('Guanacaste'),('Melina'),('Teca'),
                    ('Caobilla'),('Cenizaro'),('Laurel'),('Pino');
                INSERT OR IGNORE INTO Grosores (Valor) VALUES
                    (0.5),(1),(1.5),(1.75),(1.125),(2),(3);
            ";
            cmd.ExecuteNonQuery();

            // Migración para sync: agrega columnas si no existen
            MigrarParaSync(conn);
            LimpiarClientesDuplicados(conn);
        }

        // ── Agrega columnas UUID a tablas existentes ──────────
        private static void MigrarParaSync(SqliteConnection conn)
        {
            // Intentar agregar columnas — SQLite no tiene IF NOT EXISTS en ALTER TABLE
            var migraciones = new[]
            {
                "ALTER TABLE Cotizaciones ADD COLUMN Uuid TEXT",
                "ALTER TABLE Cotizaciones ADD COLUMN SyncPendiente INTEGER DEFAULT 1",
                "ALTER TABLE Piezas ADD COLUMN Uuid TEXT",
                "ALTER TABLE Piezas ADD COLUMN CotizacionUuid TEXT",
                "ALTER TABLE Piezas ADD COLUMN SyncPendiente INTEGER DEFAULT 1",
                "ALTER TABLE Clientes ADD COLUMN Uuid TEXT",
                "ALTER TABLE Empresa ADD COLUMN Impresora TEXT NOT NULL DEFAULT ''",
            };

            foreach (var sql in migraciones)
            {
                try
                {
                    using var cmd = new SqliteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
                catch { /* columna ya existe — ignorar */ }
            }

            // Generar UUIDs para registros que no tienen uno
            GenerarUuidsPendientes(conn, "Cotizaciones");
            GenerarUuidsPendientes(conn, "Piezas");
            GenerarUuidsPendientes(conn, "Clientes");

            // Vincular CotizacionUuid en Piezas existentes
            using var linkCmd = new SqliteCommand(@"
                UPDATE Piezas
                SET CotizacionUuid = (
                    SELECT Uuid FROM Cotizaciones
                    WHERE Cotizaciones.Id = Piezas.CotizacionId
                )
                WHERE CotizacionUuid IS NULL AND CotizacionId IS NOT NULL;
            ", conn);
            linkCmd.ExecuteNonQuery();
        }

        private static void GenerarUuidsPendientes(SqliteConnection conn, string tabla)
        {
            var ids = new List<long>();
            using (var q = new SqliteCommand($"SELECT Id FROM {tabla} WHERE Uuid IS NULL;", conn))
            using (var r = q.ExecuteReader())
                while (r.Read()) ids.Add(r.GetInt64(0));

            foreach (var id in ids)
            {
                using var u = new SqliteCommand(
                    $"UPDATE {tabla} SET Uuid = @uuid WHERE Id = @id;", conn);
                u.Parameters.AddWithValue("@uuid", Guid.NewGuid().ToString());
                u.Parameters.AddWithValue("@id", id);
                u.ExecuteNonQuery();
            }
        }

        // ═══════════════════════════════════════════════════════
        // EMPRESA
        // ═══════════════════════════════════════════════════════
        public static Empresa ObtenerEmpresa()
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Nombre,Ubicacion,Telefono,Correo,LogoPath,Impresora FROM Empresa WHERE Id=1;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
                return new Empresa
                {
                    Id = r.GetInt32(0),
                    Nombre = r.IsDBNull(1) ? "" : r.GetString(1),
                    Ubicacion = r.IsDBNull(2) ? "" : r.GetString(2),
                    Telefono = r.IsDBNull(3) ? "" : r.GetString(3),
                    Correo = r.IsDBNull(4) ? "" : r.GetString(4),
                    LogoPath = r.IsDBNull(5) ? "" : r.GetString(5),
                    Impresora = r.IsDBNull(6) ? "" : r.GetString(6),
                };
            return new Empresa();
        }

        public static void GuardarEmpresa(Empresa e)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Empresa SET
                Nombre=@n,Ubicacion=@u,Telefono=@t,Correo=@c,LogoPath=@l,Impresora=@i WHERE Id=1;";
            cmd.Parameters.AddWithValue("@n", e.Nombre);
            cmd.Parameters.AddWithValue("@u", e.Ubicacion);
            cmd.Parameters.AddWithValue("@t", e.Telefono);
            cmd.Parameters.AddWithValue("@c", e.Correo);
            cmd.Parameters.AddWithValue("@l", e.LogoPath);
            cmd.Parameters.AddWithValue("@i", e.Impresora);
            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════
        // CLIENTES
        // ═══════════════════════════════════════════════════════
        public static List<ClienteDB> GetClientes()
        {
            var lista = new List<ClienteDB>();
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, COALESCE(Uuid,''), Nombre, Telefono
                FROM Clientes ORDER BY Nombre ASC;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new ClienteDB
                {
                    Id = r.GetInt32(0),
                    Uuid = r.GetString(1),
                    Nombre = r.IsDBNull(2) ? "" : r.GetString(2),
                    Telefono = r.IsDBNull(3) ? "" : r.GetString(3),
                });
            return lista;
        }

        public static void AgregarCliente(ClienteDB c)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Clientes (Uuid, Nombre, Telefono)
                VALUES (@uuid, @n, @t);";
            cmd.Parameters.AddWithValue("@uuid", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@n", c.Nombre);
            cmd.Parameters.AddWithValue("@t", c.Telefono);
            cmd.ExecuteNonQuery();
        }

        public static void ActualizarCliente(ClienteDB c)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Clientes SET Nombre=@n,Telefono=@t WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@n", c.Nombre);
            cmd.Parameters.AddWithValue("@t", c.Telefono);
            cmd.Parameters.AddWithValue("@id", c.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarCliente(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Clientes WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════
        // TIPOS DE MADERA
        // ═══════════════════════════════════════════════════════
        public static List<string> GetTiposMadera()
        {
            var lista = new List<string>();
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Nombre FROM TiposMadera ORDER BY Nombre ASC;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) lista.Add(r.GetString(0));
            return lista;
        }

        public static void AgregarTipoMadera(string nombre)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO TiposMadera (Nombre) VALUES (@n);";
            cmd.Parameters.AddWithValue("@n", nombre);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarTipoMadera(string nombre)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM TiposMadera WHERE Nombre=@n;";
            cmd.Parameters.AddWithValue("@n", nombre);
            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════
        // GROSORES
        // ═══════════════════════════════════════════════════════
        public static List<double> GetGrosores()
        {
            var lista = new List<double>();
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Valor FROM Grosores ORDER BY Valor ASC;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) lista.Add(r.GetDouble(0));
            return lista;
        }

        public static void AgregarGrosor(double valor)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Grosores (Valor) VALUES (@v);";
            cmd.Parameters.AddWithValue("@v", valor);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarGrosor(double valor)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Grosores WHERE Valor=@v;";
            cmd.Parameters.AddWithValue("@v", valor);
            cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════════════════════
        // MÉTODOS PARA SYNC — usados por NeonService
        // ═══════════════════════════════════════════════════════

        /// <summary>Devuelve todos los UUIDs de una tabla local.</summary>
        public static HashSet<string> GetUuids(string tabla)
        {
            var set = new HashSet<string>();
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Uuid FROM {tabla} WHERE Uuid IS NOT NULL;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) set.Add(r.GetString(0));
            return set;
        }

        /// <summary>Cotizaciones aún no sincronizadas con Neon.</summary>
        public static List<CotizacionSync> GetCotizacionesPendientes()
        {
            var lista = new List<CotizacionSync>();
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Uuid, Fecha, Cliente, Subtotal, IVA, Total
                FROM Cotizaciones
                WHERE SyncPendiente = 1 AND Uuid IS NOT NULL;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new CotizacionSync
                {
                    Uuid = r.GetString(0),
                    Fecha = r.IsDBNull(1) ? "" : r.GetString(1),
                    Cliente = r.IsDBNull(2) ? "" : r.GetString(2),
                    Subtotal = r.IsDBNull(3) ? 0 : r.GetDouble(3),
                    IVA = r.IsDBNull(4) ? 0 : r.GetDouble(4),
                    Total = r.IsDBNull(5) ? 0 : r.GetDouble(5),
                });
            return lista;
        }

        /// <summary>Piezas aún no sincronizadas con Neon.</summary>
        public static List<PiezaSync> GetPiezasPendientes()
        {
            var lista = new List<PiezaSync>();
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Uuid, COALESCE(CotizacionUuid,''), TipoMadera, Anchos,
                       Largo, TotalAncho, Grosor, Pulgadas, Precio, Total
                FROM Piezas
                WHERE SyncPendiente = 1 AND Uuid IS NOT NULL
                  AND CotizacionUuid IS NOT NULL;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new PiezaSync
                {
                    Uuid = r.GetString(0),
                    CotizacionUuid = r.GetString(1),
                    TipoMadera = r.IsDBNull(2) ? "" : r.GetString(2),
                    Anchos = r.IsDBNull(3) ? "" : r.GetString(3),
                    Largo = r.IsDBNull(4) ? 0 : r.GetDouble(4),
                    TotalAncho = r.IsDBNull(5) ? 0 : r.GetDouble(5),
                    Grosor = r.IsDBNull(6) ? 0 : r.GetDouble(6),
                    Pulgadas = r.IsDBNull(7) ? 0 : r.GetDouble(7),
                    Precio = r.IsDBNull(8) ? 0 : r.GetDouble(8),
                    Total = r.IsDBNull(9) ? 0 : r.GetDouble(9),
                });
            return lista;
        }

        public static void MarcarCotizacionSincronizada(string uuid)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Cotizaciones SET SyncPendiente=0 WHERE Uuid=@uuid;";
            cmd.Parameters.AddWithValue("@uuid", uuid);
            cmd.ExecuteNonQuery();
        }

        public static void MarcarPiezaSincronizada(string uuid)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Piezas SET SyncPendiente=0 WHERE Uuid=@uuid;";
            cmd.Parameters.AddWithValue("@uuid", uuid);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Inserta cotización descargada de Neon. Retorna el ID local generado.</summary>
        public static long InsertarCotizacionDescargada(CotizacionSync c)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Cotizaciones (Uuid, Fecha, Cliente, Subtotal, IVA, Total, SyncPendiente)
                VALUES (@uuid,@fecha,@cliente,@subtotal,@iva,@total, 0);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@uuid", c.Uuid);
            cmd.Parameters.AddWithValue("@fecha", c.Fecha);
            cmd.Parameters.AddWithValue("@cliente", c.Cliente);
            cmd.Parameters.AddWithValue("@subtotal", c.Subtotal);
            cmd.Parameters.AddWithValue("@iva", c.IVA);
            cmd.Parameters.AddWithValue("@total", c.Total);
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        /// <summary>Inserta pieza descargada de Neon usando el ID local de la cotización.</summary>
        public static void InsertarPiezaDescargada(PiezaSync p, long cotizacionLocalId)
        {
            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Piezas
                (Uuid, CotizacionId, CotizacionUuid, TipoMadera, Anchos,
                 Largo, TotalAncho, Grosor, Pulgadas, Precio, Total, SyncPendiente)
                VALUES
                (@uuid,@cid,@cuuid,@tipo,@anchos,@largo,@tancho,@grosor,@pulg,@precio,@total,0);";
            cmd.Parameters.AddWithValue("@uuid", p.Uuid);
            cmd.Parameters.AddWithValue("@cid", cotizacionLocalId);
            cmd.Parameters.AddWithValue("@cuuid", p.CotizacionUuid);
            cmd.Parameters.AddWithValue("@tipo", p.TipoMadera);
            cmd.Parameters.AddWithValue("@anchos", p.Anchos);
            cmd.Parameters.AddWithValue("@largo", p.Largo);
            cmd.Parameters.AddWithValue("@tancho", p.TotalAncho);
            cmd.Parameters.AddWithValue("@grosor", p.Grosor);
            cmd.Parameters.AddWithValue("@pulg", p.Pulgadas);
            cmd.Parameters.AddWithValue("@precio", p.Precio);
            cmd.Parameters.AddWithValue("@total", p.Total);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Inserta cliente descargado de Neon si no existe localmente.</summary>
        public static void InsertarClienteDescargado(ClienteDB c)
        {
            using var conn = GetConnection();
            conn.Open();

            // 1. Si ya existe ese UUID localmente, solo actualiza el nombre/teléfono
            var checkUuid = new SqliteCommand(
                "SELECT Id FROM Clientes WHERE Uuid = @uuid;", conn);
            checkUuid.Parameters.AddWithValue("@uuid", c.Uuid);
            var existeUuid = checkUuid.ExecuteScalar();

            if (existeUuid != null)
            {
                // Ya existe, actualizar por si cambió nombre o teléfono
                var upd = new SqliteCommand(
                    "UPDATE Clientes SET Nombre=@n, Telefono=@t WHERE Uuid=@uuid;", conn);
                upd.Parameters.AddWithValue("@n", c.Nombre);
                upd.Parameters.AddWithValue("@t", c.Telefono);
                upd.Parameters.AddWithValue("@uuid", c.Uuid);
                upd.ExecuteNonQuery();
                return;
            }

            // 2. Si ya existe mismo nombre+teléfono (cliente local sin UUID de Neon),
            //    solo asignarle el UUID de Neon — no insertar duplicado
            var checkNombre = new SqliteCommand(@"
        SELECT Id FROM Clientes
        WHERE LOWER(TRIM(Nombre))   = LOWER(TRIM(@n))
          AND LOWER(TRIM(Telefono)) = LOWER(TRIM(@t))
        LIMIT 1;", conn);
            checkNombre.Parameters.AddWithValue("@n", c.Nombre);
            checkNombre.Parameters.AddWithValue("@t", c.Telefono);
            var existeNombre = checkNombre.ExecuteScalar();

            if (existeNombre != null)
            {
                // Asignar el UUID de Neon al registro local existente
                var upd = new SqliteCommand(
                    "UPDATE Clientes SET Uuid=@uuid WHERE Id=@id;", conn);
                upd.Parameters.AddWithValue("@uuid", c.Uuid);
                upd.Parameters.AddWithValue("@id", existeNombre);
                upd.ExecuteNonQuery();
                return;
            }

            // 3. Es un cliente genuinamente nuevo — insertar
            var ins = new SqliteCommand(
                "INSERT INTO Clientes (Uuid, Nombre, Telefono) VALUES (@uuid, @n, @t);", conn);
            ins.Parameters.AddWithValue("@uuid", c.Uuid);
            ins.Parameters.AddWithValue("@n", c.Nombre);
            ins.Parameters.AddWithValue("@t", c.Telefono);
            ins.ExecuteNonQuery();
        }

        private static void LimpiarClientesDuplicados(SqliteConnection conn)
        {
            // Elimina duplicados por nombre+teléfono, conservando el registro con UUID
            var cmd = new SqliteCommand(@"
        DELETE FROM Clientes
        WHERE Id NOT IN (
            SELECT MIN(CASE WHEN Uuid IS NOT NULL AND Uuid != '' THEN Id ELSE 99999999 END)
            FROM Clientes
            GROUP BY LOWER(TRIM(Nombre)), LOWER(TRIM(Telefono))
        )
        AND Id IN (
            SELECT Id FROM Clientes
            WHERE (LOWER(TRIM(Nombre)), LOWER(TRIM(Telefono))) IN (
                SELECT LOWER(TRIM(Nombre)), LOWER(TRIM(Telefono))
                FROM Clientes
                GROUP BY LOWER(TRIM(Nombre)), LOWER(TRIM(Telefono))
                HAVING COUNT(*) > 1
            )
        );
    ", conn);

            try { cmd.ExecuteNonQuery(); }
            catch { }
        }
    }
}