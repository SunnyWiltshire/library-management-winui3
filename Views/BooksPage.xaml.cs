using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Biblioteca.Views
{
    public sealed partial class BooksPage : Page
    {
        private List<Book> _books = new();
        private List<Book> _allBooks = new();
        private Session _session;
        private CancellationTokenSource _toastCts;

        private const int MAX_TITULO = 40;
        private const int MAX_AUTOR = 40;
        private const int MAX_ISBN = 13;
        private const int MAX_CANTIDAD = 999;
        private const int MAX_ANIO_MIN = 1000;
        private const int MAX_ANIO_MAX = 2026;

        private static readonly Regex _soloDigitos =
            new Regex(@"^\d*$", RegexOptions.Compiled);

        private static readonly Regex _emojis =
            new Regex(@"[\u2600-\u27BF]|[\uD83C-\uDBFF\uDC00-\uDFFF]",
                      RegexOptions.Compiled);

        // Solo letras, espacios y acentos (para autor y categoría)
        private static readonly Regex _soloLetrasYEspacios =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s\.\,\-\']+$", RegexOptions.Compiled);

        // Categoría: letras, espacios, acentos y algunos signos (& y /)
        private static readonly Regex _categoriaValida =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s\&\/\-\.]+$", RegexOptions.Compiled);

        private static string Normalizar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            var norm = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in norm)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
        }

        private static readonly List<string> _categorias = new()
        {
            "Acción y Aventura", "Administración", "Agricultura", "Álgebra",
            "Anatomía", "Animales", "Antropología", "Arqueología",
            "Arquitectura", "Arte y Diseño", "Astronomía", "Autoayuda",
            "Bilingüe", "Biografía", "Biología", "Botánica",
            "Cálculo", "Ciencia Ficción", "Ciencias Naturales",
            "Ciencias Políticas", "Clásicos", "Cocina y Gastronomía",
            "Computación", "Comunicación", "Contabilidad",
            "Crecimiento Personal", "Crimen y Misterio", "Cuentos Infantiles",
            "Datos", "Derecho", "Desarrollo Personal", "Deportes", "Distopía",
            "Ecología", "Economía", "Educación", "Electrónica",
            "Emprendimiento", "Ensayo", "Entretenimiento", "Épica", "Estadística",
            "Fantasía", "Filosofía", "Física", "Fotografía", "Finanzas Personales",
            "Geografía", "Geología", "Gestión de Proyectos",
            "Historia", "Horror", "Humor",
            "Idiomas", "Ingeniería", "Inteligencia Artificial", "Investigación Científica",
            "Jardínería", "Juegos y Pasatiempos",
            "Literatura", "Liderazgo", "Lingüística", "Lógica",
            "Matemáticas", "Mecánica", "Medicina", "Meditación",
            "Mercadotecnia", "Mitología", "Música",
            "Negocios", "Neurociencia", "Novela",
            "Oceanografía",
            "Pediatría", "Poesía", "Programación", "Psicología",
            "Química",
            "Redes", "Religión", "Robótica", "Romance",
            "Salud", "Seguridad Informática", "Sociología",
            "Teatro", "Tecnología", "Terror", "Thriller",
            "Urbanismo", "Viajes", "Zoología"
        };

        public BooksPage()
        {
            this.InitializeComponent();
            _session = SessionService.GetSession();
            RootGrid.Loaded += (s, e) =>
            {
                ApplyPermissions();
                LoadBooks();
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
        // CARGAR LIBROS
        // =============================================
        private async void LoadBooks()
        {
            _allBooks = BookService.GetAll()
                .Where(x => x.Activo)
                .OrderBy(x => x.Titulo)
                .ToList();

            _books = _allBooks;

            BooksGrid.ItemsSource = null;
            BooksGrid.ItemsSource = _books;

            SubtitleText.Text = $"{_books.Count} libro{(_books.Count != 1 ? "s" : "")} en el catálogo";

            await Task.Delay(150);
            AplicarPortadas(_books);
        }

        private void AplicarPortadas(List<Book> lista)
        {
            foreach (var book in lista)
            {
                if (string.IsNullOrWhiteSpace(book.CoverUrl)) continue;

                var container = BooksGrid.ContainerFromItem(book)
                    as Microsoft.UI.Xaml.Controls.GridViewItem;
                if (container == null) continue;

                var imgReal = EncontrarHijo<Image>(container, "ImgPortada");
                var fallback = EncontrarHijo<Border>(container, "FallbackCover");

                if (imgReal == null || fallback == null) continue;

                try
                {
                    string uri = book.CoverUrl.StartsWith("http")
                        ? book.CoverUrl
                        : "file:///" + book.CoverUrl.Replace("\\", "/");

                    imgReal.Source = new BitmapImage(new Uri(uri));
                    imgReal.Visibility = Visibility.Visible;
                    fallback.Visibility = Visibility.Collapsed;
                }
                catch { }
            }
        }

        // =============================================
        // BÚSQUEDA
        // =============================================
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text.Length > 42)
            {
                SearchBox.Text = SearchBox.Text.Substring(0, 42);
                SearchBox.SelectionStart = SearchBox.Text.Length;
            }

            string f = SearchBox.Text.ToLower().Trim();

            List<Book> filtrados = string.IsNullOrEmpty(f)
                ? _allBooks
                : _allBooks
                    .Where(x => x.Titulo.ToLower().Contains(f) ||
                                x.Autor.ToLower().Contains(f))
                    .ToList();

            _books = filtrados;

            BooksGrid.ItemsSource = null;
            BooksGrid.ItemsSource = filtrados;

            SubtitleText.Text = string.IsNullOrEmpty(f)
                ? $"{filtrados.Count} libro{(filtrados.Count != 1 ? "s" : "")} en el catálogo"
                : $"{filtrados.Count} resultado{(filtrados.Count != 1 ? "s" : "")}";

            await Task.Delay(150);
            AplicarPortadas(filtrados);
        }

        // =============================================
        // HOVER — ocultar acciones para Lector
        // =============================================
        private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Border card) return;

            // Lector: solo escala, sin overlay de acciones
            if (_session?.Rol == "Lector")
            {
                AnimarCardScale(card, 1.04);
                return;
            }

            var overlay = EncontrarHijo<Border>(card, "HoverOverlay");
            if (overlay != null) AnimarOverlay(overlay, true);
            AnimarCardScale(card, 1.04);
        }

        private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Border card) return;

            if (_session?.Rol == "Lector")
            {
                AnimarCardScale(card, 1.0);
                return;
            }

            var overlay = EncontrarHijo<Border>(card, "HoverOverlay");
            if (overlay != null) AnimarOverlay(overlay, false);
            AnimarCardScale(card, 1.0);
        }

        private void AnimarOverlay(Border overlay, bool mostrar)
        {
            var fade = new DoubleAnimation
            {
                To = mostrar ? 1 : 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var sb = new Storyboard();
            Storyboard.SetTarget(fade, overlay);
            Storyboard.SetTargetProperty(fade, "Opacity");
            sb.Children.Add(fade);
            sb.Begin();
        }

        private void AnimarCardScale(Border card, double to)
        {
            if (card.RenderTransform is not ScaleTransform scale)
            {
                scale = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
                card.RenderTransform = scale;
                card.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            }

            var animX = new DoubleAnimation { To = to, Duration = new Duration(TimeSpan.FromMilliseconds(200)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var animY = new DoubleAnimation { To = to, Duration = new Duration(TimeSpan.FromMilliseconds(200)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            var sb = new Storyboard();
            Storyboard.SetTarget(animX, scale); Storyboard.SetTargetProperty(animX, "ScaleX");
            Storyboard.SetTarget(animY, scale); Storyboard.SetTargetProperty(animY, "ScaleY");
            sb.Children.Add(animX);
            sb.Children.Add(animY);
            sb.Begin();
        }

        private T EncontrarHijo<T>(DependencyObject parent, string name)
            where T : FrameworkElement
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && (string.IsNullOrEmpty(name) || fe.Name == name)) return fe;
                var result = EncontrarHijo<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        // =============================================
        // ACCIONES
        // =============================================
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit()) { await Msg("No tienes permisos."); return; }
            await ShowBookDialog(null);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit()) { await Msg("No tienes permisos."); return; }
            int id = (int)(sender as Button).Tag;
            var book = _allBooks.FirstOrDefault(x => x.Id == id);
            await ShowBookDialog(book);
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit()) { await Msg("No tienes permisos."); return; }

            int id = (int)(sender as Button).Tag;
            var all = BookService.GetAll();
            var book = all.FirstOrDefault(x => x.Id == id);
            if (book == null) return;

            int prestamosActivos = book.CantidadTotal - book.Disponibles;
            if (prestamosActivos > 0)
            {
                MostrarToast(
                    $"No puedes eliminar este libro — tiene {prestamosActivos} préstamo(s) activo(s)",
                    "error");
                return;
            }

            var confirm = new ContentDialog
            {
                Title = "Eliminar libro",
                Content = $"¿Eliminar \"{book.Titulo}\"?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            EstilarDialog(confirm);

            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

            book.Activo = false;
            BookService.Save(all);
            DataService.SaveLog(_session.Usuario,
                $"Eliminó libro | Título: {book.Titulo} | Autor: {book.Autor}");

            LoadBooks();
            MostrarToast("Libro eliminado correctamente", "success");
        }

        private bool CanEdit() =>
            _session != null &&
            (_session.Rol == "Administrador" || _session.Rol == "Bibliotecario");

        // =============================================
        // FILE PICKER
        // =============================================
        private async Task<string> SeleccionarPortada()
        {
            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".webp");

                var file = await picker.PickSingleFileAsync();
                if (file == null) return null;

                string coversPath = Path.Combine(DataService.BasePath, "Covers");
                Directory.CreateDirectory(coversPath);

                string ext = Path.GetExtension(file.Name);
                string nombre = $"cover_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                string destino = Path.Combine(coversPath, nombre);

                File.Copy(file.Path, destino, overwrite: true);
                return destino;
            }
            catch { return null; }
        }

        // =============================================
        // DIALOG AGREGAR / EDITAR
        // =============================================
        private async Task ShowBookDialog(Book editBook)
        {
            var titulo = CrearTextBox("Título", MAX_TITULO);
            var autor = CrearTextBox("Autor", MAX_AUTOR);
            var isbn = CrearTextBox("ISBN", MAX_ISBN);
            var anio = CrearTextBox("Año", 4);
            var cantidad = CrearTextBox("Cantidad", 4);

            // ── Bloqueo ISBN: solo dígitos ────────────
            isbn.BeforeTextChanging += (s, args) =>
            {
                if (!_soloDigitos.IsMatch(args.NewText) || args.NewText.Length > MAX_ISBN)
                    args.Cancel = true;
            };

            // ── Bloqueo Año: solo dígitos ─────────────
            anio.BeforeTextChanging += (s, args) =>
            {
                if (!_soloDigitos.IsMatch(args.NewText) || args.NewText.Length > 4)
                    args.Cancel = true;
            };

            // ── Bloqueo Cantidad: solo dígitos ────────
            cantidad.BeforeTextChanging += (s, args) =>
            {
                if (!_soloDigitos.IsMatch(args.NewText) || args.NewText.Length > 4)
                    args.Cancel = true;
            };

            // ── Bloqueo Autor: solo letras ────────────
            autor.BeforeTextChanging += (s, args) =>
            {
                if (string.IsNullOrEmpty(args.NewText)) return;
                if (_emojis.IsMatch(args.NewText) ||
                    !_soloLetrasYEspacios.IsMatch(args.NewText))
                    args.Cancel = true;
            };

            // ── AutoSuggestBox categoría ─────────────
            var categoria = new AutoSuggestBox
            {
                PlaceholderText = "Categoría *",
                Height = 44,
                FontSize = 13,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            EstilarAutoSuggest(categoria);

            bool _agregandoCategoria = false;
            string _categoriaAgregar = "";

            // Mostrar lista completa al recibir foco (click en el campo)
            categoria.GotFocus += (s, ev) =>
            {
                string filtroActual = Normalizar(categoria.Text);
                var lista = string.IsNullOrWhiteSpace(categoria.Text)
                    ? _categorias.Cast<object>().ToList()
                    : _categorias
                        .Where(c => Normalizar(c).Contains(filtroActual))
                        .Cast<object>()
                        .ToList();

                categoria.ItemsSource = lista.Count > 0 ? lista : null;
            };

            // Cerrar lista al perder foco
            categoria.LostFocus += (s, ev) =>
            {
                // Pequeño delay para permitir selección antes de cerrar
                _ = Task.Delay(200).ContinueWith(_ =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        categoria.ItemsSource = null;

                        // Normalizar texto si coincide con una categoría existente
                        string inputNorm = Normalizar(categoria.Text);
                        var match = _categorias.FirstOrDefault(c => Normalizar(c) == inputNorm);
                        if (match != null) categoria.Text = match;
                    });
                });
            };

            categoria.TextChanged += (s, args) =>
            {
                if (_agregandoCategoria)
                {
                    _agregandoCategoria = false;
                    categoria.Text = _categoriaAgregar;
                    categoria.ItemsSource = null;
                    return;
                }

                if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

                string filtro = categoria.Text.Trim();

                // Bloquear números, emojis y símbolos inválidos
                if (!string.IsNullOrEmpty(filtro))
                {
                    if (_emojis.IsMatch(filtro) || !_categoriaValida.IsMatch(filtro))
                    {
                        // Quitar el último carácter inválido
                        categoria.Text = filtro.Length > 1
                            ? filtro.Substring(0, filtro.Length - 1)
                            : "";
                        categoria.ItemsSource = null;
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(filtro))
                {
                    // Si está vacío mostrar lista completa
                    categoria.ItemsSource = _categorias.Cast<object>().ToList();
                    return;
                }

                string filtroNorm = Normalizar(filtro);
                var coincidencias = _categorias
                    .Where(c => Normalizar(c).Contains(filtroNorm))
                    .ToList<object>();

                bool yaExiste = _categorias.Any(c => Normalizar(c) == filtroNorm);
                if (!yaExiste && _session?.Rol == "Administrador")
                    coincidencias.Add($"➕ Agregar \"{filtro}\"");

                categoria.ItemsSource = coincidencias.Count > 0 ? coincidencias : null;
            };

            categoria.SuggestionChosen += (s, args) =>
            {
                string elegido = args.SelectedItem?.ToString() ?? "";

                if (elegido.StartsWith("➕ Agregar "))
                {
                    string nueva = elegido.Substring("➕ Agregar \"".Length).TrimEnd('"');
                    string nuevaNorm = Normalizar(nueva);
                    if (!_categorias.Any(c => Normalizar(c) == nuevaNorm))
                    {
                        _categorias.Add(nueva);
                        _categorias.Sort();
                        MostrarToast($"Categoría \"{nueva}\" agregada", "info");
                    }
                    _agregandoCategoria = true;
                    _categoriaAgregar = nueva;
                }
                else
                {
                    categoria.Text = elegido;
                }
            };

            Func<Task> guardar = null;
            ContentDialog dialog = null;

            titulo.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) autor.Focus(FocusState.Programmatic); };
            autor.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) categoria.Focus(FocusState.Programmatic); };
            categoria.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) isbn.Focus(FocusState.Programmatic); };
            isbn.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) anio.Focus(FocusState.Programmatic); };
            anio.KeyDown += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) cantidad.Focus(FocusState.Programmatic); };
            cantidad.KeyDown += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && guardar != null)
                    await guardar();
            };

            string coverSeleccionada = editBook?.CoverUrl ?? "";

            var imgPreview = new Image
            {
                Height = 120,
                Width = 80,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };

            var fallbackPreview = new Border
            {
                Height = 120,
                Width = 80,
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            fallbackPreview.Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 61,  31,  140), Offset = 0 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 124, 92,  255), Offset = 1 }
                }
            };
            fallbackPreview.Child = new FontIcon
            {
                Glyph = "\uEB9F",
                FontSize = 32,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            void ActualizarPreview(string path)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    try
                    {
                        string uri = path.StartsWith("http")
                            ? path
                            : "file:///" + path.Replace("\\", "/");
                        imgPreview.Source = new BitmapImage(new Uri(uri));
                        imgPreview.Visibility = Visibility.Visible;
                        fallbackPreview.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        imgPreview.Visibility = Visibility.Collapsed;
                        fallbackPreview.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    imgPreview.Visibility = Visibility.Collapsed;
                    fallbackPreview.Visibility = Visibility.Visible;
                }
            }

            if (editBook != null)
            {
                titulo.Text = editBook.Titulo;
                autor.Text = editBook.Autor;
                categoria.Text = editBook.Categoria;
                isbn.Text = editBook.ISBN;
                anio.Text = editBook.Anio.ToString();
                cantidad.Text = editBook.CantidadTotal.ToString();
            }

            ActualizarPreview(coverSeleccionada);

            var iconoBtn = new FontIcon { Glyph = "\uEB9F", FontSize = 14, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 197, 184, 255)) };
            var textoBtn = new TextBlock { Text = "Seleccionar portada", FontSize = 13 };
            var btnContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            btnContent.Children.Add(iconoBtn);
            btnContent.Children.Add(textoBtn);

            var btnPortada = new Button
            {
                Content = btnContent,
                Height = 40,
                Padding = new Thickness(16, 0, 16, 0),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnPortada.Resources["ButtonBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 124, 92, 255));
            btnPortada.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 124, 92, 255));
            btnPortada.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(25, 124, 92, 255));
            btnPortada.Resources["ButtonBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 124, 92, 255));
            btnPortada.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 124, 92, 255));
            btnPortada.Resources["ButtonForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 197, 184, 255));
            btnPortada.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 210, 255));

            var btnQuitar = new Button
            {
                Height = 40,
                Padding = new Thickness(12, 0, 12, 0),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = string.IsNullOrWhiteSpace(coverSeleccionada) ? Visibility.Collapsed : Visibility.Visible
            };
            btnQuitar.Resources["ButtonBackground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 95, 122));
            btnQuitar.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 255, 95, 122));
            btnQuitar.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(25, 255, 95, 122));
            btnQuitar.Resources["ButtonBorderBrush"] = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 95, 122));
            btnQuitar.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 255, 95, 122));
            btnQuitar.Resources["ButtonForeground"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 95, 122));
            btnQuitar.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 140, 155));

            var quitarContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            quitarContent.Children.Add(new FontIcon { Glyph = "\uE74D", FontSize = 13, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 95, 122)) });
            quitarContent.Children.Add(new TextBlock { Text = "Quitar", FontSize = 13 });
            btnQuitar.Content = quitarContent;

            var filaBotones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            filaBotones.Children.Add(btnPortada);
            filaBotones.Children.Add(btnQuitar);

            btnPortada.Click += async (s, _) =>
            {
                string ruta = await SeleccionarPortada();
                if (!string.IsNullOrWhiteSpace(ruta))
                {
                    coverSeleccionada = ruta;
                    ActualizarPreview(ruta);
                    iconoBtn.Glyph = "\uE73E";
                    iconoBtn.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 211, 150));
                    textoBtn.Text = "Portada seleccionada ✔";
                    textoBtn.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 211, 150));
                    btnQuitar.Visibility = Visibility.Visible;
                }
            };

            btnQuitar.Click += (s, _) =>
            {
                coverSeleccionada = "";
                ActualizarPreview("");
                iconoBtn.Glyph = "\uEB9F";
                iconoBtn.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 197, 184, 255));
                textoBtn.Text = "Seleccionar portada";
                textoBtn.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 197, 184, 255));
                btnQuitar.Visibility = Visibility.Collapsed;
            };

            var error = new TextBlock
            {
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 95, 122)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            var panel = new StackPanel { Spacing = 0, Width = 320 };

            // ── Logo desde Assets en lugar de emoji ───
            var logoBorde = new Border
            {
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(6)
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

            // Intentar cargar el logo desde Assets
            try
            {
                var logoImg = new Image
                {
                    Source = new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png")),
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                logoBorde.Child = logoImg;
            }
            catch
            {
                // Fallback al ícono si no carga el logo
                logoBorde.Child = new FontIcon
                {
                    Glyph = "\uE736",
                    FontSize = 22,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            var tituloLabel = new TextBlock
            {
                Text = editBook == null ? "Agregar libro" : "Editar libro",
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
                Text = "Completa la información del libro",
                FontSize = 12,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 90, 99, 128)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            panel.Children.Add(CrearLabel("PORTADA"));
            panel.Children.Add(imgPreview);
            panel.Children.Add(fallbackPreview);
            panel.Children.Add(filaBotones);

            var sep = new Border { Height = 1, Opacity = 0.08, Margin = new Thickness(0, 0, 0, 20) };
            sep.Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Windows.UI.Color.FromArgb(0,   255, 255, 255), Offset = 0   },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(255, 255, 255, 255), Offset = 0.5 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(0,   255, 255, 255), Offset = 1   }
                }
            };
            panel.Children.Add(sep);

            panel.Children.Add(CrearLabel("TÍTULO *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = titulo });
            panel.Children.Add(CrearLabel("AUTOR *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = autor });
            panel.Children.Add(CrearLabel("CATEGORÍA *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = categoria });
            panel.Children.Add(CrearLabel("ISBN *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = isbn });
            panel.Children.Add(CrearLabel("AÑO"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 14), Child = anio });
            panel.Children.Add(CrearLabel("CANTIDAD *"));
            panel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 16), Child = cantidad });
            panel.Children.Add(error);

            panel.Opacity = 0;
            var panelT = new TranslateTransform { Y = 20 };
            panel.RenderTransform = panelT;

            var scroll = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 520,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            dialog = new ContentDialog
            {
                Content = scroll,
                PrimaryButtonText = "Guardar",
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

            guardar = async () =>
            {
                if (string.IsNullOrWhiteSpace(titulo.Text))
                { MostrarToast("El título es obligatorio", "error"); return; }

                if (string.IsNullOrWhiteSpace(autor.Text))
                { MostrarToast("El autor es obligatorio", "error"); return; }

                string catTexto = categoria.Text.Trim();
                if (string.IsNullOrWhiteSpace(catTexto))
                { MostrarToast("La categoría es obligatoria", "error"); return; }
                if (_emojis.IsMatch(catTexto) || !_categoriaValida.IsMatch(catTexto))
                { MostrarToast("La categoría solo acepta letras", "error"); return; }

                string isbnTexto = isbn.Text.Trim();
                if (string.IsNullOrWhiteSpace(isbnTexto))
                { MostrarToast("El ISBN es obligatorio", "error"); return; }
                if (isbnTexto.Length != 10 && isbnTexto.Length != 13)
                { MostrarToast("El ISBN debe tener 10 o 13 dígitos", "error"); return; }

                string anioTexto = anio.Text.Trim();
                if (!string.IsNullOrEmpty(anioTexto))
                {
                    if (!int.TryParse(anioTexto, out int yearVal) ||
                        yearVal < MAX_ANIO_MIN || yearVal > MAX_ANIO_MAX)
                    { MostrarToast($"Año inválido ({MAX_ANIO_MIN}–{MAX_ANIO_MAX})", "warning"); return; }
                }
                int year = string.IsNullOrEmpty(anioTexto) ? 0 : int.Parse(anioTexto);

                string cantTexto = cantidad.Text.Trim();
                if (string.IsNullOrWhiteSpace(cantTexto))
                { MostrarToast("La cantidad es obligatoria", "warning"); return; }
                if (!int.TryParse(cantTexto, out int stock))
                { MostrarToast("La cantidad debe ser un número entero", "warning"); return; }
                if (stock <= 0)
                { MostrarToast("La cantidad debe ser mayor a 0", "warning"); return; }
                if (stock > MAX_CANTIDAD)
                { MostrarToast($"La cantidad máxima permitida es {MAX_CANTIDAD}", "warning"); return; }

                var all = BookService.GetAll();
                if (all.Any(x => x.ISBN == isbnTexto &&
                                 x.Id != (editBook?.Id ?? 0) &&
                                 x.Activo))
                { MostrarToast("El ISBN ya existe en el catálogo", "error"); return; }

                string catNorm = Normalizar(catTexto);
                var matchCat = _categorias.FirstOrDefault(c => Normalizar(c) == catNorm);
                string catFinal = matchCat ?? catTexto;

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

                bool esNuevo = editBook == null;

                if (esNuevo)
                {
                    all.Add(new Book
                    {
                        Id = BookService.NextId(),
                        Titulo = titulo.Text.Trim(),
                        Autor = autor.Text.Trim(),
                        Categoria = catFinal,
                        ISBN = isbnTexto,
                        Anio = year,
                        CantidadTotal = stock,
                        Disponibles = stock,
                        Activo = true,
                        CoverUrl = coverSeleccionada
                    });
                    DataService.SaveLog(_session.Usuario,
                        $"Registró libro | Título: {titulo.Text.Trim()}");
                }
                else
                {
                    var libro = all.First(x => x.Id == editBook.Id);
                    int prestados = libro.CantidadTotal - libro.Disponibles;

                    if (stock < prestados)
                    {
                        MostrarToast(
                            $"No puedes poner una cantidad menor a los libros prestados ({prestados})",
                            "error");
                        return;
                    }

                    libro.Titulo = titulo.Text.Trim();
                    libro.Autor = autor.Text.Trim();
                    libro.Categoria = catFinal;
                    libro.ISBN = isbnTexto;
                    libro.Anio = year;
                    libro.CantidadTotal = stock;
                    libro.Disponibles = stock - prestados;
                    libro.CoverUrl = coverSeleccionada;

                    DataService.SaveLog(_session.Usuario,
                        $"Editó libro | Título: {libro.Titulo}");
                }

                BookService.Save(all);
                dialog.Hide();
                LoadBooks();

                MostrarToast(
                    esNuevo ? "Libro agregado correctamente" : "Libro actualizado correctamente",
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

        private TextBox CrearTextBox(string placeholder, int maxLength = 200)
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

        private TextBlock CrearLabel(string texto) => new TextBlock
        {
            Text = texto,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 64, 72, 112)),
            CharacterSpacing = 80,
            Margin = new Thickness(2, 0, 0, 7)
        };

        private async Task Msg(string texto)
        {
            var d = new ContentDialog
            {
                Title = "Sistema",
                Content = texto,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            EstilarDialog(d);
            await d.ShowAsync();
        }
    }
}