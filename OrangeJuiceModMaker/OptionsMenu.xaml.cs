using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using static OrangeJuiceModMaker.GlobalSettings;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace OrangeJuiceModMaker
{
    public class GlobalSettings
    {
        private string FileLocation;
        public SettingsList<string> ModDirectories { get; set; }
        public SettingsList<string> MirrorDirectories { get; set; }
        public SettingsList<string> AutoUpdate { get; set; }

        public GlobalSettings()
        {
            ModDirectories = new SettingsList<string>();
            MirrorDirectories = new SettingsList<string>();
            AutoUpdate = new SettingsList<string>();
            FileLocation =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\GlobalSettings.json";
        }

        public GlobalSettings(bool clean = false)
        {
            ModDirectories = new SettingsList<string>();
            MirrorDirectories = new SettingsList<string>();
            AutoUpdate = new SettingsList<string>();
            if (clean)
            {
                AutoUpdate = new SettingsList<string>(new List<string> { "Check for updates", "Skip this version", "Don't check for updates" }, 0);
                MirrorDirectories = new SettingsList<string>(new List<string> { "None" }, 0);
            }
            FileLocation =
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
                Items = new List<T>();
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
            File.WriteAllText(FileLocation, json);
        }
    }
    
    /// <summary>
    /// Interaction logic for OptionsMenu.xaml
    /// </summary>
    public partial class OptionsMenu : Window
    {
        private readonly MainWindow parent;
        private GlobalSettings globalSettings => parent.globalSettings;
        private string[] WorkshopModNames;
        private string[] WorkshopModPaths;


        public OptionsMenu(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();
        }

        private void BackupDirectoryComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            globalSettings.MirrorDirectories.SelectedIndex = backupDirectoryComboBox.SelectedIndex;
        }

        private void AutoUpdateComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            globalSettings.AutoUpdate.SelectedIndex = autoUpdateComboBox.SelectedIndex;
        }

        private void ModDirectoryComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            globalSettings.ModDirectories.SelectedIndex = modDirectoryComboBox.SelectedIndex;
        }

        private void WorkshopModComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private async void OptionsMenu_OnLoaded(object sender, RoutedEventArgs e)
        {
            autoUpdateComboBox.ItemsSource = globalSettings.AutoUpdate.Items;
            autoUpdateComboBox.SelectedIndex = globalSettings.AutoUpdate.SelectedIndex;
            modDirectoryComboBox.ItemsSource = globalSettings.ModDirectories.Items;
            modDirectoryComboBox.SelectedIndex = globalSettings.ModDirectories.SelectedIndex;

            if (globalSettings.MirrorDirectories.Items.Count == 0)
            {
                globalSettings.MirrorDirectories = new SettingsList<string>(new List<string> { "None" }, 0);
                globalSettings.Save();
            }

            backupDirectoryComboBox.ItemsSource = globalSettings.MirrorDirectories.Items;
            backupDirectoryComboBox.SelectedIndex = globalSettings.MirrorDirectories.SelectedIndex;
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
                    if (!mods.Any())
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

                    WorkshopModPaths = mods.Where(z => z is not null).ToArray()!;
                    WorkshopModNames = modNames.Where(z => z is not null).ToArray()!;
                });

                importButton.IsEnabled = true;

                if (WorkshopModNames.Any())
                {
                    workshopModComboBox.ItemsSource = WorkshopModNames;
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
            globalSettings.Save();
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void CheckForUpdatestButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void NewModFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new();
            fd.ShowDialog();
            if (fd.SelectedPath == "") return;
            globalSettings.ModDirectories.Items.Add(fd.SelectedPath);
            globalSettings.ModDirectories.SelectedItem = fd.SelectedPath;
            OptionsMenu_OnLoaded(sender, e);
        }

        private void NewMirrorButton_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fd = new();
            fd.ShowDialog();
            if (fd.SelectedPath == "") return;
            globalSettings.MirrorDirectories.Items.Add(fd.SelectedPath);
            globalSettings.MirrorDirectories.SelectedItem = fd.SelectedPath;
            OptionsMenu_OnLoaded(sender, e);
        }
    }
}
