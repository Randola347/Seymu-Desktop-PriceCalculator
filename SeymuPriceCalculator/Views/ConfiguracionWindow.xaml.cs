using SeymuPriceCalculator.Views;
using System.Windows;
using System.Windows.Controls;

namespace SeymuPriceCalculator.Views
{
    public partial class ConfiguracionWindow : Window
    {
        public ConfiguracionWindow()
        {
            InitializeComponent();
            // Abre en Empresa por defecto
            MostrarEmpresa_Click(this, null!);
        }

        private void MostrarHistorial_Click(object sender, RoutedEventArgs e)
        {
            ContenidoPrincipal.Content = new HistorialView();
            lblSeccionActual.Text = "📋  Historial de Cotizaciones";
            ActualizarBotones(btnHistorial);
        }

        private void MostrarAdministrar_Click(object sender, RoutedEventArgs e)
        {
            ContenidoPrincipal.Content = new AdministrarView();
            lblSeccionActual.Text = "🗂  Administrar Catálogos";
            ActualizarBotones(btnAdministrar);
        }

        private void MostrarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            ContenidoPrincipal.Content = new EmpresaView();
            lblSeccionActual.Text = "🏢  Datos de la Empresa";
            ActualizarBotones(btnEmpresa);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
            => Close();

        // Marca el botón activo visualmente
        private void ActualizarBotones(Button activo)
        {
            var style = (Style)FindResource("BtnMenu");
            var styleActivo = (Style)FindResource("BtnMenuActivo");

            btnHistorial.Style = style;
            btnAdministrar.Style = style;
            btnEmpresa.Style = style;

            activo.Style = styleActivo;
        }
    }
}