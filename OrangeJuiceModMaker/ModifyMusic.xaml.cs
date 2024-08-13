using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using Microsoft.Win32;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyMusic.xaml
    /// </summary>
    public partial class ModifyMusic
    {
        private MyMusicPlayer MusicPlayer = new();
        private PlayState mediaPlayerState;
        private readonly MusicList[] sets;
        private MusicList songs;
        private readonly List<ModMusic> musicMods = [];
        private ModMusic modifiedMusic;
        private TimeSpan LoopPoint => TickFromSamples(modifiedMusic.LoopPoint ?? 0);
        private static TimeSpan TickFromSamples(long samples) => TimeSpan.FromTicks(samples * 10000 / 43);
        private readonly MainWindow mainWindow;
        private PlayState MediaPlayerState
        {
            get => mediaPlayerState;
            set
            {
                switch (value)
                {
                    case PlayState.Stop when MusicPlayer.Reader is not null:
                        MusicPlayer.Out.Volume = 0;
                        MusicPlayer.Out.Stop();
                        //MusicPlayer.Reader.Close();
                        //MusicPlayer.Position = TimeSpan.Zero;
                        PlayPauseButton.Content = "▶";
                        break;
                    case PlayState.Play when MusicPlayer.Reader is not null:
                        MusicPlayer.Out.Volume = 0.5f;
                        MusicPlayer.Out.Play();
                        PlayPauseButton.Content = "▐▐";
                        break;
                    case PlayState.Pause when MusicPlayer.Reader is not null:
                        MusicPlayer.Out.Volume = 0;
                        MusicPlayer.Out.Pause();
                        PlayPauseButton.Content = "▶";
                        break;
                    default:
                        MusicPlayer.Out.Volume = 0;
                        mediaPlayerState = PlayState.Stop;
                        return;

                }
                mediaPlayerState = value;
            }
        }

        public ModifyMusic(MainWindow window)
        {
            mainWindow = window;
            modifiedMusic = new ModMusic(null, ModMusic.SongType.UnitTheme);
            MusicPlayer.EndOfSong += MusicPlayer_EndOfSong;
            MusicPlayer.PositionChanged += MusicPlayer_PositionChanged;
            //musicPlayer.BufferingEnded += MusicPlayer_BufferingEnded;

            sets = mainWindow.CsvFiles.Where(z => z.Type == CsvHolder.TypeList.Music).Select(z => new MusicList(z)).ToArray();
            if (sets.Length == 0)
            {
                throw new Exception("No Music");
            }

            foreach (MusicList set in sets)
            {
                set.Tracks.Sort((first, second) => string.CompareOrdinal(first.Id, second.Id));
            }
            songs = sets.First();

            InitializeComponent();
            if (mainWindow.Debug)
            {
            }

            foreach (Music m in mainWindow.LoadedModReplacements.Music)
            {
                musicMods.Add(new ModMusic(m) { File = $@"{mainWindow.LoadedModPath}\{m.File}.ogg" });
            }

            SetComboBox.ItemsSource = sets.Select(z => z.Name);
            SelectedSongComboBox.ItemsSource = songs.Tracks.Select(z => z.Id);
            DescriptionComboBox.ItemsSource = songs.Tracks.Select(z => z.Description);
            SetComboBox.SelectedIndex = 0;
            SelectedSongComboBox.SelectedIndex = 0;
        }

        private void MusicPlayer_PositionChanged(object? sender, TimeSpan e)
        {
            Dispatcher.Invoke(() => UpdateCurrentPositionUi(e.Ticks));
        }

        private void MusicPlayer_EndOfSong(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MediaPlayerState = PlayState.Pause;
                MusicPlayer.Position = TimeSpan.Zero;
                UpdateCurrentPosition(0);
            });
        }

        private void MusicPlayer_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            Console.WriteLine(e.ErrorException.Message);
        }

        //Media Functions
        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            long progress = (long)(MusicPlayer.Duration.Ticks * ProgressSlider.Value / 10);
            MusicPlayer.Position = TimeSpan.FromTicks(progress);
        }


        private void UpdateCurrentPosition(long ticks)
        {
            if (MusicPlayer.Duration == TimeSpan.Zero)
            {
                return;
            }
            MusicPlayer.Position = TimeSpan.FromTicks(ticks);
            UpdateCurrentPositionUi(ticks);
        }

        private void UpdateCurrentPositionUi(long ticks)
        {
            string s = $"{43 * ticks / 10000}";
            double p = (double)ticks * 10 / MusicPlayer.Duration.Ticks;
            ProgressSlider.ValueChanged -= ProgressSlider_ValueChanged;
            ProgressSlider.Value = p;
            ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            CurrentPositionBox.TextChanged -= CurrentPositionBox_OnTextChanged;
            CurrentPositionBox.Text = s;
            CurrentPositionBox.TextChanged += CurrentPositionBox_OnTextChanged;
        }

        private void EnableMusicControls(bool musicEnabled)
        {
            PreviewLoopCheckBox.IsEnabled = musicEnabled;
            SetLoopButton.IsEnabled = musicEnabled;
            VolumeBox.IsEnabled = musicEnabled;
            LoopPointBox.IsEnabled = musicEnabled;
            CurrentPositionBox.IsEnabled = musicEnabled;
            FfButton.IsEnabled = musicEnabled;
            PlayPauseButton.IsEnabled = musicEnabled;
            RwButton.IsEnabled = musicEnabled;
            ProgressSlider.IsEnabled = musicEnabled;
        }

        private async Task RefreshGrid()
        {
            MediaPlayerState = PlayState.Stop;

            if (modifiedMusic.File is null)
            {
                CurrentPositionBox.Text = "";
                LoopPointBox.Text = "";
                VolumeBox.Text = "";
                return;
            }

            if (Path.GetExtension(modifiedMusic.File) != ".ogg")
            {
                modifiedMusic.File = null;
                return;
            }

            string mp3Path = Path.ChangeExtension(modifiedMusic.File, "mp3");

            if (!File.Exists(mp3Path))
            {
                MusicReplaceButton.IsEnabled = false;
                MusicReplaceButton.Content = "Loading Music";
                InputFile inFile = new(modifiedMusic.File);
                OutputFile outFile = new(mp3Path);
                Engine ffmpeg = new($@"{mainWindow.AppData}\ffmpeg\ffmpeg.exe");
                ConversionOptions options = new()
                {
                    AudioSampleRate = AudioSampleRate.Hz44100
                };
                await ffmpeg.ConvertAsync(inFile, outFile, options, CancellationToken.None);
                if (File.Exists(mp3Path))
                {
                    MusicReplaceButton.IsEnabled = true;
                    MusicReplaceButton.Content = "Replace with...";
                }
                else
                {
                    MessageBox.Show("Media failed to load. To retry, close and reopen window.");
                    modifiedMusic.File = null;
                    await RefreshGrid();
                    return;
                }
            }

            MusicPlayer.Open(mp3Path);
            MusicPlayer.Out.Pause();

            LoopPointBox.Text = (modifiedMusic.LoopPoint ?? 0).ToString();
            VolumeBox.Text = (modifiedMusic.Volume ?? 0).ToString();

            UpdateCurrentPosition(0);

            EnableMusicControls(true);
        }

        private void RwButton_OnClick(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Position -= TimeSpan.FromSeconds(7);
        }

        private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            MediaPlayerState = PlayPauseButton.Content.ToString() == "▶" ? PlayState.Play : PlayState.Pause;
        }

        private void FfButton_OnClick(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Position += TimeSpan.FromSeconds(30);
        }

        private void CurrentPositionBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentPositionBox.Text == "")
            {
                return;
            }

            if (MediaPlayerState == PlayState.Stop)
            {
                return;
            }

            MusicPlayer.Position = TickFromSamples(CurrentPositionBox.Text.ToLongOrDefault());
        }

        private void CheckForNumber(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.IsNumber();
        }

        private void LoopPointBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedMusic.File == null)
            {
                return;
            }

            modifiedMusic.LoopPoint = LoopPointBox.Text.ToIntOrNull();
            if (modifiedMusic.LoopPoint is not null)
            {
                MusicPlayer.LoopPoint = TickFromSamples(modifiedMusic.LoopPoint.Value);
            }

        }

        private void VolumeBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedMusic.File is not null)
            {
                modifiedMusic.Volume = VolumeBox.Text.ToIntOrNull();
            }
        }

        private void SetLoopButton_OnClick(object sender, RoutedEventArgs e)
        {
            LoopPointBox.Text = CurrentPositionBox.Text;
        }

        private async void MusicReplaceButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                Filter = "Audio Files (*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav|MP3 (*.mp3)|*.mp3|OGG (*.ogg)|*.ogg|WAV (*.wav)|*.wav|All Files (*.*)|*.*",
                Title = "Select music file"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }

            MusicReplaceButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            MusicReplaceButton.Content = "Loading Music";
            EnableMusicControls(false);

            string tempPath = $@"{mainWindow.LoadedModPath}\music\{modifiedMusic.Id}{Path.GetFileNameWithoutExtension(o.FileName)}.temp";
            string mp3Path = $@"{mainWindow.LoadedModPath}\music\{modifiedMusic.Id}{Path.GetFileNameWithoutExtension(o.FileName)}.mp3";
            string oggPath = $@"{mainWindow.LoadedModPath}\music\{modifiedMusic.Id}{Path.GetFileNameWithoutExtension(o.FileName)}.ogg";

            File.Copy(Path.GetFullPath(o.FileName), tempPath, true);

            Task t = Task.Run(() =>
            {
                Task m = Task.Run(() =>
                {
                    InputFile inFile = new(tempPath);
                    OutputFile outFile = new(mp3Path);
                    Engine ffmpeg = new($@"{mainWindow.AppData}\ffmpeg\ffmpeg.exe");
                    ConversionOptions options = new()
                    {
                        AudioSampleRate = AudioSampleRate.Hz44100
                    };
                    ffmpeg.ConvertAsync(inFile, outFile, options, CancellationToken.None).GetAwaiter().GetResult();
                });

                Task t = Task.Run(() =>
                {
                    InputFile inFile = new(tempPath);
                    OutputFile outFile = new(oggPath);
                    Engine ffmpeg = new($@"{mainWindow.AppData}\ffmpeg\ffmpeg.exe");
                    ConversionOptions options = new()
                    {
                        AudioSampleRate = AudioSampleRate.Hz44100
                    };
                    ffmpeg.ConvertAsync(inFile, outFile, options, CancellationToken.None).GetAwaiter().GetResult();
                });
                m.Wait();
                t.Wait();
            });

            await t;

            File.Delete(tempPath);
            modifiedMusic = new ModMusic(oggPath, modifiedMusic.Song)
            {
                LoopPoint = 0,
                Id = modifiedMusic.Id,
                Volume = 0
            };
            MusicPlayer.Open(mp3Path);
            MusicPlayer.Out.Pause();
            LoopPointBox.Text = (modifiedMusic.LoopPoint ?? 0).ToString();
            EnableMusicControls(true);
            SaveButton.IsEnabled = true;
            MusicReplaceButton.IsEnabled = true;
            MusicReplaceButton.Content = "Replace with...";
        }

        private void MusicEditor_OnClosed(object? sender, EventArgs e)
        {
            MusicPlayer.Dispose();
        }

        private void Set_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            songs = sets[SetComboBox.SelectedIndex];

            SelectedSongComboBox.ItemsSource = songs.Tracks.Select(z => z.Id);
            SelectedSongComboBox.SelectedIndex = 0;

            DescriptionComboBox.ItemsSource = songs.Tracks.Select(z => z.Description);
            DescriptionComboBox.SelectedIndex = 0;
        }

        private async void SelectedSong_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedSongComboBox.SelectedIndex == -1)
            {
                return;
            }
            if (SelectedSongComboBox.SelectedIndex != DescriptionComboBox.SelectedIndex)
            {
                DescriptionComboBox.SelectedIndex = SelectedSongComboBox.SelectedIndex;
            }

            ModMusic.SongType t = songs.Name == "Events" ? ModMusic.SongType.EventTheme : ModMusic.SongType.UnitTheme;
            string id = songs.Tracks[SelectedSongComboBox.SelectedIndex].Id;
            if (musicMods.Any(z => z.Id == id && z.Song == t))
            {
            }
            else
            {
                musicMods.Add(new ModMusic(null, t)
                {
                    Id = id
                });
            }
            modifiedMusic = musicMods.First(z => z.Id == id && z.Song == t);

            if (modifiedMusic.File is null || !File.Exists(modifiedMusic.File))
            {
                MediaPlayerState = PlayState.Stop;
                EnableMusicControls(false);
                await RefreshGrid();
                return;
            }

            EnableMusicControls(true);
            await RefreshGrid();

        }

        private void ModifyMusic_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!musicMods.Any(z => z.File is not null))
            {
                return;
            }

            ModMusic fileToSelect = musicMods.First(z => z.File is not null);
            switch (fileToSelect.Song)
            {
                case ModMusic.SongType.EventTheme:
                    SetComboBox.SelectedItem = "Events";
                    break;
                case ModMusic.SongType.UnitTheme:
                    SetComboBox.SelectedItem = "UnitThemes";
                    break;
                default:
                    return;
            }

            SelectedSongComboBox.SelectedItem = fileToSelect.Id;
        }

        private void DescriptionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedSongComboBox.SelectedIndex = DescriptionComboBox.SelectedIndex;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            modifiedMusic.SaveToMod(mainWindow.LoadedModPath, mainWindow.LoadedModDefinition, ref mainWindow.LoadedModReplacements);
        }

        private void PreviewLoopCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            MusicPlayer.IsLooped = PreviewLoopCheckBox.IsChecked is true;
        }

        private void PreviewLoopCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            MusicPlayer.IsLooped = PreviewLoopCheckBox.IsChecked is true;
        }
    }
}
