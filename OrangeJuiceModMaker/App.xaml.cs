using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            bool debug = e.Args.Any(z => z.ToLower().StripStart(1) is "debug" or "d" or "verbose" or "v");
            if (debug)
            {
                IntPtr handle = GetConsoleWindow();

                if (handle == IntPtr.Zero)
                {
                    AllocConsole();
                }
                else
                {
                    ShowWindow(handle, SW_SHOW);
                }
            }
            else
            {
                IntPtr handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }



            MainWindow window = new(debug);
            window.ShowDialog();
        }
    }
}
