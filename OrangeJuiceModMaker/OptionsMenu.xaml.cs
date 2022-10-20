using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker
{
    public class GlobalSettings
    {
        private string FileLocation;
        public SettingsList<string> ModDirectories { get; set; }
        public SettingsList<string> MirrorDirectories { get; set; }
        public SettingsList<string> SelectedUpdateChannel { get; set; }
        public SettingsList<string> AutoUpdate { get; set; }

        public GlobalSettings()
        {
            ModDirectories = new SettingsList<string>();
            MirrorDirectories = new SettingsList<string>();
            SelectedUpdateChannel = new SettingsList<string>();
            AutoUpdate = new SettingsList<string>();
            FileLocation =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OrangeJuiceModMaker\GlobalSettings.json";
        }

        public GlobalSettings(bool clean = false)
        {
            ModDirectories = new SettingsList<string>();
            MirrorDirectories = new SettingsList<string>();
            SelectedUpdateChannel = new SettingsList<string>();
            AutoUpdate = new SettingsList<string>();
            if (clean)
            {
                SelectedUpdateChannel = new SettingsList<string>(new List<string> { "Stable", "Beta" });
                AutoUpdate = new SettingsList<string>(new List<string> { "Check for updates", "Skip this version", "Don't check for updates" });
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

        private void UpdateChannelComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            globalSettings.SelectedUpdateChannel.SelectedIndex = updateChannelComboBox.SelectedIndex;
        }

        private void ModDirectoryComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            globalSettings.ModDirectories.SelectedIndex = modDirectoryComboBox.SelectedIndex;
        }

        private void WorkshopModComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void OptionsMenu_OnLoaded(object sender, RoutedEventArgs e)
        {
            autoUpdateComboBox.ItemsSource = globalSettings.AutoUpdate.Items;
            autoUpdateComboBox.SelectedIndex = globalSettings.AutoUpdate.SelectedIndex;
            modDirectoryComboBox.ItemsSource = globalSettings.ModDirectories.Items;
            modDirectoryComboBox.SelectedIndex = globalSettings.ModDirectories.SelectedIndex;
            backupDirectoryComboBox.ItemsSource = globalSettings.MirrorDirectories.Items;
            backupDirectoryComboBox.SelectedIndex = globalSettings.MirrorDirectories.SelectedIndex;
            updateChannelComboBox.ItemsSource = globalSettings.SelectedUpdateChannel.Items;
            updateChannelComboBox.SelectedIndex = globalSettings.SelectedUpdateChannel.SelectedIndex;
        }

        private void OptionsMenu_OnUnloaded(object sender, RoutedEventArgs e)
        {
            globalSettings.Save();
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CheckForUpdatestButton_OnClick(object sender, RoutedEventArgs e)
        {
            //Maybe instant reboot?
            throw new NotImplementedException();
        }

        private void NewModFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void NewMirrorButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
