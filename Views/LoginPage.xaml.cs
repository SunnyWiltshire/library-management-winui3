using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Biblioteca.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string user = UsernameBox.Text;
            string pass = PasswordBox.Password;

            if (user == "admin" && pass == "admin")
            {
                // Mensaje opcional
                ContentDialog ok = new ContentDialog()
                {
                    Title = "Acceso correcto",
                    Content = "Bienvenido administrador",
                    CloseButtonText = "Continuar",
                    XamlRoot = this.XamlRoot
                };

                await ok.ShowAsync();

                // Navegación (cuando tengas Dashboard)
                // MainWindow.AppFrame.Navigate(typeof(DashboardPage));
            }
            else
            {
                ContentDialog error = new ContentDialog()
                {
                    Title = "Error",
                    Content = "Credenciales incorrectas",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await error.ShowAsync();
            }
        }
    }
}
