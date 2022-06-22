﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Unosquare.FFME.Common;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using MediaElement = Unosquare.FFME.MediaElement;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyMusic.xaml
    /// </summary>
    public partial class ModifyMusic
    {
        private MediaElement musicPlayer => MainWindow.MusicPlayer;
        private long songLength = 1;
        private PlayState mediaPlayerState;
        private readonly MusicList[] sets;
        private MusicList songs;
        private List<ModMusic> musicMods = new();
        private ModMusic modifiedMusic;
        private TimeSpan LoopPoint => TickFromSamples(modifiedMusic.LoopPoint ?? 0);
        TimeSpan TickFromSamples(long samples) => TimeSpan.FromTicks(samples * 10000 / 43);
        private PlayState MediaPlayerState
        {
            get => mediaPlayerState;
            set
            {
                switch (value)
                {
                    case PlayState.Stop when musicPlayer.HasAudio:
                        musicPlayer.Volume = 0;
                        musicPlayer.Stop().GetAwaiter().GetResult();
                        musicPlayer.Close();
                        musicPlayer.Position = TimeSpan.Zero;
                        PlayPauseButton.Content = "▶";
                        break;
                    case PlayState.Play when musicPlayer.HasAudio:
                        musicPlayer.Volume = 0.5;
                        musicPlayer.Play().GetAwaiter().GetResult();
                        PlayPauseButton.Content = "▐▐";
                        break;
                    case PlayState.Pause when musicPlayer.HasAudio:
                        musicPlayer.Volume = 0;
                        musicPlayer.Pause().GetAwaiter().GetResult();
                        PlayPauseButton.Content = "▶";
                        break;
                    default:
                        musicPlayer.Volume = 0;
                        mediaPlayerState = PlayState.Stop;
                        return;

                }
                mediaPlayerState = value;
            }
        }

        public ModifyMusic()
        {
            modifiedMusic = new ModMusic(null, ModMusic.SongType.UnitTheme);
            //musicPlayer.BufferingEnded += MusicPlayer_BufferingEnded;

            sets = MainWindow.CsvFiles.Where(z => z.Type == CsvHolder.TypeList.Music).Select(z => new MusicList(z)).ToArray();
            if (!sets.Any())
            {
                throw new Exception("No Music");
            }
            songs = sets.First();

            InitializeComponent();
            musicPlayer.MediaEnded += MusicPlayer_MediaEnded;
            musicPlayer.PositionChanged += MusicPlayer_PositionChanged;
            musicPlayer.MediaOpened += MusicPlayer_MediaOpened;
            //musicPlayer.MessageLogged += MusicPlayer_MessageLogged;
            //musicPlayer.MediaFailed += MusicPlayer_MediaFailed;

            foreach (Music m in MainWindow.LoadedModReplacements.Music)
            {
                musicMods.Add(new ModMusic(m) { File = $@"{MainWindow.LoadedModPath}\{m.File}.ogg" });
            }

            SetComboBox.ItemsSource = sets.Select(z => z.Name);
            SelectedSongComboBox.ItemsSource = songs.ID;
            DescriptionComboBox.ItemsSource = songs.Description;
            SetComboBox.SelectedIndex = 0;
            SelectedSongComboBox.SelectedIndex = 0;
        }

        private void MusicPlayer_MediaFailed(object? sender, MediaFailedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.Message);
        }

        private void MusicPlayer_MessageLogged(object? sender, MediaLogMessageEventArgs e)
        {
            MessageBox.Show(e.Message);
        }

        private void MusicPlayer_MediaOpened(object? sender, MediaOpenedEventArgs e)
        {
            songLength = musicPlayer.NaturalDuration?.Ticks ?? 1;
            musicPlayer.Position = TimeSpan.Zero;
            CurrentPositionBox.Text = "0";
        }

        private void MusicPlayer_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            UpdateCurrentPosition();
        }

        //Media Functions
        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (musicPlayer.NaturalDuration is null)
            {
                return;
            }
            long progress = (long)(songLength * ProgressSlider.Value / 10);
            musicPlayer.Position = TimeSpan.FromTicks(progress);
        }

        private async void MusicPlayer_MediaEnded(object? sender, EventArgs e)
        {
            await musicPlayer.Stop();
            if (PreviewLoopCheckBox.IsChecked is not true || modifiedMusic.LoopPoint is null)
            {
                MediaPlayerState = PlayState.Pause;
                musicPlayer.Position = TimeSpan.Zero;
                UpdateCurrentPosition(0);
                return;
            }

            UpdateCurrentPosition(LoopPoint.Ticks);
            await musicPlayer.Play();
            UpdateCurrentPosition(LoopPoint.Ticks);
        }

        private void UpdateCurrentPosition() => UpdateCurrentPositionUi(musicPlayer.Position.Ticks);
        private void UpdateCurrentPosition(long ticks)
        {
            musicPlayer.Position = TimeSpan.FromTicks(ticks);
            UpdateCurrentPositionUi(ticks);
        }

        private void UpdateCurrentPositionUi(long ticks)
        {
            string s = $"{43 * ticks / 10000}";
            double p = (double)ticks * 10 / songLength;
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
                Engine ffmpeg = new($@"{MainWindow.AppData}\ffmpeg\ffmpeg.exe");
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

            int retry = 5;
            while (!await musicPlayer.Open(new Uri(mp3Path, UriKind.RelativeOrAbsolute)))
            {
                await Task.Run(() => Thread.Sleep(100));
                if (retry == 0)
                {
                    if (MessageBox.Show("Media failed to load. Try again?", "An error occurred", MessageBoxButton.YesNo)
                        is MessageBoxResult.No)
                    {
                        return;
                    }
                }
                else
                {
                    --retry;
                }
            }
            
            await musicPlayer.Pause();

            LoopPointBox.Text = (modifiedMusic.LoopPoint ?? 0).ToString();

            UpdateCurrentPosition(0);

            EnableMusicControls(true);
        }

        private void RwButton_OnClick(object sender, RoutedEventArgs e)
        {
            musicPlayer.Position -= TimeSpan.FromSeconds(7);
        }

        private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            MediaPlayerState = PlayPauseButton.Content.ToString() == "▶" ? PlayState.Play : PlayState.Pause;
        }

        private void FfButton_OnClick(object sender, RoutedEventArgs e)
        {
            musicPlayer.Position += TimeSpan.FromSeconds(30);
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

            musicPlayer.Position = TickFromSamples(CurrentPositionBox.Text.ToLongOrDefault());
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
                Filter = "Audio Files (*.mp3;*.ogg)|*.mp3;*.ogg|MP3 (*.mp3)|*.mp3|OGG (*.ogg)|*.ogg|All Files (*.*)|*.*",
                Title = "Select music file"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }

            MusicReplaceButton.IsEnabled = false;
            MusicReplaceButton.Content = "Loading Music";
            EnableMusicControls(false);

            string tempPath = $@"{MainWindow.LoadedModPath}\music\{modifiedMusic.Id}{Path.GetFileNameWithoutExtension(o.FileName)}.temp";
            string mp3Path = $@"{MainWindow.LoadedModPath}\music\{modifiedMusic.Id}{Path.GetFileNameWithoutExtension(o.FileName)}.mp3";
            string oggPath = $@"{MainWindow.LoadedModPath}\music\{modifiedMusic.Id}{Path.GetFileNameWithoutExtension(o.FileName)}.ogg";

            File.Copy(Path.GetFullPath(o.FileName), tempPath, true);

            Task t = Task.Run(() =>
            {
                Task m = Task.Run(() =>
                {
                    InputFile inFile = new(tempPath);
                    OutputFile outFile = new(mp3Path);
                    Engine ffmpeg = new($@"{MainWindow.AppData}\ffmpeg\ffmpeg.exe");
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
                    Engine ffmpeg = new($@"{MainWindow.AppData}\ffmpeg\ffmpeg.exe");
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
            await musicPlayer.Open(new Uri(mp3Path, UriKind.RelativeOrAbsolute));
            LoopPointBox.Text = (modifiedMusic.LoopPoint ?? 0).ToString();
            EnableMusicControls(true);
            MusicReplaceButton.IsEnabled = true;
            MusicReplaceButton.Content = "Replace with...";
        }

        private void MusicEditor_OnClosed(object? sender, EventArgs e)
        {
            musicPlayer.Stop();
            musicPlayer.Close();
        }

        private void Set_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            songs = sets[SetComboBox.SelectedIndex];

            SelectedSongComboBox.ItemsSource = songs.ID;
            SelectedSongComboBox.SelectedIndex = 0;

            DescriptionComboBox.ItemsSource = songs.Description;
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
            string id = songs.ID[SelectedSongComboBox.SelectedIndex];
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
            
        }

        private void DescriptionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedSongComboBox.SelectedIndex = DescriptionComboBox.SelectedIndex;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            modifiedMusic.SaveToMod();
        }
    }
}
