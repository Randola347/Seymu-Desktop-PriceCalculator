using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SeymuPriceCalculator.Views
{
    public partial class ConfiguracionWindow : Window
    {
        public ConfiguracionWindow()
        {
            InitializeComponent();
            ActualizarHeader();
            MostrarEmpresa_Click(this, null!);
        }

        // ── Actualiza sidebar desde DB ────────────────────────
        public void ActualizarHeader()
        {
            Empresa emp = DatabaseService.ObtenerEmpresa();

            lblNombreEmpresaConf.Text = string.IsNullOrWhiteSpace(emp.Nombre)
                                        ? "SEYMU"
                                        : emp.Nombre.ToUpper();

            txtLetraLogoConf.Text = emp.Nombre.Length > 0
                                    ? emp.Nombre[0].ToString().ToUpper()
                                    : "S";

            AplicarLogo(emp.LogoPath);
        }

        // ── Aplica una imagen al sidebar ──────────────────────
        private void AplicarLogo(string ruta)
        {
            if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    imgLogoConf.Source = bmp;
                    imgLogoConf.Visibility = Visibility.Visible;
                    txtLetraLogoConf.Visibility = Visibility.Collapsed;
                    return;
                }
                catch { }
            }

            imgLogoConf.Visibility = Visibility.Collapsed;
            txtLetraLogoConf.Visibility = Visibility.Visible;
        }

        // ── Recibe nombre en tiempo real desde EmpresaView ────
        public void ActualizarNombreEnVivo(string nombre)
        {
            lblNombreEmpresaConf.Text = string.IsNullOrWhiteSpace(nombre)
                                        ? "SEYMU"
                                        : nombre.ToUpper();

            txtLetraLogoConf.Text = nombre.Length > 0
                                    ? nombre.Trim()[0].ToString().ToUpper()
                                    : "S";
        }

        // ── Recibe logo en tiempo real desde EmpresaView ──────
        public void ActualizarLogoEnVivo(string rutaLogo)
            => AplicarLogo(rutaLogo);

        // ── Navegación ────────────────────────────────────────
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
            // ── Crea la vista y se suscribe a sus eventos ──────
            var vista = new EmpresaView();

            vista.NombreCambiado += (nombre) =>
                ActualizarNombreEnVivo(nombre);

            vista.LogoCambiado += (ruta) =>
            {
                ActualizarLogoEnVivo(ruta);

                // También actualiza MainWindow en tiempo real
                if (Application.Current.MainWindow is MainWindow mw)
                    mw.CargarDatosEmpresa();
            };

            ContenidoPrincipal.Content = vista;
            lblSeccionActual.Text = "🏢  Datos de la Empresa";
            ActualizarBotones(btnEmpresa);
        }

        private void MostrarImpresora_Click(object sender, RoutedEventArgs e)
        {
            ContenidoPrincipal.Content = new ImpresoraView();
            lblSeccionActual.Text = "🖨  Configuración de Impresora";
            ActualizarBotones(btnImpresora);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
            => Close();

        private void ActualizarBotones(Button activo)
        {
            var style = (Style)FindResource("BtnMenu");
            var styleActivo = (Style)FindResource("BtnMenuActivo");

            btnHistorial.Style = style;
            btnAdministrar.Style = style;
            btnEmpresa.Style = style;
            btnImpresora.Style = style;

            activo.Style = styleActivo;
        }
    }
}