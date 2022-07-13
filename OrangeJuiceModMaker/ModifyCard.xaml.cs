using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.Win32;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for ModifyCard.xaml
    /// </summary>
    public partial class ModifyCard
    {
        private readonly CsvHolder[] files;
        private string[][] loadedRows;
        private ModTexture? loadedTexture;
        private readonly List<ModTexture> modifiedTextures = new();
        private ModReplacements replacements;
        private readonly CsvHolder flavorLookUp;
        private readonly string modPath;
        private readonly ModDefinition definition;
        public ModifyCard(List<CsvHolder> csvFiles, MainWindow window)
        {
            modPath = window.LoadedModPath;
            files = csvFiles.Where(z => z.Type == CsvHolder.TypeList.Card).Where(z => z.Rows.Any()).ToArray();
            InitializeComponent();
            loadedRows = files.First().Rows;
            replacements = window.LoadedModReplacements;
            flavorLookUp = window.FlavorLookUp ?? throw new Exception("Missing flavor lookup");
            definition = window.LoadedModDefinition ?? throw new Exception("Mod Definition not loaded");

        }

        private void ModifyCard_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (Texture t in replacements.Textures.Where(z => z.Path.ToLower().StartsWith("cards")))
            {
                modifiedTextures.Add(new ModTexture(t.Path, replacements, modPath));
            }
            
            if (modifiedTextures.Any())
            {
                SetSelectionBox.ItemsSource = files.Select(z => z.Name);
                ModTexture itemToSelect = modifiedTextures.First();
                SetSelectionBox.SelectedItem = files.Last(z => z.Rows.Any(y => y[1] == itemToSelect.Id)).Name;
                CardSelectionBox.SelectedItem = loadedRows.First(z => z[1] == itemToSelect.Id)[0];
            }
            else
            {
                SetSelectionBox.ItemsSource = files.Select(z => z.Name);
                SetSelectionBox.SelectedIndex = Math.Min(11, files.Length - 1);
            }
        }

        private void UnitSelectionBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CardSelectionBox.SelectedIndex == -1)
            {
                return;
            }
            if (modifiedTextures.All(z => z.Id != loadedRows[CardSelectionBox.SelectedIndex][1]))
            {
                modifiedTextures.Add(new ModTexture($@"cards\{loadedRows[CardSelectionBox.SelectedIndex][1]}", replacements, modPath));
            }
            loadedTexture = modifiedTextures.First(z => z.Id == loadedRows[CardSelectionBox.SelectedIndex][1]);
            CardName.Text = loadedTexture.CustomName ?? loadedRows[CardSelectionBox.SelectedIndex][0];
            FlavorUpdateBox.Text = loadedTexture.CustomFlavor ?? flavorLookUp.Rows.FirstOrDefault(z => z[1] == loadedRows[CardSelectionBox.SelectedIndex][1])?[3] ?? "";
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

        private void SetSelectionBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            loadedRows = files[SetSelectionBox.SelectedIndex].Rows.OrderBy(z => z[0]).ToArray();
            CardSelectionBox.ItemsSource = loadedRows.Select(z => z[0]);
            CardSelectionBox.SelectedIndex = 0;
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
