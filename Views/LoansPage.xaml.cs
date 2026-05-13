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
using System.Threading;
using System.Threading.Tasks;

namespace Biblioteca.Views
{
    public sealed partial class LoansPage : Page
    {
        private List<Loan> _allLoans = new(); // todos sin filtrar
        private List<Loan> _loans = new(); // filtrados actualmente
        private Session _session;
        private CancellationTokenSource _toastCts;

        // Filtro activo
        private string _filtroEstado = "Todos";

        public LoansPage()
        {
            this.InitializeComponent();
            _session = SessionService.GetSession();
            RootGrid.Loaded += (s, e) =>
            {
                ApplyPermissions();
                LoadLoans();
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
            if (_session.Rol == "Lector")
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
        // CARGAR PRÉSTAMOS
        // =============================================
        private void LoadLoans()
        {
            var all = LoanService.GetAll()
                .OrderByDescending(x => x.Id)
                .ToList();

            _allLoans = _session?.Rol == "Lector"
                ? all.Where(x => x.UserId == _session.Id).ToList()
                : all;

            AplicarFiltros();
        }

        // =============================================
        // APLICAR FILTROS DE ESTADO + BÚSQUEDA
        // =============================================
        private void AplicarFiltros()
        {
            string busqueda = SearchBox?.Text?.ToLower().Trim() ?? "";

            var filtrados = _allLoans.AsEnumerable();

            // Filtro por estado
            filtrados = _filtroEstado switch
            {
                "Activos" => filtrados.Where(x => !x.Devuelto && DateTime.Parse(x.FechaDevolucion) >= DateTime.Today),
                "Vencidos" => filtrados.Where(x => !x.Devuelto && DateTime.Parse(x.FechaDevolucion) < DateTime.Today),
                "Devueltos" => filtrados.Where(x => x.Devuelto),
                "Historial" => filtrados.Where(x => x.Devuelto),
                _ => filtrados
            };

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(busqueda))
            {
                filtrados = filtrados.Where(x =>
                    x.Libro.ToLower().Contains(busqueda) ||
                    x.Usuario.ToLower().Contains(busqueda));
            }

            _loans = filtrados.ToList();

            LoansList.ItemsSource = null;
            LoansList.ItemsSource = _loans;

            string label = _filtroEstado == "Todos" ? "préstamo" : _filtroEstado.ToLower().TrimEnd('s');
            SubtitleText.Text = $"{_loans.Count} {(_filtroEstado == "Todos" ? $"préstamo{(_loans.Count != 1 ? "s" : "")} registrados" : $"préstamo{(_loans.Count != 1 ? "s" : "")} — {_filtroEstado.ToLower()}")}";

            // Actualizar botones de filtro
            ActualizarBotonesFiltro();
        }

        private void ActualizarBotonesFiltro()
        {
            var botones = new[]
            {
                (BtnFiltroTodos,     "Todos"),
                (BtnFiltroActivos,   "Activos"),
                (BtnFiltroVencidos,  "Vencidos"),
                (BtnFiltroDevueltos, "Devueltos"),
                (BtnFiltroHistorial, "Historial")
            };

            foreach (var (btn, nombre) in botones)
            {
                bool activo = _filtroEstado == nombre;
                btn.Background = activo
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(60, 124, 92, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(20, 255, 255, 255));
                btn.BorderBrush = activo
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(120, 124, 92, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 255, 255));
                if (btn.Content is TextBlock tb)
                    tb.Foreground = activo
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 197, 184, 255))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 139, 143, 196));
            }
        }

        // ── Eventos filtro ────────────────────────────
        private void FiltroTodos_Click(object s, RoutedEventArgs e) { _filtroEstado = "Todos"; AplicarFiltros(); }
        private void FiltroActivos_Click(object s, RoutedEventArgs e) { _filtroEstado = "Activos"; AplicarFiltros(); }
        private void FiltroVencidos_Click(object s, RoutedEventArgs e) { _filtroEstado = "Vencidos"; AplicarFiltros(); }
        private void FiltroDevueltos_Click(object s, RoutedEventArgs e) { _filtroEstado = "Devueltos"; AplicarFiltros(); }
        private void FiltroHistorial_Click(object s, RoutedEventArgs e) { _filtroEstado = "Historial"; AplicarFiltros(); }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => AplicarFiltros();

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
        // NUEVO PRÉSTAMO
        // =============================================
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            if (_session?.Rol == "Lector")
            { MostrarToast("No tienes permisos para registrar préstamos", "error"); return; }

            var books = BookService.GetAll()
                .Where(x => x.Activo && x.Disponibles > 0)
                .OrderBy(x => x.Titulo)
                .ToList();

            // Solo lectores activos SIN préstamos vencidos
            var allLoansSnap = LoanService.GetAll();
            var users = UserService.GetAll()
                .Where(x => x.Activo && x.Rol == "Lector")
                .OrderBy(x => x.Nombre)
                .ToList();

            if (books.Count == 0) { MostrarToast("No hay libros disponibles", "warning"); return; }
            if (users.Count == 0) { MostrarToast("No hay lectores activos", "warning"); return; }

            // ── AutoSuggestBox libro ──────────────────
            var txtBook = new AutoSuggestBox
            {
                PlaceholderText = "Buscar libro...",
                Height = 44,
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            EstilarAutoSuggest(txtBook);

            txtBook.TextChanged += (s, ev) =>
            {
                if (ev.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
                txtBook.ItemsSource = books
                    .Where(x => x.Titulo.ToLower().Contains(txtBook.Text.ToLower()))
                    .Select(x => $"{x.Titulo}  ({x.Disponibles} disp.)")
                    .ToList();
            };
            txtBook.SuggestionChosen += (s, ev) => txtBook.Text = ev.SelectedItem.ToString();

            // ── AutoSuggestBox usuario ────────────────
            var txtUser = new AutoSuggestBox
            {
                PlaceholderText = "Buscar lector...",
                Height = 44,
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            EstilarAutoSuggest(txtUser);

            txtUser.TextChanged += (s, ev) =>
            {
                if (ev.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
                txtUser.ItemsSource = users
                    .Where(x => x.Nombre.ToLower().Contains(txtUser.Text.ToLower()))
                    .Select(x => x.Nombre)
                    .ToList();
            };
            txtUser.SuggestionChosen += (s, ev) => txtUser.Text = ev.SelectedItem.ToString();

            // ── ComboBox días ─────────────────────────
            var cbDias = new ComboBox
            {
                Height = 44,
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Días de préstamo"
            };
            cbDias.Resources["ComboBoxBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 18, 40));
            cbDias.Resources["ComboBoxBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 22, 46));
            cbDias.Resources["ComboBoxBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 26, 52));
            cbDias.Resources["ComboBoxBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 44, 74));
            cbDias.Resources["ComboBoxBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 68, 112));
            cbDias.Resources["ComboBoxBorderBrushFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            cbDias.Resources["ComboBoxForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 236, 248));
            cbDias.Resources["ComboBoxDropDownBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 20, 43));
            cbDias.Resources["ComboBoxDropDownBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 124, 92, 255));
            cbDias.Items.Add("7 días");
            cbDias.Items.Add("15 días");
            cbDias.Items.Add("30 días");
            cbDias.SelectedIndex = 0;

            // Bloquear Enter en ComboBox
            cbDias.KeyDown += (s, ev) =>
            {
                if (ev.Key == Windows.System.VirtualKey.Enter)
                    ev.Handled = true;
            };

            var panel = new StackPanel { Spacing = 0, Width = 320 };

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
                Glyph = "\uE736",
                FontSize = 20,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var tituloLabel = new TextBlock
            {
                Text = "Nuevo préstamo",
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
                Text = "Registra un nuevo préstamo",
                FontSize = 12,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 90, 99, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24)
            });

            panel.Children.Add(CrearLabel("LIBRO *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = txtBook });
            panel.Children.Add(CrearLabel("LECTOR *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = txtUser });
            panel.Children.Add(CrearLabel("DURACIÓN"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 0), Child = cbDias });

            panel.Opacity = 0;
            var panelT = new TranslateTransform { Y = 20 };
            panel.RenderTransform = panelT;

            var scroll = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 500,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            ContentDialog dialog = null;

            // ── Acción guardar centralizada ───────────
            Func<Task> guardar = async () =>
            {
                var book = books.FirstOrDefault(x => txtBook.Text.TrimEnd().StartsWith(x.Titulo));
                var user = users.FirstOrDefault(x => x.Nombre == txtUser.Text.Trim());

                if (book == null) { MostrarToast("Selecciona un libro válido", "error"); return; }
                if (user == null) { MostrarToast("Selecciona un lector válido", "error"); return; }

                var currentLoans = LoanService.GetAll();
                var userLoans = currentLoans.Where(x => x.UserId == user.Id && !x.Devuelto).ToList();

                if (userLoans.Any(x => DateTime.Parse(x.FechaDevolucion) < DateTime.Today))
                { MostrarToast("El lector tiene préstamos vencidos", "warning"); return; }

                if (userLoans.Count >= 3)
                { MostrarToast("El lector alcanzó el máximo de 3 préstamos activos", "warning"); return; }

                int dias = cbDias.SelectedIndex switch { 1 => 15, 2 => 30, _ => 7 };

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

                var allBooks = BookService.GetAll();

                currentLoans.Add(new Loan
                {
                    Id = LoanService.NextId(),
                    BookId = book.Id,
                    UserId = user.Id,
                    Libro = book.Titulo,
                    Usuario = user.Nombre,
                    FechaPrestamo = DateTime.Now.ToString("yyyy-MM-dd"),
                    FechaDevolucion = DateTime.Now.AddDays(dias).ToString("yyyy-MM-dd"),
                    Devuelto = false
                });

                var realBook = allBooks.First(x => x.Id == book.Id);
                realBook.Disponibles--;

                LoanService.Save(currentLoans);
                BookService.Save(allBooks);
                DataService.SaveLog(_session.Usuario,
                    $"Registró préstamo: {book.Titulo} a {user.Nombre}");

                dialog?.Hide();
                LoadLoans();
                MostrarToast($"Préstamo registrado — {book.Titulo}", "success");
            };

            // Enter en los AutoSuggestBox guarda
            txtBook.KeyDown += async (s, ev) =>
            {
                if (ev.Key == Windows.System.VirtualKey.Enter)
                { ev.Handled = true; txtUser.Focus(FocusState.Programmatic); }
            };
            txtUser.KeyDown += async (s, ev) =>
            {
                if (ev.Key == Windows.System.VirtualKey.Enter)
                { ev.Handled = true; await guardar(); }
            };

            dialog = new ContentDialog
            {
                Content = scroll,
                PrimaryButtonText = "Registrar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,
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

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                args.Cancel = true;
                await guardar();
            };

            await dialog.ShowAsync();
        }

        // =============================================
        // DEVOLVER
        // =============================================
        private async void Return_Click(object sender, RoutedEventArgs e)
        {
            if (_session?.Rol == "Lector")
            { MostrarToast("No tienes permisos para devolver préstamos", "error"); return; }

            int id = (int)(sender as Button).Tag;
            var allLoans = LoanService.GetAll();
            var allBooks = BookService.GetAll();
            var loan = allLoans.FirstOrDefault(x => x.Id == id);

            if (loan == null || loan.Devuelto)
            { MostrarToast("Este préstamo ya fue devuelto", "info"); return; }

            var confirm = new ContentDialog
            {
                Title = "Confirmar devolución",
                Content = $"¿Confirmar devolución de \"{loan.Libro}\" de {loan.Usuario}?",
                PrimaryButtonText = "Confirmar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            EstilarDialog(confirm);

            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

            loan.Devuelto = true;

            var book = allBooks.FirstOrDefault(x => x.Id == loan.BookId);
            if (book != null) book.Disponibles++;

            LoanService.Save(allLoans);
            BookService.Save(allBooks);
            DataService.SaveLog(_session.Usuario,
                $"Devolvió préstamo | Libro: {loan.Libro} | Lector: {loan.Usuario}");

            LoadLoans();
            MostrarToast($"Devolución registrada — {loan.Libro}", "success");
        }

        // =============================================
        // ELIMINAR
        // =============================================
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_session?.Rol != "Administrador")
            { MostrarToast("Solo el administrador puede eliminar préstamos", "error"); return; }

            int id = (int)(sender as Button).Tag;
            var loan = _allLoans.FirstOrDefault(x => x.Id == id);
            if (loan == null) return;

            var confirm = new ContentDialog
            {
                Title = "Eliminar préstamo",
                Content = $"¿Eliminar el préstamo de \"{loan.Libro}\"? Esta acción no se puede deshacer.",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            EstilarDialog(confirm);

            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

            var allLoans = LoanService.GetAll();
            var allBooks = BookService.GetAll();
            var loanReal = allLoans.FirstOrDefault(x => x.Id == id);
            if (loanReal == null) return;

            if (!loanReal.Devuelto)
            {
                var book = allBooks.FirstOrDefault(x => x.Id == loanReal.BookId);
                if (book != null) book.Disponibles++;
            }

            allLoans.Remove(loanReal);
            LoanService.Save(allLoans);
            BookService.Save(allBooks);
            DataService.SaveLog(_session.Usuario,
                $"Eliminó préstamo | Libro: {loanReal.Libro} | Lector: {loanReal.Usuario}");

            LoadLoans();
            MostrarToast("Préstamo eliminado correctamente", "success");
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

        private void EstilarAutoSuggest(AutoSuggestBox box)
        {
            box.Resources["TextControlBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 18, 40));
            box.Resources["TextControlBackgroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 26, 52));
            box.Resources["TextControlBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 17, 22, 46));
            box.Resources["TextControlBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 44, 74));
            box.Resources["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 68, 112));
            box.Resources["TextControlBorderBrushFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            box.Resources["TextControlForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 236, 248));
            box.Resources["TextControlForegroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 247, 255));
            box.Resources["TextControlPlaceholderForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(85, 107, 122, 170));
            box.Resources["TextControlSelectionHighlightColor"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 124, 92, 255));
            box.Resources["AutoSuggestBoxSuggestionsListBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 20, 43));
            box.Resources["AutoSuggestBoxSuggestionsListBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 124, 92, 255));
            box.Resources["AutoSuggestBoxSuggestionsListBorderThickness"] = new Thickness(1);
            box.Resources["ListViewItemBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            box.Resources["ListViewItemBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 124, 92, 255));
            box.Resources["ListViewItemBackgroundSelected"] = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 124, 92, 255));
            box.Resources["ListViewItemForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 195, 255));
            box.Resources["ListViewItemForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            box.Resources["ListViewItemForegroundSelected"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
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

        // ── Helper botón filtro ───────────────────────
        private Button CrearBtnFiltro(string texto, RoutedEventHandler click)
        {
            var tb = new TextBlock
            {
                Text = texto,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                CharacterSpacing = 20
            };
            var btn = new Button
            {
                Content = tb,
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1)
            };
            btn.Resources["ButtonBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(20, 255, 255, 255));
            btn.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(35, 124, 92, 255));
            btn.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 124, 92, 255));
            btn.Resources["ButtonBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 255, 255));
            btn.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 124, 92, 255));
            btn.Click += click;
            return btn;
        }
    }
}