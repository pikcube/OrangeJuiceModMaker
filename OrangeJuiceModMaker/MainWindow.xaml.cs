using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Newtonsoft.Json;
using Unosquare.FFME;
using Unosquare.FFME.Common;
using MediaElement = Unosquare.FFME.MediaElement;
using SearchOption = System.IO.SearchOption;

namespace OrangeJuiceModMaker
{
    public partial class MainWindow
    {
        public static MainWindow? Instance { get; private set; }
        //public readonly UpdateApp? UpdateApp;
        public readonly bool Debug;
        public static string? GameDirectory;
        public static readonly MediaElement MusicPlayer = new();
        private static bool steamVersion;
        public static bool UnpackComplete = true;
        public readonly string Temp;
        public readonly string AppData;
        public ModReplacements LoadedModReplacements = new();
        public ModDefinition LoadedModDefinition = new("temp", "desc", "auth", 2);
        public string LoadedModPath = "";
        public readonly List<CsvHolder> CsvFiles = new();
        public readonly List<Unit> UnitHyperTable = new();
        public CsvHolder? FlavorLookUp;
        public readonly List<string> Cards = new();
        public readonly GlobalSettings globalSettings;
        private static bool exitTime;
        private List<string> mods = new();
        private readonly string[] config;
        private readonly string modsDirectoryPath = "";
        private readonly string exePath = "";
        private static string[] newHash;
        private readonly Task flavorTask = Task.CompletedTask;
        private readonly Task dumpTempFiles = Task.CompletedTask;
        private readonly Task loadHyperData = Task.CompletedTask;
        private readonly Task loadOjData = Task.CompletedTask;
        public static string? WorkshopItemsDirectory = null;

        private const string DisableMod = " Disable Mod ";
        private const string EnableMod = " Enable Mod ";


        public static bool ExitTime
        {
            set
            {
                if (exitTime) return;
                if (!value) return;
                exitTime = true;
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
            File.AppendAllLines("main_error.error", error);
            MessageBox.Show("Error in load, see main_error.error for more information");
            Close();
        }

        public MainWindow(bool debug, App app, string downloadPath)
        {
            Instance = this;
            DebugLogger.Initialize(debug);
            DebugLogger.LogLine("Initializing Constants");
            Debug = debug;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Temp = @$"{appData}\OrangeJuiceModMaker\temp";
            AppData = $@"{appData}\OrangeJuiceModMaker";
            InitializeComponent();
            config = File.Exists($@"{Temp}\oj.config")
                ? File.ReadAllLines($@"{Temp}\oj.config")
                : new[] { "0", "0" };
            MusicPlayer.LoadedBehavior = MediaPlaybackState.Pause;
            MusicPlayer.UnloadedBehavior = MediaPlaybackState.Manual;
            globalSettings = new GlobalSettings(true);

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            try
            {
                if (!Directory.Exists(AppData))
                {
                    Directory.CreateDirectory(AppData);
                }
                if (!File.Exists($@"{AppData}\GlobalSettings.json"))
                {
                    File.WriteAllText($@"{AppData}\GlobalSettings.json", JsonConvert.SerializeObject(new GlobalSettings(true)));
                }

                globalSettings = GlobalSettings.LoadSettingsFromFile($@"{AppData}\GlobalSettings.json");
                
                DebugLogger.LogLine("Setting up app data");
                exePath = $@"{Directory.GetCurrentDirectory()}\OrangeJuiceModMaker.exe";
                string exeDirectory = Directory.GetCurrentDirectory();
                //UpdateApp = new UpdateApp(app, downloadPath, debug, exeDirectory);

                Directory.CreateDirectory(AppData);
                Directory.SetCurrentDirectory(AppData);
                Directory.CreateDirectory(Temp);

                //First Time App Data Setup
                string[] files =
                {
                    "7za.dll",
                    "7za.exe",
                    "HyperLookupTable.csv",
                    "FlavorLookUp.csv",
                    "ffmpeg.7z",
                    "csvFiles.7z",
                    "oj.version"
                };
                if (!File.Exists($@"{AppData}\oj.version") || File.ReadAllBytes($@"{AppData}\oj.version") != File.ReadAllBytes($@"{exeDirectory}\OrangeJuiceModMaker\oj.version"))
                {
                    foreach (string file in Directory.GetFiles(AppData).Where(z => Path.GetExtension(z) == ".error"))
                    {
                        File.Delete(file);
                    }
                    DebugLogger.LogLine("Copying over data files");
                    foreach (string file in files)
                    {
                        File.Copy(@$"{exeDirectory}\OrangeJuiceModMaker\{file}", @$"{AppData}\{file}", true);
                    }
                }
                steamVersion = File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\100 Orange Juice\100orange.exe");
                //First Time Setup Code
                DebugLogger.LogLine("Setting up game directory");
                if (steamVersion)
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

                        open.Title = "Please locate your 100% Orange Juice game executable";

                        bool keepSearching = true;

                        do
                        {
                            if (open.ShowDialog() == true)
                            {
                                GameDirectory = Path.GetDirectoryName(open.FileName);
                                if (GameDirectory is null)
                                {
                                    Environment.Exit(0);
                                    return;
                                }
                                if (!Directory.Exists($@"{GameDirectory}\data"))
                                {
                                    if (!HelpFindSteamDirectory())
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    File.WriteAllText("gamedirectory.path", GameDirectory);
                                    keepSearching = false;
                                }
                            }
                            else
                            {
                                Close();
                                return;
                            }
                        } while (keepSearching);
                    }
                }

                WorkshopItemsDirectory = $@"{GameDirectory}\..\..\workshop\content\282800";
                WorkshopItemsDirectory = Path.GetFullPath(WorkshopItemsDirectory);

                if (!Directory.Exists(WorkshopItemsDirectory))
                {
                    WorkshopItemsDirectory = null;
                }


                //Hash the pak files to see if we need a new unpack. Don't bother making this async, it's load blocking
                newHash = NewHashStrings();

                //Unpack Base Files
                if (IsNotUnpacked())
                {
                    App.ShowHideConsole(debug);
                    DebugLogger.LogLine("Unpacking pakFiles");
                    Directory.CreateDirectory(@$"{AppData}\pakFiles");
                    File.WriteAllText(@$"{AppData}\pakFiles\filesUnpacked.status", "false");
                    UnpackComplete = false;
                    new UnpackFiles(GameDirectory, AppData, debug).ShowDialog();
                    if (!UnpackComplete)
                    {
                        MessageBox.Show("Unpack Failed");
                        Close();
                        return;
                    }
                    File.Delete(@"pakFiles\filesUnpacked.status");
                    File.WriteAllLines($@"{AppData}\pakfiles.hash", newHash);
                    
                }

                Cards = Directory.GetFiles(@"pakFiles\cards").ToList();

                DebugLogger.LogLine("Loading mods");
                if (globalSettings.ModDirectories.SelectedItem is null)
                {
                    if (globalSettings.ModDirectories.Items.Any())
                    {
                        globalSettings.ModDirectories.SelectedIndex = 0;
                    }
                    else
                    {
                        globalSettings.ModDirectories =
                            new GlobalSettings.SettingsList<string>(new List<string> { $@"{GameDirectory}\mods" }, 0);
                    }
                }

                modsDirectoryPath = globalSettings.ModDirectories.SelectedItem!;

                if (!UpdateModsLoaded())
                {
                    Close();
                    Environment.Exit(0);
                    return;
                }

                App.ShowHideConsole(debug);

                DebugLogger.LogLine("Main window initialized");

                flavorTask = new Task(() =>
                {
                    DebugLogger.LogLine("Reading FlavorLookUp");
                    FlavorLookUp = new CsvHolder($@"{AppData}\FlavorLookUp.csv");
                    foreach (string[] t in FlavorLookUp.Rows)
                    {
                        for (int m = 0; m < t.Length; ++m)
                        {
                            t[m] = t[m].Replace(@"\n", Environment.NewLine);
                        }
                    }
                });

                dumpTempFiles = new Task(() =>
                {
                    if (File.Exists("ffmpeg.7z"))
                    {
                        DebugLogger.LogLine("Setting up ffmpeg");
                        ProcessStartInfo unpackInfo = new()
                        {
                            Arguments = @"e ffmpeg.7z -offmpeg -y",
                            FileName = "7za.exe",
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        };
                        Process.Start(unpackInfo)?.WaitForExit();
                        File.Delete("ffmpeg.7z");
                    }
                    else
                    {
                        MessageBox.Show("Please download ffmpeg executable");
                        Environment.Exit(0);
                    }

                    DebugLogger.LogLine("Cleaning up temp files");
                    foreach (string? f in Directory.GetFiles(Temp).Where(z => Path.GetExtension(z) != ".config"))
                    {
                        File.Delete(f);
                    }
                });
                loadOjData = new Task(() =>
                {
                    DebugLogger.LogLine("Fetching csvFiles");
                    if (File.Exists("csvFiles.7z"))
                    {
                        DebugLogger.LogLine("Unpacking csv files");
                        ProcessStartInfo unpackInfo = new()
                        {
                            Arguments = @"e csvFiles.7z -y -ocsvFiles",
                            FileName = "7za.exe",
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true
                        };
                        Process.Start(unpackInfo)?.WaitForExit();
                        File.Delete("csvFiles.7z");
                    }
                    string[] csvFileList = Directory.GetFiles("csvFiles");
                    const int count = 23;
                    if (csvFileList.Length != count)
                    {
                        throw csvFileList.Length > count
                            ? new Exception($"There's more than {count} csv files. Did you forget to update line 331 again?")
                            : new Exception("Didn't find all files");
                    }
                    foreach (string file in csvFileList)
                    {
                        DebugLogger.LogLine($"Reading {file}");
                        CsvFiles.Add(new CsvHolder(file));
                    }
                });
                loadHyperData = new Task(() =>
                {
                    DebugLogger.LogLine("Preparing to load in HyperLookupTable");
                    using TextFieldParser parser = new("HyperLookupTable.csv");
                    parser.Delimiters = new[] { "," };
                    parser.HasFieldsEnclosedInQuotes = true;
                    _ = parser.ReadFields();
                    loadOjData.Wait();
                    DebugLogger.LogLine("Loading HyperLookupTable");
                    CsvHolder charCards = CsvFiles.First(z => z.Name == "CharacterCards");
                    while (!parser.EndOfData)
                    {
                        UnitHyperTable.Add(new Unit(parser.ReadFields() ?? throw new InvalidOperationException(), charCards, this));
                    }
                });
                //CreateLookUp();
            }
            catch (Exception exception)
            {
                string[] error =
                    { DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? "" };
                File.AppendAllLines("main_error.error", error);
                if (debug)
                {
                    Console.WriteLine(error.AsString());
                }
                else
                {
                    MessageBox.Show("Error in load, see main_error.error for more information");
                }
                Close();
            }

            bool HelpFindSteamDirectory()
            {
                MessageBoxResult result = MessageBox.Show(
                    "Could not find game data. Do you need help locating your game directory?",
                    "Make sure you select the game executable, not the mod installer.", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        switch (MessageBox.Show("Did you install the game through steam?",
                                    "How did you install the game", MessageBoxButton.YesNoCancel))
                        {
                            case MessageBoxResult.Yes:
                                MessageBox.Show(
                                    "Open steam and select library (press OK to continue)");
                                MessageBox.Show("Click on 100% Orange Juice in the side bar");
                                MessageBox.Show(
                                    "Right click the game in the side bar, select Manage -> Browse Local Files");
                                MessageBox.Show(
                                    "You should see a few folders and files, one of them being 100orange.exe");
                                MessageBox.Show(
                                    "Select the path by clicking in the address bar at the top of the window (some text should appear blue). " +
                                    "Then right click and press copy.");
                                MessageBox.Show(
                                    "When the file picker appears again in a second, you can paste the path by clicking the " +
                                    "address bar, deleting the text, right clicking, and clicking paste. Then press enter.");
                                break;
                            case MessageBoxResult.No:
                                switch (MessageBox.Show("Did you install the game to an external drive?",
                                            "Where did you install the game?",
                                            MessageBoxButton.YesNoCancel))
                                {
                                    case MessageBoxResult.Yes:
                                        MessageBox.Show(
                                            "Look on the external drive for the executable");
                                        break;
                                    case MessageBoxResult.No:
                                        MessageBox.Show(
                                            "You likely installed the game in either program files or program files x86");
                                        break;
                                    case MessageBoxResult.None:
                                    case MessageBoxResult.OK:
                                    case MessageBoxResult.Cancel:
                                    default:
                                        Close();
                                        return false;
                                }

                                break;
                            case MessageBoxResult.None:
                            case MessageBoxResult.OK:
                            case MessageBoxResult.Cancel:
                            default:
                                Close();
                                return false;
                        }

                        break;
                    case MessageBoxResult.No:
                        break;
                    case MessageBoxResult.Cancel:
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    default:
                        Close();
                        return false;
                }

                return true;
            }
        }

        private bool IsNotUnpacked()
        {
            if (!Directory.Exists(@$"{AppData}\pakFiles"))
            {
                return true;
            }

            if (File.Exists($@"{AppData}\pakFiles\filesUnpacked.status"))
            {
                return true;
            }

            if (!File.Exists($@"{AppData}\pakfiles.hash"))
            {
                return true;
            }
            
            Task<string[]> oldHashTask = File.ReadAllLinesAsync($@"{AppData}\pakfiles.hash");

            string[] oldHashStrings = oldHashTask.GetAwaiter().GetResult();

            return oldHashStrings.Zip(newHash).Any(z => z.First != z.Second);
        }

        private static string[] NewHashStrings()
        {
            SHA256 sha256 = SHA256.Create();



            byte[] unitHashTask = sha256.ComputeHash(new MemoryStream(File.ReadAllBytes($@"{GameDirectory}\data\units.pak")));
            byte[] cardsHashTask = sha256.ComputeHash(new MemoryStream(File.ReadAllBytes($@"{GameDirectory}\data\cards.pak")));

            return new []{
                Convert.ToBase64String(unitHashTask),
                Convert.ToBase64String(cardsHashTask)
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EditButton.IsEnabled = false;
            Library.FFmpegDirectory = @$"{AppData}\ffmpeg";
            MusicPlayer.ScrubbingEnabled = true;
            MusicPlayer.LoopingBehavior = MediaPlaybackState.Manual;
            SelectedModComboBox.SelectedIndex = config[0].ToInt();
            SelectedModeComboBox.ItemsSource = new[]
            {
                "Modify Unit",
                "Modify Card",
                "Modify Music",
                "Modify Sound Effect",
                "Modify Mod Definition"
            };
            SelectedModeComboBox.SelectedIndex = config[1].ToInt();
            if (SelectedModeComboBox.SelectedIndex is >= 3 or -1)
            {
                SelectedModeComboBox.SelectedIndex = 0;
            }

            loadOjData.Start();
            loadHyperData.Start();
            flavorTask.Start();
            dumpTempFiles.Start();

            await loadOjData;
            await loadHyperData;
            await flavorTask;
            AggregateException? ex = loadOjData.Exception ?? loadHyperData.Exception ?? flavorTask.Exception ?? null;

            
            if (ex is not null)
            {
                MessageBox.Show(ex.Message);
                Close();
                return;
            }
            EditButton.IsEnabled = true;
            await dumpTempFiles;
        }

        //Check the game directory for mod files
        private bool UpdateModsLoaded()
        {
            //Load a mod
            mods = Directory.GetFiles(path: modsDirectoryPath, searchPattern: "*.json", searchOption: SearchOption.AllDirectories).ToList();

            //The zero mod case
            if (mods.Count == 0)
            {
                new NewMod(this) { Owner = IsLoaded ? this : null }.ShowDialog();
                mods = Directory.GetFiles(modsDirectoryPath, "*.json", SearchOption.AllDirectories).ToList();
                if (mods.Count == 0)
                {
                    return false;
                }
            }

            string?[]? itemsSource = mods.Select(z => Root.ReadJson(z)?.ModDefinition.Name).ToArray();
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
                    foreach (string path in possibleMods)
                    {
                        Root? m = Root.ReadJson(path);
                        if (m is null)
                        {
                            continue;
                        }
                        m.ModDefinition.Name += x;
                        ++x;
                        Root.WriteJson(m, path);
                    }
                    SelectedModComboBox_SelectionChanged(sender, e);
                    return;
            }

            DisableModButton.Content = Path.GetFileNameWithoutExtension(possibleMods[0]) == "mod" ? DisableMod : EnableMod;

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
            NewMod newMod = new(this) { Owner = this };
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
                    return;
                case "Modify Unit":
                    try
                    {
                        new ModifyUnit(this) { Owner = this }.ShowDialog();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        throw;
                    }
                    return;
                case "Modify Card":
                    new ModifyCard(CsvFiles, this) { Owner = this }.ShowDialog();
                    return;
                case "Modify Music":
                    new ModifyMusic(this) { Owner = this }.ShowDialog();
                    return;
                case "Modify Sound Effect":
                    new ModifySoundEffect(this) { Owner = this }.ShowDialog();
                    return;
                case "Modify Mod Definition":
                    NewMod newMod = new(this, LoadedModDefinition) { Owner = this };
                    newMod.ShowDialog();
                    UpdateModsLoaded(); 
                    SelectedModComboBox.SelectedItem = newMod.NewModName ?? SelectedModComboBox.SelectedItem;
                    return;
                default:
                    MessageBox.Show("Error");
                    return;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (string f in Directory.GetFiles(Temp).Where(z => Path.GetExtension(z) != ".config"))
            {
                File.Delete(f);
            }

            File.WriteAllLines(@$"{Temp}\oj.config", config);

            if (globalSettings.MirrorDirectories.SelectedItem is null or "None")
            {
                return;
            }

            string mirror = globalSettings.MirrorDirectories.SelectedItem;

            if (!Directory.Exists(mirror))
            {
                Directory.CreateDirectory(mirror);
            }

            var p = new ProcessStartInfo();
            p.CreateNoWindow = !Debug;
            p.WindowStyle = Debug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
            p.FileName = "CMD.exe";
            p.Arguments = $"/c robocopy /MIR /MT:8 \"{modsDirectoryPath}\" \"{mirror}\"";
            Process.Start(p).WaitForExit();

        }

        //Click on image
        private void Viewbox_MouseDown(object sender, MouseButtonEventArgs e)
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
            if (steamVersion)
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
            if (DisableModButton.Content is DisableMod)
            {
                if (File.Exists(@$"{LoadedModPath}\mod.json"))
                {
                    File.Move(@$"{LoadedModPath}\mod.json", @$"{LoadedModPath}\disabled_mod.json");
                    DisableModButton.Content = EnableMod;
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
                    DisableModButton.Content = DisableMod;
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
            Process.Start(exePath);
            Close();
        }

        private void RepairModButton_OnClick(object sender, RoutedEventArgs e)
        {
            Root.RepairMod(ref LoadedModReplacements, LoadedModPath);
            Root.WriteJson(LoadedModPath, LoadedModDefinition, LoadedModReplacements);
        }

        private void ValidateModButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Root.IsValidMod(LoadedModReplacements, LoadedModPath, out List<string> missingFiles))
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
            int redundantFiles = Root.CleanMod(LoadedModReplacements, LoadedModPath);
            MessageBox.Show(redundantFiles == 0 ? "Directory already clean" : $"{redundantFiles} extra files removed");
        }

        private void OpenModDirectory_OnClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(modsDirectoryPath))
            {
                ProcessStartInfo startInfo = new()
                {
                    Arguments = modsDirectoryPath,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }
            else
            {
                MessageBox.Show("Something has gone horribly wrong, application will now shutdown to save data");
                Close();
            }
        }

        private void OpenModDotJson_OnClick(object sender, RoutedEventArgs e)
        {
            if (OpenJson($@"{LoadedModPath}\mod.json"))
            {
                return;
            }

            if (OpenJson($@"{LoadedModPath}\disabled_mod.json"))
            {
                return;
            }

            string? mod = Directory.GetFiles(LoadedModPath, "*.json", SearchOption.AllDirectories).FirstOrDefault();

            if (mod is null)
            {
                return;
            }

            OpenJson(mod);

            static bool OpenJson(string path)
            {
                if (!File.Exists(path))
                {
                    return false;
                }
                
                ProcessStartInfo startInfo = new()
                {
                    FileName = "explorer",
                    Arguments = $@"{path}\"
                };
                Process.Start(startInfo);
                return true;
            }
        }

        private void EditSettings(object sender, RoutedEventArgs e)
        {
            OptionsMenu optionsMenu = new OptionsMenu(this) { Owner = this };
            optionsMenu.ShowDialog();
            if (optionsMenu.ForceRefresh)
            {
                UpdateModsLoaded();
            }
        }
    }
}
