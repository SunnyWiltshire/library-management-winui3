using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Biblioteca
{
    public sealed partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            this.InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string usuario = UsernameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            string confirmar = ConfirmPasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmar))
            {
                StatusText.Text = "Complete todos los campos";
                return;
            }

            if (password.Length < 4)
            {
                StatusText.Text = "La contraseña debe tener mínimo 4 caracteres";
                return;
            }

            if (password != confirmar)
            {
                StatusText.Text = "Las contraseñas no coinciden";
                return;
            }

            StatusText.Foreground =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Green);

            StatusText.Text = "Cuenta creada correctamente";

            this.Close();
        }
    }
}