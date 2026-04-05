using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SeymuPriceCalculator.Views
{
    public partial class HistorialView : UserControl
    {
        private List<CotizacionHistorial> _todas = new();
        private CotizacionHistorial? _seleccionada = null;

        public HistorialView()
        {
            InitializeComponent();
            CargarHistorial();
        }

        // ── Cargar todas las cotizaciones ─────────────────────
        private void CargarHistorial()
        {
            _todas.Clear();

            using var conn = DatabaseService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Fecha, Cliente, Subtotal, IVA, Total
                FROM Cotizaciones
                ORDER BY Id DESC;
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _todas.Add(new CotizacionHistorial
                {
                    Id = reader.GetInt32(0),
                    Fecha = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Cliente = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Subtotal = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                    IVA = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                    Total = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                });
            }

            AplicarFiltros();
        }

        // ── Filtrar por cliente y/o fecha ─────────────────────
        private void AplicarFiltros()
        {
            string busqueda = txtBuscar?.Text?.Trim().ToLower() ?? "";
            DateTime? fecha = dpFecha?.SelectedDate;

            var resultado = _todas.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busqueda))
                resultado = resultado.Where(c =>
                    c.Cliente.ToLower().Contains(busqueda));

            if (fecha.HasValue)
                resultado = resultado.Where(c =>
                    c.Fecha.StartsWith(
                        fecha.Value.ToString("yyyy-MM-dd"),
                        StringComparison.OrdinalIgnoreCase));

            dgHistorial.ItemsSource = resultado.ToList();
            panelDetalle.Visibility = Visibility.Collapsed;
            _seleccionada = null;
        }

        // ── Cargar piezas de una cotización ───────────────────
        private List<PiezaDetalle> CargarPiezas(int cotizacionId)
        {
            var piezas = new List<PiezaDetalle>();

            using var conn = DatabaseService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TipoMadera, Anchos, Largo, Grosor, Pulgadas, Precio, Total
                FROM Piezas
                WHERE CotizacionId = $id
                ORDER BY Id ASC;
            ";
            cmd.Parameters.AddWithValue("$id", cotizacionId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                piezas.Add(new PiezaDetalle
                {
                    TipoMadera = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Anchos = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Largo = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                    Grosor = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                    Pulgadas = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                    Precio = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    Total = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                });
            }

            return piezas;
        }

        // ── Evento: selección en tabla ────────────────────────
        private void dgHistorial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgHistorial.SelectedItem is not CotizacionHistorial cot)
            {
                panelDetalle.Visibility = Visibility.Collapsed;
                return;
            }

            _seleccionada = cot;

            string numFactura = $"SEYMU-{cot.Id:D4}";
            lblDetalleCliente.Text =
                $"{numFactura}  ·  {cot.Cliente}  ·  {cot.Fecha}  ·  Total: ₡ {cot.Total:N2}";

            dgDetallePiezas.ItemsSource = CargarPiezas(cot.Id);
            panelDetalle.Visibility = Visibility.Visible;
        }

        // ── Evento: búsqueda en tiempo real ──────────────────
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
            => AplicarFiltros();

        // ── Evento: filtro por fecha ──────────────────────────
        private void dpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
            => AplicarFiltros();

        // ── Evento: limpiar filtros ───────────────────────────
        private void LimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Text = "";
            dpFecha.SelectedDate = null;
            AplicarFiltros();
        }

        // ── Evento: reimprimir ────────────────────────────────
        private void Reimprimir_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null)
            {
                MessageBox.Show(
                    "Selecciona una cotización de la lista para reimprimir.",
                    "Sin selección",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var piezas = CargarPiezas(_seleccionada.Id);

            if (piezas.Count == 0)
            {
                MessageBox.Show(
                    "Esta cotización no tiene piezas registradas.",
                    "Sin piezas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool impreso = ImprimirCotizacion(_seleccionada, piezas);

            if (impreso)
                MessageBox.Show(
                    $"Cotización SEYMU-{_seleccionada.Id:D4} reimpresa correctamente.",
                    "Reimpresión exitosa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            else
                MessageBox.Show(
                    "No se detectó una impresora térmica AON / PR-100B.\n\n" +
                    "Verifica que la impresora esté instalada y encendida.",
                    "Impresora no encontrada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
        }

        // ── Detectar impresora térmica ────────────────────────
        private string DetectarImpresoraTermica()
        {
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                string n = p.ToLower();
                if (n.Contains("aon") || n.Contains("pr-100b") ||
                    n.Contains("thermal") || n.Contains("receipt") ||
                    n.Contains("pos") || n.Contains("58"))
                    return p;
            }
            return string.Empty;
        }

        // ── Imprimir ──────────────────────────────────────────
        private bool ImprimirCotizacion(CotizacionHistorial cot, List<PiezaDetalle> piezas)
        {
            string impresora = DetectarImpresoraTermica();
            if (string.IsNullOrWhiteSpace(impresora)) return false;

            PrintDocument pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = impresora;
            if (!pd.PrinterSettings.IsValid) return false;

            Empresa emp = DatabaseService.ObtenerEmpresa();

            pd.PrintPage += (sender, e) =>
            {
                Font fTitulo = new Font("Consolas", 12, System.Drawing.FontStyle.Bold);
                Font f = new Font("Consolas", 9);
                Font fTotal = new Font("Consolas", 10, System.Drawing.FontStyle.Bold);
                float y = 10;

                string nombreEmp = string.IsNullOrWhiteSpace(emp.Nombre)
                                   ? "SEYMU" : emp.Nombre.ToUpper();

                e.Graphics.DrawString(nombreEmp, fTitulo, Brushes.Black, 55, y); y += 25;

                if (!string.IsNullOrWhiteSpace(emp.Ubicacion))
                { e.Graphics.DrawString(emp.Ubicacion, f, Brushes.Black, 10, y); y += 18; }
                if (!string.IsNullOrWhiteSpace(emp.Telefono))
                { e.Graphics.DrawString($"Tel: {emp.Telefono}", f, Brushes.Black, 10, y); y += 18; }
                if (!string.IsNullOrWhiteSpace(emp.Correo))
                { e.Graphics.DrawString(emp.Correo, f, Brushes.Black, 10, y); y += 18; }

                string num = $"SEYMU-{cot.Id:D4}";
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Cotización: {num}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Fecha: {cot.Fecha}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Cliente: {cot.Cliente}", f, Brushes.Black, 10, y); y += 22;

                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString("Madera   Pulg   Total", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 20;

                foreach (var pieza in piezas)
                {
                    string linea =
                        $"{pieza.TipoMadera.PadRight(8)}" +
                        $"{pieza.Pulgadas.ToString("0").PadLeft(5)}" +
                        $"{pieza.Total.ToString("₡0").PadLeft(8)}";
                    e.Graphics.DrawString(linea, f, Brushes.Black, 10, y); y += 18;
                }

                y += 10;
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Subtotal: {cot.Subtotal:C}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"IVA 13%:  {cot.IVA:C}", f, Brushes.Black, 10, y); y += 22;
                e.Graphics.DrawString($"TOTAL: {cot.Total:C}", fTotal, Brushes.Black, 10, y); y += 25;
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString("Gracias por su compra", f, Brushes.Black, 20, y);
            };

            pd.Print();
            return true;
        }
    }

    // ── Modelo local solo para detalle de piezas del historial ─
    public class PiezaDetalle
    {
        public string TipoMadera { get; set; } = "";
        public string Anchos { get; set; } = "";
        public double Largo { get; set; }
        public double Grosor { get; set; }
        public double Pulgadas { get; set; }
        public double Precio { get; set; }
        public double Total { get; set; }  // ← typo corregido (faltaba {)
    }
}