using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Biblioteca.Views
{
    public sealed partial class UsersPage : Page
    {
        private List<User> _users = new();
        private Session _session;
        private CancellationTokenSource _toastCts;

        // Roles válidos del sistema
        private static readonly string[] _roles = { "Administrador", "Lector" };

        // Validaciones
        private static readonly Regex _soloLetras = new(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]+$", RegexOptions.Compiled);
        private static readonly Regex _usuarioOk = new(@"^[a-zA-Z0-9._-]*$", RegexOptions.Compiled);
        private static readonly Regex _peligrosos = new(@"[<>{};/\\]", RegexOptions.Compiled);
        private static readonly Regex _emojis = new(@"[\u2600-\u27BF]|[\uD83C-\uDBFF\uDC00-\uDFFF]", RegexOptions.Compiled);
        private static readonly Regex _tieneUpper = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex _tieneLower = new(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex _tieneDigit = new(@"[0-9]", RegexOptions.Compiled);
        private static readonly Regex _tieneEspec = new(@"[!@#$%^&*()_+=\[\]|?.,~`]", RegexOptions.Compiled);

        private const int MAX_NOMBRE = 60;
        private const int MAX_USUARIO = 20;
        private const int MIN_PASS = 8;
        private const int MAX_PASS = 64;

        public UsersPage()
        {
            this.InitializeComponent();
            _session = SessionService.GetSession();
            RootGrid.Loaded += (s, e) =>
            {
                ApplyPermissions();
                LoadUsers();
                AnimarEntrada();
            };
        }

        private void AnimarEntrada()
        {
            RootGrid.Opacity = 0;
            var t = new TranslateTransform { Y = 16 };
            RootGrid.RenderTransform = t;

            var fade = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var slide = new DoubleAnimation { From = 16, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            var sb = new Storyboard();
            Storyboard.SetTarget(fade, RootGrid); Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(slide, t); Storyboard.SetTargetProperty(slide, "Y");
            sb.Children.Add(fade);
            sb.Children.Add(slide);
            sb.Begin();
        }

        private void ApplyPermissions()
        {
            if (_session == null) return;
            if (_session.Rol != "Administrador")
                AddButton.Visibility = Visibility.Collapsed;
        }

        // =============================================
        // TOAST
        // =============================================
        private async void MostrarToast(string mensaje, string tipo = "success")
        {
            _toastCts?.Cancel();
            _toastCts = new CancellationTokenSource();
            var token = _toastCts.Token;

            switch (tipo)
            {
                case "success":
                    ToastIcono.Glyph = "\uE73E";
                    ToastIcono.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 211, 150));
                    break;
                case "error":
                    ToastIcono.Glyph = "\uEA39";
                    ToastIcono.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 95, 122));
                    break;
                case "warning":
                    ToastIcono.Glyph = "\uE7BA";
                    ToastIcono.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 185, 50));
                    break;
                case "info":
                    ToastIcono.Glyph = "\uE946";
                    ToastIcono.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 211, 238));
                    break;
            }

            ToastTexto.Text = mensaje;
            ToastContainer.Opacity = 0;

            var t = new TranslateTransform { Y = 20 };
            ToastContainer.RenderTransform = t;

            var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(250)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var slideIn = new DoubleAnimation { From = 20, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(250)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            var sbIn = new Storyboard();
            Storyboard.SetTarget(fadeIn, ToastContainer); Storyboard.SetTargetProperty(fadeIn, "Opacity");
            Storyboard.SetTarget(slideIn, t); Storyboard.SetTargetProperty(slideIn, "Y");
            sbIn.Children.Add(fadeIn);
            sbIn.Children.Add(slideIn);
            sbIn.Begin();

            try { await Task.Delay(2800, token); }
            catch { return; }

            var fadeOut = new DoubleAnimation { From = 1, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(300)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
            var sbOut = new Storyboard();
            Storyboard.SetTarget(fadeOut, ToastContainer); Storyboard.SetTargetProperty(fadeOut, "Opacity");
            sbOut.Children.Add(fadeOut);
            sbOut.Begin();
        }

        // =============================================
        // CARGAR USUARIOS
        // =============================================
        private void LoadUsers()
        {
            _users = UserService.GetAll()
                .OrderBy(x => x.Nombre)
                .ToList();

            UsersList.ItemsSource = null;
            UsersList.ItemsSource = _users;

            SubtitleText.Text = $"{_users.Count} usuario{(_users.Count != 1 ? "s" : "")} registrados";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string f = SearchBox.Text.ToLower();
            UsersList.ItemsSource = _users
                .Where(x => x.Nombre.ToLower().Contains(f) ||
                            x.Usuario.ToLower().Contains(f))
                .ToList();
        }

        // =============================================
        // HOVER FILAS
        // =============================================
        private void Row_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border row)
                row.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(18, 124, 92, 255));
        }

        private void Row_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border row)
                row.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        }

        // =============================================
        // ACCIONES
        // =============================================
        private async void Add_Click(object sender, RoutedEventArgs e)
            => await ShowUserDialog(null);

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as Button).Tag;
            var user = _users.FirstOrDefault(x => x.Id == id);
            await ShowUserDialog(user);
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as Button).Tag;
            var all = UserService.GetAll();
            var u = all.FirstOrDefault(x => x.Id == id);
            if (u == null) return;

            u.Activo = !u.Activo;
            UserService.Save(all);
            LoadUsers();
            MostrarToast(u.Activo ? $"{u.Nombre} activado" : $"{u.Nombre} desactivado", "info");
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_session?.Rol != "Administrador")
            { MostrarToast("No tienes permisos para eliminar usuarios", "error"); return; }

            int id = (int)(sender as Button).Tag;

            if (_session.Id == id)
            { MostrarToast("No puedes eliminar tu propio usuario", "warning"); return; }

            var all = UserService.GetAll();
            var loans = LoanService.GetAll();
            var user = all.FirstOrDefault(x => x.Id == id);
            if (user == null) return;

            if (loans.Any(x => x.UserId == user.Id && !x.Devuelto))
            { MostrarToast("El usuario tiene préstamos activos", "error"); return; }

            var confirm = new ContentDialog
            {
                Title = "Eliminar usuario",
                Content = $"¿Eliminar a \"{user.Nombre}\"? Esta acción no se puede deshacer.",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,   // ← siempre this.XamlRoot
                RequestedTheme = ElementTheme.Dark
            };
            EstilarDialog(confirm);

            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

            all.Remove(user);
            UserService.Save(all);
            DataService.SaveLog(_session.Usuario, $"Eliminó usuario: {user.Usuario} ({user.Nombre})");

            LoadUsers();
            MostrarToast($"Usuario \"{user.Nombre}\" eliminado", "success");
        } 


        // =============================================
        // DIALOG AGREGAR / EDITAR
        // =============================================
        private async Task ShowUserDialog(User editUser)
        {
            var nombre = CrearTextBox("Nombre completo", MAX_NOMBRE);
            var usuario = CrearTextBox("Usuario", MAX_USUARIO);
            var pass = CrearPasswordBox("Contraseña", MAX_PASS);

            // ── Bloqueo en tiempo real ────────────────
            nombre.BeforeTextChanging += (s, args) =>
            {
                if (args.NewText.Length > MAX_NOMBRE ||
                    _peligrosos.IsMatch(args.NewText) ||
                    _emojis.IsMatch(args.NewText) ||
                    (!string.IsNullOrEmpty(args.NewText) && !_soloLetras.IsMatch(args.NewText)))
                    args.Cancel = true;
            };

            usuario.BeforeTextChanging += (s, args) =>
            {
                if (args.NewText.Length > MAX_USUARIO ||
                    args.NewText.Contains(" ") ||
                    _peligrosos.IsMatch(args.NewText) ||
                    _emojis.IsMatch(args.NewText) ||
                    (!string.IsNullOrEmpty(args.NewText) && !_usuarioOk.IsMatch(args.NewText)))
                    args.Cancel = true;
            };

            // ── ComboBox rol — solo 2 roles válidos ───
            var rol = new ComboBox
            {
                Height = 44,
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Seleccionar rol"
            };
            rol.Resources["ComboBoxBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 18, 40));
            rol.Resources["ComboBoxBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 22, 46));
            rol.Resources["ComboBoxBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 26, 52));
            rol.Resources["ComboBoxBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 44, 74));
            rol.Resources["ComboBoxBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 68, 112));
            rol.Resources["ComboBoxBorderBrushFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            rol.Resources["ComboBoxForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 236, 248));
            rol.Resources["ComboBoxDropDownBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 20, 43));
            rol.Resources["ComboBoxDropDownBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 124, 92, 255));

            foreach (var r in _roles) rol.Items.Add(r);

            if (editUser != null)
            {
                nombre.Text = editUser.Nombre;
                usuario.Text = editUser.Usuario;
                pass.Password = editUser.Password;
                // Solo asignar si el rol existe en la lista
                rol.SelectedItem = _roles.Contains(editUser.Rol) ? editUser.Rol : _roles[0];
            }
            else
            {
                rol.SelectedIndex = 0; // Administrador por defecto
            }

            // ── Acción guardar centralizada ───────────
            Func<Task> guardar = null;
            ContentDialog dialog = null;

            // ── Enter avanza / guarda ─────────────────
            // Bloquear Enter en ComboBox para que no abra dropdown
            rol.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    e.Handled = true; // ← evita que Enter abra el dropdown
                    guardar?.Invoke();
                }
            };

            nombre.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) usuario.Focus(FocusState.Programmatic); };
            usuario.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) pass.Focus(FocusState.Programmatic); };
            pass.KeyDown += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && guardar != null)
                    await guardar();
            };

            var panel = new StackPanel { Spacing = 0, Width = 320 };

            // Header
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
            logoBorde.Child = new FontIcon
            {
                Glyph = "\uE77B",
                FontSize = 20,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var tituloLabel = new TextBlock
            {
                Text = editUser == null ? "Agregar usuario" : "Editar usuario",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            tituloLabel.Foreground = new LinearGradientBrush
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
            panel.Children.Add(tituloLabel);
            panel.Children.Add(new TextBlock
            {
                Text = "Completa la información del usuario",
                FontSize = 12,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 90, 99, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24)
            });

            panel.Children.Add(CrearLabel("NOMBRE COMPLETO *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = nombre });
            panel.Children.Add(CrearLabel("USUARIO *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = usuario });
            panel.Children.Add(CrearLabel("CONTRASEÑA *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = pass });
            panel.Children.Add(CrearLabel("ROL *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 0), Child = rol });

            panel.Opacity = 0;
            var panelT = new TranslateTransform { Y = 20 };
            panel.RenderTransform = panelT;

            var scroll = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 480,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            dialog = new ContentDialog
            {
                Content = scroll,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,   // ← siempre this.XamlRoot
                RequestedTheme = ElementTheme.Dark
            };
            EstilarDialog(dialog);

            dialog.Opened += (s, _) =>
            {
                var fi = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var si = new DoubleAnimation { From = 20, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var sb = new Storyboard();
                Storyboard.SetTarget(fi, panel); Storyboard.SetTargetProperty(fi, "Opacity");
                Storyboard.SetTarget(si, panelT); Storyboard.SetTargetProperty(si, "Y");
                sb.Children.Add(fi); sb.Children.Add(si);
                sb.Begin();
            };

            // ── Lógica centralizada de guardado ───────
            guardar = async () =>
            {
                string nombreTxt = nombre.Text.Trim();
                string usuarioTxt = usuario.Text.Trim();
                string passTxt = pass.Password;

                // Nombre
                if (string.IsNullOrWhiteSpace(nombreTxt) || nombreTxt.Length < 3)
                { MostrarToast("Nombre inválido (mínimo 3 caracteres)", "error"); return; }
                if (!_soloLetras.IsMatch(nombreTxt))
                { MostrarToast("El nombre solo acepta letras y espacios", "error"); return; }

                // Usuario
                if (string.IsNullOrWhiteSpace(usuarioTxt) || usuarioTxt.Length < 4)
                { MostrarToast("Usuario inválido (mínimo 4 caracteres)", "error"); return; }
                if (!_usuarioOk.IsMatch(usuarioTxt))
                { MostrarToast("Usuario: solo letras, números, punto, guion", "error"); return; }

                // Contraseña
                if (passTxt.Length < MIN_PASS)
                { MostrarToast($"Contraseña mínimo {MIN_PASS} caracteres", "warning"); return; }
                if (!_tieneUpper.IsMatch(passTxt))
                { MostrarToast("Contraseña necesita al menos una mayúscula", "warning"); return; }
                if (!_tieneLower.IsMatch(passTxt))
                { MostrarToast("Contraseña necesita al menos una minúscula", "warning"); return; }
                if (!_tieneDigit.IsMatch(passTxt))
                { MostrarToast("Contraseña necesita al menos un número", "warning"); return; }
                if (!_tieneEspec.IsMatch(passTxt))
                { MostrarToast("Contraseña necesita al menos un carácter especial (!@#$...)", "warning"); return; }

                // Rol
                if (rol.SelectedItem == null)
                { MostrarToast("Selecciona un rol", "warning"); return; }

                var all = UserService.GetAll();

                if (all.Any(x => x.Usuario.ToLower() == usuarioTxt.ToLower() &&
                                 x.Id != (editUser?.Id ?? 0)))
                { MostrarToast("Ese usuario ya existe", "error"); return; }

                // Animación salida
                var st = new ScaleTransform();
                panel.RenderTransform = st;
                panel.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

                var fo = new DoubleAnimation { From = 1, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(250)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                var sox = new DoubleAnimation { From = 1, To = 0.95, Duration = new Duration(TimeSpan.FromMilliseconds(250)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                var soy = new DoubleAnimation { From = 1, To = 0.95, Duration = new Duration(TimeSpan.FromMilliseconds(250)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };

                var sbo = new Storyboard();
                Storyboard.SetTarget(fo, panel); Storyboard.SetTargetProperty(fo, "Opacity");
                Storyboard.SetTarget(sox, st); Storyboard.SetTargetProperty(sox, "ScaleX");
                Storyboard.SetTarget(soy, st); Storyboard.SetTargetProperty(soy, "ScaleY");
                sbo.Children.Add(fo); sbo.Children.Add(sox); sbo.Children.Add(soy);
                sbo.Begin();

                await Task.Delay(260);

                bool esNuevo = editUser == null;

                if (esNuevo)
                {
                    all.Add(new User
                    {
                        Id = UserService.NextId(),
                        Nombre = nombreTxt,
                        Usuario = usuarioTxt,
                        Correo = "",   // sin correo
                        Password = passTxt,
                        Rol = rol.SelectedItem.ToString(),
                        Activo = true,
                        IntentosFallidos = 0,
                        BloqueadoHasta = ""
                    });
                    DataService.SaveLog(_session?.Usuario ?? "sistema",
                        $"Creó usuario: {usuarioTxt}");
                }
                else
                {
                    var u = all.First(x => x.Id == editUser.Id);

                    u.Nombre = nombreTxt;
                    u.Usuario = usuarioTxt;
                    u.Password = passTxt;
                    u.Rol = rol.SelectedItem.ToString();

                    // Actualizar nombre en préstamos
                    var loans = LoanService.GetAll();

                    foreach (var loan in loans.Where(x => x.UserId == u.Id))
                    {
                        loan.Usuario = nombreTxt;
                    }

                    LoanService.Save(loans);

                    DataService.SaveLog(_session?.Usuario ?? "sistema",
                        $"Editó usuario: {usuarioTxt}");
                }


                UserService.Save(all);
                dialog.Hide();
                LoadUsers();

                MostrarToast(
                    esNuevo ? "Usuario creado correctamente" : "Usuario actualizado correctamente",
                    "success");
            };

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                args.Cancel = true;
                await guardar();
            };

            await dialog.ShowAsync();
        }

        // =============================================
        // HELPERS
        // =============================================
        private void EstilarDialog(ContentDialog d)
        {
            d.Resources["ContentDialogBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 20, 43));
            d.Resources["ContentDialogBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 255, 255));
            d.Resources["ContentDialogBorderThickness"] = new Thickness(1);
            d.Resources["ButtonBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            d.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 149, 112, 255));
            d.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 70, 224));
            d.Resources["ButtonForeground"] = new SolidColorBrush(Colors.White);
            d.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Colors.White);
            d.Resources["ButtonBorderBrush"] = new SolidColorBrush(Colors.Transparent);
            d.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Colors.Transparent);
        }

        private TextBox CrearTextBox(string placeholder, int maxLength = 100)
        {
            var tb = new TextBox
            {
                PlaceholderText = placeholder,
                Height = 44,
                MaxLength = maxLength,
                Padding = new Thickness(12, 0, 12, 0),
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
                Padding = new Thickness(12, 0, 12, 0),
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
    }
}