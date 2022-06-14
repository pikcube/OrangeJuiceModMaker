using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ImageMagick;
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
        public static bool SteamVersion = false;
        public static bool UnpackComplete = true;
        public static ModReplacements LoadedModReplacements = new();
        public static ModDefinition? LoadedModDefinition;
        public static string LoadedModPath = "";
        public static List<CsvHolder> CsvFiles = new();
        public static List<Unit> UnitHyperTable = new();
        public static CsvHolder? FlavorLookUp;
        public static List<string> Cards = new();
        public static List<string> UnitImagePaths = new();
        private static bool _exitTime;
        private List<string> mods = new();
        private string[] config;
        public static string Temp = "";
        public static string AppData = "";
        private string ModPath = "";
        private string ExePath = "";
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
            string[] error =
            {
                DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "",
                exception.StackTrace ?? ""
            };
            File.AppendAllLines("main_error.txt", error);
            MessageBox.Show("Error in load, see error.txt for more information");
            Close();
        }

        public MainWindow()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Temp = @$"{appdata}\OrangeJuiceModMaker\temp";
            AppData = $@"{appdata}\OrangeJuiceModMaker";
            FlavorLookUp = new CsvHolder($@"{AppData}\FlavorLookUp.csv");
            InitializeComponent();
            config = File.Exists($@"{Temp}\oj.config")
                ? File.ReadAllLines($@"{Temp}\oj.config")
                : new[] { "0", "0" };

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            try
            {
                ExePath = $@"{Directory.GetCurrentDirectory()}\OrangeJuiceModMaker.exe";
                Directory.SetCurrentDirectory(AppData);
                Directory.CreateDirectory(Temp);
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
                                CreateNoWindow = true
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

                    foreach (string? f in Directory.GetFiles(Temp).Where(z => Path.GetExtension(z) != ".config"))
                    {
                        File.Delete(f);
                    }
                });
                Task loadOjData = Task.Run(() =>
                {
                    string[] files = Directory.GetFiles("csvFiles");
                    if (files.Length != 20)
                    {
                        throw new Exception("Didn't find all files");
                    }
                    foreach (string file in files)
                    {
                        CsvFiles.Add(new CsvHolder(file));
                    }
                });
                SteamVersion = File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\100 Orange Juice\100orange.exe");
                //First Time Setup Code
                if (SteamVersion)
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

                Task loadHyperData = Task.Run(() =>
                {
                    using TextFieldParser parser = new("HyperLookupTable.csv");
                    parser.Delimiters = new[] { "," };
                    parser.HasFieldsEnclosedInQuotes = true;
                    List<string[]> rawRows = new();
                    _ = parser.ReadFields();
                    loadOjData.Wait();
                    CsvHolder charCards = CsvFiles.First(z => z.Name == "CharacterCards");
                    while (!parser.EndOfData)
                    {
                        UnitHyperTable.Add(new Unit(parser.ReadFields() ?? throw new InvalidOperationException(), charCards));
                    }
                });
                
                ModPath = $@"{GameDirectory}\mods";

                if (!UpdateModsLoaded())
                {
                    Close();
                }

                loadOjData.Wait();
                loadHyperData.Wait();

                UnitImagePaths = Directory.GetFiles(@"pakFiles\units").ToList();

                AggregateException? ex = loadOjData.Exception ?? loadHyperData.Exception ?? null;

                if (ex is not null)
                {
                    throw ex;
                }

                dumpTempFiles.Wait();

                foreach (string[] t in FlavorLookUp.Rows)
                {
                    for (int m = 0; m < t.Length; ++m)
                    {
                        t[m] = t[m].Replace(@"\n", Environment.NewLine);
                    }
                }

                //CreateLookUp();
            }
            catch (Exception exception)
            {
                string[] error =
                    { DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                File.AppendAllLines("main_error.txt", error);
                MessageBox.Show("Error in load, see error.txt for more information");
                Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SelectedModComboBox.SelectedIndex = config[0].ToInt();
            SelectedModeComboBox.ItemsSource = new[] { "Modify Unit", "Modify Card", "Modify Mod Definition"};
            SelectedModeComboBox.SelectedIndex = config[1].ToInt();
            if (SelectedModeComboBox.SelectedIndex is >= 3 or -1)
            {
                SelectedModeComboBox.SelectedIndex = 0;
            }
        }

        //Check the game directory for mod files
        private bool UpdateModsLoaded()
        {
            //Load a mod
            mods = Directory.GetFiles(path: ModPath, searchPattern: "*.json", searchOption: SearchOption.AllDirectories).ToList();

            //The zero mod case
            if (mods.Count == 0)
            {
                new NewMod { Owner = IsLoaded ? this : null }.ShowDialog();
                mods = Directory.GetFiles(ModPath, "*.json", SearchOption.AllDirectories).ToList();
                if (mods.Count == 0)
                {
                    return false;
                }
            }

            string?[]? itemsSource = mods.Select(z => Root.ReadJson(z)?.ModDefinition?.Name).ToArray();
            if (itemsSource is null)
            {
                throw new Exception("No mods?");
            }
            SelectedModComboBox.ItemsSource = itemsSource;
            SelectedModComboBox.SelectedIndex = config[0].ToInt();
            if (SelectedModComboBox.SelectedIndex >= itemsSource.Length || SelectedModComboBox.SelectedIndex == -1)
            {
                SelectedModComboBox.SelectedIndex = 0;
            }

            return true;
        }

        //Change selected mod
        private void SelectedModComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            config[0] = SelectedModComboBox.SelectedIndex.ToString();
            string? modName = SelectedModComboBox.SelectedItem.ToString();
            if (modName is null)
            {
                return;
            }
            string[] possibleMods = mods.Where(z => Root.ReadJson(z)?.ModDefinition.Name == modName).ToArray();
            switch (possibleMods.Length)
            {
                case 1:
                    Root extractedMod = Root.ReadJson(possibleMods[0]) ??
                                        throw new FileFormatException("Failed to Load Mod");
                    LoadedModDefinition = extractedMod.ModDefinition;
                    LoadedModReplacements = extractedMod.ModReplacements ?? new ModReplacements();
                    string containingFolder = Path.GetDirectoryName(possibleMods[0]) ??
                                              throw new IOException("Mod Path Not Found");
                    LoadedModPath = containingFolder;
                    Directory.CreateDirectory($@"{containingFolder}\cards");
                    Directory.CreateDirectory($@"{containingFolder}\units");
                    Directory.CreateDirectory($@"{containingFolder}\music");
                    break;
                case 0:
                    throw new InvalidOperationException("No Mods...this really shouldn't be possible");
                default:
                    int x = 1;
                    foreach (string modPath in possibleMods)
                    {
                        Root? m = Root.ReadJson(modPath);
                        if (m is null)
                        {
                            continue;
                        }
                        m.ModDefinition.Name += x;
                        ++x;
                        Root.WriteJson(m, modPath);
                    }
                    SelectedModComboBox_SelectionChanged(sender, e);
                    return;
            }

            if (Path.GetFileNameWithoutExtension(possibleMods[0]) == "mod")
            {
                DisableModButton.Content = "Disable Mod";
            }
            else
            {
                DisableModButton.Content = "Enable Mod";
            }

            if (!File.Exists($@"{LoadedModPath}\preview.png"))
            {
                Preview.ImageSource = null;
                Text16By9.Text = "For best results:\nUpload a 16:9 image";
                return;
            }

            BitmapImage bi = CreateBitmap($@"{LoadedModPath}\preview.png");
            Text16By9.Text = "";
            Preview.ImageSource = bi;
        }

        private static BitmapImage CreateBitmap(string path)
        {
            BitmapImage bi = new();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(File.ReadAllBytes(path));
            bi.EndInit();
            return bi;
        }

        private void NewModButton_OnClick(object sender, RoutedEventArgs e)
        {
            var newMod = new NewMod(){Owner = this};
            newMod.ShowDialog();
            if (!UpdateModsLoaded())
            {
                Close();
                return;
            }
            SelectedModComboBox.SelectedItem = newMod.NewModName ?? SelectedModComboBox.SelectedItem;
        }

        private void SelectedModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            config[1] = SelectedModeComboBox.SelectedIndex.ToString();
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
                case "Modify Mod Definition":
                    NewMod newMod = new(LoadedModDefinition) {Owner = this};
                    newMod.ShowDialog();
                    UpdateModsLoaded();
                    SelectedModComboBox.SelectedItem = newMod.NewModName ?? SelectedModComboBox.SelectedItem;
                    break;
                default:
                    MessageBox.Show("Error");
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (string f in Directory.GetFiles(Temp).Where(z => Path.GetExtension(z) != ".config"))
            {
                File.Delete(f);
            }

            File.WriteAllLines(@$"{Temp}\oj.config", config);
        }

        //Click on image
        private void Viewbox_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ReplacePreview();
        }

        private void ReplacePreview()
        {
            OpenFileDialog o = new()
            {
                Filter = "All Images|*.BMP;*.DIB;*.RLE;*.JPG;*.JPEG;*.JPE;*.JFIF;*.GIF;*.TIF;*.TIFF;*.PNG|BMP Files: (*.BMP;*.DIB;*.RLE)|*.BMP;*.DIB;*.RLE|JPEG Files: (*.JPG;*.JPEG;*.JPE;*.JFIF)|*.JPG;*.JPEG;*.JPE;*.JFIF|GIF Files: (*.GIF)|*.GIF|TIFF Files: (*.TIF;*.TIFF)|*.TIF;*.TIFF|PNG Files: (*.PNG)|*.PNG|All Files|*.*"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }

            MagickImage m = new(o.FileName);
            m.Resize(448, 252);
            m.Format = MagickFormat.Png;
            m.Write($@"{LoadedModPath}\preview.png");

            Text16By9.Text = "";
            Preview.ImageSource = CreateBitmap($@"{LoadedModPath}\preview.png");
        }

        private void OpenOJ_OnClick(object sender, RoutedEventArgs e)
        {
            if (SteamVersion)
            {
                Process.Start(@"C:\Program Files (x86)\Steam\steam.exe", @"steam://rungameid/282800");
            }
            else
            {
                Process.Start($@"{GameDirectory}\100orange.exe");
            }
        }

        private void DisableModButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DisableModButton.Content is "Disable Mod")
            {
                if(File.Exists(@$"{LoadedModPath}\mod.json"))
                {
                    File.Move(@$"{LoadedModPath}\mod.json", @$"{LoadedModPath}\disabled_mod.json");
                    DisableModButton.Content = "Enable Mod";
                }
                else
                {
                    MessageBox.Show("Can't find json file");
                }
            }
            else
            {
                if (File.Exists(@$"{LoadedModPath}\disabled_mod.json"))
                {
                    File.Move(@$"{LoadedModPath}\disabled_mod.json", @$"{LoadedModPath}\mod.json");
                    DisableModButton.Content = "Disable Mod";
                }
                else
                {
                    MessageBox.Show("Please rename your json file to mod.json, then reload the app");
                    Process.Start("explorer.exe", LoadedModPath);
                }
            }
        }

        private void DeleteModButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure about this? This won't just delete the json file, it will delete the entire directory. " +
                "If you are only having issues with the mod builder, try the repair json option or manually edit the json file.",
                "Warning", MessageBoxButton.YesNo);
            if (result is not MessageBoxResult.Yes)
            {
                return;
            }

            Directory.Delete(LoadedModPath, true);
            Process.Start(ExePath);
            Close();
        }

        private void RepairModButton_OnClick(object sender, RoutedEventArgs e)
        {
            Root.RepairMod(ref LoadedModReplacements);
            Root.WriteJson();
        }

        private void ValidateModButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Root.IsValidMod(LoadedModReplacements, out List<string> missingFiles))
            {
                MessageBox.Show("Mod files all exist. Your mod shouldn't have any errors");
            }
            else
            {
                MessageBox.Show(
                    "Your mod is missing these files. Either replace them or click repair mod to remove " +
                    $"references to the files{Environment.NewLine}{string.Join(Environment.NewLine, missingFiles)}");
            }
        }

        private void CleanDirectory_OnClick(object sender, RoutedEventArgs e)
        {
            int RedundantFiles = 0;
            foreach (string filePath in Directory.GetFiles($@"{LoadedModPath}\music"))
            {
                string testName = $@"music\{Path.GetFileNameWithoutExtension(filePath)}";
                if (LoadedModReplacements.Music.Any(z => z.File == testName))
                {
                    continue;
                }
                File.Delete(filePath);
                ++RedundantFiles;
            }

            foreach (string filePath in Directory.GetFiles($@"{LoadedModPath}\cards"))
            {
                string testName = $@"cards\{Path.GetFileNameWithoutExtension(filePath)}";
                if (LoadedModReplacements.Textures.Any(z => z.Path == testName))
                {
                    continue;
                }

                testName = $@"units\{Path.GetFileNameWithoutExtension(filePath)}";
                if (LoadedModReplacements.Textures.Any(z => z.Path == testName))
                {
                    continue;
                }

                File.Delete(filePath);
                ++RedundantFiles;
            }

            MessageBox.Show(RedundantFiles == 0 ? "Directory already clean" : $"{RedundantFiles} extra files removed");
        }
    }
}
