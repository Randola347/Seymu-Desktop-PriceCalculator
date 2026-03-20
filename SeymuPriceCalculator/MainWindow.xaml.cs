using SeymuPriceCalculator.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SeymuPriceCalculator.Database;
using Microsoft.Data.Sqlite;
using System.Drawing;
using System.Drawing.Printing;


namespace SeymuPriceCalculator
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Pieza> piezas = new ObservableCollection<Pieza>();
        private Pieza? piezaEnEdicion = null;

        public MainWindow()
        {
            InitializeComponent();
            DatabaseService.Initialize();
            dgPiezas.ItemsSource = piezas;
        }
        private void GuardarCotizacion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                // Obtener último ID para generar número tipo factura
                var countCmd = connection.CreateCommand();
                countCmd.CommandText = "SELECT IFNULL(MAX(Id),0) FROM Cotizaciones";
                long ultimoId = (long)countCmd.ExecuteScalar();

                long siguienteId = ultimoId + 1;

                string numeroFactura = $"SEYMU-{siguienteId.ToString("D4")}";

                // Insertar cotización
                var command = connection.CreateCommand();

                command.CommandText = @"
INSERT INTO Cotizaciones (Fecha, Cliente, Subtotal, IVA, Total)
VALUES ($fecha, $cliente, $subtotal, $iva, $total);
SELECT last_insert_rowid();
";

                command.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                command.Parameters.AddWithValue("$cliente", txtNombre.Text);

                command.Parameters.AddWithValue("$subtotal",
                    double.Parse(lblSubtotal.Text.Replace("₡", "").Trim()));

                command.Parameters.AddWithValue("$iva",
                    double.Parse(lblIVA.Text.Replace("₡", "").Trim()));

                command.Parameters.AddWithValue("$total",
                    double.Parse(lblTotal.Text.Replace("₡", "").Trim()));

                long cotizacionId = (long)command.ExecuteScalar();

                // Guardar piezas
                foreach (var pieza in piezas)
                {
                    var piezaCmd = connection.CreateCommand();

                    piezaCmd.CommandText = @"
INSERT INTO Piezas
(CotizacionId, TipoMadera, Anchos, Largo, TotalAncho, Grosor, Pulgadas, Precio, Total)
VALUES
($cotizacionId, $tipo, $anchos, $largo, $totalAncho, $grosor, $pulgadas, $precio, $total)
";

                    piezaCmd.Parameters.AddWithValue("$cotizacionId", cotizacionId);
                    piezaCmd.Parameters.AddWithValue("$tipo", pieza.TipoMadera);
                    piezaCmd.Parameters.AddWithValue("$anchos", pieza.Anchos);
                    piezaCmd.Parameters.AddWithValue("$largo", pieza.Largo);
                    piezaCmd.Parameters.AddWithValue("$totalAncho", pieza.TotalAncho);
                    piezaCmd.Parameters.AddWithValue("$grosor", pieza.Grosor);
                    piezaCmd.Parameters.AddWithValue("$pulgadas", pieza.TotalBase);
                    piezaCmd.Parameters.AddWithValue("$precio", pieza.Precio);
                    piezaCmd.Parameters.AddWithValue("$total", pieza.Total);

                    piezaCmd.ExecuteNonQuery();
                }

                // Mensaje visual mejorado
                ImprimirTicket(numeroFactura);
                MessageBox.Show(
        $@"✔ Cotización guardada correctamente

Número de cotización:
{numeroFactura}

Cliente:
{txtNombre.Text}

Total:
{lblTotal.Text}",
                "SEYMU - Cotización registrada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

                // Limpiar pantalla para nueva cotización
                piezas.Clear();

                txtNombre.Clear();
                txtTelefono.Clear();

                txtAnchos.Clear();
                txtLargo.Clear();
                txtPrecio.Clear();

                cmbMadera.SelectedIndex = 0;
                cmbGrosor.SelectedIndex = 0;

                ActualizarTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
        $@"Ocurrió un error al guardar la cotización.

Detalle técnico:
{ex.Message}",
                "SEYMU - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            }
        }

        private string DetectarImpresoraTermica()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.ToLower().Contains("aon") ||
                    printer.ToLower().Contains("thermal") ||
                    printer.ToLower().Contains("receipt") ||
                    printer.ToLower().Contains("receipt") ||
                    printer.ToLower().Contains("pos"))
                {
                    return printer;
                }
            }

            return PrinterSettings.InstalledPrinters.Count > 0
                ? PrinterSettings.InstalledPrinters[0]
                : "";
        }

        private void ImprimirTicket(string numeroFactura)
        {
            PrintDocument pd = new PrintDocument();

            // Detectar impresora térmica automáticamente
            string impresora = DetectarImpresoraTermica();

            if (!string.IsNullOrEmpty(impresora))
                pd.PrinterSettings.PrinterName = impresora;

            pd.PrintPage += (sender, e) =>
            {
                Font fontTitulo = new Font(new FontFamily("Consolas"), 12, System.Drawing.FontStyle.Bold);
                Font font = new Font(new FontFamily("Consolas"), 9);
                Font fontTotal = new Font(new FontFamily("Consolas"), 10, System.Drawing.FontStyle.Bold);

                float y = 10;

                // ENCABEZADO
                e.Graphics.DrawString("SEYMU", fontTitulo, Brushes.Black, 55, y);
                y += 25;

                e.Graphics.DrawString("Maderas y Aserradero", font, Brushes.Black, 20, y);
                y += 20;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString($"Cotización: {numeroFactura}", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString($"Cliente: {txtNombre.Text}", font, Brushes.Black, 10, y);
                y += 22;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                // CABECERA TABLA
                e.Graphics.DrawString("Madera   Pulg   Total", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 20;

                // PIEZAS
                foreach (var pieza in piezas)
                {
                    string madera = pieza.TipoMadera.PadRight(8);
                    string pulg = pieza.TotalBase.ToString("0").PadLeft(5);
                    string total = pieza.Total.ToString("₡0").PadLeft(8);

                    string linea = $"{madera}{pulg}{total}";

                    e.Graphics.DrawString(linea, font, Brushes.Black, 10, y);

                    y += 18;
                }

                y += 10;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                // TOTALES
                e.Graphics.DrawString($"Subtotal: {lblSubtotal.Text}", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString($"IVA 13%: {lblIVA.Text}", font, Brushes.Black, 10, y);
                y += 22;

                e.Graphics.DrawString($"TOTAL: {lblTotal.Text}", fontTotal, Brushes.Black, 10, y);
                y += 25;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString("Gracias por su compra", font, Brushes.Black, 20, y);
                y += 30;
            };

            pd.Print();
        }
        // ========================= AGREGAR =========================
        private void AgregarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario(out double largo, out double precio, out double grosor, out double totalAncho))
                return;

            string tipoMadera = (cmbMadera.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            Pieza nuevaPieza = new Pieza
            {
                Numero = piezas.Count + 1,
                TipoMadera = tipoMadera,
                Anchos = txtAnchos.Text,
                Largo = largo,
                TotalAncho = totalAncho,
                Grosor = grosor,
                Precio = precio
            };

            piezas.Add(nuevaPieza);

            RenumerarPiezas();
            ActualizarTotales();
            LimpiarFormulario();
        }

        // ========================= EDITAR =========================
        private void EditarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton)
                return;

            if (boton.Tag is not Pieza pieza)
                return;

            piezaEnEdicion = pieza;

            cmbMadera.Text = pieza.TipoMadera;
            txtAnchos.Text = pieza.Anchos;
            txtLargo.Text = pieza.Largo.ToString(CultureInfo.InvariantCulture);
            cmbGrosor.Text = pieza.Grosor.ToString(CultureInfo.InvariantCulture);
            txtPrecio.Text = pieza.Precio.ToString(CultureInfo.InvariantCulture);

            btnAgregar.Visibility = Visibility.Collapsed;
            btnGuardar.Visibility = Visibility.Visible;
            btnCancelar.Visibility = Visibility.Visible;
        }

        private void GuardarEdicion_Click(object sender, RoutedEventArgs e)
        {
            if (piezaEnEdicion == null)
                return;

            if (!ValidarFormulario(out double largo, out double precio, out double grosor, out double totalAncho))
                return;

            string tipoMadera = (cmbMadera.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            piezaEnEdicion.TipoMadera = tipoMadera;
            piezaEnEdicion.Anchos = txtAnchos.Text;
            piezaEnEdicion.Largo = largo;
            piezaEnEdicion.TotalAncho = totalAncho;
            piezaEnEdicion.Grosor = grosor;
            piezaEnEdicion.Precio = precio;

            dgPiezas.Items.Refresh();

            ActualizarTotales();
            LimpiarFormulario();
        }

        private void CancelarEdicion_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            piezaEnEdicion = null;

            txtLargo.Clear();
            txtAnchos.Clear();
            txtPrecio.Clear();

            btnAgregar.Visibility = Visibility.Visible;
            btnGuardar.Visibility = Visibility.Collapsed;
            btnCancelar.Visibility = Visibility.Collapsed;
        }

        // ========================= ELIMINAR =========================
        private void EliminarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton)
                return;

            if (boton.Tag is not Pieza pieza)
                return;

            var resultado = MessageBox.Show(
                "¿Desea eliminar esta pieza?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                piezas.Remove(pieza);
                RenumerarPiezas();
                ActualizarTotales();
            }
        }

        // ========================= VALIDACIÓN CENTRAL =========================
        private bool ValidarFormulario(out double largo, out double precio, out double grosor, out double totalAncho)
        {
            largo = 0;
            precio = 0;
            grosor = 0;
            totalAncho = 0;

            if (!double.TryParse(txtLargo.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out largo) || largo <= 0)
            {
                MessageBox.Show("Ingrese un largo válido.");
                return false;
            }

            if (!double.TryParse(txtPrecio.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido.");
                return false;
            }

            if (cmbGrosor.SelectedItem is not ComboBoxItem grosorItem)
            {
                MessageBox.Show("Seleccione un grosor.");
                return false;
            }

            grosor = double.Parse(grosorItem.Content?.ToString() ?? "0", CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(txtAnchos.Text))
            {
                MessageBox.Show("Ingrese al menos un ancho.");
                return false;
            }

            string[] partes = txtAnchos.Text.Split('-');

            foreach (var parte in partes)
            {
                if (!double.TryParse(parte.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) || valor <= 0)
                {
                    MessageBox.Show("Formato de anchos inválido.");
                    return false;
                }

                totalAncho += valor;
            }

            return true;
        }

        // ========================= NUMERACIÓN =========================
        private void RenumerarPiezas()
        {
            for (int i = 0; i < piezas.Count; i++)
            {
                piezas[i].Numero = i + 1;
            }

            dgPiezas.Items.Refresh();
        }

        // ========================= TOTALES =========================
        private void ActualizarTotales()
        {
            double subtotal = piezas.Sum(p => p.Total);
            double iva = subtotal * 0.13;
            double total = subtotal + iva;

            lblSubtotal.Text = subtotal.ToString("C");
            lblIVA.Text = iva.ToString("C");
            lblTotal.Text = total.ToString("C");
        }

        // ========================= VALIDACIONES INPUT =========================
        private void SoloLetras(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[a-zA-ZáéíóúÁÉÍÓÚñÑ ]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void SoloNumeros(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void SoloDecimal(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            if (e.Text == "." && tb.Text.Contains("."))
            {
                e.Handled = true;
                return;
            }

            Regex regex = new Regex("^[0-9.]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void SoloNumerosYGuion(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9-]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }
    }
}