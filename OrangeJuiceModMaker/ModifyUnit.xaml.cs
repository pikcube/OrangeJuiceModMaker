using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using ImageMagick;
using Microsoft.Win32;
using NAudio.Wave;
using OrangeJuiceModMaker.Data;


namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyUnit.xaml
    /// </summary>
    public partial class ModifyUnit
    {
        //Variable Declaration
        private Unit selectedUnit;
        private readonly MainWindow mainWindow;
        private MyMusicPlayer MusicPlayer;
        private readonly List<ModifiedUnit> modifiedUnitHyperTable = [];
        private ModifiedUnit modifiedUnit;
        private int selectedHyper;
        private int selectedCharacter;
        private int selectedCharacterCard;
        private PlayState MediaPlayerState
        {
            /*
                        get => mediaPlayerState;
            */
            set
            {
                switch (value)
                {
                    case PlayState.Stop when MusicPlayer.Reader is not null:
                        MusicPlayer.Out.Volume = 0;
                        MusicPlayer.Out.Stop();

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
                        return;

                }
            }
        }
        private TimeSpan LoopPoint => TickFromSamples(modifiedUnit.Music?.LoopPoint ?? 0);
        private bool IsPlaying => PlayPauseButton.Content.ToString() == "▐▐";

        private int SelectedHyper
        {
            get => selectedHyper;
            set => selectedHyper = selectedUnit.HyperCards.Length == 0 ? 0 : (value + selectedUnit.HyperCards.Length) % selectedUnit.HyperCards.Length;
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
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            this.mainWindow = mainWindow;

            selectedUnit = mainWindow.Units.First();
            modifiedUnit = new ModifiedUnit(selectedUnit, mainWindow.LoadedModPath, mainWindow.LoadedModReplacements);
            MusicPlayer = new();
            MusicPlayer.PositionChanged += MusicPlayer_PositionChanged;
            MusicPlayer.EndOfSong += MusicPlayer_EndOfSong;
            InitializeComponent();
            if (mainWindow.Debug)
            {
            }
        }

        private void MusicPlayer_EndOfSong(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MediaPlayerState = PlayState.Pause;
            });
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.Message);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UnitSelectionBox.ItemsSource = mainWindow.Units.Select(z => z.UnitName).OrderBy(z => z);

            int index = mainWindow.Units.FindIndexOf(z => new ModifiedUnit(z, mainWindow.LoadedModPath, mainWindow.LoadedModReplacements).IsModified);
            if (index == -1)
            {
                index = 0;
            }

            UnitSelectionBox.SelectedIndex = index;
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
            MusicPlayer.Out.Stop();
            MusicPlayer.Reader?.Close();
            MusicPlayer.Dispose();
        }

        //On Unit Select
        private async void UnitSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitSelectionBox.IsEnabled == false)
            {
                e.Handled = false;
                return;
            }
            UnitSelectionBox.IsEnabled = false;
            //Guard Clause
            if (UnitSelectionBox.SelectedItem is not string unitName)
            {
                UnitSelectionBox.IsEnabled = true;
                return;
            }

            MediaPlayerState = PlayState.Stop;

            //Get Unit
            selectedUnit = mainWindow.Units.First(z => z.UnitName == unitName);
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

            long progress = (long)(MusicPlayer.Duration.Ticks * ProgressSlider.Value / 10);
            MusicPlayer.Position = TimeSpan.FromTicks(progress);
        }

        private void MusicPlayer_PositionChanged(object? sender, TimeSpan e)
        {
            Dispatcher.Invoke(() => UpdateCurrentPositionUi(e.Ticks));
        }


        private void UpdateCurrentPosition(long ticks)
        {
            if (MusicPlayer.Duration.Ticks == 0)
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
            ProgressSlider.Value = p is double.NaN ? 0 : p;
            ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            CurrentPositionBox.TextChanged -= CurrentPositionBox_OnTextChanged;
            CurrentPositionBox.Text = s;
            CurrentPositionBox.TextChanged += CurrentPositionBox_OnTextChanged;
        }

        //Refresh data
        private async Task RefreshGrid()
        {
            while (true)
            {
                SelectedCharacterCard = 0;
                SelectedHyper = 0;
                SelectedCharacter = 0;
                UnloadImages();
                EnableMusicControls(false);

                FaceXBox.Text = modifiedUnit.FaceX.First().ToString();
                FaceYBox.Text = modifiedUnit.FaceY.First().ToString();

                //Get Hyper Art
                if (selectedUnit.HyperCards.Length != 0)
                {
                    //HyperArt.ImageSource = GetCardImageFromPath(selectedUnit.HyperCardPaths.First());
                    HyperNameTextBox.Text = selectedUnit.HyperCards.First().CardName;
                    HyperNameUpdateBox.Text = modifiedUnit.HyperCards.First().CardName;
                    HyperFlavorUpdateBox.Text = modifiedUnit.HyperCards.First().FlavorText ?? "";
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

                bool hyperButtonsEnabled = selectedUnit.HyperCards.Length >= 2;
                bool cardButtonsEnabled = selectedUnit.CharacterCards.Length >= 2;

                HyperLeftButton.IsEnabled = hyperButtonsEnabled;
                HyperRightButton.IsEnabled = hyperButtonsEnabled;

                CharacterCardLeftButton.IsEnabled = cardButtonsEnabled;
                CharacterCardRightButton.IsEnabled = cardButtonsEnabled;

                CharacterNameTextBox.Text = selectedUnit.UnitId;

                HyperFlavorUpdateBox.IsEnabled = HyperFlavorUpdateBox.Text != "";

                //Get Card Art
                if (selectedUnit.CharacterCards.Length != 0)
                {
                    SelectedCharacterCard = 0;
                    CharacterCardNameTextBox.Text = selectedUnit.CharacterCards.First().CardName;
                    CardNameUpdateBox.Text = modifiedUnit.CharacterCards.First().CardName;
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
                    InputFile inFile = new(modifiedUnit.Music.File);
                    OutputFile outFile = new(mp3Path);
                    Engine ffmpeg = new($@"{mainWindow.AppData}\ffmpeg\ffmpeg.exe");
                    ConversionOptions options = new() { AudioSampleRate = AudioSampleRate.Hz44100 };
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
                        continue;
                    }
                }


                MusicPlayer.Open(mp3Path);



                MediaPlayerState = PlayState.Pause;

                if (modifiedUnit.Music is null)
                {
                    continue;
                }

                LoopPointBox.Text = (modifiedUnit.Music.LoopPoint ?? 0).ToString();
                VolumeBox.Text = (modifiedUnit.Music.Volume ?? 0).ToString();

                UpdateCurrentPosition(0);

                EnableMusicControls(true);
                break;
            }
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

        private void ReplacePicture(string modifiedUnitCharacterCard, out string[]? paths256, out string[]? paths128, int index, bool ignore128 = false)
        {
            int res = 256;
            string TempName() => $@"{mainWindow.Temp}\temp{modifiedUnitCharacterCard}{index}{res}.temp";
            OpenFileDialog a = new()
            {
                Filter =
                    "Portable Network Graphics (*.png)|*.png|Microsoft Direct Draw Surface (*.dds)|*.dds|All files (*.*)|*.*",
                Title = "Please select your image. Select a 256x256 png for best results.",
                Multiselect = true
            };
            if (a.ShowDialog() is not true)
            {
                paths256 = null;
                paths128 = null;
                return;
            }

            UnloadImages();

            MagickImage[] pictures = [.. a.FileNames.Select(z => new MagickImage(z))];

            if (pictures.Length % 2 == 1 || ignore128)
            {
                paths256 = new string[pictures.Length];
                paths128 = new string[pictures.Length];
                GeneralCase(ref paths256, ref paths128);
                return;
            }

            bool allSquares = pictures.All(z => z.Width == z.Height);
            if (!allSquares)
            {
                paths256 = new string[pictures.Length];
                paths128 = new string[pictures.Length];
                GeneralCase(ref paths256, ref paths128);
                return;
            }

            bool all256 = pictures.All(z => z.Width == 256);
            if (all256)
            {
                paths256 = new string[pictures.Length];
                paths128 = new string[pictures.Length];
                GeneralCase(ref paths256, ref paths128);
                return;
            }

            MagickImage[] pictures128 = [.. pictures.Where(z => z.Width == 128)];
            MagickImage[] pictures256 = [.. pictures.Where(z => z.Width == 256)];

            if (pictures128.Length != pictures256.Length)
            {
                //General case
                paths256 = new string[pictures.Length];
                paths128 = new string[pictures.Length];
                GeneralCase(ref paths256, ref paths128);
                return;
            }

            paths256 = new string[pictures256.Length];
            paths128 = new string[pictures128.Length];

            for (int n = 0; n < pictures128.Length; ++n)
            {
                res = 256;
                pictures256[n].Write(TempName());
                paths256[n] = TempName();
                pictures256[n].Dispose();

                res = 128;
                pictures128[n].Write(TempName());
                paths128[n] = TempName();
                pictures128[n].Dispose();
            }

            void GeneralCase(ref string[] paths256, ref string[] paths128)
            {
                for (int n = 0; n < pictures.Length; n++)
                {
                    MagickImage m = pictures[n];
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

                    paths256[n] = TempName();
                    if (!ignore128)
                    {
                        m.FilterType = FilterType.Point;
                        m.Resize(128, 128);
                        m.Write($@"{mainWindow.Temp}\temp{modifiedUnitCharacterCard}128.temp");
                        paths128[n] = $@"{mainWindow.Temp}\temp{modifiedUnitCharacterCard}128.temp";
                    }

                    if (m != pictures.Last())
                    {
                        ++n;
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
            if (modifiedUnit.CharacterArt.Length != 0)
            {
                CharacterArt.ImageSource = GetUnitArt(modifiedUnit.CharacterArt[SelectedCharacter]);
                CharacterNameTextBox.Text = $"{modifiedUnit.UnitId}_{selectedCharacter:00}";
            }
            else
            {
                CharacterArt.ImageSource = null;
                CharacterNameTextBox.Text = "";
            }

            if (modifiedUnit.CharacterCards.Length != 0)
            {
                CardArt.ImageSource = GetCardImageFromPath(modifiedUnit.CharacterCards[SelectedCharacterCard].Path);
                SmallCardArt.ImageSource = GetCardImageFromPath(modifiedUnit.CharacterCards[SelectedCharacterCard].PathLow);
            }
            else
            {
                CardArt.ImageSource = null;
                SmallCardArt.ImageSource = null;
            }

            if (modifiedUnit.HyperCards.Length != 0)
            {
                HyperArt.ImageSource = GetCardImageFromPath(modifiedUnit.HyperCards[SelectedHyper].Path);
                SmallHyperArt.ImageSource = GetCardImageFromPath(modifiedUnit.HyperCards[SelectedHyper].PathLow);
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

        private static TimeSpan TickFromSamples(long samples) => TimeSpan.FromTicks(samples * 10000 / 43);

        private static BitmapImage GetUnitArt(string x)
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

        private static BitmapImage GetCardImageFromPath(string path)
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

        private void ModifyUnit_OnUnloaded(object sender, RoutedEventArgs e)
        {

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

