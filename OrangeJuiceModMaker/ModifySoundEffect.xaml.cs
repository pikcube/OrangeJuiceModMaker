using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using Microsoft.Win32;
using OrangeJuiceModMaker.Data;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifySoundEffect.xaml
    /// </summary>
    public partial class ModifySoundEffect
    {
        private readonly MainWindow parent;
        private readonly string[] soundNameTable;
        private readonly string[] soundDescriptionTable;
        private int SoundIndex => SelectedSongComboBox.SelectedIndex;

        /*
            set => SelectedSongComboBox.SelectedIndex = value;
*/
        private string SelectedName => soundNameTable[SoundIndex];
        private string SelectedDescription => soundDescriptionTable[SoundIndex];
        private readonly List<string> moddedSoundEffects;
        private readonly SoundPlayer wavPlayer = new();

        public ModifySoundEffect(MainWindow parent)
        {
            InitializeComponent();
            this.parent = parent;
            soundNameTable = [.. parent.Sounds.Select(z => z.File)];
            soundDescriptionTable = [.. parent.Sounds.Select(z => z.Description)];
            moddedSoundEffects = [.. parent.LoadedModReplacements.SoundEffects];

        }

        private async void SelectedSongComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DescriptionTextBox.Text = SelectedDescription;
            string filePath = $@"{parent.LoadedModPath}\sound\{SelectedName}";
            bool fileExist = File.Exists(filePath);
            PlayPauseButton.IsEnabled = fileExist;
            if (!fileExist)
            {
                wavPlayer.Stream = null;
                return;
            }

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

            wavPlayer.Stream = new MemoryStream(fileBytes);
            wavPlayer.LoadAsync();
        }

        private async void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            PlayPauseButton.IsEnabled = false;
            await Task.Run(() => wavPlayer.PlaySync());
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

            string destFileName = $@"{parent.LoadedModPath}\sound\{SelectedName}";
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

            wavPlayer.Stream = new MemoryStream(fileBytes);
            wavPlayer.LoadAsync();

            MusicReplaceButton.IsEnabled = true;
            PlayPauseButton.IsEnabled = true;
        }

        private void SaveToMod_OnClick(object sender, RoutedEventArgs e)
        {
            parent.LoadedModReplacements.SoundEffects = moddedSoundEffects;
            Root.WriteJson(parent.LoadedModPath, parent.LoadedModDefinition, parent.LoadedModReplacements);
        }

        private void ModifySoundEffect_OnLoaded(object sender, RoutedEventArgs e)
        {
            SelectedSongComboBox.ItemsSource = soundNameTable;
            SelectedSongComboBox.SelectedIndex = 0;
            if (moddedSoundEffects.Any())
            {
                SelectedSongComboBox.SelectedItem = moddedSoundEffects.First();
            }
        }

        private void ModifySoundEffect_OnClosing(object? sender, CancelEventArgs e)
        {
            wavPlayer.Dispose();
        }
    }
}
