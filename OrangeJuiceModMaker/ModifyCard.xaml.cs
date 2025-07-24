using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.Win32;
using OrangeJuiceModMaker.Data;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyCard.xaml
    /// </summary>
    public partial class ModifyCard
    {
        private ModTexture? loadedTexture;
        private readonly CardRef[] cards;
        private readonly List<ModTexture> modifiedTextures = [];
        private ModReplacements replacements;
        private readonly string modPath;
        private readonly ModDefinition definition;
        public ModifyCard(MainWindow window)
        {
            modPath = window.LoadedModPath;
            InitializeComponent();
            definition = window.LoadedModDefinition ?? throw new Exception("Mod Definition not loaded");
            replacements = window.LoadedModReplacements;
            cards = window.Cards;

        }

        private void ModifyCard_OnLoaded(object sender, RoutedEventArgs e)
        {
            CardSelectionBox.ItemsSource = cards.Select(z => z.CardName);
            foreach (Texture t in replacements.Textures.Where(z => z.Path.ToLower().StartsWith("cards")))
            {
                modifiedTextures.Add(new ModTexture(t.Path, replacements, modPath));
            }

            if (modifiedTextures.Any())
            {

                ModTexture itemToSelect = modifiedTextures.First();
                CardSelectionBox.SelectedItem = cards.First(z => z.CardId == itemToSelect.Id).CardName;
            }
            else
            {

            }
        }

        private void UnitSelectionBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CardSelectionBox.SelectedIndex == -1)
            {
                return;
            }
            if (modifiedTextures.All(z => z.Id != cards[CardSelectionBox.SelectedIndex].CardId))
            {
                modifiedTextures.Add(new ModTexture($@"cards\{cards[CardSelectionBox.SelectedIndex].CardId}", replacements, modPath));
            }
            loadedTexture = modifiedTextures.First(z => z.Id == cards[CardSelectionBox.SelectedIndex].CardId);
            CardName.Text = loadedTexture.CustomName ?? cards[CardSelectionBox.SelectedIndex].CardName;
            FlavorUpdateBox.Text = loadedTexture.CustomFlavor ?? cards[CardSelectionBox.SelectedIndex].FlavorText ?? "";
            ReloadArt();
            FlavorUpdateBox.IsEnabled = FlavorUpdateBox.Text != "";


        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            loadedTexture?.SaveToMod(modPath, definition, ref replacements);
        }

        private void ReplacementCharacterCardButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (loadedTexture is null)
            {
                return;
            }

            OpenFileDialog o = new()
            {
                Title = "Select image. For best results select 256x256 png",
                Filter = "Portable Network Graphics (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };

            if (o.ShowDialog() is not true)
            {
                return;
            }

            CardArt.ImageSource = null;
            LowCardArt.ImageSource = null;

            string newFile = o.FileName;
            string tempName = @$"temp\{loadedTexture.Id}256.temp";
            string tempSmall = @$"temp\{loadedTexture.Id}128.temp";
            using MagickImage image = new(newFile);
            image.Format = MagickFormat.Png;
            if (image.Width != 256 || image.Height != 256)
            {
                image.FilterType = FilterType.Point;
                image.Resize(256, 256);
            }

            if (File.Exists(tempName))
            {
                File.Delete(tempName);
            }
            image.Write(tempName);
            image.FilterType = FilterType.Point;
            image.Resize(128, 128);
            if (File.Exists(tempSmall))
            {
                File.Delete(tempSmall);
            }
            image.Write(tempSmall);
            loadedTexture.CurrentArtPath = tempName;
            loadedTexture.CurrentLowArtPath = tempSmall;

            ReloadArt();

        }

        private void CardName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (loadedTexture is null)
            {
                return;
            }
            loadedTexture.CustomName = CardName.Text;
        }

        private void FlavorUpdateBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (loadedTexture is null)
            {
                return;
            }
            loadedTexture.CustomFlavor = FlavorUpdateBox.Text;
        }

        private void ReplaceSmallButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (loadedTexture is null)
            {
                return;
            }
            OpenFileDialog o = new()
            {
                Title = "Select 128x128 png or dds",
                Filter = "Portable Network Graphics (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };

            if (o.ShowDialog() is not true)
            {
                return;
            }

            CardArt.ImageSource = null;
            LowCardArt.ImageSource = null;

            ReplaceFile(o.FileName, 128);

            ReloadArt();
        }

        private void ReplaceLargeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (loadedTexture is null)
            {
                return;
            }
            OpenFileDialog o = new()
            {
                Title = "Select 256x256 png or dds",
                Filter = "Portable Network Graphics (*.png)|*.png|DirectDraw Surface (*.dds)|*.dds|All Files (*.*)|*.*"
            };

            if (o.ShowDialog() is not true)
            {
                return;
            }

            CardArt.ImageSource = null;
            LowCardArt.ImageSource = null;

            ReplaceFile(o.FileName, 256);

            ReloadArt();
        }

        private void ReloadArt()
        {
            if (loadedTexture is null)
            {
                return;
            }

            BitmapImage cardArtBi = new();
            BitmapImage lowArtBi = new();
            cardArtBi.BeginInit();
            lowArtBi.BeginInit();
            cardArtBi.StreamSource = new MemoryStream(File.ReadAllBytes(loadedTexture.CurrentArtPath));
            lowArtBi.StreamSource = new MemoryStream(File.ReadAllBytes(loadedTexture.CurrentLowArtPath));
            cardArtBi.EndInit();
            lowArtBi.EndInit();
            CardArt.ImageSource = cardArtBi;
            LowCardArt.ImageSource = lowArtBi;
        }

        private void ReplaceFile(string newFile, int res)
        {
            if (loadedTexture is null)
            {
                return;
            }
            string tempName = @$"temp\{loadedTexture.Id}{res}.temp";
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

            if (image.Format is MagickFormat.Png)
            {
                File.Copy(newFile, tempName);
            }
            else
            {
                image.Format = MagickFormat.Png;
                image.Write(tempName);
            }
            loadedTexture.CurrentArtPath = tempName;
        }
    }
}
