using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifySoundEffect.xaml
    /// </summary>
    public partial class ModifySoundEffect : Window
    {
        private MainWindow parent;
        private string[] soundNameTable;
        private string[] soundDescriptionTable;
        private int SoundIndex
        {
            get => SelectedSongComboBox.SelectedIndex;
            set => SelectedSongComboBox.SelectedIndex = value;
        }
        private string selectedName => soundNameTable[SoundIndex];
        private string selectedDescription => soundDescriptionTable[SoundIndex];
        private List<string> ModdedSoundEffects;
        private SoundPlayer WavPlayer = new();

        public ModifySoundEffect(MainWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
            CsvHolder soundTable = parent.CsvFiles.First(z => z.Type == CsvHolder.TypeList.Sound);
            soundNameTable = soundTable.Rows.Select(z => z[0]).ToArray();
            soundDescriptionTable = soundTable.Rows.Select(z => z[1]).ToArray();
            ModdedSoundEffects = parent.LoadedModReplacements.SoundEffects.ToList();

        }

        private async void SelectedSongComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DescriptionTextBox.Text = selectedDescription;
            string filePath = $@"{parent.LoadedModPath}\sound\{selectedName}";
            bool fileExist = File.Exists(filePath);
            PlayPauseButton.IsEnabled = fileExist;
            if (!fileExist)
            {
                WavPlayer.Stream = null;
                return;
            }

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            WavPlayer.Stream = new MemoryStream(fileBytes);
            WavPlayer.LoadAsync();
        }

        private async void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            PlayPauseButton.IsEnabled = false;
            await Task.Run(() => WavPlayer.PlaySync());
            PlayPauseButton.IsEnabled = true;
        }

        private async void MusicReplaceButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav|MP3 (*.mp3)|*.mp3|OGG (*.ogg)|*.ogg||WAV (*.wav)|*.wav|All Files (*.*)|*.*",
                Title = "Select music file"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }

            string destFileName = $@"{parent.LoadedModPath}\sound\{selectedName}";
            if (o.FileName == destFileName)
            {
                return;
            }

            MusicReplaceButton.IsEnabled = false;
            PlayPauseButton.IsEnabled = false;
            if (Path.GetExtension(o.FileName).ToLower() == ".wav")
            {
                File.Copy(o.FileName, destFileName, true);
            }
            else
            {
                InputFile inFile = new(o.FileName);
                OutputFile outFile = new(destFileName);
                Engine ffmpeg = new($@"{parent.AppData}\ffmpeg\ffmpeg.exe");
                ConversionOptions options = new()
                {
                    AudioSampleRate = AudioSampleRate.Hz44100
                };
                await ffmpeg.ConvertAsync(inFile, outFile, options, CancellationToken.None);
            }

            byte[] fileBytes = await File.ReadAllBytesAsync(destFileName);

            WavPlayer.Stream = new MemoryStream(fileBytes);
            WavPlayer.LoadAsync();

            MusicReplaceButton.IsEnabled = true;
            PlayPauseButton.IsEnabled = true;
        }

        private void SaveToMod_OnClick(object sender, RoutedEventArgs e)
        {
            parent.LoadedModReplacements.SoundEffects = ModdedSoundEffects;
            Root.WriteJson(parent.LoadedModPath, parent.LoadedModDefinition, parent.LoadedModReplacements);
        }

        private void ModifySoundEffect_OnLoaded(object sender, RoutedEventArgs e)
        {
            SelectedSongComboBox.ItemsSource = soundNameTable;
            SelectedSongComboBox.SelectedIndex = 0;
            if (ModdedSoundEffects.Any())
            {
                SelectedSongComboBox.SelectedItem = ModdedSoundEffects.First();
            }
        }

        private void ModifySoundEffect_OnClosing(object? sender, CancelEventArgs e)
        {
            WavPlayer.Dispose();
        }
    }
}
