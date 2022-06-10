using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ImageMagick;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyUnit.xaml
    /// </summary>
    public partial class ModifyUnit : Window
    {
        //Variable Declaration
        private Unit selectedUnit = MainWindow.UnitHyperTable.First();
        private readonly List<ModifiedUnit> ModifiedUnitHyperTable = new();
        private ModifiedUnit modifiedUnit;
        private int _selectedHyper;
        private int _selectedCharacter;
        private int _selectedCharacterCard;
        private MediaPlayer musicPlayer;
        private DispatcherTimer musicTimer;
        private TimeSpan loopPoint => TickFromSamples(modifiedUnit.Music?.loop_point ?? 0);
        private bool isPlaying => PlayPauseButton.Content.ToString() == "▐▐";
        public int RefreshRetries = 30;

        private int SelectedHyper
        {
            get => _selectedHyper;
            set => _selectedHyper = selectedUnit.HyperIds.Length == 0 ? 0 : (value + selectedUnit.HyperIds.Length) % selectedUnit.HyperIds.Length;
        }
        private int SelectedCharacter
        { 
            get => _selectedCharacter;
            set => _selectedCharacter = selectedUnit.CharacterArt.Length == 0
                ? 0
                : (value + selectedUnit.CharacterArt.Length) % selectedUnit.CharacterArt.Length;
        }

        private int SelectedCharacterCard
        {
            get => _selectedCharacterCard;
            set => _selectedCharacterCard = selectedUnit.CharacterCards.Length == 0 ? 0 : (value + selectedUnit.CharacterCards.Length) % selectedUnit.CharacterCards.Length;
        }

        //On Load
        public ModifyUnit()
        {
            modifiedUnit = new ModifiedUnit(selectedUnit);
            ModifiedUnitHyperTable.Add(modifiedUnit);
            musicPlayer = new MediaPlayer();
            musicPlayer.MediaEnded += MusicPlayer_MediaEnded;
            musicTimer = new DispatcherTimer();
            musicTimer.Tick += MusicTimer_Tick;
            musicPlayer.BufferingEnded += MusicPlayer_BufferingEnded;
            musicTimer.Interval = TimeSpan.FromMilliseconds(1);
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UnitSelectionBox.ItemsSource = MainWindow.UnitHyperTable.Select(z => z.UnitName).OrderBy(z => z);
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
            musicTimer.Stop();
            musicPlayer.Stop();
            musicPlayer.Close();
        }

        //On Unit Select
        private async void UnitSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Guard Clause
            if (UnitSelectionBox.SelectedItem is not string unitName)
            {
                return;
            }

            //Get Unit
            selectedUnit = MainWindow.UnitHyperTable.First(z => z.UnitName == unitName);
            if (ModifiedUnitHyperTable.All(z => z.UnitName != unitName))
            {
                ModifiedUnitHyperTable.Add(new ModifiedUnit(selectedUnit));
            }

            modifiedUnit = ModifiedUnitHyperTable.First(z => z.UnitName == unitName);
            await RefreshGrid();
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
        private void PlayPause()
        {
            if (PlayPauseButton.Content.ToString() == "▶")
            {
                PlayPauseButton.Content = "▐▐";
                musicPlayer.Play();
                musicTimer.Start();
            }
            else
            {
                PlayPauseButton.Content = "▶";
                musicPlayer.Pause();
                musicTimer.Stop();
                UpdateCurrentPosition();
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            long Progress = (long)(musicPlayer.NaturalDuration.TimeSpan.Ticks * ProgressSlider.Value / 10);
            musicPlayer.Position = TimeSpan.FromTicks(Progress);
        }

        private void MusicPlayer_BufferingEnded(object? sender, EventArgs e)
        {

        }

        private void MusicPlayer_MediaEnded(object? sender, EventArgs e)
        {
            if (PreviewLoopCheckBox.IsChecked is not true || modifiedUnit.Music?.loop_point is null)
            {
                musicPlayer.Position = TimeSpan.Zero;
                PlayPause();
                return;
            }
            musicPlayer.Position = loopPoint;
            musicPlayer.Play();
        }

        private void MusicTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCurrentPosition();
        }

        private void UpdateCurrentPosition()
        {
            var t = musicPlayer.Position.Ticks;
            var s = SamplesFromTicks(t);
            CurrentPositionBox.Text = s.ToString();
            double Progress = (double)musicPlayer.Position.Ticks * 10 / musicPlayer.NaturalDuration.TimeSpan.Ticks;
            ProgressSlider.Value = Progress;
        }

        //Refresh data
        private async Task RefreshGrid()
        {
            SelectedCharacterCard = 0;
            SelectedHyper = 0;
            SelectedCharacter = 0;
            UnloadImages();
            EnableMusicControls(false);
            musicPlayer.Stop();

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

            bool HyperButtonsEnabled = selectedUnit.HyperIds.Length >= 2;
            bool CardButtonsEnabled = selectedUnit.CharacterCards.Length >= 2;

            HyperLeftButton.IsEnabled = HyperButtonsEnabled;
            HyperRightButton.IsEnabled = HyperButtonsEnabled;

            CharacterCardLeftButton.IsEnabled = CardButtonsEnabled;
            CharacterCardRightButton.IsEnabled = CardButtonsEnabled;

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

            if (Path.GetExtension(modifiedUnit.Music.file) != ".ogg")
            {
                modifiedUnit.Music = null;
                return;
            }

            string mp3Path = Path.ChangeExtension(modifiedUnit.Music.file, "mp3");

            if (!File.Exists(mp3Path))
            {
                MusicReplaceButton.IsEnabled = false;
                MusicReplaceButton.Content = "Loading Music";
                await Task.Run(() =>
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "ffmpeg.exe",
                        Arguments = $"-i \"{modifiedUnit.Music.file}\" -ar 44100 \"{mp3Path}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    };
                    Process? p = Process.Start(psi);
                    p?.WaitForExit();
                });
                if (File.Exists(mp3Path))
                {
                    MusicReplaceButton.IsEnabled = true;
                    MusicReplaceButton.Content = "Replace with...";
                }
                else
                {
                    MessageBox.Show("Media failed to load. To retry, close and reopen window.");
                    modifiedUnit.Music = null;
                    await KillFFMPEG();
                    await RefreshGrid();
                    return;
                }
            }

            musicPlayer.Open(new Uri(mp3Path, UriKind.RelativeOrAbsolute));

            LoopPointBox.Text = (modifiedUnit.Music.loop_point ?? 0).ToString();

            EnableMusicControls(true);
        }

        private async Task KillFFMPEG()
        {
            await File.WriteAllTextAsync("killffmpeg.bat", "taskkill /f /im ffmpeg.exe");
            await Process.Start("killffmpeg.bat").WaitForExitAsync();
            File.Delete("killffmpeg.bat");
        }

        private void EnableMusicControls(bool musicEnabled)
        {
            PreviewLoopCheckBox.IsEnabled = musicEnabled;
            SetLoopButton.IsEnabled = musicEnabled;
            VolumeBox.IsEnabled = musicEnabled;
            LoopPointBox.IsEnabled = musicEnabled;
            CurrentPositionBox.IsEnabled = musicEnabled;
            FFButton.IsEnabled = musicEnabled;
            PlayPauseButton.IsEnabled = musicEnabled;
            RWButton.IsEnabled = musicEnabled;
            ProgressSlider.IsEnabled = musicEnabled;
        }

        private void ReplacePicture(string modifiedUnitCharacterCard, string[] paths256, string[]? paths128, Func<int> getIndex, Action incIndex)
        {
            string tempName = $@"temp\temp{modifiedUnitCharacterCard}256.temp";
            OpenFileDialog a = new()
            {
                Filter =
                    "Portable Network Graphics (*.png)|*.png|Microsoft Direct Draw Surface (*.dds)|*.dds|All files (*.*)|*.*",
                Title = "Please select your image. Select a 256x256 png for best results."
            };
            if (a.ShowDialog() is not true)
            {
                return;
            }

            UnloadImages();

            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }

            foreach (string f in a.FileNames)
            {
                using MagickImage m = new(f);
                bool type = m.Format == MagickFormat.Png;
                bool dim = m.Width == 256 && m.Height == 256;
                m.Format = MagickFormat.Png;
                if (type && dim)
                {
                    File.Copy(f, tempName);
                }
                else if (!dim)
                {
                    m.FilterType = FilterType.Point;
                    m.Resize(256, 256);
                    m.Write(tempName);
                }
                else
                {
                    m.Write(tempName);
                }
                paths256[getIndex()] = tempName;
                if (paths128 is not null)
                {
                    m.FilterType = FilterType.Point;
                    m.Resize(128, 128);
                    m.Write($@"temp\temp{modifiedUnitCharacterCard}128.temp");
                    paths128[getIndex()] = $@"temp\temp{modifiedUnitCharacterCard}128.temp";
                }
                if (f != a.FileNames.Last())
                {
                    incIndex();
                }
            }

            ReloadImages();
        }

        private void UnloadImages()
        {
            CharacterArt.ImageSource = null;
            HyperArt.ImageSource = null;
            CardArt.ImageSource = null;
        }

        private void ReloadImages()
        {
            if (modifiedUnit.CharacterArt.Any())
            {
                CharacterArt.ImageSource = GetUnitArt(modifiedUnit.CharacterArt[SelectedCharacter]);
            }
            else
            {
                CharacterArt.ImageSource = null;
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
            string tempName = @$"temp\{id}{res}.temp";
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

        long SamplesFromTicks(long Ticks) => 43 * Ticks / 10000;
        TimeSpan TickFromSamples(long Samples) => TimeSpan.FromTicks(Samples * 10000 / 43);

        private BitmapImage GetUnitArt(string x)
        {
            using MagickImage m = new(x);
            
            if (m.Format is MagickFormat.Dds)
            {
                m.Format = MagickFormat.Png;
                m.Write(x);
            }

            return new BitmapImage(new Uri(x, UriKind.RelativeOrAbsolute));
        }

        private BitmapImage GetCardImageFromPath(string path)
        {
            using MagickImage m = new(path);

            if (m.Format is MagickFormat.Dds)
            {
                m.Format = MagickFormat.Png;
                m.Write(path);
            }

            return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }

        private void CheckForNumber(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.IsNumber();
        }
    }

    public static class MyExtensions
    {
        public static bool IsNumber(this string text) => new Regex("[^0-9]+").IsMatch(text);
        
        public static bool IsInteger(this string text) => int.TryParse(text, out _);
        public static int ToInt(this string text) => int.Parse(text);
        public static int? ToIntOrNull(this string text) => int.TryParse(text, out int value) ? value : null;
        public static int ToIntOrDefault(this string text) => int.TryParse(text, out int value) ? value : 0;

        public static bool IsLong(this string text) => long.TryParse(text, out _);
        public static long ToLong(this string text) => long.Parse(text);
        public static long? ToLongOrNull(this string text) => long.TryParse(text, out long value) ? value : null;
        public static long ToLongOrDefault(this string text) => long.TryParse(text, out long value) ? value : 0;
    }
}

