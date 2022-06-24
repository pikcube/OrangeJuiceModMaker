using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Octokit;

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

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            string downloadPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\ojDownload";
            bool debug = e.Args.Any(z => z.ToLower().StripStart(1) is "debug" or "d" or "verbose" or "v");
            IntPtr handle = GetConsoleWindow();
            int swInt = debug ? SwShow : SwHide;
            
            //Handle Console
            _ = debug && handle == IntPtr.Zero ? AllocConsole() : ShowWindow(handle, swInt);

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
    }
}
