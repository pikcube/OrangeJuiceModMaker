using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Newtonsoft.Json;
using static OrangeJuiceModMaker.GlobalSettings;
using MessageBox = System.Windows.MessageBox;

namespace OrangeJuiceModMaker
{
    public class GlobalSettings
    {
        private readonly string fileLocation;
        public SettingsList<string> ModDirectories { get; set; }
        public SettingsList<string> MirrorDirectories { get; set; }
        public SettingsList<string> AutoUpdate { get; set; }

        public GlobalSettings()
        {
            ModDirectories = new SettingsList<string>();
            MirrorDirectories = new SettingsList<string>();
            AutoUpdate = new SettingsList<string>();
            fileLocation =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\GlobalSettings.json";
        }

        public GlobalSettings(bool clean = false)
        {
            ModDirectories = new SettingsList<string>();
            MirrorDirectories = new SettingsList<string>();
            AutoUpdate = new SettingsList<string>();
            if (clean)
            {
                AutoUpdate = new SettingsList<string>(["Check for updates", "Skip this version", "Don't check for updates"], 0);
                MirrorDirectories = new SettingsList<string>(["None"], 0);
            }
            fileLocation =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\GlobalSettings.json";
        }

        public static GlobalSettings LoadSettingsFromFile(string path)
        {
            return JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText(path)) ??
                   new GlobalSettings(true);
        }

        public class SettingsList <T>
        {
            public readonly List<T> Items;
            
            public int SelectedIndex;

            public T? SelectedItem
            {
                get => SelectedIndex == -1 ? default(T) : Items[SelectedIndex];
                set => SelectedIndex = value is null ? -1 : Items.IndexOf(value);
            }

            public SettingsList(int selectedIndex = -1)
            {
                SelectedIndex = selectedIndex;
                Items = [];
            }

            public SettingsList(List<T> items, int selectedIndex = -1)
            {
                SelectedIndex = selectedIndex;
                Items = items.Select(z => z).ToList();
            }

            public static implicit operator List<T>(SettingsList<T> t) => t.Items;

            public static IEnumerable<T> ToIEnumerable(SettingsList<T> t) => t.Items;

            public static explicit operator SettingsList<T>(List<T> t) => new(t);
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(fileLocation, json);
        }
    }
    
    /// <summary>
    /// Interaction logic for OptionsMenu.xaml
    /// </summary>
    public partial class OptionsMenu : Window
    {
        private readonly MainWindow parent;
        private GlobalSettings GlobalSettings => parent.GlobalSettings;
        private string[] workshopModNames;
        private string[] workshopModPaths;

        public string? ImportedMod = null;

        public OptionsMenu(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
        }

        private void BackupDirectoryComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GlobalSettings.MirrorDirectories.SelectedIndex = backupDirectoryComboBox.SelectedIndex;
        }


        private void ModDirectoryComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GlobalSettings.ModDirectories.SelectedIndex = modDirectoryComboBox.SelectedIndex;
        }

        private void WorkshopModComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private async void OptionsMenu_OnLoaded(object sender, RoutedEventArgs e)
        {
            modDirectoryComboBox.ItemsSource = GlobalSettings.ModDirectories.Items;
            modDirectoryComboBox.SelectedIndex = GlobalSettings.ModDirectories.SelectedIndex;

            if (GlobalSettings.MirrorDirectories.Items.Count == 0)
            {
                GlobalSettings.MirrorDirectories = new SettingsList<string>(["None"], 0);
                GlobalSettings.Save();
            }

            backupDirectoryComboBox.ItemsSource = GlobalSettings.MirrorDirectories.Items;
            backupDirectoryComboBox.SelectedIndex = GlobalSettings.MirrorDirectories.SelectedIndex;
            if (MainWindow.WorkshopItemsDirectory is null)
            {
                NoWorkshopMods();
            }
            else
            {
                workshopModComboBox.IsEnabled = false;
                importButton.IsEnabled = false;
                await Task.Run(() =>
                {
                    string?[] mods = Directory.GetDirectories(MainWindow.WorkshopItemsDirectory)
                        .Where(z => File.Exists(z + @"\mod.json"))
                        .Where(z => Root.IsValidMod(z, out _))
                        .ToArray();
                    if (mods.Length == 0)
                    {
                        
                        return;
                    }

                    string?[] modNames = mods.Select(z => Root.ReadJson(z + @"\mod.json")?.ModDefinition.Name)
                        .ToArray();
                    if (modNames.Any(z => z is null) || mods.Any(z => z is null))
                    {
                        for (int n = 0; n < modNames.Length; ++n)
                        {
                            if (modNames[n] is null)
                            {
                                mods[n] = null;
                            }

                            if (mods[n] is null)
                            {
                                modNames[n] = null;
                            }
                        }
                    }

                    workshopModPaths = mods.Where(z => z is not null).ToArray()!;
                    workshopModNames = modNames.Where(z => z is not null).ToArray()!;
                });

                importButton.IsEnabled = true;

                if (workshopModNames.Length != 0)
                {
                    workshopModComboBox.ItemsSource = workshopModNames;
                    workshopModComboBox.SelectedIndex = 0;
                    workshopModComboBox.IsEnabled = true;
                }
                else
                {
                    NoWorkshopMods();
                }

            }
        }

        private void NoWorkshopMods()
        {
            workshopModComboBox.ItemsSource = new List<string> { "We can't find your workshop items" };
            workshopModComboBox.SelectedIndex = 0;
            importButton.Content = "Locate?";
            workshopModComboBox.IsEnabled = false;
        }

        private void OptionsMenu_OnUnloaded(object sender, RoutedEventArgs e)
        {
            GlobalSettings.Save();
            parent.OnLoadedModsChanged();
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (importButton.Content.ToString() == "Locate?")
            {
                FolderBrowserDialog fd = new();
                fd.ShowDialog();
                if (fd.SelectedPath == "") return;
                MainWindow.WorkshopItemsDirectory = fd.SelectedPath;
                importButton.Content = "Import This?";
                OptionsMenu_OnLoaded(sender, e);
                return;
            }
            
            try
            {
                //Get Selected Index
                int index = workshopModComboBox.SelectedIndex;
                if (index == -1)
                {
                    return;
                }
                string workshopModPath = workshopModPaths[index];

                ImportMod(workshopModPath);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private void ImportMod(string workshopModPath)
        {
            //Delete temp directory if it exists
            if (Directory.Exists($@"{GlobalSettings.ModDirectories.SelectedItem}\temp"))
            {
                Directory.Delete($@"{GlobalSettings.ModDirectories.SelectedItem}\temp", true);
            }

            //Create temp directory
            Directory.CreateDirectory(
                $@"{GlobalSettings.ModDirectories.SelectedItem ?? throw new FileNotFoundException()}\temp");

            //Copy over workshop files
            foreach (string file in Directory.GetFiles(workshopModPath, "*.*", SearchOption.AllDirectories))
            {
                string stripFile = file[workshopModPath.Length..];
                string newPath = $@"{GlobalSettings.ModDirectories.SelectedItem}\temp{stripFile}";
                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
                File.Copy(file, newPath);
            }

            //Read Mod Definition
            Root r = Root.ReadJson($@"{GlobalSettings.ModDirectories.SelectedItem}\temp\mod.json") ??
                     throw new Exception("Mod def error");

            //Find new directory name (should be mod name unless copies were made)
            string modName = r.ModDefinition.Name;
            if (Directory.Exists($@"{GlobalSettings.ModDirectories.SelectedItem}\{modName}"))
            {
                MessageBoxResult result = MessageBox.Show(this,
                    "A mod with this name already exists. Would you like to overwrite it?",
                    "Mod already exists", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    Directory.Delete($@"{GlobalSettings.ModDirectories.SelectedItem}\{modName}", true);
                }
                else
                {
                    Directory.Delete($@"{GlobalSettings.ModDirectories.SelectedItem}\temp", true);
                    return;
                }
            }

            //Rename Directory
            Directory.Move($@"{GlobalSettings.ModDirectories.SelectedItem}\temp",
                $@"{GlobalSettings.ModDirectories.SelectedItem}\{modName}");

            //Write json in case changes were made
            Root.WriteJson(r, $@"{GlobalSettings.ModDirectories.SelectedItem}\{modName}\mod.json");

            parent.OnLoadedModsChanged();
            ImportedMod = r.ModDefinition.Name;
            Close();
        }

        private async void CheckForUpdatestButton_OnClick(object sender, RoutedEventArgs e)
        {
            await Process.Start("CMD.exe", @"/C winget upgrade Pikcube.OrangeJuiceModMaker").WaitForExitAsync();
        }

        private void NewModFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new();
            fd.ShowDialog();
            if (fd.SelectedPath == "") return;
            GlobalSettings.ModDirectories.Items.Add(fd.SelectedPath);
            GlobalSettings.ModDirectories.SelectedItem = fd.SelectedPath;
            OptionsMenu_OnLoaded(sender, e);
        }

        private void NewMirrorButton_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new();
            fd.ShowDialog();
            if (fd.SelectedPath == "") return;
            GlobalSettings.MirrorDirectories.Items.Add(fd.SelectedPath);
            GlobalSettings.MirrorDirectories.SelectedItem = fd.SelectedPath;
            OptionsMenu_OnLoaded(sender, e);
        }
    }
}
