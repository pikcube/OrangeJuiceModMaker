using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using TextFieldParser = Microsoft.VisualBasic.FileIO.TextFieldParser;
using SearchOption = System.IO.SearchOption;
using Path = System.IO.Path;


namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string? GameDirectory;
        public static bool UnpackComplete = true;
        public static ModReplacements LoadedModReplacements = new();
        public static ModDefinition? LoadedModDefinition;
        public static string LoadedModPath = "";
        public static List<CsvHolder> CSVFiles = new();
        public static List<Unit> UnitHyperTable = new();
        public static CsvHolder FlavorLookUp = new("FlavorLookUp.csv");
        public static List<string> Cards = new();
        public static List<string> UnitImagePaths = new();
        private static bool _exitTime;
        private List<string> mods = new();
        public static bool ExitTime
        {
            set
            {
                if (_exitTime) return;
                if (!value) return;
                _exitTime = true;
                MessageBox.Show(
                    "Type B error has been thrown, please check the error files for more information. The app will now exit");
                Environment.Exit(0);
            }
        }


        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            AggregateException exception = e.Exception;
            string[] Error =
            {
                DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "",
                exception.StackTrace ?? ""
            };
            File.AppendAllLines("main_error.txt", Error);
            MessageBox.Show("Error in load, see error.txt for more information");
            Close();
        }

        public MainWindow()
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            try
            {
                Directory.CreateDirectory("temp");
                Task dumpTempFiles = Task.Run(() =>
                {
                    if (!File.Exists("ffmpeg.exe"))
                    {
                        if (File.Exists("ffmpeg.zip"))
                        {
                            ProcessStartInfo unpackinfo = new()
                            {
                                Arguments = $@"e ffmpeg.zip -y",
                                FileName = "7za.exe",
                                UseShellExecute = false,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true,
                            };
                            Process.Start(unpackinfo)?.WaitForExit();
                            File.Delete("ffmpeg.zip");
                        }
                        else
                        {
                            MessageBox.Show("Please download ffmpeg executable");
                            Environment.Exit(0);
                        }
                    }
                    
                    foreach (string? f in Directory.GetFiles("temp"))
                    {
                        File.Delete(f);
                    }
                });
                Task LoadOJData = Task.Run(() =>
                {
                    string[] Files = Directory.GetFiles("csvFiles");
                    if (Files.Length != 20)
                    {
                        throw new Exception("Didn't find all files");
                    }
                    foreach (string file in Files)
                    {
                        CSVFiles.Add(new CsvHolder(file));
                    }
                });

                //First Time Setup Code
                if (File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\100 Orange Juice\100orange.exe"))
                {
                    GameDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\100 Orange Juice";
                }
                else
                {
                    //Locate Directory
                    if (File.Exists("gamedirectory.path"))
                    {
                        GameDirectory = File.ReadAllText("gamedirectory.path");
                    }
                    else
                    {
                        OpenFileDialog open = new()
                        {
                            Filter = "Applications (*.exe)|*.exe|All files (*.*)|*.*"
                        };
                        if (Directory.Exists(@"C:\Program Files (x86)"))
                        {
                            open.InitialDirectory = @"C:\Program Files (x86)";
                        }

                        open.Title = "Please locate your 100% Orange Juice executable";

                        if (open.ShowDialog() == true)
                        {
                            GameDirectory = Path.GetDirectoryName(open.FileName) ?? throw new FileNotFoundException();
                            File.WriteAllText("gamedirectory.path", GameDirectory);
                        }
                        else
                        {
                            Close();
                            return;
                        }
                    }
                }

                //Unpack Base Files
                if (!Directory.Exists("pakFiles") ||
                    File.Exists(@"pakFiles\filesUnpacked.status"))
                {
                    Directory.CreateDirectory("pakFiles");
                    File.WriteAllText(@"pakFiles\filesUnpacked.status", "false");
                    UnpackComplete = false;
                    new UnpackFiles(GameDirectory).ShowDialog();
                    if (!UnpackComplete)
                    {
                        MessageBox.Show("Unpack Failed");
                        Close();
                    }
                    File.Delete(@"pakFiles\filesUnpacked.status");
                }

                Cards = Directory.GetFiles(@"pakFiles\cards").ToList();

                Task LoadHyperData = Task.Run(() =>
                {
                    using TextFieldParser parser = new("HyperLookupTable.csv");
                    parser.Delimiters = new[] { "," };
                    parser.HasFieldsEnclosedInQuotes = true;
                    List<string[]> rawRows = new();
                    _ = parser.ReadFields();
                    LoadOJData.Wait();
                    CsvHolder charCards = CSVFiles.First(z => z.Name == "CharacterCards");
                    while (!parser.EndOfData)
                    {
                        UnitHyperTable.Add(new Unit(parser.ReadFields() ?? throw new InvalidOperationException(), charCards));
                    }
                });

                InitializeComponent();
                if (!UpdateModsLoaded())
                {
                    Close();
                }

                LoadOJData.Wait();
                LoadHyperData.Wait();

                UnitImagePaths = Directory.GetFiles(@"pakFiles\units").ToList();

                AggregateException? e = LoadOJData.Exception ?? LoadHyperData.Exception ?? null;

                if (e is not null)
                {
                    throw e;
                }

                dumpTempFiles.Wait();

                for (int n = 0; n < FlavorLookUp.Rows.Length; ++n)
                {
                    for (int m = 0; m < FlavorLookUp.Rows[n].Length; ++m)
                    {
                        FlavorLookUp.Rows[n][m] = FlavorLookUp.Rows[n][m].Replace(@"\n", Environment.NewLine);
                    }
                }

                //CreateLookUp();
            }
            catch (Exception exception)
            {
                string[] Error =
                    { DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                File.AppendAllLines("main_error.txt", Error);
                MessageBox.Show("Error in load, see error.txt for more information");
                Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SelectedModeComboBox.ItemsSource = new[] { "Modify Unit", "Modify Card" };
            SelectedModeComboBox.SelectedIndex = 0;
        }

        private bool UpdateModsLoaded()
        {
            //Load a mod
            mods = Directory
                .GetFiles($@"{GameDirectory}\mods", "*",
                    SearchOption.AllDirectories)
                .Where(z => Path.GetExtension(z).ToLower() == ".json").ToList();

            if (mods.Count == 0)
            {
                new NewMod { Owner = IsLoaded ? this : null }.ShowDialog();
                mods = Directory.GetFiles($@"{GameDirectory}\mods", "*",
                        SearchOption.AllDirectories)
                    .Where(z => Path.GetExtension(z).ToLower() == ".json").ToList();
                if (mods.Count == 0)
                {
                    return false;
                }
            }

            SelectedModComboBox.ItemsSource = mods.Select(z =>
                JsonConvert.DeserializeObject<Root>(File.ReadAllText(z).Replace("/", @"\\"))?.ModDefinition?.name);
            SelectedModComboBox.SelectedIndex = 0;

            return true;
        }

        private void SelectedModComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? ModName = SelectedModComboBox.SelectedItem.ToString();
            if (ModName is null)
            {
                return;
            }
            string[] possiblemods = mods.Where(z => JsonConvert.DeserializeObject<Root>(File.ReadAllText(z).Replace("/",@"\\"))?.ModDefinition.name == ModName)
                .ToArray();
            switch (possiblemods.Length)
            {
                case 1:
                    Root extractedMod = JsonConvert.DeserializeObject<Root>(File.ReadAllText(possiblemods[0]).Replace("/", @"\\")) ??
                                        throw new FileFormatException("Failed to Load Mod");
                    LoadedModDefinition = extractedMod.ModDefinition;
                    LoadedModReplacements = extractedMod.ModReplacements ?? new ModReplacements();
                    string containingFolder = Path.GetDirectoryName(possiblemods[0]) ??
                                              throw new IOException("Mod Path Not Found");
                    LoadedModPath = containingFolder;
                    Directory.CreateDirectory($@"{containingFolder}\cards");
                    Directory.CreateDirectory($@"{containingFolder}\units");
                    Directory.CreateDirectory($@"{containingFolder}\music");
                    break;
                case 0:
                    throw new NotImplementedException("No Mods...wait what?");
                default:
                    throw new NotImplementedException("Too Many Mods");
            }
        }

        private void NewModButton_OnClick(object sender, RoutedEventArgs e)
        {
            new NewMod().ShowDialog();
            if (!UpdateModsLoaded())
            {
                Close();
            }
        }

        private void SelectedModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            switch (SelectedModeComboBox.SelectedItem as string)
            {
                case null:
                    break;
                case "Modify Unit":
                    new ModifyUnit { Owner = this }.ShowDialog();
                    break;
                case "Modify Card":
                    new ModifyCard { Owner = this }.ShowDialog();
                    break;
                default:
                    MessageBox.Show("Error");
                    break;
            }
        }
    }
}
