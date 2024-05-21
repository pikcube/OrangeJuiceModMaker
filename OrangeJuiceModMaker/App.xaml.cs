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
        private static bool _createStars;

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Loading");
            try
            {
                Process.Start("CMD.exe",
                        "/C winget upgrade Pikcube.OrangeJuiceModMaker --accept-source-agreements --accept-package-agreements")
                    .WaitForExit();
            }
            catch
            {
                //ignore
            }
            bool debug = e.Args.Any(z => z.ToLower().StripStart(1) is "debug" or "d" or "verbose" or "v");
            _createStars = !debug;
            Task.Run(() =>
            {
                while (_createStars)
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

            try
            {
                new MainWindow(debug).ShowDialog();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            //postAction is set as part of check update
            //if (!string.IsNullOrEmpty(PostAction.Result))
            //{
            //    Process.Start(PostAction.Result);
            //}
        }

        public static void ShowHideConsole(bool debug)
        {
            _createStars = false;
            IntPtr handle = GetConsoleWindow();
            int swInt = debug ? SwShow : SwHide;

            //Handle Console
            _ = debug && handle == IntPtr.Zero ? AllocConsole() : ShowWindow(handle, swInt);
        }
    }
}
