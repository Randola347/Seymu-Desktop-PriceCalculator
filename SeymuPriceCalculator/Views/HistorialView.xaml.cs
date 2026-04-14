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
            // 1. Intentar obtener la impresora guardada en la base de datos
            var emp = DatabaseService.ObtenerEmpresa();
            if (!string.IsNullOrEmpty(emp.Impresora))
            {
                if (PrinterSettings.InstalledPrinters.Cast<string>().Any(p => p == emp.Impresora))
                    return emp.Impresora;
            }

            // 2. Búsqueda automática de respaldo
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
                if (e.Graphics == null) return;

                // Configuración de dimensiones para 58mm (aprox 200 unidades)
                float width = 195;
                float margin = 2;
                float y = 10;

                Font fTitulo = new Font("Consolas", 11, System.Drawing.FontStyle.Bold);
                Font f = new Font("Consolas", 8.5f);
                Font fBold = new Font("Consolas", 8.5f, System.Drawing.FontStyle.Bold);
                Font fTotal = new Font("Consolas", 10, System.Drawing.FontStyle.Bold);

                StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };

                // --- ENCABEZADO (EMPRESA) ---
                string nombreEmp = string.IsNullOrWhiteSpace(emp.Nombre) ? "SEYMU" : emp.Nombre.ToUpper();
                RectangleF rectNombre = new RectangleF(margin, y, width - (margin * 2), 40);
                e.Graphics.DrawString(nombreEmp, fTitulo, Brushes.Black, rectNombre, sfCenter);
                y += e.Graphics.MeasureString(nombreEmp, fTitulo, (int)width).Height + 2;

                if (!string.IsNullOrWhiteSpace(emp.Ubicacion))
                {
                    RectangleF rectUbi = new RectangleF(margin, y, width - (margin * 2), 60);
                    e.Graphics.DrawString(emp.Ubicacion, f, Brushes.Black, rectUbi, sfCenter);
                    y += e.Graphics.MeasureString(emp.Ubicacion, f, (int)width).Height + 2;
                }

                if (!string.IsNullOrWhiteSpace(emp.Telefono))
                {
                    e.Graphics.DrawString($"Tel: {emp.Telefono}", f, Brushes.Black, new RectangleF(margin, y, width, 20), sfCenter);
                    y += 15;
                }

                if (!string.IsNullOrWhiteSpace(emp.Correo))
                {
                    RectangleF rectCorreo = new RectangleF(margin, y, width - (margin * 2), 40);
                    e.Graphics.DrawString(emp.Correo, f, Brushes.Black, rectCorreo, sfCenter);
                    y += e.Graphics.MeasureString(emp.Correo, f, (int)width).Height + 5;
                }

                string separator = new string('-', 32);
                e.Graphics.DrawString(separator, f, Brushes.Black, margin, y); y += 15;

                // --- INFO COMPRA ---
                string num = $"SEYMU-{cot.Id:D4}";
                e.Graphics.DrawString($"Tickete: {num}", fBold, Brushes.Black, margin, y); y += 15;
                e.Graphics.DrawString($"Fecha: {cot.Fecha}", f, Brushes.Black, margin, y); y += 15;

                string clienteStr = $"Cliente: {cot.Cliente}";
                RectangleF rectCliente = new RectangleF(margin, y, width - (margin * 2), 40);
                e.Graphics.DrawString(clienteStr, f, Brushes.Black, rectCliente);
                y += e.Graphics.MeasureString(clienteStr, f, (int)width).Height + 5;

                e.Graphics.DrawString(separator, f, Brushes.Black, margin, y); y += 15;

                // --- CABECERA TABLA ---
                e.Graphics.DrawString("Madera", fBold, Brushes.Black, margin, y);
                e.Graphics.DrawString("Pulg", fBold, Brushes.Black, 110, y, sfRight);
                e.Graphics.DrawString("Total", fBold, Brushes.Black, width - margin, y, sfRight);
                y += 15;
                e.Graphics.DrawString(separator, f, Brushes.Black, margin, y); y += 15;

                // --- DETALLE PIEZAS ---
                foreach (var pieza in piezas)
                {
                    string madera = pieza.TipoMadera;
                    float maderaHeight = e.Graphics.MeasureString(madera, f, 100).Height;
                    e.Graphics.DrawString(madera, f, Brushes.Black, new RectangleF(margin, y, 105, 40));

                    e.Graphics.DrawString(pieza.Pulgadas.ToString("N1"), f, Brushes.Black, 110, y, sfRight);
                    e.Graphics.DrawString(pieza.Total.ToString("C0"), f, Brushes.Black, width - margin, y, sfRight);

                    y += Math.Max(maderaHeight, 15);
                }

                // --- TOTALES ---
                y += 10;
                e.Graphics.DrawString(separator, f, Brushes.Black, margin, y); y += 15;

                e.Graphics.DrawString("Subtotal:", f, Brushes.Black, margin, y);
                e.Graphics.DrawString(cot.Subtotal.ToString("C"), f, Brushes.Black, width - margin, y, sfRight);
                y += 15;

                e.Graphics.DrawString("IVA 13%:", f, Brushes.Black, margin, y);
                e.Graphics.DrawString(cot.IVA.ToString("C"), f, Brushes.Black, width - margin, y, sfRight);
                y += 20;

                e.Graphics.DrawString("TOTAL:", fTotal, Brushes.Black, margin, y);
                e.Graphics.DrawString(cot.Total.ToString("C"), fTotal, Brushes.Black, width - margin, y, sfRight);
                y += 25;

                e.Graphics.DrawString(separator, f, Brushes.Black, margin, y); y += 15;
                e.Graphics.DrawString("Gracias por su preferencia", fBold, Brushes.Black, new RectangleF(margin, y, width, 20), sfCenter);
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