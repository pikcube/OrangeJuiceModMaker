using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using Microsoft.Win32;

namespace OrangeJuiceModMaker
{
    public partial class ModifyUnit
    {
        //Buttons
        private void SmallCardButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                Title = "Select image. Image must be 128x128 png or dds",
                Filter = "Portable Network Graphic (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }
            UnloadImages();
            ReplaceFile(o.FileName, 128, modifiedUnit.CharacterCards[SelectedCharacterCard], out string? path);
            if (path is not null)
            {
                modifiedUnit.CharacterCardPathsLow[SelectedCharacterCard] = path;
            }
            ReloadImages();
        }

        private void LargeCardButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                Title = "Select image. Image must be 256x256 png or dds",
                Filter = "Portable Network Graphic (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }
            UnloadImages();
            ReplaceFile(o.FileName, 256, modifiedUnit.CharacterCards[SelectedCharacterCard], out string? path);
            if (path is not null)
            {
                modifiedUnit.CharacterCardPaths[SelectedCharacterCard] = path;
            }
            ReloadImages();
        }

        private void SmallHyperButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                Title = "Select image. Image must be 128x128 png or dds",
                Filter = "Portable Network Graphic (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };
            if (o.ShowDialog() is not true)
            {
                return;
            }
            UnloadImages();
            ReplaceFile(o.FileName, 128, modifiedUnit.HyperIds[SelectedCharacterCard], out string? path);
            if (path is not null)
            {
                modifiedUnit.HyperCardPathsLow[SelectedCharacterCard] = path;
            }
            ReloadImages();
        }

        private void LargeHyperButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                Title = "Select image. Image must be 256x256 png or dds",
                Filter = "Portable Network Graphic (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };
            UnloadImages();
            if (o.ShowDialog() is not true)
            {
                return;
            }
            ReplaceFile(o.FileName, 256, modifiedUnit.HyperIds[SelectedCharacterCard], out string? path);
            if (path is not null)
            {
                modifiedUnit.HyperCardPaths[SelectedCharacterCard] = path;
            }
            ReloadImages();
        }

        private void CharacterLeftButton_Click(object sender, RoutedEventArgs e)
        {
            --SelectedCharacter;
            FaceXBox.Text = modifiedUnit.FaceX[SelectedCharacter].ToString();
            FaceYBox.Text = modifiedUnit.FaceY[SelectedCharacter].ToString();
            ReloadImages();
        }

        private void CharacterRightButton_Click(object sender, RoutedEventArgs e)
        {
            ++SelectedCharacter;
            FaceXBox.Text = modifiedUnit.FaceX[SelectedCharacter].ToString();
            FaceYBox.Text = modifiedUnit.FaceY[SelectedCharacter].ToString();
            ReloadImages();
        }

        private void HyperRightButton_Click(object sender, RoutedEventArgs e)
        {
            ++SelectedHyper;
            HyperNameTextBox.Text = selectedUnit.HyperNames[SelectedHyper];
            HyperNameUpdateBox.Text = modifiedUnit.HyperNames[SelectedHyper];
            HyperFlavorUpdateBox.Text = modifiedUnit.HyperFlavor[SelectedHyper];
            ReloadImages();
        }

        private void HyperLeftButton_Click(object sender, RoutedEventArgs e)
        {
            --SelectedHyper;
            HyperNameTextBox.Text = selectedUnit.HyperNames[SelectedHyper];
            HyperNameUpdateBox.Text = modifiedUnit.HyperNames[SelectedHyper];
            HyperFlavorUpdateBox.Text = modifiedUnit.HyperFlavor[SelectedHyper];
            ReloadImages();
        }

        private void CharacterCardRightButton_OnClick(object sender, RoutedEventArgs e)
        {
            ++SelectedCharacterCard;
            CharacterCardNameTextBox.Text = selectedUnit.CharacterCardNames[SelectedCharacterCard];
            CardNameUpdateBox.Text = modifiedUnit.CharacterCardNames[SelectedCharacterCard];
            ReloadImages();
        }

        private void CharacterCardLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            --SelectedCharacterCard;
            CharacterCardNameTextBox.Text = selectedUnit.CharacterCardNames[SelectedCharacterCard];
            CardNameUpdateBox.Text = modifiedUnit.CharacterCardNames[SelectedCharacterCard];
            ReloadImages();
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            UnloadImages();
            modifiedUnit.SaveToMod(mainWindow.LoadedModPath, mainWindow.LoadedModDefinition, ref mainWindow.LoadedModReplacements);
            ReloadImages();
        }

        private void ReplacementHyperButton_OnClick(object sender, RoutedEventArgs e)
        {
            string modifiedUnitCharacterCard = modifiedUnit.HyperIds[SelectedHyper];
            string[] paths = modifiedUnit.HyperCardPaths;
            string[] pathsLow = modifiedUnit.HyperCardPathsLow;

            int pre = SelectedHyper;
            ReplacePicture(modifiedUnitCharacterCard, paths, pathsLow, () => SelectedHyper, () => ++SelectedHyper);
            SelectedHyper = pre;
            ReloadImages();
        }

        private void ReplacementCharacterCardButton_OnClick(object sender, RoutedEventArgs e)
        {
            string modifiedUnitCharacterCard = modifiedUnit.CharacterCards[SelectedCharacterCard];
            string[] paths = modifiedUnit.CharacterCardPaths;
            string[] pathsLow = modifiedUnit.CharacterCardPathsLow;

            int pre = SelectedCharacterCard;
            ReplacePicture(modifiedUnitCharacterCard, paths, pathsLow, () => SelectedCharacterCard,
                () => ++SelectedCharacterCard);
            SelectedCharacterCard = pre;
            ReloadImages();
        }

        private void ReplacementBoardButton_OnClick(object sender, RoutedEventArgs e)
        {
            string modifiedUnitCharacterCard =
                Path.GetFileNameWithoutExtension(modifiedUnit.CharacterArt[SelectedCharacter]);
            string[] paths = modifiedUnit.CharacterArt;

            int pre = SelectedCharacter;
            ReplacePicture(modifiedUnitCharacterCard, paths, null, () => SelectedCharacter, () => ++SelectedCharacter);
            SelectedCharacter = pre;
            ReloadImages();
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

            string tempPath = $@"{mainWindow.LoadedModPath}\music\{modifiedUnit.UnitId}{Path.GetFileNameWithoutExtension(o.FileName)}.temp";
            string mp3Path = $@"{mainWindow.LoadedModPath}\music\{modifiedUnit.UnitId}{Path.GetFileNameWithoutExtension(o.FileName)}.mp3";
            string oggPath = $@"{mainWindow.LoadedModPath}\music\{modifiedUnit.UnitId}{Path.GetFileNameWithoutExtension(o.FileName)}.ogg";

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
            modifiedUnit.Music = new Music(oggPath)
            {
                LoopPoint = 0,
                UnitId = modifiedUnit.UnitId,
                Volume = 0
            };
            
            MusicPlayer.Open(mp3Path);
            LoopPointBox.Text = (modifiedUnit.Music.LoopPoint ?? 0).ToString();
            EnableMusicControls(true);
            MusicReplaceButton.IsEnabled = true;
            MusicReplaceButton.Content = "Replace with...";
        }

        private void EnableDangerZone_OnChecked(object sender, RoutedEventArgs e)
        {
            EnableDangerZone.IsChecked ??= false;

            ResetAll.IsEnabled = EnableDangerZone.IsChecked.Value;
            ResetHyperCard.IsEnabled = EnableDangerZone.IsChecked.Value;
            ResetMusic.IsEnabled = EnableDangerZone.IsChecked.Value;
            ResetPoses.IsEnabled = EnableDangerZone.IsChecked.Value;
            ResetUnitCard.IsEnabled = EnableDangerZone.IsChecked.Value;
        }
        private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            MediaPlayerState = PlayPauseButton.Content.ToString() == "▶" ? PlayState.Play : PlayState.Pause;
        }

        private void SetLoopButtonClick(object sender, RoutedEventArgs e)
        {
            LoopPointBox.Text = CurrentPositionBox.Text;
        }
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Position -= TimeSpan.FromSeconds(7);
        }

        private void ButtonFFOnClick(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Position += TimeSpan.FromSeconds(30);
        }

        private async void ResetUnitCard_OnClick(object sender, RoutedEventArgs e)
        {
            modifiedUnit.CharacterCardNames[SelectedCharacterCard] = selectedUnit.CharacterCardNames[SelectedCharacterCard];
            modifiedUnit.CharacterCardPaths[SelectedCharacterCard] = selectedUnit.CharacterCardPaths[SelectedCharacterCard];
            modifiedUnit.CharacterCards[SelectedCharacterCard] = selectedUnit.CharacterCards[SelectedCharacterCard];
            await RefreshGrid();
        }

        private async void ResetHyperCard_OnClick(object sender, RoutedEventArgs e)
        {
            modifiedUnit.HyperFlavor[SelectedCharacterCard] = selectedUnit.HyperFlavor[SelectedCharacterCard];
            modifiedUnit.HyperNames[SelectedCharacterCard] = selectedUnit.HyperNames[SelectedCharacterCard];
            modifiedUnit.HyperCardPaths[SelectedCharacterCard] = selectedUnit.HyperCardPaths[SelectedCharacterCard];
            await RefreshGrid();
        }

        private async void ResetPoses_OnClick(object sender, RoutedEventArgs e)
        {
            for (int n = 0; n < modifiedUnit.CharacterArt.Length; ++n)
            {
                modifiedUnit.CharacterArt[n] = selectedUnit.CharacterArt[n];
                modifiedUnit.FaceX[n] = 0;
                modifiedUnit.FaceY[n] = 0;
            }

            await RefreshGrid();
        }

        private async void ResetMusic_OnClick(object sender, RoutedEventArgs e)
        {
            modifiedUnit.Music = null;
            await RefreshGrid();
        }

        private async void ResetAll_OnClick(object sender, RoutedEventArgs e)
        {
            modifiedUnit = new ModifiedUnit(selectedUnit, mainWindow.LoadedModPath, mainWindow.LoadedModReplacements,
                false);
            await RefreshGrid();
        }

        //Text Change Events
        private void CurrentPositionBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentPositionBox.Text == "")
            {
                return;
            }
            if (!IsPlaying)
            {
                MusicPlayer.Position = TickFromSamples(CurrentPositionBox.Text.ToLongOrDefault());
            }
        }

        private void LoopPointBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.Music == null)
            {
                return;
            }

            modifiedUnit.Music.LoopPoint = LoopPointBox.Text.ToIntOrNull();
            if (modifiedUnit.Music.LoopPoint is not null)
            {
                MusicPlayer.LoopPoint = TickFromSamples(modifiedUnit.Music.LoopPoint.Value);
            }
        }

        private void VolumeBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.Music is not null)
            {
                modifiedUnit.Music.Volume = VolumeBox.Text.ToIntOrNull();
            }
        }

        private void CardNameUpdateBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.CharacterCardNames.Length == 0)
            {
                return;
            }
            modifiedUnit.CharacterCardNames[SelectedCharacterCard] = CardNameUpdateBox.Text;
        }

        private void HyperNameUpdateBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.HyperNames.Length == 0)
            {
                return;
            }
            modifiedUnit.HyperNames[SelectedHyper] = HyperNameUpdateBox.Text;
        }

        private void HyperFlavorUpdateBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.HyperFlavor.Length == 0)
            {
                return;
            }
            modifiedUnit.HyperFlavor[SelectedHyper] = HyperFlavorUpdateBox.Text;
        }

        private void FaceXBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {

            if (modifiedUnit.FaceX.Length == 0)
            {
                return;
            }
            modifiedUnit.FaceX[SelectedCharacter] = FaceXBox.Text.ToIntOrDefault();
        }

        private void FaceYBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.FaceY.Length == 0)
            {
                return;
            }
            modifiedUnit.FaceY[SelectedCharacter] = FaceYBox.Text.ToIntOrDefault();
        }
    }
}