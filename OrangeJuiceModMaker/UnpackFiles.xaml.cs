using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ImageMagick;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for UnpackFiles.xaml
    /// </summary>
    public partial class UnpackFiles
    {
        private readonly string gameDirectory;
        private const int TotalFilesInitial = 11119;
        private int totalFiles;
        private static int _cardsConverted;
        private readonly string[] paks = "graphics".Split(',');
        private int paksUnzipped = 1;
        private int finished;
        private readonly Thread timer;
        private bool showStatus;
        private static bool _exit;
        private readonly string appData;
        private readonly bool debug;

        public UnpackFiles(string gameDirectory, string appData, bool debug)
        {
            UnpackFiles f = this;
            this.debug = debug;
            InitializeComponent();
            this.gameDirectory = gameDirectory;
            timer = new Thread(() =>
            {
                while (finished != 1)
                {
                    if (showStatus)
                    {
                        f.Dispatcher.Invoke(() =>
                        {
                            f.Status.Text =
                                $"Unpacking {(f.paksUnzipped > 1 ? "Complete" : $"{f.paksUnzipped}/1")}";
                        });
                    }
                    if (_exit)
                    {
                        return;
                    }
                }

                f.Dispatcher.Invoke(() =>
                {
                    f.Status.Text =
                        $"Unpacking {(f.paksUnzipped > 1 ? "Complete" : $"{f.paksUnzipped}/1")}";
                    MainWindow.UnpackComplete = true;
                    f.Close();
                });
            });
            this.appData = appData;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (string pakName in paks)
                {
                    bool @continue = false;
                    _ = Task.Run(() =>
                    {
                        string pak = pakName;
                        @continue = true;
                        ProcessStartInfo unpackInfo = new()
                        {
                            Arguments = $@"x ""{gameDirectory}\data\{pak}.pak"" -opakFiles -y -mmt=4",
                            FileName = $@"{appData}\7za.exe",
                            //UseShellExecute = true,
                            //WindowStyle = ProcessWindowStyle.Normal,
                            //CreateNoWindow = false
                            UseShellExecute = debug,
                            WindowStyle = debug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                            CreateNoWindow = !debug,
                            RedirectStandardOutput = !debug,
                            RedirectStandardError = !debug,
                        };
                        Process process = Process.Start(unpackInfo) ?? throw new Exception("Unpack Failed");

                        string output = "";
                        string error = "";

                        if (!debug)
                        {
                            output = process.StandardOutput.ReadToEnd();
                            error = process.StandardError.ReadToEnd();
                        }

                        process.WaitForExit();

                        ++paksUnzipped;

                        if (process.ExitCode != 0)
                        {
                            File.WriteAllText("7zip_error.error", $"{output}{Environment.NewLine}{error}");
                            MainWindow.ExitTime = true;
                        }

                        if (_exit)
                        {
                            return;
                        }

                        ConvertImages(pak);
                        ++finished;
                    });
                    while (!@continue)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception exception)
            {
                string[] error =
                    [exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? ""];
                File.WriteAllLines("unpack_error.txt", error);
                Console.WriteLine(error.AsString());
                throw;
            }

            _cardsConverted = 0;
            timer.Start();
        }

        private void ConvertImages(string pak)
        {
            return;
            string[] unpackedFiles = Directory.GetFiles($@"pakFiles\{pak}", "*.dat", SearchOption.AllDirectories);

            totalFiles += unpackedFiles.Length;

            const int length = 200;

            string[][] sets = new string[unpackedFiles.Length / length + 1][];
            for (int i = 0; i < sets.Length - 1; ++i)
            {
                sets[i] = new string[length];
            }

            sets[^1] = new string[unpackedFiles.Length % length];
            for (int i = 0; i < unpackedFiles.Length; ++i)
            {
                int set = i / length;
                int index = i % length;
                sets[set][index] = unpackedFiles[i];
            }

            Task[] tasks = new Task[sets.Length];

            for (int index = 0; index < sets.Length; index++)
            {
                tasks[index] = CovertImageLoop(sets[index]);
            }

            Task.WaitAll(tasks);
        }

        private static Task CovertImageLoop(string[] unpackedFiles)
        {
            bool taskRunning = true;
            Thread t = new(() =>
            {
                foreach (string file in unpackedFiles)
                {
                    
                    using MagickImage m = new(file);
                    m.Format = MagickFormat.Png;
                    m.Write(Path.ChangeExtension(file, "png"));
                    ++_cardsConverted;
                    if (_exit)
                    {
                        return;
                    }
                }

                foreach (string file in unpackedFiles)
                {
                    File.Delete(file);
                    if (_exit)
                    {
                        return;
                    }
                }
                taskRunning = false;
            })
            {
                IsBackground = true,
                Priority = _p
            };
            t.Start();
            Threads.Add(t);
            return Task.Run(() =>
            {
                while (taskRunning)
                {
                    Thread.Sleep(1);
                }
            });
        }

        private static ThreadPriority _p = ThreadPriority.Highest;
        private static readonly List<Thread> Threads = [];

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            showStatus = true;
            _p = ThreadPriority.Lowest;
            Threads.Where(z => z.IsAlive).ForEach(z => z.Priority = _p);
            if (sender is Button b)
            {
                b.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _exit = true;
        }
    }
}
