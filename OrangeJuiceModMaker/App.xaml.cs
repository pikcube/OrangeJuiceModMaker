using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Newtonsoft.Json;
using Octokit;
using Application = System.Windows.Application;

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
        private Task<string> postAction = Task.Run(() => "");

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            string downloadPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\ojDownload";
            if (Directory.Exists(downloadPath))
            {
                Directory.Delete(downloadPath, true);
            }
            
            async Task<string> DownloadExeAsync(HttpClient client, Release release, bool isBeta)
            {
                string path = downloadPath; ;
                Directory.CreateDirectory(path);
                path += @"\OJSetup.exe";
                HttpResponseMessage response = client.GetAsync(release.Assets[isBeta ? 0 : 1].BrowserDownloadUrl).Result;
                byte[] byteArray = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(path, byteArray);
                return path;
            }

            bool SkipVersion(string newVersion)
            {
                string skipFile =
                    $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\release.version";

                return File.Exists(skipFile) && File.ReadAllText(skipFile) != newVersion;
            }

            bool debug;
            //Check Version
            using (HttpClient client = new())
            {
                Task<string> a = client.GetStringAsync(@"https://raw.githubusercontent.com/pikcube/OrangeJuiceModMaker/main/release.version");

                debug = e.Args.Any(z => z.ToLower().StripStart(1) is "debug" or "d" or "verbose" or "v");

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

                e.Args.ForEach(Console.WriteLine);

                try
                {
                    string[] version = File.ReadAllText("release.version").Split(":");
                    string[] checkedVersion = a.Result.Split(":");
                    bool isBeta = version[0] == "Beta";
                    string checkedVersionString = isBeta ? checkedVersion[1] : checkedVersion[3];
                    bool upToDate = version[1] == checkedVersionString;
                    if (SkipVersion(checkedVersionString))
                    {

                    }
                    else if (!upToDate)
                    {
                        GitHubClient github = new(new ProductHeaderValue("OrangeJuiceModUpdateChecker"));
                        Release? release = github.Repository.Release.GetAll("pikcube", "OrangeJuiceModMaker").Result
                            .Where(z => !z.Prerelease || isBeta).MaxBy(z => z.CreatedAt) ?? null;
                        if(release is null)
                        {
                        }
                        else
                        {
                            bool invalid;
                            do
                            {
                                invalid = false;
                                int? option;
                                if (debug)
                                {
                                    Console.WriteLine("New version of application released:");
                                    Console.WriteLine("1. Update now");
                                    Console.WriteLine("2. Update on exit");
                                    Console.WriteLine("3. Remind me later");
                                    Console.WriteLine("4. Skip this version");
                                    option = Console.ReadLine()?.ToIntOrNull();
                                }
                                else
                                {
                                    option = new UpdateWindow().GetOption();
                                }
                                switch (option)
                                {
                                    case 1:
                                        string path = DownloadExeAsync(client, release, isBeta).Result;
                                        Process.Start(path);
                                        Environment.Exit(0);
                                        break;
                                    case 2:
                                        postAction = DownloadExeAsync(client, release, isBeta);
                                        break;
                                    case 3:
                                        break;
                                    case 4:
                                        File.WriteAllTextAsync(
                                            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\release.version",
                                            checkedVersionString);
                                        return;
                                    default:
                                        Console.WriteLine("Invalid option");
                                        invalid = true;
                                        break;
                                }
                            } while (invalid);
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to check version");
                }
            }
            MainWindow window = new(debug);
            window.ShowDialog();
            if (!string.IsNullOrEmpty(postAction.Result))
            {
                Process.Start(postAction.Result);
            }
        }
    }
}
