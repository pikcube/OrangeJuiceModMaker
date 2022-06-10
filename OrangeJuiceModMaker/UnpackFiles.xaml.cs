using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ImageMagick;
using Path = System.IO.Path;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for UnpackFiles.xaml
    /// </summary>
    public partial class UnpackFiles : Window
    {
        private string _gameDirectory;
        public UnpackFiles(string gameDirectory)
        {
            InitializeComponent();
            this._gameDirectory = gameDirectory;
        }

        private int _zi = 9521;
        private int _z = 0;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] paks = "cards,units".Split(',');
                int x = 1;
                int y = 0;

                int finished = 0;
                foreach (string pakName in paks)
                {
                    bool @continue = false;
                    Task t = Task.Run(() =>
                    {
                        string pak = pakName;
                        @continue = true;
                        ProcessStartInfo unpackinfo = new()
                        {
                            Arguments = $@"e ""{_gameDirectory}\data\{pak}.pak"" -opakFiles\{pak} -y",
                            FileName = "7za.exe",
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                        };
                        Process p = Process.Start(unpackinfo) ?? throw new Exception("Unpack Failed");

                        p.WaitForExit();

                        ++x;

                        ConvertImages(pak, ref y);
                        ++finished;
                    });

                    while (!@continue)
                    {
                    }
                }

                while (finished != 2)
                {
                    Status.Text = $"Unpacking {(x > 2 ? "Complete" : $"{x}/2")}{Environment.NewLine}Converting {y}/{(x == 3 ? _z : _zi)}";
                    await Task.Run(() => Thread.Sleep(10));
                }

                Status.Text += Environment.NewLine + "Cleaning Up";
                _ = Task.Run(() =>
                {
                    foreach (string file in Directory.GetFiles(@"pakFiles", "*.dat", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                });

                Status.Text += Environment.NewLine + "All Files Unpacked!";
                MainWindow.UnpackComplete = true;

                await Task.Run(() => Thread.Sleep(1000));
                Close();
            }
            catch (Exception exception)
            {
                string[] Error =
                    { exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                File.WriteAllLines("unpack_error.txt", Error);
                throw;
            }
        }

        private void ConvertImages(string pak, ref int y)
        {
            string[] unpackedFiles = Directory.GetFiles($@"pakFiles\{pak}", "*.dat", SearchOption.AllDirectories);

            _z += unpackedFiles.Length;

            string[][] sets = new string[unpackedFiles.Length / 1000 + 1][];
            for (int i = 0; i < sets.Length - 1; ++i)
            {
                sets[i] = new string[1000];
            }

            sets[^1] = new string[unpackedFiles.Length % 1000];
            for (int i = 0; i < unpackedFiles.Length; ++i)
            {
                int set = i / 1000;
                int index = i % 1000;
                sets[set][index] = unpackedFiles[i];
            }

            Task[] tasks = new Task[sets.Length];

            for (int index = 0; index < sets.Length; index++)
            {
                tasks[index] = CovertImageLoop(ref y, sets[index]);
            }

            Task.WaitAll(tasks);
        }

        private static Task CovertImageLoop(ref int y, string[] unpackedFiles)
        {
            foreach (string file in unpackedFiles)
            {
                using MagickImage m = new(file);
                m.Format = MagickFormat.Png;
                m.Write(Path.ChangeExtension(file, "png"));
                ++y;
            }
            return Task.CompletedTask;
        }
    }
}
