using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OrangeJuiceModMaker
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private int? option = 4;
        public UpdateWindow()
        {
            InitializeComponent();
        }

        public int? GetOption()
        {
            ShowDialog();
            return option;
        }

        private void Option1Button(object sender, RoutedEventArgs e)
        {
            option = 1;
            Close();
        }
        private void Option2Button(object sender, RoutedEventArgs e)
        {
            option = 2;
            Close();
        }
        private void Option3Button(object sender, RoutedEventArgs e)
        {
            option = 3;
            Close();
        }
        private void Option4Button(object sender, RoutedEventArgs e)
        {
            option = 4;
            Close();
        }
    }
}
