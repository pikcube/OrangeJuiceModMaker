using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for NewMod.xaml
    /// </summary>
    public partial class NewMod
    {
        private readonly List<TextBox> singleLineBoxes = [];

        private readonly bool editMode;

        public string? NewModName;

        private readonly MainWindow window;

        public NewMod(MainWindow window, ModDefinition? d = null)
        {
            this.window = window;
            InitializeComponent();
            singleLineBoxes.Add(NameBox);
            singleLineBoxes.Add(AuthorBox);
            IsContest.ItemsSource = new[] { "True", "False" };
            IsContest.SelectedIndex = 1;
            Loaded += NewMod_Loaded;

            if (d is null)
            {
                return;
            }

            editMode = true;

            CreateButton.Content = "Save Mod";

            NameBox.Text = d.Name;
            AuthorBox.Text = d.Author;
            IsContest.SelectedIndex = d.Contest is true ? 0 : 1;
            if (d.Color is not null)
            {
                HBox.Text = d.Color;
            }
            DescriptionBox.Text = d.Description;
            ToggleChangeLog(true);

            ChangeBox.Text = d.Changelog ?? "";

        }

        private void ToggleChangeLog(bool enabled)
        {
            ChangeBox.IsEnabled = enabled;
            ChangeBox.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

            ch1.Height = enabled ? GridLength.Auto : new GridLength(0);
            ch2.Height = enabled ? new GridLength(10, GridUnitType.Pixel) : new GridLength(0);
            ch3.Height = enabled ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            ch4.Height = enabled ? new GridLength(10, GridUnitType.Pixel) : new GridLength(0);

            ViewLogButton.Content = enabled ? "Hide Changelog" : "View Changelog";
        }

        private static void NewMod_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private static double PixelToPoint(double pixels)
        {
            double result = pixels * 2 / 3;
            return result switch
            {
                double.NaN => 5,
                <= 0 => 1,
                _ => result
            };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (TextBox box in singleLineBoxes)
            {
                box.FontSize = PixelToPoint(box.ActualHeight);
            }

            IsContest.FontSize = PixelToPoint(IsContest.ActualHeight);

            CreateButton.FontSize = PixelToPoint(CreateButton.ActualHeight - 4);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ModDefinition mod = new(name: NameBox.Text, auth: AuthorBox.Text, sysVer: 2, desc: DescriptionBox.Text)
            {
                Contest = IsContest.SelectedItem.ToString() == "True",
                Color = colorDefault ? null : GetHex(RBox.Text.ToIntOrDefault(), GBox.Text.ToIntOrDefault(), BBox.Text.ToIntOrDefault()),
                Changelog = ChangeBox.Text,
            };

            NewModName = mod.Name;

            if (editMode)
            {
                window.LoadedModDefinition = mod;
                Root.WriteJson(window.LoadedModPath, window.LoadedModDefinition, window.LoadedModReplacements);
                Close();
                return;
            }

            Root root = new(mod);
            if (Directory.Exists($@"{MainWindow.GameDirectory}\mods\{mod.Name}") && Directory.GetFiles($@"{MainWindow.GameDirectory}\mods\{mod.Name}").Length != 0)
            {
                MessageBoxResult r = MessageBox.Show(this, "Mod already exists in mods directory. Delete and overwrite?", "Non empty directory exists", MessageBoxButton.YesNo);
                if (r == MessageBoxResult.Yes)
                {
                    foreach (string f in Directory.GetFiles($@"{MainWindow.GameDirectory}\mods\{mod.Name}"))
                    {
                        File.Delete(f);
                    }
                }
                else
                {
                    return;
                }
            }
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.Name}");
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.Name}\cards");
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.Name}\units");
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.Name}\music");
            File.WriteAllText($@"{MainWindow.GameDirectory}\mods\{mod.Name}\mod.json", JsonConvert.SerializeObject(root, Formatting.Indented));
            Close();
        }

        private bool textLock;
        private bool colorDefault = true;

        private void ColorTextChangedRgb(object sender, TextChangedEventArgs e)
        {
            if (textLock)
            {
                return;
            }

            if (ColorBrush is null)
            {
                return;
            }

            textLock = true;

            if (colorDefault)
            {
                RBox.Text = RBox.Text == "" ? "0" : RBox.Text;
                GBox.Text = GBox.Text == "" ? "0" : GBox.Text;
                BBox.Text = BBox.Text == "" ? "0" : BBox.Text;
                colorDefault = false;
            }

            const byte zero = byte.MinValue;
            ColorBrush.Color = new Color
            {
                A = byte.MaxValue,
                R = byte.TryParse(RBox.Text, out byte b) ? b : zero,
                G = byte.TryParse(GBox.Text, out b) ? b : zero,
                B = byte.TryParse(BBox.Text, out b) ? b : zero,
            };

            string h = GetHex(RBox.Text.ToIntOrDefault(), GBox.Text.ToIntOrDefault(), BBox.Text.ToIntOrDefault());
            if (HBox.Text != h)
            {
                HBox.Text = h;
            }

            if (sender is TextBox t)
            {
                if (t.Text != t.Text.ToIntOrDefault().ToString())
                {
                    int selection = t.SelectionStart - 1 < 1 ? 1 : t.SelectionStart - 1;
                    t.Text = t.Text.ToIntOrDefault().ToString();
                    t.SelectionStart = selection;
                }
            }

            textLock = false;
        }

        private static string GetHex(int r, int g, int b)
        {
            int[] values = new int[6];
            values[0] = r / 16;
            values[2] = g / 16;
            values[4] = b / 16;
            values[1] = r % 16;
            values[3] = g % 16;
            values[5] = b % 16;
            char[] hex = "0123456789ABCDEF".ToCharArray();
            return $"#{string.Join("", values.Select(z => hex[z]))}";
        }

        private void ColorTextChangedHex(object sender, TextChangedEventArgs e)
        {
            if (textLock)
            {
                return;
            }

            if (HBox.Text.Length > 0 && HBox.Text[0] != '#')
            {
                int selection = HBox.SelectionStart + 1;
                HBox.Text = "#" + HBox.Text;
                HBox.SelectionStart = selection;
                return;
            }

            if (ColorBrush is null)
            {
                return;
            }

            if (colorDefault)
            {
                textLock = true;
                RBox.Text = RBox.Text == "" ? "0" : RBox.Text;
                GBox.Text = GBox.Text == "" ? "0" : GBox.Text;
                BBox.Text = BBox.Text == "" ? "0" : BBox.Text;
                colorDefault = false;
                textLock = false;
            }

            if (HBox.Text.Length != 7)
            {
                return;
            }

            if (HBox.Text[0] != '#')
            {
                return;
            }

            char[] hex = "0123456789ABCDEF".ToCharArray();
            int[] values = new int[6];

            for (int n = 1; n < 7; ++n)
            {
                values[n - 1] = hex.ToList().IndexOf(HBox.Text[n]);
            }

            if (values.Any(z => z == -1))
            {
                return;
            }

            textLock = true;

            int[] rgb =
            [
                16 * values[0] + values[1],
                16 * values[2] + values[4],
                16 * values[3] + values[5]
            ];

            ColorBrush.Color = new Color
            {
                A = Byte.MaxValue,
                R = Convert.ToByte(rgb[0].ToString()),
                G = Convert.ToByte(rgb[1].ToString()),
                B = Convert.ToByte(rgb[2].ToString())
            };

            if (RBox.Text != rgb[0].ToString())
            {
                RBox.Text = rgb[0].ToString();
            }

            if (GBox.Text != rgb[1].ToString())
            {
                GBox.Text = rgb[1].ToString();
            }

            if (BBox.Text != rgb[2].ToString())
            {
                BBox.Text = rgb[2].ToString();
            }

            textLock = false;
        }

        private void CheckForNumber(object sender, TextCompositionEventArgs e) => e.Handled = e.Text.IsNumber();

        private void ViewLogButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleChangeLog(!ChangeBox.IsEnabled);
        }
    }
}
