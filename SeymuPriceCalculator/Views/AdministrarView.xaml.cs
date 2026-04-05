using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SeymuPriceCalculator.Views
{
    public partial class AdministrarView : UserControl
    {
        private List<ClienteDB> _todosClientes = new();
        private ClienteDB? _clienteEditando = null;

        // Madera en edición
        private string _maderaEditando = "";

        // Grosor en edición
        private double _grosorEditando = -1;

        public AdministrarView()
        {
            InitializeComponent();
            CargarTodo();
        }

        private void CargarTodo()
        {
            CargarClientes();
            CargarMaderas();
            CargarGrosores();
        }

        // ═══════════════════════════════════════════════════════
        // CLIENTES
        // ═══════════════════════════════════════════════════════
        private void CargarClientes()
        {
            _todosClientes = DatabaseService.GetClientes();
            dgClientes.ItemsSource = _todosClientes.ToList();
        }

        private void FiltrarClientes()
        {
            string busq = txtBuscarCliente.Text.Trim().ToLower();
            dgClientes.ItemsSource = string.IsNullOrWhiteSpace(busq)
                ? _todosClientes.ToList()
                : _todosClientes
                    .Where(c => c.Nombre.ToLower().Contains(busq) ||
                                c.Telefono.Contains(busq))
                    .ToList();
        }

        private void AgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtClienteNombre.Text.Trim();
            string tel = txtClienteTelefono.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre es obligatorio.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClienteNombre.Focus();
                return;
            }

            DatabaseService.AgregarCliente(new ClienteDB { Nombre = nombre, Telefono = tel });
            LimpiarFormCliente();
            CargarClientes();
        }

        private void EditarCliente_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (b.Tag is not ClienteDB c) return;

            _clienteEditando = c;
            txtClienteNombre.Text = c.Nombre;
            txtClienteTelefono.Text = c.Telefono;

            btnAgregarCliente.Visibility = Visibility.Collapsed;
            btnGuardarCliente.Visibility = Visibility.Visible;
            btnCancelarCliente.Visibility = Visibility.Visible;

            txtClienteNombre.Focus();
        }

        private void GuardarCliente_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteEditando == null) return;

            string nombre = txtClienteNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre es obligatorio.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _clienteEditando.Nombre = nombre;
            _clienteEditando.Telefono = txtClienteTelefono.Text.Trim();

            DatabaseService.ActualizarCliente(_clienteEditando);
            LimpiarFormCliente();
            CargarClientes();
        }

        private void CancelarCliente_Click(object sender, RoutedEventArgs e)
            => LimpiarFormCliente();

        private void LimpiarFormCliente()
        {
            _clienteEditando = null;
            txtClienteNombre.Clear();
            txtClienteTelefono.Clear();
            btnAgregarCliente.Visibility = Visibility.Visible;
            btnGuardarCliente.Visibility = Visibility.Collapsed;
            btnCancelarCliente.Visibility = Visibility.Collapsed;
        }

        private void EliminarCliente_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (b.Tag is not ClienteDB c) return;

            if (MessageBox.Show($"¿Eliminar a {c.Nombre}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            DatabaseService.EliminarCliente(c.Id);
            CargarClientes();
        }

        private void txtBuscarCliente_TextChanged(object sender, TextChangedEventArgs e)
            => FiltrarClientes();

        // ═══════════════════════════════════════════════════════
        // TIPOS DE MADERA
        // ═══════════════════════════════════════════════════════
        private void CargarMaderas()
            => dgMaderas.ItemsSource = DatabaseService.GetTiposMadera();

        private void AgregarMadera_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNuevaMadera.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Ingrese un nombre de madera.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DatabaseService.AgregarTipoMadera(nombre);
            txtNuevaMadera.Clear();
            CargarMaderas();
            RefrescarMainWindow();
        }

        private void EditarMadera_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (b.Tag is not string nombre) return;

            _maderaEditando = nombre;
            txtNuevaMadera.Text = nombre;
            lblTituloMadera.Text = "EDITAR TIPO DE MADERA";

            btnAgregarMadera.Visibility = Visibility.Collapsed;
            panelBotonesMadera.Visibility = Visibility.Visible;

            txtNuevaMadera.Focus();
            txtNuevaMadera.SelectAll();
        }

        private void GuardarMadera_Click(object sender, RoutedEventArgs e)
        {
            string nuevo = txtNuevaMadera.Text.Trim();
            if (string.IsNullOrWhiteSpace(nuevo))
            {
                MessageBox.Show("El nombre no puede estar vacío.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (nuevo == _maderaEditando)
            {
                CancelarMadera_Click(sender, e);
                return;
            }

            // Eliminar el viejo e insertar el nuevo
            DatabaseService.EliminarTipoMadera(_maderaEditando);
            DatabaseService.AgregarTipoMadera(nuevo);

            LimpiarFormMadera();
            CargarMaderas();
            RefrescarMainWindow();
        }

        private void CancelarMadera_Click(object sender, RoutedEventArgs e)
            => LimpiarFormMadera();

        private void LimpiarFormMadera()
        {
            _maderaEditando = "";
            txtNuevaMadera.Clear();
            lblTituloMadera.Text = "AGREGAR TIPO DE MADERA";
            btnAgregarMadera.Visibility = Visibility.Visible;
            panelBotonesMadera.Visibility = Visibility.Collapsed;
        }

        private void EliminarMadera_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (b.Tag is not string nombre) return;

            if (MessageBox.Show($"¿Eliminar '{nombre}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            DatabaseService.EliminarTipoMadera(nombre);
            CargarMaderas();
            RefrescarMainWindow();
        }

        // ═══════════════════════════════════════════════════════
        // GROSORES
        // ═══════════════════════════════════════════════════════
        private void CargarGrosores()
            => dgGrosores.ItemsSource = DatabaseService.GetGrosores();

        private void AgregarGrosor_Click(object sender, RoutedEventArgs e)
        {
            string texto = txtNuevoGrosor.Text.Trim();

            if (!double.TryParse(texto, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double valor) || valor <= 0)
            {
                MessageBox.Show("Ingrese un valor numérico válido (ej: 1.5).",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DatabaseService.AgregarGrosor(valor);
            txtNuevoGrosor.Clear();
            CargarGrosores();
            RefrescarMainWindow();
        }

        private void EditarGrosor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (b.Tag is not double valor) return;

            _grosorEditando = valor;
            txtNuevoGrosor.Text = valor.ToString(CultureInfo.InvariantCulture);
            lblTituloGrosor.Text = "EDITAR GROSOR (PULGADAS)";

            btnAgregarGrosor.Visibility = Visibility.Collapsed;
            panelBotonesGrosor.Visibility = Visibility.Visible;

            txtNuevoGrosor.Focus();
            txtNuevoGrosor.SelectAll();
        }

        private void GuardarGrosor_Click(object sender, RoutedEventArgs e)
        {
            string texto = txtNuevoGrosor.Text.Trim();

            if (!double.TryParse(texto, NumberStyles.Any,
                CultureInfo.InvariantCulture, out double nuevo) || nuevo <= 0)
            {
                MessageBox.Show("Ingrese un valor numérico válido (ej: 1.5).",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (nuevo == _grosorEditando)
            {
                CancelarGrosor_Click(sender, e);
                return;
            }

            DatabaseService.EliminarGrosor(_grosorEditando);
            DatabaseService.AgregarGrosor(nuevo);

            LimpiarFormGrosor();
            CargarGrosores();
            RefrescarMainWindow();
        }

        private void CancelarGrosor_Click(object sender, RoutedEventArgs e)
            => LimpiarFormGrosor();

        private void LimpiarFormGrosor()
        {
            _grosorEditando = -1;
            txtNuevoGrosor.Clear();
            lblTituloGrosor.Text = "AGREGAR GROSOR (PULGADAS)";
            btnAgregarGrosor.Visibility = Visibility.Visible;
            panelBotonesGrosor.Visibility = Visibility.Collapsed;
        }

        private void EliminarGrosor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            if (b.Tag is not double valor) return;

            if (MessageBox.Show($"¿Eliminar grosor {valor}?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            DatabaseService.EliminarGrosor(valor);
            CargarGrosores();
            RefrescarMainWindow();
        }

        // ═══════════════════════════════════════════════════════
        // HELPER — refresca MainWindow en tiempo real
        // ═══════════════════════════════════════════════════════
        private void RefrescarMainWindow()
        {
            if (Application.Current.MainWindow is MainWindow mw)
                mw.CargarComboBoxes();
        }
    }
}