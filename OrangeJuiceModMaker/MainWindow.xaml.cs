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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ImageMagick;
using Newtonsoft.Json;
using OrangeJuiceModMaker.Data;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SearchOption = System.IO.SearchOption;

namespace OrangeJuiceModMaker
{
    public partial class MainWindow
    {
        public static MainWindow? Instance { get; private set; }
        //public readonly UpdateApp? UpdateApp;
        public bool Debug { get; set; }

        public UnitRef[] Units { get; set; } = null!;
        public static string? GameDirectory { get; set; }
        private static bool _steamVersion { get; set; }
        public static bool UnpackComplete { get; set; } = true;
        public string Temp { get; }
        public string AppData { get; }
        public ModReplacements LoadedModReplacements { get; set; } = new();
        public ModDefinition LoadedModDefinition { get; set; } = new("temp", "desc", "auth", 2);
        public string LoadedModPath { get; set; } = "";
        public GlobalSettings GlobalSettings { get; set; }
        private static bool _exitTime { get; set; }
        private List<string> mods { get; set; } = [];
        private string[] config { get; set; }
        private string modsDirectoryPath { get; set; } = "";
        private string exePath { get; set; } = "";
        public CardRef[] Cards { get; set; } = null!;
        public MusicRef[] Musics { get; set; } = null!;

        public SoundRef[] Sounds { get; set; } = null!;

        private static string[]? _newHash { get; set; }
        public static string? WorkshopItemsDirectory { get; set; } = null;
        public event EventHandler LoadedModsChanged;

        private const string DisableMod = " Disable Mod ";
        private const string EnableMod = " Enable Mod ";


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
            string[] error = [
                DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "",
                exception.StackTrace ?? ""
            ];
            File.AppendAllLines("main_error.error", error);
            MessageBox.Show("Error in load, see main_error.error for more information");
            Close();
        }

        public MainWindow(bool debug)
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
                : ["0", "0"];

            GlobalSettings = new GlobalSettings(true);

            LoadedModsChanged += MainWindow_LoadedModsChanged;

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

                GlobalSettings = GlobalSettings.LoadSettingsFromFile($@"{AppData}\GlobalSettings.json");

                DebugLogger.LogLine("Setting up app data");
                exePath = $@"{Directory.GetCurrentDirectory()}\OrangeJuiceModMaker.exe";
                string exeDirectory = Directory.GetCurrentDirectory();
                //UpdateApp = new UpdateApp(app, downloadPath, debug, exeDirectory);

                Directory.CreateDirectory(AppData);
                Directory.SetCurrentDirectory(AppData);
                Directory.CreateDirectory(Temp);

                //First Time App Data Setup
                string[] files =
                [
                    "7za.dll",
                    "7za.exe",
                    "ffmpeg.7z",
                    "oj.version",
                    "sound.json",
                    "music.json",
                    "units.json",
                    "cards.json",
                ];
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

                Units = JsonConvert.DeserializeObject<UnitRef[]>(File.ReadAllText("units.json"))!.OrderBy(z => z.UnitName).ToArray();
                Cards = JsonConvert.DeserializeObject<CardRef[]>(File.ReadAllText("cards.json"))!.OrderBy(z => z.CardName).ToArray();
                Musics = JsonConvert.DeserializeObject<MusicRef[]>(File.ReadAllText("music.json"))!.OrderBy(z => z.UnitId ?? z.Event).ToArray();
                Sounds = JsonConvert.DeserializeObject<SoundRef[]>(File.ReadAllText("sound.json"))!.OrderBy(z => z.File).ToArray();

                _steamVersion = File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\100 Orange Juice\100orange.exe");
                //First Time Setup Code
                DebugLogger.LogLine("Setting up game directory");
                if (_steamVersion)
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
                _newHash = NewHashStrings();

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
                    File.WriteAllLines($@"{AppData}\pakfiles.hash", _newHash);

                }

                DebugLogger.LogLine("Loading mods");
                if (GlobalSettings.ModDirectories.SelectedItem is null)
                {
                    if (GlobalSettings.ModDirectories.Items.Count != 0)
                    {
                        GlobalSettings.ModDirectories.SelectedIndex = 0;
                    }
                    else
                    {
                        GlobalSettings.ModDirectories =
                            new GlobalSettings.SettingsList<string>([$@"{GameDirectory}\mods"], 0);
                    }
                }

                modsDirectoryPath = GlobalSettings.ModDirectories.SelectedItem!;

                OnLoadedModsChanged();

                App.ShowHideConsole(debug);

                DebugLogger.LogLine("Main window initialized");

                _ = new Task(() =>
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
            }
            catch (Exception exception)
            {
                string[] error =
                    [DateTime.Now.ToString(CultureInfo.InvariantCulture), exception.GetType().ToString(), exception.Message, exception.StackTrace ?? "", exception.StackTrace ?? ""];
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

        private void MainWindow_LoadedModsChanged(object? sender, EventArgs e)
        {
            if (UpdateModsLoaded() is false)
            {
                Close();
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

            return oldHashStrings.Zip(_newHash!).Any(z => z.First != z.Second);
        }

        private static string[] NewHashStrings()
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream graphicsPakFileStream = File.Open($@"{GameDirectory}\data\graphics.pak", FileMode.Open);
            byte[] unitHash = sha256.ComputeHash(graphicsPakFileStream);
            return [
                Convert.ToBase64String(unitHash),
            ];
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EditButton.IsEnabled = false;
            //Library.FFmpegDirectory = @$"{AppData}\ffmpeg";
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

            EditButton.IsEnabled = true;
        }

        //Check the game directory for mod files
        private bool UpdateModsLoaded()
        {
            //Load a mod
            mods = [.. Directory.GetFiles(path: modsDirectoryPath, searchPattern: "*.json", searchOption: SearchOption.AllDirectories)];

            //The zero mod case
            if (mods.Count == 0)
            {
                new NewMod(this) { Owner = IsLoaded ? this : null }.ShowDialog();
                mods = [.. Directory.GetFiles(modsDirectoryPath, "*.json", SearchOption.AllDirectories)];
                if (mods.Count == 0)
                {
                    return false;
                }
            }

            string?[]? itemsSource = mods.Select(z => Root.ReadJson(z)?.ModDefinition.Name).ToArray() ?? throw new Exception("No mods?");
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
            string[] possibleMods = [.. mods.Where(z => Root.ReadJson(z)?.ModDefinition.Name == modName)];
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
            OnLoadedModsChanged();
            SelectedModComboBox.SelectedItem = newMod.NewModName ?? SelectedModComboBox.SelectedItem;
        }

        private void SelectedModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            config[1] = SelectedModeComboBox.SelectedIndex.ToString();
        }

        public enum EditWindow
        {
            ModifyUnit = 0,
            ModifyCard = 1,
            ModifyMusic = 2,
            ModifySoundEffect = 3,
            ModifyModDefinition = 4,
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditWindow? editWindow = (SelectedModeComboBox.SelectedItem as string) switch
            {
                "Modify Unit" => EditWindow.ModifyUnit,
                "Modify Card" => EditWindow.ModifyCard,
                "Modify Music" => EditWindow.ModifyMusic,
                "Modify Sound Effect" => EditWindow.ModifySoundEffect,
                "Modify Mod Definition" => EditWindow.ModifyModDefinition,
                _ => null
            };

            if (editWindow is null)
            {
                return;
            }

            OpenEditWindow(editWindow.Value);
        }

        private void OpenEditWindow(EditWindow windowName)
        {
            switch (windowName)
            {
                case EditWindow.ModifyUnit:
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
                case EditWindow.ModifyCard:
                    new ModifyCard(this) { Owner = this }.ShowDialog();
                    return;
                case EditWindow.ModifyMusic:
                    new ModifyMusic(this) { Owner = this }.ShowDialog();
                    return;
                case EditWindow.ModifySoundEffect:
                    new ModifySoundEffect(this) { Owner = this }.ShowDialog();
                    return;
                case EditWindow.ModifyModDefinition:
                    NewMod newMod = new(this, LoadedModDefinition) { Owner = this };
                    newMod.ShowDialog();
                    OnLoadedModsChanged();
                    SelectedModComboBox.SelectedItem = newMod.NewModName ?? SelectedModComboBox.SelectedItem;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(windowName), windowName, null);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (string f in Directory.GetFiles(Temp).Where(z => Path.GetExtension(z) != ".config"))
            {
                File.Delete(f);
            }

            File.WriteAllLines(@$"{Temp}\oj.config", config);

            if (GlobalSettings.MirrorDirectories.SelectedItem is null or "None")
            {
                return;
            }

            string mirror = GlobalSettings.MirrorDirectories.SelectedItem;

            if (!Directory.Exists(mirror))
            {
                Directory.CreateDirectory(mirror);
            }

            ProcessStartInfo p = new()
            {
                CreateNoWindow = !Debug,
                WindowStyle = Debug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                FileName = "CMD.exe",
                Arguments = $"/c robocopy /MIR /MT:8 \"{modsDirectoryPath}\" \"{mirror}\""
            };
            Process.Start(p)?.WaitForExit();

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
            if (_steamVersion)
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
            Root.RepairMod(LoadedModReplacements, LoadedModPath);
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
            OptionsMenu optionsMenu = new(this) { Owner = this };
            optionsMenu.ShowDialog();
            if (optionsMenu.ImportedMod is null)
            {
                return;
            }

            SelectedModComboBox.SelectedItem = optionsMenu.ImportedMod;
        }

        public void OnLoadedModsChanged()
        {
            LoadedModsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void NewMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            NewModButton_OnClick(sender, e);
        }

        private void LoadFromFileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".json",
                Filter = "Mod.json|mod.json|.json File|*.json"
            };
            if (open.ShowDialog() is not true)
            {
                return;
            }

            if (open.FileName.StartsWith(modsDirectoryPath))
            {
                //Do nothing
            }
            else
            {
                DirectoryInfo? sourceFolder = Directory.GetParent(open.FileName) ?? throw new NullReferenceException();
                if (Directory.Exists($"{modsDirectoryPath}\\{sourceFolder.Name}"))
                {
                    MessageBoxResult result = MessageBox.Show("Mod already exists in collection, overwrite?",
                        "Folder exists in mod directory", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.Delete($"{modsDirectoryPath}\\{sourceFolder.Name}", true);
                    }
                    else
                    {
                        return;
                    }
                }
                CloneDirectory(sourceFolder.FullName, $"{modsDirectoryPath}\\{sourceFolder.Name}");
                OnLoadedModsChanged();
            }
        }

        private void LoadFromWorkshopMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            EditSettings(sender, e);
        }

        private void OpenGameMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenOJ_OnClick(sender, e);
        }

        private void SaveAsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folder = new()
            {
                ShowNewFolderButton = true,
                AutoUpgradeEnabled = true,
            };
            DialogResult result = folder.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            if (string.IsNullOrEmpty(folder.SelectedPath))
            {
                return;
            }

            CloneDirectory(LoadedModPath, folder.SelectedPath);
            OnLoadedModsChanged();
        }

        private static void CloneDirectory(string oldFolder, string newFolder)
        {
            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(newFolder);
            }

            foreach (string directory in Directory.GetDirectories(oldFolder).Where(Directory.Exists))
            {
                string subFolderPath = newFolder + directory.Remove(0, oldFolder.Length);
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                }
            }

            foreach (string file in Directory.GetFiles(oldFolder, "*.*", SearchOption.AllDirectories))
            {
                string newPath = newFolder + file.Remove(0, oldFolder.Length);
                File.Copy(file, newPath, true);
            }
        }

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EditNameMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(EditWindow.ModifyModDefinition);

        }

        private void EditDefinitionMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(EditWindow.ModifyModDefinition);
        }

        private void EditUnitMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(EditWindow.ModifyUnit);
        }

        private void EditCardMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(EditWindow.ModifyCard);
        }

        private void EditMusicMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(EditWindow.ModifyMusic);
        }

        private void EditSoundEffectMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(EditWindow.ModifySoundEffect);
        }

        private void EditSettingsMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            EditSettings(sender, e);
        }

        private void ToolsOpenModFolderMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenModDirectory_OnClick(sender, e);
        }

        private void ToolsOpenJsonMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenModDotJson_OnClick(sender, e);
        }

        private void ToolsRepairMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            RepairModButton_OnClick(sender, e);
        }

        private void ToolsCleanMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            CleanDirectory_OnClick(sender, e);
        }

        private void ToolsDeleteModMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            DeleteModButton_OnClick(sender, e);
        }
    }
}
