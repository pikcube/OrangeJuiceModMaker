using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SwHide = 0;
        private const int SwShow = 5;
        public Task<string> PostAction = Task.Run(() => "");
        private static bool createStars;

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Loading");

            bool debug = e.Args.Any(z => z.ToLower().StripStart(1) is "debug" or "d" or "verbose" or "v");
            createStars = !debug;
            Task.Run(() =>
            {
                while (createStars)
                {
                    Console.Write("*");
                    Thread.Sleep(100);
                }
            });
            string downloadPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\ojDownload";

            //Clean up from prior updates
            if (Directory.Exists(downloadPath))
            {
                Directory.Delete(downloadPath, true);
            }

            new MainWindow(debug, this, downloadPath).ShowDialog();

            //postAction is set as part of check update
            if (!string.IsNullOrEmpty(PostAction.Result))
            {
                Process.Start(PostAction.Result);
            }
        }

        public static void ShowHideConsole(bool debug)
        {
            createStars = false;
            IntPtr handle = GetConsoleWindow();
            int swInt = debug ? SwShow : SwHide;

            //Handle Console
            _ = debug && handle == IntPtr.Zero ? AllocConsole() : ShowWindow(handle, swInt);
        }
    }
}
