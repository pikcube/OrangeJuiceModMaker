using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for NewMod.xaml
    /// </summary>
    public partial class NewMod : Window
    {
        private List<TextBox> _singleLineBoxes = new();

        public NewMod()
        {
            InitializeComponent();
            _singleLineBoxes.Add(nameBox);
            _singleLineBoxes.Add(authorBox);
            isContest.ItemsSource = new[] { "True", "False" };
            isContest.SelectedIndex = 1;
        }

        public double PixelToPoint(double pixels)
        {
            double result = pixels * 2 / 3;
            switch (result)
            {
                case double.NaN:
                    return 5;
                case <= 0:
                    return 1;
                default:
                    return result;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (TextBox box in _singleLineBoxes)
            {
                box.FontSize = PixelToPoint(box.ActualHeight);
            }

            isContest.FontSize = PixelToPoint(isContest.ActualHeight);

            CreateButton.FontSize = PixelToPoint(CreateButton.ActualHeight - 4);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ModDefinition mod = new()
            {
                name = nameBox.Text,
                author = authorBox.Text,
                contest = isContest.SelectedItem.ToString() == "True",
                //}
                //    mod.color = ModColor.SelectedColor.Value.ToString().Remove(1, 2);
                //{
                //if (ModColor.SelectedColor.HasValue)
                description = descriptionBox.Text,
                system_version = 2
            };
            Root root = new(mod);
            if (Directory.Exists($@"{MainWindow.GameDirectory}\mods\{mod.name}") && Directory.GetFiles($@"{MainWindow.GameDirectory}\mods\{mod.name}").Any())
            {
                MessageBoxResult r = MessageBox.Show(this, "Mod already exists in mods directory. Delete and overwrite?", "Non empty directory exists", MessageBoxButton.YesNo);
                if (r == MessageBoxResult.Yes)
                {
                    foreach (string f in Directory.GetFiles($@"{MainWindow.GameDirectory}\mods\{mod.name}"))
                    {
                        File.Delete(f);
                    }
                }
                else
                {
                    return;
                }
            }
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.name}");
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.name}\cards");
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.name}\units");
            Directory.CreateDirectory($@"{MainWindow.GameDirectory}\mods\{mod.name}\music");
            File.WriteAllText($@"{MainWindow.GameDirectory}\mods\{mod.name}\mod.json", JsonConvert.SerializeObject(root, Formatting.Indented));
            Close();
        }
    }
}
