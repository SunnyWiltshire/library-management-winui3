using Biblioteca.Services;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Biblioteca
{
    public partial class App : Application
    {
        public static Window CurrentWindow { get; set; }

        public App()
        {
            this.InitializeComponent();

            this.UnhandledException += (s, e) =>
            {
                string msg = e.Exception?.ToString() ?? "null";

                Debug.WriteLine("=== CRASH ===");
                Debug.WriteLine(msg);

                e.Handled = true;
            };
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            DataService.Initialize();

            // Ventana inicial
            CurrentWindow = new LoginWindow();

            // Mostrar ventana
            CurrentWindow.Activate();

            // Aplicar icono personalizado
            SetWindowIcon();
        }

        // =====================================================
        // CAMBIAR ICONO DE LA BARRA SUPERIOR
        // =====================================================
        private void SetWindowIcon()
        {
            try
            {
                // HWND de la ventana
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(CurrentWindow);

                // Ruta del icono
                string iconPath = Path.Combine(
                    Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                    "Assets",
                    "App.ico"
                );

                // Cargar icono
                IntPtr hIcon = LoadImage(
                    IntPtr.Zero,
                    iconPath,
                    IMAGE_ICON,
                    32,
                    32,
                    LR_LOADFROMFILE
                );

                // Aplicar icono
                if (hIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, ICON_SMALL, hIcon);
                    SendMessage(hwnd, WM_SETICON, ICON_BIG, hIcon);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al cargar icono:");
                Debug.WriteLine(ex.Message);
            }
        }

        // =====================================================
        // WIN32
        // =====================================================

        private const int WM_SETICON = 0x0080;

        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x0010;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int Msg,
            int wParam,
            IntPtr lParam
        );

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(
            IntPtr hInst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad
        );
    }
}