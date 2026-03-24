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

        // ========================= MENSAJES =========================
        private void MostrarInfo(string titulo, string mensaje)
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MostrarAdvertencia(string titulo, string mensaje)
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void MostrarError(string titulo, string mensaje)
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ========================= VALIDACIÓN ANTES DE GUARDAR =========================
        private bool ValidarAntesDeGuardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarAdvertencia("Información incompleta", "Debe ingresar el nombre del cliente antes de guardar o imprimir.");
                txtNombre.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MostrarAdvertencia("Información incompleta", "Debe ingresar el teléfono del cliente antes de guardar o imprimir.");
                txtTelefono.Focus();
                return false;
            }

            if (piezas.Count == 0)
            {
                MostrarAdvertencia("Información incompleta", "Debe agregar al menos una pieza antes de guardar o imprimir.");
                return false;
            }

            double subtotal = piezas.Sum(p => p.Total);
            if (subtotal <= 0)
            {
                MostrarAdvertencia("Información incompleta", "El total de la cotización no es válido.");
                return false;
            }

            return true;
        }

        // ========================= GUARDAR / IMPRIMIR =========================
        private void GuardarCotizacion_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarAntesDeGuardar())
                return;

            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                double subtotal = piezas.Sum(p => p.Total);
                double iva = subtotal * 0.13;
                double total = subtotal + iva;

                // Obtener último ID para generar número tipo factura
                var countCmd = connection.CreateCommand();
                countCmd.CommandText = "SELECT IFNULL(MAX(Id),0) FROM Cotizaciones";
                long ultimoId = Convert.ToInt64(countCmd.ExecuteScalar());
                long siguienteId = ultimoId + 1;

                string numeroFactura = $"SEYMU-{siguienteId:D4}";

                // Insertar cotización
                var command = connection.CreateCommand();
                command.CommandText = @"
INSERT INTO Cotizaciones (Fecha, Cliente, Subtotal, IVA, Total)
VALUES ($fecha, $cliente, $subtotal, $iva, $total);
SELECT last_insert_rowid();
";

                command.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                command.Parameters.AddWithValue("$cliente", txtNombre.Text.Trim());
                command.Parameters.AddWithValue("$subtotal", subtotal);
                command.Parameters.AddWithValue("$iva", iva);
                command.Parameters.AddWithValue("$total", total);

                long cotizacionId = Convert.ToInt64(command.ExecuteScalar());

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

                bool impreso = false;
                string detalleImpresion = "";

                try
                {
                    impreso = ImprimirTicket(numeroFactura);

                    if (!impreso)
                        detalleImpresion = "No se detectó una impresora térmica AON / PR-100B instalada.";
                }
                catch (Exception exPrint)
                {
                    impreso = false;
                    detalleImpresion = exPrint.Message;
                }

                if (impreso)
                {
                    MostrarInfo(
                        "SEYMU - Cotización registrada",
                        $@"✔ Cotización guardada e impresa correctamente

Número de cotización:
{numeroFactura}

Cliente:
{txtNombre.Text}

Teléfono:
{txtTelefono.Text}

Total:
{total:C}");
                }
                else
                {
                    MostrarAdvertencia(
                        "SEYMU - Cotización guardada",
                        $@"La cotización se guardó correctamente, pero no se pudo imprimir.

Número de cotización:
{numeroFactura}

Motivo:
{detalleImpresion}");
                }

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
                MostrarError(
                    "SEYMU - Error",
                    $@"Ocurrió un error al guardar la cotización.

Detalle técnico:
{ex.Message}");
            }
        }

        // ========================= IMPRESORA =========================
        private string DetectarImpresoraTermica()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                string nombre = printer.ToLower();

                if (nombre.Contains("aon") ||
                    nombre.Contains("pr-100b") ||
                    nombre.Contains("thermal") ||
                    nombre.Contains("receipt") ||
                    nombre.Contains("pos") ||
                    nombre.Contains("58"))
                {
                    return printer;
                }
            }

            // NO devolver la primera impresora del sistema
            return string.Empty;
        }

        private bool ImprimirTicket(string numeroFactura)
        {
            string impresora = DetectarImpresoraTermica();

            if (string.IsNullOrWhiteSpace(impresora))
                return false;

            PrintDocument pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = impresora;

            if (!pd.PrinterSettings.IsValid)
                return false;

            pd.PrintPage += (sender, e) =>
            {
                Font fontTitulo = new Font(new FontFamily("Consolas"), 12, System.Drawing.FontStyle.Bold);
                Font font = new Font(new FontFamily("Consolas"), 9);
                Font fontTotal = new Font(new FontFamily("Consolas"), 10, System.Drawing.FontStyle.Bold);

                float y = 10;

                // Encabezado
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
                y += 18;

                e.Graphics.DrawString($"Tel: {txtTelefono.Text}", font, Brushes.Black, 10, y);
                y += 22;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                // Cabecera tabla
                e.Graphics.DrawString("Madera   Pulg   Total", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 20;

                foreach (var pieza in piezas)
                {
                    string madera = pieza.TipoMadera.PadRight(8);
                    string pulg = pieza.TotalBase.ToString("0").PadLeft(5);
                    string totalLinea = pieza.Total.ToString("₡0").PadLeft(8);

                    string linea = $"{madera}{pulg}{totalLinea}";
                    e.Graphics.DrawString(linea, font, Brushes.Black, 10, y);
                    y += 18;
                }

                double subtotal = piezas.Sum(p => p.Total);
                double iva = subtotal * 0.13;
                double totalFinal = subtotal + iva;

                y += 10;
                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString($"Subtotal: {subtotal:C}", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString($"IVA 13%: {iva:C}", font, Brushes.Black, 10, y);
                y += 22;

                e.Graphics.DrawString($"TOTAL: {totalFinal:C}", fontTotal, Brushes.Black, 10, y);
                y += 25;

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, 10, y);
                y += 18;

                e.Graphics.DrawString("Gracias por su compra", font, Brushes.Black, 20, y);
            };

            pd.Print();
            return true;
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
                MostrarAdvertencia("Dato inválido", "Ingrese un largo válido.");
                txtLargo.Focus();
                return false;
            }

            if (!double.TryParse(txtPrecio.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out precio) || precio <= 0)
            {
                MostrarAdvertencia("Dato inválido", "Ingrese un precio válido.");
                txtPrecio.Focus();
                return false;
            }

            if (cmbGrosor.SelectedItem is not ComboBoxItem grosorItem)
            {
                MostrarAdvertencia("Dato inválido", "Seleccione un grosor.");
                cmbGrosor.Focus();
                return false;
            }

            grosor = double.Parse(grosorItem.Content?.ToString() ?? "0", CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(txtAnchos.Text))
            {
                MostrarAdvertencia("Dato inválido", "Ingrese al menos un ancho.");
                txtAnchos.Focus();
                return false;
            }

            string[] partes = txtAnchos.Text.Split('-');

            foreach (var parte in partes)
            {
                if (!double.TryParse(parte.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) || valor <= 0)
                {
                    MostrarAdvertencia("Dato inválido", "Formato de anchos inválido.");
                    txtAnchos.Focus();
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