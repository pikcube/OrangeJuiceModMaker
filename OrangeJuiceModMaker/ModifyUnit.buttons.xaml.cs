using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace OrangeJuiceModMaker
{
    public partial class ModifyUnit : Window
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
            modifiedUnit.SaveToMod();
            ReloadImages();
        }

        private void ReplacementHyperButton_OnClick(object sender, RoutedEventArgs e)
        {
            string modifiedUnitCharacterCard = modifiedUnit.HyperIds[SelectedHyper];
            string[] paths = modifiedUnit.HyperCardPaths;
            string[] pathsLow = modifiedUnit.HyperCardPathsLow;

            ReplacePicture(modifiedUnitCharacterCard, paths, pathsLow, () => SelectedHyper, () => ++SelectedHyper);
        }

        private void ReplacementCharacterCardButton_OnClick(object sender, RoutedEventArgs e)
        {
            string modifiedUnitCharacterCard = modifiedUnit.CharacterCards[SelectedCharacterCard];
            string[] paths = modifiedUnit.CharacterCardPaths;
            string[] pathsLow = modifiedUnit.CharacterCardPathsLow;

            ReplacePicture(modifiedUnitCharacterCard, paths, pathsLow, () => SelectedCharacterCard,
                () => ++SelectedCharacterCard);
        }

        private void ReplacementBoardButton_OnClick(object sender, RoutedEventArgs e)
        {
            string modifiedUnitCharacterCard =
                Path.GetFileNameWithoutExtension(modifiedUnit.CharacterArt[SelectedCharacter]);
            string[] paths = modifiedUnit.CharacterArt;

            ReplacePicture(modifiedUnitCharacterCard, paths, null, () => SelectedCharacter, () => ++SelectedCharacter);
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

            string tempPath = $@"{MainWindow.LoadedModPath}\music\{modifiedUnit.UnitId}{Path.GetFileNameWithoutExtension(o.FileName)}.temp";
            string mp3Path = $@"{MainWindow.LoadedModPath}\music\{modifiedUnit.UnitId}{Path.GetFileNameWithoutExtension(o.FileName)}.mp3";
            string oggPath = $@"{MainWindow.LoadedModPath}\music\{modifiedUnit.UnitId}{Path.GetFileNameWithoutExtension(o.FileName)}.ogg";

            File.Copy(Path.GetFullPath(o.FileName), tempPath, true);

            bool leave = false;

            while (!leave)
            {
                Task t = Task.Run(() =>
                {
                    var m = Task.Run(() =>
                    {
                        ProcessStartInfo psi = new()
                        {
                            FileName = "ffmpeg.exe",
                            Arguments = $"-i \"{tempPath}\" -ar 44100 \"{mp3Path}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        };
                        Process? p = Process.Start(psi);
                        p?.WaitForExit();
                    });

                    var t = Task.Run(() =>
                    {
                        ProcessStartInfo psi = new()
                        {
                            FileName = "ffmpeg.exe",
                            Arguments = $"-i \"{tempPath}\" -ar 44100 \"{oggPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        };
                        Process? p = Process.Start(psi);
                        p?.WaitForExit();
                    });
                    m.Wait();
                    t.Wait();
                });

                int KickingMachine = 30;
                while (KickingMachine > 0)
                {
                    if (t.IsCompleted)
                    {
                        break;
                    }
                    KickingMachine--;
                    await Task.Run(() => Thread.Sleep(1000));
                }
                if (t.IsCompleted)
                {
                    break;
                }

                MessageBoxResult a = MessageBox.Show(this,
                    "Media has failed to load. If you're file is really big, it may just not be done. " +
                    Environment.NewLine +
                    "Select Yes to keep waiting on your media." + Environment.NewLine +
                    "Select No to restart the process." + Environment.NewLine +
                    "Select cancel to stop replacing media", "ffmpeg hasn't returned", MessageBoxButton.YesNo);
                switch (a)
                {
                    case MessageBoxResult.Yes:
                        await t;
                        leave = true;
                        break;
                    case MessageBoxResult.No:
                        await KillFFMPEG();
                        break;
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    case MessageBoxResult.Cancel:
                    default:
                        leave = true;
                        await KillFFMPEG();
                        modifiedUnit.Music = null;
                        await RefreshGrid();
                        return;
                }

            }

            File.Delete(tempPath);
            modifiedUnit.Music = new Music(oggPath)
            {
                loop_point = 0,
                unit_id = modifiedUnit.UnitId,
                volume = 0,
            };
            musicPlayer.Open(new Uri(mp3Path, UriKind.RelativeOrAbsolute));
            LoopPointBox.Text = (modifiedUnit.Music.loop_point ?? 0).ToString();
            EnableMusicControls(true);
            MusicReplaceButton.IsEnabled = true;
            MusicReplaceButton.Content = "Replace with...";
        }

        private void EnableDangerZone_OnChecked(object sender, RoutedEventArgs e)
        {
            EnableDangerZone.IsChecked ??= false;

            resetAll.IsEnabled = EnableDangerZone.IsChecked.Value;
            resetHyperCard.IsEnabled = EnableDangerZone.IsChecked.Value;
            resetMusic.IsEnabled = EnableDangerZone.IsChecked.Value;
            resetPoses.IsEnabled = EnableDangerZone.IsChecked.Value;
            resetUnitCard.IsEnabled = EnableDangerZone.IsChecked.Value;
        }
        private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }

        private void SetLoopButtonClick(object sender, RoutedEventArgs e)
        {
            LoopPointBox.Text = CurrentPositionBox.Text;
        }
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            musicPlayer.Position -= TimeSpan.FromSeconds(7);
        }

        private void ButtonFFOnClick(object sender, RoutedEventArgs e)
        {
            musicPlayer.Position += TimeSpan.FromSeconds(30);
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
            modifiedUnit = new ModifiedUnit(selectedUnit, false);
            await RefreshGrid();
        }

        //Text Change Events
        private void CurrentPositionBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentPositionBox.Text == "")
            {
                return;
            }
            if (!isPlaying)
            {
                musicPlayer.Position = TickFromSamples(CurrentPositionBox.Text.ToLongOrDefault());
            }
        }

        private void LoopPointBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.Music == null)
            {
                return;
            }

            modifiedUnit.Music.loop_point = LoopPointBox.Text.ToIntOrNull();
        }

        private void VolumeBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (modifiedUnit.Music is not null)
            {
                modifiedUnit.Music.volume = VolumeBox.Text.ToIntOrNull();
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