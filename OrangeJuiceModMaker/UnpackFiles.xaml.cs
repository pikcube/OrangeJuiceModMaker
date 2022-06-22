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
        private const int TotalFilesInitial = 9521;
        private int totalFiles;
        private static int cardsConverted;
        readonly string[] paks = "cards,units".Split(',');
        private int paksUnzipped = 1;
        int finished;
        private Thread timer;
        private bool showStatus;
        private static bool exit;
        private string appData;
        private bool debug;

        public UnpackFiles(string gameDirectory, string appData, bool debug)
        {
            UnpackFiles f = this;
            this.debug = debug;
            InitializeComponent();
            this.gameDirectory = gameDirectory;
            timer = new Thread(() =>
            {
                while (finished != 2)
                {
                    if (showStatus)
                    {
                        f.Dispatcher.Invoke(() =>
                        {
                            f.Status.Text =
                                $"Unpacking {(f.paksUnzipped > 2 ? "Complete" : $"{f.paksUnzipped}/2")}{Environment.NewLine}" +
                                $"Converting {cardsConverted}/{(f.paksUnzipped == 3 ? f.totalFiles : TotalFilesInitial)}";
                        });
                    }
                    if (exit)
                    {
                        return;
                    }
                }

                f.Dispatcher.Invoke(() =>
                {
                    f.Status.Text = $"Unpacking {(f.paksUnzipped > 2 ? "Complete" : $"{f.paksUnzipped}/2")}{Environment.NewLine}" +
                                    $"Converting {cardsConverted}/{(f.paksUnzipped == 3 ? f.totalFiles : TotalFilesInitial)}";
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
                            Arguments = $@"e ""{gameDirectory}\data\{pak}.pak"" -opakFiles\{pak} -cardsConverted",
                            FileName = $@"{appData}\7za.exe",
                            //UseShellExecute = true,
                            //WindowStyle = ProcessWindowStyle.Normal,
                            //CreateNoWindow = false
                            UseShellExecute = debug,
                            WindowStyle = debug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                            CreateNoWindow = !debug,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        };
                        Process process = Process.Start(unpackInfo) ?? throw new Exception("Unpack Failed");

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        ++paksUnzipped;

                        if (process.ExitCode != 0)
                        {
                            File.WriteAllText("7zip_error.error", $"{output}{Environment.NewLine}{error}");
                            MainWindow.ExitTime = true;
                        }

                        if (exit)
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
                    { exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                File.WriteAllLines("unpack_error.txt", error);
                throw;
            }

            cardsConverted = 0;
            timer.Start();
        }

        private void ConvertImages(string pak)
        {
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
                    ++cardsConverted;
                    if (exit)
                    {
                        return;
                    }
                }

                foreach (string file in unpackedFiles)
                {
                    File.Delete(file);
                    if (exit)
                    {
                        return;
                    }
                }
                taskRunning = false;
            })
            {
                IsBackground = true,
                Priority = p
            };
            t.Start();
            threads.Add(t);
            return Task.Run(() =>
            {
                while (taskRunning)
                {
                    Thread.Sleep(1);
                }
            });
        }

        private static ThreadPriority p = ThreadPriority.Highest;
        private static List<Thread> threads = new();

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            showStatus = true;
            p = ThreadPriority.Lowest;
            threads.Where(z => z.IsAlive).ForEach(z => z.Priority = p);
            if (sender is Button b)
            {
                b.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            exit = true;
        }
    }
}
