using System;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System.Drawing;
using System.Linq;

namespace SeymuPriceCalculator.Views
{
    public partial class ImpresoraView : UserControl
    {
        public ImpresoraView()
        {
            InitializeComponent();
            CargarImpresoras();
        }

        private void CargarImpresoras()
        {
            cmbImpresoras.Items.Clear();
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                cmbImpresoras.Items.Add(p);
            }

            var emp = DatabaseService.ObtenerEmpresa();
            if (!string.IsNullOrEmpty(emp.Impresora) && cmbImpresoras.Items.Contains(emp.Impresora))
            {
                cmbImpresoras.SelectedItem = emp.Impresora;
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbImpresoras.SelectedItem == null)
            {
                MostrarStatus("Error: Selecciona una impresora primero.", false);
                return;
            }

            if (cmbImpresoras.SelectedItem is string seleccion)
            {
                var emp = DatabaseService.ObtenerEmpresa();
                emp.Impresora = seleccion;
                DatabaseService.GuardarEmpresa(emp);

                MostrarStatus("✔ Selección guardada correctamente.", true);
            }
        }

        private void Prueba_Click(object sender, RoutedEventArgs e)
        {
            if (cmbImpresoras.SelectedItem == null)
            {
                MostrarStatus("Error: Selecciona una impresora primero.", false);
                return;
            }

            if (cmbImpresoras.SelectedItem is not string impresora)
                return;
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = impresora;
                if (!pd.PrinterSettings.IsValid)
                {
                    MostrarStatus("Error: La impresora seleccionada no es válida.", false);
                    return;
                }

                pd.PrintPage += (s, ev) =>
                {
                    System.Drawing.Font f = new System.Drawing.Font("Consolas", 10, System.Drawing.FontStyle.Bold);
                    float y = 10;
                    ev.Graphics.DrawString("TEST DE IMPRESIÓN", f, System.Drawing.Brushes.Black, 10, y); y += 20;
                    ev.Graphics.DrawString("-------------------", f, System.Drawing.Brushes.Black, 10, y); y += 20;
                    ev.Graphics.DrawString("Impresora seleccionada:", f, System.Drawing.Brushes.Black, 10, y); y += 15;
                    ev.Graphics.DrawString(impresora, new System.Drawing.Font("Consolas", 8), System.Drawing.Brushes.Black, 10, y); y += 25;
                    ev.Graphics.DrawString("¡Funciona correctamente!", f, System.Drawing.Brushes.Black, 10, y);
                };

                pd.Print();
                MostrarStatus("✔ Impresión de prueba enviada.", true);
            }
            catch (Exception ex)
            {
                MostrarStatus("Error al imprimir: " + ex.Message, false);
            }
        }

        private void MostrarStatus(string msg, bool exito)
        {
            lblStatus.Text = msg;
            lblStatus.Foreground = exito 
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 106, 39)) 
                : System.Windows.Media.Brushes.Red;
            lblStatus.Visibility = Visibility.Visible;
        }
    }
}
