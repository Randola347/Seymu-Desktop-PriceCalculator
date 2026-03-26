using Microsoft.Win32;
using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SeymuPriceCalculator.Views
{
    public partial class EmpresaView : UserControl
    {
        // ── Ya no se necesita instancia de _db ────────────────
        private string _logoPath = "";

        public EmpresaView()
        {
            InitializeComponent();
            CargarDatos();
        }

        // ── Cargar datos existentes al abrir ──────────────────
        private void CargarDatos()
        {
            Empresa emp = DatabaseService.ObtenerEmpresa(); // ← estático

            txtNombre.Text = emp.Nombre;
            txtUbicacion.Text = emp.Ubicacion;
            txtTelefono.Text = emp.Telefono;
            txtCorreo.Text = emp.Correo;
            _logoPath = emp.LogoPath;

            if (!string.IsNullOrWhiteSpace(emp.LogoPath) && File.Exists(emp.LogoPath))
            {
                txtLogoRuta.Text = Path.GetFileName(emp.LogoPath);
                txtLogoRuta.Foreground = System.Windows.Media.Brushes.Black;
                MostrarPreview(emp.LogoPath);
            }
        }

        // ── Seleccionar imagen ────────────────────────────────
        private void SeleccionarLogo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar logo de la empresa",
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() != true) return;

            string carpeta = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Data", "Assets");
            Directory.CreateDirectory(carpeta);

            string nombre = "logo_empresa" + Path.GetExtension(dialog.FileName);
            string destino = Path.Combine(carpeta, nombre);

            File.Copy(dialog.FileName, destino, overwrite: true);

            _logoPath = destino;
            txtLogoRuta.Text = nombre;
            txtLogoRuta.Foreground = System.Windows.Media.Brushes.Black;

            MostrarPreview(destino);
            lblMensaje.Visibility = Visibility.Collapsed;
        }

        // ── Preview de imagen ─────────────────────────────────
        private void MostrarPreview(string ruta)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();

                imgLogo.Source = bmp;
                borderLogo.Visibility = Visibility.Visible;
            }
            catch
            {
                borderLogo.Visibility = Visibility.Collapsed;
            }
        }

        // ── Guardar ───────────────────────────────────────────
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

            DatabaseService.GuardarEmpresa(empresa); // ← estático

            // Actualizar header de MainWindow en tiempo real
            if (Application.Current.MainWindow is MainWindow mw)
                mw.CargarDatosEmpresa();

            lblMensaje.Visibility = Visibility.Visible;
        }
    }
}