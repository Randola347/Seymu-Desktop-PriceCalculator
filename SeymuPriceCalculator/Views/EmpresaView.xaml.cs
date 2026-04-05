using Microsoft.Win32;
using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeymuPriceCalculator.Views
{
    public partial class EmpresaView : UserControl
    {
        public event Action<string>? NombreCambiado;
        public event Action<string>? LogoCambiado;

        private string _logoPath = "";

        public EmpresaView()
        {
            InitializeComponent();
            CargarDatos();

            txtNombre.TextChanged += TxtNombre_TextChanged;
            txtUbicacion.TextChanged += TxtUbicacion_TextChanged;
        }

        private void TxtNombre_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarPreviewTexto();
            NombreCambiado?.Invoke(txtNombre.Text.Trim());

            if (Application.Current.MainWindow is MainWindow mw)
                mw.CargarDatosEmpresa();

            if (Window.GetWindow(this) is ConfiguracionWindow cw)
                cw.ActualizarHeader();
        }

        private void TxtUbicacion_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarPreviewTexto();

            if (Window.GetWindow(this) is ConfiguracionWindow cw)
                cw.ActualizarHeader();
        }

        private void CargarDatos()
        {
            Empresa emp = DatabaseService.ObtenerEmpresa();

            txtNombre.Text = emp.Nombre ?? "";
            txtUbicacion.Text = emp.Ubicacion ?? "";
            txtTelefono.Text = emp.Telefono ?? "";
            txtCorreo.Text = emp.Correo ?? "";
            _logoPath = emp.LogoPath ?? "";

            if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
            {
                txtLogoRuta.Text = Path.GetFileName(_logoPath);
                txtLogoRuta.Foreground = Brushes.Black;
                MostrarPreviewLocal(_logoPath);
            }
            else
            {
                txtLogoRuta.Text = "Sin logo seleccionado";
                txtLogoRuta.Foreground = Brushes.Gray;
                imgLogoGrande.Source = null;
                imgLogoGrande.Visibility = Visibility.Collapsed;
                stackPlaceholder.Visibility = Visibility.Visible;
            }

            ActualizarPreviewTexto();

            NombreCambiado?.Invoke(txtNombre.Text.Trim());

            if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
                LogoCambiado?.Invoke(_logoPath);

            if (Application.Current.MainWindow is MainWindow mw)
                mw.CargarDatosEmpresa();

            if (Window.GetWindow(this) is ConfiguracionWindow cw)
                cw.ActualizarHeader();
        }

        private void SeleccionarLogo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar logo de la empresa",
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() != true)
                return;

            string carpeta = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "Assets");

            Directory.CreateDirectory(carpeta);

            string nombre = "logo_empresa" + Path.GetExtension(dialog.FileName);
            string destino = Path.Combine(carpeta, nombre);

            File.Copy(dialog.FileName, destino, true);

            _logoPath = destino;
            txtLogoRuta.Text = nombre;
            txtLogoRuta.Foreground = Brushes.Black;

            MostrarPreviewLocal(destino);

            LogoCambiado?.Invoke(destino);

            if (Application.Current.MainWindow is MainWindow mw)
                mw.CargarDatosEmpresa();

            if (Window.GetWindow(this) is ConfiguracionWindow cw)
                cw.ActualizarHeader();

            lblMensaje.Visibility = Visibility.Collapsed;
        }

        private void MostrarPreviewLocal(string ruta)
        {
            try
            {
                var bmp = CargarBitmap(ruta);
                imgLogoGrande.Source = bmp;
                imgLogoGrande.Visibility = Visibility.Visible;
                stackPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch
            {
                imgLogoGrande.Source = null;
                imgLogoGrande.Visibility = Visibility.Collapsed;
                stackPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void ActualizarPreviewTexto()
        {
            lblNombrePreview.Text =
                string.IsNullOrWhiteSpace(txtNombre.Text)
                ? "Nombre de la empresa"
                : txtNombre.Text.Trim().ToUpper();

            lblUbicacionPreview.Text =
                string.IsNullOrWhiteSpace(txtUbicacion.Text)
                ? ""
                : txtUbicacion.Text.Trim();
        }

        private BitmapImage CargarBitmap(string ruta)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(ruta, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private void GuardarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show(
                    "El nombre de la empresa es obligatorio.",
                    "Campo requerido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                txtNombre.Focus();
                return;
            }

            var empresa = new Empresa
            {
                Nombre = txtNombre.Text.Trim(),
                Ubicacion = txtUbicacion.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Correo = txtCorreo.Text.Trim(),
                LogoPath = _logoPath
            };

            DatabaseService.GuardarEmpresa(empresa);

            NombreCambiado?.Invoke(empresa.Nombre);

            if (!string.IsNullOrWhiteSpace(empresa.LogoPath) && File.Exists(empresa.LogoPath))
                LogoCambiado?.Invoke(empresa.LogoPath);

            if (Application.Current.MainWindow is MainWindow mw)
                mw.CargarDatosEmpresa();

            if (Window.GetWindow(this) is ConfiguracionWindow cw)
                cw.ActualizarHeader();

            if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
                MostrarPreviewLocal(_logoPath);

            lblMensaje.Visibility = Visibility.Visible;
        }
    }
}