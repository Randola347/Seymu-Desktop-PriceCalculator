using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SeymuPriceCalculator.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
namespace SeymuPriceCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// using SeymuPriceCalculator.Models;
    
    public partial class MainWindow : Window
   
    {

        public MainWindow()
        {
            InitializeComponent();
        }
        private Cotizacion cotizacionActual = new Cotizacion();
        private void AgregarPieza_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtLargo.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double largo) || largo <= 0)
            {
                MessageBox.Show("Ingrese un largo válido (Ej: 0.375, 1.2, etc).");
                return;
            }

            if (!double.TryParse(txtPrecio.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido.");
                return;
            }

            if (cmbGrosor.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un grosor.");
                return;
            }

            double grosor = double.Parse(((ComboBoxItem)cmbGrosor.SelectedItem).Content.ToString(), CultureInfo.InvariantCulture);



            // Procesar anchos separados por -
            if (string.IsNullOrWhiteSpace(txtAnchos.Text))
            {
                MessageBox.Show("Ingrese al menos un ancho.");
                return;
            }

            string[] partes = txtAnchos.Text.Split('-');

            double totalAncho = 0;

            foreach (var parte in partes)
            {
                if (!double.TryParse(parte.Trim(), out double valor) || valor <= 0)
                {
                    MessageBox.Show("Formato de anchos inválido. Ejemplo correcto: 10-7-5-9");
                    return;
                }

                totalAncho += valor;
            }

            Pieza nuevaPieza = new Pieza
            {
                TipoMadera = ((ComboBoxItem)cmbMadera.SelectedItem).Content.ToString(),
                Largo = largo,
                TotalAncho = totalAncho,
                Grosor = grosor,
                Precio = precio
            };

            cotizacionActual.Piezas.Add(nuevaPieza);

            dgPiezas.ItemsSource = null;
            dgPiezas.ItemsSource = cotizacionActual.Piezas;

            lblSubtotal.Text = cotizacionActual.Subtotal.ToString("C");
            lblIVA.Text = cotizacionActual.IVA.ToString("C");
            lblTotal.Text = cotizacionActual.TotalFinal.ToString("C");

            txtLargo.Clear();
            txtAnchos.Clear();
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