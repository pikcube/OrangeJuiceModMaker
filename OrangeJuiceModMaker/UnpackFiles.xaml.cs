using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for UnpackFiles.xaml
    /// </summary>
    public partial class UnpackFiles : Window
    {
        private string gameDirectory;
        private int zi = 9521;
        private int z = 0;
        private static int y = 0;
        string[] paks = "cards,units".Split(',');
        int x = 1;
        int finished = 0;
        private Thread timer;
        private static UnpackFiles? f;
        private bool ShowStatus;
        private static bool Exit = false;

        public UnpackFiles(string gameDirectory)
        {
            f = this;
            InitializeComponent();
            this.gameDirectory = gameDirectory;
            timer = new Thread(() =>
            {
                while (finished != 2)
                {
                    if (ShowStatus)
                    {
                        f!.Dispatcher.Invoke(() =>
                        {
                            f.Status.Text =
                                $"Unpacking {(f.x > 2 ? "Complete" : $"{f.x}/2")}{Environment.NewLine}Converting {y}/{(f.x == 3 ? f.z : f.zi)}";
                        });
                    }
                    if (Exit)
                    {
                        return;
                    }
                }

                f!.Dispatcher.Invoke(() =>
                {
                    f.Status.Text = $"Unpacking {(f.x > 2 ? "Complete" : $"{f.x}/2")}{Environment.NewLine}Converting {y}/{(f.x == 3 ? f.z : f.zi)}";
                    MainWindow.UnpackComplete = true;
                    f.Close();
                });
            });
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
                        ProcessStartInfo unpackinfo = new()
                        {
                            Arguments = $@"e ""{gameDirectory}\data\{pak}.pak"" -opakFiles\{pak} -y",
                            FileName = "7za.exe",
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        };
                        Process p = Process.Start(unpackinfo) ?? throw new Exception("Unpack Failed");

                        p.WaitForExit();

                        ++x;
                        if (Exit)
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

            y = 0;
            timer.Start();
        }

        private void ConvertImages(string pak)
        {
            string[] unpackedFiles = Directory.GetFiles($@"pakFiles\{pak}", "*.dat", SearchOption.AllDirectories);

            z += unpackedFiles.Length;

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
                    ++y;
                    if (Exit)
                    {
                        return;
                    }
                }

                foreach (string file in unpackedFiles)
                {
                    File.Delete(file);
                    if (Exit)
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
            ShowStatus = true;
            p = ThreadPriority.Lowest;
            threads.Where(z => z.IsAlive).ForEach(z => z.Priority = p);
            if (sender is Button b)
            {
                b.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Exit = true;
        }
    }
}
