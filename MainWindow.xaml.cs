using Biblioteca.Services;
using Biblioteca.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace Biblioteca
{
    public sealed partial class MainWindow : Window
    {
        private string currentRole = "";
        private Button _btnActivo = null;

        // Brush activo (violeta) y brush inactivo
        private readonly SolidColorBrush _fgActivo = new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        private readonly SolidColorBrush _bgActivo = new(Windows.UI.Color.FromArgb(40, 124, 92, 255));
        private readonly SolidColorBrush _fgInactivo = new(Windows.UI.Color.FromArgb(255, 106, 116, 153));
        private readonly SolidColorBrush _bgInactivo = new(Windows.UI.Color.FromArgb(0, 0, 0, 0));

        public MainWindow()
        {
            this.InitializeComponent();

            // ICONO
            SetWindowIcon();

            RootGrid.Loaded += RootGrid_Loaded;
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSession();
            AnimarEntrada();
        }

        // =============================================
        // ICONO VENTANA
        // =============================================
        private void SetWindowIcon()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            var iconPath = System.IO.Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "Assets",
                "App.ico");

            IntPtr hIcon = LoadImage(
                IntPtr.Zero,
                iconPath,
                1,      // IMAGE_ICON
                32,
                32,
                0x0010  // LR_LOADFROMFILE
            );

            if (hIcon != IntPtr.Zero)
            {
                SendMessage(hwnd, 0x0080, 0, hIcon); // ICON_SMALL
                SendMessage(hwnd, 0x0080, 1, hIcon); // ICON_BIG
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            int wParam,
            IntPtr lParam);

        [System.Runtime.InteropServices.DllImport(
            "user32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr LoadImage(
            IntPtr hInst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad);

        // =============================================
        // ENTRADA
        // =============================================
        private void AnimarEntrada()
        {
            RootGrid.Opacity = 0;

            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(450)),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var sb = new Storyboard();

            Storyboard.SetTarget(fade, RootGrid);
            Storyboard.SetTargetProperty(fade, "Opacity");

            sb.Children.Add(fade);
            sb.Begin();
        }

        // =============================================
        // SALIDA cinematográfica
        // =============================================
        private async void AnimarSalida(Action onComplete)
        {
            var scaleT = new ScaleTransform();

            RootGrid.RenderTransform = scaleT;
            RootGrid.RenderTransformOrigin =
                new Windows.Foundation.Point(0.5, 0.5);

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseIn
                }
            };

            var scaleX = new DoubleAnimation
            {
                From = 1,
                To = 1.04,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseIn
                }
            };

            var scaleY = new DoubleAnimation
            {
                From = 1,
                To = 1.04,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseIn
                }
            };

            var sb = new Storyboard();

            Storyboard.SetTarget(fadeOut, RootGrid);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");

            Storyboard.SetTarget(scaleX, scaleT);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");

            Storyboard.SetTarget(scaleY, scaleT);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");

            sb.Children.Add(fadeOut);
            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);

            sb.Begin();

            await Task.Delay(370);

            onComplete();
        }

        // =============================================
        // NAVEGACIÓN con animación
        // =============================================
        private void Navegar(Type pagina, Button btnOrigen = null)
        {
            // Actualizar estado visual del botón activo
            if (btnOrigen != null)
                SetBtnActivo(btnOrigen);

            ContentFrame.Opacity = 0;

            var transform = new TranslateTransform
            {
                Y = 16
            };

            ContentFrame.RenderTransform = transform;

            ContentFrame.Navigate(
                pagina,
                null,
                new SuppressNavigationTransitionInfo());

            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var slide = new DoubleAnimation
            {
                From = 16,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var sb = new Storyboard();

            Storyboard.SetTarget(fade, ContentFrame);
            Storyboard.SetTargetProperty(fade, "Opacity");

            Storyboard.SetTarget(slide, transform);
            Storyboard.SetTargetProperty(slide, "Y");

            sb.Children.Add(fade);
            sb.Children.Add(slide);

            sb.Begin();
        }

        // =============================================
        // ESTADO VISUAL botón activo
        // =============================================
        private void SetBtnActivo(Button btn)
        {
            // Desactivar el anterior
            if (_btnActivo != null)
            {
                _btnActivo.Resources["ButtonBackground"] = _bgInactivo;
                _btnActivo.Resources["ButtonForeground"] = _fgInactivo;
            }

            // Activar el nuevo
            btn.Resources["ButtonBackground"] = _bgActivo;
            btn.Resources["ButtonForeground"] = _fgActivo;

            _btnActivo = btn;
        }

        // =============================================
        // SESIÓN
        // =============================================
        private void LoadSession()
        {
            var session = SessionService.GetSession();

            if (session == null)
                return;

            currentRole = session.Rol;

            WelcomeText.Text = session.Usuario;
            RoleText.Text = session.Rol.ToUpper();

            ApplyPermissions(currentRole);
            OpenHomeByRole(currentRole);
        }

        private void ApplyPermissions(string rol)
        {
            if (rol == "Administrador")
                return;

            if (rol == "Bibliotecario")
            {
                BtnUsers.Visibility = Visibility.Collapsed;
                return;
            }

            if (rol == "Lector")
            {
                BtnDashboard.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                return;
            }

            if (rol == "Invitado")
            {
                BtnDashboard.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnLoans.Visibility = Visibility.Collapsed;
            }
        }

        private void OpenHomeByRole(string rol)
        {
            if (rol == "Administrador" || rol == "Bibliotecario")
            {
                Navegar(typeof(DashboardPage), BtnDashboard);
                return;
            }

            if (rol == "Lector")
            {
                Navegar(typeof(LoansPage), BtnLoans);
                return;
            }

            Navegar(typeof(BooksPage), BtnBooks);
        }

        // =============================================
        // CLICK en botones de navegación
        // =============================================
        private void NavBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (btn == null)
                return;

            string tag = btn.Tag?.ToString();

            switch (tag)
            {
                case "dashboard":

                    if (currentRole == "Administrador" ||
                        currentRole == "Bibliotecario")
                    {
                        Navegar(typeof(DashboardPage), btn);
                    }

                    break;

                case "books":

                    Navegar(typeof(BooksPage), btn);

                    break;

                case "users":

                    if (currentRole == "Administrador")
                    {
                        Navegar(typeof(UsersPage), btn);
                    }

                    break;

                case "loans":

                    if (currentRole != "Invitado")
                    {
                        Navegar(typeof(LoansPage), btn);
                    }

                    break;

                case "logout":

                    AnimarSalida(() =>
                    {
                        AuthService.Logout();

                        new LoginWindow().Activate();

                        this.Close();
                    });

                    break;
            }
        }
    }
}