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

        public MainWindow()
        {
            InitializeComponent();
            dgPiezas.ItemsSource = piezas;
        }

        private void AgregarPieza_Click(object sender, RoutedEventArgs e)
        {
            // Validar largo
            if (!double.TryParse(txtLargo.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double largo) || largo <= 0)
            {
                MessageBox.Show("Ingrese un largo válido.");
                return;
            }

            // Validar precio
            if (!double.TryParse(txtPrecio.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido.");
                return;
            }

            // Validar grosor seguro
            if (cmbGrosor.SelectedItem is not ComboBoxItem grosorItem)
            {
                MessageBox.Show("Seleccione un grosor.");
                return;
            }

            double grosor = double.Parse(
                grosorItem.Content?.ToString() ?? "0",
                CultureInfo.InvariantCulture);

            // Validar anchos
            if (string.IsNullOrWhiteSpace(txtAnchos.Text))
            {
                MessageBox.Show("Ingrese al menos un ancho.");
                return;
            }

            string[] partes = txtAnchos.Text.Split('-');
            double totalAncho = 0;

            foreach (var parte in partes)
            {
                if (!double.TryParse(parte.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor) || valor <= 0)
                {
                    MessageBox.Show("Formato de anchos inválido.");
                    return;
                }

                totalAncho += valor;
            }

            // Obtener tipo madera seguro
            string tipoMadera = "";

            if (cmbMadera.SelectedItem is ComboBoxItem maderaItem)
            {
                tipoMadera = maderaItem.Content?.ToString() ?? "";
            }

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

            txtLargo.Clear();
            txtAnchos.Clear();
        }

        private void RenumerarPiezas()
        {
            for (int i = 0; i < piezas.Count; i++)
            {
                piezas[i].Numero = i + 1;
            }

            dgPiezas.Items.Refresh();
        }

        private void ActualizarTotales()
        {
            double subtotal = piezas.Sum(p => p.Total);
            double iva = subtotal * 0.13;
            double total = subtotal + iva;

            lblSubtotal.Text = subtotal.ToString("C");
            lblIVA.Text = iva.ToString("C");
            lblTotal.Text = total.ToString("C");
        }

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