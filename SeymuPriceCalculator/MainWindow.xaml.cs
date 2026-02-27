using SeymuPriceCalculator.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SeymuPriceCalculator
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Pieza> piezas = new ObservableCollection<Pieza>();
        private Pieza? piezaEnEdicion = null;

        public MainWindow()
        {
            InitializeComponent();
            dgPiezas.ItemsSource = piezas;
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