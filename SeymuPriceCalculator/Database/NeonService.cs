using Npgsql;
using SeymuPriceCalculator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SeymuPriceCalculator.Database   // ← Database, no Services
{
    public static class NeonService
    {
        private static string _cs = "";

        public static void CargarConfiguracion()
        {
            try
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                _cs = doc.RootElement
                         .GetProperty("NeonConnectionString")
                         .GetString() ?? "";
            }
            catch { }
        }

        public static bool EstaConfigurado => !string.IsNullOrWhiteSpace(_cs);

        public static async Task<bool> TestConexionAsync()
        {
            if (!EstaConfigurado) return false;
            try
            {
                await using var conn = new NpgsqlConnection(_cs);
                await conn.OpenAsync();
                return true;
            }
            catch { return false; }
        }

        public static async Task InicializarTablasAsync()
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            string sql = @"
                CREATE TABLE IF NOT EXISTS cotizaciones (
                    uuid           TEXT PRIMARY KEY,
                    fecha          TEXT,
                    cliente        TEXT,
                    subtotal       DOUBLE PRECISION DEFAULT 0,
                    iva            DOUBLE PRECISION DEFAULT 0,
                    total          DOUBLE PRECISION DEFAULT 0,
                    fecha_creacion TIMESTAMP DEFAULT NOW()
                );
                CREATE TABLE IF NOT EXISTS piezas (
                    uuid            TEXT PRIMARY KEY,
                    cotizacion_uuid TEXT,
                    tipo_madera     TEXT,
                    anchos          TEXT,
                    largo           DOUBLE PRECISION DEFAULT 0,
                    total_ancho     DOUBLE PRECISION DEFAULT 0,
                    grosor          DOUBLE PRECISION DEFAULT 0,
                    pulgadas        DOUBLE PRECISION DEFAULT 0,
                    precio          DOUBLE PRECISION DEFAULT 0,
                    total           DOUBLE PRECISION DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS clientes (
                    uuid               TEXT PRIMARY KEY,
                    nombre             TEXT DEFAULT '',
                    telefono           TEXT DEFAULT '',
                    fecha_modificacion TIMESTAMP DEFAULT NOW()
                );
                CREATE TABLE IF NOT EXISTS tipos_madera (
                    nombre TEXT PRIMARY KEY
                );
                CREATE TABLE IF NOT EXISTS grosores (
                    valor DOUBLE PRECISION PRIMARY KEY
                );
                CREATE TABLE IF NOT EXISTS empresa (
                    id                 INTEGER PRIMARY KEY,
                    nombre             TEXT DEFAULT '',
                    ubicacion          TEXT DEFAULT '',
                    telefono           TEXT DEFAULT '',
                    correo             TEXT DEFAULT '',
                    fecha_modificacion TIMESTAMP DEFAULT NOW()
                );
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════
        // PUSH: SQLite → Neon
        // ═══════════════════════════════════════════════════════
        public static async Task<int> SubirCotizacionesAsync()
        {
            var pendientes = DatabaseService.GetCotizacionesPendientes();
            if (pendientes.Count == 0) return 0;

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            int count = 0;
            foreach (var c in pendientes)
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO cotizaciones (uuid,fecha,cliente,subtotal,iva,total)
                    VALUES (@uuid,@fecha,@cliente,@subtotal,@iva,@total)
                    ON CONFLICT (uuid) DO NOTHING;", conn);

                cmd.Parameters.AddWithValue("@uuid", c.Uuid);
                cmd.Parameters.AddWithValue("@fecha", c.Fecha);
                cmd.Parameters.AddWithValue("@cliente", c.Cliente);
                cmd.Parameters.AddWithValue("@subtotal", c.Subtotal);
                cmd.Parameters.AddWithValue("@iva", c.IVA);
                cmd.Parameters.AddWithValue("@total", c.Total);
                await cmd.ExecuteNonQueryAsync();

                DatabaseService.MarcarCotizacionSincronizada(c.Uuid);
                count++;
            }
            return count;
        }

        public static async Task<int> SubirPiezasAsync()
        {
            var pendientes = DatabaseService.GetPiezasPendientes();
            if (pendientes.Count == 0) return 0;

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            int count = 0;
            foreach (var p in pendientes)
            {
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO piezas
                    (uuid,cotizacion_uuid,tipo_madera,anchos,largo,total_ancho,grosor,pulgadas,precio,total)
                    VALUES (@uuid,@cuuid,@tipo,@anchos,@largo,@tancho,@grosor,@pulg,@precio,@total)
                    ON CONFLICT (uuid) DO NOTHING;", conn);

                cmd.Parameters.AddWithValue("@uuid", p.Uuid);
                cmd.Parameters.AddWithValue("@cuuid", p.CotizacionUuid);
                cmd.Parameters.AddWithValue("@tipo", p.TipoMadera);
                cmd.Parameters.AddWithValue("@anchos", p.Anchos);
                cmd.Parameters.AddWithValue("@largo", p.Largo);
                cmd.Parameters.AddWithValue("@tancho", p.TotalAncho);
                cmd.Parameters.AddWithValue("@grosor", p.Grosor);
                cmd.Parameters.AddWithValue("@pulg", p.Pulgadas);
                cmd.Parameters.AddWithValue("@precio", p.Precio);
                cmd.Parameters.AddWithValue("@total", p.Total);
                await cmd.ExecuteNonQueryAsync();

                DatabaseService.MarcarPiezaSincronizada(p.Uuid);
                count++;
            }
            return count;
        }

        public static async Task SubirClientesAsync()
        {
            var clientes = DatabaseService.GetClientes();

            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            foreach (var c in clientes)
            {
                if (string.IsNullOrWhiteSpace(c.Uuid)) continue;

                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO clientes (uuid,nombre,telefono)
                    VALUES (@uuid,@nombre,@telefono)
                    ON CONFLICT (uuid) DO UPDATE SET
                        nombre             = EXCLUDED.nombre,
                        telefono           = EXCLUDED.telefono,
                        fecha_modificacion = NOW();", conn);

                cmd.Parameters.AddWithValue("@uuid", c.Uuid);
                cmd.Parameters.AddWithValue("@nombre", c.Nombre);
                cmd.Parameters.AddWithValue("@telefono", c.Telefono);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task SubirCatalogosAsync()
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            foreach (var m in DatabaseService.GetTiposMadera())
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO tipos_madera (nombre) VALUES (@n) ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("@n", m);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var g in DatabaseService.GetGrosores())
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO grosores (valor) VALUES (@v) ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("@v", g);
                await cmd.ExecuteNonQueryAsync();
            }

            var emp = DatabaseService.ObtenerEmpresa();
            await using var empCmd = new NpgsqlCommand(@"
                INSERT INTO empresa (id,nombre,ubicacion,telefono,correo)
                VALUES (1,@n,@u,@t,@c)
                ON CONFLICT (id) DO UPDATE SET
                    nombre             = EXCLUDED.nombre,
                    ubicacion          = EXCLUDED.ubicacion,
                    telefono           = EXCLUDED.telefono,
                    correo             = EXCLUDED.correo,
                    fecha_modificacion = NOW();", conn);
            empCmd.Parameters.AddWithValue("@n", emp.Nombre);
            empCmd.Parameters.AddWithValue("@u", emp.Ubicacion);
            empCmd.Parameters.AddWithValue("@t", emp.Telefono);
            empCmd.Parameters.AddWithValue("@c", emp.Correo);
            await empCmd.ExecuteNonQueryAsync();
        }

        // ═══════════════════════════════════════════════════════
        // PULL: Neon → SQLite
        // ═══════════════════════════════════════════════════════
        public static async Task<int> DescargarCotizacionesAsync()
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            var neonUuids = new List<string>();
            await using (var cmd = new NpgsqlCommand(
                "SELECT uuid FROM cotizaciones;", conn))
            await using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    neonUuids.Add(r.GetString(0));

            var localUuids = DatabaseService.GetUuids("Cotizaciones");

            int count = 0;
            foreach (var uuid in neonUuids)
            {
                if (localUuids.Contains(uuid)) continue;

                CotizacionSync? cot = null;
                await using (var cmd = new NpgsqlCommand(@"
                    SELECT uuid,fecha,cliente,subtotal,iva,total
                    FROM cotizaciones WHERE uuid=@uuid;", conn))
                {
                    cmd.Parameters.AddWithValue("@uuid", uuid);
                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                        cot = new CotizacionSync
                        {
                            Uuid = r.GetString(0),
                            Fecha = r.IsDBNull(1) ? "" : r.GetString(1),
                            Cliente = r.IsDBNull(2) ? "" : r.GetString(2),
                            Subtotal = r.IsDBNull(3) ? 0 : r.GetDouble(3),
                            IVA = r.IsDBNull(4) ? 0 : r.GetDouble(4),
                            Total = r.IsDBNull(5) ? 0 : r.GetDouble(5),
                        };
                }

                if (cot == null) continue;

                long localId = DatabaseService.InsertarCotizacionDescargada(cot);
                var piezasUuids = DatabaseService.GetUuids("Piezas");

                await using (var cmd = new NpgsqlCommand(@"
                    SELECT uuid,cotizacion_uuid,tipo_madera,anchos,
                           largo,total_ancho,grosor,pulgadas,precio,total
                    FROM piezas WHERE cotizacion_uuid=@uuid;", conn))
                {
                    cmd.Parameters.AddWithValue("@uuid", uuid);
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        string pUuid = r.GetString(0);
                        if (piezasUuids.Contains(pUuid)) continue;

                        DatabaseService.InsertarPiezaDescargada(new PiezaSync
                        {
                            Uuid = pUuid,
                            CotizacionUuid = r.GetString(1),
                            TipoMadera = r.IsDBNull(2) ? "" : r.GetString(2),
                            Anchos = r.IsDBNull(3) ? "" : r.GetString(3),
                            Largo = r.IsDBNull(4) ? 0 : r.GetDouble(4),
                            TotalAncho = r.IsDBNull(5) ? 0 : r.GetDouble(5),
                            Grosor = r.IsDBNull(6) ? 0 : r.GetDouble(6),
                            Pulgadas = r.IsDBNull(7) ? 0 : r.GetDouble(7),
                            Precio = r.IsDBNull(8) ? 0 : r.GetDouble(8),
                            Total = r.IsDBNull(9) ? 0 : r.GetDouble(9),
                        }, localId);
                    }
                }

                count++;
            }
            return count;
        }

        public static async Task DescargarClientesAsync()
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT uuid,nombre,telefono FROM clientes;", conn);
            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                DatabaseService.InsertarClienteDescargado(new ClienteDB
                {
                    Uuid = r.GetString(0),
                    Nombre = r.IsDBNull(1) ? "" : r.GetString(1),
                    Telefono = r.IsDBNull(2) ? "" : r.GetString(2),
                });
            }
        }
    }
}