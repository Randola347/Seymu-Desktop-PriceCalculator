using Microsoft.Data.Sqlite;
using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using SeymuPriceCalculator.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SeymuPriceCalculator.Services;
using System.Threading.Tasks;

namespace SeymuPriceCalculator
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Pieza> piezas = new();
        private Pieza? piezaEnEdicion = null;
        private List<ClienteDB> _clientesGuardados = new();

        public MainWindow()
        {
            InitializeComponent();
            DatabaseService.Initialize();
            dgPiezas.ItemsSource = piezas;
            CargarDatosEmpresa();
            CargarComboBoxes();
            CargarClientesGuardados();
            SeymuPriceCalculator.Database.NeonService.CargarConfiguracion();
            _ = SincronizarAsync();
        }

        private async Task SincronizarAsync()
        {
            ActualizarBadgeSync("⟳  Sincronizando...", "#AAFFAA");

            var result = await SyncService.SincronizarAsync();

            Dispatcher.Invoke(() =>
            {
                if (result.Exitoso)
                {
                    string badge = result.Subidos > 0 || result.Descargados > 0
                        ? $"✔  ↑{result.Subidos}  ↓{result.Descargados}"
                        : "✔  Sincronizado";
                    ActualizarBadgeSync(badge, "#CCFFCC");

                    // Si bajaron cotizaciones nuevas, recargar clientes
                    if (result.Descargados > 0)
                        CargarClientesGuardados();
                }
                else
                {
                    ActualizarBadgeSync("○  Sin conexión", "#AAAAAA");
                }
            });
        }

        private void ActualizarBadgeSync(string texto, string hex)
        {
            lblSync.Text = texto;
            lblSync.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString(hex));
        }

        // ========================= EMPRESA =========================
        public void CargarDatosEmpresa()
        {
            Empresa emp = DatabaseService.ObtenerEmpresa();

            txtNombreEmpresa.Text = string.IsNullOrWhiteSpace(emp.Nombre)
                                    ? "SEYMU"
                                    : emp.Nombre.ToUpper();

            txtLetraLogo.Text = emp.Nombre.Length > 0
                                ? emp.Nombre[0].ToString().ToUpper()
                                : "S";

            if (!string.IsNullOrWhiteSpace(emp.LogoPath) && File.Exists(emp.LogoPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(emp.LogoPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    imgLogoEmpresa.Source = bmp;
                    imgLogoEmpresa.Visibility = Visibility.Visible;
                    txtLetraLogo.Visibility = Visibility.Collapsed;
                    this.Icon = bmp;
                }
                catch
                {
                    imgLogoEmpresa.Visibility = Visibility.Collapsed;
                    txtLetraLogo.Visibility = Visibility.Visible;
                }
            }
            else
            {
                imgLogoEmpresa.Visibility = Visibility.Collapsed;
                txtLetraLogo.Visibility = Visibility.Visible;
            }

            this.Title = string.IsNullOrWhiteSpace(emp.Nombre)
                         ? "Seymu — Calculadora de Madera"
                         : $"{emp.Nombre} — Calculadora de Madera";
        }

        // ========================= COMBOBOXES DINÁMICOS =========================
        public void CargarComboBoxes()
        {
            var maderas = DatabaseService.GetTiposMadera();
            cmbMadera.Items.Clear();
            foreach (var m in maderas)
                cmbMadera.Items.Add(new ComboBoxItem { Content = m });
            if (cmbMadera.Items.Count > 0)
                cmbMadera.SelectedIndex = 0;

            var grosores = DatabaseService.GetGrosores();
            cmbGrosor.Items.Clear();
            foreach (var g in grosores)
                cmbGrosor.Items.Add(new ComboBoxItem
                {
                    Content = g.ToString(CultureInfo.InvariantCulture)
                });
            if (cmbGrosor.Items.Count > 0)
                cmbGrosor.SelectedIndex = 0;
        }

        // ========================= CLIENTES GUARDADOS =========================
        public void CargarClientesGuardados()
        {
            _clientesGuardados = DatabaseService.GetClientes();
        }

        // ── Filtra mientras escribe en Nombre ─────────────────
        private void txtNombre_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtNombre.Text.Trim().ToLower();

            if (texto.Length < 1)
            {
                popupNombre.IsOpen = false;
                return;
            }

            var coincidencias = _clientesGuardados
                .Where(c => c.Nombre.ToLower().Contains(texto))
                .Take(6)
                .ToList();

            if (coincidencias.Count == 0)
            {
                popupNombre.IsOpen = false;
                return;
            }

            lstSugerenciasNombre.ItemsSource = coincidencias;
            popupNombre.IsOpen = true;
        }

        // ── Filtra mientras escribe en Teléfono ───────────────
        private void txtTelefono_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtTelefono.Text.Trim();

            if (texto.Length < 1)
            {
                popupTelefono.IsOpen = false;
                return;
            }

            var coincidencias = _clientesGuardados
                .Where(c => c.Telefono.Contains(texto) ||
                            c.Nombre.ToLower().Contains(texto.ToLower()))
                .Take(6)
                .ToList();

            if (coincidencias.Count == 0)
            {
                popupTelefono.IsOpen = false;
                return;
            }

            lstSugerenciasTelefono.ItemsSource = coincidencias;
            popupTelefono.IsOpen = true;
        }

        // ── Seleccionar sugerencia de Nombre ──────────────────
        private void lstSugerenciasNombre_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (lstSugerenciasNombre.SelectedItem is not ClienteDB c) return;

            txtNombre.TextChanged -= txtNombre_TextChanged;
            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            txtNombre.Text = c.Nombre;
            txtTelefono.Text = c.Telefono;

            txtNombre.TextChanged += txtNombre_TextChanged;
            txtTelefono.TextChanged += txtTelefono_TextChanged;

            popupNombre.IsOpen = false;
            lstSugerenciasNombre.SelectedItem = null;

            txtTelefono.Focus();
            txtTelefono.CaretIndex = txtTelefono.Text.Length;
        }

        // ── Seleccionar sugerencia de Teléfono ────────────────
        private void lstSugerenciasTelefono_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (lstSugerenciasTelefono.SelectedItem is not ClienteDB c) return;

            txtNombre.TextChanged -= txtNombre_TextChanged;
            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            txtNombre.Text = c.Nombre;
            txtTelefono.Text = c.Telefono;

            txtNombre.TextChanged += txtNombre_TextChanged;
            txtTelefono.TextChanged += txtTelefono_TextChanged;

            popupTelefono.IsOpen = false;
            lstSugerenciasTelefono.SelectedItem = null;

            txtTelefono.Focus();
            txtTelefono.CaretIndex = txtTelefono.Text.Length;
        }

        // ── Cerrar popup al perder foco ───────────────────────
        private void txtNombre_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!lstSugerenciasNombre.IsKeyboardFocusWithin)
                    popupNombre.IsOpen = false;
            }), DispatcherPriority.Background);
        }

        private void txtTelefono_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!lstSugerenciasTelefono.IsKeyboardFocusWithin)
                    popupTelefono.IsOpen = false;
            }), DispatcherPriority.Background);
        }

        // ── Flechas y Escape en campo Nombre ──────────────────
        private void txtNombre_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!popupNombre.IsOpen) return;

            if (e.Key == Key.Down)
            {
                lstSugerenciasNombre.Focus();
                if (lstSugerenciasNombre.Items.Count > 0)
                    lstSugerenciasNombre.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popupNombre.IsOpen = false;
                e.Handled = true;
            }
        }

        // ── Flechas y Escape en campo Teléfono ────────────────
        private void txtTelefono_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!popupTelefono.IsOpen) return;

            if (e.Key == Key.Down)
            {
                lstSugerenciasTelefono.Focus();
                if (lstSugerenciasTelefono.Items.Count > 0)
                    lstSugerenciasTelefono.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popupTelefono.IsOpen = false;
                e.Handled = true;
            }
        }

        // ── Enter en la lista selecciona el item ──────────────
        private void lstSugerencias_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (sender is ListBox lb)
            {
                if (lb == lstSugerenciasNombre)
                    lstSugerenciasNombre_SelectionChanged(lb, null!);
                else
                    lstSugerenciasTelefono_SelectionChanged(lb, null!);
            }
            e.Handled = true;
        }

        // ========================= CONFIGURACIÓN =========================
        private void AbrirConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ConfiguracionWindow();
            ventana.Owner = this;
            ventana.ShowDialog();
            CargarDatosEmpresa();
            CargarClientesGuardados();
            CargarComboBoxes();
        }

        // ========================= MENSAJES =========================
        private void MostrarInfo(string titulo, string mensaje)
            => MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);

        private void MostrarAdvertencia(string titulo, string mensaje)
            => MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Warning);

        private void MostrarError(string titulo, string mensaje)
            => MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Error);

        // ========================= VALIDACIÓN ANTES DE GUARDAR =========================
        private bool ValidarAntesDeGuardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarAdvertencia("Información incompleta", "Debe ingresar el nombre del cliente.");
                txtNombre.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MostrarAdvertencia("Información incompleta", "Debe ingresar el teléfono del cliente.");
                txtTelefono.Focus();
                return false;
            }
            if (piezas.Count == 0)
            {
                MostrarAdvertencia("Información incompleta", "Debe agregar al menos una pieza.");
                return false;
            }
            if (piezas.Sum(p => p.Total) <= 0)
            {
                MostrarAdvertencia("Información incompleta", "El total de la cotización no es válido.");
                return false;
            }
            return true;
        }

        // ========================= GUARDAR / IMPRIMIR =========================
        private void GuardarCotizacion_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarAntesDeGuardar()) return;

            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                double subtotal = piezas.Sum(p => p.Total);
                double iva = subtotal * 0.13;
                double total = subtotal + iva;

                var countCmd = connection.CreateCommand();
                countCmd.CommandText = "SELECT IFNULL(MAX(Id),0) FROM Cotizaciones";
                long ultimoId = Convert.ToInt64(countCmd.ExecuteScalar());
                string numFactura = $"SEYMU-{ultimoId + 1:D4}";

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

                foreach (var pieza in piezas)
                {
                    var pc = connection.CreateCommand();
                    pc.CommandText = @"
                        INSERT INTO Piezas
                        (CotizacionId, TipoMadera, Anchos, Largo, TotalAncho, Grosor, Pulgadas, Precio, Total)
                        VALUES ($cid,$tipo,$anchos,$largo,$tancho,$grosor,$pulg,$precio,$total)
                    ";
                    pc.Parameters.AddWithValue("$cid", cotizacionId);
                    pc.Parameters.AddWithValue("$tipo", pieza.TipoMadera);
                    pc.Parameters.AddWithValue("$anchos", pieza.Anchos);
                    pc.Parameters.AddWithValue("$largo", pieza.Largo);
                    pc.Parameters.AddWithValue("$tancho", pieza.TotalAncho);
                    pc.Parameters.AddWithValue("$grosor", pieza.Grosor);
                    pc.Parameters.AddWithValue("$pulg", pieza.TotalBase);
                    pc.Parameters.AddWithValue("$precio", pieza.Precio);
                    pc.Parameters.AddWithValue("$total", pieza.Total);
                    pc.ExecuteNonQuery();
                }

                bool impreso = false;
                string detalleImpresion = "";

                try
                {
                    impreso = ImprimirTicket(numFactura);
                    if (!impreso)
                        detalleImpresion = "No se detectó una impresora térmica AON / PR-100B instalada.";
                }
                catch (Exception exPrint)
                {
                    detalleImpresion = exPrint.Message;
                }

                if (impreso)
                    MostrarInfo("Cotización registrada",
                        $"✔ Guardada e impresa correctamente\n\nNúmero: {numFactura}\n" +
                        $"Cliente: {txtNombre.Text}\nTeléfono: {txtTelefono.Text}\nTotal: {total:C}");
                else
                    MostrarAdvertencia("Cotización guardada",
                        $"Guardada correctamente, pero no se pudo imprimir.\n\n" +
                        $"Número: {numFactura}\nMotivo: {detalleImpresion}");

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
                MostrarError("Error", $"Error al guardar.\n\n{ex.Message}");
            }
        }

        // ========================= IMPRESORA =========================
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

        private bool ImprimirTicket(string numeroFactura)
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

                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Cotización: {numeroFactura}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Cliente: {txtNombre.Text}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Tel: {txtTelefono.Text}", f, Brushes.Black, 10, y); y += 22;

                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString("Madera   Pulg   Total", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 20;

                foreach (var pieza in piezas)
                {
                    string linea =
                        $"{pieza.TipoMadera.PadRight(8)}" +
                        $"{pieza.TotalBase.ToString("0").PadLeft(5)}" +
                        $"{pieza.Total.ToString("₡0").PadLeft(8)}";
                    e.Graphics.DrawString(linea, f, Brushes.Black, 10, y); y += 18;
                }

                double sub = piezas.Sum(p => p.Total);
                double iva = sub * 0.13;
                double tfin = sub + iva;

                y += 10;
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"Subtotal: {sub:C}", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString($"IVA 13%:  {iva:C}", f, Brushes.Black, 10, y); y += 22;
                e.Graphics.DrawString($"TOTAL: {tfin:C}", fTotal, Brushes.Black, 10, y); y += 25;
                e.Graphics.DrawString("--------------------------------", f, Brushes.Black, 10, y); y += 18;
                e.Graphics.DrawString("Gracias por su compra", f, Brushes.Black, 20, y);
            };

            pd.Print();
            return true;
        }

        // ========================= AGREGAR =========================
        private void AgregarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario(out double largo, out double precio,
                                   out double grosor, out double totalAncho))
                return;

            string tipoMadera = (cmbMadera.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            piezas.Add(new Pieza
            {
                Numero = piezas.Count + 1,
                TipoMadera = tipoMadera,
                Anchos = txtAnchos.Text,
                Largo = largo,
                TotalAncho = totalAncho,
                Grosor = grosor,
                Precio = precio
            });

            RenumerarPiezas();
            ActualizarTotales();
            LimpiarFormulario();
        }

        // ========================= EDITAR =========================
        private void EditarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton) return;
            if (boton.Tag is not Pieza pieza) return;

            piezaEnEdicion = pieza;

            cmbMadera.Text = pieza.TipoMadera;
            txtAnchos.Text = pieza.Anchos;
            txtLargo.Text = pieza.Largo.ToString(CultureInfo.InvariantCulture);
            cmbGrosor.Text = pieza.Grosor.ToString(CultureInfo.InvariantCulture);
            txtPrecio.Text = pieza.Precio.ToString(CultureInfo.InvariantCulture);

            btnAgregar.Visibility = Visibility.Collapsed;
            panelEdicion.Visibility = Visibility.Visible;
        }

        private void GuardarEdicion_Click(object sender, RoutedEventArgs e)
        {
            if (piezaEnEdicion == null) return;
            if (!ValidarFormulario(out double largo, out double precio,
                                   out double grosor, out double totalAncho))
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
            => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            piezaEnEdicion = null;
            txtLargo.Clear();
            txtAnchos.Clear();
            txtPrecio.Clear();
            btnAgregar.Visibility = Visibility.Visible;
            panelEdicion.Visibility = Visibility.Collapsed;
        }

        // ========================= ELIMINAR =========================
        private void EliminarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton) return;
            if (boton.Tag is not Pieza pieza) return;

            if (MessageBox.Show("¿Desea eliminar esta pieza?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                piezas.Remove(pieza);
                RenumerarPiezas();
                ActualizarTotales();
            }
        }

        // ========================= VALIDACIÓN CENTRAL =========================
        private bool ValidarFormulario(out double largo, out double precio,
                                       out double grosor, out double totalAncho)
        {
            largo = 0; precio = 0; grosor = 0; totalAncho = 0;

            if (!double.TryParse(txtLargo.Text, NumberStyles.Any,
                CultureInfo.InvariantCulture, out largo) || largo <= 0)
            {
                MostrarAdvertencia("Dato inválido", "Ingrese un largo válido.");
                txtLargo.Focus(); return false;
            }
            if (!double.TryParse(txtPrecio.Text, NumberStyles.Any,
                CultureInfo.InvariantCulture, out precio) || precio <= 0)
            {
                MostrarAdvertencia("Dato inválido", "Ingrese un precio válido.");
                txtPrecio.Focus(); return false;
            }
            if (cmbGrosor.SelectedItem is not ComboBoxItem grosorItem)
            {
                MostrarAdvertencia("Dato inválido", "Seleccione un grosor.");
                cmbGrosor.Focus(); return false;
            }
            string grosorTexto = grosorItem.Content?.ToString() ?? "0";
            grosor = double.Parse(grosorTexto, CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(txtAnchos.Text))
            {
                MostrarAdvertencia("Dato inválido", "Ingrese al menos un ancho.");
                txtAnchos.Focus(); return false;
            }
            foreach (var parte in txtAnchos.Text.Split('-'))
            {
                if (!double.TryParse(parte.Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double val) || val <= 0)
                {
                    MostrarAdvertencia("Dato inválido", "Formato de anchos inválido. Ej: 10-7-5");
                    txtAnchos.Focus(); return false;
                }
                totalAncho += val;
            }
            return true;
        }

        // ========================= NUMERACIÓN =========================
        private void RenumerarPiezas()
        {
            for (int i = 0; i < piezas.Count; i++)
                piezas[i].Numero = i + 1;
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
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ ]+$");

        private void SoloNumeros(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");

        private void SoloDecimal(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox tb && e.Text == "." && tb.Text.Contains("."))
            { e.Handled = true; return; }
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.]+$");
        }

        private void SoloNumerosYGuion(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[0-9-]+$");
    }
}