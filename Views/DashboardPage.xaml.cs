using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;
using System;
using System.Linq;

namespace Biblioteca.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();
            RootGrid.Loaded += (s, e) => LoadDashboard();
        }

        private void LoadDashboard()
        {
            var books = BookService.GetAll()
                .Where(x => x.Activo)
                .ToList();

            var users = UserService.GetAll();
            var loans = LoanService.GetAll();

            int totalBooks = books.Count;

            int disponibles = books.Sum(x => x.Disponibles);

            int usersActive = users.Count(x => x.Activo);

            int activeLoans = loans.Count(x => !x.Devuelto);

            int lateLoans = loans.Count(x =>
                !x.Devuelto &&
                DateTime.Parse(x.FechaDevolucion) < DateTime.Today);

            TxtBooks.Text = totalBooks.ToString();
            TxtAvailable.Text = disponibles.ToString();
            TxtUsers.Text = usersActive.ToString();
            TxtLoans.Text = activeLoans.ToString();
            TxtLate.Text = lateLoans.ToString();

            var recientes = loans
                .OrderByDescending(x => x.Id)
                .Take(5)
                .Select(x => $"{x.Libro}  →  {x.Usuario}  ·  {x.FechaPrestamo}")
                .ToList();

            RecentList.ItemsSource = recientes;

            int prestados = books.Sum(x => x.CantidadTotal - x.Disponibles);

            TxtSummary1.Text = prestados.ToString();
            TxtSummary2.Text = users.Count.ToString();
            TxtSummary3.Text = loans.Count.ToString();

            AnimarCards();
        }

        private void AnimarCards()
        {
            UIElement[] elementos = { Card1, Card2, Card3, Card4, Card5 };

            for (int i = 0; i < elementos.Length; i++)
            {
                var el = elementos[i];
                el.Opacity = 0;

                var delayMs = i * 80;
                var transform = new Microsoft.UI.Xaml.Media.TranslateTransform { Y = 18 };
                el.RenderTransform = transform;

                var fade = new DoubleAnimation { From = 0, To = 1, BeginTime = TimeSpan.FromMilliseconds(delayMs), Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var slide = new DoubleAnimation { From = 18, To = 0, BeginTime = TimeSpan.FromMilliseconds(delayMs), Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

                var sb = new Storyboard();
                Storyboard.SetTarget(fade, el); Storyboard.SetTargetProperty(fade, "Opacity");
                Storyboard.SetTarget(slide, transform); Storyboard.SetTargetProperty(slide, "Y");
                sb.Children.Add(fade);
                sb.Children.Add(slide);
                sb.Begin();
            }
        }
    }
}