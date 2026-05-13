using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;

namespace Biblioteca
{
    public sealed partial class LoginWindow : Window
    {
        private const string SUBTITULO = "Inicia sesión para continuar";
        private const int MAX_NOMBRE = 60;
        private const int MAX_USUARIO = 20;
        private const int MAX_CORREO = 80;
        private const int MAX_PASSWORD = 64;
        private const int MIN_PASSWORD = 8;
        private bool _loginEnProceso = false;
private bool _registroAbierto = false;

        // Caracteres peligrosos bloqueados globalmente
        private static readonly Regex _peligrosos =
            new Regex(@"[<>{};/\-\-\\]", RegexOptions.Compiled);

        // Solo letras, espacios y acentos válidos
        private static readonly Regex _soloLetras =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]+$", RegexOptions.Compiled);

        // Usuario: letras, números, punto, guion, guion bajo
        private static readonly Regex _usuarioValido =
            new Regex(@"^[a-zA-Z0-9._-]*$", RegexOptions.Compiled);

        // Correo básico
        private static readonly Regex _correoValido =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // Contraseña fuerte
        private static readonly Regex _tieneUpper = new Regex(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex _tieneLower = new Regex(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex _tieneDigit = new Regex(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex _tieneEspecial = new Regex(@"[!@#$%^&*()_+=\[\]|?.,~`]", RegexOptions.Compiled);
        private static readonly Regex _tieneEmoji = new Regex(
            @"[\u2600-\u27BF]|[\uD83C-\uDBFF\uDC00-\uDFFF]", RegexOptions.Compiled);

        private bool _entradaYaJugada = false;
        private bool _ventanaCerrada = false;

        public LoginWindow()
        {
            this.InitializeComponent();

            // ===== ÍCONO VENTANA =====
            SetWindowIcon();

            RootGrid.Loaded += LoginWindow_Loaded;
        }

        // =============================================
        // ICONO VENTANA
        // =============================================
        private void SetWindowIcon()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

                string iconPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets",
                    "App.ico");

                IntPtr hIcon = LoadImage(
                    IntPtr.Zero,
                    iconPath,
                    1,
                    32,
                    32,
                    0x0010
                );

                if (hIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, 0x0080, 0, hIcon);
                    SendMessage(hwnd, 0x0080, 1, hIcon);
                }
            }
            catch
            {
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
        // ANIMACIÓN ENTRADA
        // =============================================
        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_entradaYaJugada) return;
            _entradaYaJugada = true;

            TarjetaLogin.Opacity = 0;
            var transformTarjeta = new TranslateTransform();
            TarjetaLogin.RenderTransform = transformTarjeta;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(500)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var slideUp = new DoubleAnimation { From = 24, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(500)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            var sbEntrada = new Storyboard();
            Storyboard.SetTarget(fadeIn, TarjetaLogin); Storyboard.SetTargetProperty(fadeIn, "Opacity");
            Storyboard.SetTarget(slideUp, transformTarjeta); Storyboard.SetTargetProperty(slideUp, "Y");
            sbEntrada.Children.Add(fadeIn); sbEntrada.Children.Add(slideUp);
            sbEntrada.Begin();

            await Task.Delay(300);
            await EfectoTyping(SubtitleText, SUBTITULO, 38);

            await Task.Delay(100);
            await AnimarFadeSlide(LabelUsuario, delayMs: 0);
            await AnimarFadeSlide(GridUsuario, delayMs: 60);
            await AnimarFadeSlide(LabelPassword, delayMs: 60);
            await AnimarFadeSlide(GridPassword, delayMs: 60);
            await AnimarFadeSlide(GridBotonLogin, delayMs: 80);
            await AnimarFadeSlide(GridBotonRegister, delayMs: 60);

            IniciarShimmer();
        }

        private async Task EfectoTyping(TextBlock tb, string texto, int intervaloMs)
        {
            tb.Text = "";
            foreach (char c in texto)
            {
                if (_ventanaCerrada) return;
                try { tb.Text += c; }
                catch { return; }
                await Task.Delay(intervaloMs);
            }
        }

        private async Task AnimarFadeSlide(UIElement elemento, int delayMs)
        {
            if (delayMs > 0) await Task.Delay(delayMs);

            var t = new TranslateTransform();
            elemento.RenderTransform = t;

            var fade = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(320)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var slide = new DoubleAnimation { From = 12, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(320)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            var sb = new Storyboard();
            Storyboard.SetTarget(fade, elemento); Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(slide, t); Storyboard.SetTargetProperty(slide, "Y");
            sb.Children.Add(fade); sb.Children.Add(slide);
            sb.Begin();
        }

        private void IniciarShimmer()
        {
            var shimmer = new DoubleAnimation
            {
                From = -80,
                To = 460,
                Duration = new Duration(TimeSpan.FromMilliseconds(1800)),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            var sb = new Storyboard();
            Storyboard.SetTarget(shimmer, ShimmerTranslate);
            Storyboard.SetTargetProperty(shimmer, "X");
            sb.Children.Add(shimmer);
            sb.Begin();
        }

        // =============================================
        // LOGIN
        // =============================================
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string usuario = UsernameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Complete todos los campos";
                AnimarShakeYBorde(UsernameBox, string.IsNullOrWhiteSpace(usuario));
                AnimarShakeYBorde(PasswordBox, string.IsNullOrWhiteSpace(password));
                AnimarShake(ErrorText);
                return;
            }

            string resultado = AuthService.Login(usuario, password);
            if (resultado == "OK") TransicionSalida();
            else
            {
                ErrorText.Text = resultado;
                AnimarShakeYBorde(UsernameBox, true);
                AnimarShakeYBorde(PasswordBox, true);
                AnimarShake(ErrorText);
            }
        }

        private async void TransicionSalida()
        {
            _ventanaCerrada = true;
            var scaleT = new ScaleTransform();
            RootGrid.RenderTransform = scaleT;
            RootGrid.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

            var fadeOut = new DoubleAnimation { From = 1, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(400)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
            var scaleOut = new DoubleAnimation { From = 1, To = 1.06, Duration = new Duration(TimeSpan.FromMilliseconds(400)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
            var scaleOutY = new DoubleAnimation { From = 1, To = 1.06, Duration = new Duration(TimeSpan.FromMilliseconds(400)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };

            var sb = new Storyboard();
            Storyboard.SetTarget(fadeOut, RootGrid); Storyboard.SetTargetProperty(fadeOut, "Opacity");
            Storyboard.SetTarget(scaleOut, scaleT); Storyboard.SetTargetProperty(scaleOut, "ScaleX");
            Storyboard.SetTarget(scaleOutY, scaleT); Storyboard.SetTargetProperty(scaleOutY, "ScaleY");
            sb.Children.Add(fadeOut); sb.Children.Add(scaleOut); sb.Children.Add(scaleOutY);
            sb.Begin();

            await Task.Delay(420);

            MainWindow main = new MainWindow();
            App.CurrentWindow = main;
            main.Activate();
            this.Close();
        }

        // =============================================
        // ANIMACIONES
        // =============================================
        private async void AnimarShakeYBorde(Control campo, bool activar)
        {
            if (!activar) return;
            var rojo = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 80, 80));
            var normal = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 44, 74));
            var hover = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 68, 112));
            campo.Resources["TextControlBorderBrush"] = rojo;
            campo.Resources["TextControlBorderBrushPointerOver"] = rojo;
            AnimarShake(campo);
            await Task.Delay(1200);
            campo.Resources["TextControlBorderBrush"] = normal;
            campo.Resources["TextControlBorderBrushPointerOver"] = hover;
        }

        private void AnimarShake(UIElement elemento)
        {
            var transform = new TranslateTransform();
            elemento.RenderTransform = transform;
            var shake = new DoubleAnimationUsingKeyFrames();
            Storyboard.SetTarget(shake, transform);
            Storyboard.SetTargetProperty(shake, "X");
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(0), Value = 0 });
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(70), Value = -9 });
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(140), Value = 9 });
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(210), Value = -7 });
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(280), Value = 7 });
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(350), Value = -4 });
            shake.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(420), Value = 0 });
            var sb = new Storyboard();
            sb.Children.Add(shake);
            sb.Begin();
        }

        private void BtnRegister_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimarScale(ScaleBotonRegister, 1.02);
        private void BtnRegister_PointerExited(object sender, PointerRoutedEventArgs e) => AnimarScale(ScaleBotonRegister, 1.0);

        private void AnimarScale(ScaleTransform scale, double to)
        {
            var animX = new DoubleAnimation { To = to, Duration = new Duration(TimeSpan.FromMilliseconds(180)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var animY = new DoubleAnimation { To = to, Duration = new Duration(TimeSpan.FromMilliseconds(180)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var sb = new Storyboard();
            Storyboard.SetTarget(animX, scale); Storyboard.SetTargetProperty(animX, "ScaleX");
            Storyboard.SetTarget(animY, scale); Storyboard.SetTargetProperty(animY, "ScaleY");
            sb.Children.Add(animX); sb.Children.Add(animY);
            sb.Begin();
        }

        private void AnimarFadeIcono(FontIcon icon, double to, int duracionMs = 200)
        {
            var fade = new DoubleAnimation { To = to, Duration = new Duration(TimeSpan.FromMilliseconds(duracionMs)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var sb = new Storyboard();
            Storyboard.SetTarget(fade, icon); Storyboard.SetTargetProperty(fade, "Opacity");
            sb.Children.Add(fade); sb.Begin();
        }

        // =============================================
        // EVENTOS INPUT LOGIN
        // =============================================
        private void Input_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) Login_Click(sender, null);
        }

        private void UsernameBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // Bloquear espacios, emojis y caracteres no válidos
            if (args.NewText.Length > MAX_USUARIO ||
                args.NewText.Contains(" ") ||
                _tieneEmoji.IsMatch(args.NewText) ||
                _peligrosos.IsMatch(args.NewText) ||
                !_usuarioValido.IsMatch(args.NewText))
            {
                args.Cancel = true;
            }
        }

        private void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
            => UsernameBox.Text = UsernameBox.Text.Trim();

        // =============================================
        // ICONOS VALIDACIÓN
        // =============================================
        private void MostrarIcono(FontIcon icon, bool ok)
        {
            icon.Glyph = ok ? "\uE73E" : "\uEA39";
            icon.Foreground = ok
                ? new SolidColorBrush(Colors.LimeGreen)
                : new SolidColorBrush(Colors.IndianRed);
            icon.Opacity = 1;

            // Reset transform para que sea visible
            if (icon.RenderTransform is ScaleTransform s)
            { s.ScaleX = 1; s.ScaleY = 1; }
            else
                icon.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };

            AnimarFadeIcono(icon, 1, 200);
        }

        private void OcultarIcono(FontIcon icon)
        {
            AnimarFadeIcono(icon, 0, 150);
        }

        // =============================================
        // VALIDACIONES CENTRALIZADAS
        // =============================================
        private static string ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "El nombre es obligatorio";
            if (nombre.Length < 3) return "Mínimo 3 caracteres";
            if (nombre.Length > MAX_NOMBRE) return $"Máximo {MAX_NOMBRE} caracteres";
            if (!_soloLetras.IsMatch(nombre)) return "Solo letras, espacios y acentos";
            if (_peligrosos.IsMatch(nombre)) return "Caracteres no permitidos";
            return null;
        }

        private static string ValidarUsuario(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario)) return "El usuario es obligatorio";
            if (usuario.Length < 4) return "Mínimo 4 caracteres";
            if (usuario.Length > MAX_USUARIO) return $"Máximo {MAX_USUARIO} caracteres";
            if (usuario.Contains(" ")) return "Sin espacios";
            if (!_usuarioValido.IsMatch(usuario)) return "Solo letras, números, punto, guion";
            if (_peligrosos.IsMatch(usuario)) return "Caracteres no permitidos";
            if (_tieneEmoji.IsMatch(usuario)) return "No se permiten emojis";
            return null;
        }

        private static string ValidarCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return "El correo es obligatorio";
            if (correo.Length > MAX_CORREO) return $"Máximo {MAX_CORREO} caracteres";
            if (correo.Contains(" ")) return "Sin espacios";
            if (_peligrosos.IsMatch(correo)) return "Caracteres no permitidos";
            if (_tieneEmoji.IsMatch(correo)) return "No se permiten emojis";
            if (!_correoValido.IsMatch(correo)) return "Correo inválido";
            return null;
        }

        private static (bool ok, string mensaje, List<string> faltantes) ValidarPassword(string pass)
        {
            var faltantes = new List<string>();
            if (pass.Length < MIN_PASSWORD) faltantes.Add($"• mínimo {MIN_PASSWORD} caracteres");
            if (pass.Length > MAX_PASSWORD) return (false, $"Máximo {MAX_PASSWORD} caracteres", faltantes);
            if (!_tieneUpper.IsMatch(pass)) faltantes.Add("• una mayúscula");
            if (!_tieneLower.IsMatch(pass)) faltantes.Add("• una minúscula");
            if (!_tieneDigit.IsMatch(pass)) faltantes.Add("• un número");
            if (!_tieneEspecial.IsMatch(pass)) faltantes.Add("• un carácter especial (!@#$%...)");
            if (_tieneEmoji.IsMatch(pass)) faltantes.Add("• sin emojis");
            if (_peligrosos.IsMatch(pass)) faltantes.Add("• sin caracteres peligrosos");
            bool ok = faltantes.Count == 0;
            return (ok, ok ? "✔ Contraseña segura" : string.Join("  ", faltantes), faltantes);
        }

        // =============================================
        // REGISTRO
        // =============================================
        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            var txtNombre = CrearTextBox("Nombre completo", MAX_NOMBRE);
            var txtUsuario = CrearTextBox("Usuario", MAX_USUARIO);
            var txtPass = CrearPasswordBox("Contraseña", MAX_PASSWORD);
            var txtPass2 = CrearPasswordBox("Confirmar contraseña", MAX_PASSWORD);

            var iconNombre = CrearIcono();
            var iconUsuario = CrearIcono();
            var iconPass = CrearIcono();
            var iconPass2 = CrearIcono();

            var passInfo = new TextBlock
            {
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 0, 0, 0)
            };

            var error = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 95, 122)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            // ── Bloqueo en tiempo real ────────────────
            txtNombre.BeforeTextChanging += (s, args) =>
            {
                if (args.NewText.Length > MAX_NOMBRE ||
                    _peligrosos.IsMatch(args.NewText) ||
                    _tieneEmoji.IsMatch(args.NewText) ||
                    (!string.IsNullOrEmpty(args.NewText) && !_soloLetras.IsMatch(args.NewText)))
                    args.Cancel = true;
            };

            txtUsuario.BeforeTextChanging += (s, args) =>
            {
                if (args.NewText.Length > MAX_USUARIO ||
                    args.NewText.Contains(" ") ||
                    _peligrosos.IsMatch(args.NewText) ||
                    _tieneEmoji.IsMatch(args.NewText) ||
                    (!string.IsNullOrEmpty(args.NewText) && !_usuarioValido.IsMatch(args.NewText)))
                    args.Cancel = true;
            };

            // ── Validación visual en tiempo real ───────
            txtNombre.TextChanged += (s, _) =>
            {
                string n = txtNombre.Text;
                if (string.IsNullOrEmpty(n)) { OcultarIcono(iconNombre); return; }
                MostrarIcono(iconNombre, ValidarNombre(n) == null);
            };

            txtUsuario.TextChanged += (s, _) =>
            {
                string u = txtUsuario.Text;
                if (string.IsNullOrEmpty(u)) { OcultarIcono(iconUsuario); return; }
                MostrarIcono(iconUsuario, ValidarUsuario(u) == null);
            };

            txtPass.PasswordChanged += (s, _) =>
            {
                string p = txtPass.Password;
                if (string.IsNullOrEmpty(p)) { OcultarIcono(iconPass); passInfo.Text = ""; return; }
                var (ok, msg, _) = ValidarPassword(p);
                MostrarIcono(iconPass, ok);
                passInfo.Text = msg;
                passInfo.Foreground = ok
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 211, 150))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 180, 50));
            };

            txtPass2.PasswordChanged += (s, _) =>
            {
                string p2 = txtPass2.Password;
                if (string.IsNullOrEmpty(p2)) { OcultarIcono(iconPass2); return; }
                MostrarIcono(iconPass2, txtPass.Password == p2);
            };

            // ── Enter avanza ──────────────────────────
            txtNombre.KeyDown += (s, ev) => { if (ev.Key == Windows.System.VirtualKey.Enter) txtUsuario.Focus(FocusState.Programmatic); };
            txtUsuario.KeyDown += (s, ev) => { if (ev.Key == Windows.System.VirtualKey.Enter) txtPass.Focus(FocusState.Programmatic); };
            txtPass.KeyDown += (s, ev) => { if (ev.Key == Windows.System.VirtualKey.Enter) txtPass2.Focus(FocusState.Programmatic); };

            // ── Panel ────────────────────────────────
            var panel = new StackPanel { Spacing = 0, Width = 320, Opacity = 0 };
            var panelT = new TranslateTransform { Y = 20 };
            panel.RenderTransform = panelT;

            var logoBorde = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            logoBorde.Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1),
                GradientStops = new GradientStopCollection
        {
            new GradientStop { Color = Windows.UI.Color.FromArgb(255, 124, 92,  255), Offset = 0 },
            new GradientStop { Color = Windows.UI.Color.FromArgb(255, 34,  211, 238), Offset = 1 }
        }
            };
            logoBorde.Child = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/Logo.png")),
                Stretch = Stretch.Uniform
            };

            var tituloDialog = new TextBlock
            {
                Text = "Crear cuenta",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            tituloDialog.Foreground = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 0),
                GradientStops = new GradientStopCollection
        {
            new GradientStop { Color = Windows.UI.Color.FromArgb(255, 255, 255, 255), Offset = 0 },
            new GradientStop { Color = Windows.UI.Color.FromArgb(255, 197, 184, 255), Offset = 1 }
        }
            };

            panel.Children.Add(logoBorde);
            panel.Children.Add(tituloDialog);
            panel.Children.Add(new TextBlock
            {
                Text = "Completa tu información",
                FontSize = 12,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 90, 99, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24)
            });

            panel.Children.Add(CrearLabel("NOMBRE COMPLETO *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = CampoConIcono(txtNombre, iconNombre) });
            panel.Children.Add(CrearLabel("USUARIO *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = CampoConIcono(txtUsuario, iconUsuario) });
            panel.Children.Add(CrearLabel("CONTRASEÑA *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 6), Child = CampoConIcono(txtPass, iconPass) });
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = passInfo });
            panel.Children.Add(CrearLabel("CONFIRMAR CONTRASEÑA *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 16), Child = CampoConIcono(txtPass2, iconPass2) });
            panel.Children.Add(error);

            var scroll = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 480,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var dialog = new ContentDialog
            {
                Content = scroll,
                PrimaryButtonText = "Registrarme",
                CloseButtonText = "Cancelar",
                XamlRoot = ((FrameworkElement)this.Content).XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            dialog.Resources["ContentDialogBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 20, 43));
            dialog.Resources["ContentDialogBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 255, 255));
            dialog.Resources["ContentDialogBorderThickness"] = new Thickness(1);
            dialog.Resources["ButtonBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            dialog.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 149, 112, 255));
            dialog.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 70, 224));
            dialog.Resources["ButtonForeground"] = new SolidColorBrush(Colors.White);
            dialog.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Colors.White);
            dialog.Resources["ButtonBorderBrush"] = new SolidColorBrush(Colors.Transparent);
            dialog.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);

            dialog.Opened += (s, _) =>
            {
                var fi = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(380)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var si = new DoubleAnimation { From = 20, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(380)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var sb = new Storyboard();
                Storyboard.SetTarget(fi, panel); Storyboard.SetTargetProperty(fi, "Opacity");
                Storyboard.SetTarget(si, panelT); Storyboard.SetTargetProperty(si, "Y");
                sb.Children.Add(fi); sb.Children.Add(si);
                sb.Begin();
            };

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                args.Cancel = true;

                string nombre = txtNombre.Text.Trim();
                string usuario = txtUsuario.Text.Trim();
                string pass = txtPass.Password;
                string pass2 = txtPass2.Password;

                string errNombre = ValidarNombre(nombre);
                if (errNombre != null) { error.Text = $"⚠ {errNombre}"; AnimarShake(error); return; }

                string errUsuario = ValidarUsuario(usuario);
                if (errUsuario != null) { error.Text = $"⚠ {errUsuario}"; AnimarShake(error); return; }

                var (passOk, passMsg, _) = ValidarPassword(pass);
                if (!passOk) { error.Text = $"⚠ {passMsg}"; AnimarShake(error); return; }

                if (pass != pass2) { error.Text = "⚠ Las contraseñas no coinciden"; AnimarShake(error); return; }

                var users = UserService.GetAll();

                if (users.Any(x => x.Usuario.ToLower() == usuario.ToLower()))
                { error.Text = "⚠ Usuario ya existe"; AnimarShake(error); return; }

                error.Foreground = new SolidColorBrush(Colors.LightGreen);
                error.Text = "✔ Cuenta creada correctamente";

                users.Add(new User
                {
                    Id = UserService.NextId(),
                    Nombre = nombre,
                    Usuario = usuario,
                    Correo = "",
                    Password = pass,
                    Rol = "Administrador",
                    Activo = true,
                    IntentosFallidos = 0,
                    BloqueadoHasta = ""
                });
                UserService.Save(users);

                await Task.Delay(700);

                var scaleT = new ScaleTransform();
                panel.RenderTransform = scaleT;
                panel.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

                var fo = new DoubleAnimation { From = 1, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(300)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                var sox = new DoubleAnimation { From = 1, To = 0.94, Duration = new Duration(TimeSpan.FromMilliseconds(300)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                var soy = new DoubleAnimation { From = 1, To = 0.94, Duration = new Duration(TimeSpan.FromMilliseconds(300)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };

                var sbOut = new Storyboard();
                Storyboard.SetTarget(fo, panel); Storyboard.SetTargetProperty(fo, "Opacity");
                Storyboard.SetTarget(sox, scaleT); Storyboard.SetTargetProperty(sox, "ScaleX");
                Storyboard.SetTarget(soy, scaleT); Storyboard.SetTargetProperty(soy, "ScaleY");
                sbOut.Children.Add(fo); sbOut.Children.Add(sox); sbOut.Children.Add(soy);
                sbOut.Begin();

                await Task.Delay(320);
                dialog.Hide();

                UsernameBox.Text = usuario;
                PasswordBox.Password = "";
            };

            await dialog.ShowAsync();
        }

        // =============================================
        // HELPERS UI
        // =============================================
        private TextBox CrearTextBox(string placeholder, int maxLength = 100)
        {
            var tb = new TextBox
            {
                PlaceholderText = placeholder,
                Height = 44,
                MaxLength = maxLength,
                Padding = new Thickness(12, 0, 40, 0),
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            tb.Resources["TextControlBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 18, 40));
            tb.Resources["TextControlBackgroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 26, 52));
            tb.Resources["TextControlBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 22, 46));
            tb.Resources["TextControlBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 44, 74));
            tb.Resources["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 68, 112));
            tb.Resources["TextControlBorderBrushFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            tb.Resources["TextControlForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 236, 248));
            tb.Resources["TextControlForegroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 247, 255));
            tb.Resources["TextControlPlaceholderForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(85, 107, 122, 170));
            tb.Resources["TextControlPlaceholderForegroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(68, 107, 122, 170));
            tb.Resources["TextControlPlaceholderForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(85, 107, 122, 170));
            tb.Resources["TextControlSelectionHighlightColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            return tb;
        }

        private PasswordBox CrearPasswordBox(string placeholder, int maxLength = 64)
        {
            var pb = new PasswordBox
            {
                PlaceholderText = placeholder,
                Height = 44,
                MaxLength = maxLength,
                Padding = new Thickness(12, 0, 40, 0),
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            pb.Resources["TextControlBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 18, 40));
            pb.Resources["TextControlBackgroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 26, 52));
            pb.Resources["TextControlBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 22, 46));
            pb.Resources["TextControlBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 44, 74));
            pb.Resources["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 68, 112));
            pb.Resources["TextControlBorderBrushFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            pb.Resources["TextControlForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 236, 248));
            pb.Resources["TextControlForegroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 247, 255));
            pb.Resources["TextControlPlaceholderForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(85, 107, 122, 170));
            pb.Resources["TextControlPlaceholderForegroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(68, 107, 122, 170));
            pb.Resources["TextControlPlaceholderForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(85, 107, 122, 170));
            pb.Resources["TextControlSelectionHighlightColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            return pb;
        }

        private TextBlock CrearLabel(string texto) => new TextBlock
        {
            Text = texto,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 64, 72, 112)),
            CharacterSpacing = 80,
            Margin = new Thickness(2, 0, 0, 7)
        };

        private FontIcon CrearIcono() => new FontIcon
        {
            Opacity = 0,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 },
            IsHitTestVisible = false   // ← evita bloquear clicks
        };

        private Grid CampoConIcono(Control input, FontIcon icon)
        {
            var grid = new Grid();
            // Input primero, icono encima — el icono es IsHitTestVisible=false
            grid.Children.Add(input);
            grid.Children.Add(icon);
            return grid;
        }
    }
}