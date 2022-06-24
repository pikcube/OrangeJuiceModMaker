using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using Unosquare.FFME.Common;
using MediaElement = Unosquare.FFME.MediaElement;
using Path = System.IO.Path;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyUnit.xaml
    /// </summary>
    public partial class ModifyUnit
    {
        //Variable Declaration
        private Unit selectedUnit;
        MainWindow mainWindow;
        private MediaElement musicPlayer => MainWindow.MusicPlayer;
        private long songLength = 1;
        private readonly List<ModifiedUnit> modifiedUnitHyperTable = new();
        private ModifiedUnit modifiedUnit;
        private int selectedHyper;
        private int selectedCharacter;
        private int selectedCharacterCard;
        private PlayState mediaPlayerState;
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
        private TimeSpan LoopPoint => TickFromSamples(modifiedUnit.Music?.LoopPoint ?? 0);
        private bool IsPlaying => PlayPauseButton.Content.ToString() == "▐▐";
        public int RefreshRetries = 30;

        private int SelectedHyper
        {
            get => selectedHyper;
            set => selectedHyper = selectedUnit.HyperIds.Length == 0 ? 0 : (value + selectedUnit.HyperIds.Length) % selectedUnit.HyperIds.Length;
        }
        private int SelectedCharacter
        {
            get => selectedCharacter;
            set =>
                selectedCharacter = selectedUnit.CharacterArt.Length == 0
                    ? 0
                    : (value + selectedUnit.CharacterArt.Length) % selectedUnit.CharacterArt.Length;
        }

        private int SelectedCharacterCard
        {
            get => selectedCharacterCard;
            set => selectedCharacterCard = selectedUnit.CharacterCards.Length == 0 ? 0 : (value + selectedUnit.CharacterCards.Length) % selectedUnit.CharacterCards.Length;
        }

        //On Load
        public ModifyUnit(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            selectedUnit = mainWindow.UnitHyperTable.First();
            modifiedUnit = new ModifiedUnit(selectedUnit, mainWindow.LoadedModPath, mainWindow.LoadedModReplacements);
            modifiedUnitHyperTable.Add(modifiedUnit);
            musicPlayer.MediaEnded += MusicPlayer_MediaEnded;
            musicPlayer.PositionChanged += MusicPlayer_PositionChanged;
            musicPlayer.MediaOpened += MusicPlayer_MediaOpened;
            if (mainWindow.Debug)
            {
                musicPlayer.MessageLogged += MusicPlayer_MessageLogged;
                musicPlayer.MediaFailed += MusicPlayer_MediaFailed;
            }
            InitializeComponent();
        }

        private static void MusicPlayer_MediaFailed(object? sender, MediaFailedEventArgs e)
        {
            Console.WriteLine(e.ErrorException);
        }

        private static void MusicPlayer_MessageLogged(object? sender, MediaLogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private void MusicPlayer_MediaOpened(object? sender, Unosquare.FFME.Common.MediaOpenedEventArgs e)
        {
            songLength = musicPlayer.NaturalDuration?.Ticks ?? 1;
            musicPlayer.Position = TimeSpan.Zero;
            CurrentPositionBox.Text = "0";
        }

        private void MusicPlayer_PositionChanged(object? sender, Unosquare.FFME.Common.PositionChangedEventArgs e)
        {
            if (songLength == 0)
            {
                return;
            }
            UpdateCurrentPosition();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UnitSelectionBox.ItemsSource = mainWindow.UnitHyperTable.Select(z => z.UnitName).OrderBy(z => z);
            UnitSelectionBox.SelectedIndex = 0;
        }

        private void ModifyUnit_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            HyperFlavorUpdateBox.SetValue(Grid.RowProperty, Height < 560 ? 2 : 3);
            HyperFlavorUpdateBox.SetValue(Grid.RowSpanProperty, Height < 560 ? 2 : 1);
            HyperFlavorUpdateBox.SetValue(Grid.ColumnProperty, Height < 560 ? 14 : 12);
            HyperFlavorUpdateBox.SetValue(Grid.ColumnSpanProperty, Height < 560 ? 1 : 3);

        }

        private void ModifyUnit_OnClosed(object? sender, EventArgs e)
        {
            musicPlayer.Stop();
            musicPlayer.Close();
        }

        //On Unit Select
        private async void UnitSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UnitSelectionBox.IsEnabled = false;
            //Guard Clause
            if (UnitSelectionBox.SelectedItem is not string unitName)
            {
                UnitSelectionBox.IsEnabled = true;
                return;
            }

            MediaPlayerState = PlayState.Stop;

            //Get Unit
            selectedUnit = mainWindow.UnitHyperTable.First(z => z.UnitName == unitName);
            if (modifiedUnitHyperTable.All(z => z.UnitName != unitName))
            {
                modifiedUnitHyperTable.Add(new ModifiedUnit(selectedUnit, mainWindow.LoadedModPath, mainWindow.LoadedModReplacements));
            }

            modifiedUnit = modifiedUnitHyperTable.First(z => z.UnitName == unitName);
            await RefreshGrid();
            UnitSelectionBox.IsEnabled = true;
        }

        //Click Picture Get X Y Offset
        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(UnitPoseRectangle);
            int x = (int)p.X;
            int y = (int)p.Y;
            FaceXBox.Text = x.ToString();
            FaceYBox.Text = y.ToString();
            e.Handled = true;

        }

        //Media Functions
        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (musicPlayer.NaturalDuration is null)
            {
                return;
            }
            long progress = (long)(musicPlayer.NaturalDuration.Value.Ticks * ProgressSlider.Value / 10);
            musicPlayer.Position = TimeSpan.FromTicks(progress);
        }

        private async void MusicPlayer_MediaEnded(object? sender, EventArgs e)
        {
            await musicPlayer.Stop();
            if (PreviewLoopCheckBox.IsChecked is not true || modifiedUnit.Music?.LoopPoint is null)
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
            if (songLength == 0)
            {
                return;
            }
            musicPlayer.Position = TimeSpan.FromTicks(ticks);
            UpdateCurrentPositionUi(ticks);
        }

        private void UpdateCurrentPositionUi(long ticks)
        {
            string s = $"{43 * ticks / 10000}";
            double p = (double)ticks * 10 / songLength;
            ProgressSlider.ValueChanged -= ProgressSlider_ValueChanged;
            ProgressSlider.Value = p is double.NaN ? 0 : p;
            ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            CurrentPositionBox.TextChanged -= CurrentPositionBox_OnTextChanged;
            CurrentPositionBox.Text = s;
            CurrentPositionBox.TextChanged += CurrentPositionBox_OnTextChanged;
        }

        //Refresh data
        private async Task RefreshGrid()
        {
            SelectedCharacterCard = 0;
            SelectedHyper = 0;
            SelectedCharacter = 0;
            UnloadImages();
            EnableMusicControls(false);
            musicPlayer.Stop().GetAwaiter().GetResult();

            FaceXBox.Text = modifiedUnit.FaceX.First().ToString();
            FaceYBox.Text = modifiedUnit.FaceY.First().ToString();

            //Get Hyper Art
            if (selectedUnit.HyperIds.Any())
            {
                //HyperArt.ImageSource = GetCardImageFromPath(selectedUnit.HyperCardPaths.First());
                HyperNameTextBox.Text = selectedUnit.HyperNames.First();
                HyperNameUpdateBox.Text = modifiedUnit.HyperNames.First();
                HyperFlavorUpdateBox.Text = modifiedUnit.HyperFlavor.First();
                HyperNameUpdateBox.IsEnabled = true;
            }
            else
            {
                HyperArt.ImageSource = null;
                HyperNameTextBox.Text = "";
                HyperNameUpdateBox.Text = "";
                HyperNameUpdateBox.IsEnabled = false;
                HyperFlavorUpdateBox.Text = "";
            }

            bool hyperButtonsEnabled = selectedUnit.HyperIds.Length >= 2;
            bool cardButtonsEnabled = selectedUnit.CharacterCards.Length >= 2;

            HyperLeftButton.IsEnabled = hyperButtonsEnabled;
            HyperRightButton.IsEnabled = hyperButtonsEnabled;

            CharacterCardLeftButton.IsEnabled = cardButtonsEnabled;
            CharacterCardRightButton.IsEnabled = cardButtonsEnabled;

            CharacterNameTextBox.Text = selectedUnit.UnitId;

            HyperFlavorUpdateBox.IsEnabled = HyperFlavorUpdateBox.Text != "";

            //Get Card Art
            if (selectedUnit.CharacterCards.Any())
            {
                SelectedCharacterCard = 0;
                CharacterCardNameTextBox.Text = selectedUnit.CharacterCardNames.First();
                CardNameUpdateBox.Text = modifiedUnit.CharacterCardNames.First();
                CardNameUpdateBox.IsEnabled = true;
            }
            else
            {
                CardArt.ImageSource = null;
                CharacterCardNameTextBox.Text = "";
                CardNameUpdateBox.Text = "";
                CardNameUpdateBox.IsEnabled = false;
            }

            ReloadImages();

            if (modifiedUnit.Music is null)
            {
                CurrentPositionBox.Text = "";
                LoopPointBox.Text = "";
                return;
            }

            if (Path.GetExtension(modifiedUnit.Music.File) != ".ogg")
            {
                modifiedUnit.Music = null;
                return;
            }

            string mp3Path = Path.ChangeExtension(modifiedUnit.Music.File, "mp3");

            if (!File.Exists(mp3Path))
            {
                MusicReplaceButton.IsEnabled = false;
                MusicReplaceButton.Content = "Loading Music";
                MusicReplaceButton.IsEnabled = false;
                MusicReplaceButton.Content = "Loading Music";
                InputFile inFile = new(modifiedUnit.Music.File);
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
                    modifiedUnit.Music = null;
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

            musicPlayer.Pause().GetAwaiter().GetResult();

            if (modifiedUnit.Music is null)
            {
                await RefreshGrid();
                return;
            }

            LoopPointBox.Text = (modifiedUnit.Music.LoopPoint ?? 0).ToString();

            UpdateCurrentPosition(0);

            EnableMusicControls(true);
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

        private void ReplacePicture(string modifiedUnitCharacterCard, string[] paths256, string[]? paths128, Func<int> getIndex, Action incIndex)
        {
            int res = 256;
            string TempName() => $@"{mainWindow.Temp}\temp{modifiedUnitCharacterCard}{getIndex()}{res}.temp";
            OpenFileDialog a = new()
            {
                Filter =
                    "Portable Network Graphics (*.png)|*.png|Microsoft Direct Draw Surface (*.dds)|*.dds|All files (*.*)|*.*",
                Title = "Please select your image. Select a 256x256 png for best results.",
                Multiselect = true
            };
            if (a.ShowDialog() is not true)
            {
                return;
            }

            UnloadImages();

            MagickImage[] pictures = a.FileNames.Select(z => new MagickImage(z)).ToArray();

            if (pictures.Length % 2 == 1 || paths128 == null)
            {
                GeneralCase();
                return;
            }

            bool allSquares = pictures.All(z => z.Width == z.Height);
            if (!allSquares)
            {
                GeneralCase();
                return;
            }

            bool all256 = pictures.All(z => z.Width == 256);
            if (all256)
            {
                GeneralCase();
                return;
            }

            MagickImage[] pictures128 = pictures.Where(z => z.Width == 128).ToArray();
            MagickImage[] pictures256 = pictures.Where(z => z.Width == 256).ToArray();

            if (pictures128.Length != pictures256.Length)
            {
                //General case
                GeneralCase();
                return;
            }

            for (int n = 0; n < pictures128.Length; ++n, incIndex())
            {
                res = 256;
                pictures256[n].Write(TempName());
                paths256[getIndex()] = TempName();
                pictures256[n].Dispose();

                res = 128;
                pictures128[n].Write(TempName());
                paths128[getIndex()] = TempName();
                pictures128[n].Dispose();
            }

            void GeneralCase()
            {
                foreach (MagickImage m in pictures)
                {
                    bool type = m.Format == MagickFormat.Png;
                    bool dim = m.Width == 256 && m.Height == 256;
                    m.Format = MagickFormat.Png;
                    if (type && dim)
                    {
                        m.Write(TempName());
                    }
                    else if (!dim)
                    {
                        m.FilterType = FilterType.Point;
                        m.Resize(256, 256);
                        m.Write(TempName());
                    }
                    else
                    {
                        m.Write(TempName());
                    }

                    paths256[getIndex()] = TempName();
                    if (paths128 is not null)
                    {
                        m.FilterType = FilterType.Point;
                        m.Resize(128, 128);
                        m.Write($@"{mainWindow.Temp}\temp{modifiedUnitCharacterCard}128.temp");
                        paths128[getIndex()] = $@"{mainWindow.Temp}\temp{modifiedUnitCharacterCard}128.temp";
                    }

                    if (m != pictures.Last())
                    {
                        incIndex();
                    }
                    m.Dispose();
                }
            }
        }

        private void UnloadImages()
        {
            CharacterArt.ImageSource = null;
            HyperArt.ImageSource = null;
            CardArt.ImageSource = null;
            SmallHyperArt.ImageSource = null;
            SmallCardArt.ImageSource = null;
        }

        private void ReloadImages()
        {
            UnloadImages();
            if (modifiedUnit.CharacterArt.Any())
            {
                CharacterArt.ImageSource = GetUnitArt(modifiedUnit.CharacterArt[SelectedCharacter]);
                CharacterNameTextBox.Text = $"{modifiedUnit.UnitId}_{selectedCharacter:00}";
            }
            else
            {
                CharacterArt.ImageSource = null;
                CharacterNameTextBox.Text = "";
            }

            if (modifiedUnit.CharacterCards.Any())
            {
                CardArt.ImageSource = GetCardImageFromPath(modifiedUnit.CharacterCardPaths[SelectedCharacterCard]);
                SmallCardArt.ImageSource = GetCardImageFromPath(modifiedUnit.CharacterCardPathsLow[SelectedCharacterCard]);
            }
            else
            {
                CardArt.ImageSource = null;
                SmallCardArt.ImageSource = null;
            }

            if (modifiedUnit.HyperIds.Any())
            {
                HyperArt.ImageSource = GetCardImageFromPath(modifiedUnit.HyperCardPaths[SelectedHyper]);
                SmallHyperArt.ImageSource = GetCardImageFromPath(modifiedUnit.HyperCardPathsLow[SelectedHyper]);
            }
            else
            {
                HyperArt.ImageSource = null;
                SmallHyperArt.ImageSource = null;
            }

        }

        private void ReplaceFile(string newFile, int res, string id, out string? path)
        {
            path = null;
            string tempName = @$"{mainWindow.Temp}\{id}{res}.temp";
            using MagickImage image = new(newFile);

            if (image.Format is not (MagickFormat.Png or MagickFormat.Dds))
            {
                MessageBox.Show("Invalid format. Please use general replacement button for automatic conversion.");
                return;
            }

            if (image.Width != res || image.Height != res)
            {
                MessageBox.Show("Resolution incorrect. Please use general replacement button for automatic conversion");
                return;
            }

            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }

            File.Copy(newFile, tempName);
            path = tempName;
        }

        TimeSpan TickFromSamples(long samples) => TimeSpan.FromTicks(samples * 10000 / 43);

        private BitmapImage GetUnitArt(string x)
        {
            using MagickImage m = new(x);

            if (m.Format is MagickFormat.Dds)
            {
                m.Format = MagickFormat.Png;
                m.Write(x);
            }

            BitmapImage bi = new();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(File.ReadAllBytes(x));
            bi.EndInit();

            return bi;
        }

        private BitmapImage GetCardImageFromPath(string path)
        {
            using MagickImage m = new(path);

            if (m.Format is MagickFormat.Dds)
            {
                m.Format = MagickFormat.Png;
                m.Write(path);
            }

            BitmapImage bi = new();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(File.ReadAllBytes(path));
            bi.EndInit();

            return bi;
        }

        private void CheckForNumber(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.IsNumber();
        }
    }
}

